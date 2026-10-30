import { expect } from "@playwright/test";
import { withIdentityBrowser } from "./identityBrowser.ts";
import { identityAccessibility } from "./identityAccessibility.ts";
import { IdentityPrivacyCapture } from "./identityPrivacyCapture.ts";

async function verifyFailures() {
  const root = process.env.KOFTE_IDENTITY_PRIVATE_ROOT;
  const authority = process.env.KOFTE_IDENTITY_AUTHORITY_FILE;
  const origin = process.env.KOFTE_IDENTITY_WEB_URL;
  const user = process.env.KOFTE_IDENTITY_EXPECTED_USER;
  const canary = process.env.KOFTE_IDENTITY_PRIVACY_CANARY;
  if (!root || !authority || !origin || !user || !canary)
    throw new Error("identity_browser_owned_fixture_required");
  await withIdentityBrowser(root, origin, authority, async (context, phase) => {
    console.log("identity_browser_stage_trusted_browser_ready");
    const page = context.pages()[0];
    await page.bringToFront();
    console.log("identity_browser_stage_page_active");
    const privacy = new IdentityPrivacyCapture(page, [canary], "oidc", phase);
    let primaryFailure: unknown;
    let hadPrimaryFailure = false;
    let captureFailure: unknown;
    let hadCaptureFailure = false;
    try {
      let submissions = 0;
      let returnedAt: number | undefined;
      const callbackMaterial: string[] = [];
      page.on("request", (request) => {
        const url = new URL(request.url());
        if (request.method() === "POST" && url.pathname === "/identity")
          submissions++;
        if (url.pathname === "/auth/oidc/callback") {
          returnedAt = performance.now();
          for (const name of ["code", "state", "error_description"]) {
            const value = url.searchParams.get(name);
            if (value) callbackMaterial.push(value);
          }
        }
      });
      const panel = page.getByRole("region", {
        name: "Your identity",
        exact: true,
      });
      const status = panel.getByRole("status").first();
      const signIn = panel.getByRole("button", {
        name: "Sign in",
        exact: true,
      });
      const retry = panel.getByRole("button", { name: "Retry identity check" });
      let release: (() => void) | undefined;
      await page.route(
        "**/api/identity/v1/browser-context",
        async (route) => {
          await new Promise<void>((resolve) => {
            release = resolve;
          });
          await route.continue();
        },
        { times: 1 },
      );
      await page.goto(new URL("/identity", origin).href);
      try {
        await expect(status).toHaveText("Checking your identity");
        await expect(retry).toHaveAttribute("aria-disabled", "true");
        await expect.poll(() => typeof release).toBe("function");
        phase("loading_accessibility_started");
        await identityAccessibility(page, status, retry);
        phase("loading_accessibility_finished");
      } finally {
        release?.();
      }
      await expect(status).toHaveText("You are signed out");
      phase("signed_out_accessibility_started");
      await identityAccessibility(page, status, signIn);
      phase("signed_out_accessibility_finished");
      console.log("identity_browser_stage_loading_signed_out_accessible");
      await privacy.snapshot(context);
      console.log("identity_browser_stage_signed_out_capture_verified");
      for (const [outcome, message] of [
        ["cancelled", "Sign-in was cancelled."],
        ["failed", "Sign-in could not be completed."],
        ["unavailable", "Sign-in is unavailable."],
      ]) {
        const previous = submissions;
        returnedAt = undefined;
        await signIn.press("Enter");
        const alert = panel.getByRole("alert");
        await expect(alert).toContainText(message, { timeout: 5000 });
        expect(returnedAt !== undefined).toBe(true);
        expect(performance.now() - returnedAt!).toBeLessThan(5000);
        const destination = new URL(page.url());
        expect(destination.origin).toBe(new URL(origin).origin);
        expect(destination.pathname).toBe("/identity");
        expect([...destination.searchParams.keys()].sort()).toEqual([
          "correlationId",
          "outcome",
        ]);
        expect(destination.searchParams.get("outcome")).toBe(outcome);
        expect(destination.searchParams.get("correlationId")).toMatch(
          /^[a-f0-9]{32}$/,
        );
        await expect(status).toHaveText("You are signed out");
        await expect(page.getByText(user, { exact: true })).toHaveCount(0);
        expect(
          (await context.cookies()).some(
            ({ name }) => name === "__Host-kofte-session",
          ),
        ).toBe(false);
        expect(submissions).toBe(previous + 1);
        await identityAccessibility(page, alert, signIn);
        const html = await page.content();
        expect(callbackMaterial.length).toBeGreaterThan(0);
        expect(
          callbackMaterial.some(
            (value) => html.includes(value) || page.url().includes(value),
          ),
        ).toBe(false);
        await privacy.snapshot(context);
        const before = submissions;
        let releaseRetry: (() => void) | undefined;
        await page.route(
          "**/api/identity/v1/browser-context",
          async (route) => {
            await new Promise<void>((resolve) => {
              releaseRetry = resolve;
            });
            await route.continue();
          },
          { times: 1 },
        );
        phase("retry_started");
        try {
          await retry.focus();
          await page.keyboard.press("Enter");
          await expect.poll(() => typeof releaseRetry).toBe("function");
          await expect(status).toHaveText("Checking your identity");
          await expect(retry).toHaveAttribute("aria-disabled", "true");
          phase("retry_loading_observed");
        } finally {
          releaseRetry?.();
        }
        await expect(status).toHaveText("You are signed out");
        await expect(retry).toBeFocused();
        await privacy.snapshot(context);
        phase("retry_finished");
        expect(submissions).toBe(before);
        console.log(`identity_browser_stage_${outcome}_retry_accessible`);
        // The next sign-in remains a deliberate keyboard action, not an automatic retry.
        await signIn.focus();
      }
      const before = submissions;
      returnedAt = undefined;
      await signIn.press("Enter");
      await expect(status).toHaveText("You are signed in", { timeout: 5000 });
      expect(returnedAt !== undefined).toBe(true);
      expect(performance.now() - returnedAt!).toBeLessThan(5000);
      expect(submissions).toBe(before + 1);
      await expect(page.getByText(user, { exact: true })).toBeVisible();
      await identityAccessibility(
        page,
        status,
        panel.getByLabel("One-time authorization", { exact: true }),
      );
      expect(await page.evaluate(() => document.cookie)).toBe("");
      expect(
        await page.evaluate(() => localStorage.length + sessionStorage.length),
      ).toBe(0);
      expect(callbackMaterial.some((value) => page.url().includes(value))).toBe(
        false,
      );
      console.log("identity_browser_stage_corrected_login_accessible");
      await privacy.proveCapture(context);
      console.log("identity_browser_stage_private_capture_verified");
    } catch (error) {
      hadPrimaryFailure = true;
      primaryFailure = error;
      reportFailure(error);
      phase("journey_failure_observed");
    } finally {
      phase("capture_cleanup_started");
      try {
        await privacy.close();
        phase("capture_cleanup_finished");
      } catch (error) {
        hadCaptureFailure = true;
        captureFailure = error;
        reportFailure(error);
        phase("capture_cleanup_failed");
      }
    }
    if (hadPrimaryFailure && hadCaptureFailure)
      throw new AggregateError(
        [primaryFailure, captureFailure],
        "identity_browser_failure_boundaries_failed",
      );
    if (hadPrimaryFailure) throw primaryFailure;
    if (hadCaptureFailure) throw captureFailure;
  });
  console.log("identity_browser_stage_failure_boundaries_verified");
}

