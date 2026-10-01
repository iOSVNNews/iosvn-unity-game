# Account server and provider linking

The IPA API shares the existing bot's HTTPS host at `https://tutien.iosvn.com.vn/ipa/api`. Caddy strips `/ipa` and forwards to the dedicated `iosvn-ipa` service on private port 8788. The bot keeps its existing upstream on port 8787. IPA account and character files live under `/var/lib/iosvn-ipa`.

Username/password registration and login work without an email provider. The same login input accepts a verified email linked to that username, preserving the account ID and character. Passwords use salted scrypt hashes; bearer sessions are hashed on disk. Registration requires 3–24 ASCII username characters and a 10–128 character password. Email registration retains the six-digit verification flow. Replaying a used email verification code never creates another session.

The Unity account panel reads provider availability from the server. Unconfigured linking services show **Chưa mở**. They are enabled after the corresponding server configuration is available. Google/Facebook linking opens the system browser and refreshes the current profile when the player returns to the game. Linking requires an active game session and an expiring, one-time OAuth state. Google identity tokens are verified with the official Google auth library. Provider credentials are never shipped in the IPA.

## Configure providers

On the game host, add protected environment values to `/etc/iosvn-ipa.env`, preserving the existing `IPA_HOST`, `IPA_PORT`, `IPA_DATA_DIR` and `IPA_PUBLIC_API_URL` entries. Set permissions to root-only `600`, then restart **iosvn-ipa**. Do not restart the Telegram bot service for account configuration.

| Feature | Server variables | Provider callback |
| --- | --- | --- |
| Email registration and linking | `GMAIL_SMTP_USER`, `GMAIL_SMTP_APP_PASSWORD`, optional `GMAIL_FROM_NAME` | Six-digit code sent by the server |
| Google/Gmail linking | `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET` | `https://tutien.iosvn.com.vn/ipa/api/auth/link/google/callback` |
| Facebook linking | `FACEBOOK_APP_ID`, `FACEBOOK_APP_SECRET`, `FACEBOOK_GRAPH_VERSION` | `https://tutien.iosvn.com.vn/ipa/api/auth/link/facebook/callback` |

Create a **Web application** OAuth client for the server callback in Google's console and register the exact authorized redirect URI. Enable Facebook Login for the Facebook application and register its exact callback, choosing a supported Graph API version for that application. Complete provider publishing/review requirements before opening these services to all players. Use the system browser rather than an embedded web view for Google linking.

Provider configuration must be supplied by the owner of the Google/Facebook applications and sender mailbox. Source code alone cannot create those credentials. `ipa_server.env.example` lists the variables without secrets.

References: [Google server OAuth](https://developers.google.com/identity/protocols/oauth2/web-server), [Google OpenID Connect](https://developers.google.com/identity/openid-connect/openid-connect), [Facebook login setup](https://firebase.google.com/docs/auth/web/facebook-login).

## Deployment and checks

`npm test` exercises registration, password failure, persistence, email verification replay prevention, email identity linking, provider ownership conflicts, session revocation, OAuth state and proxy address handling. Mail/OAuth provider responses in these tests are controlled fixtures; production provider login is validated only after real credentials are configured.

`npm run deploy:ipa -- --deploy` deploys an isolated release with a systemd service and verifies the public API and bot website. `IPA_SSH_KEY` may select the existing Lightsail key. No credentials or bot data are packaged. Firewall access to port 8788 is limited to the existing proxy's private IP. The installer preserves `/etc/iosvn-ipa.env` and account data. Caddy changes are validated before reload and restored if reload fails.

Unity batch rendering: `IOSVN.TuTien.Editor.LoginScreenValidation.RenderAuthScreens` renders the production login/register code at 1280×590 into `build/login-layout-captures`. It is a layout check, not an on-device tap test. The background prompt and generation mode are recorded in `login-background-prompt.txt`.
