# IPA gameplay core provenance

`engine.js`, `catalog.js`, and `store.js` were copied from the running AWS game source at `D:\Bot_Danh_Gia_Uy_Tin_Telegram\tutien` on 2026-09-30. Antigravity reported that the updated engine/catalog snapshot was checked with 156 passing tests and matched the AWS source hashes before it was staged for this IPA build. These files stay isolated under `ipa_core/` so the Telegram Mini App and its current server remain on their existing code.

SHA-256 of the synchronized IPA snapshot:

- `engine.js`: `2774c09ad7a2d363e29be41d538da01a71fc61e20eda23cd8369a395e54e6747`
- `catalog.js`: `e9ae38e719c1e503800277151d080f9516ce3c0ca3c81e9651966d2c25b0acdb` (includes the IPA server realm-name export)
- `store.js`: `dd86ea12b00e26764b2c8c89737bc1d61e1c820b0ef3a444993f5c8d324d5e78`

The IPA server reads `CULTIVATION_REALM_NAMES` from `catalog.js` to expose realm names for email-owned accounts and its separate save store, so that export must remain present. Do not edit the live Telegram game files when adding IPA-specific behavior. Keep later source updates traceable to an AWS source snapshot and review game rules before importing them.

`mode_maps.js` is a new IPA-only battle-map catalog. It defines the Phàm Giới and Tiên Giới PvP/PvE map sets and a server-clocked random rotation across five Cổ Động maps per realm. Active map IDs are attached to live battle responses so a map stays selected for that encounter. It does not modify the AWS Telegram Mini App snapshot.
