# Decision memo — one operator identity, or two?

*27 August 2026. Open decision 2 from `STATE-2026-08-27.md`. Researched against the code by
twelve agents (five mapping, three designing, three adversarially judging, one synthesizing);
every claim below was then re-verified by hand. Nothing here was built or run — no .NET SDK
is obtainable in the authoring environment.*

---

## The recommendation

**One identity: certification records sign with the operator keypair.** But take the answer
without the architecture that was proposed to carry it, and **ship the security fix first,
separately, because it does not depend on this decision at all.**

Concretely: resolve `~/.ashlar/keys/operator.key` inside `Ashlar.Infrastructure` and pass it
to the `ed25519PrivateKeyBase64` parameter `CertificationRecordSigner` already has. No new
project, no `Infrastructure → Manifest` reference, no new NuGet package.

---

## The finding that reframes the question

**The identity split never blocked the security work, and the hole is worse than recorded.**

`CertificationRecordSigning.BuildPayload` begins:

```csharp
if (record.SchemaVersion is null)
    return BuildLegacyPayload(record);        // CertificationRecordSigning.cs:106
```

`BuildLegacyPayload` (`:144-163`) contains **zero Ed25519 references**. So the downgrade
does not require stripping `ed25519Signature` as previously documented — setting
`schemaVersion` to null is enough, and lands the record on a payload the strong signature
never covered. Recompute the HMAC under the committed public constant and it verifies.

**No minimum schema version is checked anywhere.** Repo-wide grep for `SchemaVersion >=`,
`SchemaVersion <`, `MinimumSchema`, `MinSchema`: zero non-test hits. The only non-test
stamping site is `CertificationGate.cs:424`, and nothing compares against it.

The planning consequence is what matters: **any design that hardens a new schema version
while v1/v2 stay verifiable under the committed constant closes nothing** — the attacker
mints an old record and never meets the new lane. The missing control is a version floor,
and a floor is identity-independent. It can ship this week.

---

## The three options

### Option A — Unify on the operator keypair · **RECOMMENDED (answer), REJECTED (architecture)**

Certification signs with `~/.ashlar/keys`. `ashlar keys init` becomes the whole story.

