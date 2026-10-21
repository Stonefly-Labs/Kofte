"use client";

import { CheckCircle2, CircleHelp, Unplug } from "lucide-react";
import { Button } from "../../components/ui/button";
import { useBootstrapStatus } from "./useBootstrapStatus";

export function BootstrapStatus() {
  const { state, checking, retry } = useBootstrapStatus();
  const label = {
    loading: "Checking the local host",
    ready: "Local host is ready",
    unavailable: "Local host is unavailable",
    stale: "Readiness needs to be checked",
  }[state.kind];
  const Icon =
    state.kind === "ready"
      ? CheckCircle2
      : state.kind === "unavailable"
        ? Unplug
        : CircleHelp;
  return (
    <section className="panel" aria-labelledby="connection-heading">
      <h2 id="connection-heading">Your local connection</h2>
      <div
        className="status"
        role="status"
        aria-live="polite"
        aria-atomic="true"
      >
        <Icon size={22} aria-hidden="true" />
        <span>{label}</span>
      </div>
      <p>
        {state.kind === "ready"
          ? "The host can reach its configured platform store. This is a bootstrap connection check, not a production-readiness assessment."
          : "Make sure the local host is running and its platform store is available. Correct the cause, then retry. Retrying never resets your data."}
      </p>
      <Button
        onClick={retry}
        aria-disabled={checking}
        aria-label="Retry connection check"
      >
        Retry connection
      </Button>
      {state.kind === "unavailable" && state.correlationId && (
        <p className="correlation">Reference: {state.correlationId}</p>
      )}
    </section>
  );
}
