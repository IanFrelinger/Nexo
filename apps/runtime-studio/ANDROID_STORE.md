# Android / Play publishing (out of repo)

This repository is **Linux-first** and no longer ships a native Android or MAUI client. Packaging an app for Google Play (AAB/APK, signing, tracks) belongs in a **separate application repo** that consumes **`Ashlar.Client`** (NuGet) or calls **`Ashlar.API`** over HTTPS.

For workload-free reference clients that run on Linux CI, see **`docs/demos/README.md`** and **`Ashlar.Demos.sln`**.
