import { expect, it } from "vitest";
import { isCurrentIdentity } from "./identity";

const current = {
  version: "1",
  userId: "00000000-0000-4000-8000-000000000001",
  displayName: null,
  identityMode: "oidc",
  isInitialAdministrator: false,
};

it("accepts required self identity and additive response fields without manufacturing defaults", () => {
  expect(isCurrentIdentity(current)).toBe(true);
  expect(
    isCurrentIdentity({
      ...current,
      displayName: "🍽️".repeat(64),
      identityMode: "development",
      futureField: true,
    }),
  ).toBe(true);
  for (const field of Object.keys(current)) {
    const incomplete: Record<string, unknown> = { ...current };
    delete incomplete[field];
    expect(isCurrentIdentity(incomplete)).toBe(false);
  }
});

it("rejects malformed or ambiguous identity rather than retaining stale signed-in state", () => {
  for (const value of [
    null,
    [],
    { ...current, version: "2" },
    { ...current, userId: "00000000-0000-0000-0000-000000000000" },
    {
      ...current,
      userId: current.userId.toUpperCase().replace("4000", "FFFF"),
    },
    { ...current, displayName: "x".repeat(129) },
    { ...current, displayName: "unsafe\nname" },
    { ...current, displayName: "\ud800" },
    { ...current, identityMode: "provider" },
    { ...current, isInitialAdministrator: "false" },
  ])
    expect(isCurrentIdentity(value)).toBe(false);
});
