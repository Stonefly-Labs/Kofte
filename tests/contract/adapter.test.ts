import { randomUUID } from "node:crypto";
import { expect, it } from "vitest";
import { isReadiness } from "@kofte/bootstrap-contract";
import { controlRuntime } from "../e2e/control";
import {
  identityMethods,
  identityNumericBoundaries,
  identityRoutes,
  identityTransport,
} from "./identity-corpus";
import { requestWithBody, requestWithHeaderBytes } from "./http";

const web = process.env.KOFTE_WEB_URL;
if (!web) throw new Error("isolated_test_web_required");

function record(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

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
    expect(Object.keys(anonymous).sort()).toEqual(
      ["version", "identityMode", "csrfToken"].sort(),
    );
    expect(anonymous).toMatchObject({
      version: "1",
      identityMode: "development",
    });
    const mutation = {
      Origin: web,
      "X-Kofte-CSRF": anonymous.csrfToken,
      "Content-Type": "application/json",
    };
    const bodyBoundary = identityNumericBoundaries.find(
      ([name]) => name === "mutation_body_bytes",
    );
    const headerBoundary = identityNumericBoundaries.find(
      ([name]) => name === "headers_and_cookies_bytes",
    );
    if (!bodyBoundary || !headerBoundary)
      throw new Error("independent_development_boundary_required");
    for (const [allowed, path] of identityRoutes.filter(([, path]) =>
      path.startsWith("/api/identity/"),
    ))
      for (const method of identityMethods) {
        if (method === allowed) continue;
        const response = await send(path, { method });
        expect(response.status).toBe(405);
        expect(response.headers.get("allow")).toBe(allowed);
        expect(response.headers.getSetCookie()).toEqual([]);
        expect(await response.text()).toBe("");
      }
    for (const path of [
      "/auth/login",
      "/auth/oidc/callback",
      "/auth/Login",
      "/api/identity/v2/me",
      "/api/identity/v1/Me",
      "/api/identity/v1/development-session/",
      "/api/identity/v1/admin",
      "/api/identity/v1/test/reset",
    ])
      for (const method of identityMethods) {
        const response = await send(path, { method });
        expect(response.status).toBe(404);
        expect(response.headers.getSetCookie()).toEqual([]);
        expect(await response.text()).toBe("");
      }
    const refusedHeaders: HeadersInit[] = [
      {
        "X-Kofte-CSRF": anonymous.csrfToken,
        "Content-Type": "application/json",
      },
      { ...mutation, Origin: "null" },
      { ...mutation, Origin: "https://foreign.example.invalid" },
      { ...mutation, "Sec-Fetch-Site": "cross-site" },
      { Origin: web, "Content-Type": "application/json" },
      { ...mutation, "X-Kofte-CSRF": "never-issued" },
    ];
    for (const headers of refusedHeaders) {
      const response = await send("/api/identity/v1/development-session", {
        method: "POST",
        headers,
        body: '{"identityKey":"synthetic"}',
      });
      expect(response.status).toBe(403);
      expect(await response.json()).toMatchObject({
        status: 403,
        code: "identity_forbidden",
      });
    }
    const continuity = [...cookies]
      .map(([key, value]) => `${key}=${value}`)
      .join("; ");
    const ambiguous: Record<string, string | string[]>[] = [
      { Origin: [web, web] },
      { "Sec-Fetch-Site": ["same-origin", "cross-site"] },
      { "X-Kofte-CSRF": [anonymous.csrfToken, anonymous.csrfToken] },
    ];
    for (const headers of ambiguous) {
      const response = await requestWithBody(
        new URL("/api/identity/v1/development-session", transport),
        '{"identityKey":"synthetic"}',
        { ...mutation, Cookie: continuity, ...headers },
        "POST",
      );
      expect(response.status).toBe(403);
      expect(response.cacheControl).toBe("no-store");
      expect(JSON.parse(response.body)).toMatchObject({
        status: 403,
        code: "identity_forbidden",
      });
    }
    for (const body of [
      "null",
      "[]",
      "{}",
      '{"identityKey":false}',
      '{"identityKey":"not-configured"}',
      '{"identityKey":"synthetic","identityKey":"synthetic"}',
      '{"identityKey":"synthetic","userId":"caller-selected"}',
      '{"identityKey":"synthetic","isInitialAdministrator":true}',
      '{"identityKey":"synthetic","email":"admin@example.invalid","displayName":"Administrator"}',
      '{"identityKey":"synthetic"}'.padEnd(bodyBoundary[2], " "),
    ]) {
      const response = await send("/api/identity/v1/development-session", {
        method: "POST",
        headers: mutation,
        body,
      });
      expect(response.status).toBe(400);
      expect(response.headers.getSetCookie()).toEqual([]);
      expect(await response.json()).toMatchObject({
        status: 400,
        code: "identity_request_invalid",
      });
      expect((await send("/api/identity/v1/me")).status).toBe(401);
    }
    const contextQuery = await send(
      "/api/identity/v1/browser-context?userId=caller-selected",
    );
    expect(contextQuery.status).toBe(400);
    const contextBody = await requestWithBody(
      new URL("/api/identity/v1/browser-context", transport),
      '{"userId":"caller-selected"}',
    );
    expect(contextBody.status).toBe(400);
    expect(contextBody.cacheControl).toBe("no-store");
    const lengths: readonly number[] = [headerBoundary[1], headerBoundary[2]];
    for (const length of lengths) {
      const response = await requestWithHeaderBytes(
        new URL("/api/identity/v1/browser-context", transport),
        length,
      );
      expect(response.status).toBe(length === headerBoundary[1] ? 200 : 400);
      expect(response.cacheControl).toBe(identityTransport.cacheControl);
      expect(response.headers.pragma).toBe(identityTransport.pragma);
      expect(response.headers["referrer-policy"]).toBe(
        identityTransport.referrerPolicy,
      );
      expect(response.headers.location).toBeUndefined();
      if (length === headerBoundary[2])
        expect(response.headers["set-cookie"]).toBeUndefined();
    }
    const selected = await send("/api/identity/v1/development-session", {
      method: "POST",
      headers: mutation,
      body: '{"identityKey":"synthetic"}'.padEnd(bodyBoundary[1], " "),
    });
    expect(selected.status).toBe(200);
    const identity: unknown = await selected.json();
    if (!record(identity) || typeof identity.userId !== "string")
      throw new Error("development_identity_shape_invalid");
    expect(Object.keys(identity).sort()).toEqual(
      [...identityTransport.currentIdentityFields].sort(),
    );
    expect(identity.version).toBe("1");
    expect(identity.userId).toMatch(
      /^[a-f0-9]{8}(?:-[a-f0-9]{4}){3}-[a-f0-9]{12}$/,
    );
    expect(identity.userId).not.toBe("00000000-0000-0000-0000-000000000000");
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
    expect(attributes).toContain("samesite=lax");
    expect(attributes).toContain("path=/");
    expect(attributes?.some((value) => value.startsWith("domain="))).toBe(
      false,
    );
    expect(attributes).not.toContain("secure");
    for (const origin of [host, web]) {
      const response = await fetch(new URL("/api/identity/v1/me", origin), {
        headers: {
          Cookie: [...cookies]
            .map(([key, value]) => `${key}=${value}`)
            .join("; "),
          "X-User-Id": "00000000-0000-4000-8000-000000000099",
          "X-Initial-Administrator": "true",
        },
        redirect: "manual",
        cache: "no-store",
        signal: AbortSignal.timeout(6000),
      });
      expect(response.status).toBe(200);
      expect(await response.json()).toEqual(identity);
    }
    const credentials: HeadersInit[] = [
      {},
      { Cookie: "kofte-dev-session=never-issued" },
      {
        Cookie: `__Host-kofte-session=${cookies.get("kofte-dev-session")}`,
      },
      { Authorization: "Bearer externally-asserted" },
      {
        Cookie: [...cookies]
          .map(([key, value]) => `${key}=${value}`)
          .join("; "),
        Authorization: "Basic externally-asserted",
      },
    ];
    for (const headers of credentials) {
      const response = await fetch(new URL("/api/identity/v1/me", transport), {
        headers,
        redirect: "manual",
        cache: "no-store",
        signal: AbortSignal.timeout(6000),
      });
      expect(response.status).toBe(401);
      expect(response.headers.get("cache-control")).toBe("no-store");
      expect(response.headers.get("location")).toBeNull();
      expect(await response.json()).toMatchObject({
        status: 401,
        code: "identity_unauthenticated",
      });
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
    const enrollment = await send("/api/identity/v1/first-administrator", {
      method: "POST",
      headers: {
        Origin: web,
        "X-Kofte-CSRF": fresh.csrfToken,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ authorization: "A".repeat(43) }),
    });
    expect(enrollment.status).toBe(403);
    expect(await enrollment.json()).toMatchObject({
      status: 403,
      code: "identity_forbidden",
    });
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
