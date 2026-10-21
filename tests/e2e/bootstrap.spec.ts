import { expect, test } from "@playwright/test";

test("shows real readiness and an honest, keyboard-operable bootstrap", async ({
  page,
}) => {
  await page.goto("/");
  await expect(
    page.getByRole("heading", { name: "Köfte", exact: true }),
  ).toBeVisible();
  await expect(page.getByRole("status")).toContainText("Local host is ready");
  await expect(
    page.getByText(/No feature modules, teams, services, login or tools/),
  ).toBeVisible();
  await page.keyboard.press("Tab");
  await expect(
    page.getByRole("button", { name: "Retry connection check" }),
  ).toBeFocused();
  await page.keyboard.press("Enter");
  await expect(page.getByRole("status")).toContainText("Local host is ready");
});
