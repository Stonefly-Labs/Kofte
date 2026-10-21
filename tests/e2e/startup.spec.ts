import { spawn, spawnSync, type ChildProcess } from "node:child_process";
import { randomUUID } from "node:crypto";
import { mkdtempSync, realpathSync, rmSync, writeFileSync } from "node:fs";
import { createServer } from "node:net";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { DatabaseSync } from "node:sqlite";
import { fileURLToPath } from "node:url";
import { expect, test } from "@playwright/test";
import { isReadiness } from "../../packages/bootstrap-contract/src/status";

const repository = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const delay = (milliseconds: number) =>
  new Promise<void>((done) => setTimeout(done, milliseconds));

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

async function stop(children: ChildProcess[]): Promise<void> {
  for (const child of children) signalGroup(child, "SIGTERM");
  for (const signal of [0, "SIGKILL"] as const) {
    if (signal) {
      for (const child of children) {
        if (signalGroup(child, 0)) signalGroup(child, signal);
      }
    }
    const deadline = performance.now() + 5000;
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

test("built runtime preserves a private fixture through warm restarts", async ({
  page,
}, info) => {
  const cycles = Number(process.env.BOOTSTRAP_WARM_STARTS ?? "2");
  if (!Number.isInteger(cycles) || cycles < 2 || cycles > 10)
    throw new Error("startup_cycle_count_invalid");
  test.setTimeout(cycles * 320000 + 30000);
  const hostPort = await freePort();
  let webPort = await freePort();
  while (webPort === hostPort) webPort = await freePort();
  const host = `http://127.0.0.1:${hostPort}`;
  const web = `http://127.0.0.1:${webPort}`;
  const root = mkdtempSync(join(realpathSync(tmpdir()), "kofte-startup-"));
  const environment = Object.fromEntries(
    Object.entries(process.env).filter(
      ([key]) => !key.startsWith("KOFTE_") && !key.startsWith("OTEL_"),
    ),
  );
  Object.assign(environment, {
    KOFTE_PROFILE: "local-sqlite",
    KOFTE_HOST_URL: host,
    KOFTE_WEB_URL: web,
    KOFTE_BACKEND_ORIGIN: host,
    KOFTE_DATA_ROOT: root,
    ASPNETCORE_ENVIRONMENT: "Development",
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
    const databasePath = join(root, "platform/platform.db");
    const database = new DatabaseSync(databasePath);
    try {
      database.exec(
        "CREATE TABLE bootstrap_fixture (run_id TEXT NOT NULL, fixture_key TEXT PRIMARY KEY, payload TEXT NOT NULL)",
      );
      database
        .prepare("INSERT INTO bootstrap_fixture VALUES (?, ?, ?)")
        .run(runId, fixtureKey, payload);
    } finally {
      database.close();
    }
    for (let cycle = 1; cycle <= cycles; cycle++) {
      const children: ChildProcess[] = [];
      const failed = new Set<ChildProcess>();
      const start = (command: string, args: string[]) => {
        const child = spawn(command, args, {
          cwd: repository,
          env: environment,
          detached: true,
          stdio: ["ignore", "pipe", "pipe"],
        });
        children.push(child);
        child.on("error", () => failed.add(child));
        child.stdout?.resume();
        child.stderr?.resume();
      };
      const began = performance.now(),
        deadline = began + 285000;
      stopped = false;
      try {
        start("dotnet", [assembly]);
        await waitReady(`${host}/health/ready`, children, failed, deadline);
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
        await stop(children);
        stopped = true;
      }
      const persisted = new DatabaseSync(databasePath, { readOnly: true });
      try {
        const row = persisted
          .prepare(
            "SELECT run_id, payload FROM bootstrap_fixture WHERE fixture_key = ?",
          )
          .get(fixtureKey);
        const matches = row?.run_id === runId && row.payload === payload;
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
      const report = info.outputPath("startup-observations.json");
      writeFileSync(report, JSON.stringify(observations), { mode: 0o600 });
      await info.attach("startup-observations", {
        path: report,
        contentType: "application/json",
      });
    } finally {
      if (stopped) rmSync(root, { recursive: true });
    }
  }
});
