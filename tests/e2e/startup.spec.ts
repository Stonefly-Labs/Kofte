import { spawn, spawnSync, type ChildProcess } from "node:child_process";
import { randomUUID } from "node:crypto";
import { mkdtempSync, realpathSync, rmSync, writeFileSync } from "node:fs";
import { createServer } from "node:net";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { DatabaseSync } from "node:sqlite";
import { fileURLToPath } from "node:url";
import { expect, test, type Page, type TestInfo } from "@playwright/test";
import { isReadiness } from "../../packages/bootstrap-contract/src/status";

const repository = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const delay = (milliseconds: number) =>
  new Promise<void>((done) => setTimeout(done, milliseconds));
const configuredOwners = [
  "org.example.alpha",
  "org.example.beta",
  "org.example.no-storage",
];

function compositionPublication(output: string) {
  const owners = new Set<string>();
  let published = false;
  for (const line of output.split("\n").slice(0, -1)) {
    let record: unknown;
    try {
      record = JSON.parse(line);
    } catch (error) {
      if (!(error instanceof SyntaxError)) throw error;
    }
    // A parser error can contain private output; fail without retaining it as a cause.
    if (record === undefined)
      throw new Error("startup_composition_output_invalid");
    if (
      !record ||
      typeof record !== "object" ||
      !("stage" in record) ||
      !("outcome" in record) ||
      !("owner" in record) ||
      record.outcome !== "passed"
    )
      continue;
    if (
      record.stage === "started" &&
      typeof record.owner === "string" &&
      configuredOwners.includes(record.owner)
    )
      owners.add(record.owner);
    if (
      record.stage === "published" &&
      record.owner === null &&
      "code" in record &&
      record.code === "composition_ready"
    )
      published = true;
  }
  return { owners: [...owners].sort(), published };
}

async function freePort(port = 0): Promise<number> {
  const listener = createServer();
  await new Promise<void>((done, reject) => {
    listener.once("error", reject);
    listener.listen(port, "127.0.0.1", done);
  });
  const address = listener.address();
  await new Promise<void>((done, reject) =>
    listener.close((error) => (error ? reject(error) : done())),
  );
  if (!address || typeof address === "string")
    throw new Error("startup_port_reservation_failed");
  return address.port;
}

function signalGroup(
  child: Pick<ChildProcess, "pid">,
  signal: NodeJS.Signals | 0,
): boolean {
  if (!child.pid) return false;
  try {
    process.kill(-child.pid, signal);
    return true;
  } catch (error) {
    if (error instanceof Error && "code" in error) {
      if (error.code === "ESRCH") return false;
      // Darwin can deny a probe while an owned zombie awaits reaping.
      if (signal === 0 && error.code === "EPERM") return true;
    }
    throw new Error("startup_owned_group_signal_failed", { cause: error });
  }
}

async function stop(
  children: ChildProcess[],
  gracefulMilliseconds = 5000,
): Promise<void> {
  for (const child of children) signalGroup(child, "SIGTERM");
  for (const signal of [0, "SIGKILL"] as const) {
    if (signal) {
      for (const child of children) {
        if (signalGroup(child, 0)) signalGroup(child, signal);
      }
    }
    const deadline = performance.now() + (signal ? 5000 : gracefulMilliseconds);
    while (
      children.some((child) => signalGroup(child, 0)) &&
      performance.now() < deadline
    )
      await delay(25);
    if (!children.some((child) => signalGroup(child, 0))) return;
  }
  throw new Error("startup_owned_group_cleanup_timeout");
}

async function waitReady(
  url: string,
  children: ChildProcess[],
  failed: Set<ChildProcess>,
  deadline: number,
  configured = false,
): Promise<void> {
  while (performance.now() < deadline) {
    if (
      children.some(
        (child) =>
          failed.has(child) ||
          child.exitCode !== null ||
          child.signalCode !== null,
      )
    )
      throw new Error("startup_owned_child_exited");
    let response: Response;
    try {
      response = await fetch(url, {
        signal: AbortSignal.timeout(1000),
        redirect: "error",
      });
    } catch (error) {
      if (
        !(error instanceof TypeError) &&
        !(error instanceof DOMException && error.name === "TimeoutError")
      )
        throw error;
      await delay(50);
      continue;
    }
    const payload: unknown = await response.json();
    if (
      configured &&
      isReadiness(payload, response.status) &&
      response.status === 503 &&
      payload.state === "unavailable" &&
      payload.component === "host"
    ) {
      await delay(50);
      continue;
    }
    if (
      response.status !== 200 ||
      !isReadiness(payload, response.status) ||
      payload.state !== "ready"
    )
      throw new Error("startup_readiness_contract_failed");
    return;
  }
  throw new Error("startup_readiness_timeout");
}

