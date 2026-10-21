import { expect, test } from "@playwright/test";
import { controlRuntime } from "./control";

test("actual selected-store loss preserves liveness and clears healthy display", async ({
  page,
}) => {
  const host = process.env.KOFTE_HOST_URL;
  if (!host) throw new Error("isolated_test_host_required");
  await page.goto("/");
  await expect(page.getByRole("status")).toContainText("Local host is ready");
  await controlRuntime("unavailable-store");
  try {
    const live = await page.request.get(new URL("/health/live", host).href);
    const ready = await page.request.get(new URL("/health/ready", host).href);
    expect(live.status()).toBe(200);
    expect(ready.status()).toBe(503);
    const started = performance.now();
    await expect(page.getByRole("status")).toContainText(
      "Local host is unavailable",
      { timeout: 40_000 },
    );
    expect(performance.now() - started).toBeLessThan(40_000);
  } finally {
    await controlRuntime("restore-store");
  }
  await page.getByRole("button", { name: "Retry connection check" }).click();
  await expect(page.getByRole("status")).toContainText("Local host is ready", {
    timeout: 10_000,
  });
});

test("an actual host stop and restart recover without resetting the store", async ({
  page,
}) => {
  await page.goto("/");
  await expect(page.getByRole("status")).toContainText("Local host is ready");
  await controlRuntime("stop-host");
  try {
    await page.getByRole("button", { name: "Retry connection check" }).click();
    await expect(page.getByRole("status")).toContainText(
      "Local host is unavailable",
      { timeout: 10_000 },
    );
  } finally {
    await controlRuntime("start-host");
  }
  await page.getByRole("button", { name: "Retry connection check" }).click();
  await expect(page.getByRole("status")).toContainText("Local host is ready");
  await expect(page.getByText(/Retrying never resets your data/)).toHaveCount(
    0,
  );
});

test("a hanging attempt expires and late malformed data cannot replace a retry", async ({
  page,
}) => {
  let release: (() => void) | undefined;
  let intercepted = false;
  await page.route("**/backend/health/ready", async (route) => {
    if (!intercepted) {
      intercepted = true;
      await new Promise<void>((resolve) => {
        release = resolve;
      });
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          state: "ready",
          credential: "credential-canary",
        }),
      });
    } else await route.continue();
  });
  const started = performance.now();
  await page.goto("/");
  await expect(page.getByRole("status")).toContainText(
    "Checking the local host",
  );
  await expect(page.getByRole("status")).toContainText(
    "Local host is unavailable",
    { timeout: 11_000 },
  );
  expect(performance.now() - started).toBeLessThan(11_000);
  await page.getByRole("button", { name: "Retry connection check" }).click();
  await expect(page.getByRole("status")).toContainText("Local host is ready");
  release?.();
  await page.waitForTimeout(100);
  await expect(page.getByRole("status")).toContainText("Local host is ready");
});
