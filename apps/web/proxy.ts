import { NextResponse, type NextRequest } from "next/server";

export function proxy(request: NextRequest) {
  const path = request.nextUrl.pathname;
  if (
    (path.toLowerCase().startsWith("/api/identity/") &&
      (!path.startsWith("/api/identity/") || path.endsWith("/"))) ||
    (path.toLowerCase().startsWith("/auth/") &&
      path !== "/auth/login" &&
      path !== "/auth/oidc/callback")
  )
    return new Response(null, {
      status: 404,
      headers: {
        "Cache-Control": "no-store",
        Pragma: "no-cache",
        "Referrer-Policy": "no-referrer",
      },
    });
  return NextResponse.next();
}

export const config = {
  matcher: ["/([aA][pP][iI])/:path*", "/([aA][uU][tT][hH])/:path*"],
};
