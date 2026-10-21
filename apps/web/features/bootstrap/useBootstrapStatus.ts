"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { isReadiness } from "@kofte/bootstrap-contract";

export type ReadinessView = {
  kind: "loading" | "ready" | "unavailable" | "stale";
  correlationId?: string;
};

export function useBootstrapStatus() {
  const [state, setState] = useState<ReadinessView>({ kind: "loading" });
  const [checking, setChecking] = useState(false);
  const attempt = useRef(0);
  const active = useRef<AbortController | null>(null);
  const deadlineTimer = useRef<ReturnType<typeof setTimeout> | undefined>(
    undefined,
  );
  const refreshTimer = useRef<ReturnType<typeof setTimeout> | undefined>(
    undefined,
  );

  const cancel = useCallback(() => {
    ++attempt.current;
    active.current?.abort();
    active.current = null;
    clearTimeout(deadlineTimer.current);
    clearTimeout(refreshTimer.current);
  }, []);

  const retry = useCallback(function check() {
    if (active.current || document.hidden) return;
    clearTimeout(refreshTimer.current);
    refreshTimer.current = setTimeout(check, 30_000);
    const id = ++attempt.current;
    const controller = new AbortController();
    active.current = controller;
    setChecking(true);
    const deadline = setTimeout(() => {
      if (attempt.current !== id) return;
      controller.abort();
      active.current = null;
      ++attempt.current;
      setState({ kind: "unavailable" });
      setChecking(false);
    }, 10_000);
    deadlineTimer.current = deadline;
    void (async () => {
      try {
        const response = await fetch("/backend/health/ready", {
          cache: "no-store",
          redirect: "error",
          signal: controller.signal,
          headers: { Accept: "application/json" },
        });
        if (
          !/^application\/json(?:;|$)/i.test(
            response.headers.get("content-type") ?? "",
          )
        )
          throw new TypeError("readiness_content_type_invalid");
        const body: unknown = await response.json();
        if (!isReadiness(body, response.status))
          throw new TypeError("readiness_contract_invalid");
        if (attempt.current === id)
          setState({
            kind: body.state === "ready" ? "ready" : "unavailable",
            correlationId: body.correlationId,
          });
      } catch (error) {
        if (!(
          error instanceof TypeError ||
          error instanceof SyntaxError ||
          error instanceof DOMException
        ))
          throw error;
        if (attempt.current === id) setState({ kind: "unavailable" });
      } finally {
        clearTimeout(deadline);
        if (attempt.current === id) {
          active.current = null;
          setChecking(false);
        }
      }
    })();
  }, []);

  useEffect(() => {
    retry();
    const visibility = () => {
      if (document.hidden) {
        cancel();
        setChecking(false);
        setState({ kind: "stale" });
      } else retry();
    };
    const focus = () => {
      if (!document.hidden) retry();
    };
    const blur = () => {
      cancel();
      setChecking(false);
      setState({ kind: "stale" });
    };
    document.addEventListener("visibilitychange", visibility);
    window.addEventListener("focus", focus);
    window.addEventListener("blur", blur);
    return () => {
      cancel();
      document.removeEventListener("visibilitychange", visibility);
      window.removeEventListener("focus", focus);
      window.removeEventListener("blur", blur);
    };
  }, [retry, cancel]);

  return { state, checking, retry };
}
