# Nexo.Guide Mac Catalyst crash (Swift Observation)

## What happens

On some macOS versions, Nexo.Guide can crash shortly after launch when run as a Mac Catalyst app (`net9.0-maccatalyst`). **MAUI 9 does not fix it**—the same crash occurs. The crash is **inside Apple’s frameworks**, not in our C# code.

- **Exception:** `EXC_BAD_ACCESS (SIGSEGV)` at address `0x000000000000003f`
- **Where:** `libswiftObservation.dylib` → `ObservationTracking._AccessList.addAccess`  
  Called from UIKit when updating window scene traits:  
  `UIWindowScene` → `_updateSceneTraitsAndPushTraitsToScreen` → `UINSWindowProxyFocusHelper _updateIfAppearsKeyChanged` → Swift Observation.

So the fault is in the **Swift Observation** runtime during a **trait collection update** on the Mac Catalyst bridge. This can occur on:

- **macOS 26.x (beta)** and possibly other versions where UIKit (iOSSupport) uses Swift Observation for trait state.
- At launch (scene creation) or within seconds (trait update). On macOS 26.3 (25D5112c) it is reproducible.

## What we did in code

To reduce the chance of related issues and to avoid Catalyst state-restoration paths that can also crash:

- **Mac Catalyst `Info.plist`**  
  - `UIApplicationSupportsStateRestoration` = `false`  
  So we don’t rely on or trigger state save/restore.

This doesn’t fix the Swift Observation bug inside the OS/frameworks, but it avoids state-restoration code paths and is good practice for Catalyst.

## What you can do

1. **Use a non-beta macOS**  
   If you’re on macOS 26 (e.g. 25D5112c), try running on a stable release (e.g. macOS 14 or 15). This crash has been seen on macOS 26.3; it may be fixed or different on stable builds.

2. **Run on iOS Simulator instead (recommended on macOS 26)**  
   Same app, no Catalyst stack—avoids the crash. From repo root:
   ```bash
   ./scripts/run-guide-ios-simulator.sh
   ```
   Or: `dotnet run --project src/Nexo.Guide/Nexo.Guide.csproj -f net9.0-ios -p:TreatWarningsAsErrors=false`  
   (Requires Xcode and an iOS simulator; start a simulator first if needed.)

3. **Run the app on other targets**  
   - **Android** or **Windows** if you need a desktop-style UI without Catalyst.

4. **Report upstream**  
   If you have a reproducible case (especially on a stable macOS version), consider opening an issue with:
   - [dotnet/maui](https://github.com/dotnet/maui/issues) (Mac Catalyst lifecycle / windowing)
   - [dotnet/macios](https://github.com/dotnet/macios/issues) (iOS/Catalyst native bridge)  
   and attach the crash report (Process: Nexo.Guide, Exception: EXC_BAD_ACCESS, thread 0 in `libswiftObservation.dylib` / `ObservationTracking._AccessList.addAccess` and the UIKit stack above it).

## References

- MAUI multi-window Catalyst: [dotnet/maui#18093](https://github.com/dotnet/maui/issues/18093), [dotnet/maui#17841](https://github.com/dotnet/maui/issues/17841)
- Xcode/.NET 8 compatibility: [dotnet/maui#21057](https://github.com/dotnet/maui/issues/21057), [dotnet/macios#20257](https://github.com/dotnet/macios/issues/20257)
- Apple: trait collection changes (e.g. `traitCollectionDidChange`) and Swift Observation in UIKit on newer OS versions
