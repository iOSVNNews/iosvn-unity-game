# Unity online vertical slice

## Current direction

- The iOS game is a Unity app with its **own dedicated game server**. The AWS game is a source of gameplay rules and a foundation to develop from; Unity will not connect to the AWS game service as its production server.
- The Unity app uses a standalone email account, not Telegram login.
- `D:\Telegram\Payload\App.app` is a UX reference for compact 2D mobile navigation, account flow, cloud save and live updates. This project does not copy its code or assets.
- Initial vertical slice: email account, character creation, profile, world target list, and a server-authoritative hunt.

## Current project state

The Unity folder includes Unity 6.3 LTS project metadata, an editor setup script that generates a starter scene, and a runtime-built mobile UI for email login, character creation, profile, target list, and basic server-authoritative combat. The API base URL is a project setting for the separate IPA server.

The first local implementation is in `ipa_server.js` and `email_auth_store.js`. Its gameplay files in `ipa_core/` were copied from the running AWS game source on 2026-09-30; `ipa_core/PROVENANCE.md` records the source hashes. The current root `server.js` and root game files remain the Telegram Mini App branch. The IPA server uses server-issued email identities and an independent save file. This draft has not been deployed or connected to a Unity build.

## Dedicated server contract

`GameServerConfig.apiBaseUrl` is the HTTPS root of the separate game server, ending in `/api`. The client expects:

- `POST /auth/email/register` with `{ "email", "password" }`.
- `POST /auth/email/login` with `{ "email", "password" }`, returning `{ "ok": true, "accessToken", "expiresAt" }`.
- Authenticated `GET /state` returns game state.
- Authenticated `POST /register` creates a character with `{ "name", "gender", "mon", "he" }`.
- Authenticated `POST /breakthrough` asks the game server to validate and resolve a cultivation breakthrough.
- Authenticated `GET /world/monsters` lists current targets.
- Authenticated `POST /world/hunt` accepts `{ "monsterUid" }`; combat and rewards are computed by the server.
- Authenticated `GET /battle/current` reads the active server battle.
- Authenticated `POST /battle/act` accepts `{ "a": "attack|dodge|flee" }` and returns the authoritative updated battle.
- Authenticated `POST /auth/logout` revokes the bearer session.

The first server draft stores password hashes with scrypt and stores random bearer tokens by hash with expiry/revocation. Login/register requests are rate-limited. It does not verify mailbox ownership or provide password recovery yet, so use it only for closed development until an email delivery/recovery service is configured. Do not store passwords or long-lived bearer tokens in the Unity client. The current starter keeps the access token in memory only.

## Opening the Unity project

Open `D:\game_iosvn\unity_project` with Unity **6000.3.13f1 (Unity 6.3 LTS)**. On first editor load, a setup script creates `Assets/Scenes/OnlinePrototype.unity`, registers it as the first build scene and creates `Assets/Resources/GameServerConfig.asset`. Set `apiBaseUrl` to the separate server's HTTPS address before using online features.

The machine has Unity Hub 3.22.0, but the Unity Editor executable is not installed in the usual Hub or Program Files locations. The project has not been compiled in Unity. `.github/workflows/build-ios.yml` uses GameCI Unity Builder v5.0.0 to export an Xcode project and can optionally sign/export an IPA on a macOS runner. A signed IPA needs GitHub Actions secrets for the Unity license and Apple signing certificate/provisioning profile, plus the bundle ID and Apple team ID.

The repository workflow reads `IOS_BUNDLE_ID` and `IPA_SERVER_URL` from GitHub Actions Variables. It reads `UNITY_LICENSE`, `IOS_TEAM_ID`, `IOS_CERTIFICATE_P12_BASE64`, `IOS_CERTIFICATE_PASSWORD`, and `IOS_PROFILE_BASE64` from GitHub Actions Secrets. Keep the repository private because the gameplay source and assets are project IP. The IPA job is manual and requires an Apple Developer signing profile appropriate to the selected distribution type. GitHub-hosted macOS runners can build and sign through Xcode, but private-repository macOS minutes may be billed under the account's plan.
