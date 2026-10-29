import { createHash, X509Certificate } from "node:crypto";
import { lstat, mkdtemp, readFile, realpath, rmdir } from "node:fs/promises";
import { join } from "node:path";
import { DatabaseSync } from "node:sqlite";
import { chromium, expect, type BrowserContext } from "@playwright/test";
import { IdentityBrowserOwnership } from "./identityBrowserOwnership.ts";

export async function withIdentityBrowser(
  root: string,
  origin: string,
  authorityFile: string,
  visit: (
    context: BrowserContext,
    progress: (phase: string) => void,
  ) => Promise<void>,
) {
  const began = performance.now();
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
  // Chromium's singleton socket must fit Linux's Unix-socket path limit.
  const temporary = await mkdtemp(
    join(await realpath("/tmp"), "kofte-browser-"),
  );
  const profile = await mkdtemp(join(temporary, "profile-"));
  let context: BrowserContext | undefined;
  const owners = new WeakMap<BrowserContext, IdentityBrowserOwnership>();
  const observedOwners: IdentityBrowserOwnership[] = [];
  const transports = new WeakMap<
    BrowserContext,
    { context: boolean; browser: boolean; launch: number }
  >();
  let lastOwner: IdentityBrowserOwnership | undefined;
  let launchUnverified = false;
  let launches = 0;
  const phase = (name: string, launch = launches) =>
    console.log(
      JSON.stringify({
        eventCode: "identity_browser_profile_phase",
        phase: name,
        launch,
        elapsedMilliseconds: Math.round(performance.now() - began),
      }),
    );
  const launch = async () => {
    launches++;
    phase("launch_started");
    launchUnverified = true;
    context = await chromium.launchPersistentContext(profile, {
      channel: "chromium",
      headless: true,
      ignoreHTTPSErrors: false,
      timeout: 15000,
      env: {
        ...process.env,
        TMPDIR: profile,
        TMP: profile,
        TEMP: profile,
      },
    });
    phase("context_created");
    const browser = context.browser();
    if (!browser) throw new Error("identity_browser_ownership_required");
    const transport = { context: false, browser: false, launch: launches };
    transports.set(context, transport);
    context.once("close", () => {
      transport.context = true;
      phase("owned_context_closed_event", transport.launch);
    });
    browser.once("disconnected", () => {
      transport.browser = true;
      phase("owned_browser_disconnected_event", transport.launch);
    });
    const diagnostics = await browser.newBrowserCDPSession();
    phase("browser_cdp_opened");
    try {
      const { processInfo } = await diagnostics.send(
        "SystemInfo.getProcessInfo",
      );
      phase("browser_owner_observed");
      const observed = processInfo.filter((entry) => entry.type === "browser");
      const group = observed[0]?.id;
      if (
        observed.length !== 1 ||
        !Number.isSafeInteger(group) ||
        group <= 1 ||
        group === process.pid ||
        group === process.ppid
      )
        throw new Error("identity_browser_ownership_required");
      lastOwner = await IdentityBrowserOwnership.observe(
        profile,
        chromium.executablePath(),
        group,
      );
      owners.set(context, lastOwner);
      observedOwners.push(lastOwner);
      launchUnverified = false;
    } finally {
      await diagnostics.detach();
    }
    return context;
  };
  const close = async (owned: BrowserContext) => {
    const owner = owners.get(owned);
    if (!owner) throw new Error("identity_browser_ownership_required");
    await owner.close(
      () => owned.close(),
      (name) => phase(name, transports.get(owned)!.launch),
      () => {
        const observed = transports.get(owned);
        return observed?.context === true && observed.browser;
      },
    );
  };
  let hadPrimaryFailure = false;
  let primaryFailure: unknown;
  try {
    console.log("identity_browser_stage_initial_launch");
    context = await launch();
    console.log("identity_browser_stage_initial_browser_ready");
    const probe = context.pages()[0];
    const target = new URL(origin).href;
    const [refusal] = await Promise.all([
      probe.waitForEvent("requestfailed", {
        predicate: (request) => request.url() === target,
        timeout: 5000,
      }),
      probe.evaluate((url) => {
        const frame = document.createElement("iframe");
        frame.src = url;
        document.body.append(frame);
      }, target),
    ]);
    expect(
      refusal.failure()?.errorText,
      "identity_browser_rejects_untrusted_tls",
    ).toBe("net::ERR_CERT_AUTHORITY_INVALID");
    console.log("identity_browser_stage_untrusted_tls_refused");
    phase("untrusted_refused");
    await probe.locator("iframe").evaluate((frame) => frame.remove());
    phase("initial_close_started");
    await close(context);
    phase("initial_close_finished");
    console.log("identity_browser_stage_initial_browser_closed");
    context = undefined;

    const database = new DatabaseSync(
      join(profile, "Default", "ServerCertificate"),
    );
    phase("trust_database_opened");
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
      phase("trust_database_written");
    } finally {
      database.close();
      phase("trust_database_closed");
    }
    console.log("identity_browser_stage_private_authority_installed");
    context = await launch();
    context.setDefaultTimeout(5000);
    context.setDefaultNavigationTimeout(5000);
    phase("trusted_journey_started");
    await visit(context, phase);
  } catch (error) {
    hadPrimaryFailure = true;
    primaryFailure = error;
  }
  let hadCleanupFailure = false;
  let cleanupFailure: unknown;
  try {
    if (context) await close(context);
    if (launchUnverified || !lastOwner)
      throw new Error("identity_browser_cleanup_unverified");
    await Promise.all(
      observedOwners.map((owner) => owner.requireProtocolComplete()),
    );
    await lastOwner.removeProfile();
    await rmdir(temporary).catch((error: unknown) => {
      throw new Error("identity_browser_cleanup_unverified", { cause: error });
    });
    console.log("identity_browser_stage_process_closed");
  } catch (error) {
    hadCleanupFailure = true;
    cleanupFailure = error;
  }
  if (hadPrimaryFailure && hadCleanupFailure)
    throw new AggregateError(
      [primaryFailure, cleanupFailure],
      "identity_browser_failure_boundaries_failed",
    );
  if (hadPrimaryFailure) throw primaryFailure;
  if (hadCleanupFailure) throw cleanupFailure;
}
