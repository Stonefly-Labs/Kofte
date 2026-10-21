import { expect, test } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";

for (const width of [320, 768, 1440]) {
  test(`bootstrap is accessible and fits at ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 900 });
    await page.emulateMedia({ reducedMotion: "reduce" });
    let release: (() => void) | undefined;
    let initial = true;
    let unavailable = false;
    await page.route("**/backend/health/ready", async (route) => {
      if (initial) {
        initial = false;
        await new Promise<void>((resolve) => {
          release = resolve;
        });
      }
      await route.fulfill({
        status: unavailable ? 503 : 200,
        contentType: "application/json",
        body: JSON.stringify({
          version: "1",
          state: unavailable ? "unavailable" : "ready",
          component: "storage",
          correlationId: "a".repeat(32),
        }),
      });
    });
    await page.goto("/");
    await expect.poll(() => typeof release).toBe("function");
    for (const label of [
      "Checking the local host",
      "Local host is ready",
      "Local host is unavailable",
    ]) {
      await expect(page.getByRole("status")).toContainText(label);
      await expect(page.getByRole("main")).toBeVisible();
      const results = await new AxeBuilder({ page })
        .withTags(["wcag2a", "wcag2aa", "wcag21aa", "wcag22aa"])
        .analyze();
      expect(
        results.violations.filter(
          ({ impact }) => impact === "serious" || impact === "critical",
        ),
      ).toEqual([]);
      expect(
        await page.evaluate(
          () => document.documentElement.scrollWidth <= window.innerWidth,
        ),
      ).toBe(true);
      if (label === "Checking the local host") {
        release?.();
      } else if (label === "Local host is ready") {
        await page.keyboard.press("Tab");
        const retry = page.getByRole("button", {
          name: "Retry connection check",
        });
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
        unavailable = true;
        await page.keyboard.press("Enter");
        await expect(retry).toBeFocused();
      }
    }
    if (width === 320) {
      await page.evaluate(() => {
        document.documentElement.style.fontSize = "200%";
      });
      expect(
        await page.evaluate(
          () => document.documentElement.scrollWidth <= window.innerWidth,
        ),
      ).toBe(true);
    }
  });
}
