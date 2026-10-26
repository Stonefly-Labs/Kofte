"""Offline reconciliation of the reviewed complete bootstrap dependency graph."""

import argparse
import base64
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tarfile
import xml.etree.ElementTree as ET
import zipfile


ROOT = Path(__file__).resolve().parents[1]
LICENSES = {
    "(MIT OR CC0-1.0)", "0BSD", "Apache-2.0",
    "Apache-2.0 AND LGPL-3.0-or-later", "Apache-2.0 AND LGPL-3.0-or-later AND MIT",
    "BSD-2-Clause", "BSD-3-Clause", "BlueOak-1.0.0", "CC-BY-4.0", "CC0-1.0", "ISC",
    "LGPL-3.0-or-later", "MIT", "MIT-0", "MPL-2.0", "PostgreSQL", "Python-2.0",
}
NOTICE_RE = re.compile(r"(?i)license|licence|notice|copying")
EDGE_FIELDS = ("dependencies", "optionalDependencies", "peerDependencies", "peerDependenciesMeta")
# Only these fixed checker codes may leave transient command output.
FAILURE_CODES = frozenset("""
action_pin_invalid action_reference_drift action_review_missing adoption_decisions_missing
adoption_evidence_invalid adoption_provenance_missing adoption_provenance_unresolved
adoption_review_date_missing adoption_review_unresolved archive_integrity_invalid
archive_integrity_missing container_image_unreviewed copied_source_notice_drift
dependency_evidence_missing_or_invalid dependency_identity_drift dependency_identity_invalid
dependency_name_invalid image_inventory_invalid image_reference_drift license_unreviewed
notice_digest_invalid notice_drift notice_evidence_missing notice_inventory_invalid
notice_path_invalid notice_unreviewed npm_archive_evidence_missing npm_archive_integrity_drift
npm_archive_metadata_drift npm_archive_metadata_missing npm_archive_notice_drift npm_archive_root_invalid
npm_bundle_archive_evidence_missing npm_bundle_archive_integrity_drift npm_bundle_dependency_edges_drift
npm_bundle_archive_preparation_failed
npm_bundle_metadata_drift npm_bundle_metadata_missing npm_bundle_parent_drift npm_bundle_parent_unreviewed
npm_dependency_edges_drift npm_dependency_unreviewed npm_direct_dependency_not_pinned
npm_installed_metadata_drift npm_inventory_graph_drift npm_lock_inventory_drift npm_lock_path_invalid
npm_lock_version_invalid npm_manifest_lock_drift npm_package_path_invalid npm_required_package_missing
npm_source_unreviewed npm_workspace_lock_drift npm_workspace_path_invalid nuget_archive_evidence_missing
nuget_archive_integrity_drift nuget_archive_metadata_missing nuget_archive_notice_drift
nuget_central_version_unreviewed nuget_dependency_unreviewed nuget_installed_metadata_drift
nuget_installed_metadata_missing
nuget_inventory_graph_drift nuget_lock_inventory_drift nuget_lock_missing nuget_lock_version_invalid
nuget_manifest_lock_drift nuget_metadata_invalid nuget_package_evidence_missing
nuget_project_reference_drift nuget_projects_missing nuget_provenance_drift nuget_restore_evidence_drift
nuget_source_unreviewed postgres_image_pin_invalid workflow_failure_policy_invalid workflow_image_unreviewed
""".split())
ADOPTED_SOURCE_NOTICE = "8c9bde708e3029ec146781e13d14c1fded74a3e430f7fee2980bb180d9113d98"


class DependencyError(Exception):
    pass


def require(condition, code):
    if not condition:
        raise DependencyError(code)


def read_json(path):
    return json.loads(path.read_text())


def sha256(content):
    return hashlib.sha256(content).hexdigest()


def checked_file(directory, name):
    require(isinstance(name, str) and name and not Path(name).is_absolute()
            and ".." not in Path(name).parts, "notice_path_invalid")
    path = directory / name
    require(path.is_file() and not path.is_symlink()
            and path.resolve().is_relative_to(directory.resolve()), "notice_evidence_missing")
    return path


