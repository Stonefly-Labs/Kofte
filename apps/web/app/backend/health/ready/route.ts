import { randomBytes } from "node:crypto";
import { isReadiness } from "@kofte/bootstrap-contract";
import { backendReadinessUrl } from "../../../../features/bootstrap/backendConfiguration";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";
const headers = { "Cache-Control": "no-store" };

function unsupported() {
  return new Response(null, {
    status: 405,
    headers: { ...headers, Allow: "GET" },
  });
}

export const HEAD = unsupported;
export const POST = unsupported;
export const PUT = unsupported;
export const PATCH = unsupported;
export const DELETE = unsupported;
export const OPTIONS = unsupported;

export async function GET(request: Request): Promise<Response> {
  if (request.method !== "GET") return unsupported();
  if (
    request.url.includes("?") ||
    request.body !== null ||
    Number(request.headers.get("content-length")) > 0 ||
    request.headers.has("transfer-encoding")
  )
    return new Response(null, { status: 400, headers });
  const signal = AbortSignal.any([request.signal, AbortSignal.timeout(5_000)]);
  let abort: (() => void) | undefined;
  const expired = new Promise<never>((_, reject) => {
    abort = () => reject(new DOMException("readiness_aborted", "AbortError"));
    if (signal.aborted) abort();
    else signal.addEventListener("abort", abort, { once: true });
  });
  try {
    const response = await Promise.race([
      fetch(backendReadinessUrl(), {
        cache: "no-store",
        redirect: "error",
        signal,
        headers: { Accept: "application/json" },
      }),
      expired,
    ]);
    if (
      !/^application\/json(?:;|$)/i.test(
        response.headers.get("content-type") ?? "",
      )
    )
      throw new TypeError("readiness_content_type_invalid");
    const body: unknown = await Promise.race([response.json(), expired]);
    if (!isReadiness(body, response.status))
      throw new TypeError("readiness_contract_invalid");
    return Response.json(body, { status: response.status, headers });
  } catch (error) {
    if (!(
      error instanceof TypeError ||
      error instanceof SyntaxError ||
      error instanceof DOMException
    ))
      throw error;
    const correlationId = randomBytes(16).toString("hex");
    console.error(
      JSON.stringify({
        eventCode: "backend_unavailable",
        component: "host",
        correlationId,
      }),
    );
    return Response.json(
      { version: "1", state: "unavailable", component: "host", correlationId },
      { status: 503, headers },
    );
  } finally {
    if (abort) signal.removeEventListener("abort", abort);
  }
}
