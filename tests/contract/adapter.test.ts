import { randomUUID } from "node:crypto";
import { expect, it } from "vitest";
import { isReadiness } from "@kofte/bootstrap-contract";
import { isCurrentIdentity } from "@kofte/bootstrap-contract/identity";
import { controlRuntime } from "../e2e/control";
import { requestWithBody } from "./http";

const web = process.env.KOFTE_WEB_URL;
if (!web) throw new Error("isolated_test_web_required");

it("establishes only configured Development identities with fresh CSRF and exact direct/adapted session parity", async () => {
  const host = process.env.KOFTE_HOST_URL;
  if (!host) throw new Error("isolated_test_host_required");
  let originalUser: string | undefined;
  for (const transport of [host, web]) {
    const cookies = new Map<string, string>();
    async function send(path: string, init?: RequestInit) {
      const headers = new Headers(init?.headers);
      headers.set(
        "Cookie",
        [...cookies].map(([key, value]) => `${key}=${value}`).join("; "),
      );
      const response = await fetch(new URL(path, transport), {
        ...init,
        headers,
        redirect: "manual",
        cache: "no-store",
        signal: AbortSignal.timeout(6000),
      });
      for (const cookie of response.headers.getSetCookie()) {
        const pair = cookie.split(";")[0];
        const separator = pair.indexOf("=");
        const key = pair.slice(0, separator),
          value = pair.slice(separator + 1);
        if (value) cookies.set(key, value);
        else cookies.delete(key);
      }
      expect(response.headers.get("cache-control")).toBe("no-store");
      expect(response.headers.get("pragma")).toBe("no-cache");
      expect(response.headers.get("referrer-policy")).toBe("no-referrer");
      expect(response.headers.get("location")).toBeNull();
      return response;
    }
    const context = await send("/api/identity/v1/browser-context");
    const anonymous: unknown = await context.json();
    if (
      typeof anonymous !== "object" ||
      anonymous === null ||
      !("csrfToken" in anonymous) ||
      typeof anonymous.csrfToken !== "string"
    )
      throw new Error("development_browser_context_required");
    const selected = await send("/api/identity/v1/development-session", {
      method: "POST",
      headers: {
        Origin: web,
        "X-Kofte-CSRF": anonymous.csrfToken,
        "Content-Type": "application/json",
      },
      body: '{"identityKey":"synthetic"}',
    });
    expect(selected.status).toBe(200);
    const identity: unknown = await selected.json();
    if (!isCurrentIdentity(identity))
      throw new Error("development_identity_shape_invalid");
    expect(identity.identityMode).toBe("development");
    expect(identity.isInitialAdministrator).toBe(false);
    expect(identity.displayName).toBeNull();
    originalUser ??= identity.userId;
    expect(identity.userId).toBe(originalUser);
    expect(cookies.has("kofte-dev-session")).toBe(true);
    expect(cookies.has("__Host-kofte-session")).toBe(false);
    const sessionCookie = selected.headers
      .getSetCookie()
      .find((value) => value.startsWith("kofte-dev-session="));
    const attributes = sessionCookie
      ?.split(";")
      .slice(1)
      .map((value) => value.trim().toLowerCase());
    expect(attributes).toContain("httponly");
    expect(attributes).not.toContain("secure");
    for (const origin of [host, web]) {
      const response = await fetch(new URL("/api/identity/v1/me", origin), {
        headers: {
          Cookie: [...cookies]
            .map(([key, value]) => `${key}=${value}`)
            .join("; "),
        },
        redirect: "manual",
        cache: "no-store",
        signal: AbortSignal.timeout(6000),
      });
      expect(response.status).toBe(200);
      expect(await response.json()).toEqual(identity);
    }
    const stale = await send("/api/identity/v1/sign-out", {
      method: "POST",
      headers: { Origin: web, "X-Kofte-CSRF": anonymous.csrfToken },
    });
    expect(stale.status).toBe(403);
    const signedIn = await send("/api/identity/v1/browser-context");
    const fresh: unknown = await signedIn.json();
    if (
      typeof fresh !== "object" ||
      fresh === null ||
      !("csrfToken" in fresh) ||
      typeof fresh.csrfToken !== "string"
    )
      throw new Error("development_fresh_context_required");
    expect(fresh.csrfToken).not.toBe(anonymous.csrfToken);
    const revoked = await send("/api/identity/v1/sign-out", {
      method: "POST",
      headers: { Origin: web, "X-Kofte-CSRF": fresh.csrfToken },
    });
    expect(revoked.status).toBe(204);
    expect((await send("/api/identity/v1/me")).status).toBe(401);
  }
}, 20_000);

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