def check_notices(directory, notices):
    require(isinstance(notices, dict), "notice_inventory_invalid")
    for name, expected in notices.items():
        require(re.fullmatch(r"[a-f0-9]{64}", expected) is not None, "notice_digest_invalid")
        require(sha256(checked_file(directory, name).read_bytes()) == expected, "notice_drift")
    actual = {
        path.name for path in directory.iterdir()
        if path.is_file() and NOTICE_RE.search(path.name)
    }
    require(actual.issubset(notices), "notice_unreviewed")


def check_identity(key, entry, nuget=False):
    require(set(entry) == {
        "name", "version", "license", "source", "integrity", "notices",
        *(("repository",) if nuget else ()),
    }, "adoption_evidence_invalid")
    require(isinstance(entry["name"], str) and isinstance(entry["version"], str)
            and re.fullmatch(r"[0-9]+\.[0-9]+\.[0-9]+(?:[-+][a-zA-Z0-9.-]+)?", entry["version"]),
            "dependency_identity_invalid")
    require(re.fullmatch(r"[a-zA-Z0-9_.-]+" if nuget else r"(?:@[a-zA-Z0-9_.-]+/)?[a-zA-Z0-9_.-]+",
                         entry["name"]) is not None, "dependency_name_invalid")
    identity = f"{entry['name']}@{entry['version']}"
    require(key == (identity.lower() if nuget else identity), "dependency_identity_drift")
    require(entry["license"] in LICENSES, "license_unreviewed")
    integrity = entry["integrity"]
    if not nuget:
        require(integrity.startswith("sha512-"), "archive_integrity_missing")
        integrity = integrity[7:]
    try:
        require(len(base64.b64decode(integrity, validate=True)) == 64, "archive_integrity_invalid")
    except (ValueError, TypeError) as error:
        raise DependencyError("archive_integrity_invalid") from error


def npm_name(path, entry):
    return entry.get("name") or path.rsplit("node_modules/", 1)[1]


def npm_archive(cache, integrity):
    digest = base64.b64decode(integrity[7:], validate=True).hex()
    return cache / "content-v2/sha512" / digest[:2] / digest[2:4] / digest[4:]


def archive_root(package):
    manifests = [name for name in package.getnames()
                 if name.count("/") == 1 and name.endswith("/package.json")]
    require(len(manifests) == 1, "npm_archive_root_invalid")
    return manifests[0].rsplit("/", 1)[0]


def prepare_bundle_archives(root, inventory, cache):
    packages = read_json(root / "package-lock.json")["packages"]
    prepared = set()
    for relative, locked in packages.items():
        if not locked.get("inBundle"):
            continue
        require(not Path(relative).is_absolute() and ".." not in Path(relative).parts,
                "npm_lock_path_invalid")
        parent_path = relative.rpartition("/node_modules/")[0]
        parent = packages[parent_path]
        identity = f"{npm_name(parent_path, parent)}@{parent['version']}"
        require(identity in inventory, "npm_bundle_parent_unreviewed")
        reviewed = inventory[identity]
        check_identity(identity, reviewed)
        require(all(parent.get(field) == reviewed[key] for field, key in (
            ("version", "version"), ("resolved", "source"), ("integrity", "integrity"),
            ("license", "license"),
        )), "npm_bundle_parent_drift")
        name, version = reviewed["name"], reviewed["version"]
        require(reviewed["source"] ==
                f"https://registry.npmjs.org/{name}/-/{name.rsplit('/', 1)[-1]}-{version}.tgz",
                "npm_source_unreviewed")
        archive = npm_archive(cache, reviewed["integrity"])
        if identity in prepared or archive.exists():
            continue
        try:
            result = subprocess.run(
                ["npm", "cache", "add", reviewed["source"], "--cache", str(cache.parent.absolute())],
                cwd=root, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=90,
            )
        except (OSError, subprocess.TimeoutExpired) as error:
            raise DependencyError("npm_bundle_archive_preparation_failed") from error
        require(result.returncode == 0, "npm_bundle_archive_preparation_failed")
        require(archive.is_file(), "npm_bundle_archive_evidence_missing")
        prepared.add(identity)


