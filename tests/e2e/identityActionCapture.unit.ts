import assert from "node:assert/strict";
import { createHash, randomBytes } from "node:crypto";
import { EventEmitter } from "node:events";
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
import { join, resolve } from "node:path";
import test from "node:test";
import { gzipSync } from "node:zlib";
import type { Page, Request, Response } from "@playwright/test";
import {
  consumeIdentityActionCapture,
  identityActionCaptureHeader,
} from "./identityActionCapture.ts";
import { IdentityPrivacyCapture } from "./identityPrivacyCapture.ts";

const rejected = { message: "identity_browser_private_action_capture_invalid" };
async function ownedRoot(visit: (root: string) => Promise<void>) {
  const parent = join(process.cwd(), "artifacts", "verification");
  await mkdir(parent, { recursive: true });
  const root = await mkdtemp(join(parent, "identity-action-"));
  try {
    await visit(root);
  } finally {
    await chmod(root, 0o700);
    await rm(root, { recursive: true });
  }
}

function envelope(root: string, body: Buffer, encoding = "identity") {
  return {
    version: 1,
    id: randomBytes(16).toString("hex"),
    method: "POST",
    route: "identity-next-action",
    rootBinding: createHash("sha256").update(resolve(root)).digest("hex"),
    status: 303,
    length: body.length,
    sha256: createHash("sha256").update(body).digest("hex"),
    contentEncoding: encoding,
    createdUnixMilliseconds: Date.now(),
    bodyBase64: body.toString("base64"),
  };
}

async function writeRecord(
  root: string,
  record: ReturnType<typeof envelope>,
  text = JSON.stringify(record),
) {
  const path = join(root, `identity-action-${record.id}.json`);
  await writeFile(path, text, { flag: "wx", mode: 0o600 });
  return path;
}

test("consumes the exact bounded upstream body once, including supported compression", async () => {
  for (const encoding of ["identity", "gzip"]) {
    await ownedRoot(async (root) => {
      const actual =
        "actual-upstream-response-" + randomBytes(16).toString("hex");
      const bytes =
        encoding === "gzip" ? gzipSync(actual) : Buffer.from(actual);
      const record = envelope(root, bytes, encoding);
      const path = await writeRecord(root, record);
      assert.equal(
        (await consumeIdentityActionCapture(root, record.id, 303, encoding)) ===
          actual,
        true,
      );
      await assert.rejects(lstat(path), { code: "ENOENT" });
      await assert.rejects(
        consumeIdentityActionCapture(root, record.id, 303, encoding),
        rejected,
      );
    });
  }
});

test("rejects mismatched method, route, status, length, hash, encoding, base64 and duplicate shape", async () => {
  for (const patch of [
    { method: "GET" },
    { route: "other" },
    { status: 200 },
    { length: 1 },
    { sha256: "0".repeat(64) },
    { contentEncoding: "gzip" },
    { bodyBase64: "not-base64!" },
    { createdUnixMilliseconds: Date.now() - 6000 },
    { extra: "foreign" },
  ]) {
    await ownedRoot(async (root) => {
      const record = envelope(root, Buffer.from("actual-response"));
      await writeRecord(root, Object.assign(record, patch));
      await assert.rejects(
        consumeIdentityActionCapture(root, record.id, 303, undefined),
        rejected,
      );
    });
  }
  await ownedRoot(async (root) => {
    const record = envelope(root, Buffer.from("actual-response"));
    await writeRecord(
      root,
      record,
      JSON.stringify(record).slice(0, -1) + `,"id":"${record.id}"}`,
    );
    await assert.rejects(
      consumeIdentityActionCapture(root, record.id, 303, undefined),
      rejected,
    );
  });
  await ownedRoot(async (root) => {
    const record = envelope(root, Buffer.from("actual-response"));
    await writeRecord(
      root,
      record,
      JSON.stringify({ ...record, id: randomBytes(16).toString("hex") }),
    );
    await assert.rejects(
      consumeIdentityActionCapture(root, record.id, 303, undefined),
      rejected,
    );
  });
});

test("refuses oversized raw or decompressed bodies and non-canonical base64", async () => {
  for (const encoding of ["identity", "gzip"]) {
    await ownedRoot(async (root) => {
      const actual = Buffer.alloc(65537, "x");
      const record = envelope(
        root,
        encoding === "gzip" ? gzipSync(actual) : actual,
        encoding,
      );
      await writeRecord(root, record);
      await assert.rejects(
        consumeIdentityActionCapture(root, record.id, 303, encoding),
        rejected,
      );
    });
  }
  await ownedRoot(async (root) => {
    const record = envelope(root, Buffer.from("a"));
    record.bodyBase64 = record.bodyBase64.replace(/=+$/, "");
    await writeRecord(root, record);
    await assert.rejects(
      consumeIdentityActionCapture(root, record.id, 303, undefined),
      rejected,
    );
  });
});

