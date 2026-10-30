import type { components } from "./generated";

export type CurrentIdentityV1 = components["schemas"]["CurrentIdentityV1"];

export function isIdentityDisplayName(value: unknown): value is string | null {
  return (
    value === null ||
    (typeof value === "string" &&
      [...value].length <= 128 &&
      !/[\p{Cc}\p{Cs}]/u.test(value))
  );
}

export function isCurrentIdentity(value: unknown): value is CurrentIdentityV1 {
  if (typeof value !== "object" || value === null || Array.isArray(value))
    return false;
  const identity: Record<string, unknown> = Object.fromEntries(
    Object.entries(value),
  );
  return (
    identity.version === "1" &&
    typeof identity.userId === "string" &&
    /^(?!00000000-0000-0000-0000-000000000000$)[0-9a-f]{8}(-[0-9a-f]{4}){3}-[0-9a-f]{12}$/.test(
      identity.userId,
    ) &&
    isIdentityDisplayName(identity.displayName) &&
    (identity.identityMode === "oidc" ||
      identity.identityMode === "development") &&
    typeof identity.isInitialAdministrator === "boolean"
  );
}