def check_npm(root, inventory, cache):
    lock = read_json(root / "package-lock.json")
    require(lock.get("lockfileVersion") == 3, "npm_lock_version_invalid")
    packages, graph = lock["packages"], {}
    for relative, locked in packages.items():
        if "node_modules/" not in relative or locked.get("link"):
            continue
        require(not Path(relative).is_absolute() and ".." not in Path(relative).parts,
                "npm_lock_path_invalid")
        name = npm_name(relative, locked)
        identity = f"{name}@{locked['version']}"
        if locked.get("inBundle"):
            parent_path, _, nested_path = relative.rpartition("/node_modules/")
            parent = packages[parent_path]
            parent_identity = f"{npm_name(parent_path, parent)}@{parent['version']}"
            require(parent_identity in inventory, "npm_bundle_parent_unreviewed")
            reviewed_parent = inventory[parent_identity]
            require(parent["integrity"] == reviewed_parent["integrity"], "npm_bundle_parent_drift")
            archive = npm_archive(cache, reviewed_parent["integrity"])
            require(archive.is_file(), "npm_bundle_archive_evidence_missing")
            require("sha512-" + base64.b64encode(hashlib.sha512(archive.read_bytes()).digest()).decode()
                    == reviewed_parent["integrity"], "npm_bundle_archive_integrity_drift")
            with tarfile.open(archive, "r:gz") as package:
                member = package.extractfile(f"{archive_root(package)}/node_modules/{nested_path}/package.json")
                require(member is not None, "npm_bundle_metadata_missing")
                original = json.load(member)
                require(original.get("name") == name and original.get("version") == locked["version"]
                        and original.get("license") == locked.get("license")
                        and original.get("license") in LICENSES, "npm_bundle_metadata_drift")
                require(all(original.get(field, {}) == locked.get(field, {}) for field in EDGE_FIELDS),
                        "npm_bundle_dependency_edges_drift")
            continue
        graph[identity] = inventory.get(identity)
        reviewed = graph[identity]
        require(reviewed is not None, "npm_dependency_unreviewed")
        check_identity(identity, reviewed)
        require(all(locked.get(field) == reviewed[review_field] for field, review_field in (
            ("version", "version"), ("resolved", "source"), ("integrity", "integrity"),
            ("license", "license"),
        )), "npm_lock_inventory_drift")
        require(reviewed["source"] ==
                f"https://registry.npmjs.org/{name}/-/{name.rsplit('/', 1)[-1]}-{locked['version']}.tgz",
                "npm_source_unreviewed")
        directory = root / relative
        require(not directory.is_symlink() and directory.resolve().is_relative_to(root.resolve()),
                "npm_package_path_invalid")
        if not (directory / "package.json").is_file():
            require(locked.get("optional") is True and not directory.exists(), "npm_required_package_missing")
            # Other-platform optionals remain lock/integrity-pinned; no claim
            # about locally absent licenses, signatures or installed artifacts.
            continue
        require(not directory.is_symlink(), "npm_package_path_invalid")
        metadata = read_json(directory / "package.json")
        require(metadata.get("name") == name and metadata.get("version") == locked["version"]
                and metadata.get("license") == reviewed["license"], "npm_installed_metadata_drift")
        check_notices(directory, reviewed["notices"])
        archive = npm_archive(cache, reviewed["integrity"])
        require(archive.is_file(), "npm_archive_evidence_missing")
        require("sha512-" + base64.b64encode(
            hashlib.sha512(archive.read_bytes()).digest()).decode() == reviewed["integrity"],
            "npm_archive_integrity_drift")
        with tarfile.open(archive, "r:gz") as package:
            prefix = archive_root(package)
            member = package.extractfile(f"{prefix}/package.json")
            require(member is not None, "npm_archive_metadata_missing")
            original = json.load(member)
            require(original.get("name") == name and original.get("version") == locked["version"]
                    and original.get("license") == reviewed["license"], "npm_archive_metadata_drift")
            require(all(original.get(field, {}) == locked.get(field, {}) for field in EDGE_FIELDS),
                    "npm_dependency_edges_drift")
            for notice, digest in reviewed["notices"].items():
                member = package.extractfile(f"{prefix}/{notice}")
                require(member is not None and sha256(member.read()) == digest,
                        "npm_archive_notice_drift")
    require(set(graph) == set(inventory), "npm_inventory_graph_drift")
    manifest_paths = ["", *read_json(root / "package.json")["workspaces"]]
    require(all(isinstance(path, str) and not Path(path).is_absolute()
                and ".." not in Path(path).parts for path in manifest_paths), "npm_workspace_path_invalid")
    require({p for p in packages if p and "node_modules/" not in p} == set(manifest_paths) - {""},
            "npm_workspace_lock_drift")
    for path in manifest_paths:
        manifest = read_json(root / path / "package.json")
        locked = packages[path]
        for field in ("name", "version", "dependencies", "devDependencies", "engines"):
            require(manifest.get(field, {}) == locked.get(field, {}), "npm_manifest_lock_drift")
        for field in ("dependencies", "devDependencies"):
            for name, version in manifest.get(field, {}).items():
                require(re.fullmatch(r"[0-9]+\.[0-9]+\.[0-9]+", version) is not None,
                        "npm_direct_dependency_not_pinned")


