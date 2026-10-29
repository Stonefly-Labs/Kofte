import AxeBuilder from "@axe-core/playwright";
import { expect, type Locator, type Page } from "@playwright/test";

export async function identityAccessibility(
  page: Page,
  state: Locator,
  nextControl: Locator,
) {
  await page.bringToFront();
  await page.setViewportSize({ width: 320, height: 900 });
  await page.evaluate(() => {
    document.documentElement.style.fontSize = "200%";
  });
  await expect(state).toBeVisible();
  const panel = page.getByRole("region", {
    name: "Your identity",
    exact: true,
  });
  await expect(panel.getByRole("status").first()).toHaveAttribute(
    "aria-live",
    "polite",
  );
  await expect(panel.getByRole("status").first()).toHaveAttribute(
    "aria-atomic",
    "true",
  );
  await panel
    .getByRole("heading", { name: "Your identity", exact: true })
    .focus();
  await page.keyboard.press("Tab");
  await expect(nextControl).toBeFocused();
  expect(
    await nextControl.evaluate((control) => {
      const style = getComputedStyle(control);
      return (
        style.outlineStyle !== "none" &&
        Number.parseFloat(style.outlineWidth) > 0
      );
    }),
    "identity_browser_visible_keyboard_focus",
  ).toBe(true);
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= window.innerWidth,
    ),
    "identity_browser_narrow_enlarged_reflow",
  ).toBe(true);
  const result = await new AxeBuilder({ page })
    .withTags(["wcag2a", "wcag2aa", "wcag21aa", "wcag22aa"])
    .analyze();
  // Do not serialize axe's DOM excerpts: identity pages may contain protected inputs.
  expect(
    result.violations
      .filter(({ impact }) => impact === "serious" || impact === "critical")
      .map(({ id }) => id),
  ).toEqual([]);
  await page.bringToFront();
  await expect(state).toBeVisible();
}
