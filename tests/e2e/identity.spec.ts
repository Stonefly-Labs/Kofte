import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { controlRuntime } from "./control";
import { identityAccessibility } from "./identityAccessibility";
import { IdentityPrivacyCapture } from "./identityPrivacyCapture";
import { observeIdentityBrowserLifecycle } from "./identityBrowser";

test("the actual identity page uses a private non-personal shell and explicit development context", async ({
  page,
}) => {
  const response = await page.goto("/identity");
  expect(response?.headers()["cache-control"]).toContain("no-store");
  expect(response?.headers()["referrer-policy"]).toBe("no-referrer");
  const html = await response!.text();
  expect(html).not.toContain('name="__RequestVerificationToken"');
  await expect(page.getByRole("status")).toHaveText("You are signed out");
  await expect(
    page.getByText("Development identity", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Sign in", exact: true }),
  ).toHaveCount(0);
  expect(await page.evaluate(() => document.cookie)).toBe("");
  expect(
    await page.evaluate(() => localStorage.length + sessionStorage.length),
  ).toBe(0);
  const identity = await page.request.get("/api/identity/v1/me");
  expect(identity.status()).toBe(401);
  const problem: unknown = await identity.json();
  expect(problem).toMatchObject({
    status: 401,
    code: "identity_unauthenticated",
  });
});

test("identity becomes an actionable failure within five seconds of actual host loss and retries explicitly", async ({
  page,
}) => {
  await page.goto("/identity");
  await expect(page.getByRole("status")).toHaveText("You are signed out");
  await controlRuntime("stop-host");
  try {
    const began = performance.now();
    await page.getByRole("button", { name: "Retry identity check" }).click();
    await expect(page.getByRole("status")).toHaveText(
      "Identity verification is unavailable",
      { timeout: 5000 },
    );
    expect(performance.now() - began).toBeLessThan(5000);
    await expect(page.getByText(/No identity is being assumed/)).toBeVisible();
    await expect(page.locator("form")).toHaveCount(0);
    await identityAccessibility(
      page,
      page.getByRole("status"),
      page.getByRole("button", { name: "Retry identity check" }),
    );
  } finally {
    await controlRuntime("start-host");
  }
  await page.getByRole("button", { name: "Retry identity check" }).click();
  await expect(page.getByRole("status")).toHaveText("You are signed out");
});

test("the delivered identity shell reflows at 320px and 200% with keyboard focus and no serious axe findings", async ({
  page,
}) => {
  await page.setViewportSize({ width: 320, height: 900 });
  await page.goto("/identity");
  await expect(page.getByRole("status")).toHaveText("You are signed out");
  await page.evaluate(() => {
    document.documentElement.style.fontSize = "200%";
  });
  await page.keyboard.press("Tab");
  const selection = page.getByLabel("Configured Development identity");
  await expect(selection).toBeFocused();
  expect(
    await selection.evaluate((element) => {
      const style = getComputedStyle(element);
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
});

test("configured Development selection uses the real browser boundary and never server-renders personal identity", async ({
  page,
  context,
}) => {
  const host = process.env.KOFTE_HOST_URL;
  if (!host) throw new Error("isolated_test_host_required");
  const started = performance.now();
  const phase = (name: string) =>
    console.log(
      JSON.stringify({
        eventCode: "identity_browser_profile_phase",
        phase: name,
        launch: 1,
        elapsedMilliseconds: Math.round(performance.now() - started),
      }),
    );
  const privacy = new IdentityPrivacyCapture(page, [], "development", phase);
  await privacy.run(async () => {
    await observeIdentityBrowserLifecycle(page, phase);
    phase("development_journey_started");
    await page.goto("/identity");
    await expect(
      page.getByText("You are signed out", { exact: true }),
    ).toBeVisible();
    const selection = page.getByLabel("Configured Development identity");
    await expect(selection).toHaveValue("");
    const submit = page.getByRole("button", {
      name: "Use Development identity",
    });
    await expect(submit).toBeDisabled();
    await selection.selectOption("synthetic");
    const began = performance.now();
    phase("development_selection_started");
    await submit.click();
    await expect(
      page.getByText("You are signed in", { exact: true }),
    ).toBeVisible({ timeout: 5000 });
    phase("development_selection_finished");
    expect(performance.now() - began).toBeLessThan(5000);
    await expect(
      page.getByText("Development identity", { exact: true }),
    ).toBeVisible();
    const adapted = await page.request.get("/api/identity/v1/me");
    expect(adapted.status()).toBe(200);
    const identity: unknown = await adapted.json();
    if (
      typeof identity !== "object" ||
      identity === null ||
      !("userId" in identity) ||
      typeof identity.userId !== "string"
    )
      throw new Error("development_browser_identity_required");
    expect(identity).toMatchObject({
      version: "1",
      identityMode: "development",
      isInitialAdministrator: false,
    });
    const expectedUser = process.env.KOFTE_IDENTITY_EXPECTED_USER;
    if (expectedUser) expect(identity.userId).toBe(expectedUser);
    await expect(
      page.getByText(identity.userId, { exact: true }),
    ).toBeVisible();
    phase("signed_in_accessibility_started");
    await identityAccessibility(
      page,
      page.getByText("You are signed in", { exact: true }),
      page.getByRole("button", { name: "Sign out", exact: true }),
    );
    phase("signed_in_accessibility_finished");
    await privacy.snapshot(context);
    const direct = await page.request.get(
      new URL("/api/identity/v1/me", host).href,
    );
    expect(direct.status()).toBe(200);
    expect(await direct.json()).toEqual(identity);
    const shell = await page.request.get("/identity");
    expect(await shell.text()).not.toContain(identity.userId);
    expect(await page.evaluate(() => document.cookie)).toBe("");
    expect(
      await page.evaluate(() => localStorage.length + sessionStorage.length),
    ).toBe(0);
    phase("sign_out_started");
    await page.getByRole("button", { name: "Sign out" }).click();
    await expect(
      page.getByText("You are signed out", { exact: true }),
    ).toBeVisible();
    await expect(page.getByText(identity.userId, { exact: true })).toHaveCount(
      0,
    );
    phase("signed_out_accessibility_started");
    await identityAccessibility(
      page,
      page.getByText("You are signed out", { exact: true }),
      page.getByLabel("Configured Development identity"),
    );
    phase("signed_out_accessibility_finished");
    expect((await page.request.get("/api/identity/v1/me")).status()).toBe(401);
    expect(
      (
        await page.request.get(new URL("/api/identity/v1/me", host).href)
      ).status(),
    ).toBe(401);
    await privacy.proveCapture(context);
  });
});