def nuget_metadata(package):
    tree = ET.fromstring(package)
    metadata = tree.find("{*}metadata")
    require(metadata is not None, "nuget_metadata_invalid")
    license_node = metadata.find("{*}license")
    repository = metadata.find("{*}repository")
    return {
        "name": metadata.findtext("{*}id"),
        "version": metadata.findtext("{*}version"),
        "license": license_node.text if license_node is not None else None,
        "licenseType": license_node.get("type") if license_node is not None else None,
        "repository": repository.attrib if repository is not None else {},
    }


def check_nuget(root, inventory, cache):
    graph = set()
    central_path = root / "Directory.Packages.props"
    central = {
        node.get("Include", "").lower(): node.get("Version")
        for node in ET.parse(central_path).getroot().iter("PackageVersion")
    } if central_path.is_file() else {}
    for name, version in central.items():
        require(re.fullmatch(r"[0-9]+\.[0-9]+\.[0-9]+", version or "") is not None
                and f"{name}@{version}" in inventory, "nuget_central_version_unreviewed")
    projects = [*sorted((root / "src").rglob("*.csproj")),
                *sorted((root / "tests").rglob("*.csproj"))]
    require(bool(projects), "nuget_projects_missing")
    for project in projects:
        lock = project.parent / "packages.lock.json"
        require(lock.is_file(), "nuget_lock_missing")
        resolved = read_json(lock)
        require(resolved.get("version") == 2, "nuget_lock_version_invalid")
        for dependencies in resolved["dependencies"].values():
            for name, locked in dependencies.items():
                if locked["type"] == "Project":
                    continue
                identity = f"{name}@{locked['resolved']}".lower()
                require(identity in inventory, "nuget_dependency_unreviewed")
                require(locked["contentHash"] == inventory[identity]["integrity"],
                        "nuget_lock_inventory_drift")
                graph.add(identity)
        direct = {
            name.lower(): value for dependencies in resolved["dependencies"].values()
            for name, value in dependencies.items() if value["type"] == "Direct"
        }
        references = list(ET.parse(project).getroot().iter("PackageReference"))
        require({reference.get("Include", "").lower() for reference in references} == set(direct),
                "nuget_project_reference_drift")
        for reference in references:
            name = reference.get("Include", "").lower()
            version = reference.get("VersionOverride") or reference.get("Version") or central.get(name)
            require(name in direct and re.fullmatch(r"[0-9]+\.[0-9]+\.[0-9]+", version or "")
                    and direct[name]["resolved"] == version, "nuget_manifest_lock_drift")
    require(graph == set(inventory), "nuget_inventory_graph_drift")
    for identity in sorted(graph):
        reviewed = inventory[identity]
        check_identity(identity, reviewed, nuget=True)
        name, version = reviewed["name"].lower(), reviewed["version"].lower()
        directory = cache / name / version
        require(directory.is_dir() and not directory.is_symlink(), "nuget_package_evidence_missing")
        source = f"https://api.nuget.org/v3-flatcontainer/{name}/{version}/{name}.{version}.nupkg"
        require(reviewed["source"] == source, "nuget_source_unreviewed")
        metadata = read_json(directory / ".nupkg.metadata")
        require(metadata.get("source") == "https://api.nuget.org/v3/index.json"
                and metadata.get("contentHash") == reviewed["integrity"],
                "nuget_restore_evidence_drift")
        archive = directory / f"{name}.{version}.nupkg"
        require(archive.is_file(), "nuget_archive_evidence_missing")
        # Signed ZIP hashes and NuGet's lock content hash differ. Verify the
        # retained signed archive separately; do not claim new signature proof.
        require(base64.b64encode(hashlib.sha512(archive.read_bytes()).digest()).decode()
                == (directory / f"{name}.{version}.nupkg.sha512").read_text().strip(),
                "nuget_archive_integrity_drift")
        check_notices(directory, reviewed["notices"])
        with zipfile.ZipFile(archive) as package:
            nuspecs = [name for name in package.namelist() if name.endswith(".nuspec") and "/" not in name]
            require(len(nuspecs) == 1, "nuget_archive_metadata_missing")
            original = nuget_metadata(package.read(nuspecs[0]))
            # NuGet normalizes the installed filename, not the ZIP member's case.
            installed_manifest = directory / f"{name}.nuspec"
            require(installed_manifest.is_file() and not installed_manifest.is_symlink(),
                    "nuget_installed_metadata_missing")
            installed = nuget_metadata(installed_manifest.read_bytes())
            require(original == installed, "nuget_installed_metadata_drift")
            require(original["name"] == reviewed["name"] and original["version"] == reviewed["version"]
                    and original["license"] == reviewed["license"] and original["licenseType"] == "expression"
                    and original["repository"] == reviewed["repository"], "nuget_provenance_drift")
            for notice, digest in reviewed["notices"].items():
                require(sha256(package.read(notice)) == digest, "nuget_archive_notice_drift")


