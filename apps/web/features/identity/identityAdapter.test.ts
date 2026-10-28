// @vitest-environment node
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { forwardIdentity } from "./identityAdapter";
import { identityConfiguration } from "./identityConfiguration";
import * as contextRoute from "../../app/api/identity/v1/browser-context/route";
import * as meRoute from "../../app/api/identity/v1/me/route";
import * as loginRoute from "../../app/auth/login/route";
import * as callbackRoute from "../../app/auth/oidc/callback/route";
import * as enrollmentRoute from "../../app/api/identity/v1/first-administrator/route";
import * as signOutRoute from "../../app/api/identity/v1/sign-out/route";

vi.mock("./identityConfiguration", () => ({
  identityConfiguration: vi.fn(async () => ({
    backendOrigin: "http://127.0.0.1:5080",
    publicOrigin: "https://kofte.example",
    mode: "oidc",
    authorizationEndpoint: "https://issuer.example/authorize",
  })),
}));

const current = {
  version: "1",
  userId: "00000000-0000-4000-8000-000000000001",
  displayName: null,
  identityMode: "oidc",
  isInitialAdministrator: false,
};
const csrf = {
  version: "1",
  identityMode: "oidc",
  csrfToken: "request-token",
};
const privateHeaders = {
  "cache-control": "no-store",
  pragma: "no-cache",
  "referrer-policy": "no-referrer",
};
const cookie =
  "__Host-kofte-browser=browser-value; path=/; secure; samesite=lax; httponly";

function request(path: string, method = "GET", headers: HeadersInit = {}) {
  return new Request("https://kofte.example" + path, {
    method,
    headers:
      method === "POST"
        ? {
            "content-type": "application/x-www-form-urlencoded",
            origin: "https://kofte.example",
            "x-kofte-csrf": "request-token",
            ...headers,
          }
        : headers,
    ...(method === "POST" ? { body: "returnPath=%2Fidentity" } : {}),
  });
}

function upstream(body: unknown = current) {
  return Response.json(body, { headers: privateHeaders });
}

beforeEach(() => {
  vi.spyOn(console, "error").mockImplementation(() => {});
});

