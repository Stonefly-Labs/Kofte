import { randomBytes } from "node:crypto";
import { isCurrentIdentity } from "@kofte/bootstrap-contract/identity";
import {
  identityConfiguration,
  type IdentityAdapterConfiguration,
} from "./identityConfiguration";

type Route = "context" | "login" | "callback" | "me" | "enrollment";
const routes = {
  context: ["GET", "/api/identity/v1/browser-context"],
  login: ["POST", "/auth/login"],
  callback: ["GET", "/auth/oidc/callback"],
  me: ["GET", "/api/identity/v1/me"],
  enrollment: ["POST", "/api/identity/v1/first-administrator"],
} as const;
const privacy = {
  "Cache-Control": "no-store",
  Pragma: "no-cache",
  "Referrer-Policy": "no-referrer",
};
const failures = {
  identity_request_invalid: [400, "Invalid identity request"],
  identity_unauthenticated: [401, "Authentication required"],
  identity_forbidden: [403, "Identity request forbidden"],
  identity_enrollment_unavailable: [409, "Enrollment unavailable"],
  identity_admission_exceeded: [429, "Identity admission exceeded"],
  identity_unavailable: [503, "Identity unavailable"],
  identity_outcome_uncertain: [503, "Identity outcome uncertain"],
} as const;
type Failure = keyof typeof failures;
class IdentityInputError extends TypeError {}

