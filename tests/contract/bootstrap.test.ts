import { expect, it } from "vitest";
import { isReadiness, isStatus } from "@kofte/bootstrap-contract";
import { readFileSync } from "node:fs";
import { randomUUID } from "node:crypto";
import { Ajv2020 } from "ajv/dist/2020.js";
import { requestWithBody } from "./http";

const host = process.env.KOFTE_HOST_URL;
if (!host) throw new Error("isolated_test_host_required");
const baseline: unknown = JSON.parse(
  readFileSync(
    new URL("../../packages/bootstrap-contract/openapi.json", import.meta.url),
    "utf8",
  ),
);
function record(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}
if (!record(baseline)) throw new Error("openapi_baseline_invalid");
const schemas = new Ajv2020({ strict: false });
schemas.addSchema(baseline, "bootstrap");

it("observes actual live and ready responses without cached or extra fields", async () => {
  const live = await fetch(new URL("/health/live", host));
  expect(live.status).toBe(200);
  expect(live.headers.get("cache-control")).toBe("no-store");
  const liveness: unknown = await live.json();
  expect(isStatus(liveness)).toBe(true);
  expect(liveness).toMatchObject({ state: "live", component: "host" });
  const liveSchema = schemas.getSchema(
    "bootstrap#/components/schemas/LiveStatus",
  );
  if (!liveSchema) throw new Error("liveness_schema_missing");
  expect(liveSchema(liveness)).toBe(true);
  const ready = await fetch(new URL("/health/ready", host));
  expect(ready.status).toBe(200);
  expect(ready.headers.get("cache-control")).toBe("no-store");
  const readiness: unknown = await ready.json();
  expect(isReadiness(readiness, ready.status)).toBe(true);
  const readySchema = schemas.getSchema(
    "bootstrap#/components/schemas/ReadyStatus",
  );
  if (!readySchema) throw new Error("readiness_schema_missing");
  expect(readySchema(readiness)).toBe(true);
});

it("matches the complete native-generated OpenAPI baseline and anonymous policy", async () => {
  const response = await fetch(new URL("/openapi/v1.json", host));
  expect(response.status).toBe(200);
  expect(response.headers.get("cache-control")).toBe("no-store");
  const document: unknown = await response.json();
  if (!record(document)) throw new Error("runtime_openapi_invalid");
  expect(document.servers).toEqual([{ url: new URL(host).origin }]);
  document.servers = structuredClone(baseline.servers);
  // An absent root security list and an empty list both declare no requirement.
  document.security ??= [];
  expect(document).toEqual(baseline);
});

it.each([
  ["/Health/Ready", "GET", 404],
  ["/health/ready?credential=canary", "GET", 400],
  ["/health/ready", "POST", 405],
  ["/mcp", "GET", 404],
  ["/admin", "GET", 404],
  ["/test/reset", "POST", 404],
  ["/health/live", "HEAD", 405],
  ["/health/ready", "OPTIONS", 405],
  ["/openapi/v1.json", "POST", 405],
  ["/OpenApi/v1.json", "GET", 404],
])(
  "rejects unsupported anonymous access without reflecting input",
  async (path, method, status) => {
    const response = await fetch(new URL(path, host), { method });
    expect(response.status).toBe(status);
    expect(await response.text()).toBe("");
    expect(response.headers.get("cache-control")).toBe("no-store");
  },
);

it.each(["/health/live", "/health/ready", "/openapi/v1.json"])(
  "rejects a GET body at %s before reflecting credential-like input",
  async (path) => {
    const canary = `credential-canary-${randomUUID()}`;
    const observed = await requestWithBody(new URL(path, host), canary);
    expect(observed.status).toBe(400);
    expect(observed.body).toBe("");
    expect(observed.cacheControl).toBe("no-store");
  },
);

it("does not trust malformed correlation or credential headers", async () => {
  const canary = `credential-canary-${randomUUID()}`;
  const response = await fetch(new URL("/health/ready", host), {
    headers: {
      Authorization: `Bearer ${canary}`,
      Cookie: `credential=${canary}`,
      traceparent: canary,
      tracestate: `secret=${canary}`,
      baggage: `secret=${canary}`,
    },
  });
  expect(response.status).toBe(200);
  const text = await response.text();
  expect(text.includes(canary)).toBe(false);
  expect(isReadiness(JSON.parse(text), 200)).toBe(true);
});
