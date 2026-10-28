import { createHash, X509Certificate } from "node:crypto";
import { lstat, mkdtemp, readFile, rm } from "node:fs/promises";
import { join } from "node:path";
import { DatabaseSync } from "node:sqlite";
import { chromium, expect, type BrowserContext } from "@playwright/test";

export async function withIdentityBrowser(
  root: string,
  origin: string,
  authorityFile: string,
  visit: (context: BrowserContext) => Promise<void>,
) {
  const directory = await lstat(root);
  const authority = await lstat(authorityFile);
  if (
    !directory.isDirectory() ||
    directory.isSymbolicLink() ||
    (directory.mode & 0o077) !== 0 ||
    directory.uid !== process.getuid?.() ||
    !authority.isFile() ||
    authority.isSymbolicLink() ||
    (authority.mode & 0o077) !== 0 ||
    authority.uid !== directory.uid
  ) {
    throw new Error("identity_browser_private_trust_required");
  }
  const certificate = new X509Certificate(await readFile(authorityFile));
  if (!certificate.ca || !certificate.verify(certificate.publicKey)) {
    throw new Error("identity_browser_authority_invalid");
  }
  const profile = await mkdtemp(join(root, "chromium-"));
  let context: BrowserContext | undefined;
  const groups = new WeakMap<BrowserContext, number>();
  const launch = async () => {
    context = await chromium.launchPersistentContext(profile, {
      channel: "chromium",
      headless: true,
      ignoreHTTPSErrors: false,
      timeout: 15000,
    });
    const browser = context.browser();
    if (!browser) throw new Error("identity_browser_ownership_required");
    const diagnostics = await browser.newBrowserCDPSession();
    try {
      const { processInfo } = await diagnostics.send(
        "SystemInfo.getProcessInfo",
      );
      const owners = processInfo.filter((entry) => entry.type === "browser");
      const group = owners[0]?.id;
      if (
        owners.length !== 1 ||
        !Number.isSafeInteger(group) ||
        group <= 1 ||
        group === process.pid ||
        group === process.ppid
      )
        throw new Error("identity_browser_ownership_required");
      groups.set(context, group);
    } finally {
      await diagnostics.detach();
    }
    return context;
  };
  const close = async (owned: BrowserContext) => {
    const group = groups.get(owned);
    if (!group) throw new Error("identity_browser_ownership_required");
    await owned.close();
    const expires = performance.now() + 1000;
    while (true) {
      try {
        process.kill(-group, 0);
      } catch (error) {
        if (error instanceof Error && "code" in error && error.code === "ESRCH")
          return;
        throw new Error("identity_browser_cleanup_unverified", {
          cause: error,
        });
      }
      if (performance.now() >= expires)
        throw new Error("identity_browser_cleanup_unverified");
      await new Promise((resolve) => setTimeout(resolve, 10));
    }
  };
  try {
    context = await launch();
    const probe = context.pages()[0];
    await expect(
      probe.goto(origin, { timeout: 5000 }),
      "identity_browser_rejects_untrusted_tls",
    ).rejects.toThrow("net::ERR_CERT_AUTHORITY_INVALID");
    const manager = await context.newPage();
    await manager.goto("chrome://certificate-manager/", { timeout: 5000 });
    await close(context);
    context = undefined;

    const database = new DatabaseSync(
      join(profile, "Default", "ServerCertificate"),
    );
    try {
      const version = database
        .prepare("SELECT value FROM meta WHERE key='version'")
        .get();
      const compatible = database
        .prepare("SELECT value FROM meta WHERE key='last_compatible_version'")
        .get();
      const columns = database
        .prepare("PRAGMA table_info(certificates)")
        .all()
        .map((column) => column.name);
      if (
        version?.value !== "1" ||
        compatible?.value !== "1" ||
        columns.join(",") !== "sha256hash_hex,der_cert,trust_settings" ||
        database.prepare("SELECT COUNT(*) AS count FROM certificates").get()
          ?.count !== 0
      ) {
        throw new Error("identity_browser_trust_schema_changed");
      }
      // Chromium CertificateMetadata.trust.trust_type=TRUSTED (schema v1).
      // This imports one CA into the stopped, newly owned profile, never OS trust.
      const result = database
        .prepare(
          "INSERT INTO certificates(sha256hash_hex,der_cert,trust_settings) VALUES(?,?,?)",
        )
        .run(
          createHash("sha256").update(certificate.raw).digest("hex"),
          certificate.raw,
          Buffer.from([0x0a, 0x02, 0x08, 0x03]),
        );
      if (result.changes !== 1) {
        throw new Error("identity_browser_authority_import_failed");
      }
    } finally {
      database.close();
    }
    context = await launch();
    context.setDefaultTimeout(5000);
    context.setDefaultNavigationTimeout(5000);
    await visit(context);
  } finally {
    if (context) await close(context);
    console.log("identity_browser_stage_process_closed");
    await rm(profile, { recursive: true });
  }
}
