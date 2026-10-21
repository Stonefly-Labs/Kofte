import { describe, expect, it } from "vitest";
import { isStatus, isReadiness } from "./status";

const ready = {
  version: "1",
  state: "ready",
  component: "storage",
  correlationId: "a".repeat(32),
};

describe("bootstrap runtime contract", () => {
  it("accepts only exact combinations, fields and correlations", () => {
    expect(isReadiness(ready, 200)).toBe(true);
    expect(isReadiness({ ...ready, state: "unavailable" }, 503)).toBe(true);
    for (const invalid of [
      null,
      [],
      {},
      { ...ready, secret: "canary" },
      { ...ready, version: "2" },
      { ...ready, component: "host" },
      { ...ready, correlationId: "arbitrary-header" },
      { ...ready, correlationId: "A".repeat(32) },
    ])
      expect(isStatus(invalid)).toBe(false);
  });
  it("rejects status/body mismatches and liveness as readiness", () => {
    expect(isReadiness(ready, 503)).toBe(false);
    expect(
      isReadiness({ ...ready, state: "live", component: "host" }, 200),
    ).toBe(false);
  });
});
