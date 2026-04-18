# Google Play — Internal testing track

Use the **internal testing** track for fast iteration before open testing or production. The repo produces a signed **AAB** via [maui-android-publish.yml](../../.github/workflows/maui-android-publish.yml); uploading it is a Play Console operation (or API).

## Manual path (fastest first time)

1. Run the **MAUI Android AAB Publish** workflow (or `dotnet publish` locally per [ANDROID_STORE.md](./ANDROID_STORE.md)).
2. Download the **`nexo-client-mobile-aab`** artifact from the workflow run.
3. Play Console → **Testing** → **Internal testing** → create release → upload the AAB.
4. Add testers by email list or Google Group; they receive an opt-in link.

## Automation (CI → Play)

1. Create a **Google Play Android Developer API** service account with permission to release to testing tracks; download JSON.
2. Link the service account in Play Console (**API access**).
3. Store the JSON as a GitHub Actions secret (e.g. `PLAY_SERVICE_ACCOUNT_JSON`).
4. Add a guarded job to your private workflow using [upload-google-play](https://github.com/r0adkll/upload-google-play-android) or `fastlane supply`:

   - `packageName`: must match `ApplicationId` in `Nexo.Client.Mobile.csproj`.
   - `releaseFiles`: path to the built `.aab`.
   - `track`: `internal`.

Do **not** commit the service account JSON. Rotate keys if leaked.

## Version codes

Each Play upload must increase **`ApplicationVersion`** (versionCode) in the mobile project. Bump it before tagging a release build.
