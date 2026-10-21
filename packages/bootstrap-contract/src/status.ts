import type { components } from "./generated";

export type BootstrapStatus =
  | components["schemas"]["LiveStatus"]
  | components["schemas"]["ReadyStatus"]
  | components["schemas"]["UnavailableStatus"];

export function isStatus(value: unknown): value is BootstrapStatus {
  if (typeof value !== "object" || value === null || Array.isArray(value))
    return false;
  const object: Record<string, unknown> = Object.fromEntries(
    Object.entries(value),
  );
  if (
    Object.keys(object).sort().join(",") !==
      "component,correlationId,state,version" ||
    object.version !== "1" ||
    typeof object.correlationId !== "string" ||
    !/^[0-9a-f]{32}$/.test(object.correlationId)
  )
    return false;
  return (
    (object.state === "live" && object.component === "host") ||
    (object.state === "ready" && object.component === "storage") ||
    (object.state === "unavailable" &&
      (object.component === "host" || object.component === "storage"))
  );
}

export function isReadiness(
  value: unknown,
  status: number,
): value is BootstrapStatus {
  return (
    isStatus(value) &&
    ((status === 200 && value.state === "ready") ||
      (status === 503 && value.state === "unavailable"))
  );
}
