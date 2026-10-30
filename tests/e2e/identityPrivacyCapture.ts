import { exportIdentityCanaries } from "./identityCanaryExport.ts";
import { createHash } from "node:crypto";
import {
  consumeIdentityActionCapture,
  identityActionCaptureHeader,
} from "./identityActionCapture.ts";
import {
  type BrowserContext,
  type Page,
  type Request,
  type Response,
} from "@playwright/test";

type Channel =
  "response" | "redirect" | "page" | "cache" | "console" | "handoff";

export type IdentityCapturePhase =
  | "capture_snapshot_started"
  | "capture_cookies_read"
  | "capture_page_read"
  | "capture_storage_read"
  | "capture_responses_drained"
  | "capture_snapshot_finished"
  | "capture_request_failed"
  | "capture_network_aborted"
  | "capture_network_length_mismatch"
  | "capture_network_incomplete_encoding"
  | "capture_network_failure_unclassified"
  | "capture_context_requested"
  | "capture_context_response_started"
  | "capture_context_body_finished"
  | "capture_context_body_failed"
  | "capture_identity_requested"
  | "capture_identity_response_started"
  | "capture_identity_body_finished"
  | "capture_identity_body_failed"
  | "capture_body_resource_missing"
  | "capture_body_target_closed"
  | "journey_failure_observed"
  | "capture_cleanup_started"
  | "capture_cleanup_finished"
  | "capture_cleanup_failed";

const responseFailures = new Set([
  "identity_browser_capture_response_headers_failed",
  "identity_browser_capture_response_body_failed",
  "identity_browser_capture_identity_body_failed",
  "identity_browser_capture_context_body_failed",
  "identity_browser_capture_handoff_body_failed",
  "identity_browser_capture_page_body_failed",
  "identity_browser_capture_response_aborted",
  "identity_browser_private_cache_invalid",
  "identity_browser_private_handoff_invalid",
  "identity_browser_private_identity_invalid",
  "identity_browser_private_action_capture_invalid",
]);

function hasEmptySignOutFraming(headers: Record<string, string>) {
  return (
    headers["content-type"] === undefined &&
    headers["content-encoding"] === undefined &&
    headers["transfer-encoding"] === undefined &&
    (headers["content-length"] === undefined ||
      headers["content-length"] === "0")
  );
}

export class IdentityPrivacyCapture {
  private readonly secrets = new Set<string>();
  private readonly transportCanaries = new Set<string>();
  private readonly buffers = new Map<Channel, string[]>();
  private readonly pending = new Set<Promise<void>>();
  private readonly failures: string[] = [];
  private readonly aborted = new Map<Request, string>();
  private readonly capturedActions = new Set<Request>();
  private readonly handoffs = new Set<string>();
  private bytes = 0;
  private closed = false;
  private transportObserved = false;
  private developmentSelected = false;
  private developmentRemoved = false;
  private csrfObserved = false;
  private identityObserved = false;
  private readonly page: Page;
  private readonly mode: "oidc" | "development";
  private readonly progress?: (phase: IdentityCapturePhase) => void;

  constructor(
    page: Page,
    forbidden: readonly string[] = [],
    mode: "oidc" | "development" = "oidc",
    progress?: (phase: IdentityCapturePhase) => void,
  ) {
    this.page = page;
    this.mode = mode;
    this.progress = progress;
    for (const value of forbidden) this.protect(value);
    page.on("request", this.observeRequest);
    page.on("response", this.observeResponse);
    page.on("requestfailed", this.observeRequestFailure);
    page.on("console", this.observeConsole);
    page.on("pageerror", this.observeError);
  }

  protect(value: string) {
    if (value.length < 16 || value.length > 8192)
      throw new Error("identity_browser_private_canary_invalid");
    this.secrets.add(value);
  }

  private readonly observeRequest = (request: Request) => {
    const url = new URL(request.url());
    if (url.pathname === "/api/identity/v1/browser-context")
      this.progress?.("capture_context_requested");
    if (url.pathname === "/api/identity/v1/me")
      this.progress?.("capture_identity_requested");
    const csrf = request.headers()["x-kofte-csrf"];
    if (csrf) {
      if (csrf.length < 16 || csrf.length > 8192) {
        this.failures.push("identity_browser_capture_csrf_invalid");
        return;
      }
      this.transportCanaries.add(csrf);
      this.csrfObserved = true;
    }
    if (["/authorize", "/auth/oidc/callback"].includes(url.pathname)) {
      for (const name of ["code", "state", "nonce", "error_description"]) {
        const value = url.searchParams.get(name);
        if (value) this.secrets.add(value);
      }
    } else {
      this.add("redirect", request.url());
    }
  };