afterEach(() => {
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

function enrollmentRequest(body: unknown = { authorization: "A".repeat(43) }) {
  return new Request(
    "https://kofte.example/api/identity/v1/first-administrator",
    {
      method: "POST",
      headers: {
        "content-type": "application/json",
        origin: "https://kofte.example",
        "x-kofte-csrf": "signed-in-request-token",
        cookie: "__Host-kofte-session=session-value",
      },
      body: JSON.stringify(body),
    },
  );
}

function signOutRequest(body?: string) {
  return new Request("https://kofte.example/api/identity/v1/sign-out", {
    method: "POST",
    headers: {
      origin: "https://kofte.example",
      "x-kofte-csrf": "signed-in-request-token",
      cookie: "__Host-kofte-session=session-value",
    },
    body,
  });
}

it("preserves confirmed empty sign-out and its individually expired cookie without a second mutation", async () => {
  const headers = new Headers(privateHeaders);
  headers.append(
    "Set-Cookie",
    "__Host-kofte-session=; path=/; secure; httponly; samesite=lax; expires=Thu, 01 Jan 1970 00:00:00 GMT",
  );
  const fetch = vi
    .fn()
    .mockResolvedValue(new Response(null, { status: 204, headers }));
  vi.stubGlobal("fetch", fetch);
  const response = await signOutRoute.POST(signOutRequest());
  expect(response.status).toBe(204);
  expect(await response.text()).toBe("");
  expect(response.headers.get("content-type")).toBeNull();
  expect(response.headers.getSetCookie()).toEqual(headers.getSetCookie());
  expect(fetch).toHaveBeenCalledTimes(1);
  const [url, options] = fetch.mock.calls[0];
  expect(url.href).toBe("http://127.0.0.1:5080/api/identity/v1/sign-out");
  expect(options.method).toBe("POST");
  expect(options.body).toBeUndefined();
  expect(options.redirect).toBe("manual");
  expect(new Headers(options.headers).get("x-kofte-csrf")).toBe(
    "signed-in-request-token",
  );
});

it("rejects a caller-selected sign-out target before dispatch", async () => {
  const fetch = vi.fn();
  vi.stubGlobal("fetch", fetch);
  const response = await forwardIdentity(
    signOutRequest('{"session":"other"}'),
    "signOut",
  );
  expect(response.status).toBe(400);
  expect(fetch).not.toHaveBeenCalled();
});

it("never turns a lost sign-out acknowledgement into confirmed revocation or retries it", async () => {
  const fetch = vi
    .fn()
    .mockRejectedValue(new TypeError("private-sign-out-canary"));
  vi.stubGlobal("fetch", fetch);
  const response = await forwardIdentity(signOutRequest(), "signOut");
  expect(response.status).toBe(503);
  const problem = await response.json();
  expect(problem.code).toBe("identity_outcome_uncertain");
  expect(JSON.stringify(problem)).not.toContain("private-sign-out-canary");
  expect(fetch).toHaveBeenCalledTimes(1);
});

it.each([undefined, "null", "https://foreign.example.invalid"])(
  "requires exact Origin for empty sign-out before any dispatch",
  async (origin) => {
    const fetch = vi.fn();
    vi.stubGlobal("fetch", fetch);
    const request = signOutRequest();
    request.headers.delete("origin");
    if (origin !== undefined) request.headers.set("origin", origin);
    expect((await signOutRoute.POST(request)).status).toBe(403);
    expect(fetch).not.toHaveBeenCalled();
  },
);

it.each(["GET", "HEAD", "OPTIONS", "PUT", "PATCH", "DELETE"] as const)(
  "keeps the actual sign-out %s export method-bound",
  async (method) => {
    const fetch = vi.fn();
    vi.stubGlobal("fetch", fetch);
    const response = await signOutRoute[method](
      new Request("https://kofte.example/api/identity/v1/sign-out", { method }),
    );
    expect(response.status).toBe(405);
    expect(response.headers.get("allow")).toBe("POST");
    expect(fetch).not.toHaveBeenCalled();
  },
);

it("delivers only the protected enrollment exchange and safe committed receipt", async () => {
  const receipt = {
    version: "1",
    status: "enrolled",
    correlationId: "a".repeat(32),
  };
  const fetch = vi
    .fn()
    .mockResolvedValue(upstream({ ...receipt, futureField: true }));
  vi.stubGlobal("fetch", fetch);
  const response = await enrollmentRoute.POST(enrollmentRequest());
  expect(response.status).toBe(200);
  expect(await response.json()).toEqual(receipt);
  const [url, options] = fetch.mock.calls[0];
  expect(url.href).toBe(
    "http://127.0.0.1:5080/api/identity/v1/first-administrator",
  );
  expect(options.method).toBe("POST");
  expect(options.body).toBe(JSON.stringify({ authorization: "A".repeat(43) }));
  expect(new Headers(options.headers).get("x-kofte-csrf")).toBe(
    "signed-in-request-token",
  );
  expect(options.redirect).toBe("manual");
  expect(options.cache).toBe("no-store");
});

it.each([
  { authorization: "A".repeat(43), nominee: "caller-chosen" },
  { authorization: "A".repeat(42) },
  { authorization: "A".repeat(42) + "B" },
])(
  "rejects caller authority or noncanonical enrollment before dispatch",
  async (body) => {
    const fetch = vi.fn();
    vi.stubGlobal("fetch", fetch);
    const response = await enrollmentRoute.POST(enrollmentRequest(body));
    expect(response.status).toBe(400);
    expect(fetch).not.toHaveBeenCalled();
  },
);

it("does not turn a lost enrollment reply into success or retry the mutation", async () => {
  const fetch = vi
    .fn()
    .mockRejectedValue(new TypeError("private synthetic exchange"));
  vi.stubGlobal("fetch", fetch);
  const response = await enrollmentRoute.POST(enrollmentRequest());
  expect(response.status).toBe(503);
  const problem = await response.json();
  expect(problem.code).toBe("identity_outcome_uncertain");
  expect(JSON.stringify(problem)).not.toContain("private synthetic exchange");
  expect(fetch).toHaveBeenCalledTimes(1);
});

it.each([
  ["context", "/api/identity/v1/browser-context", "GET", csrf],
  ["me", "/api/identity/v1/me", "GET", current],
] as const)(
  "forwards only fixed %s input and admitted cookies, never caller destinations or identity headers",
  async (kind, path, method, body) => {
    const fetch = vi.fn().mockResolvedValue(upstream(body));
    vi.stubGlobal("fetch", fetch);
    const response = await forwardIdentity(
      request(path, method, {
        cookie:
          "unrelated=private; __Host-kofte-session=session-value; __Host-kofte-browser=browser-value",
        host: "attacker.example",
        "x-forwarded-host": "attacker.example",
        "x-forwarded-for": "1.2.3.4",
        "x-user-id": "caller-controlled",
      }),
      kind,
    );
    expect(response.status).toBe(200);
    expect(await response.json()).toEqual(body);
    const [url, options] = fetch.mock.calls[0];
    expect(url.href).toBe("http://127.0.0.1:5080" + path);
    expect(options.method).toBe(method);
    expect(options.redirect).toBe("manual");
    expect(options.cache).toBe("no-store");
    expect(Object.fromEntries(new Headers(options.headers))).toEqual({
      accept: "application/json",
      cookie:
        "__Host-kofte-session=session-value; __Host-kofte-browser=browser-value",
    });
    expect(response.headers.get("cache-control")).toBe("no-store");
    expect(response.headers.get("pragma")).toBe("no-cache");
    expect(response.headers.get("referrer-policy")).toBe("no-referrer");
  },
);

it.each(["", "Bearer caller-controlled"])(
  "rejects unsupported credentials locally, even alongside a session",
  async (authorization) => {
    const fetch = vi.fn();
    vi.stubGlobal("fetch", fetch);
    const response = await forwardIdentity(
      request("/api/identity/v1/me", "GET", {
        authorization,
        cookie: "__Host-kofte-session=session-value",
      }),
      "me",
    );
    expect(response.status).toBe(401);
    expect((await response.json()).code).toBe("identity_unauthenticated");
    expect(fetch).not.toHaveBeenCalled();
  },
);

it.each([
  ["/api/identity/v1/me?url=https://attacker.example", "GET", {}, 400],
  ["/api/identity/v1/me", "HEAD", {}, 405],
  ["/api/identity/v1/me", "OPTIONS", {}, 405],
  ["/api/identity/v1/me", "GET", { "content-length": "1" }, 400],
  [
    "/api/identity/v1/me",
    "GET",
    { cookie: "__Host-kofte-session=a; __Host-kofte-session=b" },
    400,
  ],
  [
    "/api/identity/v1/me",
    "GET",
    {
      cookie: Array.from(
        { length: 17 },
        (_, index) => `__Host-kofte-correlation.${index}=value`,
      ).join("; "),
    },
    400,
  ],
] as const)(
  "rejects request misuse before egress",
  async (path, method, headers, status) => {
    const fetch = vi.fn();
    vi.stubGlobal("fetch", fetch);
    const response = await forwardIdentity(
      request(path, method, headers),
      "me",
    );
    expect(response.status).toBe(status);
    if (status === 405) expect(response.headers.get("allow")).toBe("GET");
    expect(fetch).not.toHaveBeenCalled();
  },
);

it("preserves individual approved Set-Cookie values, including an Expires comma", async () => {
  const headers = new Headers(privateHeaders);
  headers.append("set-cookie", cookie);
  headers.append(
    "set-cookie",
    "__Host-kofte-antiforgery=antiforgery-value; path=/; secure; httponly; samesite=lax; expires=Wed, 01 Jan 2031 00:00:00 GMT",
  );
  vi.stubGlobal(
    "fetch",
    vi.fn().mockResolvedValue(Response.json(csrf, { headers })),
  );
  const response = await forwardIdentity(
    request("/api/identity/v1/browser-context"),
    "context",
  );
  expect(response.status).toBe(200);
  expect(response.headers.getSetCookie()).toEqual(headers.getSetCookie());
});

it.each([
  [16, 200],
  [17, 400],
])(
  "enforces the admitted auth-cookie count at its exact boundary",
  async (count, status) => {
    const fetch = vi.fn().mockResolvedValue(upstream());
    vi.stubGlobal("fetch", fetch);
    const response = await forwardIdentity(
      request("/api/identity/v1/me", "GET", {
        cookie: Array.from(
          { length: count },
          (_, index) => `__Host-kofte-correlation.${index}=value`,
        ).join("; "),
      }),
      "me",
    );
    expect(response.status).toBe(status);
    expect(fetch).toHaveBeenCalledTimes(status === 200 ? 1 : 0);
  },
);

it("keeps development cookies separately named and strips production continuity", async () => {
  vi.mocked(identityConfiguration).mockResolvedValueOnce({
    backendOrigin: "http://127.0.0.1:5080",
    publicOrigin: "http://127.0.0.1:3000",
    mode: "development",
  });
  const setCookie =
    "kofte-dev-browser=private-value; path=/; httponly; samesite=lax";
  const fetch = vi.fn().mockResolvedValue(
    Response.json(
      {
        ...csrf,
        identityMode: "development",
      },
      {
        headers: { ...privateHeaders, "set-cookie": setCookie },
      },
    ),
  );
  vi.stubGlobal("fetch", fetch);
  const response = await forwardIdentity(
    request("/api/identity/v1/browser-context", "GET", {
      cookie:
        "kofte-dev-antiforgery=development-value; __Host-kofte-session=production-value",
    }),
    "context",
  );
  expect(response.status).toBe(200);
  expect(new Headers(fetch.mock.calls[0][1].headers).get("cookie")).toBe(
    "kofte-dev-antiforgery=development-value",
  );
  expect(response.headers.getSetCookie()).toEqual([setCookie]);
});

it.each([
  "foreign=value; path=/; secure; httponly; samesite=lax",
  "__Host-kofte-session=value; path=/; httponly; samesite=lax",
  "__Host-kofte-session=value; path=/; secure; samesite=lax",
  "__Host-kofte-session=value; path=/; secure; httponly; samesite=lax; domain=kofte.example",
])("refuses unsafe upstream cookies without reflecting them", async (value) => {
  vi.stubGlobal(
    "fetch",
    vi.fn().mockResolvedValue(
      Response.json(current, {
        headers: { ...privateHeaders, "set-cookie": value },
      }),
    ),
  );
  const response = await forwardIdentity(request("/api/identity/v1/me"), "me");
  expect(response.status).toBe(503);
  expect(response.headers.getSetCookie()).toEqual([]);
});

it.each([
  [
    "login",
    "/auth/login",
    "POST",
    "https://issuer.example/authorize?state=opaque",
  ],
  [
    "callback",
    "/auth/oidc/callback?state=opaque&code=opaque",
    "GET",
    "/identity",
  ],
] as const)(
  "admits only the fixed %s redirect and never follows it",
  async (kind, path, method, location) => {
    const fetch = vi.fn().mockResolvedValue(
      new Response(null, {
        status: 303,
        headers: {
          ...privateHeaders,
          location,
          "content-type": "text/plain; charset=utf-8",
          "set-cookie": cookie,
        },
      }),
    );
    vi.stubGlobal("fetch", fetch);
    const response = await forwardIdentity(request(path, method), kind);
    expect(response.status).toBe(303);
    expect(response.headers.get("location")).toBe(location);
    expect(await response.text()).toBe("");
    expect(response.headers.getSetCookie()).toEqual([cookie]);
    expect(fetch).toHaveBeenCalledTimes(1);
    expect(fetch.mock.calls[0][1].redirect).toBe("manual");
  },
);

it.each([
  "https://attacker.example/authorize",
  "https://issuer.example/other",
  "https://issuer.example/authorize#fragment",
])("refuses a challenge to an unconfigured endpoint", async (location) => {
  vi.stubGlobal(
    "fetch",
    vi.fn().mockResolvedValue(
      new Response(null, {
        status: 303,
        headers: {
          ...privateHeaders,
          location,
          "content-type": "text/plain",
        },
      }),
    ),
  );
  const response = await forwardIdentity(
    request("/auth/login", "POST"),
    "login",
  );
  expect(response.status).toBe(503);
  expect(response.headers.get("location")).toBeNull();
  expect((await response.json()).code).toBe("identity_outcome_uncertain");
});

it.each(["//attacker.example", "/%69dentity", "/identity?code=private"])(
  "refuses unsafe callback destinations",
  async (location) => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(null, {
          status: 303,
          headers: {
            ...privateHeaders,
            location,
            "content-type": "text/plain",
          },
        }),
      ),
    );
    const response = await forwardIdentity(
      request("/auth/oidc/callback?state=opaque&code=opaque"),
      "callback",
    );
    expect(response.status).toBe(503);
    expect(response.headers.get("location")).toBeNull();
  },
);

