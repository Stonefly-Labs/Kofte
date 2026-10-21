import { expect, it } from "vitest";
import { isReadiness, isStatus } from "@kofte/bootstrap-contract";

const host = process.env.KOFTE_HOST_URL;
const web = process.env.KOFTE_WEB_URL;
const canary = process.env.BOOTSTRAP_PRIVACY_CANARY;
if (!host || !web || !canary) throw new Error("owned_privacy_runtime_required");

it("does not reflect credential-like headers, malformed queries or paths", async () => {
  for (const [origin, path, status] of [
    [host, "/health/ready", 200],
    [web, "/backend/health/ready", 200],
    [host, `/health/live?password=${encodeURIComponent(canary)}`, 400],
    [web, `/backend/health/ready?password=${encodeURIComponent(canary)}`, 400],
    [host, `/admin/${encodeURIComponent(canary)}`, 404],
  ] as const) {
    const response = await fetch(new URL(path, origin), {
      headers: {
        Authorization: `Bearer ${canary}`,
        Cookie: `password=${canary}`,
        traceparent: canary,
        baggage: `password=${canary}`,
      },
    });
    expect(response.status).toBe(status);
    const text = await response.text();
    expect(text.includes(canary)).toBe(false);
    expect(
      /(?:\/Users\/|\/private\/tmp\/|\/home\/runner\/|System\.|Npgsql\.| at [^\n]+:\d+)/.test(
        text,
      ),
    ).toBe(false);
    if (status === 200) {
      const payload: unknown = JSON.parse(text);
      expect(
        origin === web ? isReadiness(payload, status) : isStatus(payload),
      ).toBe(true);
    } else expect(text).toBe("");
  }
});
