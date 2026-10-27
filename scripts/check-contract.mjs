import { execFileSync } from "node:child_process";
import { mkdtemp, readFile, rm } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { fileURLToPath, URL } from "node:url";
import console from "node:console";
import { isDeepStrictEqual } from "node:util";
import { format } from "prettier";

const root = new URL("../", import.meta.url);
const source = new URL("packages/bootstrap-contract/openapi.json", root);
const baseline = new URL(
  "specs/001-repository-bootstrap/contracts/bootstrap.openapi.json",
  root,
);
const current = JSON.parse(await readFile(source, "utf8"));
const bootstrap = JSON.parse(await readFile(baseline, "utf8"));
for (const [path, definition] of Object.entries(bootstrap.paths))
  if (!isDeepStrictEqual(current.paths?.[path], definition))
    throw new Error("Bootstrap operation differs from the approved baseline.");
for (const [kind, definitions] of Object.entries(bootstrap.components))
  for (const [name, definition] of Object.entries(definitions))
    if (!isDeepStrictEqual(current.components?.[kind]?.[name], definition))
      throw new Error(
        "Bootstrap component differs from the approved baseline.",
      );
if (
  current.openapi !== bootstrap.openapi ||
  current.info?.title !== bootstrap.info.title ||
  current.info?.version !== bootstrap.info.version
)
  throw new Error("Bootstrap version differs from the approved baseline.");
const temporary = await mkdtemp(join(tmpdir(), "kofte-contract-"));
try {
  const generated = join(temporary, "generated.ts");
  execFileSync(
    fileURLToPath(
      new URL(
        "packages/bootstrap-contract/node_modules/.bin/openapi-typescript",
        root,
      ),
    ),
    [fileURLToPath(source), "-o", generated],
    { stdio: "pipe" },
  );
  const expected = await format(await readFile(generated, "utf8"), {
    parser: "typescript",
  });
  const actual = await readFile(
    new URL("packages/bootstrap-contract/src/generated.ts", root),
    "utf8",
  );
  if (expected !== actual)
    throw new Error(
      "Generated contract types drifted; run the contract workspace generate script.",
    );
  console.log(
    "Bootstrap operations/components preserved; generated contract types match.",
  );
} finally {
  await rm(temporary, { recursive: true });
}