def check_references(root, inventory):
    actions = inventory["actions"]
    require(bool(actions), "action_review_missing")
    for name, commit in actions.items():
        require(re.fullmatch(r"actions/[a-z-]+", name) is not None
                and re.fullmatch(r"[a-f0-9]{40}", commit) is not None, "action_pin_invalid")
    for workflow in (root / ".github/workflows").glob("*.y*ml"):
        text = workflow.read_text()
        for reference in re.findall(r"(?m)^\s*(?:-\s*)?uses:\s*([^\s#]+)", text):
            name, separator, commit = reference.partition("@")
            require(separator and actions.get(name) == commit, "action_reference_drift")
        require("pull_request_target" not in text and "continue-on-error" not in text,
                "workflow_failure_policy_invalid")
        for image in re.findall(r"(?:docker pull\s+|(?m:^\s*image:\s*))[\"']?([^\"'\s#]+)", text):
            require(image in inventory["images"].values(), "workflow_image_unreviewed")
    images = inventory["images"]
    require(set(images) == {"postgres"}, "image_inventory_invalid")
    require(re.fullmatch(r"postgres:17@sha256:[a-f0-9]{64}", images["postgres"]) is not None,
            "postgres_image_pin_invalid")
    compose = (root / "deploy/docker/compose.test.yml").read_text()
    references = re.findall(r"(?m)^\s*image:\s*[\"']?([^\"'\s#]+)", compose)
    require(references == [images["postgres"]], "image_reference_drift")
    for container in (root / "deploy").rglob("*Dockerfile*"):
        for image in re.findall(r"(?im)^FROM\s+(\S+)", container.read_text()):
            require(image in images.values(), "container_image_unreviewed")