export async function forwardIdentity(
  request: Request,
  route: Route,
): Promise<Response> {
  const [method, path] = routes[route];
  const signal = AbortSignal.any([request.signal, AbortSignal.timeout(4500)]);
  let abort: (() => void) | undefined;
  const expired = new Promise<never>((_, reject) => {
    abort = () =>
      reject(new DOMException("identity_adapter_aborted", "AbortError"));
    if (signal.aborted) abort();
    else signal.addEventListener("abort", abort, { once: true });
  });
  let dispatched = false;
  try {
    const configuration = await Promise.race([
      identityConfiguration(),
      expired,
    ]);
    if (
      configuration.mode !== "oidc" &&
      (route === "login" || route === "callback")
    )
      return new Response(null, { status: 404, headers: privacy });
    if (request.method !== method)
      return new Response(null, {
        status: 405,
        headers: { ...privacy, Allow: method },
      });
    if (request.headers.has("authorization"))
      return problem("identity_unauthenticated");
    const url = new URL(request.url);
    if (url.pathname !== path)
      return new Response(null, { status: 404, headers: privacy });
    const length = request.headers.get("content-length");
    if (
      (route !== "callback" && request.url.includes("?")) ||
      Buffer.byteLength(url.pathname + url.search) > 16384 ||
      headerBytes(request.headers) > 16384 ||
      (length !== null && (!/^\d+$/.test(length) || Number(length) > 8192)) ||
      (method === "GET" &&
        (request.body !== null ||
          Number(request.headers.get("content-length")) > 0 ||
          request.headers.has("transfer-encoding"))) ||
      request.headers.has("content-encoding")
    )
      return problem("identity_request_invalid");
    const headers = new Headers({ Accept: "application/json" });
    const cookies = requestCookies(
      request.headers.get("cookie") ?? "",
      configuration,
    );
    if (cookies === null) return problem("identity_request_invalid");
    if (cookies) headers.set("Cookie", cookies);
    let body: string | undefined;
    if (method === "POST") {
      if (
        request.headers.get("origin") !== configuration.publicOrigin ||
        (request.headers.has("sec-fetch-site") &&
          !["same-origin", "same-site", "none"].includes(
            request.headers.get("sec-fetch-site")!,
          ))
      )
        return problem("identity_forbidden");
      if (
        !(
          route === "login"
            ? /^application\/x-www-form-urlencoded(?:;|$)/i
            : /^application\/json(?:;|$)/i
        ).test(request.headers.get("content-type") ?? "")
      )
        return problem("identity_request_invalid");
      body = await readBody(request, expired, true);
      if (route === "login") {
        const fields = new URLSearchParams(body);
        if (
          [...fields.keys()].some(
            (key) =>
              !["returnPath", "__RequestVerificationToken"].includes(key) ||
              fields.getAll(key).length !== 1,
          ) ||
          !["/", "/identity"].includes(fields.get("returnPath") ?? "/identity")
        )
          return problem("identity_request_invalid");
      } else {
        let value: unknown;
        try {
          value = JSON.parse(body);
        } catch (error) {
          if (!(error instanceof SyntaxError)) throw error;
          throw new IdentityInputError();
        }
        if (
          typeof value !== "object" ||
          value === null ||
          Array.isArray(value) ||
          Object.keys(value).length !== 1 ||
          !("authorization" in value) ||
          typeof value.authorization !== "string" ||
          !/^[A-Za-z0-9_-]{43}$/.test(value.authorization) ||
          Buffer.from(value.authorization, "base64url").toString(
            "base64url",
          ) !== value.authorization
        )
          return problem("identity_request_invalid");
      }
      headers.set("Content-Type", request.headers.get("content-type")!);
      headers.set("Origin", configuration.publicOrigin);
      const csrf = request.headers.get("x-kofte-csrf");
      if (csrf !== null) headers.set("X-Kofte-CSRF", csrf);
    }
    const destination = new URL(path + url.search, configuration.backendOrigin);
    dispatched = true;
    const upstream = await Promise.race([
      fetch(destination, {
        method,
        headers,
        body,
        signal,
        redirect: "manual",
        cache: "no-store",
      }),
      expired,
    ]);
    const text = await readBody(upstream, expired);
    if (
      headerBytes(upstream.headers) > 16384 ||
      upstream.headers.get("cache-control") !== "no-store" ||
      upstream.headers.get("pragma") !== "no-cache" ||
      upstream.headers.get("referrer-policy") !== "no-referrer" ||
      upstream.headers.has("content-encoding")
    )
      throw new TypeError("identity_upstream_invalid");
    const output = new Headers(privacy);
    for (const cookie of responseCookies(upstream.headers, configuration))
      output.append("Set-Cookie", cookie);
    if (
      upstream.status === 303 &&
      (route === "login" || route === "callback")
    ) {
      const location = upstream.headers.get("location") ?? "";
      if (
        !/^text\/plain(?:;|$)/i.test(
          upstream.headers.get("content-type") ?? "",
        ) ||
        !validRedirect(location, route, configuration)
      )
        throw new TypeError("identity_upstream_invalid");
      output.set("Location", location);
      output.set("Content-Type", "text/plain; charset=utf-8");
      return new Response(null, { status: 303, headers: output });
    }
    const mediaType =
      upstream.status === 200 ? "application/json" : "application/problem+json";
    if (
      upstream.headers.get("location") !== null ||
      upstream.headers
        .get("content-type")
        ?.split(";")[0]
        .trim()
        .toLowerCase() !== mediaType
    )
      throw new TypeError("identity_upstream_invalid");
    const value: unknown = JSON.parse(text);
    if (upstream.status === 200 && route === "me" && isCurrentIdentity(value)) {
      if (value.identityMode !== configuration.mode)
        throw new TypeError("identity_upstream_invalid");
      return json(
        {
          version: value.version,
          userId: value.userId,
          displayName: value.displayName,
          identityMode: value.identityMode,
          isInitialAdministrator: value.isInitialAdministrator,
        },
        200,
        output,
      );
    }
    if (typeof value !== "object" || value === null || Array.isArray(value))
      throw new TypeError("identity_upstream_invalid");
    const object: Record<string, unknown> = Object.fromEntries(
      Object.entries(value),
    );
    if (
      upstream.status === 200 &&
      route === "context" &&
      object.version === "1" &&
      object.identityMode === configuration.mode &&
      typeof object.csrfToken === "string" &&
      /^[A-Za-z0-9_-]{1,2048}$/.test(object.csrfToken)
    )
      return json(
        {
          version: "1",
          identityMode: object.identityMode,
          csrfToken: object.csrfToken,
        },
        200,
        output,
      );
    const code = object.code;
    if (
      upstream.status === 200 &&
      route === "enrollment" &&
      object.version === "1" &&
      object.status === "enrolled" &&
      typeof object.correlationId === "string" &&
      /^[0-9a-f]{32}$/.test(object.correlationId)
    )
      return json(
        {
          version: "1",
          status: "enrolled",
          correlationId: object.correlationId,
        },
        200,
        output,
      );
    if (
      typeof code !== "string" ||
      !Object.hasOwn(failures, code) ||
      !isFailure(code) ||
      failures[code][0] !== upstream.status ||
      object.status !== upstream.status ||
      object.type !== "urn:kofte:problem:" + code ||
      object.title !== failures[code][1] ||
      typeof object.correlationId !== "string" ||
      !/^[0-9a-f]{32}$/.test(object.correlationId)
    )
      throw new TypeError("identity_upstream_invalid");
    const retry = upstream.headers.get("retry-after");
    if (upstream.status === 429 && retry !== null) {
      if (!/^(?:[1-9]|[1-5][0-9]|60)$/.test(retry))
        throw new TypeError("identity_upstream_invalid");
      output.set("Retry-After", retry);
    }
    return problem(code, object.correlationId, output);
  } catch (error) {
    if (error instanceof IdentityInputError)
      return problem("identity_request_invalid");
    if (!(
      error instanceof TypeError ||
      error instanceof SyntaxError ||
      error instanceof DOMException
    ))
      throw error;
    return problem(
      dispatched && (method === "POST" || route === "callback")
        ? "identity_outcome_uncertain"
        : "identity_unavailable",
    );
  } finally {
    if (abort) signal.removeEventListener("abort", abort);
  }
}

