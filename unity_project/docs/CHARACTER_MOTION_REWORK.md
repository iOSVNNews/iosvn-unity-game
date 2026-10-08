# Character motion rework — 2026-10-08

The previous battle actor used a cropped torso, then cutout limbs with mismatched joint pivots. The visible result had gaps at the waist and inconsistent arm and leg poses.

## Reference inspected

`D:\QCBH ver 1.2.113\Mod\modFQA\MOD模板例子\制作“静态和动态”立绘例子\ModProject\ModRes\ResBuildABProject\Assets\立绘资源`

- `立绘预览节点/战斗小人节点.prefab`: distinct battle actor, robe, head, hair, arms and leg nodes.
- `Human（战斗小人）/Man/yifu/101/yifu.png.meta`: authored sprite mesh, bone hierarchy, vertices, triangle indices and per-vertex bone weights.
- `PortraitDynamic（动态立绘）`: separate portrait animation resources.

The new client uses the same principle of keeping continuous clothing on a weighted mesh. It does not copy QCBH's game models, textures, or proprietary animation data into the client. Local reference PNG copies in `build/qcbh-reference-study` are for inspection only.

## Implementation

`QcbhSkinnedActor2D.cs` draws an original full-body sprite on a continuous UI mesh, with hierarchical skin matrices for the torso, head, shoulders, elbows, hips and knees. Blended normalized weights preserve a continuous waist and robe. The weapon follows a point deformed by the same mesh. Whole-actor mirroring preserves facing direction, and each pose uses the clock supplied by the battle presentation.

The new actor is connected to PvE and PvP battle screens. Character creation and character sheet screens retain their existing portrait layers, as QCBH also distinguishes portraits from battle actors. PvP now also updates the walking flag when joystick input moves the character. Existing character state and server contracts are preserved.

The two new battle base illustrations are male and female. Detailed facial and hairstyle choices retain their portrait rendering; the battle actor currently uses the base illustration for each gender, plus the selected weapon and clothing tint.

## Verification

- `npm test`: 17 tests passed.
- Unity menu `iOSVN/Preview/Validate character motion`: male/female idle, walk, attack, cast, hurt and down; finite deformed points, walk phase advance, facing and sprite presence.
- Unity menu `iOSVN/Preview/Render skinned walk cycle`: 24 rendered frames, exported to `build/qcbh-captures/character-walk.gif`.
- `QcbhPreview.ReviewSkinnedCharacters`: motion checks plus creator, character sheet, map, bag, PvE, PvP and tablet screenshots.
- These are Editor checks and visual reviews. No new IPA has been installed or tested on a physical iPhone in this session.

## Generated art

Built-in `image_gen` tool, transparent-background generation. Project asset: `Assets/Resources/Characters/FullBodyActorsV1.png`.

Final generation prompt:

> Create a production 2D xianxia RPG character sprite atlas, a single transparent PNG asset containing exactly two full-body characters in two equal side-by-side cells. Left cell: handsome young Chinese male cultivator in simple ivory inner robe and dark teal outer robe, black long hair with modest topknot. Right cell: young adult Chinese female cultivator in ivory and muted lavender long robe, black long hair with modest hairpin. Painterly ink illustration, readable silhouette for a top-down 2D action RPG, restrained detail rather than ornamental armor. Both characters are standing in a neutral relaxed animation bind pose, three-quarter view facing LEFT, arms relaxed slightly away from torso with hands visible at hip level, sleeves moderate width, feet slightly separated, both boots fully visible below the robe. IMPORTANT entire figure head to soles, connected natural anatomy, continuous unbroken robe across chest waist and skirt; two adult characters same height and proportions. Neither weapons nor aura. Each stands centered within its equal half-cell; crown at 8% image height, soles at 94%; generous transparent space around silhouettes, no overlap across cell boundary. True transparent alpha background, no colored rectangular backdrop, no shadow, no text, no panel borders. This asset will be deformed with a weighted mesh skeleton, so do not create cutout body parts or separated limbs.
