import { readFileSync } from "node:fs";
import { expect, it } from "vitest";
import {
  allowedIdentityReturns,
  identityNumericBoundaries,
  identityRefusals,
  identityRoutes,
  identityTransport,
  rejectedIdentityReturns,
} from "./identity-corpus";
import { requestWithBody } from "./http";

const file = process.env.KOFTE_IDENTITY_CONTRACT_FILE;
if (!file) throw new Error("owned_identity_contract_fixture_required");
const input: unknown = JSON.parse(readFileSync(file, "utf8"));
function record(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === "object" && !Array.isArray(value);
}
if (
  !record(input) ||
  typeof input.nativeOrigin !== "string" ||
  typeof input.webOrigin !== "string" ||
  typeof input.publicOrigin !== "string" ||
  typeof input.cookie !== "string" ||
  typeof input.csrf !== "string" ||
  typeof input.idToken !== "string" ||
  !record(input.expected) ||
  typeof input.expected.isInitialAdministrator !== "boolean"
)
  throw new Error("owned_identity_contract_input_invalid");
const { nativeOrigin, webOrigin, publicOrigin, cookie, csrf, idToken } = input;
const expected = input.expected;
const origins = [nativeOrigin, webOrigin];
const session = { Cookie: cookie };
const mutation = {
  ...session,
  Origin: publicOrigin,
  "X-Kofte-CSRF": csrf,
};

function send(origin: string, path: string, init?: RequestInit) {
  return fetch(new URL(path, origin), {
    redirect: "manual",
    cache: "no-store",
    signal: AbortSignal.timeout(6000),
    ...init,
  });
}
function privateResponse(response: Response) {
  expect(response.headers.get("cache-control")).toBe(
    identityTransport.cacheControl,
  );
  expect(response.headers.get("pragma")).toBe(identityTransport.pragma);
  expect(response.headers.get("referrer-policy")).toBe(
    identityTransport.referrerPolicy,
  );
}
async function problem(response: Response, status: number, code: string) {
  expect(response.status).toBe(status);
  privateResponse(response);
  expect(response.headers.get("location")).toBeNull();
  expect(response.headers.get("content-type")?.split(";")[0]).toBe(
    "application/problem+json",
  );
  const body: unknown = await response.json();
  if (!record(body)) throw new Error("identity_problem_shape_invalid");
  expect(Object.keys(body).sort()).toEqual(
    ["type", "title", "status", "code", "correlationId"].sort(),
  );
  expect(body).toMatchObject({
    type: `urn:kofte:problem:${code}`,
    status,
    code,
  });
  expect(body.correlationId).toMatch(/^[a-f0-9]{32}$/);
}
function refusal(name: string) {
  const row = identityRefusals.find(([key]) => key === name);
  if (!row || row[2] === null)
    throw new Error("independent_identity_expectation_required");
  return { status: row[1], code: row[2] };
}

it("compares exact independent current identity and separately compiled actor without granting authority", async () => {
  for (const origin of origins) {
    const response = await send(origin, "/api/identity/v1/me", {
      headers: {
        ...session,
        "X-User-Id": "00000000-0000-4000-8000-000000000099",
        "X-Identity-Mode": "development",
        "X-Initial-Administrator": "true",
        "X-Forwarded-Host": "foreign.example.invalid",
      },
    });
    expect(response.status).toBe(200);
    privateResponse(response);
    const body: unknown = await response.json();
    expect(body).toEqual(expected);
    expect(Object.keys(expected).sort()).toEqual(
      [...identityTransport.currentIdentityFields].sort(),
    );
  }
  const actor = await send(nativeOrigin, "/fixture-actor", {
    headers: session,
  });
  expect(actor.status).toBe(200);
  expect(await actor.json()).toEqual({
    userId: expected.userId,
    identityMode: "oidc",
    isInitialAdministrator: expected.isInitialAdministrator,
  });
  const denied = refusal("initial_admin_ungranted_action");
  await problem(
    await send(nativeOrigin, "/fixture-ungranted", { headers: session }),
    denied.status,
    denied.code,
  );
});

