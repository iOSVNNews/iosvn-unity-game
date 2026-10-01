'use strict';

// Copies the AWS mini-app's first-province monster and loot catalog into the
// Unity offline slice, including the same crisp SVG pixel icons as PNG sprites.
const fs = require('node:fs');
const path = require('node:path');
const zlib = require('node:zlib');
const crypto = require('node:crypto');

const root = path.resolve(__dirname, '../..');
const awsSourceRoot = process.env.TUTIEN_AWS_SOURCE || path.resolve(root, '..', 'Bot_Danh_Gia_Uy_Tin_Telegram', 'tutien');
const unityResources = path.join(root, 'unity_project', 'Assets', 'Resources');
function ensureUnityFolder(folderPath) {
  fs.mkdirSync(folderPath, { recursive: true });
  const metaPath = `${folderPath}.meta`;
  if (fs.existsSync(metaPath)) return;
  const relative = path.relative(root, folderPath).replaceAll(path.sep, '/');
  const guid = crypto.createHash('md5').update(relative).digest('hex');
  fs.writeFileSync(metaPath, `fileFormatVersion: 2\nguid: ${guid}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n`);
}
const mapCatalog = JSON.parse(fs.readFileSync(path.join(unityResources, 'MapCatalog.json'), 'utf8'));
const sourceCatalog = require(path.join(awsSourceRoot, 'catalog.js'));
const firstMortalMap = mapCatalog.maps.find(map => map && !map.ascensionRequired);
if (!firstMortalMap) throw new Error('MapCatalog.json has no Mortal Realm map.');

const monsterIds = [...new Set(mapCatalog.towns
  .filter(town => town && town.mapId === firstMortalMap.id)
  .flatMap(town => town.monsterPool || []))];
const monstersById = new Map(sourceCatalog.MONSTERS.map(monster => [monster.id, monster]));
const itemGroups = [
  ['equip', sourceCatalog.EQUIPMENT || []],
  ['mat', sourceCatalog.MATERIALS || []],
  ['cons', sourceCatalog.CONSUMABLES || []],
  ['skill', sourceCatalog.SKILLS || []],
];
const itemsById = new Map(itemGroups.flatMap(([kind, items]) => items.map(item => [item.id, { ...item, kind }])));
const monsters = monsterIds.map(id => monstersById.get(id)).filter(Boolean);
const lootIds = [...new Set(monsters.flatMap(monster => (monster.drops || []).map(drop => drop.id)))];
const items = lootIds.map(id => itemsById.get(id)).filter(Boolean);

const outputCatalog = {
  sourceMapId: firstMortalMap.id,
  sourceMapName: firstMortalMap.provinceName || firstMortalMap.name,
  monsters: monsters.map(monster => ({
    id: monster.id,
    name: monster.name,
    realm: monster.realm || 0,
    element: monster.element || 'tho',
    hp: monster.hp || 425,
    drops: monster.drops || [],
  })),
  items: items.map(item => ({
    id: item.id,
    name: item.name,
    kind: item.kind,
    tier: item.tier || 'pham',
    desc: item.desc || '',
  })),
};
fs.writeFileSync(path.join(unityResources, 'OfflineHuntCatalog.json'), `${JSON.stringify(outputCatalog, null, 2)}\n`);
const catalogMetaPath = path.join(unityResources, 'OfflineHuntCatalog.json.meta');
if (!fs.existsSync(catalogMetaPath)) {
  const guid = crypto.createHash('md5').update('unity_project/Assets/Resources/OfflineHuntCatalog.json').digest('hex');
  fs.writeFileSync(catalogMetaPath, `fileFormatVersion: 2\nguid: ${guid}\nTextScriptImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n`);
}

