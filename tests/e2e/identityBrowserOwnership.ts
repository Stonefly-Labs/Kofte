import { execFile } from "node:child_process";
import { createHash } from "node:crypto";
import { lstat, realpath, rm } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { promisify } from "node:util";

export interface BrowserProcess {
  pid: number;
  parent: number;
  group: number;
  uid: number;
  session: number;
  state: string;
  started: string;
  command: string;
}

interface ProcessProbe {
  read(known?: readonly number[]): Promise<BrowserProcess[]>;
  signal(pid: number, signal: "SIGTERM" | "SIGKILL"): void;
}

const execute = promisify(execFile);
const wait = (milliseconds: number) =>
  new Promise<void>((done) => setTimeout(done, milliseconds));
type OwnershipPhase =
  | "owned_inventory_invalid"
  | "owned_profile_changed"
  | "owned_process_birth_changed"
  | "owned_process_command_changed"
  | "owned_process_parent_changed"
  | "owned_process_group_unverified";
class OwnershipError extends Error {
  readonly phase: OwnershipPhase;

  constructor(phase: OwnershipPhase) {
    super("identity_browser_ownership_required");
    this.phase = phase;
  }
}
const ownershipFailure = (phase: OwnershipPhase = "owned_inventory_invalid") =>
  new OwnershipError(phase);
const cleanupFailure = () => new Error("identity_browser_cleanup_unverified");