**For:** the only option that *executes* accepted SPEC-006 §1 ("v1 trust is a single local
operator keypair… It replaces the committed dev-HMAC") rather than amending it. It is also
the only one that makes the shipped CLI truthful — see the falsehoods listed below. Drops
what a user must configure from three things to one.

**Against, as proposed:** the proposal wrapped the answer in an `Ashlar.Signing` leaf
project, a throw-on-missing-key default, and HMAC-free v3 records. All three are wrong:

- The leaf adds an 18th project to the `Ashlar.Hosting` pack graph and silently drops
  `src/Ashlar.Manifest/**` from the cross-OS portability gate's trigger paths.
- Throwing on a missing key breaks 17 `new CertificationGate(` sites, 18
  `AddCertificationGate` registrations, both shipped tools, and
  `scripts/pack-certified-brick-reuse.sh` on any keyless runner.
- HMAC-free v3 records are rejected *before* the Ed25519 check ever runs
  (`CertificationTrustVerifier.cs:29-30`, `CertificationRecordSigner.cs:79`) and would be
  reported ABSENT by the store — every certified brick would silently vanish.

### Option B — Two named identities (builder machine / operator human) · **REJECTED, harvested**

**For:** it has the only real trust root of the three — extending the pin set requires an
Ed25519 signature under `operator.key`, not a file drop. Its presence-activated versioning
(mint v3 only when a key exists) is the best migration idea in the set, and putting
`SignerRole`/`SignerFingerprint` *inside* the signed bytes is the precondition for ever
asking "which key should have signed this". Both are grafted into the recommendation.

**Against:** it formalizes a boundary that, in the only deployment this repo evidences, runs
down the middle of one laptop — two seeds in one directory, same owner, same permissions.
There is no evidence of the build machine its premise requires: zero `ASHLAR_CERT_*`
references in `.github/workflows`, and no `certify` verb in the CLI. It must amend an
accepted spec to legitimize an accident.

### Option C — Shared signing primitive, identities left split · **REJECTED**

**Against:** it does not answer the question, and makes the wrong answer cheaper to keep.
Every user-facing falsehood survives verbatim. Its headline feature is a security
regression: it offers pin-checking on netstandard2.0, where the signature math is compiled
out — so a forged record naming the operator's genuine public key returns TRUSTED *with a
pinned stamp*. That is worse than today's silent skip. **netstandard2.0 must refuse what it
cannot verify, never pin it.**

---

## Why the merge is nearly free

The audit's premise — "the split is structural, and netstandard2.0 rules out moving NSec
down" — is true but not load-bearing. It blocks moving the operator *types* into
`Ashlar.Certification.Contracts`. It does not block the merge:

| Fact | Evidence |
|---|---|
| `operator.key` is base64 of a raw 32-byte Ed25519 seed | `OperatorKey.cs:59,:68` |
| `CertificationRecordSigner` already takes `string? ed25519PrivateKeyBase64` | `CertificationRecordSigner.cs:34` |
| `ResolvePrivateKey` already validates exactly 32 raw bytes | `CertificationRecordEd25519.cs:90-111` |

The minting side merges by reading one file and passing one string, in a layer that already
does file I/O. Note `SigningIdentity` deliberately exposes **no seed accessor**
(`OperatorKey.cs:145-188`) — so route around it by reading the file, and never add a
private-key export API.

---

## What the CLI currently claims that is not true

`ashlar keys init` **provably cannot reach the certification signer**: `Ashlar.Infrastructure`
has no reference to `Ashlar.Manifest`, and `grep -rn ASHLAR_CERT application/` returns zero
hits. Yet:

- `KeysCommand.cs:58` — "gate decisions on this machine are now signed". True of `GateStore`;
  false of `CertificationGate`, which `README.md:17` calls "the gate".
- `VerifyCommand.cs:157` — "unsigned — run `ashlar keys init` to certify".
- `ExportCommand.cs:213-215` — stamps `✓ CERTIFIED bundle · signed ed25519:…` over bundles
  whose brick records are dev-HMAC signed.

This is the strongest argument for the merge, and it is a correctness problem rather than an
architectural preference. Nothing has shipped (`git tag` is empty), so this is the cheapest
moment this decision will ever have.

---

## Sequencing

Steps 1–2 are the security fix and **do not depend on the identity decision**. Everything
except step 6 needs a .NET SDK.

0. **Fix `CompositionCertificationRecordSigner`, alone.** It discards its injected signer
   (`_ = brickSigner;`) and reads the env var directly, and computes `UsesDevKey()` with no
   argument so the flag reports ambient state rather than its own key. SPEC-006 S-4's only
   stated migration path is already broken for compositions. Also give
   `FileCertificationRecordStore` a logger — it currently returns null on a verification
   failure, so any later strictness change presents as "my brick vanished".
1. **Add `CertificationVerifyOptions`** (`MinimumSchemaVersion`, `RequireEd25519`,
   `TrustedPublicKeys`, `HmacKey`) with no `#if` — it declares no crypto. Defaults reproduce
   today's semantics exactly.
2. **The floor is the control.** Refuse records below the floor. Thread the same options
   into `CertificationRecordSigner.Verify` — the second tier, gating five call sites
   including the record store, which all three proposals missed. Add the ns2.0 `#else` that
   *refuses* a record it cannot verify.
3. **The merge:** `OperatorCertificationIdentity` in `Ashlar.Infrastructure`, resolution
   order explicit → `operator.key` → `ASHLAR_CERT_ED25519_KEY` (kept, deprecated) → none.
   Expose as a static factory, not a constructor change — 8 of 31 construction sites pass
   arguments and two pass the key positionally.
4. **Schema v3, presence-activated and HMAC dual-writing.** Stamp v3 only when an operator
   key resolved; otherwise keep stamping 2, so a keyless runner is byte-identical to today.
   Do not touch `BuildLegacyPayload` — its bytes are pinned by test.
5. **Pin against `operator.pub` only.** Do **not** enumerate `trusted/`:
   `OperatorKey.Generate(rotate: true)` writes the *previous* public key there and there is
   no revocation (`OperatorKey.cs:45-52,:28`), so `keys init --rotate` after a suspected
   theft would permanently re-authorize the stolen key — an action that reads as remediation
   and is worse than a no-op.
6. **CLI copy, as a separate PR against an `application/` base** —
   `.github/workflows/layer-boundary.yml` fails any PR touching both `src/` and
   `application/` without a `[coordinated-integration]` token.
7. **Flip defaults last**, after records migrate, starting with
   `SelfProducedBrickCertificationPolicy` — the one live flow where a forged certification
   record admits a `repo.fs.write` that is then stamped as an operator-signed, mesh-shareable
   GateRecord.

---

## Corrections this research forced

- **The identity decision was described as "the gate on the remaining trust work". It is
  not.** Every security gain is separable from it. The floor should ship first.
- **Limitation 7's attack was understated.** Stripping the signature is the *harder* path;
  nulling `schemaVersion` is easier and defeats more. Recorded as limitation 8.
- **SPEC-006 §4's canonical-form claim was false** and is now corrected in the spec: the two
  sides never shared a canonical form (`CanonicalJson.cs:29-36` ordinal-sorts and omits
  nulls; `CertificationRecordSigning.cs:91,:141` is camelCase, declaration-ordered, nulls
  written). Converging them would invalidate every record and every signature at once.
- **"~30 call sites must opt in" overstated production cost tenfold** — 28 of 31 are tests;
  three are production. It *understated* the test cost of any throw-on-missing-key design.
- **`record.Signed` is not a backstop.** It is a `required bool ... init` data field the
  attacker simply leaves `true`.

## Still unknown

- Nothing here was compiled. Whether the ns2.0 inner build accepts `CertificationVerifyOptions`
  under `TreatWarningsAsErrors=true` is unverified, and three CI gates depend on it.
- Whether v3 leaves v1/v2 bytes untouched is provable only by running the byte-pinned tests.
- Whether any real external consumer resolves the ns2.0 asset at all. No in-repo ns2.0-only
  consumer was found. If none exists, a net8-only verification package would be cheaper than
  all of this and the HMAC could go entirely.
- Whether `applications/Ashlar.Certification.Physical` — a third live Ed25519 path with an
  8-byte fingerprint frozen into a QR/NDEF layout — is in scope. The recommendation scopes it
  out, so "one identity" remains an overclaim until the owner ratifies that.
