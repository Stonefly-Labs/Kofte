import { expect } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { lstat, readFile, writeFile } from "node:fs/promises";
import { dirname, join } from "node:path";
import type { BrowserContext, Page } from "@playwright/test";
import { withIdentityBrowser } from "./identityBrowser.ts";
import { identityAccessibility } from "./identityAccessibility.ts";
import { IdentityPrivacyCapture } from "./identityPrivacyCapture.ts";

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
    const page = context.pages()[0];
    const privacy = new IdentityPrivacyCapture(page);
    try {
      console.log("identity_browser_stage_trust_ready");
      await page.bringToFront();
      console.log("identity_browser_stage_page_active");
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
      console.log("identity_browser_stage_shell_received");
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
      await privacy.snapshot(context);
      if (process.env.KOFTE_IDENTITY_SESSION_BOUNDARY === "1") {
        await verifySession(page, context, root, user, privacy);
        await privacy.proveCapture(context);
        return;
      }
      const enrollmentFile = process.env.KOFTE_IDENTITY_ENROLLMENT_FILE;
      if (enrollmentFile) {
        await verifyEnrollment(page, context, root, enrollmentFile, privacy);
        await privacy.proveCapture(context);
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
      await privacy.proveCapture(context);
    } catch (error) {
      reportFailure(error);
      throw error;
    } finally {
      await privacy.close();
    }
  });
  console.log("identity_browser_stage_owned_browser_closed");
}

async function browserControl(root: string, phase: string) {
  await writeFile(join(root, `browser-control-${phase}-request`), "", {
    flag: "wx",
    mode: 0o600,
  });
  const until = performance.now() + 5000;
  while (performance.now() < until) {
    try {
      const file = await lstat(join(root, `browser-control-${phase}-ready`));
      if (
        !file.isFile() ||
        file.isSymbolicLink() ||
        file.size !== 0 ||
        (file.mode & 0o077) !== 0 ||
        file.uid !== process.getuid?.()
      )
        throw new Error("identity_browser_private_control_invalid");
      return;
    } catch (error) {
      if (!(
        error instanceof Error &&
        "code" in error &&
        error.code === "ENOENT"
      ))
        throw error;
    }
    await new Promise((resolve) => setTimeout(resolve, 10));
  }
  throw new Error("identity_browser_private_control_timeout");
}

