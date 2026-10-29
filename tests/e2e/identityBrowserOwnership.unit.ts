import assert from "node:assert/strict";
import { spawn } from "node:child_process";
import { once } from "node:events";
import { lstat, mkdir, mkdtemp, rename, rm, symlink } from "node:fs/promises";
import { join } from "node:path";
import test from "node:test";
import {
  IdentityBrowserOwnership,
  type BrowserProcess,
} from "./identityBrowserOwnership.ts";

async function fixture(
  visit: (profile: string, root: string) => Promise<void>,
) {
  const parent = join(process.cwd(), "artifacts", "verification");
  await mkdir(parent, { recursive: true });
  const root = await mkdtemp(join(parent, "identity-browser-ownership-"));
  const profile = join(root, "profile");
  await mkdir(profile, { mode: 0o700 });
  try {
    await visit(profile, root);
  } finally {
    await rm(root, { recursive: true });
  }
}

function fakeProcesses(profile: string) {
  let inventory: BrowserProcess[] = [
    {
      pid: 42424242,
      parent: process.pid,
      group: 42424242,
      uid: process.getuid!(),
      session: 42424242,
      state: "S",
      started: "Wed Oct 28 19:00:00 2026",
      command: `${process.execPath} --user-data-dir=${profile}`,
    },
  ];
  const signals: number[] = [];
  return {
    probe: {
      read: async () => structuredClone(inventory),
      signal: (pid: number) => signals.push(pid),
    },
    signals,
    replace: (replacement: BrowserProcess[]) => {
      inventory = replacement;
    },
    current: () => structuredClone(inventory),
  };
}

test("requires real close completion and observed process exit before deleting a profile", async () => {
  await fixture(async (profile) => {
    const fake = fakeProcesses(profile);
    const owner = await IdentityBrowserOwnership.observe(
      profile,
      process.execPath,
      42424242,
      fake.probe,
    );
    const phases: string[] = [];
    await owner.close(
      async () => fake.replace([]),
      (phase) => phases.push(phase),
    );
    assert.deepEqual(fake.signals, []);
    assert.ok(phases.includes("owned_context_close_completed"));
    assert.ok(phases.includes("owned_process_exit_observed"));
    assert.equal(phases.at(-1), "owned_close_finished");
    await owner.removeProfile();
    await assert.rejects(lstat(profile), { code: "ENOENT" });
  });
});

test("refuses PID reuse, changed provenance, shared groups and substituted profiles before signaling", async () => {
  for (const change of [
    "started",
    "command",
    "parent",
    "uid",
    "group",
    "session",
    "shared",
    "inode",
    "symlink",
  ]) {
    await fixture(async (profile, root) => {
      const fake = fakeProcesses(profile);
      const owner = await IdentityBrowserOwnership.observe(
        profile,
        process.execPath,
        42424242,
        fake.probe,
      );
      const rows = fake.current();
      switch (change) {
        case "started":
          rows[0].started = "Wed Oct 28 19:00:01 2026";
          break;
        case "command":
          rows[0].command = process.execPath + " --unowned";
          rows[0].session = process.pid;
          break;
        case "parent":
          rows[0].parent = process.ppid;
          break;
        case "uid":
          rows[0].uid++;
          break;
        case "group":
          rows[0].group++;
          break;
        case "session":
          rows[0].session = process.pid;
          break;
        case "shared":
          rows.push({ ...rows[0], pid: 42424243, session: process.pid });
          break;
        case "inode":
        case "symlink":
          await rename(profile, join(root, "held-profile"));
          if (change === "inode") await mkdir(profile, { mode: 0o700 });
          else await symlink(join(root, "held-profile"), profile);
          break;
      }
      fake.replace(rows);
      await assert.rejects(
        owner.close(
          async () => {},
          () => {},
        ),
        { message: "identity_browser_ownership_required" },
      );
      assert.deepEqual(fake.signals, []);
      assert.ok(await lstat(profile));
    });
  }
});

test("does not mistake process exit for a completed or successful close protocol", async () => {
  for (const pending of [true, false]) {
    await fixture(async (profile) => {
      const fake = fakeProcesses(profile);
      const owner = await IdentityBrowserOwnership.observe(
        profile,
        process.execPath,
        42424242,
        fake.probe,
      );
      const phases: string[] = [];
      await assert.rejects(
        owner.close(
          () => {
            fake.replace([]);
            return pending
              ? new Promise<void>(() => {})
              : Promise.reject(new Error("private-close-error"));
          },
          (phase) => phases.push(phase),
        ),
        { message: "identity_browser_cleanup_unverified" },
      );
      assert.ok(phases.includes("owned_process_exit_observed"));
      assert.ok(
        phases.includes(
          pending
            ? "owned_context_close_pending_after_exit"
            : "owned_context_close_failed",
        ),
      );
      assert.equal(phases.includes("owned_close_finished"), false);
      assert.deepEqual(fake.signals, []);
      assert.ok(await lstat(profile));
      let retried = false;
      await assert.rejects(
        owner.close(
          async () => {
            retried = true;
          },
          (phase) => phases.push(phase),
        ),
        { message: "identity_browser_cleanup_unverified" },
      );
      assert.equal(retried, false);
    });
  }
});

