import { expect, it } from "vitest";
import { isReadiness, isStatus } from "@kofte/bootstrap-contract";

const host = process.env.KOFTE_HOST_URL;
if (!host) throw new Error("isolated_test_host_required");

it("observes actual live and ready responses without cached or extra fields", async () => {
  const live = await fetch(new URL("/health/live", host));
  expect(live.status).toBe(200);
  expect(live.headers.get("cache-control")).toBe("no-store");
  expect(isStatus(await live.json())).toBe(true);
  const ready = await fetch(new URL("/health/ready", host));
  expect(ready.status).toBe(200);
  expect(ready.headers.get("cache-control")).toBe("no-store");
  expect(isReadiness(await ready.json(), ready.status)).toBe(true);
});

it.each([
  ["/Health/Ready", "GET", 404],
  ["/health/ready?credential=canary", "GET", 400],
  ["/health/ready", "POST", 405],
  ["/mcp", "GET", 404],
  ["/admin", "GET", 404],
])(
  "rejects unsupported anonymous access without reflecting input",
  async (path, method, status) => {
    const response = await fetch(new URL(path, host), { method });
    expect(response.status).toBe(status);
    expect(await response.text()).toBe("");
  },
);
