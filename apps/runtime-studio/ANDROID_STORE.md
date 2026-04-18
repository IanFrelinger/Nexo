# MAUI Android — signing and store prep

The repo ships two CI lanes:

| Workflow | Purpose |
|----------|---------|
| [`.github/workflows/maui-client-build-gate.yml`](../../.github/workflows/maui-client-build-gate.yml) | Fast **compile** checks (Windows, Mac Catalyst, Android). |
| [`.github/workflows/maui-android-publish.yml`](../../.github/workflows/maui-android-publish.yml) | **Release AAB** artifact (`dotnet publish` with `AndroidPackageFormat=aab`). Runs on **workflow_dispatch**, weekly schedule, or when this workflow file changes on `main`/`master`. |

## Local AAB

From repo root (Android workload + JDK 17 installed):

```bash
dotnet publish src/Nexo.Client.Mobile/Nexo.Client.Mobile.csproj \
  -f net8.0-android -c Release \
  -p:AndroidPackageFormat=aab
```

Output is under `src/Nexo.Client.Mobile/bin/Release/net8.0-android/publish/`.

## CI signing with your keystore (GitHub Actions)

1. Create a **release** keystore (Play App Signing can still wrap your upload key).
2. Base64-encode the `.jks` / `.keystore` file for a secret (example name `ANDROID_KEYSTORE_B64`).
3. In the repository **Secrets and variables → Actions**, add:

| Secret | Example |
|--------|---------|
| `ANDROID_KEYSTORE_B64` | Base64 of keystore bytes |
| `ANDROID_KEYSTORE_PASSWORD` | Keystore password |
| `ANDROID_KEY_ALIAS` | Key alias |
| `ANDROID_KEY_PASSWORD` | Key password (often same as keystore) |

4. Extend `maui-android-publish.yml` (or a private workflow) with a step **before** `dotnet publish` that writes the keystore to disk from `ANDROID_KEYSTORE_B64` and passes MSBuild properties:

```text
-p:AndroidSigningKeyStore=path/to/release.keystore
-p:AndroidSigningStorePass=***
-p:AndroidSigningKeyAlias=***
-p:AndroidSigningKeyPass=***
```

Do **not** commit keystores or passwords. Prefer [Play App Signing](https://support.google.com/googleplay/android-developer/answer/9842756) so Google holds the app signing key.

## Store checklist (manual)

- Bump `ApplicationVersion` / `ApplicationDisplayVersion` in `Nexo.Client.Mobile.csproj`.
- Set `ApplicationId` to your final package name before first Play upload.
- Privacy policy URL, content rating, and data safety forms in Play Console.
- Internal testing track → production rollout.
