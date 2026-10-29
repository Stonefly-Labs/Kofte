import assert from "node:assert/strict";
import { EventEmitter } from "node:events";
import test from "node:test";
import type { BrowserContext, Page, Response } from "@playwright/test";
import {
  IdentityPrivacyCapture,
  type IdentityCapturePhase,
} from "./identityPrivacyCapture.ts";

function response(overrides: Partial<Response>): Response {
  return {
    request: () => ({
      method: () => "GET",
      url: () => "https://127.0.0.1:12345/api/identity/v1/me",
      resourceType: () => "fetch",
      headers: () => ({}),
      failure: () => ({ errorText: "net::ERR_ABORTED" }),
    }),
    url: () => "https://127.0.0.1:12345/api/identity/v1/me",
    status: () => 401,
    allHeaders: async () => ({
      "cache-control": "no-store",
      "referrer-policy": "no-referrer",
    }),
    finished: async () => null,
    text: async () => '{"code":"identity_unauthenticated"}',
    ...overrides,
  } as unknown as Response;
}

test("journey failure remains the cause when strict capture cleanup also fails", async () => {
  const phases: IdentityCapturePhase[] = [];
  const page = new EventEmitter();
  const capture = new IdentityPrivacyCapture(
    page as unknown as Page,
    [],
    "development",
    (phase) => phases.push(phase),
  );
  const journeyFailure = new Error("primary-journey-failure");
  await assert.rejects(
    () =>
      capture.run(async () => {
        page.emit(
          "response",
          response({
            text: async () => {
              throw new Error("private-protocol-failure");
            },
          }),
        );
        throw journeyFailure;
      }),
    (error: unknown) =>
      error instanceof AggregateError &&
      error.message === "identity_browser_capture_identity_body_failed" &&
      error.cause === journeyFailure &&
      error.errors[0] === journeyFailure &&
      error.errors[1] instanceof Error &&
      !error.message.includes("private-protocol-failure"),
  );
  assert.ok(phases.includes("journey_failure_observed"));
  assert.ok(phases.includes("capture_cleanup_failed"));
  assert.equal(page.listenerCount("response"), 0);
});

test("a successful journey still fails when a body is unavailable to the capture", async () => {
  const phases: IdentityCapturePhase[] = [];
  const page = new EventEmitter();
  const capture = new IdentityPrivacyCapture(
    page as unknown as Page,
    [],
    "development",
    (phase) => phases.push(phase),
  );
  await assert.rejects(
    () =>
      capture.run(async () => {
        page.emit(
          "response",
          response({
            text: async () => {
              throw new Error(
                "Protocol error (Network.getResponseBody): No resource with given identifier found",
              );
            },
          }),
        );
      }),
    { message: "identity_browser_capture_identity_body_failed" },
  );
  assert.ok(phases.includes("capture_body_resource_missing"));
  assert.ok(phases.includes("capture_identity_body_failed"));
  assert.ok(!phases.includes("journey_failure_observed"));
});

test("snapshot progress identifies the exact pending capture boundary", async () => {
  const phases: IdentityCapturePhase[] = [];
  let releaseCookies!: (cookies: []) => void;
  let releaseStorage!: (storage: unknown) => void;
  const page = Object.assign(new EventEmitter(), {
    content: async () => "<html><body>Signed out</body></html>",
    evaluate: () =>
      new Promise((resolve) => {
        releaseStorage = resolve;
      }),
  });
  const context = {
    cookies: () =>
      new Promise<[]>((resolve) => {
        releaseCookies = resolve;
      }),
  } as unknown as BrowserContext;
  const capture = new IdentityPrivacyCapture(
    page as unknown as Page,
    [],
    "oidc",
    (phase) => phases.push(phase),
  );
  const snapshot = capture.snapshot(context);
  assert.deepEqual(phases, ["capture_snapshot_started"]);
  releaseCookies([]);
  await new Promise<void>((resolve) => setImmediate(resolve));
  assert.deepEqual(phases, [
    "capture_snapshot_started",
    "capture_cookies_read",
    "capture_page_read",
  ]);
  releaseStorage({ local: [], session: [], caches: [], cookie: "" });
  await snapshot;
  assert.deepEqual(phases, [
    "capture_snapshot_started",
    "capture_cookies_read",
    "capture_page_read",
    "capture_storage_read",
    "capture_responses_drained",
    "capture_snapshot_finished",
  ]);
  await capture.close();
});

test("snapshot progress does not report success when the real cache guard refuses data", async () => {
  const phases: IdentityCapturePhase[] = [];
  const privateValue = "private-storage-value-that-must-not-be-progress";
  const page = Object.assign(new EventEmitter(), {
    content: async () => "<html><body>Signed out</body></html>",
    evaluate: async () => ({
      local: [["private", privateValue]],
      session: [],
      caches: [],
      cookie: "",
    }),
  });
  const capture = new IdentityPrivacyCapture(
    page as unknown as Page,
    [],
    "oidc",
    (phase) => phases.push(phase),
  );
  await assert.rejects(
    capture.snapshot({
      cookies: async () => [],
    } as unknown as BrowserContext),
    { message: "identity_browser_private_cache_invalid" },
  );
  assert.equal(phases.at(-1), "capture_storage_read");
  assert.equal(phases.includes("capture_snapshot_finished"), false);
  assert.equal(JSON.stringify(phases).includes(privateValue), false);
  await capture.close();
});