  private add(channel: Channel, value: string) {
    if (this.closed) {
      this.failures.push("identity_browser_capture_closed");
      return;
    }
    this.bytes += Buffer.byteLength(value);
    if (this.bytes > 4_000_000) {
      this.failures.push("identity_browser_capture_limit_exceeded");
      return;
    }
    const values = this.buffers.get(channel) ?? [];
    values.push(value);
    this.buffers.set(channel, values);
  }

  private readonly observeConsole = (message: { text(): string }) =>
    this.add("console", message.text());

  private readonly observeError = (error: Error) =>
    this.add("console", error.message);

  private readonly observeRequestFailure = (request: Request) => {
    const path = new URL(request.url()).pathname;
    if (
      !["/identity", "/auth/oidc/callback"].includes(path) &&
      !path.startsWith("/api/identity/v1/")
    )
      return;
    const resource = request.resourceType();
    const code =
      path === "/api/identity/v1/browser-context" && resource === "fetch"
        ? "identity_browser_capture_browser_context_fetch_aborted"
        : path === "/api/identity/v1/me" && resource === "fetch"
          ? "identity_browser_capture_current_identity_fetch_aborted"
          : path === "/identity" && resource === "document"
            ? "identity_browser_capture_identity_document_aborted"
            : path === "/identity" && resource === "fetch"
              ? "identity_browser_capture_identity_action_fetch_aborted"
              : path === "/auth/oidc/callback" && resource === "document"
                ? "identity_browser_capture_callback_document_aborted"
                : "identity_browser_capture_response_aborted";
    this.aborted.set(request, code);
    this.progress?.("capture_request_failed");
    const network = request.failure()?.errorText;
    this.progress?.(
      network === "net::ERR_ABORTED"
        ? "capture_network_aborted"
        : network === "net::ERR_CONTENT_LENGTH_MISMATCH"
          ? "capture_network_length_mismatch"
          : network === "net::ERR_INCOMPLETE_CHUNKED_ENCODING" ||
              network === "net::ERR_CONTENT_DECODING_FAILED"
            ? "capture_network_incomplete_encoding"
            : "capture_network_failure_unclassified",
    );
    const captureId = request.headers()[identityActionCaptureHeader];
    if (
      captureId &&
      request.method() === "POST" &&
      [
        "/api/identity/v1/first-administrator",
        "/api/identity/v1/sign-out",
      ].includes(path)
    ) {
      const read = this.readLostAcknowledgement(request, path, captureId).catch(
        () => {
          this.failures.push("identity_browser_private_action_capture_invalid");
        },
      );
      this.pending.add(read);
      void read.finally(() => this.pending.delete(read));
    }
  };

  private captureHeaders(headers: Record<string, string>, login: boolean) {
    for (const [name, value] of Object.entries(headers)) {
      if (name === "set-cookie") {
        for (const match of value.matchAll(
          /(?:^|\n|, )(?:__Host-kofte-|kofte-dev-)[^=;\s]+=([^;\s,]+)/g,
        )) {
          if (match[1].length >= 16) this.secrets.add(match[1]);
        }
      } else if (login && ["location", "x-action-redirect"].includes(name)) {
        const target = new URL(value.split(";")[0]);
        if (
          target.protocol !== "https:" ||
          target.pathname !== "/authorize" ||
          target.username ||
          target.password ||
          target.hash ||
          !target.searchParams.get("state") ||
          !target.searchParams.get("nonce")
        )
          throw new Error("identity_browser_private_handoff_invalid");
        this.handoffs.add(target.href);
        this.add("handoff", value);
        this.transportObserved = true;
      } else this.add(name === "location" ? "redirect" : "response", value);
    }
  }

