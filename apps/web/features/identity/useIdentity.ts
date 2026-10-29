"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import {
  isCurrentIdentity,
  type CurrentIdentityV1,
} from "@kofte/bootstrap-contract/identity";

type BrowserContext = {
  version: "1";
  identityMode: "oidc" | "development";
  csrfToken: string;
};
type LoginNotice = {
  outcome: "cancelled" | "failed" | "unavailable";
  correlationId?: string;
};
type SignOutNotice = {
  kind: "pending" | "confirmed" | "expired" | "uncertain";
  correlationId?: string;
};
type DevelopmentNotice = {
  kind: "pending" | "selected" | "denied" | "uncertain";
  correlationId?: string;
};
export type IdentityView =
  | { kind: "loading" }
  | { kind: "unavailable"; correlationId?: string }
  | {
      kind: "signed-out" | "expired";
      context: BrowserContext;
      notice?: LoginNotice;
    }
  | {
      kind: "signed-in";
      context: BrowserContext;
      identity: CurrentIdentityV1;
    };

function problemReference(value: unknown, status: number): string | undefined {
  if (typeof value !== "object" || value === null || Array.isArray(value))
    return undefined;
  const body: Record<string, unknown> = Object.fromEntries(
    Object.entries(value),
  );
  const codes: Readonly<Record<number, readonly string[]>> = {
    400: ["identity_request_invalid"],
    401: ["identity_unauthenticated"],
    403: ["identity_forbidden"],
    409: ["identity_enrollment_unavailable"],
    429: ["identity_admission_exceeded"],
    503: ["identity_unavailable", "identity_outcome_uncertain"],
  };
  if (
    body.status === status &&
    typeof body.code === "string" &&
    codes[status]?.includes(body.code) &&
    body.type === `urn:kofte:problem:${body.code}` &&
    typeof body.correlationId === "string" &&
    /^[a-f0-9]{32}$/.test(body.correlationId)
  )
    return body.correlationId;
  return undefined;
}

function isContext(value: unknown): value is BrowserContext {
  if (typeof value !== "object" || value === null || Array.isArray(value))
    return false;
  const object: Record<string, unknown> = Object.fromEntries(
    Object.entries(value),
  );
  return (
    object.version === "1" &&
    (object.identityMode === "oidc" || object.identityMode === "development") &&
    typeof object.csrfToken === "string" &&
    /^[A-Za-z0-9_-]{1,2048}$/.test(object.csrfToken)
  );
}

function loginNotice(): LoginNotice | undefined {
  const query = new URLSearchParams(window.location.search);
  const outcome = query.get("outcome");
  if (
    query.getAll("outcome").length !== 1 ||
    (outcome !== "cancelled" &&
      outcome !== "failed" &&
      outcome !== "unavailable")
  )
    return undefined;
  const correlation = query.get("correlationId");
  return {
    outcome,
    ...(correlation &&
    /^[a-f0-9]{32}$/.test(correlation) &&
    query.getAll("correlationId").length === 1
      ? { correlationId: correlation }
      : {}),
  };
}

export async function readIdentityJson(response: Response): Promise<unknown> {
  if (
    !/^application\/(?:json|problem\+json)(?:;|$)/i.test(
      response.headers.get("content-type") ?? "",
    ) ||
    !response.body
  )
    throw new TypeError("identity_response_invalid");
  const reader = response.body.getReader();
  const chunks: Uint8Array[] = [];
  let length = 0;
  try {
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;
      length += value.byteLength;
      if (length > 8192) throw new TypeError("identity_response_invalid");
      chunks.push(value);
    }
  } finally {
    reader.releaseLock();
  }
  const bytes = new Uint8Array(length);
  let offset = 0;
  for (const chunk of chunks) {
    bytes.set(chunk, offset);
    offset += chunk.byteLength;
  }
  return JSON.parse(new TextDecoder("utf-8", { fatal: true }).decode(bytes));
}

