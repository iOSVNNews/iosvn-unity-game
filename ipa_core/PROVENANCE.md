# IPA gameplay core provenance

`engine.js`, `catalog.js`, and `store.js` are copied from the running AWS game source at `D:\Bot_Danh_Gia_Uy_Tin_Telegram\tutien` on 2026-09-30. They are isolated under `ipa_core/` so the Telegram Mini App and its current server stay on their existing code.

SHA-256 at import:

- `engine.js`: `73f6d1af1219798424751057fb953458c1743397a98e9db72c50b1cc208629d3`
- `catalog.js`: `625ce9d00b3fb4debae86dd5c1ecd95acad309a1a4ebf47b01fbc1bddf39c0a5`
- `store.js`: `dd86ea12b00e26764b2c8c89737bc1d61e1c820b0ef3a444993f5c8d324d5e78`

The only local edit to the imported gameplay core is exporting `CULTIVATION_REALM_NAMES` from `catalog.js`; the IPA server uses it to adapt the cultivation system to email-owned accounts and a separate save store. Do not edit the live Telegram game files when adding IPA-specific behavior. Keep later source updates traceable to an AWS source snapshot and review the game rules before importing them.
