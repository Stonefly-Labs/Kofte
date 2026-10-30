"use server";

import { cookies, headers } from "next/headers";
import { redirect } from "next/navigation";
import { NextResponse } from "next/server";
import { forwardIdentity } from "./identityAdapter";

export type LoginStartFailure = {
  kind: "failed" | "uncertain";
  correlationId?: string;
};

export async function startLogin(input: unknown): Promise<LoginStartFailure> {
  if (
    typeof input !== "object" ||
    input === null ||
    Array.isArray(input) ||
    Object.keys(input).length !== 2 ||
    !("returnPath" in input) ||
    !("csrfToken" in input) ||
    typeof input.returnPath !== "string" ||
    typeof input.csrfToken !== "string" ||
    !["/", "/identity"].includes(input.returnPath) ||
    !/^[A-Za-z0-9_-]{1,2048}$/.test(input.csrfToken)
  )
    return { kind: "failed" };

  const incoming = await headers();
  const origin = incoming.get("origin");
  if (origin === null || origin === "null") return { kind: "failed" };
  let url: URL;
  try {
    url = new URL("/auth/login", origin);
  } catch (error) {
    if (!(error instanceof TypeError)) throw error;
    return { kind: "failed" };
  }
  const admitted = new Headers({
    "Content-Type": "application/x-www-form-urlencoded",
    Origin: origin,
  });
  for (const name of ["cookie", "authorization", "sec-fetch-site"]) {
    const value = incoming.get(name);
    if (value !== null) admitted.set(name, value);
  }
  const response = await forwardIdentity(
    new Request(url, {
      method: "POST",
      headers: admitted,
      body: new URLSearchParams({
        returnPath: input.returnPath,
        __RequestVerificationToken: input.csrfToken,
      }),
    }),
    "login",
  );
  const parsed = new NextResponse(null, { headers: response.headers });
  if (parsed.cookies.getAll().length > 0) {
    const outgoing = await cookies();
    for (const cookie of parsed.cookies.getAll()) outgoing.set(cookie);
  }
  if (response.status !== 303) {
    let reference: unknown;
    if (
      response.headers
        .get("content-type")
        ?.startsWith("application/problem+json")
    ) {
      const problem: unknown = await response.json();
      if (
        typeof problem === "object" &&
        problem !== null &&
        "correlationId" in problem
      )
        reference = problem.correlationId;
    }
    return {
      kind: response.status === 503 ? "uncertain" : "failed",
      ...(typeof reference === "string" && /^[0-9a-f]{32}$/.test(reference)
        ? { correlationId: reference }
        : {}),
    };
  }
  const location = response.headers.get("location");
  if (location === null) throw new TypeError("identity_redirect_missing");
  redirect(location);
}
