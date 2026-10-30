import { forwardIdentity } from "../../../../../features/identity/identityAdapter";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";
export const POST = (request: Request) =>
  forwardIdentity(request, "development");
export const GET = POST;
export const HEAD = POST;
export const PUT = POST;
export const PATCH = POST;
export const DELETE = POST;
export const OPTIONS = POST;
