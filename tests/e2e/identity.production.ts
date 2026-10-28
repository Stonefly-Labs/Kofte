import { expect } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { lstat, readFile } from "node:fs/promises";
import { dirname } from "node:path";
import type { Page } from "@playwright/test";
import { withIdentityBrowser } from "./identityBrowser.ts";

async function verifyLogin() {
  const origin = process.env.KOFTE_IDENTITY_WEB_URL;
  const root = process.env.KOFTE_IDENTITY_PRIVATE_ROOT;
  const authority = process.env.KOFTE_IDENTITY_AUTHORITY_FILE;
  const user = process.env.KOFTE_IDENTITY_EXPECTED_USER;
  if (
    !origin ||
    !root ||
    !authority ||
    !user ||
    !/^https:\/\/127\.0\.0\.1:[1-9]\d*\/?$/.test(origin) ||
    !/^[0-9a-f]{8}(-[0-9a-f]{4}){3}-[0-9a-f]{12}$/.test(user)
  ) {
    throw new Error("identity_browser_owned_fixture_required");
  }
  await withIdentityBrowser(root, origin, authority, async (context) => {
    try {
      console.log("identity_browser_stage_trust_ready");
      const page = context.pages()[0];
      await page.bringToFront();
      const paths: string[] = [];
      page.on("request", (request) => {
        const path = new URL(request.url()).pathname;
        if (request.method() === "POST" && path === "/identity") {
          paths.push("login-action");
        } else if (["/authorize", "/auth/oidc/callback"].includes(path)) {
          paths.push(path);
        }
      });
      const shell = await page.goto(new URL("/identity", origin).href);
      expect(shell?.headers()["cache-control"]).toContain("no-store");
      expect(shell?.headers()["referrer-policy"]).toBe("no-referrer");
      expect(await shell!.text()).not.toContain(
        'name="__RequestVerificationToken"',
      );
      console.log("identity_browser_stage_shell_verified");
      await expect(
        page.getByRole("status"),
        "identity_browser_initial_state",
      ).toHaveText("You are signed out", { timeout: 5000 });
      console.log("identity_browser_stage_signed_out");
      await page
        .getByRole("button", { name: "Sign in", exact: true })
        .click({ timeout: 5000 });
      console.log("identity_browser_stage_submitted");
      await expect(
        page.getByRole("status"),
        "identity_browser_signed_in",
      ).toHaveText("You are signed in", { timeout: 5000 });
      console.log("identity_browser_stage_signed_in");
      await expect(page).toHaveURL(new URL("/identity", origin).href);
      await expect(page.getByText(user, { exact: true })).toBeVisible();
      await expect(
        page.getByText("<script>synthetic profile</script>", { exact: true }),
      ).toBeVisible();
      await expect(
        page.getByText(
          "Signing in does not grant administrator, team or resource permissions.",
        ),
      ).toBeVisible();
      const current = await page.evaluate(async () => {
        const response = await fetch("/api/identity/v1/me", {
          cache: "no-store",
        });
        if (
          response.status !== 200 ||
          !response.headers.get("cache-control")?.includes("no-store")
        ) {
          throw new Error("identity_browser_current_unavailable");
        }
        return response.json();
      });
      expect(current).toEqual({
        version: "1",
        userId: user,
        displayName: "<script>synthetic profile</script>",
        identityMode: "oidc",
        isInitialAdministrator: false,
      });
      const session = (await context.cookies()).find(
        (cookie) => cookie.name === "__Host-kofte-session",
      );
      expect(session !== undefined).toBe(true);
      expect({
        httpOnly: session!.httpOnly,
        secure: session!.secure,
        path: session!.path,
        sameSite: session!.sameSite,
      }).toEqual({ httpOnly: true, secure: true, path: "/", sameSite: "Lax" });
      expect(session!.value.includes(user)).toBe(false);
      expect(await page.evaluate(() => document.cookie)).toBe("");
      expect(
        await page.evaluate(() => localStorage.length + sessionStorage.length),
      ).toBe(0);
      expect(paths).toEqual([
        "login-action",
        "/authorize",
        "/auth/oidc/callback",
      ]);
      const enrollmentFile = process.env.KOFTE_IDENTITY_ENROLLMENT_FILE;
      if (enrollmentFile) {
        await verifyEnrollment(page, root, enrollmentFile);
        return;
      }

      await context.clearCookies();
      await page.goto(new URL("/identity?returnPath=%2F", origin).href);
      await expect(page.getByRole("status")).toHaveText("You are signed out");
      await page.getByRole("button", { name: "Sign in", exact: true }).click();
      await expect(page).toHaveURL(new URL("/", origin).href);
      const repeated = await page.evaluate(async () => {
        const response = await fetch("/api/identity/v1/me", {
          cache: "no-store",
        });
        return response.json();
      });
      expect(repeated).toEqual({
        ...current,
        isInitialAdministrator: false,
      });
      expect(paths).toEqual([
        "login-action",
        "/authorize",
        "/auth/oidc/callback",
        "login-action",
        "/authorize",
        "/auth/oidc/callback",
      ]);

      await context.clearCookies();
      for (const query of [
        "?returnPath=%2F%2Fforeign.invalid",
        "?returnPath=%2F&returnPath=%2Fidentity",
      ]) {
        await page.goto(new URL("/identity" + query, origin).href);
        await expect(page.getByRole("status")).toHaveText(
          "Identity navigation is unavailable",
        );
        await expect(page.locator("form")).toHaveCount(0);
      }
      console.log("identity_browser_stage_safe_returns_verified");
    } catch (error) {
      reportFailure(error);
      throw error;
    }
  });
  console.log("identity_browser_stage_owned_browser_closed");
}

