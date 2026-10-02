# IPA gameplay core provenance

`engine.js`, `catalog.js`, and `store.js` were copied from the running AWS game source at `D:\Bot_Danh_Gia_Uy_Tin_Telegram\tutien` on 2026-09-30. Antigravity reported that the updated engine/catalog snapshot was checked with 156 passing tests and matched the AWS source hashes before it was staged for this IPA build. These files stay isolated under `ipa_core/` so the Telegram Mini App and its current server remain on their existing code.

SHA-256 of the synchronized IPA snapshot:

- `engine.js`: `2774c09ad7a2d363e29be41d538da01a71fc61e20eda23cd8369a395e54e6747`
- `catalog.js`: `e9ae38e719c1e503800277151d080f9516ce3c0ca3c81e9651966d2c25b0acdb` (includes the IPA server realm-name export)
- `store.js`: `dd86ea12b00e26764b2c8c89737bc1d61e1c820b0ef3a444993f5c8d324d5e78`

The IPA server reads `CULTIVATION_REALM_NAMES` from `catalog.js` to expose realm names for email-owned accounts and its separate save store, so that export must remain present. Do not edit the live Telegram game files when adding IPA-specific behavior. Keep later source updates traceable to an AWS source snapshot and review game rules before importing them.

`mode_maps.js` is a new IPA-only battle-map catalog. It defines the Phàm Giới and Tiên Giới PvP/PvE map sets and a server-clocked random rotation across five Cổ Động maps per realm. Active map IDs are attached to live battle responses so a map stays selected for that encounter. It does not modify the AWS Telegram Mini App snapshot.

## 2026-10-01 content sync

`engine.js`, `catalog.js` and `store.js` were re-synchronised from `D:\Bot_Danh_Gia_Uy_Tin_Telegram\tutien` (source last modified 2026-10-01 07:20 UTC; engine SHA-256 `440e7966…`, catalog `36d46b96…`). The IPA-only changes were re-applied on top:

- character creation appearance and three creation talents (`CREATION_TALENT_BONUSES`, `CREATION_APPEARANCES`, talent stat bonuses, appearance fields in `view`);
- province exploration position (`moveWorldPosition`, `worldPosition` in `view`);
- `CULTIVATION_REALM_NAMES` exported from `catalog.js` for the IPA realm store.

The source engine suite (144 tests: engine, time phase, sense/repair, Tiên Giới balance, boss/party balance) passes against this snapshot. New content in this sync includes the 19 equipment trial dungeons (`EQUIP_DUNGEONS`) and the latest equipment, town and dungeon balance.

Resulting SHA-256: `engine.js` `f2d65ac0…`, `catalog.js` `fc668e1b…`, `store.js` `dd86ea12…`.
