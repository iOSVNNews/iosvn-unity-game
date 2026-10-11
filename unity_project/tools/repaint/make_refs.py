# Builds reference grids (old pixel art) + manifest for the ChatGPT repaint of skills, weapons and monsters.
import json, os, subprocess, sys
from pathlib import Path
from PIL import Image, ImageDraw
ROOT = str(Path(__file__).resolve().parents[3])
RES = ROOT + '/unity_project/Assets/Resources'
OUT = ROOT + '/unity_project/ArtSource/RepaintRefs'
os.makedirs(OUT, exist_ok=True)
cat = json.loads(subprocess.check_output(['node', '-e', '''
const C=require("./ipa_core/catalog.js");
console.log(JSON.stringify({skills:C.SKILLS.map(s=>({id:s.id,name:s.name,mon:s.mon,kind:s.kind,el:s.element||"",desc:(s.desc||"").slice(0,90),rarity:s.rarity})),
 equip:C.EQUIPMENT.map(e=>({id:e.id,name:e.name,slot:e.slot,wtype:e.wtype||"",tier:e.tier})),
 monsters:C.MONSTERS.map(m=>({id:m.id,name:m.name,el:m.element||"",boss:!!(m.isBoss||m.worldBoss||!m.small),small:!!m.small,realm:m.realm||0}))}));
'''], cwd=ROOT))
manifest = []
def grid(kind, items, load, cols=4, rows=4, cell=256):
    n = cols * rows
    for s in range(0, len(items), n):
        chunk = items[s:s + n]
        name = f'{kind}_{s // n:02d}'
        im = Image.new('RGB', (cols * cell, rows * cell), (128, 128, 128))
        d = ImageDraw.Draw(im)
        for i, it in enumerate(chunk):
            x, y = (i % cols) * cell, (i // cols) * cell
            d.rectangle([x, y, x + cell - 1, y + cell - 1], outline=(100, 100, 100))
            src = load(it)
            if src is None: continue
            src = src.convert('RGBA')
            k = min((cell * .86) / src.width, (cell * .86) / src.height)
            src = src.resize((max(1, int(src.width * k)), max(1, int(src.height * k))), Image.NEAREST)
            im.paste(src, (x + (cell - src.width) // 2, y + (cell - src.height) // 2), src)
        im.save(f'{OUT}/{name}.png')
        manifest.append({'sheet': name, 'kind': kind, 'cols': cols, 'rows': rows, 'items': chunk})
def first_frame(path):
    im = Image.open(path)
    return im
skills = cat['skills']
grid('skill', skills, lambda it: first_frame(f"{RES}/CombatPixel/Skills/{it['id']}.png"))
weapon_ids = sorted(f[:-4] for f in os.listdir(RES + '/CombatPixel/Weapons') if f.endswith('.png'))
eq = {e['id']: e for e in cat['equip']}
weapons = [eq[i] for i in weapon_ids if i in eq]
grid('weapon', weapons, lambda it: first_frame(f"{RES}/CombatPixel/Weapons/{it['id']}.png"))
import io
def monster_ref(it):
    p = f"{RES}/Art/Monsters/{it['id']}.bytes"
    if not os.path.exists(p): return None
    im = Image.open(io.BytesIO(open(p, 'rb').read())).convert('RGBA')
    return im.crop((0, 0, 256, 256))
mons = cat['monsters']
grid('monster', mons, monster_ref, cols=1, rows=4)
json.dump(manifest, open(OUT + '/manifest.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
print(len(manifest), sum(1 for m in manifest if m['kind']=='skill'), sum(1 for m in manifest if m['kind']=='weapon'), sum(1 for m in manifest if m['kind']=='monster'))
