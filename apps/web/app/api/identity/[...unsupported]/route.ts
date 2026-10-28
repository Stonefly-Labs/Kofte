export const dynamic = "force-dynamic";
export const runtime = "nodejs";
export const GET = () =>
  new Response(null, {
    status: 404,
    headers: {
      "Cache-Control": "no-store",
      Pragma: "no-cache",
      "Referrer-Policy": "no-referrer",
    },
  });
export const POST = GET;
export const HEAD = GET;
export const PUT = GET;
export const PATCH = GET;
export const DELETE = GET;
export const OPTIONS = GET;