export function useIdentity() {
  const [state, setState] = useState<IdentityView>({ kind: "loading" });
  const [signOutNotice, setSignOutNotice] = useState<SignOutNotice>();
  const [developmentNotice, setDevelopmentNotice] =
    useState<DevelopmentNotice>();
  const active = useRef<AbortController | null>(null);
  const mutationController = useRef<AbortController | null>(null);
  const locallyCleared = useRef(false);
  const mounted = useRef(false);
  const attempt = useRef(0);
  const established = useRef(false);
  const deadline = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);
  const refresh = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  const cancel = useCallback(() => {
    ++attempt.current;
    active.current?.abort();
    active.current = null;
    clearTimeout(deadline.current);
    clearTimeout(refresh.current);
  }, []);

  const retry = useCallback(function check() {
    if (active.current || mutationController.current || document.hidden) return;
    clearTimeout(refresh.current);
    const id = ++attempt.current;
    const controller = new AbortController();
    active.current = controller;
    setState({ kind: "loading" });
    deadline.current = setTimeout(() => {
      if (attempt.current !== id) return;
      ++attempt.current;
      controller.abort();
      active.current = null;
      setState({ kind: "unavailable" });
    }, 4500);
    void (async () => {
      let correlationId: string | undefined;
      try {
        const [contextResponse, identityResponse] = await Promise.all(
          [
            "/api/identity/v1/browser-context",
            ...(locallyCleared.current ? [] : ["/api/identity/v1/me"]),
          ].map((path) =>
            fetch(path, {
              cache: "no-store",
              redirect: "error",
              signal: controller.signal,
              headers: { Accept: "application/json" },
            }),
          ),
        );
        const context = await readIdentityJson(contextResponse);
        if (contextResponse.status !== 200 || !isContext(context)) {
          correlationId = problemReference(context, contextResponse.status);
          throw new TypeError("identity_context_invalid");
        }
        let next: IdentityView;
        if (!identityResponse || identityResponse.status === 401) {
          if (identityResponse) await readIdentityJson(identityResponse);
          next = {
            kind: established.current ? "expired" : "signed-out",
            context,
            notice: loginNotice(),
          };
        } else {
          const identity = await readIdentityJson(identityResponse);
          if (
            identityResponse.status !== 200 ||
            !isCurrentIdentity(identity) ||
            identity.identityMode !== context.identityMode
          ) {
            correlationId = problemReference(identity, identityResponse.status);
            throw new TypeError("identity_response_invalid");
          }
          next = { kind: "signed-in", context, identity };
        }
        if (attempt.current === id) {
          established.current ||= next.kind === "signed-in";
          setState(next);
          refresh.current = setTimeout(check, 30_000);
        }
      } catch (error) {
        if (!(
          error instanceof TypeError ||
          error instanceof SyntaxError ||
          error instanceof DOMException
        ))
          throw error;
        if (attempt.current === id) {
          controller.abort();
          setState({ kind: "unavailable", correlationId });
        }
      } finally {
        if (attempt.current === id) {
          clearTimeout(deadline.current);
          active.current = null;
        }
      }
    })();
  }, []);

  useEffect(() => {
    mounted.current = true;
    retry();
    const suspend = () => {
      cancel();
      setState({ kind: "loading" });
    };
    const visibility = () => (document.hidden ? suspend() : retry());
    const focus = () => retry();
    window.addEventListener("blur", suspend);
    window.addEventListener("focus", focus);
    document.addEventListener("visibilitychange", visibility);
    return () => {
      mounted.current = false;
      mutationController.current?.abort();
      cancel();
      window.removeEventListener("blur", suspend);
      window.removeEventListener("focus", focus);
      document.removeEventListener("visibilitychange", visibility);
    };
  }, [cancel, retry]);

  const refreshIdentity = useCallback(() => {
    cancel();
    retry();
  }, [cancel, retry]);

  const mutate = useCallback(
    async (kind: "signOut" | "development", identityKey?: string) => {
      if (
        !("context" in state) ||
        mutationController.current ||
        (kind === "signOut" && state.kind !== "signed-in") ||
        (kind === "development" && state.context.identityMode !== "development")
      )
        return;
      cancel();
      locallyCleared.current = true;
      established.current = false;
      setState({ kind: "loading" });
      setSignOutNotice(kind === "signOut" ? { kind: "pending" } : undefined);
      setDevelopmentNotice(
        kind === "development" ? { kind: "pending" } : undefined,
      );
      const controller = new AbortController();
      mutationController.current = controller;
      let abort: (() => void) | undefined;
      const interrupted = new Promise<never>((_, reject) => {
        abort = () =>
          reject(new DOMException("identity_mutation_aborted", "AbortError"));
        controller.signal.addEventListener("abort", abort, { once: true });
      });
      const timeout = setTimeout(() => controller.abort(), 4500);
      try {
        const response = await Promise.race([
          fetch(
            kind === "signOut"
              ? "/api/identity/v1/sign-out"
              : "/api/identity/v1/development-session",
            {
              method: "POST",
              headers: {
                "X-Kofte-CSRF": state.context.csrfToken,
                ...(kind === "development"
                  ? { "Content-Type": "application/json" }
                  : {}),
              },
              ...(kind === "development"
                ? { body: JSON.stringify({ identityKey }) }
                : {}),
              cache: "no-store",
              redirect: "error",
              signal: controller.signal,
            },
          ),
          interrupted,
        ]);
        if (!mounted.current) return;
        if (kind === "signOut" && response.status === 204)
          setSignOutNotice({ kind: "confirmed" });
        else if (kind === "signOut" && response.status === 401) {
          await Promise.race([response.body?.cancel(), interrupted]);
          setSignOutNotice({ kind: "expired" });
        } else {
          const value = await Promise.race([
            readIdentityJson(response),
            interrupted,
          ]);
          if (!mounted.current) return;
          if (kind === "development") {
            if (
              response.status === 200 &&
              isCurrentIdentity(value) &&
              value.identityMode === "development" &&
              !value.isInitialAdministrator
            ) {
              locallyCleared.current = false;
              setDevelopmentNotice({ kind: "selected" });
            } else {
              const correlationId = problemReference(value, response.status);
              setDevelopmentNotice({
                kind:
                  correlationId &&
                  [400, 401, 403, 429].includes(response.status)
                    ? "denied"
                    : "uncertain",
                correlationId,
              });
            }
          } else
            setSignOutNotice({
              kind: "uncertain",
              correlationId: problemReference(value, response.status),
            });
        }
      } catch (error) {
        if (!(
          error instanceof TypeError ||
          error instanceof SyntaxError ||
          error instanceof DOMException
        ))
          throw error;
        if (mounted.current) {
          if (kind === "development")
            setDevelopmentNotice({ kind: "uncertain" });
          else setSignOutNotice({ kind: "uncertain" });
        }
      } finally {
        clearTimeout(timeout);
        if (abort) controller.signal.removeEventListener("abort", abort);
        mutationController.current = null;
        if (mounted.current) retry();
      }
    },
    [state, cancel, retry],
  );

  return {
    state,
    retry,
    refreshIdentity,
    signOutNotice,
    developmentNotice,
    signOut: () => mutate("signOut"),
    selectDevelopment: (identityKey: string) =>
      mutate("development", identityKey),
  };
}