  private async readLostAcknowledgement(
    request: Request,
    path: string,
    id: string,
  ) {
    const signOut = path === "/api/identity/v1/sign-out";
    const text = await consumeIdentityActionCapture(
      process.env.KOFTE_IDENTITY_PRIVATE_ROOT,
      id,
      signOut ? 204 : 200,
      undefined,
      signOut ? "identity-sign-out-lost-ack" : "identity-enrollment-lost-ack",
    );
    const value: unknown = JSON.parse(text);
    if (
      typeof value !== "object" ||
      value === null ||
      Object.keys(value).join(",") !==
        "method,path,requestSha256,headers,body" ||
      !("method" in value) ||
      value.method !== "POST" ||
      !("path" in value) ||
      value.path !== path ||
      !("requestSha256" in value) ||
      value.requestSha256 !==
        createHash("sha256")
          .update(request.postDataBuffer() ?? Buffer.alloc(0))
          .digest("hex") ||
      !("headers" in value) ||
      typeof value.headers !== "object" ||
      value.headers === null ||
      Array.isArray(value.headers) ||
      !("body" in value) ||
      typeof value.body !== "string" ||
      Buffer.byteLength(value.body) > 8192 ||
      (signOut && value.body !== "")
    )
      throw new Error("identity_browser_private_action_capture_invalid");
    const headers: Record<string, string> = {};
    for (const [name, values] of Object.entries(value.headers)) {
      if (
        !/^[a-z0-9-]+$/.test(name) ||
        !Array.isArray(values) ||
        !values.length ||
        values.some((item: unknown) => typeof item !== "string")
      )
        throw new Error("identity_browser_private_action_capture_invalid");
      headers[name] = values.join("\n");
    }
    if (
      !headers["cache-control"]?.includes("no-store") ||
      headers["referrer-policy"] !== "no-referrer" ||
      headers["location"] !== undefined ||
      headers[identityActionCaptureHeader] !== undefined ||
      (signOut && !hasEmptySignOutFraming(headers))
    )
      throw new Error("identity_browser_private_action_capture_invalid");
    this.captureHeaders(headers, false);
    this.add("response", value.body);
    this.capturedActions.add(request);
  }

  private readonly observeResponse = (response: Response) => {
    const read = this.readResponse(response).catch((error: unknown) => {
      this.failures.push(
        error instanceof Error && responseFailures.has(error.message)
          ? error.message
          : "identity_browser_capture_response_failed",
      );
    });
    this.pending.add(read);
    void read.finally(() => this.pending.delete(read));
  };

