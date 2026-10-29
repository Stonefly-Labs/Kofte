import { forwardIdentity } from "../../../../features/identity/identityAdapter";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";
export async function GET(request: Request) {
  const response = await forwardIdentity(request, "callback");
  if (
    request.method !== "GET" ||
    request.headers.get("sec-fetch-mode") !== "navigate" ||
    request.headers.get("sec-fetch-dest") !== "document" ||
    !request.headers
      .get("accept")
      ?.split(",")
      .some((type) => type.trim().split(";")[0] === "text/html") ||
    response.status < 400 ||
    response.headers.get("content-type")?.split(";")[0] !==
      "application/problem+json"
  )
    return response;

  // The adapter has already reduced the upstream failure to its fixed safe contract.
  const problem: unknown = await response.clone().json();
  if (
    typeof problem !== "object" ||
    problem === null ||
    !("correlationId" in problem) ||
    typeof problem.correlationId !== "string" ||
    !/^[0-9a-f]{32}$/.test(problem.correlationId)
  )
    return response;
  const query = new URL(request.url).searchParams;
  const cancelled =
    response.status === 401 &&
    query.getAll("error").length === 1 &&
    query.get("error") === "access_denied";
  const outcome =
    response.status === 503
      ? "unavailable"
      : cancelled
        ? "cancelled"
        : "failed";
  const headers = new Headers(response.headers);
  headers.set(
    "Location",
    `/identity?outcome=${outcome}&correlationId=${problem.correlationId}`,
  );
  headers.set("Content-Type", "text/plain; charset=utf-8");
  await response.body?.cancel();
  return new Response(null, { status: 303, headers });
}
export const POST = GET;
export const HEAD = GET;
export const PUT = GET;
export const PATCH = GET;
export const DELETE = GET;
export const OPTIONS = GET;
