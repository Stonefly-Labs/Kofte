import { createHash } from "node:crypto";
import { constants } from "node:fs";
import { lstat, open, realpath, unlink } from "node:fs/promises";
import { join, resolve } from "node:path";
import { brotliDecompressSync, gunzipSync, inflateSync } from "node:zlib";

export const identityActionCaptureHeader = "x-kofte-test-identity-response";
const maximumBody = 65536;

export async function consumeIdentityActionCapture(
  root: string | undefined,
  id: string | undefined,
  status: number,
  encoding: string | undefined,
  route = "identity-next-action",
): Promise<string> {
  let encoded: Buffer | undefined;
  let raw: Buffer | undefined;
  let decoded: Buffer | undefined;
  try {
    if (!root || !id || !/^[a-f0-9]{32}$/.test(id)) throw new Error();
    const directory = await lstat(root);
    if (
      !directory.isDirectory() ||
      directory.isSymbolicLink() ||
      (directory.mode & 0o777) !== 0o700 ||
      directory.uid !== process.getuid?.() ||
      (await realpath(root)) !== resolve(root)
    )
      throw new Error();
    const path = join(root, `identity-action-${id}.json`);
    const before = await lstat(path);
    if (
      !before.isFile() ||
      before.isSymbolicLink() ||
      (before.mode & 0o777) !== 0o600 ||
      before.uid !== directory.uid ||
      before.nlink !== 1 ||
      before.size < 1 ||
      before.size > 90000
    )
      throw new Error();
    const file = await open(path, constants.O_RDONLY | constants.O_NOFOLLOW);
    try {
      const current = await file.stat();
      if (
        current.ino !== before.ino ||
        current.dev !== before.dev ||
        current.size !== before.size ||
        current.uid !== before.uid ||
        current.mode !== before.mode ||
        current.nlink !== 1
      )
        throw new Error();
      encoded = Buffer.alloc(before.size + 1);
      let length = 0;
      while (length < encoded.length) {
        const read = await file.read(
          encoded,
          length,
          encoded.length - length,
          length,
        );
        if (!read.bytesRead) break;
        length += read.bytesRead;
      }
      if (length !== before.size) throw new Error();
      const text = new TextDecoder("utf-8", { fatal: true }).decode(
        encoded.subarray(0, length),
      );
      const value: unknown = JSON.parse(text);
      if (
        typeof value !== "object" ||
        value === null ||
        Array.isArray(value) ||
        Object.keys(value).join(",") !==
          "version,id,method,route,rootBinding,status,length,sha256,contentEncoding,createdUnixMilliseconds,bodyBase64" ||
        JSON.stringify(value) !== text
      )
        throw new Error();
      const record = value as Record<string, unknown>;
      const now = Date.now();
      if (
        record.version !== 1 ||
        record.id !== id ||
        record.method !== "POST" ||
        ![
          "identity-next-action",
          "identity-enrollment-lost-ack",
          "identity-sign-out-lost-ack",
        ].includes(route) ||
        record.route !== route ||
        record.rootBinding !==
          createHash("sha256").update(resolve(root)).digest("hex") ||
        record.status !== status ||
        !Number.isInteger(status) ||
        status < 200 ||
        status > 599 ||
        typeof record.length !== "number" ||
        !Number.isInteger(record.length) ||
        record.length < 0 ||
        record.length > maximumBody ||
        typeof record.sha256 !== "string" ||
        !/^[a-f0-9]{64}$/.test(record.sha256) ||
        typeof record.bodyBase64 !== "string" ||
        record.bodyBase64.length > 87384 ||
        typeof record.contentEncoding !== "string" ||
        !["identity", "gzip", "deflate", "br"].includes(
          record.contentEncoding,
        ) ||
        record.contentEncoding !== (encoding ?? "identity") ||
        typeof record.createdUnixMilliseconds !== "number" ||
        !Number.isSafeInteger(record.createdUnixMilliseconds) ||
        record.createdUnixMilliseconds > now ||
        now - record.createdUnixMilliseconds > 5000 ||
        before.mtimeMs < record.createdUnixMilliseconds - 1000 ||
        before.mtimeMs > now + 1000
      )
        throw new Error();
      raw = Buffer.from(record.bodyBase64, "base64");
      if (
        raw.toString("base64") !== record.bodyBase64 ||
        raw.length !== record.length ||
        createHash("sha256").update(raw).digest("hex") !== record.sha256
      )
        throw new Error();
      decoded =
        record.contentEncoding === "gzip"
          ? gunzipSync(raw, { maxOutputLength: maximumBody })
          : record.contentEncoding === "br"
            ? brotliDecompressSync(raw, { maxOutputLength: maximumBody })
            : record.contentEncoding === "deflate"
              ? inflateSync(raw, { maxOutputLength: maximumBody })
              : raw;
      if (decoded.length > maximumBody) throw new Error();
      const body = new TextDecoder("utf-8", { fatal: true }).decode(decoded);
      const retained = await lstat(path);
      if (
        retained.ino !== before.ino ||
        retained.dev !== before.dev ||
        retained.uid !== before.uid ||
        retained.mode !== before.mode ||
        retained.size !== before.size ||
        retained.nlink !== 1
      )
        throw new Error();
      await unlink(path);
      return body;
    } finally {
      await file.close();
    }
  } catch {
    throw new Error("identity_browser_private_action_capture_invalid");
  } finally {
    encoded?.fill(0);
    raw?.fill(0);
    decoded?.fill(0);
  }
}