test("owned group cleanup waits for denied probes but refuses denied signals", () => {
  const original = process.kill;
  process.kill = () => {
    throw Object.assign(new Error("denied"), { code: "EPERM" });
  };
  try {
    const child = { pid: process.pid };
    expect(signalGroup(child, 0)).toBe(true);
    expect(() => signalGroup(child, "SIGTERM")).toThrow(
      "startup_owned_group_signal_failed",
    );
    expect(() => signalGroup(child, "SIGKILL")).toThrow(
      "startup_owned_group_signal_failed",
    );
  } finally {
    process.kill = original;
  }
});

test("configured startup waits only for valid transient composition unavailability", async () => {
  const original = globalThis.fetch;
  const unavailable = {
    version: "1",
    state: "unavailable",
    component: "host",
    correlationId: "a".repeat(32),
  };
  try {
    let calls = 0;
    globalThis.fetch = async () =>
      ++calls === 1
        ? Response.json(unavailable, { status: 503 })
        : Response.json({
            ...unavailable,
            state: "ready",
            component: "storage",
          });
    await waitReady(
      "http://127.0.0.1:1",
      [],
      new Set(),
      performance.now() + 1000,
      true,
    );
    expect(calls).toBe(2);
    for (const [payload, configured] of [
      [unavailable, false],
      [{ ...unavailable, component: "storage" }, true],
      [{ ...unavailable, version: "invalid" }, true],
    ] as const) {
      globalThis.fetch = async () => Response.json(payload, { status: 503 });
      await expect(
        waitReady(
          "http://127.0.0.1:1",
          [],
          new Set(),
          performance.now() + 1000,
          configured,
        ),
      ).rejects.toThrow("startup_readiness_contract_failed");
    }
  } finally {
    globalThis.fetch = original;
  }
});

test("configured startup requires completed module starts and publication", () => {
  const starts = configuredOwners.map((owner) => ({
    owner,
    stage: "started",
    outcome: "passed",
  }));
  const published = {
    owner: null,
    stage: "published",
    outcome: "passed",
    code: "composition_ready",
  };
  const output = (records: unknown[]) =>
    records.map((record) => JSON.stringify(record)).join("\n") + "\n";
  expect(compositionPublication(output([...starts, published]))).toEqual({
    owners: configuredOwners,
    published: true,
  });
  expect(compositionPublication(output([published])).owners).toEqual([]);
  expect(compositionPublication(output(starts)).published).toBe(false);
  expect(
    compositionPublication(output([...starts, published]).trimEnd()).published,
  ).toBe(false);
  expect(() => compositionPublication("private-output-canary\n")).toThrow(
    "startup_composition_output_invalid",
  );
  expect(
    compositionPublication(output([{ ...starts[0], outcome: "progress" }])),
  ).toEqual({ owners: [], published: false });
});

