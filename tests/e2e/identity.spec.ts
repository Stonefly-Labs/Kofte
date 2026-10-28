import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { controlRuntime } from "./control";

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
  const retry = page.getByRole("button", { name: "Retry identity check" });
  await expect(retry).toBeFocused();
  expect(
    await retry.evaluate((button) => {
      const style = getComputedStyle(button);
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
