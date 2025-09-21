# Release Policy and Configuration

This document describes the release process, required secrets, and manual steps for enabling security features in the Nexo project.

## Release Workflow Overview

The release workflow (`.github/workflows/release.yml`) is triggered by:
- **Tagged releases**: Push tags starting with `v*` (e.g., `v1.0.0`)
- **Pull requests**: Changes to source code, tests, or workflow files

### Workflow Jobs

1. **build-pack**: Builds, tests, packages, and generates SBOM
2. **sign-and-publish**: Signs and publishes packages (requires secrets)
3. **container**: Builds and signs container images (requires secrets)
4. **security-scan**: Runs vulnerability scans
5. **release-notes**: Generates and creates GitHub releases

## Required Secrets

### For Package Publishing

#### Option 1: NuGet.org Publishing
```bash
# Required secrets in GitHub repository settings
NUGET_API_KEY=<your-nuget-api-key>
NUGET_USERNAME=<your-nuget-username>
```

#### Option 2: GitHub Packages Publishing
```bash
# Uses default GITHUB_TOKEN (automatically provided)
# No additional secrets required
```

### For Artifact Signing

#### Cosign Signing (Recommended)
```bash
# Generate a new key pair
cosign generate-key-pair

# Add secrets to GitHub repository settings
COSIGN_PRIVATE_KEY=<private-key-content>
COSIGN_PASSWORD=<password-for-private-key>
```

#### Alternative: Sigstore (Keyless)
```bash
# No secrets required - uses GitHub OIDC
# Automatically enabled when COSIGN_PRIVATE_KEY is not set
```

### For Container Registry

```bash
# Uses default GITHUB_TOKEN for GitHub Container Registry
# For other registries, add:
CONTAINER_REGISTRY_USERNAME=<username>
CONTAINER_REGISTRY_PASSWORD=<password>
```

## Manual Setup Steps

### 1. Enable GitHub Environments

1. Go to repository **Settings** → **Environments**
2. Create a new environment called `release`
3. Add protection rules if needed (e.g., require approval for production releases)

### 2. Configure Repository Secrets

1. Go to repository **Settings** → **Secrets and variables** → **Actions**
2. Add the required secrets listed above
3. Ensure secrets are scoped to the `release` environment

### 3. Enable GitHub Container Registry

1. Go to repository **Settings** → **Actions** → **General**
2. Under "Workflow permissions", ensure "Read and write permissions" is selected
3. Check "Allow GitHub Actions to create and approve pull requests"

### 4. Configure Package Publishing

