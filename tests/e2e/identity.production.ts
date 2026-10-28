import { expect } from "@playwright/test";
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
      try {
        await expect(
          page.getByRole("status"),
          "identity_browser_signed_in",
        ).toHaveText("You are signed in", { timeout: 5000 });
      } catch (error) {
        const labels = await page.getByRole("status").allTextContents();
        const state =
          labels.length !== 1
            ? "missing"
            : labels[0] === "Checking your identity"
              ? "loading"
              : labels[0] === "Identity verification is unavailable"
                ? "unavailable"
                : labels[0] === "You are signed out"
                  ? "signed_out"
                  : "other";
        console.log("identity_browser_stage_return_" + state);
        throw error;
      }
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
      expect(repeated).toEqual(current);
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
