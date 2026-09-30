# Unity IPA project

This Unity client targets Unity `6000.6.3f1`, matching the Editor installed at `D:\Unity\Editors\6000.6.3f1`. iOS Build Support is installed. The Editor generates the prototype scene and `GameServerConfig.asset` the first time it opens the project.

See the project overview at [`../README.md`](../README.md) and the online contract at [`docs/UNITY_ONLINE_CONTRACT.md`](docs/UNITY_ONLINE_CONTRACT.md).

## GitHub build

`.github/workflows/build-ios.yml` builds the Xcode project on a GitHub macOS runner. It can produce an unsigned Xcode artifact after Unity activation is configured. A testable IPA requires an Apple Developer signing certificate and provisioning profile that match `IOS_BUNDLE_ID`.

For a Personal license, activate it in Unity Hub and add the `.ulf` file contents plus `UNITY_EMAIL` and `UNITY_PASSWORD` as repository Actions secrets. On Windows, Unity Hub normally writes the license to `C:\ProgramData\Unity\Unity_lic.ulf`; use **Preferences > Licenses > Add > Get a free personal license** to create the file if needed. Do not put Unity or Apple credentials in the repository.

To export a signed IPA, configure the Actions variable `IOS_BUNDLE_ID` and secrets `IOS_TEAM_ID`, `IOS_CERTIFICATE_P12_BASE64`, `IOS_CERTIFICATE_PASSWORD`, and `IOS_PROFILE_BASE64`. The bundle ID in the profile must match the variable. `IPA_SERVER_URL` is still unset because the dedicated IPA server does not yet have a host or HTTPS address.

The first IPA stays small by leaving downloadable content out of the app bundle. At startup, the patcher checks `version_manifest.json` under `ASSET_CDN_URL`, verifies bundle size and SHA-256, resumes partial downloads when the CDN supports HTTP Range, and saves files in the app's persistent data folder. AssetBundles are loaded on demand with `AssetDownloadManager.LoadAssetAsync<T>`. To publish content, assign bundle names to assets in Unity, use **iOSVN > Assets > Build iOS downloadable bundles**, and upload the generated `build/AssetBundles/iOS` directory to an HTTPS host. Until `ASSET_CDN_URL` is configured, the app skips patching and opens the prototype login screen.
