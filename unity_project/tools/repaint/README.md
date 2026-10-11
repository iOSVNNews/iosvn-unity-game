# Painted game art

The catalog manifest identifies 245 skill icons, 547 equipment / flying sword / mount icons,
and 279 monster species. Each monster has four distinct action poses: idle, run, attack,
and pounce. Existing breathing bodies and runtime action hooks are preserved.

Raw sheets live locally in `ArtSource/ChatGPTRepaint`. The import can resume Claude's
downloads named `gem_cg4_<sheet>.png` from Downloads. Source images and pixel backups
stay in ArtSource; the finished PNG payloads in `Assets/Resources/Art` are versioned.

```powershell
python tools/repaint/resume_repaint.py --audit
python tools/repaint/resume_repaint.py
python tools/repaint/validate_repaint.py
```

Use `--sheet skill_12` (repeat the flag for additional sheets) to reimport a selected sheet.
The splitter requires every monster pose and isolates neighbouring cells when effects
touch. It never substitutes an idle picture for a missing action pose.
Use `--kind monster --force` to rebuild monster sheets. Species listed in `fixes.json`
are imported from `ArtSource/ChatGPTRepaint/Fixes` last, so a later rebuild preserves
the corrected drawings. All poses use one shared pixel scale and retain a top margin.

Unity validation: execute `IOSVN.TuTien.Editor.PaintedArtAudit.Run` in batch mode. It
loads every catalog asset through the runtime APIs, checks action switching and consistent
sprite scale, then renders the current world, character and battle interfaces.
Reports and screenshots are written to the repository's `build` folder.

## Completion on 2026-10-11

Recovered and imported Claude's downloaded weapon sheets 01–17 and 19–34, and monster
sheets 41–69. Corrected extraction of skill sheet 12 and monster sheets 05 / 35. Existing
terrain paintings, completed art and code integrations remain in place.

Generated replacement sheets 04 (skills) and 18 (equipment) with Codex's built-in
imagegen tool because the original ChatGPT conversations were still stuck generating.
Their source images are saved in `ArtSource/ChatGPTRepaint/skill_04.png` and
`ArtSource/ChatGPTRepaint/weapon_18.png`. See `completion-prompts.md` for the prompt set.
Also regenerated complete four-pose strips for Thiên Ma and Thái Ất Kim Long after
visual review found clipped anatomy in the original combined sheets. These corrected
strips retain their original IDs and are stored in the local `Fixes` source folder.

No IPA was built or packaged for this update.

Validation passed: 245/245 skills, 547/547 unique equipment IDs and 279/279 species;
all PNG payloads have alpha, correct dimensions and four distinct monster poses.
Unity 6000.6.3f1 loaded the full catalog, verified run / attack / pounce-to-bite transitions,
and rendered the world, boss preview, character and battle interfaces successfully.
The movement, collision, joystick touch and monster tap checks also passed. Verification
was performed in Unity on Windows; this update has not been tested on a physical iPhone.