const crcTable = new Uint32Array(256);
for (let n = 0; n < 256; n++) {
  let c = n;
  for (let k = 0; k < 8; k++) c = (c & 1) ? (0xedb88320 ^ (c >>> 1)) : (c >>> 1);
  crcTable[n] = c >>> 0;
}
function crc32(buffer) {
  let c = 0xffffffff;
  for (const byte of buffer) c = crcTable[(c ^ byte) & 0xff] ^ (c >>> 8);
  return (c ^ 0xffffffff) >>> 0;
}
function pngChunk(type, data) {
  const name = Buffer.from(type);
  const length = Buffer.alloc(4); length.writeUInt32BE(data.length);
  const checksum = Buffer.alloc(4); checksum.writeUInt32BE(crc32(Buffer.concat([name, data])));
  return Buffer.concat([length, name, data, checksum]);
}
function svgPixelPng(svg, label) {
  const size = 64;
  const pixels = Buffer.alloc(size * size * 4);
  const rectPattern = /<rect\b([^>]*)\/?\s*>/g;
  let match;
  while ((match = rectPattern.exec(svg))) {
    const attrs = Object.fromEntries([...match[1].matchAll(/([\w-]+)="([^"]*)"/g)].map(value => [value[1], value[2]]));
    if (attrs.fill === undefined) continue;
    const color = /^#[0-9a-f]{6}$/i.test(attrs.fill) ? attrs.fill : '#ffffff';
    const rgba = [1, 3, 5].map(index => parseInt(color.slice(index, index + 2), 16));
    const x = Math.max(0, Math.floor(Number(attrs.x || 0)));
    const y = Math.max(0, Math.floor(Number(attrs.y || 0)));
    const width = Math.max(0, Math.ceil(Number(attrs.width || 0)));
    const height = Math.max(0, Math.ceil(Number(attrs.height || 0)));
    for (let py = y; py < Math.min(size, y + height); py++) for (let px = x; px < Math.min(size, x + width); px++) {
      const offset = (py * size + px) * 4;
      pixels[offset] = rgba[0]; pixels[offset + 1] = rgba[1]; pixels[offset + 2] = rgba[2]; pixels[offset + 3] = 255;
    }
  }
  const rows = Buffer.alloc(size * (size * 4 + 1));
  for (let y = 0; y < size; y++) pixels.copy(rows, y * (size * 4 + 1) + 1, y * size * 4, (y + 1) * size * 4);
  const header = Buffer.alloc(13);
  header.writeUInt32BE(size, 0); header.writeUInt32BE(size, 4);
  header[8] = 8; header[9] = 6; header[10] = 0; header[11] = 0; header[12] = 0;
  return Buffer.concat([
    Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]),
    pngChunk('IHDR', header),
    pngChunk('IDAT', zlib.deflateSync(rows, { level: 9 })),
    pngChunk('IEND', Buffer.alloc(0)),
  ]);
}
function skillScrollSvg(item) {
  const element = String(item.element || item.id).toLowerCase();
  const colors = { hoa: '#f17b55', tho: '#d2a776', thuy: '#6db9ec', kim: '#f1d576', moc: '#82cf85', phong: '#78d1bd', loi: '#bd9cf4' };
  const accent = Object.entries(colors).find(([key]) => element.includes(key))?.[1] || '#b59bed';
  const rects = [];
  const rect = (x, y, w, h, color) => rects.push(`<rect x="${x}" y="${y}" width="${w}" height="${h}" fill="${color}"/>`);
  rect(20, 4, 24, 4, '#202531'); rect(16, 8, 32, 44, '#202531'); rect(20, 12, 24, 36, '#e7d5a1');
  rect(24, 16, 16, 28, '#8f362f'); rect(28, 20, 8, 20, accent); rect(12, 52, 40, 4, '#202531');
  rect(20, 56, 24, 4, '#202531'); rect(24, 52, 16, 4, '#e7d5a1');
  const seed = crypto.createHash('sha1').update(item.id).digest().readUInt32BE(0);
  for (let y = 0; y < 4; y++) for (let x = 0; x < 4; x++)
    if (((seed >>> ((x + y * 4) % 24)) + x * 3 + y) % 3 === 0) rect(28 + x * 4, 20 + y * 4, 4, 4, '#f5e3a2');
  rect(8 + seed % 44, 8 + (seed >>> 5) % 44, 4, 4, accent);
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" shape-rendering="crispEdges"><g>${rects.join('')}</g></svg>`;
}
function consumablePillSvg(item) {
  const element = String(item.element || item.id).toLowerCase();
  const colors = { hoa: '#f17b55', tho: '#d2a776', thuy: '#6db9ec', kim: '#f1d576', moc: '#82cf85', phong: '#78d1bd', loi: '#bd9cf4' };
  const accent = Object.entries(colors).find(([key]) => element.includes(key))?.[1] || '#e78095';
  const rects = [];
  const rect = (x, y, w, h, color) => rects.push(`<rect x="${x}" y="${y}" width="${w}" height="${h}" fill="${color}"/>`);
  rect(24, 8, 16, 4, '#202531'); rect(16, 12, 32, 4, '#202531'); rect(12, 16, 40, 24, '#202531');
  rect(16, 40, 32, 8, '#202531'); rect(24, 48, 16, 4, '#202531');
  rect(20, 16, 24, 8, accent); rect(16, 24, 32, 12, accent); rect(20, 36, 24, 8, accent); rect(24, 44, 16, 4, accent);
  rect(20, 20, 8, 4, '#fff0b4'); rect(24, 28, 4, 4, '#f5e3a2'); rect(36, 36, 8, 4, '#202531');
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" shape-rendering="crispEdges"><g>${rects.join('')}</g></svg>`;
}
function writeSprite(kind, item) {
  const sourcePath = path.join(awsSourceRoot, 'public', 'icons', `${kind === 'monster' ? 'monsters' : 'items'}`, `${item.id}.svg`);
  if (!fs.existsSync(sourcePath) && !['skill', 'cons'].includes(kind)) return false;
  const destinationDirectory = path.join(unityResources, 'PixelArt', kind === 'monster' ? 'Monsters' : 'Items');
  ensureUnityFolder(path.join(unityResources, 'PixelArt'));
  ensureUnityFolder(destinationDirectory);
  const destinationPath = path.join(destinationDirectory, `${item.id}.png`);
  const sourceSvg = fs.existsSync(sourcePath) ? fs.readFileSync(sourcePath, 'utf8') : kind === 'skill' ? skillScrollSvg(item) : consumablePillSvg(item);
  fs.writeFileSync(destinationPath, svgPixelPng(sourceSvg, item.name || item.id));
  const relativePath = path.relative(root, destinationPath).replaceAll(path.sep, '/');
  const guid = crypto.createHash('md5').update(relativePath).digest('hex');
  fs.writeFileSync(`${destinationPath}.meta`, `fileFormatVersion: 2\nguid: ${guid}\nTextureImporter:\n  internalIDToNameTable: []\n  externalObjects: {}\n  serializedVersion: 13\n  mipmaps:\n    mipMapMode: 0\n    enableMipMap: 0\n    sRGBTexture: 1\n    linearTexture: 0\n  isReadable: 0\n  streamingMipmaps: 0\n  textureFormat: 1\n  maxTextureSize: 64\n  textureSettings:\n    serializedVersion: 2\n    filterMode: 0\n    aniso: 0\n    mipBias: 0\n    wrapU: 1\n    wrapV: 1\n    wrapW: 1\n  nPOTScale: 0\n  spriteMode: 0\n  alphaUsage: 1\n  alphaIsTransparency: 1\n  textureType: 0\n  textureShape: 1\n  platformSettings: []\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n`);
  return true;
}

let monsterSprites = 0;
for (const monster of outputCatalog.monsters) if (writeSprite('monster', monster)) monsterSprites++;
let itemSprites = 0;
for (const item of outputCatalog.items) if (writeSprite(item.kind, item)) itemSprites++;
console.log(`Synced ${monsters.length} AWS monsters, ${monsterSprites} monster pixel sprites, ${items.length} loot items, ${itemSprites} item pixel sprites for ${outputCatalog.sourceMapName}.`);
