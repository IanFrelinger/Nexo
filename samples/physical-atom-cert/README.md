# Physical-atom certificate sample (Phase 0)

**Maturity:** Prototype — sample issuer key is for documentation and CI only, not production PKI.

## Files

| File | Purpose |
|------|---------|
| `instance-scope.example.json` | Signed `Instance` scope certificate |
| `design-scope.bundle.json` | Self-contained Design-scope certified bundle manifest (Phase 1) |
| `design-scope.tag-qr.txt` | QR payload for the design-scope bundle (Phase 2) |
| `issuer-public-key.sample.b64` | Base64 Ed25519 public key (32 bytes) for verification |

## Bound asset

The certificate binds to UTF-8 bytes of the string:

```
sample-digital-twin-asset-v1
```

SHA-256 hex (`assetHash` in the JSON): `18301e3630ca2816dcb1e23264aaec41d9ed4108337c9b5a936e39565d01c742`

## Verify (headless)

```csharp
using Ashlar.Certification.Physical;

var cert = /* deserialize instance-scope.example.json */;
var assetBytes = System.Text.Encoding.UTF8.GetBytes("sample-digital-twin-asset-v1");
var issuerPublicKey = Convert.FromBase64String(
    File.ReadAllText("samples/physical-atom-cert/issuer-public-key.sample.b64").Trim());

var result = PhysicalAtomCertificateVerifier.Verify(cert, assetBytes, issuerPublicKey);
// result.Trusted == true
```

Hermetic replay: `PhysicalAtomSampleCertTests` and `PhysicalAtomCertBundleManifestTests` in the cert-gate suite.

## Spec

See `docs/physical-atom-phase0-spec.md`.
