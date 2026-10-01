#!/usr/bin/env node
'use strict';

// Imports the pixel monster/item art and gameplay catalog from the Telegram game
// into Unity Resources so the IPA can run its exploration loop without a CDN.
// Usage: node scripts/import_telegram_world_assets.js "D:\\Bot_Danh_Gia_Uy_Tin_Telegram\\tutien"

const fs = require('node:fs');
const path = require('node:path');
const zlib = require('node:zlib');

const repoRoot = path.resolve(__dirname, '..');
const telegramRoot = path.resolve(process.argv[2] || 'D:/Bot_Danh_Gia_Uy_Tin_Telegram/tutien');
const sourceIcons = path.join(telegramRoot, 'public', 'icons');
const unityAssets = path.join(repoRoot, 'unity_project', 'Assets');
const outputMonsterIcons = path.join(unityAssets, 'Resources', 'PixelArt', 'Monsters');
const outputItemIcons = path.join(unityAssets, 'Resources', 'PixelArt', 'Items');
const outputCatalog = path.join(unityAssets, 'Resources', 'OfflineHuntCatalog.json');
const mapCatalogPath = path.join(unityAssets, 'Resources', 'MapCatalog.json');
const botCatalog = require(path.join(telegramRoot, 'catalog.js'));
const ipaCatalog = require(path.join(repoRoot, 'ipa_core', 'catalog.js'));
const OUTPUT_SIZE = 256;
const SVG_VIEW_SIZE = 64;

function crc32(buffer) {
  let crc = 0xffffffff;
  for (const byte of buffer) {
    crc ^= byte;
    for (let bit = 0; bit < 8; bit++) crc = (crc >>> 1) ^ (0xedb88320 & -(crc & 1));
  }
  return (crc ^ 0xffffffff) >>> 0;
}

function pngChunk(type, data) {
  const name = Buffer.from(type, 'ascii');
  const length = Buffer.alloc(4); length.writeUInt32BE(data.length);
  const checksum = Buffer.alloc(4); checksum.writeUInt32BE(crc32(Buffer.concat([name, data])));
  return Buffer.concat([length, name, data, checksum]);
}

function parseColor(value) {
  const match = /^#([0-9a-f]{6})([0-9a-f]{2})?$/i.exec(value || '');
  if (!match) throw new Error(`Unsupported pixel SVG color: ${value}`);
  return [
    parseInt(match[1].slice(0, 2), 16),
    parseInt(match[1].slice(2, 4), 16),
    parseInt(match[1].slice(4, 6), 16),
    match[2] ? parseInt(match[2], 16) : 255,
  ];
}

function renderPixelSvg(svg, sourceName) {
  if (!/viewBox=["']0 0 64 64["']/.test(svg)) throw new Error(`${sourceName} must use a 64x64 viewBox`);
  const pixels = Buffer.alloc(OUTPUT_SIZE * OUTPUT_SIZE * 4);
  const scale = OUTPUT_SIZE / SVG_VIEW_SIZE;
  const rectPattern = /<rect\b([^>]*)\/?\s*>/g;
  let rectCount = 0;
  for (const match of svg.matchAll(rectPattern)) {
    const attributes = Object.fromEntries([...match[1].matchAll(/([\w-]+)=["']([^"']*)["']/g)].map(item => [item[1], item[2]]));
    const x = Number(attributes.x || 0), y = Number(attributes.y || 0);
    const width = Number(attributes.width), height = Number(attributes.height);
    if (![x, y, width, height].every(Number.isFinite) || width <= 0 || height <= 0) throw new Error(`Bad rect in ${sourceName}`);
    const color = parseColor(attributes.fill);
    const x0 = Math.max(0, Math.floor(x * scale)), y0 = Math.max(0, Math.floor(y * scale));
    const x1 = Math.min(OUTPUT_SIZE, Math.ceil((x + width) * scale));
    const y1 = Math.min(OUTPUT_SIZE, Math.ceil((y + height) * scale));
    for (let py = y0; py < y1; py++) {
      for (let px = x0; px < x1; px++) {
        const index = (py * OUTPUT_SIZE + px) * 4;
        pixels[index] = color[0]; pixels[index + 1] = color[1]; pixels[index + 2] = color[2]; pixels[index + 3] = color[3];
      }
    }
    rectCount++;
  }
  if (!rectCount) throw new Error(`${sourceName} contains no pixel rectangles`);

  const scanlines = Buffer.alloc((OUTPUT_SIZE * 4 + 1) * OUTPUT_SIZE);
  for (let y = 0; y < OUTPUT_SIZE; y++) pixels.copy(scanlines, y * (OUTPUT_SIZE * 4 + 1) + 1, y * OUTPUT_SIZE * 4, (y + 1) * OUTPUT_SIZE * 4);
  const header = Buffer.alloc(13);
  header.writeUInt32BE(OUTPUT_SIZE, 0); header.writeUInt32BE(OUTPUT_SIZE, 4);
  header[8] = 8; header[9] = 6; header[10] = 0; header[11] = 0; header[12] = 0;
  return Buffer.concat([
    Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]),
    pngChunk('IHDR', header), pngChunk('IDAT', zlib.deflateSync(scanlines, { level: 9 })), pngChunk('IEND', Buffer.alloc(0)),
  ]);
}