async function warmRestarts(
  page: Page,
  info: TestInfo,
  configured: boolean,
): Promise<void> {
  const cycles = configured
    ? 2
    : Number(process.env.BOOTSTRAP_WARM_STARTS ?? "2");
  if (!Number.isInteger(cycles) || cycles < 2 || cycles > 10)
    throw new Error("startup_cycle_count_invalid");
  test.setTimeout(cycles * 320000 + 30000);
  const hostPort = await freePort();
  let webPort = await freePort();
  while (webPort === hostPort) webPort = await freePort();
  const host = `http://127.0.0.1:${hostPort}`;
  const web = `http://127.0.0.1:${webPort}`;
  const root = mkdtempSync(join(realpathSync(tmpdir()), "kofte-startup-"));
  const dataRoot = join(root, "data");
  const environment = Object.fromEntries(
    Object.entries(process.env).filter(
      ([key]) =>
        !key.startsWith("KOFTE_") &&
        !key.startsWith("OTEL_") &&
        !key.startsWith("COMPOSITION_TEST_"),
    ),
  );
  Object.assign(environment, {
    KOFTE_PROFILE: "local-sqlite",
    KOFTE_HOST_URL: host,
    KOFTE_WEB_URL: web,
    KOFTE_BACKEND_ORIGIN: host,
    KOFTE_DATA_ROOT: dataRoot,
    ASPNETCORE_ENVIRONMENT: "Development",
    DOTNET_ENVIRONMENT: "Development",
    NEXT_TELEMETRY_DISABLED: "1",
  });
  const assembly = join(
    repository,
    "src/Kofte.Host/bin/Release/net10.0/Kofte.Host.dll",
  );
  const runId = randomUUID(),
    fixtureKey = randomUUID(),
    payload = randomUUID();
  const observations: {
    cycle: number;
    readySeconds: number;
    pageSeconds: number;
    fixtureMatches: boolean;
    totalSeconds?: number;
    modulesPublished?: boolean;
  }[] = [];
  let stopped = true;
  try {
    const initialized = spawnSync("dotnet", [assembly, "--store", "init"], {
      cwd: repository,
      env: environment,
      timeout: 10000,
      stdio: "pipe",
    });
    if (initialized.error || initialized.status !== 0)
      throw new Error("startup_store_initialization_failed");
    const seeded = spawnSync(
      "dotnet",
      [
        join(
          repository,
          "tests/contract/Kofte.Storage.Consumer/bin/Release/net10.0/Kofte.Storage.Consumer.dll",
        ),
        "--provider",
        "sqlite",
        "--scenario",
        "clean-install",
      ],
      { cwd: repository, env: environment, timeout: 120000, stdio: "pipe" },
    );
    if (seeded.error || seeded.status !== 0 || seeded.stderr.length !== 0)
      throw new Error("startup_storage_fixture_failed");
    const identityPath = join(root, "identity.json");
    writeFileSync(
      identityPath,
      JSON.stringify({
        version: "1",
        mode: "development",
        publicOrigin: web,
        keyRingDirectory: join(root, "identity-keys"),
        development: { enabled: true, identities: [{ key: "synthetic" }] },
      }),
      { mode: 0o600, flag: "wx" },
    );
    environment.KOFTE_AUTH_CONFIG_FILE = identityPath;
    for (const [command, operation, failure] of [
      ["--registry", "upgrade", "startup_registry_upgrade_failed"],
      ["--identity", "prepare", "startup_identity_preparation_failed"],
    ]) {
      const prepared = spawnSync("dotnet", [assembly, command, operation], {
        cwd: repository,
        env: environment,
        timeout: 120000,
        stdio: "pipe",
      });
      if (prepared.error || prepared.status !== 0) throw new Error(failure);
    }
    const registry = new DatabaseSync(join(dataRoot, "platform/platform.db"), {
      readOnly: true,
    });
    let allocationKey: string;
    try {
      const owner = registry
        .prepare(
          "SELECT allocation_key FROM __kofte_module_registrations WHERE module_id = ? AND state = 'ready' AND migration_state = 'current'",
        )
        .get("org.example.consumer-alpha-clean-install");
      if (
        typeof owner?.allocation_key !== "string" ||
        !/^m_[a-z0-9_]{1,24}_[0-9a-f]{32}$/.test(owner.allocation_key)
      )
        throw new Error("startup_storage_assignment_invalid");
      allocationKey = owner.allocation_key;
    } finally {
      registry.close();
    }
    const databasePath = join(dataRoot, "modules", allocationKey, "module.db");
    const fixturePayload = JSON.stringify({ runId, fixtureKey, payload });
    const database = new DatabaseSync(databasePath);
    try {
      expect(
        database
          .prepare('UPDATE "Records" SET "Payload" = ? WHERE "Id" = 1')
          .run(fixturePayload).changes,
      ).toBe(1);
    } finally {
      database.close();
    }
    if (configured) {
      const selection = join(root, "module-selection.json");
      writeFileSync(
        selection,
        JSON.stringify({
          schemaVersion: "1",
          modules: [
            { id: "org.example.alpha", enabled: true, settings: { seed: 7 } },
            { id: "org.example.beta", enabled: true, settings: { seed: 11 } },
            {
              id: "org.example.no-storage",
              enabled: true,
              settings: { seed: 13 },
            },
          ],
        }),
        { mode: 0o600 },
      );
      Object.assign(environment, {
        KOFTE_MODULES_FILE: selection,
        COMPOSITION_TEST_CATALOG: "BootDisabled",
      });
    }
    for (let cycle = 1; cycle <= cycles; cycle++) {
      const children: ChildProcess[] = [];
      const failed = new Set<ChildProcess>();
      let compositionOutput = "";
      let compositionBytes = 0;
      const start = (command: string, args: string[]) => {
        const child = spawn(command, args, {
          cwd: repository,
          env: environment,
          detached: true,
          stdio: ["ignore", "pipe", "pipe"],
        });
        children.push(child);
        child.on("error", () => failed.add(child));
        if (configured && command === "dotnet") {
          child.stdout?.on("data", (bytes: Buffer) => {
            compositionBytes += bytes.length;
            if (compositionBytes > 65536) failed.add(child);
            else compositionOutput += bytes.toString("utf8");
          });
          child.stdout?.on("error", () => failed.add(child));
        }
        child.stdout?.resume();
        child.stderr?.resume();
      };
      const began = performance.now(),
        deadline = began + 285000;
      stopped = false;
      try {
        start(
          "dotnet",
          configured
            ? [
                join(
                  repository,
                  "tests/contract/Kofte.Composition.Consumer/bin/Release/net10.0/Kofte.Composition.Consumer.dll",
                ),
                "--deployment",
              ]
            : [assembly],
        );
        await waitReady(
          `${host}/health/ready`,
          children,
          failed,
          deadline,
          configured,
        );
        start("node", [
          join(repository, "node_modules/next/dist/bin/next"),
          "start",
          join(repository, "apps/web"),
          "--hostname",
          "127.0.0.1",
          "--port",
          String(webPort),
        ]);
        await waitReady(
          `${web}/backend/health/ready`,
          children,
          failed,
          deadline,
        );
        if (configured) {
          await expect
            .poll(() => compositionPublication(compositionOutput), {
              timeout: Math.max(1, deadline - performance.now()),
            })
            .toEqual({ owners: configuredOwners, published: true });
          expect(failed.size).toBe(0);
        }
        const readySeconds = (performance.now() - began) / 1000;
        const navigation = performance.now();
        const remaining = () => Math.max(1, deadline - performance.now());
        await page.goto(web, { timeout: remaining() });
        await expect(
          page.getByRole("heading", { name: "Köfte", exact: true }),
        ).toBeVisible({ timeout: remaining() });
        await expect(page.getByRole("status")).toContainText(
          "Local host is ready",
          { timeout: remaining() },
        );
        observations.push({
          cycle,
          readySeconds,
          pageSeconds: (performance.now() - navigation) / 1000,
          fixtureMatches: false,
          ...(configured ? { modulesPublished: true } : {}),
        });
        expect(
          readySeconds,
          "owned runtime is ready within the work budget",
        ).toBeLessThan(285);
        expect(
          observations[observations.length - 1].pageSeconds,
          "the built ready page is usable within two seconds",
        ).toBeLessThan(2);
      } finally {
        await stop(children, configured ? 15000 : 5000);
        stopped = true;
      }
      const totalSeconds = (performance.now() - began) / 1000;
      if (configured)
        observations[observations.length - 1].totalSeconds = totalSeconds;
      expect(
        totalSeconds,
        "startup, usable page and owned cleanup fit the existing total budget",
      ).toBeLessThan(300);
      const persisted = new DatabaseSync(databasePath, { readOnly: true });
      try {
        const row = persisted
          .prepare('SELECT "Payload" FROM "Records" WHERE "Id" = 1')
          .get();
        const matches = row?.Payload === fixturePayload;
        expect(matches, "exact run-owned fixture survives restart").toBe(true);
        observations[observations.length - 1].fixtureMatches = matches;
      } finally {
        persisted.close();
      }
      await freePort(hostPort);
      await freePort(webPort);
    }
  } finally {
    try {
      const report = info.outputPath(
        configured
          ? "composition-startup-observations.json"
          : "startup-observations.json",
      );
      writeFileSync(report, JSON.stringify(observations), { mode: 0o600 });
      await info.attach("startup-observations", {
        path: report,
        contentType: "application/json",
      });
    } finally {
      if (stopped) rmSync(root, { recursive: true });
    }
  }
}

test("built runtime preserves a private fixture through warm restarts", async ({
  page,
}, info) => warmRestarts(page, info, false));

test("configured-modules preserve a private fixture within the existing stack envelope", async ({
  page,
}, info) => warmRestarts(page, info, true));