  private async readResponse(response: Response) {
    const request = response.request();
    const url = new URL(response.url());
    if (
      !["/identity", "/auth/oidc/callback"].includes(url.pathname) &&
      !url.pathname.startsWith("/api/identity/v1/")
    )
      return;
    if (url.pathname === "/api/identity/v1/browser-context")
      this.progress?.("capture_context_response_started");
    if (url.pathname === "/api/identity/v1/me")
      this.progress?.("capture_identity_response_started");
    let headers: Record<string, string>;
    try {
      headers = await response.allHeaders();
    } catch {
      throw new Error("identity_browser_capture_response_headers_failed");
    }
    const login = request.method() === "POST" && url.pathname === "/identity";
    this.captureHeaders(headers, login);
    if (
      login &&
      request.resourceType() === "fetch" &&
      Boolean(request.headers()["next-action"])
    ) {
      // Next's external navigation can abort the browser fetch. Use only its
      // correlated, complete upstream response; never substitute an empty body.
      const body = await consumeIdentityActionCapture(
        process.env.KOFTE_IDENTITY_PRIVATE_ROOT,
        headers[identityActionCaptureHeader],
        response.status(),
        headers["content-encoding"],
      );
      this.add("handoff", body);
      this.capturedActions.add(request);
      return;
    }
    if (headers[identityActionCaptureHeader])
      throw new Error("identity_browser_private_action_capture_invalid");
    if (response.status() >= 300 && response.status() < 400) return;
    if (request.method() === "POST") {
      if (
        url.pathname === "/api/identity/v1/development-session" &&
        response.status() === 200
      )
        this.developmentSelected = true;
      if (
        url.pathname === "/api/identity/v1/sign-out" &&
        response.status() === 204
      )
        this.developmentRemoved = true;
    }
    if (url.pathname !== "/identity" || request.method() === "GET") {
      if (
        !headers["cache-control"]?.includes("no-store") ||
        headers["referrer-policy"] !== "no-referrer"
      )
        throw new Error("identity_browser_private_cache_invalid");
    }
    if (
      url.pathname === "/api/identity/v1/sign-out" &&
      request.method() === "POST" &&
      response.status() === 204
    ) {
      if (
        !hasEmptySignOutFraming(headers) ||
        (await response.finished()) !== null
      )
        throw new Error("identity_browser_capture_response_body_failed");
      return;
    }
    let body = "";
    let bodyFailureCode: string | undefined;
    try {
      // The pinned client's finished() never settles for requestfailed.
      // text() awaits server completion; the failure event is captured separately.
      body = await response.text();
      if (url.pathname === "/api/identity/v1/browser-context")
        this.progress?.("capture_context_body_finished");
      if (url.pathname === "/api/identity/v1/me")
        this.progress?.("capture_identity_body_finished");
    } catch (error) {
      if (url.pathname === "/api/identity/v1/browser-context")
        this.progress?.("capture_context_body_failed");
      if (url.pathname === "/api/identity/v1/me")
        this.progress?.("capture_identity_body_failed");
      if (error instanceof Error) {
        if (
          error.message.includes("No resource with given identifier found") ||
          error.message.includes(
            "No data found for resource with given identifier",
          )
        )
          this.progress?.("capture_body_resource_missing");
        else if (
          error.message.includes(
            "Target page, context or browser has been closed",
          )
        )
          this.progress?.("capture_body_target_closed");
      }
      bodyFailureCode =
        url.pathname === "/api/identity/v1/me"
          ? "identity_browser_capture_identity_body_failed"
          : url.pathname === "/api/identity/v1/browser-context"
            ? "identity_browser_capture_context_body_failed"
            : login
              ? "identity_browser_capture_handoff_body_failed"
              : url.pathname === "/identity"
                ? "identity_browser_capture_page_body_failed"
                : "identity_browser_capture_response_body_failed";
    }
    if (bodyFailureCode) throw new Error(bodyFailureCode);
    if (url.pathname === "/api/identity/v1/me" && response.status() === 200) {
      let identity: unknown;
      try {
        identity = JSON.parse(body);
      } catch {
        throw new Error("identity_browser_private_identity_invalid");
      }
      if (
        typeof identity !== "object" ||
        identity === null ||
        !("userId" in identity) ||
        typeof identity.userId !== "string" ||
        !/^[0-9a-f]{8}(-[0-9a-f]{4}){3}-[0-9a-f]{12}$/.test(identity.userId)
      )
        throw new Error("identity_browser_private_identity_invalid");
      // Identity and CSRF are necessary browser data, but never transport-log data.
      this.transportCanaries.add(identity.userId);
      this.identityObserved = true;
    }
    this.add(login ? "handoff" : "response", body);
  }

  async snapshot(context: BrowserContext) {
    this.progress?.("capture_snapshot_started");
    for (const cookie of await context.cookies()) {
      if (
        /^(?:__Host-kofte-|kofte-dev-)/.test(cookie.name) &&
        cookie.value.length >= 16
      )
        this.secrets.add(cookie.value);
    }
    this.progress?.("capture_cookies_read");
    this.add("page", await this.page.content());
    this.progress?.("capture_page_read");
    const browser = await this.page.evaluate(async () => ({
      local: Object.entries(localStorage),
      session: Object.entries(sessionStorage),
      caches: await caches.keys(),
      cookie: document.cookie,
    }));
    this.progress?.("capture_storage_read");
    this.add("cache", JSON.stringify(browser));
    if (
      browser.local.length ||
      browser.session.length ||
      browser.caches.length ||
      browser.cookie
    )
      throw new Error("identity_browser_private_cache_invalid");
    await this.drain();
    this.progress?.("capture_responses_drained");
    this.scan();
    this.progress?.("capture_snapshot_finished");
  }

  private scan() {
    if (this.failures.length) throw new Error(this.failures[0]);
    for (const [request, code] of this.aborted)
      if (!this.capturedActions.has(request)) throw new Error(code);
    for (const [channel, values] of this.buffers) {
      if (
        values.some((raw) => {
          // Exempt only the exact admitted protocol URL in its necessary handoff response.
          const value =
            channel === "handoff"
              ? [...this.handoffs].reduce(
                  (text, target) => text.replaceAll(target, ""),
                  raw,
                )
              : raw;
          const forbidden =
            channel === "console" ||
            channel === "redirect" ||
            channel === "cache"
              ? new Set([...this.secrets, ...this.transportCanaries])
              : this.secrets;
          return [...forbidden].some(
            (secret) =>
              value.includes(secret) ||
              value.includes(encodeURIComponent(secret)),
          );
        })
      )
        throw new Error("identity_browser_privacy_disclosure");
    }
  }

