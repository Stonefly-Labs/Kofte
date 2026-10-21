import { randomUUID } from "node:crypto";
import {
  lstat,
  readFile,
  realpath,
  rename,
  unlink,
  writeFile,
} from "node:fs/promises";
import { isAbsolute, join } from "node:path";
import { setTimeout } from "node:timers/promises";

type ControlAction =
  "unavailable-store" | "restore-store" | "stop-host" | "start-host";

export async function controlRuntime(action: ControlAction): Promise<void> {
  const directory = process.env.BOOTSTRAP_TEST_CONTROL;
  const runId = process.env.BOOTSTRAP_TEST_RUN_ID;
  if (
    !directory ||
    !isAbsolute(directory) ||
    !runId ||
    !/^[0-9a-f]{32}$/.test(runId)
  )
    throw new Error("owned_runtime_control_required");
  const stat = await lstat(directory);
  if (
    (await realpath(directory)) !== directory ||
    !stat.isDirectory() ||
    stat.uid !== process.getuid?.() ||
    (stat.mode & 0o777) !== 0o700
  )
    throw new Error("owned_runtime_control_invalid");
  const nonce = randomUUID().replaceAll("-", "");
  const temporary = join(directory, `.request-${nonce}.tmp`);
  await writeFile(temporary, JSON.stringify({ runId, nonce, action }), {
    flag: "wx",
    mode: 0o600,
  });
  await rename(temporary, join(directory, `request-${nonce}.json`));
  const response = join(directory, `response-${nonce}.json`);
  const deadline = performance.now() + 15_000;
  while (performance.now() < deadline) {
    try {
      const value: unknown = JSON.parse(await readFile(response, "utf8"));
      if (
        typeof value !== "object" ||
        value === null ||
        !("runId" in value) ||
        value.runId !== runId ||
        !("nonce" in value) ||
        value.nonce !== nonce ||
        !("ok" in value) ||
        value.ok !== true
      )
        throw new Error("owned_runtime_action_failed");
      await unlink(response);
      return;
    } catch (error) {
      if (!(
        error instanceof Error &&
        "code" in error &&
        error.code === "ENOENT"
      ))
        throw error;
    }
    await setTimeout(25);
  }
  throw new Error("owned_runtime_control_deadline");
}
