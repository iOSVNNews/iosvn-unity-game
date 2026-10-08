# Map and character scale review — 2026-10-08

Reference installation: `D:\QCBH ver 1.2.113`.

## Reference evidence and limits

Opened the installed game and attempted its existing save. The game first warned
about missing mods, then reported `This save is corrupted`. The save was copied
before loading to `build/qcbh-reference-save-backup-20261008`. No original saves
were deleted or replaced. Live world/minimap/battle screenshots could therefore
not be measured. The adjustments below are an adaptation, not a claim of an
exact camera or character height match to the reference game.

Read the reference's mod documentation/data under
`Mod\modFQA\配置修改教程\配置（只读）Json格式`:

- `DungeonSceneBase.json`: room themes separate background, floor choices,
  decoration and edge decoration. Bamboo scene 13 uses floors 101/102/103;
  scene 14 uses a different set and the duel sound `qiecuo`.
- `DungeonRoomBase.json`: room sizes vary (examples 120×60, 48×29, 40×20).
- `DungeonSceneObject.json`: decorations have spacing, grouping and barrier
  definitions; scene terrain is more than a stretched world-map thumbnail.
- `MapPosition.json`: world locations are assigned to areas and building types.
- The actual example project has separate world decoration `Map/Unit/206` and
  battle floor/barrier prefabs under `4.BattleScenesUnit（战斗场景部件示例）`.
  The empty catalogue prefabs under `Resources（资源对照）` are placeholders.

## Adaptation in this project

- Explore at zoom 0.52; range 0.28–0.85. Close/wide previews now exercise distinct
  limits. Zooming while following keeps focus on the player, including near the
  map boundary after zooming out.
- Minimap shows the current province and switches on a province crossing or
  teleport. Towns, gates and the player share province coordinates. A transparent
  outline shows the current camera footprint; tapping still opens the full atlas.
- World, minimap and atlas share the same aspect-preserving painting crop. The
  province crop is composed with that UV rectangle rather than taking the full
  source image and shifting all the markers.
- PvE and PvP use the same human size (132 canvas units). The previous PvP human
  was 319 units. World exploration retains its map-specific sprite size.
- PvE battlefield dimensions follow the ground texture's aspect ratio, cover the
  viewport and allow camera travel. Extent factor reduced from 2.25 to 1.35 so
  authored scenery is less magnified. PvP crops scenery to the viewport aspect.
- Existing original project battle paintings remain assigned to forest,
  wasteland, icefield, cave, world boss, sect arena and duel modes. No QCBH art
  was imported into the game as part of this change.

## Verification

Run Unity batch method `IOSVN.TuTien.Editor.QcbhPreview.ReviewMaps`.
It renders existing screens and verifies different zoom footprints, player
visibility after zoom, province switching, marker coordinates, painting aspect
on 1280×590 and 1024×768, and equal PvE/PvP human heights. Results are written to
`build/qcbh-captures/map-review.txt`; screenshots go to the same directory.
This does not replace an iPhone runtime or an exact live QCBH comparison.
