import base64
import hashlib
import io
import json
from pathlib import Path
import shutil
import subprocess
import tarfile
import tempfile
import unittest
from unittest.mock import patch
import zipfile

from scripts import check_dependencies as dependencies


class DependencyTests(unittest.TestCase):
    def setUp(self):
        base = dependencies.ROOT / "artifacts/python-tests"
        base.mkdir(parents=True, exist_ok=True)
        self.temporary = tempfile.TemporaryDirectory(dir=base)
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.npm_cache = self.root / "npm-cache/_cacache"
        self.nuget_cache = self.root / "nuget-cache"
        self.npm_metadata = {"name": "example", "version": "1.2.3", "license": "MIT"}
        self.npm_package = self.root / "node_modules/example"
        self.npm_package.mkdir(parents=True)
        self.license = b"Copyright Example contributors. MIT license retained.\n"
        (self.npm_package / "package.json").write_text(json.dumps(self.npm_metadata))
        (self.npm_package / "LICENSE").write_bytes(self.license)
        archive = io.BytesIO()
        with tarfile.open(fileobj=archive, mode="w:gz") as package:
            for name, content in (
                ("package/package.json", json.dumps(self.npm_metadata).encode()),
                ("package/LICENSE", self.license),
            ):
                member = tarfile.TarInfo(name)
                member.size = len(content)
                package.addfile(member, io.BytesIO(content))
        integrity = "sha512-" + base64.b64encode(hashlib.sha512(archive.getvalue()).digest()).decode()
        self.npm_archive = dependencies.npm_archive(self.npm_cache, integrity)
        self.npm_archive.parent.mkdir(parents=True)
        self.npm_archive.write_bytes(archive.getvalue())
        self.npm = {"example@1.2.3": {
            **self.npm_metadata, "source": "https://registry.npmjs.org/example/-/example-1.2.3.tgz",
            "integrity": integrity, "notices": {"LICENSE": dependencies.sha256(self.license)},
        }}
        self.lock = {"lockfileVersion": 3, "packages": {
            "": {"name": "fixture", "version": "0.0.0", "devDependencies": {"example": "1.2.3"}},
            "node_modules/example": {
                "version": "1.2.3", "resolved": self.npm["example@1.2.3"]["source"],
                "integrity": integrity, "license": "MIT",
            },
        }}
        self.write_json("package.json", {
            "name": "fixture", "version": "0.0.0", "workspaces": [],
            "devDependencies": {"example": "1.2.3"},
        })
        self.write_json("package-lock.json", self.lock)
        self.nuspec = (
            '<package><metadata><id>Example</id><version>1.2.3</version>'
            '<license type="expression">MIT</license>'
            '<repository type="git" url="https://example.invalid/source" commit="'
            + "b" * 40 + '"/></metadata></package>'
        ).encode()
        self.nuget_package = self.nuget_cache / "example/1.2.3"
        self.nuget_package.mkdir(parents=True)
        (self.nuget_package / "example.nuspec").write_bytes(self.nuspec)
        (self.nuget_package / "LICENSE").write_bytes(self.license)
        self.nuget_archive = self.nuget_package / "example.1.2.3.nupkg"
        self.write_nuget_archive(self.nuspec)
        content_hash = base64.b64encode(hashlib.sha512(b"unsigned-nuget-content").digest()).decode()
        self.nuget = {"example@1.2.3": {
            "name": "Example", "version": "1.2.3", "license": "MIT",
            "source": "https://api.nuget.org/v3-flatcontainer/example/1.2.3/example.1.2.3.nupkg",
            "integrity": content_hash,
            "repository": {"type": "git", "url": "https://example.invalid/source", "commit": "b" * 40},
            "notices": {"LICENSE": dependencies.sha256(self.license)},
        }}
        (self.nuget_package / ".nupkg.metadata").write_text(json.dumps({
            "source": "https://api.nuget.org/v3/index.json", "contentHash": content_hash,
        }))
        self.nuget_lock = {"version": 2, "dependencies": {"net10.0": {"Example": {
            "type": "Direct", "resolved": "1.2.3", "contentHash": content_hash,
        }}}}
        self.write_json("src/Fixture/packages.lock.json", self.nuget_lock)
        (self.root / "src/Fixture/Fixture.csproj").write_text(
            '<Project><ItemGroup><PackageReference Include="Example" Version="1.2.3" /></ItemGroup></Project>',
        )

    def write_json(self, path, value):
        file = self.root / path
        file.parent.mkdir(parents=True, exist_ok=True)
        file.write_text(json.dumps(value))

    def write_nuget_archive(self, nuspec):
        with zipfile.ZipFile(self.nuget_archive, "w") as package:
            package.writestr("example.nuspec", nuspec)
            package.writestr("LICENSE", self.license)
        (self.nuget_package / "example.1.2.3.nupkg.sha512").write_text(
            base64.b64encode(hashlib.sha512(self.nuget_archive.read_bytes()).digest()).decode(),
        )

    def check_npm(self):
        dependencies.check_npm(self.root, self.npm, self.npm_cache)

    def check_nuget(self):
        dependencies.check_nuget(self.root, self.nuget, self.nuget_cache)

    def test_reviewed_locked_restored_packages_and_notices_reconcile(self):
        self.check_npm()
        self.check_nuget()

    def test_exact_npm_integrity_source_and_license_drift_are_rejected(self):
        original = dict(self.lock["packages"]["node_modules/example"])
        for field, value in (
            ("integrity", "sha512-" + "A" * 86 + "=="),
            ("resolved", "https://unreviewed.invalid/example.tgz"),
            ("license", "Unreviewed-License"),
        ):
            with self.subTest(field=field):
                self.lock["packages"]["node_modules/example"] = original | {field: value}
                self.write_json("package-lock.json", self.lock)
                with self.assertRaisesRegex(dependencies.DependencyError, "npm_lock_inventory_drift"):
                    self.check_npm()

    def test_unreviewed_transitive_identity_and_unused_inventory_are_rejected(self):
        self.lock["packages"]["node_modules/unreviewed"] = {
            "version": "1.0.0", "license": "MIT",
        }
        self.write_json("package-lock.json", self.lock)
        with self.assertRaisesRegex(dependencies.DependencyError, "npm_dependency_unreviewed"):
            self.check_npm()
        del self.lock["packages"]["node_modules/unreviewed"]
        self.write_json("package-lock.json", self.lock)
        self.npm["unused@1.0.0"] = self.npm["example@1.2.3"]
        with self.assertRaisesRegex(dependencies.DependencyError, "npm_inventory_graph_drift"):
            self.check_npm()

    def test_notice_changes_and_new_unreviewed_notice_files_are_rejected(self):
        (self.npm_package / "LICENSE").write_text("tampered")
        with self.assertRaisesRegex(dependencies.DependencyError, "notice_drift"):
            self.check_npm()
        (self.npm_package / "LICENSE").write_bytes(self.license)
        (self.npm_package / "LICENCE.md").write_text("Previously unrecorded obligations")
        with self.assertRaisesRegex(dependencies.DependencyError, "notice_unreviewed"):
            self.check_npm()

    def test_changed_transitive_edges_are_rejected_even_when_identities_match(self):
        self.lock["packages"]["node_modules/example"]["dependencies"] = {"unreviewed": "*"}
        self.write_json("package-lock.json", self.lock)
        with self.assertRaisesRegex(dependencies.DependencyError, "npm_dependency_edges_drift"):
            self.check_npm()

    def test_bundled_identity_is_proven_by_its_immutable_parent_archive_not_invented_hashes(self):
        contents = {}
        with tarfile.open(self.npm_archive, "r:gz") as package:
            for member in package:
                contents[member.name] = package.extractfile(member).read()
        contents["package/node_modules/nested/package.json"] = json.dumps({
            "name": "nested", "version": "2.0.0", "license": "MIT",
        }).encode()
        archive = io.BytesIO()
        with tarfile.open(fileobj=archive, mode="w:gz") as package:
            for name, content in contents.items():
                member = tarfile.TarInfo(name)
                member.size = len(content)
                package.addfile(member, io.BytesIO(content))
        integrity = "sha512-" + base64.b64encode(hashlib.sha512(archive.getvalue()).digest()).decode()
        self.npm["example@1.2.3"]["integrity"] = integrity
        self.lock["packages"]["node_modules/example"]["integrity"] = integrity
        path = dependencies.npm_archive(self.npm_cache, integrity)
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(archive.getvalue())
        self.lock["packages"]["node_modules/example/node_modules/nested"] = {
            "version": "2.0.0", "inBundle": True, "license": "MIT",
        }
        self.write_json("package-lock.json", self.lock)
        self.check_npm()
        self.lock["packages"]["node_modules/example"]["optional"] = True
        self.write_json("package-lock.json", self.lock)
        content = path.read_bytes()
        path.unlink()
        shutil.rmtree(self.npm_package)
        with self.assertRaisesRegex(dependencies.DependencyError, "npm_bundle_archive_evidence_missing"):
            self.check_npm()
        with patch.object(dependencies.subprocess, "run", return_value=subprocess.CompletedProcess([], 1)):
            with self.assertRaisesRegex(dependencies.DependencyError, "npm_bundle_archive_preparation_failed"):
                dependencies.prepare_bundle_archives(self.root, self.npm, self.npm_cache)

        def populate(command, **kwargs):
            self.assertEqual(["npm", "cache", "add", self.npm["example@1.2.3"]["source"]], command[:4])
            self.assertEqual(self.root, kwargs["cwd"])
            path.write_bytes(content)
            return subprocess.CompletedProcess(command, 0)

        with patch.object(dependencies.subprocess, "run", side_effect=populate) as prepare:
            dependencies.prepare_bundle_archives(self.root, self.npm, self.npm_cache)
            dependencies.prepare_bundle_archives(self.root, self.npm, self.npm_cache)
        self.assertEqual(1, prepare.call_count)
        self.assertFalse(self.npm_package.exists())
        self.check_npm()
        self.lock["packages"]["node_modules/example/node_modules/nested"]["version"] = "2.0.1"
        self.write_json("package-lock.json", self.lock)
        with self.assertRaisesRegex(dependencies.DependencyError, "npm_bundle_metadata_drift"):
            self.check_npm()

    def test_missing_and_tampered_restored_archives_fail(self):
        self.npm_archive.write_bytes(b"changed")
        with self.assertRaisesRegex(dependencies.DependencyError, "npm_archive_integrity_drift"):
            self.check_npm()
        self.nuget_archive.unlink()
        with self.assertRaisesRegex(dependencies.DependencyError, "nuget_archive_evidence_missing"):
            self.check_nuget()

    def test_uninstalled_optional_package_is_pinned_but_not_falsely_verified(self):
        shutil.rmtree(self.npm_package)
        with self.assertRaisesRegex(dependencies.DependencyError, "npm_required_package_missing"):
            self.check_npm()
        self.lock["packages"]["node_modules/example"]["optional"] = True
        self.write_json("package-lock.json", self.lock)
        self.check_npm()
        self.lock["packages"]["node_modules/example"]["license"] = "unreviewed"
        self.write_json("package-lock.json", self.lock)
        with self.assertRaisesRegex(dependencies.DependencyError, "npm_lock_inventory_drift"):
            self.check_npm()

    def test_restored_platform_optional_still_requires_its_actual_notice_adoption(self):
        self.lock["packages"]["node_modules/example"]["optional"] = True
        self.write_json("package-lock.json", self.lock)
        self.npm["example@1.2.3"]["notices"] = {}
        absent = self.root / "other-platform-installation"
        shutil.move(self.npm_package, absent)
        self.check_npm()
        shutil.move(absent, self.npm_package)
        with self.assertRaisesRegex(dependencies.DependencyError, "notice_unreviewed"):
            self.check_npm()
        self.npm["example@1.2.3"]["notices"] = {"LICENSE": dependencies.sha256(self.license)}
        self.check_npm()

    def test_nuget_lock_content_hash_and_published_provenance_must_match(self):
        self.nuget_lock["dependencies"]["net10.0"]["Example"]["contentHash"] = "changed"
        self.write_json("src/Fixture/packages.lock.json", self.nuget_lock)
        with self.assertRaisesRegex(dependencies.DependencyError, "nuget_lock_inventory_drift"):
            self.check_nuget()
        self.nuget_lock["dependencies"]["net10.0"]["Example"]["contentHash"] = self.nuget["example@1.2.3"]["integrity"]
        self.write_json("src/Fixture/packages.lock.json", self.nuget_lock)
        modified = self.nuspec.replace(b"b" * 40, b"c" * 40)
        (self.nuget_package / "example.nuspec").write_bytes(modified)
        self.write_nuget_archive(modified)
        with self.assertRaisesRegex(dependencies.DependencyError, "nuget_provenance_drift"):
            self.check_nuget()

    def test_missing_or_changed_nuget_restore_source_is_not_proof(self):
        (self.nuget_package / ".nupkg.metadata").write_text(json.dumps({
            "source": "https://unreviewed.invalid/v3/index.json",
            "contentHash": self.nuget["example@1.2.3"]["integrity"],
        }))
        with self.assertRaisesRegex(dependencies.DependencyError, "nuget_restore_evidence_drift"):
            self.check_nuget()

    def test_central_nuget_version_changes_cannot_escape_lock_comparison(self):
        (self.root / "src/Fixture/Fixture.csproj").write_text(
            '<Project><ItemGroup><PackageReference Include="Example" /></ItemGroup></Project>',
        )
        central = self.root / "Directory.Packages.props"
        central.write_text('<Project><ItemGroup><PackageVersion Include="Example" Version="1.2.3" /></ItemGroup></Project>')
        self.check_nuget()
        central.write_text('<Project><ItemGroup><PackageVersion Include="Example" Version="1.2.4" /></ItemGroup></Project>')
        with self.assertRaisesRegex(dependencies.DependencyError, "nuget_central_version_unreviewed"):
            self.check_nuget()

    def test_floating_actions_and_images_fail_even_with_reviewed_package_names(self):
        workflow = self.root / ".github/workflows/product.yml"
        workflow.parent.mkdir(parents=True)
        workflow.write_text("steps:\n  - uses: actions/checkout@" + "a" * 40 + "\n")
        compose = self.root / "deploy/docker/compose.test.yml"
        compose.parent.mkdir(parents=True)
        image = "postgres:17@sha256:" + "b" * 64
        compose.write_text(f"services:\n  postgres:\n    image: {image}\n")
        inventory = {"actions": {"actions/checkout": "a" * 40}, "images": {"postgres": image}}
        dependencies.check_references(self.root, inventory)
        workflow.write_text("steps:\n  - uses: actions/checkout@v4\n")
        with self.assertRaisesRegex(dependencies.DependencyError, "action_reference_drift"):
            dependencies.check_references(self.root, inventory)
        workflow.write_text("steps:\n  - uses: actions/checkout@" + "a" * 40 + "\n")
        compose.write_text("services:\n  postgres:\n    image: postgres:17\n")
        with self.assertRaisesRegex(dependencies.DependencyError, "image_reference_drift"):
            dependencies.check_references(self.root, inventory)


if __name__ == "__main__":
    unittest.main()