it.each([
  new Response("private-upstream-error", {
    headers: { ...privateHeaders, "content-type": "text/html" },
  }),
  upstream({ ...current, userId: "not-an-id" }),
  new Response("x".repeat(8193), {
    headers: { ...privateHeaders, "content-type": "application/json" },
  }),
])("bounds and validates upstream bodies without reflection", async (value) => {
  vi.stubGlobal("fetch", vi.fn().mockResolvedValue(value));
  const response = await forwardIdentity(request("/api/identity/v1/me"), "me");
  expect(response.status).toBe(503);
  const body = await response.text();
  expect(body).not.toContain("private-upstream-error");
  expect(JSON.parse(body).code).toBe("identity_unavailable");
});

it("bounds a stalled body and reports a lost mutation reply as uncertain, with no retry", async () => {
  const timeout = new AbortController();
  vi.spyOn(AbortSignal, "timeout").mockReturnValue(timeout.signal);
  const fetch = vi.fn().mockResolvedValue(
    new Response(new ReadableStream(), {
      headers: { ...privateHeaders, "content-type": "application/json" },
    }),
  );
  vi.stubGlobal("fetch", fetch);
  const pending = forwardIdentity(request("/auth/login", "POST"), "login");
  await vi.waitFor(() => expect(fetch).toHaveBeenCalledOnce());
  timeout.abort();
  const response = await pending;
  expect(response.status).toBe(503);
  expect((await response.json()).code).toBe("identity_outcome_uncertain");
  expect(fetch).toHaveBeenCalledOnce();
  expect(fetch.mock.calls[0][1].signal.aborted).toBe(true);
  expect(AbortSignal.timeout).toHaveBeenCalledWith(4500);
});

