"use client";

import { useState, useTransition } from "react";
import { Button } from "../../components/ui/button";
import { startLogin, type LoginStartFailure } from "./startLoginAction";
import { useIdentity } from "./useIdentity";

export function IdentityPanel({
  returnPath = "/identity",
}: {
  returnPath?: string;
}) {
  const { state, retry } = useIdentity();
  const [starting, startTransition] = useTransition();
  const [loginFailure, setLoginFailure] = useState<LoginStartFailure>();
  const validReturn = returnPath === "/" || returnPath === "/identity";
  const label = !validReturn
    ? "Identity navigation is unavailable"
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
      <h2 id="identity-heading">Your identity</h2>
      <p role="status" aria-live="polite" aria-atomic="true">
        {label}
      </p>
      {context?.identityMode === "development" && (
        <p>
          <strong>Development identity</strong> — local use only. This is not
          production authentication.
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
