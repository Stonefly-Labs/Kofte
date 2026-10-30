import { constants } from "node:fs";
import { open, realpath } from "node:fs/promises";
import { isAbsolute, resolve } from "node:path";
import { isIdentityDisplayName } from "@kofte/bootstrap-contract/identity";
import {
  backendReadinessUrl,
  isLoopback,
} from "../bootstrap/backendConfiguration";

export interface DevelopmentIdentityChoice {
  key: string;
  displayName: string | null;
}

export interface IdentityAdapterConfiguration {
  backendOrigin: string;
  publicOrigin: string;
  mode: "oidc" | "development";
  authorizationEndpoint?: string;
  developmentIdentities?: readonly DevelopmentIdentityChoice[];
}

export async function identityConfiguration(
  environment: Readonly<Record<string, string | undefined>> = process.env,
): Promise<IdentityAdapterConfiguration> {
  try {
    const backendOrigin = backendReadinessUrl(environment).origin;
    if (
      !isLoopback(
        new URL(
          environment.KOFTE_HOST_URL ?? "http://127.0.0.1:5080",
        ).hostname.replace(/^\[|\]$/g, ""),
      )
    )
      throw new TypeError("identity_adapter_configuration_invalid");
    const path = environment.KOFTE_AUTH_CONFIG_FILE;
    if (
      !path ||
      !isAbsolute(path) ||
      resolve(path) !== path ||
      (await realpath(path)) !== path
    )
      throw new TypeError("identity_adapter_configuration_invalid");
    const file = await open(
      path,
      constants.O_RDONLY | constants.O_NOFOLLOW | constants.O_NONBLOCK,
    );
    let text: string;
    try {
      const stat = await file.stat();
      if (
        !stat.isFile() ||
        stat.uid !== process.getuid?.() ||
        (stat.mode & 0o077) !== 0 ||
        stat.size < 2 ||
        stat.size > 32768
      )
        throw new TypeError("identity_adapter_configuration_invalid");
      const bytes = new Uint8Array(32769);
      let length = 0;
      while (length < bytes.length) {
        const { bytesRead } = await file.read(
          bytes,
          length,
          bytes.length - length,
          null,
        );
        if (bytesRead === 0) break;
        length += bytesRead;
      }
      if (length > 32768)
        throw new TypeError("identity_adapter_configuration_invalid");
      text = new TextDecoder("utf-8", { fatal: true }).decode(
        bytes.subarray(0, length),
      );
    } finally {
      await file.close();
    }
    const value: unknown = JSON.parse(text);
    rejectDuplicateProperties(text);
    const root = object(value);
    const origin = new URL(string(root.publicOrigin));
    if (
      root.version !== "1" ||
      root.publicOrigin !== origin.origin ||
      (root.mode !== "oidc" && root.mode !== "development")
    )
      throw new TypeError("identity_adapter_configuration_invalid");
    if (root.mode === "oidc") {
      const provider = object(root.provider);
      const endpoint = new URL(string(provider.authorizationEndpoint));
      if (
        origin.protocol !== "https:" ||
        endpoint.protocol !== "https:" ||
        endpoint.username ||
        endpoint.password ||
        endpoint.hash ||
        /\s/.test(string(provider.authorizationEndpoint))
      )
        throw new TypeError("identity_adapter_configuration_invalid");
      return {
        backendOrigin,
        publicOrigin: origin.origin,
        mode: "oidc",
        authorizationEndpoint: endpoint.href,
      };
    }
    const hostname = origin.hostname.replace(/^\[|\]$/g, "");
    if (
      (environment.ASPNETCORE_ENVIRONMENT ?? environment.DOTNET_ENVIRONMENT) !==
        "Development" ||
      origin.protocol !== "http:" ||
      !isLoopback(hostname) ||
      root.publicOrigin !==
        (environment.KOFTE_WEB_URL ?? "http://127.0.0.1:3000") ||
      object(root.development).enabled !== true
    )
      throw new TypeError("identity_adapter_configuration_invalid");
    const development = object(root.development);
    if (
      Object.keys(development).some(
        (key) => !["enabled", "identities"].includes(key),
      ) ||
      !Array.isArray(development.identities) ||
      development.identities.length < 1 ||
      development.identities.length > 8
    )
      throw new TypeError("identity_adapter_configuration_invalid");
    const developmentIdentities = development.identities.map(
      (entry: unknown) => {
        const choice = object(entry);
        const displayName = choice.displayName ?? null;
        if (
          Object.keys(choice).some(
            (key) => !["key", "displayName"].includes(key),
          ) ||
          typeof choice.key !== "string" ||
          choice.key.length < 1 ||
          choice.key.length > 255 ||
          /[^ -~]/.test(choice.key) ||
          !isIdentityDisplayName(displayName)
        )
          throw new TypeError("identity_adapter_configuration_invalid");
        return { key: choice.key, displayName };
      },
    );
    if (
      new Set(developmentIdentities.map((choice) => choice.key)).size !==
      developmentIdentities.length
    )
      throw new TypeError("identity_adapter_configuration_invalid");
    return {
      backendOrigin,
      publicOrigin: origin.origin,
      mode: "development",
      developmentIdentities,
    };
  } catch (error) {
    if (
      error instanceof SyntaxError ||
      (error instanceof Error && "code" in error)
    )
      throw new TypeError("identity_adapter_configuration_invalid", {
        cause: error,
      });
    throw error;
  }
}

function object(value: unknown): Record<string, unknown> {
  if (typeof value !== "object" || value === null || Array.isArray(value))
    throw new TypeError("identity_adapter_configuration_invalid");
  return Object.fromEntries(Object.entries(value));
}

function string(value: unknown): string {
  if (typeof value !== "string" || value.length === 0)
    throw new TypeError("identity_adapter_configuration_invalid");
  return value;
}

export function rejectDuplicateProperties(text: string): void {
  const stack: (Set<string> | null)[] = [];
  let key = false;
  for (const token of text.match(
    /"(?:\\[\s\S]|[^"\\])*"|[{}[\],:]|[^\s{}[\],:]+/g,
  ) ?? []) {
    if (token === "{" || token === "[") {
      stack.push(token === "{" ? new Set() : null);
      key = token === "{";
      if (stack.length > 8)
        throw new TypeError("identity_adapter_configuration_invalid");
    } else if (token === "}" || token === "]") {
      stack.pop();
      key = false;
    } else if (token === ",") {
      key = stack.at(-1) !== null;
    } else if (key && token.startsWith('"')) {
      const name: unknown = JSON.parse(token);
      const names = stack.at(-1);
      if (typeof name !== "string" || !names || names.has(name))
        throw new TypeError("identity_adapter_configuration_invalid");
      names.add(name);
      key = false;
    }
  }
}
