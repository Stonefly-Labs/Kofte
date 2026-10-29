import { lstat, open } from "node:fs/promises";
import { join } from "node:path";

export async function exportIdentityCanaries(
  root: string | undefined,
  canaries: readonly string[],
) {
  let encoded: Buffer | undefined;
  try {
    if (!root) throw new Error();
    const directory = await lstat(root);
    if (
      !directory.isDirectory() ||
      directory.isSymbolicLink() ||
      (directory.mode & 0o077) !== 0 ||
      directory.uid !== process.getuid?.() ||
      !canaries.length ||
      canaries.length > 256 ||
      new Set(canaries).size !== canaries.length ||
      canaries.some((value) => value.length < 16 || value.length > 8192)
    )
      throw new Error();
    encoded = Buffer.from(JSON.stringify({ version: 1, canaries }));
    if (encoded.length > 65536) throw new Error();
    const file = await open(
      join(root, "identity-browser-canaries.json"),
      "wx",
      0o600,
    );
    try {
      await file.writeFile(encoded);
      const metadata = await file.stat();
      if (
        !metadata.isFile() ||
        (metadata.mode & 0o777) !== 0o600 ||
        metadata.uid !== directory.uid ||
        metadata.size !== encoded.length
      )
        throw new Error();
    } finally {
      await file.close();
    }
  } catch {
    throw new Error("identity_browser_private_canary_export_failed");
  } finally {
    encoded?.fill(0);
  }
}