function reportFailure(error: unknown) {
  const location =
    error instanceof Error
      ? error.stack?.match(
          /\b(?:identity\.failures|identityBrowser|identityAccessibility|identityPrivacyCapture|identityCanaryExport)\.ts:([1-9][0-9]{0,3}):[1-9][0-9]{0,4}\b/,
        )?.[0]
      : undefined;
  if (location) console.error(location);
  const known = new Set([
    "identity_browser_owned_fixture_required",
    "identity_browser_private_trust_required",
    "identity_browser_authority_invalid",
    "identity_browser_ownership_required",
    "identity_browser_cleanup_unverified",
    "identity_browser_trust_schema_changed",
    "identity_browser_authority_import_failed",
    "identity_browser_private_canary_invalid",
    "identity_browser_private_handoff_invalid",
    "identity_browser_private_cache_invalid",
    "identity_browser_private_identity_invalid",
    "identity_browser_private_action_capture_invalid",
    "identity_browser_privacy_capture_failed",
    "identity_browser_capture_response_headers_failed",
    "identity_browser_capture_response_body_failed",
    "identity_browser_capture_identity_body_failed",
    "identity_browser_capture_context_body_failed",
    "identity_browser_capture_handoff_body_failed",
    "identity_browser_capture_page_body_failed",
    "identity_browser_capture_response_aborted",
    "identity_browser_capture_browser_context_fetch_aborted",
    "identity_browser_capture_current_identity_fetch_aborted",
    "identity_browser_capture_identity_document_aborted",
    "identity_browser_capture_identity_action_fetch_aborted",
    "identity_browser_capture_callback_document_aborted",
    "identity_browser_capture_response_failed",
    "identity_browser_capture_csrf_invalid",
    "identity_browser_capture_closed",
    "identity_browser_capture_limit_exceeded",
    "identity_browser_privacy_capture_incomplete",
    "identity_browser_privacy_capture_missing",
    "identity_browser_privacy_disclosure",
    "identity_browser_privacy_scanner_failed",
    "identity_browser_private_canary_export_failed",
    "identity_browser_rejects_untrusted_tls",
    "identity_browser_visible_keyboard_focus",
    "identity_browser_narrow_enlarged_reflow",
  ]);
  let code =
    error instanceof Error && known.has(error.message)
      ? error.message
      : undefined;
  if (!code && error instanceof Error) {
    const heading = error.message.split("\n", 1)[0].trim();
    if (known.has(heading)) code = heading;
    else if (error.name === "TimeoutError")
      code = "identity_browser_action_timeout";
    else if (
      "matcherResult" in error &&
      typeof error.matcherResult === "object" &&
      error.matcherResult !== null &&
      "name" in error.matcherResult
    ) {
      switch (error.matcherResult.name) {
        case "toHaveText":
        case "toContainText":
          code = "identity_browser_assert_text_failed";
          break;
        case "toHaveAttribute":
          code = "identity_browser_assert_attribute_failed";
          break;
        case "toBeFocused":
          code = "identity_browser_assert_focus_failed";
          break;
        case "toBeVisible":
          code = "identity_browser_assert_visibility_failed";
          break;
        case "toHaveCount":
          code = "identity_browser_assert_count_failed";
          break;
        case "toBe":
        case "toEqual":
          code = "identity_browser_assert_equal_failed";
          break;
        case "toMatch":
          code = "identity_browser_assert_match_failed";
          break;
        case "toBeLessThan":
          code = "identity_browser_assert_latency_failed";
          break;
      }
    }
  }
  console.error(code ?? "identity_browser_failure_unclassified");
}

void verifyFailures().catch((error: unknown) => {
  if (error instanceof AggregateError && error.errors.length === 2) {
    for (const failure of error.errors) reportFailure(failure);
  } else reportFailure(error);
  console.error("identity_browser_failure_boundaries_failed");
  process.exitCode = 1;
});