test("refuses missing, foreign, symlinked and non-private evidence", async () => {
  await assert.rejects(
    consumeIdentityActionCapture(undefined, undefined, 303, undefined),
    rejected,
  );
  await ownedRoot(async (root) => {
    const record = envelope(root, Buffer.from("actual-response"));
    await assert.rejects(
      consumeIdentityActionCapture(root, record.id, 303, undefined),
      rejected,
    );
    const path = await writeRecord(root, record);
    await chmod(path, 0o644);
    await assert.rejects(
      consumeIdentityActionCapture(root, record.id, 303, undefined),
      rejected,
    );
    await chmod(path, 0o600);
    await chmod(root, 0o755);
    await assert.rejects(
      consumeIdentityActionCapture(root, record.id, 303, undefined),
      rejected,
    );
    await chmod(root, 0o700);
    await ownedRoot(async (foreignRoot) => {
      await writeFile(
        join(foreignRoot, `identity-action-${record.id}.json`),
        await readFile(path),
        { flag: "wx", mode: 0o600 },
      );
      await assert.rejects(
        consumeIdentityActionCapture(foreignRoot, record.id, 303, undefined),
        rejected,
      );
    });
    const retained = join(root, "retained-private-evidence");
    await writeFile(retained, await readFile(path), {
      flag: "wx",
      mode: 0o600,
    });
    await rm(path);
    await symlink(retained, path);
    await assert.rejects(
      consumeIdentityActionCapture(root, record.id, 303, undefined),
      rejected,
    );
    assert.equal((await lstat(retained)).isFile(), true);
  });
});

function actionResponse(id: string | undefined, encoding?: string): Response {
  const request = {
    method: () => "POST",
    url: () => "https://127.0.0.1:12345/identity",
    resourceType: () => "fetch",
    headers: () => ({ "next-action": "a".repeat(42) }),
  } as unknown as Request;
  return {
    request: () => request,
    url: () => request.url(),
    status: () => 303,
    allHeaders: async () => ({
      "x-action-redirect":
        "https://127.0.0.1:23456/authorize?state=actual-protocol-state&nonce=actual-protocol-nonce;push",
      ...(id ? { [identityActionCaptureHeader]: id } : {}),
      ...(encoding ? { "content-encoding": encoding } : {}),
    }),
    text: async () => {
      throw new Error("private-browser-unreadable-response");
    },
  } as unknown as Response;
}

test("the normal scanner accepts only a fully captured aborted action and rejects an actual body canary", async () => {
  for (const disclosure of [false, true]) {
    await ownedRoot(async (root) => {
      const previous = process.env.KOFTE_IDENTITY_PRIVATE_ROOT;
      process.env.KOFTE_IDENTITY_PRIVATE_ROOT = root;
      try {
        const canary = "private-upstream-action-canary";
        const body = disclosure ? canary : "actual-upstream-action-body";
        const record = envelope(root, gzipSync(body), "gzip");
        const path = await writeRecord(root, record);
        const page = new EventEmitter();
        const capture = new IdentityPrivacyCapture(page as unknown as Page, [
          canary,
        ]);
        const response = actionResponse(record.id, "gzip");
        page.emit("response", response);
        page.emit("requestfailed", response.request());
        if (disclosure)
          await assert.rejects(capture.close(), {
            message: "identity_browser_privacy_disclosure",
          });
        else await capture.close();
        await assert.rejects(lstat(path), { code: "ENOENT" });
      } finally {
        if (previous === undefined)
          delete process.env.KOFTE_IDENTITY_PRIVATE_ROOT;
        else process.env.KOFTE_IDENTITY_PRIVATE_ROOT = previous;
      }
    });
  }
});

test("an aborted action cannot use missing or mismatched private evidence", async () => {
  for (const mismatch of [false, true]) {
    await ownedRoot(async (root) => {
      const previous = process.env.KOFTE_IDENTITY_PRIVATE_ROOT;
      process.env.KOFTE_IDENTITY_PRIVATE_ROOT = root;
      try {
        const record = envelope(root, Buffer.from("actual-response"));
        if (mismatch) {
          record.status = 200;
          await writeRecord(root, record);
        }
        const page = new EventEmitter();
        const capture = new IdentityPrivacyCapture(page as unknown as Page);
        const response = actionResponse(record.id);
        page.emit("response", response);
        page.emit("requestfailed", response.request());
        await assert.rejects(capture.close(), rejected);
      } finally {
        if (previous === undefined)
          delete process.env.KOFTE_IDENTITY_PRIVATE_ROOT;
        else process.env.KOFTE_IDENTITY_PRIVATE_ROOT = previous;
      }
    });
  }
});