  private async drain() {
    let timer: ReturnType<typeof setTimeout> | undefined;
    try {
      await Promise.race([
        (async () => {
          while (this.pending.size) await Promise.all([...this.pending]);
        })(),
        new Promise<never>((_, reject) => {
          timer = setTimeout(
            () =>
              reject(new Error("identity_browser_privacy_capture_incomplete")),
            4500,
          );
        }),
      ]);
    } finally {
      clearTimeout(timer);
    }
  }

  async proveCapture(context: BrowserContext) {
    await this.page.evaluate(() =>
      console.debug("identity_browser_private_capture_ready"),
    );
    await this.snapshot(context);
    if (
      (this.mode === "oidc"
        ? !this.transportObserved
        : !this.developmentSelected ||
          !this.developmentRemoved ||
          !this.csrfObserved ||
          !this.identityObserved) ||
      this.secrets.size < 3 ||
      ["response", "redirect", "page", "cache", "console"].some(
        (channel) => !this.buffers.get(channel as Channel)?.length,
      )
    )
      throw new Error("identity_browser_privacy_capture_missing");
    const secret = [...this.secrets][0];
    const consoleBefore = this.buffers.get("console")!.length;
    await Promise.all([
      this.page.waitForEvent("console", {
        predicate: (message) => message.text() === secret,
      }),
      this.page.evaluate((value) => console.debug(value), secret),
    ]);
    if (this.buffers.get("console")!.length <= consoleBefore)
      throw new Error("identity_browser_privacy_capture_missing");
    let rejected = false;
    try {
      this.scan();
    } catch (error) {
      if (
        !(error instanceof Error) ||
        error.message !== "identity_browser_privacy_disclosure"
      )
        throw error;
      rejected = true;
    }
    if (!rejected) throw new Error("identity_browser_privacy_scanner_failed");
    this.buffers.get("console")!.splice(consoleBefore);
    const channels: Channel[] = [
      "response",
      "redirect",
      "page",
      "cache",
      ...(this.mode === "oidc" ? ["handoff" as const] : []),
    ];
    for (const channel of channels) {
      const values = this.buffers.get(channel)!;
      values.push(values[0] + secret);
      rejected = false;
      try {
        this.scan();
      } catch (error) {
        if (
          !(error instanceof Error) ||
          error.message !== "identity_browser_privacy_disclosure"
        )
          throw error;
        rejected = true;
      } finally {
        values.pop();
      }
      if (!rejected) throw new Error("identity_browser_privacy_scanner_failed");
    }
    this.scan();
    await exportIdentityCanaries(process.env.KOFTE_IDENTITY_PRIVATE_ROOT, [
      ...new Set([...this.secrets, ...this.transportCanaries]),
    ]);
  }

  async run(visit: () => Promise<void>) {
    let journeyFailure: unknown;
    let journeyFailed = false;
    try {
      await visit();
    } catch (error) {
      journeyFailed = true;
      journeyFailure = error;
      this.progress?.("journey_failure_observed");
    }
    let captureFailure: unknown;
    let captureFailed = false;
    this.progress?.("capture_cleanup_started");
    try {
      await this.close();
      this.progress?.("capture_cleanup_finished");
    } catch (error) {
      captureFailed = true;
      captureFailure = error;
      this.progress?.("capture_cleanup_failed");
    }
    if (journeyFailed && captureFailed)
      // Playwright serializes Error.cause, not AggregateError.errors.
      throw new AggregateError(
        [journeyFailure, captureFailure],
        captureFailure instanceof Error
          ? captureFailure.message
          : "identity_browser_privacy_capture_failed",
        { cause: journeyFailure },
      );
    if (journeyFailed) throw journeyFailure;
    if (captureFailed) throw captureFailure;
  }

  async close() {
    this.page.off("request", this.observeRequest);
    this.page.off("response", this.observeResponse);
    this.page.off("console", this.observeConsole);
    this.page.off("pageerror", this.observeError);
    try {
      await this.drain();
      this.scan();
    } finally {
      this.page.off("requestfailed", this.observeRequestFailure);
      this.closed = true;
      this.secrets.clear();
      this.transportCanaries.clear();
      this.handoffs.clear();
      this.buffers.clear();
      this.failures.length = 0;
      this.aborted.clear();
      this.capturedActions.clear();
      this.bytes = 0;
    }
  }
}