def check(root=ROOT, npm_cache=None, nuget_cache=None, prepare_bundled=False):
    inventory = read_json(root / "docs/development/dependency-inventory.json")
    require(set(inventory) == {
        "schemaVersion", "reviewDate", "scope", "npm", "nuget", "verification", "images", "actions",
    } and inventory["schemaVersion"] == 1, "adoption_review_unresolved")
    require(re.fullmatch(r"\d{4}-\d{2}-\d{2}", inventory["reviewDate"]) is not None,
            "adoption_review_date_missing")
    evidence = inventory["verification"]
    require(set(evidence) == {
        "npmAuditVulnerabilities", "nugetAuditVulnerabilities", "npmVerifiedRegistrySignatures",
        "npmVerifiedAttestations", "nugetVerifiedSignaturesLinux", "nugetNativeMacVerification", "linuxVerifier",
    }, "adoption_provenance_missing")
    require(evidence["npmAuditVulnerabilities"] == evidence["nugetAuditVulnerabilities"] == 0
            and evidence["npmVerifiedRegistrySignatures"] > 0
            and evidence["npmVerifiedAttestations"] > 0
            and evidence["nugetVerifiedSignaturesLinux"] == len(inventory["nuget"])
            and evidence["nugetNativeMacVerification"]
            == "unsupported; failed platform trust chains, not bypassed"
            and re.fullmatch(r"mcr.microsoft.com/dotnet/sdk@sha256:[a-f0-9]{64}", evidence["linuxVerifier"]),
            "adoption_provenance_unresolved")
    review = (root / "docs/development/dependencies.md").read_text()
    require(inventory["reviewDate"] in review and "MPL-2.0" in review and "LGPL-3.0-or-later" in review
            and "nuget-signed-package-verification" in review, "adoption_decisions_missing")
    require(sha256((root / "docs/development/third-party-notices.md").read_bytes())
            == ADOPTED_SOURCE_NOTICE, "copied_source_notice_drift")
    npm_cache = Path(npm_cache or os.environ.get("npm_config_cache", Path.home() / ".npm")) / "_cacache"
    nuget_cache = Path(nuget_cache or os.environ.get("NUGET_PACKAGES", Path.home() / ".nuget/packages"))
    if prepare_bundled:
        prepare_bundle_archives(root, inventory["npm"], npm_cache)
    check_npm(root, inventory["npm"], npm_cache)
    check_nuget(root, inventory["nuget"], nuget_cache)
    check_references(root, inventory)
    print(json.dumps({
        "eventCode": "dependency_inventory_verified",
        "npmIdentities": len(inventory["npm"]), "nugetIdentities": len(inventory["nuget"]),
        "evidence": "reviewed-locks-and-restored-artifacts",
        "optionalPlatformEvidence": "uninstalled-not-claimed",
        "signatureEvidence": "adoption-review-not-a-new-signature-audit",
    }))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--prepare-bundled-evidence", action="store_true",
        help="Fetch missing reviewed bundle-parent archives into npm's cache before offline checks.",
    )
    arguments = parser.parse_args()
    try:
        check(prepare_bundled=arguments.prepare_bundled_evidence)
    except DependencyError as error:
        print(json.dumps({"eventCode": str(error)}), file=sys.stderr)
        raise SystemExit(1)
    except (OSError, ValueError, KeyError, TypeError, ET.ParseError, tarfile.TarError, zipfile.BadZipFile):
        print(json.dumps({"eventCode": "dependency_evidence_missing_or_invalid"}), file=sys.stderr)
        raise SystemExit(1)
