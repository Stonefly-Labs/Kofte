// @vitest-environment node
import { afterEach, describe, expect, it, vi } from "vitest";
import { GET, HEAD, POST } from "./route";
import { backendReadinessUrl } from "../../../../features/bootstrap/backendConfiguration";

const ready = {
  version: "1",
  state: "ready",
  component: "storage",
  correlationId: "0123456789abcdef0123456789abcdef",
};

afterEach(() => {
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

describe("fixed server readiness adapter", () => {
  it.each(["127.0.0.1", "0.0.0.0"])(
    "derives a matching loopback destination from host %s",
    (hostname) => {
      expect(
        backendReadinessUrl({
          KOFTE_HOST_URL: `http://${hostname}:5081`,
          KOFTE_ALLOW_UNSAFE_EXPOSURE: "true",
        }).href,
      ).toBe("http://127.0.0.1:5081/health/ready");
    },
  );

  it("preserves a valid response without forwarding caller credentials", async () => {
    const fetch = vi.fn().mockResolvedValue(Response.json(ready));
    vi.stubGlobal("fetch", fetch);
    const response = await GET(
      new Request("http://localhost/backend/health/ready", {
        headers: {
          Authorization: "credential-canary",
          Cookie: "credential-canary",
        },
      }),
    );
    expect(response.status).toBe(200);
    expect(response.headers.get("cache-control")).toBe("no-store");
    expect(await response.json()).toEqual(ready);
    expect(fetch.mock.calls[0][0].pathname).toBe("/health/ready");
    expect(fetch.mock.calls[0][1].headers).toEqual({
      Accept: "application/json",
    });
    expect(fetch.mock.calls[0][1].redirect).toBe("error");
  });

  it.each([
    Response.json({ ...ready, credential: "credential-canary" }),
    Response.json(ready, { status: 503 }),
    new Response("credential-canary", { status: 500 }),
    new Response("credential-canary", {
      headers: { "content-type": "application/json" },
    }),
  ])(
    "rejects malformed, mismatched and unexpected upstream data",
    async (upstream) => {
      vi.spyOn(console, "error").mockImplementation(() => {});
      vi.stubGlobal("fetch", vi.fn().mockResolvedValue(upstream));
      const response = await GET(
        new Request("http://localhost/backend/health/ready"),
      );
      expect(response.status).toBe(503);
      const body = await response.text();
      expect(body).not.toContain("credential-canary");
      expect(JSON.parse(body)).toMatchObject({
        state: "unavailable",
        component: "host",
      });
      expect(JSON.parse(body).correlationId).toMatch(/^[0-9a-f]{32}$/);
    },
  );

  it("bounds stalled body processing and cancels its upstream signal", async () => {
    vi.spyOn(console, "error").mockImplementation(() => {});
    const timeout = new AbortController();
    vi.spyOn(AbortSignal, "timeout").mockReturnValue(timeout.signal);
    const upstream = Response.json(ready);
    vi.spyOn(upstream, "json").mockReturnValue(new Promise(() => {}));
    const fetch = vi.fn().mockResolvedValue(upstream);
    vi.stubGlobal("fetch", fetch);
    const pending = GET(new Request("http://localhost/backend/health/ready"));
    await Promise.resolve();
    timeout.abort();
    expect((await pending).status).toBe(503);
    expect(fetch.mock.calls[0][1].signal.aborted).toBe(true);
  });

  it("rejects query/body/method misuse without contacting the host", async () => {
    const fetch = vi.fn();
    vi.stubGlobal("fetch", fetch);
    expect(
      (
        await GET(
          new Request("http://localhost/backend/health/ready?secret=canary"),
        )
      ).status,
    ).toBe(400);
    expect(
      (
        await GET(
          new Request("http://localhost/backend/health/ready", {
            headers: { "content-length": "5" },
          }),
        )
      ).status,
    ).toBe(400);
    expect(HEAD().status).toBe(405);
    expect(POST().status).toBe(405);
    expect(fetch).not.toHaveBeenCalled();
  });

  it.each([
    "http://example.com",
    "http://user:canary@127.0.0.1",
    "http://127.0.0.1/../",
    "https://127.0.0.1",
    "http://0.0.0.0",
    "http://127.0.0.1:0",
    "http://@127.0.0.1",
    "http://127.0.0.1\n",
    "http://127.0.0.1:5081",
    "http://127.0.0.2:5080",
  ])("refuses an unsafe configured origin", (origin) =>
    expect(() =>
      backendReadinessUrl({ KOFTE_BACKEND_ORIGIN: origin }),
    ).toThrow(),
  );
});