test("bounds graceful shutdown and kills only the verified owned Node process that ignores SIGTERM", async () => {
  await fixture(async (profile) => {
    const child = spawn(
      process.execPath,
      [
        "-e",
        'process.on("SIGTERM",()=>{});process.stdout.write("ready");setInterval(()=>{},1000)',
        "--",
        `--user-data-dir=${profile}`,
      ],
      { detached: true, stdio: ["ignore", "pipe", "pipe"], env: {} },
    );
    const exit = once(child, "exit");
    try {
      await once(child.stdout, "data", { signal: AbortSignal.timeout(2000) });
      assert.ok(child.pid);
      const owner = await IdentityBrowserOwnership.observe(
        profile,
        process.execPath,
        child.pid,
      );
      const phases: string[] = [];
      await owner.close(
        async () => {
          await exit;
        },
        (phase) => phases.push(phase),
      );
      assert.ok(phases.includes("owned_context_close_pending"));
      assert.ok(phases.includes("owned_termination_started"));
      assert.ok(phases.includes("owned_kill_started"));
      assert.ok(phases.includes("owned_context_close_completed"));
      assert.ok(phases.includes("owned_process_exit_observed"));
      assert.equal(child.signalCode, "SIGKILL");
      await owner.removeProfile();
      await assert.rejects(lstat(profile), { code: "ENOENT" });
    } finally {
      if (child.exitCode === null && child.signalCode === null)
        child.kill("SIGKILL");
      await exit;
    }
  });
});

test("uses immutable private-session provenance when titles change and still requires close and exit", async () => {
  await fixture(async (profile) => {
    const fake = fakeProcesses(profile);
    let complete: (() => void) | undefined;
    const closed = new Promise<void>((done) => {
      complete = done;
    });

    const signals: string[] = [];
    const owner = await IdentityBrowserOwnership.observe(
      profile,
      process.execPath,
      42424242,
      {
        read: fake.probe.read,
        signal: (_pid, signal) => {
          signals.push(signal);
          const current = fake.current();
          current[0].command = "changed-exiting-process-title";
          fake.replace(current);
          setTimeout(() => {
            fake.replace([]);
            complete?.();
          }, 50);
        },
      },
    );
    const phases: string[] = [];
    await owner.close(
      () => closed,
      (phase) => phases.push(phase),
    );
    assert.deepEqual(signals, ["SIGTERM"]);
    assert.ok(phases.includes("owned_process_command_changed"));
    assert.ok(phases.includes("owned_context_close_completed"));
    assert.ok(phases.includes("owned_process_exit_observed"));
    await owner.removeProfile();
    await assert.rejects(lstat(profile), { code: "ENOENT" });
  });
});

test("escalates only inside the verified private session and rejects an initially foreign profile", async () => {
  await fixture(async (profile) => {
    const fake = fakeProcesses(profile);
    const foreign = fake.current();
    foreign[0].command = process.execPath + " --user-data-dir=foreign";
    fake.replace(foreign);
    await assert.rejects(
      IdentityBrowserOwnership.observe(
        profile,
        process.execPath,
        42424242,
        fake.probe,
      ),
      { message: "identity_browser_ownership_required" },
    );
    fake.replace([
      {
        ...foreign[0],
        command: `${process.execPath} --user-data-dir=${profile}`,
      },
    ]);
    let complete: (() => void) | undefined;
    const closed = new Promise<void>((done) => {
      complete = done;
    });
    const signals: string[] = [];
    const owner = await IdentityBrowserOwnership.observe(
      profile,
      process.execPath,
      42424242,
      {
        read: fake.probe.read,
        signal: (_pid, signal) => {
          signals.push(signal);
          if (signal === "SIGTERM") {
            const current = fake.current();
            current[0].command = "mutable-owned-process-title";
            fake.replace(current);
          } else {
            fake.replace([]);
            complete?.();
          }
        },
      },
    );
    await owner.close(
      () => closed,
      () => {},
    );
    assert.deepEqual(signals, ["SIGTERM", "SIGKILL"]);
    await owner.removeProfile();
  });
});

test("defers only a transport-confirmed close and still requires its original protocol result", async () => {
  for (const rejected of [false, true]) {
    await fixture(async (profile) => {
      const fake = fakeProcesses(profile);
      const owner = await IdentityBrowserOwnership.observe(
        profile,
        process.execPath,
        42424242,
        fake.probe,
      );
      let complete: (() => void) | undefined;
      let fail: (() => void) | undefined;
      const original = new Promise<void>((done, reject) => {
        complete = done;
        fail = () => reject(new Error("private-original-close-failure"));
      });
      const phases: string[] = [];
      await owner.close(
        () => {
          fake.replace([]);
          return original;
        },
        (phase) => phases.push(phase),
        () => true,
      );
      assert.ok(phases.includes("owned_close_verified_pending_protocol"));
      assert.equal(phases.includes("owned_context_close_completed"), false);
      assert.equal(phases.includes("owned_close_finished"), false);
      let finished = false;
      const result = owner.requireProtocolComplete().then(() => {
        finished = true;
      });
      await Promise.resolve();
      assert.equal(finished, false);
      if (rejected) {
        fail?.();
        await assert.rejects(result, {
          message: "identity_browser_cleanup_unverified",
        });
        assert.equal(phases.includes("owned_close_finished"), false);
      } else {
        complete?.();
        await result;
        assert.ok(phases.includes("owned_context_close_completed"));
        assert.equal(phases.at(-1), "owned_close_finished");
      }
      assert.deepEqual(fake.signals, []);
      await owner.removeProfile();
    });
  }
});