async function verifySession(
  page: Page,
  context: BrowserContext,
  root: string,
  user: string,
  privacy: IdentityPrivacyCapture,
) {
  const panel = page.getByRole("region", {
    name: "Your identity",
    exact: true,
  });
  const heading = panel.getByRole("heading", {
    name: "Your identity",
    exact: true,
  });
  let submissions = 0;
  page.on("request", (request) => {
    if (
      request.method() === "POST" &&
      new URL(request.url()).pathname === "/api/identity/v1/sign-out"
    )
      submissions++;
  });
  const invalid = await page.evaluate(async () => {
    const response = await fetch("/api/identity/v1/sign-out", {
      method: "POST",
      cache: "no-store",
      redirect: "error",
      headers: { "X-Kofte-CSRF": "invalid-pair" },
    });
    return response.status;
  });
  expect(invalid).toBe(403);
  expect(submissions).toBe(1);
  await expect(page.getByText(user, { exact: true })).toBeVisible();
  const origin = new URL(page.url()).origin;
  const browserContext = await context.request.get(
    new URL("/api/identity/v1/browser-context", origin).href,
    { timeout: 5000 },
  );
  expect(browserContext.status()).toBe(200);
  const continuity = await browserContext.json();
  expect(continuity.csrfToken).toEqual(expect.any(String));
  const invalidOrigin = await context.request.post(
    new URL("/api/identity/v1/sign-out", origin).href,
    {
      headers: { Origin: "null", "X-Kofte-CSRF": continuity.csrfToken },
      timeout: 5000,
    },
  );
  expect(invalidOrigin.status()).toBe(403);
  expect(invalidOrigin.headers()["cache-control"]).toBe("no-store");
  await expect(page.getByText(user, { exact: true })).toBeVisible();
  const ungranted = await page.evaluate(async () => {
    const response = await fetch("/fixture-direct-ungranted", {
      cache: "no-store",
      redirect: "error",
    });
    const body = await response.json();
    return {
      status: response.status,
      code: body.code,
      noStore: response.headers.get("cache-control"),
      location: response.headers.get("location"),
    };
  });
  expect(ungranted).toEqual({
    status: 403,
    code: "identity_forbidden",
    noStore: "no-store",
    location: null,
  });
  const direct = await page.evaluate(async () => {
    const response = await fetch("/fixture-direct-identity", {
      cache: "no-store",
    });
    return response.json();
  });
  expect(direct).toEqual({
    version: "1",
    userId: user,
    displayName: "<script>synthetic profile</script>",
    identityMode: "oidc",
    isInitialAdministrator: false,
  });

  async function denied() {
    const statuses = await page.evaluate(async () => {
      const results = [];
      for (const path of ["/api/identity/v1/me", "/fixture-direct-identity"]) {
        const response = await fetch(path, {
          cache: "no-store",
          redirect: "error",
        });
        const body = await response.json();
        results.push({
          status: response.status,
          code: body.code,
          noStore: response.headers.get("cache-control"),
          location: response.headers.get("location"),
        });
      }
      return results;
    });
    expect(statuses).toEqual(
      Array.from({ length: 2 }, () => ({
        status: 401,
        code: "identity_unauthenticated",
        noStore: "no-store",
        location: null,
      })),
    );
  }

  async function enter(expected?: string) {
    await panel.getByRole("button", { name: "Sign in", exact: true }).click();
    await expect(panel.getByRole("status")).toHaveText("You are signed in", {
      timeout: 5000,
    });
    const identity = await page.evaluate(async () => {
      const response = await fetch("/api/identity/v1/me", {
        cache: "no-store",
      });
      return response.json();
    });
    expect(identity.identityMode).toBe("oidc");
    expect(identity.isInitialAdministrator).toBe(false);
    expect(identity.displayName).toBe("<script>synthetic profile</script>");
    expect(identity.userId).toMatch(
      /^[0-9a-f]{8}(-[0-9a-f]{4}){3}-[0-9a-f]{12}$/,
    );
    if (expected) expect(identity.userId).toBe(expected);
    else expect(identity.userId).not.toBe(user);
    const direct = await page.evaluate(async () => {
      const response = await fetch("/fixture-direct-identity", {
        cache: "no-store",
        redirect: "error",
      });
      return {
        status: response.status,
        noStore: response.headers.get("cache-control"),
        body: await response.json(),
      };
    });
    expect(direct).toEqual({
      status: 200,
      noStore: "no-store",
      body: identity,
    });
    const shell = await context.request.get(new URL("/identity", origin).href, {
      timeout: 5000,
    });
    expect(shell.status()).toBe(200);
    expect(shell.headers()["cache-control"]).toBe("no-store");
    const html = await shell.text();
    expect(html).not.toContain(user);
    expect(html).not.toContain(identity.userId);
    await expect(
      page.getByText(identity.userId, { exact: true }),
    ).toBeVisible();
    return identity.userId as string;
  }

  await browserControl(root, "expire-idle");
  await panel.getByRole("button", { name: "Retry identity check" }).click();
  await expect(panel.getByRole("status")).toHaveText(
    "Your session has expired",
    { timeout: 5000 },
  );
  await expect(page.getByText(user, { exact: true })).toHaveCount(0);
  await identityAccessibility(
    page,
    panel.getByText("Your session has expired", { exact: true }),
    panel.getByRole("button", { name: "Sign in", exact: true }),
  );
  await denied();
  await privacy.snapshot(context);
  await enter(user);
  for (let index = 1; index <= 16; index++) {
    await browserControl(root, `activity-${String(index).padStart(2, "0")}`);
    expect(
      await page.evaluate(
        async () =>
          (
            await fetch("/api/identity/v1/me", {
              cache: "no-store",
              redirect: "error",
            })
          ).status,
      ),
    ).toBe(200);
  }
  await browserControl(root, "expire-absolute");
  await panel.getByRole("button", { name: "Retry identity check" }).click();
  await expect(panel.getByRole("status")).toHaveText(
    "Your session has expired",
    { timeout: 5000 },
  );
  await expect(page.getByText(user, { exact: true })).toHaveCount(0);
  await denied();
  await privacy.snapshot(context);
  await enter(user);

  const oldCookie = (await context.cookies()).find(
    (cookie) => cookie.name === "__Host-kofte-session",
  );
  expect(oldCookie).toBeDefined();
  const began = performance.now();
  await panel.getByRole("button", { name: "Sign out", exact: true }).focus();
  await page.keyboard.press("Enter");
  await expect(
    panel.getByText("Signed out of Köfte.", { exact: false }),
  ).toBeVisible({ timeout: 5000 });
  expect(performance.now() - began).toBeLessThan(5000);
  await expect(page.getByText(user, { exact: true })).toHaveCount(0);
  await expect(heading).toBeFocused();
  expect(submissions).toBe(2);
  await privacy.snapshot(context);
  await context.addCookies([oldCookie!]);
  await denied();
  await privacy.snapshot(context);
  const shell = await page.reload();
  expect(shell?.headers()["cache-control"]).toContain("no-store");
  expect(await shell!.text()).not.toContain(user);
  await expect(panel.getByRole("status")).toHaveText("You are signed out", {
    timeout: 5000,
  });
  await browserControl(root, "switch-identity");
  let previousIdentityObserved = false;
  page.on("console", (message) => {
    if (message.text() === "identity_browser_previous_user_observed")
      previousIdentityObserved = true;
  });
  function watchPreviousIdentity(previousUser: string) {
    new MutationObserver(() => {
      if (document.body?.textContent?.includes(previousUser))
        console.error("identity_browser_previous_user_observed");
    }).observe(document, {
      childList: true,
      characterData: true,
      subtree: true,
    });
  }
  await page.addInitScript(watchPreviousIdentity, user);
  await page.evaluate(watchPreviousIdentity, user);
  const other = await enter();
  await expect(page.getByText(user, { exact: true })).toHaveCount(0);
  await browserControl(root, "lose-sign-out");
  const uncertain = performance.now();
  await panel.getByRole("button", { name: "Sign out", exact: true }).click();
  await expect(panel.getByRole("alert")).toContainText(
    "Server revocation could not be confirmed",
    { timeout: 5000 },
  );
  expect(performance.now() - uncertain).toBeLessThan(5000);
  await expect(page.getByText(other, { exact: true })).toHaveCount(0);
  await identityAccessibility(
    page,
    panel.getByRole("alert"),
    panel.getByRole("button", { name: "Retry identity check" }),
  );
  await denied();
  await page.addInitScript(watchPreviousIdentity, other);
  await page.evaluate(watchPreviousIdentity, other);
  await panel.getByRole("button", { name: "Retry identity check" }).click();
  await expect(
    panel.getByRole("button", { name: "Sign in", exact: true }),
  ).toBeVisible();
  expect(submissions).toBe(3);
  await expect(page.getByText(other, { exact: true })).toHaveCount(0);
  expect(
    await page.evaluate(() => localStorage.length + sessionStorage.length),
  ).toBe(0);
  await page.setViewportSize({ width: 320, height: 900 });
  await page.evaluate(() => {
    document.documentElement.style.fontSize = "200%";
  });
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
  await expect(
    panel.getByRole("button", { name: "Sign in", exact: true }),
  ).toBeVisible();
  await expect(page.getByText(other, { exact: true })).toHaveCount(0);
  expect(previousIdentityObserved).toBe(false);
  console.log("identity_browser_stage_session_boundaries_verified");
}

async function verifyEnrollment(
  page: Page,
  context: BrowserContext,
  root: string,
  file: string,
  privacy: IdentityPrivacyCapture,
) {
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
  privacy.protect(authorization);
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
  privacy.protect(wrong);
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
  await expect(entry).toHaveAttribute("aria-invalid", "true");
  await expect(panel.getByRole("alert")).toContainText(
    "Ask your operator to check the nomination",
  );
  await identityAccessibility(page, panel.getByRole("alert"), entry);
  await privacy.snapshot(context);
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
  await identityAccessibility(
    page,
    panel.getByRole("alert"),
    panel.getByRole("button", { name: "Sign out", exact: true }),
  );
  await privacy.snapshot(context);
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
