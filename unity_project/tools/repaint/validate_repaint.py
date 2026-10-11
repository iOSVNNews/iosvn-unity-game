"""Check catalog coverage, transparent PNG payloads and four unique monster poses."""
import hashlib
import json
from pathlib import Path
import numpy as np
from PIL import Image

project = Path(__file__).resolve().parents[2]
manifest = project / 'tools/repaint/manifest.json'
resources = project / 'Assets/Resources/Art'
totals = {'skill': 0, 'weapon': 0, 'monster': 0}
errors = []
for sheet in json.loads(manifest.read_text(encoding='utf-8')):
    kind = sheet['kind']
    sub = {'skill': 'Skills', 'weapon': 'Items', 'monster': 'MonsterPoses'}[kind]
    for item in sheet['items']:
        asset = resources / sub / (item['id'] + '.bytes')
        if not asset.exists():
            errors.append(f'{sub}/{item["id"]}: missing')
            continue
        im = Image.open(asset)
        expected = (1024, 256) if kind == 'monster' else (256, 256)
        if im.format != 'PNG' or im.size != expected or im.mode != 'RGBA':
            errors.append(f'{asset.name}: invalid image {im.format} {im.size} {im.mode}')
            continue
        frames = [im.crop((c * 256, 0, (c + 1) * 256, 256)) for c in range(4)] if kind == 'monster' else [im]
        for c, frame in enumerate(frames):
            a = np.asarray(frame)[..., 3]
            if (a > 128).sum() < 300 or (a == 0).sum() < 1000:
                errors.append(f'{asset.name} frame {c}: empty or opaque background')
            if kind == 'monster' and (a[0] > 128).any():
                errors.append(f'{asset.name} frame {c}: pose clipped at the top')
        if kind == 'monster':
            if len({hashlib.sha256(f.tobytes()).hexdigest() for f in frames}) != 4:
                errors.append(f'{asset.name}: repeated pose')
            body = resources / 'Monsters' / asset.name
            if not body.exists() or Image.open(body).size != (1024, 256):
                errors.append(f'{asset.name}: missing breathing body')
        totals[kind] += 1
report = {'totals': totals, 'errors': errors}
out = project / '../build/repaint-validation.json'
out.write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
raise SystemExit(1 if errors else 0)
