import { randomUUID } from "node:crypto";
import { expect, it } from "vitest";
import { isReadiness } from "@kofte/bootstrap-contract";
import { controlRuntime } from "../e2e/control";
import { requestWithBody } from "./http";

const web = process.env.KOFTE_WEB_URL;
if (!web) throw new Error("isolated_test_web_required");

it("serves only a strict fixed readiness contract without caller credentials", async () => {
  const canary = `credential-canary-${randomUUID()}`;
  const response = await fetch(new URL("/backend/health/ready", web), {
    headers: {
      Authorization: `Bearer ${canary}`,
      Cookie: `credential=${canary}`,
      traceparent: canary,
    },
  });
  expect(response.status).toBe(200);
  expect(response.headers.get("cache-control")).toBe("no-store");
  const text = await response.text();
  expect(text.includes(canary)).toBe(false);
  expect(isReadiness(JSON.parse(text), 200)).toBe(true);
});

it.each([
  [
    "/backend/health/ready?url=http://example.invalid/credential-canary",
    "GET",
    400,
  ],
  ["/backend/health/ready", "POST", 405],
  ["/backend/health/ready", "HEAD", 405],
  ["/backend/health/ready", "OPTIONS", 405],
])(
  "rejects adapter input and unsupported methods",
  async (path, method, status) => {
    const response = await fetch(new URL(path, web), { method });
    expect(response.status).toBe(status);
    expect(response.headers.get("cache-control")).toBe("no-store");
    expect(await response.text()).toBe("");
  },
);

it("rejects an actual GET body without reflecting caller data", async () => {
  const response = await requestWithBody(
    new URL("/backend/health/ready", web),
    `credential-canary-${randomUUID()}`,
  );
  expect(response.status).toBe(400);
  expect(response.body).toBe("");
  expect(response.cacheControl).toBe("no-store");
});

it("preserves actual selected-provider 503 and recovers without changing provider", async () => {
  const host = process.env.KOFTE_HOST_URL;
  if (!host) throw new Error("isolated_test_host_required");
  await controlRuntime("unavailable-store");
  try {
    const live = await fetch(new URL("/health/live", host));
    expect(live.status).toBe(200);
    for (const url of [
      new URL("/health/ready", host),
      new URL("/backend/health/ready", web),
    ]) {
      const response = await fetch(url);
      expect(response.status).toBe(503);
      expect(response.headers.get("cache-control")).toBe("no-store");
      const body: unknown = await response.json();
      expect(isReadiness(body, 503)).toBe(true);
      expect(body).toMatchObject({
        state: "unavailable",
        component: "storage",
      });
    }
  } finally {
    await controlRuntime("restore-store");
  }
  const recovered = await fetch(new URL("/backend/health/ready", web));
  expect(recovered.status).toBe(200);
  expect(isReadiness(await recovered.json(), 200)).toBe(true);
}, 20_000);