async function processTable(
  selection: string[],
  commands: boolean,
): Promise<BrowserProcess[]> {
  let absent = false;
  const result = await execute(
    "/bin/ps",
    [
      ...selection,
      "-o",
      "pid=,ppid=,pgid=,uid=,stat=,lstart=" + (commands ? ",args=" : ""),
    ],
    {
      env: { LC_ALL: "C", TZ: "UTC" },
      timeout: 500,
      maxBuffer: 4 * 1024 * 1024,
    },
  ).catch((error: unknown) => {
    absent =
      commands &&
      error instanceof Error &&
      "code" in error &&
      error.code === 1 &&
      "stdout" in error &&
      error.stdout === "" &&
      "stderr" in error &&
      error.stderr === "";
    return undefined;
  });
  if (absent) return [];
  if (!result) throw ownershipFailure();
  const processes: BrowserProcess[] = [];
  for (const line of result.stdout.split("\n").filter((line) => line.trim())) {
    const match =
      /^\s*(\d+)\s+(-?\d+)\s+(-?\d+)\s+(-?\d+)\s+(\S+)\s+((?:Mon|Tue|Wed|Thu|Fri|Sat|Sun)\s+(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+\d{1,2}\s+\d{2}:\d{2}:\d{2}\s+\d{4})(?:\s+(.+))?\s*$/.exec(
        line,
      );
    if (!match || (commands && !match[7])) throw ownershipFailure();
    const numbers = match.slice(1, 5).map(Number);
    if (numbers.some((value) => !Number.isSafeInteger(value)))
      throw ownershipFailure();
    processes.push({
      pid: numbers[0],
      parent: numbers[1],
      group: numbers[2],
      uid: numbers[3],
      session: 0,
      state: match[5],
      started: match[6].replace(/\s+/g, " "),
      command: match[7] ?? "",
    });
  }
  if (new Set(processes.map(({ pid }) => pid)).size !== processes.length)
    throw ownershipFailure();
  return processes;
}

async function readProcesses(pid: number, known: readonly number[] = []) {
  const inventory = await processTable(["-axww"], false);
  const queried = [
    ...new Set([
      pid,
      ...known,
      ...inventory
        .filter((entry) => entry.uid === process.getuid?.())
        .map((entry) => entry.pid),
    ]),
  ];
  if (queried.length > 4096) throw ownershipFailure();
  const result = await execute(
    "python3",
    [
      "-I",
      "-B",
      "-c",
      "import json,os,sys\nresult={}\nfor pid in json.loads(sys.argv[1]):\n try: result[str(pid)]=os.getsid(pid)\n except ProcessLookupError: result[str(pid)]=None\njson.dump(result,sys.stdout)",
      JSON.stringify(queried),
    ],
    {
      env: { PATH: process.env.PATH, LC_ALL: "C" },
      timeout: 500,
      maxBuffer: 65536,
    },
  ).catch(() => undefined);
  if (!result) throw ownershipFailure();
  let sessions: unknown;
  try {
    sessions = JSON.parse(result.stdout);
  } catch {
    throw ownershipFailure();
  }
  if (
    !sessions ||
    typeof sessions !== "object" ||
    Array.isArray(sessions) ||
    Object.keys(sessions).length !== queried.length ||
    queried.some((id) => !Object.hasOwn(sessions, String(id)))
  )
    throw ownershipFailure();
  const sessionByPid = new Map<number, number | null>();
  for (const id of queried) {
    const value: unknown = (sessions as Record<string, unknown>)[String(id)];
    if (
      value !== null &&
      (!Number.isSafeInteger(value) || typeof value !== "number" || value <= 0)
    )
      throw ownershipFailure();
    sessionByPid.set(id, value as number | null);
  }
  const selected = new Set([pid, ...known]);
  let added: boolean;
  do {
    added = false;
    for (const entry of inventory) {
      if (
        selected.has(entry.pid) ||
        (!selected.has(entry.parent) && sessionByPid.get(entry.pid) !== pid)
      )
        continue;
      if (selected.size >= 64) throw ownershipFailure();
      selected.add(entry.pid);
      added = true;
    }
  } while (added);
  const current = await processTable(
    ["-ww", "-p", [...selected].join(",")],
    true,
  );
  return [
    ...inventory.filter((entry) => !selected.has(entry.pid)),
    ...current
      .filter((entry) => sessionByPid.get(entry.pid) !== null)
      .map((entry) => {
        const session = sessionByPid.get(entry.pid);
        if (!session) throw ownershipFailure();
        return { ...entry, session };
      }),
  ];
}

function fingerprint(process: BrowserProcess, includeCommand = true) {
  return createHash("sha256")
    .update(
      JSON.stringify([
        process.pid,
        process.group,
        process.uid,
        process.session,
        process.started,
        includeCommand ? process.command : null,
      ]),
    )
    .digest("hex");
}

async function profileIdentity(profile: string) {
  const parent = dirname(profile);
  const found = await Promise.all([
    lstat(profile),
    lstat(parent),
    realpath(profile),
  ]).catch(() => undefined);
  if (!found) throw ownershipFailure("owned_profile_changed");
  const [directory, container, canonical] = found;
  if (
    profile !== resolve(profile) ||
    canonical !== profile ||
    !directory.isDirectory() ||
    directory.isSymbolicLink() ||
    !container.isDirectory() ||
    container.isSymbolicLink() ||
    directory.uid !== process.getuid?.() ||
    container.uid !== directory.uid ||
    (directory.mode & 0o077) !== 0 ||
    (container.mode & 0o077) !== 0
  )
    throw ownershipFailure("owned_profile_changed");
  return [directory.dev, directory.ino, container.dev, container.ino].join(":");
}

export class IdentityBrowserOwnership {
  private readonly tracked = new Map<
    number,
    { birth: string; command: string }
  >();
  private readonly profile: string;
  private readonly profileStamp: string;
  private readonly owner: BrowserProcess;
  private readonly probe: ProcessProbe;
  private closing: Promise<void> | undefined;
  private phase: ((name: string) => void) | undefined;
  private readonly changedCommands = new Set<number>();
  private protocol: Promise<void> | undefined;
  private protocolRejected = false;
  private deferredProtocol = false;

  private constructor(
    profile: string,
    profileStamp: string,
    owner: BrowserProcess,
    probe: ProcessProbe,
  ) {
    this.profile = profile;
    this.profileStamp = profileStamp;
    this.owner = owner;
    this.probe = probe;
    this.track(owner);
  }

  private track(entry: BrowserProcess) {
    this.tracked.set(entry.pid, {
      birth: fingerprint(entry, false),
      command: fingerprint(entry),
    });
  }

  static async observe(
    profile: string,
    executable: string,
    pid: number,
    probe: ProcessProbe = {
      read: (known) => readProcesses(pid, known),
      signal: (ownedPid, signal) => process.kill(ownedPid, signal),
    },
  ) {
    const stamp = await profileIdentity(profile);
    const inventory = await probe.read();
    const owner = inventory.find((entry) => entry.pid === pid);
    const argument = `--user-data-dir=${profile}`;
    if (
      !owner ||
      pid <= 1 ||
      pid === process.pid ||
      pid === process.ppid ||
      owner.parent !== process.pid ||
      owner.group !== pid ||
      owner.uid !== process.getuid?.() ||
      owner.session !== pid ||
      owner.state.startsWith("Z") ||
      !owner.command.startsWith(executable + " ") ||
      owner.command.split(argument).length !== 2 ||
      !(
        owner.command.includes(" " + argument + " ") ||
        owner.command.endsWith(" " + argument)
      )
    )
      throw ownershipFailure();
    const ownership = new IdentityBrowserOwnership(
      profile,
      stamp,
      owner,
      probe,
    );
    await ownership.live();
    return ownership;
  }

  private async live() {
    if ((await profileIdentity(this.profile)) !== this.profileStamp)
      throw ownershipFailure("owned_profile_changed");
    const inventory = await this.probe.read([...this.tracked.keys()]);
    const byPid = new Map(inventory.map((entry) => [entry.pid, entry]));
    const live = new Map<number, BrowserProcess>();
    for (const [pid, stamp] of this.tracked) {
      const current = byPid.get(pid);
      if (!current) continue;
      if (fingerprint(current, false) !== stamp.birth)
        throw ownershipFailure("owned_process_birth_changed");
      if (current.session !== this.owner.pid)
        throw ownershipFailure("owned_process_group_unverified");
      if (
        fingerprint(current) !== stamp.command &&
        !this.changedCommands.has(pid)
      ) {
        this.changedCommands.add(pid);
        this.phase?.("owned_process_command_changed");
      }
      if (pid === this.owner.pid && current.parent !== process.pid)
        throw ownershipFailure("owned_process_parent_changed");
      live.set(pid, current);
    }
    let added: boolean;
    do {
      added = false;
      for (const current of inventory) {
        if (
          live.has(current.pid) ||
          (!live.has(current.parent) && current.session !== this.owner.pid)
        )
          continue;
        if (
          current.uid !== this.owner.uid ||
          current.session !== this.owner.pid ||
          this.tracked.size >= 64
        )
          throw ownershipFailure("owned_process_group_unverified");
        this.track(current);
        live.set(current.pid, current);
        added = true;
      }
    } while (added);
    if (
      inventory.some(
        (entry) => entry.group === this.owner.group && !live.has(entry.pid),
      )
    )
      throw ownershipFailure();
    return [...live.values()];
  }

  async requireStopped() {
    if ((await this.live()).length !== 0) throw cleanupFailure();
  }

  async removeProfile() {
    await this.requireStopped();
    const removed = await rm(this.profile, { recursive: true }).then(
      () => true,
      () => false,
    );
    const retained = await lstat(this.profile).then(
      () => true,
      (error: unknown) =>
        error instanceof Error && "code" in error && error.code === "ENOENT"
          ? false
          : undefined,
    );
    if (!removed || retained !== false) throw cleanupFailure();
  }

  close(
    closeContext: () => Promise<void>,
    phase: (name: string) => void,
    transportClosed: () => boolean = () => false,
  ) {
    return (this.closing ??= this.closeCore(
      closeContext,
      phase,
      transportClosed,
    ).catch((error: unknown) => {
      if (error instanceof OwnershipError) phase(error.phase);
      throw error;
    }));
  }

  async requireProtocolComplete() {
    if (!this.protocol) throw cleanupFailure();
    await this.protocol;
    if (this.protocolRejected) throw cleanupFailure();
    if (this.deferredProtocol) this.phase?.("owned_close_finished");
  }

  private async closeCore(
    closeContext: () => Promise<void>,
    phase: (name: string) => void,
    transportClosed: () => boolean,
  ) {
    this.phase = phase;
    await this.live();
    const deadline = performance.now() + 5000;
    let settled = false;
    let rejected = false;
    const closing = closeContext().then(
      () => {
        settled = true;
        phase("owned_context_close_completed");
      },
      () => {
        settled = true;
        rejected = true;
        this.protocolRejected = true;
        phase("owned_context_close_failed");
      },
    );
    this.protocol = closing;
    const settle = async (milliseconds: number) => {
      let timer: ReturnType<typeof setTimeout> | undefined;
      try {
        await Promise.race([
          closing,
          new Promise<void>((done) => {
            timer = setTimeout(done, milliseconds);
          }),
        ]);
      } finally {
        clearTimeout(timer);
      }
    };
    const awaitExit = async (milliseconds: number) => {
      const expires = Math.min(deadline, performance.now() + milliseconds);
      while ((await this.live()).length !== 0) {
        if (performance.now() >= expires) return false;
        await wait(25);
      }
      return true;
    };
    phase("owned_close_started");
    await settle(750);
    if (!settled) phase("owned_context_close_pending");
    let exited = await awaitExit(0);
    for (const signal of ["SIGTERM", "SIGKILL"] as const) {
      if (exited) break;
      phase(
        signal === "SIGTERM"
          ? "owned_termination_started"
          : "owned_kill_started",
      );
      const targets = await this.live();
      for (const target of targets) {
        if (performance.now() >= deadline) throw cleanupFailure();
        const current = (await this.live()).find(
          (entry) => entry.pid === target.pid,
        );
        if (!current || current.state.startsWith("Z")) continue;
        let failed = false;
        try {
          this.probe.signal(current.pid, signal);
        } catch {
          failed = true;
        }
        if (
          failed &&
          (await this.live()).some(({ pid }) => pid === current.pid)
        )
          throw cleanupFailure();
      }
      exited = await awaitExit(signal === "SIGTERM" ? 750 : 1000);
    }
    if (!exited) throw cleanupFailure();
    phase("owned_process_exit_observed");
    if (!settled && !transportClosed()) await settle(5000);
    if (!settled) {
      phase("owned_context_close_pending_after_exit");
      if (!transportClosed()) throw cleanupFailure();
      await this.requireStopped();
      this.deferredProtocol = true;
      phase("owned_close_verified_pending_protocol");
      return;
    }
    if (rejected) throw cleanupFailure();
    await this.requireStopped();
    phase("owned_close_finished");
  }
}