function isFailure(value: string): value is Failure {
  return Object.hasOwn(failures, value);
}

function problem(
  code: Failure,
  correlationId = randomBytes(16).toString("hex"),
  headers = new Headers(privacy),
): Response {
  const [status, title] = failures[code];
  if (status === 503)
    console.error(
      JSON.stringify({
        eventCode: code,
        component: "identity_adapter",
        correlationId,
      }),
    );
  headers.set("Content-Type", "application/problem+json");
  return json(
    { type: "urn:kofte:problem:" + code, title, status, code, correlationId },
    status,
    headers,
  );
}

function json(value: unknown, status: number, headers: Headers): Response {
  if (!headers.has("Content-Type"))
    headers.set("Content-Type", "application/json; charset=utf-8");
  return new Response(
    JSON.stringify(value).replace(
      /[<>&\u2028\u2029]/g,
      (character) =>
        "\\u" + character.charCodeAt(0).toString(16).padStart(4, "0"),
    ),
    { status, headers },
  );
}

function headerBytes(headers: Headers): number {
  return (
    [...headers]
      .filter(([name]) => name !== "set-cookie")
      .reduce(
        (total, [name, value]) =>
          total + Buffer.byteLength(name) + Buffer.byteLength(value) + 4,
        0,
      ) +
    headers
      .getSetCookie()
      .reduce((total, cookie) => total + Buffer.byteLength(cookie) + 14, 0)
  );
}

function admittedCookie(
  name: string,
  configuration: IdentityAdapterConfiguration,
): boolean {
  if (configuration.mode === "development")
    return [
      "kofte-dev-session",
      "kofte-dev-antiforgery",
      "kofte-dev-browser",
    ].includes(name);
  return (
    [
      "__Host-kofte-session",
      "__Host-kofte-antiforgery",
      "__Host-kofte-browser",
    ].includes(name) ||
    /^__Host-kofte-(?:nonce|correlation)\.[A-Za-z0-9_-]+$/.test(name)
  );
}

function requestCookies(
  header: string,
  configuration: IdentityAdapterConfiguration,
): string | null {
  const names = new Set<string>();
  const result: string[] = [];
  for (const raw of header.split(";")) {
    const cookie = raw.trim();
    const separator = cookie.indexOf("=");
    const name = cookie.slice(0, separator);
    if (
      separator < 1 ||
      !(
        [
          "__Host-kofte-session",
          "__Host-kofte-antiforgery",
          "__Host-kofte-browser",
          "kofte-dev-session",
          "kofte-dev-antiforgery",
          "kofte-dev-browser",
        ].includes(name) ||
        name.startsWith("__Host-kofte-nonce.") ||
        name.startsWith("__Host-kofte-correlation.")
      )
    )
      continue;
    if (
      names.has(name) ||
      !/^[A-Za-z0-9._~-]*$/.test(cookie.slice(separator + 1))
    )
      return null;
    names.add(name);
    if (admittedCookie(name, configuration)) result.push(cookie);
  }
  return names.size > 16 ? null : result.join("; ");
}

