import { beforeEach, expect, it, vi } from "vitest";
import { cookies, headers } from "next/headers";
import { redirect } from "next/navigation";
import { forwardIdentity } from "./identityAdapter";
import { startLogin } from "./startLoginAction";

vi.mock("next/headers", () => ({ cookies: vi.fn(), headers: vi.fn() }));
vi.mock("next/navigation", () => ({ redirect: vi.fn() }));
vi.mock("./identityAdapter", () => ({ forwardIdentity: vi.fn() }));

let incoming: Headers;
const set = vi.fn();
const input = { returnPath: "/identity", csrfToken: "fresh-token" };

beforeEach(() => {
  vi.clearAllMocks();
  incoming = new Headers({
    Origin: "https://kofte.example",
    Cookie: "__Host-kofte-antiforgery=private-pair",
    "Sec-Fetch-Site": "same-origin",
  });
  vi.mocked(headers).mockImplementation(async () => incoming);
  vi.mocked(cookies).mockResolvedValue({
    size: 0,
    get: vi.fn(),
    getAll: () => [],
    has: () => false,
    toString: () => "",
    [Symbol.iterator]: () =>
      new Map<string, { name: string; value: string }>().entries(),
    set,
    delete: vi.fn(),
  });
  vi.mocked(redirect).mockImplementation(() => {
    throw new Error("framework_redirect");
  });
  vi.mocked(forwardIdentity).mockResolvedValue(
    new Response(null, {
      status: 303,
      headers: { Location: "https://issuer.example/authorize" },
    }),
  );
});

it("rejects malformed, oversized and caller-selected inputs before dispatch", async () => {
  for (const value of [
    null,
    [],
    { ...input, nominee: "never-admitted" },
    { ...input, returnPath: "//foreign.invalid" },
    { ...input, csrfToken: "a".repeat(2049) },
    { returnPath: "/identity" },
  ]) {
    expect(await startLogin(value)).toEqual({ kind: "failed" });
  }
  expect(forwardIdentity).not.toHaveBeenCalled();
  expect(headers).not.toHaveBeenCalled();
});

it("never substitutes a trusted origin for absent or opaque browser origin", async () => {
  for (const value of [null, "null", "not-an-origin"]) {
    if (value === null) incoming.delete("origin");
    else incoming.set("origin", value);
    expect(await startLogin(input)).toEqual({ kind: "failed" });
  }
  expect(forwardIdentity).not.toHaveBeenCalled();
  expect(redirect).not.toHaveBeenCalled();
});

it("uses the same bounded adapter with the actual credential and origin evidence", async () => {
  incoming.set("origin", "https://foreign.invalid");
  incoming.set("authorization", "Bearer unsupported");
  incoming.set("sec-fetch-site", "cross-site");
  incoming.set("x-forwarded-host", "must-not-select-backend.invalid");
  vi.mocked(forwardIdentity).mockResolvedValue(
    Response.json(
      { correlationId: "a".repeat(32) },
      { status: 403, headers: { "Content-Type": "application/problem+json" } },
    ),
  );
  expect(await startLogin({ ...input, returnPath: "/" })).toEqual({
    kind: "failed",
    correlationId: "a".repeat(32),
  });
  expect(forwardIdentity).toHaveBeenCalledTimes(1);
  const [request, route] = vi.mocked(forwardIdentity).mock.calls[0];
  expect(route).toBe("login");
  expect(request.method).toBe("POST");
  expect(new URL(request.url).pathname).toBe("/auth/login");
  expect(Object.fromEntries(request.headers)).toEqual({
    authorization: "Bearer unsupported",
    "content-type": "application/x-www-form-urlencoded",
    cookie: "__Host-kofte-antiforgery=private-pair",
    origin: "https://foreign.invalid",
    "sec-fetch-site": "cross-site",
  });
  expect([...new URLSearchParams(await request.text())]).toEqual([
    ["returnPath", "/"],
    ["__RequestVerificationToken", "fresh-token"],
  ]);
  expect(set).not.toHaveBeenCalled();
  expect(redirect).not.toHaveBeenCalled();
});

it("publishes separate protected protocol cookies before the framework redirect", async () => {
  const outgoing = new Headers({
    Location: "https://issuer.example/authorize",
  });
  outgoing.append(
    "Set-Cookie",
    "__Host-kofte-correlation.a=one; Path=/; Secure; HttpOnly; SameSite=Lax",
  );
  outgoing.append(
    "Set-Cookie",
    "__Host-kofte-nonce.b=two; Path=/; Secure; HttpOnly; SameSite=Lax",
  );
  vi.mocked(forwardIdentity).mockResolvedValue(
    new Response(null, { status: 303, headers: outgoing }),
  );
  await expect(startLogin(input)).rejects.toThrow("framework_redirect");
  expect(set.mock.calls.map(([cookie]) => cookie)).toEqual([
    {
      name: "__Host-kofte-correlation.a",
      value: "one",
      path: "/",
      secure: true,
      httpOnly: true,
      sameSite: "lax",
    },
    {
      name: "__Host-kofte-nonce.b",
      value: "two",
      path: "/",
      secure: true,
      httpOnly: true,
      sameSite: "lax",
    },
  ]);
  expect(redirect).toHaveBeenCalledWith("https://issuer.example/authorize");
  expect(set.mock.invocationCallOrder[1]).toBeLessThan(
    vi.mocked(redirect).mock.invocationCallOrder[0],
  );
});

it("returns only a safe uncertain outcome without redirect or automatic retry", async () => {
  vi.mocked(forwardIdentity).mockResolvedValue(
    Response.json(
      { correlationId: "private-canary", detail: "must-not-reflect" },
      { status: 503, headers: { "Content-Type": "application/problem+json" } },
    ),
  );
  expect(await startLogin(input)).toEqual({ kind: "uncertain" });
  expect(forwardIdentity).toHaveBeenCalledTimes(1);
  expect(redirect).not.toHaveBeenCalled();
});
