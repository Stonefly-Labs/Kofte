import { forwardIdentity } from "../../../../features/identity/identityAdapter";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";
export const GET = (request: Request) => forwardIdentity(request, "callback");
export const POST = GET;
export const HEAD = GET;
export const PUT = GET;
export const PATCH = GET;
export const DELETE = GET;
export const OPTIONS = GET;
