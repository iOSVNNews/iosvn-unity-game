# Character creation and Open Sans

The creator has a detailed V3 portrait and a separate FullBodyActorsV2 preview matching the game actor. Both preserve the original painted proportions. Stretching the painted face/body to simulate facial and body options produced distorted characters; that deformation has been removed. Hair styles, beards, separate garment cuts, and geometric facial controls without suitable art are not offered. Old saved fields remain compatible but no longer stretch the V3 portrait or full-body appearance.

Available appearance controls are gender at initial creation, ten color/accessory presets, hair/skin/robe colors, headwear, weapons and aura. Coloring preserves painted shading and most metal details using a UI shader. It uses soft spatial masks, so this is not a layered wardrobe system with independently drawn hairstyles and garments. The character retains its drawn face and silhouette.

Changing gender does not discard the typed name. Repeated option clicks disable old buttons immediately. An existing character edits its saved base look rather than baking currently equipped armor into that look. After an online save, the UI uses the server's canonical state, including the equipped appearance. Offline appearance changes update the saved character and survive reopening the game. Gender changes for an existing online character remain part of the game's existing gender-change service; the appearance editor does not bypass it.

The creator uses a dark teal frame, portrait/full-body preview tabs, two-column appearance controls, two-row color swatches, and a separate destiny card. The name field uses relative widths and the preview stays centered on both phone and tablet layouts. Appearance editing shows the existing destiny as information instead of offering unsaved sect/talent changes.

All runtime text factories use bundled Open Sans Regular, SemiBold or Bold; display headings also use Open Sans Bold. Medium uses Regular. The former Be Vietnam Pro and Playfair font binaries are removed. Open Sans is sourced from the upstream Google Fonts project: https://github.com/googlefonts/opensans/tree/main/fonts/ttf . Its SIL Open Font License is bundled at `Assets/Fonts/OFL-OpenSans.txt`.

Run `IOSVN.TuTien.Editor.QcbhPreview.ReviewCreatorAndFonts` in Unity batch mode to check actual next-button effects, typed-name retention, player look propagation into world/combat, offline appearance persistence, reopening the appearance editor, Vietnamese glyphs, and the font family on every rendered screen. It also runs the existing character-motion, minimap, map-scale and reported-error checks. Captures are written to `build/qcbh-captures` at 1280×590 and 1024×768. These are editor checks, not physical-device performance measurements.

## Fresh IPA export

Push builds previously downloaded the bootstrap Xcode archive even when the Unity source changed. The workflow now rejects an archive whose `iosvn-source-revision.txt` differs from the checked-out Git commit. `IOSBuild.Build` writes this receipt after a successful export when passed `-iosSourceRevision <commit>` (or `GITHUB_SHA` in CI).

For local Windows exports, export the committed source with Unity's iOS Build Support, zip the contents of the export directory including the receipt, upload `ios-xcode-export.zip` to the selected bootstrap release, then push or dispatch the matching source revision. The macOS workflow compiles that project and packages `TuTienGioi-unsigned.ipa`; it requires the user's usual signing tool for installation. The existing LCSign preparation workflow can add replaceable ad-hoc signatures if requested.
