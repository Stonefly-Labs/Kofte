// @vitest-environment node
import {
  chmod,
  mkdtemp,
  realpath,
  rm,
  symlink,
  writeFile,
} from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { afterEach, beforeEach, expect, it } from "vitest";
import { identityConfiguration } from "./identityConfiguration";

let root: string;
let path: string;
const configuration = {
  version: "1",
  mode: "oidc",
  publicOrigin: "https://kofte.example",
  keyRingDirectory: "/not-opened/private-keys",
  provider: {
    authorizationEndpoint: "https://issuer.example/authorize",
    clientSecretFile: "/not-opened/client-secret",
  },
};

beforeEach(async () => {
  root = await mkdtemp(join(await realpath(tmpdir()), "kofte-web-config-"));
  path = join(root, "identity.json");
  await writeFile(path, JSON.stringify(configuration), { mode: 0o600 });
});

afterEach(async () => {
  await rm(root, { recursive: true, force: true });
});

it("reads only public routing fields from the private server configuration, not credential references", async () => {
  expect(await identityConfiguration({ KOFTE_AUTH_CONFIG_FILE: path })).toEqual(
    {
      backendOrigin: "http://127.0.0.1:5080",
      publicOrigin: "https://kofte.example",
      mode: "oidc",
      authorizationEndpoint: "https://issuer.example/authorize",
    },
  );
});

it.each([
  '{"version":"1","mode":"oidc","mode":"development","publicOrigin":"https://kofte.example"}',
  '{"version":"1","mode":"oidc","publicOrigin":"https://kofte.example","provider":{"authorizationEndpoint":"https://issuer.example/authorize","authorization\\u0045ndpoint":"https://attacker.example"}}',
  JSON.stringify({ ...configuration, publicOrigin: "http://kofte.example" }),
  JSON.stringify({
    ...configuration,
    publicOrigin: "https://kofte.example/path",
  }),
  JSON.stringify({
    ...configuration,
    provider: {
      authorizationEndpoint: "https://issuer.example/authorize#fragment",
    },
  }),
  JSON.stringify({
    ...configuration,
    provider: { authorizationEndpoint: "http://issuer.example/authorize" },
  }),
  " ".repeat(32769),
])(
  "rejects ambiguous, invalid and oversized routing configuration",
  async (text) => {
    await writeFile(path, text);
    await expect(
      identityConfiguration({ KOFTE_AUTH_CONFIG_FILE: path }),
    ).rejects.toBeInstanceOf(TypeError);
  },
);

it("rejects a public or symlinked configuration instead of following it", async () => {
  await chmod(path, 0o644);
  await expect(
    identityConfiguration({ KOFTE_AUTH_CONFIG_FILE: path }),
  ).rejects.toBeInstanceOf(TypeError);
  await chmod(path, 0o600);
  const link = join(root, "linked.json");
  await symlink(path, link);
  await expect(
    identityConfiguration({ KOFTE_AUTH_CONFIG_FILE: link }),
  ).rejects.toBeInstanceOf(TypeError);
});

it("admits development routing only with explicit opt-in, loopback and the host's Development environment", async () => {
  await writeFile(
    path,
    JSON.stringify({
      version: "1",
      mode: "development",
      publicOrigin: "http://127.0.0.1:3000",
      development: { enabled: true },
    }),
  );
  await expect(
    identityConfiguration({ KOFTE_AUTH_CONFIG_FILE: path }),
  ).rejects.toBeInstanceOf(TypeError);
  expect(
    await identityConfiguration({
      KOFTE_AUTH_CONFIG_FILE: path,
      ASPNETCORE_ENVIRONMENT: "Development",
    }),
  ).toEqual({
    backendOrigin: "http://127.0.0.1:5080",
    publicOrigin: "http://127.0.0.1:3000",
    mode: "development",
  });
});
