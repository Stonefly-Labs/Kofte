"use client";

import { useEffect, useId, useRef, useState } from "react";
import { Button } from "../../components/ui/button";
import { readIdentityJson } from "./useIdentity";

export type EnrollmentNotice = {
  kind: "enrolled" | "expired" | "uncertain";
  correlationId?: string;
};

export function EnrollmentForm({
  csrfToken,
  onRefresh,
}: {
  csrfToken: string;
  onRefresh: (notice: EnrollmentNotice) => void;
}) {
  const id = useId();
  const submitting = useRef(false);
  const mounted = useRef(false);
  const active = useRef<AbortController | null>(null);
  const entry = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);
  const [failure, setFailure] = useState<{ correlationId?: string }>();

  useEffect(() => {
    mounted.current = true;
    return () => {
      mounted.current = false;
      active.current?.abort();
    };
  }, []);
  useEffect(() => {
    if (failure && !busy) entry.current?.focus();
  }, [busy, failure]);

  async function submit(form: HTMLFormElement) {
    if (submitting.current) return;
    const authorization = new FormData(form).get("authorization");
    form.reset();
    setFailure(undefined);
    if (
      typeof authorization !== "string" ||
      !/^[A-Za-z0-9_-]{42}[AEIMQUYcgkosw048]$/.test(authorization)
    ) {
      setFailure({});
      return;
    }
    submitting.current = true;
    setBusy(true);
    const controller = new AbortController();
    active.current = controller;
    try {
      const response = await fetch("/api/identity/v1/first-administrator", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "X-Kofte-CSRF": csrfToken,
        },
        body: JSON.stringify({ authorization }),
        cache: "no-store",
        redirect: "error",
        signal: AbortSignal.any([controller.signal, AbortSignal.timeout(4500)]),
      });
      const value = await readIdentityJson(response);
      if (!mounted.current) return;
      if (typeof value !== "object" || value === null || Array.isArray(value))
        throw new TypeError("identity_enrollment_response_invalid");
      const body: Record<string, unknown> = Object.fromEntries(
        Object.entries(value),
      );
      const correlationId =
        typeof body.correlationId === "string" &&
        /^[0-9a-f]{32}$/.test(body.correlationId)
          ? body.correlationId
          : undefined;
      if (
        response.status === 200 &&
        body.version === "1" &&
        body.status === "enrolled" &&
        correlationId
      ) {
        onRefresh({ kind: "enrolled", correlationId });
        return;
      }
      const codes = {
        400: "identity_request_invalid",
        401: "identity_unauthenticated",
        403: "identity_forbidden",
        409: "identity_enrollment_unavailable",
        429: "identity_admission_exceeded",
      } as const;
      if (response.status === 503)
        onRefresh({ kind: "uncertain", correlationId });
      else if (
        Object.hasOwn(codes, response.status) &&
        correlationId &&
        Object.entries(codes).some(
          ([status, code]) =>
            Number(status) === response.status &&
            body.code === code &&
            body.type === `urn:kofte:problem:${code}` &&
            body.status === response.status,
        )
      ) {
        if (response.status === 401)
          onRefresh({ kind: "expired", correlationId });
        else setFailure({ correlationId });
      } else throw new TypeError("identity_enrollment_response_invalid");
    } catch (error) {
      if (!(
        error instanceof TypeError ||
        error instanceof SyntaxError ||
        error instanceof DOMException
      ))
        throw error;
      if (mounted.current) onRefresh({ kind: "uncertain" });
    } finally {
      submitting.current = false;
      active.current = null;
      if (mounted.current) setBusy(false);
    }
  }

  return (
    <form
      method="post"
      action="/api/identity/v1/first-administrator"
      onSubmit={(event) => {
        event.preventDefault();
        void submit(event.currentTarget);
      }}
    >
      <h3>Operator-nominated enrollment</h3>
      <p id={`${id}-help`}>
        Only continue if your operator nominated your exact production identity
        and privately provided a one-time authorization. Signing in alone grants
        no designation.
      </p>
      <label htmlFor={id}>One-time authorization</label>
      <input
        id={id}
        ref={entry}
        name="authorization"
        type="password"
        autoComplete="off"
        required
        minLength={43}
        maxLength={43}
        spellCheck={false}
        aria-describedby={`${id}-help`}
        disabled={busy}
        className="identity-authorization"
      />
      {failure && (
        <p role="alert">
          Enrollment was not completed. Check the operator-provided
          authorization and retry explicitly.
          {failure.correlationId && <> Reference: {failure.correlationId}</>}
        </p>
      )}
      {busy && (
        <p role="status" aria-live="polite">
          Submitting enrollment once. Waiting for confirmation.
        </p>
      )}
      <Button type="submit" disabled={busy}>
        {busy ? "Confirming enrollment…" : "Enroll as initial administrator"}
      </Button>
    </form>
  );
}