#### For NuGet.org:
1. Create account at [nuget.org](https://www.nuget.org)
2. Generate API key in account settings
3. Add `NUGET_API_KEY` and `NUGET_USERNAME` secrets

#### For GitHub Packages:
1. No additional setup required
2. Packages will be published to `ghcr.io/your-org/nexo`

### 5. Enable Security Scanning

1. Go to repository **Security** → **Code scanning**
2. Enable "CodeQL" if not already enabled
3. The workflow will automatically upload Trivy scan results

### 6. Configure Release Drafter (Optional)

1. Create `.github/release-drafter.yml` configuration
2. Customize release note templates
3. Enable automatic release note generation

## Release Process

### Creating a Release

1. **Prepare the release**:
   ```bash
   # Update version in Directory.Build.props
   # Update CHANGELOG.md
   # Create release branch
   git checkout -b release/v1.0.0
   ```

2. **Create and push tag**:
   ```bash
   # Create annotated tag
   git tag -a v1.0.0 -m "Release v1.0.0"
   git push origin v1.0.0
   ```

3. **Monitor workflow**:
   - Check Actions tab for workflow progress
   - Verify all jobs complete successfully
   - Review generated artifacts

4. **Verify release**:
   - Check package registry for published packages
   - Verify container images are available
   - Review security scan results

### Dry Run Testing

To test the workflow without creating a release:

1. **Create a test branch**:
   ```bash
   git checkout -b test/release-workflow
   ```

2. **Make a small change**:
   ```bash
   # Edit any file in src/ or tests/
   echo "# Test" >> README.md
   git add . && git commit -m "Test release workflow"
   ```

3. **Create pull request**:
   - The workflow will run the `build-pack` job
   - Verify SBOM generation and artifact upload
   - Check that no publishing occurs

## Security Features

### Software Bill of Materials (SBOM)

- **Format**: CycloneDX JSON and XML
- **Scope**: All project dependencies and dev dependencies
- **Location**: Uploaded as workflow artifacts
- **Usage**: Security auditing, compliance, vulnerability tracking

### Artifact Signing

- **Method**: Cosign with Sigstore
- **Scope**: NuGet packages and container images
- **Verification**: `cosign verify-blob` and `cosign verify`
- **Attestation**: Cryptographic proof of artifact integrity

### Vulnerability Scanning

- **Tool**: Trivy
- **Scope**: Filesystem and container images
- **Output**: SARIF format for GitHub Security tab
- **Frequency**: Every release

## Troubleshooting

### Common Issues

1. **Package publishing fails**:
   - Verify API keys are correct
   - Check package version doesn't already exist
   - Ensure package metadata is valid

2. **Signing fails**:
   - Verify Cosign keys are properly formatted
   - Check key permissions and password
   - Ensure Sigstore connectivity

3. **Container build fails**:
   - Verify Dockerfile.cli exists
   - Check container registry permissions
   - Review build arguments and context

4. **SBOM generation fails**:
   - Ensure CycloneDX tool is installed
   - Check solution file path
   - Verify package restore completed

### Debug Mode

Enable verbose logging by adding to workflow:
```yaml
- name: Build solution
  run: dotnet build ${{ env.SOLUTION_FILE }} --configuration Release --verbosity diagnostic
```

### Manual Verification

Test components individually:
```bash
# Test package creation
dotnet pack --configuration Release

# Test SBOM generation
dotnet tool install --global CycloneDX
cyclonedx-dotnet Nexo.sln

# Test container build
docker build -f Dockerfile.cli -t nexo:test .

# Test signing (if configured)
cosign sign --yes nexo:test
```

## Compliance and Auditing

### SBOM Verification

```bash
# Download SBOM from workflow artifacts
# Verify CycloneDX format
cat sbom.json | jq '.bomFormat'  # Should be "CycloneDX"

# Check component count
cat sbom.json | jq '.components | length'
```

### Signature Verification

```bash
# Verify package signatures
cosign verify-blob --certificate-identity="*" --certificate-oidc-issuer="*" package.nupkg

# Verify container signatures
cosign verify --certificate-identity="*" --certificate-oidc-issuer="*" ghcr.io/org/nexo:latest
```

### Security Scan Review

1. Go to repository **Security** → **Code scanning**
2. Review Trivy scan results
3. Address any high/critical vulnerabilities
4. Update dependencies as needed

## Best Practices

1. **Version Management**:
   - Use semantic versioning (semver)
   - Update version in Directory.Build.props
   - Tag releases with `v` prefix

2. **Security**:
   - Rotate signing keys regularly
   - Monitor vulnerability databases
   - Keep dependencies updated

3. **Documentation**:
   - Update CHANGELOG.md for each release
   - Document breaking changes
   - Include migration guides

4. **Testing**:
   - Test release workflow in feature branches
   - Verify all artifacts before release
   - Test installation and basic functionality

5. **Monitoring**:
   - Monitor workflow success rates
   - Track security scan results
   - Review release metrics

## Support

For issues with the release process:

1. Check workflow logs in Actions tab
2. Review this documentation
3. Create an issue with workflow logs
4. Contact maintainers for security-related issues

---

**Note**: This release process is designed for security and compliance. All artifacts are signed and verified, and comprehensive SBOMs are generated for audit purposes.
