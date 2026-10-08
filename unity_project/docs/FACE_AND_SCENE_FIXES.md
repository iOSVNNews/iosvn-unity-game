# Face and scene fixes — 2026-10-08

The reported character sheet showed city scenery behind low-contrast statistics and a face assembled from unrelated eye/nose/mouth patches. Town entry reported a NullReferenceException inside ProvinceWorld.StepActor.

Changes:
- New original PuppetMaleV3 / PuppetFemaleV3 atlases paint complete faces into the body texture. Creator and profile disable the six old face patch images. Small eye/brow/nose/mouth proportion changes deform the continuous mesh together.
- Suspend the city panorama for service screens and the creator. Character statistics have an opaque dark panel.
- Deactivate retired world/city objects before Unity's deferred Destroy. StepActor stops after callbacks that cancel/replace the path or retire its scene; manual movement and chase callbacks likewise stop before touching retired objects.

Verification: ReviewReportedErrors renders all existing screens then tests cancelling a path, opening town from a step callback, opening profile from town, and integrated face assets for both genders at 1280×590 and 1024×768. Results: build/qcbh-captures/reported-errors-review.txt. ValidateReportedErrors runs only those regression cases. This is Editor validation; physical iPhone validation remains separate.

The world painting is a finite-resolution bitmap and is still visibly magnified at close zoom. These fixes do not claim to add missing terrain detail.

Assets were edited with the built-in imagegen tool and saved in Assets/Resources/Characters/PuppetMaleV3.png and PuppetFemaleV3.png. V2 sources were preserved.

Male prompt:
Use case: precise-object-edit. Edit target: attached game character atlas PuppetMaleV2.png. Repair ONLY the blank face on the male figure in the LEFT half. Paint a coherent handsome adult Vietnamese/East Asian xianxia cultivator face: natural symmetric eyes aligned to the existing slightly downward three-quarter head, eyebrows, nose, lips, subtle expression, same painterly realism, lighting, skin tone, integrated seamlessly into the existing jaw and forehead. No pasted photoreal facial patches, no makeup masks, no extra face. Keep exact canvas aspect 1536x1024 and exact layout: male figure in left half, separate headless clothing in right half. Preserve original pose, hair, silhouette, garments and alignment of the two halves; do not move or resize either figure. Transparent alpha background; remove hazy background glow, preserve fine hair and fabric edges. No labels or text. Output a production atlas.

Female prompt:
Use case: precise-object-edit. Edit target: attached PuppetFemaleV2 game atlas. Repair ONLY the blank face of the adult female xianxia cultivator in the LEFT half. Paint natural coherent East Asian/Vietnamese facial anatomy, gentle calm expression, dark detailed eyes, proportionate eyebrows, nose and lips in the SAME elegant painterly fantasy style as the body and existing lighting. Seamlessly integrate face into the forehead, hairline and jaw; no pasted facial patches. Preserve exact head position and body pose, silver hair, costume and two-half atlas layout, with separate headless clothing in the right half. Preserve canvas aspect ratio and do not enlarge head or move either figure. Genuine transparent background and clean alpha edges for hair and flowing fabric. No captions, text or watermark. Output a production atlas.

