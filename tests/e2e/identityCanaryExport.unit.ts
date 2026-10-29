import assert from "node:assert/strict";
import { randomBytes } from "node:crypto";
import {
  chmod,
  lstat,
  mkdir,
  mkdtemp,
  readFile,
  rm,
  symlink,
  writeFile,
} from "node:fs/promises";
import { join } from "node:path";
import test from "node:test";
import { exportIdentityCanaries } from "./identityCanaryExport.ts";

const rejected = { message: "identity_browser_private_canary_export_failed" };
const values = () => [
  randomBytes(32).toString("base64url"),
  randomBytes(32).toString("base64url"),
  randomBytes(32).toString("base64url"),
];

async function ownedRoot(visit: (root: string) => Promise<void>) {
  const parent = join(process.cwd(), "artifacts", "verification");
  await mkdir(parent, { recursive: true });
  const root = await mkdtemp(join(parent, "canary-export-"));
  try {
    await visit(root);
  } finally {
    await chmod(root, 0o700);
    await rm(root, { recursive: true });
  }
}

test("exports actual distinct canaries once into a new private envelope", async () => {
  await ownedRoot(async (root) => {
    const canaries = values();
    await exportIdentityCanaries(root, canaries);
    const path = join(root, "identity-browser-canaries.json");
    const metadata = await lstat(path);
    assert.equal(metadata.mode & 0o777, 0o600);
    assert.equal(metadata.uid, process.getuid?.());
    const envelope: unknown = JSON.parse(await readFile(path, "utf8"));
    assert.equal(
      JSON.stringify(envelope) === JSON.stringify({ version: 1, canaries }),
      true,
    );
    await assert.rejects(exportIdentityCanaries(root, values()), rejected);
    assert.equal(
      (await readFile(path, "utf8")) ===
        JSON.stringify({ version: 1, canaries }),
      true,
    );
  });
});

test("rejects a symlink without changing its target", async () => {
  await ownedRoot(async (root) => {
    const target = join(root, "retained-private-canary");
    const retained = randomBytes(32).toString("base64url");
    await writeFile(target, retained, { mode: 0o600, flag: "wx" });
    await symlink(target, join(root, "identity-browser-canaries.json"));
    await assert.rejects(exportIdentityCanaries(root, values()), rejected);
    assert.equal((await readFile(target, "utf8")) === retained, true);
  });
});

test("fails closed for missing, shared, symlinked and unwritable roots", async () => {
  await assert.rejects(exportIdentityCanaries(undefined, values()), rejected);
  await ownedRoot(async (root) => {
    await chmod(root, 0o755);
    await assert.rejects(exportIdentityCanaries(root, values()), rejected);
    await chmod(root, 0o700);
    const link = join(root, "linked-root");
    await symlink(root, link);
    await assert.rejects(exportIdentityCanaries(link, values()), rejected);
    await chmod(root, 0o500);
    await assert.rejects(exportIdentityCanaries(root, values()), rejected);
    await assert.rejects(lstat(join(root, "identity-browser-canaries.json")), {
      code: "ENOENT",
    });
  });
});

test("rejects invalid, duplicate, over-count and UTF-8 byte-overflow envelopes", async () => {
  await ownedRoot(async (root) => {
    const seed = values()[0];
    for (const canaries of [
      [],
      [seed, seed],
      [seed.slice(0, 15)],
      [seed.repeat(200)],
      Array.from({ length: 257 }, () => randomBytes(16).toString("hex")),
      Array.from({ length: 9 }, (_, index) => "界".repeat(3000) + index),
    ]) {
      await assert.rejects(exportIdentityCanaries(root, canaries), rejected);
      await assert.rejects(
        lstat(join(root, "identity-browser-canaries.json")),
        { code: "ENOENT" },
      );
    }
  });
});
