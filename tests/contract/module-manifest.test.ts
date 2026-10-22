import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import Ajv2020 from "ajv/dist/2020.js";
import { describe, expect, it } from "vitest";

const base = new URL(
  "../../specs/002-platform-contracts/contracts/",
  import.meta.url,
);
const read = (path: string): unknown =>
  JSON.parse(readFileSync(fileURLToPath(new URL(path, base)), "utf8"));
const ajv = new Ajv2020({ strict: true, allErrors: true });
const validate = ajv.compile(read("module-manifest.schema.json") as object);
const minimal = read("examples/minimal.json") as Record<string, unknown>;
const change = (field: string, value: unknown) => ({
  ...minimal,
  [field]: value,
});

describe("module manifest schema 1 structural contract", () => {
  it("accepts the exact examples and semantic-only rejections structurally", () => {
    for (const input of [
      minimal,
      read("examples/full.json"),
      change("version", "2147483648.0.0+001"),
      change("hostContract", ">=1.0.0 <1.0.0"),
      change("hostContract", ">=1.02.0 <2.0.0"),
      change("capabilities", [
        { kind: "backend", contractVersion: "1.0.0" },
        { kind: "backend", contractVersion: "1.0.0+different" },
      ]),
      change("capabilities", [{ kind: "backend", contractVersion: "1.1.0" }]),
      change("capabilities", [
        { kind: "backend", contractVersion: "1.0.0-rc.1" },
      ]),
      change("dependencies", [
        { id: "org.example.absent", versionRange: "1.0.0" },
        { id: "org.example.absent", versionRange: "2.0.0" },
      ]),
      change("dependencies", [
        { id: "org.example.notes", versionRange: "1.0.0" },
      ]),
      change("dependencies", [
        { id: "org.other.notes", versionRange: "1.0.0" },
      ]),
      change("permissions", [
        { name: "notes.read", description: "Read." },
        { name: "notes.read", description: "Read." },
      ]),
      change("displayName", " "),
      change("displayName", "Köfte 🧆"),
    ])
      expect(validate(input)).toBe(true);
  });

  it("rejects unknown, missing, mistyped, enum and grammar fields", () => {
    const missing = { ...minimal };
    delete missing.storage;
    for (const input of [
      missing,
      change("capabilities", null),
      { ...minimal, unexpected: true },
      change("schemaVersion", "2"),
      ...[
        "Org.example.notes",
        " org.example.notes",
        "org.exämple.notes",
        "org..notes",
        "org.example.-notes",
        "org/example/notes",
        "org.notes",
      ].map((id) => change("id", id)),
      ...["1.02.0", "1.0", "1.0.0-01", "1.0.0+"].map((version) =>
        change("version", version),
      ),
      change("hostContract", "^1.0.0"),
      change("capabilities", [{ kind: "unknown", contractVersion: "1.0.0" }]),
      change("permissions", [{ name: "Notes.Read", description: "Read." }]),
      ...["path", "schema"].map((key) =>
        change("storage", { relational: false, [key]: "../invalid" }),
      ),
      { ...minimal, $ref: "https://invalid.example/" },
      { ...minimal, hooks: [] },
      change("displayName", "Notes\n"),
      change("displayName", "Notes\u0085"),
      { ...minimal, ["unknown\nmarker"]: true },
    ])
      expect(validate(input)).toBe(false);
  });

  it("enforces independent scalar, length and collection boundaries", () => {
    expect(validate(change("displayName", "🧆".repeat(128)))).toBe(true);
    expect(validate(change("displayName", "🧆".repeat(129)))).toBe(false);
    expect(
      validate(
        change("id", [63, 63, 63, 61].map((n) => "a".repeat(n)).join(".")),
      ),
    ).toBe(true);
    expect(
      validate(
        change("id", [63, 63, 63, 62].map((n) => "a".repeat(n)).join(".")),
      ),
    ).toBe(false);
    for (const [field, count, item] of [
      ["capabilities", 9, { kind: "backend", contractVersion: "1.0.0" }],
      ["dependencies", 64, { id: "org.example.absent", versionRange: "1.0.0" }],
      ["permissions", 128, { name: "read", description: "Read." }],
    ] as const) {
      expect(
        validate(
          change(
            field,
            Array.from({ length: count }, () => item),
          ),
        ),
      ).toBe(true);
      expect(
        validate(
          change(
            field,
            Array.from({ length: count + 1 }, () => item),
          ),
        ),
      ).toBe(false);
    }
  });
});
