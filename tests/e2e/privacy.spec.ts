import { expect, test } from "@playwright/test";
import { controlRuntime } from "./control";

test("failure UI, HTTP and browser diagnostics never disclose seeded credentials", async ({
  page,
}) => {
  const canary = process.env.BOOTSTRAP_PRIVACY_CANARY;
  if (!canary) throw new Error("owned_privacy_canary_required");
  const messages: string[] = [];
  page.on("console", (message) => messages.push(message.text()));
  page.on("pageerror", (error) => messages.push(error.message));
  await page.setExtraHTTPHeaders({
    Authorization: `Bearer ${canary}`,
    Cookie: `credential=${canary}`,
    traceparent: canary,
    baggage: `password=${canary}`,
  });
  await page.goto("/");
  await expect(page.getByRole("status")).toContainText("Local host is ready");
  await controlRuntime("unavailable-store");
  try {
    await page.getByRole("button", { name: "Retry connection check" }).click();
    await expect(page.getByRole("status")).toContainText(
      "Local host is unavailable",
      { timeout: 10_000 },
    );
    await expect(page.getByText(/^Reference: [0-9a-f]{32}$/)).toBeVisible();
    const response = await page.request.get("/backend/health/ready");
    expect(response.status()).toBe(503);
    const surfaces = [await response.text(), await page.content(), ...messages];
    expect(surfaces.some((value) => value.includes(canary))).toBe(false);
    expect(
      surfaces.some((value) =>
        /(?:\/Users\/|\/private\/tmp\/|\/home\/runner\/|Npgsql\.|System\.IO\.| at [^\n]+:\d+)/.test(
          value,
        ),
      ),
    ).toBe(false);
  } finally {
    await controlRuntime("restore-store");
  }
  await page.getByRole("button", { name: "Retry connection check" }).click();
  await expect(page.getByRole("status")).toContainText("Local host is ready");
});
