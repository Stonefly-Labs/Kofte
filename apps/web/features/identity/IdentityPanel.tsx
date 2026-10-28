"use client";

import { useEffect, useRef, useState, useTransition } from "react";
import { Button } from "../../components/ui/button";
import { startLogin, type LoginStartFailure } from "./startLoginAction";
import { useIdentity } from "./useIdentity";
import { EnrollmentForm, type EnrollmentNotice } from "./EnrollmentForm";

export function IdentityPanel({
  returnPath = "/identity",
}: {
  returnPath?: string;
}) {
  const { state, retry, refreshIdentity, signOut, signOutNotice } =
    useIdentity();
  const [enrollmentNotice, setEnrollmentNotice] = useState<EnrollmentNotice>();
  const [starting, startTransition] = useTransition();
  const [loginFailure, setLoginFailure] = useState<LoginStartFailure>();
  const heading = useRef<HTMLHeadingElement>(null);
  const identityFocus = useRef(false);
  useEffect(() => {
    if (identityFocus.current && state.kind !== "loading") {
      identityFocus.current = false;
      heading.current?.focus();
    }
  }, [state]);
  const validReturn = returnPath === "/" || returnPath === "/identity";
  const label = !validReturn
    ? "Identity navigation is unavailable"
    : signOutNotice?.kind === "uncertain"
      ? "Your local identity has been cleared"
      : {
          loading: "Checking your identity",
          "signed-out": "You are signed out",
          "signed-in": "You are signed in",
          expired: "Your session has expired",
          unavailable: "Identity verification is unavailable",
        }[state.kind];
  const context = "context" in state ? state.context : undefined;
  return (
    <section className="panel" aria-labelledby="identity-heading">
      <h2 id="identity-heading" ref={heading} tabIndex={-1}>
        Your identity
      </h2>
      <p role="status" aria-live="polite" aria-atomic="true">
        {label}
      </p>
      {context?.identityMode === "development" && (
        <p>
          <strong>Development identity</strong> — local use only. This is not
          production authentication.
        </p>
      )}
      {signOutNotice && (
        <p role={signOutNotice.kind === "uncertain" ? "alert" : "status"}>
          {signOutNotice.kind === "pending"
            ? "Waiting for sign-out confirmation. No identity is being assumed."
            : signOutNotice.kind === "confirmed"
              ? "Signed out of Köfte. This does not sign you out of your identity provider."
              : signOutNotice.kind === "expired"
                ? "Your session is no longer valid. Sign in again to continue."
                : "Server revocation could not be confirmed. Your local identity has been cleared. Sign in again to continue; no automatic sign-out retry was made."}
          {signOutNotice.correlationId && (
            <> Reference: {signOutNotice.correlationId}</>
          )}
        </p>
      )}
      {enrollmentNotice && (
        <p role={enrollmentNotice.kind === "enrolled" ? "status" : "alert"}>
          {enrollmentNotice.kind === "enrolled"
            ? "Enrollment was confirmed. The designation is being checked through your current identity."
            : enrollmentNotice.kind === "expired"
              ? "Your session must be verified again before enrollment."
              : "Enrollment could not be confirmed. No additional enrollment was attempted. Check your current identity before retrying."}
          {enrollmentNotice.correlationId && (
            <> Reference: {enrollmentNotice.correlationId}</>
          )}
        </p>
      )}
      {state.kind === "signed-in" && (
        <>
          <dl>
            <dt>Name</dt>
            <dd>{state.identity.displayName ?? "No display name provided"}</dd>
            <dt>Köfte user</dt>
            <dd>{state.identity.userId}</dd>
          </dl>
          <p>
            {state.identity.isInitialAdministrator
              ? "Initial administrator designated. This does not grant unrelated permissions."
              : "Signing in does not grant administrator, team or resource permissions."}
          </p>
          {state.identity.identityMode === "oidc" &&
            !state.identity.isInitialAdministrator && (
              <EnrollmentForm
                csrfToken={state.context.csrfToken}
                onRefresh={(notice) => {
                  identityFocus.current = true;
                  setEnrollmentNotice(notice);
                  refreshIdentity();
                }}
              />
            )}
          <Button
            type="button"
            onClick={() => {
              identityFocus.current = true;
              setEnrollmentNotice(undefined);
              setLoginFailure(undefined);
              void signOut();
            }}
          >
            Sign out
          </Button>
        </>
      )}
      {(state.kind === "signed-out" || state.kind === "expired") && (
        <>
          {state.notice && (
            <p role="alert">
              {state.notice.cancelled
                ? "Sign-in was cancelled. You can try again."
                : "Sign-in could not be completed. Correct the cause, then try again."}
              {state.notice.correlationId && (
                <> Reference: {state.notice.correlationId}</>
              )}
            </p>
          )}
          {validReturn && context?.identityMode === "oidc" && (
            <form
              action="/auth/login"
              method="post"
              onSubmit={(event) => {
                event.preventDefault();
                if (starting) return;
                setLoginFailure(undefined);
                startTransition(async () => {
                  try {
                    setLoginFailure(
                      await startLogin({
                        returnPath,
                        csrfToken: context.csrfToken,
                      }),
                    );
                  } catch (error) {
                    if (
                      !(error instanceof TypeError) &&
                      !(error instanceof DOMException)
                    )
                      throw error;
                    setLoginFailure({ kind: "uncertain" });
                  }
                });
              }}
            >
              <input type="hidden" name="returnPath" value={returnPath} />
              <input
                type="hidden"
                name="__RequestVerificationToken"
                value={context.csrfToken}
              />
              <p>
                Continue through your installation&apos;s configured identity
                provider.
              </p>
              {loginFailure && (
                <p role="alert">
                  {loginFailure.kind === "uncertain"
                    ? "The sign-in outcome could not be confirmed. No identity is being assumed."
                    : "Sign-in could not be started. Correct the cause and retry the identity check."}
                  {loginFailure.correlationId && (
                    <> Reference: {loginFailure.correlationId}</>
                  )}
                </p>
              )}
              <Button type="submit" disabled={starting}>
                {starting ? "Starting sign-in…" : "Sign in"}
              </Button>
            </form>
          )}
        </>
      )}
      {state.kind === "unavailable" && (
        <>
          <p>
            No identity is being assumed. Correct the connection or
            configuration, then retry.
          </p>
          {state.correlationId && (
            <p className="correlation">Reference: {state.correlationId}</p>
          )}
        </>
      )}
      <Button
        type="button"
        onClick={retry}
        aria-label="Retry identity check"
        aria-disabled={state.kind === "loading"}
      >
        Retry identity check
      </Button>
    </section>
  );
}
