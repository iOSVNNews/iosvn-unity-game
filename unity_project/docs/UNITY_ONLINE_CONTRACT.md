# Unity online vertical slice

## Current direction

- The iOS game is a Unity app with its **own dedicated game server**. The AWS game is a source of gameplay rules and a foundation to develop from; Unity will not connect to the AWS game service as its production server.
- The Unity app uses a standalone email account, not Telegram login.
- `D:\Telegram\Payload\App.app` is a UX reference for compact 2D mobile navigation, account flow, cloud save and live updates. This project does not copy its code or assets.
- Initial vertical slice: email account with Gmail verification, character creation, profile, world target list, and a server-authoritative hunt. A bundled offline map preview is available before the dedicated server is configured.

## Current project state

The Unity project targets the installed Unity `6000.6.3f1` Editor on drive D. An editor setup script creates a starter scene, and runtime UI provides email login, Gmail verification, character creation, profile, target list, and basic server-authoritative combat. The login screen also has an offline preview of the bundled world atlas for map feedback before a game server is hosted.

The first local implementation is in `ipa_server.js` and `email_auth_store.js`. Its gameplay files in `ipa_core/` were copied from the running AWS game source on 2026-09-30; `ipa_core/PROVENANCE.md` records the source hashes. The current root `server.js` and root game files remain the Telegram Mini App branch. The IPA server uses server-issued email identities and an independent save file. The dedicated server has not been hosted, and its API URL is not configured in the Unity build.

## Dedicated server contract

`GameServerConfig.apiBaseUrl` is the HTTPS root of the separate game server, ending in `/api`. The client expects:

- `POST /auth/email/register` with `{ "email", "password" }`; sends a six-digit Gmail verification code and returns `verificationRequired`.
- `POST /auth/email/verify` with `{ "email", "code" }`; verifies the mailbox and returns `{ "ok": true, "accessToken", "expiresAt" }`.
- `POST /auth/email/resend` with `{ "email" }`; sends a replacement code when the account is awaiting verification.
- `POST /auth/email/login` with `{ "email", "password" }`; verified accounts receive `{ "ok": true, "accessToken", "expiresAt" }`.
- Authenticated `GET /state` returns game state.
- Authenticated `POST /register` creates a character with `{ "name", "gender", "mon", "he" }`.
- Authenticated `POST /breakthrough` asks the game server to validate and resolve a cultivation breakthrough.
- Authenticated `GET /world/monsters` lists current targets.
- Authenticated `POST /world/hunt` accepts `{ "monsterUid" }`; combat and rewards are computed by the server.
- Authenticated `GET /battle/current` reads the active server battle.
- Authenticated `POST /battle/act` accepts `{ "a": "attack|dodge|flee" }` and returns the authoritative updated battle.
- Authenticated `POST /auth/logout` revokes the bearer session.

The server stores password hashes with scrypt and stores random bearer tokens by hash with expiry/revocation. New accounts must verify a six-digit code sent by Gmail before they can log in; codes expire after ten minutes, are stored only as hashes, and have a resend cooldown and attempt limit. Login, registration, resend, and verification requests are rate-limited. Existing version-1 accounts are migrated as verified. Password recovery is not implemented yet. Do not store passwords, Gmail credentials, or long-lived bearer tokens in the Unity client; the client keeps its access token in memory only.

Configure `GMAIL_SMTP_USER` and `GMAIL_SMTP_APP_PASSWORD` on the dedicated IPA server. The second value must be a Google App Password for the sending mailbox, not its normal account password. Store both in the server host's secret environment settings. Registration remains unavailable until these are configured. Set the GitHub Actions variable `IPA_SERVER_URL` to the HTTPS API base ending in `/api` after the separate host is ready. The AWS Telegram Mini App does not implement this email-account API.

## Opening the Unity project

Open `D:\game_iosvn\unity_project` with Unity **6000.6.3f1**. On first editor load, a setup script creates `Assets/Scenes/OnlinePrototype.unity`, registers it as the first build scene and creates `Assets/Resources/GameServerConfig.asset`. Set `apiBaseUrl` to the separate server's HTTPS address before using online features; set `assetCdnBaseUrl` to enable startup content downloads. The login screen's offline preview reads `Resources/MapCatalog.json` and allows map browsing only; online actions remain disabled there.

The Editor and iOS Build Support are installed on drive D. `.github/workflows/build-ios.yml` uses GameCI Unity Builder v5.0.0 to export an Xcode project and can optionally sign/export an IPA on a macOS runner. A signed IPA needs GitHub Actions secrets for the Unity license and Apple signing certificate/provisioning profile, plus the bundle ID and Apple team ID.

The repository workflow reads `IOS_BUNDLE_ID`, `IPA_SERVER_URL`, and `ASSET_CDN_URL` from GitHub Actions Variables. It reads `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`, `IOS_TEAM_ID`, `IOS_CERTIFICATE_P12_BASE64`, `IOS_CERTIFICATE_PASSWORD`, and `IOS_PROFILE_BASE64` from GitHub Actions Secrets. Keep the repository private because the gameplay source and assets are project IP. The IPA job is manual and requires an Apple Developer signing profile appropriate to the selected distribution type. GitHub-hosted macOS runners can build and sign through Xcode, but private-repository macOS minutes may be billed under the account's plan.