function importFolder(name, outputFolder) {
  const inputFolder = path.join(sourceIcons, name);
  fs.mkdirSync(outputFolder, { recursive: true });
  let count = 0;
  for (const filename of fs.readdirSync(inputFolder).filter(file => file.endsWith('.svg')).sort()) {
    const svgPath = path.join(inputFolder, filename);
    const pngPath = path.join(outputFolder, `${path.basename(filename, '.svg')}.png`);
    const svg = fs.readFileSync(svgPath, 'utf8');
    fs.writeFileSync(pngPath, renderPixelSvg(svg, svgPath));
    count++;
  }
  return count;
}

function renderHandDrawnWorldBoss(id) {
  const rects = [];
  const add = (color, entries) => entries.forEach(([x, y, width, height]) => rects.push(`<rect x="${x}" y="${y}" width="${width}" height="${height}" fill="#${color}"/>`));
  if (id === 'tien_canh_12_huyen_hai_giao') {
    // Coiling sea dragon: swept horns, whiskers, fin crest, segmented body and wave pearls.
    add('123953', [[11,31,12,7],[18,25,13,8],[27,22,12,8],[35,18,10,8],[41,12,13,12],[44,7,3,7],[50,7,3,7],[48,23,4,9],[54,21,6,2],[54,25,7,2],[8,38,4,3],[11,44,3,5],[22,43,4,5],[32,37,4,6],[37,14,2,3]]);
    add('1a84a1', [[13,31,10,5],[20,26,11,6],[29,23,10,6],[37,19,9,6],[43,13,10,9],[45,8,2,5],[51,8,2,5],[49,24,2,6],[55,22,4,1],[55,26,5,1],[9,39,2,2],[12,45,1,3],[23,44,2,3],[33,38,2,4]]);
    add('55d3cf', [[15,32,8,2],[22,27,8,2],[31,24,7,2],[39,20,6,2],[45,14,7,2],[46,25,2,3],[13,46,2,1],[24,45,2,1],[34,39,2,1]]);
    add('f4d37d', [[47,17,2,2],[53,18,2,2],[42,10,2,3],[51,10,2,3],[5,35,4,2],[6,41,3,2],[17,22,4,2],[27,18,4,2],[35,15,3,2],[21,49,3,2],[31,44,3,2]]);
    add('f6fbeb', [[51,17,2,2],[55,24,2,2],[6,51,3,2],[57,39,2,2],[4,26,2,2]]);
    add('102d3d', [[49,16,2,2],[53,17,1,1]]);
  } else if (id === 'tien_canh_12_tinh_thu') {
    // Astral guardian beast: armored horned head, layered mane, plated paws and a star-ring halo.
    add('342a66', [[24,9,16,5],[20,14,24,7],[15,20,34,11],[12,30,40,10],[18,40,28,8],[20,47,7,8],[36,47,7,8],[23,6,4,8],[36,6,4,8],[8,16,4,3],[52,16,4,3],[5,28,5,3],[54,28,5,3],[7,40,4,3],[53,40,4,3]]);
    add('6853b7', [[26,11,12,3],[22,16,20,4],[17,22,30,7],[15,31,34,6],[20,41,24,5],[22,48,4,5],[38,48,4,5],[24,8,2,5],[37,8,2,5],[9,17,3,1],[53,17,3,1],[6,29,4,1],[54,29,4,1]]);
    add('aa8de8', [[28,13,8,2],[24,18,16,2],[20,24,24,2],[18,33,28,2],[23,42,18,2],[24,49,2,3],[40,49,2,3]]);
    add('ffd777', [[28,23,4,4],[38,23,4,4],[29,33,12,3],[30,36,10,2],[24,7,2,5],[37,7,2,5],[4,18,4,2],[55,18,4,2],[4,42,4,2],[56,42,4,2],[31,4,2,2],[15,10,2,2],[47,10,2,2],[12,49,2,2],[49,49,2,2]]);
    add('fff6d5', [[29,24,2,2],[39,24,2,2],[33,36,4,1],[31,5,2,1],[15,11,1,1],[48,11,1,1],[5,18,2,1],[56,18,2,1]]);
    add('171833', [[29,24,1,2],[39,24,1,2],[30,36,10,1]]);
  } else if (id === 'tien_canh_12_kiem_linh') {
    // Sword guardian spirit: crowned helm, bright blade, crossguard, robe and orbiting flying swords.
    add('34264d', [[29,8,7,40],[22,15,21,6],[18,21,29,7],[23,28,18,6],[17,34,31,4],[22,38,21,12],[25,49,5,6],[35,49,5,6],[9,14,3,16],[6,11,2,9],[48,14,3,16],[53,11,2,9],[18,17,3,4],[44,17,3,4]]);
    add('c28a39', [[31,10,3,34],[25,17,16,3],[21,23,24,3],[26,29,13,3],[19,35,27,2],[24,40,17,8],[27,50,3,4],[36,50,3,4],[10,15,1,13],[7,12,1,7],[49,15,1,13],[54,12,1,7]]);
    add('f1cf73', [[32,9,1,34],[27,18,12,1],[23,24,20,1],[28,30,9,1],[20,35,25,1],[25,41,14,5],[28,51,1,3],[37,51,1,3],[11,14,1,12],[8,11,1,6],[50,14,1,12],[55,11,1,6]]);
    add('e9f4f6', [[32,8,1,27],[32,37,1,7],[10,13,1,8],[49,13,1,8]]);
    add('7de2ed', [[26,23,4,2],[38,23,4,2],[30,33,5,1],[13,20,2,2],[46,20,2,2],[15,8,2,2],[47,8,2,2],[4,25,2,2],[57,25,2,2],[12,48,2,2],[51,48,2,2]]);
    add('17243a', [[28,23,2,2],[39,23,2,2],[31,33,4,1]]);
  }
  if (!rects.length) throw new Error(`No custom pixel art registered for ${id}`);
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64">${rects.join('')}</svg>`;
}

