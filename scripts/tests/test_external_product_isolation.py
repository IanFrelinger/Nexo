#!/usr/bin/env python3
"""Guard packaging-lane consumer isolation from monorepo MSBuild imports."""
from __future__ import annotations

import subprocess
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
VERIFY = ROOT / "scripts" / "verify-external-product-shape.sh"

REPO_BLEED = """<Project>
  <ItemGroup>
    <PackageReference Include="System.Text.Encodings.Web" />
    <PackageReference Include="System.Text.RegularExpressions" />
  </ItemGroup>
</Project>
"""

ISOLATION_PROPS = """<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="System.Text.Encodings.Web" Version="10.0.11" />
    <PackageReference Include="System.Text.RegularExpressions" Version="4.3.1" />
  </ItemGroup>
</Project>
"""

MINIMAL_CSPROJ = """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
"""


class ExternalProductIsolationTests(unittest.TestCase):
    def test_verify_script_writes_consumer_isolation_files(self) -> None:
        text = VERIFY.read_text(encoding="utf-8")
        self.assertIn('cat > "${CONSUMER_ROOT}/Directory.Build.props"', text)
        self.assertIn('cat > "${CONSUMER_ROOT}/Directory.Build.targets"', text)
        self.assertIn('System.Text.Encodings.Web" Version="10.0.11"', text)
        self.assertIn("ManagePackageVersionsCentrally>false", text)

    def test_repo_props_bleed_is_nu1015_without_isolation(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "Directory.Build.props").write_text(REPO_BLEED, encoding="utf-8")
            consumer = root / "consumer"
            consumer.mkdir()
            proj = consumer / "Demo.csproj"
            proj.write_text(MINIMAL_CSPROJ, encoding="utf-8")
            run = subprocess.run(
                ["dotnet", "restore", str(proj), "-v", "q"],
                cwd=consumer,
                text=True,
                capture_output=True,
                check=False,
            )
        combined = run.stdout + run.stderr
        self.assertNotEqual(0, run.returncode, combined)
        self.assertIn("NU1015", combined)

    def test_consumer_isolation_stops_versionless_bleed(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "Directory.Build.props").write_text(REPO_BLEED, encoding="utf-8")
            consumer = root / "consumer"
            consumer.mkdir()
            (consumer / "Directory.Build.props").write_text(ISOLATION_PROPS, encoding="utf-8")
            (consumer / "Directory.Build.targets").write_text(
                "<Project />\n", encoding="utf-8"
            )
            proj = consumer / "Demo.csproj"
            proj.write_text(MINIMAL_CSPROJ, encoding="utf-8")
            run = subprocess.run(
                ["dotnet", "restore", str(proj), "-v", "q"],
                cwd=consumer,
                text=True,
                capture_output=True,
                check=False,
            )
        combined = run.stdout + run.stderr
        self.assertEqual(0, run.returncode, combined)
        self.assertNotIn("NU1015", combined)


if __name__ == "__main__":
    unittest.main()