it.each([
  [8192, 303],
  [8193, 400],
])(
  "bounds streamed mutation input at exactly eight KiB",
  async (size, status) => {
    const fetch = vi.fn().mockResolvedValue(
      new Response(null, {
        status: 303,
        headers: {
          ...privateHeaders,
          location: "https://issuer.example/authorize",
          "content-type": "text/plain",
        },
      }),
    );
    vi.stubGlobal("fetch", fetch);
    const prefix = "__RequestVerificationToken=";
    const incoming = new Request("https://kofte.example/auth/login", {
      method: "POST",
      headers: {
        origin: "https://kofte.example",
        "content-type": "application/x-www-form-urlencoded",
      },
      body: prefix + "a".repeat(size - prefix.length),
    });
    const response = await forwardIdentity(incoming, "login");
    expect(response.status).toBe(status);
    expect(fetch).toHaveBeenCalledTimes(status === 303 ? 1 : 0);
  },
);

it.each([
  [16384, 200],
  [16385, 400],
])("bounds total caller headers at sixteen KiB", async (size, status) => {
  const fetch = vi.fn().mockResolvedValue(upstream());
  vi.stubGlobal("fetch", fetch);
  const response = await forwardIdentity(
    request("/api/identity/v1/me", "GET", {
      "x-padding": "a".repeat(size - "x-padding".length - 4),
    }),
    "me",
  );
  expect(response.status).toBe(status);
  expect(fetch).toHaveBeenCalledTimes(status === 200 ? 1 : 0);
});

