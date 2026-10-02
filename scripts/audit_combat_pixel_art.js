#!/usr/bin/env node
'use strict';

const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const C = require('../ipa_core/catalog');
const root = path.join(__dirname, '..', 'unity_project', 'Assets', 'Resources');
const combat = path.join(root, 'CombatPixel');
const weaponIds = C.EQUIPMENT.filter(item => item.slot === 'weapon' || item.slot === 'phi_kiem' || item.wtype).map(item => item.id);
const expected = {
  Monsters: C.MONSTERS.map(item => item.id),
  Weapons: weaponIds,
  Skills: [...C.SKILLS.map(item => item.id), ...C.MONSTERS.map(item => 'quai_' + item.id)],
  Items: fs.readdirSync(path.join(root, 'PixelArt', 'Items')).filter(name => name.endsWith('.png'))
    .map(name => name.slice(0, -4)).filter(id => !weaponIds.includes(id) && !C.SKILLS.some(skill => skill.id === id)),
};

let failed = false;
const allHashes = new Set();
for (const [group, records] of Object.entries(expected)) {
  const ids = [...new Set(records)];
  const directory = path.join(combat, group);
  const actual = fs.readdirSync(directory).filter(name => name.endsWith('.png'));
  const missing = ids.filter(id => !fs.existsSync(path.join(directory, id + '.png')));
  const extra = actual.filter(name => !ids.includes(name.slice(0, -4)));
  const hashes = actual.map(name => crypto.createHash('sha256').update(fs.readFileSync(path.join(directory, name))).digest('hex'));
  const duplicates = hashes.length - new Set(hashes).size;
  for (const hash of hashes) allHashes.add(hash);
  if (missing.length || extra.length || duplicates) failed = true;
  console.log(`${group}: catalog=${records.length} uniqueIDs=${ids.length} png=${actual.length} missing=${missing.length} extra=${extra.length} duplicateImages=${duplicates}`);
  if (missing.length) console.log('  Missing:', missing.slice(0, 10).join(', '));
  if (extra.length) console.log('  Extra:', extra.slice(0, 10).join(', '));
}
const imageCount = Object.keys(expected).reduce((sum, group) => sum + fs.readdirSync(path.join(combat, group)).filter(name => name.endsWith('.png')).length, 0);
if (imageCount !== allHashes.size) { console.error('Cross-group images are duplicated:', imageCount - allHashes.size); failed = true; }
process.exitCode = failed ? 1 : 0;