async function verifyEnrollment(page: Page, root: string, file: string) {
  const metadata = await lstat(file);
  if (
    dirname(file) !== root ||
    !metadata.isFile() ||
    metadata.isSymbolicLink() ||
    (metadata.mode & 0o077) !== 0 ||
    metadata.uid !== process.getuid?.() ||
    metadata.size !== 43
  )
    throw new Error("identity_browser_private_trust_required");
  const authorization = await readFile(file, "ascii");
  if (!/^[A-Za-z0-9_-]{42}[AEIMQUYcgkosw048]$/.test(authorization))
    throw new Error("identity_browser_owned_fixture_required");
  const panel = page.getByRole("region", {
    name: "Your identity",
    exact: true,
  });
  const entry = panel.getByLabel("One-time authorization", { exact: true });
  await expect(entry).toHaveAttribute("type", "password");
  await expect(entry).toHaveAttribute("autocomplete", "off");
  await expect(entry).toHaveValue("");
  await page.setViewportSize({ width: 320, height: 900 });
  await page.evaluate(() => {
    document.documentElement.style.fontSize = "200%";
  });
  const heading = page.getByRole("heading", {
    name: "Your identity",
    exact: true,
  });
  await heading.focus();
  await page.keyboard.press("Tab");
  await expect(entry).toBeFocused();
  expect(
    await entry.evaluate((input) => {
      const style = getComputedStyle(input);
      return (
        style.outlineStyle !== "none" &&
        Number.parseFloat(style.outlineWidth) > 0
      );
    }),
  ).toBe(true);
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= window.innerWidth,
    ),
  ).toBe(true);
  const axe = await new AxeBuilder({ page })
    .withTags(["wcag2a", "wcag2aa", "wcag21aa", "wcag22aa"])
    .analyze();
  expect(
    axe.violations.filter(
      ({ impact }) => impact === "serious" || impact === "critical",
    ),
  ).toEqual([]);
  await page.bringToFront();
  await expect(page.getByRole("status")).toHaveText("You are signed in");
  let submissions = 0;
  let identityQueries = 0;
  page.on("request", (request) => {
    expect(request.url()).not.toContain(authorization);
    if (
      request.method() === "POST" &&
      new URL(request.url()).pathname === "/api/identity/v1/first-administrator"
    )
      submissions++;
    if (new URL(request.url()).pathname === "/api/identity/v1/me")
      identityQueries++;
  });
  const wrong =
    authorization.slice(0, -1) + (authorization.endsWith("A") ? "Q" : "A");
  await entry.fill(wrong);
  const [refused] = await Promise.all([
    page.waitForResponse(
      (response) =>
        new URL(response.url()).pathname ===
          "/api/identity/v1/first-administrator" &&
        response.request().method() === "POST",
    ),
    entry.press("Enter"),
  ]);
  expect(refused.status()).toBe(409);
  await expect(panel.getByRole("alert")).toContainText(
    "Enrollment was not completed",
  );
  await expect(entry).toHaveValue("");
  await expect(entry).toBeFocused();
  const queriesBefore = identityQueries;
  await entry.fill(authorization);
  const began = performance.now();
  await entry.press("Enter");
  await expect(panel.getByRole("alert")).toContainText(
    "Enrollment could not be confirmed. No additional enrollment was attempted.",
    { timeout: 5000 },
  );
  await expect(
    page.getByText(
      "Initial administrator designated. This does not grant unrelated permissions.",
      { exact: true },
    ),
  ).toBeVisible({ timeout: 5000 });
  expect(performance.now() - began).toBeLessThan(5000);
  await expect(heading).toBeFocused();
  await expect(entry).toHaveCount(0);
  expect(submissions).toBe(2);
  expect(identityQueries).toBeGreaterThan(queriesBefore);
  expect(await page.content()).not.toContain(authorization);
  expect(await page.content()).not.toContain(wrong);
  expect(
    await page.evaluate(() => localStorage.length + sessionStorage.length),
  ).toBe(0);
  console.log("identity_browser_stage_enrollment_verified");
}

function reportFailure(error: unknown) {
  const message = error instanceof Error ? error.message : "";
  const code = message.match(/\bidentity_browser_[a-z_]{1,70}\b/)?.[0];
  const line =
    error instanceof Error
      ? error.stack?.match(
          /\b(identity\.production|identityBrowser)\.ts:(\d{1,4}):\d+/,
        )
      : undefined;
  console.error(
    code ??
      (line
        ? `identity_browser_${line[1] === "identity.production" ? "page" : "trust"}_line_${line[2]}`
        : "identity_browser_verification_failed"),
  );
}

void verifyLogin().catch((error: unknown) => {
  reportFailure(error);
  process.exitCode = 1;
});
