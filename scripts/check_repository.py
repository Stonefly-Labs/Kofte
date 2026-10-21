"""Dependency-free checks for repository-owned bootstrap assets."""

import importlib.util
import json
from pathlib import Path
import re
import sys
from urllib.parse import unquote, urlsplit
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[1]


def check(root=ROOT):
    cli = json.loads((root / ".github/mcp.json").read_text())["mcpServers"]
    editor = json.loads((root / ".vscode/mcp.json").read_text())["servers"]
    expected = {"microsoft-learn", "context7", "next-devtools", "shadcn", "playwright"}
    if not expected.issubset(cli) or set(cli) != set(editor):
        raise ValueError("MCP server inventory is missing or differs between clients.")
    for name, server in cli.items():
        if {key: value for key, value in server.items() if key != "tools"} != editor[name]:
            raise ValueError(f"MCP configuration drift: {name}")
        if server["type"] == "http":
            if urlsplit(server["url"]).scheme != "https":
                raise ValueError(f"Remote MCP server must use HTTPS: {name}")
        elif server["type"] == "stdio":
            if server["command"] != "npx" or not any(
                re.fullmatch(r".+@\d+\.\d+\.\d+", arg) for arg in server["args"]
            ):
                raise ValueError(f"Local MCP package must be exactly pinned: {name}")
        else:
            raise ValueError(f"Unsupported MCP transport: {name}")

    markdown = [
        root / "README.md", root / "CONTRIBUTING.md",
        root / ".github/copilot-instructions.md",
        root / ".github/pull_request_template.md",
        root / ".github/skills/drive-speckit/SKILL.md",
        root / ".github/skills/ship-change/SKILL.md",
        root / ".github/agents/spec-review.agent.md",
        *sorted((root / "docs").rglob("*.md")),
    ]
    for path in markdown:
        text = path.read_text()
        if not text.endswith("\n"):
            raise ValueError(f"Missing final newline: {path.relative_to(root)}")
        text = re.sub(r"```.*?```", "", text, flags=re.DOTALL)
        text = re.sub(r"`[^`\n]+`", "", text)
        for link in re.findall(r"!?\[[^\]]*\]\(([^)\s]+)\)", text):
            target = urlsplit(link)
            if target.scheme or target.netloc or not target.path:
                continue
            resolved = (path.parent / unquote(target.path)).resolve()
            if not resolved.is_relative_to(root.resolve()) or not resolved.exists():
                raise ValueError(f"Broken local link in {path.relative_to(root)}: {link}")
        for source in re.findall(r'<img[^>]+src="([^"]+)"', text):
            if not urlsplit(source).scheme and not (path.parent / source).is_file():
                raise ValueError(f"Broken local image in {path.relative_to(root)}: {source}")

    logo = ET.parse(root / "docs/assets/kofte-logo.svg").getroot()
    namespace = "{http://www.w3.org/2000/svg}"
    if logo.tag != f"{namespace}svg" or logo.find(f"{namespace}title") is None or logo.find(f"{namespace}desc") is None:
        raise ValueError("Logo must be SVG with accessible title and description.")
    for element in logo.iter():
        if element.tag in {f"{namespace}script", f"{namespace}foreignObject"} or any(
            key.startswith("on") for key in element.attrib
        ):
            raise ValueError("Logo must not contain scripts or event handlers.")

    for name in ("drive-speckit", "ship-change"):
        path = root / f".github/skills/{name}/SKILL.md"
        match = re.match(r"---\n(.*?)\n---\n", path.read_text(), re.DOTALL)
        if match is None or f"name: {name}\n" not in match.group(1) + "\n" or "description: " not in match.group(1):
            raise ValueError(f"Invalid project skill frontmatter: {name}")
    module_path = root / ".github/skills/drive-speckit/scripts/state.py"
    spec = importlib.util.spec_from_file_location("speckit_state", module_path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    ledger = module.Ledger(root)
    ledger.next()
    print("Repository assets, local documentation links, MCP parity and driver state are valid.")


if __name__ == "__main__":
    try:
        check()
    except (OSError, ValueError, KeyError, TypeError, AttributeError, ET.ParseError) as error:
        print(f"repository-check: {error}", file=sys.stderr)
        sys.exit(1)