for (const [name, overrides, code] of [
  [
    "headers",
    {
      allHeaders: async () => {
        throw new Error("private-header-detail");
      },
    },
    "identity_browser_capture_response_headers_failed",
  ],
  [
    "current identity body",
    {
      text: async () => {
        throw new Error("private-body-detail");
      },
    },
    "identity_browser_capture_identity_body_failed",
  ],
  [
    "response policy",
    { allHeaders: async () => ({ "cache-control": "no-store" }) },
    "identity_browser_private_cache_invalid",
  ],
] as const) {
  test(`fails closed with a safe specific code for ${name}`, async () => {
    const page = new EventEmitter();
    const capture = new IdentityPrivacyCapture(page as unknown as Page);
    page.emit("response", response(overrides));
    await assert.rejects(capture.close(), { message: code });
  });
}

test("final drainage awaits the actual body without an unresolved client finished promise", async () => {
  const page = new EventEmitter();
  let releaseBody!: (body: string) => void;
  const body = new Promise<string>((resolve) => {
    releaseBody = resolve;
  });
  const capture = new IdentityPrivacyCapture(page as unknown as Page);
  page.emit(
    "response",
    response({
      finished: () => new Promise(() => {}),
      text: () => body,
    }),
  );
  let closed = false;
  const closing = capture.close().then(() => {
    closed = true;
  });
  await new Promise<void>((resolve) => setImmediate(resolve));
  assert.equal(closed, false);
  releaseBody('{"code":"identity_unauthenticated"}');
  await closing;
  assert.equal(closed, true);
});

test("a real request failure during final drainage fails closed even when its body remains readable", async () => {
  const page = new EventEmitter();
  let releaseBody!: (body: string) => void;
  const body = new Promise<string>((resolve) => {
    releaseBody = resolve;
  });
  const captured = response({ text: () => body });
  const capture = new IdentityPrivacyCapture(page as unknown as Page);
  page.emit("response", captured);
  const closing = capture.close();
  const refused = assert.rejects(closing, {
    message: "identity_browser_capture_current_identity_fetch_aborted",
  });
  page.emit("requestfailed", captured.request());
  releaseBody('{"code":"identity_unauthenticated"}');
  await refused;
});

test("unknown network failures stay fatal without projecting their private error text", async () => {
  const phases: IdentityCapturePhase[] = [];
  const page = new EventEmitter();
  const capture = new IdentityPrivacyCapture(
    page as unknown as Page,
    [],
    "development",
    (phase) => phases.push(phase),
  );
  const request = response({}).request();
  request.failure = () => ({ errorText: "private-network-canary" });
  page.emit("requestfailed", request);
  await assert.rejects(capture.close(), {
    message: "identity_browser_capture_current_identity_fetch_aborted",
  });
  assert.ok(phases.includes("capture_network_failure_unclassified"));
  assert.ok(!JSON.stringify(phases).includes("private-network-canary"));
});

function signOutResponse(headers: Record<string, string> = {}): Response {
  return response({
    request: () =>
      ({
        method: () => "POST",
        url: () => "http://127.0.0.1:12345/api/identity/v1/sign-out",
        resourceType: () => "fetch",
        headers: () => ({}),
        failure: () => ({ errorText: "net::ERR_ABORTED" }),
      }) as ReturnType<Response["request"]>,
    url: () => "http://127.0.0.1:12345/api/identity/v1/sign-out",
    status: () => 204,
    allHeaders: async () => ({
      "cache-control": "no-store",
      "referrer-policy": "no-referrer",
      ...headers,
    }),
    text: async () => {
      throw new Error("Chromium has no response-body resource for HTTP 204");
    },
  });
}

test("the exact bodyless sign-out contract awaits completion without requesting a Chromium body resource", async () => {
  const page = new EventEmitter();
  let finish!: (error: Error | null) => void;
  const completed = new Promise<Error | null>((resolve) => {
    finish = resolve;
  });
  const capture = new IdentityPrivacyCapture(page as unknown as Page);
  page.emit("response", signOutResponse({}));
  page.emit(
    "response",
    signOutResponse({
      "content-length": "0",
    }),
  );
  const pending = signOutResponse();
  pending.finished = () => completed;
  page.emit("response", pending);
  let closed = false;
  const closing = capture.close().then(() => {
    closed = true;
  });
  await new Promise<void>((resolve) => setImmediate(resolve));
  assert.equal(closed, false);
  finish(null);
  await closing;
  assert.equal(closed, true);
});

const invalidSignOutFraming: Record<string, string>[] = [
  { "content-length": "1" },
  { "content-type": "application/json" },
  { "transfer-encoding": "chunked" },
  { "content-encoding": "gzip" },
];
for (const headers of invalidSignOutFraming) {
  test(`bodyless sign-out refuses unexpected framing ${Object.keys(headers)[0]}`, async () => {
    const page = new EventEmitter();
    const capture = new IdentityPrivacyCapture(page as unknown as Page);
    page.emit("response", signOutResponse(headers));
    await assert.rejects(capture.close(), {
      message: "identity_browser_capture_response_body_failed",
    });
  });
}

test("bodyless sign-out still refuses an aborted request even after response completion", async () => {
  const page = new EventEmitter();
  const capture = new IdentityPrivacyCapture(page as unknown as Page);
  const outgoing = signOutResponse();
  page.emit("response", outgoing);
  page.emit("requestfailed", outgoing.request());
  await assert.rejects(capture.close(), {
    message: "identity_browser_capture_response_aborted",
  });
});
