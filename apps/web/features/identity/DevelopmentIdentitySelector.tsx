"use client";

import { useId, useState } from "react";
import { Button } from "../../components/ui/button";
import type { DevelopmentIdentityChoice } from "./identityConfiguration";

export function DevelopmentIdentitySelector({
  choices,
  onSelect,
}: {
  choices: readonly DevelopmentIdentityChoice[];
  onSelect: (identityKey: string) => void;
}) {
  const id = useId();
  const [key, setKey] = useState("");
  if (choices.length === 0)
    return (
      <p role="alert">
        Configured Development identities are unavailable. Correct the
        configuration and reload.
      </p>
    );
  return (
    <form
      method="post"
      action="/api/identity/v1/development-session"
      onSubmit={(event) => {
        event.preventDefault();
        if (choices.some((choice) => choice.key === key)) onSelect(key);
      }}
    >
      <label htmlFor={id}>Configured Development identity</label>
      <select
        id={id}
        name="identityKey"
        required
        value={key}
        className="identity-selector"
        onChange={(event) => setKey(event.currentTarget.value)}
      >
        <option value="">Choose a configured identity</option>
        {choices.map((choice) => (
          <option key={choice.key} value={choice.key}>
            {choice.displayName
              ? `${choice.displayName} (${choice.key})`
              : choice.key}
          </option>
        ))}
      </select>
      <Button
        type="submit"
        disabled={!choices.some((choice) => choice.key === key)}
      >
        Use Development identity
      </Button>
    </form>
  );
}