function responseCookies(
  headers: Headers,
  configuration: IdentityAdapterConfiguration,
): string[] {
  const cookies = headers.getSetCookie();
  const names = new Set<string>();
  if (cookies.length > 16) throw new TypeError("identity_upstream_invalid");
  for (const cookie of cookies) {
    const [pair, ...parts] = cookie.split(";");
    const separator = pair.indexOf("=");
    const name = pair.slice(0, separator);
    const attributes = parts.map((part) => part.trim().toLowerCase());
    const attributeNames = attributes.map((part) => part.split("=")[0]);
    if (
      separator < 1 ||
      !admittedCookie(name, configuration) ||
      names.has(name) ||
      !/^[A-Za-z0-9._~-]*$/.test(pair.slice(separator + 1)) ||
      new Set(attributeNames).size !== attributeNames.length ||
      !attributes.includes("path=/") ||
      !attributes.includes("httponly") ||
      !attributes.includes("samesite=lax") ||
      attributeNames.includes("domain") ||
      (configuration.mode === "oidc" && !attributes.includes("secure"))
    )
      throw new TypeError("identity_upstream_invalid");
    names.add(name);
  }
  return cookies;
}

function validRedirect(
  location: string,
  route: Route,
  configuration: IdentityAdapterConfiguration,
): boolean {
  if (route === "callback") return location === "/" || location === "/identity";
  const target = new URL(location);
  const expected = new URL(configuration.authorizationEndpoint!);
  return (
    target.origin === expected.origin &&
    target.pathname === expected.pathname &&
    !target.username &&
    !target.password &&
    !target.hash &&
    Buffer.byteLength(location) <= 16384 &&
    [...expected.searchParams].every(
      ([key, value]) =>
        target.searchParams.getAll(key).length === 1 &&
        target.searchParams.get(key) === value,
    )
  );
}

async function readBody(
  message: Request | Response,
  expired: Promise<never>,
  input = false,
): Promise<string> {
  const length = message.headers.get("content-length");
  if (length !== null && (!/^\d+$/.test(length) || Number(length) > 8192))
    throw new (input ? IdentityInputError : TypeError)("identity_body_invalid");
  if (!message.body) {
    if (length !== null && Number(length) !== 0)
      throw new (input ? IdentityInputError : TypeError)(
        "identity_body_invalid",
      );
    return "";
  }
  const reader = message.body.getReader();
  const chunks: Uint8Array[] = [];
  let size = 0;
  let complete = false;
  try {
    while (true) {
      const result = await Promise.race([reader.read(), expired]);
      if (result.done) {
        complete = true;
        break;
      }
      size += result.value.byteLength;
      if (size > 8192)
        throw new (input ? IdentityInputError : TypeError)(
          "identity_body_invalid",
        );
      chunks.push(result.value);
    }
    if (length !== null && Number(length) !== size)
      throw new (input ? IdentityInputError : TypeError)(
        "identity_body_invalid",
      );
    const bytes = new Uint8Array(size);
    let offset = 0;
    for (const chunk of chunks) {
      bytes.set(chunk, offset);
      offset += chunk.byteLength;
    }
    return new TextDecoder("utf-8", { fatal: true }).decode(bytes);
  } catch (error) {
    if (
      input &&
      error instanceof TypeError &&
      !(error instanceof IdentityInputError)
    )
      throw new IdentityInputError("identity_body_invalid", { cause: error });
    throw error;
  } finally {
    if (!complete)
      void reader.cancel().catch(() =>
        console.error(
          JSON.stringify({
            eventCode: "identity_body_cleanup_failed",
            component: "identity_adapter",
          }),
        ),
      );
    reader.releaseLock();
  }
}