if (!expected.isInitialAdministrator) {
  it("refuses anonymous, forged, wrong namespace and actual signed provider credentials", async () => {
    const denied = refusal("authorization_header_with_or_without_cookie");
    const credentials: HeadersInit[] = [
      {},
      { Cookie: "__Host-kofte-session=never-issued" },
      { Cookie: "kofte-dev-session=never-issued" },
      { Authorization: `Bearer ${idToken}` },
      { ...session, Authorization: `Bearer ${idToken}` },
    ];
    for (const origin of origins) {
      for (const headers of credentials)
        await problem(
          await send(origin, "/api/identity/v1/me", { headers }),
          denied.status,
          denied.code,
        );
    }
  });

  it("retains exact empty private method refusals and Allow through actual routes", async () => {
    const routes = identityRoutes.filter(
      ([, path]) =>
        (path.startsWith("/api/identity/") || path.startsWith("/auth/")) &&
        path !== "/api/identity/v1/development-session",
    );
    for (const origin of origins)
      for (const [allowed, path] of routes)
        for (const method of [
          "GET",
          "POST",
          "PUT",
          "PATCH",
          "DELETE",
          "HEAD",
          "OPTIONS",
        ]) {
          if (method === allowed) continue;
          const response = await send(origin, path, {
            method,
            headers: session,
          });
          expect(response.status).toBe(405);
          expect(response.headers.get("allow")).toBe(allowed);
          privateResponse(response);
          expect(await response.text()).toBe("");
          expect(response.headers.get("location")).toBeNull();
        }
  });

  it("never redirects or renders HTML for unknown, wrong-case or noncanonical identity paths", async () => {
    for (const [transport, origin] of origins.entries())
      for (const [index, path] of [
        "/api/identity/v1/unknown",
        "/api/identity/v2/me",
        "/api/identity/v1/Me",
        "/API/identity/v1/me",
        "/api/identity/v1/me/",
        "/api/identity/v1/development-session",
        "/auth/Login",
        "/Auth/login",
        "/auth/unknown",
        "/auth/login/",
        "/auth/oidc/callback/",
      ].entries())
        for (const method of [
          "GET",
          "POST",
          "PUT",
          "PATCH",
          "DELETE",
          "HEAD",
          "OPTIONS",
        ]) {
          const response = await send(origin, path, {
            method,
            headers: session,
          });
          if (response.status !== 404)
            throw new Error(
              `identity_independent_route_${transport}_${index}_${response.status}`,
            );
          expect(response.status).toBe(404);
          privateResponse(response);
          expect(await response.text()).toBe("");
          expect(response.headers.get("location")).toBeNull();
          expect(response.headers.getSetCookie()).toEqual([]);
        }
  });

  it("binds protected mutation to the exact Origin, Fetch Metadata and current CSRF pair", async () => {
    const denied = refusal("missing_foreign_null_origin_or_cross_site");
    const anonymous = await send(
      nativeOrigin,
      "/api/identity/v1/browser-context",
    );
    const context: unknown = await anonymous.json();
    if (!record(context) || typeof context.csrfToken !== "string")
      throw new Error("independent_anonymous_context_required");
    for (const origin of origins)
      for (const headers of [
        { ...session, "X-Kofte-CSRF": csrf },
        { ...mutation, Origin: "null" },
        { ...mutation, Origin: "https://foreign.example.invalid" },
        { ...mutation, "Sec-Fetch-Site": "cross-site" },
        { ...session, Origin: publicOrigin },
        { ...mutation, "X-Kofte-CSRF": "never-issued" },
        { ...mutation, "X-Kofte-CSRF": context.csrfToken },
      ])
        await problem(
          await send(origin, "/api/identity/v1/sign-out", {
            method: "POST",
            headers,
          }),
          denied.status,
          denied.code,
        );
  });

  it("rejects caller identity, query/body confusion and duplicate or unknown mutation fields", async () => {
    const denied = refusal("unknown_duplicate_json_or_form_field");
    const authorization = "A".repeat(43);
    for (const origin of origins) {
      await problem(
        await send(origin, "/api/identity/v1/me?userId=caller-selected", {
          headers: session,
        }),
        denied.status,
        denied.code,
      );
      const body = await requestWithBody(
        new URL("/api/identity/v1/me", origin),
        '{"userId":"caller-selected","isInitialAdministrator":true,"email":"admin@example.invalid","displayName":"Administrator"}',
        session,
      );
      expect(body.status).toBe(400);
      expect(body.cacheControl).toBe("no-store");
      for (const body of [
        `{"authorization":"${authorization}","authorization":"${authorization}"}`,
        JSON.stringify({ authorization, userId: "caller-selected" }),
        JSON.stringify({ authorization, isInitialAdministrator: true }),
        JSON.stringify({
          authorization,
          displayName: "Administrator",
          email: "admin@example.invalid",
          identityMode: "development",
        }),
      ])
        await problem(
          await send(origin, "/api/identity/v1/first-administrator", {
            method: "POST",
            headers: { ...mutation, "Content-Type": "application/json" },
            body,
          }),
          denied.status,
          denied.code,
        );
    }
  });

  it("refuses ambiguous Origin, Fetch Metadata and antiforgery headers without revoking the current session", async () => {
    const ambiguous: Record<string, string | string[]>[] = [
      { Origin: [publicOrigin, publicOrigin] },
      { "Sec-Fetch-Site": ["same-origin", "cross-site"] },
      { "X-Kofte-CSRF": [csrf, csrf] },
    ];
    for (const origin of origins) {
      for (const headers of ambiguous) {
        const response = await requestWithBody(
          new URL("/api/identity/v1/sign-out", origin),
          "",
          { ...mutation, ...headers },
          "POST",
        );
        expect(response.status).toBe(403);
        expect(response.cacheControl).toBe(identityTransport.cacheControl);
        expect(response.headers.location).toBeUndefined();
        const body: unknown = JSON.parse(response.body);
        expect(body).toMatchObject({
          status: 403,
          code: "identity_forbidden",
        });
      }
      const current = await send(origin, "/api/identity/v1/me", {
        headers: session,
      });
      expect(current.status).toBe(200);
      expect(await current.json()).toEqual(expected);
    }
  });

  it("admits exactly sixteen KiB of combined headers and refuses the next byte before issuing cookies", async () => {
    const boundary = identityNumericBoundaries.find(
      ([name]) => name === "headers_and_cookies_bytes",
    );
    if (!boundary) throw new Error("independent_header_boundary_required");
    for (const [transport, origin] of origins.entries()) {
      const headers = {
        Host: new URL(origin).host,
        "X-Forwarded-Host": "boundary.invalid",
        "X-Forwarded-Port": "443",
        "X-Forwarded-Proto": "https",
        "X-Forwarded-For": "127.0.0.1",
        "X-Identity-Padding": "",
      };
      const fixedBytes = Object.entries(headers).reduce(
        (total, [name, value]) =>
          total + Buffer.byteLength(`${name}: ${value}\r\n`),
        0,
      );
      const lengths: readonly number[] = [boundary[1], boundary[2]];
      for (const length of lengths) {
        const response = await requestWithBody(
          new URL("/api/identity/v1/browser-context", origin),
          "",
          {
            ...headers,
            "X-Identity-Padding": "a".repeat(length - fixedBytes),
          },
        );
        if (response.status !== (length === boundary[1] ? 200 : 400))
          throw new Error(
            `identity_independent_header_${transport}_${length}_${response.status}`,
          );
        expect(response.status).toBe(length === boundary[1] ? 200 : 400);
        expect(response.cacheControl).toBe(identityTransport.cacheControl);
        expect(response.headers.pragma).toBe(identityTransport.pragma);
        expect(response.headers["referrer-policy"]).toBe(
          identityTransport.referrerPolicy,
        );
        expect(response.headers.location).toBeUndefined();
        if (length === boundary[2]) {
          expect(response.headers["set-cookie"]).toBeUndefined();
          expect(JSON.parse(response.body)).toMatchObject({
            status: 400,
            code: "identity_request_invalid",
          });
        } else {
          expect(response.headers["set-cookie"]).toHaveLength(2);
          expect(JSON.parse(response.body)).toMatchObject({
            version: "1",
            identityMode: "oidc",
            csrfToken: expect.any(String),
          });
        }
      }
    }
  });

  it("refuses duplicate and unknown login fields, incorrect media and query input before a challenge", async () => {
    const denied = refusal("unknown_duplicate_json_or_form_field");
    for (const origin of origins)
      for (const [path, contentType, body] of [
        [
          "/auth/login",
          "application/x-www-form-urlencoded",
          "returnPath=%2F&returnPath=%2Fidentity",
        ],
        [
          "/auth/login",
          "application/x-www-form-urlencoded",
          "returnPath=%2Fidentity&userId=caller-selected",
        ],
        [
          "/auth/login",
          "application/json",
          JSON.stringify({ returnPath: "/identity" }),
        ],
        [
          "/auth/login?returnPath=%2Fidentity",
          "application/x-www-form-urlencoded",
          "returnPath=%2Fidentity",
        ],
      ]) {
        const response = await send(origin, path, {
          method: "POST",
          headers: { ...mutation, "Content-Type": contentType },
          body,
        });
        await problem(response, denied.status, denied.code);
        expect(response.headers.getSetCookie()).toEqual([]);
      }
  });

  it("admits sixteen individual authentication cookies but refuses duplicates and the seventeenth", async () => {
    const boundary = identityNumericBoundaries.find(
      ([name]) => name === "auth_cookies",
    );
    if (!boundary) throw new Error("independent_cookie_boundary_required");
    const invalid = refusal("unknown_duplicate_json_or_form_field");
    const counts: readonly number[] = [boundary[1], boundary[2]];
    for (const origin of origins) {
      for (const count of counts) {
        const response = await send(
          origin,
          "/api/identity/v1/browser-context",
          {
            headers: {
              Cookie: Array.from(
                { length: count },
                (_, index) => `__Host-kofte-correlation.${index}=never-issued`,
              ).join("; "),
            },
          },
        );
        if (count === boundary[2]) {
          await problem(response, invalid.status, invalid.code);
          expect(response.headers.getSetCookie()).toEqual([]);
        } else {
          expect(response.status).toBe(200);
          privateResponse(response);
          const cookies = response.headers.getSetCookie();
          expect(cookies.length).toBe(2);
          for (const cookie of cookies) {
            const attributes = cookie
              .split(";")
              .slice(1)
              .map((value) => value.trim().toLowerCase());
            expect(attributes).toEqual(
              expect.arrayContaining([
                "secure",
                "httponly",
                "samesite=lax",
                "path=/",
              ]),
            );
            expect(
              attributes.some((value) => value.startsWith("domain=")),
            ).toBe(false);
          }
        }
      }
      const duplicate = await send(origin, "/api/identity/v1/browser-context", {
        headers: { Cookie: "__Host-kofte-browser=a; __Host-kofte-browser=b" },
      });
      await problem(duplicate, invalid.status, invalid.code);
      expect(duplicate.headers.getSetCookie()).toEqual([]);
    }
  });

  it("admits an eight-KiB valid mutation shape but rejects one byte over before any authority change", async () => {
    const boundary = identityNumericBoundaries.find(
      ([name]) => name === "mutation_body_bytes",
    );
    if (!boundary) throw new Error("independent_body_boundary_required");
    const body = JSON.stringify({ authorization: "A".repeat(43) });
    for (const origin of origins)
      for (const length of [boundary[1], boundary[2]])
        await problem(
          await send(origin, "/api/identity/v1/first-administrator", {
            method: "POST",
            headers: { ...mutation, "Content-Type": "application/json" },
            body: body.padEnd(length, " "),
          }),
          length === boundary[1] ? 409 : 400,
          length === boundary[1]
            ? "identity_enrollment_unavailable"
            : "identity_request_invalid",
        );
  });

  it("rejects every independently frozen unsafe return without following any redirect", async () => {
    const denied = refusal("invalid_return_path");
    for (const origin of origins)
      for (const returnPath of rejectedIdentityReturns)
        await problem(
          await send(origin, "/auth/login", {
            method: "POST",
            headers: {
              ...mutation,
              "Content-Type": "application/x-www-form-urlencoded",
            },
            body: new URLSearchParams({ returnPath }),
          }),
          denied.status,
          denied.code,
        );
  });

  it("admits only the two independent local returns into a deliberate configured challenge", async () => {
    for (const origin of origins)
      for (const returnPath of allowedIdentityReturns) {
        const response = await send(origin, "/auth/login", {
          method: "POST",
          headers: {
            ...mutation,
            "Content-Type": "application/x-www-form-urlencoded",
          },
          body: new URLSearchParams({ returnPath }),
        });
        expect(response.status).toBe(303);
        privateResponse(response);
        const location = response.headers.get("location");
        if (!location) throw new Error("independent_oidc_challenge_required");
        expect(new URL(location).protocol).toBe("https:");
        expect(new URL(location).searchParams.get("response_type")).toBe(
          "code",
        );
        expect(
          new URL(location).searchParams.get("code_challenge_method"),
        ).toBe("S256");
        expect(response.headers.getSetCookie().length).toBeGreaterThan(0);
      }
  });
}