function fillMissingWorldBossArt(outputFolder) {
  let count = 0;
  for (const monster of botCatalog.MONSTERS || []) {
    const target = path.join(outputFolder, `${monster.id}.png`);
    if (fs.existsSync(target)) continue;
    fs.writeFileSync(target, renderPixelSvg(renderHandDrawnWorldBoss(monster.id), `hand-drawn ${monster.id}`));
    count++;
  }
  return count;
}

function writeOfflineCatalog() {
  const mapCatalog = JSON.parse(fs.readFileSync(mapCatalogPath, 'utf8'));
  const items = new Map();
  const addItems = (records, kind) => {
    for (const item of records || []) if (item && item.id) items.set(item.id, {
      id: item.id,
      name: item.name || item.id,
      kind,
      tier: item.tier || item.qualityName || '',
      desc: item.desc || '',
    });
  };
  addItems(botCatalog.CONSUMABLES, 'cons');
  addItems(botCatalog.EQUIPMENT, 'equip');
  addItems(Object.values(botCatalog.MATERIALS || {}), 'mat');
  const monsters = (botCatalog.MONSTERS || []).map(monster => ({
    id: monster.id,
    name: monster.name,
    realm: Number(monster.realm) || 0,
    element: monster.element || '',
    hp: Number(monster.hp) || 1,
    atk: Number(monster.atk) || 1,
    def: Number(monster.def) || 0,
    spd: Number(monster.spd) || 1,
    small: Boolean(monster.small),
    worldBoss: Boolean(monster.worldBoss),
    drops: (monster.drops || []).map(drop => ({
      kind: drop.kind || 'mat', id: drop.id, rate: Number(drop.rate) || 0,
      qty: Number(drop.qty) || 0, min: Number(drop.min) || 0, max: Number(drop.max) || 0,
    })),
  }));
  const catalog = {
    sourceMapId: 'map_1',
    sourceMapName: mapCatalog.maps?.[0]?.provinceName || 'Thanh Châu',
    sourceName: 'Telegram game catalog.js · imported for offline iOS gameplay',
    realmNames: botCatalog.CULTIVATION_REALM_NAMES || ipaCatalog.CULTIVATION_REALM_NAMES || [],
    classes: Object.values(botCatalog.MON || {}),
    elements: Object.values(botCatalog.HE || {}),
    skills: (botCatalog.SKILLS || []).map(skill => ({
      id: skill.id, mon: skill.mon || '', name: skill.name || skill.id,
      icon: skill.icon || '', kind: skill.kind || 'atk', realm: Number(skill.realm) || 0,
      mp: Number(skill.mp) || 0, cd: Number(skill.cd) || 0, desc: skill.desc || '',
    })),
    monsters,
    items: [...items.values()],
  };
  fs.writeFileSync(outputCatalog, `${JSON.stringify(catalog, null, 2)}\n`);
  return { monsters: monsters.length, items: items.size, skills: catalog.skills.length, realms: catalog.realmNames.length, maps: mapCatalog.maps?.length || 0 };
}

if (!fs.existsSync(sourceIcons) || !fs.existsSync(path.join(telegramRoot, 'catalog.js'))) {
  throw new Error(`Telegram game source not found at ${telegramRoot}`);
}
const monsters = importFolder('monsters', outputMonsterIcons);
const items = importFolder('items', outputItemIcons);
const handDrawnMonsters = fillMissingWorldBossArt(outputMonsterIcons);
const catalog = writeOfflineCatalog();
console.log(JSON.stringify({ importedMonsterArt: monsters, handDrawnMonsterArt: handDrawnMonsters, importedItemArt: items, ...catalog }, null, 2));