it("keeps safe display encoding and drops unknown upstream response fields", async () => {
  vi.stubGlobal(
    "fetch",
    vi.fn().mockResolvedValue(
      upstream({
        ...current,
        displayName: "<display>",
        unexpected: "private-upstream-value",
      }),
    ),
  );
  const response = await forwardIdentity(request("/api/identity/v1/me"), "me");
  expect(response.status).toBe(200);
  const text = await response.text();
  expect(text).not.toContain("<display>");
  expect(text).not.toContain("private-upstream-value");
  expect(JSON.parse(text).displayName).toBe("<display>");
});

it.each(["POST", "HEAD"])(
  "keeps OIDC-only routes absent in development, even with method or credential confusion",
  async (method) => {
    vi.mocked(identityConfiguration).mockResolvedValueOnce({
      backendOrigin: "http://127.0.0.1:5080",
      publicOrigin: "http://127.0.0.1:3000",
      mode: "development",
    });
    const fetch = vi.fn();
    vi.stubGlobal("fetch", fetch);
    const response = await forwardIdentity(
      request("/auth/login", method, {
        authorization: "unsupported",
      }),
      "login",
    );
    expect(response.status).toBe(404);
    expect(fetch).not.toHaveBeenCalled();
  },
);

it.each([
  [contextRoute.GET, "/api/identity/v1/browser-context", "GET", csrf],
  [meRoute.GET, "/api/identity/v1/me", "GET", current],
] as const)(
  "wires each actual GET export to its fixed upstream contract",
  async (handler, path, method, value) => {
    const fetch = vi.fn().mockResolvedValue(upstream(value));
    vi.stubGlobal("fetch", fetch);
    expect((await handler(request(path, method))).status).toBe(200);
    expect(fetch.mock.calls[0][0].href).toBe("http://127.0.0.1:5080" + path);
  },
);

it.each([
  [loginRoute.POST, "/auth/login", "POST", "https://issuer.example/authorize"],
  [callbackRoute.GET, "/auth/oidc/callback?state=opaque", "GET", "/identity"],
] as const)(
  "wires actual protocol exports without following their redirects",
  async (handler, path, method, location) => {
    const fetch = vi.fn().mockResolvedValue(
      new Response(null, {
        status: 303,
        headers: { ...privateHeaders, location, "content-type": "text/plain" },
      }),
    );
    vi.stubGlobal("fetch", fetch);
    const response = await handler(request(path, method));
    expect(response.status).toBe(303);
    expect(response.headers.get("location")).toBe(location);
    expect(fetch.mock.calls[0][0].pathname).toBe(path.split("?")[0]);
    expect(fetch).toHaveBeenCalledOnce();
  },
);
