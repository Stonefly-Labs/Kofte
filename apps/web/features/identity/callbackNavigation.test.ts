// @vitest-environment node
import { afterEach, expect, it, vi } from "vitest";
import { GET } from "../../app/auth/oidc/callback/route";
import { identityConfiguration } from "./identityConfiguration";

vi.mock("./identityConfiguration", () => ({
  identityConfiguration: vi.fn(async () => ({
    backendOrigin: "http://127.0.0.1:5080",
    publicOrigin: "https://kofte.example",
    mode: "oidc",
    authorizationEndpoint: "https://issuer.example/authorize",
  })),
}));

afterEach(() => {
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

it.each([
  [401, "identity_unauthenticated", "Authentication required", "cancelled"],
  [503, "identity_unavailable", "Identity unavailable", "unavailable"],
] as const)(
  "returns an actual browser navigation from callback %s to a private actionable page",
  async (status, code, title, outcome) => {
    vi.spyOn(console, "error").mockImplementation(() => {});
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        Response.json(
          {
            type: `urn:kofte:problem:${code}`,
            status,
            code,
            title,
            correlationId: "a".repeat(32),
            detail: "private-provider-detail",
          },
          {
            status,
            headers: {
              "content-type": "application/problem+json",
              "cache-control": "no-store",
              pragma: "no-cache",
              "referrer-policy": "no-referrer",
            },
          },
        ),
      ),
    );

    const response = await GET(
      new Request(
        "https://kofte.example/auth/oidc/callback?state=private-state&error=access_denied&error_description=private-provider-detail",
        {
          headers: {
            accept: "text/html,application/xhtml+xml",
            "sec-fetch-mode": "navigate",
            "sec-fetch-dest": "document",
          },
        },
      ),
    );
    expect(response.status).toBe(303);
    expect(response.headers.get("location")).toBe(
      `/identity?outcome=${outcome}&correlationId=${"a".repeat(32)}`,
    );
    expect(response.headers.get("cache-control")).toBe("no-store");
    expect(response.headers.get("referrer-policy")).toBe("no-referrer");
    expect(await response.text()).toBe("");
    expect([...response.headers].flat().join(" ")).not.toContain("private-");
    expect(identityConfiguration).toHaveBeenCalled();
  },
);

it("keeps a direct callback refusal as a bounded API error instead of redirecting it", async () => {
  vi.spyOn(console, "error").mockImplementation(() => {});
  const problem = {
    type: "urn:kofte:problem:identity_unauthenticated",
    title: "Authentication required",
    status: 401,
    code: "identity_unauthenticated",
    correlationId: "b".repeat(32),
  };
  vi.stubGlobal(
    "fetch",
    vi.fn().mockResolvedValue(
      Response.json(problem, {
        status: 401,
        headers: {
          "content-type": "application/problem+json",
          "cache-control": "no-store",
          pragma: "no-cache",
          "referrer-policy": "no-referrer",
        },
      }),
    ),
  );
  const response = await GET(
    new Request(
      "https://kofte.example/auth/oidc/callback?state=private-state&error=access_denied",
      { headers: { accept: "application/json" } },
    ),
  );
  expect(response.status).toBe(401);
  expect(response.headers.has("location")).toBe(false);
  expect(await response.json()).toEqual(problem);
});
