// Lõi hệ thống Tu Tiên: nhân vật, chỉ số, thể lực, túi/kho, trang bị, kỹ năng,
// rơi đồ theo từng món và kho của game, trận săn yêu thú thời gian thực.
// Máy chủ quyết định mọi thứ; client chỉ gửi nút bấm.
'use strict';
const crypto = require('crypto');
const C = require('./catalog');
const MAX_BOSS_HP = 100_000_000;
const CREATION_TALENT_BONUSES = Object.freeze({
    dao_the: { hp: 1.08 },
    kiem_tam: { atk: 1.08 },
    tu_linh: { mp: 1.08 },
    son_nhac: { def: 1.08 },
    phong_hanh: { spd: 1.08 },
    than_thuc: { sense: 1.08 },
    phuong_hoang: { hp: 1.05, def: 1.03 },
    van_thu: { hp: 1.03, sense: 1.05 },
    ky_duyen: { hp: 1.02, mp: 1.02, atk: 1.02, def: 1.02, spd: 1.02, sense: 1.02 },
});
const CREATION_APPEARANCES = Object.freeze({
    thanh_ngoc: { gender: null, hair: 'Tóc đen', outfit: 'Áo xanh ngọc', eyes: 'Mắt lục' },
    bach_van: { gender: 'nam', hair: 'Tóc nâu', outfit: 'Áo bạch lam', eyes: 'Mắt lam' },
    xich_lien: { gender: 'nu', hair: 'Tóc bạc', outfit: 'Áo xích hắc', eyes: 'Mắt hổ phách' },
});

// Giữ nguyên dàn NPC hiện có; chỉ tự bổ sung những role còn thiếu để mỗi
// đường tu của người chơi có xấp xỉ 10 NPC đại diện.
const NPC_ROLE_SEEDS = Object.freeze({
    dan: [
        ['Tô Thanh Linh', 'Bích Lạc Đan Sư', 'nu', 'moc', 2, '🪷'], ['Mộ Dung Tử Yên', 'Tử Yên Dược Tôn', 'nu', 'hoa', 4, '🌸'],
        ['Đan Hà Tử', 'Xích Hà Đan Quân', 'nam', 'hoa', 7, '🔥'], ['Thanh Mộc Dược Vương', 'Vạn Thảo Chân Nhân', 'nam', 'moc', 10, '🌿'],
    ],
    kiem: [['Tạ Vân Kiếm', 'Thanh Tiêu Kiếm Quân', 'nam', 'phong', 3, '🗡️']],
    the: [
        ['Thiết Sơn', 'Bất Động Chiến Tôn', 'nam', 'tho', 2, '🛡️'], ['Long Tượng Tôn Giả', 'Long Tượng Kim Thân', 'nam', 'tho', 4, '🐘'],
        ['Huyền Cốt', 'Bạch Cốt Thể Tôn', 'nam', 'tho', 6, '💀'], ['Võ Thần A Man', 'Man Hoang Chiến Thần', 'nam', 'hoa', 8, '💪'],
        ['Cửu Chuyển Thể Hoàng', 'Vạn Kiếp Bất Diệt', 'nam', 'kim', 11, '⚔️'],
    ],
    quyen: [
        ['A Thanh', 'Thiết Quyền Hành Giả', 'nam', 'tho', 1, '👊'], ['Lạc Trường Phong', 'Trường Phong Quyền Sư', 'nam', 'phong', 2, '🥋'],
        ['Hồng Liên Quyền Cơ', 'Liệt Diễm Quyền Cơ', 'nu', 'hoa', 3, '🔥'], ['Tần Vô Song', 'Vô Song Quyền Tôn', 'nam', 'kim', 4, '🥊'],
        ['Mộc Dã', 'Thanh Mộc Quyền Vương', 'nam', 'moc', 5, '🌳'], ['Bắc Đường Hùng', 'Hám Sơn Quyền Hoàng', 'nam', 'tho', 6, '🏔️'],
        ['Lôi Minh', 'Cửu Tiêu Lôi Quyền', 'nam', 'loi', 7, '⚡'], ['Tạ Vô Nhai', 'Vô Nhai Võ Tôn', 'nam', 'thuy', 9, '🌊'],
        ['Chiến Thiên', 'Phá Giới Quyền Đế', 'nam', 'thien', 12, '🌌'],
    ],
    ren: [
        ['Âu Dã Tử', 'Bách Luyện Khí Sư', 'nam', 'hoa', 1, '🔨'], ['Mạc Thiết Sơn', 'Huyền Thiết Thợ Rèn', 'nam', 'tho', 2, '⚒️'],
        ['Linh Nhi', 'Linh Lung Khí Sư', 'nu', 'kim', 3, '🛠️'], ['Cổ Vân', 'Cổ Khí Chân Nhân', 'nam', 'moc', 4, '⚙️'],
        ['Hỏa Vân Tử', 'Xích Diễm Khí Tôn', 'nam', 'hoa', 5, '🔥'], ['Thiên Công Tử', 'Thiên Công Thần Tượng', 'nam', 'kim', 6, '🔩'],
        ['Bạch Luyện Tiên Cô', 'Bách Bảo Tiên Sư', 'nu', 'thuy', 7, '💎'], ['Cửu Đỉnh Tử', 'Cửu Đỉnh Khí Hoàng', 'nam', 'tho', 9, '🏺'],
        ['Vạn Khí Đạo Chủ', 'Vạn Khí Quy Tông', 'nam', 'thien', 12, '✨'],
    ],
    phu: [
        ['Tống Linh Phù', 'Thanh Trúc Phù Sư', 'nu', 'moc', 1, '📜'], ['Phù Vân Tử', 'Vân Triện Đạo Nhân', 'nam', 'phong', 2, '🌀'],
        ['Hạ Tử Khanh', 'Tử Vi Phù Cơ', 'nu', 'hoa', 3, '🔖'], ['Lục Minh', 'Kim Quang Phù Sư', 'nam', 'kim', 4, '🟡'],
        ['Vô Trần Tử', 'Thiên Cương Phù Sư', 'nam', 'thien', 5, '☯️'], ['Mặc Nghiên', 'Huyền Mặc Phù Sư', 'nu', 'thuy', 6, '🖌️'],
        ['Lôi Phù Chân Quân', 'Cửu Tiêu Lôi Phù', 'nam', 'loi', 7, '⚡'], ['Bạch Hạc Phù Tôn', 'Bạch Hạc Tiên Phù', 'nam', 'phong', 9, '🕊️'],
        ['Thái Hư Phù Đế', 'Nhất Phù Khai Thiên', 'nam', 'thien', 12, '🌠'],
    ],
    thu: [
        ['Linh Thúy', 'Thanh Khâu Ngự Thú Sư', 'nu', 'moc', 1, '🐾'], ['Trương Tiểu Phàm', 'Linh Khế Hành Giả', 'nam', 'thuy', 2, '🐕'],
        ['Bạch Linh', 'Bách Thú Tiên Tử', 'nu', 'phong', 3, '🦊'], ['Hổ Si', 'Man Hoang Thú Sư', 'nam', 'tho', 4, '🐅'],
        ['Ngự Thú Chân Nhân', 'Vạn Linh Đạo Nhân', 'nam', 'moc', 5, '🐉'], ['Hải Tâm Nương Tử', 'Thủy Linh Thú Chủ', 'nu', 'thuy', 6, '🐬'],
        ['Kim Sí Vương', 'Đại Bằng Ngự Thú Sư', 'nam', 'kim', 7, '🦅'], ['Huyền Minh', 'U Minh Linh Thú Sư', 'nam', 'am', 9, '👻'],
        ['Cửu Vĩ Thiên Hồ', 'Thanh Khâu Ngự Thú Hoàng', 'nu', 'hoa', 11, '🦊'], ['Vạn Thú Đạo Tôn', 'Chúa Tể Vạn Linh', 'nam', 'thien', 13, '🐲'],
    ],
});
const NPC_ROLE_FILL_INS = (() => {
    const counts = {};
    for (const npc of C.WORLD_NPCS || []) counts[npc.mon] = (counts[npc.mon] || 0) + 1;
    const additions = [];
    for (const [role, seeds] of Object.entries(NPC_ROLE_SEEDS)) {
        const missing = Math.max(0, 10 - (counts[role] || 0));
        for (let index = 0; index < missing; index += 1) {
            const [name, title, gender, he, baseRealm, avatar] = seeds[index];
            additions.push({
                id: `npc_role_${role}_${String(index + 1).padStart(2, '0')}`,
                name, title, gender, mon: role, he, baseRealm, avatar,
                desc: `Thiên kiêu ${C.MON[role]?.name || role}, tu luyện và lịch luyện theo thời gian thực.`,
            });
        }
    }
    return additions;
})();
const NPC_WORLD_ROSTER = Object.freeze([...(C.WORLD_NPCS || []), ...NPC_ROLE_FILL_INS]);
const npcPopulationKey = npc => `${npc?.mon || 'chung'}:${npc?.isDemon ? 'ma' : 'thuong'}`;
const NPC_ACTIVE_POPULATION_TARGETS = Object.freeze(NPC_WORLD_ROSTER.reduce((counts, npc) => {
    if (npc?.mon) {
        const key = npcPopulationKey(npc);
        counts[key] = (counts[key] || 0) + 1;
    }
    return counts;
}, {}));
const NPC_MAX_INTELLIGENCE = 100;
const NPC_FOUND_SECT_CHANCE = 0.30;
const NPC_JOIN_SECT_CHANCE = 0.70;

// Chỉ số tra cứu catalog dùng ở các đường nóng (mỗi trận và mỗi lần NPC rơi đồ).
// Tránh lọc toàn bộ trang bị hàng trăm món lặp lại cho từng trận.
const MONSTER_DROP_LIMIT = 3;
const EQUIPMENT_DROP_LIMIT = 2;
const GENERATED_MATERIAL_DROP_LIMIT = 2;
const OTHER_ITEM_DROP_LIMIT = 2;
const { monsterSkillKit } = require('./monster_skills');
const EQUIPMENT_DEFINITIONS = [...C.EQUIP_BY_ID.values()];
const monsterLootSourceLoad = new Map();
const HUNTABLE_MONSTER_IDS = new Set([
    ...(C.TOWNS || []).flatMap(town => town.monsterPool || []),
    ...(C.DUNGEONS || []).flatMap(dungeon => (dungeon.stages || []).map(stage => stage.monsterId)),
    ...(C.MONSTERS || []).filter(monster => monster.worldBoss).map(monster => monster.id),
]);
function stableLootHash(value) {
    let hash = 2166136261;
    for (const char of String(value)) hash = Math.imul(hash ^ char.charCodeAt(0), 16777619) >>> 0;
    return hash;
}
function pickBalancedMonsterSources(candidates, limit, key) {
    const unique = [...new Map(candidates.filter(Boolean).map(monster => [monster.id, monster])).values()];
    const picked = [];
    while (unique.length && picked.length < limit) {
        unique.sort((a, b) => (monsterLootSourceLoad.get(a.id) || 0) - (monsterLootSourceLoad.get(b.id) || 0)
            || stableLootHash(`${key}:${a.id}`) - stableLootHash(`${key}:${b.id}`)
            || a.id.localeCompare(b.id));
        const monster = unique.shift();
        picked.push(monster);
        monsterLootSourceLoad.set(monster.id, (monsterLootSourceLoad.get(monster.id) || 0) + 1);
    }
    return picked;
}

const NPC_EQUIPMENT_BY_REALM = Array.from({ length: 66 }, (_, realm) => EQUIPMENT_DEFINITIONS.filter(item =>
    item.drop && item.tier !== 'tien' && !item.bossOnly &&
    (item.srcMinRealm == null || item.srcMinRealm <= realm) &&
    (item.srcMaxRealm == null || realm <= item.srcMaxRealm)
));

function pickWeightedNpcEquipment(items, rng) {
    if (!items?.length) return null;
    const weights = items.map(item => Math.max(0, Number(item.drop) || 0));
    const total = weights.reduce((sum, weight) => sum + weight, 0);
    if (total <= 0) return items[Math.floor(rng() * items.length)];
    let roll = rng() * total;
    for (let i = 0; i < items.length; i += 1) {
        roll -= weights[i];
        if (roll < 0) return items[i];
    }
    return items[items.length - 1];
}
const GENERATED_MATERIAL_STOP_WORDS = new Set([
    'pham', 'hoang', 'huyen', 'dia', 'thien', 'tien', 'pham', 'phamh', 'phamchat',
    'tinh', 'than', 'linh', 'tam', 'thach', 'nguyen', 'ngu', 'hanh', 'vat', 'lieu',
]);
const GENERATED_MATERIAL_FAMILY_ELEMENTS = Object.freeze({
    'duoc lieu': ['moc', 'thuy', 'tho'],
    'khoang thach': ['kim', 'tho', 'loi'],
    'linh moc': ['moc', 'tho'],
    'linh dich': ['thuy', 'bang'],
    'linh thu lieu': ['kim', 'tho', 'hoa', 'thuy', 'bang', 'loi'],
    'tinh phach': ['am', 'ma', 'thien', 'loi', 'tho', 'phong'],
    'ngu hanh linh vat': ['moc', 'hoa', 'tho', 'kim', 'thuy'],
    'phu lieu tu luyen': ['thien', 'moc', 'thuy', 'tho', 'kim', 'loi'],
});
function generatedMaterialWords(value) {
    return String(value || '').normalize('NFD').replace(/[\u0300-\u036f]/g, '')
        .replace(/[đĐ]/g, char => char === 'Đ' ? 'D' : 'd').toLowerCase().match(/[a-z0-9]+/g) || [];
}
function generatedMaterialAffinity(material, monster) {
    const itemName = String(material.name || material.id || '').replace(/^(Phàm|Hoàng|Huyền|Địa|Thiên|Tiên)\s+/i, '');
    const itemText = [itemName, material.family].join(' ');
    const distinctiveWords = [...new Set(generatedMaterialWords(itemText).filter(word => !GENERATED_MATERIAL_STOP_WORDS.has(word)))];
    const monsterName = new Set(generatedMaterialWords(monster.name));
    const monsterContext = new Set(generatedMaterialWords([monster.id, monster.desc, monster.trait].join(' ')));
    const nameMatches = distinctiveWords.filter(word => monsterName.has(word));
    const contextMatches = distinctiveWords.filter(word => !monsterName.has(word) && monsterContext.has(word));
    let score = 1;
    const itemElement = material.element || material.he || material.elementId || material.elemMastery?.element;
    if (itemElement && monster.element === itemElement) score += 3.5;
    const familyElements = GENERATED_MATERIAL_FAMILY_ELEMENTS[generatedMaterialWords(material.family).join(' ')] || [];
    if (familyElements.includes(monster.element)) score += 0.65;
    const elementWords = {
        kim: ['kim', 'thep', 'sat', 'vang', 'bac'], moc: ['moc', 'thao', 'la', 're'], thuy: ['thuy', 'nuoc', 'hai', 'bien', 'song'],
        hoa: ['hoa', 'lua', 'diem', 'viem', 'xich'], tho: ['tho', 'son', 'nui', 'thach', 'dia mach'],
        loi: ['loi', 'dien', 'tieu', 'thien kiep'], phong: ['phong', 'gio', 'van'], bang: ['bang', 'tuyet', 'han'],
        am: ['am', 'u minh', 'hon', 'quy'], duong: ['duong', 'nhat', 'quang'], ma: ['ma', 'sat luc', 'huyet ma'], thien: ['thien dao', 'thien'],
    };
    const itemWords = new Set(distinctiveWords);
    for (const [element, terms] of Object.entries(elementWords)) {
        if (element === monster.element && terms.some(term => generatedMaterialWords(term).every(word => itemWords.has(word)))) score += 1.4;
    }
    score += nameMatches.length * 2.4 + contextMatches.length * 0.7;
    const rawMin = Number(material.minRealm ?? material.realmMin ?? material.realm ?? material.toRealm ?? C.TIER[material.tier]?.realm ?? 0);
    const minRealm = Number.isFinite(rawMin) ? rawMin : 0;
    const rawMax = Number(material.maxRealm ?? material.srcMaxRealm ?? (minRealm + 3));
    const maxRealm = Number.isFinite(rawMax) ? Math.max(minRealm, rawMax) : minRealm + 3;
    const monsterRealm = Number(monster.realm) || 0;
    const realmDistance = monsterRealm < minRealm ? minRealm - monsterRealm : monsterRealm > maxRealm ? monsterRealm - maxRealm : 0;
    score += realmDistance === 0 ? 0.6 : 0.6 / (1 + realmDistance);
    if (monster.worldBoss) score += 0.5;
    else if (monster.tier === 'elite') score += 0.25;
    // Keep even exact score ties distinct and deterministic.
    score *= 0.9 + stableLootHash(material.id + ':' + monster.id) / 0xffffffff * 0.2;
    return score;
}
function pickAffinityBalancedMonsterSources(candidates, limit, key, material) {
    const unique = [...new Map(candidates.filter(Boolean).map(monster => [monster.id, monster])).values()];
    const picked = [];
    while (unique.length && picked.length < limit) {
        unique.sort((a, b) => {
            const scoreA = generatedMaterialAffinity(material, a) - Math.log1p(monsterLootSourceLoad.get(a.id) || 0) * 0.15;
            const scoreB = generatedMaterialAffinity(material, b) - Math.log1p(monsterLootSourceLoad.get(b.id) || 0) * 0.15;
            return scoreB - scoreA || stableLootHash(`${key}:${a.id}`) - stableLootHash(`${key}:${b.id}`) || a.id.localeCompare(b.id);
        });
        const monster = unique.shift();
        picked.push(monster);
        monsterLootSourceLoad.set(monster.id, (monsterLootSourceLoad.get(monster.id) || 0) + 1);
    }
    return picked;
}


const allMonsterDropsByItem = new Map();
for (const monster of C.MONSTERS || []) {
    for (const drop of monster.drops || []) {
        if (drop.kind === 'equip') continue; // Equipment uses its curated source map below.
        if (!allMonsterDropsByItem.has(drop.id)) allMonsterDropsByItem.set(drop.id, []);
        allMonsterDropsByItem.get(drop.id).push({ monster, drop });
    }
}
const MONSTER_DROP_SOURCES_BY_ITEM = new Map();
const MONSTER_DROP_ALLOWED_BY_ITEM = new Map();
const MONSTER_DROP_RATE_BY_ITEM = new Map();
function rawMonsterDropChance(drop) {
    if (drop.kind === 'equip') {
        const maxChance = 0.06; // tăng 50% trần rơi trang bị từ 0.04 lên 0.06
        return drop.directChance
            ? Math.min(0.075, (Number(drop.rate) || 0) * 1.5)
            : Math.min(maxChance, (Number(drop.rate) || 0.1) * 0.08 * 1.5);
    }
    const maxChance = drop.kind === 'mat' ? 0.12 : (drop.kind === 'cons' ? 0.08 : 0.04);
    return drop.directChance
        ? Math.min(0.05, Number(drop.rate) || 0)
        : Math.min(maxChance, (Number(drop.rate) || 0.1) * 0.08);
}
function addRateToItemMap(map, itemId, monsterId, rate) {
    if (!map.has(itemId)) map.set(itemId, new Map());
    map.get(itemId).set(monsterId, rate);
}
function distinctSourceRateMap(item, monsters, getBaseRate, limit = monsters.length) {
    const ranked = [...new Map(monsters.filter(Boolean).map(monster => [monster.id, monster])).values()]
        .sort((a, b) => generatedMaterialAffinity(item, b) - generatedMaterialAffinity(item, a)
            || a.id.localeCompare(b.id))
        .slice(0, limit);
    const baseRate = Math.max(0, ...ranked.map(getBaseRate));
    const rates = new Map();
    for (const [index, monster] of ranked.entries()) {
        rates.set(monster.id, baseRate * (index === 0 ? 1 : Math.pow(0.3, index)));
    }
    return { monsters: ranked, rates };
}
function itemRealm(item, fallback = 0) {
    for (const value of [item?.minRealm, item?.realmMin, item?.realm, item?.toRealm, C.TIER[item?.tier]?.realm]) {
        const realm = Number(value);
        if (Number.isFinite(realm)) return realm;
    }
    return fallback;
}
function reachableMonsterCandidates(item, minRealm = itemRealm(item), maxRealm = minRealm + 3, extraFilter = () => true) {
    const reachable = (C.MONSTERS || []).filter(monster => HUNTABLE_MONSTER_IDS.has(monster.id) && extraFilter(monster));
    let candidates = reachable.filter(monster => monster.realm >= minRealm && monster.realm <= maxRealm);
    if (candidates.length) return candidates;
    const nearestDistance = Math.min(...reachable.map(monster => Math.abs(monster.realm - minRealm)), Infinity);
    return reachable.filter(monster => Math.abs(monster.realm - minRealm) === nearestDistance);
}
for (const [itemId, candidates] of [...allMonsterDropsByItem.entries()].sort(([a], [b]) => a.localeCompare(b))) {
    const item = C.MATERIAL_BY_ID?.get(itemId) || C.CONSUMABLE_BY_ID?.get(itemId) || { id: itemId, name: itemId };
    const reachableEntries = candidates.filter(entry => HUNTABLE_MONSTER_IDS.has(entry.monster.id));
    const eligibleEntries = reachableEntries.length ? reachableEntries : [];
    const sourceMonsters = pickAffinityBalancedMonsterSources(eligibleEntries.map(entry => entry.monster), MONSTER_DROP_LIMIT, `drop:${itemId}`, item)
        .sort((a, b) => generatedMaterialAffinity(item, b) - generatedMaterialAffinity(item, a) || a.id.localeCompare(b.id));
    const sourceIds = new Set(sourceMonsters.map(monster => monster.id));
    if (sourceIds.size) MONSTER_DROP_ALLOWED_BY_ITEM.set(itemId, sourceIds);
    const byMonster = new Map();
    for (const entry of eligibleEntries) {
        if (sourceIds.has(entry.monster.id) && !byMonster.has(entry.monster.id)) byMonster.set(entry.monster.id, entry);
    }
    const entries = [...byMonster.values()].sort((a, b) => generatedMaterialAffinity(item, b.monster) - generatedMaterialAffinity(item, a.monster) || a.monster.id.localeCompare(b.monster.id));
    if (entries.length) {
        MONSTER_DROP_SOURCES_BY_ITEM.set(itemId, entries);
        const rateMap = distinctSourceRateMap(item, entries.map(entry => entry.monster), monster => {
            const entry = entries.find(candidate => candidate.monster.id === monster.id);
            return entry ? rawMonsterDropChance(entry.drop) : 0;
        }, MONSTER_DROP_LIMIT).rates;
        MONSTER_DROP_RATE_BY_ITEM.set(itemId, rateMap);
    }
}

// Consumables that only came from shops/recipes still receive a rare, curated
// monster drop source, so the Codex never leaves an obtainable item untraced.
const SUPPLEMENTAL_ITEM_DROPS_BY_ITEM = new Map();
const SUPPLEMENTAL_ITEM_DROPS_BY_MONSTER = new Map();
function supplementalItemBaseRate(item, kind) {
    const rank = Math.max(0, Number(item.qualityRank) || 0, Number(C.TIER[item.tier]?.rank) || 0);
    if (kind === 'cons') return Math.max(0.00008, 0.004 / Math.pow(2.4, rank));
    if (kind === 'equip') return Math.max(0.000015, (0.0005 / Math.pow(2, rank)) * 1.5);
    return Math.max(0.00001, 0.0005 / Math.pow(2, rank));
}
function assignSupplementalItemSources(item, kind, options = {}) {
    const minRealm = options.minRealm ?? itemRealm(item);
    const maxRealm = options.maxRealm ?? minRealm + 3;
    const candidates = options.candidates || reachableMonsterCandidates(item, minRealm, maxRealm, options.filter);
    const picked = pickAffinityBalancedMonsterSources(candidates, OTHER_ITEM_DROP_LIMIT, `supplemental:${item.id}`, item)
        .sort((a, b) => generatedMaterialAffinity(item, b) - generatedMaterialAffinity(item, a) || a.id.localeCompare(b.id));
    if (!picked.length) return [];
    const { rates } = distinctSourceRateMap(item, picked, () => options.baseRate ?? supplementalItemBaseRate(item, kind), OTHER_ITEM_DROP_LIMIT);
    const assigned = picked.map(monster => ({ monster, item, kind, rate: rates.get(monster.id) || 0 }));
    SUPPLEMENTAL_ITEM_DROPS_BY_ITEM.set(item.id, assigned);
    for (const source of assigned) {
        if (!SUPPLEMENTAL_ITEM_DROPS_BY_MONSTER.has(source.monster.id)) SUPPLEMENTAL_ITEM_DROPS_BY_MONSTER.set(source.monster.id, []);
        SUPPLEMENTAL_ITEM_DROPS_BY_MONSTER.get(source.monster.id).push(source);
    }
    return assigned;
}
for (const consumable of C.CONSUMABLES || []) {
    if (!MONSTER_DROP_SOURCES_BY_ITEM.has(consumable.id)) assignSupplementalItemSources(consumable, 'cons');
}

// Equipment gets explicit, balanced source monsters instead of dropping from
// every monster in a realm interval. Existing named sources are preserved.
function calculateEquipDropRate(item, monster) {
    const tier = item.tier || 'pham';
    const mRealm = monster.realm ?? 0;
    let baseRate = 0.005;
    if (tier === 'thien') {
        if (mRealm <= 7) baseRate = 0.005;
        else if (mRealm === 8) baseRate = 0.008;
        else if (mRealm === 9) baseRate = 0.011;
        else baseRate = 0.014;
    } else if (tier === 'dia') {
        if (mRealm <= 5) baseRate = 0.005;
        else baseRate = 0.008;
    } else if (tier === 'huyen') {
        baseRate = 0.006;
    } else if (tier === 'hoang') {
        baseRate = 0.008;
    } else if (tier === 'pham') {
        baseRate = 0.012;
    } else if (tier === 'tien') {
        if (mRealm <= 11) baseRate = 0.005;
        else if (mRealm >= 30) baseRate = 0.020;
        else baseRate = 0.005 + ((mRealm - 11) / 19) * 0.015;
    }
    if (item.drop && Number(item.drop) > 0) {
        baseRate = Math.max(baseRate, Number(item.drop));
    }
    const smallFactor = monster.small ? 0.75 : 1.0;
    return Number((baseRate * smallFactor).toFixed(5));
}

const EQUIPMENT_SOURCE_BY_ITEM = new Map();
const EQUIPMENT_DROP_BY_MONSTER = new Map();
const EQUIPMENT_DROP_RATE_BY_ITEM = new Map();
for (const item of EQUIPMENT_DEFINITIONS.sort((a, b) => a.id.localeCompare(b.id))) {
    const tierRealm = Number(C.TIER[item.tier]?.realm) || 0;
    const minRealm = Number.isFinite(Number(item.srcMinRealm)) ? Number(item.srcMinRealm) : (item.tier === 'pham' ? 0 : tierRealm);
    const maxRealm = Number.isFinite(Number(item.srcMaxRealm)) ? Number(item.srcMaxRealm) : (item.tier === 'pham' ? 1 : (item.tier === 'dia' ? 6 : (item.tier === 'thien' ? 10 : minRealm + 2)));
    const isValidSource = monster => (!item.worldBossOnly || monster.worldBoss)
        && (!(item.elite || item.unique || item.bossOnly) || !monster.small)
        && (!item.bossOnly || !monster.small);
    const namedSources = Array.isArray(item.src)
        ? item.src.map(id => C.MONSTER_BY_ID.get(id)).filter(monster => monster && HUNTABLE_MONSTER_IDS.has(monster.id) && (!item.worldBossOnly || monster.worldBoss) && monster.realm >= minRealm && monster.realm <= maxRealm)
        : [];
    let candidates = [...new Map([...namedSources, ...C.MONSTERS.filter(monster =>
        HUNTABLE_MONSTER_IDS.has(monster.id) && monster.realm >= minRealm && monster.realm <= maxRealm && isValidSource(monster))]
        .map(monster => [monster.id, monster])).values()];
    if (!candidates.length) {
        candidates = C.MONSTERS.filter(monster => HUNTABLE_MONSTER_IDS.has(monster.id) && isValidSource(monster))
            .sort((a, b) => Math.abs(a.realm - minRealm) - Math.abs(b.realm - minRealm));
        if (candidates.length) {
            const closestRealm = candidates[0].realm;
            candidates = candidates.filter(monster => monster.realm === closestRealm);
        }
    }
    const sourceMonsters = pickAffinityBalancedMonsterSources(candidates, EQUIPMENT_DROP_LIMIT, `equipment:${item.id}`, item)
        .sort((a, b) => generatedMaterialAffinity(item, b) - generatedMaterialAffinity(item, a) || a.id.localeCompare(b.id));
    EQUIPMENT_SOURCE_BY_ITEM.set(item.id, sourceMonsters);
    const equipmentRateMap = new Map();
    sourceMonsters.forEach((monster, index) => {
        const base = calculateEquipDropRate(item, monster);
        const rate = index === 0 ? base : Number((base * 0.7).toFixed(5));
        equipmentRateMap.set(monster.id, rate);
    });
    EQUIPMENT_DROP_RATE_BY_ITEM.set(item.id, equipmentRateMap);
    for (const monster of sourceMonsters) {
        if (!EQUIPMENT_DROP_BY_MONSTER.has(monster.id)) EQUIPMENT_DROP_BY_MONSTER.set(monster.id, []);
        EQUIPMENT_DROP_BY_MONSTER.get(monster.id).push(item);
    }
}

// Generated materials keep their quality band and element affinity, while each
// individual material is assigned to only two suitable, balanced monsters.
const GENERATED_MATERIAL_SOURCE_BY_ITEM = new Map();
const GENERATED_MATERIALS_BY_MONSTER = new Map();
const generatedMaterialLegacyPools = new Map();
const generatedMaterialRealmPools = new Map();
// Vật liệu Thiên Đạo chỉ rơi từ một vài thủ lĩnh Cổ Động phù hợp; mỗi thủ lĩnh
// có tỉ lệ riêng theo mức độ gắn với món đồ.
const DUNGEON_MATERIAL_DROP_SOURCES_BY_ITEM = new Map([
    ['mat_thien_dao_tinh', [
        { dungeonId: 'dong_lao_quan_dien', rate: 0.0213 }, // Thiên Đạo Lôi Thú: cùng lôi ý Thiên Đạo
        { dungeonId: 'dong_thai_so', rate: 0.0147 }, // Thiên Kiếp Lôi Ngao: cùng hệ Lôi
    ]],
    ['mat_thien_dao_nguyen_an', [
        { dungeonId: 'dong_do_kiep_dai', rate: 0.35 }, // Vạn Kiếp Phong Lôi Cổ Động (Độ Kiếp Đài - Map 8)
        { dungeonId: 'dong_to_long_dao', rate: 0.30 }, // Tổ Long Cổ Động (Tổ Long Đảo - Map 8)
        { dungeonId: 'dong_ma_quat', rate: 0.25 }, // Vạn Kiếp Ma Quật (Thiên Đạo Tông - Map 8)
        { dungeonId: 'thi_luyen_map_8', rate: 0.35 }, // Bí Cảnh Thí Luyện Man Châu (Map 8)
        { dungeonId: 'dong_lao_quan_dien', rate: 0.0089 }, // Thiên Đạo Lôi Thú (Map 9)
        { dungeonId: 'dong_hon_don_tien_dinh', rate: 0.0043 }, // Tàn Niệm Tiên Đế (Map 9)
    ]],
]);
const DUNGEON_MATERIAL_DROP_RULES_BY_DUNGEON = new Map();
for (const [itemId, sources] of DUNGEON_MATERIAL_DROP_SOURCES_BY_ITEM) {
    for (const source of sources) {
        if (!DUNGEON_MATERIAL_DROP_RULES_BY_DUNGEON.has(source.dungeonId)) {
            DUNGEON_MATERIAL_DROP_RULES_BY_DUNGEON.set(source.dungeonId, new Map());
        }
        DUNGEON_MATERIAL_DROP_RULES_BY_DUNGEON.get(source.dungeonId).set(itemId, source.rate);
    }
}

for (const monster of C.MONSTERS) {
    const realmPool = C.MATERIALS.filter(material => material.generated
        && monster.realm >= material.minRealm && monster.realm <= material.maxRealm);
    const elementPool = monster.element ? realmPool.filter(material => material.element === monster.element) : [];
    generatedMaterialLegacyPools.set(monster.id, elementPool.length ? elementPool : realmPool);
    generatedMaterialRealmPools.set(monster.id, realmPool);
}
const generatedMaterialBaseChance = monster => monster.worldBoss ? 0.12
    : (monster.tier === 'elite' ? 0.06 : (monster.small ? 0.03 : (monster.tier === 'normal' ? 0.045 : 0.09)));
for (const material of C.MATERIALS.filter(entry => entry.generated).sort((a, b) => a.id.localeCompare(b.id))) {
    const candidates = C.MONSTERS.filter(monster => HUNTABLE_MONSTER_IDS.has(monster.id)
        && (generatedMaterialRealmPools.get(monster.id) || []).some(entry => entry.id === material.id));
    const sourceMonsters = pickAffinityBalancedMonsterSources(candidates, GENERATED_MATERIAL_DROP_LIMIT, `material:${material.id}`, material)
        .sort((a, b) => generatedMaterialAffinity(material, b) - generatedMaterialAffinity(material, a));
    const legacyEligible = candidates.filter(monster => (generatedMaterialLegacyPools.get(monster.id) || []).some(entry => entry.id === material.id));
    const legacyExpected = legacyEligible.reduce((sum, monster) => {
        const pool = generatedMaterialLegacyPools.get(monster.id) || [];
        return sum + (pool.length ? generatedMaterialBaseChance(monster) / pool.length : 0);
    }, 0);
    const sourceRates = sourceMonsters.map((monster, index) =>
        legacyExpected * (sourceMonsters.length > 1 ? (index === 0 ? 0.5 : 0.15) : 1));
    const dropScaleByMonster = new Map(sourceMonsters.map((monster, index) => [
        monster.id,
        sourceRates[index] > 0
            ? sourceRates[index] / generatedMaterialBaseChance(monster)
            : 0,
    ]));
    GENERATED_MATERIAL_SOURCE_BY_ITEM.set(material.id, {
        monsters: sourceMonsters,
        dropScaleByMonster,
    });
    for (const monster of sourceMonsters) {
        if (!GENERATED_MATERIALS_BY_MONSTER.has(monster.id)) GENERATED_MATERIALS_BY_MONSTER.set(monster.id, []);
        GENERATED_MATERIALS_BY_MONSTER.get(monster.id).push(material);
    }
}
function generatedMaterialRate(itemId, monster) {
    const sourceInfo = GENERATED_MATERIAL_SOURCE_BY_ITEM.get(itemId);
    const sourceScale = Number(sourceInfo?.dropScaleByMonster?.get(monster.id)) || 0;
    if (!(sourceScale > 0)) return 0;
    const totalWeight = (GENERATED_MATERIALS_BY_MONSTER.get(monster.id) || []).reduce((sum, material) =>
        sum + (Number(GENERATED_MATERIAL_SOURCE_BY_ITEM.get(material.id)?.dropScaleByMonster?.get(monster.id)) || 0), 0);
    if (!(totalWeight > 0)) return 0;
    return Math.min(0.12, generatedMaterialBaseChance(monster) * totalWeight) * sourceScale / totalWeight;
}
for (let pass = 0; pass < 30; pass += 1) {
    let adjusted = false;
    for (const material of C.MATERIALS.filter(entry => entry.generated)) {
        const sourceInfo = GENERATED_MATERIAL_SOURCE_BY_ITEM.get(material.id);
        const priorRates = new Set();
        for (const monster of sourceInfo?.monsters || []) {
            let rate = generatedMaterialRate(material.id, monster);
            let attempts = 0;
            while (rate > 0 && priorRates.has(formatDropPercent(rate)) && attempts < 20) {
                const oldScale = Number(sourceInfo.dropScaleByMonster.get(monster.id)) || 0;
                sourceInfo.dropScaleByMonster.set(monster.id, oldScale * 0.5);
                rate = generatedMaterialRate(material.id, monster);
                adjusted = true;
                attempts += 1;
            }
            if (rate > 0) priorRates.add(formatDropPercent(rate));
        }
    }
    if (!adjusted) break;
}
for (const material of C.MATERIALS || []) {
    if (material.generated || MONSTER_DROP_SOURCES_BY_ITEM.has(material.id) || DUNGEON_MATERIAL_DROP_SOURCES_BY_ITEM.has(material.id)) continue;
    assignSupplementalItemSources(material, 'mat');
}

function curatedLootSources(item, namedSources, limit, minRealm = itemRealm(item), maxRealm = minRealm + 3, filter = () => true) {
    const picked = [...new Map(namedSources.filter(Boolean).map(monster => [monster.id, monster])).values()].slice(0, limit);
    const candidates = reachableMonsterCandidates(item, minRealm, maxRealm, filter).filter(monster => !picked.some(source => source.id === monster.id));
    while (picked.length < limit && candidates.length) {
        candidates.sort((a, b) => generatedMaterialAffinity(item, b) / Math.sqrt(1 + (monsterLootSourceLoad.get(b.id) || 0))
            - generatedMaterialAffinity(item, a) / Math.sqrt(1 + (monsterLootSourceLoad.get(a.id) || 0))
            || a.id.localeCompare(b.id));
        const monster = candidates.shift();
        picked.push(monster);
        monsterLootSourceLoad.set(monster.id, (monsterLootSourceLoad.get(monster.id) || 0) + 1);
    }
    return picked.sort((a, b) => generatedMaterialAffinity(item, b) - generatedMaterialAffinity(item, a) || a.id.localeCompare(b.id));
}

const SKILL_SOURCE_BY_ITEM = new Map();
const SKILL_DROP_BY_MONSTER = new Map();
const SKILL_DROP_RATE_BY_ITEM = new Map();
const DEMON_MONSTER_WORDS = /\b(ma|quy|huyet|am sat|u minh|nghich ma|ma khi|ma linh)\b/;
function isDemonLootSource(monster) {
    return monster.element === 'ma' || DEMON_MONSTER_WORDS.test(generatedMaterialWords([monster.id, monster.name, monster.desc, monster.trait].join(' ')).join(' '));
}
function skillDropBaseRate(skill) {
    if (Number(skill.drop) > 0) return Number(skill.drop);
    const rank = Number(C.SKILL_RULES?.rarityRank?.[skill.rarity]) || 0;
    return Math.max(0.000001, 0.004 / Math.pow(3, rank));
}
for (const skill of C.SKILLS || []) {
    const minRealm = Math.max(0, Number(skill.realm) || 0);
    const maxRealm = Math.min(65, minRealm + 4);
    const demonFilter = skill.demonOnly ? isDemonLootSource : () => true;
    let candidates = reachableMonsterCandidates(skill, minRealm, maxRealm, monster => demonFilter(monster)
        && (!monster.small || skill.rarity === 'pt'));
    if (candidates.length < 2 && skill.demonOnly) {
        candidates = reachableMonsterCandidates(skill, minRealm, maxRealm, monster => !monster.small || skill.rarity === 'pt');
    }
    const sources = pickAffinityBalancedMonsterSources(candidates, OTHER_ITEM_DROP_LIMIT, 'skill:' + skill.id, skill)
        .sort((a, b) => generatedMaterialAffinity(skill, b) - generatedMaterialAffinity(skill, a) || a.id.localeCompare(b.id));
    const rateMap = distinctSourceRateMap(skill, sources, monster => skillDropBaseRate(skill) * (monster.small ? 0.6 : 1), OTHER_ITEM_DROP_LIMIT).rates;
    SKILL_SOURCE_BY_ITEM.set(skill.id, sources);
    SKILL_DROP_RATE_BY_ITEM.set(skill.id, rateMap);
    for (const monster of sources) {
        if (!SKILL_DROP_BY_MONSTER.has(monster.id)) SKILL_DROP_BY_MONSTER.set(monster.id, []);
        SKILL_DROP_BY_MONSTER.get(monster.id).push({ skill, rate: rateMap.get(monster.id) || 0 });
    }
}

const FIRE_DROP_BY_MONSTER = new Map();
const FIRE_DROP_RATE_BY_ITEM = new Map();
for (const fire of C.CRAFT_FIRES || []) {
    const named = (fire.sourceMonsterIds || [fire.sourceMonsterId]).map(id => C.MONSTER_BY_ID.get(id)).filter(monster => monster && HUNTABLE_MONSTER_IDS.has(monster.id));
    const sources = curatedLootSources(fire, named, 2, fire.realmMin, fire.realmMin + 4);
    const rates = distinctSourceRateMap(fire, sources, () => fire.dropChance, 2).rates;
    FIRE_DROP_RATE_BY_ITEM.set(fire.id, rates);
    for (const monster of sources) {
        if (!FIRE_DROP_BY_MONSTER.has(monster.id)) FIRE_DROP_BY_MONSTER.set(monster.id, []);
        FIRE_DROP_BY_MONSTER.get(monster.id).push(fire);
    }
}
const GOURD_DROP_BY_MONSTER = new Map();
const GOURD_SOURCE_BY_ITEM = new Map();
const GOURD_DROP_RATE_BY_ITEM = new Map();
for (const gourd of C.BEAST_GOURDS || []) {
    const named = (gourd.sourceMonsterIds || [gourd.sourceMonsterId]).map(id => C.MONSTER_BY_ID.get(id)).filter(monster => monster && HUNTABLE_MONSTER_IDS.has(monster.id));
    const sources = curatedLootSources(gourd, named, 2, gourd.realmMin, gourd.realmMin + 4);
    const baseRate = Number(gourd.dropChance) || (gourd.starter ? 0.0001 : supplementalItemBaseRate(gourd, 'gourd'));
    const rates = distinctSourceRateMap(gourd, sources, () => baseRate, 2).rates;
    GOURD_SOURCE_BY_ITEM.set(gourd.id, sources);
    GOURD_DROP_RATE_BY_ITEM.set(gourd.id, rates);
    for (const monster of sources) {
        if (!GOURD_DROP_BY_MONSTER.has(monster.id)) GOURD_DROP_BY_MONSTER.set(monster.id, []);
        GOURD_DROP_BY_MONSTER.get(monster.id).push(gourd);
    }
}
function equipmentDropsFor(monster) {
    return EQUIPMENT_DROP_BY_MONSTER.get(monster.id) || [];
}
function bossMutationEquipMultiplier(m) {
    if (!m) return 1;
    const name = String(m.name || '');
    const devour = Number(m.devourCount) || 0;
    const level = Number(m.level) || 0;
    const realm = Number(m.realm) || 0;

    let mutationMul = 1.0;
    // Bậc dị biến Đại Boss: thường < Yêu Tướng < Yêu Vương < Yêu Vương Thôn Thiên
    if (name.includes('Yêu Vương Thôn Thiên') || devour >= 3) {
        mutationMul = 2.0; // Yêu Vương Thôn Thiên: +100% tỉ lệ rơi trang bị
    } else if (name.includes('Yêu Vương') || devour >= 2) {
        mutationMul = 1.5; // Yêu Vương: +50% tỉ lệ rơi trang bị
    } else if (name.includes('Yêu Tướng') || name.includes('Thôn Phệ') || devour >= 1 || level >= 2) {
        mutationMul = 1.25; // Yêu Tướng: +25% tỉ lệ rơi trang bị
    } else {
        mutationMul = 1.0; // Thường
    }

    // Bậc cảnh giới của Đại Boss: mỗi cảnh giới tăng thêm 3% (tối đa +45%)
    const realmMul = (!m.small || m.worldBoss) ? 1 + Math.min(0.45, realm * 0.03) : 1;
    return mutationMul * realmMul;
}

const TOWN_REALM_CAP = new Map(C.TOWNS.map(town => {
    const localMax = (town.monsterPool || []).reduce((max, id) => Math.max(max, C.MONSTER_BY_ID.get(id)?.realm || 0), 0);
    const townRealm = Math.max(0, Number(town.realmMin) || 0);
    const mapMax = Number(C.MAP_BY_ID.get(town.mapId)?.realmMax);
    const mapCeiling = Number.isFinite(mapMax) ? Math.max(townRealm, mapMax) : 65;
    const configuredCap = Number.isFinite(Number(town.realmCap)) ? Number(town.realmCap) : Math.max(townRealm + 3, localMax);
    const hardCeiling = Math.min(65, mapCeiling, Number.isFinite(Number(town.realmCap)) ? Number(town.realmCap) : townRealm + 3);
    return [town.id, Math.max(townRealm, Math.min(configuredCap, hardCeiling))];
}));
const ASSIGNED_TOWN_MONSTER_IDS = new Set(C.TOWNS.flatMap(town => town.monsterPool || []));
const UNASSIGNED_NORMAL_MONSTER_IDS = new Set(C.MONSTERS
    .filter(monster => !monster.worldBoss && !ASSIGNED_TOWN_MONSTER_IDS.has(monster.id))
    .map(monster => monster.id));

const VN_OFFSET_MS = 7 * 3600 * 1000;
const PVP_ACTIVE_WINDOW_MS = 30 * 24 * 60 * 60 * 1000;
const PVP_BOT_IDLE_MS = 2 * 60 * 1000;
const DAO_RECOVERY_INTERVAL_MS = 24 * 60 * 60 * 1000;
const DAO_RECOVERY_PER_DAY = 2;
const NPC_EQUIPMENT_DROP_CHANCE = 0.01;
const NPC_BAG_RECOVERY_CHANCE = 0.10;
const vnDate = now => new Date(now + VN_OFFSET_MS).toISOString().slice(0, 10);
const vnHour = now => new Date(now + VN_OFFSET_MS).getUTCHours();
const isNight = now => { const h = vnHour(now); return h >= 18 || h < 6; };
function formatDropPercent(rate) {
    const percent = Number(rate) * 100;
    if (!Number.isFinite(percent) || percent <= 0) return '0';
    const digits = percent >= 1 ? 2 : Math.min(12, Math.ceil(-Math.log10(percent)) + 1);
    return percent.toFixed(digits).replace(/\.?0+$/, '').replace('.', ',');
}

function craftingTierRank(def = {}, recipe = {}) {
    const tierRank = Math.max(C.TIER[recipe.tier]?.rank ?? 0, C.TIER[def.tier]?.rank ?? 0);
    return clamp(Math.max(
        Number.isFinite(Number(def.qualityRank)) ? Number(def.qualityRank) : 0,
        tierRank,
        C.qualityRankForRealm?.(def.realmMin ?? def.toRealm ?? 0) || 0,
    ), 0, 5);
}
function requiredFireRankForCraft(rank = 0) {
    if (rank <= 1) return 0;
    return clamp(rank - 1, 1, 4);
}
function inboxMailHasRewards(mail) {
    return Boolean(mail && (
        Number(mail.stones) > 0 ||
        Number(mail.exp) > 0 ||
        (Array.isArray(mail.items) && mail.items.length > 0)
    ));
}
const SCRIBE_CRAFT_RANKS = Object.freeze([
    { xp: 0, name: 'Phù Học Đồ' },
    { xp: 10, name: 'Phù Sư Tập Sự' },
    { xp: 30, name: 'Phù Sư Chính Thức' },
    { xp: 60, name: 'Đại Phù Sư' },
    { xp: 100, name: 'Phù Tông' },
    { xp: 150, name: 'Tiên Phù Sư' },
]);
function isCraftingTool(def) {
    return Boolean(def && (def.slot === 'lo_dinh' || ['dinh', 'bua', 'but'].includes(def.wtype)));
}
function craftingToolRepairCost(def, durability) {
    if (!def || !isCraftingTool(def)) return null;
    const tier = C.TIER[def.tier] || C.TIER.pham;
    const rank = Number(tier.rank) || 0;
    // Keep artisan tools meaningfully more expensive than ordinary gear, but
    // soften the previous 20x multiplier and floor so late-game repairs do
    // not consume an unreasonable amount of spirit stones.
    const costPerPoint = Math.max((Number(tier.repair) || 0) * 12, 30 * (rank + 1));
    const missing = Math.max(0, 100 - clamp(Number(durability ?? 100), 0, 100));
    return Math.ceil(missing * costPerPoint);
}
function equipmentRepairCost(def, durability) {
    if (!def) return 0;
    const toolCost = craftingToolRepairCost(def, durability);
    if (toolCost != null) return toolCost;
    const tier = C.TIER[def.tier] || C.TIER.pham;
    const rank = Number(tier.rank) || 0;
    const costPerPoint = Math.max(2 * (rank + 1), Math.round((Number(tier.repair) || 0) * 2.0));
    const missing = Math.max(0, 100 - clamp(Number(durability ?? 100), 0, 100));
    return missing > 0 ? Math.max(1, Math.ceil(missing * costPerPoint)) : 0;
}
function artisanExpertise(roleStat) { return Math.min(0.08, Math.max(0, Number(roleStat) || 0) * 0.0004); }
function alchemySuccessRate(cauldronDef, recipeRank, expertise = 0) {
    const toolRank = C.TIER[cauldronDef?.tier]?.rank || 0;
    const base = clamp(Number(cauldronDef?.alchemyRate) || 0.55, 0.40, 0.92);
    const gap = Math.max(0, recipeRank - toolRank);
    return clamp(base - recipeRank * 0.07 - gap * 0.05 + expertise, 0.05, 0.85);
}
function forgingSuccessRate(recipeRank, toolRank, expertise = 0) {
    const baseByRank = [0.90, 0.80, 0.68, 0.54, 0.40, 0.28];
    const gap = Math.max(0, recipeRank - toolRank);
    return clamp(baseByRank[recipeRank] - gap * 0.05 + expertise + Math.min(0.05, toolRank * 0.01), 0.05, 0.90);
}
function talismanSuccessRate(recipeRank, toolRank, expertise = 0) {
    const baseByRank = [0.88, 0.78, 0.66, 0.52, 0.38, 0.26];
    const gap = Math.max(0, recipeRank - toolRank);
    return clamp(baseByRank[recipeRank] - gap * 0.05 + expertise + Math.min(0.05, toolRank * 0.01), 0.05, 0.88);
}

function getTimePhase(now) {
    const h = vnHour(now);
    // 1. Về đêm: 0h đến 5h sáng (0, 1, 2, 3, 4) -> x3 Máu, x2 Công, x2 Rơi Đồ, x2.5 EXP, x2 Linh Thạch
    if (h >= 0 && h < 5) {
        return {
            phase: 'late_night',
            name: 'Về Đêm',
            desc: 'Quái x3 Máu, x2 Công. Rơi đồ x2, EXP x2.5, Linh Thạch x2',
            hpMul: 3.0,
            atkMul: 2.0,
            dropMul: 2.0,
            expMul: 2.5,
            stoneMul: 2.0,
        };
    }
    // 2. Buổi trưa: 11h đến 14h (11, 12, 13) - Cùng thời gian với Boss Thế Giới trưa -> x2 Máu, x1.5 Công, x2 Rơi Đồ, x2 EXP, x2 Linh Thạch
    if (h >= 11 && h < 14) {
        return {
            phase: 'midday',
            name: 'Buổi Trưa',
            desc: 'Khung giờ Boss Thế Giới trưa: Quái x2 Máu, x1.5 Công. Rơi đồ x2, EXP x2, Linh Thạch x2',
            hpMul: 2.0,
            atkMul: 1.5,
            dropMul: 2.0,
            expMul: 2.0,
            stoneMul: 2.0,
        };
    }
    // 3. Buổi chiều / tối: 18h đến 24h (18, 19, 20, 21, 22, 23) - Cùng thời gian với Boss Thế Giới tối -> x2 Máu, x1.5 Công, x2 Rơi Đồ, x2 EXP, x2 Linh Thạch
    if (h >= 18) {
        return {
            phase: 'evening',
            name: 'Buổi Tối',
            desc: 'Khung giờ Boss Thế Giới tối: Quái x2 Máu, x1.5 Công. Rơi đồ x2, EXP x2, Linh Thạch x2',
            hpMul: 2.0,
            atkMul: 1.5,
            dropMul: 2.0,
            expMul: 2.0,
            stoneMul: 2.0,
        };
    }
    // 4. Ban ngày: Các khung giờ còn lại (5h-11h, 14h-18h) -> chuẩn 1.0x
    return {
        phase: 'day',
        name: 'Ban Ngày',
        desc: 'Chỉ số quái và phần thưởng tiêu chuẩn',
        hpMul: 1.0,
        atkMul: 1.0,
        dropMul: 1.0,
        expMul: 1.0,
        stoneMul: 1.0,
    };
}

function encounterPartyHpMul(monsterDef, partySize, now) {
    const actualPartyMul = C.RULES.partyHpMul[partySize - 1] || 1;
    const isTeamContent = !monsterDef?.isNpc
        && (monsterDef?.worldBoss || monsterDef?.isDungeon || !monsterDef?.small);
    if (!isTeamContent) return actualPartyMul;
    const targetPartySize = getTimePhase(now).phase === 'day'
        ? (C.RULES.bossDayTargetPartySize || 3)
        : (C.RULES.bossNightTargetPartySize || 5);
    const targetPartyMul = C.RULES.partyHpMul[Math.min(C.RULES.partyMax, targetPartySize) - 1] || 1;
    return Math.max(actualPartyMul, targetPartyMul);
}

function recommendedEncounterPartySize(now) {
    return getTimePhase(now).phase === 'day'
        ? (C.RULES.bossDayTargetPartySize || 3)
        : (C.RULES.bossNightTargetPartySize || 5);
}

function minimumEncounterPartySize(monsterDef, now) {
    // Bosses and Cổ Động remain open to solo attempts. Team sizes below are
    // recommendations used for encounter scaling and UI guidance, never gates.
    return 1;
}

function monsterRespawnMs(monsterDef, now) {
    if (monsterDef?.small) return SMALL_MONSTER_RESPAWN_MS;
    return 15 * 60 * 1000;
}

const newId = () => crypto.randomBytes(6).toString('base64url');
const clamp = (v, lo, hi) => Math.max(lo, Math.min(hi, v));
const SMALL_MONSTER_RESPAWN_MS = 20 * 1000;
const TOWN_HEAL_MIN_COST = 500;
const TOWN_HEAL_MAX_COST = 50_000;
const IMMORTAL_HEAL_MIN_COST = 100_000;
const IMMORTAL_HEAL_MAX_COST = 500_000;
const townHealingCost = (missingHp, maxHp, immortalTown = false) => {
    const healingRatio = maxHp > 0 ? clamp(missingHp / maxHp, 0, 1) : 0;
    const minCost = immortalTown ? IMMORTAL_HEAL_MIN_COST : TOWN_HEAL_MIN_COST;
    const maxCost = immortalTown ? IMMORTAL_HEAL_MAX_COST : TOWN_HEAL_MAX_COST;
    return Math.max(minCost, Math.min(maxCost, Math.ceil(maxCost * healingRatio)));
};
// DEF reduces damage according to the attack-to-defense ratio, so both stats scale together across realms.
const damageAfterDefense = (raw, def) => {
    const attack = Math.max(0, Number(raw) || 0);
    const defense = Math.max(0, Number(def) || 0);
    return Math.round(attack * (attack / Math.max(1, attack + 2 * defense)));
};
function skillCooldownUntil(cooldowns, skillId, now) {
    const current = Number(cooldowns?.[skillId]) || 0;
    const capped = Math.min(current, now + C.SKILL_RULES.maxCooldownSeconds * 1000);
    if (cooldowns && capped !== current) cooldowns[skillId] = capped;
    return capped;
}
const combatHitChance = (accuracy, dodge = 0) => clamp((Number(accuracy ?? 90) - Number(dodge || 0)) / 100, 0.1, 0.99);
function requiredMaForSkill(skill) {
    const explicit = Math.max(0, Number(skill?.reqMa) || 0);
    return explicit || (skill?.demonOnly ? Math.max(10, Number(skill.realm) <= 2 ? 10 : 30) : 0);
}
function requiredMaForEquipment(item) {
    const explicit = Math.max(0, Number(item?.reqMa) || 0);
    if (explicit) return explicit;
    if (!item?.demonOnly && item?.element !== 'ma') return 0;
    const realm = Number(item.realmMin ?? C.TIER[item.tier]?.realm) || 0;
    return Math.max(10, Math.ceil(realm / 2) * 10);
}
const canonicalMonsterName = name => String(name || 'Yêu thú')
    .replace(/(?:👑|⚡|🔥)?\s*\[(?:ma hóa|thôn phệ(?:\s*×\s*\d+)?|dị biến(?:\s+thôn phệ)?|yêu vương(?:\s+thôn thiên)?|yêu tướng|tiến hóa)\]\s*/giu, '')
    .replace(/\s+/gu, ' ')
    .trim();

const SUB_STAGES = ['Sơ kỳ', 'Trung kỳ', 'Hậu kỳ', 'Viên mãn', 'Đại viên mãn'];

function breakthroughMaterialId(toRealm) {
    if (toRealm <= 3) return 'mat_yeu_dan';
    if (toRealm <= 6) return 'mat_huyet_tinh';
    if (toRealm <= 11) return 'mat_long_lan';
    if (toRealm <= 19) return 'mat_tien_thach';
    if (toRealm <= 25) return 'mat_phap_tac_tinh';
    return 'mat_hon_don_tinh';
}
const SECT_MAX_LEVEL = 20;
const SECT_MAX_CONTRIBUTION = 5_000_000;
const SECT_DAILY_CONTRIBUTION_CAP = 10_000;
const SECT_DAILY_STONE_DONATION_CAP = 500;
const SECT_CONTRIBUTION_PER_STONE = 5;
const SECT_PROMOTION_REQUIREMENTS = [
    { role: 'noi_mon', name: 'Đệ tử nội môn', required: 2_000 },
    { role: 'dai_de_tu', name: 'Đại đệ tử', required: 10_000 },
    { role: 'elder', name: 'Trưởng lão', required: 30_000 },
    { role: 'dai_elder', name: 'Đại trưởng lão', required: 75_000 },
    { role: 'vice', name: 'Phó chưởng môn', required: 150_000 },
];
const SECT_ROLE_NAMES = {
    leader: 'Chưởng môn',
    vice: 'Phó chưởng môn',
    dai_elder: 'Đại trưởng lão',
    elder: 'Trưởng lão',
    dai_de_tu: 'Đại đệ tử',
    noi_mon: 'Đệ tử nội môn',
    ngoai_mon: 'Đệ tử ngoại môn',
    member: 'Đệ tử ngoại môn',
};
function sectNextPromotion(role) {
    const currentRole = role === 'member' || !role ? 'ngoai_mon' : role;
    if (currentRole === 'leader' || currentRole === 'vice') return null;
    const currentIndex = currentRole === 'ngoai_mon'
        ? -1
        : SECT_PROMOTION_REQUIREMENTS.findIndex(entry => entry.role === currentRole);
    return SECT_PROMOTION_REQUIREMENTS[currentIndex + 1] || null;
}
const sectStorageCapacity = level => 50 + Math.floor(Math.max(1, Number(level) || 1) / 5) * 20;
const sectLevelContribution = level => Math.max(1, Math.floor(Number(level) || 1)) * 25_000;
function sectDailyContribution(p, today) {
    if (p.sectContributionDate !== today) {
        p.sectContributionDate = today;
        p.sectContributionToday = 0;
    }
    if (p.sectLastDonateDate !== today) p.sectDonateToday = 0;
    const contributed = Math.max(0, Math.floor(Number(p.sectContributionToday) || 0));
    const donatedStones = Math.max(0, Math.floor(Number(p.sectDonateToday) || 0));
    return {
        contributed,
        remaining: Math.max(0, SECT_DAILY_CONTRIBUTION_CAP - contributed),
        donatedStones,
        remainingStones: Math.max(0, SECT_DAILY_STONE_DONATION_CAP - donatedStones),
    };
}
function sectLevelBenefits(sect) {
    const level = clamp(Math.floor(Number(sect?.level) || 1), 1, SECT_MAX_LEVEL);
    const basePct = level;
    const buff = sect?.buff || {};
    const pct = key => Math.round((basePct + (Number(buff[key]) || 0) * 100) * 10) / 10;
    return {
        atkPct: pct('atkPct'),
        defPct: pct('defPct'),
        hpPct: pct('hpPct'),
        spdPct: pct('spdPct'),
        sensePct: pct('sensePct'),
        storageCapacity: sectStorageCapacity(level),
        storageBonus: Math.floor(level / 5) * 20,
    };
}
function skillDamageProfile(skill) {
    const cap = C.SKILL_RULES.damageCap[skill?.rarity] || 3;
    const power = Math.max(0, Number(skill?.power) || 0);
    const dotTicks = Math.max(1, Math.min(5, Math.ceil((Number(skill?.dur) || 4) / 2)));
    const dotTotal = Math.min(cap, Math.max(0, Number(skill?.dot) || 0) * dotTicks);
    if (skill?.kind === 'multi') {
        const hits = Math.max(1, Math.floor(Number(skill.hits) || 1));
        const total = Math.min(cap, power * hits);
        return { cap, hits, total, perHit: total / hits, dotTicks, dotTotal, dotPerTick: dotTotal / dotTicks };
    }
    if (skill?.kind === 'dot') {
        const ticks = Math.max(1, Math.floor(Number(skill.dur) || 1));
        const total = Math.min(cap, Math.max(0, Number(skill.dot) || 0) * ticks);
        return { cap, ticks, total, perTick: total / ticks };
    }
    const total = Math.min(cap, power);
    return { cap, total, perHit: total, dotTicks, dotTotal, dotPerTick: dotTotal / dotTicks };
}
function skillMasteryDamageMultiplier(combatant, element = combatant?.element) {
    const elementMastery = clamp(Number(combatant?.elemMastery?.[element]) || 0, 0, 100);
    const roleMastery = clamp(Number(combatant?.roleStat) || 0, 0, 200) * 0.1;
    return (1 + elementMastery / 100) * (1 + roleMastery / 100);
}
function cappedSkillMasteryMultiplier(combatant, skillTotal, skillCap) {
    const mastery = skillMasteryDamageMultiplier(combatant);
    const total = Math.max(0, Number(skillTotal) || 0);
    const cap = Math.max(0, Number(skillCap) || 0);
    return total > 0 && cap > 0 ? Math.min(mastery, cap / total) : mastery;
}
function regenerateBattleMp(combatant, now) {
    const maxMp = Math.max(0, Number(combatant?.maxMp) || 0);
    const storedLastAt = Number(combatant?.mpRegenAt);
    const lastAt = Number.isFinite(storedLastAt) ? storedLastAt : now;
    const elapsedSeconds = Math.floor(Math.max(0, now - lastAt) / 1000);
    if (elapsedSeconds <= 0) return 0;
    combatant.mpRegenAt = lastAt + elapsedSeconds * 1000;
    const perSecond = Math.max(1, Math.ceil(maxMp * C.SKILL_RULES.battleMpRegenFractionPerSecond));
    const before = Math.max(0, Number(combatant.mp) || 0);
    combatant.mp = Math.min(maxMp, before + perSecond * elapsedSeconds);
    return combatant.mp - before;
}
function addSectContribution(sect, amount) {
    if (!sect) return { added: 0, leveledUp: false };
    sect.level = clamp(Math.floor(Number(sect.level) || 1), 1, SECT_MAX_LEVEL);
    const oldExp = Math.max(0, Math.floor(Number(sect.exp) || 0));
    const legacyTotal = 25_000 * ((sect.level - 1) * sect.level / 2) + oldExp;
    sect.totalContribution = clamp(Math.floor(Number(sect.totalContribution ?? legacyTotal) || 0), 0, SECT_MAX_CONTRIBUTION);
    const added = Math.min(Math.max(0, Math.floor(Number(amount) || 0)), SECT_MAX_CONTRIBUTION - sect.totalContribution);
    sect.totalContribution += added;
    sect.exp = oldExp + added;
    let leveledUp = false;
    while (sect.level < SECT_MAX_LEVEL && sect.exp >= sectLevelContribution(sect.level)) {
        sect.exp -= sectLevelContribution(sect.level);
        sect.level += 1;
        leveledUp = true;
    }
    if (sect.level >= SECT_MAX_LEVEL) sect.exp = 0;
    return { added, leveledUp };
}
const THIEN_KIEU_USERS = new Set(['1354709393', '5811879139']);
const SCROLL_PRICE = { pt: 20, hiem: 200, cuchiem: 2000, tt: 10000, cam: 25000, vang: 50000, docban: 1000000 };
const SLOT_NAMES = { weapon: 'Vũ khí', armor: 'Giáp', acc1: 'Trang sức 1', acc2: 'Trang sức 2', ring1: 'Nhẫn / Vòng 1', ring2: 'Nhẫn / Vòng 2', phiKiem: 'Phi kiếm / Tọa kỵ', loDinh: 'Lô đỉnh', nhanTruDo: 'Nhẫn Trữ Đồ', nhanNaDi: 'Nhẫn Dịch Chuyển' };

const LOOK_STYLE_KEYS = { preset: 10, bo: 4, fa: 4, ea: 3, ey: 8, br: 5, no: 4, mo: 5, bd: 5, ha: 10, ti: 4, to: 6, tot: 6, pa: 4, sh: 3, be: 3, hat: 6, wp: 11, au: 6 };
const LOOK_COLOR_KEYS = ['sk', 'hc', 'ec', 'tc', 'oc', 'pc', 'sc', 'bc', 'hac', 'ac', 'auc'];   // 'wc' (weapon colour) only comes from equipment, see wornLook

/** Validates the layered-avatar look string ("g=m;fa=0;hc=#1e1a1e;..."). Returns { text, values } or null. */
function sanitizeLook(text, gender) {
    if (typeof text !== 'string' || !text || text.length > 400) return null;
    const values = {};
    for (const part of text.split(';')) {
        const i = part.indexOf('=');
        if (i <= 0) continue;
        const key = part.slice(0, i).trim();
        const value = part.slice(i + 1).trim();
        if (Object.prototype.hasOwnProperty.call(LOOK_STYLE_KEYS, key)) {
            const n = Number(value);
            if (Number.isInteger(n) && n >= 0 && n < LOOK_STYLE_KEYS[key]) values[key] = n;
        } else if (LOOK_COLOR_KEYS.includes(key)) {
            if (/^#[0-9a-fA-F]{6}$/.test(value)) values[key] = value.toLowerCase();
        }
    }
    values.g = gender === 'nu' ? 'f' : 'm';
    if (values.g === 'f') values.bd = 0;
    const order = ['g', ...Object.keys(LOOK_STYLE_KEYS), ...LOOK_COLOR_KEYS].filter(k => values[k] !== undefined);
    return { text: order.map(k => `${k}=${values[k]}`).join(';'), values };
}

class GameError extends Error {}
const fail = message => { throw new GameError(message); };

function pickWeighted(list, weightOf, rng) {
    const total = list.reduce((sum, item) => sum + weightOf(item), 0);
    let roll = rng() * total;
    for (const item of list) {
        roll -= weightOf(item);
        if (roll < 0) return item;
    }
    return list[list.length - 1];
}

function shuffle(list, rng) {
    const out = list.slice();
    for (let i = out.length - 1; i > 0; i -= 1) {
        const j = Math.floor(rng() * (i + 1));
        [out[i], out[j]] = [out[j], out[i]];
    }
    return out;
}

// Chỉ số yêu thú sau hệ số độ khó chung (RULES.monster*Mul).
// Chỉ số gốc theo đường tăng trưởng của người chơi cùng cảnh giới; số trong
// MONSTERS chỉ quyết định "tính cách" (trâu, cứng, hung) của từng con.
function monsterStats(def, now = null) {
    const env = k => Number(process.env[`TUTIEN_${k}`]) || null;
    const realm = Math.max(0, Number(def.realm) || 0);
    const tp = now != null ? getTimePhase(now) : { hpMul: 1, atkMul: 1 };
    const hpCap = def.small ? 50_000_000 : MAX_BOSS_HP;

    // PHÀM GIỚI (Cảnh giới 0 -> 10): Giữ trọn vẹn 100% công thức cũ cho người chơi phàm giới
    if (realm <= 10) {
        const g = Math.pow(C.REALM_GROWTH, realm);
        const flavor = (value, base, norm, lo, hi) => clamp(value / (base * g) / norm, lo, hi);
        let fh = flavor(def.hp, C.BASE_STATS.hp, 0.85, 0.75, 1.3);
        let fa = flavor(def.atk, C.BASE_STATS.atk, 0.8, 0.8, 1.25);
        let fd = flavor(def.def, C.BASE_STATS.def, 0.8, 0.7, 1.8);
        // Giữ độ khó tổng thể trong một khoảng: con trâu thì yếu công, con hung thì mỏng.
        const total = fh * fa * Math.sqrt(fd);
        const fix = total > 1.25 ? Math.cbrt(1.25 / total) : (total < 0.85 ? Math.cbrt(0.85 / total) : 1);
        fh *= fix; fa *= fix; fd *= fix;
        const sm = def.small
            ? { hp: Number(process.env.TUTIEN_SHP) || C.RULES.smallHpMul, atk: Number(process.env.TUTIEN_SATK) || C.RULES.smallAtkMul, def: C.RULES.smallDefMul }
            : def.worldBoss
                ? { hp: C.RULES.worldBossHpMul, atk: C.RULES.worldBossAtkMul, def: C.RULES.worldBossDefMul }
                : { hp: Number(process.env.TUTIEN_BHP) || C.RULES.bossHpMul, atk: Number(process.env.TUTIEN_BATK) || C.RULES.bossAtkMul, def: 1 };
        const realmHpScale = 1 + realm * 0.28;
        const realmAtkScale = 1 + realm * 0.20;
        return {
            hp: Math.min(hpCap, Math.round(C.BASE_STATS.hp * g * (env('HPMUL') || C.RULES.monsterHpMul) * fh * sm.hp * realmHpScale * (tp.hpMul || 1) * 1.10)),
            atk: Math.round(C.BASE_STATS.atk * g * (env('ATKMUL') || C.RULES.monsterAtkMul) * fa * sm.atk * realmAtkScale * (tp.atkMul || 1) * 1.10),
            def: Math.round(C.BASE_STATS.def * g * (env('DEFMUL') || C.RULES.monsterDefMul) * fd * sm.def * 1.10),
        };
    }

    // TIÊN GIỚI (Cảnh giới 11+): Tiên Phàm cách biệt rõ rệt, quái Tiên Giới có thể phách cao hơn phàm giới nhưng vừa phải để solo vẫn có tỉ lệ thắng
    const tienProgress = realm - 11;
    const gTien = Math.pow(C.REALM_GROWTH, tienProgress);
    let baseHp, baseAtk, baseDef;
    if (def.small) {
        // Tiểu yêu Tiên Giới (Bán Tiên realm 11): 40k HP, 1.250 ATK, 850 DEF (không 1-hit chết người chơi)
        baseHp = 40_000;
        baseAtk = 1_250;
        baseDef = 850;
    } else if (def.worldBoss) {
        // Đại Boss Thế Giới / Trấn Giới Cự Thú: 500k HP, 3.200 ATK, 2.000 DEF (cần tổ đội lớn 4-5 người)
        baseHp = 500_000;
        baseAtk = 3_200;
        baseDef = 2_000;
    } else {
        // Đại Yêu / Thủ Hộ Tiên Giới: 125k HP, 2.000 ATK, 1.250 DEF (solo có ~25% tỉ lệ thắng khi đồ cơ bản, 100% khi thần trang)
        baseHp = 125_000;
        baseAtk = 2_000;
        baseDef = 1_250;
    }
    // Tiên Giới: giữ tiểu yêu ở mức tăng nhẹ ban đêm; boss tăng rõ hơn để
    // Cổ Động và các trận boss vẫn khuyến khích phối hợp tổ đội.
    const tienHpMul = Math.min(def.small ? 1.20 : C.RULES.tienBossNightHpMul, tp.hpMul || 1);
    const tienAtkMul = Math.min(def.small ? 1.12 : C.RULES.tienBossNightAtkMul, tp.atkMul || 1);
    const hp = Math.min(hpCap, Math.round(baseHp * gTien * tienHpMul));
    const atk = Math.round(baseAtk * gTien * tienAtkMul * (def.small ? 1 : 1.1));
    const defVal = Math.round(baseDef * gTien);
    return { hp, atk, def: defVal };
}

// Tăng khả năng sống sót và gây sát thương của người chơi trong PvE mà không
// làm thay đổi cân bằng Đấu Pháp giữa người chơi với nhau.
function boostPvePlayerStats(stats) {
    return {
        ...stats,
        hp: Math.round(stats.hp * 1.6),
        atk: Math.round(stats.atk * 1.5),
        def: Math.round(stats.def * 1.5),
        power: Math.round((stats.power || 0) * 1.5),
    };
}

function itemDef(item) {
    if (!item) return null;
    if (item.kind === 'equip') return C.EQUIP_BY_ID.get(item.id);
    if (item.kind === 'cons') {
        const id = (item.id === 'dan_truc_co') ? 'truc_co_dan' : item.id;
        return C.CONSUMABLE_BY_ID.get(id) || C.CONSUMABLE_BY_ID.get(item.id);
    }
    if (item.kind === 'scroll') return C.SKILL_BY_ID.get(item.id);
    if (item.kind === 'mat') return C.MATERIAL_BY_ID?.get(item.id) || C.FIRE_BY_ID?.get(item.id) || C.BEAST_GOURD_BY_ID?.get(item.id);
    if (item.kind === 'tool') return C.BEAST_GOURD_BY_ID?.get(item.id);
    return null;
}

function itemName(item) {
    const def = itemDef(item);
    if (!def) return item?.id || '?';
    return item.kind === 'scroll' ? `Ngọc giản: ${def.name}` : def.name;
}

// What the figure wears follows the equipment: the weapon in hand is the equipped weapon type in its
// quality colour, armour replaces the outer robe. Style ids match the client's layered-avatar parts.
const WORN_WEAPON_STYLE = { kiem: 4, trongkhi: 5, phapkhi: 6, bua: 7, but: 8, quyensao: 9, dinh: 10 };
const WORN_TIER_COLOR = { pham: '#a9a391', hoang: '#7fd08a', huyen: '#64b5f0', dia: '#b69cff', thien: '#f0a24e', tien: '#ff6a5c' };
const WORN_ARMOR_COLORS = {
    pham: ['#8a8474', '#b8b0a0'], hoang: ['#3f6f4a', '#c8a050'], huyen: ['#2f5f8a', '#c8d8e8'],
    dia: ['#5a4a8a', '#d8c8f0'], thien: ['#8a5a2a', '#f0d080'], tien: ['#7a2a3a', '#f0d080'],
};

function wornLook(p) {
    if (!p || typeof p.look !== 'string' || !p.look) return p?.look || null;
    const values = {};
    const order = [];
    for (const part of p.look.split(';')) {
        const i = part.indexOf('=');
        if (i <= 0) continue;
        const key = part.slice(0, i).trim();
        if (!(key in values)) order.push(key);
        values[key] = part.slice(i + 1).trim();
    }
    const set = (key, value) => { if (!(key in values)) order.push(key); values[key] = String(value); };
    const equipped = slot => {
        const uid = p.equip?.[slot];
        const item = uid ? (p.items || []).find(it => it.uid === uid && it.place === 'equip') : null;
        return item ? itemDef(item) : null;
    };
    const weapon = equipped('weapon');
    if (weapon && WORN_WEAPON_STYLE[weapon.wtype]) {
        set('wp', WORN_WEAPON_STYLE[weapon.wtype]);
        set('wc', WORN_TIER_COLOR[weapon.tier] || WORN_TIER_COLOR.pham);
    }
    const armor = equipped('armor');
    if (armor) {
        const name = String(armor.name || '');
        const colors = WORN_ARMOR_COLORS[armor.tier] || WORN_ARMOR_COLORS.pham;
        let style = null;
        if (/Giáp|Khải|Thuẫn|Thần Tướng/.test(name)) style = 5;
        else if (/Bào/.test(name)) style = 3;
        else if (/ Y$| Y |Động Y|Chiến Y|Kiếm Y|Tráo/.test(name)) style = ['thien', 'tien'].includes(armor.tier) ? 4 : 1;
        if (style) set('to', style);
        // mortal-grade clothes keep the colours chosen in the creator; better armour shows its quality
        if (armor.tier && armor.tier !== 'pham' && (style || Number(values.to) > 0)) { set('oc', colors[0]); set('ac', colors[1]); }
    }
    return order.map(key => `${key}=${values[key]}`).join(';');
}

// ---------------------------------------------------------------------------
// Trận đấu thời gian thực
// ---------------------------------------------------------------------------

class Battle {
    // players: 1 người (solo) hoặc 2–5 người (tổ đội đánh đại yêu).
    constructor(game, players, monsterDef, now) {
        this.game = game;
        this.rng = game.rng;
        this.id = newId();
        this.kind = monsterDef.small ? 'small' : 'boss';
        this.party = players.length > 1;
        this.startAt = now;
        this.endsAt = now + C.RULES.battleMaxMs;
        this.lastActionAt = now;
        this.over = false;
        this.result = null;
        this.log = [];
        this.night = isNight(now);
        this.monsterDef = monsterDef;

        this.tier = monsterDef.tier || (monsterDef.worldBoss || (!monsterDef.small && monsterDef.isBoss) ? 'boss' : (monsterDef.small ? 'normal' : 'boss'));
        const partyMul = encounterPartyHpMul(monsterDef, players.length, now);
        const ms = monsterStats(monsterDef, now);
        const isWorldMonsterInstance = Boolean(monsterDef.uid && monsterDef.monsterId);
        // NPC duels pass fully calculated stats (including their power-based
        // attack scale). Do not feed them through the generic monster/boss
        // multipliers a second time, or a manual NPC fight becomes far harder
        // than its quick-simulation counterpart.
        const hasExplicitStats = isWorldMonsterInstance || Boolean(monsterDef.isNpc) || Boolean(monsterDef.explicitStats);
        const baseHp = hasExplicitStats && monsterDef.hp != null ? monsterDef.hp : ms.hp;
        const baseMaxHp = hasExplicitStats && monsterDef.maxHp != null ? monsterDef.maxHp : (hasExplicitStats && monsterDef.hp != null ? monsterDef.hp : ms.hp);
        const baseAtk = hasExplicitStats && monsterDef.atk != null ? monsterDef.atk : ms.atk;
        const baseDef = hasExplicitStats && monsterDef.def != null ? monsterDef.def : ms.def;
        const capsBossHp = !monsterDef.small && !monsterDef.isNpc;
        const scalesWithEncounterSize = capsBossHp || Boolean(monsterDef.isDungeon);
        const encounterHpCap = monsterDef.small ? 50_000_000 : MAX_BOSS_HP;
        const effectiveMaxHp = scalesWithEncounterSize
            ? Math.min(encounterHpCap, Math.round(baseMaxHp * partyMul))
            : Math.round(baseMaxHp * partyMul);
        const effectiveHp = scalesWithEncounterSize
            ? Math.min(effectiveMaxHp, Math.round(baseHp * partyMul))
            : Math.round(baseHp * partyMul);

        this.members = new Map();
        for (const player of players) {
            const baseStats = game.stats(player, now);
            const st = boostPvePlayerStats(baseStats);
            const hpRatio = player.hp != null ? clamp(player.hp / Math.max(1, baseStats.hp), 0, 1) : 1;
            const initialHp = Math.min(st.hp, Math.max(1, Math.round(st.hp * hpRatio)));
            this.members.set(String(player.userId), {
                userId: String(player.userId), name: player.name, element: player.he, elemMastery: { ...(player.elemMastery || {}) },
                roleStat: Number(player.roleStats?.[player.mon]) || 0, realmIndex: st.realmIndex,
                armorK: st.realmIndex >= 11 ? Math.round(2000 * Math.pow(C.REALM_GROWTH, st.realmIndex - 11)) : Math.round(100 * Math.pow(C.REALM_GROWTH, st.realmIndex)),
                hp: initialHp, maxHp: st.hp, baseMaxHp: st.hp, mp: st.mp, maxMp: st.mp, mpRegenAt: now, atk: st.atk, def: st.def, spd: st.spd, crit: st.crit, critDmg: st.critDmg || 150, accuracy: st.accuracy, dodge: st.dodge, combatPower: st.power,
                shield: 0, buffs: {}, dots: [], stunUntil: 0, bindUntil: 0, stunImmuneUntil: 0, immuneUntil: 0,
                dodgeUntil: 0, dodgeReadyAt: 0, atkReadyAt: now, reflect: null,
                nextCrit: false, slowUntil: 0, atkDebuffUntil: 0, dmgTakenUntil: 0,
                lastActionAt: now, actionTimes: [], out: null, final: null, summary: null, dealt: 0,
            });
        }
        this.userIds = [...this.members.keys()];
        this.armorK = monsterDef.realm >= 11 ? Math.round(2000 * Math.pow(C.REALM_GROWTH, monsterDef.realm - 11)) : Math.round(100 * Math.pow(C.REALM_GROWTH, monsterDef.realm));
        // World NPC duels are one-on-one encounters, not a boss plus four adds.
        this.isBoss = !monsterDef.small && !monsterDef.isNpc;
        this.packSize = this.isBoss ? 5 : 1;
        if (this.isBoss) {
            this.minions = [
                { name: 'Tiểu Yêu Hộ Vệ 1', hp: Math.round(baseHp * 0.15), maxHp: Math.round(baseHp * 0.15), atk: Math.round(baseAtk * 0.15) },
                { name: 'Tiểu Yêu Hộ Vệ 2', hp: Math.round(baseHp * 0.15), maxHp: Math.round(baseHp * 0.15), atk: Math.round(baseAtk * 0.15) },
                { name: 'Tiểu Yêu Hộ Vệ 3', hp: Math.round(baseHp * 0.15), maxHp: Math.round(baseHp * 0.15), atk: Math.round(baseAtk * 0.15) },
                { name: 'Tiểu Yêu Hộ Vệ 4', hp: Math.round(baseHp * 0.15), maxHp: Math.round(baseHp * 0.15), atk: Math.round(baseAtk * 0.15) },
            ];
        } else {
            this.minions = [];
        }
        const soloAttackMul = players.length !== 1 || monsterDef.isNpc
            ? 1
            : monsterDef.worldBoss
                ? (C.RULES.soloWorldBossAtkMul ?? 1)
                : (monsterDef.isDungeon
                    ? (C.RULES.soloDungeonAtkMul || C.RULES.soloBossAtkMul || 1)
                    : (!monsterDef.small ? (C.RULES.soloBossAtkMul || 1) : 1));
        this.m = {
            name: monsterDef.name,
            element: monsterDef.element, realm: monsterDef.realm,
            icon: monsterDef.icon || '👹',
            hp: effectiveHp, maxHp: effectiveMaxHp,
            atk: Math.round(baseAtk * soloAttackMul), def: Math.round(baseDef),
            spd: Number(monsterDef.spd) || 10, crit: monsterDef.trait === 'crit' ? 0.25 : 0.05,
            accuracy: monsterDef.accuracy ?? clamp(90 + 8 * (monsterDef.realm || 0) / ((monsterDef.realm || 0) + 10), 90, 98),
            dodge: clamp(((monsterDef.spd || 10) / ((monsterDef.spd || 10) + 300)) * 40, 2, 45),
            dots: [], stunUntil: 0, bindUntil: 0, slowUntil: 0, atkDebuffUntil: 0,
            attackCount: 0, nextAttackAt: now + 2000, lastAttackAt: 0, pendingBig: false,
            ...(monsterDef.isNpc ? {
                mp: Math.max(0, Number(monsterDef.mp) || C.BASE_STATS.mp),
                maxMp: Math.max(0, Number(monsterDef.maxMp) || Number(monsterDef.mp) || C.BASE_STATS.mp),
                mpRegenAt: now,
                shield: 0,
                npcSkillCds: {},
                npcAtkMul: 1,
                npcDmgTakenMul: 1,
                npcCritBonus: 0,
            } : {}),
        };
        const packNotice = this.isBoss ? ' · bầy 5 quái (1 Boss + 4 Tiểu Yêu)' : '';
        this.say(`Gặp ${this.m.name} · ${game.realmName(monsterDef.realm)} · hệ ${C.HE[monsterDef.element].name}${packNotice}${this.party ? ` · tổ đội ${players.length} người` : ''}.`, now);

        this.isTiengioiMonster = Boolean((Number(monsterDef.realm) || 0) >= 11 && !monsterDef.isNpc);
        if (this.isTiengioiMonster) {
            if (!this.party) {
                this.say(`⚠️ [TIÊN UY ÁP CHẾ] Khí tức Tiên Giới áp đảo! Tu sĩ đơn độc chịu Tiên Uy (+15% sát thương gánh chịu, giảm 15% sát thương gây ra). Khuyên bạn nên lập tổ đội để hợp kích an toàn!`, now);
            } else {
                this.say(`✨ [TIÊN TRẬN LIÊN THỦ] Tổ đội ${players.length} tu sĩ kích hoạt trận pháp liên thủ, hóa giải hoàn toàn Tiên Uy Áp Chế và nhận thêm uy lực hợp kích!`, now);
            }
        }
    }

    // Tương thích: trận solo dùng battle.p / battle.userId như trước.
    get userId() { return this.userIds[0]; }
    get p() { return this.members.get(this.userIds[0]); }
    member(userId) { return this.members.get(String(userId)) || null; }
    fighting() { return [...this.members.values()].filter(p => !p.out); }

    say(text, now) {
        this.log.push({ t: now, text });
        if (this.log.length > 40) this.log.splice(0, this.log.length - 40);
    }

    elementMultiplier(attElement, defElement, forceCounter) {
        if (!attElement || !defElement) return { mul: 1, counter: false, countered: false };
        if (forceCounter || C.HE[attElement].beats === defElement) return { mul: 1.25, counter: true, countered: false };
        if (C.HE[defElement].beats === attElement) return { mul: 0.8, counter: false, countered: true };
        return { mul: 1, counter: false, countered: false };
    }

    dayNightMul(element) {
        if (!element) return 1;
        if (!this.night && C.DAY_ELEMENTS.includes(element)) return 1.1;
        if (this.night && C.NIGHT_ELEMENTS.includes(element)) return 1.1;
        return 1;
    }

    // Người chơi p đánh yêu thú. coef: hệ số chiêu; isSkill: kỹ năng mang hệ.
    playerHit(p, coef, now, opts = {}) {
        const m = this.m;
        const realmDiff = (Number(this.monsterDef.realm) || 0) - (Number(p.realmIndex) || 0);
        const monsterDodge = now < (m.bindUntil || 0) ? 0 : m.dodge;
        // Uy áp cảnh giới: quái vật cao cấp hơn né đòn dễ hơn và người chơi khó trúng hơn nhiều
        const realmDodgeBonus = realmDiff > 0 ? Math.min(20, realmDiff * (this.party ? 2.5 : 4.0)) : 0;
        const realmAccuracyPenalty = realmDiff > 0 ? Math.min(20, realmDiff * (this.party ? 1.5 : 3.0)) : 0;
        const effectiveDodge = Math.min(75, monsterDodge + realmDodgeBonus);
        const effectiveAccuracy = Math.max(25, p.accuracy - realmAccuracyPenalty);
        if (!opts.unavoidable && this.rng() >= combatHitChance(effectiveAccuracy, effectiveDodge)) {
            return { dmg: 0, crit: false, counter: false, hit: false };
        }
        if (opts.controlOnly) return { dmg: 0, crit: false, counter: false, hit: true };
        let atk = p.atk;
        if (p.buffs.atk && p.buffs.atk.until > now) atk *= 1 + p.buffs.atk.value;
        if (p.buffs.rage && p.buffs.rage.until > now) atk *= 1 + (1 - p.hp / p.maxHp);
        if (p.atkDebuffUntil > now) atk *= 0.9;
        let pierce = opts.pierce || 0;
        if (opts.isSkill && p.element === 'kim') pierce += 0.15;
        const def = m.def * (1 - clamp(pierce, 0, 0.9));
        let dmg = atk * coef * this.armorK / (this.armorK + def);
        let counter = false;
        if (opts.isSkill) {
            const forceCounter = p.buffs.forceCounter && p.buffs.forceCounter.until > now;
            const el = this.elementMultiplier(p.element, m.element, forceCounter);
            dmg *= el.mul * this.dayNightMul(p.element) * cappedSkillMasteryMultiplier(p, opts.skillTotal, opts.skillCap);
            counter = el.counter;
            const root = this.game.linhCan(this.game.player(p.userId));
            if (root?.elemBonus) dmg *= 1 + root.elemBonus;
        } else if (this.monsterDef.trait === 'armor') {
            dmg *= 0.7;
        }
        // Kỹ năng phòng thủ của NPC giảm sát thương họ nhận, không làm yếu
        // đòn đánh của NPC lên người chơi.
        if (this.monsterDef.isNpc && (m.npcAtkBuffUntil || 0) > now) {
            dmg *= clamp(Number(m.npcDmgTakenMul) || 1, 0.1, 1);
        }
        // Uy áp cảnh giới: đối thủ cao hơn cảnh giới sẽ áp chế mạnh sát thương người chơi
        // Đi solo áp chế cực kỳ nặng, rất khó solo thắng
        // Uy áp cảnh giới: đòn đánh bị áp chế cực nặng — solo gần như không gây sát thương
        if (realmDiff > 0) {
            const baseRatio = this.party ? 0.30 : 0.12;
            const realmSuppression = Math.max(0.005, Math.pow(baseRatio, realmDiff));
            dmg *= realmSuppression;
        }
        if (this.isTiengioiMonster) {
            const isTeamEncounter = !this.monsterDef.isNpc
                && (this.monsterDef.worldBoss || this.monsterDef.isDungeon || !this.monsterDef.small);
            if (isTeamEncounter) {
                if (!this.party) {
                    dmg *= C.RULES.soloBossDamageMul || 0.55;
                } else {
                    dmg *= 1 + Math.min(4, this.members.size - 1) * (C.RULES.teamBossDamagePerMember || 0.08);
                }
            } else if (!this.party) {
                dmg *= 0.85; // Tiên Uy hạn chế sát thương solo khi săn tiểu yêu.
            } else {
                dmg *= (1 + (this.members.size - 1) * 0.05);
            }
        } else if (!this.monsterDef.isNpc
            && (this.monsterDef.worldBoss || this.monsterDef.isDungeon || !this.monsterDef.small)) {
            if (!this.party) {
                dmg *= C.RULES.soloBossDamageMul || 0.55;
            } else {
                dmg *= 1 + Math.min(4, this.members.size - 1) * (C.RULES.teamBossDamagePerMember || 0.08);
            }
        }
        let critChance = p.crit + (opts.critBonus || 0);
        if (p.buffs.crit && p.buffs.crit.until > now) critChance += p.buffs.crit.value;
        if (opts.isSkill && p.element === 'loi') critChance += 0.1;
        if (realmDiff > 0) critChance = Math.max(0, critChance - realmDiff * 0.25);
        const crit = p.nextCrit || this.rng() < clamp(critChance, 0, 0.9);
        if (crit) {
            const critMul = Math.min(3.5, Math.max(1.5, Number(p.critDmg || 150) / 100));
            dmg *= critMul;
            p.nextCrit = false;
        }
        dmg *= 0.9 + this.rng() * 0.2;
        dmg = Math.max(1, Math.round(dmg));
        const shieldAbsorbed = this.monsterDef.isNpc && m.shield > 0 ? Math.min(m.shield, dmg) : 0;
        if (shieldAbsorbed) m.shield -= shieldAbsorbed;
        const dealt = Math.max(0, dmg - shieldAbsorbed);
        m.hp = Math.max(0, m.hp - dealt);
        p.dealt += dealt;
        return { dmg: dealt, shieldAbsorbed, crit, counter, hit: true };
    }

    applyElementEffect(p, now, counter, doubled) {
        const m = this.m;
        const times = doubled ? 2 : 1;
        switch (p.element) {
            case 'moc': p.hp = Math.min(p.maxHp, p.hp + Math.round(p.maxHp * 0.05 * times)); break;
            case 'thuy': m.atkDebuffUntil = now + 6000; break;
            case 'hoa': m.dots.push({ perSec: Math.max(1, Math.round(Math.min(m.maxHp * 0.02, p.atk * 0.5) * times)), until: now + 5000, nextAt: now + 1000, name: 'Đốt', by: p.userId }); break;
            case 'tho': p.dmgTakenUntil = now + 6000; break;
            case 'loi': if (this.rng() < 0.1 * (counter ? 2 : 1) * times) this.stunMonster(p, 2, now, 'Tê liệt'); break;
            case 'phong': if (this.rng() < 0.15 * (counter ? 2 : 1) * times) {
                const extra = this.playerHit(p, 0.6, now, { isSkill: false });
                this.say(`${this.party ? `${p.name} · ` : ''}Phong: đánh thêm ${extra.dmg}.`, now);
            } break;
            case 'bang': m.slowUntil = now + 5000; break;
            default: break;
        }
    }

    stunMonster(p, seconds, now, label) {
        const m = this.m;
        const realmDiff = (Number(this.monsterDef.realm) || 0) - (Number(p.realmIndex) || 0);
        if (realmDiff >= 2) {
            this.say(`⚡ Uy áp cảnh giới: ${m.name} miễn nhiễm toàn bộ choáng từ tu sĩ cảnh giới thấp!`, now);
            return;
        }
        if (realmDiff === 1) {
            if (this.rng() < 0.5) {
                this.say(`⚡ Uy áp cảnh giới: ${m.name} hóa giải đòn choáng của bạn!`, now);
                return;
            }
            seconds *= 0.5;
        }
        let ms = seconds * 1000;
        if (C.HE[m.element]?.beats === p.element) ms /= 2; // bị khắc: choáng giảm một nửa
        m.stunUntil = Math.max(m.stunUntil, now + ms);
        m.nextAttackAt = Math.max(m.nextAttackAt, m.stunUntil + 300);
        this.say(`${label}: ${m.name} bị choáng ${(ms / 1000).toFixed(1)} giây.`, now);
    }

    bindMonster(p, seconds, now, label) {
        const m = this.m;
        const realmDiff = (Number(this.monsterDef.realm) || 0) - (Number(p.realmIndex) || 0);
        if (realmDiff >= 2) {
            this.say(`⚡ Uy áp cảnh giới: ${m.name} miễn nhiễm toàn bộ trói buộc từ tu sĩ cảnh giới thấp!`, now);
            return;
        }
        if (realmDiff === 1) {
            if (this.rng() < 0.5) {
                this.say(`⚡ Uy áp cảnh giới: ${m.name} hóa giải trói buộc của bạn!`, now);
                return;
            }
            seconds *= 0.5;
        }
        m.bindUntil = Math.max(m.bindUntil || 0, now + seconds * 1000);
        m.nextAttackAt = Math.max(m.nextAttackAt, m.bindUntil + 300);
        this.say(`${label}: ${m.name} bị trói trong ${seconds.toFixed(1)} giây, không thể né đòn.`, now);
    }

    stunPlayer(p, seconds, now) {
        if (now < p.immuneUntil || now < p.stunImmuneUntil) { this.say(`${this.party ? `${p.name}: ` : ''}Miễn khống chế, không bị choáng.`, now); return; }
        let ms = seconds * 1000;
        if (C.HE[p.element]?.beats === this.m.element) ms /= 2;
        const oldUntil = Math.max(p.stunUntil || 0, p.bindUntil || 0, now);
        p.stunUntil = Math.max(p.stunUntil || 0, now + ms);
        this.freezePlayerCooldowns(p, now, Math.max(0, p.stunUntil - oldUntil));
        p.stunImmuneUntil = p.stunUntil + C.RULES.stunImmuneMs;
        this.say(`${this.party ? p.name : 'Bạn'} bị choáng ${(ms / 1000).toFixed(1)} giây! Chỉ kỹ năng giải khống mới thoát được.`, now);
    }

    bindPlayer(p, seconds, now) {
        if (now < p.immuneUntil || now < p.stunImmuneUntil) { this.say(`${this.party ? `${p.name}: ` : ''}Miễn khống chế, không bị trói.`, now); return; }
        const oldUntil = Math.max(p.bindUntil || 0, now);
        p.bindUntil = Math.max(p.bindUntil || 0, now + seconds * 1000);
        this.freezePlayerCooldowns(p, now, Math.max(0, p.bindUntil - oldUntil));
        this.say(`${this.party ? p.name : 'Bạn'} bị trói trong ${seconds.toFixed(1)} giây, không thể né đòn.`, now);
    }

    freezePlayerCooldowns(p, now, durationMs) {
        if (durationMs <= 0) return;
        for (const key of ['atkReadyAt', 'dodgeReadyAt']) {
            if ((p[key] || 0) > now) p[key] += durationMs;
        }
        const player = this.game.player(p.userId);
        if (player?.cd) {
            for (const key of Object.keys(player.cd)) {
                if (player.cd[key] > now) player.cd[key] += durationMs;
            }
        }
    }

    // Trả về sát thương thật nhận vào (0 nếu né).
    hurt(p, raw, now) {
        if (now <= p.dodgeUntil) return { dodged: true, taken: 0, absorbed: 0 };
        const playerDodge = now < (p.bindUntil || 0) ? 0 : p.dodge;
        if (this.rng() >= combatHitChance(this.m.accuracy, playerDodge)) return { dodged: true, taken: 0, absorbed: 0 };
        let dmg = raw;
        if (p.buffs.dmgTaken && p.buffs.dmgTaken.until > now) dmg *= p.buffs.dmgTaken.value;
        if (p.dmgTakenUntil > now) dmg *= 0.9;
        if (this.isTiengioiMonster && !this.party) {
            dmg *= 1.15; // Quái Tiên Giới tăng 15% sát thương lên người chơi solo (thay vì 50%)
        }
        const realmDiff = (Number(this.monsterDef.realm) || 0) - (Number(p.realmIndex) || 0);
        if (this.isBoss && Number(this.monsterDef.realm) > 30) {
            // Boss cảnh giới cao đe dọa cá nhân rõ hơn; tổ đội phải quản lý từng người.
            const maxHitFraction = this.party
                ? (this.m.pendingBig ? 0.42 : 0.28)
                : (this.m.pendingBig ? 0.55 : 0.38);
            dmg = Math.min(dmg, Math.max(1, Math.ceil(p.maxHp * maxHitFraction)));
        } else if (realmDiff > 0) {
            // Khi quái vật/NPC cao cảnh giới hơn người chơi:
            // Uy áp cảnh giới hủy diệt, không được bảo vệ bởi trần sát thương thấp!
            if (realmDiff >= 2) {
                // Chênh lệch cảnh giới cao gây sát thương lớn, nhưng boss vẫn không kết liễu trong một đòn.
                const maxHitFraction = this.party ? (this.m.pendingBig ? 0.65 : 0.50) : (this.m.pendingBig ? 0.80 : 0.65);
                dmg = Math.min(dmg, Math.max(1, Math.ceil(p.maxHp * maxHitFraction)));
            } else {
                // Chênh lệch một cảnh giới vẫn rất nguy hiểm, nhưng không kết liễu ngay.
                const maxHitFraction = this.party ? (this.m.pendingBig ? 0.55 : 0.38) : (this.m.pendingBig ? 0.70 : 0.55);
                dmg = Math.min(dmg, Math.max(1, Math.ceil(p.maxHp * maxHitFraction)));
            }
        } else {
            if (this.isTiengioiMonster) {
                // Quái Tiên Giới ngang hoặc dưới cảnh giới: TUYỆT ĐỐI KHÔNG 1 HIT CHẾT NGƯỜI CHƠI!
                // Giới hạn sát thương tối đa theo % HP để người chơi solo vẫn có cơ hội sống sót, né chiêu, dùng thuốc và win
                let maxHitFraction;
                if (this.monsterDef.small) {
                    maxHitFraction = this.party ? (this.m.pendingBig ? 0.20 : 0.14) : (this.m.pendingBig ? 0.24 : 0.18); // Tiểu yêu: tối đa 14% (party) / 18% (solo) đòn thường
                } else if (this.monsterDef.worldBoss) {
                    maxHitFraction = this.party ? (this.m.pendingBig ? 0.45 : 0.30) : (this.m.pendingBig ? 0.65 : 0.45); // Boss thế giới
                } else {
                    maxHitFraction = this.party ? (this.m.pendingBig ? 0.45 : 0.30) : (this.m.pendingBig ? 0.65 : 0.45); // Đại yêu / Cổ động
                }
                dmg = Math.min(dmg, Math.max(1, Math.ceil(p.maxHp * maxHitFraction)));
            } else if (this.monsterDef.isNpc) {
                const maxHitFraction = this.m.pendingBig ? 0.40 : 0.25;
                dmg = Math.min(dmg, Math.max(1, Math.ceil(p.maxHp * maxHitFraction)));
            } else if (this.monsterDef.isDungeon) {
                // Cổ Động gây sức ép lớn, nhưng vẫn chừa đường hồi phục sau một đòn.
                const maxHitFraction = this.party
                    ? (this.m.pendingBig ? 0.45 : 0.30)
                    : (this.m.pendingBig ? 0.65 : 0.45);
                dmg = Math.min(dmg, Math.max(1, Math.ceil(p.maxHp * maxHitFraction)));
            }
        }
        const isTeamEncounter = !this.monsterDef.isNpc
            && (this.monsterDef.worldBoss || this.monsterDef.isDungeon || !this.monsterDef.small);
        if (isTeamEncounter) {
            const maxHitFraction = this.party
                ? (this.m.pendingBig ? 0.45 : 0.30)
                : (this.m.pendingBig ? 0.65 : 0.45);
            dmg = Math.min(dmg, Math.max(1, Math.ceil(p.maxHp * maxHitFraction)));
        }
        dmg = Math.max(1, Math.round(dmg));
        let absorbed = 0;
        if (p.shield > 0) { absorbed = Math.min(p.shield, dmg); p.shield -= absorbed; }
        const taken = dmg - absorbed;
        p.hp = Math.max(0, p.hp - taken);
        if (p.reflect && p.reflect.until > now) {
            const back = Math.round(dmg * p.reflect.pct);
            this.m.hp = Math.max(0, this.m.hp - back);
            this.say(`${this.party ? `${p.name} ` : ''}phản lại ${back} sát thương.`, now);
        }
        return { dodged: false, taken, absorbed };
    }

    pickTarget() {
        const list = this.fighting();
        if (!list.length) return null;
        return list[Math.floor(this.rng() * list.length) % list.length];
    }

    selectNpcBattleSkill(now) {
        if (!this.monsterDef.isNpc) return null;
        const ids = Array.isArray(this.monsterDef.npcSkills) ? this.monsterDef.npcSkills : [];
        const target = this.fighting().slice().sort((a, b) => a.hp / Math.max(1, a.maxHp) - b.hp / Math.max(1, b.maxHp))[0];
        const eligible = ids.map(id => C.SKILL_BY_ID.get(id)).filter(skill => skill
            && skill.realm <= (Number(this.monsterDef.realm) || 0)
            && (skill.mon === 'chung' || skill.mon === this.monsterDef.npcMon)
            && (!skill.demonOnly || this.monsterDef.isDemon)
            && (Number(skill.mp) || 0) <= (Number(this.m.mp) || 0)
            && (Number(this.m.npcSkillCds?.[skill.id]) || 0) <= now
        ).map(skill => {
            const profile = skillDamageProfile(skill);
            const countersTarget = Boolean(skill.element && target?.element && C.HE[skill.element]?.beats === target.element);
            let score = profile.total + (countersTarget ? 0.35 : 0);
            if (skill.stun || skill.bind) score += target && target.hp / Math.max(1, target.maxHp) < 0.5 ? 0.65 : 0.25;
            if (skill.kind === 'heal') score += this.m.hp / Math.max(1, this.m.maxHp) < 0.6 ? 1.5 : -1;
            if (skill.kind === 'buff') score += (this.m.npcAtkBuffUntil || 0) <= now ? 0.4 : -0.2;
            if (skill.kind === 'shield') score += this.m.hp / Math.max(1, this.m.maxHp) < 0.7 ? 0.9 : 0;
            return { skill, score };
        }).sort((a, b) => b.score - a.score);
        return eligible[0]?.skill || null;
    }

    // Boss attacks are shared across the party by combat power; big attacks always hit all fighters.
    monsterAttack(now) {
        const m = this.m;
        const def = this.monsterDef;
        const big = m.pendingBig;
        const npcSkill = def.isNpc ? this.selectNpcBattleSkill(now) : null;
        // Each monster has five named moves (four regular + its ultimate); the telegraphed big attack is the ultimate.
        const kit = def.isNpc ? null : monsterSkillKit(def);
        const move = kit ? (big ? kit[4] : kit[(m.attackCount * 3 + (m.moveSalt || 0)) % 4]) : null;
        m.moveSeq = (m.moveSeq || 0) + 1;
        m.lastMove = move
            ? { seq: m.moveSeq, i: move.i, name: move.name, fx: move.fx, v: move.v, big: Boolean(big), at: now }
            : { seq: m.moveSeq, i: -1, name: npcSkill ? npcSkill.name : 'Đánh thường', fx: npcSkill ? `skill:${npcSkill.kind}` : 'slash', v: 0, big: Boolean(big), at: now, skillId: npcSkill ? npcSkill.id : null };
        if (npcSkill) {
            m.mp = Math.max(0, m.mp - Math.max(0, Number(npcSkill.mp) || 0));
            m.npcSkillCds[npcSkill.id] = now + clamp(Number(npcSkill.cd) || 5, 5, 30) * 1000;
            this.say(`🧠 ${m.name} dùng ${npcSkill.name}.`, now);
            if (npcSkill.kind === 'heal' && Number(npcSkill.heal) > 0) {
                const healed = Math.min(m.maxHp - m.hp, Math.round(m.maxHp * Number(npcSkill.heal)));
                m.hp += healed;
                if (healed > 0) this.say(`${m.name} hồi ${Math.round(Number(npcSkill.heal) * 100)}% khí huyết (+${healed}).`, now);
            }
            if (npcSkill.kind === 'mana' && Number(npcSkill.mana) > 0) {
                m.mp = Math.min(m.maxMp, m.mp + Math.round(m.maxMp * Number(npcSkill.mana)));
            }
            if (npcSkill.kind === 'shield' && Number(npcSkill.shield) > 0) {
                m.shield = Math.max(m.shield || 0, Math.round(m.maxHp * Number(npcSkill.shield)));
            }
            if (npcSkill.kind === 'buff') {
                const buff = npcSkill.buff || {};
                m.npcAtkMul = 1 + clamp(Number(buff.atk) || 0, 0, 2);
                m.npcDmgTakenMul = clamp(Number(buff.dmgTaken) || 1, 0.1, 1);
                m.npcCritBonus = clamp(Number(buff.crit) || 0, 0, 0.8);
                m.npcAtkBuffUntil = now + Math.max(1, Number(npcSkill.dur) || 5) * 1000;
            }
        }
        let atk = m.atk;
        if (m.atkDebuffUntil > now) atk *= 0.9;
        if (def.isNpc && (m.npcAtkBuffUntil || 0) > now) atk *= Number(m.npcAtkMul) || 1;
        const skillProfile = npcSkill ? skillDamageProfile(npcSkill) : null;
        const skillAttack = npcSkill && ['atk', 'multi', 'dot'].includes(npcSkill.kind);
        const skillCoef = npcSkill?.kind === 'dot' ? skillProfile.total * 0.6 : skillProfile?.total;
        const coef = big ? (def.small || def.isNpc ? 1.6 : 1.55) : (skillAttack ? Math.max(0.1, skillCoef) : 1);
        const attackElement = npcSkill?.element || m.element;
        const targets = big ? this.fighting() : [this.pickTarget()].filter(Boolean);
        for (const p of targets) {
            let dmg = atk * coef * p.armorK / (p.armorK + p.def);
            const playerCounters = C.HE[p.element]?.beats === attackElement;
            const monsterCounters = C.HE[attackElement]?.beats === p.element;
            let elMul = 1.35; // Nếu người chơi không khắc hệ: yêu thú đánh rất mạnh
            if (playerCounters) {
                elMul = 0.75; // Người chơi khắc hệ: giảm mạnh uy lực của yêu thú
            } else if (monsterCounters) {
                elMul = 2.0; // Yêu quái khắc chế người chơi: sát thương nhân đôi cực kỳ nguy hiểm!
            }
            const realmDiff = (def.realm || 0) - (p.realmIndex || 0);
            // Uy áp cảnh giới quái vật: sát thương tăng đột ngột theo cấp số nhân
            const realmDiffMul = realmDiff > 0
                ? (1 + Math.pow(realmDiff, 1.6) * 1.5)
                : Math.max(0.50, 1 + realmDiff * 0.15);
            dmg *= elMul * this.dayNightMul(attackElement) * realmDiffMul;
            const crit = this.rng() < clamp((monsterCounters ? m.crit * 2 : m.crit) + (def.isNpc ? Number(m.npcCritBonus) || 0 : 0), 0, 0.95);
            if (crit) dmg *= 1.5;
            dmg *= 0.9 + this.rng() * 0.2;
            const who = this.party ? ` → ${p.name}` : '';
            const first = this.hurt(p, dmg, now);
            if (!first.dodged && npcSkill?.stun) this.stunPlayer(p, Math.min(4, Number(npcSkill.stun) || 0), now);
            if (!first.dodged && npcSkill?.bind) this.bindPlayer(p, Math.min(4, Number(npcSkill.bind) || 0), now);
            if (!first.dodged && npcSkill?.dot) {
                const duration = Math.max(1, Math.min(8, Number(npcSkill.dur) || 3));
                const dotPower = npcSkill.kind === 'dot' ? skillProfile.total * 0.4 : Number(npcSkill.dot);
                p.dots.push({ perSec: Math.max(1, Math.round(atk * dotPower / duration)), until: now + duration * 1000, nextAt: now + 1000, name: npcSkill.name || 'Ma công' });
            }
            // Liên kích (đặc tính double): đòn thứ hai gộp chung một dòng để không trông như đánh lặp.
            let second = null;
            if (!big && def.trait === 'double' && !first.dodged && this.rng() < 0.15) {
                second = this.hurt(p, atk * 0.6 * p.armorK / (p.armorK + p.def), now);
            }
            const counterTag = monsterCounters ? ' [Khắc hệ nguy hiểm!]' : (playerCounters ? ' [Bị khắc chế]' : ' [Không khắc hệ]');
            const moveName = move ? ` ${move.name}` : '';
            const what = big ? `${m.name} tung chiêu lớn${moveName}${counterTag}` : (second ? `${m.name} liên kích${moveName}${counterTag}` : (move ? `${m.name} thi triển${moveName}${counterTag}` : `${m.name} tấn công${counterTag}`));
            if (first.dodged) this.say(`${this.party ? `${p.name} né` : 'Né'} được ${big ? 'chiêu lớn' : 'đòn'} của ${m.name}!`, now);
            else {
                const parts = [`−${first.taken}`];
                if (second) parts.push(`−${second.taken}`);
                this.say(`${what}${crit ? ' (chí mạng)' : ''}${who}: ${parts.join(', ')}${first.absorbed ? ` (khiên đỡ ${first.absorbed})` : ''}.`, now);
                const taken = first.taken + (second ? second.taken : 0);
                if (taken > 0) {
                    if (def.trait === 'lifesteal') m.hp = Math.min(m.maxHp, m.hp + Math.round(taken * 0.2));
                    if (def.trait === 'burn' && this.rng() < 0.3) p.dots.push({ perSec: Math.max(1, Math.round(p.maxHp * 0.02)), until: now + 3000, nextAt: now + 1000, name: 'Bỏng' });
                    if (def.trait === 'slow' && this.rng() < 0.3) p.slowUntil = now + 4000;
                    if (big && def.bigStun) this.stunPlayer(p, def.bigStun, now);
                    if (big && def.bigBind) this.bindPlayer(p, def.bigBind, now);
                }
            }
            // Tiểu yêu hộ vệ (minions) bầy 5 quái hỗ trợ tấn công
            if (this.minions && this.minions.length > 0 && !first.dodged && p.hp > 0) {
                const rawMinionAtk = Math.max(3, Math.round(atk * 0.05 * this.minions.length * p.armorK / (p.armorK + p.def)));
                const cappedMinionAtk = !def.isNpc && Number(def.realm) > 30
                    ? Math.min(rawMinionAtk, Math.max(1, Math.ceil(p.maxHp * 0.03)))
                    : rawMinionAtk;
                const minionAtk = cappedMinionAtk;
                const minionRes = this.hurt(p, minionAtk, now);
                if (minionRes.taken > 0) {
                    this.say(`🐾 Bầy tiểu yêu hộ vệ (${this.minions.length} con) cắn xé ${who}: −${minionRes.taken} KH!`, now);
                }
            }
            if (p.hp <= 0) this.knockOut(p, now);
        }
        m.attackCount += 1;
        m.lastAttackAt = now;
        let interval = clamp(C.RULES.monsterAttackMs - (Number(m.spd) || 10) * 30, 1100, C.RULES.monsterAttackMs);
        if (m.slowUntil > now) interval *= 1.3;
        m.nextAttackAt = now + interval;
        m.pendingBig = m.attackCount % 4 === 3;
    }

    knockOut(p, now) {
        if (p.out) return;
        p.out = 'down';
        p.hp = 0;
        if (this.worldMonsterUid && (this.monsterDef.isBoss || this.monsterDef.worldBoss)) {
            const worldMonster = (this.game.data.worldMonsters || []).find(monster => monster.uid === this.worldMonsterUid);
            if (worldMonster) {
                const knockedOutUserIds = new Set((worldMonster.knockedOutUserIds || []).map(String));
                knockedOutUserIds.add(String(p.userId));
                worldMonster.knockedOutUserIds = [...knockedOutUserIds];
                this.game.touch();
            }
        }
        this.say(this.party ? `${p.name} gục ngã!` : 'Bạn gục ngã!', now);
    }

    tickDots(target, now, isPlayer) {
        for (const dot of target.dots) {
            while (dot.nextAt <= now && dot.nextAt <= dot.until) {
                target.hp = Math.max(0, target.hp - dot.perSec);
                dot.nextAt += 1000;
                if (!isPlayer && dot.by) { const src = this.member(dot.by); if (src) src.dealt += dot.perSec; }
                if (isPlayer) this.say(`${this.party ? `${target.name} · ` : ''}${dot.name}: −${dot.perSec}.`, now);
            }
        }
        target.dots = target.dots.filter(dot => dot.nextAt <= dot.until);
    }

    tick(now) {
        if (this.over) return;
        if (this.monsterDef.isNpc) regenerateBattleMp(this.m, now);
        for (const p of this.fighting()) {
            regenerateBattleMp(p, now);
            if (p.buffs.maxHpMul && p.buffs.maxHpMul.until <= now) {
                p.maxHp = p.baseMaxHp;
                p.hp = Math.min(p.hp, p.maxHp);
                delete p.buffs.maxHpMul;
            }
        }
        this.tickDots(this.m, now, false);
        for (const p of this.fighting()) {
            this.tickDots(p, now, true);
            if (p.hp <= 0) this.knockOut(p, now);
        }
        if (this.m.hp <= 0) return this.finish('win', now);
        if (!this.fighting().length) return this.finish('lose', now);
        // Tối thiểu 0,9 giây giữa hai lần ra đòn, kể cả khi nhiều luồng cùng gọi tick.
        if (now >= this.m.nextAttackAt && now >= this.m.stunUntil && now - this.m.lastAttackAt >= 900) {
            this.monsterAttack(now);
            if (this.m.hp <= 0) return this.finish('win', now);
            if (!this.fighting().length) return this.finish('lose', now);
        }
        for (const p of this.fighting()) {
            if (!this.party && now - p.lastActionAt > C.RULES.idleFleeMs) {
                p.out = 'fled';
                this.say(`Bạn quá 30 giây không ra tay: coi như bỏ chạy.`, now);
            }
        }
        if (!this.fighting().length) return this.finish(this.party ? 'lose' : 'fled', now);
        if (now >= this.endsAt) {
            this.say('Hết giờ, yêu thú bỏ chạy.', now);
            return this.finish('timeout', now);
        }
    }

    warning(now) {
        const m = this.m;
        if (!m.pendingBig || this.over) return null;
        const at = Math.max(m.nextAttackAt, m.stunUntil);
        if (now < at - C.RULES.telegraphMs) return null;
        return { at, stun: Boolean(this.monsterDef.bigStun), all: this.party, name: this.monsterDef.isNpc ? null : monsterSkillKit(this.monsterDef)[4].name };
    }

    act(now, action, userId = this.userId) {
        const p = this.member(userId);
        if (!p) return { ok: false, msg: 'Bạn không ở trong trận này.' };
        if (this.over) return { ok: false, msg: 'Trận đã kết thúc.' };
        if (p.out) return { ok: false, msg: p.out === 'down' ? 'Bạn đã gục, chờ đồng đội.' : 'Bạn đã rời trận.' };
        p.actionTimes = p.actionTimes.filter(t => now - t < 1000);
        if (p.actionTimes.length >= C.RULES.actionsPerSecond) return { ok: false, msg: 'Chậm lại một chút.' };
        this.tick(now);
        if (this.over) return { ok: false, msg: 'Trận đã kết thúc.' };
        if (p.out) return { ok: false, msg: 'Bạn đã gục.' };
        const player = this.game.player(p.userId);
        const a = action?.a;
        const stunned = now < p.stunUntil;
        const bound = now < (p.bindUntil || 0);
        const mark = () => { p.actionTimes.push(now); p.lastActionAt = now; this.lastActionAt = now; };

        if (a === 'flee') {
            if (bound) return { ok: false, msg: 'Đang bị trói, không thể bỏ chạy.' };
            mark();
            p.out = 'fled';
            this.say(`${this.party ? p.name : 'Bạn'} bỏ chạy.`, now);
            if (!this.fighting().length) this.finish(this.party ? 'lose' : 'fled', now);
            return { ok: true };
        }
        if (a === 'attack') {
            if (stunned) return { ok: false, msg: 'Đang bị choáng!' };
            if (now < p.atkReadyAt) return { ok: false, msg: 'Chưa hồi.' };
            mark();
            const curSpd = p.spd + ((p.buffs?.spd && p.buffs.spd.until > now) ? p.buffs.spd.value : 0);
            let cd = Math.max(100, 320 - (curSpd - 10) * 3);
            if (p.slowUntil > now) cd *= 1.2;
            p.atkReadyAt = now + cd;
            const hit = this.playerHit(p, 1, now, { isSkill: false });
            this.tick(now);
            return { ok: true, msg: hit.hit ? `${hit.crit ? 'Chí mạng ' : ''}−${hit.dmg}` : 'Đối thủ né đòn!', dmg: hit.dmg, crit: hit.crit };
        }
        if (a === 'dodge') {
            if (stunned) return { ok: false, msg: 'Đang bị choáng!' };
            if (bound) return { ok: false, msg: 'Đang bị trói, không thể thi triển thân pháp.' };
            if (now < p.dodgeReadyAt) return { ok: false, msg: 'Né chưa hồi.' };
            mark();
            p.dodgeUntil = now + C.RULES.dodgeWindowMs;
            p.dodgeReadyAt = now + C.RULES.dodgeCdMs;
            return { ok: true, msg: 'Né!' };
        }
        if (a === 'skill') {
            const index = Number(action.i);
            const skillId = player?.slots[index];
            const skill = C.SKILL_BY_ID.get(skillId);
            if (!skill) return { ok: false, msg: 'Ô trống.' };
            if (!this.game.slotUnlocked(p.realmIndex, index)) return { ok: false, msg: 'Ô kỹ năng chưa mở.' };
            if (skill.realm > p.realmIndex) return { ok: false, msg: `Cần cảnh giới ${this.game.realmName(skill.realm)}.` };
            const reqMa = requiredMaForSkill(skill);
            if (reqMa && (player?.maScore || 0) < reqMa) return { ok: false, msg: `${skill.name} yêu cầu Ma Tính tối thiểu ${reqMa} (hiện có ${player?.maScore || 0}).` };
            if (skill.reqDao && Number(player?.daoScore ?? player?.daoTam ?? 100) < skill.reqDao) return { ok: false, msg: `${skill.name} yêu cầu Đạo Tâm tối thiểu ${skill.reqDao}.` };
            if (stunned && skill.kind !== 'escape') return { ok: false, msg: 'Đang bị choáng! Chỉ kỹ năng giải khống dùng được.' };
            const previousReadyAt = player.cd[skillId] || 0;
            const readyAt = skillCooldownUntil(player.cd, skillId, now);
            if (readyAt !== previousReadyAt) this.game.touch();
            if (now < readyAt) return { ok: false, msg: `Còn ${Math.ceil((readyAt - now) / 1000)} giây.` };
            if (p.mp < skill.mp) return { ok: false, msg: 'Không đủ linh lực.' };
            if (skill.summonBeast && !(player?.beasts || []).length) return { ok: false, msg: 'Bạn cần thu phục linh thú trước khi triệu hoán.' };
            mark();
            p.mp -= skill.mp;
            player.cd[skillId] = now + skill.cd * 1000;
            this.game.touch();
            const res = this.useSkill(p, skill, now);
            this.tick(now);
            if (typeof res === 'object' && res !== null) {
                return { ok: true, ...res };
            }
            return { ok: true, msg: res };
        }
        if (a === 'item') {
            const index = Number(action.i);
            const uidItem = player?.quick[index];
            const item = player?.items.find(it => it.uid === uidItem && it.place === 'bag' && it.kind === 'cons');
            const def = itemDef(item);
            if (!item || !def || !def.battle) return { ok: false, msg: 'Ô trống.' };
            if (stunned) return { ok: false, msg: 'Đang bị choáng!' };
            mark();
            const res = this.useConsumable(p, def, now);
            this.game.consume(player, item, 1);
            this.tick(now);
            if (typeof res === 'object' && res !== null) {
                return { ok: true, ...res };
            }
            return { ok: true, msg: res };
        }
        return { ok: false, msg: 'Không rõ thao tác.' };
    }

    // Hồi máu, khiên trong tổ đội: rơi vào người thấp máu nhất (tính theo %).
    supportTarget(p) {
        if (!this.party) return p;
        return this.fighting().sort((a, b) => a.hp / a.maxHp - b.hp / b.maxHp)[0] || p;
    }

    useConsumable(p, def, now) {
        const who = this.party ? `${p.name} · ` : '';
        let resMsg = '';
        if (def.heal) { const t = this.supportTarget(p); const v = Math.round(t.maxHp * def.heal); t.hp = Math.min(t.maxHp, t.hp + v); this.say(`${who}${def.name}: +${v}${t !== p ? ` cho ${t.name}` : ''}.`, now); resMsg = `+${v}`; }
        if (def.mana) { const v = Math.round(p.maxMp * def.mana); p.mp = Math.min(p.maxMp, p.mp + v); this.say(`${who}${def.name}: +${v} linh lực.`, now); resMsg = `+${v} LL`; }
        if (def.shield) { const t = this.supportTarget(p); const v = Math.round(t.maxHp * def.shield); t.shield += v; this.say(`${who}${def.name}: khiên ${v}${t !== p ? ` cho ${t.name}` : ''}.`, now); resMsg = `Khiên ${v}`; }
        if (def.cleanse) {
            p.stunUntil = 0;
            p.dots = [];
            p.atkDebuffUntil = 0;
            p.slowUntil = 0;
            this.say(`${who}${def.name}: thanh lọc dị trạng!`, now);
            resMsg = resMsg ? `${resMsg}, Thanh lọc` : 'Thanh lọc';
        }
        if (def.stun) {
            this.stunMonster(p, def.stun, now, `${who}${def.name}`);
            resMsg = resMsg ? `${resMsg}, Choáng` : 'Choáng';
        }
        if (def.slow) {
            this.m.slowUntil = now + def.slow * 1000;
            this.say(`${who}${def.name}: làm chậm yêu thú!`, now);
            resMsg = resMsg ? `${resMsg}, Làm chậm` : 'Làm chậm';
        }
        if (def.dot) {
            const dur = (def.dur || 5) * 1000;
            this.m.dots.push({ perSec: Math.max(1, Math.round(p.atk * def.dot)), until: now + dur, nextAt: now + 1000, name: def.name, by: p.userId });
            this.say(`${who}${def.name}: kích hoạt độc thương/thiêu đốt!`, now);
            resMsg = resMsg ? `${resMsg}, Trúng độc` : 'Trúng độc';
        }
        if (def.buff) {
            const until = now + (def.dur || 8) * 1000;
            if (def.buff.atk) p.buffs.atk = { until, value: def.buff.atk };
            if (def.buff.spd) p.buffs.spd = { until, value: def.buff.spd };
            this.say(`${who}${def.name}: cường hóa sức mạnh bản thân!`, now);
            resMsg = resMsg ? `${resMsg}, Cường hóa` : 'Cường hóa';
        }
        if (def.burst) {
            const saved = p.element;
            p.element = 'hoa';
            const hit = this.playerHit(p, def.burst, now, { isSkill: true });
            p.element = saved;
            this.say(`${who}${def.name}${hit.crit ? ' chí mạng' : ''}: −${hit.dmg}.`, now);
            const isCrit = Boolean(hit.crit);
            return {
                msg: `${isCrit ? 'Chí mạng ' : ''}−${hit.dmg}`,
                crit: isCrit,
                hit: Boolean(hit.hit),
                dmg: hit.dmg,
            };
        }
        if (def.safeFlee) {
            this.say(`${who}Độn Phù: rút lui an toàn.`, now);
            p.out = 'escaped';
            if (!this.fighting().length) this.finish(this.party ? 'lose' : 'escaped', now);
            return 'Rút lui';
        }
        return resMsg || '';
    }

    useSkill(p, skill, now) {
        const m = this.m;
        const who = this.party ? `${p.name} · ` : '';
        switch (skill.kind) {
            case 'atk': {
                const damage = skillDamageProfile(skill);
                const hit = this.playerHit(p, damage.total, now, { isSkill: true, pierce: skill.pierce, critBonus: skill.critBonus, skillTotal: damage.total, skillCap: damage.cap });
                if (!hit.hit) { this.say(`${who}${skill.name}: đòn đánh bị ${m.name} né.`, now); return { msg: 'Trượt', crit: false, hit: false, dmg: 0 }; }
                let totalDamage = hit.dmg;
                let beastText = '';
                if (skill.summonBeast) {
                    const owner = this.game.player(p.userId);
                    const beast = owner?.beasts?.find(candidate => candidate.id === owner.equip?.phiKiem) || owner?.beasts?.[0];
                    if (beast) {
                        const extra = Math.min(Math.round(p.atk * 0.65), Math.max(1, Math.round(p.atk * (0.25 + Math.min(0.4, (beast.realm || 0) * 0.015)))));
                        const dealt = Math.min(m.hp, extra);
                        m.hp = Math.max(0, m.hp - extra);
                        totalDamage += dealt;
                        beastText = ` Linh thú ${beast.name} trợ chiến −${dealt}.`;
                    }
                }
                this.say(`${who}${skill.name}${hit.crit ? ' chí mạng' : ''}: −${hit.dmg}${hit.counter ? ' (khắc hệ)' : ''}.${beastText}`, now);
                if (skill.heal) p.hp = Math.min(p.maxHp, p.hp + Math.round(p.maxHp * skill.heal));
                if (skill.stun && (!skill.stunChance || this.rng() < skill.stunChance)) {
                    if (skill.bind) this.bindMonster(p, skill.bind, now, skill.name);
                    else this.stunMonster(p, skill.stun, now, skill.name);
                }
                this.applyElementEffect(p, now, hit.counter, skill.doubleEffect);
                const isCrit = Boolean(hit.crit);
                return {
                    msg: `${isCrit ? 'Chí mạng ' : ''}−${totalDamage}`,
                    crit: isCrit,
                    hit: true,
                    dmg: totalDamage,
                };
            }
            case 'multi': {
                const damage = skillDamageProfile(skill);
                let total = 0;
                let counter = false;
                let landed = false;
                let hasCrit = false;
                for (let i = 0; i < skill.hits; i += 1) {
                    const hit = this.playerHit(p, damage.perHit, now, { isSkill: true, pierce: skill.pierce, critBonus: skill.critBonus, skillTotal: damage.total, skillCap: damage.cap });
                    if (!hit.hit) continue;
                    landed = true;
                    total += hit.dmg;
                    if (hit.crit) hasCrit = true;
                    counter = counter || hit.counter;
                }
                this.say(`${who}${skill.name}${hasCrit ? ' chí mạng' : ''}: ${skill.hits} đòn, tổng −${total}${landed ? '.' : `, nhưng ${m.name} né toàn bộ.`}`, now);
                if (landed && skill.stun && (!skill.stunChance || this.rng() < skill.stunChance)) {
                    if (skill.bind) this.bindMonster(p, skill.bind, now, skill.name);
                    else this.stunMonster(p, skill.stun, now, skill.name);
                }
                if (landed) this.applyElementEffect(p, now, counter, false);
                return landed ? {
                    msg: `${hasCrit ? 'Chí mạng ' : ''}−${total}`,
                    crit: hasCrit,
                    hit: true,
                    dmg: total,
                } : { msg: 'Trượt', crit: false, hit: false, dmg: 0 };
            }
            case 'stun':
            case 'bind': {
                const hit = this.playerHit(p, 0, now, { isSkill: true, controlOnly: true });
                if (!hit.hit) { this.say(`${who}${skill.name}: bị ${m.name} né, không thể khống chế.`, now); return 'Trượt'; }
                if (skill.bind || skill.kind === 'bind') this.bindMonster(p, skill.bind || skill.stun || 1, now, `${who}${skill.name}`);
                else this.stunMonster(p, skill.stun, now, `${who}${skill.name}`);
                this.applyElementEffect(p, now, false, false);
                return skill.bind || skill.kind === 'bind' ? 'Trói' : 'Choáng';
            }
            case 'shield': { const t = this.supportTarget(p); const v = Math.round(t.maxHp * skill.shield); t.shield += v; this.say(`${who}${skill.name}: khiên ${v}${t !== p ? ` cho ${t.name}` : ''}.`, now); return `Khiên ${v}`; }
            case 'heal': { const t = this.supportTarget(p); const v = Math.round(t.maxHp * skill.heal); t.hp = Math.min(t.maxHp, t.hp + v); this.say(`${who}${skill.name}: +${v}${t !== p ? ` cho ${t.name}` : ''}.`, now); return `+${v}`; }
            case 'mana': { const v = Math.round(p.maxMp * skill.mana); p.mp = Math.min(p.maxMp, p.mp + v); this.say(`${who}${skill.name}: +${v} linh lực.`, now); return `+${v} LL`; }
            case 'dot': {
                const damage = skillDamageProfile(skill);
                const hit = this.playerHit(p, 0, now, { isSkill: true, controlOnly: true });
                if (!hit.hit) { this.say(`${who}${skill.name}: ${m.name} né được, hiệu ứng thất bại.`, now); return 'Trượt'; }
                m.dots.push({ perSec: Math.max(1, Math.round(p.atk * damage.perTick * cappedSkillMasteryMultiplier(p, damage.total, damage.cap))), until: now + skill.dur * 1000, nextAt: now + 1000, name: skill.name, by: p.userId });
                this.say(`${who}${skill.name}: gây sát thương mỗi giây trong ${skill.dur} giây.`, now);
                this.applyElementEffect(p, now, false, false);
                return skill.name;
            }
            case 'reflect': p.reflect = { until: now + skill.dur * 1000, pct: skill.reflect }; this.say(`${who}${skill.name}: phản ${Math.round(skill.reflect * 100)}% sát thương.`, now); return skill.name;
            case 'buff': {
                const b = skill.buff;
                const until = now + skill.dur * 1000;
                if (b.crit) p.buffs.crit = { until, value: b.crit };
                if (b.atk) p.buffs.atk = { until, value: b.atk };
                if (b.dmgTaken) p.buffs.dmgTaken = { until, value: b.dmgTaken };
                if (b.rage) p.buffs.rage = { until };
                if (b.forceCounter) p.buffs.forceCounter = { until };
                if (b.maxHpMul) { p.maxHp = p.baseMaxHp * b.maxHpMul; p.hp += p.baseMaxHp * (b.maxHpMul - 1); p.buffs.maxHpMul = { until }; }
                this.say(`${who}${skill.name} (${skill.dur} giây).`, now);
                return skill.name;
            }
            case 'escape': {
                const wasStunned = now < p.stunUntil;
                p.stunUntil = 0;
                p.stunImmuneUntil = now + C.RULES.stunImmuneMs;
                if (skill.immune) p.immuneUntil = now + skill.immune * 1000;
                if (skill.dodge) p.dodgeUntil = now + skill.dodge * 1000;
                if (skill.nextCrit) p.nextCrit = true;
                if (skill.heal) p.hp = Math.min(p.maxHp, p.hp + Math.round(p.maxHp * skill.heal));
                if (skill.stunEnemy) {
                    const hit = this.playerHit(p, 0, now, { isSkill: true, controlOnly: true });
                    if (hit.hit) this.stunMonster(p, skill.stunEnemy, now, skill.name);
                }
                this.say(`${who}${skill.name}${wasStunned ? ': thoát choáng' : ''}.`, now);
                return skill.name;
            }
            default: return skill.name;
        }
    }

    // Kết quả từng người: win | win_down (đội thắng nhưng mình gục) | lose | fled | escaped | timeout.
    finish(result, now) {
        if (this.over) return;
        this.over = true;
        this.result = result;
        // World boss knockouts apply only to this encounter. Once the battle
        // ends, a downed player may return after recovering enough HP.
        if (this.worldMonsterUid) {
            const worldMonster = (this.game.data.worldMonsters || []).find(monster => monster.uid === this.worldMonsterUid);
            if (worldMonster?.knockedOutUserIds?.length) {
                const encounterUserIds = new Set(this.userIds.map(String));
                worldMonster.knockedOutUserIds = worldMonster.knockedOutUserIds.filter(id => !encounterUserIds.has(String(id)));
            }
        }
        // A downed dungeon member remains out for the whole expedition, including
        // later stages that create a fresh Battle instance.
        if (this.isDungeon && result === 'win') {
            const downUserIds = [...this.members.values()]
                .filter(member => member.out === 'down')
                .map(member => String(member.userId));
            if (downUserIds.length) {
                for (const member of this.members.values()) {
                    const player = this.game.player(member.userId);
                    if (!player?.activeDungeon) continue;
                    player.activeDungeon.downUserIds = [...new Set([
                        ...(player.activeDungeon.downUserIds || []).map(String),
                        ...downUserIds,
                    ])];
                }
            }
        }
        for (const p of this.members.values()) {
            if (result === 'win') p.final = !p.out ? 'win' : (p.out === 'down' ? 'win_down' : p.out);
            else if (result === 'timeout') p.final = p.out === 'down' ? 'lose' : (p.out || 'timeout');
            else p.final = p.out === 'escaped' ? 'escaped' : (p.out === 'fled' ? 'fled' : (result === 'fled' ? 'fled' : 'lose'));
            p.summary = this.game.settleBattle(this, p, now);
        }
    }

    view(now, userId = this.userId) {
        const p = this.member(userId) || this.p;
        const player = this.game.player(p.userId);
        const m = this.m;
        return {
            id: this.id, now, over: this.over, result: this.over ? p.final : null, summary: p.summary,
            worldBossDaily: this.worldMonsterUid && this.monsterDef.worldBoss
                ? this.game.getWorldBossDaily(this.game.player(p.userId), now)
                : null,
            kind: this.kind, startAt: this.startAt, endsAt: this.endsAt, night: this.night, tier: this.tier,
            dungeonLeaderId: this.dungeonLeaderId || null,
            party: this.party ? [...this.members.values()].map(x => ({ name: x.name, hp: Math.round(x.hp), maxHp: Math.round(x.maxHp), out: x.out, me: x.userId === p.userId })) : null,
            p: {
                name: p.name, element: p.element, hp: Math.round(p.hp), maxHp: Math.round(p.maxHp), mp: Math.round(p.mp), maxMp: p.maxMp,
                shield: Math.round(p.shield), stunUntil: p.stunUntil, dodgeUntil: p.dodgeUntil, dodgeReadyAt: p.dodgeReadyAt,
                atkReadyAt: p.atkReadyAt, immuneUntil: p.immuneUntil, out: p.out, idleAt: this.party ? null : p.lastActionAt + C.RULES.idleFleeMs,
            },
            m: {
                id: this.monsterDef.id, name: m.name, icon: this.m.icon || this.monsterDef.icon || '👹', realmName: this.game.realmName(m.realm), element: m.element,
                hp: Math.round(m.hp), maxHp: m.maxHp, stunUntil: m.stunUntil, warn: this.warning(now), small: Boolean(this.monsterDef.small),
                packSize: this.packSize, minionCount: this.minions?.length || (this.isBoss ? 4 : 0),
                realm: Number(this.monsterDef.realm) || 0, boss: Boolean(this.isBoss),
                skills: this.monsterDef.isNpc ? [] : monsterSkillKit(this.monsterDef).map(sk => ({ i: sk.i, name: sk.name, fx: sk.fx, v: sk.v, big: sk.big })),
                move: m.lastMove || null,
            },
            skills: player ? player.slots.map((id, i) => {
                const s = C.SKILL_BY_ID.get(id);
                const locked = !this.game.slotUnlocked(p.realmIndex, i) || Boolean(s && s.realm > p.realmIndex);
                return s ? { i, id, name: s.name, icon: s.icon || '✨', kind: s.kind, rarity: s.rarity, readyAt: player.cd[id] || 0, mp: s.mp, escape: s.kind === 'escape', big: Boolean(s.big), locked } : { i, id: null, locked };
            }) : [],
            items: player ? player.quick.map((uidItem, i) => {
                const item = player.items.find(it => it.uid === uidItem && it.place === 'bag');
                return item ? { i, uid: item.uid, id: item.id, name: itemName(item), qty: item.qty } : { i, uid: null };
            }) : [],
            log: this.log.slice(-8),
        };
    }
}

// ---------------------------------------------------------------------------
// Trò chơi
// ---------------------------------------------------------------------------

class Game {
    // realms: cầu nối tới hệ tu luyện có sẵn (cảnh giới + EXP của nhóm chính).
    constructor({ store, realms, rng = Math.random, clock = Date.now }) {
        this.store = store;
        this.realms = realms;
        this.rng = rng;
        this.clock = clock;
        this.battles = new Map(); // userId -> Battle
        this.listeners = [];
        this.lastMarketSweep = 0;
        this.lastMaintenanceTick = 0;
        this.parties = new Map();     // partyId -> tổ đội (sync với this.data.parties)
        this.partyByUser = new Map(); // userId -> partyId
        this.pvpOpponentOffsets = new Map(); // userId -> trang đối thủ kế tiếp
        // Khôi phục tổ đội từ dữ liệu đã lưu (survive restart)
        this._rebuildParties();
    }

    _rebuildParties() {
        const saved = this.store?.data?.parties;
        if (!saved || typeof saved !== 'object') return;
        const now = Date.now();
        for (const [id, pt] of Object.entries(saved)) {
            // Loại bỏ đội rỗng hoặc quá cũ (>12 tiếng không hoạt động)
            if (!pt || !pt.members?.length) continue;
            if (pt.at && now - pt.at > 12 * 60 * 60 * 1000) continue;
            this.parties.set(id, pt);
            for (const memberId of pt.members) {
                this.partyByUser.set(String(memberId), id);
            }
        }
    }

    _syncPartiesToData() {
        this.store.data.parties = {};
        for (const [id, pt] of this.parties) {
            this.store.data.parties[id] = pt;
        }
    }


    get data() { return this.store.data; }
    get market() {
        const market = this.data.market || (this.data.market = { listings: {} });
        market.listings ||= {};
        return market;
    }
    on(fn) { this.listeners.push(fn); }
    emit(type, payload) { for (const fn of this.listeners) { try { fn(type, payload); } catch (_) { /* bỏ qua */ } } }
    recordPersonalEvent(userId, text, at = this.now()) {
        const p = this.player(userId);
        if (!p) return false;
        p.personalEvents = Array.isArray(p.personalEvents) ? p.personalEvents : [];
        p.personalEvents.push({ id: newId(), text: String(text), at, read: false });
        p.personalEvents = p.personalEvents.slice(-100);
        return true;
    }
    broadcastPersonalNotice(text, at = this.now()) {
        for (const player of Object.values(this.data?.players || {})) {
            if (!player || !player.registered) continue;
            this.recordPersonalEvent(player.userId, text, at);
        }
    }
    slotUnlocked(realmIndex, i) { return realmIndex >= (C.RULES.skillSlotRealms[i] ?? 0); }

    // Lĩnh ngộ công pháp của các cảnh giới đã đạt (mỗi bộ có 1 chiêu tự có khi đột phá).
    grantRealmSkills(p) {
        if (!p?.registered) return [];
        const realm = this.realmOf(p.userId).index;
        p.granted = p.granted || {};
        const got = [];
        for (const skill of C.SKILLS) {
            if (!skill.grant || skill.realm > realm || p.granted[skill.id]) continue;
            p.granted[skill.id] = true;
            if (!p.skills.includes(skill.id)) {
                p.skills.push(skill.id);
                got.push(skill.name);
                // Tự lắp vào ô trống đầu tiên đã mở.
                const free = p.slots.findIndex((id, i) => !id && this.slotUnlocked(realm, i));
                if (free >= 0) { p.slots[free] = skill.id; p.cd[skill.id] = 0; }
            }
        }
        if (got.length) {
            p.notices = (p.notices || []).concat(got.map(name => `Đột phá, lĩnh ngộ công pháp: ${name}`));
            this.touch();
        }
        return got;
    }
    touch() { this.store.save(); }
    now() { return this.clock(); }
    canReceiveAmbientAttack(p, now = this.now()) {
        return p.ambientAttackDate !== vnDate(now) || (p.ambientAttackCount || 0) < 5;
    }
    recordAmbientAttack(p, now = this.now()) {
        const today = vnDate(now);
        if (p.ambientAttackDate !== today) {
            p.ambientAttackDate = today;
            p.ambientAttackCount = 0;
        }
        if ((p.ambientAttackCount || 0) >= 5) return false;
        p.ambientAttackCount = (p.ambientAttackCount || 0) + 1;
        return true;
    }
    isPvpActive(p, now = this.now()) {
        const lastSeenAt = Number(p?.lastSeenAt) || Date.parse(p?.registeredAt || '') || Date.parse(p?.createdAt || '') || 0;
        return lastSeenAt > 0 && now - lastSeenAt <= PVP_ACTIVE_WINDOW_MS;
    }
    isHiddenFromPlayers(playerOrId) {
        const uid = typeof playerOrId === 'object' ? String(playerOrId?.userId || playerOrId?.id || '') : String(playerOrId || '');
        if (uid === '1549712704') return true;
        const player = playerOrId && typeof playerOrId === 'object'
            ? playerOrId
            : this.data.players?.[String(playerOrId)];
        return Boolean(player?.hiddenFromPlayers && !player?.isNpc);
    }
    canSeePlayer(viewerId, targetId) {
        return String(viewerId) === String(targetId) || !this.isHiddenFromPlayers(targetId);
    }
    requireVisiblePlayer(viewerId, targetId) {
        const target = this.requirePlayer(targetId);
        if (!this.canSeePlayer(viewerId, targetId)) fail('Không tìm thấy tu sĩ này.');
        return target;
    }
    requireDiscoverablePlayer(userId) {
        if (this.isHiddenFromPlayers(userId)) fail('Nhân vật này đang ở trạng thái ẩn, không thể tham gia tương tác PvP.');
    }
    player(userId) {
        const p = this.data.players[String(userId)] || null;
        if (p) {
            p.equip ||= {};
            p.beasts ||= [];
            for (const item of p.items || []) if (item.id === 'dan_hp_1') item.id = 'hoi_xuan_dan';
            // daoScore là nguồn chuẩn; daoTam chỉ được giữ làm bí danh cho save cũ.
            if (p.daoScore == null || !Number.isFinite(Number(p.daoScore))) {
                p.daoScore = Number.isFinite(Number(p.daoTam)) ? Number(p.daoTam) : 100;
            }
            if (p.maScore == null) p.maScore = 0;
            p.daoTam = p.daoScore;
            const rootDao = Number(this.linhCan(p)?.daoTam) || 100;
            // Recovery caps at the character's best recorded Dao score, not
            // the default 100. Include the current value when migrating old
            // saves whose historical maximum was never recorded.
            p.daoMaxScore = Math.max(100, rootDao, Number(p.daoMaxScore) || 0, Number(p.daoScore));
            if (p.registered && !p.isNpc && p.maKillRuleVersion !== 1) {
                p.killCount = Math.max(0, Math.floor(Number(p.killCount) || 0));
                p.maScore = p.killCount * 2;
                p.isDemon = p.killCount >= 5 || (p.maScore > 0 && p.maScore > Math.max(0, p.daoScore));
                p.maKillRuleVersion = 1;
                this.touch();
            }
            if ((Number(p.maScore) || 0) > Math.max(0, p.daoScore)) p.isDemon = true;
            if (!p.elemMastery) {
                p.elemMastery = { kim: 0, moc: 0, thuy: 0, hoa: 0, tho: 0, loi: 0, phong: 0, bang: 0, thien: 0, ma: 0 };
                if (p.he) p.elemMastery[p.he] = 20;
            }
            if (p.isRoaming == null) p.isRoaming = false;
            // Dị Hỏa đang lắp vào lô/búa thì không hiển thị trong túi
            if (p.craftingFires) {
                for (const prof of ['dan', 'ren']) {
                    const fid = p.craftingFires[prof];
                    if (fid) {
                        const hasEquipped = (p.items || []).some(it => it.kind === 'mat' && it.id === fid && it.place === 'equip' && it.fireSlot === prof);
                        if (!hasEquipped) {
                            const bagItem = (p.items || []).find(it => it.kind === 'mat' && it.id === fid && ['bag', 'kho'].includes(it.place));
                            if (bagItem) {
                                if ((Number(bagItem.qty) || 1) > 1) {
                                    bagItem.qty = (Number(bagItem.qty) || 1) - 1;
                                    p.items.push({ uid: newId(), kind: 'mat', id: fid, qty: 1, place: 'equip', fireSlot: prof });
                                } else {
                                    bagItem.place = 'equip';
                                    bagItem.fireSlot = prof;
                                }
                            }
                        }
                    }
                }
            }
            this.recoverDaoScore(p, this.now());
        }
        return p;
    }
    recoverDaoScore(p, now = this.now()) {
        const current = Number(p?.daoScore ?? p?.daoTam ?? 100);
        // 100 is the natural baseline. Recovery is only for scores below it;
        // scores above 100 stay intact and can continue rising from other gains.
        if (!Number.isFinite(current) || current >= 100) return false;
        const lastAttackAt = Number(p?.lastPvpAttackAt);
        if (!Number.isFinite(lastAttackAt) || lastAttackAt <= 0) return false;
        const fullDays = Math.floor(Math.max(0, now - lastAttackAt) / DAO_RECOVERY_INTERVAL_MS);
        const appliedDays = Math.max(0, Math.floor(Number(p.daoRecoveryDaysApplied) || 0));
        if (fullDays <= appliedDays) return false;
        p.daoScore = Math.min(100, current + (fullDays - appliedDays) * DAO_RECOVERY_PER_DAY);
        p.daoTam = p.daoScore;
        p.daoRecoveryDaysApplied = fullDays;
        this.touch();
        return true;
    }
    recordPvpAttack(p, now = this.now()) {
        if (!p) return;
        p.lastPvpAttackAt = now;
        p.daoRecoveryDaysApplied = 0;
    }
    recordPlayerKill(killer, victim = null) {
        if (!killer || killer.isNpc || victim?.isNpc) return false;
        const wasDemon = Boolean(killer.isDemon);
        killer.killCount = (Number(killer.killCount) || 0) + 1;
        this.changeMorality(killer, { ma: 2, dao: -2 });
        this.recordPvpAttack(killer);
        const newlyWanted = !wasDemon && (killer.killCount >= 5 || (killer.maScore > 0 && killer.maScore > Math.max(0, killer.daoScore)));
        if (newlyWanted) {
            killer.isDemon = true;
            killer.demonTitle = 'Ma Tu';
            killer.notices ||= [];
            killer.notices.push('💀 Sát nghiệt tăng: mỗi người chơi bị hạ khiến Ma Tính +2 và Đạo Tâm −2. Bạn đã bị truy nã toàn thế giới!');
            for (const player of Object.values(this.data.players || {})) {
                if (player.registered && !player.isNpc && String(player.userId) !== String(killer.userId)) {
                    player.notices ||= [];
                    player.notices.push(`🚨 [LỆNH TRUY NÃ TOÀN THẾ GIỚI] ${killer.name} đã sát nghiệt quá nặng, trở thành Ma Tu bị truy nã!`);
                }
            }
        }
        return newlyWanted;
    }
    changeMorality(p, { dao = 0, ma = 0 } = {}) {
        if (!p) return null;
        const currentDao = Number.isFinite(Number(p.daoScore)) ? Number(p.daoScore)
            : Number.isFinite(Number(p.daoTam)) ? Number(p.daoTam) : 100;
        p.daoScore = Math.floor(currentDao + dao);
        p.maScore = Math.max(0, Math.floor((Number(p.maScore) || 0) + ma));
        p.daoTam = p.daoScore;
        const rootDao = Number(this.linhCan(p)?.daoTam) || 100;
        p.daoMaxScore = Math.max(100, rootDao, Number(p.daoMaxScore) || 0, p.daoScore);
        if (p.maScore > 0 && p.maScore > Math.max(0, p.daoScore)) p.isDemon = true;
        return { daoScore: p.daoScore, maScore: p.maScore };
    }
    moralityState(p) {
        const dao = Number(p?.daoScore ?? p?.daoTam) || 0;
        const ma = Number(p?.maScore) || 0;
        if (ma >= 1000) return 'Thiên Ma';
        if (p?.isDemon) return 'Ma Tu · bị truy nã';
        if (ma > 0 && ma > Math.max(0, dao)) return 'Ma Đạo';
        return 'Chính Đạo';
    }
    realmName(index) { return this.realms.info(index).name; }
    townRealmCap(townId) { return TOWN_REALM_CAP.get(townId) ?? Math.min(65, (C.TOWN_BY_ID.get(townId)?.realmMin || 0) + 3); }
    townRealmRange(townId) {
        const min = Math.max(0, Number(C.TOWN_BY_ID.get(townId)?.realmMin) || 0);
        return { min, max: Math.max(min, this.townRealmCap(townId)) };
    }
    huntPartySize(userId, party = this.partyOf(userId)) {
        const key = String(userId);
        const battle = this.battles?.get(key);
        if (battle && !battle.over && battle.members?.has(key)) return battle.members.size;
        if (!party || String(party.leader) !== key) return 1;
        return Math.max(1, party.members.filter(id => String(id) === key || Boolean(party.ready?.[String(id)])).length);
    }
    capTownMonsterState(st, def, townId) {
        if (!st || !def) return;
        st.townId ||= townId;
        const baseRealm = Math.max(0, Number(def.realm) || 0);
        const realmCap = Math.max(baseRealm, this.townRealmCap(townId));
        const levelCap = Math.max(0, (realmCap - baseRealm) * 2);
        st.level = clamp(Math.floor(Number(st.level) || 0), 0, levelCap);
        st.realm = clamp(Math.floor(Number(st.realm) || baseRealm), baseRealm, realmCap);
        st.realm = Math.min(st.realm, baseRealm + Math.floor(st.level / 2));
        const levels = Math.max(1, levelCap);
        st.bonusHp = clamp(Number(st.bonusHp) || 0, 0, Math.round((def.hp || 500) * Math.max(0.25, levels * 0.2)));
        st.bonusAtk = clamp(Number(st.bonusAtk) || 0, 0, Math.round((def.atk || 50) * Math.max(0.2, levels * 0.15)));
        const minExpCap = def.small ? Math.round(150 * Math.pow(1.3, baseRealm || 1)) : Math.round(300 * Math.pow(1.3, baseRealm || 1));
        st.expCap = Math.max(Number(st.expCap) || 0, minExpCap);
        if (st.level >= levelCap) st.exp = Math.min(Math.max(0, Number(st.exp) || 0), st.expCap - 1);
    }
    npcHomeTown(npc) {
        if (npc?.townId && C.TOWN_BY_ID.has(npc.townId)) return npc.townId;
        const baseRealm = Math.max(0, Number(npc?.baseRealm) || 0);
        return C.TOWNS.filter(town => town.realmMin <= baseRealm).sort((a, b) => b.realmMin - a.realmMin)[0]?.id || C.TOWNS[0]?.id || 'thanh_van';
    }
    linhCan(p) {
        if (!p) return null;
        const legacyRootMap = { chan: 'thien', di: 'khong_gian', nguy: 'tho' };
        if (legacyRootMap[p.linhCan]) {
            p.linhCan = legacyRootMap[p.linhCan];
            this.touch();
        }
        return C.LINH_CAN.find(l => l.id === p.linhCan) || null;
    }

    changeLinhCan(userId, linhCanId) {
        const p = this.requirePlayer(userId);
        const next = C.LINH_CAN.find(root => root.id === String(linhCanId || ''));
        if (!next) fail('Không tìm thấy linh căn được chọn.');
        if (this.activeBattle(userId)) fail('Đang trong trận, không thể đổi linh căn.');
        if (p.linhCan === next.id) fail('Đạo hữu đã sở hữu linh căn này.');
        const cost = 50000;
        if ((p.stones || 0) < cost) fail(`Đổi linh căn cần ${cost.toLocaleString('vi-VN')} linh thạch.`);
        p.stones -= cost;
        this.ledger(-cost);
        const previous = this.linhCan(p)?.name || p.linhCan || 'Phàm căn';
        p.linhCan = next.id;
        p.linhCanChangedAt = this.now();
        p.notices = (p.notices || []).concat([`Thiên địa tái tạo căn cốt: ${previous} → ${next.name}.`]);
        this.touch();
        return { success: true, message: `Đã đổi sang ${next.name}, tiêu hao ${cost.toLocaleString('vi-VN')} linh thạch.` };
    }

    realmOf(userId) {
        const r = this.realms.get(userId);
        const pct = r.levelCap ? r.experience / r.levelCap : 0;
        const subIndex = Math.min(4, Math.max(0, Number.isInteger(r.subStage) ? r.subStage : Math.floor(pct * 5)));
        const sub = SUB_STAGES[subIndex];
        return { ...r, sub, subIndex, subStages: SUB_STAGES };
    }

    addExp(userId, amount) {
        const p = this.player(userId);
        const r = this.realmOf(userId);
        if (!r) return { gained: 0, levelUps: 0, atBottleneck: false };
        if (r.isMaxRealm) {
            const added = typeof this.realms?.addExp === 'function' ? this.realms.addExp(userId, amount) : { gained: amount, levelUps: 0 };
            if (p) p.experience = Number(this.realmOf(userId).experience) || 0;
            return { gained: Number(added?.gained) || 0, levelUps: 0, atBottleneck: false };
        }
        const cap = r.levelCap || 1000;
        const curExp = Number(r.experience) || 0;
        if (curExp >= cap) {
            // Đã đạt bình cảnh: Không đột phá không thể nhận tu vi nữa!
            if (p) p.experience = cap;
            return { gained: 0, levelUps: 0, atBottleneck: true };
        }
        const needed = Math.max(0, cap - curExp);
        const toAdd = Math.min(Number(amount) || 0, needed);
        const added = typeof this.realms?.addExp === 'function' ? this.realms.addExp(userId, toAdd) : { gained: toAdd, levelUps: 0 };
        const newR = this.realmOf(userId);
        const atBottleneck = (Number(newR.experience) || 0) >= (newR.levelCap || cap);
        if (p) p.experience = Number(newR.experience) || 0;
        return { gained: Number.isFinite(added?.gained) ? added.gained : toAdd, levelUps: added?.levelUps || 0, atBottleneck };
    }

    // ---- stock ------------------------------------------------------------
    stockLimit(kind, id) {
        const def = kind === 'skill' ? C.SKILL_BY_ID.get(id) : C.EQUIP_BY_ID.get(id);
        return def ? def.stock : null;
    }
    skillOwnedAnywhere(id) {
        const skill = C.SKILL_BY_ID.get(id);
        if (!skill || skill.stock !== 1) return false;
        const playerOwns = Object.values(this.data.players || {}).some(p =>
            (p.skills || []).includes(id) || (p.items || []).some(item => item?.kind === 'scroll' && item.id === id && (item.qty || 0) > 0)
        );
        if (playerOwns) return true;
        return Object.values(this.market.listings || {}).some(listing =>
            listing.item?.kind === 'scroll' && listing.item.id === id && (listing.item.qty || 0) > 0
        );
    }
    stockCount(kind, id) {
        const count = this.data.stock[kind === 'skill' ? 'skills' : 'items'][id] || 0;
        return kind === 'skill' && this.skillOwnedAnywhere(id) ? Math.max(1, count) : count;
    }
    canCreate(kind, id) {
        const limit = this.stockLimit(kind, id);
        if (limit == null) return true;
        const bucket = this.data.stock[kind === 'skill' ? 'skills' : 'items'];
        const count = bucket[id] || 0;
        if (kind === 'skill' && limit === 1) {
            if (count >= 1) return false;
            // Repair stale stock data once for older saves, then future drop
            // checks use the O(1) stock counter instead of scanning players.
            if (this.skillOwnedAnywhere(id)) {
                this.stockAdd('skill', id, 1);
                return false;
            }
        }
        return count < limit;
    }
    stockAdd(kind, id, delta) {
        if (this.stockLimit(kind, id) == null) return;
        const bucket = this.data.stock[kind === 'skill' ? 'skills' : 'items'];
        bucket[id] = Math.max(0, (bucket[id] || 0) + delta);
    }

    ledger(delta) {
        const day = vnDate(this.now());
        const row = this.data.ledger[day] || (this.data.ledger[day] = { in: 0, out: 0 });
        if (delta > 0) row.in += delta; else row.out += -delta;
    }

    // ---- nhân vật ---------------------------------------------------------
    register(user, choice) {
        const now = this.now();
        const userId = String(user.id);
        if (this.player(userId)?.registered) fail('Bạn đã đăng ký tu tiên rồi. Mỗi người chỉ đăng ký một lần.');
        const punished = this.data.punished[userId];
        if (punished && now - punished.at < C.RULES.reRegisterWaitMs) {
            const days = Math.ceil((punished.at + C.RULES.reRegisterWaitMs - now) / 86400000);
            fail(`Bạn vừa chịu thiên đạo trừng phạt. Còn ${days} ngày nữa mới được tu luyện lại.`);
        }
        const gender = choice?.gender === 'nu' ? 'nu' : (choice?.gender === 'nam' ? 'nam' : null);
        const mon = C.MON[choice?.mon];
        const he = C.HE[choice?.he];
        if (!gender || !mon || !he) fail('Hãy chọn đủ giới tính, môn võ học và hệ.');
        const appearanceId = String(choice?.appearance || 'thanh_ngoc');
        const appearance = CREATION_APPEARANCES[appearanceId];
        if (!appearance || (appearance.gender && appearance.gender !== gender)) fail('Diện mạo không hợp lệ với giới tính đã chọn.');
        const requestedTalents = Array.isArray(choice?.talents) ? choice.talents : ['dao_the', 'kiem_tam', 'tu_linh'];
        const talents = [...new Set(requestedTalents.map(id => String(id || '')).filter(id => Object.prototype.hasOwnProperty.call(CREATION_TALENT_BONUSES, id)))].slice(0, 3);
        if (talents.length !== 3) fail('Hãy chọn đúng 3 tiên thiên khí vận hợp lệ.');

        const fullName = [user.first_name, user.last_name].filter(Boolean).join(' ').trim() || user.username || 'Đạo hữu';
        const existing = this.player(userId);
        const p = existing || {
            userId, items: [], stones: C.RULES.startStones, safeStorageUntil: 0, equip: { weapon: null, armor: null, acc1: null, acc2: null, ring1: null, ring2: null, phiKiem: null, loDinh: null, nhanTruDo: null, nhanNaDi: null }, inbox: [],
            quick: [null, null], cd: {}, stamina: C.RULES.staminaMax, staminaAt: now, injuredUntil: 0,
            wins: 0, losses: 0, hoiLuc: { day: null, count: 0 }, createdAt: new Date(now).toISOString(),
            town: 'thanh_van', mapId: 'map_1', hp: null, traveling: null, bounties: null, ascended: false,
            duongKhi: 100, amKhi: 100, dailyEscapes: 2, dailyStaminaBought: 0, dailyStaminaDate: '',
        };
        const currentMapId = p.mapId || C.TOWN_BY_ID.get(p.town)?.mapId;
        p.ascended = Boolean(p.ascended || C.MAP_BY_ID.get(currentMapId)?.ascensionRequired);
        p.name = fullName.slice(0, 40);
        p.fullName = fullName;
        p.username = user.username || null;
        if (user.photo_url) p.photoUrl = user.photo_url;
        p.gender = gender;
        p.appearanceId = appearanceId;
        p.appearanceColors = { hair: appearance.hair, outfit: appearance.outfit, eyes: appearance.eyes };
        const look = sanitizeLook(choice?.look, gender);
        if (look) {
            p.look = look.text;
            p.appearanceColors = { hair: look.values.hc || appearance.hair, outfit: (Number(look.values.to) > 0 ? look.values.oc : look.values.tc) || appearance.outfit, eyes: look.values.ec || appearance.eyes };
        }
        p.talents = talents;
        p.mon = mon.id;
        p.roleStats ||= {};
        for (const monId of Object.keys(C.MON)) p.roleStats[monId] ??= 0;
        p.he = he.id;
        p.registered = true;
        p.registeredAt = new Date(now).toISOString();
        p.skills = [];
        p.slots = [null, null, null, null, null];
        p.granted = {};
        let selectedRoot = null;
        if (!p.linhCan) {
            const root = pickWeighted(C.LINH_CAN, l => l.weight, this.rng);
            selectedRoot = root;
            p.linhCan = root.id;
            p.daoTam = root.daoTam ?? 100;
        }

        // 3 kỹ năng khởi đầu: Phổ thông của môn (và kỹ năng chung), 10% một cái là Hiếm.
        const common = C.SKILLS.filter(s => (s.mon === mon.id || s.mon === 'chung') && s.rarity === 'pt' && s.realm === 0 && !s.set);
        let picks = mon.id === 'thu'
            ? [...['thu_an_tran', 'trieu_hoan_linh_thu'].map(id => C.SKILL_BY_ID.get(id)).filter(Boolean), ...shuffle(common.filter(s => s.mon === 'chung'), this.rng).slice(0, 1)]
            : shuffle(common, this.rng).slice(0, 3);
        if (this.rng() < 0.1) {
            const realmNow = this.realmOf(userId).index;
            const rare = C.SKILLS.filter(s => s.mon === mon.id && s.rarity === 'hiem' && !s.set && s.realm <= realmNow && this.canCreate('skill', s.id));
            if (rare.length) picks[2] = rare[Math.floor(this.rng() * rare.length)];
        }
        for (const s of picks) {
            p.skills.push(s.id);
            this.stockAdd('skill', s.id, 1);
        }
        picks.forEach((s, i) => {
            p.slots[i] = s.id;
            p.cd[s.id] = 0; // kỹ năng khởi đầu sẵn sàng ngay
        });

        if (!existing) {
            // Điểm khởi đầu lấy từ bảng linh căn; mọi hệ thống sau đó dùng cùng daoScore.
            p.daoScore = selectedRoot?.daoTam ?? 100;
            p.daoTam = p.daoScore;
            p.daoMaxScore = Math.max(100, Number(p.daoScore) || 0);
            p.maScore = 0;
            p.maKillRuleVersion = 1;
            this.data.players[userId] = p;
            p.beasts ||= [];
            p.hasBeastGourd = true;
            p.items.push({ uid: newId(), kind: 'tool', id: 'ho_lo_thu', name: 'Hồ Lô Thu Thú', icon: '🏺', desc: 'Dùng sau trận thắng để thử thu phục yêu thú.', qty: 1, place: 'bag', at: now, bound: true, unique: true });
            this.addStack(p, 'cons', 'hoi_xuan_dan', 3);
            const potion = p.items.find(it => it.id === 'hoi_xuan_dan');
            if (potion) p.quick[0] = potion.uid;
        }
        this.grantRealmSkills(p);
        this.applyGifts(p);
        this.touch();
        return { player: p, linhCan: this.linhCan(p), skills: picks.map(s => s.name) };
    }

    // Đổi môn: mất toàn bộ tu vi và kỹ năng, giữ vật phẩm và linh thạch.
    resetMon(userId) {
        const p = this.player(userId);
        if (!p?.registered) fail('Bạn chưa đăng ký tu tiên.');
        if (this.activeBattle(userId)) fail('Đang trong trận.');
        for (const id of p.skills) this.stockAdd('skill', id, -1);
        p.skills = [];
        p.slots = [null, null, null, null, null];
        p.granted = {};
        p.registered = false;
        this.realms.reset(userId);
        this.touch();
    }

    maxStamina(p) {
        const realmIndex = p?.userId == null ? 0 : Math.max(0, Math.floor(Number(this.realmOf(p.userId)?.index) || 0));
        return C.RULES.staminaMax + realmIndex * 10;
    }

    syncStamina(p, now = this.now()) {
        if (!p) return false;
        const max = this.maxStamina(p);
        const regen = C.RULES.staminaRegenMs;
        let changed = false;
        const storedStamina = Number(p.stamina);
        const stamina = Number.isFinite(storedStamina)
            ? clamp(Math.floor(storedStamina), 0, max)
            : max;
        if (p.stamina !== stamina) { p.stamina = stamina; changed = true; }

        const storedAt = Number(p.staminaAt);
        let staminaAt = Number.isFinite(storedAt) && storedAt > 0 && storedAt <= now
            ? storedAt
            : now;
        if (p.staminaAt !== staminaAt) { p.staminaAt = staminaAt; changed = true; }

        // While at cap, anchor the next recovery period to the latest server
        // request. The spending action persists this timestamp with its cost.
        if (p.stamina >= max) { p.staminaAt = now; if (changed) this.touch(); return changed; }

        const gained = Math.floor(Math.max(0, now - staminaAt) / regen);
        if (gained > 0) {
            p.stamina = Math.min(max, p.stamina + gained);
            staminaAt += gained * regen;
            if (p.stamina >= max) staminaAt = now;
            p.staminaAt = staminaAt;
            changed = true;
        }
        if (changed) this.touch();
        return changed;
    }

    syncHp(p, now = this.now(), st = null) {
        if (!p) return false;
        st = st || this.stats(p, now);
        const max = st.hp;
        let changed = false;

        if (p.hp == null) {
            p.hp = max;
            p.hpAt = now;
            return false;
        }

        const storedHp = Number(p.hp);
        const hp = Number.isFinite(storedHp) ? clamp(Math.floor(storedHp), 0, max) : max;
        if (p.hp !== hp) { p.hp = hp; changed = true; }

        if (p.hp >= max) {
            if (p.hpAt !== now) { p.hpAt = now; changed = true; }
            if (changed) this.touch();
            return changed;
        }

        const storedAt = Number(p.hpAt);
        let hpAt = Number.isFinite(storedAt) && storedAt > 0 && storedAt <= now ? storedAt : now;
        if (p.hpAt !== hpAt) { p.hpAt = hpAt; changed = true; }

        const regen = C.RULES.hpRegenMs || 60000;
        const gained = Math.floor(Math.max(0, now - hpAt) / regen);
        if (gained > 0) {
            const healPerTick = Math.max(1, Math.round(max * (C.RULES.hpRegenPct || 0.01)));
            p.hp = Math.min(max, p.hp + gained * healPerTick);
            hpAt += gained * regen;
            if (p.hp >= max) hpAt = now;
            p.hpAt = hpAt;
            changed = true;
        }
        if (changed) this.touch();
        return changed;
    }

    unequipIneligibleGear(p) {
        if (!p || !p.equip) return [];
        const realm = this.realmOf(p.userId);
        const realmIndex = realm ? realm.index : 0;
        const pDao = Number(p.daoScore ?? p.daoTam ?? 100);
        const unequipped = [];
        p.items = p.items || [];

        for (const slot of Object.keys(p.equip)) {
            const uid = p.equip[slot];
            if (!uid) continue;
            if (slot === 'phiKiem' && (p.beasts || []).some(beast => beast.id === uid)) continue;
            const item = p.items.find(it => it.uid === uid);
            const def = itemDef(item);
            if (!item || !def) {
                p.equip[slot] = null;
                continue;
            }
            if (item.kind !== 'equip') {
                p.equip[slot] = null;
                item.place = 'kho';
                continue;
            }
            const tier = C.TIER[def.tier];
            const tierRealm = tier ? tier.realm : 0;
            const hasRealm = realmIndex >= tierRealm;
            const hasDao = !def.reqDao || pDao >= def.reqDao;
            const reqMa = requiredMaForEquipment(def);
            const hasMa = !reqMa || (p.maScore || 0) >= reqMa;
            const hasElem = !def.reqElement || p.he === def.reqElement || (p.elemMastery?.[def.reqElement] || 0) >= 15;

            if (!hasRealm || !hasDao || !hasMa || !hasElem) {
                p.equip[slot] = null;
                item.place = 'kho';
                const reason = !hasRealm ? `cảnh giới ${this.realmName(tierRealm)}` :
                               !hasDao ? `Đạo Tâm tối thiểu ${def.reqDao}` :
                               !hasMa ? `Ma Tính tối thiểu ${reqMa}` : `thuộc tính hệ`;
                p.notices = p.notices || [];
                p.notices.push(`📦 [BẢO VẬT TỰ VỀ KHO] Trang bị [${def.name}] (${tier?.name || def.tier} phẩm) yêu cầu ${reason}. Do chưa đủ điều kiện, thiên đạo đã tự chuyển vật phẩm về Kho Tàng và tạm khóa cho tới khi đạo hữu đạt đủ cảnh giới & chỉ số!`);
                unequipped.push({ slot, item, def, reason });
            }
        }
        if (unequipped.length > 0) {
            this.touch();
        }
        return unequipped;
    }

    stats(p, now = this.now()) {
        this.unequipIneligibleGear(p);
        const realm = this.realmOf(p.userId);
        const mon = C.MON[p.mon] || C.MON.kiem;
        const k = Math.pow(C.REALM_GROWTH, realm.index);
        const s = {};
        let equipCritBonus = 0;
        const root = this.linhCan(p);
        for (const key of ['hp', 'mp', 'atk', 'def', 'spd', 'sense']) s[key] = C.BASE_STATS[key] * k * mon.mul[key] * (root?.statMul?.[key] || 1);
        const gear = [];
        p.items = p.items || [];
        p.equip = p.equip || { weapon: null, armor: null, acc1: null, acc2: null, ring1: null, ring2: null, phiKiem: null, loDinh: null, nhanTruDo: null, nhanNaDi: null };
        p.equip.loDinh ??= null;
        p.equip.nhanNaDi ??= null;
        p.equip.ring1 = p.equip.ring1 || null;
        p.equip.ring2 = p.equip.ring2 || null;
        this.migrateTeleportRingSlot(p);
        for (const slot of Object.keys(p.equip)) {
            const item = p.items.find(it => it.uid === p.equip[slot]);
            const def = itemDef(item);
            if (!item || !def) continue;
            let mul = 1;
            const refining = Boolean(item.refinedBy && String(item.refinedBy) !== String(p.userId)) || now < (item.refineAt || 0);
            if (refining) mul *= 0.5;
            if ((item.dur ?? 100) <= 0) mul = 0;
            const pDao = Number(p.daoScore ?? p.daoTam ?? 100);
            if (def.reqDao && pDao < def.reqDao) mul = 0; // chưa đủ đạo tâm: vô hiệu
            const reqMa = requiredMaForEquipment(def);
            if (reqMa && (p.maScore || 0) < reqMa) mul = 0; // chưa đủ ma tính: vô hiệu
            if (def.reqElement && p.he !== def.reqElement && (p.elemMastery?.[def.reqElement] || 0) < 15) mul = 0; // chưa đủ thuộc tính hệ: vô hiệu
            if (def.reqRoleStat && (p.roleStats?.[p.mon] || 0) < def.reqRoleStat) mul = 0; // chưa đủ hệ phái ngộ tính: vô hiệu
            const isMatchWeapon = def.wtype === mon.weapon || (p.mon === 'ren' && (def.wtype === 'bua' || def.wtype === 'trongkhi')) || (p.mon === 'the' && (def.wtype === 'trongkhi' || def.wtype === 'bua'));
            if (def.slot === 'weapon' && !isMatchWeapon) mul *= 0.5;
            if (def.element && def.element === p.he) mul *= 1.1;
            const itemStats = item.stats || def.stats || {};
            for (const [key, value] of Object.entries(itemStats)) {
                if (key === 'crit') equipCritBonus += (Number(value) || 0) * mul;
                else s[key] = (s[key] || 0) + value * mul;
            }
            gear.push({ slot, name: def.name, refining, broken: (item.dur ?? 100) <= 0 });
        }
        for (const talentId of p.talents || []) {
            const bonuses = CREATION_TALENT_BONUSES[talentId];
            if (!bonuses) continue;
            for (const [key, multiplier] of Object.entries(bonuses)) s[key] = (s[key] || 0) * multiplier;
        }
        const daoScore = Number(p.daoScore ?? p.daoTam ?? 100);
        const maScore = Math.max(0, Number(p.maScore) || 0);
        // Đạo Tâm điều hòa sinh cơ/phòng ngự; Ma Tính chuyển hóa sát khí thành công lực.
        const daoCombatBonus = clamp(daoScore, -100, 100) / 1000;
        const maCombatBonus = Math.min(0.30, maScore / 1000);
        s.hp *= 1 + daoCombatBonus;
        s.def *= 1 + daoCombatBonus;
        s.atk *= 1 + maCombatBonus;
        for (const key of Object.keys(s)) s[key] = Math.round(s[key]);
        // Giới hạn an toàn tránh tràn số nhưng không kìm hãm chỉ số từ trang bị (10x base)
        const baseSt = {};
        for (const key of ['hp', 'mp', 'atk', 'def', 'spd', 'sense']) {
            baseSt[key] = Math.round(C.BASE_STATS[key] * k * mon.mul[key]);
            if (s[key] > baseSt[key] * 10) s[key] = Math.round(baseSt[key] * 10);
        }
        if (p.sectId && this.sects[p.sectId]) {
            const sect = this.sects[p.sectId];
            const levelPct = Math.max(1, Number(sect.level) || 1) * 0.01;
            s.atk = Math.round(s.atk * (1 + levelPct + (sect.buff?.atkPct || 0)));
            s.def = Math.round(s.def * (1 + levelPct + (sect.buff?.defPct || 0)));
            s.hp = Math.round(s.hp * (1 + levelPct + (sect.buff?.hpPct || 0)));
            s.spd = Math.round(s.spd * (1 + levelPct + (sect.buff?.spdPct || 0)));
            s.sense = Math.round(s.sense * (1 + levelPct + (sect.buff?.sensePct || 0)));
        }
        if (p.songTuBuffUntil && p.songTuBuffUntil > now) {
            s.atk = Math.round(s.atk * 1.05);
            s.hp = Math.round(s.hp * 1.05);
        }
        if (p.permStats) {
            for (const [key, value] of Object.entries(p.permStats)) {
                if (key === 'crit') continue;
                else s[key] = (s[key] || 0) + value;
            }
        }
        const permCritBonus = Number(p.permStats?.crit) || 0;
        s.crit = clamp(0.05 + s.sense * 0.002 + equipCritBonus + permCritBonus, 0.05, 0.95);
        s.critRate = Math.round(s.crit * 100);
        s.critDmg = clamp(Math.round(150 + (s.sense / (s.sense + 400)) * 150), 150, 300);
        const dodgeBonus = s.dodge || 0;
        const accuracyBonus = s.accuracy || 0;
        s.accuracy = clamp(Math.round(90 + (s.sense / (s.sense + 1000)) * 10 + accuracyBonus), 50, 99);
        s.dodge = clamp(Math.round((s.spd / (s.spd + 300)) * 40 + dodgeBonus), 2, 45);
        s.dmgReduction = clamp(Math.round((s.def / (s.def + 400)) * 60), 0, 75);
        const isThienKieu = THIEN_KIEU_USERS.has(String(p.userId));
        if (isThienKieu) {
            s.atk = Math.round(s.atk * 1.15);
            s.def = Math.round(s.def * 1.15);
            s.hp = Math.round(s.hp * 1.15);
            s.spd = Math.round(s.spd * 1.10);
        }
        // Uy Áp Cảnh Giới: Tu vi càng cao thì linh lực căn cơ càng hùng hậu, tự nhiên áp chế cảnh giới thấp
        const realmBasePower = Math.round(realm.index * 150 + Math.pow(realm.index, 2) * 20);
        s.power = Math.round(s.atk * 2 + s.def * 1.5 + s.hp / 10 + s.spd + s.sense + realmBasePower);
        if (isThienKieu) {
            s.power = Math.round(s.power * 1.15);
        }
        s.isThienKieu = Boolean(isThienKieu);
        s.realmIndex = realm.index;
        s.realmName = realm.name;
        s.gear = gear;
        s.daoScore = Number(p.daoScore ?? p.daoTam ?? 100);
        s.maScore = p.maScore != null ? p.maScore : 0;
        s.isDemon = ((s.maScore || 0) > 0 && (s.maScore || 0) > Math.max(0, s.daoScore)) || Boolean(p.isDemon);
        s.elemMastery = p.elemMastery || { kim: 0, moc: 0, thuy: 0, hoa: 0, tho: 0, loi: 0, phong: 0, bang: 0, thien: 0, ma: 0 };
        return s;
    }

    migrateTeleportRingSlot(p) {
        if (!p?.equip) return false;
        p.equip.nhanNaDi ??= null;
        if (p.equip.nhanNaDi) return false;
        for (const oldSlot of ['ring1', 'ring2']) {
            const uid = p.equip[oldSlot];
            const item = (p.items || []).find(candidate => candidate.uid === uid && candidate.place === 'equip');
            if (!item || !itemDef(item)?.teleportRing) continue;
            p.equip.nhanNaDi = uid;
            p.equip[oldSlot] = null;
            this.touch();
            return true;
        }
        return false;
    }

    // ---- túi, kho ----------------------------------------------------------
    countPlace(p, place) { return (p.items || []).filter(it => it.place === place).length; }
    capacity(place, p = null) {
        if (place !== 'kho') return C.RULES.bagSize;
        let bonus = 0;
        if (p && p.equip && p.equip.nhanTruDo) {
            const ringItem = (p.items || []).find(it => it.uid === p.equip.nhanTruDo);
            if (ringItem) {
                const def = C.EQUIP_BY_ID.get(ringItem.id);
                if (def && def.storageBonus) {
                    bonus = Math.min(200, Number(def.storageBonus) || 0);
                }
            }
        }
        if (p?.sectId) {
            const sect = this.sects[p.sectId];
            if (sect) bonus += Math.min(80, Math.floor((sect.level || 1) / 5) * 20);
        }
        return Math.min(C.RULES.khoSize + 200, C.RULES.khoSize + bonus);
    }

    // Tự sắp xếp túi đồ & hòm đồ: equip (theo tier cao→thấp) → cons → scroll → mat → blueprint/fragment
    mergePlayerStacks(p) {
        if (!p || !Array.isArray(p.items)) return false;
        const stackableKinds = new Set(['cons', 'scroll', 'mat', 'blueprint', 'fragment']);
        const groups = new Map();
        for (const item of p.items) {
            if (!item || !stackableKinds.has(item.kind) || item.place === 'equip') continue;
            const def = itemDef(item);
            if (item.unique || def?.unique) continue;
            const bound = Boolean(item.bound || def?.bound);
            const key = JSON.stringify([item.kind, item.id, item.place, bound]);
            if (!groups.has(key)) groups.set(key, []);
            groups.get(key).push(item);
        }

        const removedUids = new Map();
        const createdItems = [];
        let changed = false;
        for (const group of groups.values()) {
            const firstQty = group[0]?.qty == null ? 1 : Math.floor(Number(group[0].qty) || 0);
            if (group.length < 2 && firstQty > 0 && firstQty <= C.RULES.stackMax) continue;
            const total = group.reduce((sum, item) => {
                const qty = item.qty == null ? 1 : Math.max(0, Math.floor(Number(item.qty) || 0));
                return sum + qty;
            }, 0);
            if (total <= 0) {
                for (const item of group) {
                    if (item.uid) removedUids.set(item.uid, null);
                    changed = true;
                }
                continue;
            }
            const survivorCount = Math.ceil(total / C.RULES.stackMax);
            const survivors = group.slice(0, survivorCount);
            while (survivors.length < survivorCount) {
                const extra = { ...group[0], uid: newId(), qty: 0 };
                survivors.push(extra);
                createdItems.push(extra);
                changed = true;
            }
            let left = total;
            for (const item of survivors) {
                const qty = Math.min(C.RULES.stackMax, left);
                if (Number(item.qty) !== qty) changed = true;
                item.qty = qty;
                left -= qty;
            }
            for (const item of group.slice(survivorCount)) {
                if (item.uid && survivors[0]?.uid) removedUids.set(item.uid, survivors[0].uid);
                changed = true;
            }
        }

        if (removedUids.size) {
            p.items = p.items.filter(item => !removedUids.has(item.uid));
            if (Array.isArray(p.quick)) {
                p.quick = p.quick.map(uid => removedUids.has(uid) ? (removedUids.get(uid) || null) : uid);
            }
        }
        if (createdItems.length) p.items.push(...createdItems);
        return changed;
    }

    sortPlayerItems(p, persist = true) {
        if (!p || !Array.isArray(p.items)) return false;
        const before = p.items.map(item => String(item.uid || '')).join('\u0000');
        const merged = this.mergePlayerStacks(p);
        const placeOrder = { equip: 0, bag: 1, kho: 2, safe: 3 };
        const kindOrder = { equip: 0, cons: 1, scroll: 2, mat: 3, blueprint: 4, fragment: 5 };
        const tierOrder = { tien: 0, thien: 1, dia: 2, huyen: 3, hoang: 4, pham: 5 };
        const rarityOrder = { docban: 0, vang: 1, cam: 2, tt: 3, cuchiem: 4, hiem: 5, pt: 6 };
        const equippedSlotOrder = { weapon: 0, armor: 1, acc1: 2, acc2: 3, ring1: 4, ring2: 5, phiKiem: 6, loDinh: 7, nhanTruDo: 8, nhanNaDi: 9 };
        const equippedSlotByUid = new Map(Object.entries(p.equip || {}).filter(([, uid]) => uid).map(([slot, uid]) => [uid, slot]));
        p.items.sort((a, b) => {
            const pa = placeOrder[a.place] ?? 9;
            const pb = placeOrder[b.place] ?? 9;
            if (pa !== pb) return pa - pb;
            const ka = kindOrder[a.kind] ?? 9;
            const kb = kindOrder[b.kind] ?? 9;
            if (ka !== kb) return ka - kb;
            if (a.kind === 'equip' && a.place === 'equip' && b.place === 'equip') {
                const sa = equippedSlotOrder[equippedSlotByUid.get(a.uid)] ?? 99;
                const sb = equippedSlotOrder[equippedSlotByUid.get(b.uid)] ?? 99;
                if (sa !== sb) return sa - sb;
            }
            const da = itemDef(a) || {}, db = itemDef(b) || {};
            const ta = tierOrder[da.tier] ?? 9;
            const tb = tierOrder[db.tier] ?? 9;
            if (ta !== tb) return ta - tb;
            const ra = rarityOrder[da.rarity] ?? 9;
            const rb = rarityOrder[db.rarity] ?? 9;
            if (ra !== rb) return ra - rb;
            const realmDiff = (Number(db.realm) || 0) - (Number(da.realm) || 0);
            if (realmDiff) return realmDiff;
            return itemName(a).localeCompare(itemName(b), 'vi') || (a.id || '').localeCompare(b.id || '');
        });
        const after = p.items.map(item => String(item.uid || '')).join('\u0000');
        const changed = merged || before !== after;
        if (changed && persist) this.touch();
        return changed;
    }

    addEquip(p, def, now = this.now()) {
        const bagFull = this.countPlace(p, 'bag') >= this.capacity('bag', p);
        const khoFull = this.countPlace(p, 'kho') >= this.capacity('kho', p);
        if (bagFull && khoFull) return null;
        if (!this.canCreate('item', def.id)) return null;
        const place = bagFull ? 'kho' : 'bag';
        const item = {
            uid: newId(),
            kind: 'equip',
            id: def.id,
            qty: 1,
            place,
            dur: 100,
            refinedBy: String(p.userId),
            refineAt: 0,
            at: now,
            stats: def.stats ? { ...def.stats } : undefined,
            tierName: def.tierName || undefined,
        };
        p.items.push(item);
        this.stockAdd('item', def.id, 1);
        return item;
    }

    addStack(p, kind, id, qty, place = 'bag') {
        const compacted = this.mergePlayerStacks(p);
        let left = qty;
        for (const item of p.items) {
            if (left <= 0) break;
            if (item.kind === kind && item.id === id && item.place === place && item.qty < C.RULES.stackMax) {
                const add = Math.min(left, C.RULES.stackMax - item.qty);
                item.qty += add;
                left -= add;
            }
        }
        while (left > 0 && this.countPlace(p, place) < this.capacity(place, p)) {
            const add = Math.min(left, C.RULES.stackMax);
            p.items.push({ uid: newId(), kind, id, qty: add, place, at: this.now() });
            left -= add;
        }
        const added = qty - left;
        const sorted = this.sortPlayerItems(p, false);
        if (compacted || added > 0 || sorted) this.touch();
        return added;
    }

    consume(p, item, qty) {
        item.qty -= qty;
        if (item.qty <= 0) {
            p.items = p.items.filter(it => it !== item);
            const itemBound = Boolean(item.bound || itemDef(item)?.bound);
            const replacement = p.items.find(it => it.kind === item.kind && it.id === item.id && it.place === item.place
                && Boolean(it.bound || itemDef(it)?.bound) === itemBound
                && (it.qty == null || Number(it.qty) > 0));
            p.quick = (p.quick || []).map(q => (q === item.uid ? (replacement?.uid || null) : q));
        }
        this.touch();
    }

    findItem(p, itemUid) {
        const item = p.items.find(it => it.uid === itemUid);
        if (!item) fail('Không tìm thấy vật phẩm.');
        return item;
    }

    marketStackItems(p, item) {
        const stackableKinds = new Set(['cons', 'scroll', 'mat', 'blueprint', 'fragment']);
        if (!item || !stackableKinds.has(item.kind) || item.unique || itemDef(item)?.unique) return [item].filter(Boolean);
        const bound = Boolean(item.bound || itemDef(item)?.bound);
        return p.items.filter(candidate => candidate.kind === item.kind && candidate.id === item.id
            && ['bag', 'kho'].includes(candidate.place)
            && !candidate.unique && !itemDef(candidate)?.unique
            && Boolean(candidate.bound || itemDef(candidate)?.bound) === bound);
    }

    stackQuantity(items) {
        return items.reduce((sum, item) => sum + Math.max(1, Math.floor(Number(item.qty) || 1)), 0);
    }

    removeStackQuantity(p, items, qty) {
        let remaining = qty;
        const depleted = [];
        for (const stack of items) {
            if (remaining <= 0) break;
            const count = Math.max(1, Math.floor(Number(stack.qty) || 1));
            const removed = Math.min(count, remaining);
            stack.qty = count - removed;
            remaining -= removed;
            if (stack.qty <= 0) depleted.push(stack);
        }
        if (remaining > 0) fail('Số lượng vật phẩm không còn đủ, hãy tải lại hành trang.');
        if (depleted.length) {
            const depletedUids = new Set(depleted.map(item => item.uid));
            p.items = p.items.filter(item => !depletedUids.has(item.uid));
            p.quick = (p.quick || []).map(uid => {
                if (!depletedUids.has(uid)) return uid;
                const old = depleted.find(item => item.uid === uid);
                const replacement = p.items.find(item => item.kind === old.kind && item.id === old.id
                    && item.place === old.place && Boolean(item.bound || itemDef(item)?.bound) === Boolean(old.bound || itemDef(old)?.bound));
                return replacement?.uid || null;
            });
        }
    }

    expireSafeStorage(p, now = this.now()) {
        if (!p || !Array.isArray(p.items)) return false;
        const until = Number(p.safeStorageUntil) || 0;
        if (until > now) return false;
        const safeItems = p.items.filter(item => item.place === 'safe');
        if (!until && safeItems.length === 0) return false;
        for (const item of safeItems) item.place = 'kho';
        p.safeStorageUntil = 0;
        if (safeItems.length) {
            p.notices = p.notices || [];
            p.notices.push(`🔓 Hết hạn Kho An Toàn: ${safeItems.length} ô vật phẩm đã chuyển về Kho Động Phủ thường và có thể bị rơi khi thua trận.`);
        }
        this.touch();
        return true;
    }

    rentSafeStorage(userId) {
        const p = this.requirePlayer(userId);
        this.requireIdle(p);
        const now = this.now();
        if (now < (p.injuredUntil || 0)) fail('Đang trọng thương, chưa thể thuê Kho An Toàn.');
        const cost = C.RULES.safeStorageMonthlyCost;
        if ((p.stones || 0) < cost) fail(`Cần ${cost.toLocaleString('vi-VN')} linh thạch để thuê Kho An Toàn trong 30 ngày.`);
        p.stones -= cost;
        this.ledger(-cost);
        p.safeStorageUntil = Math.max(now, Number(p.safeStorageUntil) || 0) + C.RULES.safeStorageMonthMs;
        this.touch();
        return { cost, until: p.safeStorageUntil, slots: C.RULES.safeStorageSlots };
    }

    getDroppableItems(p) {
        if (!p || !Array.isArray(p.items)) return [];
        this.expireSafeStorage(p);
        const equippedUids = new Set(Object.values(p.equip || {}).filter(Boolean));
        const droppable = [];
        for (const item of p.items) {
            if (!item || !['bag', 'kho'].includes(item.place) || item.bound || itemDef(item)?.bound || equippedUids.has(item.uid)) continue;
            const qty = item.qty == null ? 1 : clamp(Math.floor(Number(item.qty) || 0), 0, C.RULES.stackMax);
            for (let i = 0; i < qty; i += 1) droppable.push(item);
        }
        return droppable;
    }

    takeDroppableItem(p, item) {
        if (!p || !Array.isArray(p.items) || !item || !p.items.includes(item)) return null;
        if (!['bag', 'kho'].includes(item.place) || item.bound || itemDef(item)?.bound || Object.values(p.equip || {}).includes(item.uid)) return null;
        const qty = item.qty == null ? 1 : clamp(Math.floor(Number(item.qty) || 0), 0, C.RULES.stackMax);
        if (qty <= 0) return null;
        const dropped = { ...item, qty: 1, place: 'bag' };
        if (qty > 1) item.qty = qty - 1;
        else {
            p.items = p.items.filter(entry => entry !== item);
            p.quick = (p.quick || []).map(uid => uid === item.uid ? null : uid);
        }
        return dropped;
    }

    applyHeavenPunishmentPenalty(p, summaryNotes = null) {
        if (!p) return { lostExp: 0, lostItem: null };

        const currentExp = Math.max(0, Number(this.realms.get(p.userId)?.experience) || 0);
        const requestedExpLoss = currentExp > 0 ? Math.max(20, Math.round(currentExp * 0.02)) : 0;
        const lostExp = requestedExpLoss > 0 ? this.realms.loseExp(p.userId, requestedExpLoss) : 0;

        // Thiên Đạo thu một vật phẩm ngẫu nhiên trong túi/kho thường; bảo vật
        // đang mặc, vật phẩm khóa và Kho An Toàn giữ nguyên như các trận khác.
        const droppable = this.getDroppableItems(p);
        let lostItem = null;
        if (droppable.length > 0) {
            const item = droppable[Math.min(droppable.length - 1, Math.floor(this.rng() * droppable.length))];
            lostItem = this.takeDroppableItem(p, item);
            if (lostItem) {
                const heaven = this.data.worldNpcs?.npc_thien_dao;
                if (heaven) {
                    heaven.bag ||= [];
                    heaven.bag.push(lostItem);
                }
            }
        }

        if (lostExp > 0) this.checkInjuryRealmDrop(p.userId, summaryNotes);
        return { lostExp, lostItem };
    }

    requireIdle(p) {
        if (this.activeBattle(p.userId)) fail('Đang trong trận, chưa thao tác được.');
    }

    requirePlayer(userId) {
        const p = this.player(userId);
        if (!p?.registered) fail('Bạn chưa đăng ký tu tiên.');
        p.safeStorageUntil = Number(p.safeStorageUntil) || 0;
        this.expireSafeStorage(p);
        if (p.daoScore == null) p.daoScore = Number(p.daoTam) || 100;
        if (p.maScore == null) p.maScore = 0;
        if (!p.elemMastery) {
            p.elemMastery = { kim: 0, moc: 0, thuy: 0, hoa: 0, tho: 0, loi: 0, phong: 0, bang: 0, thien: 0, ma: 0 };
            if (p.he) p.elemMastery[p.he] = 20;
        }
        if (p.isRoaming == null) p.isRoaming = false;
        return p;
    }

    move(userId, itemUid, to) {
        const p = this.requirePlayer(userId);
        this.requireIdle(p);
        if (!['bag', 'kho', 'safe'].includes(to)) fail('Chỗ để không hợp lệ.');
        const now = this.now();
        if (now < (p.injuredUntil || 0)) fail('Đang trọng thương, không chuyển đồ được.');
        const item = this.findItem(p, itemUid);
        if (item.place === 'equip') fail('Hãy tháo trang bị trước.');
        if (item.place === to) return;
        const def = itemDef(item);
        if (to === 'safe' && (Number(p.safeStorageUntil) || 0) <= now) fail('Kho An Toàn chưa thuê hoặc đã hết hạn. Hãy thuê 30 ngày để mở 10 ô an toàn.');
        if (to === 'kho' && def?.unique) fail('Độc bản không cất vào kho được.');
        const limit = to === 'safe' ? C.RULES.safeStorageSlots : this.capacity(to, p);
        if (this.countPlace(p, to) >= limit) fail(to === 'safe' ? 'Kho An Toàn đã đủ 10 ô.' : (to === 'kho' ? 'Kho đã đầy.' : 'Túi đã đầy.'));
        item.place = to;
        if (to !== 'bag') p.quick = p.quick.map(q => (q === item.uid ? null : q));
        this.touch();
    }

    equip(userId, itemUid, slotWanted) {
        const p = this.requirePlayer(userId);
        this.requireIdle(p);
        const now = this.now();
        const item = this.findItem(p, itemUid);
        if (item.kind !== 'equip') fail('Vật phẩm này không trang bị được.');
        if (item.place !== 'bag') fail('Chỉ mặc được đồ đang ở trong túi.');
        const def = itemDef(item);
        const tier = C.TIER[def.tier];
        const realm = this.realmOf(userId);
        if (realm.index < tier.realm) fail(`Cần cảnh giới ${this.realmName(tier.realm)} để dùng đồ ${tier.name} phẩm.`);
        if (def.reqDao && (p.daoScore || 0) < def.reqDao) {
            fail(`Bảo vật này yêu cầu Đạo Tâm tối thiểu ${def.reqDao} (hiện có: ${p.daoScore || 0}). Tâm tính không phù hợp không thể ngự dụng!`);
        }
        const reqMa = requiredMaForEquipment(def);
        if (reqMa && (p.maScore || 0) < reqMa) {
            fail(`Ma binh này yêu cầu Ma Tính tối thiểu ${reqMa} (hiện có: ${p.maScore || 0}). Người chính đạo không thể khống chế!`);
        }
        if (def.reqElement && p.he !== def.reqElement && (p.elemMastery?.[def.reqElement] || 0) < 15) {
            fail(`Trang bị này yêu cầu thuộc tính hệ [${C.HE[def.reqElement]?.name || def.reqElement}] hoặc thông thạo hệ từ 15 điểm trở lên!`);
        }
        if (def.reqRoleStat && (p.roleStats?.[p.mon] || 0) < def.reqRoleStat) {
            fail(`${C.MON[p.mon].statName} cần đạt ${def.reqRoleStat} điểm mới có thể trang bị vật phẩm này.`);
        }
        let slot = def.slot;
        if (def.teleportRing) slot = 'nhanNaDi';
        else if (def.slot === 'phi_kiem' || def.slot === 'toa_ky' || def.slot === 'toaKy') slot = 'phiKiem';
        else if (def.slot === 'lo_dinh' || def.slot === 'loDinh') slot = 'loDinh';
        else if (def.slot === 'nhan_tru_do') slot = 'nhanTruDo';
        else if (def.slot === 'acc') slot = slotWanted === 'acc2' ? 'acc2' : (slotWanted === 'acc1' ? 'acc1' : (p.equip.acc1 ? (p.equip.acc2 ? 'acc1' : 'acc2') : 'acc1'));
        else if (def.slot === 'vong' || def.slot === 'ring' || def.slot === 'bracelet') {
            slot = slotWanted === 'ring2' ? 'ring2' : (slotWanted === 'ring1' ? 'ring1' : (p.equip.ring1 ? (p.equip.ring2 ? 'ring1' : 'ring2') : 'ring1'));
        }
        const previous = p.equip[slot];
        if (previous) {
            const old = p.items.find(it => it.uid === previous);
            if (old) old.place = 'bag';
        }
        item.place = 'equip';
        p.equip[slot] = item.uid;
        p.quick = p.quick.map(q => (q === item.uid ? null : q));
        let refine = null;
        if (item.refinedBy !== String(p.userId)) {
            item.refinedBy = String(p.userId);
            item.refineAt = now + tier.refine * 1000;
            refine = item.refineAt;
        }
        this.touch();
        return { slot, refineAt: refine };
    }

    unequip(userId, slot) {
        const p = this.requirePlayer(userId);
        this.requireIdle(p);
        if (!(slot in p.equip)) fail('Ô không hợp lệ.');
        const itemUid = p.equip[slot];
        if (!itemUid) return;
        if (slot === 'phiKiem' && (p.beasts || []).some(beast => beast.id === itemUid)) {
            p.equip.phiKiem = null;
            this.touch();
            return;
        }
        if (this.countPlace(p, 'bag') >= C.RULES.bagSize) fail('Túi đầy, cất bớt vào kho trước.');
        const item = p.items.find(it => it.uid === itemUid);
        if (item) item.place = 'bag';
        p.equip[slot] = null;
        this.touch();
    }

    mountBeast(userId, beastId) {
        const p = this.requirePlayer(userId);
        this.requireIdle(p);
        const beast = (p.beasts || []).find(candidate => candidate.id === beastId);
        if (!beast) fail('Không tìm thấy linh thú đã thu phục.');
        const previous = p.equip.phiKiem;
        if (previous && previous !== beast.id && !(p.beasts || []).some(candidate => candidate.id === previous)) {
            if (this.countPlace(p, 'bag') >= this.capacity('bag', p)) fail('Túi đầy, không thể cất phi kiếm đang trang bị.');
            const old = p.items.find(it => it.uid === previous);
            if (old) old.place = 'bag';
        }
        p.equip.phiKiem = beast.id;
        this.touch();
        return { success: true, message: `Đã gọi ${beast.name} vào ô Phi kiếm / Tọa kỵ.` };
    }

    setQuick(userId, index, itemUid) {
        const p = this.requirePlayer(userId);
        this.requireIdle(p);
        const i = Number(index);
        if (!(i >= 0 && i < C.RULES.quickSlots)) fail('Ô không hợp lệ.');
        if (itemUid) {
            const item = this.findItem(p, itemUid);
            const def = itemDef(item);
            if (item.kind !== 'cons' || !def?.battle || item.place !== 'bag') fail('Chỉ đan dược, phù lục dùng trong trận và đang ở trong túi.');
        }
        p.quick[i] = itemUid || null;
        this.touch();
    }

    setSkillSlot(userId, index, skillId) {
        const p = this.requirePlayer(userId);
        this.requireIdle(p);
        const i = Number(index);
        if (!(i >= 0 && i < C.RULES.skillSlots)) fail('Ô không hợp lệ.');
        const now = this.now();
        if (!skillId) { p.slots[i] = null; this.touch(); return; }
        const skill = C.SKILL_BY_ID.get(skillId);
        if (!skill || !p.skills.includes(skillId)) fail('Bạn chưa học kỹ năng này.');
        const realmIndex = this.realmOf(userId).index;
        if (!this.slotUnlocked(realmIndex, i)) fail(`Ô ${i + 1} mở khi đạt ${this.realmName(C.RULES.skillSlotRealms[i])}.`);
        if (skill.realm > realmIndex) fail(`Cần cảnh giới ${this.realmName(skill.realm)} mới dùng được ${skill.name}.`);
        const reqMa = requiredMaForSkill(skill);
        if (reqMa && (p.maScore || 0) < reqMa) fail(`${skill.name} yêu cầu Ma Tính tối thiểu ${reqMa} (hiện có: ${p.maScore || 0}).`);
        if (skill.reqDao && Number(p.daoScore ?? p.daoTam ?? 100) < skill.reqDao) fail(`${skill.name} yêu cầu Đạo Tâm tối thiểu ${skill.reqDao} (hiện có: ${p.daoScore ?? p.daoTam ?? 100}).`);
        if (p.slots.includes(skillId)) fail('Kỹ năng đã được lắp.');
        const next = p.slots.slice();
        next[i] = skillId;
        const defs = next.map(id => C.SKILL_BY_ID.get(id)).filter(Boolean);
        if (defs.filter(s => s.big).length > C.RULES.maxBigSkills) fail('Tối đa 2 chiêu lớn cùng lúc.');
        if (defs.filter(s => s.kind === 'escape').length > C.RULES.maxEscapeSkills) fail('Tối đa 1 kỹ năng giải khống cùng lúc.');
        if (skill.mon !== 'chung' && skill.mon !== p.mon) fail(`Kỹ năng ${skill.name} chỉ dành cho ${C.MON[skill.mon]?.name || 'môn phái tương ứng'}.`);
        if (skill.reqRoleStat && (p.roleStats?.[p.mon] || 0) < skill.reqRoleStat) fail(`${C.MON[p.mon].statName} cần đạt ${skill.reqRoleStat} điểm để lắp ${skill.name}.`);
        p.slots = next;
        // Kỹ năng vừa lắp bắt đầu với hồi chiêu đầy; gỡ ra lắp lại không xóa hồi chiêu đang chạy.
        p.cd[skillId] = Math.max(p.cd[skillId] || 0, now + skill.cd * 1000);
        this.touch();
    }

    learn(userId, itemUid) {
        const p = this.requirePlayer(userId);
        this.requireIdle(p);
        const item = p.items.find(it => it.uid === itemUid || (it.kind === 'scroll' && it.id === itemUid));
        if (!item) fail('Không tìm thấy ngọc giản trong hành trang.');
        if (item.kind !== 'scroll') fail('Vật phẩm này không phải ngọc giản công pháp.');
        const skill = C.SKILL_BY_ID.get(item.id);
        if (!skill) fail('Tuyệt kỹ không tồn tại trong danh mục thiên địa.');
        // Nghiền ngẫm ngọc giản đã học để nhận EXP và Linh Thạch
        if (p.skills.includes(skill.id)) {
            this.consume(p, item, 1);
            const studyExp = Math.round((skill.realm + 1) * 350);
            const studyStones = Math.round((skill.realm + 1) * 50);
            this.realms.addExp(userId, studyExp);
            p.stones = (p.stones || 0) + studyStones;
            this.touch();
            return {
                name: skill.name,
                studied: true,
                exp: studyExp,
                stones: studyStones,
                message: `Đã nghiền ngẫm ngọc giản [${skill.name}], thu hoạch tinh hoa: +${studyExp} EXP và +${studyStones} Linh Thạch!`
            };
        }

        if (skill.mon !== 'chung' && skill.mon !== p.mon) fail(`Ngọc giản này chỉ dành cho ${C.MON[skill.mon]?.name || 'môn phái tương ứng'}.`);
        if (skill.reqRoleStat && (p.roleStats?.[p.mon] || 0) < skill.reqRoleStat) fail(`${C.MON[p.mon].statName} cần đạt ${skill.reqRoleStat} điểm mới lĩnh ngộ được ${skill.name}.`);
        const realm = this.realmOf(userId);
        if (realm.index < skill.realm) fail(`Cần đạt cảnh giới [${this.realmName(skill.realm)}] mới có thể lĩnh ngộ tuyệt kỹ này.`);

        if (skill.reqDao && (p.daoScore || 0) < skill.reqDao) {
            fail(`Bí kíp chính đạo này yêu cầu Đạo Tâm tối thiểu ${skill.reqDao} (hiện có: ${p.daoScore || 0}) mới có thể lĩnh ngộ!`);
        }
        const reqMa = requiredMaForSkill(skill);
        if (reqMa > 0 && (p.maScore || 0) < reqMa) {
            fail(`Ma công cao cấp này yêu cầu Ma Tính tối thiểu ${reqMa} (hiện có: ${p.maScore || 0}). Hãy tu luyện ma công nhập môn hoặc đồ sát dã ngoại để tăng ma tính!`);
        }
        if (skill.element && p.he !== skill.element && (p.elemMastery?.[skill.element] || 0) < 15) {
            fail(`Kỹ năng này yêu cầu thuộc tính hệ [${C.HE[skill.element]?.name || skill.element}] hoặc thông thạo hệ từ 15 điểm.`);
        }
        this.consume(p, item, 1);
        p.skills.push(skill.id);
        // Tự động trang bị vào ô chiêu thức trống nếu có
        const openSlotIndex = p.slots.findIndex((s, idx) => !s && (realm.index >= (C.RULES.skillSlotRealms[idx] || 0)));
        if (openSlotIndex !== -1) {
            p.slots[openSlotIndex] = skill.id;
        }
        this.touch();
        return skill.name;
    }

    // ---- cửa hàng -----------------------------------------------------------
    buy(userId, id, qty = 1) {
        const p = this.requirePlayer(userId);
        this.requireIdle(p);
        const now = this.now();
        const rareMaterialPrices = { mat_huyet_tinh: 200_000, mat_long_lan: 300_000 };
        if (Object.hasOwn(rareMaterialPrices, id)) {
            if (Number(qty) !== 1) fail('Mỗi lần chỉ được mua đúng 1 nguyên liệu hiếm.');
            const today = vnDate(now);
            if (p.dailyRareBoughtDate !== today) {
                p.dailyRareBoughtDate = today;
                p.dailyRareBought = {};
            }
            p.dailyRareBought = p.dailyRareBought || {};
            if (p.dailyRareBought[id]) fail('Hôm nay đã hết hàng nguyên liệu này.');
            const price = rareMaterialPrices[id];
            if (p.stones < price) fail('Không đủ linh thạch.');
            const added = this.addStack(p, 'mat', id, 1);
            if (added !== 1) fail('Túi đã đầy.');
            p.stones -= price;
            p.dailyRareBought[id] = true;
            this.ledger(-price);
            this.touch();
            return 1;
        }
        const def = C.CONSUMABLE_BY_ID.get(id);
        const n = Math.floor(Number(qty));
        if (!def || !(n >= 1 && n <= 99)) fail('Món không hợp lệ.');
        const cost = Math.ceil(def.price * 0.8) * n;
        if (p.stones < cost) fail('Không đủ linh thạch.');
        if (def.stamina) {
            const today = vnDate(now);
            if (p.dailyStaminaDate !== today) {
                p.dailyStaminaDate = today;
                p.dailyStaminaBought = 0;
            }
            const maxDaily = C.RULES.maxDailyStaminaItems || 100;
            if ((p.dailyStaminaBought || 0) + n > maxDaily) {
                fail(`Mỗi ngày chỉ được mua tối đa ${maxDaily} vật phẩm hồi thể lực (hôm nay đã mua ${p.dailyStaminaBought || 0}).`);
            }
            p.dailyStaminaBought = (p.dailyStaminaBought || 0) + n;
        }
        const added = this.addStack(p, 'cons', id, n);
        if (added <= 0) fail('Túi đã đầy.');
        const unitCost = Math.ceil(def.price * 0.8);
        p.stones -= unitCost * added;
        this.ledger(-unitCost * added);
        this.touch();
        return added;
    }

    sellPrice(item) {
        const def = itemDef(item);
        if (!def) return 0;
        if (item.kind === 'equip') return def.bound ? 1 : Math.max(1, Math.round(C.TIER[def.tier].sell * (0.5 + (item.dur ?? 100) / 200)));
        if (item.kind === 'scroll') return SCROLL_PRICE[def.rarity] || 10;
        if (item.kind === 'cons') return Math.max(1, Math.floor(def.price / 2));
        return 0;
    }

    sell(userId, itemUid, qty = 1) {
        const p = this.requirePlayer(userId);
        this.requireIdle(p);
        const item = this.findItem(p, itemUid);
        if (item.place === 'equip') fail('Hãy tháo trang bị trước khi bán.');
        if (item.place === 'safe') fail('Hãy lấy vật phẩm khỏi Kho An Toàn trước khi bán.');
        const stacks = this.marketStackItems(p, item);
        const available = this.stackQuantity(stacks);
        const requested = qty == null ? 1 : Number(qty);
        if (!Number.isInteger(requested) || requested < 1 || requested > available) fail(`Số lượng không hợp lệ. Hiện có ${available} món cùng loại trong túi và kho.`);
        const n = requested;
        const gain = this.sellPrice(item) * n;
        if (item.kind === 'equip') this.stockAdd('item', item.id, -1);
        if (item.kind === 'scroll') this.stockAdd('skill', item.id, -n);
        this.removeStackQuantity(p, stacks, n);
        p.stones += gain;
        this.ledger(gain);
        this.touch();
        return gain;
    }

    repair(userId, itemUid) {
        const p = this.requirePlayer(userId);
        this.requireIdle(p);
        const item = this.findItem(p, itemUid);
        if (item.kind !== 'equip') fail('Chỉ sửa được trang bị.');
        if ((Number(item.dur ?? 100)) <= 0) {
            this.discardBrokenEquipment(p, item);
            this.touch();
            fail('Trang bị đã vỡ do độ bền về 0 và biến mất, không thể sửa chữa.');
        }
        const def = itemDef(item);
        const missing = 100 - (item.dur ?? 100);
        if (missing <= 0) fail('Trang bị còn nguyên độ bền.');
        const cost = equipmentRepairCost(def, item.dur);
        if (p.stones < cost) fail(`Cần ${cost} linh thạch.`);
        p.stones -= cost;
        item.dur = 100;
        this.ledger(-cost);
        this.touch();
        return cost;
    }

    discardBrokenEquipment(p, item) {
        if (!p || !item) return;
        p.items = (p.items || []).filter(candidate => candidate.uid !== item.uid);
        p.quick = (p.quick || []).map(uid => uid === item.uid ? null : uid);
        for (const slot of Object.keys(p.equip || {})) {
            if (p.equip[slot] === item.uid) p.equip[slot] = null;
        }
        this.stockAdd('item', item.id, -1);
    }

    wearEquippedDurability(p, amountForItem = () => 1) {
        const affected = [];
        const broken = [];
        const seen = new Set();
        for (const uid of Object.values(p.equip || {})) {
            if (!uid || seen.has(uid)) continue;
            seen.add(uid);
            const item = (p.items || []).find(candidate => candidate.uid === uid && candidate.kind === 'equip');
            if (!item) continue;
            const amount = Math.max(1, Math.floor(Number(amountForItem(item)) || 1));
            item.dur = Math.max(0, clamp(Number(item.dur ?? 100), 0, 100) - amount);
            const def = itemDef(item);
            const name = def?.name || item.id;
            if (item.dur <= 0) {
                this.discardBrokenEquipment(p, item);
                broken.push(name);
            } else {
                affected.push(`${name} −${amount} (${item.dur}/100)`);
            }
        }
        return { affected, broken };
    }

    injuryDurabilityLoss(item) {
        const def = itemDef(item);
        const rank = Math.max(0, Number(C.TIER[def?.tier]?.rank) || 0);
        return Math.min(10, 5 + rank);
    }

    durabilitySummary(wear, label) {
        if (!wear || (!wear.affected.length && !wear.broken.length)) return '';
        const parts = [...wear.affected, ...wear.broken.map(name => `${name} vỡ và biến mất`)];
        return `${label}: ${parts.join('; ')}.`;
    }

    use(userId, itemUid) {
        const p = this.requirePlayer(userId);
        this.requireIdle(p);
        const item = this.findItem(p, itemUid);
        if (!item) fail('Không tìm thấy vật phẩm.');
        if (item.kind !== 'cons') fail('Chỉ có thể dùng vật phẩm tiêu hao.');
        const def = itemDef(item);
        if (!def) fail('Vật phẩm này không thể sử dụng trực tiếp.');
        const now = this.now();
        const day = vnDate(now);
        const st = this.stats(p, now);

        if (def.recipeId && def.recipeType) {
            const recipeTables = {
                equip: C.CRAFT_EQUIP_RECIPE_BY_ID,
                potion: C.CRAFT_POTION_RECIPE_BY_ID,
                talisman: C.CRAFT_TALISMAN_RECIPE_BY_ID,
            };
            if (!recipeTables[def.recipeType]?.has(def.recipeId)) fail('Bản thảo không còn công thức tương ứng.');
            p.learnedRecipes ||= {};
            if (p.learnedRecipes[def.recipeId]) fail(`Bạn đã lĩnh ngộ công thức [${def.name}] rồi.`);
            p.learnedRecipes[def.recipeId] = true;
            this.consume(p, item, 1);
            this.touch();
            return { success: true, message: `📜 Đã lĩnh ngộ ${def.name}. Công thức đã được mở trong Xưởng Chế Tác.` };
        }

        // Âm/Dương Khí pills — special case before general stamina check
        if (def.id === 'dan_bo_duong') {
            p.duongKhi = Math.min(100, (p.duongKhi || 0) + 50);
            this.consume(p, item, 1);
            this.touch();
            return { success: true, message: `Dương Khí hồi phục 50 điểm (hiện ${p.duongKhi}).` };
        }
        if (def.id === 'dan_duong_am') {
            p.amKhi = Math.min(100, (p.amKhi || 0) + 50);
            this.consume(p, item, 1);
            this.touch();
            return { success: true, message: `Âm Khí hồi phục 50 điểm (hiện ${p.amKhi}).` };
        }

        let msg = '';
        let tracker = null;
        if (def.locateMonster) {
            tracker = this.locateMonsterAndDungeon(p, now);
            msg = 'Đã tìm thấy dấu vết yêu thú và Cổ Động.';
        } else if (def.stamina) {
            if (p.hoiLuc?.day !== day) p.hoiLuc = { day, count: 0 };
            const maxDailyUses = def.dailyMax || C.RULES.maxDailyStaminaUses || 8;
            if (p.hoiLuc.count >= maxDailyUses) fail(`Mỗi ngày chỉ được dùng tối đa ${maxDailyUses} lần tăng thể lực (hôm nay đã dùng ${p.hoiLuc.count} lần).`);
            this.syncStamina(p, now);
            const staminaCap = this.maxStamina(p);
            if (p.stamina >= staminaCap) fail(`Thể lực đã đạt giới hạn tích trữ (${p.stamina}/${staminaCap}), hãy tiêu hao bớt trước khi dùng thêm.`);
            p.stamina = Math.min(staminaCap, p.stamina + def.stamina);
            p.hoiLuc.count += 1;
            msg = `Đã dùng ${def.name}, hồi phục +${def.stamina} thể lực (hiện có ${p.stamina}/${this.maxStamina(p)}). Hôm nay đã dùng ${p.hoiLuc.count}/${maxDailyUses} lần.`;
        } else if (def.heal || def.cleanse) {
            if (def.cleanse) {
                p.injuredUntil = 0;
                p.hp = st.hp;
                msg = `Đã dùng ${def.name}, xóa bỏ trọng thương và phục hồi hoàn toàn Khí Huyết!`;
            } else if (def.heal) {
                const cur = p.hp == null ? st.hp : p.hp;
                const healAmt = Math.round(st.hp * def.heal);
                p.hp = Math.min(st.hp, cur + healAmt);
                const pct = Math.round((def.heal || 0) * 100);
                msg = `Đã dùng ${def.name}, hồi phục +${pct}% Khí Huyết (+${healAmt.toLocaleString('vi-VN')}, hiện có: ${p.hp.toLocaleString('vi-VN')}/${st.hp.toLocaleString('vi-VN')}).`;
            }
        } else if (def.exp || def.id === 'tay_tuy_dan') {
            let expGain = Math.max(1, Number(def.exp) || 0);
            if (def.id === 'tay_tuy_dan') {
                const curRealm = this.realmOf(p.userId);
                expGain = Math.max(1, Math.round((curRealm?.levelCap || 1000) * 0.10));
            } else {
                expGain = Math.min(5000, expGain);
            }
            const expResult = this.realms.addExp(p.userId, expGain);
            const gainedExp = Number.isFinite(expResult?.gained) ? expResult.gained : expGain;
            if (gainedExp <= 0) fail('Đan dược không thể tăng tu vi lúc này vì mốc tiểu cảnh đã đầy.');
            msg = def.id === 'tay_tuy_dan'
                ? `Đã dùng ${def.name}, nhận +${Number(gainedExp).toLocaleString('vi-VN')} EXP tu vi (tối đa ~10% mốc tiểu cảnh)!`
                : `Đã dùng ${def.name}, nhận +${Number(gainedExp).toLocaleString('vi-VN')} EXP tu vi!`;
        } else if (def.intimacy) {
            if (!p.companion) fail('Đạo hữu chưa có đạo lữ để tặng đan dược này.');
            p.companion.intimacy = (p.companion.intimacy || 0) + def.intimacy;
            msg = `Đã tặng ${def.name} cho đạo lữ ${p.companion.name}, tăng +${def.intimacy} điểm thân mật!`;
        } else if (def.permStat) {
            if (def.demonOnly && !p.isDemon) fail('Chỉ có Ma Tu mới có thể hấp thu ma đan này.');
            p.permStats = p.permStats || {};
            for (const [k, v] of Object.entries(def.permStat)) {
                p.permStats[k] = (p.permStats[k] || 0) + v;
            }
            const statNames = { atk: 'Công kích', def: 'Phòng ngự', spd: 'Thân pháp', crit: 'Chí mạng', hp: 'Khí huyết', mp: 'Linh lực' };
            const statText = Object.entries(def.permStat).map(([k, v]) => `+${k === 'crit' ? Math.round(v * 100) + '%' : v} ${statNames[k] || k}`).join(', ');
            msg = `✨ Đã hấp thu ${def.name}, vĩnh viễn gia tăng: ${statText}!`;
        } else if (def.elemMastery) {
            const element = def.elemMastery.element;
            if (!C.HE[element]) fail('Đan dược không có hệ thông thạo hợp lệ.');
            p.elemMastery ||= { kim: 0, moc: 0, thuy: 0, hoa: 0, tho: 0, loi: 0, phong: 0, bang: 0, thien: 0, ma: 0 };
            const current = clamp(Number(p.elemMastery[element]) || 0, 0, 100);
            if (current >= 100) fail(`Thông thạo hệ ${C.HE[element].name} đã đạt giới hạn 100%.`);
            const increase = Math.min(100 - current, Math.max(1, Number(def.elemMastery.amount) || 1));
            p.elemMastery[element] = current + increase;
            msg = `✨ ${def.name} tăng vĩnh viễn +${increase}% thông thạo hệ ${C.HE[element].name} (${p.elemMastery[element]}/100%).`;
        } else if (def.roleStat) {
            p.roleStats ||= {};
            const monId = def.roleStatMon || p.mon;
            const mon = C.MON[monId];
            if (!mon) fail('Đan dược không có chuyên môn hệ phái hợp lệ.');
            const current = clamp(Number(p.roleStats[monId]) || 0, 0, 200);
            if (current >= 200) fail(`${mon.statName} đã đạt giới hạn 200 điểm.`);
            const increase = Math.min(200 - current, Math.max(1, Math.floor(def.roleStat)));
            p.roleStats[monId] = current + increase;
            msg = `📘 ${def.name} tăng vĩnh viễn +${increase} điểm ${mon.statName} (${p.roleStats[monId]}/200).`;
        } else if (def.breakthrough) {
            // Đan đột phá cảnh giới — delegate sang breakthrough() tự kiểm tra và tiêu hao
            return this.breakthrough(userId);
        } else {
            fail('Vật phẩm này chỉ dùng trong trận.');
        }

        this.consume(p, item, 1);
        this.touch();
        return { success: true, message: msg, stamina: p.stamina, hp: p.hp, ...(tracker ? { tracker } : {}) };
    }

    getSmallMonsterState(townId, monsterId, now = this.now()) {
        return this.getMonsterState(townId, monsterId, now);
    }

    getMonsterState(townId, monsterId, now = this.now()) {
        this.data.monsterStates = this.data.monsterStates || this.data.smallMonsterStates || {};
        this.data.smallMonsterStates = this.data.monsterStates;
        const key = `${townId}_${monsterId}`;
        let st = this.data.monsterStates[key];
        const def = C.MONSTER_BY_ID.get(monsterId);
        const isSmall = def ? Boolean(def.small) : true;
        const defaultMax = isSmall ? 3 : 1;

        if (!st) {
            st = {
                id: monsterId,
                townId,
                count: defaultMax,
                maxCount: defaultMax,
                respawnAt: 0,
                fightingBy: null,
                replaceWith: null,
                exp: 0,
                expCap: isSmall ? Math.round(150 * Math.pow(1.3, (def?.realm ?? 0) || 1)) : Math.round(300 * Math.pow(1.3, (def?.realm ?? 0) || 1)),
                level: 0,
                realm: (def && typeof def.realm === 'number') ? def.realm : 0,
                bonusHp: 0,
                bonusAtk: 0,
                bonusDef: 0,
                devourCount: 0,
                lastGrownAt: now,
            };
            this.data.monsterStates[key] = st;
        }

        if (st.level === 0 && (!st.devourCount) && def && typeof def.realm === 'number') {
            st.realm = def.realm;
        }

        if (def && !def.small && st.count <= 0 && (!st.respawnAt || now >= st.respawnAt)) {
            st.count = Math.max(1, st.maxCount || 1);
            st.respawnAt = 0;
            st.replaceWith = null;
        }

        // Small monsters keep their pack count, but each defeated instance still needs time to respawn.
        if (st.count > 0 && st.respawnAt > 0 && now >= st.respawnAt) {
            st.respawnAt = 0;
        }

        // Hồi sinh / thay thế vị trí khi hết thời gian chờ
        if (st.count <= 0 && st.respawnAt > 0 && now >= st.respawnAt) {
            if (st.replaceWith && st.replaceWith !== monsterId) {
                const repKey = `${townId}_${st.replaceWith}`;
                const repDef = C.MONSTER_BY_ID.get(st.replaceWith);
                const repMax = repDef?.small ? 3 : 1;
                this.data.monsterStates[repKey] = this.data.monsterStates[repKey] || {
                    id: st.replaceWith,
                    townId,
                    count: repMax,
                    maxCount: repMax,
                    respawnAt: 0,
                    fightingBy: null,
                    replaceWith: null,
                    exp: 0,
                    expCap: repDef?.small ? 150 : 300,
                    level: 0,
                    realm: (repDef && typeof repDef.realm === 'number') ? repDef.realm : 0,
                    bonusHp: 0,
                    bonusAtk: 0,
                    bonusDef: 0,
                    devourCount: 0,
                    lastGrownAt: now,
                };
                this.data.monsterStates[repKey].count = Math.max(this.data.monsterStates[repKey].count, repMax);
                this.data.monsterStates[repKey].respawnAt = 0;
            }
            st.count = st.maxCount;
            st.respawnAt = 0;
            st.fightingBy = null;
            st.replaceWith = null;
        }

        if (st.fightingBy && st.fightingBy.until <= now) {
            st.fightingBy = null;
        }
        if (def) this.capTownMonsterState(st, def, townId);
        return st;
    }

    pickReplacementMonster(townId, def) {
        if (!def) return 'ty_tho_yeu';
        const townDef = C.TOWN_BY_ID.get(townId || 'thanh_van');
        const townPool = (townDef?.monsterPool || []).map(id => C.MONSTER_BY_ID.get(id))
            .filter(m => m && Boolean(m.small) === Boolean(def.small) && m.id !== def.id);
        if (townPool.length > 0) {
            return townPool[Math.floor(this.rng() * townPool.length)].id;
        }
        const globalCandidates = C.MONSTERS.filter(m => Boolean(m.small) === Boolean(def.small) && m.id !== def.id && Math.abs(m.realm - def.realm) <= 2);
        if (globalCandidates.length > 0) {
            return globalCandidates[Math.floor(this.rng() * globalCandidates.length)].id;
        }
        return def.id;
    }

    smallMonsterFallbackIds(townId) {
        const town = C.TOWN_BY_ID.get(townId);
        const townPool = town?.monsterPool || [];
        const eligible = m => m && m.small && !m.worldBoss;
        const local = townPool.map(id => C.MONSTER_BY_ID.get(id)).filter(eligible);
        const ids = new Set(local.map(m => m.id));
        if (ids.size < 3) {
            const townRealm = Number(town?.realmMin) || 0;
            const nearby = C.MONSTERS.filter(m => eligible(m) && !ids.has(m.id))
                .sort((a, b) => Math.abs(a.realm - townRealm) - Math.abs(b.realm - townRealm));
            for (const monster of nearby) {
                ids.add(monster.id);
                if (ids.size >= 3) break;
            }
        }
        return [...ids];
    }

    // ---- săn yêu thú -------------------------------------------------------
    monsterList(userId, now = this.now()) {
        const realm = this.realmOf(userId).index;
        const night = isNight(now);
        const party = this.partyOf(userId);
        const partySize = this.huntPartySize(userId, party);
        const p = this.player(userId);
        const townId = p?.town || 'thanh_van';
        const townPool = new Set(C.TOWN_BY_ID.get(townId)?.monsterPool || []);
        const smallFallbackIds = new Set(this.smallMonsterFallbackIds(townId));
        const { min: townRealmMin, max: townRealmMax } = this.townRealmRange(townId);
        const unassignedMonsterIds = new Set(C.MONSTERS
            .filter(monster => UNASSIGNED_NORMAL_MONSTER_IDS.has(monster.id)
                && monster.realm >= townRealmMin && monster.realm <= townRealmMax)
            .map(monster => monster.id));
        const list = C.MONSTERS
            .filter(m => (townPool.has(m.id) || smallFallbackIds.has(m.id) || unassignedMonsterIds.has(m.id)) && !m.worldBoss
                && (!m.night || night)
                && (() => {
                    const state = this.getMonsterState(townId, m.id, now);
                    return !(state.count <= 0 && state.respawnAt > now);
                })())
            .map(m => {
                const mst = this.getMonsterState(townId, m.id, now);
                const isDepleted = Boolean(mst.respawnAt > now);
                const mStats = monsterStats(m, now);
                let displayName = m.name;
                if (mst.devourCount >= 3) displayName = `👑 [Yêu Vương Thôn Thiên] ${m.name}`;
                else if (mst.devourCount >= 2) displayName = `⚡ [Dị Biến Thôn Phệ] ${m.name}`;
                else if (mst.devourCount >= 1) displayName = `🔥 [Thôn Phệ] ${m.name}`;
                else if (mst.level >= 2) displayName = `⚡ [Yêu Tướng] ${m.name}`;
                else if (mst.realm > m.realm) displayName = `⚡ [Tiến Hóa] ${m.name}`;

                const curHp = Math.round(mStats.hp + (mst.bonusHp || 0));
                const curAtk = Math.round(mStats.atk + (mst.bonusAtk || 0));
                const curDef = Math.round(mStats.def + (mst.bonusDef || 0));
                const curRealm = Math.max(m.realm, mst.realm || m.realm);

                const smallState = m.small ? {
                    count: mst.count,
                    maxCount: mst.maxCount,
                    isDepleted,
                    respawnAt: mst.respawnAt,
                    fightingBy: mst.fightingBy,
                    replaceWith: mst.replaceWith,
                } : null;

                const monsterState = {
                    count: mst.count,
                    maxCount: mst.maxCount,
                    isDepleted,
                    respawnAt: mst.respawnAt,
                    fightingBy: mst.fightingBy,
                    replaceWith: mst.replaceWith,
                    devourCount: mst.devourCount || 0,
                    level: mst.level || 0,
                };

                const encounterHpMul = encounterPartyHpMul(m, partySize, now);
                return {
                    id: m.id, name: displayName, rawName: m.name, icon: m.icon || '👹', realm: curRealm, realmName: this.realmName(curRealm), element: m.element,
                    elementName: C.HE[m.element].name, night: Boolean(m.night), realmAvailable: true,
                    available: !isDepleted,
                    trait: m.trait || null, bigStun: m.bigStun || 0,
                    hp: Math.round(curHp * encounterHpMul), atk: curAtk, def: curDef, spd: mStats.spd, sense: mStats.sense,
                    small: Boolean(m.small),
                    cost: m.small ? C.RULES.smallCost : C.RULES.huntCost,
                    soloOk: true,
                    partyOk: true,
                    requiredPartySize: minimumEncounterPartySize(m, now),
                    recommendedPartySize: recommendedEncounterPartySize(now),
                    noReward: curRealm <= realm - 2,
                    noRewardText: curRealm <= realm - 2 ? 'Cảnh giới của bạn vượt xa; trận thắng chỉ nhận ít phần thưởng.' : '',
                    smallState,
                    monsterState,
                };
            });

        if (this.isBeastTide(townId)) {
            this.ensureWorldMonsters(now);
            const tideBoss = (this.data.worldMonsters || []).find(m => m.townId === townId && m.isBeastTideBoss);
            if (tideBoss) {
                list.unshift({
                    id: tideBoss.uid,
                    worldUid: tideBoss.uid,
                    name: tideBoss.name,
                    rawName: tideBoss.name,
                    icon: tideBoss.icon || '🦁',
                    realm: tideBoss.realm,
                    realmName: this.realmName(tideBoss.realm),
                    element: tideBoss.element,
                    elementName: C.HE[tideBoss.element]?.name || tideBoss.element,
                    night: false,
                    available: true,
                    trait: 'armor',
                    bigStun: 2,
                    hp: tideBoss.hp,
                    atk: tideBoss.atk,
                    def: tideBoss.def,
                    spd: tideBoss.spd,
                    sense: 50,
                    small: false,
                    cost: C.RULES.huntCost,
                    soloOk: true,
                    partyOk: true,
                    requiredPartySize: 1,
                    recommendedPartySize: recommendedEncounterPartySize(now),
                    isWorldBoss: true,
                    isBeastTideBoss: true,
                    noReward: false,
                    monsterState: {
                        count: 1,
                        maxCount: 1,
                        isDepleted: false,
                        respawnAt: 0,
                        fightingBy: tideBoss.lockedBy ? { userId: tideBoss.lockedBy, name: tideBoss.lockedByName, until: tideBoss.lockedUntil } : null,
                        replaceWith: null,
                        devourCount: tideBoss.devourCount || 0,
                        level: tideBoss.level || 5,
                    },
                });
            }
        }
        return list;
    }

    // Tiểu yêu có thể solo; đại yêu cần đủ đội hình theo khung giờ.
    startHunt(userId, monsterId) {
        const p = this.requirePlayer(userId);
        this.requireNotKnockedOutInDungeon(p);
        const key = String(userId);
        const now = this.now();
        const current = this.battles.get(key);
        if (current && !current.over) return current;
        p.town = p.town || 'thanh_van';
        this.checkTravelArrival(p, now);
        if (p.traveling) fail('Đang ngự kiếm phi hành trên mây, chưa tới nơi.');

        this.ensureWorldMonsters(now);
        const wm = (this.data.worldMonsters || []).find(x => x.uid === monsterId);
        if (wm) {
            return this.startWorldHunt(userId, monsterId);
        }

        if (now < (p.injuredUntil || 0)) {
            const secs = Math.ceil((p.injuredUntil - now) / 1000);
            fail(`Đang trọng thương, còn ${secs >= 60 ? `${Math.ceil(secs / 60)} phút` : `${secs} giây`}.`);
        }

        const def = C.MONSTER_BY_ID.get(monsterId);
        if (!def) fail('Không có yêu thú này.');
        const townPool = new Set(C.TOWN_BY_ID.get(p.town)?.monsterPool || []);
        const smallFallback = def.small && this.smallMonsterFallbackIds(p.town).includes(def.id);
        if (!townPool.has(def.id) && !smallFallback) fail('Yêu thú này không xuất hiện tại thành trấn hiện tại.');
        if (def.night && !isNight(now)) fail(`${def.name} chỉ xuất hiện ban đêm (18:00–6:00).`);

        const mst = this.getMonsterState(p.town, monsterId, now);
        if (mst.fightingBy && mst.fightingBy.until > now && mst.fightingBy.userId !== String(userId)) {
            fail(`⚠️ [${def.name}] đang bị tu sĩ [${mst.fightingBy.name}] giao chiến! Hãy chọn yêu thú khác.`);
        }
        if (mst.respawnAt > now) {
            const secLeft = Math.ceil((mst.respawnAt - now) / 1000);
            fail(`⚠️ [${def.name}] đang hồi sinh tại khu vực này; còn ${secLeft} giây mới có thể săn tiếp.`);
        }

        const st = this.stats(p, now);
        if (p.hp == null) p.hp = st.hp;
        if (p.hp <= Math.round(st.hp * 0.15)) fail('Khí huyết quá thấp (dưới 15%), hãy đến Y Quán thành trấn chữa trị hoặc dùng đan dược trước khi xuất chiến!');

        const cost = def.small ? C.RULES.smallCost : C.RULES.huntCost;
        let players = [p];
        const party = this.partyOf(userId);

        if (party && party.members.length > 1 && party.leader === key) {
            // Trưởng nhóm có thể tự đánh; chỉ thành viên đã sẵn sàng mới được kéo vào đội.
            const selectedIds = party.members.filter(id => id === key || Boolean(party.ready[String(id)]));
            players = selectedIds.map(id => this.player(id)).filter(Boolean);
            for (const m of players) {
                const name = m.name;
                if (!m.registered) fail(`${name} chưa nhập môn.`);
                this.checkTravelArrival(m, now);
                if (m.traveling || (m.town || 'thanh_van') !== (p.town || 'thanh_van')) fail(`${name} phải ở cùng thành trấn với trưởng nhóm mới tham chiến.`);
                if (this.activeBattle(m.userId)) fail(`${name} đang trong trận khác.`);
                if (now < (m.injuredUntil || 0)) fail(`${name} đang trọng thương.`);
                const memberMaxHp = this.stats(m, now).hp;
                if (m.hp != null && Number(m.hp) <= Math.round(memberMaxHp * 0.15)) fail(`${name} khí huyết còn dưới 15%, cần chờ hồi phục hoặc dùng đan dược trước khi tham chiến.`);
                this.syncStamina(m, now);
                if (m.stamina < cost) fail(`${name} không đủ thể lực.`);
            }
        } else {
            this.syncStamina(p, now);
            if (p.stamina < cost) fail('Không đủ thể lực.');
        }
        for (const m of players) {
            m.stamina -= cost;
            if (m.stamina < C.RULES.staminaMax && m.staminaAt > now) m.staminaAt = now;
        }

        mst.fightingBy = { userId: String(userId), name: p.fullName || p.name, until: now + C.RULES.battleMaxMs };

        let battleDef = def;
        if (mst.bonusHp || mst.bonusAtk || mst.bonusDef || mst.level || mst.devourCount) {
            let evolvedName = def.name;
            if (mst.devourCount >= 3) evolvedName = `👑 [Yêu Vương Thôn Thiên] ${def.name}`;
            else if (mst.devourCount >= 2) evolvedName = `⚡ [Dị Biến Thôn Phệ] ${def.name}`;
            else if (mst.devourCount >= 1) evolvedName = `🔥 [Thôn Phệ] ${def.name}`;
            else if (mst.level >= 2) evolvedName = `⚡ [Yêu Tướng] ${def.name}`;

            const baseSt = monsterStats(def, now);
            battleDef = {
                ...def,
                name: evolvedName,
                explicitStats: true,
                hp: Math.round(baseSt.hp + (mst.bonusHp || 0)),
                atk: Math.round(baseSt.atk + (mst.bonusAtk || 0)),
                def: Math.round(baseSt.def + (mst.bonusDef || 0)),
                realm: Math.max(def.realm, mst.realm || def.realm),
                devourCount: mst.devourCount || 0,
                level: mst.level || 0,
            };
        }
        const battle = new Battle(this, players, battleDef, now);
        for (const m of players) this.battles.set(String(m.userId), battle);
        if (party && players.length > 1) { party.ready = {}; party.at = now; this._syncPartiesToData(); }
        this.touch();
        return battle;
    }

    battle(userId) { return this.battles.get(String(userId)) || null; }
    activeBattle(userId) { const b = this.battle(userId); return b && !b.over ? b : null; }

    requireNotKnockedOutInDungeon(p) {
        const downUserIds = p?.activeDungeon?.downUserIds || [];
        if (Array.isArray(downUserIds) && downUserIds.map(String).includes(String(p?.userId))) {
            fail('Bạn đã gục trong Cổ Động; hãy chờ tổ đội hoàn tất chuyến thám hiểm rồi mới tham chiến lại.');
        }
    }

    checkInjuryRealmDrop(userId, summaryNotes = null) {
        const curExp = this.realms.get(userId)?.experience || 0;
        const curRealmIdx = this.realmOf(userId).index;
        if (curExp <= 0 && curRealmIdx > 0) {
            const oldRealmName = this.realmName(curRealmIdx);
            const newRealmIdx = curRealmIdx - 1;
            const newCap = this.realms.info(newRealmIdx).levelCap || 1000;
            this.realms.set(userId, { index: newRealmIdx, experience: Math.round(newCap * 0.8) });
            const msg = `⚡ [GIÁNG CẤP] Khí huyết cạn kiệt, tu vi về 0 khi trọng thương khiến đạo cơ sụp đổ, giáng cấp từ [${oldRealmName}] xuống [${this.realmName(newRealmIdx)}]!`;
            if (Array.isArray(summaryNotes)) summaryNotes.push(msg);
            const p = this.player(userId);
            if (p) {
                p.notices = p.notices || [];
                p.notices.push(msg);
            }
            return { dropped: true, oldRealmName, newRealmName: this.realmName(newRealmIdx) };
        }
        return { dropped: false };
    }

    triggerDemonTauHoaNhapMa(userId, foeName, summaryNotes = null) {
        if (this.rng() >= 0.10) return false;
        const curRealmIdx = this.realmOf(userId).index;
        if (curRealmIdx <= 0) return false;
        const oldRealmName = this.realmName(curRealmIdx);
        const newRealmIdx = curRealmIdx - 1;
        const newCap = this.realms.info(newRealmIdx).levelCap || 1000;
        this.realms.set(userId, { index: newRealmIdx, experience: Math.round(newCap * 0.5) });
        const msg = `⚡ [TẨU HỎA NHẬP MA] Nhiễm phải ma khí của Ma Tu ${foeName} khi chiến bại, bạn đã tẩu hỏa nhập ma, tổn thương căn cơ và bị giáng 1 cảnh giới từ [${oldRealmName}] xuống [${this.realmName(newRealmIdx)}]!`;
        if (Array.isArray(summaryNotes)) summaryNotes.push(msg);
        const p = this.player(userId);
        if (p) {
            p.notices = p.notices || [];
            p.notices.push(msg);
        }
        return true;
    }

    tickAll(now = this.now()) {
        if (now - this.lastMarketSweep > 60 * 1000) { this.lastMarketSweep = now; this.sweepMarket(now); }
        // World/NPC maintenance work scans the persistent roster and monster
        // registry. Five seconds is enough for spawn and background schedules;
        // combat itself continues on the realtime battle tick below.
        if (now - this.lastMaintenanceTick >= 5000) {
            this.lastMaintenanceTick = now;
            // Lên lịch và phát tin Chợ Thần Bí đúng thời điểm kể cả khi
            // không có người chơi đang mở giao diện Phường Thị.
            this.ensureRareShop(now);
            this.tickNpcs(now);
            this.ensureWorldMonsters(now);
            this.cultivateWorldMonsters(now);
        }
        const seen = new Set();
        for (const [key, battle] of this.battles) {
            if (!seen.has(battle)) { seen.add(battle); battle.tick(now); }
            if (battle.over && now - battle.lastActionAt > 5 * 60 * 1000) this.battles.delete(key);
        }
    }

    rollDrops(p, battle, now, factor = 1) {
        const m = battle.monsterDef;
        const drops = [];
        const realmDiff = (m.realm || 0) - this.realmOf(p.userId).index;
        const realmBonus = realmDiff > 0 ? (1 + realmDiff * 0.15) : Math.max(0.35, 1 + realmDiff * 0.15);
        const tierMul = (battle.tier === 'boss' ? 1.25 : (battle.tier === 'elite' ? 1.1 : 1)) * factor * Math.min(1.2, realmBonus);
        const tp = getTimePhase(now);
        const nightMul = (battle.night ? 1.1 : 1) * (tp.dropMul || 1);
        const kindMul = m.small ? 0.9 : 1.1;
        const rawTotalMul = tierMul * nightMul * kindMul;
        const isBossLoot = Boolean(m.worldBoss || battle.tier === 'boss');
        // Boss loot used to multiply boss, realm, night, and time-of-day bonuses
        // together (up to ~3x). Keep late-hour flavor, but cap the final odds.
        const bossNightCap = tp.phase === 'late_night' ? 1.25 : (tp.phase === 'evening' || tp.phase === 'midday') ? 1.15 : 1.5;
        const totalMul = isBossLoot ? Math.min(rawTotalMul, bossNightCap) : rawTotalMul;
        let battleItemDrops = 0;
        const maxBattleItemDrops = 1;

        // 1. Dị Hỏa và Hồ Lô dùng đúng nguồn cùng tỷ lệ ghi trong Thư Các.
        const fires = FIRE_DROP_BY_MONSTER.get(m.id) || [];
        const gourds = GOURD_DROP_BY_MONSTER.get(m.id) || [];
        const rareDrops = [
            ...fires.map(def => ({ kind: 'fire', def, rate: FIRE_DROP_RATE_BY_ITEM.get(def.id)?.get(m.id) || 0 })),
            ...gourds.map(def => ({ kind: 'gourd', def, rate: GOURD_DROP_RATE_BY_ITEM.get(def.id)?.get(m.id) || 0 })),
        ];
        for (const entry of rareDrops) {
            if (battleItemDrops >= maxBattleItemDrops) break;
            const chance = clamp(entry.rate * totalMul, 0, 0.25);
            if (this.rng() < chance) {
                if (entry.kind === 'fire') {
                    if (this.addStack(p, 'mat', entry.def.id, 1) > 0) {
                        drops.push(`🔥 Dị Hỏa ${entry.def.name} (${entry.def.fireQualityName} phẩm)`);
                        battleItemDrops++;
                        break;
                    }
                } else {
                    const toolItem = { uid: newId(), kind: 'tool', id: entry.def.id, name: entry.def.name, icon: entry.def.icon, desc: entry.def.desc, qty: 1, bound: true, unique: true };
                    const place = this.countPlace(p, 'bag') < this.capacity('bag', p) ? 'bag'
                        : this.countPlace(p, 'kho') < this.capacity('kho', p) ? 'kho' : null;
                    if (place) {
                        toolItem.place = place;
                        toolItem.at = now;
                        p.items.push(toolItem);
                        drops.push(`🏺 ${entry.def.name} (${entry.def.tierName} phẩm)`);
                        battleItemDrops++;
                        break;
                    }
                }
            }
        }

        // 2. Quái rơi theo bảng rơi đồ riêng biệt (Monster-specific Drop Table)
        const dropList = m.drops || [];
        for (const drop of dropList) {
            if (battleItemDrops >= maxBattleItemDrops) break;
            if (!MONSTER_DROP_ALLOWED_BY_ITEM.get(drop.id)?.has(m.id)) continue;
            const sourceRate = MONSTER_DROP_RATE_BY_ITEM.get(drop.id)?.get(m.id);
            if (!(sourceRate > 0)) continue;
            let maxChance = drop.kind === 'mat' ? 0.12 : (drop.kind === 'cons' ? 0.08 : 0.04);
            let dropMul = totalMul;
            if (drop.kind === 'equip') {
                maxChance = 0.08;
                dropMul *= 1.5; // Tăng 50% tỉ lệ rơi trang bị (tiểu yêu, đại boss, boss thế giới)
                if (isBossLoot || !m.small) {
                    dropMul *= bossMutationEquipMultiplier(m); // Tăng theo bậc cảnh giới / dị biến của Đại Boss
                }
            }
            const chance = Math.min(maxChance, sourceRate * dropMul);
            if (this.rng() < chance) {
                const qty = 1;
                if (drop.kind === 'mat') {
                    const matDef = C.MATERIAL_BY_ID?.get(drop.id) || { name: drop.id };
                    if (this.addStack(p, 'mat', drop.id, qty) > 0) {
                        drops.push(`${matDef.name} ×${qty}`);
                        battleItemDrops++;
                    }
                } else if (drop.kind === 'cons') {
                    const cDef = C.CONSUMABLE_BY_ID.get(drop.id);
                    if (this.addStack(p, 'cons', drop.id, qty) > 0 && cDef) {
                        const dropLabel = cDef.substageBreakthrough ? 'Đan tiểu cảnh: ' : (cDef.breakthrough ? 'Đan đột phá: ' : '');
                        drops.push(`${dropLabel}${cDef.name}${cDef.breakthrough ? ` ×${qty}` : ''}`);
                        battleItemDrops++;
                    }
                } else if (drop.kind === 'equip') {
                    const eqDef = C.EQUIP_BY_ID.get(drop.id);
                    if (eqDef && this.canCreate('item', eqDef.id)) {
                        const item = this.addEquip(p, eqDef, now);
                        drops.push(item ? eqDef.name : `${eqDef.name} (túi đầy, vào kho)`);
                        battleItemDrops++;
                    }
                } else if (drop.kind === 'scroll') {
                    const skDef = C.SKILL_BY_ID.get(drop.id);
                    if (skDef && this.canCreate('skill', skDef.id) && !p.skills.includes(skDef.id)) {
                        if (this.addStack(p, 'scroll', skDef.id, 1) > 0) {
                            this.stockAdd('skill', skDef.id, 1);
                            drops.push(`Ngọc giản: ${skDef.name}`);
                            battleItemDrops++;
                        }
                    }
                }
            }
        }

        // 2b. Vật phẩm trước đây chỉ có nguồn cửa hàng/chế tác vẫn có nguồn quái riêng.
        for (const entry of SUPPLEMENTAL_ITEM_DROPS_BY_MONSTER.get(m.id) || []) {
            if (battleItemDrops >= maxBattleItemDrops) break;
            if (this.rng() >= Math.min(0.12, entry.rate * totalMul)) continue;
            if (entry.kind === 'mat' && this.addStack(p, 'mat', entry.item.id, 1) > 0) {
                drops.push(`${entry.item.name} ×1`);
                battleItemDrops++;
            } else if (entry.kind === 'cons' && this.addStack(p, 'cons', entry.item.id, 1) > 0) {
                const label = entry.item.substageBreakthrough ? 'Đan tiểu cảnh: ' : (entry.item.breakthrough ? 'Đan đột phá: ' : '');
                drops.push(`${label}${entry.item.name}`);
                battleItemDrops++;
            }
        }

        // 3. Trang bị & Pháp bảo hiếm từ thế giới (tỉ lệ khớp Thư Các)
        for (const def of equipmentDropsFor(m)) {
            if (battleItemDrops >= maxBattleItemDrops) break;
            if (m.small && (def.elite || def.unique)) continue;
            if (def.bossOnly && (m.small || battle.tier !== 'boss')) continue;
            if (def.worldBossOnly && !m.worldBoss) continue;
            if (def.elite && battle.tier === 'normal') continue;
            const sourceRate = EQUIPMENT_DROP_RATE_BY_ITEM.get(def.id)?.get(m.id) || 0;
            let equipTotalMul = totalMul * 1.5; // Tăng 50% tỉ lệ rơi trang bị
            if (isBossLoot || !m.small) {
                equipTotalMul *= bossMutationEquipMultiplier(m); // Tăng theo bậc cảnh giới / dị biến của Đại Boss
            }
            const dropChance = Math.min(0.10, sourceRate * equipTotalMul);
            if (this.rng() >= dropChance) continue;
            if (!this.canCreate('item', def.id)) continue;
            const item = this.addEquip(p, def, now);
            if (item) {
                drops.push(def.name);
                battleItemDrops++;
                break; // Tối đa 1 món trang bị rơi mỗi trận
            }
        }

        // 4. Ngọc giản rơi từ các nguồn quái vật đã phân bổ riêng.
        for (const { skill, rate } of SKILL_DROP_BY_MONSTER.get(m.id) || []) {
            if (battleItemDrops >= maxBattleItemDrops) break;
            if (skill.mon !== p.mon && skill.mon !== 'chung') continue;
            if (skill.demonOnly && !p.isDemon) continue;
            if (m.small && skill.rarity !== 'pt') continue;
            if (p.skills.includes(skill.id)) continue;
            const dropChance = Math.min(0.05, rate * totalMul);
            if (this.rng() >= dropChance || !this.canCreate('skill', skill.id)) continue;
            if (this.addStack(p, 'scroll', skill.id, 1) > 0) {
                this.stockAdd('skill', skill.id, 1);
                drops.push(`${skill.demonOnly ? '[MA PHÁP] ' : ''}Ngọc giản: ${skill.name}`);
                battleItemDrops++;
            }
        }

        // Generated materials are assigned to two source monsters each. Scale
        // the per-source rate to preserve their old aggregate drop frequency.
        if (battleItemDrops < maxBattleItemDrops) {
            const pool = GENERATED_MATERIALS_BY_MONSTER.get(m.id) || [];
            const baseChance = m.worldBoss ? 0.12 : (battle.tier === 'boss' ? 0.09 : (battle.tier === 'elite' ? 0.06 : (m.small ? 0.03 : 0.045)));
            const weightedPool = pool.map(material => ({
                material,
                weight: Math.max(0, Number(GENERATED_MATERIAL_SOURCE_BY_ITEM.get(material.id)?.dropScaleByMonster?.get(m.id)) || 0),
            })).filter(entry => entry.weight > 0);
            const totalWeight = weightedPool.reduce((sum, entry) => sum + entry.weight, 0);
            const chance = Math.min(0.12, baseChance * totalWeight * totalMul);
            if (weightedPool.length && this.rng() < chance) {
                let roll = this.rng() * totalWeight;
                let material = weightedPool[weightedPool.length - 1].material;
                for (const entry of weightedPool) {
                    roll -= entry.weight;
                    if (roll < 0) { material = entry.material; break; }
                }
                if (this.addStack(p, 'mat', material.id, 1) > 0) {
                    drops.push(`${material.name} ×1`);
                    battleItemDrops++;
                }
            }
        }

        // 5. Thưởng Cống Hiến Tông Môn khi hạ boss/yêu quái tinh anh
        if (p.sectId && (battle.tier === 'boss' || battle.tier === 'elite' || m.worldBoss)) {
            const bonusCoins = m.worldBoss ? 10 : (battle.tier === 'boss' ? 3 : 1);
            p.sectCoins = (p.sectCoins || 0) + bonusCoins;
            drops.push(`Cống hiến tông môn +${bonusCoins} 🪙`);
        }

        return drops;
    }

    settleBattle(battle, member = battle.p, now = this.now()) {
        const p = this.player(member.userId);
        const result = member.final || battle.result;
        const summary = { result, exp: 0, expLost: 0, stones: 0, drops: [], levelUps: 0, notes: [] };
        if (!p) return summary;
        if (battle.worldMonsterUid && battle.monsterDef.worldBoss) {
            summary.worldBossDaily = this.getWorldBossDaily(p, now);
        }
        const realm = this.realmOf(p.userId);
        const small = Boolean(battle.monsterDef.small);
        const injuryMin = Math.round(C.RULES.injuryMs / 60000);
        const st = this.stats(p, now);

        if (result === 'win' || result === 'win_down') {
            p.wins = (p.wins || 0) + 1;
            const winWear = this.wearEquippedDurability(p, () => 1);
            const winWearSummary = this.durabilitySummary(winWear, '🛠️ Trang bị sau chiến đấu');
            if (winWearSummary) summary.notes.push(winWearSummary);
            const factor = result === 'win_down' ? 0.5 : 1;
            const tierMul = battle.tier === 'boss' ? 2.0 : (battle.tier === 'elite' ? 1.4 : 1);
            const root = this.linhCan(p);
            const mRealm = battle.monsterDef.realm || 0;
            const devourCount = battle.monsterDef.devourCount || 0;
            // Quái yếu vẫn nhận EXP & quà, quái càng mạnh EXP và quà càng cao
            const realmDiff = mRealm - realm.index;
            const realmScale = realmDiff > 0
                ? (1 + realmDiff * 0.25)
                : Math.max(0.2, 1 + realmDiff * 0.15);

            if (realmDiff <= -2) {
                summary.notes.push('Yêu thú tu vi thấp hơn: EXP và phần thưởng bị suy giảm.');
            } else if (realmDiff > 0) {
                summary.notes.push(`⚔️ Yêu thú cao hơn bạn ${realmDiff} cảnh giới: EXP và tỷ lệ rơi đồ quý tăng.`);
            }
            // Chuyên môn hệ phái là tiến trình riêng: ngang cảnh giới +1,
            // cao hơn +1 mỗi cảnh giới chênh lệch, tối đa +5/trận. Quái thấp
            // và quyết đấu NPC không tăng chuyên môn.
            const roleStatGain = !battle.isNpc && realmDiff >= 0
                ? Math.min(5, Math.max(1, Math.floor(realmDiff)))
                : 0;
            if (roleStatGain > 0) {
                p.roleStats ||= {};
                const statName = C.MON[p.mon]?.statName || 'Chuyên Môn';
                const currentRoleStat = clamp(Number(p.roleStats[p.mon]) || 0, 0, 200);
                const gained = Math.min(roleStatGain, 200 - currentRoleStat);
                if (gained > 0) {
                    p.roleStats[p.mon] = currentRoleStat + gained;
                    summary.notes.push(`📘 ${statName}: +${gained} điểm chuyên môn (${p.roleStats[p.mon]}/200).`);
                }
            }
            if (devourCount > 0) {
                summary.notes.push(`🔥 Trảm sát yêu thú đã thôn phệ ×${devourCount}: Nhận thêm EXP và Linh Thạch trân quý!`);
            }

            // EXP cơ bản tính theo tu vi quái + độ khó + % cấp độ người chơi
            const isWorldBoss = Boolean(battle.worldMonsterUid && battle.monsterDef.worldBoss);
            const mBaseExp = (mRealm + 1) * (small ? 30 : 90);
            const capRate = isWorldBoss ? 0.05 : (small ? 0.008 : 0.018);
            const capExp = realm.levelCap * capRate;
            const devourBonus = 1 + devourCount * 0.35;

            let exp = Math.round((capExp + mBaseExp) * (root?.expMul || 1) * tierMul * realmScale * devourBonus);
            // Boss thế giới khó hơn nhiều và có phần thưởng EXP riêng, vẫn có trần rõ ràng.
            const maxCap = Math.max(isWorldBoss ? 50 : 25, Math.round(realm.levelCap * (isWorldBoss ? 0.08 : 0.04)));
            exp = Math.min(maxCap, Math.max((mRealm + 1) * 15, exp));
            if (factor < 1) exp = Math.max(1, Math.round(exp * factor));

            // Ma tu đánh quái ít exp hơn bình thường (giảm ~55% so với chính đạo)
            if (p.isDemon) {
                exp = Math.max(1, Math.round(exp * 0.45));
                summary.notes.push('🩸 Ma Tu sát phạt yêu thú: Nhận ít EXP hơn chính đạo do ma công cần hấp thụ linh khí đồng đạo!');
            }
            const rawStones = Math.round((small ? (2 + mRealm * 1.5) : (C.RULES.stoneMinPerWin + mRealm * 3)) * (0.8 + this.rng() * 0.5) * 1.3 * tierMul * factor * Math.max(0.4, 1 + realmDiff * 0.15) * devourBonus);
            let stones;
            if (battle.isNpc) {
                // Giữ nguyên kinh tế phần thưởng của các trận quyết đấu NPC.
                stones = small ? Math.min(80, Math.max(3, rawStones)) : Math.min(660, Math.max(10, rawStones));
            } else {
                // Quái luôn có mức thưởng đáng kể; cảnh giới và độ khó tăng phần thưởng,
                // còn các hệ số thú triều/thời gian vẫn được áp dụng trước khi chặn trần.
                const rankBonus = battle.tier === 'boss' ? 450 : (battle.tier === 'elite' ? 250 : 0);
                const stoneBase = isWorldBoss
                    ? 1200 + mRealm * 50
                    : (small ? 500 + mRealm * 25 : 700 + mRealm * 40 + rankBonus);
                stones = Math.max(500, Math.round(stoneBase * (0.8 + this.rng() * 0.5) * 1.3 * tierMul * factor * Math.max(0.4, 1 + realmDiff * 0.15) * devourBonus));
            }

            // Thú triều x2 thưởng
            if (this.isBeastTide(p.town || 'thanh_van')) {
                exp *= 2;
                stones *= 2;
                summary.notes.push('⚔️ THÚ TRIỀU CHIẾN CÔNG: Đánh lui thú triều nhận gấp đôi Linh Thạch và EXP!');
            }
            if (battle.monsterDef.isBeastTideBoss) {
                exp = Math.round(exp * 2.5);
                stones = Math.round(stones * 3);
                summary.notes.push('👑 CHIẾN CÔNG THẦN THÁNH: Trảm sát Đại Boss Thú Triều nhận thêm gấp 3 Linh Thạch và EXP cực đại!');
                const btPill = C.BREAKTHROUGH_BY_REALM[battle.monsterDef.realm] || C.BREAKTHROUGH_BY_REALM[battle.monsterDef.realm - 1];
                if (btPill) {
                    this.giveConsumable(p, btPill.id, 1);
                    summary.notes.push(`🎁 Nhận được đan dược trân quý: [${btPill.name}]!`);
                }
            }
            // Giới hạn cơ bản mỗi trận trước hệ số Thú Triều và thời gian: tiểu yêu 80, boss 660.
            const tp = getTimePhase(now);
            exp = Math.round(exp * (tp.expMul || 1));
            stones = Math.round(stones * (tp.stoneMul || 1));
            if (tp.phase !== 'day') {
                summary.notes.push(`🌙 [${tp.name.toUpperCase()}] Hấp thu linh khí thiên địa: EXP ×${tp.expMul}, Linh Thạch ×${tp.stoneMul}!`);
            }
            if (!battle.isNpc) stones = Math.min(5000, Math.max(500, Math.round(stones)));

            const expRes = this.addExp(p.userId, exp);
            summary.exp = expRes.gained;
            summary.levelUps = expRes.levelUps || 0;
            if (expRes.atBottleneck) {
                summary.notes.push(`⚠️ [BÌNH CẢNH] Tu vi đã chạm đỉnh cảnh giới [${this.realmName(this.realmOf(p.userId).index)}]! Cần dùng đan dược để Đột Phá cảnh giới mới có thể tiếp tục nhận thêm tu vi.`);
            }
            p.stones += stones;
            this.ledger(stones);
            summary.stones = stones;
            summary.drops = this.rollDrops(p, battle, now, factor * realmScale);

            const beastLimit = p.mon === 'thu' ? 8 : 5;
            const gourd = this.beastGourdFor(p, realm.index);
            if (result === 'win' && small && !battle.isNpc && !battle.isDungeon && gourd && (p.beasts || []).length < beastLimit) {
                const beastDef = battle.monsterDef;
                p.pendingBeastCapture = { monsterId: beastDef.id, name: beastDef.name, icon: beastDef.icon || '🐾', realm: beastDef.realm || 0, gourdId: gourd.id, gourdName: gourd.name };
                const captureChance = this.beastCaptureChance(p, p.pendingBeastCapture, realm.index, gourd);
                summary.beastCapture = { ...p.pendingBeastCapture, chance: captureChance, count: (p.beasts || []).length, max: beastLimit };
            }

            if (battle.isVariant) {
                const extraDan = Math.floor(2 + this.rng() * 2);
                this.addStack(p, 'mat', 'mat_yeu_dan', extraDan);
                summary.drops.push(`Yêu Đan (Dị Biến) ×${extraDan}`);
                if (this.rng() < 0.50) {
                    const frags = ['frag_trang_bi', 'frag_cong_phap', 'frag_dan_duoc'];
                    const fId = frags[Math.floor(this.rng() * frags.length)];
                    this.addStack(p, 'fragment', fId, 1);
                    summary.drops.push(`Mảnh Tàn Đồ: ${C.BLUEPRINTS?.find(b => b.fragmentId === fId)?.name || fId}`);
                }
                summary.notes.push('⚡ [TRẢM SÁT DỊ BIẾN] Đã tiêu diệt yêu quái Dị Biến! Thu được thêm Yêu Đan và Mảnh Tàn Đồ quý hiếm!');
            }

            if (battle.isNpc) {
                const npc = this.data.worldNpcs?.[battle.npcId];
                if (npc) {
                    npc.losses = (npc.losses || 0) + 1;
                    this.markWorldNpcDead(npc.id, `tử trận khi giao chiến với ${p.fullName || p.name}`, now);
                    summary.notes.push(`🏆 Đánh bại cao thủ NPC [${npc.name}]. NPC đã tử trận và sẽ không hồi sinh.`);
                    if (npc.isDemon) {
                        this.changeMorality(p, { dao: 10 });
                        summary.notes.push('☯️ Trừ tà diệt ma: Đạo Tâm +10.');
                    }
                    if (npc.bag && npc.bag.length > 0 && this.rng() < NPC_BAG_RECOVERY_CHANCE) {
                        const lootIdx = Math.floor(this.rng() * npc.bag.length);
                        const stolenItem = npc.bag[lootIdx];
                        // Chân Long Nghịch Lân là nguyên liệu tối cao, không để bị cướp tự do từ túi NPC
                        if (stolenItem && stolenItem.id === 'mat_long_lan' && this.rng() > 0.001) {
                            // Không rơi Chân Long Nghịch Lân từ túi NPC
                        } else {
                            npc.bag.splice(lootIdx, 1);
                            if (stolenItem) {
                                this.giveItem(p, stolenItem, this.fitsInBag(p, stolenItem) ? 'bag' : 'kho', true);
                                summary.notes.push(`🎁 [ĐOẠT LẠI BẢO VẬT] Tịch thu được [${itemName(stolenItem)}] từ túi của ${npc.name}!`);
                            }
                        }
                    }
                    {
                        // Chân Long Nghịch Lân là nguyên liệu đột phá cực phẩm, nerf mạnh tỷ lệ rơi từ NPC:
                        // NPC cảnh giới cao chủ yếu rơi Vẫn Thiết / Huyết Tinh, chỉ 0.001 (0.1%) xác suất cực hiếm rơi Chân Long Nghịch Lân
                        let mId;
                        let matDropChance;
                        if ((npc.realm || 0) >= 6) {
                            if (this.rng() < 0.001) {
                                mId = 'mat_long_lan';
                                matDropChance = 1.0;
                            } else {
                                mId = (npc.realm >= 8) ? 'mat_huyet_tinh' : 'mat_van_thiet';
                                matDropChance = 0.08;
                            }
                        } else {
                            const matIds = ['mat_yeu_dan', 'mat_van_thiet', 'mat_huyet_tinh'];
                            mId = matIds[Math.min(matIds.length - 1, Math.floor((npc.realm || 0) / 2))];
                            matDropChance = mId === 'mat_huyet_tinh' ? 0.08 : 0.25;
                        }
                        const mQty = 1;
                        if (this.rng() < matDropChance && this.addStack(p, 'mat', mId, mQty) > 0) {
                            summary.drops.push(`${C.MATERIAL_BY_ID?.get(mId)?.name || mId} ×${mQty}`);
                        }
                    }
                    if (this.rng() < 0.40) {
                        const pills = ['hoi_xuan_dan', 'boi_nguyen_dan', 'dan_tu_linh', 'tay_tuy_dan'];
                        const pId = pills[Math.floor(this.rng() * pills.length)];
                        if (this.addStack(p, 'cons', pId, 1) > 0) {
                            summary.drops.push(C.CONSUMABLE_BY_ID?.get(pId)?.name || pId);
                        }
                    }
                    if (this.rng() < NPC_EQUIPMENT_DROP_CHANCE) {
                        const suitableGears = NPC_EQUIPMENT_BY_REALM[clamp(Math.floor(npc.realm || 0), 0, 65)] || [];
                        if (suitableGears.length > 0) {
                            const eq = pickWeightedNpcEquipment(suitableGears, this.rng);
                            const addedGear = this.addEquip(p, eq, now);
                            if (addedGear) summary.drops.push(eq.name);
                        }
                    }
                    if (npc.isDemon && this.rng() < 0.08) {
                        const demonSkillIds = ['ma_sat_luc', 'ma_hoa_huyet', 'ma_cuu_u', 'ma_thien_ma_don', 'ma_nghich_chi', 'ma_phe_hon', 'ma_thac_thien']
                            .filter(id => this.canCreate('skill', id) && !p.skills.includes(id) && !p.items.some(item => item.kind === 'scroll' && item.id === id));
                        const skId = demonSkillIds.length ? demonSkillIds[Math.floor(this.rng() * demonSkillIds.length)] : null;
                        if (skId && this.addStack(p, 'scroll', skId, 1) > 0) {
                            this.stockAdd('skill', skId, 1);
                            summary.drops.push(`Bí kíp: ${C.SKILL_BY_ID?.get(skId)?.name || 'Ma Công'}`);
                        }
                    }
                }
            }
            if (result === 'win_down') summary.notes.push('Bạn gục ngã nhưng đồng đội đã hạ yêu thú: nhận một nửa phần thưởng, không bị phạt.');
            // A downed party member stays out until this encounter ends, then
            // returns at the low recovery threshold and can regenerate or use a potion.
            p.hp = result === 'win_down' && battle.party
                ? Math.max(1, Math.round(st.hp * 0.1))
                : Math.max(1, Math.min(st.hp, Math.round(member.hp)));
            if (result === 'win_down' && battle.party) {
                summary.notes.push('Bạn đã gục trong trận tổ đội; HP còn 10% và sẽ tự hồi phục. Có thể dùng đan dược để hồi nhanh hơn.');
            }
            p.hpAt = now;
            this.trackBountyKill(p, battle.monsterDef.id);
            this.trackTownBoardProgress(p, battle);
            if (battle.userIds && Array.isArray(battle.userIds)) {
                for (const otherUid of battle.userIds) {
                    if (String(otherUid) !== String(p.userId)) {
                        const otherP = this.player(otherUid);
                        if (otherP) {
                            this.trackBountyKill(otherP, battle.monsterDef.id);
                            this.trackTownBoardProgress(otherP, battle);
                        }
                    }
                }
            }

            // Xử lý trận chiến Cổ Động Thủ Công (Dungeon Battle)
            if (battle.isDungeon) {
                const d = C.DUNGEON_BY_ID.get(battle.dungeonId);
                const dState = this.data.dungeonsState?.[battle.dungeonId];
                const active = p.activeDungeon;

                const nextStageIndex = (battle.dungeonStageIndex || 0) + 1;
                // Hồi phục 20% HP sau mỗi ải chỉ cho người còn đứng vững.
                // Người đã gục không được hồi sinh giữa chuyến thám hiểm.
                if (result !== 'win_down') {
                    p.hp = Math.min(st.hp, Math.round(p.hp + st.hp * 0.20));
                }
                if (result === 'win_down') {
                    summary.notes.push('Bạn đã gục; không thể hồi sinh hoặc tham chiến lại cho đến khi tổ đội hoàn tất Cổ Động.');
                }

                if (active && nextStageIndex < active.totalStages) {
                    summary.dungeonNextStage = true;
                    summary.dungeonStageIndex = nextStageIndex;
                    summary.totalStages = active.totalStages;
                    summary.dungeonId = battle.dungeonId;
                    summary.dungeonName = battle.dungeonName;
                    summary.nextStageName = active.stages[nextStageIndex]?.name || ('Ải ' + (nextStageIndex + 1));
                    summary.notes.push('✨ Vượt qua [Ải ' + nextStageIndex + ': ' + battle.dungeonStageName + ']! Khí huyết được hồi phục 20%. Chuẩn bị tiến vào [' + summary.nextStageName + ']!');
                } else {
                    summary.dungeonComplete = true;
                    summary.notes.push('🎉 Chúc mừng đạo hữu đã đại phá [' + battle.dungeonName + '], trảm sát thủ lĩnh Cổ Động thành công!');

                    if (d) {
                        p.stones = (p.stones || 0) + d.stones;
                        this.realms.addExp(p.userId, d.expReward);
                        summary.stones = (summary.stones || 0) + d.stones;
                        summary.exp = (summary.exp || 0) + d.expReward;

                        if (d.isEquipTrial) {
                            // Bí Cảnh Thí Luyện Trang Bị:
                            // 1. Chắc chắn nhận 1 Trang Bị theo cảnh giới bản đồ
                            const trialEquip = this.pickEquipTrialReward(d, p);
                            if (trialEquip) {
                                const it = this.addEquip(p, trialEquip, now);
                                summary.drops.push(it ? `🎁 [Trang Bị] ${trialEquip.name}` : `🎁 [Trang Bị] ${trialEquip.name} (vào kho)`);
                            }
                            // 2. Rơi 1~2 Mảnh Tàn Đồ Trang Bị
                            const fragQty = Math.floor(1 + this.rng() * 2);
                            this.addStack(p, 'fragment', 'frag_trang_bi', fragQty);
                            summary.drops.push(`Mảnh Tàn Đồ Trang Bị ×${fragQty}`);

                            // 3. Nguyên liệu rèn
                            if (this.rng() < 0.70) {
                                this.addStack(p, 'mat', 'mat_van_thiet', 1);
                                summary.drops.push('Huyền Thiên Vẫn Thiết ×1');
                            }
                            this.rollCuratedDungeonMaterialDrops(d, p, summary.drops);
                            summary.notes.push(`⚔️ ĐẠI THẮNG THÍ LUYỆN: Vượt qua [${d.name}], đoạt được thần binh bảo giáp!`);
                        } else {
                            if (d.guaranteedPill && this.rng() < (d.guaranteedPillRate != null ? d.guaranteedPillRate * 0.4 : 0.025)) {
                                const pillDef = C.CONSUMABLE_BY_ID.get(d.guaranteedPill);
                                if (this.addStack(p, 'cons', d.guaranteedPill, 1) > 0) {
                                    summary.drops.push('Đan đột phá: ' + (pillDef?.name || d.guaranteedPill));
                                }
                            }

                            if (d.bonusPills && d.bonusPills.length > 0 && this.rng() < 0.008) {
                                const bPillId = d.bonusPills[Math.floor(this.rng() * d.bonusPills.length)];
                                const bDef = C.CONSUMABLE_BY_ID.get(bPillId);
                                if (this.addStack(p, 'cons', bPillId, 1) > 0) {
                                    summary.drops.push('Bổ sung: ' + (bDef?.name || bPillId));
                                }
                            }

                            // Boss Cổ Động rơi trang bị: tăng 50% tỉ lệ rơi (0.03 -> 0.045)
                            if (d.equipDrop && this.rng() < 0.045) {
                                const eqDef = C.EQUIP_BY_ID.get(d.equipDrop);
                                if (eqDef) {
                                    const it = this.addEquip(p, eqDef, now);
                                    summary.drops.push(it ? eqDef.name : (eqDef.name + ' (túi đầy)'));
                                }
                            }

                            // Nguyên liệu cơ bản — không phải luôn rơi
                            if (this.rng() < 0.50) {
                                this.addStack(p, 'mat', 'mat_yeu_dan', 1);
                                summary.drops.push('Yêu Đan ×1');
                            }
                            if (this.rng() < 0.50) {
                                this.addStack(p, 'mat', 'mat_van_thiet', 1);
                                summary.drops.push('Huyền Thiên Vẫn Thiết ×1');
                            }

                            this.rollCuratedDungeonMaterialDrops(d, p, summary.drops);
                        }

                        // Cơ duyên ngộ đạo (10% tỷ lệ)
                        if (this.rng() < 0.10) {
                            const burstExp = Math.round(d.expReward * 0.5);
                            this.realms.addExp(p.userId, burstExp);
                            summary.fortuitous = {
                                type: 'exp_burst',
                                text: '🌟【CƠ DUYÊN NGỘ ĐẠO】Lĩnh ngộ linh vận viễn cổ trong hang động, tu vi tăng thêm +' + burstExp + ' EXP!',
                                exp: burstExp,
                            };
                        }

                        // Cooldown tối đa 15 phút
                        const cdMs = Math.min(15 * 60 * 1000, Math.max(3 * 60 * 1000, (180 + (d.realmMin || 0) * 60 + (d.equipDrop ? 120 : 0)) * 1000));
                        if (dState) {
                            dState.respawnAt = now + cdMs;
                            dState.lastClearedBy = p.fullName || p.name;
                            dState.fightingBy = null;
                            summary.dungeonRespawnAt = dState.respawnAt;
                            summary.dungeonCooldownSec = Math.ceil(cdMs / 1000);
                        }
                    }
                    for (const memberId of active?.userIds || [String(p.userId)]) {
                        const expeditionMember = this.player(memberId);
                        if (expeditionMember) delete expeditionMember.activeDungeon;
                    }
                }
            }

            // Nếu là quái thế giới: tiêu diệt xong quái sẽ biến mất khỏi bản đồ
            if (battle.worldMonsterUid) {
                const slainWm = (this.data.worldMonsters || []).find(x => x.uid === battle.worldMonsterUid);
                this.queueWorldMonsterRespawn(slainWm, now);
                if (slainWm?.isBeastTideBoss) {
                    summary.notes.push(`🎉 THẮNG LỢI VĨ ĐẠI: Tổ đội đã trảm sát [${slainWm.name}], dập tắt hung uy Thú Triều, giải cứu ${C.TOWN_BY_ID.get(slainWm.townId)?.name || 'thành trấn'}!`);
                    const townId = slainWm.townId || p.town || 'thanh_van';
                    const states = this.data.monsterStates || this.data.smallMonsterStates || {};
                    for (const k of Object.keys(states)) {
                        if (k.startsWith(`${townId}_`)) {
                            states[k].count = Math.max(1, Math.floor((states[k].count || 1) / 2));
                        }
                    }
                    if (this.data.beastTideEvent?.townIds) {
                        this.data.beastTideEvent.townIds = this.data.beastTideEvent.townIds.filter(id => id !== townId);
                    }
                    if (this.data.bossInvasions?.[townId]) {
                        delete this.data.bossInvasions[townId];
                    }
                }
                this.data.worldMonsters = (this.data.worldMonsters || []).filter(x => x.uid !== battle.worldMonsterUid);
            }
            const mst = this.getMonsterState(p.town || 'thanh_van', battle.monsterDef.id, now);
            mst.fightingBy = null;
            if (!small) {
                mst.count = 0;
                const dungeonRespawnAt = battle.isDungeon
                    ? Number(this.data.dungeonsState?.[battle.dungeonId]?.respawnAt) || 0
                    : 0;
                mst.respawnAt = dungeonRespawnAt || (now + monsterRespawnMs(battle.monsterDef, now));
                mst.replaceWith = null;
                const respawnSec = Math.ceil((mst.respawnAt - now) / 1000);
                summary.notes.push(`🐾 [${battle.monsterDef.name}] đã bị đánh lui; cần ${Math.ceil(respawnSec / 60)} phút để hồi sinh, không thể săn liên tục.`);
            } else {
                mst.count = Math.max(0, mst.count - 1);
                mst.respawnAt = now + SMALL_MONSTER_RESPAWN_MS;
            }
            if (small && mst.count <= 0) {
                const delay = SMALL_MONSTER_RESPAWN_MS;
                mst.respawnAt = now + delay;
                mst.replaceWith = this.pickReplacementMonster(p.town || 'thanh_van', battle.monsterDef);
                summary.notes.push(`🐾 [${battle.monsterDef.name}] đã bị tiêu diệt cạn kiệt! Quái khác đang di chuyển tới thay thế sau ${Math.round(delay / 1000)}s.`);
            } else if (small) {
                summary.notes.push(`🐾 [${battle.monsterDef.name}] còn ${mst.count}/${mst.maxCount} con; đàn cần ${Math.max(0, Math.ceil((mst.respawnAt - now) / 1000))} giây hồi phục trước khi săn tiếp.`);
            }
        } else if (result === 'lose' || result === 'fled') {
            p.losses = (p.losses || 0) + 1;
            if (battle.isNpc && battle.npcId === 'npc_thien_dao') {
                const npc = this.data.worldNpcs?.[battle.npcId];
                if (npc) npc.wins = (npc.wins || 0) + 1;
                p.injuredUntil = now + C.RULES.injuryMs;
                p.hp = Math.max(1, Math.round(st.hp * 0.15));
                p.hpAt = now;
                const penalty = this.applyHeavenPunishmentPenalty(p, summary.notes);
                summary.expLost = penalty.lostExp;
                const injuryWear = this.wearEquippedDurability(p, item => this.injuryDurabilityLoss(item));
                const injuryWearSummary = this.durabilitySummary(injuryWear, '🛠️ Trọng thương làm hư hại trang bị');
                if (injuryWearSummary) summary.notes.push(injuryWearSummary);
                const itemText = penalty.lostItem
                    ? `Thiên Đạo thu lấy [${itemName(penalty.lostItem)}] trong túi/kho thường`
                    : 'không có vật phẩm đủ điều kiện để thu lấy (đồ đang mặc, khóa hoặc trong Kho An Toàn được bảo vệ)';
                const punishmentText = `🌩️ Thiên Đạo giáng phạt: bạn bị trọng thương ${injuryMin} phút, khí huyết còn 15%, mất ${penalty.lostExp.toLocaleString('vi-VN')} EXP và ${itemText}.`;
                summary.notes.push(punishmentText);
                p.notices = p.notices || [];
                p.notices.push(punishmentText);
            } else {
                if (battle.isDungeon) {
                    const dState = this.data.dungeonsState?.[battle.dungeonId];
                    // Clear lock nếu người gọi là leader HOẶC là thành viên trong nhóm (fightingBy.memberIds)
                    if (dState && dState.fightingBy && (
                        dState.fightingBy.userId === String(p.userId) ||
                        (dState.fightingBy.memberIds || []).includes(String(p.userId))
                    )) {
                        dState.fightingBy = null;
                    }
                    // Dùng battle.userIds để đảm bảo xóa activeDungeon cho toàn bộ tổ đội
                    // (p.activeDungeon có thể đã bị xóa bởi member khác gọi trước)
                    const allMemberIds = battle.userIds?.length
                        ? battle.userIds.map(String)
                        : (p.activeDungeon?.userIds || [String(p.userId)]).map(String);
                    for (const memberId of allMemberIds) {
                        const expeditionMember = this.player(memberId);
                        if (expeditionMember) delete expeditionMember.activeDungeon;
                    }
                    summary.notes.push('💀 Thất bại tại [' + (battle.dungeonName || 'Cổ Động') + ']! Bạn bị trọng thương và đánh bật ra ngoài.');
                }
                const mst = this.getMonsterState(p.town || 'thanh_van', battle.monsterDef.id, now);
                mst.fightingBy = null;
                const curExp = Math.max(0, realm.experience || 0);
                let lost = 0;
                if (p.isDemon) {
                    // Ma tu bị trọng thương: mất 2.5% exp hiện có, tối thiểu 50
                    lost = Math.max(50, Math.round(curExp * 0.025));
                } else {
                    // Chính đạo: mất 2% exp hiện có, tối thiểu 20
                    lost = Math.max(20, Math.round(curExp * 0.02));
                }
                summary.expLost = this.realms.loseExp(p.userId, lost);
                const injuryWear = this.wearEquippedDurability(p, item => this.injuryDurabilityLoss(item));
                const injuryWearSummary = this.durabilitySummary(injuryWear, '🛠️ Trọng thương làm hư hại trang bị');
                const downedPartyMember = Boolean(battle.party && member.out === 'down');
                if (downedPartyMember) {
                    delete p.injuredUntil;
                    p.hp = Math.max(1, Math.round(st.hp * 0.1));
                    summary.notes.push('Bạn gục trong trận tổ đội; HP còn 10% và sẽ tự hồi phục. Có thể dùng đan dược để hồi nhanh hơn.');
                } else {
                    p.injuredUntil = now + C.RULES.injuryMs;
                    // Trọng thương: khí huyết chỉ còn 10%
                    p.hp = Math.max(1, Math.round(st.hp * 0.1));
                }
                p.hpAt = now;
                if (!downedPartyMember) summary.notes.push(`Trọng thương, hồi sinh sau ${injuryMin} phút. Độ bền mất 5–10 điểm tùy phẩm chất.${injuryWearSummary ? ` ${injuryWearSummary}` : ''} Hãy đến Y Quán hồi phục!`);
                this.checkInjuryRealmDrop(p.userId, summary.notes);

                if (battle.isNpc) {
                    const npc = this.data.worldNpcs?.[battle.npcId];
                    if (npc) {
                        npc.wins = (npc.wins || 0) + 1;
                        const robChance = npc.isDemon ? 0.70 : 0.40;
                        if (this.rng() < robChance) {
                            const pool = this.getDroppableItems(p);
                            if (pool.length > 0) {
                                const item = pool[Math.min(pool.length - 1, Math.floor(this.rng() * pool.length))];
                                const stolen = this.takeDroppableItem(p, item);
                                if (stolen) {
                                    const stolenName = itemName(stolen);
                                    npc.bag = npc.bag || [];
                                    npc.bag.push(stolen);
                                    summary.notes.push(`⚠️ [BỊ CƯỚP ĐỒ] Cao thủ ${npc.name} đã đánh bại bạn và cướp mất [${stolenName}] trong túi hoặc Kho Động Phủ thường! Kho An Toàn đang thuê không bị ảnh hưởng.`);
                                    p.notices = p.notices || [];
                                    p.notices.push(`⚔️ [CHIẾN BẠI TRƯỚC NPC] Bạn đã bại dưới tay ${npc.name}! Đối phương đã cướp mất [${stolenName}]! Hãy mau tu luyện để đoạt lại!`);
                                }
                            }
                        }
                        if (npc.isDemon) {
                            this.triggerDemonTauHoaNhapMa(p.userId, npc.name, summary.notes);
                        }
                    }
                }

                // Quái thế giới mở khóa và quái cấp cao cướp đồ/tu vi của người chơi
                if (battle.worldMonsterUid) {
                    const wm = (this.data.worldMonsters || []).find(x => x.uid === battle.worldMonsterUid);
                    if (wm) {
                        wm.lockedBy = null;
                        wm.lockedByName = null;
                        wm.lockedUntil = 0;
                        wm.hp = Math.max(1, Math.min(wm.maxHp, Math.round(battle.m.hp)));
                        if (wm.realm >= 2) {
                            wm.exp = (wm.exp || 0) + (summary.expLost || 150);
                            const unequipped = this.getDroppableItems(p);
                            if (unequipped.length > 0 && this.rng() < 0.3) {
                                const item = unequipped[Math.min(unequipped.length - 1, Math.floor(this.rng() * unequipped.length))];
                                const stolen = this.takeDroppableItem(p, item);
                                if (stolen) summary.notes.push(`💀 Yêu thú cấp cao ${wm.name} hung hãn cướp mất [${itemName(stolen)}] trong túi hoặc Kho Động Phủ thường và hấp thu tu vi! Kho An Toàn đang thuê không bị ảnh hưởng.`);
                                else summary.notes.push(`💀 Yêu thú cấp cao ${wm.name} đã hấp thu tu vi của bạn để gia tăng yêu lực!`);
                            } else {
                                summary.notes.push(`💀 Yêu thú cấp cao ${wm.name} đã hấp thu tu vi của bạn để gia tăng yêu lực!`);
                            }
                        } else {
                            summary.notes.push('💨 Quái thế giới tạm lùi lại, bạn vẫn có thể tiếp tục khiêu chiến.');
                        }
                    }
                }
            }
        } else if (result === 'escaped') {
            p.hp = Math.max(1, Math.min(st.hp, Math.round(member.hp)));
            summary.notes.push('Rút lui an toàn nhờ Độn Phù.');
            if (battle.isDungeon) {
                const dState = this.data.dungeonsState?.[battle.dungeonId];
                if (dState && dState.fightingBy && (
                    dState.fightingBy.userId === String(p.userId) ||
                    (dState.fightingBy.memberIds || []).includes(String(p.userId))
                )) {
                    dState.fightingBy = null;
                }
                const allMemberIds = battle.userIds?.length
                    ? battle.userIds.map(String)
                    : (p.activeDungeon?.userIds || [String(p.userId)]).map(String);
                for (const memberId of allMemberIds) {
                    const expeditionMember = this.player(memberId);
                    if (expeditionMember) delete expeditionMember.activeDungeon;
                }
                summary.notes.push('Đã rút lui an toàn khỏi [' + (battle.dungeonName || 'Cổ Động') + '].');
            }
            if (battle.worldMonsterUid) {
                const wm = (this.data.worldMonsters || []).find(x => x.uid === battle.worldMonsterUid);
                if (wm) { wm.lockedBy = null; wm.lockedByName = null; wm.lockedUntil = 0; }
            }
        } else if (result === 'timeout') {
            p.hp = Math.max(1, Math.min(st.hp, Math.round(member.hp)));
            summary.notes.push('Yêu thú bỏ chạy, không có thưởng.');
            if (battle.isDungeon) {
                const dState = this.data.dungeonsState?.[battle.dungeonId];
                if (dState && dState.fightingBy && (
                    dState.fightingBy.userId === String(p.userId) ||
                    (dState.fightingBy.memberIds || []).includes(String(p.userId))
                )) {
                    dState.fightingBy = null;
                }
                const allMemberIds = battle.userIds?.length
                    ? battle.userIds.map(String)
                    : (p.activeDungeon?.userIds || [String(p.userId)]).map(String);
                for (const memberId of allMemberIds) {
                    const expeditionMember = this.player(memberId);
                    if (expeditionMember) delete expeditionMember.activeDungeon;
                }
                summary.notes.push('Quá thời gian khiêu chiến tại [' + (battle.dungeonName || 'Cổ Động') + '], chuyến thám hiểm kết thúc.');
            }
            if (battle.worldMonsterUid) {
                const wm = (this.data.worldMonsters || []).find(x => x.uid === battle.worldMonsterUid);
                if (wm) { wm.lockedBy = null; wm.lockedByName = null; wm.lockedUntil = 0; }
            }
        }
        summary.realm = this.realmOf(p.userId);
        this.grantRealmSkills(p);
        this.touch();
        return summary;
    }

    beastGourdFor(p, realmIndex = 0) {
        const owned = (p.items || [])
            .filter(item => ['bag', 'equip'].includes(item.place) && ['tool', 'mat'].includes(item.kind))
            .map(item => C.BEAST_GOURD_BY_ID?.get(item.id))
            .filter(def => def && (Number(def.realmMin) || 0) <= realmIndex);
        if (p.hasBeastGourd && !owned.some(def => def.id === 'ho_lo_thu')) owned.push(C.BEAST_GOURD_BY_ID.get('ho_lo_thu'));
        return owned.filter(Boolean).sort((a, b) => (b.qualityRank || 0) - (a.qualityRank || 0))[0] || null;
    }

    beastCaptureChance(p, target, realmIndex, gourd) {
        const base = p.mon === 'thu' ? 0.70 : 0.40;
        const realmAdjust = clamp((realmIndex - (Number(target.realm) || 0)) * 0.04, -0.25, 0.20);
        return clamp(base + realmAdjust + (Number(gourd?.captureBonus) || 0), 0.08, 0.90);
    }

    resolveBeastCapture(userId, accept) {
        const p = this.requirePlayer(userId);
        const target = p.pendingBeastCapture;
        if (!target) fail('Không có yêu thú nào đang chờ thu phục.');
        delete p.pendingBeastCapture;
        if (!accept) {
            this.touch();
            return { success: true, message: `Bạn đã thả ${target.name} về núi.` };
        }
        const max = p.mon === 'thu' ? 8 : 5;
        if ((p.beasts || []).length >= max) {
            this.touch();
            return { success: false, message: `Đã thu phục tối đa ${max} linh thú.` };
        }
        const realm = this.realmOf(userId).index;
        const gourd = C.BEAST_GOURD_BY_ID?.get(target.gourdId) || this.beastGourdFor(p, realm) || C.BEAST_GOURD_BY_ID.get('ho_lo_thu');
        const chance = this.beastCaptureChance(p, target, realm, gourd);
        if (this.rng() >= chance) {
            this.touch();
            return { success: false, message: `Thu phục ${target.name} thất bại (${Math.round(chance * 100)}%).` };
        }
        p.beasts ||= [];
        const beast = { id: newId(), ...target, level: 1, caughtAt: this.now() };
        p.beasts.push(beast);
        this.touch();
        return { success: true, beast, message: `Thu phục thành công ${beast.name} bằng ${gourd.name}! Có thể lắp tại ô Phi kiếm / Tọa kỵ.` };
    }

    // ---- tổ đội ---------------------------------------------------------------
    partyOf(userId) {
        const id = this.partyByUser.get(String(userId));
        return id ? this.parties.get(id) || null : null;
    }

    partyCreate(userId) {
        const player = this.requirePlayer(userId);
        this.requireDiscoverablePlayer(userId);
        this.checkTravelArrival(player, this.now());
        if (player.traveling) fail('Phải đến thành trấn rồi mới lập tổ đội.');
        const key = String(userId);
        if (this.partyOf(key)) fail('Bạn đã ở trong một tổ đội.');
        let code;
        do { code = Array.from({ length: 4 }, () => 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789'[Math.floor(Math.random() * 32)]).join(''); }
        while ([...this.parties.values()].some(pt => pt.code === code));
        const party = { id: newId(), code, leader: key, townId: player.town || 'thanh_van', members: [key], ready: {}, at: this.now() };
        this.parties.set(party.id, party);
        this.partyByUser.set(key, party.id);
        this._syncPartiesToData(); this.touch();
        return party;
    }

    partyJoin(userId, code) {
        const player = this.requirePlayer(userId);
        this.requireDiscoverablePlayer(userId);
        this.checkTravelArrival(player, this.now());
        if (player.traveling) fail('Phải đến thành trấn rồi mới vào tổ đội.');
        const key = String(userId);
        if (this.partyOf(key)) fail('Rời tổ đội hiện tại trước.');
        const party = [...this.parties.values()].find(pt => pt.code === String(code || '').trim().toUpperCase());
        if (!party) fail('Không có tổ đội với mã này.');
        const leader = this.player(party.leader);
        const leaderTown = leader?.town || party.townId || 'thanh_van';
        party.townId ||= leaderTown;
        if ((player.town || 'thanh_van') !== party.townId || leaderTown !== party.townId) fail(`Chỉ lập đội khi mọi người cùng ở ${C.TOWN_BY_ID.get(party.townId)?.name || 'một thành trấn'}.`);
        if (party.members.length >= C.RULES.partyMax) fail(`Tổ đội đã đủ ${C.RULES.partyMax} người.`);
        party.members.push(key);
        party.at = this.now();
        this.partyByUser.set(key, party.id);
        this._syncPartiesToData(); this.touch();
        return party;
    }

    partyLeave(userId) {
        const key = String(userId);
        const party = this.partyOf(key);
        if (!party) return;
        party.members = party.members.filter(id => id !== key);
        delete party.ready[key];
        this.partyByUser.delete(key);
        if (!party.members.length) this.parties.delete(party.id);
        else if (party.leader === key) party.leader = party.members[0];
        this._syncPartiesToData(); this.touch();
    }

    partyKick(leaderId, targetId) {
        const party = this.partyOf(leaderId);
        if (!party || party.leader !== String(leaderId)) fail('Chỉ trưởng nhóm mới mời ra được.');
        if (!party.members.includes(String(targetId)) || String(targetId) === String(leaderId)) fail('Không có người này trong đội.');
        this.partyLeave(targetId);
    }

    partyReady(userId, ready) {
        const party = this.partyOf(userId);
        if (!party) fail('Bạn chưa vào tổ đội.');
        if (ready) party.ready[String(userId)] = true; else delete party.ready[String(userId)];
        party.at = this.now();
        this._syncPartiesToData(); this.touch();
    }


    partyView(userId) {
        const party = this.partyOf(userId);
        if (!party) return null;
        const now = this.now();
        return {
            code: party.code, leader: party.leader === String(userId), max: C.RULES.partyMax,
            townId: party.townId || (this.player(party.leader)?.town || 'thanh_van'),
            townName: C.TOWN_BY_ID.get(party.townId || this.player(party.leader)?.town || 'thanh_van')?.name || 'Thành trấn',
            members: party.members.filter(id => this.canSeePlayer(userId, id)).map(id => {
                const m = this.player(id);
                const r = this.realmOf(id);
                if (m) this.syncStamina(m, now);
                return {
                    userId: id,
                    name: m?.name || id,
                    fullName: m?.fullName || m?.name || id,
                    photoUrl: m?.photoUrl || '',
                    mon: m?.mon || 'kiem',
                    monName: C.MON[m?.mon]?.name || 'Tu sĩ',
                    realmName: r.name,
                    realmIndex: r.index,
                    power: m ? this.stats(m, now).power : 0,
                    leader: party.leader === id,
                    ready: party.leader === id || Boolean(party.ready[id]),
                    stamina: m?.stamina ?? 0,
                    injured: Boolean(m && now < (m.injuredUntil || 0)),
                    inBattle: Boolean(this.activeBattle(id)),
                    townId: m?.town || 'thanh_van',
                    townName: C.TOWN_BY_ID.get(m?.town || 'thanh_van')?.name || 'Thành trấn',
                    traveling: Boolean(m?.traveling),
                    me: id === String(userId),
                };
            }),
        };
    }

    // ---- quà: 500 linh thạch và bộ đồ Tân Thủ, mỗi nhân vật một lần ----------
    applyGifts(p) {
        if (!p?.registered) return;
        const gift = C.RULES.gift;
        p.gifts = p.gifts || {};
        if (p.gifts[gift.id]) return;
        p.gifts[gift.id] = true;
        const now = this.now();
        p.stones += gift.stones;
        this.ledger(gift.stones);
        const set = C.RULES.starterSet;
        const weaponType = C.MON[p.mon]?.weapon;
        const wanted = [['weapon', set[weaponType]], ['armor', set.armor], ['acc1', set.acc1], ['acc2', set.acc2], ['nhanTruDo', set.nhanTruDo]];
        for (const [slot, id] of wanted) {
            const def = C.EQUIP_BY_ID.get(id);
            if (!def) continue;
            const item = { uid: newId(), kind: 'equip', id: def.id, qty: 1, place: 'bag', dur: 100, refinedBy: String(p.userId), refineAt: now, at: now };
            p.items.push(item);
            const currentUid = p.equip[slot];
            const current = currentUid ? p.items.find(it => it.uid === currentUid) : null;
            const currentDef = current ? itemDef(current) : null;
            // Mặc ngay nếu ô trống hoặc đang mặc đồ Phàm phẩm thường.
            if (!current || (currentDef && currentDef.tier === 'pham' && !currentDef.bound)) {
                if (current) current.place = this.countPlace(p, 'bag') < C.RULES.bagSize ? 'bag' : 'kho';
                item.place = 'equip';
                p.equip[slot] = item.uid;
            } else if (this.countPlace(p, 'bag') > C.RULES.bagSize) item.place = 'kho';
        }
        p.notices = (p.notices || []).concat([`Quà Hồng Hoang: +${gift.stones} linh thạch và bộ đồ Tân Thủ (đã mặc sẵn).`]);
        this.touch();
    }

    // ---- bảng xếp hạng chiến lực (chỉ người chơi thực thụ) ----------------
    leaderboard(limit = 20) {
        const now = this.now();
        const players = Object.values(this.data.players)
            .filter(p => p.registered && !this.isHiddenFromPlayers(p))
            .map(p => {
                const s = this.stats(p, now);
                return {
                    userId: p.userId,
                    name: p.name,
                    fullName: p.fullName || p.name,
                    photoUrl: p.photoUrl || '',
                    mon: C.MON[p.mon]?.name,
                    he: C.HE[p.he]?.name,
                    realm: s.realmName,
                    power: s.power,
                    isNpc: false
                };
            });
        return players.sort((a, b) => b.power - a.power).slice(0, limit);
    }

    // ---- thiên đạo trừng phạt ----------------------------------------------
    // Rời nhóm hoặc bị ban: thu hồi toàn bộ tu vi, nhân vật và vật phẩm; vật phẩm
    // trả về kho của game. Giữ bản chụp 7 ngày để Owner khôi phục nếu phạt nhầm.
    punish(userId, reason) {
        const key = String(userId);
        const now = this.now();
        const p = this.player(key);
        const realm = this.realms.get(key);
        const hadTuVi = realm.index > 0 || realm.experience > 0;
        if (!p && !hadTuVi) return { punished: false, announce: false };
        this.battles.delete(key);
        this.partyLeave(key);
        const listed = Object.values(this.market.listings).filter(l => l.seller === key);
        for (const l of listed) delete this.market.listings[l.id];
        const snapshot = { player: p ? JSON.parse(JSON.stringify(p)) : null, realm: { index: realm.index, experience: realm.experience } };
        if (snapshot.player) snapshot.player.items.push(...listed.map(l => ({ ...JSON.parse(JSON.stringify(l.item)), place: 'kho' })));
        if (p) {
            for (const item of [...p.items, ...listed.map(l => l.item)]) {
                if (item.kind === 'equip') this.stockAdd('item', item.id, -1);
                if (item.kind === 'scroll') this.stockAdd('skill', item.id, -item.qty);
            }
            for (const id of p.skills || []) this.stockAdd('skill', id, -1);
            delete this.data.players[key];
        }
        this.realms.reset(key);
        this.data.punished[key] = { at: now, reason, snapshot };
        this.touch();
        return { punished: true, announce: Boolean(p?.registered) || realm.index >= 1, name: p?.name || null, realmName: realm.name };
    }

    restore(userId) {
        const key = String(userId);
        const record = this.data.punished[key];
        const now = this.now();
        if (!record) fail('Không có hồ sơ bị phạt của người này.');
        if (now - record.at > C.RULES.punishKeepMs) fail('Đã quá 7 ngày, không khôi phục được nữa.');
        const { player, realm } = record.snapshot;
        const skipped = [];
        if (player) {
            player.items = player.items.filter(item => {
                if (item.kind === 'equip') {
                    if (!this.canCreate('item', item.id)) { skipped.push(itemName(item)); return false; }
                    this.stockAdd('item', item.id, 1);
                }
                if (item.kind === 'scroll') {
                    if (!this.canCreate('skill', item.id)) { skipped.push(itemName(item)); return false; }
                    this.stockAdd('skill', item.id, item.qty);
                }
                return true;
            });
            // Khôi phục xong đồ thì nó sẽ ở ngoài kho (chuyển toàn bộ vào túi 'bag')
            for (const item of player.items) {
                if (item.place === 'kho') item.place = 'bag';
            }
            const alive = new Set(player.items.map(it => it.uid));
            for (const slot of Object.keys(player.equip)) if (!alive.has(player.equip[slot])) player.equip[slot] = null;
            player.quick = player.quick.map(q => (alive.has(q) ? q : null));
            player.skills = (player.skills || []).filter(id => {
                if (!this.canCreate('skill', id)) { skipped.push(C.SKILL_BY_ID.get(id)?.name || id); return false; }
                this.stockAdd('skill', id, 1);
                return true;
            });
            player.slots = player.slots.map(id => (player.skills.includes(id) ? id : null));
            this.data.players[key] = player;
        }
        this.realms.set(key, realm);
        delete this.data.punished[key];
        this.touch();
        return { name: player?.name || key, skipped };
    }

    // ---- dữ liệu cho giao diện ---------------------------------------------
    // ---- mô tả vật phẩm cho giao diện -------------------------------------
    describeItem(item, viewer = null) {
        const def = itemDef(item);
        const realmIndex = viewer ? this.realmOf(viewer.userId).index : 0;
        const out = { uid: item.uid, kind: item.kind, id: item.id, name: itemName(item), qty: item.qty, place: item.place, sell: this.sellPrice(item) };
        if (item.kind === 'tool') Object.assign(out, { name: item.name || 'Hồ Lô Thu Thú', icon: item.icon || '🏺', desc: item.desc || 'Dùng sau trận thắng để thử thu phục yêu thú.', bound: true, unique: true });
        if (!def) return out;
        if (item.kind === 'equip') {
            const tier = C.TIER[def.tier];
            const hasRealm = realmIndex >= tier.realm;
            const viewerDao = Number(viewer?.daoScore ?? viewer?.daoTam ?? 100);
            const hasDao = !def.reqDao || viewerDao >= def.reqDao;
            const reqMa = requiredMaForEquipment(def);
            const hasMa = !reqMa || (viewer?.maScore || 0) >= reqMa;
            const hasElem = !def.reqElement || viewer?.he === def.reqElement || (viewer?.elemMastery?.[def.reqElement] || 0) >= 15;
            const hasRoleStat = !def.reqRoleStat || !viewer || (viewer.roleStats?.[viewer.mon] || 0) >= def.reqRoleStat;
            const usable = hasRealm && hasDao && hasMa && hasElem && hasRoleStat;
            Object.assign(out, {
                slot: def.slot, wtype: def.wtype || null, tier: def.tier, tierName: tier.name,
                qualityRank: def.qualityRank ?? tier.rank ?? 0, qualityName: def.qualityName || tier.name, element: def.element || null,
                storageBonus: def.storageBonus || null,
                superRare: Boolean(def.superRare),
                teleportRing: Boolean(def.teleportRing),
                stats: def.stats, dur: item.dur ?? 100, unique: Boolean(def.unique), bound: Boolean(def.bound), realmMin: def.realmMin ?? tier.realm, realmMinName: this.realmName(def.realmMin ?? tier.realm),
                reqDao: def.reqDao || 0, reqMa, reqElement: def.reqElement || null,
                reqRoleStat: def.reqRoleStat || 0, roleStatName: viewer ? C.MON[viewer.mon]?.statName : null, hasRoleStat,
                mount: Boolean(def.mount), alchemyRate: def.alchemyRate || null,
                usable,
                refineAt: viewer && item.refinedBy === String(viewer.userId) ? item.refineAt : null,
                refined: Boolean(viewer && item.refinedBy === String(viewer.userId)), refineSec: tier.refine,
                repairCost: equipmentRepairCost(def, item.dur),
            });
        } else if (item.kind === 'cons') {
            Object.assign(out, {
                desc: def.desc, battle: Boolean(def.battle), talisman: Boolean(def.talisman || def.id.startsWith('phu_')),
                locateMonster: Boolean(def.locateMonster),
                heal: def.heal || 0, cleanse: Boolean(def.cleanse), breakthrough: Boolean(def.breakthrough),
                elemMastery: def.elemMastery || null, roleStat: def.roleStat || 0, roleStatMon: def.roleStatMon || null,
                qualityRank: def.qualityRank ?? 0, qualityName: def.qualityName || 'Phàm',
                realmMin: def.realmMin ?? 0, realmMinName: def.breakthrough ? this.realmName(def.realmMin ?? 0) : null,
                icon: def.icon || '💊',
            });
        } else if (item.kind === 'tool') {
            Object.assign(out, {
                name: def.name, icon: def.icon || '🏺', desc: def.desc,
                tier: def.tier, tierName: def.tierName || C.TIER[def.tier]?.name || 'Phàm',
                qualityRank: def.qualityRank || 0, realmMin: def.realmMin || 0,
                realmMinName: this.realmName(def.realmMin || 0), captureBonus: def.captureBonus || 0,
                bound: true, unique: true,
            });
        } else if (item.kind === 'mat') {
            const tier = C.TIER[def.tier];
            const realmMin = def.realmMin ?? def.minRealm ?? tier?.realm ?? 0;
            Object.assign(out, {
                icon: def.icon || '📦', desc: def.desc, tier: def.tier,
                tierName: def.qualityName || tier?.name || 'Phàm',
                qualityRank: def.qualityRank ?? tier?.rank ?? 0,
                realmMin, realmMinName: this.realmName(realmMin),
                fireQualityName: def.fireQualityName || null,
            });
        } else if (item.kind === 'scroll') {
            const hasRealm = realmIndex >= def.realm;
            const viewerDao = Number(viewer?.daoScore ?? viewer?.daoTam ?? 100);
            const hasDao = !def.reqDao || viewerDao >= def.reqDao;
            const reqMa = requiredMaForSkill(def);
            const hasMa = (viewer?.maScore || 0) >= reqMa;
            const hasElem = !def.element || viewer?.he === def.element || (viewer?.elemMastery?.[def.element] || 0) >= 15;
            const hasMon = def.mon === 'chung' || !viewer || def.mon === viewer.mon;
            const hasRoleStat = !def.reqRoleStat || !viewer || (viewer.roleStats?.[viewer.mon] || 0) >= def.reqRoleStat;
            const alreadyLearned = Boolean(viewer?.skills?.includes(def.id));
            const usable = alreadyLearned || (hasRealm && hasDao && hasMa && hasElem && hasMon && hasRoleStat);
            Object.assign(out, {
                rarity: def.rarity, rarityName: C.RARITY[def.rarity].name, mon: def.mon, monName: C.MON[def.mon]?.name || 'Mọi môn',
                realmMin: def.realm, realmMinName: this.realmName(def.realm),
                reqDao: def.reqDao || 0, reqMa,
                reqRoleStat: def.reqRoleStat || 0, roleStatName: viewer ? C.MON[viewer.mon]?.statName : null, hasRoleStat,
                usable,
                hasMon,
                alreadyLearned,
                canStudy: alreadyLearned,
                desc: describeSkill(def), set: def.set || null,
            });
        }
        return out;
    }

    skillInfo(id, p = null) {
        const sk = C.SKILL_BY_ID.get(id);
        if (!sk) return null;
        const realmIndex = p ? this.realmOf(p.userId).index : 0;
        const hasRealm = realmIndex >= sk.realm;
        const hasMon = !p || sk.mon === 'chung' || sk.mon === p.mon;
        const hasRoleStat = !p || !sk.reqRoleStat || (p.roleStats?.[p.mon] || 0) >= sk.reqRoleStat;
        const reqDao = sk.reqDao || 0;
        const reqMa = requiredMaForSkill(sk);
        const hasDao = !p || !reqDao || Number(p.daoScore ?? p.daoTam ?? 100) >= reqDao;
        const hasMa = !p || !reqMa || (p.maScore || 0) >= reqMa;
        const locked = !hasRealm || !hasMon || !hasRoleStat || !hasDao || !hasMa;
        const now = p ? this.now() : 0;
        const previousReadyAt = p ? (p.cd[id] || 0) : 0;
        const readyAt = p ? skillCooldownUntil(p.cd, id, now) : 0;
        if (readyAt !== previousReadyAt) this.touch();
        return {
            id, name: sk.name, icon: sk.icon || '✨', mon: sk.mon, monName: C.MON[sk.mon]?.name || 'Mọi môn', rarity: sk.rarity, rarityName: C.RARITY[sk.rarity].name,
            qualityRank: sk.qualityRank ?? 0, realmBand: sk.realmBand ?? 0,
            kind: sk.kind, cd: sk.cd, mp: sk.mp, big: Boolean(sk.big), escape: sk.kind === 'escape', desc: describeSkill(sk),
            realmMin: sk.realm, realmMinName: this.realmName(sk.realm), set: sk.set || null, setName: sk.set ? this.realmName(sk.set) : null,
            grant: Boolean(sk.grant), locked, hasRealm, hasMon, hasRoleStat, hasDao, hasMa,
            reqRoleStat: sk.reqRoleStat || 0, roleStatName: p ? C.MON[p.mon]?.statName : null,
            readyAt,
            reqDao, reqMa,
        };
    }

    // ---- dữ liệu cho giao diện ---------------------------------------------
    liveState(userId) {
        const now = this.now();
        const p = this.player(userId);
        let s = null;
        if (p?.registered) {
            this.syncStamina(p, now);
            s = this.stats(p, now);
            this.syncHp(p, now, s);
        }
        const maxHp = s ? s.hp : 0;
        const curHp = p?.registered ? (p.hp == null ? maxHp : p.hp) : 0;
        const events = p?.registered ? this.getPersonalEvents(userId) : { list: [], unreadCount: 0 };
        const max = p?.registered ? this.maxStamina(p) : 0;
        return {
            now,
            inBattle: Boolean(this.activeBattle(userId)),
            player: {
                injuredUntil: p?.injuredUntil || 0,
                traveling: p?.traveling || null,
                hp: curHp,
                maxHp: maxHp,
                hpNextAt: (p?.registered && curHp < maxHp) ? ((p.hpAt || now) + (C.RULES.hpRegenMs || 60000)) : null,
                stamina: p?.registered ? p.stamina : null,
                staminaMax: max,
                staminaNextAt: p?.registered && p.stamina < max ? p.staminaAt + C.RULES.staminaRegenMs : null,
            },
            personalEvents: events.list.filter(event => !event.read),
            personalUnreadCount: events.unreadCount,
        };
    }

    view(userId) {
        const now = this.now();
        const p = this.player(userId);
        const realm = this.realmOf(userId);
        const base = {
            now, night: isNight(now), vnHour: vnHour(now), timePhase: getTimePhase(now),
            realm: { index: realm.index, name: realm.name, sub: realm.sub, experience: realm.experience, levelCap: realm.levelCap },
            catalog: {
                mon: Object.values(C.MON).map(m => ({ id: m.id, name: m.name, weaponName: m.weaponName, statName: m.statName })),
                he: Object.values(C.HE).map(h => ({ id: h.id, name: h.name, beats: h.beats, effect: h.effect })),
            },
        };
        if (!p?.registered) {
            const punished = this.data.punished[String(userId)];
            return { ...base, registered: false, waitUntil: punished ? punished.at + C.RULES.reRegisterWaitMs : 0, hasItems: Boolean(p) };
        }
        p.safeStorageUntil = Number(p.safeStorageUntil) || 0;
        this.expireSafeStorage(p, now);
        const currentMapId = p.mapId || C.TOWN_BY_ID.get(p.town)?.mapId;
        if (!p.ascended && this.realmOf(userId).index >= 11 && C.MAP_BY_ID.get(currentMapId)?.ascensionRequired) {
            p.ascended = true;
            this.touch();
        }
        this.syncStamina(p, now);
        this.grantRealmSkills(p);
        this.applyGifts(p);
        this.checkSystemMails(p);
        this.checkTravelArrival(p, now);
        this.ensureBounties(p, now);
        this.tickNpcs(now);
        if (!p.lastSeenAt || now - p.lastSeenAt >= 60 * 60 * 1000) {
            p.lastSeenAt = now;
            this.touch();
        }

        const notices = Array.isArray(p.notices) ? p.notices.splice(0) : [];
        if (notices.length) {
            p.personalEvents = Array.isArray(p.personalEvents) ? p.personalEvents : [];
            p.personalEvents.push(...notices.map(text => ({ id: newId(), text: String(text), at: now, read: false })));
            p.personalEvents = p.personalEvents.slice(-100);
            this.touch();
        }
        const s = this.stats(p, now);
        this.syncHp(p, now, s);
        if (p.hp == null) p.hp = s.hp;
        p.hp = Math.min(s.hp, Math.max(1, p.hp));

        const describe = item => this.describeItem(item, p);
        const battle = this.battle(userId);
        const myListings = Object.values(this.market.listings).filter(l => l.seller === String(p.userId)).length;
        const currentTown = C.TOWN_BY_ID.get(p.town || 'thanh_van') || C.TOWNS[0];
        this.ensureWorldMonsters(now);
        const activeBeastTideIds = new Set(this.getBeastTideTownIds(now));
        p.equip.ring1 = p.equip.ring1 || null;
        p.equip.ring2 = p.equip.ring2 || null;
        p.equip.nhanTruDo = p.equip.nhanTruDo || null;
        p.equip.nhanNaDi ??= null;
        const missingHp = Math.max(0, s.hp - (p.hp == null ? s.hp : p.hp));
        const currentTownIsImmortal = Boolean(C.MAP_BY_ID.get(currentTown.mapId)?.ascensionRequired);
        const healingCost = townHealingCost(missingHp, s.hp, currentTownIsImmortal);
        const fullHealingCost = townHealingCost(s.hp, s.hp, currentTownIsImmortal);
        const dungeonsView = this.getDungeonsView(userId);

        return {
            ...base,
            registered: true,
            notices,
            town: { ...currentTown, lordName: this.townLordName(currentTown), lordTitle: 'Thành Chủ', lordRealmName: this.realmName(Math.min(65, currentTown.realmMin + 3)), healingCost, fullHealingCost, minHealingCost: currentTownIsImmortal ? IMMORTAL_HEAL_MIN_COST : TOWN_HEAL_MIN_COST },
            allTowns: C.TOWNS.map(t => {
                const immortalTown = Boolean(C.MAP_BY_ID.get(t.mapId)?.ascensionRequired);
                return { ...t, healingCost: townHealingCost(missingHp, s.hp, immortalTown), fullHealingCost: townHealingCost(s.hp, s.hp, immortalTown), minHealingCost: immortalTown ? IMMORTAL_HEAL_MIN_COST : TOWN_HEAL_MIN_COST, teleportCost: C.getTeleportCost(currentTown.id, t.id), lordName: this.townLordName(t), lordTitle: 'Thành Chủ', lordRealmName: this.realmName(Math.min(65, t.realmMin + 3)), hasBeastTide: activeBeastTideIds.has(t.id) };
            }),
            activeBeastTides: this.getActiveBeastTides(now, activeBeastTideIds),
            allMaps: C.MAPS,
            beastTide: activeBeastTideIds.has(currentTown.id),
            worldMonsters: this.getWorldMonsters(userId, false),
            worldBossStatus: (() => {
                const schedule = this.data.worldBossSchedule || { slots: [] };
                const bossInstances = (this.data.worldMonsters || []).filter(monster =>
                    monster.isScheduledWorldBoss && monster.worldBossDate === schedule.date && Number(monster.hp) > 0
                );
                const respawns = (schedule.respawns || []).filter(entry => Number(entry.dueAt) > now);
                return {
                    date: schedule.date || vnDate(now),
                    activeCount: bossInstances.length,
                    maxCount: 20,
                    respawningCount: respawns.length,
                    nextAt: respawns.length ? Math.min(...respawns.map(entry => entry.dueAt)) : null,
                    windowOpen: this.isWorldBossWindow(now),
                };
            })(),
            npcs: this.getNpcList(userId, false),
            player: {
                userId: p.userId, name: p.name, fullName: p.fullName || p.name, photoUrl: p.photoUrl || null,
                gender: p.gender, appearanceId: p.appearanceId || 'thanh_ngoc', appearanceColors: p.appearanceColors || null, look: p.look || null, lookWorn: wornLook(p),
                talents: p.talents || [], mon: p.mon, monName: C.MON[p.mon].name, weaponType: C.MON[p.mon].weapon,
                roleStat: (() => {
                    const value = clamp(Number(p.roleStats?.[p.mon]) || 0, 0, 200);
                    const weaponType = C.MON[p.mon]?.weapon;
                    const thresholds = [
                        ...C.SKILLS.filter(skill => skill.mon === p.mon && skill.reqRoleStat).map(skill => Number(skill.reqRoleStat)),
                        ...C.EQUIPMENT.filter(item => item.wtype === weaponType && item.reqRoleStat).map(item => Number(item.reqRoleStat)),
                    ].filter(n => Number.isFinite(n) && n > value);
                    return {
                        name: C.MON[p.mon].statName,
                        value,
                        max: 200,
                        next: thresholds.length ? Math.min(...thresholds) : null,
                        skillDamageBonusPct: value * 0.1,
                    };
                })(),
                he: p.he, heName: C.HE[p.he].name, linhCan: this.linhCan(p)?.name, linhCanId: this.linhCan(p)?.id, linhCanOptions: C.LINH_CAN.map(root => ({ id: root.id, name: root.name, weight: root.weight, expMul: root.expMul, statMul: root.statMul || {}, elemBonus: root.elemBonus || 0 })), daoTam: p.daoScore,
                stones: p.stones, stamina: p.stamina, staminaMax: this.maxStamina(p),
                staminaNextAt: p.stamina >= this.maxStamina(p) ? null : p.staminaAt + C.RULES.staminaRegenMs,
                injuredUntil: p.injuredUntil || 0, wins: p.wins || 0, losses: p.losses || 0,
                hp: p.hp, maxHp: s.hp,
                hpNextAt: (p.hp != null && p.hp < s.hp) ? ((p.hpAt || now) + (C.RULES.hpRegenMs || 60000)) : null,
                stats: { hp: p.hp, maxHp: s.hp, mp: s.mp, atk: s.atk, def: s.def, spd: s.spd, sense: s.sense, crit: s.crit, accuracy: s.accuracy, dodge: s.dodge, power: s.power },
                detailedStats: {
                    hp: p.hp, maxHp: s.hp, mp: s.mp, atk: s.atk, def: s.def, spd: s.spd, sense: s.sense,
                    critRate: s.critRate, critDmg: s.critDmg, accuracy: s.accuracy, dodge: s.dodge, dmgReduction: s.dmgReduction, power: s.power,
                },
                town: currentTown.id,
                townName: currentTown.name,
                ascended: Boolean(p.ascended),
                traveling: p.traveling || null,
                worldPosition: p.worldPosition || null,
                teleportCooldownUntil: p.teleportCooldownUntil || 0,
                bounties: p.bounties || null,
                equip: Object.fromEntries(Object.entries(p.equip).map(([slot, id]) => [slot, id ? ((slot === 'phiKiem' && p.beasts.some(beast => beast.id === id)) ? { uid: id, id: p.beasts.find(beast => beast.id === id).monsterId, name: p.beasts.find(beast => beast.id === id).name, icon: p.beasts.find(beast => beast.id === id).icon, kind: 'beast', slot: 'phi_kiem', place: 'equip', mount: true, desc: 'Linh thú đã thu phục.' } : describe(p.items.find(it => it.uid === id))) : null])),
                beasts: p.beasts.map(beast => ({ ...beast, mounted: p.equip.phiKiem === beast.id })),
                hasBeastGourd: Boolean(p.hasBeastGourd || p.items.some(it => it.id === 'ho_lo_thu')),
                slotNames: SLOT_NAMES,
                bag: (() => { this.sortPlayerItems(p); return p.items.filter(it => it.place === 'bag').map(describe); })(),
                kho: p.items.filter(it => it.place === 'kho').map(describe),
                safe: p.items.filter(it => it.place === 'safe').map(describe),
                safeStorage: {
                    slots: C.RULES.safeStorageSlots,
                    used: p.items.filter(it => it.place === 'safe').length,
                    monthlyCost: C.RULES.safeStorageMonthlyCost,
                    until: p.safeStorageUntil,
                    active: p.safeStorageUntil > now,
                },
                bagSize: C.RULES.bagSize, khoSize: this.capacity('kho', p),
                quick: p.quick,
                skills: p.skills.map(id => this.skillInfo(id, p)).filter(Boolean),
                slots: p.slots.map(id => (id ? this.skillInfo(id, p) : null)),
                slotRealms: C.RULES.skillSlotRealms.map(r => ({ realm: r, name: this.realmName(r), open: realm.index >= r })),
                listings: myListings, maxListings: C.RULES.marketMaxListings,
                sectId: p.sectId || null,
                sectName: p.sectId && this.sects[p.sectId] ? this.sects[p.sectId].name : null,
                sectRole: p.sectRole || null,
                sectCoins: p.sectCoins || 0,
                pvp: p.pvp || { points: 1000, wins: 0, losses: 0 },
                companion: p.companion ? {
                    id: p.companion.id,
                    name: p.companion.name,
                    gender: p.companion.gender,
                    intimacy: p.companion.intimacy || 0,
                    isNpc: Boolean(p.companion.isNpc),
                    marriedAt: p.companion.marriedAt,
                    songTuToday: p.companion.lastSongTuDate === vnDate(now) ? (p.companion.songTuToday || 0) : 0,
                    protectedUntil: p.companion.protectedUntil || 0,
                } : null,
                proposalsCount: (p.marriageProposals || []).length,
                songTuBuff: Boolean(p.songTuBuffUntil && p.songTuBuffUntil > now),
                isDemon: Boolean(p.isDemon) || ((p.maScore || 0) > 0 && (p.maScore || 0) > Math.max(0, p.daoScore || 0)),
                isWantedMaTu: Boolean(p.isDemon),
                moralState: this.moralityState({ ...p, isDemon: Boolean(p.isDemon) || ((p.maScore || 0) > 0 && (p.maScore || 0) > Math.max(0, p.daoScore || 0)) }),
                personalEventCount: (p.personalEvents || []).filter(event => !event.read).length,
                killCount: p.killCount || 0,
                daoScore: p.daoScore != null ? p.daoScore : 100,
                maScore: p.maScore != null ? p.maScore : 0,
                isThienMa: (p.maScore || 0) >= 1000,
                elemMastery: p.elemMastery || { kim: 0, moc: 0, thuy: 0, hoa: 0, tho: 0, loi: 0, phong: 0, bang: 0, thien: 0, ma: 0 },
                isRoaming: Boolean(p.isRoaming),
            },
            sect: p.sectId ? this.sectView(userId) : null,
            road: this.realmRoad(realm.index),
            monsters: this.monsterList(userId, now),
            dungeons: dungeonsView,
            equipTrials: dungeonsView.equipTrials || [],
            activeDungeon: p.activeDungeon ? {
                ...p.activeDungeon,
                dungeonName: C.DUNGEON_BY_ID.get(p.activeDungeon.dungeonId)?.name || 'Cổ Động',
                stageName: p.activeDungeon.stages?.[p.activeDungeon.stageIndex]?.name || ('Ải ' + ((p.activeDungeon.stageIndex || 0) + 1)),
            } : null,
            equipTrialDaily: (() => {
                const today = vnDate(now);
                const d = p.equipTrialDaily?.date === today ? p.equipTrialDaily : { date: today, count: 0 };
                return { count: d.count || 0, max: 10, remaining: Math.max(0, 10 - (d.count || 0)) };
            })(),
            townBountyBoard: this.getTownBountyBoard(userId),
            dungeonDaily: (() => {
                const today = vnDate(now);
                const d = p.dungeonDaily?.date === today ? p.dungeonDaily : { date: today, count: 0 };
                return { count: d.count || 0, max: 10, remaining: Math.max(0, 10 - (d.count || 0)) };
            })(),
            worldBossDaily: this.getWorldBossDaily(p, now),
            pvpDaily: this.getPvpDaily(p, now),
            npcDaily: this.getNpcDaily(p, now),
            breakthrough: this.canBreakthrough(userId),
            ascension: this.canAscend(userId),
            blueprints: (C.BLUEPRINTS || []).map(bp => {
                const reqMat = C.MATERIAL_BY_ID?.get(bp.matId) || { name: bp.matId };
                let myCount = 0;
                for (const it of p.items) {
                    if (it.kind === 'mat' && it.id === bp.matId && it.place === 'bag') {
                        myCount += (it.qty || 1);
                    }
                }
                return {
                    id: bp.id,
                    name: bp.name,
                    matId: bp.matId,
                    matName: reqMat.name,
                    reqCount: bp.reqCount,
                    myCount,
                    hasEnough: myCount >= bp.reqCount,
                    minRealm: bp.minRealm,
                    minRealmName: this.realmName(bp.minRealm),
                    realmOk: realm.index >= bp.minRealm,
                    element: bp.element || null,
                    desc: bp.desc,
                    targetKind: bp.targetKind,
                    targetId: bp.targetId,
                };
            }),
            roamingPlayers: Object.values(this.data.players || {})
                .filter(pl => pl.registered && !this.isHiddenFromPlayers(pl) && pl.isRoaming && String(pl.userId) !== String(userId) && (C.TOWN_BY_ID.get(pl.town || 'thanh_van')?.mapId === currentTown.mapId))
                .map(pl => ({
                    userId: pl.userId,
                    name: pl.name,
                    fullName: pl.fullName || pl.name,
                    photoUrl: pl.photoUrl || null,
                    power: this.stats(pl, now).power,
                    realmName: this.realmName(this.realmOf(pl.userId).index),
                    townName: (C.TOWN_BY_ID.get(pl.town || 'thanh_van') || C.TOWNS[0]).name,
                    isDemon: ((pl.maScore || 0) > 0 && (pl.maScore || 0) > Math.max(0, pl.daoScore ?? pl.daoTam ?? 100)) || Boolean(pl.isDemon),
                })),
            npcLeaderboard: this.getNpcLeaderboard(15, false),
            wantedList: Object.values(this.data.players || {})
                .filter(pl => pl.registered && !this.isHiddenFromPlayers(pl) && (pl.isDemon || ((pl.maScore || 0) > 0 && (pl.maScore || 0) > Math.max(0, pl.daoScore ?? pl.daoTam ?? 100))) && String(pl.userId) !== String(userId))
                .map(pl => ({
                    userId: pl.userId,
                    name: pl.name,
                    fullName: pl.fullName || pl.name,
                    photoUrl: pl.photoUrl || null,
                    killCount: pl.killCount || 0,
                    bounty: Math.round(250 + (pl.killCount || 5) * 50),
                    realmName: this.realmName(this.realmOf(pl.userId).index),
                    power: this.stats(pl, now).power,
                    townName: (C.TOWN_BY_ID.get(pl.town || 'thanh_van') || C.TOWNS[0]).name,
                }))
                .sort((a, b) => b.killCount - a.killCount || b.power - a.power),
            shop: this.townShop(p.town || 'thanh_van'),
            shopDate: vnDate(now),
            inboxUnread: (p.inbox || []).filter(m => !m.read && !m.claimed).length,
            inboxCount: (p.inbox || []).length,
            marketTax: C.RULES.marketTax,
            party: this.partyView(userId),
            injuryMin: Math.round(C.RULES.injuryMs / 60000),
            inBattle: Boolean(battle && !battle.over),
        };
    }

    // Những gì mở ra ở 3 cảnh giới kế tiếp.
    realmRoad(current) {
        const out = [];
        for (let r = current + 1; r <= Math.min(65, current + 3); r += 1) {
            const unlocks = [];
            C.RULES.skillSlotRealms.forEach((need, i) => { if (need === r) unlocks.push(`Mở ô kỹ năng ${i + 1}`); });
            for (const sk of C.SKILLS) if (sk.grant && sk.realm === r) unlocks.push(`Lĩnh ngộ ${sk.name}`);
            const setCount = C.SKILLS.filter(sk => sk.set === r && !sk.grant).length;
            if (setCount) unlocks.push(`Bộ công pháp ${this.realmName(r)}: thêm ${setCount} ngọc giản rơi từ yêu thú`);
            for (const tier of Object.values(C.TIER)) if (tier.realm === r) unlocks.push(`Dùng được trang bị ${tier.name} phẩm`);
            const monSkills = C.SKILLS.filter(sk => !sk.set && sk.realm === r).length;
            if (monSkills) unlocks.push(`Học được ${monSkills} công pháp môn phái cấp cao`);
            out.push({ index: r, name: this.realmName(r), unlocks });
        }
        return out;
    }

    // ---- phường thị: người chơi bán đồ cho nhau -----------------------------
    fitsInPlace(p, item, place) {
        const slotCapacity = this.capacity(place, p);
        const def = itemDef(item);
        if (item.kind === 'equip' || item.unique || def?.unique) return this.countPlace(p, place) < slotCapacity;
        const qty = Math.max(1, Math.floor(Number(item.qty) || 1));
        let room = Math.max(0, slotCapacity - this.countPlace(p, place)) * C.RULES.stackMax;
        for (const it of p.items) {
            if (it.place === place && it.kind === item.kind && it.id === item.id) {
                room += Math.max(0, C.RULES.stackMax - (Number(it.qty) || 1));
            }
        }
        return room >= qty;
    }

    fitsInBag(p, item) { return this.fitsInPlace(p, item, 'bag'); }

    // Đưa vật phẩm vào túi/kho; kho được phép vượt sức chứa khi trả hàng về.
    giveItem(p, item, place, keepRefine) {
        if (item.kind === 'equip') {
            const copy = { ...item, place };
            if (!keepRefine) { copy.refinedBy = null; copy.refineAt = 0; }
            p.items.push(copy);
            return;
        }
        const added = this.addStack(p, item.kind, item.id, item.qty, place);
        let left = item.qty - added;
        if (left > 0 && place === 'bag') left -= this.addStack(p, item.kind, item.id, left, 'kho');
        while (left > 0) {
            const n = Math.min(left, C.RULES.stackMax);
            p.items.push({ uid: newId(), kind: item.kind, id: item.id, qty: n, place: 'kho', at: this.now() });
            left -= n;
        }
    }

    marketList(userId, itemUid, price, qty) {
        const p = this.requirePlayer(userId);
        this.requireIdle(p);
        const item = this.findItem(p, itemUid);
        if (item.place === 'equip') fail('Hãy tháo trang bị trước khi bán.');
        if (item.place === 'safe') fail('Hãy lấy vật phẩm khỏi Kho An Toàn trước khi treo bán.');
        if (itemDef(item)?.bound) fail('Đồ Tân Thủ là quà, không giao dịch được.');
        const total = Math.floor(Number(price));
        if (!(total >= 1 && total <= C.RULES.marketMaxPrice)) fail('Giá không hợp lệ.');
        const key = String(p.userId);
        const mine = Object.values(this.market.listings).filter(l => l.seller === key).length;
        if (mine >= C.RULES.marketMaxListings) fail(`Mỗi người treo bán tối đa ${C.RULES.marketMaxListings} món.`);
        const stacks = this.marketStackItems(p, item);
        const available = this.stackQuantity(stacks);
        const requested = qty == null ? available : Number(qty);
        if (!Number.isInteger(requested) || requested < 1 || requested > available) fail(`Số lượng không hợp lệ. Hiện có ${available} món cùng loại trong túi và kho.`);
        const n = requested;
        const listed = { ...item, uid: n === available && stacks.length === 1 ? item.uid : newId(), qty: n, place: 'market' };
        this.removeStackQuantity(p, stacks, n);
        const now = this.now();
        const id = newId();
        this.market.listings[id] = { id, seller: key, sellerName: p.name, item: listed, price: total, at: now, expiresAt: now + C.RULES.marketDurationMs };
        this.touch();
        return { id, name: itemName(listed), qty: n, price: total };
    }

    marketCancel(userId, listingId) {
        const p = this.requirePlayer(userId);
        this.requireIdle(p);
        const listing = this.market.listings[listingId];
        if (!listing || listing.seller !== String(p.userId)) fail('Không tìm thấy món bạn đang bán.');
        delete this.market.listings[listingId];
        this.giveItem(p, listing.item, this.fitsInBag(p, listing.item) ? 'bag' : 'kho', true);
        this.touch();
        return itemName(listing.item);
    }

    ensureRareShop(now = this.now()) {
        const market = this.market;
        const today = vnDate(now);
        const scheduleVersion = 7;
        if (market.rareShopDate === today && market.rareShopVersion === scheduleVersion && Array.isArray(market.rareShopSchedule)) {
            const schedule = market.rareShopSchedule;
            let changed = false;
            for (const slot of schedule) {
                if (slot.soldAt || slot.startsAt > now || now >= slot.expiresAt) continue;
                if (!slot.announcedAppear) {
                    slot.announcedAppear = true;
                    this.broadcastPersonalNotice(`🏛️ [PHƯỜNG THỊ THẦN BÍ · PHIÊN ${slot.session || Math.floor(Number(slot.index) / 10) + 1}] ${slot.itemName} đã xuất hiện tại Phường Thị (${slot.townName}), giá ${slot.price.toLocaleString('vi-VN')} linh thạch. Món hàng chỉ có 1 suất toàn server.`, slot.startsAt);
                    changed = true;
                }
                const id = `rare_shop_${today}_${slot.index}`;
                if (market.listings[id]) continue;
                market.listings[id] = {
                    id,
                    seller: 'system_rare_shop',
                    sellerName: 'Phường Thị Thần Bí',
                    item: { uid: id, kind: slot.kind || 'mat', id: slot.itemId, qty: 1, place: 'market', at: slot.startsAt },
                    price: slot.price,
                    at: slot.startsAt,
                    expiresAt: slot.expiresAt,
                    rareShop: true,
                    rareShopSlot: slot.index,
                    townName: slot.townName,
                };
                changed = true;
            }
            if (changed) this.touch();
            return changed;
        }

        for (const listing of Object.values(market.listings)) {
            if (listing.rareShop) delete market.listings[listing.id];
        }

        // Khi cập nhật lịch trong cùng ngày, giữ các món đã được mua khỏi bị
        // quay lại kho hàng do đổi định dạng lịch.
        const alreadySold = new Set((market.rareShopDate === today ? market.rareShopSchedule || [] : [])
            .filter(slot => slot.soldAt)
            .map(slot => `${slot.kind || 'mat'}:${slot.itemId}`));
        const dayStart = Date.parse(`${today}T00:00:00.000Z`) - VN_OFFSET_MS;
        const dayEnd = dayStart + 24 * 60 * 60 * 1000;
        const towns = C.TOWNS || [];
        const rareTierBands = {
            pham: [500_000, 1_200_000],
            hoang: [1_200_000, 2_800_000],
            huyen: [2_800_000, 4_800_000],
            dia: [4_800_000, 7_600_000],
            thien: [7_600_000, 10_000_000],
        };
        const tierWeights = { pham: 15, hoang: 20, huyen: 25, dia: 23, thien: 17 };
        const tierForRank = rank => ['pham', 'hoang', 'huyen', 'dia', 'thien'][clamp(Math.floor(Number(rank) || 0), 0, 4)];
        const pool = [];
        const addRareOffer = (kind, def, tier = def?.tier) => {
            if (!def?.id || !rareTierBands[tier] || alreadySold.has(`${kind}:${def.id}`)) return;
            pool.push({ kind, id: def.id, name: def.name, icon: def.icon || '💎', desc: def.desc || '', tier, tierName: def.qualityName || def.tierName || C.TIER[tier]?.name || 'Hiếm' });
        };
        for (const material of C.MATERIALS || []) {
            const tier = material.tier || tierForRank(material.qualityRank);
            if ((Number(material.qualityRank) || 0) >= 2) addRareOffer('mat', material, tier);
        }
        for (const equipment of C.EQUIPMENT || []) {
            const rank = Number(equipment.qualityRank ?? C.TIER[equipment.tier]?.rank) || 0;
            const isRare = rank >= 2 || (Number(equipment.drop) > 0 && Number(equipment.drop) <= 0.01);
            if (isRare && !equipment.bound && !equipment.unique) {
                addRareOffer('equip', equipment, tierForRank(rank));
            }
        }
        for (const consumable of C.CONSUMABLES || []) {
            const rank = Number(consumable.qualityRank ?? C.TIER[consumable.tier]?.rank) || 0;
            if (rank >= 2 && !consumable.bound && !consumable.unique) {
                addRareOffer('cons', consumable, tierForRank(rank));
            }
        }
        for (const fire of C.CRAFT_FIRES || []) addRareOffer('mat', fire, tierForRank(fire.fireRank));
        for (const gourd of C.BEAST_GOURDS || []) {
            if (!gourd.starter && (Number(gourd.qualityRank) || 0) >= 2 && Number(gourd.dropChance) > 0) {
                addRareOffer('tool', gourd, tierForRank(gourd.qualityRank));
            }
        }
        for (const skill of C.SKILLS || []) {
            if (!skill.grant && Number(skill.drop) > 0 && (Number(skill.qualityRank) >= 2 || Number(skill.realm) >= 4)) {
                addRareOffer('scroll', skill, tierForRank(skill.qualityRank ?? Math.floor((Number(skill.realm) || 0) / 3)));
            }
        }

        const usedRareIds = new Set();
        const drawRareOffer = (sourcePool = pool) => {
            const available = sourcePool.filter(item => !usedRareIds.has(`${item.kind}:${item.id}`));
            const availableTiers = Object.keys(rareTierBands).filter(tier => available.some(item => item.tier === tier));
            const tier = pickWeighted(availableTiers, value => tierWeights[value], this.rng);
            const candidates = available.filter(item => item.tier === tier);
            const item = candidates[Math.floor(this.rng() * candidates.length)];
            if (!item) return null;
            usedRareIds.add(`${item.kind}:${item.id}`);
            const [minPrice, maxPrice] = rareTierBands[tier];
            const price = Math.min(maxPrice, Math.round((minPrice + this.rng() * (maxPrice - minPrice)) / 10_000) * 10_000);
            return { ...item, price };
        };
        const offers = [];
        for (let session = 0; session < 2; session += 1) {
            for (const kind of ['tool', 'cons']) {
                const offer = drawRareOffer(pool.filter(item => item.kind === kind));
                if (offer) offers.push(offer);
            }
            while (offers.length < (session + 1) * 10) {
                const offer = drawRareOffer();
                if (!offer) break;
                offers.push(offer);
            }
        }

        market.rareShopDate = today;
        market.rareShopVersion = scheduleVersion;
        const openMinutes = [
            6 * 60 + Math.floor(this.rng() * (5 * 60)),
            13 * 60 + Math.floor(this.rng() * (8 * 60)),
        ];
        const sessionTowns = openMinutes.map(() => towns[Math.floor(this.rng() * towns.length)]);
        market.rareShopSchedule = offers.map((offer, index) => {
            if (!offer || !towns.length) return null;
            const session = Math.floor(index / 10) + 1;
            const startsAt = dayStart + openMinutes[session - 1] * 60 * 1000;
            const town = sessionTowns[session - 1];
            return {
                index,
                session,
                sessionIndex: index % 10,
                itemId: offer.id,
                kind: offer.kind,
                itemName: offer.name,
                icon: offer.icon,
                desc: offer.desc,
                tier: offer.tier,
                tierName: offer.tierName,
                startsAt,
                expiresAt: dayEnd,
                price: offer.price,
                townName: town.name,
                soldAt: null,
            };
        }).filter(Boolean);
        for (const slot of market.rareShopSchedule) {
            if (slot.startsAt > now) continue;
            if (!slot.announcedAppear) {
                slot.announcedAppear = true;
                this.broadcastPersonalNotice(`🏛️ [PHƯỜNG THỊ THẦN BÍ · PHIÊN ${slot.session}] ${slot.itemName} đã xuất hiện tại Phường Thị (${slot.townName}), giá ${slot.price.toLocaleString('vi-VN')} linh thạch. Món hàng chỉ có 1 suất toàn server.`, slot.startsAt);
            }
            const id = `rare_shop_${today}_${slot.index}`;
            market.listings[id] = {
                id,
                seller: 'system_rare_shop',
                sellerName: 'Phường Thị Thần Bí',
                item: { uid: id, kind: slot.kind || 'mat', id: slot.itemId, qty: 1, place: 'market', at: slot.startsAt },
                price: slot.price,
                at: slot.startsAt,
                expiresAt: slot.expiresAt,
                rareShop: true,
                rareShopSlot: slot.index,
                townName: slot.townName,
            };
        }
        this.touch();
        return true;
    }

    marketBuy(userId, listingId) {
        const p = this.requirePlayer(userId);
        this.requireIdle(p);
        this.ensureRareShop();
        const listing = this.market.listings[listingId];
        if (!listing) fail('Món này đã được bán hoặc đã gỡ.');
        if (this.isHiddenFromPlayers(listing.seller)) fail('Món này đã được bán hoặc đã gỡ.');
        if (listing.seller === String(p.userId)) fail('Không mua được đồ của chính mình.');
        if (p.stones < listing.price) fail('Không đủ linh thạch.');
        if (!this.fitsInBag(p, listing.item)) fail('Túi đầy, cất bớt vào kho trước.');
        const seller = this.player(listing.seller);
        delete this.market.listings[listingId];
        if (listing.rareShop && Number.isInteger(listing.rareShopSlot)) {
            const slot = this.market.rareShopSchedule?.find(entry => entry.index === listing.rareShopSlot);
            if (slot) {
                slot.soldAt = this.now();
                    this.broadcastPersonalNotice(`🏛️ [PHƯỜNG THỊ THẦN BÍ · PHIÊN ${slot.session}] ${itemName(listing.item)} tại ${slot.townName} đã được ${p.name} mua. Suất duy nhất đã hết trên toàn server.`, this.now());
            }
        }
        const tax = Math.ceil(listing.price * C.RULES.marketTax);
        const gain = listing.price - tax;
        p.stones -= listing.price;
        if (seller) seller.stones += gain;
        this.ledger(-tax);
        this.giveItem(p, listing.item, 'bag', false);
        if (seller) {
            const notice = `🛒 [PHƯỜNG THỊ GIAO DỊCH] ${p.name} đã mua ${itemName(listing.item)}${listing.item.qty > 1 ? ` ×${listing.item.qty}` : ''} với giá ${listing.price.toLocaleString('vi-VN')} linh thạch. Bạn nhận ${gain.toLocaleString('vi-VN')} linh thạch sau thuế.`;
            this.recordPersonalEvent(listing.seller, notice);
        }
        this.touch();
        const name = itemName(listing.item);
        this.emit('sold', { seller: listing.seller, buyerName: p.name, name, qty: listing.item.qty, price: listing.price, gain });
        return { name, qty: listing.item.qty, price: listing.price };
    }

    sweepMarket(now = this.now()) {
        for (const listing of Object.values(this.market.listings)) {
            if (listing.expiresAt > now) continue;
            delete this.market.listings[listing.id];
            const seller = this.player(listing.seller);
            if (seller) {
                this.giveItem(seller, listing.item, 'kho', true);
                const notice = `⏳ [PHƯỜNG THỊ HẾT HẠN] ${itemName(listing.item)}${listing.item.qty > 1 ? ` ×${listing.item.qty}` : ''} đã hết hạn và được trả về Kho.`;
                this.recordPersonalEvent(listing.seller, notice);
                this.emit('expired', { seller: listing.seller, name: itemName(listing.item), qty: listing.item.qty });
            } else if (!listing.rareShop && listing.item.kind === 'equip') this.stockAdd('item', listing.item.id, -1);
            else if (!listing.rareShop && listing.item.kind === 'scroll') this.stockAdd('skill', listing.item.id, -listing.item.qty);
            this.touch();
        }
    }

    marketView(userId) {
        const p = this.player(userId);
        const key = String(userId);
        const now = this.now();
        this.ensureRareShop(now);
        return {
            tax: C.RULES.marketTax,
            rareShopSchedule: (this.market.rareShopSchedule || []).map(slot => ({
                index: slot.index,
                session: slot.session || Math.floor(Number(slot.index) / 10) + 1,
                sessionIndex: slot.sessionIndex ?? (Number(slot.index) % 10),
                itemId: slot.itemId,
                kind: slot.kind || 'mat',
                itemName: slot.itemName,
                icon: slot.icon || '💎',
                desc: slot.desc || '',
                tier: slot.tier || 'huyen',
                tierName: slot.tierName || C.TIER[slot.tier]?.name || 'Hiếm',
                startsAt: slot.startsAt,
                expiresAt: slot.expiresAt,
                price: slot.price,
                townName: slot.townName,
                status: slot.soldAt ? 'sold' : (now < slot.startsAt ? 'upcoming' : (now < slot.expiresAt ? 'available' : 'expired')),
            })),
            listings: Object.values(this.market.listings)
                .filter(l => l.expiresAt > now && !this.isHiddenFromPlayers(l.seller))
                .sort((a, b) => Number(Boolean(b.rareShop)) - Number(Boolean(a.rareShop)) || b.at - a.at)
                .slice(0, 200)
                .map(l => ({ id: l.id, sellerName: l.sellerName, mine: l.seller === key, price: l.price, at: l.at, expiresAt: l.expiresAt, ...(l.rareShop ? { rareShop: true, rareShopSlot: l.rareShopSlot, townName: l.townName } : {}), item: this.describeItem(l.item, p) })),
        };
    }

    rollCuratedDungeonMaterialDrops(dungeon, player, drops) {
        if (!dungeon || !player || !Array.isArray(drops)) return;
        for (const [itemId, rate] of DUNGEON_MATERIAL_DROP_RULES_BY_DUNGEON.get(dungeon.id) || []) {
            if (this.rng() >= rate || this.addStack(player, 'mat', itemId, 1) <= 0) continue;
            drops.push(`${C.MATERIAL_BY_ID.get(itemId)?.name || itemId} ×1`);
        }
    }

    // ---- Tra cứu nguồn gốc vật phẩm (Item Drop Sources Lookup) -------------
    getItemDropSources(itemId) {
        if (!itemId) return { sources: [], summary: 'Chưa có thông tin' };

        if (!this._monsterTownsMap) {
            this._monsterTownsMap = new Map();
            for (const t of C.TOWNS || []) {
                for (const mId of (t.monsterPool || [])) {
                    if (!this._monsterTownsMap.has(mId)) this._monsterTownsMap.set(mId, []);
                    this._monsterTownsMap.get(mId).push(t.name);
                }
            }
        }

        const sources = [];

        // 1. Quái rơi trực tiếp
        const droppingMonsters = [];
        for (const { monster: m, drop } of MONSTER_DROP_SOURCES_BY_ITEM.get(itemId) || []) {
            const towns = this._monsterTownsMap.get(m.id) || [];
            const townStr = towns.length ? ' ở ' + towns.slice(0, 3).join(', ') + (towns.length > 3 ? '...' : '') : '';
            const shownRate = MONSTER_DROP_RATE_BY_ITEM.get(itemId)?.get(m.id) || 0;
            const rateStr = shownRate ? ` [Tỉ lệ ~${formatDropPercent(shownRate)}%]` : '';
            droppingMonsters.push(`${m.name} (${this.realmName(m.realm)} · ${m.small ? 'Tiểu yêu' : (m.worldBoss ? 'Đại Boss Thế Giới' : 'Đại yêu')}${townStr})${rateStr}`);
        }
        if (droppingMonsters.length) {
            sources.push({ type: 'monster', title: '🐾 Săn Yêu Quái Vật', list: droppingMonsters });
        }
        const supplementalSources = (SUPPLEMENTAL_ITEM_DROPS_BY_ITEM.get(itemId) || []).map(({ monster, rate }) => {
            const towns = this._monsterTownsMap.get(monster.id) || [];
            const townText = towns.length ? ` tại ${towns.slice(0, 2).join(', ')}` : '';
            return `${monster.name} (${this.realmName(monster.realm)})${townText} [Tỉ lệ ~${formatDropPercent(rate)}%]`;
        });
        if (supplementalSources.length) sources.push({ type: 'monster', title: '🐾 Săn Yêu Quái Vật', list: supplementalSources });
        const materialDef = C.MATERIAL_BY_ID?.get(itemId);
        if (materialDef?.generated) {
            const sourceInfo = GENERATED_MATERIAL_SOURCE_BY_ITEM.get(itemId);
            const assignedSources = sourceInfo?.monsters || [];
            if (assignedSources.length) sources.push({
                type: 'monster', title: '🐾 Săn Yêu Quái Vật',
                list: assignedSources.map(monster => {
                    const towns = this._monsterTownsMap.get(monster.id) || [];
                    const townText = towns.length ? ` tại ${towns.slice(0, 2).join(', ')}` : '';
                    const rate = generatedMaterialRate(itemId, monster);
                    return `${monster.name} (${this.realmName(monster.realm)})${townText} [Tỉ lệ ~${formatDropPercent(rate)}%]`;
                }),
            });
        }

        const sourceMonsterText = monsterId => {
            const monster = C.MONSTER_BY_ID?.get(monsterId);
            if (!monster) return 'nguồn chưa được cấu hình';
            const places = [];
            const towns = this._monsterTownsMap.get(monsterId) || [];
            if (towns.length) places.push(...towns);
            const dungeons = (C.DUNGEONS || []).filter(d => (d.stages || []).some(stage => stage.monsterId === monsterId)).map(d => d.name);
            if (dungeons.length) places.push(...dungeons);
            if (monster.worldBoss) places.push('đợt Boss Thế Giới');
            return `${monster.name} (${this.realmName(monster.realm)})${places.length ? ` tại ${[...new Set(places)].slice(0, 5).join(', ')}` : ''}`;
        };
        const fire = C.FIRE_BY_ID?.get(itemId);
        if (fire) {
            const assignedSources = (fire.sourceMonsterIds || [fire.sourceMonsterId]).map(id => C.MONSTER_BY_ID.get(id)).filter(Boolean);
            const list = (FIRE_DROP_RATE_BY_ITEM.get(itemId) ? [...FIRE_DROP_RATE_BY_ITEM.get(itemId)] : [])
                .map(([monsterId, rate]) => `Rơi từ ${sourceMonsterText(monsterId)}; tỷ lệ cơ bản ${formatDropPercent(rate)}%.`);
            if (!list.length) list.push(...assignedSources.map(monster => `Rơi từ ${sourceMonsterText(monster.id)}; tỷ lệ cơ bản ${formatDropPercent(fire.dropChance)}%.`));
            sources.push({ type: 'world_drop', title: `🔥 Dị Hỏa ${fire.fireQualityName} phẩm`, list });
        }
        const gourd = C.BEAST_GOURD_BY_ID?.get(itemId);
        if (gourd) {
            const list = [];
            if (gourd.starter) list.push('Được tặng khi tạo nhân vật mới; Hồ Lô Phàm phẩm là dụng cụ nhập môn.');
            for (const monster of GOURD_SOURCE_BY_ITEM.get(itemId) || []) {
                const rate = GOURD_DROP_RATE_BY_ITEM.get(itemId)?.get(monster.id) || 0;
                list.push(`Rơi từ ${sourceMonsterText(monster.id)}; tỷ lệ cơ bản ${formatDropPercent(rate)}%.`);
            }
            if (!gourd.starter) list.push(`Cần cảnh giới ${this.realmName(gourd.realmMin)} trở lên; tăng ${Math.round(gourd.captureBonus * 100)} điểm phần trăm tỷ lệ thu phục.`);
            sources.push({ type: 'world_drop', title: `🏺 Hồ Lô ${gourd.tierName} phẩm`, list });
        }

        // Equipment specific sources
        const eq = C.EQUIP_BY_ID?.get(itemId);
        if (eq) {
            const trialSources = [];
            for (const trial of (C.EQUIP_DUNGEONS || [])) {
                if (trial.equipPool && trial.equipPool.includes(eq.id)) {
                    trialSources.push(`[${trial.name}] (${this.realmName(trial.realmMin)} - ${this.realmName(trial.realmMax)}): Bảo đảm 100% rơi trang bị phái khi vượt ải.`);
                }
            }
            if (trialSources.length) {
                sources.push({ type: 'trial_dungeon', title: '⚔️ Bí Cảnh Thí Luyện Trang Bị (100% Rơi)', list: trialSources });
            }

            const eqSrc = (EQUIPMENT_SOURCE_BY_ITEM.get(itemId) || []).map(monster => {
                const rate = EQUIPMENT_DROP_RATE_BY_ITEM.get(itemId)?.get(monster.id) || 0;
                return `Rơi từ ${sourceMonsterText(monster.id)}; tỷ lệ cơ bản ${formatDropPercent(rate)}%.`;
            });
            if (eq.bound) eqSrc.push('Quà Tân Thủ (nhận khi tạo nhân vật)');
            if (eqSrc.length) sources.push({ type: 'world_drop', title: '⚔️ Rơi Dã Ngoại', list: eqSrc });
        }

        // Ngọc giản có nguồn quái cụ thể và tỷ lệ trùng với lúc thả vật phẩm.
        const sk = C.SKILL_BY_ID?.get(itemId);
        if (sk?.grant) {
            sources.push({
                type: 'realm', title: '✨ Tự lĩnh ngộ khi đột phá',
                list: [`Tự lĩnh ngộ khi đột phá đến ${this.realmName(sk.realm)}.`],
            });
        }
        if (sk) {
            const skillSources = (SKILL_SOURCE_BY_ITEM.get(itemId) || []).map(monster => {
                const rate = SKILL_DROP_RATE_BY_ITEM.get(itemId)?.get(monster.id) || 0;
                return `Rơi từ ${sourceMonsterText(monster.id)}; tỷ lệ cơ bản ${formatDropPercent(rate)}%.`;
            });
            if (skillSources.length) sources.push({ type: 'skill_drop', title: '✨ Rơi Ngọc Giản', list: skillSources });
        }

        // 2. Cổ Động Bí Cảnh
        const dungeons = [];
        for (const d of C.DUNGEONS || []) {
            if (d.guaranteedPill === itemId) {
                dungeons.push(`${d.name} (Cảnh giới ${this.realmName(d.realmMin)} trở lên): Rơi đảm bảo khi vượt ải`);
            } else if ((d.bonusPills || []).includes(itemId)) {
                dungeons.push(`${d.name} (Cảnh giới ${this.realmName(d.realmMin)} trở lên): Tỷ lệ thưởng thêm`);
            } else if (d.equipDrop === itemId) {
                dungeons.push(`${d.name} (Cảnh giới ${this.realmName(d.realmMin)} trở lên): Rơi trang bị độc quyền`);
            }
        }
        if (itemId === 'mat_yeu_dan' || itemId === 'mat_van_thiet') {
            dungeons.push(`Rơi cố định tại tất cả ${C.DUNGEONS.length} Cổ Động Bí Cảnh (2-5 cái mỗi lần)`);
        }
        for (const source of DUNGEON_MATERIAL_DROP_SOURCES_BY_ITEM.get(itemId) || []) {
            const dungeon = C.DUNGEON_BY_ID?.get(source.dungeonId);
            if (!dungeon) continue;
            const bossId = dungeon.stages?.at(-1)?.monsterId;
            const boss = C.MONSTER_BY_ID?.get(bossId);
            const elementName = boss ? C.HE[boss.element]?.name : null;
            const bossText = boss ? ` · thủ lĩnh ${boss.name}${elementName ? ` hệ ${elementName}` : ''}` : '';
            dungeons.push(`${dungeon.name}${bossText} (${this.realmName(dungeon.realmMin)} trở lên): ${formatDropPercent(source.rate)}%`);
        }
        if (dungeons.length) {
            sources.push({ type: 'dungeon', title: '🏔️ Cổ Động Bí Cảnh', list: dungeons });
        }

        // 3. Luyện đan / Rèn đúc / Vẽ phù
        const crafts = [];
        const potion = (C.CRAFT_POTION_RECIPES || []).find(r => r.consId === itemId ||
            (itemId === 'dan_truc_co' && r.consId === 'truc_co_dan'));
        if (potion) {
            const mats = Object.entries(potion.materials || {}).map(([mId, qty]) => `${C.MATERIAL_BY_ID?.get(mId)?.name || mId} ×${qty}`).join(', ');
            crafts.push(`Đan Sư luyện chế: cần ${mats} (Tốn ${potion.stones} linh thạch)`);
        }
        const equipCraft = (C.CRAFT_EQUIP_RECIPES || []).find(r => r.equipId === itemId);
        if (equipCraft) {
            const mats = Object.entries(equipCraft.materials || {}).map(([mId, qty]) => `${C.MATERIAL_BY_ID?.get(mId)?.name || mId} ×${qty}`).join(', ');
            crafts.push(`Thợ Rèn đúc: cần ${mats} (Tốn ${equipCraft.stones} linh thạch)`);
        }
        const talismanCraft = (C.CRAFT_TALISMAN_RECIPES || []).find(r => r.consId === itemId);
        if (talismanCraft) {
            const mats = Object.entries(talismanCraft.materials || {}).map(([mId, qty]) => `${C.MATERIAL_BY_ID?.get(mId)?.name || mId} ×${qty}`).join(', ');
            crafts.push(`Phù Sư vẽ: cần ${mats} (Tốn ${talismanCraft.stones} linh thạch)`);
        }
        if (crafts.length) {
            sources.push({ type: 'craft', title: '⚒️ Luyện Chế / Rèn Đúc', list: crafts });
        }

        // 4. Bản thảo
        const bp = (C.BLUEPRINTS || []).find(b => b.targetId === itemId ||
            (itemId === 'dan_truc_co' && b.targetId === 'truc_co_dan'));
        if (bp) {
            const mat = C.MATERIAL_BY_ID?.get(bp.matId)?.name || bp.matId;
            sources.push({ type: 'blueprint', title: '📜 Bản Thảo', list: [`Hợp nhất từ ${bp.reqCount} ${mat} (Yêu cầu cảnh giới ${this.realmName(bp.minRealm)})`] });
        }
        const bpAsMat = (C.BLUEPRINTS || []).find(b => b.matId === itemId);
        if (bpAsMat) {
            sources.push({ type: 'blueprint_mat', title: '📜 Dùng để hợp nhất', list: [`Gom đủ ${bpAsMat.reqCount} mảnh để hợp nhất thành [${bpAsMat.name}]`] });
        }

        // 5. Cửa hàng
        const cons = C.CONSUMABLE_BY_ID?.get(itemId);
        if (cons && cons.price && !cons.breakthrough) {
            sources.push({ type: 'shop', title: '🏪 Tiệm Thuốc Thành Trấn', list: [`Bán tại Tiệm Thuốc ở các Thành Trấn (Giá: ${cons.price} linh thạch)`] });
        }

        // Tóm tắt ngắn gọn 1 dòng cho badge / tooltip
        let summaryParts = [];
        for (const s of sources) {
            if (s.type === 'dungeon') summaryParts.push(`Cổ Động (${s.list.length})`);
            else if (s.type === 'monster') summaryParts.push(`Quái (${s.list.length})`);
            else if (s.type === 'skill_drop') summaryParts.push('Yêu thú rơi ngọc giản');
            else if (s.type === 'realm') summaryParts.push('Tự lĩnh ngộ khi đột phá');
            else if (s.type === 'unconfigured') summaryParts.push('Chưa cấu hình nguồn nhận');
            else if (s.type === 'craft') summaryParts.push('Nghề chế tác');
            else if (s.type === 'blueprint') summaryParts.push('Bản thảo');
            else if (s.type === 'blueprint_mat') summaryParts.push('Mảnh bản thảo');
            else if (s.type === 'shop') summaryParts.push('Tiệm thuốc');
            else if (s.type === 'world_drop') summaryParts.push('Rơi dã ngoại');
        }
        const summary = summaryParts.length ? summaryParts.join(' · ') : 'Chưa có thông tin';

        return { sources, summary };
    }

    searchItemDropSources(query) {
        if (!query || !query.trim()) return [];
        const q = query.trim().toLowerCase();
        const removeVietnameseTones = str => String(str || '')
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .replace(/[đĐ]/g, m => m === 'Đ' ? 'D' : 'd')
            .toLowerCase();
        const normQ = removeVietnameseTones(q);

        const allItems = [];
        const seen = new Set();
        const add = (id, name, kind, icon, desc, extra = {}) => {
            if (!id || seen.has(id)) return;
            seen.add(id);
            allItems.push({ id, name, kind, icon, desc, ...extra });
        };

        for (const m of C.MATERIALS || []) add(m.id, m.name, 'mat', m.icon || '📦', m.desc);
        for (const fire of C.CRAFT_FIRES || []) add(fire.id, fire.name, 'fire', fire.icon, fire.desc, { tierName: `${fire.fireQualityName} phẩm`, realmName: this.realmName(fire.realmMin) });
        for (const gourd of C.BEAST_GOURDS || []) add(gourd.id, gourd.name, 'gourd', gourd.icon, gourd.desc, { tierName: `${gourd.tierName} phẩm`, realmName: this.realmName(gourd.realmMin) });
        for (const c of C.CONSUMABLES || []) add(c.id, c.name, 'cons', c.icon || (c.talisman ? '📜' : '💊'), c.desc, { price: c.price, breakthrough: c.breakthrough });
        for (const e of C.EQUIPMENT || []) add(e.id, e.name, 'equip', e.icon || '🗡️', e.desc || '', { tier: e.tier, tierName: C.TIER[e.tier]?.name, slot: e.slot });
        for (const s of C.SKILLS || []) add(s.id, s.name, 'skill', s.icon || '✨', s.desc || '', { realm: s.realm, realmName: this.realmName(s.realm) });

        const matches = allItems.filter(item => {
            const nameMatch = removeVietnameseTones(item.name).includes(normQ);
            const idMatch = item.id.toLowerCase().includes(q);
            return nameMatch || idMatch;
        });

        // Ưu tiên trùng khớp chính xác tên lên trước
        matches.sort((a, b) => {
            const aExact = removeVietnameseTones(a.name) === normQ ? -1 : 0;
            const bExact = removeVietnameseTones(b.name) === normQ ? -1 : 0;
            return aExact - bExact;
        });

        return matches.slice(0, 10).map(item => {
            const dropInfo = this.getItemDropSources(item.id);
            return {
                id: item.id,
                name: item.name,
                kind: item.kind,
                icon: item.icon,
                desc: item.desc,
                tierName: item.tierName,
                realmName: item.realmName,
                price: item.price,
                sources: dropInfo.sources,
                summary: dropInfo.summary,
            };
        });
    }

    // ---- Vạn Bảo Lục: toàn bộ vật phẩm, công pháp, yêu thú -------------------
    codex(userId = null) {
        const traits = { armor: 'Da dày, giảm đòn thường', crit: 'Chí mạng cao', double: 'Có lúc liên kích (2 đòn liền)', burn: 'Gây bỏng', slow: 'Làm chậm', lifesteal: 'Hút máu' };
        const monsterName = id => C.MONSTER_BY_ID.get(id)?.name || id;
        const player = userId == null ? null : this.player(userId);
        const remaining = (kind, id, stock) => (stock == null ? null : Math.max(0, stock - this.stockCount(kind, id)));
        return {
            materials: (C.MATERIALS || []).map(m => {
                const dropInfo = this.getItemDropSources(m.id);
                return {
                    id: m.id,
                    name: m.name,
                    icon: m.icon || '📦',
                    tier: m.tier,
                    tierName: C.TIER[m.tier]?.name || m.qualityName || 'Phàm',
                    qualityRank: m.qualityRank ?? C.TIER[m.tier]?.rank ?? 0,
                    realmMin: m.realmMin ?? m.minRealm ?? C.TIER[m.tier]?.realm ?? 0,
                    realmMinName: this.realmName(m.realmMin ?? m.minRealm ?? C.TIER[m.tier]?.realm ?? 0),
                    desc: m.desc,
                    sell: m.sell,
                    dropSources: dropInfo,
                };
            }),
            fires: (C.CRAFT_FIRES || []).map(fire => ({
                id: fire.id, name: fire.name, icon: fire.icon, desc: fire.desc,
                qualityName: fire.fireQualityName, qualityRank: fire.fireRank,
                realmMin: fire.realmMin, realmMinName: this.realmName(fire.realmMin),
                sourceMonsterIds: fire.sourceMonsterIds || [fire.sourceMonsterId],
                dropSources: this.getItemDropSources(fire.id),
            })),
            beastGourds: (C.BEAST_GOURDS || []).map(gourd => ({
                id: gourd.id, name: gourd.name, icon: gourd.icon, desc: gourd.desc,
                tierName: gourd.tierName, qualityRank: gourd.qualityRank,
                realmMin: gourd.realmMin, realmMinName: this.realmName(gourd.realmMin),
                captureBonus: gourd.captureBonus, dropSources: this.getItemDropSources(gourd.id),
            })),
            equipment: EQUIPMENT_DEFINITIONS.map(e => {
                const tier = C.TIER[e.tier];
                let source = e.drop > 0 ? '' : 'Không rơi từ quái vật';
                const sourceMonsters = EQUIPMENT_SOURCE_BY_ITEM.get(e.id) || [];
                if (sourceMonsters.length) source = sourceMonsters.map(monster => `${monsterName(monster.id)} (${this.realmName(monster.realm)})`).join(', ');
                if (e.elite) source += ' (chỉ tinh anh, thủ lĩnh)';
                if (e.bound) source = 'Quà tân thủ, không giao dịch';
                return {
                    id: e.id, name: e.name, slot: e.slot, wtype: e.wtype || null, tier: e.tier, tierName: tier.name,
                    qualityRank: e.qualityRank ?? tier.rank ?? 0, element: e.element || null,
                    stats: e.stats, realmMin: e.realmMin ?? tier.realm, realmMinName: this.realmName(e.realmMin ?? tier.realm), refineSec: tier.refine,
                    drop: e.drop, stock: e.stock, remaining: remaining('item', e.id, e.stock), unique: Boolean(e.unique), bound: Boolean(e.bound), source,
                    dropSources: this.getItemDropSources(e.id),
                };
            }),
            skills: C.SKILLS.filter(sk => !player?.skills?.includes(sk.id)).map(sk => ({ ...this.skillInfo(sk.id, player), drop: sk.drop, stock: sk.stock, remaining: remaining('skill', sk.id, sk.stock) })),
            consumables: C.CONSUMABLES.map(c => ({
                id: c.id, name: c.name, price: c.price, desc: c.desc, battle: Boolean(c.battle),
                talisman: Boolean(c.talisman || c.id.startsWith('phu_')),
                qualityRank: c.qualityRank ?? 0, tierName: c.qualityName || 'Phàm',
                realmMin: c.realmMin ?? 0, realmMinName: c.breakthrough ? this.realmName(c.realmMin ?? 0) : null,
                dropSources: this.getItemDropSources(c.id),
            })),
            monsters: C.MONSTERS.map(m => ({
                id: m.id, name: m.name, icon: m.icon || '👹', realm: m.realm, realmName: this.realmName(m.realm), element: m.element, elementName: C.HE[m.element].name,
                night: Boolean(m.night), worldBoss: Boolean(m.worldBoss),
                spawnHours: m.worldBoss ? '11:00–14:00 và 18:00–24:00 · Boss Thế Giới' : m.night ? '18:00–06:00 · Ban đêm' : 'Cả ngày',
                trait: m.trait ? traits[m.trait] || m.trait : null, bigStun: m.bigStun || 0, ...monsterStats(m), small: Boolean(m.small),
            })),
            slotRealms: C.RULES.skillSlotRealms.map(r => this.realmName(r)),
        };
    }

    // ---- Môn phái -----------------------------------------------------------
    get sects() {
        if (!this.data.sects) {
            this.data.sects = {
                van_kiem: {
                    id: 'van_kiem',
                    name: 'Vạn Kiếm Tông',
                    desc: 'Vạn kiếm quy tông, chém đứt hồng trần, sát phạt đệ nhất thiên hạ.',
                    leaderId: 'system_1',
                    leaderName: 'Vô Danh Kiếm Tôn',
                    level: 3,
                    exp: 1500,
                    buff: { atkPct: 0.05, desc: '+5% Công' },
                    trial: { name: 'Chấp Sự Kiếm Vô Ảnh', power: 250, desc: 'Thử thách kiếm khí sơ cấp.' },
                    members: {},
                    createdAt: new Date().toISOString(),
                },
                duoc_vuong: {
                    id: 'duoc_vuong',
                    name: 'Dược Vương Cốc',
                    desc: 'Y thuật thông thần, luyện đan cứu thế, sinh cơ bất tuyệt.',
                    leaderId: 'system_2',
                    leaderName: 'Bách Thảo Tiên Ông',
                    level: 3,
                    exp: 1500,
                    buff: { hpPct: 0.10, desc: '+10% Khí huyết' },
                    trial: { name: 'Thủ Sơn Linh Thú - Bách Thảo Lộc', power: 250, desc: 'Thử thách định lực và sinh cơ.' },
                    members: {},
                    createdAt: new Date().toISOString(),
                },
                tieu_dao: {
                    id: 'tieu_dao',
                    name: 'Tiêu Dao Cung',
                    desc: 'Thân tựa mây trôi, ý chí tự do, xuất quỷ nhập thần.',
                    leaderId: 'system_3',
                    leaderName: 'Bắc Minh Chân Nhân',
                    level: 3,
                    exp: 1500,
                    buff: { spdPct: 0.08, sensePct: 0.08, desc: '+8% Tốc & Thần thức' },
                    trial: { name: 'Tiếp Dẫn Chân Nhân - Vân Du Tử', power: 250, desc: 'Thử thách thân pháp tiêu dao.' },
                    members: {},
                    createdAt: new Date().toISOString(),
                },
            };
        }
        return this.data.sects;
    }

    sectList(userId) {
        const p = this.requirePlayer(userId);
        const mySect = p.sectId ? this.sects[p.sectId] : null;
        const today = vnDate(this.now());
        const leaderboard = Object.values(this.sects).map(s => {
            addSectContribution(s, 0);
            const benefits = sectLevelBenefits(s);
            return {
                id: s.id,
                name: s.name,
                desc: s.desc,
                leaderName: (String(s.leaderId) === '1549712704' || String(s.leaderId) === 'npc_thien_dao' || s.leaderName === 'Thiên Đạo' || this.isHiddenFromPlayers(s.leaderId)) ? 'Ẩn danh' : s.leaderName,
                level: s.level,
                totalContribution: s.totalContribution || 0,
                memberCount: Object.keys(s.members || {}).filter(id => !this.isHiddenFromPlayers(id)).length + (s.leaderId?.startsWith('system_') ? 1 : 0),
                buffDesc: s.buff?.desc || `+${s.level}% toàn chỉ số`,
                buffs: benefits,
                benefits,
                trialName: s.trial?.name || 'Thủ Sơn Hộ Pháp Khôi Lỗi',
                trialPower: s.trial?.power || (200 + (s.level || 1) * 50),
                trialDesc: s.trial?.desc || 'Khảo hạch chiến lực nhập môn',
            };
        }).sort((a, b) => b.level - a.level || b.totalContribution - a.totalContribution || b.memberCount - a.memberCount || a.name.localeCompare(b.name, 'vi'));
        leaderboard.forEach((sect, index) => { sect.rank = index + 1; });
        return {
            mySect: mySect ? this.sectView(userId) : null,
            list: leaderboard,
            leaderboard,
            canCreate: this.realmOf(userId).index >= 3 && !p.sectId,
            canClaimDaily: Boolean(mySect && p.sectLastDailyDate !== today),
        };
    }

    sectView(userId) {
        const p = this.requirePlayer(userId);
        if (!p.sectId || !this.sects[p.sectId]) return null;
        const s = this.sects[p.sectId];
        addSectContribution(s, 0);
        const now = this.now();
        const today = vnDate(now);
        const dailyContribution = sectDailyContribution(p, today);
        const benefits = sectLevelBenefits(s);
        const membersList = Object.entries(s.members || {}).filter(([uid]) => String(uid) === String(userId) || !this.isHiddenFromPlayers(uid)).map(([uid, m]) => {
            const mp = this.player(uid);
            const npc = this.data.worldNpcs?.[uid];
            const mRole = m.role || 'ngoai_mon';
            let power = 0;
            if (mp) {
                power = this.stats(mp, this.now()).power;
            } else if (npc) {
                power = npc.power || this.calcNpcPower(npc);
            } else if (m.power) {
                power = m.power;
            } else {
                const rLvl = s.level || 1;
                power = Math.round(1200 + rLvl * 600);
            }
            let memName = m.name || mp?.name || npc?.name || 'Đạo hữu';
            let memFullName = mp?.fullName || npc?.name || m.fullName || m.name || mp?.name || 'Đạo hữu';
            if (String(uid) === '1549712704' || String(uid) === 'npc_thien_dao' || memName === 'Thiên Đạo' || memFullName === 'Thiên Đạo') {
                memName = 'Ẩn danh';
                memFullName = 'Ẩn danh';
            }
            return {
                userId: uid,
                name: memName,
                fullName: memFullName,
                photoUrl: mp?.photoUrl || npc?.avatar || '',
                role: mRole,
                roleName: SECT_ROLE_NAMES[mRole] || 'Đệ tử',
                contributed: m.contributed || 0,
                power,
                isNpc: Boolean(npc || uid.startsWith('system_') || m.isNpc),
            };
        });
        if (s.leaderId && !membersList.some(m => String(m.userId) === String(s.leaderId))
            && (!this.isHiddenFromPlayers(s.leaderId) || String(userId) === String(s.leaderId))) {
            const leaderNpc = this.data.worldNpcs?.[String(s.leaderId)];
            const leaderStats = this.getNpcLeaderStats(s);
            let lName = s.leaderName || leaderStats.name;
            let lFullName = leaderStats.fullName || s.leaderName;
            if (String(s.leaderId) === '1549712704' || String(s.leaderId) === 'npc_thien_dao' || lName === 'Thiên Đạo' || lFullName === 'Thiên Đạo') {
                lName = 'Ẩn danh';
                lFullName = 'Ẩn danh';
            }
            membersList.unshift({
                userId: String(s.leaderId),
                name: lName,
                fullName: lFullName,
                photoUrl: leaderNpc?.avatar || '',
                role: 'leader',
                roleName: SECT_ROLE_NAMES.leader,
                contributed: 5000,
                power: leaderStats.power || (leaderNpc ? this.calcNpcPower(leaderNpc) : 3500),
                isNpc: true,
            });
        }
        const roleOrder = { leader: 0, vice: 1, dai_elder: 2, elder: 3, dai_de_tu: 4, noi_mon: 5, ngoai_mon: 6, member: 6 };
        membersList.sort((a, b) => {
            const roA = roleOrder[a.role] ?? 99;
            const roB = roleOrder[b.role] ?? 99;
            if (roA !== roB) return roA - roB;
            return (b.contributed || 0) - (a.contributed || 0) || (b.power || 0) - (a.power || 0);
        });
        const myRole = s.leaderId === String(userId) ? 'leader' : (s.members[String(userId)]?.role || p.sectRole || 'ngoai_mon');
        const contrib = Math.max(Number(p.sectContributed) || 0, Number(s.members[String(userId)]?.contributed) || 0);
        const nextPromotion = sectNextPromotion(myRole);
        const reqContrib = nextPromotion?.required || 0;
        const promotion = nextPromotion ? {
            canChallenge: contrib >= reqContrib,
            nextRole: nextPromotion.role,
            nextRoleName: nextPromotion.name,
            reqContrib,
            currentContrib: contrib,
            nextRoleHolder: (() => {
                const holder = Object.entries(s.members || {}).find(([uid, member]) => member.role === nextPromotion.role && uid !== String(userId))?.[1]?.name || null;
                return (holder === 'Thiên Đạo') ? 'Ẩn danh' : holder;
            })(),
        } : null;

        this.data.sectTerritories = this.data.sectTerritories || (C.SECT_TERRITORIES || []).map(t => ({ ...t }));
        const myTerritories = (this.data.sectTerritories || [])
            .filter(t => t.occupiedBy === s.id)
            .map(t => {
                const accum = this._calculateTerritoryAccumulated(t, now);
                return {
                    id: t.id,
                    name: t.name,
                    desc: t.desc,
                    resource: t.resource,
                    yieldPerHour: t.yieldPerHour,
                    monsterRealm: t.monsterRealm,
                    hasMonsters: t.hasMonsters,
                    accumulated: accum,
                };
            });
        const totalYieldPerHour = myTerritories.reduce((sum, t) => sum + (t.yieldPerHour || 0), 0);
        const totalAccumStones = myTerritories.reduce((sum, t) => sum + (t.accumulated?.stones || 0), 0);
        const totalAccumItems = myTerritories.reduce((sum, t) => sum + (t.accumulated?.items || 0), 0);
        const canHarvestAny = myTerritories.some(t => t.accumulated?.canHarvest);

        const customShop = (s.shopItems || []).map(it => ({
            ...it,
            isCustom: true,
        }));
        const defaultShop = C.SECT_SHOP || [];

        return {
            id: s.id,
            name: s.name,
            desc: s.desc,
            level: s.level,
            exp: s.exp,
            expNext: s.level < SECT_MAX_LEVEL ? sectLevelContribution(s.level) : 0,
            nextExp: s.level < SECT_MAX_LEVEL ? sectLevelContribution(s.level) : 0,
            totalContribution: s.totalContribution || 0,
            maxContribution: SECT_MAX_CONTRIBUTION,
            maxLevel: SECT_MAX_LEVEL,
            buffs: benefits,
            benefits,
            storageCapacity: benefits.storageCapacity,
            leaderName: (String(s.leaderId) === '1549712704' || String(s.leaderId) === 'npc_thien_dao' || s.leaderName === 'Thiên Đạo' || (this.isHiddenFromPlayers(s.leaderId) && String(userId) !== String(s.leaderId))) ? 'Ẩn danh' : s.leaderName,
            leaderId: this.isHiddenFromPlayers(s.leaderId) && String(userId) !== String(s.leaderId) ? null : s.leaderId,
            myRole,
            myRoleName: SECT_ROLE_NAMES[myRole] || 'Đệ tử',
            promotion,
            buffDesc: s.buff?.desc || `+${s.level}% toàn chỉ số`,
            members: membersList,
            donatedToday: dailyContribution.donatedStones,
            dailyStoneDonationCap: SECT_DAILY_STONE_DONATION_CAP,
            dailyContributionToday: dailyContribution.contributed,
            dailyContributionCap: SECT_DAILY_CONTRIBUTION_CAP,
            dailyContributionRemaining: dailyContribution.remaining,
            claimedDailyToday: p.sectLastDailyDate === today,
            hasClaimedDaily: p.sectLastDailyDate === today,
            funds: s.funds || 0,
            storage: s.storage || [],
            activeWar: this.getActiveWarForSect(s.id),
            sectCoins: p.sectCoins || 0,
            shop: [...customShop, ...defaultShop],
            myTerritories,
            totalYieldPerHour,
            totalAccumStones,
            totalAccumItems,
            canHarvestAny,
        };
    }

    sectCreate(userId, { name, desc }) {
        const p = this.requirePlayer(userId);
        if (p.sectId) fail('Đạo hữu đã có môn phái, không thể lập thêm.');
        if (this.realmOf(userId).index < 3) fail('Cần đạt Trúc Cơ (cảnh giới 3) trở lên mới có thể khai tông lập phái.');
        if (p.stones < C.RULES.sectCreateCost) fail(`Cần ${C.RULES.sectCreateCost} linh thạch để lập môn phái.`);
        const cleanName = String(name || '').trim();
        if (cleanName.length < 3 || cleanName.length > 20) fail('Tên môn phái phải từ 3 đến 20 ký tự.');
        if (Object.values(this.sects).some(s => s.name.toLowerCase() === cleanName.toLowerCase())) fail('Tên môn phái này đã tồn tại.');
        p.stones -= C.RULES.sectCreateCost;
        const id = 'sect_' + newId();
        this.sects[id] = {
            id,
            name: cleanName,
            desc: String(desc || 'Tông môn mới lập, chấn hưng tiên đạo.').trim().slice(0, 100),
            leaderId: String(userId),
            leaderName: p.name,
            level: 1,
            exp: 0,
            totalContribution: 500,
            buff: { atkPct: 0.03, hpPct: 0.03, desc: '+3% Công & Khí huyết' },
            trial: { name: 'Thủ Sơn Khôi Lỗi Trận', power: 300, desc: 'Cơ quan hộ sơn khảo sát chiến lực tân đệ tử.' },
            members: {
                [String(userId)]: { name: p.name, role: 'leader', contributed: 500, joinedAt: new Date().toISOString() },
            },
            createdAt: new Date().toISOString(),
        };
        p.sectId = id;
        p.sectRole = 'leader';
        p.sectContributed = 500;
        p.sectCoins = (p.sectCoins || 0) + 100;
        this.touch();
        return this.sects[id];
    }

    sectJoin(userId, sectId) {
        const p = this.requirePlayer(userId);
        if (p.sectId) fail('Đạo hữu đã ở trong môn phái khác.');
        const s = this.sects[sectId];
        if (!s) fail('Môn phái không tồn tại.');

        // Khảo hạch nhập môn bằng chiến lực
        const trial = s.trial || { name: 'Thủ Sơn Hộ Pháp Khôi Lỗi', power: 200 + (s.level || 1) * 50, desc: 'Khảo hạch chiến lực đệ tử nhập môn' };
        const myPower = this.stats(p, this.now()).power;
        if (myPower < trial.power) {
            fail(`Khảo hạch môn phái thất bại! Bạn giao đấu với [${trial.name}] nhưng chiến lực không đủ (Chiến lực của bạn: ${myPower}, Yêu cầu: ${trial.power}). Hãy nâng cao tu vi rồi trở lại!`);
        }

        s.members = s.members || {};
        s.members[String(userId)] = {
            name: p.name,
            role: 'ngoai_mon',
            contributed: 0,
            joinedAt: new Date().toISOString(),
        };
        p.sectId = sectId;
        p.sectRole = 'ngoai_mon';
        p.sectContributed = 0;
        p.sectCoins = p.sectCoins || 0;
        this.touch();
        return s;
    }

    sectLeave(userId) {
        const p = this.requirePlayer(userId);
        if (!p.sectId) fail('Đạo hữu chưa gia nhập môn phái nào.');
        const s = this.sects[p.sectId];
        const activeWar = (this.data.sectWars || []).find(w => w.status === 'active'
            && (w.attackerSectId === p.sectId || w.defenderSectId === p.sectId));
        const currentStones = Math.max(0, Math.floor(Number(p.stones) || 0));
        const warLeavePenalty = activeWar ? Math.floor(currentStones * 0.20) : 0;
        if (warLeavePenalty > 0) {
            p.stones = currentStones - warLeavePenalty;
            activeWar.log ||= [];
            activeWar.log.push(`🏃 ${p.name || 'Một thành viên'} rời ${s?.name || 'tông môn'} giữa lúc tông chiến, bị phạt ${warLeavePenalty.toLocaleString('vi-VN')} linh thạch (20%).`);
        }
        if (s) {
            if (s.leaderId === String(userId)) {
                const otherMemberId = Object.keys(s.members || {}).find(uid => uid !== String(userId));
                if (otherMemberId) {
                    s.leaderId = otherMemberId;
                    s.leaderName = s.members[otherMemberId].name;
                    s.members[otherMemberId].role = 'leader';
                    const mp = this.player(otherMemberId);
                    if (mp) mp.sectRole = 'leader';
                } else if (!s.id.startsWith('system_')) {
                    delete this.sects[s.id];
                }
            }
            if (s.members) delete s.members[String(userId)];
        }
        p.sectId = null;
        p.sectRole = null;
        p.sectContributed = 0;
        p.sectCoins = 0;
        this.touch();
        return {
            penalty: warLeavePenalty,
            wasDuringWar: Boolean(activeWar),
            sectName: s?.name || '',
        };
    }

    sectDonate(userId, stones) {
        const p = this.requirePlayer(userId);
        if (!p.sectId || !this.sects[p.sectId]) fail('Đạo hữu chưa vào môn phái.');
        const amount = Math.floor(Number(stones) || 0);
        if (amount <= 0) fail('Số linh thạch không hợp lệ.');
        if (p.stones < amount) fail('Không đủ linh thạch.');
        const sect = this.sects[p.sectId];
        addSectContribution(sect, 0);
        const today = vnDate(this.now());
        const daily = sectDailyContribution(p, today);
        if (amount > daily.remainingStones) fail(`Mỗi ngày chỉ cống hiến tối đa ${SECT_DAILY_STONE_DONATION_CAP} linh thạch (hôm nay đã cống hiến ${daily.donatedStones}).`);
        const contributionPoints = amount * SECT_CONTRIBUTION_PER_STONE;
        if (contributionPoints > daily.remaining) fail(`Đã dùng ${daily.contributed}/${SECT_DAILY_CONTRIBUTION_CAP} điểm cống hiến hôm nay; còn ${daily.remaining} điểm.`);
        if ((sect.totalContribution || 0) + contributionPoints > SECT_MAX_CONTRIBUTION) fail('Tông môn đã đạt giới hạn 5.000.000 điểm cống hiến.');
        p.stones -= amount;
        p.sectLastDonateDate = today;
        p.sectDonateToday = (p.sectDonateToday || 0) + amount;
        p.sectContributionDate = today;
        p.sectContributionToday = daily.contributed + contributionPoints;
        p.sectContributed = (p.sectContributed || 0) + contributionPoints;
        p.sectCoins = (p.sectCoins || 0) + contributionPoints;
        const s = sect;
        const progress = addSectContribution(s, contributionPoints);
        if (s.members && s.members[String(userId)]) {
            s.members[String(userId)].contributed = (s.members[String(userId)].contributed || 0) + contributionPoints;
        }
        this.touch();
        return { gainedExp: progress.added, contributionPoints: progress.added, sectCoins: p.sectCoins, sectLevel: s.level, leveledUp: progress.leveledUp, remainingStones: p.stones };
    }

    sectClaimDaily(userId) {
        const p = this.requirePlayer(userId);
        if (!p.sectId || !this.sects[p.sectId]) fail('Đạo hữu chưa vào môn phái.');
        const today = vnDate(this.now());
        if (p.sectLastDailyDate === today) fail('Hôm nay đã nhận bổng lộc môn phái rồi.');
        const s = this.sects[p.sectId];
        const stones = 30 + (s.level || 1) * 15;
        const exp = 100 * (s.level || 1);
        const coins = 50 + (s.level || 1) * 20; // Thưởng điểm cống hiến hàng ngày
        p.stones += stones;
        p.sectCoins = (p.sectCoins || 0) + coins;
        p.sectLastDailyDate = today;
        const rResult = this.realms.addExp(userId, exp);
        this.touch();
        return { stones, exp, coins, sectCoins: p.sectCoins, levelUps: rResult?.levelUps || 0 };
    }

    sectBuy(userId, itemId) {
        const p = this.requirePlayer(userId);
        if (!p.sectId || !this.sects[p.sectId]) fail('Đạo hữu chưa vào môn phái.');
        const s = this.sects[p.sectId];
        s.shopItems = s.shopItems || [];

        // Kiểm tra xem có phải vật phẩm cống hiến từ đồng môn không
        const customIndex = s.shopItems.findIndex(it => it.id === itemId);
        if (customIndex !== -1) {
            const customItem = s.shopItems[customIndex];
            p.sectCoins = p.sectCoins || 0;
            if (p.sectCoins < customItem.price) {
                fail(`Không đủ điểm cống hiến tông môn (Hiện có: ${p.sectCoins}, Cần: ${customItem.price} điểm).`);
            }
            const now = this.now();
            if (customItem.kind === 'cons' || customItem.kind === 'mat' || customItem.kind === 'scroll') {
                const added = this.addStack(p, customItem.kind, customItem.targetId, 1);
                if (added <= 0) {
                    fail('Túi đồ đã đầy, không thể nhận thêm vật phẩm.');
                }
            } else if (customItem.kind === 'equip') {
                const eqDef = C.EQUIP_BY_ID.get(customItem.targetId);
                if (!eqDef) fail('Dữ liệu trang bị không hợp lệ.');
                const eq = this.addEquip(p, eqDef, now);
                if (!eq) {
                    fail('Túi đồ đã đầy, không thể nhận trang bị.');
                }
            } else {
                fail('Loại vật phẩm không hỗ trợ.');
            }

            p.sectCoins -= customItem.price;
            customItem.qty = (customItem.qty || 1) - 1;
            const boughtName = customItem.name;
            if (customItem.qty <= 0) {
                s.shopItems.splice(customIndex, 1);
            }
            this.touch();
            return { item: { name: boughtName, price: customItem.price }, remainingCoins: p.sectCoins };
        }

        // Nếu không có trong shop cống hiến, tìm trong danh mục SECT_SHOP mặc định
        const itemDef = C.SECT_SHOP_BY_ID?.get(itemId);
        if (!itemDef) fail('Vật phẩm không tồn tại trong Tàng Bảo Các.');
        p.sectCoins = p.sectCoins || 0;
        if (p.sectCoins < itemDef.price) {
            fail(`Không đủ điểm cống hiến tông môn (Hiện có: ${p.sectCoins}, Cần: ${itemDef.price} điểm).`);
        }
        p.sectCoins -= itemDef.price;
        const now = this.now();
        if (itemDef.kind === 'cons') {
            const added = this.addStack(p, 'cons', itemDef.targetId, itemDef.qty || 1);
            if (added <= 0) {
                p.sectCoins += itemDef.price;
                fail('Túi đồ đã đầy, không thể nhận thêm vật phẩm.');
            }
        } else if (itemDef.kind === 'equip') {
            const eqDef = C.EQUIP_BY_ID.get(itemDef.targetId);
            if (!eqDef) fail('Dữ liệu trang bị không hợp lệ.');
            const item = this.addEquip(p, eqDef, now);
            if (!item) {
                p.sectCoins += itemDef.price;
                fail('Túi đồ đã đầy, không thể nhận trang bị.');
            }
        } else if (itemDef.kind === 'scroll') {
            const added = this.addStack(p, 'scroll', itemDef.targetId, 1);
            if (added <= 0) {
                p.sectCoins += itemDef.price;
                fail('Túi đồ đã đầy, không thể nhận ngọc giản.');
            }
        }
        this.touch();
        return { item: itemDef, remainingCoins: p.sectCoins };
    }

    // ---- Đấu Pháp PVP -------------------------------------------------------
    getWorldBossDaily(p, now = this.now()) {
        const today = vnDate(now);
        const daily = p.worldBossDaily?.date === today ? p.worldBossDaily : { date: today, count: 0 };
        const rawCount = Number(daily.count);
        const count = Number.isFinite(rawCount) ? Math.max(0, Math.floor(rawCount)) : 0;
        return {
            count,
            max: 10,
            remaining: Math.max(0, 10 - count),
            date: today,
        };
    }

    useWorldBossTurn(p, now = this.now()) {
        const daily = this.getWorldBossDaily(p, now);
        if (daily.remaining <= 0) fail(`${p.name} đã dùng hết 10 lượt đánh Boss Thế Giới hôm nay.`);
        p.worldBossDaily = { date: daily.date, count: daily.count + 1 };
        return p.worldBossDaily;
    }

    getPvpDaily(p, now = this.now()) {
        const today = vnDate(now);
        p.pvpDaily = (p.pvpDaily && p.pvpDaily.date === today) ? p.pvpDaily : { date: today, count: 0 };
        return {
            count: p.pvpDaily.count || 0,
            max: 10,
            remaining: Math.max(0, 10 - (p.pvpDaily.count || 0)),
            date: today
        };
    }

    usePvpTurn(p, now = this.now()) {
        const daily = this.getPvpDaily(p, now);
        if (daily.remaining <= 0) {
            fail('Hôm nay bạn đã dùng hết 10 lượt Đấu Pháp PVP (tối đa 10 lần/ngày). Hãy quay lại vào ngày mai!');
        }
        p.pvpDaily.count = (p.pvpDaily.count || 0) + 1;
        this.touch();
    }

    getNpcDaily(p, now = this.now()) {
        const today = vnDate(now);
        p.npcDaily = (p.npcDaily && p.npcDaily.date === today) ? p.npcDaily : { date: today, count: 0 };
        const max = C.RULES?.maxDailyNpcBattles || 15;
        const count = Math.max(0, Math.floor(Number(p.npcDaily.count) || 0));
        return {
            count,
            max,
            remaining: Math.max(0, max - count),
            date: today,
        };
    }

    pvpList(userId, refresh = false) {
        const p = this.requirePlayer(userId);
        this.requireDiscoverablePlayer(userId);
        const uid = String(userId);
        if (refresh) {
            const currentOffset = this.pvpOpponentOffsets.get(uid) || 0;
            const candidateCount = Object.entries(this.data.players || {}).filter(([playerId, other]) => playerId !== uid && other.registered && !this.isHiddenFromPlayers(other)).length;
            const step = candidateCount > 15 ? 15 : 1;
            this.pvpOpponentOffsets.set(uid, candidateCount > 15 ? (currentOffset + step) % candidateCount : currentOffset + step);
        }
        const myPower = this.stats(p, this.now()).power;
        const candidates = [];
        for (const [uid, other] of Object.entries(this.data.players || {})) {
            if (uid === String(userId) || !other.registered || this.isHiddenFromPlayers(other)) continue;
            const r = this.realmOf(uid);
            const st = this.stats(other, this.now());
            const sect = other.sectId && this.sects[other.sectId] ? this.sects[other.sectId].name : null;
            const comp = other.companion ? other.companion.name : null;
            const otherTown = other.town || 'thanh_van';
            candidates.push({
                userId: uid,
                name: other.name,
                fullName: other.fullName || other.name,
                photoUrl: other.photoUrl || '',
                gender: other.gender,
                realmName: r.name,
                realmIndex: r.index,
                power: st.power,
                points: other.pvp?.points || 1000,
                wins: other.pvp?.wins || 0,
                losses: other.pvp?.losses || 0,
                sectName: sect,
                town: otherTown,
                townName: C.TOWN_BY_ID?.get(otherTown)?.name || 'Thanh Vân Thành',
                isSameTown: otherTown === (p.town || 'thanh_van'),
                isDemon: Boolean(other.isDemon),
                companionName: comp,
                hasCompanion: Boolean(other.companion),
                canRob: Boolean(other.companion && (!other.companion.protectedUntil || other.companion.protectedUntil < this.now())),
                intimacy: other.companion?.intimacy || 0,
            });
        }
        candidates.sort((a, b) => Math.abs(a.power - myPower) - Math.abs(b.power - myPower));
        const now = this.now();
        this.data.pvpChallenges = this.data.pvpChallenges || [];
        const received = this.data.pvpChallenges
            .filter(c => c.toId === String(userId) && !this.isHiddenFromPlayers(c.fromId) && c.status === 'pending' && now - c.challengedAt < 24 * 3600 * 1000)
            .map(c => ({
                id: c.id,
                fromId: c.fromId,
                fromName: c.fromName,
                fromPower: c.fromPower,
                challengedAt: c.challengedAt,
                elapsedSec: Math.floor((now - c.challengedAt) / 1000),
            }));
        const sent = this.data.pvpChallenges
            .filter(c => c.fromId === String(userId) && !this.isHiddenFromPlayers(c.toId) && c.status === 'pending' && now - c.challengedAt < 24 * 3600 * 1000)
            .map(c => ({
                id: c.id,
                toId: c.toId,
                toName: c.toName,
                toPower: c.toPower,
                challengedAt: c.challengedAt,
                elapsedSec: Math.floor((now - c.challengedAt) / 1000),
                canBotTakeover: now - c.challengedAt >= PVP_BOT_IDLE_MS,
                botTakeoverLeftSec: Math.max(0, Math.ceil((PVP_BOT_IDLE_MS - (now - c.challengedAt)) / 1000)),
            }));
        const myTown = p.town || 'thanh_van';
        const sameTownPlayers = candidates.filter(c => (c.town || 'thanh_van') === myTown);
        const offset = this.pvpOpponentOffsets.get(uid) || 0;
        const rotated = candidates.length ? candidates.slice(offset % candidates.length).concat(candidates.slice(0, offset % candidates.length)) : [];
        const dailyPvp = this.getPvpDaily(p, now);
        return {
            me: {
                points: p.pvp?.points || 1000,
                wins: p.pvp?.wins || 0,
                losses: p.pvp?.losses || 0,
                stamina: p.stamina,
                town: myTown,
                townName: C.TOWN_BY_ID?.get(myTown)?.name || 'Thanh Vân Thành',
                dailyPvpCount: dailyPvp.count,
                dailyPvpMax: dailyPvp.max,
                dailyPvpRemaining: dailyPvp.remaining,
            },
            challenges: { received, sent },
            opponents: rotated.slice(0, 15),
            sameTownPlayers: sameTownPlayers.slice(0, 20),
        };
    }

    pvpManualFight(userId, targetUserId, purpose = null) {
        const p1 = this.requirePlayer(userId);
        this.requireDiscoverablePlayer(userId);
        const p2 = this.requireVisiblePlayer(userId, targetUserId);
        if (String(userId) === String(targetUserId)) fail('Không thể tự khiêu chiến chính mình.');
        if (purpose === 'roam_attack') {
            const town1 = C.TOWN_BY_ID.get(p1.town) || C.TOWNS[0];
            const town2 = C.TOWN_BY_ID.get(p2.town) || C.TOWNS[0];
            if (town1.mapId !== town2.mapId) fail('Chỉ có thể đột kích tu sĩ đang cùng ở một đại châu.');
            if (!p2.isRoaming) fail('Đối phương đang ở trong thành an toàn được đại trận che chở, không thể đột kích!');
            p1.isRoaming = true;
        } else if (p1.town !== p2.town) {
            fail('Lôi Đài chỉ có thể quyết đấu trong cùng thành trấn.');
        }
        const now = this.now();
        this.syncStamina(p1, now);
        if (p1.stamina < C.RULES.pvpCost) fail(`Cần ${C.RULES.pvpCost} thể lực để khiêu chiến.`);
        if (p1.injuredUntil && p1.injuredUntil > now) fail('Đang trọng thương, không thể khiêu chiến.');
        if (p2.injuredUntil && p2.injuredUntil > now) fail('Đối phương đang trọng thương, hãy quay lại sau.');
        const existingBattle = this.pvpManualBattles?.get(String(userId)) || this.pvpManualBattles?.get(String(targetUserId));
        if (existingBattle && !existingBattle.over) fail('Một trong hai tu sĩ đang ở trong trận PvP khác.');
        this.usePvpTurn(p1, now);
        p1.stamina -= C.RULES.pvpCost;
        this.touch();
        const battle = this.startPvpManualBattle(userId, targetUserId, false, null, purpose);
        p2.notices = p2.notices || [];
        const notice = purpose === 'roam_attack'
            ? `💥 ${p1.fullName || p1.name} đang đột kích dã ngoại. Vào Đấu Pháp để tự tay tiếp chiêu.`
            : purpose === 'town_attack'
                ? `💥 ${p1.fullName || p1.name} đang đột kích trong thành. Vào Đấu Pháp để tự tay tiếp chiêu.`
                : `⚔️ ${p1.fullName || p1.name} đang thách đấu trực tiếp. Vào mục Đấu Pháp để tự tay tiếp chiêu.`;
        p2.notices.push(notice);
        return { success: true, battle };
    }

    startCompanionRobBattle(userId, targetUserId) {
        const p = this.requirePlayer(userId);
        const target = this.requirePlayer(targetUserId);
        if (String(userId) === String(targetUserId)) fail('Không thể tự cướp đạo lữ của mình.');
        if (p.town !== target.town) fail('Phải tới cùng thành trấn với đối thủ mới có thể tranh đoạt đạo lữ.');
        if (p.companion) fail('Đạo hữu đã có đạo lữ rồi, không thể phát động tranh đoạt.');
        if (!target.companion) fail('Đối phương hiện không có đạo lữ.');
        const now = this.now();
        this.syncStamina(p, now);
        if ((p.injuredUntil || 0) > now) fail('Đang trọng thương, không thể khiêu chiến.');
        if ((target.injuredUntil || 0) > now) fail('Đối phương đang trọng thương, hãy quay lại sau.');
        if (p.stamina < 30) fail('Cần 30 thể lực để phát động cướp đạo lữ.');
        if (p.stones < 50) fail('Cần 50 linh thạch để gửi chiến thư đoạt đạo lữ.');
        if ((target.companion.protectedUntil || 0) > now) {
            const leftSec = Math.ceil((target.companion.protectedUntil - now) / 1000);
            fail(`Đạo lữ của đối phương được thiên đạo che chở, còn ${Math.floor(leftSec / 60)} phút bảo hộ.`);
        }
        const existingBattle = this.pvpManualBattles?.get(String(userId)) || this.pvpManualBattles?.get(String(targetUserId));
        if (existingBattle && !existingBattle.over) fail('Một trong hai tu sĩ đang ở trong trận PvP khác.');
        this.usePvpTurn(p, now);
        p.stamina -= 30;
        p.stones -= 50;
        this.ledger(-50);
        const battle = this.startPvpManualBattle(userId, targetUserId, false, null, 'companion_rob');
        target.notices = target.notices || [];
        target.notices.push(`⚔️ ${p.fullName || p.name} đang phát động trận PvP tranh đoạt đạo lữ. Vào Đấu Pháp để bảo vệ duyên lữ!`);
        this.touch();
        return { success: true, battle, message: 'Lôi đài tranh đoạt đã mở. Người thắng trận sẽ nhận đạo lữ của đối thủ.' };
    }

    _handleDemonSlain(winner, loser, log = null, winnerUserId = null) {
        if (!loser.isDemon) return null;
        const wId = winnerUserId || winner.userId;
        // Bảo vệ tuyệt đối: Không thể tự trảm ma bản thân
        if (String(wId) === String(loser.userId) || String(winner.userId) === String(loser.userId)) return null;

        const bountyReward = Math.round(250 + (loser.killCount || 5) * 50);

        // 1. Trảm ma xóa trạng thái truy nã và Ma Tính; Đạo Tâm vẫn phải tự hồi phục.
        loser.isDemon = false;
        loser.killCount = 0;
        loser.maScore = 0;
        loser.demonTitle = '';
        loser.daoTam = loser.daoScore;

        // 2. Người bị trảm trừ mạnh tu vi (rớt cảnh giới nếu exp về 0)
        const loserRealm = this.realmOf(loser.userId);
        const expPenalty = Math.round((loserRealm.levelCap || 2000) * 0.3 + 3000);
        const lostExp = this.realms.loseExp(loser.userId, expPenalty);

        // 3. Mất 1-2 vật phẩm không bound trong túi/kho thường; Kho An Toàn đang thuê được bảo vệ.
        const droppedNames = [];
        const droppable = this.getDroppableItems(loser);
        const numToDrop = Math.min(droppable.length, this.rng() < 0.5 ? 2 : 1);
        for (let i = 0; i < numToDrop; i++) {
            const currentDroppable = this.getDroppableItems(loser);
            if (currentDroppable.length === 0) break;
            const item = currentDroppable[Math.min(currentDroppable.length - 1, Math.floor(this.rng() * currentDroppable.length))];
            const dropped = this.takeDroppableItem(loser, item);
            if (!dropped) continue;
            const dName = itemName(dropped);

            const winnerBagCount = (winner.items || []).filter(it => it.place === 'bag').length;
            if (winnerBagCount < this.capacity('bag', winner)) {
                this.addStack(winner, dropped.kind, dropped.id, 1);
                droppedNames.push(`${winner.name} đoạt [${dName}]`);
            } else {
                this.stockAdd(dropped.kind === 'equip' ? 'item' : (dropped.kind === 'scroll' ? 'skill' : 'cons'), dropped.id, 1);
                droppedNames.push(`[${dName}] rơi vào Thiên Địa Kho Tàng`);
            }
        }

        // Nếu người thắng cũng là Ma Tu: Hắc ăn Hắc, không nhận tiền thưởng trừ ma của triều đình/chính phái
        if (winner.isDemon) {
            winner.notices = winner.notices || [];
            winner.notices.push(`💀 [HẮC ĂN HẮC] Ngươi đã trảm sát đối thủ Ma Đầu ${loser.name}, cướp đoạt chiến lợi phẩm!`);
            loser.notices = loser.notices || [];
            loser.notices.push(`💀 [MA ĐẠO TÀN SÁT] Bạn bị Ma Tu ${winner.name} sát hại! Điểm ma đạo quy 0, tu vi bị phế trừ ${lostExp} EXP${droppedNames.length ? `, đánh mất: ${droppedNames.join(', ')}` : ''}!`);
            const msg = `💀 MA ĐẠO TÀN SÁT: ${winner.name} đã sát hại Ma Tu ${loser.name}!`;
            if (log && Array.isArray(log)) log.push(msg);
            return { bountyReward: 0, lostExp, droppedNames, msg };
        }

        winner.stones = (winner.stones || 0) + bountyReward;
        this.realms.addExp(wId, 1000);

        winner.notices = winner.notices || [];
        winner.notices.push(`🏆 [TRỪ MA VỆ ĐẠO] Bạn đã trảm sát Ma Đầu ${loser.name}, lĩnh thưởng truy nã +${bountyReward} Linh Thạch và +1000 EXP!`);
        loser.notices = loser.notices || [];
        loser.notices.push(`💀 [TRẢM MA TRỪ TÀ] Bạn đã bị ${winner.name} trảm sát! Điểm ma đạo quy 0, tu vi bị phế trừ ${lostExp} EXP${droppedNames.length ? `, đánh mất: ${droppedNames.join(', ')}` : ''}, chính thức xóa khỏi danh sách Ma Tu!`);

        const msg = `⭐ TRUY NÃ HOÀN TẤT: ${winner.name} trảm sát Ma Tu ${loser.name}! Nhận ${bountyReward} linh thạch thưởng, Ma Tu bị trừ ${lostExp} EXP tu vi${droppedNames.length ? ` và mất đồ: ${droppedNames.join(', ')}` : ''}, điểm ma đạo về 0!`;
        if (log && Array.isArray(log)) log.push(msg);

        return { bountyReward, lostExp, droppedNames, msg };
    }

    pvpFight(userId, targetUserId) {
        // Compatibility entry point for old clients. PvP always opens the
        // interactive battle; it never simulates a winner or grants rewards here.
        return this.pvpManualFight(userId, targetUserId);
    }
    pvpChallenge(userId, targetUserId) {
        const p1 = this.requirePlayer(userId);
        this.requireDiscoverablePlayer(userId);
        const p2 = this.requireVisiblePlayer(userId, targetUserId);
        if (String(userId) === String(targetUserId)) fail('Không thể tự khiêu chiến chính mình.');
        if (p1.town !== p2.town) fail('Lôi Đài chỉ có thể khiêu chiến trong cùng thị trấn. Hãy di chuyển đến thị trấn của đối thủ trước.');
        const now = this.now();
        this.syncStamina(p1, now);
        if (p1.stamina < C.RULES.pvpCost) fail(`Cần ${C.RULES.pvpCost} thể lực để gửi chiến thư.`);
        if (p1.injuredUntil && p1.injuredUntil > now) fail('Đang trọng thương, không thể khiêu chiến.');
        if (p2.injuredUntil && p2.injuredUntil > now) fail('Đối phương đang trọng thương, hãy quay lại sau.');

        this.data.pvpChallenges = this.data.pvpChallenges || [];
        const existing = this.data.pvpChallenges.find(c => c.fromId === String(userId) && c.toId === String(targetUserId) && c.status === 'pending');
        if (existing) {
            fail('Bạn đã gửi chiến thư cho tu sĩ này rồi, đang chờ hồi đáp.');
        }

        this.usePvpTurn(p1, now);
        p1.stamina -= C.RULES.pvpCost;
        const s1 = this.stats(p1, now);
        const s2 = this.stats(p2, now);
        const challenge = {
            id: newId(),
            fromId: String(userId),
            fromName: p1.fullName || p1.name,
            fromPower: s1.power,
            toId: String(targetUserId),
            toName: p2.fullName || p2.name,
            toPower: s2.power,
            challengedAt: now,
            status: 'pending',
        };
        this.data.pvpChallenges.push(challenge);
        p2.notices = p2.notices || [];
        p2.notices.push(`⚔️ [CHIẾN THƯ] Tu sĩ ${p1.fullName || p1.name} (Chiến lực ${s1.power}) đã gửi chiến thư quyết đấu! Hãy vào Lôi Đài tỉ thí!`);
        this.touch();
        return {
            success: true,
            challenge,
            message: `Đã gửi chiến thư tới ${p2.fullName || p2.name}. Chờ đối phương phản hồi; nếu không có lượt đi trong 2 phút, bạn có thể gọi Bot Hộ Đạo ra tiếp chiêu.`,
        };
    }

    pvpRespond(userId, challengeId, accept = true) {
        const p = this.requirePlayer(userId);
        this.data.pvpChallenges = this.data.pvpChallenges || [];
        const challenge = this.data.pvpChallenges.find(c => c.id === challengeId);
        if (!challenge) fail('Không tìm thấy chiến thư này.');
        if (challenge.toId !== String(userId)) fail('Bạn không phải người nhận chiến thư này.');
        if (challenge.status !== 'pending') fail('Chiến thư này đã được xử lý hoặc hết hạn.');

        const now = this.now();
        if (!accept) {
            challenge.status = 'declined';
            const suitor = this.player(challenge.fromId);
            if (suitor) {
                suitor.stamina = Math.min(100, (suitor.stamina || 0) + C.RULES.pvpCost);
                const today = vnDate(now);
                if (suitor.pvpDaily && suitor.pvpDaily.date === today && suitor.pvpDaily.count > 0) {
                    suitor.pvpDaily.count -= 1;
                }
                suitor.notices = suitor.notices || [];
                suitor.notices.push(`⚔️ Tu sĩ ${p.fullName || p.name} đã từ chối chiến thư quyết đấu. ${C.RULES.pvpCost} thể lực và 1 lượt Đấu Pháp đã được hoàn trả.`);
            }
            this.touch();
            return { success: true, message: 'Đã từ chối chiến thư quyết đấu.' };
        }

        challenge.status = 'accepted';
        this.touch();
        this.startPvpManualBattle(challenge.fromId, challenge.toId, false, challenge.id);
        const bView = this.getPvpBattle(userId);
        return { success: true, battle: bView, message: 'Hai bên đồng ý quyết đấu! Vào trận chiến đấu bằng tay!' };
    }

    pvpStartBotBattle(userId, challengeId) {
        const p = this.requirePlayer(userId);
        this.data.pvpChallenges = this.data.pvpChallenges || [];
        const challenge = this.data.pvpChallenges.find(c => c.id === challengeId);
        if (!challenge) fail('Không tìm thấy chiến thư.');
        if (challenge.fromId !== String(userId)) fail('Chỉ người gửi chiến thư mới có quyền kích hoạt Bot Hộ Đạo.');
        if (challenge.status !== 'pending') fail('Chiến thư này đã được xử lý.');

        const now = this.now();
        const diffMs = now - challenge.challengedAt;
        if (diffMs < PVP_BOT_IDLE_MS) {
            const leftSec = Math.ceil((PVP_BOT_IDLE_MS - diffMs) / 1000);
            fail(`Chưa đủ 2 phút chờ đối phương phản hồi (còn ${leftSec}s). Vui lòng đợi!`);
        }

        challenge.status = 'accepted';
        this.touch();
        const bView = this.startPvpManualBattle(challenge.fromId, challenge.toId, true, challenge.id);
        return { success: true, battle: bView, message: `Đối phương không phản hồi sau 2 phút! Bot Hộ Đạo của ${challenge.toName} xuất chiến tiếp chiêu!` };
    }

    startPvpManualBattle(p1Id, p2Id, isBot = false, challengeId = null, purpose = null) {
        if (String(p1Id) === String(p2Id)) fail('Không thể quyết đấu với chính mình.');
        const now = this.now();
        const p1 = this.requirePlayer(p1Id);
        const p2 = this.requirePlayer(p2Id);
        this.requireNotKnockedOutInDungeon(p1);
        this.requireNotKnockedOutInDungeon(p2);
        if (!isBot) {
            this.requireDiscoverablePlayer(p1Id);
            if (p2.registered && !p2.isNpc) this.requireVisiblePlayer(p1Id, p2Id);
        }
        const s1 = this.stats(p1, now);
        const s2 = this.stats(p2, now);

        const battle = {
            id: newId(),
            challengeId,
            isBot,
            p1: {
                userId: String(p1Id),
                name: p1.fullName || p1.name,
                hp: s1.hp,
                maxHp: s1.hp,
                mp: s1.mp,
                maxMp: s1.mp,
                mpRegenAt: now,
                atk: s1.atk,
                def: s1.def,
                spd: s1.spd,
                sense: s1.sense,
                accuracy: s1.accuracy,
                dodge: s1.dodge,
                crit: s1.crit,
                critDmg: s1.critDmg,
                power: s1.power,
                element: p1.he,
                elemMastery: { ...(p1.elemMastery || {}) },
                roleStat: Number(p1.roleStats?.[p1.mon]) || 0,
                shield: 0,
                buffs: {},
                buffTurns: 0,
                stunTurns: 0,
                bindTurns: 0,
                immuneTurns: 0,
                dodgePending: false,
                dot: null,
                reflectPct: 0,
                forceCounterTurns: 0,
                nextCrit: false,
                dodgeUntil: 0,
                dodgeReadyAt: 0,
                skills: (p1.slots || []).filter(Boolean).map(id => C.SKILL_BY_ID.get(id)).filter(Boolean),
            },
            p2: {
                userId: String(p2Id),
                name: isBot ? `[Bot Hộ Đạo] ${p2.fullName || p2.name}` : (p2.fullName || p2.name),
                hp: s2.hp,
                maxHp: s2.hp,
                mp: s2.mp,
                maxMp: s2.mp,
                mpRegenAt: now,
                atk: s2.atk,
                def: s2.def,
                spd: s2.spd,
                sense: s2.sense,
                accuracy: s2.accuracy,
                dodge: s2.dodge,
                crit: s2.crit,
                critDmg: s2.critDmg,
                power: s2.power,
                element: p2.he,
                elemMastery: { ...(p2.elemMastery || {}) },
                roleStat: Number(p2.roleStats?.[p2.mon]) || 0,
                shield: 0,
                buffs: {},
                buffTurns: 0,
                stunTurns: 0,
                bindTurns: 0,
                immuneTurns: 0,
                dodgePending: false,
                dot: null,
                reflectPct: 0,
                forceCounterTurns: 0,
                nextCrit: false,
                dodgeUntil: 0,
                dodgeReadyAt: 0,
                skills: (p2.slots || []).filter(Boolean).map(id => C.SKILL_BY_ID.get(id)).filter(Boolean),
            },
            round: 1,
            log: [`⚔️ Lôi Đài Khai Mạc: ${p1.name} (Chiến lực ${s1.power}) đối đầu ${isBot ? `[Bot Hộ Đạo] ${p2.name}` : p2.name} (Chiến lực ${s2.power})!`],
            over: false,
            winnerId: null,
            result: null,
            purpose,
            nextActorId: String(p1Id),
            startedAt: now,
            lastActionAt: now,
        };

        this.pvpManualBattles = this.pvpManualBattles || new Map();
        this.pvpManualBattles.set(battle.id, battle);
        this.pvpManualBattles.set(String(p1Id), battle);
        if (!isBot) this.pvpManualBattles.set(String(p2Id), battle);

        return this.getPvpBattleView(battle, String(p1Id));
    }

    getPvpBattle(userId) {
        this.pvpManualBattles = this.pvpManualBattles || new Map();
        const b = this.pvpManualBattles.get(String(userId));
        if (!b) return { none: true };
        return this.getPvpBattleView(b, String(userId));
    }

    pvpClaimBattleResult(userId, battleId) {
        this.pvpManualBattles = this.pvpManualBattles || new Map();
        const uid = String(userId);
        const battle = this.pvpManualBattles.get(uid);
        // Make acknowledgement safe to retry if the client lost the response.
        if (!battle) return { success: true, alreadyClaimed: true };
        if (battleId && String(battle.id) !== String(battleId)) {
            fail('Trận đấu đã thay đổi. Hãy tải lại mục Đấu pháp.');
        }
        if (battle.p1.userId !== uid && battle.p2.userId !== uid) {
            fail('Bạn không tham gia trận đấu này.');
        }
        if (!battle.over) fail('Trận đấu chưa kết thúc, chưa thể nhận kết quả.');

        // Each opponent acknowledges their own result; keep the other player's
        // reference to the shared battle until they claim as well.
        this.pvpManualBattles.delete(uid);
        this.pvpManualBattles.delete(String(battle.id));
        this.touch();
        return { success: true, message: 'Đã nhận kết quả trận đấu.' };
    }

    /** What the arena needs to draw a duelist: worn look, realm (aura strength) and the last move made. */
    pvpSidePresentation(side) {
        const p = this.player(side.userId);
        let realm = 0;
        try { realm = p ? Number(this.realmOf(p.userId)?.index) || 0 : 0; } catch (error) { realm = 0; }
        return { look: p ? wornLook(p) : null, gender: p?.gender || null, realm, lastAct: side.lastAct || null };
    }

    getPvpBattleView(b, userId) {
        const uid = String(userId);
        const me = b.p1.userId === uid ? b.p1 : b.p2;
        const foe = b.p1.userId === uid ? b.p2 : b.p1;
        const now = this.now();
        const botTakeoverEligible = !b.over && !b.isBot && uid === b.p1.userId && b.nextActorId === b.p2.userId;
        const botTakeoverLeftSec = botTakeoverEligible
            ? Math.max(0, Math.ceil((PVP_BOT_IDLE_MS - (now - (b.lastActionAt || now))) / 1000))
            : 0;
        regenerateBattleMp(me, now);
        regenerateBattleMp(foe, now);
        return {
            id: b.id,
            challengeId: b.challengeId,
            over: Boolean(b.over),
            winnerId: b.winnerId,
            isWin: b.winnerId ? b.winnerId === uid : null,
            round: b.round,
            isBot: Boolean(b.isBot),
            purpose: b.purpose || null,
            sectWar: Boolean(b.sectWarChallenge),
            canBotTakeover: botTakeoverEligible && botTakeoverLeftSec <= 0,
            botTakeoverLeftSec,
            // Human-vs-human PvP is real-time and non-turn-based: each player
            // acts only when they press an action. NPC/bot responses are handled separately.
            myTurn: !b.over,
            me: {
                name: me.name,
                hp: Math.max(0, me.hp),
                maxHp: me.maxHp,
                mp: Math.max(0, me.mp),
                maxMp: me.maxMp,
                shield: me.shield || 0,
                element: me.element,
                power: me.power,
                stunTurns: me.stunTurns || 0,
                bindTurns: me.bindTurns || 0,
                dot: me.dot ? { ticks: me.dot.ticks, source: me.dot.source } : null,
                immuneTurns: me.immuneTurns || 0,
                dodgeReadyAt: me.dodgeReadyAt || 0,
                dodgeCdLeft: (me.dodgeReadyAt || 0) > now ? Math.ceil((me.dodgeReadyAt - now) / 1000) : 0,
                attackCdLeft: (me.attackReadyAt || 0) > now ? Math.ceil((me.attackReadyAt - now) / 1000) : 0,
                skills: (me.skills || []).map((s, i) => {
                    const cdUntil = skillCooldownUntil(me.skillCds, s.id, now);
                    const cdLeft = cdUntil > now ? Math.ceil((cdUntil - now) / 1000) : 0;
                    return {
                        id: s.id,
                        name: s.name,
                        icon: s.icon || '✨',
                        kind: s.kind,
                        mp: s.mp || 0,
                        power: s.power || 1,
                        cd: s.cd || 5,
                        cdLeft,
                        canUse: me.mp >= (s.mp || 0) && cdLeft <= 0 && ((me.stunTurns || 0) <= 0 || s.kind === 'escape' || s.cleanse),
                    };
                }),
                dodging: me.dodgeUntil > now || Boolean(me.dodgePending),
                canDodge: !(me.stunTurns > 0) && !(me.bindTurns > 0),
                ...this.pvpSidePresentation(me),
            },
            opponent: {
                name: foe.name,
                hp: Math.max(0, foe.hp),
                maxHp: foe.maxHp,
                mp: Math.max(0, foe.mp),
                maxMp: foe.maxMp,
                shield: foe.shield || 0,
                element: foe.element,
                power: foe.power,
                stunTurns: foe.stunTurns || 0,
                bindTurns: foe.bindTurns || 0,
                dot: foe.dot ? { ticks: foe.dot.ticks, source: foe.dot.source } : null,
                immuneTurns: foe.immuneTurns || 0,
                dodging: foe.dodgeUntil > now || Boolean(foe.dodgePending),
                ...this.pvpSidePresentation(foe),
            },
            log: (b.log || []).slice(-15),
            result: b.result || null,
        };
    }

    applyPvpControl(target, now, { stun = 0, bind = 0 } = {}) {
        if ((target.immuneTurns || 0) > 0) {
            target.immuneTurns -= 1;
            return false;
        }
        const duration = Math.max(stun, bind);
        const turns = Math.max(1, Math.ceil(duration / 4));
        const freezeMs = Math.min(4, Math.max(1, duration)) * 1000;
        target.skillCds = target.skillCds || {};
        for (const id of Object.keys(target.skillCds)) {
            if (target.skillCds[id] > now) target.skillCds[id] += freezeMs;
        }
        if ((target.dodgeReadyAt || 0) > now) target.dodgeReadyAt += freezeMs;
        if (stun > 0) {
            target.stunTurns = Math.max(target.stunTurns || 0, turns);
            target.attackReadyAt = Math.max(target.attackReadyAt || 0, now) + freezeMs;
        }
        if (bind > 0) {
            target.bindTurns = Math.max(target.bindTurns || 0, turns);
            target.attackReadyAt = Math.max(target.attackReadyAt || 0, now) + freezeMs;
        }
        return true;
    }

    pvpManualDamage(battle, attacker, defender, power = 1, options = {}, now = this.now()) {
        if (this.rng() >= combatHitChance(attacker.accuracy, defender.dodge)) {
            battle.log.push(`💨 ${attacker.name} đánh hụt ${defender.name} (chính xác ${Math.round(attacker.accuracy || 90)}% · né ${Math.round(defender.dodge || 0)}%).`);
            return { dmg: 0, crit: false, hit: false, evaded: true };
        }
        const elementWins = C.HE[attacker.element]?.beats === defender.element;
        const elementLoses = C.HE[defender.element]?.beats === attacker.element;
        const forceCounter = (attacker.forceCounterTurns || 0) > 0;
        const elementMul = forceCounter || elementWins ? 1.2 : (elementLoses ? 0.85 : 1);
        const powerRatio = attacker.power / Math.max(1, defender.power);
        const powerMul = powerRatio >= 1
            ? Math.min(2.5, 1 + (powerRatio - 1) * 0.4)
            : Math.max(0.2, 1 / (1 + (1 / powerRatio - 1) * 0.6));
        let atkMul = 1 + (attacker.buffs?.atk || 0);
        if ((attacker.buffs?.rage || 0) > 0) atkMul += 1 - attacker.hp / Math.max(1, attacker.maxHp);
        const critChance = clamp(attacker.crit + (attacker.buffs?.crit || 0), 0, 0.9);
        const crit = !options.controlOnly && (Boolean(attacker.nextCrit) || this.rng() < critChance);
        if (!options.controlOnly) attacker.nextCrit = false;
        const skillMasteryMul = options.skillName ? cappedSkillMasteryMultiplier(attacker, options.skillTotal, options.skillCap) : 1;
        const critMul = crit ? Math.min(3.5, Math.max(1.5, Number(attacker.critDmg || 150) / 100)) : 1;
        const raw = attacker.atk * atkMul * power * elementMul * powerMul * skillMasteryMul * critMul * (0.9 + this.rng() * 0.2);
        const def = defender.def * (1 - clamp(defender.buffs?.defBreak || 0, 0, 0.8));
        let dmg = options.controlOnly ? 0 : Math.max(1, damageAfterDefense(raw, def));
        dmg = Math.round(dmg * Math.max(0.1, defender.buffs?.dmgTaken || 1));
        const wasImmune = (defender.immuneTurns || 0) > 0;
        let activeDodge = false;

        if (wasImmune) {
            defender.immuneTurns -= 1;
            dmg = 0;
            battle.log.push(`🛡️ ${defender.name} hóa giải hoàn toàn đòn đánh nhờ hộ thể.`);
        } else if ((defender.dodgeUntil || 0) > now || defender.dodgePending) {
            activeDodge = true;
            dmg = Math.round(dmg * 0.1);
            defender.dodgeUntil = 0;
            defender.dodgePending = false;
            battle.log.push(`💨 ${defender.name} né thành công, chỉ nhận 10% sát thương.`);
        }

        if (!options.pierceShield && defender.shield > 0) {
            const absorbed = Math.min(defender.shield, dmg);
            defender.shield -= absorbed;
            dmg -= absorbed;
        }
        defender.hp = Math.max(0, defender.hp - dmg);

        if (defender.reflectPct > 0 && dmg > 0) {
            let back = Math.max(1, Math.round(dmg * defender.reflectPct));
            if (attacker.shield > 0) {
                const absorbed = Math.min(attacker.shield, back);
                attacker.shield -= absorbed;
                back -= absorbed;
            }
            attacker.hp = Math.max(0, attacker.hp - back);
            defender.reflectPct = 0;
            battle.log.push(`🔁 ${defender.name} phản lại ${back} sát thương.`);
        }
        if (!activeDodge && !wasImmune && options.stun && this.rng() < (options.stunChance ?? 1)) {
            this.applyPvpControl(defender, now, { stun: options.stun });
        }
        if (!activeDodge && !wasImmune && options.bind && this.rng() < (options.bindChance ?? 1)) {
            this.applyPvpControl(defender, now, { bind: options.bind });
        }
        if (!activeDodge && !wasImmune && options.dot) {
            defender.dot = { damage: Math.max(1, Math.round(attacker.atk * options.dot * skillMasteryMul)), ticks: options.dotTicks || 3, source: options.skillName || attacker.name };
        }
        battle.log.push(`${options.skillName ? `✨ ${attacker.name} dùng [${options.skillName}]` : `⚔️ ${attacker.name} tấn công`} gây ${dmg} sát thương${crit ? ' (BẠO KÍCH!)' : ''} (${defender.name} còn ${defender.hp} KH)`);
        return { dmg, crit, hit: true, evaded: activeDodge };
    }

    pvpManualSkillEffect(battle, user, target, skill, now) {
        const durTurns = Math.max(2, Math.ceil((skill.dur || 4) / 4));
        const damage = skillDamageProfile(skill);
        const heal = amount => {
            const value = Math.max(1, Math.round(user.maxHp * amount));
            const before = user.hp;
            user.hp = Math.min(user.maxHp, user.hp + value);
            return user.hp - before;
        };
        switch (skill.kind) {
            case 'shield': {
                const amount = Math.round(user.maxHp * (skill.shield || 0.3));
                user.shield += amount;
                battle.log.push(`🛡️ ${user.name} tạo hộ thuẫn ${amount} KH bằng [${skill.name}].`);
                break;
            }
            case 'heal': {
                const amount = heal(skill.heal || 0.25);
                const healPct = Math.round((skill.heal || 0.25) * 100);
                if (skill.cleanse) { user.stunTurns = 0; user.bindTurns = 0; user.dot = null; user.buffs.defBreak = 0; }
                battle.log.push(`🌿 ${user.name} dùng [${skill.name}], hồi +${healPct}% KH (+${amount})${skill.cleanse ? ' và thanh tẩy dị trạng' : ''}.`);
                break;
            }
            case 'mana': {
                const amount = Math.round(user.maxMp * (skill.mana || 0.3));
                user.mp = Math.min(user.maxMp, user.mp + amount);
                battle.log.push(`💧 ${user.name} dùng [${skill.name}], hồi ${amount} linh lực.`);
                break;
            }
            case 'buff': {
                const buff = skill.buff || {};
                user.buffs.atk = buff.atk || user.buffs.atk || 0;
                user.buffs.crit = buff.crit || user.buffs.crit || 0;
                user.buffs.dmgTaken = buff.dmgTaken || user.buffs.dmgTaken || 1;
                user.buffs.rage = buff.rage || user.buffs.rage || 0;
                user.forceCounterTurns = buff.forceCounter ? durTurns : (user.forceCounterTurns || 0);
                user.buffTurns = Math.max(user.buffTurns || 0, durTurns);
                if (buff.maxHpMul) {
                    user.baseMaxHp = user.baseMaxHp || user.maxHp;
                    const newMax = Math.round(user.maxHp * buff.maxHpMul);
                    user.hp = Math.min(newMax, user.hp + newMax - user.maxHp);
                    user.maxHp = newMax;
                }
                battle.log.push(`☯ ${user.name} kích hoạt [${skill.name}] trong ${durTurns} lượt.`);
                break;
            }
            case 'escape':
                user.stunTurns = 0;
                user.bindTurns = 0;
                user.dot = null;
                user.buffs.defBreak = 0;
                user.immuneTurns = Math.max(user.immuneTurns || 0, skill.immune ? Math.max(1, Math.ceil(skill.immune / 4)) : 1);
                if (skill.dodge) user.dodgePending = true;
                if (skill.nextCrit) user.nextCrit = true;
                if (skill.heal) heal(skill.heal);
                if (skill.stunEnemy) this.pvpManualDamage(battle, user, target, 0, { skillName: skill.name, controlOnly: true, stun: skill.stunEnemy });
                battle.log.push(`💨 ${user.name} dùng [${skill.name}], giải khống chế và tạo thế né.`);
                break;
            case 'stun':
            case 'bind':
                this.pvpManualDamage(battle, user, target, 0, {
                    skillName: skill.name,
                    controlOnly: true,
                    stun: skill.kind === 'stun' && !skill.bind ? (skill.stun || 1) : 0,
                    stunChance: skill.stunChance,
                    bind: skill.kind === 'bind' || skill.bind ? (skill.bind || skill.stun || 1) : 0,
                    bindChance: skill.bindChance,
                });
                break;
            case 'dot':
                this.pvpManualDamage(battle, user, target, 0, {
                    skillName: skill.name,
                    controlOnly: true,
                    dot: damage.perTick,
                    dotTicks: damage.ticks,
                    skillTotal: damage.total,
                    skillCap: damage.cap,
                });
                break;
            case 'reflect':
                user.reflectPct = skill.reflect || 0.15;
                battle.log.push(`🔁 ${user.name} dựng phản kích [${skill.name}], phản ${Math.round(user.reflectPct * 100)}% đòn kế tiếp.`);
                break;
            case 'multi': {
                let landed = false;
                for (let i = 0; i < damage.hits && target.hp > 0; i += 1) {
                    const result = this.pvpManualDamage(battle, user, target, damage.perHit, { skillName: skill.name, pierceShield: skill.pierceShield, skillTotal: damage.total, skillCap: damage.cap });
                    landed = landed || result.hit;
                }
                if (landed && skill.stun && this.rng() < (skill.stunChance ?? 1)) this.applyPvpControl(target, now, { stun: skill.stun });
                if (landed && skill.bind && this.rng() < (skill.bindChance ?? 1)) this.applyPvpControl(target, now, { bind: skill.bind });
                if (landed && skill.dot) target.dot = { damage: Math.max(1, Math.round(user.atk * damage.dotPerTick * cappedSkillMasteryMultiplier(user, damage.total, damage.cap))), ticks: damage.dotTicks, source: skill.name };
                break;
            }
            default:
                this.pvpManualDamage(battle, user, target, damage.total, {
                    skillName: skill.name,
                    pierceShield: Boolean(skill.pierceShield),
                    skillTotal: damage.total,
                    skillCap: damage.cap,
                    stun: skill.stun,
                    stunChance: skill.stunChance,
                    bind: skill.bind,
                    bindChance: skill.bindChance,
                    dot: skill.dot ? damage.dotPerTick : 0,
                    dotTicks: damage.dotTicks,
                });
                if (skill.heal) heal(skill.heal);
                break;
        }
    }

    pvpBotResponse(battle, now, viewerUserId) {
        const me = battle.p1;
        const bot = battle.p2;
        if (bot.buffTurns > 0 && bot.buffAppliedAtRound < battle.round) {
            bot.buffTurns -= 1;
            bot.forceCounterTurns = Math.max(0, (bot.forceCounterTurns || 0) - 1);
            if (!bot.buffTurns) {
                bot.buffs = {};
                if (bot.baseMaxHp) { bot.maxHp = bot.baseMaxHp; bot.hp = Math.min(bot.hp, bot.maxHp); }
            }
        }
        if ((bot.dot?.ticks || 0) > 0) {
            bot.hp = Math.max(0, bot.hp - bot.dot.damage);
            bot.dot.ticks -= 1;
            battle.log.push(`☠️ ${bot.name} chịu ${bot.dot.damage} sát thương từ ${bot.dot.source}.`);
            if (bot.dot.ticks <= 0) bot.dot = null;
        }
        if (bot.hp <= 0) return this._finishPvpManualBattle(battle, me.userId, viewerUserId);
        if (bot.stunTurns > 0) {
            bot.stunTurns -= 1;
            battle.log.push(`⛓️ ${bot.name} bị khống chế, mất lượt phản kích.`);
        } else {
            const canSkill = (bot.skills || []).filter(skill => (skill.mp || 0) <= bot.mp && !(bot.skillCds?.[skill.id] > now));
            const healSkill = bot.hp < bot.maxHp * 0.35 ? canSkill.find(skill => skill.kind === 'heal' || skill.kind === 'shield') : null;
            const chosenSkill = healSkill || (canSkill.length && this.rng() < 0.5 ? canSkill[Math.floor(this.rng() * canSkill.length)] : null);
            if (chosenSkill) {
                bot.skillCds = bot.skillCds || {};
                bot.skillCds[chosenSkill.id] = now + chosenSkill.cd * 1000;
                bot.mp -= (chosenSkill.mp || 0);
                bot.lastAct = { seq: (bot.lastAct?.seq || 0) + 1, act: 'skill', skillId: chosenSkill.id, name: chosenSkill.name, kind: chosenSkill.kind, big: Boolean(chosenSkill.big), at: now };
                this.pvpManualSkillEffect(battle, bot, me, chosenSkill, now);
            } else {
                bot.lastAct = { seq: (bot.lastAct?.seq || 0) + 1, act: 'attack', at: now };
                this.pvpManualDamage(battle, bot, me, 0.95 + this.rng() * 0.25);
            }
        }
        if (me.hp <= 0) return this._finishPvpManualBattle(battle, bot.userId, viewerUserId);
        if (bot.hp <= 0) return this._finishPvpManualBattle(battle, me.userId, viewerUserId);
        battle.nextActorId = me.userId;
        battle.round += 1;
        battle.lastActionAt = now;
        this.touch();
        return this.getPvpBattleView(battle, viewerUserId);
    }

    pvpManualBotTakeover(userId, battleId) {
        this.pvpManualBattles = this.pvpManualBattles || new Map();
        const uid = String(userId);
        const battle = this.pvpManualBattles.get(uid);
        if (!battle || (battleId && String(battle.id) !== String(battleId))) fail('Không có trận quyết đấu nào đang diễn ra.');
        if (uid !== battle.p1.userId) fail('Chỉ người đã mở trận mới có thể gọi Bot Hộ Đạo thay đối thủ vắng mặt.');
        if (battle.over) fail('Trận quyết đấu đã kết thúc.');
        if (battle.isBot) fail('Bot Hộ Đạo đã tiếp quản trận này.');
        if (battle.nextActorId !== battle.p2.userId) fail('Đối thủ chưa đến lượt hoặc trận đang chờ lượt đầu tiên của bạn.');
        const now = this.now();
        const elapsedMs = now - (battle.lastActionAt || now);
        const leftMs = PVP_BOT_IDLE_MS - elapsedMs;
        if (leftMs > 0) fail(`Đối thủ mới vắng mặt ${Math.floor(elapsedMs / 1000)} giây. Có thể gọi Bot Hộ Đạo sau ${Math.ceil(leftMs / 1000)} giây.`);

        battle.isBot = true;
        if (!battle.p2.name.startsWith('[Bot Hộ Đạo] ')) battle.p2.name = `[Bot Hộ Đạo] ${battle.p2.name}`;
        if (this.pvpManualBattles.get(battle.p2.userId) === battle) this.pvpManualBattles.delete(battle.p2.userId);
        battle.log.push(`🤖 ${battle.p2.name} tiếp quản trận đấu sau 2 phút đối thủ không xuất chiêu.`);
        this.touch();
        return this.pvpBotResponse(battle, now, uid);
    }

    pvpManualAction(userId, battleId, action, skillId = null) {
        this.pvpManualBattles = this.pvpManualBattles || new Map();
        const uid = String(userId);
        const battle = this.pvpManualBattles.get(uid);
        if (!battle || (battleId && String(battle.id) !== String(battleId))) fail('Không có trận quyết đấu nào đang diễn ra.');
        if (battle.p1.userId !== uid && battle.p2.userId !== uid) fail('Bạn không tham gia trận quyết đấu này.');
        if (battle.over) return this.getPvpBattleView(battle, String(userId));

        const now = this.now();
        const me = battle.p1.userId === uid ? battle.p1 : battle.p2;
        const foe = battle.p1.userId === uid ? battle.p2 : battle.p1;
        regenerateBattleMp(me, now);
        regenerateBattleMp(foe, now);
        if (!['attack', 'dodge', 'pass', 'skill'].includes(action)) fail('Thao tác chiến đấu không hợp lệ.');
        const skillDef = action === 'skill' ? ((me.skills || []).find(s => s.id === skillId) || C.SKILL_BY_ID.get(skillId)) : null;
        if (action === 'skill' && !skillDef) fail('Không tìm thấy kỹ năng này.');
        if (skillDef) {
            const owner = this.player(uid);
            const reqMa = requiredMaForSkill(skillDef);
            if (reqMa && (owner?.maScore || 0) < reqMa) fail(`${skillDef.name} yêu cầu Ma Tính tối thiểu ${reqMa} (hiện có ${owner?.maScore || 0}).`);
            if (skillDef.reqDao && Number(owner?.daoScore ?? owner?.daoTam ?? 100) < skillDef.reqDao) fail(`${skillDef.name} yêu cầu Đạo Tâm tối thiểu ${skillDef.reqDao}.`);
        }
        if (action === 'pass' && !me.stunTurns) fail('Chỉ có thể bỏ lượt khi đang bị khống chế.');
        if (action === 'dodge' && (me.bindTurns || 0) > 0) fail('Đang bị trói, không thể thi triển thân pháp né.');
        if (action === 'attack' && (me.attackReadyAt || 0) > now) fail(`Đòn đánh thường đang hồi, còn ${Math.ceil((me.attackReadyAt - now) / 1000)} giây.`);

        const actorRecord = this.data.players?.[uid];
        const targetRecord = this.data.players?.[String(foe.userId)];
        const nonAttackSkill = skillDef && ['buff', 'heal', 'shield', 'escape', 'cleanse', 'mana'].includes(skillDef.kind);
        if (actorRecord && !actorRecord.isNpc && targetRecord && !targetRecord.isNpc
            && action !== 'pass' && action !== 'dodge' && !nonAttackSkill) {
            this.recordPvpAttack(actorRecord, now);
        }

        if ((me.dot?.ticks || 0) > 0) {
            me.hp = Math.max(0, me.hp - me.dot.damage);
            me.dot.ticks -= 1;
            battle.log.push(`☠️ ${me.name} chịu ${me.dot.damage} sát thương từ ${me.dot.source}.`);
            if (me.dot.ticks <= 0) me.dot = null;
            if (me.hp <= 0) return this._finishPvpManualBattle(battle, foe.userId, uid);
        }
        if (me.buffTurns > 0 && me.buffAppliedAtRound < battle.round) {
            me.buffTurns -= 1;
            me.forceCounterTurns = Math.max(0, (me.forceCounterTurns || 0) - 1);
            if (!me.buffTurns) {
                me.buffs = {};
                if (me.baseMaxHp) { me.maxHp = me.baseMaxHp; me.hp = Math.min(me.hp, me.maxHp); }
            }
        }

        // Each combatant acts only after a manual input. A control effect consumes
        // that player's next chosen action; it never triggers an automatic attack.
        if (me.stunTurns > 0 && skillDef?.kind !== 'escape' && !skillDef?.cleanse) {
            me.stunTurns -= 1;
            battle.log.push(`⛓️ ${me.name} bị khống chế, mất lượt xuất chiêu.`);
        } else if (action === 'dodge') {
            if ((me.dodgeReadyAt || 0) > now) fail(`Thân pháp đang hồi chiêu, còn ${Math.ceil((me.dodgeReadyAt - now) / 1000)} giây.`);
            me.dodgeUntil = now + 30000;
            me.dodgePending = true;
            me.dodgeReadyAt = now + 6000;
            me.lastAct = { seq: (me.lastAct?.seq || 0) + 1, act: 'dodge', at: now };
            battle.log.push(`💨 ${me.name} thi triển thân pháp né tránh ảo diệu!`);
        } else if (skillDef) {
            me.skillCds = me.skillCds || {};
            if (me.skillCds[skillId] && me.skillCds[skillId] > now) {
                const sec = Math.ceil((me.skillCds[skillId] - now) / 1000);
                fail(`[${skillDef.name}] đang hồi chiêu (còn ${sec}s)!`);
            }
            if (me.mp < (skillDef.mp || 0)) fail(`Linh lực không đủ để thi triển [${skillDef.name}] (cần ${skillDef.mp} LL).`);
            me.skillCds[skillId] = now + skillDef.cd * 1000;
            me.mp -= (skillDef.mp || 0);
            me.lastAct = { seq: (me.lastAct?.seq || 0) + 1, act: 'skill', skillId: skillDef.id, name: skillDef.name, kind: skillDef.kind, big: Boolean(skillDef.big), at: now };
            this.pvpManualSkillEffect(battle, me, foe, skillDef, now);
        } else {
            me.lastAct = { seq: (me.lastAct?.seq || 0) + 1, act: 'attack', at: now };
            this.pvpManualDamage(battle, me, foe, 0.95 + this.rng() * 0.25);
            me.attackReadyAt = now + 1000;
        }
        if (me.bindTurns > 0) me.bindTurns -= 1;

        // Check if foe defeated
        if (foe.hp <= 0) {
            return this._finishPvpManualBattle(battle, me.userId, uid);
        }

        // 2. Bot Hộ Đạo luôn phản kích ngay trong cùng lượt.
        if (battle.isBot) return this.pvpBotResponse(battle, now, uid);
        else {
            battle.nextActorId = foe.userId;
            battle.log.push(`⌛ Đã cập nhật hành động của ${me.name}; đối thủ vẫn có thể chủ động ra chiêu.`);
        }

        battle.round += 1;
        battle.lastActionAt = now;
        this.touch();
        return this.getPvpBattleView(battle, uid);
    }

    _finishPvpManualBattle(battle, winnerUserId, viewerUserId = winnerUserId) {
        battle.over = true;
        battle.winnerId = winnerUserId;
        const now = this.now();
        const p1 = this.requirePlayer(battle.p1.userId);
        const p2 = this.requirePlayer(battle.p2.userId);
        p1.pvp = p1.pvp || { points: 1000, wins: 0, losses: 0 };
        p2.pvp = p2.pvp || { points: 1000, wins: 0, losses: 0 };

        const winner = winnerUserId === battle.p1.userId ? p1 : p2;
        const loser = winnerUserId === battle.p1.userId ? p2 : p1;

        const winnerWear = this.wearEquippedDurability(winner, () => 1);
        const winnerWearMessage = this.durabilitySummary(winnerWear, '🛠️ Trang bị sau chiến đấu');
        if (winnerWearMessage) {
            winner.notices = winner.notices || [];
            winner.notices.push(winnerWearMessage);
            battle.log.push(winnerWearMessage);
        }
        const loserWear = this.wearEquippedDurability(loser, item => this.injuryDurabilityLoss(item));
        const loserWearMessage = this.durabilitySummary(loserWear, '🛠️ Trọng thương làm hư hại trang bị');
        if (loserWearMessage) {
            loser.notices = loser.notices || [];
            loser.notices.push(loserWearMessage);
            battle.log.push(loserWearMessage);
        }

        if (battle.purpose === 'companion_rob') {
            const robber = p1;
            const defender = p2;
            robber.pvp = robber.pvp || { points: 1000, wins: 0, losses: 0 };
            defender.pvp = defender.pvp || { points: 1000, wins: 0, losses: 0 };
            winner.pvp.wins = (winner.pvp.wins || 0) + 1;
            winner.pvp.points = (winner.pvp.points || 1000) + 20;
            loser.pvp.losses = (loser.pvp.losses || 0) + 1;
            loser.pvp.points = Math.max(0, (loser.pvp.points || 1000) - 10);
            loser.injuredUntil = now + C.RULES.injuryMs;
            loser.hp = Math.max(1, Math.floor((this.stats(loser, now).hp || 500) * 0.15));

            let companionRobbed = false;
            let companionName = '';
            if (winner.userId === robber.userId && defender.companion) {
                const taken = defender.companion;
                companionName = taken.name || 'đạo lữ';
                robber.companion = { ...taken, intimacy: 20, protectedUntil: now + 12 * 3600 * 1000, songTuToday: 0, lastSongTuDate: '' };
                if (!taken.isNpc) {
                    const partner = this.player(taken.id);
                    if (partner) partner.companion = { id: String(robber.userId), name: robber.fullName || robber.name, gender: robber.gender, isNpc: false, intimacy: 20, marriedAt: new Date(now).toISOString(), songTuToday: 0, lastSongTuDate: '', protectedUntil: now + 12 * 3600 * 1000 };
                }
                defender.companion = null;
                companionRobbed = true;
                robber.notices = robber.notices || [];
                robber.notices.push(`⚡ Chiến thắng ${defender.fullName || defender.name} trên lôi đài, đạo lữ ${companionName} đã thuộc về bạn.`);
                defender.notices = defender.notices || [];
                defender.notices.push(`⚡ Đạo lữ ${companionName} đã bị ${robber.fullName || robber.name} cướp sau khi đánh bại bạn trong PvP.`);
                battle.log.push(`💔 ${robber.name} chiến thắng và đoạt được đạo lữ ${companionName} của ${defender.name}!`);
            } else {
                if (defender.companion) defender.companion.protectedUntil = now + 6 * 3600 * 1000;
                robber.notices = robber.notices || [];
                robber.notices.push(`⚔️ Thất bại trong trận tranh đoạt đạo lữ trước ${defender.fullName || defender.name}; đạo lữ của đối phương được thiên đạo bảo hộ 6 giờ.`);
                defender.notices = defender.notices || [];
                defender.notices.push(`🛡️ Bạn đã bảo vệ thành công đạo lữ trước ${robber.fullName || robber.name}.`);
                battle.log.push(`🛡️ ${defender.name} chiến thắng và bảo vệ được đạo lữ ${defender.companion?.name || ''}.`);
            }

            battle.result = { winnerName: winner.fullName || winner.name, loserName: loser.fullName || loser.name, points: 20, stones: 0, exp: 0, companionRobbed, companionName };
            this.touch();
            return this.getPvpBattleView(battle, viewerUserId);
        }

        // Hai Ma Tu giao chiến với nhau như một trận PvP thường: không áp
        // dụng cơ chế trảm ma thanh tẩy/truy nã lên người thua.
        const demonDuel = Boolean(winner.isDemon && loser.isDemon);
        this.recordPlayerKill(winner, loser);

        let itemDroppedNotice = null;
        let bountyReward = 0;
        let demonRes = null;
        if (loser.isDemon && !demonDuel) {
            demonRes = this._handleDemonSlain(winner, loser, battle.log, winnerUserId);
            if (demonRes) {
                bountyReward = demonRes.bountyReward;
                if (demonRes.droppedNames?.length) itemDroppedNotice = demonRes.droppedNames.join(', ');
            }
        } else if (this.rng() < 0.30) {
            const droppable = this.getDroppableItems(loser);
            if (droppable.length > 0) {
                const item = droppable[Math.min(droppable.length - 1, Math.floor(this.rng() * droppable.length))];
                const dropped = this.takeDroppableItem(loser, item);
                if (dropped) {
                    const dName = itemName(dropped);
                    if (this.rng() < 0.80) {
                        const winnerBagCount = (winner.items || []).filter(it => it.place === 'bag').length;
                        if (winnerBagCount < C.RULES.bagSize) {
                            this.addStack(winner, dropped.kind, dropped.id, 1);
                            itemDroppedNotice = `${winner.name} đã đoạt được [${dName}] từ ${loser.name}!`;
                        } else {
                            this.stockAdd(dropped.kind === 'equip' ? 'item' : (dropped.kind === 'scroll' ? 'skill' : 'cons'), dropped.id, 1);
                            itemDroppedNotice = `[${dName}] của ${loser.name} bị rơi vào Kho Thiên Địa do túi đối phương đã đầy.`;
                        }
                    } else {
                        this.stockAdd(dropped.kind === 'equip' ? 'item' : (dropped.kind === 'scroll' ? 'skill' : 'cons'), dropped.id, 1);
                        itemDroppedNotice = `[${dName}] của ${loser.name} bị đánh rơi vào hư không.`;
                    }
                    battle.log.push(`🔥 CHIẾN BÁO: ${itemDroppedNotice}`);
                }
            }
        }

        winner.pvp.wins += 1;
        winner.pvp.points += 20;
        loser.pvp.losses += 1;
        loser.pvp.points = Math.max(0, loser.pvp.points - 10);
        loser.injuredUntil = now + C.RULES.injuryMs;
        loser.hp = Math.max(1, Math.floor((this.stats(loser, now).hp || 500) * 0.15));

        let loserLostExp = 0;
        const loserRealm = this.realmOf(loser.userId);
        const loserCurExp = Math.max(0, loserRealm?.experience || 0);
        if (loser.isDemon && !demonDuel) {
            const loserExpPenalty = Math.max(150, Math.round(loserCurExp * 0.05));
            loserLostExp = this.realms.loseExp(loser.userId, loserExpPenalty);
            loser.notices = loser.notices || [];
            loser.notices.push(`⚔️ [LÔI ĐÀI THẤT BẠI] Ma Thể bị ${winner.name} đánh bại! Trọng thương ${Math.round(C.RULES.injuryMs / 60000)} phút và tổn thất ${loserLostExp} EXP tu vi.`);
        } else {
            const loserExpPenalty = Math.max(5, Math.round(loserCurExp * 0.02));
            loserLostExp = this.realms.loseExp(loser.userId, loserExpPenalty);
            loser.notices = loser.notices || [];
            loser.notices.push(`⚔️ [LÔI ĐÀI THẤT BẠI] Bạn đã bị ${winner.name} đánh bại! Trọng thương ${Math.round(C.RULES.injuryMs / 60000)} phút và tổn thất nhẹ ${loserLostExp} EXP tu vi.`);
        }

        if (winner.isDemon && !demonDuel) {
            this.triggerDemonTauHoaNhapMa(loser.userId, winner.name, loser.notices);
        }
        this.checkInjuryRealmDrop(loser.userId, loser.notices);

        const baseStones = Math.floor(10 + this.rng() * 10);
        winner.stones = (winner.stones || 0) + baseStones; // bountyReward was already credited to winner.stones in _handleDemonSlain
        const pvpExp = winner.isDemon ? 3200 : 1200;
        this.realms.addExp(winnerUserId, pvpExp);

        this.emit('attacked', {
            attackerId: winnerUserId,
            attackerName: winner.name,
            victimId: loser.userId,
            victimName: loser.name,
            won: true,
            lostStones: 0,
            lostExp: loserLostExp || (demonRes?.lostExp || 0),
            droppedItem: itemDroppedNotice,
            injuredMin: Math.round(C.RULES.injuryMs / 60000),
        });

        battle.result = {
            winnerName: winner.name,
            loserName: loser.name,
            stones: baseStones + bountyReward,
            exp: pvpExp + (bountyReward ? 1000 : 0),
            points: 20,
            itemDropped: itemDroppedNotice,
        };

        if (battle.sectRoleChallenge) {
            const { sectId, targetRole, challengerId, holderId } = battle.sectRoleChallenge;
            const s = this.sects[sectId];
            if (s) {
                if (String(winnerUserId) === String(challengerId)) {
                    s.members = s.members || {};
                    const rankBelow = { vice: 'dai_elder', dai_elder: 'elder', elder: 'dai_de_tu', dai_de_tu: 'noi_mon', noi_mon: 'ngoai_mon' };
                    if (holderId && s.members[holderId]) {
                        s.members[holderId].role = rankBelow[targetRole] || 'ngoai_mon';
                        const prevHolder = this.player(holderId);
                        if (prevHolder) prevHolder.sectRole = rankBelow[targetRole] || 'ngoai_mon';
                    }
                    s.members[challengerId] = s.members[challengerId] || {
                        name: winner.fullName || winner.name,
                        contributed: winner.sectContributed || 0,
                        joinedAt: new Date(now).toISOString(),
                    };
                    s.members[challengerId].name = winner.fullName || winner.name || s.members[challengerId].name;
                    s.members[challengerId].role = targetRole;
                    s.members[challengerId].contributed = Math.max(Number(s.members[challengerId].contributed) || 0, Number(winner.sectContributed) || 0);
                    winner.sectRole = targetRole;
                    const pChallenger = this.player(challengerId);
                    if (pChallenger) pChallenger.sectRole = targetRole;

                    const roleName = SECT_ROLE_NAMES[targetRole] || 'chức vị mới';
                    battle.log.push(`👑 ĐĂNG QUANG: ${winner.name} đã đánh bại đối thủ tranh chức và chính thức trở thành ${roleName} của ${s.name}!`);
                    battle.result.newRole = targetRole;
                    battle.result.newRoleName = roleName;
                    winner.notices = winner.notices || [];
                    winner.notices.push(`🏆 Bạn đã thắng trận tranh chức và chính thức được bổ nhiệm làm ${roleName} của ${s.name}!`);
                    this.touch();
                } else {
                    battle.log.push(`💀 Tranh chức thất bại! ${loser.name} vẫn giữ nguyên chức vụ và bị trọng thương ${Math.round(C.RULES.injuryMs / 60000)} phút!`);
                    loser.injuredUntil = now + C.RULES.injuryMs;
                }
            }
        }

        if (battle.sectWarChallenge) {
            const meta = battle.sectWarChallenge;
            const { warId, targetMemberId, foeFullName } = meta;
            const war = (this.data.sectWars || []).find(w => w.id === warId && w.status === 'active');
            if (war) {
                war.defeatedAttackers = Array.isArray(war.defeatedAttackers) ? war.defeatedAttackers : [];
                war.defeatedDefenders = Array.isArray(war.defeatedDefenders) ? war.defeatedDefenders : [];
                const actorUserId = String(meta.actorUserId || battle.p1.userId);
                const targetUserId = String(targetMemberId || battle.p2.userId);
                const loserId = String(loser.userId);
                const loserSectId = loserId === actorUserId
                    ? String(meta.actorSectId || war.attackerSectId)
                    : String(meta.targetSectId || (loserId === targetUserId ? war.defenderSectId : loser.sectId));
                const defeatedKey = loserSectId === String(war.attackerSectId) ? 'defeatedAttackers' : 'defeatedDefenders';
                if (!war[defeatedKey].includes(loserId)) war[defeatedKey].push(loserId);
                war.log ||= [];
                const winnerSectId = loserSectId === String(war.attackerSectId)
                    ? String(war.defenderSectId)
                    : String(war.attackerSectId);
                const winnerSectName = winnerSectId === String(war.attackerSectId) ? war.attackerName : war.defenderName;
                const loserSectName = loserSectId === String(war.attackerSectId) ? war.attackerName : war.defenderName;
                war.log.push(`⚔️ ${winner.fullName || winner.name} của [${winnerSectName}] đã đánh bại ${loser.fullName || loser.name || foeFullName} của [${loserSectName}]!`);
                if (this.isSectWarSideEliminated(war, loserSectId)) {
                    this.resolveSectWarElimination(war, loserSectId, now);
                }
                battle.result.sectWarWin = winnerSectId === String(war.attackerSectId);
                battle.result.sectWarWinnerSectId = winnerSectId;
                battle.result.sectWarLoserSectId = loserSectId;
            }
        }

        this.touch();
        return this.getPvpBattleView(battle, String(winnerUserId));
    }

    // ---- Đột Kích Thị Trấn (Town Attack) --------------------------------
    townAttack(userId, targetUserId, isRoam = false) {
        return this.pvpManualFight(userId, targetUserId, isRoam ? 'roam_attack' : 'town_attack');
    }

    // ---- Bỏ Trốn (Town Flee) -----------------------------------------------
    townFlee(userId) {
        const p = this.requirePlayer(userId);
        const now = this.now();
        const today = vnDate(now);
        p.dailyEscapes = p.dailyEscapeDate === today ? (p.dailyEscapes || 0) : 2;
        if (p.dailyEscapes <= 0) fail('Hôm nay đã dùng hết lượt trốn thoát (tối đa 2 lần/ngày).');

        // Flee within the current realm only; changing realm maps requires the paired Ascension Gate.
        const currentWorld = C.MAP_BY_ID.get(C.TOWN_BY_ID.get(p.town)?.mapId)?.worldId;
        const realm = this.realmOf(userId);
        const towns = C.TOWNS.filter(town => town.id !== p.town
            && C.MAP_BY_ID.get(town.mapId)?.worldId === currentWorld
            && realm.index >= (C.MAP_BY_ID.get(town.mapId)?.realmMin ?? 0)).map(town => town.id);
        if (!towns.length) fail('Không có thị trấn nào để chạy trốn.');
        const newTown = towns[Math.floor(Math.random() * towns.length)];
        p.dailyEscapes -= 1;
        p.dailyEscapeDate = today;
        const oldTown = p.town;
        p.town = newTown;
        if (C.MAP_BY_ID) {
            const townDef = C.TOWNS.find(t => t.id === newTown);
            if (townDef && townDef.mapId) p.mapId = townDef.mapId;
        }
        p.worldPosition = null;
        this.touch();
        const townDef = C.TOWNS.find(t => t.id === newTown);
        return {
            success: true,
            oldTown,
            newTown,
            escapesLeft: p.dailyEscapes,
            message: `🏃 Chạy trốn thành công! Bạn đã đến ${townDef ? townDef.name : newTown}. Lượt trốn còn lại hôm nay: ${p.dailyEscapes}.`,
        };
    }

    // ---- Hối Cải Ma Tu (Demon Repent) ----------------------------------------
    demonRepent(userId) {
        const p = this.requirePlayer(userId);
        if (!p.isDemon) fail('Đạo hữu không phải Ma Tu, không cần hối cải.');
        const cost = 500 + (p.killCount || 0) * 100;
        if (p.stones < cost) fail(`Cần ${cost} linh thạch để nộp phạt truy nã và thanh tẩy sát nghiệt.`);
        p.stones -= cost;
        const cleanDao = Math.max(100, Number(p.daoMaxScore) || Number(this.linhCan(p)?.daoTam) || 100);
        p.isDemon = false;
        p.killCount = 0;
        p.maScore = 0;
        p.daoScore = cleanDao;
        p.daoTam = cleanDao;
        p.demonTitle = '';
        p.lastPvpAttackAt = this.now();
        p.daoRecoveryDaysApplied = 0;
        this.touch();
        return { success: true, cost, message: `☀️ Đã nộp ${cost} linh thạch tiền phạt. Ma Tính và sát nghiệt đã xóa, Đạo Tâm trở về ${cleanDao}, lệnh truy nã được gỡ.` };
    }

    // ---- Quản Lý Thành Trấn & Lang Bạt Dã Ngoại ------------------------------
    enterTown(userId) {
        const p = this.requirePlayer(userId);
        p.isRoaming = false;
        this.touch();
        return { success: true, isRoaming: false, message: `🛡️ Đã bước vào [${(C.TOWN_BY_ID.get(p.town) || C.TOWNS[0]).name}] an toàn. Ở đây không thể bị tấn công dã ngoại.` };
    }

    leaveTown(userId) {
        const p = this.requirePlayer(userId);
        p.isRoaming = true;
        this.touch();
        return { success: true, isRoaming: true, message: '🌲 Đã rời thành bước vào dã ngoại lang bạt! Chú ý: Tu sĩ lang bạt có thể bị người khác đột kích tấn công trực tiếp mà không cần thách đấu!' };
    }

    roamAttack(userId, targetUserId) {
        const p1 = this.requirePlayer(userId);
        const p2 = this.requirePlayer(targetUserId);
        const now = this.now();
        if (String(userId) === String(targetUserId)) fail('Không thể tự tấn công chính mình.');
        if (p1.injuredUntil > now) fail('Đang trọng thương, không thể xuất chiến.');
        if (p2.injuredUntil > now) fail('Đối phương đang trọng thương, không thể tấn công.');

        const res = this.townAttack(userId, targetUserId, true);
        res.isRoamAttack = true;
        res.message = `💥 [ĐỘT KÍCH DÃ NGOẠI] Bạn đã mở trận thủ công với tu sĩ lang bạt [${p2.fullName || p2.name}]. Cả hai bên tự chọn hành động.`;
        return res;
    }

    // ---- Hợp Nhất Mảnh Bản Thảo ----------------------------------------------
    combineFragment(userId, bpId) {
        const p = this.requirePlayer(userId);
        const bp = (C.BLUEPRINTS || []).find(b => b.id === bpId || b.matId === bpId);
        if (!bp) fail('Không tìm thấy bản thảo này.');
        const realm = this.realmOf(userId).index;
        if (realm < bp.minRealm) {
            fail(`Cảnh giới chưa đủ để hợp nhất bản thảo này (yêu cầu tối thiểu ${this.realmName(bp.minRealm)}).`);
        }
        if (bp.element && p.he !== bp.element && (p.elemMastery?.[bp.element] || 0) < 15) {
            fail(`Bản thảo này yêu cầu người tu luyện thuộc hệ [${C.HE[bp.element]?.name || bp.element}] hoặc thông thạo hệ từ 15 điểm.`);
        }
        if (bp.reqMa && (p.maScore || 0) < bp.reqMa) {
            fail(`Bản thảo ma đạo này yêu cầu Ma Tính tối thiểu ${bp.reqMa} (hiện có: ${p.maScore || 0}).`);
        }
        if (bp.mon && bp.mon !== 'chung' && p.mon !== bp.mon) {
            fail(`Bản thảo này chỉ dành cho môn phái [${C.MON[bp.mon]?.name || bp.mon}].`);
        }

        const reqMatId = bp.matId;
        let totalFrags = 0;
        const fragItems = [];
        for (const it of p.items) {
            if (it.kind === 'mat' && it.id === reqMatId && it.place === 'bag') {
                totalFrags += (it.qty || 1);
                fragItems.push(it);
            }
        }
        if (totalFrags < bp.reqCount) {
            fail(`Chưa đủ mảnh bản thảo! Cần ${bp.reqCount} mảnh [${C.MATERIAL_BY_ID.get(reqMatId)?.name || reqMatId}], hiện có ${totalFrags} mảnh.`);
        }
        const blueprintSkill = bp.targetKind === 'skill' ? C.SKILL_BY_ID.get(bp.targetId) : null;
        if (bp.targetKind === 'skill' && !blueprintSkill) fail('Kỹ năng không tồn tại.');
        if (blueprintSkill && !this.canCreate('skill', blueprintSkill.id)) {
            fail(`Ngọc giản [${blueprintSkill.name}] đã hết bản có thể lưu hành.`);
        }

        let toDeduct = bp.reqCount;
        for (const it of fragItems) {
            if (toDeduct <= 0) break;
            const take = Math.min(it.qty || 1, toDeduct);
            if ((it.qty || 1) <= take) {
                p.items = p.items.filter(x => x.uid !== it.uid);
            } else {
                it.qty -= take;
            }
            toDeduct -= take;
        }

        let rewardText = '';
        const now = this.now();
        if (bp.targetKind === 'equip') {
            const def = C.EQUIP_BY_ID.get(bp.targetId);
            if (!def) fail('Vật phẩm trang bị không tồn tại.');
            const item = this.addEquip(p, def, now);
            rewardText = `nhận được trang bị [${def.name}]`;
        } else if (bp.targetKind === 'skill') {
            const def = blueprintSkill;
            if (p.skills.includes(def.id)) {
                if (this.addStack(p, 'scroll', def.id, 1) <= 0) fail('Túi đồ đã đầy, không thể nhận ngọc giản.');
                this.stockAdd('skill', def.id, 1);
                rewardText = `nhận được ngọc giản kỹ năng [${def.name}]`;
            } else {
                p.skills.push(def.id);
                this.stockAdd('skill', def.id, 1);
                rewardText = `đã đốn ngộ thành công tuyệt kỹ [${def.name}]`;
            }
        } else if (bp.targetKind === 'cons') {
            const def = C.CONSUMABLE_BY_ID.get(bp.targetId);
            if (!def) fail('Đan dược/phù lục không tồn tại.');
            const qty = bp.yieldQty || 1;
            this.addStack(p, 'cons', def.id, qty);
            rewardText = `chế tác thành công ${qty}x [${def.name}]`;
        }
        this.touch();
        return {
            success: true,
            message: `✨ [HỢP NHẤT THÀNH CÔNG] Đạo hữu đã gom đủ ${bp.reqCount} mảnh và ${rewardText}!`,
        };
    }

    // ---- Đổi Giới Tính --------------------------------------------------------
    changeGender(userId) {
        const p = this.requirePlayer(userId);
        const cost = C.RULES.changeGenderCost || 1000;
        if (p.stones < cost) fail(`Cần ${cost} linh thạch để đổi giới tính.`);
        if (p.companion) fail('Đang có đạo lữ, không thể đổi giới tính. Hãy chia tay trước.');
        p.stones -= cost;
        p.gender = p.gender === 'nam' ? 'nu' : 'nam';
        // Reset Qi
        p.duongKhi = 100;
        p.amKhi = 100;
        this.touch();
        return { success: true, gender: p.gender, message: `✨ Đổi giới tính thành công! Hiện tại: ${p.gender === 'nu' ? 'Nữ' : 'Nam'}. Âm/Dương Khí được hồi phục.` };
    }

    // ---- PvP Leaderboards -------------------------------------------------------
    resetPvpLeaderboard() {
        let count = 0;
        for (const p of Object.values(this.data.players || {})) {
            if (!p.registered || p.isNpc) continue;
            p.pvp = { ...(p.pvp || {}), points: 1000, wins: 0, losses: 0 };
            count += 1;
        }
        this.data.meta ||= {};
        this.data.meta.pvpSeasonResetAt = this.now();
        this.touch();
        return { count, resetAt: this.data.meta.pvpSeasonResetAt };
    }

    pvpTownLeaderboard(townId) {
        const now = this.now();
        const players = Object.values(this.data.players || {})
            .filter(p => p.registered && !p.isNpc && !this.isHiddenFromPlayers(p) && p.town === townId && this.isPvpActive(p, now))
            .map(p => {
                const st = this.stats(p, now);
                const r = this.realmOf(p.userId);
                const sect = p.sectId && this.sects[p.sectId] ? this.sects[p.sectId].name : null;
                const isThienMa = (p.maScore || 0) >= 1000;
                return {
                    userId: p.userId,
                    name: p.name,
                    fullName: p.fullName || p.name,
                    photoUrl: p.photoUrl || '',
                    points: p.pvp?.points || 1000,
                    power: st.power,
                    realmName: r?.name || '',
                    realm: r?.name || '',
                    town: p.town || 'thanh_van',
                    townName: C.TOWN_BY_ID?.get(p.town || 'thanh_van')?.name || 'Thanh Vân Thành',
                    wins: p.pvp?.wins || 0,
                    losses: p.pvp?.losses || 0,
                    sectName: sect,
                    isAscended: Boolean(p.ascended),
                    killCount: p.killCount || 0,
                    isDemon: Boolean(p.isDemon) || ((p.maScore || 0) > 0 && (p.maScore || 0) > Math.max(0, p.daoScore ?? p.daoTam ?? 100)),
                    isThienKieu: Boolean(p.isThienKieu),
                    isThienMa,
                };
            })
            .sort((a, b) => (b.points || 1000) - (a.points || 1000) || (b.power || 0) - (a.power || 0))
            .slice(0, 20);
        return players.map((p, i) => ({
            ...p,
            rank: i + 1,
            isDang: i === 0 && p.isAscended && !p.isDemon,
            isNhanHoang: i === 0 && !p.isAscended && !p.isDemon,
            isThanhHoang: i === 0 && !p.isAscended && !p.isDemon,
            title: p.isThienMa ? '☯️ Thiên Ma' : i === 0 && !p.isDemon ? (p.isAscended ? '✨ Đấng' : '👑 Nhân Hoàng') : '',
        }));
    }

    pvpDemonLeaderboard() {
        const now = this.now();
        const players = Object.values(this.data.players || {})
            .filter(p => p.registered && !p.isNpc && !this.isHiddenFromPlayers(p) && (p.isDemon || ((p.maScore || 0) > 0 && (p.maScore || 0) > Math.max(0, p.daoScore ?? p.daoTam ?? 100))) && this.isPvpActive(p, now))
            .map(p => {
                const st = this.stats(p, now);
                const r = this.realmOf(p.userId);
                const sect = p.sectId && this.sects[p.sectId] ? this.sects[p.sectId].name : null;
                const bounty = Math.round(250 + (p.killCount || 5) * 50);
                const isThienMa = (p.maScore || 0) >= 1000;
                return {
                    userId: p.userId,
                    name: p.name,
                    fullName: p.fullName || p.name,
                    photoUrl: p.photoUrl || '',
                    killCount: p.killCount || 0,
                    points: p.pvp?.points || 1000,
                    bounty,
                    power: st.power,
                    realmName: r?.name || '',
                    realm: r?.name || '',
                    town: p.town || 'thanh_van',
                    townName: C.TOWN_BY_ID?.get(p.town || 'thanh_van')?.name || 'Thanh Vân Thành',
                    wins: p.pvp?.wins || 0,
                    losses: p.pvp?.losses || 0,
                    sectName: sect,
                    isDemon: true,
                    isThienKieu: Boolean(p.isThienKieu),
                    isThienMa,
                };
            })
            .sort((a, b) => (b.killCount || 0) - (a.killCount || 0) || (b.power || 0) - (a.power || 0))
            .slice(0, 20);
        return players.map((p, i) => ({
            ...p,
            rank: i + 1,
            title: p.isThienMa ? '☯️ Thiên Ma' : i === 0 ? '👿 Ma Tôn' : i === 1 ? '😈 Ma Tướng' : '🔴 Ma Tu',
        }));
    }

    pvpDaiPhongThanLeaderboard() {
        const now = this.now();

        // Bảng Cửu Châu dành riêng cho người chưa Phi Thăng.
        const playerList = Object.values(this.data.players || {})
            .filter(p => p.registered && !p.isNpc && !p.ascended && !this.isHiddenFromPlayers(p) && this.isPvpActive(p, now))
            .map(p => {
                const st = this.stats(p, now);
                const r = this.realmOf(p.userId);
                const sect = p.sectId && this.sects[p.sectId] ? this.sects[p.sectId].name : null;
                const isThienMa = (p.maScore || 0) >= 1000;
                return {
                    userId: p.userId,
                    name: p.name,
                    fullName: p.fullName || p.name,
                    photoUrl: p.photoUrl || '',
                    points: p.pvp?.points || 1000,
                    power: st.power,
                    realmName: r?.name || '',
                    realm: r?.name || '',
                    wins: p.pvp?.wins || 0,
                    losses: p.pvp?.losses || 0,
                    sectName: sect,
                    isDemon: Boolean(p.isDemon) || ((p.maScore || 0) > 0 && (p.maScore || 0) > Math.max(0, p.daoScore ?? p.daoTam ?? 100)),
                    isThienKieu: Boolean(p.isThienKieu),
                    isThienMa,
                    isNpc: false,
                };
            });

        playerList.sort((a, b) => (b.points || 1000) - (a.points || 1000) || (b.power || 0) - (a.power || 0));

        return playerList.slice(0, 50).map((p, i) => ({
            ...p,
            rank: i + 1,
            isNhanHoang: i === 0 && !p.isDemon,
            isThanhHoang: i === 0 && !p.isDemon,
            title: p.isThienMa ? '☯️ Thiên Ma' : i === 0 ? (p.isDemon ? '👿 Ma Tôn' : '👑 Nhân Hoàng') : i === 1 ? '🥈 Thiên Vương' : i === 2 ? '🥉 Địa Hoàng' : '',
        }));
    }

    pvpImmortalLeaderboard() {
        const now = this.now();
        const players = Object.values(this.data.players || {})
            .filter(p => p.registered && !p.isNpc && p.ascended && !this.isHiddenFromPlayers(p) && this.isPvpActive(p, now))
            .map(p => {
                const st = this.stats(p, now), realm = this.realmOf(p.userId);
                const sect = p.sectId && this.sects[p.sectId] ? this.sects[p.sectId].name : null;
                const isDemon = Boolean(p.isDemon) || ((p.maScore || 0) > 0 && (p.maScore || 0) > Math.max(0, p.daoScore ?? p.daoTam ?? 100));
                return {
                    userId: p.userId, name: p.name, fullName: p.fullName || p.name,
                    photoUrl: p.photoUrl || '', points: p.pvp?.points || 1000, power: st.power,
                    realmName: realm?.name || '', realm: realm?.name || '',
                    wins: p.pvp?.wins || 0, losses: p.pvp?.losses || 0, sectName: sect,
                    isDemon, isThienKieu: Boolean(p.isThienKieu), isThienMa: (p.maScore || 0) >= 1000,
                    isAscended: true, isNpc: false,
                };
            })
            .sort((a, b) => (b.points || 1000) - (a.points || 1000) || (b.power || 0) - (a.power || 0))
            .slice(0, 50);
        return players.map((p, i) => ({
            ...p, rank: i + 1,
            isDang: i === 0 && !p.isDemon,
            title: p.isThienMa ? '☯️ Thiên Ma' : i === 0 ? (p.isDemon ? '👿 Ma Tôn' : '✨ Đấng') : i === 1 ? '🥈 Tiên Vương' : i === 2 ? '🥉 Tiên Quân' : '',
        }));
    }

    pvpLeaderboard(limit = 20) {
        const now = this.now();
        const list = Object.entries(this.data.players || {})
            .filter(([, p]) => p.registered && !p.isNpc && !this.isHiddenFromPlayers(p) && this.isPvpActive(p, now))
            .map(([uid, p]) => {
                const r = this.realmOf(uid);
                const st = this.stats(p, now);
                const sect = p.sectId && this.sects[p.sectId] ? this.sects[p.sectId].name : null;
                return {
                    userId: uid,
                    name: p.name,
                    fullName: p.fullName || p.name,
                    photoUrl: p.photoUrl || '',
                    realmName: r.name,
                    power: st.power,
                    points: p.pvp?.points || 1000,
                    wins: p.pvp?.wins || 0,
                    losses: p.pvp?.losses || 0,
                    sectName: sect,
                    isNpc: false,
                };
            });
        list.sort((a, b) => b.points - a.points || b.power - a.power);
        return list.slice(0, limit);
    }

    // ---- Bạn bè, hồ sơ tu sĩ & gia đình -------------------------------------
    socialFamily(p) {
        p.friends = Array.isArray(p.friends) ? p.friends.map(String) : [];
        p.friendRequests = Array.isArray(p.friendRequests) ? p.friendRequests : [];
        p.friendSentRequests = Array.isArray(p.friendSentRequests) ? p.friendSentRequests.map(String) : [];
        p.familyRequests = Array.isArray(p.familyRequests) ? p.familyRequests : [];
        p.family = p.family && typeof p.family === 'object' ? p.family : {};
        p.family.parents = Array.isArray(p.family.parents) ? p.family.parents.map(String) : [];
        p.family.children = Array.isArray(p.family.children) ? p.family.children.map(String) : [];
        p.family.disciples = Array.isArray(p.family.disciples) ? p.family.disciples.map(String) : [];
        p.family.masterId = p.family.masterId ? String(p.family.masterId) : null;
        return p.family;
    }

    socialPlayerCard(userId, viewerId = null) {
        const other = this.player(userId);
        if (!other?.registered || (this.isHiddenFromPlayers(other) && String(viewerId) !== String(userId))) return null;
        const realm = this.realmOf(userId);
        const sect = other.sectId && this.sects[other.sectId] ? this.sects[other.sectId] : null;
        return {
            id: String(userId), name: other.name, fullName: other.fullName || other.name,
            photoUrl: other.photoUrl || '', gender: other.gender,
            realm: realm.name, realmIndex: realm.index,
            desc: `${realm.name} · ${C.MON[other.mon]?.name || 'Tu sĩ'}${sect ? ` · ${sect.name}` : ''}`,
            sectName: sect?.name || null, sectRole: other.sectRole || null,
            companion: Boolean(other.companion),
        };
    }

    socialView(userId) {
        const p = this.requirePlayer(userId);
        this.socialFamily(p);
        const friendIds = new Set(p.friends);
        const cardList = ids => ids.map(id => this.socialPlayerCard(id, userId)).filter(Boolean);
        const requests = p.friendRequests.map(r => ({ ...r, from: this.socialPlayerCard(r.fromId, userId) })).filter(r => r.from);
        const familyRequests = p.familyRequests.map(r => ({ ...r, from: this.socialPlayerCard(r.fromId, userId) })).filter(r => r.from);
        const family = p.family;
        return {
            ...this.companionView(userId),
            friends: cardList(p.friends),
            friendRequests: requests,
            friendSentRequests: p.friendSentRequests.map(id => this.socialPlayerCard(id, userId)).filter(Boolean),
            relationRequests: familyRequests,
            family: {
                parents: cardList(family.parents), children: cardList(family.children),
                master: family.masterId ? this.socialPlayerCard(family.masterId, userId) : null,
                disciples: cardList(family.disciples),
            },
            friendCount: friendIds.size,
        };
    }

    socialFriendRequest(userId, targetId) {
        const p = this.requirePlayer(userId);
        this.requireDiscoverablePlayer(userId);
        const targetKey = String(targetId || '');
        if (!/^\d{1,20}$/.test(targetKey)) fail('Hãy nhập Telegram ID hợp lệ.');
        if (targetKey === String(userId)) fail('Không thể tự kết bạn với chính mình.');
        const target = this.requireVisiblePlayer(userId, targetKey);
        if (!target.registered) fail('Tu sĩ này chưa khai mở hồ sơ trong game.');
        const family = this.socialFamily(p); this.socialFamily(target);
        if (p.friends.includes(targetKey)) fail('Hai vị đã là bạn bè.');
        p.friendSentRequests = p.friendSentRequests || [];
        target.friendRequests = target.friendRequests || [];
        if (p.friendSentRequests.includes(targetKey) || target.friendRequests.some(r => String(r.fromId) === String(userId))) fail('Lời mời kết bạn đang chờ hồi âm.');
        if (p.friendSentRequests.length >= 50 || target.friendRequests.length >= 100) fail('Danh sách lời mời đã đầy, hãy xử lý lời mời cũ trước.');
        const card = this.socialPlayerCard(userId, targetKey);
        target.friendRequests.push({ fromId: String(userId), fromName: p.name, fromFullName: p.fullName || p.name, at: this.now() });
        p.friendSentRequests.push(targetKey);
        this.touch();
        return { success: true, message: `Đã gửi lời mời kết bạn tới ${target.name}.` };
    }

    socialFriendAccept(userId, fromId) {
        const p = this.requirePlayer(userId), other = this.requireVisiblePlayer(userId, fromId);
        const fromKey = String(fromId);
        if (!(p.friendRequests || []).some(r => String(r.fromId) === fromKey)) fail('Không có lời mời kết bạn này.');
        const family = this.socialFamily(p); this.socialFamily(other);
        p.friends = [...new Set([...p.friends, fromKey])];
        other.friends = [...new Set([...(other.friends || []), String(userId)])];
        p.friendRequests = p.friendRequests.filter(r => String(r.fromId) !== fromKey);
        other.friendSentRequests = (other.friendSentRequests || []).filter(id => String(id) !== String(userId));
        this.touch();
        return { success: true, message: `Hai vị và ${other.name} đã trở thành bạn bè.` };
    }

    socialFriendReject(userId, fromId) {
        const p = this.requirePlayer(userId), fromKey = String(fromId);
        if (!(p.friendRequests || []).some(r => String(r.fromId) === fromKey)) fail('Không có lời mời kết bạn này.');
        p.friendRequests = p.friendRequests.filter(r => String(r.fromId) !== fromKey);
        const other = this.player(fromKey);
        if (other) other.friendSentRequests = (other.friendSentRequests || []).filter(id => String(id) !== String(userId));
        this.touch();
        return { success: true };
    }

    socialFriendCancel(userId, targetId) {
        const p = this.requirePlayer(userId), targetKey = String(targetId);
        if (!(p.friendSentRequests || []).includes(targetKey)) fail('Không có lời mời đang chờ tới tu sĩ này.');
        p.friendSentRequests = p.friendSentRequests.filter(id => String(id) !== targetKey);
        const target = this.player(targetKey);
        if (target) target.friendRequests = (target.friendRequests || []).filter(r => String(r.fromId) !== String(userId));
        this.touch();
        return { success: true };
    }

    socialFriendRemove(userId, targetId) {
        const p = this.requirePlayer(userId), targetKey = String(targetId);
        const target = this.player(targetKey);
        if (!(p.friends || []).includes(targetKey)) fail('Tu sĩ này chưa nằm trong danh sách bạn bè.');
        if (target) {
            const pf = this.socialFamily(p), tf = this.socialFamily(target);
            const linked = p.companion?.id === targetKey || target.companion?.id === String(userId)
                || pf.masterId === targetKey || tf.masterId === String(userId)
                || pf.parents.includes(targetKey) || pf.children.includes(targetKey) || pf.disciples.includes(targetKey)
                || tf.parents.includes(String(userId)) || tf.children.includes(String(userId)) || tf.disciples.includes(String(userId));
            if (linked) fail('Hai vị còn quan hệ đạo lữ/gia đình; hãy kết thúc quan hệ đó trước khi hủy kết bạn.');
        }
        p.friends = p.friends.filter(id => id !== targetKey);
        if (target) target.friends = (target.friends || []).filter(id => String(id) !== String(userId));
        this.touch();
        return { success: true };
    }

    socialFriendGiftItem(userId, targetId, itemUid, qty = 1) {
        const p = this.requirePlayer(userId);
        this.requireIdle(p);
        const fromKey = String(userId), targetKey = String(targetId || '');
        if (!/^\d{1,20}$/.test(targetKey) || targetKey === fromKey) fail('Người nhận không hợp lệ.');
        const target = this.requirePlayer(targetKey);
        this.requireIdle(target);
        this.socialFamily(p); this.socialFamily(target);
        if (!(p.friends || []).includes(targetKey) || !(target.friends || []).includes(fromKey)) {
            fail('Hai vị cần đang là bạn bè mới có thể tặng vật phẩm.');
        }

        const item = this.findItem(p, itemUid);
        if (!['bag', 'kho'].includes(item.place)) fail('Chỉ có thể tặng vật phẩm trong túi hoặc kho thường.');
        const def = itemDef(item);
        if (item.bound || def?.bound) fail('Vật phẩm này bị khóa, không thể đem tặng.');
        const available = Math.max(1, Math.floor(Number(item.qty) || 1));
        const amount = Math.floor(Number(qty) || 0);
        if (amount < 1 || amount > available || amount > C.RULES.stackMax) fail(`Số lượng tặng phải từ 1 đến ${Math.min(available, C.RULES.stackMax)}.`);
        if ((item.unique || def?.unique || item.kind === 'equip') && amount !== 1) fail('Vật phẩm độc bản chỉ có thể tặng từng món.');

        const name = itemName(item);
        const quantityText = amount > 1 ? ` ×${amount}` : '';
        if (amount < available) item.qty = available - amount;
        else {
            p.items = p.items.filter(entry => entry !== item);
            p.quick = (p.quick || []).map(uid => uid === item.uid ? null : uid);
        }

        const mailItem = {
            id: item.id,
            name,
            qty: amount,
            icon: def?.icon || item.icon || '📦',
            kind: item.kind,
            ...(item.kind === 'equip' || item.unique || def?.unique ? { itemData: { ...item, qty: item.kind === 'equip' ? 1 : amount } } : {}),
        };
        this.sendMail(targetKey, {
            title: `Quà tặng từ ${p.name}`,
            sender: p.name,
            content: `${p.name} đã gửi tặng bạn ${name}${quantityText}. Mở thư và nhấn Nhận để lấy quà.`,
            items: [mailItem],
        });

        const senderText = `🎁 [TẶNG QUÀ] Bạn đã tặng ${name}${quantityText} cho ${target.name}.`;
        const targetText = `🎁 [QUÀ TẶNG] ${p.name} đã tặng bạn ${name}${quantityText} qua Hòm Thư.`;
        this.recordPersonalEvent(fromKey, senderText);
        this.recordPersonalEvent(targetKey, targetText);
        this.touch();
        return { success: true, name, qty: amount, place: 'inbox', targetName: target.name, message: senderText };
    }

    socialProfileView(viewerId, targetId) {
        const viewer = this.requirePlayer(viewerId), target = this.requireVisiblePlayer(viewerId, targetId);
        const vf = this.socialFamily(viewer), tf = this.socialFamily(target);
        if (String(viewerId) !== String(targetId) && !(viewer.friends || []).includes(String(targetId))) fail('Chỉ có thể xem hồ sơ chi tiết của chính mình hoặc bạn bè.');
        const realm = this.realmOf(targetId), stats = this.stats(target, this.now());
        const equipment = Object.fromEntries(Object.entries(target.equip || {}).map(([slot, uid]) => {
            const item = (target.items || []).find(it => it.uid === uid);
            const def = itemDef(item);
            return [slot, def ? { name: def.name, tierName: item.tierName || def.tierName || '', icon: def.icon || '', dur: item.dur ?? 100 } : null];
        }));
        const card = this.socialPlayerCard(targetId, viewerId);
        const names = ids => ids.map(id => this.socialPlayerCard(id, viewerId)).filter(Boolean);
        return {
            profile: {
                ...card, daoScore: target.daoScore ?? target.daoTam ?? 100, maScore: target.maScore || 0,
                isDemon: Boolean(target.isDemon) || ((target.maScore || 0) > 0 && (target.maScore || 0) > Math.max(0, target.daoScore ?? target.daoTam ?? 100)),
                isThienMa: (target.maScore || 0) >= 1000,
                moralState: this.moralityState({ ...target, isDemon: Boolean(target.isDemon) || ((target.maScore || 0) > 0 && (target.maScore || 0) > Math.max(0, target.daoScore ?? target.daoTam ?? 100)) }),
                linhCan: this.linhCan(target)?.name || 'Chưa rõ', role: C.MON[target.mon]?.name || 'Tu sĩ',
                stats: { hp: stats.hp, mp: stats.mp, atk: stats.atk, def: stats.def, spd: stats.spd, sense: stats.sense, critRate: stats.critRate, critDmg: stats.critDmg, accuracy: stats.accuracy, dodge: stats.dodge, dmgReduction: stats.dmgReduction, power: stats.power },
                equipment,
                companion: target.companion && !target.companion.isNpc ? this.socialPlayerCard(target.companion.id, viewerId) : (target.companion ? { name: target.companion.name, isNpc: true } : null),
                family: {
                    parents: names(tf.parents), children: names(tf.children),
                    master: tf.masterId ? this.socialPlayerCard(tf.masterId, viewerId) : null, disciples: names(tf.disciples),
                },
            },
        };
    }

    socialRelationRequest(userId, targetId, kind) {
        const p = this.requirePlayer(userId), target = this.requireVisiblePlayer(userId, targetId);
        this.requireDiscoverablePlayer(userId);
        const fromId = String(userId), targetKey = String(targetId);
        if (fromId === targetKey) fail('Không thể tạo quan hệ với chính mình.');
        if (!(p.friends || []).includes(targetKey) || !(target.friends || []).includes(fromId)) fail('Hai vị cần kết bạn trước.');
        if (!['parent', 'child', 'master', 'disciple'].includes(kind)) fail('Loại quan hệ không hợp lệ.');
        if (p.familyRequests?.some(r => String(r.fromId) === fromId && r.kind === kind)
            || target.familyRequests?.some(r => String(r.fromId) === fromId && r.kind === kind)) fail('Lời thỉnh cầu quan hệ này đang chờ hồi âm.');
        const pf = this.socialFamily(p), tf = this.socialFamily(target);
        if ((kind === 'parent' && pf.parents.length >= 2) || (kind === 'child' && tf.parents.length >= 2)) {
            fail('Mỗi tu sĩ chỉ có thể có tối đa hai vị song thân.');
        }
        if (kind === 'master' && pf.masterId) fail('Bạn đã có sư phụ.');
        if (kind === 'disciple' && tf.masterId) fail('Người này đã có sư phụ.');
        const descendants = (startId, field) => {
            const seen = new Set(), stack = [String(startId)];
            while (stack.length) {
                const id = stack.pop();
                if (seen.has(id)) continue;
                seen.add(id);
                const person = this.player(id);
                for (const child of person ? (this.socialFamily(person)[field] || []) : []) stack.push(String(child));
            }
            return seen;
        };
        if (kind === 'parent' && descendants(fromId, 'children').has(targetKey)) fail('Quan hệ này sẽ tạo vòng lặp trong gia hệ.');
        if (kind === 'child' && descendants(targetKey, 'children').has(fromId)) fail('Quan hệ này sẽ tạo vòng lặp trong gia hệ.');
        if (kind === 'master' && descendants(fromId, 'disciples').has(targetKey)) fail('Không thể nhận hậu bối trong dòng đệ tử làm sư phụ.');
        if (kind === 'disciple') {
            let id = fromId, seen = new Set();
            while (id && !seen.has(id)) {
                if (id === targetKey) fail('Không thể nhận sư tổ làm đệ tử.');
                seen.add(id); id = this.socialFamily(this.player(id) || {}).masterId;
            }
        }
        target.familyRequests = target.familyRequests || [];
        if (target.familyRequests.length >= 100) fail('Hòm thư quan hệ của đối phương đã đầy.');
        const labels = { parent: 'xin được làm con của', child: 'xin nhận làm con cái', master: 'xin bái sư', disciple: 'xin thu làm đệ tử' };
        target.familyRequests.push({ fromId, kind, at: this.now() });
        this.touch();
        return { success: true, message: `Đã gửi lời ${labels[kind]} tới ${target.name}.` };
    }

    socialRelationAccept(userId, fromId, kind) {
        const p = this.requirePlayer(userId), from = this.requireVisiblePlayer(userId, fromId);
        const fromKey = String(fromId), uid = String(userId);
        const request = (p.familyRequests || []).find(r => String(r.fromId) === fromKey && (!kind || r.kind === kind));
        if (!request) fail('Không tìm thấy lời thỉnh cầu quan hệ này.');
        const type = request.kind, pf = this.socialFamily(p), ff = this.socialFamily(from);
        if (!(p.friends || []).includes(fromKey) || !(from.friends || []).includes(uid)) fail('Hai vị cần còn là bạn bè khi xác nhận quan hệ.');
        if (type === 'parent') {
            if (ff.parents.length >= 2) fail('Người thỉnh cầu đã có đủ hai vị song thân.');
            const seen = new Set([fromKey]), stack = [fromKey];
            while (stack.length) { const id = stack.pop(); const person = this.player(id); for (const c of person ? this.socialFamily(person).children : []) if (!seen.has(c)) { seen.add(c); stack.push(c); } }
            if (seen.has(uid)) fail('Quan hệ này sẽ tạo vòng lặp trong gia hệ.');
            pf.children = [...new Set([...pf.children, fromKey])]; ff.parents = [...new Set([...ff.parents, uid])];
        } else if (type === 'child') {
            if (pf.parents.length >= 2) fail('Người này đã có đủ hai vị song thân.');
            const seen = new Set([uid]), stack = [uid];
            while (stack.length) { const id = stack.pop(); const person = this.player(id); for (const c of person ? this.socialFamily(person).children : []) if (!seen.has(c)) { seen.add(c); stack.push(c); } }
            if (seen.has(fromKey)) fail('Quan hệ này sẽ tạo vòng lặp trong gia hệ.');
            ff.children = [...new Set([...ff.children, uid])]; pf.parents = [...new Set([...pf.parents, fromKey])];
        } else if (type === 'master') {
            if (ff.masterId) fail('Người thỉnh cầu đã có sư phụ.');
            let id = uid, seen = new Set();
            while (id && !seen.has(id)) { if (id === fromKey) fail('Quan hệ này sẽ tạo vòng lặp sư môn.'); seen.add(id); const person = this.player(id); id = person ? this.socialFamily(person).masterId : null; }
            ff.masterId = uid; pf.disciples = [...new Set([...pf.disciples, fromKey])];
        } else if (type === 'disciple') {
            if (pf.masterId) fail('Bạn đã có sư phụ.');
            let id = fromKey, seen = new Set();
            while (id && !seen.has(id)) { if (id === uid) fail('Quan hệ này sẽ tạo vòng lặp sư môn.'); seen.add(id); const person = this.player(id); id = person ? this.socialFamily(person).masterId : null; }
            pf.masterId = fromKey; ff.disciples = [...new Set([...ff.disciples, uid])];
        }
        p.familyRequests = p.familyRequests.filter(r => !(String(r.fromId) === fromKey && r.kind === type));
        this.touch();
        return { success: true };
    }

    socialRelationReject(userId, fromId, kind) {
        const p = this.requirePlayer(userId), fromKey = String(fromId);
        const before = (p.familyRequests || []).length;
        p.familyRequests = p.familyRequests.filter(r => !(String(r.fromId) === fromKey && (!kind || r.kind === kind)));
        if (before === p.familyRequests.length) fail('Không tìm thấy lời thỉnh cầu quan hệ này.');
        this.touch();
        return { success: true };
    }

    socialRelationBreak(userId, targetId, kind) {
        const p = this.requirePlayer(userId), uid = String(userId), targetKey = String(targetId || '');
        if (!/^\d{1,20}$/.test(targetKey) || targetKey === uid) fail('Đạo hữu không hợp lệ.');
        if (!['parent', 'child', 'master', 'disciple'].includes(kind)) fail('Loại quan hệ không hợp lệ.');

        const own = this.socialFamily(p);
        const otherPlayer = this.player(targetKey);
        const other = otherPlayer ? this.socialFamily(otherPlayer) : null;
        let linked = false;
        if (kind === 'parent') {
            linked = own.parents.includes(targetKey) || Boolean(other?.children.includes(uid));
            own.parents = own.parents.filter(id => id !== targetKey);
            if (other) other.children = other.children.filter(id => id !== uid);
        } else if (kind === 'child') {
            linked = own.children.includes(targetKey) || Boolean(other?.parents.includes(uid));
            own.children = own.children.filter(id => id !== targetKey);
            if (other) other.parents = other.parents.filter(id => id !== uid);
        } else if (kind === 'master') {
            linked = own.masterId === targetKey || Boolean(other?.disciples.includes(uid));
            if (own.masterId === targetKey) own.masterId = null;
            if (other) other.disciples = other.disciples.filter(id => id !== uid);
        } else {
            linked = own.disciples.includes(targetKey) || other?.masterId === uid;
            own.disciples = own.disciples.filter(id => id !== targetKey);
            if (other?.masterId === uid) other.masterId = null;
        }
        if (!linked) fail('Quan hệ này không còn tồn tại.');
        this.touch();
        return { success: true, message: 'Đã cắt đứt quan hệ ở cả hai phía.' };
    }

    // ---- Đạo Lữ & Song Tu ----------------------------------------------------
    get npcs() {
        return [
            { id: 'npc_tuyet_co', name: 'Băng Dao Tiên Tử', gender: 'nu', isNpc: true, desc: 'Thần nữ Tuyết Sơn, thanh tao thoát tục, lạnh lùng nhưng son sắt.' },
            { id: 'npc_van_phieu', name: 'Vân Mộng Đạo Quân', gender: 'nam', isNpc: true, desc: 'Kiếm tu phong hoa tuyệt đại, tiêu sái tự tại khắp cõi cửu châu.' },
            { id: 'npc_thanh_ha', name: 'Thanh Hà Thần Nữ', gender: 'nu', isNpc: true, desc: 'Dược thánh đệ tử, dịu dàng thanh thuần, am hiểu đan dược linh thảo.' },
        ];
    }

    companionView(userId) {
        const p = this.requirePlayer(userId);
        this.socialFamily(p);
        const now = this.now();
        const today = vnDate(now);
        const companionVisible = p.companion && (p.companion.isNpc || this.canSeePlayer(userId, p.companion.id));
        const comp = p.companion ? {
            ...p.companion,
            ...(companionVisible ? {} : { id: null, name: 'Đạo lữ ẩn danh' }),
            songTuToday: p.companion.lastSongTuDate === today ? (p.companion.songTuToday || 0) : 0,
            isProtected: Boolean(p.companion.protectedUntil && p.companion.protectedUntil > now),
            protectLeftSec: Math.max(0, Math.ceil(((p.companion.protectedUntil || 0) - now) / 1000)),
        } : null;

        const candidates = [];
        for (const [uid, other] of Object.entries(this.data.players || {})) {
            if (uid === String(userId) || !other.registered || this.isHiddenFromPlayers(other) || other.companion || other.gender !== 'nu' || !p.friends.includes(uid)) continue;
            const r = this.realmOf(uid);
            candidates.push({
                id: uid,
                name: other.name,
                fullName: other.fullName || other.name,
                photoUrl: other.photoUrl || '',
                gender: other.gender,
                desc: `${r.name} · ${C.MON[other.mon]?.name || ''}`,
                isNpc: false,
                power: this.stats(other, now).power,
            });
        }

        return {
            companion: comp,
            proposals: (p.marriageProposals || []).filter(proposal => this.canSeePlayer(userId, proposal.fromId)).slice(-5),
            candidates: candidates.slice(0, 10),
            stones: p.stones,
            songTuBuff: Boolean(p.songTuBuffUntil && p.songTuBuffUntil > now),
        };
    }

    companionPropose(userId, targetId) {
        const p = this.requirePlayer(userId);
        if (p.companion) fail('Đạo hữu đã có đạo lữ, chớ nên tham lam trăng hoa.');
        if (p.stones < C.RULES.proposeCost) fail(`Cần ${C.RULES.proposeCost} linh thạch để chuẩn bị sính lễ cầu duyên.`);
        const now = this.now();

        const npc = this.npcs.find(n => n.id === targetId);
        if (npc) {
            if (npc.gender !== 'nu') fail('Chỉ có thể kết duyên với nữ tu sĩ. Đạo hữu hãy tìm kiếm đạo lữ khác.');
            p.stones -= C.RULES.proposeCost;
            p.companion = {
                id: npc.id,
                name: npc.name,
                gender: npc.gender,
                isNpc: true,
                intimacy: 30,
                marriedAt: new Date().toISOString(),
                songTuToday: 0,
                lastSongTuDate: '',
                protectedUntil: now + 12 * 3600 * 1000,
            };
            this.touch();
            return { success: true, companion: p.companion, message: `Chúc mừng! Đạo hữu đã kết thành duyên phận cùng ${npc.name}!` };
        }

        this.requireDiscoverablePlayer(userId);
        const target = this.requireVisiblePlayer(userId, targetId);
        if (target.companion) fail('Đối phương đã có đạo lữ.');
        if (target.gender !== 'nu') fail('Chỉ có thể kết duyên với nữ tu sĩ. Đối phương không phải nhân vật nữ.');
        if (!(p.friends || []).includes(String(targetId)) || !(target.friends || []).includes(String(userId))) fail('Hai vị cần kết bạn trước khi cầu duyên.');
        p.stones -= C.RULES.proposeCost;
        target.marriageProposals = target.marriageProposals || [];
        if (!target.marriageProposals.some(pr => pr.fromId === String(userId))) {
            const r = this.realmOf(userId);
            target.marriageProposals.push({
                fromId: String(userId),
                fromName: p.name,
                fromRealm: r.name,
                at: now,
            });
        }
        this.touch();
        return { success: true, message: `Đã gửi lời cầu duyên và ${C.RULES.proposeCost} linh thạch sính lễ tới ${target.name}.` };
    }

    companionAccept(userId, fromId) {
        const p = this.requirePlayer(userId);
        if (p.companion) fail('Đạo hữu đã có đạo lữ.');
        const suitor = this.requireVisiblePlayer(userId, fromId);
        if (suitor.companion) fail('Người này đã kết duyên với người khác rồi.');
        if (!(p.friends || []).includes(String(fromId)) || !(suitor.friends || []).includes(String(userId))) fail('Hai vị cần kết bạn trước khi xác nhận cầu duyên.');
        const now = this.now();
        const compData = {
            intimacy: 35,
            marriedAt: new Date().toISOString(),
            songTuToday: 0,
            lastSongTuDate: '',
            protectedUntil: now + 12 * 3600 * 1000,
        };
        p.companion = { id: String(fromId), name: suitor.name, gender: suitor.gender, isNpc: false, ...compData };
        suitor.companion = { id: String(userId), name: p.name, gender: p.gender, isNpc: false, ...compData };
        p.marriageProposals = (p.marriageProposals || []).filter(pr => pr.fromId !== String(fromId));
        this.touch();
        return { success: true, companion: p.companion };
    }

    companionReject(userId, fromId) {
        const p = this.requirePlayer(userId);
        p.marriageProposals = (p.marriageProposals || []).filter(pr => pr.fromId !== String(fromId));
        const suitor = this.player(fromId);
        if (suitor) suitor.stones += 50;
        this.touch();
        return { success: true };
    }

    companionDivorce(userId) {
        const p = this.requirePlayer(userId);
        if (!p.companion) fail('Đạo hữu chưa có đạo lữ.');
        const comp = p.companion;
        if (!comp.isNpc) {
            const partner = this.player(comp.id);
            if (partner && partner.companion?.id === String(userId)) {
                partner.companion = null;
            }
        }
        p.companion = null;
        this.touch();
        return { success: true };
    }

    companionGift(userId, giftType) {
        const p = this.requirePlayer(userId);
        if (!p.companion) fail('Đạo hữu chưa có đạo lữ để tặng quà.');
        const gifts = {
            hoa: { name: 'Bỉ Ngạn Hoa', cost: 50, intimacy: 15 },
            ngoc: { name: 'Tùy Hầu Châu', cost: 150, intimacy: 50 },
            dan: { name: 'Định Tình Đan', cost: 300, intimacy: 120 },
        };
        const g = gifts[giftType] || gifts.hoa;
        if (p.stones < g.cost) fail(`Cần ${g.cost} linh thạch để mua ${g.name}.`);
        p.stones -= g.cost;
        p.companion.intimacy = (p.companion.intimacy || 0) + g.intimacy;
        if (!p.companion.isNpc) {
            const partner = this.player(p.companion.id);
            if (partner && partner.companion) partner.companion.intimacy = p.companion.intimacy;
        }
        this.touch();
        return { gift: g.name, newIntimacy: p.companion.intimacy, stones: p.stones };
    }

    companionSongTu(userId) {
        const p = this.requirePlayer(userId);
        if (!p.companion) fail('Cần có đạo lữ mới có thể song tu.');
        const now = this.now();
        const today = vnDate(now);
        p.companion.songTuToday = p.companion.lastSongTuDate === today ? (p.companion.songTuToday || 0) : 0;
        if (p.companion.songTuToday >= 2) fail('Hôm nay đã song tu đủ 2 lần, kinh mạch cần nghỉ ngơi.');

        // Âm/Dương Khí check
        const qiCost = C.RULES.songTuQiCost || 25;
        if (p.gender === 'nam') {
            if ((p.duongKhi || 0) < qiCost) fail(`Dương Khí không đủ (cần ${qiCost}, hiện có ${p.duongKhi || 0}). Uống Bổ Dương Đan để hồi phục.`);
            p.duongKhi = (p.duongKhi || 0) - qiCost;
        } else {
            if ((p.amKhi || 0) < qiCost) fail(`Âm Khí không đủ (cần ${qiCost}, hiện có ${p.amKhi || 0}). Uống Dưỡng Âm Đan để hồi phục.`);
            p.amKhi = (p.amKhi || 0) - qiCost;
        }

        p.companion.songTuToday += 1;
        p.companion.lastSongTuDate = today;
        p.companion.intimacy = (p.companion.intimacy || 0) + 10;
        this.syncStamina(p, now);
        p.stamina = Math.min(this.maxStamina(p), p.stamina + 25);
        p.songTuBuffUntil = now + 4 * 3600 * 1000;

        const rInfo = this.realmOf(userId);
        const expGain = Math.round(rInfo.levelCap * 0.04 + 300);
        const rResult = this.realms.addExp(userId, expGain);

        if (!p.companion.isNpc) {
            const partner = this.player(p.companion.id);
            if (partner && partner.companion) {
                partner.companion.songTuToday = p.companion.songTuToday;
                partner.companion.lastSongTuDate = today;
                partner.companion.intimacy = p.companion.intimacy;
                this.syncStamina(partner, now);
                partner.stamina = Math.min(this.maxStamina(partner), partner.stamina + 25);
                partner.songTuBuffUntil = now + 4 * 3600 * 1000;
                this.realms.addExp(p.companion.id, expGain);
            }
        }
        this.touch();
        return {
            expGain,
            staminaGain: 25,
            intimacyGain: 10,
            songTuToday: p.companion.songTuToday,
            levelUps: rResult?.levelUps || 0,
        };
    }

    // ---- Cướp Đạo Lữ ---------------------------------------------------------
    companionRob(userId, targetUserId) {
        // Keep the old entry point for legacy callers, but route it through
        // the same interactive PvP flow as the current API.
        return this.startCompanionRobBattle(userId, targetUserId);
    }

    // ---- Đồng bộ thông tin Telegram (Họ tên đầy đủ và avatar) ---------------
    syncUserInfo(user) {
        if (!user?.id) return;
        const p = this.player(user.id);
        if (!p) return;
        const fullName = [user.first_name, user.last_name].filter(Boolean).join(' ').trim();
        if (fullName) {
            p.fullName = fullName;
            p.name = fullName.slice(0, 40);
        } else if (user.username) {
            p.name = user.username;
        }
        if (user.username) p.username = user.username;
        if (user.photo_url) p.photoUrl = user.photo_url;
    }

    // ---- Y Quán & Trị liệu ---------------------------------------------------
    townHeal(userId) {
        const p = this.requirePlayer(userId);
        const now = this.now();
        p.town = p.town || 'thanh_van';
        this.checkTravelArrival(p, now);
        if (p.traveling) fail('Đang ngự kiếm phi hành, chưa tới nơi.');
        const town = C.TOWN_BY_ID.get(p.town) || C.TOWNS[0];
        const s = this.stats(p, now);
        const missingHp = Math.max(0, s.hp - (p.hp == null ? s.hp : p.hp));
        const immortalTown = Boolean(C.MAP_BY_ID.get(town.mapId)?.ascensionRequired);
        const cost = townHealingCost(missingHp, s.hp, immortalTown);
        if (p.hp != null && p.hp >= s.hp && (p.injuredUntil || 0) <= now) {
            fail('Khí huyết đã tràn đầy, không có thương tích.');
        }
        if (p.stones < cost) fail(`Cần ${cost} linh thạch để đại phu Y Quán trị thương.`);
        p.stones -= cost;
        p.hp = s.hp;
        p.hpAt = now;
        p.injuredUntil = 0;
        this.touch();
        return { cost, hp: p.hp, maxHp: s.hp, stones: p.stones };
    }

    healPotion(userId, itemUid) {
        const p = this.requirePlayer(userId);
        const now = this.now();
        const item = p.items.find(it => it.uid === itemUid && it.place === 'bag' && it.kind === 'cons');
        if (!item) fail('Không tìm thấy đan dược trong hành trang.');
        const def = C.CONSUMABLE_BY_ID.get(item.id);
        if (!def || !def.heal) fail('Vật phẩm này không có tác dụng hồi phục khí huyết.');
        const s = this.stats(p, now);
        if (p.hp != null && p.hp >= s.hp) fail('Khí huyết đã tràn đầy.');
        const healAmt = Math.round(s.hp * def.heal);
        p.hp = Math.min(s.hp, (p.hp == null ? s.hp : p.hp) + healAmt);
        if (p.hp >= s.hp) p.hpAt = now;
        this.consume(p, item, 1);
        this.touch();
        return { itemName: def.name, healed: healAmt, healPct: Math.round(def.heal * 100), hp: p.hp, maxHp: s.hp };
    }

    locateMonsterAndDungeon(p, now = this.now()) {
        this.ensureWorldMonsters(now);
        this.cultivateWorldMonsters(now);
        this.cultivateDungeonMonsters(now);

        const placeName = townId => {
            const town = C.TOWN_BY_ID.get(townId);
            if (!town) return townId || 'chưa rõ tọa độ';
            const map = C.MAP_BY_ID.get(town.mapId);
            return `${map?.name ? `${map.name} · ` : ''}${town.name}`;
        };
        const worldMonsters = (this.data.worldMonsters || []).filter(monster =>
            (monster.spawnAt || 0) <= now && (Number(monster.hp) || 0) > 0 && C.TOWN_BY_ID.has(monster.townId)
        );
        const entries = [];
        for (const target of worldMonsters) {
            const def = C.MONSTER_BY_ID.get(target.monsterId);
            const isWorldBoss = this.isWorldBossInstance(target);
            const isSmall = Boolean(def ? def.small : (target.isSmall ?? !target.isBoss));
            const category = isWorldBoss ? 'worldBoss' : isSmall ? 'small' : 'boss';
            const kind = isWorldBoss ? 'Boss thế giới' : isSmall ? 'Tiểu yêu' : 'Đại boss';
            const busy = target.lockedBy && target.lockedUntil > now ? ` · đang giao chiến với ${target.lockedByName || 'đạo hữu khác'}` : '';
            entries.push({
                type: 'world', category, uid: String(target.uid), monsterId: target.monsterId, isWorldBoss, icon: target.icon || def?.icon || '👹', kind, name: target.name,
                place: placeName(target.townId), townId: target.townId,
                realmName: this.realmName(target.realm || def?.realm || 0),
                hp: Number(target.hp || 0), maxHp: Number(target.maxHp || target.hp || 0), busy,
            });
        }

        const dungeons = (C.DUNGEONS || []).filter(dungeon => {
            const state = this.data.dungeonsState?.[dungeon.id] || {};
            return !state.respawnAt || state.respawnAt <= now;
        });
        for (const dungeon of dungeons) {
            const townId = dungeon.townId || p.town || 'thanh_van';
            const state = this.data.dungeonsState?.[dungeon.id] || {};
            const stages = dungeon.stages || [];
            const stageTargets = stages.map((stage, index) => {
                const stageState = state.stagesState?.[index] || {};
                const def = C.MONSTER_BY_ID.get(stageState.monsterId || stage?.monsterId);
                return {
                    stageNumber: index + 1,
                    stageName: stage?.name || `Ải ${index + 1}`,
                    monsterName: stageState.monsterName || def?.name || stage?.name || 'Yêu thú',
                };
            });
            const firstName = stageTargets[0]?.monsterName || 'Yêu thú';
            const lastName = stageTargets[stageTargets.length - 1]?.monsterName || 'Thủ lĩnh';
            const busy = state.fightingBy?.until > now ? ` · đang được ${state.fightingBy.name || 'đạo hữu khác'} thám hiểm` : '';
            entries.push({
                type: 'dungeon', category: 'dungeon', icon: dungeon.icon || '🏔️', name: dungeon.name,
                place: placeName(townId), townId,
                realmMinName: this.realmName(dungeon.realmMin || 0), stageCount: stages.length,
                firstName, lastName, stageTargets, busy,
            });
        }

        const players = Object.entries(this.data.players || {})
            .filter(([playerId, other]) => playerId !== String(p.userId) && other.registered && !this.isHiddenFromPlayers(other))
            .map(([playerId, other]) => {
                const realm = this.realmOf(playerId);
                const playerStats = this.stats(other, now);
                const townId = other.town || 'thanh_van';
                const town = C.TOWN_BY_ID.get(townId) || C.TOWNS[0];
                const sectName = other.sectId && this.sects[other.sectId] ? this.sects[other.sectId].name : null;
                return {
                    type: 'player', userId: String(playerId), name: other.name,
                    fullName: other.fullName || other.name, photoUrl: other.photoUrl || '',
                    realmName: realm.name, power: playerStats.power,
                    points: other.pvp?.points || 1000, sectName,
                    townId, townName: town.name,
                    isSameTown: townId === (p.town || 'thanh_van'),
                };
            })
            .sort((a, b) => a.fullName.localeCompare(b.fullName, 'vi'));

        if (!entries.length && !players.length) fail('Thiên Cơ Truy Tung Phù không dò thấy yêu thú, Cổ Động hay người chơi nào. Hãy thử lại sau.');
        return { entries, players, currentTownId: p.town || 'thanh_van' };
    }

    // ---- Bản đồ Thành Trấn & Ngự kiếm phi hành ------------------------------
    checkTravelArrival(p, now = this.now()) {
        if (!p.traveling) return;
        if (now >= p.traveling.arriveAt) {
            p.town = p.traveling.to;
            const target = C.TOWN_BY_ID.get(p.town);
            if (target && target.mapId) p.mapId = target.mapId;
            p.worldPosition = null;
            p.traveling = null;
            p.notices = (p.notices || []).concat([`Đã ngự kiếm phi hành tới ${target?.name || 'thành trấn'} an toàn.`]);
            this.touch();
        }
    }

    requireSameWorldDestination(p, target) {
        const currentTown = C.TOWN_BY_ID.get(p.town || 'thanh_van');
        const currentWorld = C.MAP_BY_ID.get(currentTown?.mapId)?.worldId;
        const targetWorld = C.MAP_BY_ID.get(target?.mapId)?.worldId;
        if (currentWorld && targetWorld && currentWorld !== targetWorld)
            fail('Chỉ có thể đổi giữa Phàm Giới và Tiên Giới qua Cổng Phi Thăng riêng.');
    }

    provinceAtWorldPosition(worldId, x, y) {
        const columns = worldId === 'world_tien' ? 4 : 3;
        const maps = C.MAPS.filter(map => map.worldId === worldId);
        const column = Math.floor(Number(x) / 256);
        const row = Math.floor(Number(y) / 160);
        if (column < 0 || column >= columns || row < 0 || row >= 3) return null;
        return maps[row * columns + column] || null;
    }

    travel(userId, toTownId) {
        const p = this.requirePlayer(userId);
        const now = this.now();
        this.checkTravelArrival(p, now);
        if (p.traveling) {
            const leftSec = Math.ceil((p.traveling.arriveAt - now) / 1000);
            const target = C.TOWN_BY_ID.get(p.traveling.to);
            fail(`Đang ngự kiếm đến ${target?.name || 'thành trấn'}, còn ${Math.ceil(leftSec / 60)} phút.`);
        }
        p.town = p.town || 'thanh_van';
        if (p.town === toTownId) fail('Đạo hữu đang ở thành trấn này rồi.');
        const target = C.TOWN_BY_ID.get(toTownId);
        if (!target) fail('Không tìm thấy thành trấn này.');
        this.requireSameWorldDestination(p, target);
        const targetMap = C.MAP_BY_ID.get(target.mapId);
        if (targetMap && targetMap.ascensionRequired && !p.ascended) {
            fail(`Cửu Thiên Tiên Giới yêu cầu tu sĩ phải đạt cảnh giới Phi Thăng và dâng Thiên Đạo Nguyên Ấn (tối thiểu ${this.realmName(11)} trở lên) mới có thể bước vào!`);
        }
        if (this.activeBattle(userId)) fail('Đang trong trận chiến, không thể ngự kiếm phi hành.');

        let baseSec = C.getTravelSec(p.town, toTownId);
        let mountSpeed = 0;
        let mountName = '';
        if (p.equip?.phiKiem) {
            const beast = (p.beasts || []).find(candidate => candidate.id === p.equip.phiKiem);
            if (beast) {
                mountSpeed = Math.min(0.4, 0.12 + (beast.realm || 0) * 0.01);
                mountName = beast.name;
            } else {
                const mountItem = p.items.find(it => it.uid === p.equip.phiKiem);
                const mountDef = mountItem ? itemDef(mountItem) : null;
                if (mountDef?.flySpeed) { mountSpeed = mountDef.flySpeed; mountName = mountDef.name; }
            }
        }
        const travelSec = Math.max(10, Math.round(baseSec * (1 - Math.min(0.9, mountSpeed))));
        p.traveling = {
            from: p.town,
            to: toTownId,
            startAt: now,
            arriveAt: now + travelSec * 1000,
            durationSec: travelSec,
            mountName,
            mountSpeedBonus: Math.round(mountSpeed * 100),
            flySpeedBonus: Math.round(mountSpeed * 100),
        };
        this.touch();
        return {
            fromTownName: (C.TOWN_BY_ID.get(p.town) || C.TOWNS[0]).name,
            toTownName: target.name,
            durationSec: travelSec,
            arriveAt: p.traveling.arriveAt,
            mountName: p.traveling.mountName || p.traveling.flySwordName || null,
            mountSpeedBonus: p.traveling.mountSpeedBonus || p.traveling.flySpeedBonus || 0,
            flySpeedBonus: p.traveling.mountSpeedBonus || p.traveling.flySpeedBonus || 0,
        };
    }

    moveWorldPosition(userId, choice = {}) {
        const p = this.requirePlayer(userId);
        const now = this.now();
        this.checkTravelArrival(p, now);
        if (p.traveling) fail('Đang ngự kiếm phi hành, chưa thể đi bộ trên bản đồ.');
        if (this.activeBattle(userId)) fail('Đang trong trận chiến, chưa thể di chuyển trên bản đồ.');

        const town = C.TOWN_BY_ID.get(p.town || 'thanh_van');
        const mapId = String(choice.mapId || '');
        const x = Number(choice.x);
        const y = Number(choice.y);
        if (!town || mapId !== town.mapId) fail('Bản đồ di chuyển không khớp với thành trấn hiện tại.');
        const worldId = C.MAP_BY_ID.get(mapId)?.worldId;
        const worldColumns = worldId === 'world_tien' ? 4 : 3;
        const worldRows = 3;
        if (!worldId
            || !Number.isInteger(x) || x < 0 || x >= worldColumns * 256
            || !Number.isInteger(y) || y < 0 || y >= worldRows * 160) {
            fail('Tọa độ bản đồ không hợp lệ.');
        }

        p.worldPosition = { mapId, x, y, updatedAt: now };
        this.touch();
        return { mapId, x, y };
    }

    /** Walking into a city in the current province makes it the current town. */
    enterTownOnFoot(userId, townId) {
        const p = this.requirePlayer(userId);
        const now = this.now();
        this.checkTravelArrival(p, now);
        if (p.traveling) fail('Đang ngự kiếm phi hành, chưa thể vào thành.');
        if (this.activeBattle(userId)) fail('Đang trong trận chiến, chưa thể vào thành.');
        const current = C.TOWN_BY_ID.get(p.town || 'thanh_van');
        const target = C.TOWN_BY_ID.get(String(townId || ''));
        if (!target) fail('Không tìm thấy thành trấn này.');
        if (!current) fail('Không tìm thấy thành trấn hiện tại.');
        const currentMap = C.MAP_BY_ID.get(current.mapId);
        const targetMap = C.MAP_BY_ID.get(target.mapId);
        if (!currentMap || !targetMap || currentMap.worldId !== targetMap.worldId)
            fail('Chỉ có thể đi bộ giữa các thành cùng một thế giới.');
        if (targetMap.worldId === 'world_tien' && !p.ascended) fail('Cần hoàn thành nghi thức Phi Thăng trước khi vào Tiên Giới.');
        const position = p.worldPosition;
        const positionMap = position && this.provinceAtWorldPosition(targetMap.worldId, position.x, position.y);
        if (!positionMap || positionMap.id !== target.mapId)
            fail(`Hãy qua cổng dịch chuyển miễn phí để vào ${targetMap.provinceName || targetMap.name}.`);
        const realm = this.realmOf(userId);
        if (realm.index < targetMap.realmMin) fail(`Cần đạt ${targetMap.realmMinName} để vào ${targetMap.provinceName || targetMap.name}.`);
        if (p.town === target.id) return { town: target.id, townName: target.name, changed: false };
        p.town = target.id;
        p.mapId = target.mapId;
        if (p.worldPosition) p.worldPosition.mapId = target.mapId;
        this.touch();
        return { town: target.id, townName: target.name, changed: true };
    }

    /** Uses a paired free gate to cross a border between adjacent provinces of one realm. */
    crossProvinceGate(userId, gateId) {
        const p = this.requirePlayer(userId);
        const now = this.now();
        this.checkTravelArrival(p, now);
        if (p.traveling) fail('Đang ngự kiếm phi hành, chưa thể qua cổng châu.');
        if (this.activeBattle(userId)) fail('Đang trong trận chiến, chưa thể qua cổng châu.');
        const gate = C.WORLD_PROVINCE_GATES.find(candidate => candidate.id === String(gateId || ''));
        if (!gate) fail('Không tìm thấy cổng dịch chuyển châu này.');
        const currentTown = C.TOWN_BY_ID.get(p.town || 'thanh_van');
        const currentWorld = C.MAP_BY_ID.get(currentTown?.mapId)?.worldId;
        const position = p.worldPosition;
        if (currentWorld !== gate.worldId || !position || C.MAP_BY_ID.get(position.mapId)?.worldId !== gate.worldId)
            fail('Cổng này không thuộc bản đồ đang đứng.');
        const near = (end) => Math.hypot((Number(position.x) || 0) - end.x, (Number(position.y) || 0) - end.y) <= 6;
        const source = near(gate.a) ? gate.a : near(gate.b) ? gate.b : null;
        if (!source) fail('Hãy đi tới sát cổng dịch chuyển của châu.');
        const target = source === gate.a ? gate.b : gate.a;
        const targetMap = C.MAP_BY_ID.get(target.mapId);
        const realm = this.realmOf(userId);
        if (realm.index < (targetMap?.realmMin ?? gate.realmMin))
            fail(`Cần đạt ${targetMap?.realmMinName || this.realmName(targetMap?.realmMin ?? gate.realmMin)} để qua cổng tới ${targetMap?.provinceName || targetMap?.name || 'châu kế tiếp'}.`);
        const targetTown = C.TOWN_BY_ID.get(targetMap?.townIds?.[0]);
        if (!targetMap || !targetTown) fail('Chưa tìm thấy thành trấn ở châu bên kia cổng.');
        p.town = targetTown.id;
        p.mapId = target.mapId;
        p.isRoaming = false;
        p.worldPosition = { mapId: target.mapId, x: target.x, y: target.y, updatedAt: now };
        this.touch();
        return {
            gateId: gate.id,
            mapId: target.mapId,
            townId: targetTown.id,
            townName: targetTown.name,
            x: target.x,
            y: target.y,
            worldId: gate.worldId,
            message: `Cổng dịch chuyển miễn phí đưa đạo hữu sang ${targetMap.provinceName || targetMap.name}.`,
        };
    }

    /** Crosses between the two separately displayed realm maps at the paired Ascension Gate. */
    crossAscensionGate(userId) {
        const p = this.requirePlayer(userId);
        const now = this.now();
        this.checkTravelArrival(p, now);
        if (p.traveling) fail('Đang ngự kiếm phi hành, chưa thể qua Cổng Phi Thăng.');
        if (this.activeBattle(userId)) fail('Đang trong trận chiến, chưa thể qua Cổng Phi Thăng.');
        const currentTown = C.TOWN_BY_ID.get(p.town || 'thanh_van');
        const sourceWorld = C.MAP_BY_ID.get(currentTown?.mapId)?.worldId;
        const sourceGate = C.WORLD_ASCENSION_GATES[sourceWorld];
        if (!sourceGate) fail('Không tìm thấy Cổng Phi Thăng ở cõi này.');
        const position = p.worldPosition;
        if (!position || C.MAP_BY_ID.get(position.mapId)?.worldId !== sourceWorld
            || Math.hypot((Number(position.x) || 0) - sourceGate.x, (Number(position.y) || 0) - sourceGate.y) > 7) {
            fail('Hãy đi đến Cổng Phi Thăng được đánh dấu trên bản đồ.');
        }
        if (!p.ascended) fail('Cần hoàn thành nghi thức Phi Thăng trước khi mở Cổng sang Tiên Giới.');
        const targetWorld = sourceWorld === 'world_tien' ? 'world_pham' : 'world_tien';
        const targetGate = C.WORLD_ASCENSION_GATES[targetWorld];
        const targetTown = C.TOWN_BY_ID.get(targetGate.townId);
        const targetMap = C.MAP_BY_ID.get(targetGate.mapId);
        if (!targetGate || !targetTown || !targetMap) fail('Chưa tìm thấy đầu cổng ở cõi bên kia.');
        p.town = targetTown.id;
        p.mapId = targetMap.id;
        p.isRoaming = false;
        p.worldPosition = { mapId: targetMap.id, x: targetGate.x, y: targetGate.y, updatedAt: now };
        this.touch();
        return {
            fromWorldId: sourceWorld,
            worldId: targetWorld,
            mapId: targetMap.id,
            townId: targetTown.id,
            townName: targetTown.name,
            x: targetGate.x,
            y: targetGate.y,
            message: `Cổng Phi Thăng đưa đạo hữu tới ${targetWorld === 'world_tien' ? 'Tiên Giới' : 'Phàm Giới'}.`,
        };
    }

    /** Updates the stored appearance (layered avatar look) after creation. */
    setLook(userId, text) {
        const p = this.requirePlayer(userId);
        const look = sanitizeLook(text, p.gender);
        if (!look) fail('Diện mạo không hợp lệ.');
        p.look = look.text;
        p.appearanceColors = { ...(p.appearanceColors || {}), hair: look.values.hc || p.appearanceColors?.hair, outfit: (Number(look.values.to) > 0 ? look.values.oc : look.values.tc) || p.appearanceColors?.outfit, eyes: look.values.ec || p.appearanceColors?.eyes };
        this.touch();
        return { look: p.look };
    }

    teleportWithRing(userId, toTownId) {
        const p = this.requirePlayer(userId);
        const now = this.now();
        this.checkTravelArrival(p, now);
        if (p.traveling) fail('Hãy đợi ngự kiếm hạ cánh trước khi dùng Nhẫn Na Di.');
        if (this.activeBattle(userId)) fail('Đang trong trận chiến, không thể dịch chuyển.');
        if ((p.teleportCooldownUntil || 0) > now) {
            const minutes = Math.ceil((p.teleportCooldownUntil - now) / 60000);
            fail(`Nhẫn Na Di đang hồi phục, còn ${minutes} phút.`);
        }
        this.migrateTeleportRingSlot(p);
        const ringId = p.equip?.nhanNaDi;
        const ring = (p.items || []).find(item => item.uid === ringId && item.place === 'equip');
        if (!ring || !itemDef(ring)?.teleportRing) fail('Cần đeo Nhẫn Na Di Vạn Giới ở ô Nhẫn Dịch Chuyển.');
        const durability = Math.max(0, Number(ring.dur ?? 100));
        if (durability <= 0) {
            fail('Nhẫn Na Di đã hỏng. Hãy sửa chữa nhẫn trước khi dịch chuyển tiếp.');
        }
        const target = C.TOWN_BY_ID.get(String(toTownId));
        if (!target) fail('Không tìm thấy thành trấn này.');
        if (target.id === p.town) fail('Đạo hữu đang ở thành trấn này rồi.');
        this.requireSameWorldDestination(p, target);
        const targetMap = C.MAP_BY_ID.get(target.mapId);
        if (targetMap?.ascensionRequired && !p.ascended) fail('Cần hoàn thành nghi thức Phi Thăng trước khi vào Tiên Giới.');
        p.town = target.id;
        p.mapId = target.mapId;
        p.isRoaming = false;
        p.worldPosition = null;
        ring.dur = durability - 1;
        const broken = ring.dur <= 0;
        p.teleportCooldownUntil = now + 30 * 60 * 1000;
        p.notices = (p.notices || []).concat([`Nhẫn Na Di đưa đạo hữu đến ${target.name} miễn phí, mất 1 độ bền${broken ? ' (nhẫn đã vỡ)' : ` (còn ${ring.dur}/100)`}.`]);
        this.touch();
        return { toTownName: target.name, cost: 0, durability: broken ? 0 : ring.dur, broken, cooldownUntil: p.teleportCooldownUntil };
    }

    marketTeleport(userId, toTownId) {
        const p = this.requirePlayer(userId);
        const now = this.now();
        this.checkTravelArrival(p, now);
        if (p.traveling) fail('Hãy đợi ngự kiếm hạ cánh trước khi truyền tống.');
        if (this.activeBattle(userId)) fail('Đang trong trận chiến, không thể truyền tống.');

        p.town = p.town || 'thanh_van';
        const target = C.TOWN_BY_ID.get(String(toTownId || ''));
        if (!target) fail('Không tìm thấy thành trấn này.');
        if (target.id === p.town) fail('Đạo hữu đang ở thành trấn này rồi.');

        const currentTown = C.TOWN_BY_ID.get(p.town);
        this.requireSameWorldDestination(p, target);
        const targetMap = C.MAP_BY_ID.get(target.mapId);
        const targetIsImmortal = Boolean(targetMap?.ascensionRequired);
        if (targetIsImmortal && !p.ascended) fail('Cần hoàn thành nghi thức Phi Thăng trước khi vào Tiên Giới.');
        const distance = Math.hypot(
            (Number(target.x) || 0) - (Number(currentTown?.x) || 0),
            (Number(target.y) || 0) - (Number(currentTown?.y) || 0),
        );
        const cost = C.getTeleportCost(p.town, target.id);
        const stones = Math.max(0, Number(p.stones) || 0);
        if (stones < cost) fail(`Truyền tống đến ${target.name} cần ${cost.toLocaleString('vi-VN')} linh thạch; hiện có ${stones.toLocaleString('vi-VN')}.`);

        p.stones = stones - cost;
        p.town = target.id;
        p.mapId = target.mapId;
        p.isRoaming = false;
        p.worldPosition = null;
        p.notices = (p.notices || []).concat([`Đã truyền tống đến ${target.name}, tiêu hao ${cost.toLocaleString('vi-VN')} linh thạch.`]);
        this.touch();
        return { fromTownName: currentTown?.name || p.town, toTownName: target.name, cost, distance, stones: p.stones };
    }

    // ---- Yêu Thú Thế Giới & Thú Triều & Tu Luyện Yêu Thú --------------------
    createWorldMonsterInstance(townId, def, now, options = {}) {
        const ms = monsterStats(def, now);
        const instance = {
            uid: newId(), townId, monsterId: def.id, name: def.name, icon: def.icon || '👹',
            realm: def.realm, element: def.element, level: 1, exp: 0,
            expCap: Math.round(300 * Math.pow(1.4, def.realm)),
            isBoss: options.isBoss ?? !def.small,
            isWorldBoss: options.isWorldBoss ?? Boolean(def.worldBoss),
            isInvasion: false,
            hp: ms.hp, maxHp: ms.hp, atk: ms.atk, def: ms.def, spd: def.spd,
            lockedBy: null, lockedByName: null, lockedUntil: 0, spawnAt: now,
            combatBalanceVersion: 2,
            ...options,
        };
        if (instance.isBoss && !instance.isInvasion) {
            instance.maxHp = Math.min(MAX_BOSS_HP, Math.max(1, Math.round(Number(instance.maxHp) || ms.hp)));
            instance.hp = Math.min(instance.maxHp, Math.max(1, Math.round(Number(instance.hp) || instance.maxHp)));
        }
        return instance;
    }

    isWorldBossInstance(monster) {
        if (!monster || monster.isBeastTideBoss || monster.isInvasion) return false;
        return Boolean(monster.isScheduledWorldBoss || C.MONSTER_BY_ID.get(monster.monsterId)?.worldBoss);
    }

    worldBossWindowKey(now = this.now()) {
        const hour = vnHour(now);
        if (hour >= 11 && hour < 14) return 'midday';
        if (hour >= 18 && hour < 24) return 'night';
        return null;
    }

    isWorldBossWindow(now = this.now()) {
        return Boolean(this.worldBossWindowKey(now));
    }

    worldBossRealmBand(realm) {
        const value = Number(realm) || 0;
        return value <= 9 ? 'low' : value <= 30 ? 'middle' : 'high';
    }

    worldBossSpawnChoices(realm, townCounts, avoidTownId = null) {
        const byRealm = new Map();
        for (const def of C.MONSTERS) {
            if (def.small || (realm != null && Number(def.realm) !== Number(realm))) continue;
            if (!byRealm.has(def.realm)) byRealm.set(def.realm, []);
            byRealm.get(def.realm).push(def);
        }
        const definitions = [...byRealm.values()].flatMap(defs => {
            const catalogBosses = defs.filter(def => def.worldBoss);
            return catalogBosses.length ? catalogBosses : defs;
        });
        const pairs = [];
        for (const def of definitions) {
            for (const town of C.TOWNS) {
                if (town.realmMin > def.realm || this.townRealmCap(town.id) < def.realm) continue;
                if ((townCounts.get(town.id) || 0) >= 3) continue;
                pairs.push({ def, town });
            }
        }
        if (avoidTownId && pairs.some(pair => pair.town.id !== avoidTownId)) {
            return pairs.filter(pair => pair.town.id !== avoidTownId);
        }
        return pairs;
    }

    spawnScheduledWorldBoss(now, schedule, townCounts, preferredRealm = null, avoidTownId = null) {
        let choices = this.worldBossSpawnChoices(preferredRealm, townCounts, avoidTownId);
        if (avoidTownId && !choices.some(pair => pair.town.id !== avoidTownId)) {
            const otherTownChoices = this.worldBossSpawnChoices(null, townCounts, avoidTownId);
            if (otherTownChoices.some(pair => pair.town.id !== avoidTownId)) choices = otherTownChoices;
        }
        if (!choices.length && preferredRealm != null) {
            choices = this.worldBossSpawnChoices(null, townCounts, avoidTownId);
        }
        if (!choices.length) return null;

        const realmCounts = new Map();
        const bandCounts = new Map([['low', 0], ['middle', 0], ['high', 0]]);
        for (const monster of this.data.worldMonsters || []) {
            if (monster.isScheduledWorldBoss && monster.worldBossDate === schedule.date) {
                const realm = Number(monster.realm) || 0;
                realmCounts.set(realm, (realmCounts.get(realm) || 0) + 1);
                const band = this.worldBossRealmBand(realm);
                bandCounts.set(band, (bandCounts.get(band) || 0) + 1);
            }
        }
        const eligibleBands = [...new Set(choices.map(pair => this.worldBossRealmBand(pair.def.realm)))];
        let selectedBand = preferredRealm != null
            ? this.worldBossRealmBand(preferredRealm)
            : null;
        if (!eligibleBands.includes(selectedBand)) selectedBand = null;
        if (selectedBand == null) {
            const leastBandCount = Math.min(...eligibleBands.map(band => bandCounts.get(band) || 0));
            const balancedBands = eligibleBands.filter(band => (bandCounts.get(band) || 0) === leastBandCount);
            selectedBand = balancedBands[Math.floor(this.rng() * balancedBands.length)];
        }
        const bandChoices = choices.filter(pair => this.worldBossRealmBand(pair.def.realm) === selectedBand);
        const eligibleRealms = [...new Set(bandChoices.map(pair => pair.def.realm))];
        let selectedRealm = preferredRealm != null && eligibleRealms.includes(Number(preferredRealm))
            ? Number(preferredRealm)
            : null;
        if (selectedRealm == null) {
            const leastCount = Math.min(...eligibleRealms.map(realm => realmCounts.get(realm) || 0));
            const balancedRealms = eligibleRealms.filter(realm => (realmCounts.get(realm) || 0) === leastCount);
            selectedRealm = balancedRealms[Math.floor(this.rng() * balancedRealms.length)];
        }
        const realmChoices = bandChoices.filter(pair => pair.def.realm === selectedRealm);
        const occupiedTownChoices = realmChoices.filter(option => (townCounts.get(option.town.id) || 0) > 0);
        const placementChoices = occupiedTownChoices.length ? occupiedTownChoices : realmChoices;
        const pair = placementChoices[Math.floor(this.rng() * placementChoices.length)];
        const sameTownChoices = placementChoices.filter(option => option.town.id === pair.town.id);
        const sourceDef = sameTownChoices[Math.floor(this.rng() * sameTownChoices.length)].def;
        const def = sourceDef.worldBoss ? sourceDef : { ...sourceDef, worldBoss: true };
        const instance = this.createWorldMonsterInstance(pair.town.id, def, now, {
            isBoss: true,
            isWorldBoss: true,
            isScheduledWorldBoss: true,
            worldBossDate: schedule.date,
            worldBossWindow: schedule.windowKey,
        });
        this.data.worldMonsters.push(instance);
        townCounts.set(pair.town.id, (townCounts.get(pair.town.id) || 0) + 1);
        for (const player of Object.values(this.data.players || {})) {
            if (player.registered && (player.town || 'thanh_van') === pair.town.id) {
                player.notices ||= [];
                player.notices.push(`🌌 Boss Thế Giới [${def.name}] xuất hiện tại ${pair.town.name}!`);
            }
        }
        return instance;
    }

    queueWorldMonsterRespawn(monster, now = this.now()) {
        if (!monster || monster.isBeastTideBoss || monster.isInvasion) return false;
        if (this.isWorldBossInstance(monster)) {
            const schedule = this.data.worldBossSchedule;
            const today = vnDate(now);
            if (!this.isWorldBossWindow(now) || schedule?.version !== 2 || schedule.date !== today) return false;
            schedule.respawns ||= [];
            if (schedule.respawns.some(entry => String(entry.slainUid || '') === String(monster.uid || ''))) return false;
            schedule.respawns.push({
                slainUid: String(monster.uid || ''),
                realm: Number(monster.realm) || Number(C.MONSTER_BY_ID.get(monster.monsterId)?.realm) || 0,
                avoidTownId: monster.townId || null,
                queuedAt: now,
                dueAt: now + (5 + Math.floor(this.rng() * 11)) * 60 * 1000,
            });
            schedule.respawns.sort((a, b) => a.dueAt - b.dueAt);
            return true;
        }
        if (monster.isScheduledWorldBoss) return false;
        const townId = monster.townId || 'thanh_van';
        this.data.worldMonsterRespawns ||= {};
        const queue = this.data.worldMonsterRespawns[townId] ||= [];
        const monsterDef = C.MONSTER_BY_ID.get(monster.monsterId);
        const isSmall = monsterDef ? Boolean(monsterDef.small) : (monster.isBoss == null ? true : !monster.isBoss);
        queue.push({
            monsterId: monster.monsterId,
            isSmall,
            queuedAt: now,
            dueAt: now + (isSmall ? SMALL_MONSTER_RESPAWN_MS : monsterRespawnMs(monsterDef, now)),
        });
        return true;
    }

    worldMonsterSpawnPools(town, localPool = null) {
        const townId = typeof town === 'string' ? town : town?.id;
        const townDef = typeof town === 'string' ? C.TOWN_BY_ID.get(town) : town;
        const configured = localPool || (townDef?.monsterPool || [])
            .map(id => C.MONSTER_BY_ID.get(id)).filter(monster => monster && !monster.worldBoss);
        const { min, max } = this.townRealmRange(townId);
        const inRealm = monster => monster && !monster.worldBoss && monster.realm >= min && monster.realm <= max;
        const unique = monsters => [...new Map(monsters.filter(Boolean).map(monster => [monster.id, monster])).values()];
        const smallPool = unique([
            ...configured.filter(monster => monster.small && inRealm(monster)),
            ...C.MONSTERS.filter(monster => monster.small && inRealm(monster)),
        ]);
        const bossPool = unique([
            ...configured.filter(monster => !monster.small && inRealm(monster)),
            ...C.MONSTERS.filter(monster => !monster.small && inRealm(monster)),
        ]);

        const midpoint = (min + max) / 2;
        if (smallPool.length < 3) {
            const nearestSmall = C.MONSTERS.filter(monster => monster.small && !monster.worldBoss && !smallPool.some(item => item.id === monster.id))
                .sort((a, b) => Math.abs(a.realm - midpoint) - Math.abs(b.realm - midpoint));
            for (const monster of nearestSmall) {
                smallPool.push(monster);
                if (smallPool.length >= 3) break;
            }
        }
        if (!bossPool.length) {
            const nearestBosses = C.MONSTERS.filter(monster => !monster.small && !monster.worldBoss)
                .sort((a, b) => Math.abs(a.realm - midpoint) - Math.abs(b.realm - midpoint));
            if (nearestBosses.length) {
                const nearestRealm = Math.abs(nearestBosses[0].realm - midpoint);
                bossPool.push(...nearestBosses.filter(monster => Math.abs(monster.realm - midpoint) === nearestRealm));
            }
        }
        return { smallPool, bossPool };
    }

    nextWorldMonsterFromRotation(townId, pool, excludedIds = new Set()) {
        if (!pool?.length) return null;
        this.data.worldMonsterRotation ||= {};
        const rotation = this.data.worldMonsterRotation[townId] ||= {};
        const candidates = [...new Map(pool.filter(Boolean).map(monster => [monster.id, monster])).values()]
            .sort((a, b) => a.id.localeCompare(b.id));
        if (!candidates.length) return null;

        const isSmall = Boolean(candidates[0].small);
        const cursorKey = isSmall ? 'lastSmallMonsterId' : 'lastLargeMonsterId';
        const lastIndex = candidates.findIndex(monster => monster.id === rotation[cursorKey]);
        let selected = null;
        for (let offset = 1; offset <= candidates.length; offset += 1) {
            const candidate = candidates[(lastIndex + offset + candidates.length) % candidates.length];
            if (!excludedIds.has(candidate.id)) {
                selected = candidate;
                break;
            }
        }
        if (!selected) selected = candidates[(lastIndex + 1 + candidates.length) % candidates.length];
        rotation[cursorKey] = selected.id;
        return selected;
    }

    worldMonsterRotationDelay() {
        return (2 * 60 + Math.floor(this.rng() * 121)) * 60 * 1000;
    }

    ensureWorldMonsters(now = this.now()) {
        let changed = false;
        if (!Array.isArray(this.data.worldMonsters)) { this.data.worldMonsters = []; changed = true; }
        if (!this.data.worldMonsterRespawns) { this.data.worldMonsterRespawns = {}; changed = true; }
        if (!this.data.worldMonsterRotation || typeof this.data.worldMonsterRotation !== 'object') {
            this.data.worldMonsterRotation = {};
            changed = true;
        }
        // Bản lưu cũ có thể thiếu UID hoặc dùng UID dạng số. Chuẩn hóa trước
        // khi render/đánh để nút trên Mini App luôn trỏ đúng một quái.
        const seenMonsterUids = new Set();
        // Nâng nhẹ chỉ số các bầy đang tồn tại đúng một lần khi cập nhật.
        for (const monster of this.data.worldMonsters) {
            if (!monster || typeof monster !== 'object') continue;
            const existingUid = monster.uid == null ? '' : String(monster.uid);
            if (!existingUid || seenMonsterUids.has(existingUid)) {
                monster.uid = newId();
                changed = true;
            } else if (monster.uid !== existingUid) {
                monster.uid = existingUid;
                changed = true;
            }
            seenMonsterUids.add(String(monster.uid));

            const monsterDef = C.MONSTER_BY_ID.get(monster.monsterId);
            const isScheduledWorldBoss = Boolean(monster.isScheduledWorldBoss);
            const isCatalogWorldBoss = Boolean(monsterDef?.worldBoss && !monster.isBeastTideBoss && !monster.isInvasion);
            if ((isScheduledWorldBoss || isCatalogWorldBoss) && !monster.isWorldBoss) {
                monster.isWorldBoss = true;
                changed = true;
            }
            if (monster.isWorldBoss && !monster.isBoss) {
                monster.isBoss = true;
                changed = true;
            }
            const balanceVersion = Number(monster.combatBalanceVersion) || 0;
            if (balanceVersion >= 2) continue;
            const isWorldBoss = this.isWorldBossInstance(monster);
            for (const stat of ['hp', 'maxHp', 'atk', 'def']) {
                const value = Number(monster[stat]);
                if (Number.isFinite(value) && value > 0) {
                    const globalBuff = 1.10 / 1.05;
                    const bossAttackBuff = isWorldBoss && stat === 'atk' ? 1.10 : 1;
                    monster[stat] = Math.round(value * globalBuff * bossAttackBuff);
                }
            }
            monster.combatBalanceVersion = 2;
            changed = true;
        }

        // Boss thế giới xuất hiện liên tục trong hai khung giờ theo giờ Việt Nam.
        // Mỗi khung có tối đa 20 con; boss bị hạ sẽ được thay sau thời gian chờ.
        const today = vnDate(now);
        const activeWindow = this.worldBossWindowKey(now);
        const previousSchedule = this.data.worldBossSchedule;
        const scheduleNeedsReset = previousSchedule?.version !== 2
            || previousSchedule.date !== today
            || (activeWindow && previousSchedule.windowKey !== activeWindow);
        if (scheduleNeedsReset) {
            const before = this.data.worldMonsters.length;
            this.data.worldMonsters = this.data.worldMonsters.filter(monster =>
                monster.isBeastTideBoss || monster.isInvasion || !this.isWorldBossInstance(monster)
            );
            if (before !== this.data.worldMonsters.length) changed = true;
            this.data.worldBossSchedule = {
                version: 2,
                date: today,
                windowKey: activeWindow,
                initialized: false,
                respawns: [],
            };
            changed = true;
            if (activeWindow) {
                const townCounts = new Map();
                let spawned = this.spawnScheduledWorldBoss(
                    now, this.data.worldBossSchedule, townCounts, 0
                ) ? 1 : 0;
                while (spawned < 20) {
                    const instance = this.spawnScheduledWorldBoss(now, this.data.worldBossSchedule, townCounts);
                    if (!instance) break;
                    spawned += 1;
                }
                this.data.worldBossSchedule.initialized = true;
                changed = true;
            }
        } else if (!activeWindow) {
            const before = this.data.worldMonsters.length;
            this.data.worldMonsters = this.data.worldMonsters.filter(monster =>
                monster.isBeastTideBoss || monster.isInvasion || !this.isWorldBossInstance(monster)
            );
            if (before !== this.data.worldMonsters.length) changed = true;
            if (this.data.worldBossSchedule.windowKey !== null
                || this.data.worldBossSchedule.initialized
                || (this.data.worldBossSchedule.respawns || []).length) {
                this.data.worldBossSchedule.windowKey = null;
                this.data.worldBossSchedule.initialized = false;
                this.data.worldBossSchedule.respawns = [];
                changed = true;
            }
        } else {
            const schedule = this.data.worldBossSchedule;
            for (const monster of this.data.worldMonsters) {
                if (this.isWorldBossInstance(monster) && !monster.isScheduledWorldBoss) {
                    monster.isScheduledWorldBoss = true;
                    changed = true;
                }
                if (monster.isScheduledWorldBoss && monster.worldBossDate !== today) {
                    monster.worldBossDate = today;
                    monster.worldBossWindow = activeWindow;
                    changed = true;
                }
            }
            schedule.respawns = (schedule.respawns || []).filter(entry => Number(entry.dueAt) > 0);
            schedule.respawns.sort((a, b) => a.dueAt - b.dueAt);
            const bossTownCounts = new Map();
            const activeBosses = this.data.worldMonsters.filter(monster =>
                monster.isScheduledWorldBoss && monster.worldBossDate === today && Number(monster.hp) > 0
            );
            for (const monster of activeBosses) {
                bossTownCounts.set(monster.townId, (bossTownCounts.get(monster.townId) || 0) + 1);
            }

            if (!schedule.initialized) {
                if (!activeBosses.some(monster => (Number(monster.realm) || 0) <= 3)) {
                    const starter = this.spawnScheduledWorldBoss(now, schedule, bossTownCounts, 0);
                    if (starter) {
                        activeBosses.push(starter);
                        changed = true;
                    }
                }
                while (activeBosses.length < 20) {
                    const instance = this.spawnScheduledWorldBoss(now, schedule, bossTownCounts);
                    if (!instance) break;
                    activeBosses.push(instance);
                    changed = true;
                }
                schedule.initialized = true;
                changed = true;
            }

            const pending = [];
            for (const respawn of schedule.respawns) {
                if (respawn.dueAt > now || activeBosses.length >= 20) {
                    pending.push(respawn);
                    continue;
                }
                const instance = this.spawnScheduledWorldBoss(
                    now, schedule, bossTownCounts, respawn.realm, respawn.avoidTownId
                );
                if (instance) {
                    activeBosses.push(instance);
                    changed = true;
                } else {
                    pending.push({ ...respawn, dueAt: now + 5 * 60 * 1000 });
                }
            }
            if (pending.length !== schedule.respawns.length
                || pending.some((entry, index) => entry.dueAt !== schedule.respawns[index]?.dueAt)) changed = true;
            schedule.respawns = pending;

            if (!schedule.respawns.length) {
                while (activeBosses.length < 20) {
                    const instance = this.spawnScheduledWorldBoss(now, schedule, bossTownCounts);
                    if (!instance) break;
                    activeBosses.push(instance);
                    changed = true;
                }
            }
        }

        // Thú Triều mở đúng hai khung giờ ngẫu nhiên mỗi ngày, tại một thành.
        const dayStart = Date.parse(`${today}T00:00:00.000Z`) - VN_OFFSET_MS;
        const makeTideSlots = () => {
            const firstHour = 7 + Math.floor(this.rng() * 9);
            const secondHour = firstHour + 4 + Math.floor(this.rng() * Math.max(1, 24 - (firstHour + 4)));
            const firstMinute = Math.floor(this.rng() * 60);
            const secondMinute = Math.floor(this.rng() * 60);
            return [
                { id: 1, at: dayStart + firstHour * 3600 * 1000 + firstMinute * 60000, spawned: false },
                { id: 2, at: dayStart + secondHour * 3600 * 1000 + secondMinute * 60000, spawned: false },
            ];
        };
        if (this.data.beastTideSchedule?.date !== today) {
            this.data.beastTideSchedule = {
                date: today,
                version: 2,
                slots: makeTideSlots(),
            };
            changed = true;
        } else if (this.data.beastTideSchedule.version !== 2 || (this.data.beastTideSchedule.slots || []).length !== 2) {
            const oldSlots = (this.data.beastTideSchedule.slots || []).slice().sort((a, b) => a.at - b.at);
            const spawned = oldSlots.filter(slot => slot.spawned).slice(0, 2);
            const pending = oldSlots.filter(slot => !slot.spawned).slice(0, Math.max(0, 2 - spawned.length));
            const slots = [...spawned, ...pending];
            if (slots.length < 2) slots.push(...makeTideSlots().slice(0, 2 - slots.length));
            slots.sort((a, b) => a.at - b.at).forEach((slot, i) => { slot.id = i + 1; });
            this.data.beastTideSchedule = { date: today, version: 2, slots };
            changed = true;
        }
        if (this.data.beastTideEvent && (this.data.beastTideEvent.endsAt || 0) <= now) {
            this.data.beastTideEvent = null;
            changed = true;
        }
        if (!this.data.beastTideEvent) {
            const dueTide = this.data.beastTideSchedule.slots.find(slot => !slot.spawned && slot.at <= now);
            if (dueTide) {
                const town = C.TOWNS[Math.floor(this.rng() * C.TOWNS.length)] || C.TOWNS[0];
                this.data.beastTideEvent = {
                    townIds: town ? [town.id] : [],
                    startsAt: now,
                    endsAt: now + 3 * 3600 * 1000,
                    wave: dueTide.id,
                };
                dueTide.spawned = true;
                changed = true;
            }
        }
        const tideTownIds = new Set(this.data.beastTideEvent?.townIds || []);

        for (const town of C.TOWNS) {
            let currentMonsters = this.data.worldMonsters.filter(m => m.townId === town.id && !m.isScheduledWorldBoss);
            const isTide = tideTownIds.has(town.id);
            const scheduledCount = this.data.worldMonsters.filter(m => m.townId === town.id && m.isScheduledWorldBoss).length;
            const reserveTideBoss = isTide && !currentMonsters.some(m => m.isBeastTideBoss) ? 1 : 0;
            const targetCount = Math.min(isTide ? 8 - reserveTideBoss : 5, Math.max(0, 10 - scheduledCount - reserveTideBoss));
            if (!isTide && currentMonsters.length > targetCount) {
                let excess = currentMonsters.length - targetCount;
                for (let i = currentMonsters.length - 1; i >= 0 && excess > 0; i -= 1) {
                    const m = currentMonsters[i];
                    if (!m.lockedUntil || m.lockedUntil <= now) {
                        this.data.worldMonsters = this.data.worldMonsters.filter(x => x.uid !== m.uid);
                        currentMonsters.splice(i, 1);
                        excess -= 1;
                        changed = true;
                    }
                }
            }

            if (!Array.isArray(this.data.worldMonsterRespawns[town.id])) { this.data.worldMonsterRespawns[town.id] = []; changed = true; }
            const queue = this.data.worldMonsterRespawns[town.id];
            if (queue.some((item, index) => index > 0 && queue[index - 1].dueAt > item.dueAt)) {
                queue.sort((a, b) => a.dueAt - b.dueAt);
                changed = true;
            }
            const localPool = (town.monsterPool || []).map(id => C.MONSTER_BY_ID.get(id)).filter(def => def && !def.worldBoss);
            const { min: townRealm, max: townCap } = this.townRealmRange(town.id);
            const inTownRealmRange = def => def && !def.worldBoss && def.realm >= townRealm && def.realm <= townCap;
            const { smallPool, bossPool } = this.worldMonsterSpawnPools(town, localPool);
            // Dữ liệu cũ có thể chứa cả tiểu yêu lẫn đại yêu sai cảnh giới.
            // Giữ nguyên mục tiêu đang bị khóa giao chiến; các mục còn lại chuyển sang loài hợp vùng.
            for (const existing of currentMonsters) {
                const existingDef = C.MONSTER_BY_ID.get(existing.monsterId);
                if (!existingDef || this.isWorldBossInstance(existing) || existing.isInvasion || existing.isBeastTideBoss
                    || inTownRealmRange(existingDef) || (existing.lockedUntil || 0) > now) continue;
                const small = Boolean(existingDef.small);
                const pool = small ? smallPool : bossPool;
                const activeIds = new Set(currentMonsters
                    .filter(monster => monster !== existing && Boolean(C.MONSTER_BY_ID.get(monster.monsterId)?.small) === small)
                    .map(monster => monster.monsterId));
                const replacement = this.nextWorldMonsterFromRotation(town.id, pool, activeIds);
                if (!replacement || replacement.id === existing.monsterId) continue;
                const refreshed = this.createWorldMonsterInstance(town.id, replacement, now, {
                    uid: existing.uid,
                    spawnAt: existing.spawnAt || now,
                });
                Object.assign(existing, refreshed);
                changed = true;
            }
            const roster = currentMonsters.filter(m => !m.isInvasion && !m.isBeastTideBoss);
            const isSmallInstance = m => Boolean(C.MONSTER_BY_ID.get(m.monsterId)?.small);
            const targetSmall = Math.min(3, targetCount);
            const targetBoss = Math.max(0, targetCount - targetSmall);

            const rotationState = this.data.worldMonsterRotation[town.id] ||= {};
            if (!(Number(rotationState.nextAt) > 0)) {
                rotationState.nextAt = now + this.worldMonsterRotationDelay();
                changed = true;
            } else if (Number(rotationState.nextAt) <= now) {
                for (const [small, pool] of [[true, smallPool], [false, bossPool]]) {
                    const candidates = currentMonsters
                        .filter(monster => !monster.isInvasion && !monster.isBeastTideBoss
                            && !this.isWorldBossInstance(monster)
                            && isSmallInstance(monster) === small
                            && (!monster.lockedUntil || monster.lockedUntil <= now))
                        .sort((a, b) => (a.spawnAt || 0) - (b.spawnAt || 0));
                    for (const current of candidates) {
                        const activeIds = new Set(currentMonsters
                            .filter(monster => monster !== current && isSmallInstance(monster) === small)
                            .map(monster => monster.monsterId));
                        const replacement = this.nextWorldMonsterFromRotation(town.id, pool, activeIds);
                        if (!replacement || replacement.id === current.monsterId) continue;
                        Object.assign(current, this.createWorldMonsterInstance(town.id, replacement, now));
                        changed = true;
                        break;
                    }
                }
                rotationState.nextAt = now + this.worldMonsterRotationDelay();
                changed = true;
            }

            // Respawn slots count toward the town's target while their cooldown is active.
            // This prevents ensureWorldMonsters from replacing a slain beast immediately.
            const queuedIsSmall = item => item.isSmall == null
                ? Boolean(C.MONSTER_BY_ID.get(item.monsterId)?.small)
                : Boolean(item.isSmall);
            const dueRespawns = queue.filter(item => item.dueAt <= now);
            const pendingRespawns = queue.filter(item => item.dueAt > now);
            queue.splice(0, queue.length, ...pendingRespawns);
            for (const respawn of dueRespawns) {
                const small = queuedIsSmall(respawn);
                const pool = small ? smallPool : bossPool;
                const activeIds = new Set(roster.filter(monster => isSmallInstance(monster) === small).map(monster => monster.monsterId));
                let def = this.nextWorldMonsterFromRotation(town.id, pool, activeIds);
                if (!def) {
                    const queuedDef = C.MONSTER_BY_ID.get(respawn.monsterId);
                    if (queuedDef && !queuedDef.worldBoss && Boolean(queuedDef.small) === small) def = queuedDef;
                }
                const wanted = small ? targetSmall : targetBoss;
                const activeCount = roster.filter(m => isSmallInstance(m) === small).length;
                const waitingCount = queue.filter(item => queuedIsSmall(item) === small).length;
                if (!def || def.worldBoss || activeCount + waitingCount >= wanted || roster.length >= targetCount || currentMonsters.length >= 10) continue;
                const instance = this.createWorldMonsterInstance(town.id, def, now, { spawnAt: now });
                this.data.worldMonsters.push(instance);
                currentMonsters.push(instance);
                roster.push(instance);
                changed = true;
            }

            // Mỗi khu duy trì 3 tiểu yêu và 2 đại yêu; hàng đợi chờ hồi giữ chỗ đến hết hạn.
            for (const [small, wanted, pool] of [[true, targetSmall, smallPool], [false, targetBoss, bossPool]]) {
                let matching = roster.filter(m => isSmallInstance(m) === small);
                if (matching.length > wanted) {
                    const removable = matching
                        .filter(m => !m.lockedUntil || m.lockedUntil <= now)
                        .sort((a, b) => Number(a.isWorldBoss) - Number(b.isWorldBoss) || (b.spawnAt || 0) - (a.spawnAt || 0));
                    while (matching.length > wanted && removable.length) {
                        const removed = removable.shift();
                        this.data.worldMonsters = this.data.worldMonsters.filter(m => m.uid !== removed.uid);
                        roster.splice(roster.indexOf(removed), 1);
                        currentMonsters.splice(currentMonsters.indexOf(removed), 1);
                        matching.splice(matching.indexOf(removed), 1);
                        changed = true;
                    }
                }
                const waitingCount = queue.filter(item => queuedIsSmall(item) === small).length;
                while (matching.length + waitingCount < wanted && roster.length < targetCount && currentMonsters.length < 10) {
                    const activeIds = new Set(matching.map(monster => monster.monsterId));
                    const def = this.nextWorldMonsterFromRotation(town.id, pool, activeIds);
                    if (!def) break;
                    const instance = this.createWorldMonsterInstance(town.id, def, now, { spawnAt: now });
                    this.data.worldMonsters.push(instance);
                    currentMonsters.push(instance);
                    roster.push(instance);
                    matching.push(instance);
                    changed = true;
                }
            }

            if (isTide && !currentMonsters.some(m => m.isBeastTideBoss)) {
                const bossPool = localPool.filter(def => !def.small);
                const chosenBoss = bossPool[Math.floor(this.rng() * bossPool.length)] || C.MONSTERS.find(m => m.worldBoss) || { id: 'tide_boss', name: 'Thượng Cổ Hung Thú', realm: 4, element: 'loi', spd: 40 };
                const bms = monsterStats(chosenBoss);
                const tideHp = Math.round(bms.hp * 2.5);
                const tideAtk = Math.round(bms.atk * 1.6);
                const tideDef = Math.round(bms.def * 1.5);
                this.data.worldMonsters.push({
                    uid: newId(), townId: town.id, monsterId: chosenBoss.id,
                    name: `👑 [ĐẠI BOSS THÚ TRIỀU] ${chosenBoss.name}`, icon: '🦁',
                    realm: chosenBoss.realm + 1, element: chosenBoss.element, level: 5, exp: 0, expCap: 9999999,
                    isBoss: true, isWorldBoss: true, isBeastTideBoss: true,
                    hp: tideHp, maxHp: tideHp, atk: tideAtk, def: tideDef, spd: chosenBoss.spd || 40,
                    lockedBy: null, lockedByName: null, lockedUntil: 0, spawnAt: now,
                    combatBalanceVersion: 2,
                });
                changed = true;
            }

            // Giữ tối đa 10 bầy ở mỗi thành trấn, ưu tiên giữ boss đang xuất hiện
            // và không xóa quái đang bị người chơi khóa trong trận.
            const townMonsters = this.data.worldMonsters.filter(m => m.townId === town.id);
            let excess = townMonsters.length - 10;
            if (excess > 0) {
                const removable = townMonsters
                    .filter(m => !m.isScheduledWorldBoss && !m.isBeastTideBoss && !m.isInvasion && (!m.lockedUntil || m.lockedUntil <= now))
                    .sort((a, b) => Number(a.isBoss) - Number(b.isBoss) || (a.spawnAt || 0) - (b.spawnAt || 0));
                for (const monster of removable) {
                    if (excess <= 0) break;
                    this.data.worldMonsters = this.data.worldMonsters.filter(m => m.uid !== monster.uid);
                    excess -= 1;
                    changed = true;
                }
            }
        }
        if (changed) this.touch();
    }

    cultivateWorldMonsters(now = this.now()) {
        const lastCult = this.data.lastMonsterCultivate || 0;
        // Chu kỳ tu luyện và thôn phệ: 60 giây mỗi lần
        const capWorldMonster = monster => {
            const def = C.MONSTER_BY_ID.get(monster.monsterId);
            if (!def) return false;
            const townCap = this.townRealmCap(monster.townId);
            const isBoss = Boolean(def.worldBoss || monster.isWorldBoss || monster.isBeastTideBoss);
            const realmCap = isBoss ? Math.min(townCap, (def.realm || 0) + 2) : townCap;
            const base = monsterStats({ ...def, realm: Math.min(Number(def.realm) || 0, realmCap) }, now);
            const before = [monster.realm, monster.maxHp, monster.hp, monster.atk, monster.def].join(':');
            monster.realm = Math.min(Number(monster.realm) || def.realm, realmCap);
            const hpCap = isBoss ? 1.6 : 1.5;
            const atkCap = isBoss ? 1.1 : 1.25;
            const defCap = isBoss ? 1.8 : 1.5;
            monster.maxHp = Math.min(monster.maxHp || base.hp, Math.round(base.hp * hpCap));
            monster.hp = Math.min(monster.hp || monster.maxHp, monster.maxHp);
            monster.atk = Math.min(monster.atk || base.atk, Math.round(base.atk * atkCap));
            monster.def = Math.min(monster.def || base.def, Math.round(base.def * defCap));
            monster.expCap = Math.max(1, monster.expCap || Math.round(300 * Math.pow(1.4, monster.realm)));
            return before !== [monster.realm, monster.maxHp, monster.hp, monster.atk, monster.def].join(':');
        };

        if (now - lastCult < 60 * 1000) {
            let capped = false;
            for (const monster of this.data.worldMonsters || []) if (capWorldMonster(monster)) capped = true;
            if (capped) this.touch();
            return;
        }
        this.data.lastMonsterCultivate = now;

        // 1. QUÁI THẾ GIỚI TỰ TU LUYỆN & THÔN PHỆ (nếu có quái thế giới)
        if (this.data.worldMonsters && this.data.worldMonsters.length > 0) {
            for (const m of this.data.worldMonsters) {
                capWorldMonster(m); // Chặn quái cũ đã vượt trần cảnh giới và chỉ số của thành trấn.
                m.exp = (m.exp || 0) + Math.round((m.realm + 1) * 35);
                m.expCap = m.expCap || Math.round(300 * Math.pow(1.4, m.realm));
                const bossDef = C.MONSTER_BY_ID.get(m.monsterId);
                const isWorldBoss = Boolean(bossDef?.worldBoss || m.isWorldBoss || m.isBeastTideBoss);
                const realmCap = isWorldBoss ? Math.min(this.townRealmCap(m.townId), (bossDef?.realm || m.realm || 0) + 2) : this.townRealmCap(m.townId);
                if (m.exp >= m.expCap && m.realm < realmCap) {
                    m.exp -= m.expCap;
                    m.level = (m.level || 1) + 1;
                    m.realm += 1;
                    m.expCap = Math.round(m.expCap * 1.5);
                    m.maxHp = Math.round(m.maxHp * 1.25);
                    m.hp = m.maxHp;
                    m.atk = Math.round(m.atk * 1.2);
                    m.def = Math.round(m.def * 1.2);
                    capWorldMonster(m);

                    const cleanName = m.name.replace(/^👑 \[Yêu Vương\] |^⚡ \[Yêu Tướng\] |^🔥 \[Thôn Phệ\] |^⚡ \[Dị Biến Thôn Phệ\] |^👑 \[Yêu Vương Thôn Thiên\] /, '');
                    if (m.realm >= 5 && !m.name.includes('Yêu Vương')) {
                        m.name = `👑 [Yêu Vương] ${cleanName}`;
                    } else if (m.realm >= 3 && !m.name.includes('Yêu Tướng') && !m.name.includes('Yêu Vương')) {
                        m.name = `⚡ [Yêu Tướng] ${cleanName}`;
                    }

                    // Yêu quái đột phá chủ động tập kích người chơi cùng thành để cướp nguyên liệu & trang bị
                    this.handleMonsterBreakthroughAmbush(m.name, m.townId, now, 'world', m.uid);
                }
                if (m.realm >= realmCap) m.exp = Math.min(Math.max(0, m.exp || 0), Math.max(0, m.expCap - 1));
            }

            // Quái thế giới thôn phệ nhau trong cùng thị trấn
            for (const town of C.TOWNS) {
                const spawned = this.data.worldMonsters.filter(m => m.townId === town.id && (m.spawnAt || 0) <= now && (!m.lockedUntil || m.lockedUntil <= now));
                if (spawned.length >= 2) {
                    spawned.sort((a, b) => (b.realm * 1000 + b.atk + b.maxHp) - (a.realm * 1000 + a.atk + a.maxHp));
                    if (this.rng() < 0.35) {
                        const predator = spawned[0];
                        const prey = spawned[spawned.length - 1];
                        if (predator && prey && predator.uid !== prey.uid && (predator.realm > prey.realm || predator.maxHp > prey.maxHp)) {
                            predator.devourCount = (predator.devourCount || 0) + 1;
                            predator.exp = (predator.exp || 0) + Math.round(prey.maxHp * 0.4 + (prey.realm + 1) * 80);
                            predator.maxHp = Math.round(predator.maxHp * 1.2);
                            predator.hp = predator.maxHp;
                            predator.atk = Math.round(predator.atk * 1.15);
                            predator.def = Math.round(predator.def * 1.15);
                            capWorldMonster(predator);

                            const cleanName = predator.name.replace(/^👑 \[Yêu Vương\] |^⚡ \[Yêu Tướng\] |^🔥 \[Thôn Phệ\] |^⚡ \[Dị Biến Thôn Phệ\] |^👑 \[Yêu Vương Thôn Thiên\] /, '');
                            if (predator.devourCount >= 3) {
                                predator.name = `👑 [Yêu Vương Thôn Thiên] ${cleanName}`;
                                predator.isBoss = true;
                            } else if (predator.devourCount >= 2) {
                                predator.name = `⚡ [Dị Biến Thôn Phệ] ${cleanName}`;
                            } else {
                                predator.name = `🔥 [Thôn Phệ] ${cleanName}`;
                            }

                            this.queueWorldMonsterRespawn(prey, now);
                            this.data.worldMonsters = this.data.worldMonsters.filter(x => x.uid !== prey.uid);

                            for (const pl of Object.values(this.data.players || {})) {
                                if (pl.registered && (pl.town || 'thanh_van') === town.id) {
                                    pl.notices = pl.notices || [];
                                    pl.notices.push(`🐺 [YÊU THÚ DỊ BIẾN] ${predator.name} đã thôn phệ ${prey.name} tại ${town.name}! Khí tức bạo trướng, trảm sát sẽ nhận thêm vô số EXP và Linh Thạch!`);
                                }
                            }
                        }
                    }
                }
            }
        }

        // 2. TIỂU YÊU & ĐẠI BOSS THÀNH TRẤN TỰ TU LUYỆN, THÔN PHỆ & TĂNG SỐ LƯỢNG (THÚ TRIỀU)
        this.cultivateTownMonsters(now);

        // 3. QUÁI CỔ ĐỘNG TỰ TU LUYỆN VÀ THÔN PHỆ CHIẾM CHỖ
        this.cultivateDungeonMonsters(now);
        this.touch();
    }

    cultivateTownMonsters(now = this.now()) {
        this.data.monsterStates = this.data.monsterStates || this.data.smallMonsterStates || {};
        this.data.smallMonsterStates = this.data.monsterStates;

        // Khởi tạo trạng thái quái cho tất cả thành trấn để đại boss và tiểu yêu đều có thể tu luyện
        for (const town of C.TOWNS) {
            for (const mId of (town.monsterPool || [])) {
                this.getMonsterState(town.id, mId, now);
            }
        }

        // 1. Tự tu luyện: Tích lũy EXP, đột phá cảnh giới và tăng chỉ số
        for (const [key, st] of Object.entries(this.data.monsterStates)) {
            if (!st) continue;
            const def = C.MONSTER_BY_ID.get(st.id);
            if (!def) continue;

            const townId = st.townId || C.TOWNS.find(town => key.startsWith(`${town.id}_`))?.id || 'thanh_van';
            this.capTownMonsterState(st, def, townId);
            if (st.count <= 0) continue;
            const levelCap = Math.max(0, (this.townRealmCap(townId) - Math.max(0, Number(def.realm) || 0)) * 2);
            if (st.level >= levelCap) continue;

            const isSmall = Boolean(def.small);
            const baseCap = isSmall ? Math.round(150 * Math.pow(1.3, def.realm || 1)) : Math.round(300 * Math.pow(1.3, def.realm || 1));
            st.expCap = Math.max(Number(st.expCap) || 0, baseCap);
            st.exp = (st.exp || 0) + (isSmall ? Math.round((def.realm + 1) * 20) : Math.round((def.realm + 1) * 45));

            if (st.exp >= st.expCap) {
                st.exp -= st.expCap;
                st.level = (st.level || 0) + 1;
                st.expCap = Math.round(st.expCap * 1.35);
                st.bonusHp = (st.bonusHp || 0) + Math.round((def.hp || 500) * 0.2);
                st.bonusAtk = (st.bonusAtk || 0) + Math.round((def.atk || 50) * 0.15);
                st.bonusDef = (st.bonusDef || 0) + Math.round((def.def || 30) * 0.15);

                if (st.level % 2 === 0) {
                    st.realm = Math.min(this.townRealmCap(townId), (def.realm || 1) + Math.floor(st.level / 2));
                }

                this.handleMonsterBreakthroughAmbush(def.name, townId, now, 'town', def.id);
                this.capTownMonsterState(st, def, townId);
            }
        }

        // 2. Thôn phệ nhau trong cùng thành trấn: Đại boss thôn phệ tiểu yêu hoặc boss mạnh thôn phệ boss yếu
        for (const town of C.TOWNS) {
            const activeKeys = Object.keys(this.data.monsterStates).filter(k => k.startsWith(`${town.id}_`) && this.data.monsterStates[k].count > 0);
            if (activeKeys.length >= 2) {
                const townMonsters = activeKeys.map(k => {
                    const st = this.data.monsterStates[k];
                    const def = C.MONSTER_BY_ID.get(st.id) || { realm: 1, hp: 500, atk: 50, def: 30, name: 'Yêu thú', small: true };
                    const power = (st.realm || def.realm) * 1000 + (def.atk + (st.bonusAtk || 0)) * 2 + (def.hp + (st.bonusHp || 0));
                    return { key: k, st, def, power };
                });

                const bosses = townMonsters.filter(m => !m.def.small);
                const preys = townMonsters.filter(m => m.def.small);

                // Ưu tiên: Đại boss thôn phệ tiểu yêu trong thành
                if (bosses.length > 0 && preys.length > 0 && this.rng() < 0.65) {
                    bosses.sort((a, b) => b.power - a.power);
                    preys.sort((a, b) => a.power - b.power);
                    const predator = bosses[0];
                    const prey = preys[0];
                    if (predator && prey && predator.key !== prey.key) {
                        predator.st.devourCount = (predator.st.devourCount || 0) + 1;
                        predator.st.exp = (predator.st.exp || 0) + Math.round(prey.def.realm * 50 + 100);
                        predator.st.bonusHp = (predator.st.bonusHp || 0) + Math.round((prey.def.hp || 500) * 0.25);
                        predator.st.bonusAtk = (predator.st.bonusAtk || 0) + Math.round((prey.def.atk || 50) * 0.20);
                        predator.st.bonusDef = (predator.st.bonusDef || 0) + Math.round((prey.def.def || 30) * 0.20);

                        // Tiểu yêu bị nuốt giảm số lượng
                        prey.st.count = Math.max(0, prey.st.count - 1);
                        if (prey.st.count <= 0) {
                            prey.st.respawnAt = now + SMALL_MONSTER_RESPAWN_MS;
                            prey.st.replaceWith = this.pickReplacementMonster(town.id, prey.def);
                        }

                        for (const pl of Object.values(this.data.players || {})) {
                            if (pl.registered && (pl.town || 'thanh_van') === town.id) {
                                pl.notices = pl.notices || [];
                                pl.notices.push(`🐺 [ĐẠI BOSS THÔN PHỆ] Đại Boss [${predator.def.name}] đã thôn phệ một bầy [${prey.def.name}] tại ${town.name}! Khí tức bạo trướng (Thôn phệ ×${predator.st.devourCount})!`);
                            }
                        }
                    }
                } else if (bosses.length >= 2 && this.rng() < 0.40) {
                    // Đại boss huyết chiến thôn phệ lẫn nhau
                    bosses.sort((a, b) => b.power - a.power);
                    const predator = bosses[0];
                    const prey = bosses[bosses.length - 1];
                    if (predator && prey && predator.key !== prey.key && predator.power > prey.power) {
                        predator.st.devourCount = (predator.st.devourCount || 0) + 1;
                        predator.st.exp = (predator.st.exp || 0) + Math.round(prey.def.realm * 100 + 300);
                        predator.st.bonusHp = (predator.st.bonusHp || 0) + Math.round((prey.def.hp || 1000) * 0.3);
                        predator.st.bonusAtk = (predator.st.bonusAtk || 0) + Math.round((prey.def.atk || 100) * 0.25);
                        predator.st.bonusDef = (predator.st.bonusDef || 0) + Math.round((prey.def.def || 50) * 0.25);

                        prey.st.count = 0;
                        prey.st.respawnAt = now + monsterRespawnMs(prey.def, now);
                        prey.st.replaceWith = this.pickReplacementMonster(town.id, prey.def);

                        for (const pl of Object.values(this.data.players || {})) {
                            if (pl.registered && (pl.town || 'thanh_van') === town.id) {
                                pl.notices = pl.notices || [];
                                pl.notices.push(`👑 [ĐẠI BOSS HUYẾT CHIẾN] Đại Boss [${predator.def.name}] đã thôn phệ diệt sát [${prey.def.name}] tại ${town.name}! Khí tức ngút trời (Thôn phệ ×${predator.st.devourCount})!`);
                            }
                        }
                    }
                }
            }

            // 3. Nếu không bị tiêu diệt: quái không biến mất mà số lượng càng ngày càng tăng lên dẫn tới thú triều
            for (const k of activeKeys) {
                const st = this.data.monsterStates[k];
                if (st && st.count > 0 && (!st.lastGrownAt || now - st.lastGrownAt >= 3 * 60 * 1000)) {
                    st.lastGrownAt = now;
                    st.count = Math.min(10, st.count + 1);
                }
            }
        }
        for (const st of Object.values(this.data.monsterStates)) {
            const def = C.MONSTER_BY_ID.get(st?.id);
            if (def) this.capTownMonsterState(st, def, st.townId || 'thanh_van');
        }
    }

    cultivateDungeonMonsters(now = this.now()) {
        this.data.dungeonsState = this.data.dungeonsState || {};
        const lastCult = this.data.lastDungeonMonsterCultivate || 0;
        const shouldCultivate = now - lastCult >= 5 * 60 * 1000;
        let normalized = false;

        for (const d of C.DUNGEONS) {
            const dState = this.data.dungeonsState[d.id] = this.data.dungeonsState[d.id] || {};
            const oldStagesState = Array.isArray(dState.stagesState) ? dState.stagesState : [];
            if (oldStagesState.length !== d.stages.length) normalized = true;
            dState.stagesState = d.stages.map((stg, i) => {
                const old = oldStagesState[i] || {};
                const canonicalName = canonicalMonsterName(C.MONSTER_BY_ID.get(stg.monsterId)?.name || stg.monsterId);
                if (old.monsterId !== stg.monsterId || old.name !== stg.name || old.baseMonsterName !== canonicalName || old.monsterName !== canonicalName || old.stageIndex !== i) normalized = true;
                return {
                    stageIndex: i,
                    monsterId: stg.monsterId,
                    name: stg.name,
                    baseMonsterName: canonicalName,
                    monsterName: canonicalName,
                    level: 1,
                    exp: 0,
                    hpMul: 1.0,
                    atkMul: 1.0,
                    defMul: 1.0,
                    devoured: [],
                    isVariant: (i === d.stages.length - 1 && this.rng() < 0.20),
                    ...old,
                    stageIndex: i,
                    monsterId: stg.monsterId,
                    name: stg.name,
                    baseMonsterName: canonicalName,
                    monsterName: canonicalName,
                    devoured: Array.isArray(old.devoured) ? old.devoured : [],
                };
            });
            if (!shouldCultivate) continue;

            // Quái cổ động tu luyện & đột phá
            for (let i = 0; i < dState.stagesState.length; i++) {
                const s = dState.stagesState[i];
                const baseName = canonicalMonsterName(C.MONSTER_BY_ID.get(s.monsterId)?.name || s.baseMonsterName || d.stages[i]?.name || s.monsterId || 'Yêu thú');
                s.baseMonsterName = baseName;
                s.devoured = Array.isArray(s.devoured) ? [...new Set(s.devoured.filter(Boolean))].slice(-3) : [];
                // Migrate old saves whose names recursively embedded the full previous prey names.
                s.isMaHoa = Boolean(s.isMaHoa || String(s.monsterName || '').includes('🔥 [Ma Hóa'));
                // Persist only the canonical name; mutation state is rendered separately.
                s.monsterName = baseName;
                const stageMonster = C.MONSTER_BY_ID.get(s.monsterId);
                const stageRealm = Math.max(0, Number(stageMonster?.realm) || 0);
                const maxLevel = Math.max(1, 1 + Math.max(0, this.townRealmCap(d.townId || 'thanh_van') - stageRealm) * 2);
                s.level = clamp(Math.floor(Number(s.level) || 1), 1, maxLevel);
                s.hpMul = clamp(Number(s.hpMul) || 1, 1, Math.pow(1.12, maxLevel - 1));
                const combatGrowthCap = Math.min(1.35, Math.pow(1.04, maxLevel - 1));
                s.atkMul = clamp(Number(s.atkMul) || 1, 1, combatGrowthCap);
                s.defMul = clamp(Number(s.defMul) || 1, 1, combatGrowthCap);
                if (s.level >= maxLevel) s.exp = Math.min(Math.max(0, Number(s.exp) || 0), 499);
                if (s.level < maxLevel) s.exp = (s.exp || 0) + Math.round((s.level || 1) * 60 + 50);
                if (s.exp >= 500 && (s.level || 1) < maxLevel) {
                    s.exp -= 500;
                    s.level = (s.level || 1) + 1;
                    s.hpMul = (s.hpMul || 1.0) * 1.12;
                    s.atkMul = Math.min(combatGrowthCap, (s.atkMul || 1.0) * 1.04);
                    s.defMul = Math.min(combatGrowthCap, (s.defMul || 1.0) * 1.04);
                    this.handleMonsterBreakthroughAmbush(baseName, d.townId || 'thanh_van', now, 'dungeon', d.id);
                }
            }

            // Quái cổ động thôn phệ nhau và chiếm chỗ quái khác (30% cơ hội mỗi chu kỳ)
            if (dState.stagesState.length >= 2 && this.rng() < 0.30) {
                const predatorIdx = dState.stagesState.length - 1;
                const predator = dState.stagesState[predatorIdx];
                const preyChoices = dState.stagesState.slice(0, -1).map((stage, index) => ({ stage, index }))
                    .filter(({ stage }) => !stage.isMaHoa);
                const preyChoice = preyChoices.length ? preyChoices[Math.floor(this.rng() * preyChoices.length)] : null;
                const prey = preyChoice?.stage;

                if (predator && prey && predator !== prey && (predator.devoured || []).length < 3) {
                    predator.devoured = predator.devoured || [];
                    const preyBaseName = C.MONSTER_BY_ID.get(prey.monsterId)?.name || d.stages[preyChoice.index]?.name || prey.monsterId || 'Yêu thú';
                    const predatorBaseName = C.MONSTER_BY_ID.get(predator.monsterId)?.name || d.stages[predatorIdx]?.name || predator.monsterId || 'Yêu thú';
                    predator.devoured.push(preyBaseName);
                    predator.hpMul = (predator.hpMul || 1.0) * 1.15;
                    predator.atkMul = (predator.atkMul || 1.0) * 1.04;
                    predator.defMul = (predator.defMul || 1.0) * 1.04;
                    predator.baseMonsterName = predatorBaseName;
                    predator.monsterName = predatorBaseName;

                    const oldPreyName = preyBaseName;
                    const predatorRealm = Math.max(0, Number(C.MONSTER_BY_ID.get(predator.monsterId)?.realm) || 0);
                    const predatorMaxLevel = Math.max(1, 1 + Math.max(0, this.townRealmCap(d.townId || 'thanh_van') - predatorRealm) * 2);
                    const combatGrowthCap = Math.min(1.35, Math.pow(1.04, predatorMaxLevel - 1));
                    predator.atkMul = Math.min(combatGrowthCap, predator.atkMul || 1);
                    predator.defMul = Math.min(combatGrowthCap, predator.defMul || 1);
                    // Keep the guardian identity fixed so each of the three stages
                    // remains a distinct species; the predator gains the consumed
                    // essence without replacing the prey's catalogue entry.

                    for (const pl of Object.values(this.data.players || {})) {
                        if (pl.registered && (pl.town || 'thanh_van') === d.townId) {
                            pl.notices = pl.notices || [];
                            pl.notices.push(`🏔️ [CỔ ĐỘNG BIẾN HÓA] Tại [${d.name}], [${predatorBaseName}] đã hấp thu linh lực của [${oldPreyName}]! Sức mạnh tăng vọt!`);
                        }
                    }
                }
            }
        }
        if (!shouldCultivate) {
            if (normalized) this.touch();
            return;
        }
        this.data.lastDungeonMonsterCultivate = now;
        this.touch();
    }

    handleMonsterBreakthroughAmbush(monsterName, townId, now, source = 'world', srcId = null) {
        if (!townId) return;
        if (this.rng() < 0.45) return;
        const candidates = Object.values(this.data.players || {}).filter(p => p.registered && !p.isNpc && !this.isHiddenFromPlayers(p) && (p.town || 'thanh_van') === townId && (p.injuredUntil || 0) <= now && this.canReceiveAmbientAttack(p, now));
        if (candidates.length === 0) return;

        const target = candidates[Math.floor(this.rng() * candidates.length)];
        if (!this.recordAmbientAttack(target, now)) return;
        const targetSt = this.stats(target, now);
        const townName = C.TOWN_BY_ID.get(townId)?.name || townId;

        const pRoll = targetSt.power * (0.8 + this.rng() * 0.4);
        const mPowerEst = Math.round(targetSt.power * (0.85 + this.rng() * 0.5));
        const win = pRoll >= mPowerEst;

        target.notices = target.notices || [];

        if (win) {
            const rewardStones = Math.floor(50 + this.rng() * 50);
            const rewardExp = Math.floor(200 + this.rng() * 150);
            target.stones += rewardStones;
            this.realms.addExp(target.userId, rewardExp);

            let dropMsg = '';
            if (this.rng() < 0.08) {
                const bkPills = ['dan_truc_co', 'dan_tay_tuy_hoan', 'dan_pha_chuong', 'mat_yeu_dan', 'mat_van_thiet'];
                const pId = bkPills[Math.floor(this.rng() * bkPills.length)];
                if (pId.startsWith('mat_')) this.addStack(target, 'mat', pId, 1);
                else this.addStack(target, 'cons', pId, 1);
                const pName = C.CONSUMABLE_BY_ID.get(pId)?.name || C.MATERIAL_BY_ID?.get(pId)?.name || pId;
                dropMsg = ` Đoạt được [${pName}] từ yêu quái!`;
            }

            target.notices.push(`🛡️ [ĐẨY LÙI YÊU QUÁI ĐỘT PHÁ] Yêu quái [${monsterName}] sau khi đột phá đã hung hăng tập kích bạn tại ${townName}, nhưng bị bạn dũng cảm đẩy lùi! Thưởng: +${rewardStones} linh thạch, +${rewardExp} EXP.${dropMsg}`);
        } else {
            target.injuredUntil = now + C.RULES.injuryMs;
            target.hp = Math.max(1, Math.round(targetSt.hp * 0.1));

            const stolenNames = [];
            const droppable = this.getDroppableItems(target);
            const numToSteal = Math.min(droppable.length, this.rng() < 0.5 ? 2 : 1);
            for (let i = 0; i < numToSteal; i += 1) {
                const currentDroppable = this.getDroppableItems(target);
                if (!currentDroppable.length) break;
                const item = currentDroppable[Math.min(currentDroppable.length - 1, Math.floor(this.rng() * currentDroppable.length))];
                const stolen = this.takeDroppableItem(target, item);
                if (stolen) stolenNames.push(itemName(stolen));
            }

            if (stolenNames.length === 0) {
                const stolenStones = Math.min(target.stones || 0, Math.floor(30 + this.rng() * 40));
                target.stones = Math.max(0, (target.stones || 0) - stolenStones);
                stolenNames.push(`${stolenStones} linh thạch`);
            }

            target.notices.push(`⚠️ [TẬP KÍCH ĐOẠT BẢO] Yêu quái [${monsterName}] đột phá cảnh giới, bất ngờ tập kích bạn tại ${townName}! Bạn bất địch trọng thương ${Math.round(C.RULES.injuryMs / 60000)} phút, bị quái cướp mất [${stolenNames.join(', ')}]!`);

            for (const pl of candidates) {
                if (String(pl.userId) !== String(target.userId)) {
                    pl.notices = pl.notices || [];
                    pl.notices.push(`🚨 [YÊU QUÁI HOÀNH HÀNH] Yêu quái đột phá [${monsterName}] vừa tập kích tu sĩ ${target.name} tại ${townName} và cướp đoạt bảo vật!`);
                }
            }
        }
        this.touch();
    }

    getBeastTideTownIds(now = this.now()) {
        if ((this.data.beastTideEvent?.endsAt || 0) <= now) return [];
        return [...new Set((this.data.beastTideEvent?.townIds || []).filter(townId => C.TOWN_BY_ID.has(townId)))];
    }

    isBeastTide(townId) {
        return this.getBeastTideTownIds().includes(townId);
    }

    getActiveBeastTides(now = this.now(), activeTownIds = null) {
        const active = [];
        const activeIds = activeTownIds || new Set(this.getBeastTideTownIds(now));
        const states = this.data.monsterStates || this.data.smallMonsterStates || {};
        for (const town of C.TOWNS) {
            if (activeIds.has(town.id)) {
                const wmCount = (this.data.worldMonsters || []).filter(m => m.townId === town.id && (m.spawnAt || 0) <= now).length;
                const townMonsterCount = Object.keys(states)
                    .filter(k => k.startsWith(`${town.id}_`))
                    .reduce((sum, k) => sum + (states[k]?.count || 0), 0);
                const count = Math.max(wmCount, townMonsterCount);
                active.push({
                    townId: town.id,
                    townName: town.name,
                    count,
                    desc: `Yêu thú tụ tập uy hiếp ${town.name} (x2 EXP và Linh thạch khi chi viện)!`
                });
            }
        }
        return active;
    }

    getWorldMonsters(userId, maintain = true) {
        const p = this.player(userId);
        const townId = p?.town || 'thanh_van';
        const now = this.now();
        const partySize = this.huntPartySize(userId);
        if (maintain) {
            this.ensureWorldMonsters(now);
            this.cultivateWorldMonsters(now);
        }
        return (this.data.worldMonsters || [])
            .filter(m => m.townId === townId && (m.spawnAt || 0) <= now && Number(m.hp) > 0)
            .map(m => {
                const isLocked = Boolean(m.lockedBy && m.lockedUntil > now);
                const lockSec = Math.max(0, Math.ceil((m.lockedUntil - now) / 1000));
                const isSpawned = (m.spawnAt || 0) <= now;
                const spawnSec = Math.max(0, Math.ceil(((m.spawnAt || 0) - now) / 1000));
                const encounterHpMul = encounterPartyHpMul({
                    small: !(m.isBoss || m.isWorldBoss || m.isScheduledWorldBoss || m.isBeastTideBoss || C.MONSTER_BY_ID.get(m.monsterId)?.worldBoss),
                    worldBoss: this.isWorldBossInstance(m),
                }, partySize, now);
                return {
                    uid: String(m.uid),
                    townId: m.townId,
                    monsterId: m.monsterId,
                    name: m.name,
                    icon: m.icon || '👹',
                    realm: m.realm,
                    realmName: this.realmName(m.realm),
                    element: m.element,
                    level: m.level || 1,
                    exp: m.exp || 0,
                    expCap: m.expCap,
                    isBoss: Boolean(m.isBoss || m.isWorldBoss || m.isScheduledWorldBoss || m.isBeastTideBoss || C.MONSTER_BY_ID.get(m.monsterId)?.worldBoss),
                    isWorldBoss: this.isWorldBossInstance(m),
                    requiredPartySize: minimumEncounterPartySize({
                        small: !(m.isBoss || m.isWorldBoss || m.isScheduledWorldBoss || m.isBeastTideBoss || C.MONSTER_BY_ID.get(m.monsterId)?.worldBoss),
                        isBoss: Boolean(m.isBoss || m.isScheduledWorldBoss || m.isBeastTideBoss),
                        worldBoss: this.isWorldBossInstance(m),
                    }, now),
                    recommendedPartySize: recommendedEncounterPartySize(now),
                    partySize,
                    partyOk: true,
                    isBeastTideBoss: Boolean(m.isBeastTideBoss),
                    isInvasion: Boolean(m.isInvasion),
                    isInvasion: Boolean(m.isInvasion),
                    hp: Math.round(m.hp * encounterHpMul),
                    maxHp: Math.round((m.maxHp || m.hp) * encounterHpMul),
                    atk: m.atk,
                    def: m.def,
                    spd: m.spd,
                    lockedBy: m.lockedBy,
                    lockedByName: m.lockedByName,
                    isLocked,
                    lockSec,
                    isSpawned,
                    spawnSec,
                    devourCount: m.devourCount || 0,
                };
            });
    }

    worldBossSnapshot(userId, now = this.now()) {
        this.ensureWorldMonsters(now);
        const p = this.player(userId);
        const schedule = this.data.worldBossSchedule || {};
        const activeCount = (this.data.worldMonsters || []).filter(monster =>
            monster.isScheduledWorldBoss && monster.worldBossDate === schedule.date && Number(monster.hp) > 0
        ).length;
        const respawns = (schedule.respawns || []).filter(entry => Number(entry.dueAt) > now);
        return {
            now,
            worldMonsters: this.getWorldMonsters(userId),
            worldBossStatus: {
                date: schedule.date || vnDate(now),
                activeCount,
                maxCount: 20,
                respawningCount: respawns.length,
                nextAt: respawns.length ? Math.min(...respawns.map(entry => Number(entry.dueAt))) : null,
                windowOpen: this.isWorldBossWindow(now),
            },
            worldBossDaily: p ? this.getWorldBossDaily(p, now) : { count: 0, max: 10, remaining: 10 },
        };
    }

    startWorldHunt(userId, monsterUid) {
        const p = this.requirePlayer(userId);
        this.requireNotKnockedOutInDungeon(p);
        const key = String(userId);
        const now = this.now();
        const current = this.battles.get(key);

        p.town = p.town || 'thanh_van';
        this.checkTravelArrival(p, now);
        if (p.traveling) fail('Đang ngự kiếm phi hành, chưa tới nơi.');

        this.ensureWorldMonsters(now);
        const requestedUid = monsterUid == null ? '' : String(monsterUid);
        const wm = (this.data.worldMonsters || []).find(x => String(x?.uid ?? '') === requestedUid);
        if (!wm) fail('Mục tiêu vừa bị hạ hoặc đã rời khỏi bản đồ. Danh sách yêu thú đã cũ, hãy tải lại rồi chọn mục tiêu còn xuất hiện.');
        if ((wm.isBoss || this.isWorldBossInstance(wm)) && (wm.knockedOutUserIds || []).some(id => String(id) === key)) {
            fail('Bạn đã gục ngã khi giao chiến với boss này; không thể tham chiến lại cho đến khi boss bị tiêu diệt.');
        }
        if (current && !current.over) {
            if (current.worldMonsterUid && String(current.worldMonsterUid) === String(wm.uid)) return current;
            fail('Bạn đang trong một trận chiến khác; hãy kết thúc trận hiện tại trước khi khiêu chiến mục tiêu mới.');
        }
        if (wm.townId !== p.town) fail(`Yêu thú đang ở ${C.TOWN_BY_ID.get(wm.townId)?.name}, cần ngự kiếm đến đó.`);

        const worldMonsterDef = C.MONSTER_BY_ID.get(wm.monsterId);
        // Các bản lưu cũ có thể chỉ còn cờ lịch hoặc định nghĩa boss trong
        // catalog; tất cả đều phải tính cùng một lượt Boss Thế Giới.
        const isWorldBoss = this.isWorldBossInstance(wm);

        if (wm.lockedBy && wm.lockedUntil > now && wm.lockedBy !== key) {
            const secs = Math.ceil((wm.lockedUntil - now) / 1000);
            fail(`⚔️ Yêu thú đang giao chiến với tu sĩ [${wm.lockedByName || 'Đạo hữu'}] (còn ${secs}s), không thể vào đánh!`);
        }

        const party = this.partyOf(userId);

        const st = this.stats(p, now);
        if (p.hp == null) p.hp = st.hp;
        if (p.hp <= Math.round(st.hp * 0.15)) {
            fail('Khí huyết quá thấp (dưới 15%), hãy đến Y Quán thành trấn chữa trị hoặc dùng đan dược trước khi xuất chiến!');
        }
        if (now < (p.injuredUntil || 0)) {
            const secs = Math.ceil((p.injuredUntil - now) / 1000);
            fail(`Đang trọng thương, còn ${secs >= 60 ? `${Math.ceil(secs / 60)} phút` : `${secs} giây`}.`);
        }

        const cost = wm.isBoss ? C.RULES.huntCost : C.RULES.smallCost;
        this.syncStamina(p, now);
        if (p.stamina < cost) fail('Không đủ thể lực.');

        let players = [p];
        if (party && party.members.length > 1 && party.leader === key) {
            // Chỉ người sẵn sàng mới được kéo cùng trưởng nhóm.
            const selectedIds = party.members.filter(id => id === key || Boolean(party.ready[String(id)]));
            players = selectedIds.map(id => this.player(id)).filter(Boolean);
            for (const m of players) {
                const name = m.name;
                if (!m.registered) fail(`${name} chưa nhập môn.`);
                this.checkTravelArrival(m, now);
                if (m.traveling || (m.town || 'thanh_van') !== p.town || (party.townId && party.townId !== p.town)) fail(`${name} phải ở cùng thành trấn với trưởng nhóm mới tham chiến.`);
                if (this.activeBattle(m.userId)) fail(`${name} đang trong trận khác.`);
                if (now < (m.injuredUntil || 0)) fail(`${name} đang trọng thương.`);
                const memberMaxHp = this.stats(m, now).hp;
                if (m.hp != null && Number(m.hp) <= Math.round(memberMaxHp * 0.15)) fail(`${name} khí huyết còn dưới 15%, cần chờ hồi phục hoặc dùng đan dược trước khi tham chiến.`);
                this.syncStamina(m, now);
                if (m.stamina < cost) fail(`${name} không đủ thể lực.`);
            }
        }

        if (isWorldBoss) {
            for (const member of players) {
                const daily = this.getWorldBossDaily(member, now);
                if (daily.remaining <= 0) fail(`${member.name} đã dùng hết 10 lượt đánh Boss Thế Giới hôm nay.`);
            }
        }

        for (const m of players) {
            m.stamina -= cost;
            if (m.stamina < C.RULES.staminaMax && m.staminaAt > now) m.staminaAt = now;
            if (isWorldBoss) this.useWorldBossTurn(m, now);
        }

        wm.lockedBy = key;
        wm.lockedByName = p.name;
        wm.lockedUntil = now + C.RULES.battleMaxMs;

        const baseDef = C.MONSTER_BY_ID.get(wm.monsterId) || C.MONSTERS[0];
        const monsterDef = {
            ...baseDef,
            id: wm.monsterId,
            name: wm.name,
            icon: wm.icon || baseDef.icon || '👹',
            realm: wm.realm,
            element: wm.element,
            small: !wm.isBoss,
            worldBoss: isWorldBoss,
            // World-monster instances already carry their authoritative live
            // stats. Recalculating from the catalogue can apply a new day/night
            // HP multiplier between the list and battle screens.
            explicitStats: true,
            hp: wm.hp,
            maxHp: wm.maxHp || wm.hp,
            atk: wm.atk,
            def: wm.def,
            spd: wm.spd,
            devourCount: wm.devourCount || 0,
        };

        const battle = new Battle(this, players, monsterDef, now);
        battle.worldMonsterUid = wm.uid;
        for (const m of players) this.battles.set(String(m.userId), battle);
        if (party && players.length > 1) { party.ready = {}; party.at = now; this._syncPartiesToData(); }
        this.touch();
        return battle;
    }

    // ---- BẢNG NHIỆM VỤ THÀNH TRẤN CÔNG CỘNG (Public Town Bounty Board) ---------
    _isTownQuestEquipment(def) {
        return Boolean(def
            && ['pham', 'hoang', 'huyen'].includes(def.tier)
            && Number(def.drop) >= 0.005
            && !def.elite && !def.unique && !def.bossOnly && !def.worldBossOnly);
    }

    _pickTownQuestEquipment(town, seed = '') {
        const targetTier = (town?.realmMin || 0) <= 1 ? 'pham' : ((town?.realmMin || 0) <= 4 ? 'hoang' : 'huyen');
        const eligible = (C.EQUIPMENT || []).filter(def => this._isTownQuestEquipment(def));
        const pool = eligible.filter(def => def.tier === targetTier);
        const choices = pool.length ? pool : eligible;
        if (!choices.length) return null;
        let hash = 2166136261;
        for (const char of `${town?.id || ''}:${seed}`) hash = Math.imul(hash ^ char.charCodeAt(0), 16777619) >>> 0;
        const index = hash % choices.length;
        return choices[index];
    }

    _createGroupTownBounties(town, now = this.now()) {
        const dungeon = C.DUNGEON_BY_TOWN_ID?.get(town.id) || C.DUNGEONS[0];
        const rewardBase = Math.max(2000, 2000 + town.realmMin * 350);
        return [
            {
                id: `tb_${town.id}_party_boss`, type: 'party_boss', requiresParty: true, partyMin: 2, difficulty: 'hard',
                title: 'Khó · Hợp Lực Trảm Đại Yêu',
                desc: `Tổ đội ít nhất 2 người cùng hạ một đại yêu quanh ${town.name}.`, icon: '⚔️', targetCount: 1, currentCount: 0,
                status: 'available', acceptedBy: null, rewardStones: rewardBase, rewardExp: Math.max(1500, 1500 + town.realmMin * 250), rewardEquipId: this._pickTownQuestEquipment(town, 'party_boss')?.id, createdAt: now,
            },
            {
                id: `tb_${town.id}_party_dungeon`, type: 'party_dungeon', requiresParty: true, partyMin: 2, difficulty: 'hard',
                title: `Khó · Đồng Tâm Phá ${dungeon?.name || 'Cổ Động'}`,
                desc: 'Cùng tổ đội ít nhất 2 người vượt qua một ải Cổ Động.', icon: '🏔️', targetId: dungeon?.id, targetCount: 1, currentCount: 0,
                status: 'available', acceptedBy: null, rewardStones: rewardBase + 500, rewardExp: Math.max(1800, 1800 + town.realmMin * 280), rewardEquipId: this._pickTownQuestEquipment(town, 'party_dungeon')?.id, createdAt: now,
            },
            {
                id: `tb_${town.id}_party_world_boss`, type: 'party_world_boss', requiresParty: true, partyMin: 2, difficulty: 'extreme',
                title: 'Siêu Khó · Viễn Chinh Đại Boss Thế Giới',
                desc: 'Hợp lực cùng tổ đội ít nhất 2 người hạ một Đại Boss Thế Giới.', icon: '🌌', targetCount: 1, currentCount: 0,
                status: 'available', acceptedBy: null, rewardStones: rewardBase * 2, rewardExp: Math.max(3000, 3000 + town.realmMin * 500), createdAt: now,
            },
        ];
    }

    _createDefaultTownBounties(townId, now = this.now()) {
        const town = C.TOWN_BY_ID.get(townId) || C.TOWNS[0];
        const pool = (town.monsterPool || []).map(id => C.MONSTER_BY_ID.get(id)).filter(Boolean);
        const smallMon = pool.find(m => m.small) || pool[0] || { id: 'ty_tho_yeu', name: 'Thỏ Yêu', icon: '🐰' };
        const bossMon = pool.slice().reverse().find(m => !m.small) || pool[pool.length - 1] || { id: 'thanh_lang_vuong', name: 'Thanh Lang Vương', icon: '🐺' };
        const d = C.DUNGEON_BY_TOWN_ID?.get(townId) || C.DUNGEONS[0];

        return [
            {
                id: `tb_${townId}_hunt_small`,
                type: 'hunt_small',
                title: `Tiễu Trừ: ${smallMon.name}`,
                desc: `Xuất thành truy quét 3 yêu quái ${smallMon.name} quấy phá dân chúng quanh ${town.name}.`,
                icon: smallMon.icon || '👾',
                targetId: smallMon.id,
                targetCount: 3,
                currentCount: 0,
                status: 'available',
                acceptedBy: null,
                rewardStones: Math.max(1000, 1000 + town.realmMin * 200),
                rewardExp: Math.max(500, 500 + town.realmMin * 100),
                createdAt: now,
            },
            {
                id: `tb_${townId}_hunt_boss`,
                type: 'hunt_boss',
                title: `Trảm Yêu: ${bossMon.name}`,
                desc: `Tiêu diệt 1 hung thú ${bossMon.name} trấn áp hung uy nơi hiểm địa quanh ${town.name}.`,
                icon: bossMon.icon || '👹',
                targetId: bossMon.id,
                targetCount: 1,
                currentCount: 0,
                status: 'available',
                acceptedBy: null,
                rewardStones: Math.max(1500, 1500 + town.realmMin * 300),
                rewardExp: Math.max(1000, 1000 + town.realmMin * 200),
                rewardEquipId: this._pickTownQuestEquipment(town, 'hunt_boss')?.id,
                createdAt: now,
            },
            {
                id: `tb_${townId}_gather_herb`,
                type: 'gather_mat',
                title: 'Thu Thập Linh Thảo',
                desc: `Cung cấp 2 gốc Linh Thảo để Tiệm Thuốc ${town.name} luyện chế đan dược cứu tế.`,
                icon: '🌿',
                targetId: 'mat_linh_thao',
                targetCount: 2,
                currentCount: 0,
                status: 'available',
                acceptedBy: null,
                rewardStones: Math.max(1000, 1000 + town.realmMin * 150),
                rewardExp: Math.max(500, 500 + town.realmMin * 80),
                createdAt: now,
            },
            {
                id: `tb_${townId}_gather_iron`,
                type: 'gather_mat',
                title: 'Quyên Góp Vẫn Thiết',
                desc: `Góp 2 khối Huyền Thiên Vẫn Thiết để gia cố hộ thành đại trận phòng ngự ${town.name}.`,
                icon: '🔩',
                targetId: 'mat_van_thiet',
                targetCount: 2,
                currentCount: 0,
                status: 'available',
                acceptedBy: null,
                rewardStones: Math.max(1000, 1000 + town.realmMin * 180),
                rewardExp: Math.max(500, 500 + town.realmMin * 100),
                createdAt: now,
            },
            {
                id: `tb_${townId}_gather_core`,
                type: 'gather_mat',
                title: 'Giao Nộp Yêu Đan',
                desc: `Giao nộp 2 viên Yêu Đan lấy từ yêu thú để hỗ trợ hộ vệ thành trì ${town.name}.`,
                icon: '🔮',
                targetId: 'mat_yeu_dan',
                targetCount: 2,
                currentCount: 0,
                status: 'available',
                acceptedBy: null,
                rewardStones: Math.max(1200, 1200 + town.realmMin * 200),
                rewardExp: Math.max(600, 600 + town.realmMin * 120),
                createdAt: now,
            },
            {
                id: `tb_${townId}_patrol`,
                type: 'patrol',
                title: 'Tuần Tra Trấn An',
                desc: `Thực hiện tuần tra dã ngoại quanh ${town.name}, hoàn thành 3 trận giao chiến đảm bảo trật tự.`,
                icon: '🛡️',
                targetCount: 3,
                currentCount: 0,
                status: 'available',
                acceptedBy: null,
                rewardStones: Math.max(1000, 1000 + town.realmMin * 160),
                rewardExp: Math.max(500, 500 + town.realmMin * 100),
                createdAt: now,
            },
            {
                id: `tb_${townId}_dungeon`,
                type: 'dungeon',
                title: `Trinh Sát: ${d?.name || 'Cổ Động'}`,
                desc: `Xâm nhập Cổ Động [${d?.name || ''}] của thành trấn, vượt qua ít nhất 1 ải yêu thú.`,
                icon: '🏔️',
                targetId: d?.id,
                targetCount: 1,
                currentCount: 0,
                status: 'available',
                acceptedBy: null,
                rewardStones: Math.max(1500, 1500 + town.realmMin * 250),
                rewardExp: Math.max(1000, 1000 + town.realmMin * 200),
                rewardEquipId: this._pickTownQuestEquipment(town, 'dungeon')?.id,
                createdAt: now,
            },
            {
                id: `tb_${townId}_world_monster`,
                type: 'world_monster',
                title: 'Hộ Trấn Sát Yêu',
                desc: `Giao chiến hoặc tiêu diệt 1 Yêu Thú Thế Giới đang hoành hành phụ cận ${town.name}.`,
                icon: '🐉',
                targetCount: 1,
                currentCount: 0,
                status: 'available',
                acceptedBy: null,
                rewardStones: Math.max(2000, 2000 + town.realmMin * 300),
                rewardExp: Math.max(1200, 1200 + town.realmMin * 250),
                createdAt: now,
            },
            ...this._createGroupTownBounties(town, now),
        ];
    }

    _createRandomTownBounty(townId, now = this.now()) {
        const town = C.TOWN_BY_ID.get(townId) || C.TOWNS[0];
        const pool = (town.monsterPool || []).map(id => C.MONSTER_BY_ID.get(id)).filter(Boolean);
        const types = ['hunt_small', 'hunt_boss', 'gather_mat', 'patrol', 'dungeon', 'world_monster'];
        const chosenType = types[Math.floor(this.rng() * types.length)];
        const uid = Math.random().toString(36).slice(2, 7);

        if (chosenType === 'hunt_small' && pool.some(m => m.small)) {
            const smallPool = pool.filter(m => m.small);
            const m = smallPool[Math.floor(this.rng() * smallPool.length)];
            return {
                id: `tb_${townId}_hunt_small_${uid}`,
                type: 'hunt_small',
                title: `Truy Quét: ${m.name}`,
                desc: `Xuất thành tiêu diệt 3 con ${m.name} bảo hộ dân cư ${town.name}.`,
                icon: m.icon || '👾',
                targetId: m.id,
                targetCount: 3,
                currentCount: 0,
                status: 'available',
                acceptedBy: null,
                rewardStones: Math.max(1000, 1000 + town.realmMin * 200),
                rewardExp: Math.max(500, 500 + town.realmMin * 100),
                createdAt: now,
            };
        } else if (chosenType === 'hunt_boss' && pool.some(m => !m.small)) {
            const bossPool = pool.filter(m => !m.small);
            const m = bossPool[Math.floor(this.rng() * bossPool.length)];
            return {
                id: `tb_${townId}_hunt_boss_${uid}`,
                type: 'hunt_boss',
                title: `Trảm Yêu: ${m.name}`,
                desc: `Truy lùng và tiêu diệt đại yêu ${m.name} hoành hành gần ${town.name}.`,
                icon: m.icon || '👹',
                targetId: m.id,
                targetCount: 1,
                currentCount: 0,
                status: 'available',
                acceptedBy: null,
                rewardStones: Math.max(1500, 1500 + town.realmMin * 300),
                rewardExp: Math.max(1000, 1000 + town.realmMin * 200),
                rewardEquipId: this._pickTownQuestEquipment(town, `hunt_boss:${uid}`)?.id,
                createdAt: now,
            };
        } else if (chosenType === 'gather_mat') {
            const mats = [
                { id: 'mat_linh_thao', name: 'Linh Thảo', icon: '🌿' },
                { id: 'mat_van_thiet', name: 'Vẫn Thiết', icon: '🔩' },
                { id: 'mat_yeu_dan', name: 'Yêu Đan', icon: '🔮' },
            ];
            const mat = mats[Math.floor(this.rng() * mats.length)];
            return {
                id: `tb_${townId}_gather_${mat.id}_${uid}`,
                type: 'gather_mat',
                title: `Thu Gom: ${mat.name}`,
                desc: `Nộp 2 phần ${mat.name} cho phường khố ${town.name}.`,
                icon: mat.icon,
                targetId: mat.id,
                targetCount: 2,
                currentCount: 0,
                status: 'available',
                acceptedBy: null,
                rewardStones: Math.max(1000, 1000 + town.realmMin * 180),
                rewardExp: Math.max(500, 500 + town.realmMin * 100),
                createdAt: now,
            };
        } else if (chosenType === 'dungeon') {
            const d = C.DUNGEON_BY_TOWN_ID?.get(townId) || C.DUNGEONS[0];
            return {
                id: `tb_${townId}_dungeon_${uid}`,
                type: 'dungeon',
                title: `Thám Cổ Động: ${d?.name || 'Bí Cảnh'}`,
                desc: `Tiến vào Cổ Động [${d?.name || 'Bí Cảnh'}] trảm sát 1 ải yêu thú.`,
                icon: '🏔️',
                targetId: d?.id,
                targetCount: 1,
                currentCount: 0,
                status: 'available',
                acceptedBy: null,
                rewardStones: Math.max(1500, 1500 + town.realmMin * 250),
                rewardExp: Math.max(1000, 1000 + town.realmMin * 200),
                rewardEquipId: this._pickTownQuestEquipment(town, `dungeon:${uid}`)?.id,
                createdAt: now,
            };
        } else if (chosenType === 'world_monster') {
            return {
                id: `tb_${townId}_world_monster_${uid}`,
                type: 'world_monster',
                title: 'Hộ Thành Sát Yêu',
                desc: `Trảm sát 1 yêu thú thế giới xuất hiện tại ${town.name}.`,
                icon: '🐉',
                targetCount: 1,
                currentCount: 0,
                status: 'available',
                acceptedBy: null,
                rewardStones: Math.max(2000, 2000 + town.realmMin * 300),
                rewardExp: Math.max(1200, 1200 + town.realmMin * 250),
                createdAt: now,
            };
        } else {
            return {
                id: `tb_${townId}_patrol_${uid}`,
                type: 'patrol',
                title: 'Tuần Tra Trấn Ngoại',
                desc: `Tuần phòng biên giới ${town.name}, giao tranh thắng 3 trận.`,
                icon: '🛡️',
                targetCount: 3,
                currentCount: 0,
                status: 'available',
                acceptedBy: null,
                rewardStones: Math.max(1000, 1000 + town.realmMin * 150),
                rewardExp: Math.max(500, 500 + town.realmMin * 100),
                createdAt: now,
            };
        }
    }

    _replenishTownBounties(board, townId) {
        if (!board || !Array.isArray(board.bounties)) return;
        while (board.bounties.length < 8) {
            const b = this._createRandomTownBounty(townId);
            board.bounties.push(b);
        }
    }

    ensureTownBountyBoard(p, townId = (p?.town || 'thanh_van'), now = this.now()) {
        const today = vnDate(now);
        this.data.townBountyBoards = this.data.townBountyBoards || {};
        let board = this.data.townBountyBoards[townId];

        if (!board || board.date !== today) {
            const town = C.TOWN_BY_ID.get(townId) || C.TOWNS[0];
            board = {
                date: today,
                townId,
                townName: town.name,
                bounties: this._createDefaultTownBounties(townId, now),
                groupMissionsSeeded: true,
            };
            this.data.townBountyBoards[townId] = board;
        }

        if (!board.groupMissionsSeeded) {
            const types = new Set((board.bounties || []).map(b => b.type));
            board.bounties.push(...this._createGroupTownBounties(C.TOWN_BY_ID.get(townId) || C.TOWNS[0], now).filter(b => !types.has(b.type)));
            board.groupMissionsSeeded = true;
        }

        // Add capped, non-rare equipment rewards to boards persisted before this update.
        const town = C.TOWN_BY_ID.get(townId) || C.TOWNS[0];
        for (const bounty of board.bounties || []) {
            if (bounty.rewardEquipId || !['hunt_boss', 'dungeon', 'party_boss', 'party_dungeon'].includes(bounty.type)) continue;
            bounty.rewardEquipId = this._pickTownQuestEquipment(town, bounty.id)?.id || null;
        }

        // Tự động thu hồi nhiệm vụ đã nhận quá 4 tiếng mà không hoàn thành để người khác nhận
        for (const b of board.bounties) {
            if (b.status === 'accepted' && b.acceptedBy?.at && (now - b.acceptedBy.at > 4 * 3600 * 1000)) {
                b.status = 'available';
                b.acceptedBy = null;
                b.currentCount = 0;
                b.claimedBy = [];
                delete b.lastBattleId;
            }
        }

        // Tự động bổ sung nhiệm vụ mới nếu số lượng nhiệm vụ trên bảng < 8
        this._replenishTownBounties(board, townId);

        // Sync tiến độ thu thập nguyên liệu cho người chơi nếu đang nhận
        if (p) this._syncTownBoardGatherProgress(p, townId);

        return board;
    }

    _syncTownBoardGatherProgress(p, townId) {
        const board = this.ensureTownBountyBoard(null, townId);
        if (!board || !Array.isArray(board.bounties)) return;
        const uidStr = String(p.userId);
        for (const b of board.bounties) {
            if (b.type === 'gather_mat' && b.status === 'accepted' && String(b.acceptedBy?.userId) === uidStr) {
                const count = (p.items || []).filter(it => it.kind === 'mat' && it.id === b.targetId && it.place === 'bag').reduce((sum, it) => sum + (it.qty || 1), 0);
                b.currentCount = count;
                if (b.currentCount >= b.targetCount) {
                    b.status = 'completed';
                }
            }
        }
    }

    trackTownBoardProgress(p, battle) {
        const townId = p.town || 'thanh_van';
        const board = this.ensureTownBountyBoard(null, townId);
        if (!board || !Array.isArray(board.bounties)) return;

        const uidStr = String(p.userId);
        let changed = false;
        for (const b of board.bounties) {
            if (b.requiresParty) {
                const acceptedMembers = (b.acceptedBy?.memberIds || []).map(String);
                if (b.status !== 'accepted' || !acceptedMembers.includes(uidStr) || b.lastBattleId === battle.id) continue;
                const battleMembers = (battle.userIds || []).map(String);
                if (acceptedMembers.length < (b.partyMin || 2) || !acceptedMembers.every(id => battleMembers.includes(id))) continue;
                const qualifies = b.type === 'party_boss'
                    ? !battle.monsterDef?.small
                    : b.type === 'party_dungeon'
                        ? Boolean(battle.isDungeon)
                        : b.type === 'party_world_boss'
                            ? Boolean(battle.worldMonsterUid && battle.monsterDef?.worldBoss)
                            : false;
                if (!qualifies) continue;
                b.lastBattleId = battle.id;
                b.currentCount = Math.min(b.targetCount, (b.currentCount || 0) + 1);
                if (b.currentCount >= b.targetCount) {
                    b.status = 'completed';
                    for (const id of acceptedMembers) {
                        const member = this.player(id);
                        if (member) {
                            member.notices ||= [];
                            member.notices.push(`🏆 Tổ đội hoàn thành [${b.title}]! Hãy lĩnh thưởng tại bảng nhiệm vụ thành trấn.`);
                        }
                    }
                }
                changed = true;
                continue;
            }
            // Chỉ người chơi đã tiếp nhận nhiệm vụ mới được tính tiến độ (người chơi không làm hộ của nhau)
            if (b.status !== 'accepted' || String(b.acceptedBy?.userId) !== uidStr) continue;

            if (b.type === 'hunt_small' && battle.monsterDef?.small) {
                if (!b.targetId || b.targetId === battle.monsterDef.id) {
                    b.currentCount = Math.min(b.targetCount, (b.currentCount || 0) + 1);
                    if (b.currentCount >= b.targetCount) {
                        b.status = 'completed';
                        p.notices = p.notices || [];
                        p.notices.push(`🏆 Nhiệm vụ thành trấn [${b.title}] đã hoàn thành! Hãy đến Bảng Nhiệm Vụ nhận thưởng.`);
                    }
                    changed = true;
                }
            } else if (b.type === 'hunt_boss' && !battle.monsterDef?.small) {
                if (!b.targetId || b.targetId === battle.monsterDef.id) {
                    b.currentCount = Math.min(b.targetCount, (b.currentCount || 0) + 1);
                    if (b.currentCount >= b.targetCount) {
                        b.status = 'completed';
                        p.notices = p.notices || [];
                        p.notices.push(`🏆 Nhiệm vụ thành trấn [${b.title}] đã hoàn thành! Hãy đến Bảng Nhiệm Vụ nhận thưởng.`);
                    }
                    changed = true;
                }
            } else if (b.type === 'patrol') {
                b.currentCount = Math.min(b.targetCount, (b.currentCount || 0) + 1);
                if (b.currentCount >= b.targetCount) {
                    b.status = 'completed';
                    p.notices = p.notices || [];
                    p.notices.push(`🏆 Nhiệm vụ tuần tra [${b.title}] đã hoàn thành! Hãy đến Bảng Nhiệm Vụ nhận thưởng.`);
                }
                changed = true;
            } else if (b.type === 'dungeon' && battle.isDungeon) {
                b.currentCount = Math.min(b.targetCount, (b.currentCount || 0) + 1);
                if (b.currentCount >= b.targetCount) {
                    b.status = 'completed';
                    p.notices = p.notices || [];
                    p.notices.push(`🏆 Nhiệm vụ cổ động [${b.title}] đã hoàn thành! Hãy đến Bảng Nhiệm Vụ nhận thưởng.`);
                }
                changed = true;
            } else if (b.type === 'world_monster' && battle.worldMonsterUid) {
                b.currentCount = Math.min(b.targetCount, (b.currentCount || 0) + 1);
                if (b.currentCount >= b.targetCount) {
                    b.status = 'completed';
                    p.notices = p.notices || [];
                    p.notices.push(`🏆 Nhiệm vụ thế giới [${b.title}] đã hoàn thành! Hãy đến Bảng Nhiệm Vụ nhận thưởng.`);
                }
                changed = true;
            }
        }
        if (changed) this.touch();
    }

    getPlayerActiveTownBountiesCount(userId) {
        const uidStr = String(userId);
        let count = 0;
        const boards = this.data.townBountyBoards || {};
        for (const tId of Object.keys(boards)) {
            const bList = boards[tId]?.bounties || [];
            for (const b of bList) {
                if (b.status === 'accepted' && (String(b.acceptedBy?.userId) === uidStr || (b.acceptedBy?.memberIds || []).map(String).includes(uidStr))) {
                    count++;
                }
            }
        }
        return count;
    }

    getTownBountyBoard(userId, townId = null) {
        const p = this.requirePlayer(userId);
        const tId = townId || p.town || 'thanh_van';
        const board = this.ensureTownBountyBoard(p, tId);
        const town = C.TOWN_BY_ID.get(tId) || C.TOWNS[0];

        const uidStr = String(userId);
        const bounties = board.bounties || [];
        const myBounties = bounties.filter(b => String(b.acceptedBy?.userId) === uidStr || (b.acceptedBy?.memberIds || []).map(String).includes(uidStr));
        const completedCount = myBounties.filter(b => b.status === 'completed' || b.status === 'claimed').length;
        const availableCount = bounties.filter(b => b.status === 'available').length;
        const myActiveCount = myBounties.filter(b => b.status === 'accepted').length;

        for (const bounty of bounties) {
            const def = bounty.rewardEquipId ? C.EQUIP_BY_ID.get(bounty.rewardEquipId) : null;
            bounty.rewardEquipment = this._isTownQuestEquipment(def) ? {
                id: def.id,
                name: def.name,
                icon: def.icon || '🗡️',
                tierName: C.TIER?.[def.tier]?.name || def.tier,
            } : null;
        }

        return {
            townId: tId,
            townName: town.name,
            townIcon: town.icon || '🏡',
            bounties,
            completedCount,
            availableCount,
            myActiveCount,
            totalCount: bounties.length,
        };
    }

    acceptTownBounty(userId, bountyId) {
        const p = this.requirePlayer(userId);
        const tId = p.town || 'thanh_van';
        const board = this.ensureTownBountyBoard(p, tId);
        const b = (board.bounties || []).find(x => x.id === bountyId);
        if (!b) fail('Không tìm thấy nhiệm vụ này trên bảng nhiệm vụ thành trấn.');

        const uidStr = String(userId);
        if (b.requiresParty && b.status === 'completed') fail('Tổ đội đã hoàn thành nhiệm vụ này; các thành viên hãy lĩnh thưởng.');
        if (b.status === 'accepted') {
            if (String(b.acceptedBy?.userId) === uidStr) {
                fail('Đạo hữu đã tiếp nhận nhiệm vụ này rồi.');
            } else {
                fail(`Nhiệm vụ này đã được tu sĩ [${b.acceptedBy?.name || 'khác'}] tiếp nhận trước! Người chơi không thể nhận nhiệm vụ của nhau.`);
            }
        }
        if (b.status === 'completed' && String(b.acceptedBy?.userId) === uidStr) {
            fail('Nhiệm vụ này đã hoàn thành, hãy bấm Lĩnh thưởng!');
        }

        let memberIds = [uidStr];
        let partyId = null;
        if (b.requiresParty) {
            const party = this.partyOf(userId);
            if (!party || party.leader !== uidStr) fail('Nhiệm vụ khó và siêu khó chỉ do trưởng nhóm tiếp nhận.');
            if (party.members.length < (b.partyMin || 2)) fail(`Cần tổ đội ít nhất ${b.partyMin || 2} người để nhận nhiệm vụ.`);
            if ((party.townId || p.town) !== tId) fail('Tổ đội phải đang ở cùng thành trấn mới nhận được nhiệm vụ.');
            memberIds = [...party.members];
            for (const id of memberIds) {
                const member = this.player(id);
                if (!member || !member.registered) fail('Mọi thành viên trong tổ đội phải đã nhập môn.');
                this.checkTravelArrival(member, this.now());
                if (member.traveling || (member.town || 'thanh_van') !== tId) fail('Mọi thành viên phải cùng thành trấn và không di chuyển.');
                if (String(id) !== uidStr && !party.ready[String(id)]) fail(`${member.name} chưa bấm Sẵn sàng.`);
                if (this.getPlayerActiveTownBountiesCount(id) >= 3) fail(`${member.name} đã nhận đủ 3 nhiệm vụ thành trấn.`);
            }
            partyId = party.id;
        } else if (this.getPlayerActiveTownBountiesCount(userId) >= 3) {
            fail('Đạo hữu chỉ có thể tiếp nhận tối đa 3 nhiệm vụ thành trấn cùng lúc!');
        }

        b.status = 'accepted';
        b.acceptedBy = {
            userId: uidStr,
            name: p.fullName || p.name || 'Tu sĩ',
            at: this.now(),
            ...(b.requiresParty ? { memberIds, partyId } : {}),
        };
        if (b.requiresParty) b.claimedBy = [];
        this._syncTownBoardGatherProgress(p, tId);
        this.touch();
        return {
            success: true,
            bounty: b,
            message: 'Đã tiếp nhận nhiệm vụ [' + b.title + ']! Chúc đạo hữu hoàn thành xuất sắc.',
        };
    }

    claimTownBounty(userId, bountyId) {
        const p = this.requirePlayer(userId);
        const tId = p.town || 'thanh_van';
        const board = this.ensureTownBountyBoard(p, tId);
        const b = (board.bounties || []).find(x => x.id === bountyId);
        if (!b) fail('Không tìm thấy nhiệm vụ này trên bảng nhiệm vụ thành trấn.');

        const rewardEquipDef = b.rewardEquipId ? C.EQUIP_BY_ID.get(b.rewardEquipId) : null;
        if (b.rewardEquipId && !this._isTownQuestEquipment(rewardEquipDef)) fail('Phần thưởng trang bị của nhiệm vụ không hợp lệ.');
        if (rewardEquipDef) {
            const bagFull = this.countPlace(p, 'bag') >= this.capacity('bag', p);
            const khoFull = this.countPlace(p, 'kho') >= this.capacity('kho', p);
            if (bagFull && khoFull) fail('Túi và kho đều đầy. Hãy dọn ít nhất một ô rồi lĩnh thưởng trang bị.');
            if (!this.canCreate('item', rewardEquipDef.id)) fail('Trang bị phần thưởng đã hết số lượng lưu hành; hãy thử lại khi có hàng.');
        }

        const uidStr = String(userId);
        if (b.requiresParty) {
            const memberIds = (b.acceptedBy?.memberIds || []).map(String);
            if (!memberIds.includes(uidStr)) fail('Nhiệm vụ tổ đội này không dành cho bạn.');
            if (!['completed', 'claimed'].includes(b.status) && (b.currentCount || 0) < b.targetCount) fail(`Tổ đội chưa hoàn thành mục tiêu (${b.currentCount || 0}/${b.targetCount}).`);
            b.claimedBy ||= [];
            if (b.claimedBy.map(String).includes(uidStr)) fail('Bạn đã lĩnh thưởng nhiệm vụ này rồi.');
            const gearItem = rewardEquipDef ? this.addEquip(p, rewardEquipDef, this.now()) : null;
            if (rewardEquipDef && !gearItem) fail('Không thể cất trang bị thưởng. Hãy dọn túi hoặc kho rồi lĩnh lại.');
            p.stones = (p.stones || 0) + b.rewardStones;
            this.realms.addExp(userId, b.rewardExp);
            b.claimedBy.push(uidStr);
            const allClaimed = memberIds.every(id => b.claimedBy.map(String).includes(id));
            if (allClaimed) {
                b.status = 'claimed';
                board.bounties = board.bounties.filter(x => x.id !== bountyId);
                this._replenishTownBounties(board, tId);
            } else {
                b.status = 'completed';
            }
            this.touch();
            return {
                success: true,
                stones: b.rewardStones,
                exp: b.rewardExp,
                equipment: gearItem ? rewardEquipDef.name : null,
                message: `Đã lĩnh phần thưởng tổ đội [${b.title}]: +${b.rewardStones} linh thạch, +${b.rewardExp} EXP${gearItem ? `, nhận trang bị [${rewardEquipDef.name}]` : ''}.${allClaimed ? ' Nhiệm vụ đã được toàn đội lĩnh đủ.' : ' Các thành viên còn lại có thể tự lĩnh thưởng.'}`,
            };
        }
        if (b.status === 'available') fail('Chưa nhận nhiệm vụ, hãy bấm Tiếp nhận trước.');
        if (String(b.acceptedBy?.userId) !== uidStr) {
            fail('Nhiệm vụ này không phải do đạo hữu tiếp nhận!');
        }

        if (b.type === 'gather_mat') {
            const hasQty = (p.items || []).filter(it => it.kind === 'mat' && it.id === b.targetId && it.place === 'bag').reduce((sum, it) => sum + (it.qty || 1), 0);
            if (hasQty < b.targetCount) {
                fail('Chưa đủ vật phẩm (' + hasQty + '/' + b.targetCount + ' ' + (C.MATERIAL_BY_ID?.get(b.targetId)?.name || b.targetId) + ').');
            }
            let remain = b.targetCount;
            for (const it of p.items) {
                if (remain <= 0) break;
                if (it.kind === 'mat' && it.id === b.targetId && it.place === 'bag') {
                    const take = Math.min(remain, it.qty);
                    this.consume(p, it, take);
                    remain -= take;
                }
            }
        } else {
            if (b.currentCount < b.targetCount) {
                fail('Chưa hoàn thành mục tiêu nhiệm vụ (' + b.currentCount + '/' + b.targetCount + ').');
            }
        }

        // Lĩnh thưởng
        const gearItem = rewardEquipDef ? this.addEquip(p, rewardEquipDef, this.now()) : null;
        if (rewardEquipDef && !gearItem) fail('Không thể cất trang bị thưởng. Hãy dọn túi hoặc kho rồi lĩnh lại.');
        b.status = 'claimed';
        p.stones = (p.stones || 0) + b.rewardStones;
        this.realms.addExp(userId, b.rewardExp);

        // Sau khi làm xong sẽ biến mất khỏi bảng nhiệm vụ
        board.bounties = board.bounties.filter(x => x.id !== bountyId);

        // Tự động sinh nhiệm vụ mới sau đó
        this._replenishTownBounties(board, tId);
        this.touch();

        return {
            success: true,
            stones: b.rewardStones,
            exp: b.rewardExp,
            equipment: gearItem ? rewardEquipDef.name : null,
            message: 'Hoàn thành nhiệm vụ [' + b.title + ']! Nhận +' + b.rewardStones + ' linh thạch, +' + b.rewardExp + ' EXP' + (gearItem ? ', nhận trang bị [' + rewardEquipDef.name + ']' : '') + '! (Nhiệm vụ đã hoàn tất và biến mất khỏi bảng)',
        };
    }

    // ---- Nhiệm Vụ Thành Trấn & Tông Môn -------------------------------------
    _createSectBountyTasks(p, town, today, previousTasks = []) {
        const sectId = p.sectId;
        const eligible = (town.monsterPool || []).map(id => C.MONSTER_BY_ID.get(id)).filter(Boolean);
        const pool = eligible.filter(monster => monster.small).length
            ? eligible.filter(monster => monster.small)
            : eligible;
        const usedMonsterIds = new Set(previousTasks.map(task => task?.monsterId).filter(Boolean));
        const titles = ['Trừ Yêu Khẩn Cấp', 'Tuần Sơn Tróc Yêu', 'Đại Săn Tông Môn'];
        const targets = [3, 6, 10];
        return titles.map((title, index) => {
            const previous = previousTasks[index];
            if (previous && previous.sectId === sectId) {
                if (!previous.id) previous.id = `sect_${today}_${index + 1}`;
                return previous;
            }
            const candidates = pool.filter(monster => !usedMonsterIds.has(monster.id));
            const choices = candidates.length ? candidates : pool;
            const monster = choices[Math.floor(this.rng() * Math.max(1, choices.length))] || eligible[0];
            if (monster) usedMonsterIds.add(monster.id);
            const realm = Number(town.realmMin) || 0;
            const multiplier = index + 1;
            return {
                id: `sect_${today}_${index + 1}`,
                title,
                sectId,
                monsterId: monster?.id || '',
                monsterName: monster?.name || 'Yêu thú',
                icon: monster?.icon || '👹',
                targetCount: targets[index],
                currentCount: 0,
                rewardStones: (120 + realm * 30) * multiplier,
                rewardExp: (120 + realm * 40) * multiplier,
                rewardContrib: 30 * multiplier,
                completed: false,
                claimed: false,
            };
        });
    }

    ensureBounties(p, now = this.now()) {
        const today = vnDate(now);
        p.town = p.town || 'thanh_van';
        if (p.bounties?.date === today) {
            if (!p.sectId) {
                p.bounties.sect = null;
                p.bounties.sectTasks = [];
                return;
            }
            const existingTasks = Array.isArray(p.bounties.sectTasks) && p.bounties.sectTasks.length
                ? p.bounties.sectTasks
                : (p.bounties.sect ? [p.bounties.sect] : []);
            if (existingTasks.length !== 3 || existingTasks.some(task => task?.sectId !== p.sectId)) {
                p.bounties.sectTasks = this._createSectBountyTasks(p, C.TOWN_BY_ID.get(p.town) || C.TOWNS[0], today, existingTasks);
            }
            p.bounties.sect = p.bounties.sectTasks[0] || null;
            return;
        }
        const town = C.TOWN_BY_ID.get(p.town) || C.TOWNS[0];
        const pool = town.monsterPool;
        const targetMonsterId = pool[Math.floor(this.rng() * pool.length)];
        const mDef = C.MONSTER_BY_ID.get(targetMonsterId);
        p.bounties = {
            date: today,
            town: {
                townId: town.id,
                monsterId: targetMonsterId,
                monsterName: mDef?.name || 'Yêu thú',
                icon: mDef?.icon || '👹',
                targetCount: 2,
                currentCount: 0,
                rewardStones: 80 + town.realmMin * 20,
                rewardExp: 80 + town.realmMin * 30,
                completed: false,
                claimed: false,
            },
            sect: null,
            sectTasks: [],
        };
        if (p.sectId) {
            p.bounties.sectTasks = this._createSectBountyTasks(p, town, today);
            p.bounties.sect = p.bounties.sectTasks[0] || null;
        }
    }

    trackBountyKill(p, monsterId) {
        if (!p.bounties) return;
        if (p.bounties.town && !p.bounties.town.completed && p.bounties.town.monsterId === monsterId) {
            p.bounties.town.currentCount = (p.bounties.town.currentCount || 0) + 1;
            if (p.bounties.town.currentCount >= p.bounties.town.targetCount) {
                p.bounties.town.completed = true;
                p.notices = (p.notices || []).concat([`Nhiệm vụ Thành Trấn hoàn thành! Nhận thưởng tại giao diện Săn Yêu.`]);
            }
        }
        const sectTasks = Array.isArray(p.bounties.sectTasks) && p.bounties.sectTasks.length
            ? p.bounties.sectTasks
            : (p.bounties.sect ? [p.bounties.sect] : []);
        for (const task of sectTasks) {
            if (task && !task.completed && !task.claimed && task.monsterId === monsterId) {
                task.currentCount = (task.currentCount || 0) + 1;
                if (task.currentCount >= task.targetCount) {
                    task.completed = true;
                    p.notices = (p.notices || []).concat([`Nhiệm vụ Tông Môn [${task.title || task.monsterName}] hoàn thành! Nhận thưởng tại giao diện Săn Yêu.`]);
                }
            }
        }
    }

    getTownBounties(userId) {
        const p = this.requirePlayer(userId);
        const now = this.now();
        this.ensureBounties(p, now);
        return p.bounties;
    }

    claimBounty(userId, type, taskId = null) {
        const p = this.requirePlayer(userId);
        const now = this.now();
        this.ensureBounties(p, now);
        const sectTasks = Array.isArray(p.bounties?.sectTasks) ? p.bounties.sectTasks : [];
        const b = type === 'sect'
            ? (taskId ? sectTasks.find(task => String(task.id) === String(taskId)) : (p.bounties?.sect || sectTasks[0]))
            : p.bounties?.town;
        if (!b) fail('Không có nhiệm vụ này.');
        if (!b.completed) fail(`Chưa hoàn thành nhiệm vụ (${b.currentCount}/${b.targetCount} ${b.monsterName}).`);
        if (b.claimed) fail('Nhiệm vụ này đã nhận thưởng hôm nay rồi.');
        b.claimed = true;
        p.stones += b.rewardStones;
        this.realms.addExp(userId, b.rewardExp);
        if (type === 'sect') {
            if (b.rewardContrib && p.sectId && this.sects[p.sectId]) {
                p.sectContrib = (p.sectContrib || 0) + b.rewardContrib;
            }
            if (Array.isArray(p.bounties.sectTasks) && p.bounties.sectTasks.length) {
                p.bounties.sect = p.bounties.sectTasks[0];
            }
        }
        this.touch();
        return {
            success: true,
            stones: b.rewardStones,
            exp: b.rewardExp,
            message: `Nhận thưởng [${b.title || b.monsterName}] thành công: +${b.rewardStones} linh thạch, +${b.rewardExp} EXP${b.rewardContrib ? `, +${b.rewardContrib} cống hiến` : ''}!`,
        };
    }

    // Một bộ chỉ số duy nhất được dùng cho mọi trận NPC. Máu được tăng để
    // kéo dài trận; công/thủ chỉ nhận thêm 10% cố định ngoài trang bị/cảnh giới.
    npcCombatStats(npc) {
        const realm = Number(npc.realm || npc.baseRealm || 0);
        const gearTier = Number(npc.gearTier || 0);
        const k = Math.pow(C.REALM_GROWTH, realm);
        const mon = C.MON[npc.mon || 'kiem'] || C.MON.kiem;
        return {
            hp: Math.round(C.BASE_STATS.hp * k * (mon.mul?.hp || 1) * (1 + gearTier * 0.08) * 2),
            mp: Math.max(1, Math.round(C.BASE_STATS.mp * k)),
            atk: Math.round(C.BASE_STATS.atk * k * (mon.mul?.atk || 1) * (1 + gearTier * 0.12) * 1.10),
            def: Math.round(C.BASE_STATS.def * k * (mon.mul?.def || 1) * (1 + gearTier * 0.10) * 1.10),
            spd: Math.round(C.BASE_STATS.spd * k * (mon.mul?.spd || 1)),
            sense: Math.round(C.BASE_STATS.sense * k * (mon.mul?.sense || 1)),
            crit: 0.15,
        };
    }

    // Tong chiến dùng lực chiến làm phép so nhanh; nó được tính từ cùng bộ
    // công/thủ/máu của NPC trong trận khiêu chiến để hai nơi không lệch nhau.
    calcNpcPower(npc) {
        const realm = Number(npc.realm || npc.baseRealm || 0);
        const stats = this.npcCombatStats(npc);
        const realmBasePower = Math.round(realm * 150 + Math.pow(realm, 2) * 20);
        return Math.round(stats.atk * 2 + stats.def * 1.5 + stats.hp / 10 + stats.spd + realmBasePower);
    }

    npcRealmExpCap(realm) {
        const npcRealm = Math.max(0, Number(realm) || 0);
        return npcRealm <= 30
            ? Math.round(1000 + ((5000000 - 1000) * npcRealm / 30))
            : Math.round(5000000 + ((10000000 - 5000000) * (Math.min(65, npcRealm) - 30) / 35));
    }

    npcDuelDefense(baseDef, npcAtk, playerAtk) {
        // Retained for older callers; defense no longer scales against the
        // opponent, which previously made identical NPCs vary by match.
        return Math.max(1, Math.round(Number(baseDef) || 1));
    }

    npcInventoryQuantity(npc, kind, id) {
        return [...(npc?.bag || []), ...(npc?.warehouse || [])]
            .filter(item => item.kind === kind && item.id === id)
            .reduce((sum, item) => sum + Math.max(1, Math.floor(Number(item.qty) || 1)), 0);
    }

    addNpcInventoryItem(npc, kind, id, qty = 1, place = 'warehouse', now = this.now()) {
        if (!npc || !kind || !id || qty <= 0) return;
        npc.bag = Array.isArray(npc.bag) ? npc.bag : [];
        npc.warehouse = Array.isArray(npc.warehouse) ? npc.warehouse : [];
        const storage = place === 'bag' ? npc.bag : npc.warehouse;
        const stack = storage.find(item => item.kind === kind && item.id === id);
        if (stack) stack.qty = Math.max(1, Number(stack.qty) || 1) + Math.floor(qty);
        else storage.push({ uid: newId(), kind, id, qty: Math.floor(qty), place, at: now });
    }

    npcBreakthroughRequirements(npc, targetRealm) {
        if (!npc || npc.maxRealmNpc) return [];
        const requirements = [];
        const pill = C.BREAKTHROUGH_BY_REALM[targetRealm];
        if (pill) requirements.push({
            kind: 'cons', id: pill.id, name: pill.name, need: 1,
            have: this.npcInventoryQuantity(npc, 'cons', pill.id),
        });
        const materialId = breakthroughMaterialId(targetRealm);
        const material = C.MATERIAL_BY_ID?.get(materialId);
        requirements.push({
            kind: 'mat', id: materialId, name: material?.name || materialId,
            need: targetRealm <= 3 ? 2 : 1,
            have: this.npcInventoryQuantity(npc, 'mat', materialId),
        });
        if (targetRealm === 11) {
            const special = C.MATERIAL_BY_ID?.get('mat_thien_dao_tinh');
            requirements.push({
                kind: 'mat', id: 'mat_thien_dao_tinh', name: special?.name || 'Thiên Đạo Tinh Thạch',
                need: 2, have: this.npcInventoryQuantity(npc, 'mat', 'mat_thien_dao_tinh'),
            });
        }
        return requirements;
    }

    consumeNpcBreakthroughItems(npc, requirements) {
        for (const requirement of requirements) {
            let remaining = requirement.need;
            for (const storageName of ['bag', 'warehouse']) {
                const storage = npc[storageName] || [];
                for (const item of storage) {
                    if (remaining <= 0) break;
                    if (item.kind !== requirement.kind || item.id !== requirement.id) continue;
                    const quantity = Math.max(1, Math.floor(Number(item.qty) || 1));
                    const used = Math.min(remaining, quantity);
                    item.qty = quantity - used;
                    remaining -= used;
                }
                npc[storageName] = storage.filter(item => (Number(item.qty) || 1) > 0);
            }
        }
    }

    collectNpcActivityLoot(npc, monster, activity, now) {
        if (!npc || !monster) return;
        const nextRealm = (Number(npc.realm) || 0) + 1;
        if (nextRealm > this.townRealmCap(npc.townId || this.npcHomeTown(npc))) return;
        const requirements = this.npcBreakthroughRequirements(npc, nextRealm);
        const materialMinRealm = {
            mat_yeu_dan: 0, mat_huyet_tinh: 3, mat_long_lan: 6,
            mat_tien_thach: 9, mat_thien_dao_tinh: 10,
            mat_phap_tac_tinh: 20, mat_hon_don_tinh: 26,
        };
        const materialChances = [0.52, 0.35, 0.02, 0.18, 0.14, 0.10, 0.07];
        const materialIds = Object.keys(materialMinRealm);
        for (const requirement of requirements) {
            if (this.npcInventoryQuantity(npc, requirement.kind, requirement.id) >= requirement.need) continue;
            const minRealm = requirement.kind === 'mat' ? (materialMinRealm[requirement.id] ?? 0) : Math.max(0, nextRealm - 2);
            if ((monster.realm || 0) + 2 < minRealm) continue;
            let chance;
            if (requirement.kind === 'cons') {
                chance = activity === 'dungeon' ? 0.27 : activity === 'quest' ? 0.21 : 0.16;
            } else {
                const rank = Math.max(0, materialIds.indexOf(requirement.id));
                chance = materialChances[rank] || 0.07;
                if (activity === 'dungeon') chance = Math.min(0.75, chance + 0.12);
            }
            if (this.rng() < chance) this.addNpcInventoryItem(npc, requirement.kind, requirement.id, 1, 'warehouse', now);
        }
        if (this.rng() < (activity === 'dungeon' ? 0.55 : 0.4)) {
            const commonId = (monster.realm || 0) >= 10 ? 'mat_tien_thach'
                : (monster.realm || 0) >= 7 ? 'mat_long_lan'
                    : (monster.realm || 0) >= 4 ? 'mat_huyet_tinh' : 'mat_yeu_dan';
            this.addNpcInventoryItem(npc, 'mat', commonId, 1, 'warehouse', now);
        }
    }

    npcBreakthroughSummary(npc) {
        if (npc?.isDead) return 'Đã vẫn lạc';
        if (npc?.maxRealmNpc) return 'Đạt cảnh giới tối cao';
        const targetRealm = (Number(npc?.realm) || 0) + 1;
        const realmCap = this.townRealmCap(npc?.townId || this.npcHomeTown(npc));
        if (targetRealm > realmCap) return 'Đã chạm trần cảnh giới khu vực';
        const missing = this.npcBreakthroughRequirements(npc, targetRealm)
            .map(item => `${item.name} ${item.have}/${item.need}`);
        return `Đột phá ${this.realmName(targetRealm)}: ${missing.join(' · ')}`;
    }

    grantNpcExp(npc, amount) {
        if (!npc || npc.maxRealmNpc || npc.isDead) return [];
        const realmCap = this.townRealmCap(npc.townId || this.npcHomeTown(npc));
        npc.realm = Math.min(Number(npc.realm) || Number(npc.baseRealm) || 0, realmCap);
        npc.exp = Math.max(0, Number(npc.exp) || 0) + Math.max(0, Math.round(Number(amount) || 0));
        const breakthroughs = [];
        while (npc.realm < realmCap) {
            const expCap = this.npcRealmExpCap(npc.realm);
            if (npc.exp < expCap) break;
            const requirements = this.npcBreakthroughRequirements(npc, npc.realm + 1);
            if (requirements.some(item => this.npcInventoryQuantity(npc, item.kind, item.id) < item.need)) {
                npc.exp = Math.min(npc.exp, expCap);
                break;
            }
            this.consumeNpcBreakthroughItems(npc, requirements);
            npc.exp -= expCap;
            npc.realm += 1;
            npc.gearTier = Math.min(10, (npc.gearTier || 0) + (npc.realm % 4 === 0 ? 1 : 0));
            npc.pvpPoints = (npc.pvpPoints || 1200) + 60;
            breakthroughs.push(npc.realm);
            if (npc.realm >= 8) {
                for (const player of Object.values(this.data.players || {})) {
                    if (!player.registered) continue;
                    player.notices ||= [];
                    player.notices.push(`⚡ [THIÊN KIÊU ĐỘT PHÁ] ${npc.name} (${npc.title}) đột phá thành công [${this.realmName(npc.realm)}]! Khí tức lay chuyển cõi Cửu Châu.`);
                }
            }
        }
        if (npc.realm >= realmCap) npc.exp = Math.min(npc.exp, Math.max(0, this.npcRealmExpCap(npc.realm) - 1));
        npc.power = this.calcNpcPower(npc);
        return breakthroughs;
    }

    // ---- NPC Thế Giới (Tu luyện, leo BXH, khiêu chiến) ----------------------
    ensureWorldNpcs(now = this.now()) {
        this.data.worldNpcs = this.data.worldNpcs || {};
        const roster = NPC_WORLD_ROSTER;
        let changed = false;
        for (const [npcIndex, npc] of roster.entries()) {
            if (!this.data.worldNpcs[npc.id]) {
                const initNpc = {
                    ...npc,
                    townId: this.npcHomeTown(npc),
                    realm: npc.maxRealmNpc ? 65 : Math.min(npc.baseRealm, this.townRealmCap(this.npcHomeTown(npc))),
                    exp: 0,
                    gearTier: npc.gearTier || Math.min(5, Math.floor((npc.baseRealm || 0) / 4)),
                    wins: 8,
                    losses: 2,
                    pvpPoints: 1200 + (npc.baseRealm || 0) * 50,
                    lastTick: now,
                    nextActivityAt: now + 45000 + (npcIndex % 6) * 20000,
                    activityCount: 0,
                    activityCounters: { hunt: 0, dungeon: 0, quest: 0, duel: 0 },
                    rankDuelWins: 0,
                    rankDuelLosses: 0,
                    bag: Array.isArray(npc.bag) ? npc.bag : [],
                    warehouse: Array.isArray(npc.warehouse) ? npc.warehouse : [],
                    intelligence: NPC_MAX_INTELLIGENCE,
                    aiLevel: NPC_MAX_INTELLIGENCE,
                    skillCds: {},
                };
                initNpc.skills = this.npcLearnedSkills(initNpc);
                initNpc.power = this.calcNpcPower(initNpc);
                this.data.worldNpcs[npc.id] = initNpc;
                changed = true;
            } else {
                // Cập nhật lại power nếu đang dùng powerBase cũ (không hợp lý)
                const n = this.data.worldNpcs[npc.id];
                n.townId ||= this.npcHomeTown(n);
                n.maxRealmNpc = Boolean(n.maxRealmNpc || npc.maxRealmNpc);
                n.excludeNpcLeaderboard = Boolean(n.excludeNpcLeaderboard || npc.excludeNpcLeaderboard);
                n.realm = n.maxRealmNpc ? 65 : Math.min(Number(n.realm) || Number(n.baseRealm) || 0, this.townRealmCap(n.townId));
                if (n.maxRealmNpc) n.exp = 0;
                if (!n.gearTier) n.gearTier = Math.min(5, Math.floor((n.realm || 0) / 4));
                n.nextActivityAt ??= now + 45000 + (npcIndex % 6) * 20000;
                n.activityCount = Math.max(0, Number(n.activityCount) || 0);
                n.activityCounters ||= { hunt: 0, dungeon: 0, quest: 0, duel: 0 };
                for (const activity of ['hunt', 'dungeon', 'quest', 'duel']) n.activityCounters[activity] = Math.max(0, Number(n.activityCounters[activity]) || 0);
                n.pvpPoints ??= 1200 + (Number(n.realm) || 0) * 50;
                n.rankDuelWins = Math.max(0, Number(n.rankDuelWins) || 0);
                n.rankDuelLosses = Math.max(0, Number(n.rankDuelLosses) || 0);
                n.bag = Array.isArray(n.bag) ? n.bag : [];
                n.warehouse = Array.isArray(n.warehouse) ? n.warehouse : [];
                n.isDead = Boolean(n.isDead);
                n.intelligence = NPC_MAX_INTELLIGENCE;
                n.aiLevel = NPC_MAX_INTELLIGENCE;
                n.skillCds = n.skillCds && typeof n.skillCds === 'object' ? n.skillCds : {};
                n.skills = this.npcLearnedSkills(n);
                // NPC cũ có lịch hoạt động 5–9 phút; đưa lịch về nhịp mới để
                // họ sớm tiếp tục săn yêu/nhận nhiệm vụ sau khi cập nhật.
                if (n.nextActivityAt > now + 6 * 60 * 1000) {
                    n.nextActivityAt = now + 30000 + (npcIndex % 4) * 20000;
                }
                const realistic = this.calcNpcPower(n);
                if (!n.power || n.power === npc.powerBase || n.power > realistic * 3 || n.power !== realistic) {
                    n.power = realistic;
                }
            }
        }

        // Giữ đủ số NPC sống cho từng role. Hồ sơ tử trận luôn được giữ nguyên;
        // người thay thế có ID mới và là một nhân vật khác, không hồi sinh NPC cũ.
        const aliveCounts = {};
        for (const npc of Object.values(this.data.worldNpcs)) {
            if (npc?.mon && !npc.isDead) {
                const key = npcPopulationKey(npc);
                aliveCounts[key] = (aliveCounts[key] || 0) + 1;
            }
        }
        const templatesByPopulation = new Map();
        for (const npc of roster) {
            const key = npcPopulationKey(npc);
            if (!templatesByPopulation.has(key)) templatesByPopulation.set(key, []);
            templatesByPopulation.get(key).push(npc);
        }
        for (const [populationKey, targetCount] of Object.entries(NPC_ACTIVE_POPULATION_TARGETS)) {
            const templates = templatesByPopulation.get(populationKey) || [];
            if (!templates.length) continue;
            let missing = Math.max(0, targetCount - (aliveCounts[populationKey] || 0));
            while (missing > 0) {
                const sequence = Math.max(0, Number(this.data.npcSuccessorSequence) || 0) + 1;
                this.data.npcSuccessorSequence = sequence;
                const template = templates[(sequence - 1) % templates.length];
                const id = `npc_successor_${String(template.mon)}_${String(sequence).padStart(6, '0')}`;
                if (this.data.worldNpcs[id]) continue;
                const townId = this.npcHomeTown(template);
                const realm = template.maxRealmNpc ? 65 : Math.min(Number(template.baseRealm) || 0, this.townRealmCap(townId));
                const successor = {
                    ...template,
                    id,
                    name: `${template.name} Tân Tú ${sequence}`,
                    townId,
                    realm,
                    exp: 0,
                    gearTier: template.gearTier || Math.min(5, Math.floor((realm || 0) / 4)),
                    wins: 0,
                    losses: 0,
                    pvpPoints: 1200 + (realm || 0) * 50,
                    lastTick: now,
                    nextActivityAt: now + 60000 + (sequence % 6) * 20000,
                    activityCount: 0,
                    activityCounters: { hunt: 0, dungeon: 0, quest: 0, duel: 0 },
                    rankDuelWins: 0,
                    rankDuelLosses: 0,
                    bag: [],
                    warehouse: [],
                    skills: Array.isArray(template.skills) ? [...template.skills] : [],
                    skillCds: {},
                    intelligence: NPC_MAX_INTELLIGENCE,
                    aiLevel: NPC_MAX_INTELLIGENCE,
                    isDead: false,
                    successorOf: template.id,
                    generation: sequence,
                };
                successor.skills = this.npcLearnedSkills(successor);
                successor.power = this.calcNpcPower(successor);
                this.data.worldNpcs[id] = successor;
                aliveCounts[populationKey] = (aliveCounts[populationKey] || 0) + 1;
                missing -= 1;
                changed = true;
            }
        }
        if (changed) this.touch();
    }

    npcLearnedSkills(npc) {
        const known = new Set(Array.isArray(npc?.skills) ? npc.skills : []);
        const realm = Math.max(0, Number(npc?.realm) || Number(npc?.baseRealm) || 0);
        for (const skill of C.SKILLS) {
            if (skill.grant && skill.realm <= realm
                && (skill.mon === 'chung' || skill.mon === npc?.mon)
                && (!skill.demonOnly || npc?.isDemon)) known.add(skill.id);
        }
        return [...known].filter(id => C.SKILL_BY_ID.has(id));
    }

    tickNpcs(now = this.now()) {
        this.ensureWorldNpcs(now);
        const npcs = this.data.worldNpcs;
        if (!npcs) return;
        this.tickHiddenHeavenPunishment(now);
        if (this.data.lastNpcTick == null) {
            this.data.lastNpcTick = now;
            return;
        }
        const lastTick = this.data.lastNpcTick || 0;
        const npcTickMs = 3 * 60 * 1000;
        if (now - lastTick < npcTickMs) return; // Chạy định kỳ mỗi 3 phút
        const elapsedTicks = clamp(Math.floor((now - lastTick) / npcTickMs), 1, 480);
        this.data.lastNpcTick = now;

        const npcList = Object.values(npcs);
        const activeNpcList = npcList.filter(n => !n.maxRealmNpc && !n.isDead);

        // 1. NPC TỰ TU LUYỆN (Cultivation & Breakthrough lên đến Tiên Đế 30)
        for (const n of npcList) {
            if (n.isDead) continue;
            if (n.maxRealmNpc) {
                n.realm = 65;
                n.exp = 0;
                n.power = this.calcNpcPower(n);
                continue;
            }
            const realmCap = this.townRealmCap(n.townId || this.npcHomeTown(n));
            n.realm = Math.min(Number(n.realm) || Number(n.baseRealm) || 0, realmCap);
            n.power = this.calcNpcPower(n);
            if (n.realm >= realmCap) { n.exp = 0; continue; }
            const expGain = Math.round((400 + (n.realm || 0) * 200) * (0.8 + this.rng() * 0.4) * elapsedTicks);
            this.grantNpcExp(n, expGain);
        }

        this.tickNpcActivities(npcList, now);
        this.tickNpcPhongThanDuels(npcList, now);

        // 2. NPC TẤN CÔNG / KHIÊU CHIẾN NGƯỜI CHƠI (Wild PK, Đột Kích Dã Ngoại)
        // Chỉ đột kích người chơi đang lang bạt dã ngoại (isRoaming), không tấn công người trong thành an toàn
        const candidates = Object.values(this.data.players || {}).filter(pl => pl.registered && !pl.isNpc && !this.isHiddenFromPlayers(pl)
            && (pl.injuredUntil || 0) <= now && (pl.npcRaidCooldownUntil || 0) <= now && pl.isRoaming && this.canReceiveAmbientAttack(pl, now));
        // NPC raids are ambient pressure, not a recurring punishment loop. The
        // tick runs every 3 minutes, while a player can only be picked hourly.
        if (candidates.length > 0 && this.rng() < 0.06) {
            const attackers = activeNpcList.filter(n => n.isDemon || n.realm >= 1);
            if (attackers.length > 0) {
                const attacker = attackers[Math.floor(this.rng() * attackers.length)];
                const victim = candidates[Math.floor(this.rng() * candidates.length)];
                this.recordAmbientAttack(victim, now);
                victim.npcRaidCooldownUntil = now + 60 * 60 * 1000;
                
                const pStats = this.stats(victim, now);
                const beats = (a, b) => C.HE[a]?.beats === b;
                const mul1 = beats(attacker.he, victim.he) ? 1.25 : (beats(victim.he, attacker.he) ? 0.8 : 1);
                const mul2 = beats(victim.he, attacker.he) ? 1.25 : (beats(attacker.he, victim.he) ? 0.8 : 1);
                
                const npcEffectivePower = (attacker.power || 300) * mul1;
                const playerEffectivePower = (pStats.power || 300) * mul2;
                const npcWinChance = Math.max(0.1, Math.min(0.7, (npcEffectivePower / (npcEffectivePower + playerEffectivePower)) * 0.85));
                const npcWon = this.rng() < npcWinChance;

                victim.notices = victim.notices || [];
                if (npcWon) {
                    victim.losses = (victim.losses || 0) + 1;
                    attacker.wins = (attacker.wins || 0) + 1;
                    victim.injuredUntil = now + C.RULES.injuryMs;
                    victim.hp = Math.max(1, Math.round(pStats.hp * 0.15));

                    const stolenStones = Math.min(victim.stones || 0, Math.floor(25 + attacker.realm * 20));
                    victim.stones = Math.max(0, (victim.stones || 0) - stolenStones);
                    attacker.stones = (attacker.stones || 500) + stolenStones;

                    const curExp = this.realms.get(victim.userId)?.experience || 0;
                    const expPenalty = Math.max(15, Math.round(curExp * 0.02));
                    const lostExp = this.realms.loseExp(victim.userId, expPenalty);

                    const noticeMsg = attacker.isDemon
                        ? `💀 [MA TU PHỤC KÍCH] Đại ma đầu ${attacker.name} (${attacker.title}) đã đột kích bạn! Bạn trọng thương ${Math.round(C.RULES.injuryMs / 60000)} phút, tổn thất -${stolenStones} linh thạch và -${lostExp} EXP!`
                        : `⚔️ [THIÊN KIÊU KHIÊU CHIẾN] Cao thủ ${attacker.name} (${attacker.title}) đã khiêu chiến và đánh bại bạn! Bạn trọng thương ${Math.round(C.RULES.injuryMs / 60000)} phút, mất -${stolenStones} linh thạch và -${lostExp} EXP!`;
                    victim.notices.push(noticeMsg);

                    // 10% khả năng tẩu hỏa nhập ma nếu chiến bại trước Ma Tu
                    if (attacker.isDemon) {
                        this.triggerDemonTauHoaNhapMa(victim.userId, attacker.name, victim.notices);
                    }
                    // Nếu exp về 0 mà bị trọng thương sẽ tụt cảnh giới
                    this.checkInjuryRealmDrop(victim.userId, victim.notices);

                    this.emit('attacked', {
                        attackerId: attacker.id,
                        attackerName: attacker.name,
                        victimId: victim.userId,
                        victimName: victim.name,
                        won: true,
                        lostStones: stolenStones,
                        lostExp,
                        injuredMin: Math.round(C.RULES.injuryMs / 60000),
                    });
                } else {
                    victim.wins = (victim.wins || 0) + 1;
                    attacker.losses = (attacker.losses || 0) + 1;
                    const rewardStones = Math.floor(35 + attacker.realm * 25);
                    const rewardExp = Math.floor(300 + attacker.realm * 150);
                    victim.stones = (victim.stones || 0) + rewardStones;
                    this.realms.addExp(victim.userId, rewardExp);
                    victim.notices.push(`🛡️ [PHÒNG THỦ VẺ VANG] ${attacker.name} (${attacker.title}) khiêu chiến bạn nhưng bị bạn đánh lui! Nhận +${rewardStones} linh thạch và +${rewardExp} EXP!`);
                }
            }
        }

        // 3. NPC CHIẾM KHU FARM TÔNG MÔN & TỰ LẬP TÔNG (NPC Sects Occupy Territories & NPCs Found/Join Sects)
        this.data.sectTerritories = this.data.sectTerritories || (C.SECT_TERRITORIES || []).map(t => ({ ...t }));
        const npcSects = this.getNpcSects();
        const npcSectIds = npcSects.length > 0 ? npcSects.map(s => s.id) : ['van_kiem', 'duoc_vuong', 'tieu_dao'];

        // a) Chiếm các mỏ vô chủ
        for (const occupyingSectId of npcSectIds) {
            const unclaimedTerrs = this.data.sectTerritories.filter(t => !t.occupiedBy);
            if (!unclaimedTerrs.length) break;
            const terr = unclaimedTerrs[Math.floor(this.rng() * unclaimedTerrs.length)];
            terr.occupiedBy = occupyingSectId;
            terr.occupiedAt = now;
            terr.lastHarvestAt = now;
        }

        // b) Tranh đoạt khu của người chơi (tỷ lệ 15% mỗi chu kỳ)
        if (this.rng() < 0.15) {
            const playerTerrs = this.data.sectTerritories.filter(t => t.occupiedBy && !this.isNpcSect(this.sects[t.occupiedBy]));
            if (playerTerrs.length > 0) {
                const targetTerr = playerTerrs[Math.floor(this.rng() * playerTerrs.length)];
                const oldSect = this.sects[targetTerr.occupiedBy];
                const invadingSectId = npcSectIds[Math.floor(this.rng() * npcSectIds.length)];
                const invadingSect = this.sects[invadingSectId] || { name: 'Cổ Tông Môn', leaderName: 'Chủ Tông' };
                
                targetTerr.occupiedBy = invadingSectId;
                targetTerr.occupiedAt = now;
                targetTerr.lastHarvestAt = now;

                for (const pl of Object.values(this.data.players || {})) {
                    if (pl.registered && pl.sectId === oldSect?.id) {
                        pl.notices = pl.notices || [];
                        pl.notices.push(`🚩 [KHU FARM BỊ CƯỚP] ${invadingSect.name} (Chủ tông: ${invadingSect.leaderName}) đã điều động cao thủ đột kích chiếm đoạt [${targetTerr.name}] của tông môn! Hãy mau chóng xuất chinh đoạt lại!`);
                    }
                }
            }
        }

        // c) NPC tự lập tông môn mới hoặc gia nhập tông môn có NPC làm chủ tông
        for (const n of npcList) {
            if (n.isDead) continue;
            if (n.sectId && this.sects[n.sectId]) continue;

            // Tăng cơ hội tụ hội: thiên kiêu đủ tu vi có thể lập tông,
            // còn người chưa có môn phái ưu tiên gia nhập một tông NPC sẵn có.
            if ((n.realm || 0) >= 3 && this.rng() < NPC_FOUND_SECT_CHANCE && Object.keys(this.sects).length < 40) {
                const sectId = 'sect_npc_' + n.id;
                if (!this.sects[sectId]) {
                    const sectSuffixes = ['Kiếm Tông', 'Tiên Các', 'Ma Phái', 'Thần Điện', 'Đạo Môn', 'Vương Phủ', 'Tiên Cung', 'Sơn Trang'];
                    const sectName = `${n.name} ${sectSuffixes[Math.floor(this.rng() * sectSuffixes.length)]}`;
                    this.sects[sectId] = {
                        id: sectId,
                        name: sectName,
                        desc: `Tông môn do cao thủ ${n.name} (${n.title || 'Tiên Nhân'}) tự lập khai sáng tông phái.`,
                        leaderId: String(n.id),
                        leaderName: n.name,
                        level: Math.max(1, Math.min(5, Math.floor((n.realm || 0) / 2))),
                        exp: 500,
                        buff: { atkPct: 0.05, hpPct: 0.05, desc: '+5% Công & Khí Huyết' },
                        trial: { name: `Thủ Sơn Đại Trận của ${n.name}`, power: Math.round((n.power || 3000) * 0.7), desc: 'Khảo hạch nhập môn' },
                        members: {
                            [String(n.id)]: {
                                role: 'leader',
                                roleName: 'Tông Chủ',
                                joinedAt: new Date(now).toISOString(),
                                contributed: 5000,
                                isNpc: true,
                                name: n.name,
                            }
                        },
                        funds: 50000,
                        isNpcSect: true,
                        createdAt: new Date(now).toISOString(),
                    };
                    addSectContribution(this.sects[sectId], 5000);
                    n.sectId = sectId;
                    n.sectRole = 'leader';
                    n.sectContributed = 5000;

                    for (const pl of Object.values(this.data.players || {})) {
                        if (pl.registered) {
                            pl.notices = pl.notices || [];
                            pl.notices.push(`🏛️ [KHAI TÔNG LẬP PHÁI] Cao thủ ${n.name} (${n.title}) đã tự lập tông môn mới: [${sectName}]! Uy danh chấn động tứ phương.`);
                        }
                    }
                    continue;
                }
            }

            // Gia nhập tông môn CÓ NPC LÀM CHỦ TÔNG (Không bao giờ gia nhập tông của người chơi!)
            if (this.rng() < NPC_JOIN_SECT_CHANCE) {
                const availableNpcSects = this.getNpcSects();
                if (availableNpcSects.length > 0) {
                    const joinableSects = availableNpcSects.filter(sect => Object.keys(sect.members || {}).length < 30);
                    if (!joinableSects.length) continue;
                    const targetSect = joinableSects[Math.floor(this.rng() * joinableSects.length)];
                    targetSect.members = targetSect.members || {};
                    const role = (n.realm || 0) >= 5 ? 'elder' : 'ngoai_mon';
                    const roleName = (n.realm || 0) >= 5 ? 'Trưởng Lão' : 'Đệ Tử';
                    const initialContribution = Math.round((n.realm || 1) * 300);
                    targetSect.members[String(n.id)] = {
                        role,
                        roleName,
                        joinedAt: new Date(now).toISOString(),
                        contributed: initialContribution,
                        isNpc: true,
                        name: n.name,
                    };
                    addSectContribution(targetSect, initialContribution);
                    n.sectId = targetSect.id;
                    n.sectRole = role;
                    n.sectContributed = initialContribution;
                }
            }
        }

        // 4. NPC ĐÁNH BOSS THẾ GIỚI (NPCs Attack World Monsters / Bosses)
        this.ensureWorldMonsters(now);
        const spawnedMonsters = (this.data.worldMonsters || []).filter(m => (m.spawnAt || 0) <= now && (!m.lockedUntil || m.lockedUntil <= now));
        if (spawnedMonsters.length > 0 && activeNpcList.length > 0 && this.rng() < 0.40) {
            const wm = spawnedMonsters[Math.floor(this.rng() * spawnedMonsters.length)];
            const town = C.TOWN_BY_ID.get(wm.townId) || { name: 'Thành trấn' };
            const fighter = activeNpcList[Math.floor(this.rng() * activeNpcList.length)];
            if (fighter) {
                const dmg = Math.round((fighter.power * 2 + 100) * (0.8 + this.rng() * 0.5));
                wm.hp = Math.max(0, (wm.hp || wm.maxHp) - dmg);
                if (wm.hp <= 0) {
                    this.queueWorldMonsterRespawn(wm, now);
                    this.data.worldMonsters = this.data.worldMonsters.filter(x => x.uid !== wm.uid);
                    for (const pl of Object.values(this.data.players || {})) {
                        if (pl.registered && (pl.town || 'thanh_van') === wm.townId) {
                            pl.notices = pl.notices || [];
                            pl.notices.push(`⚔️ [TRẢM YÊU] Cao thủ ${fighter.name} (${fighter.title}) đã tiêu diệt Đại Yêu [${wm.name}] tại ${town.name}!`);
                        }
                    }
                } else {
                    for (const pl of Object.values(this.data.players || {})) {
                        if (pl.registered && (pl.town || 'thanh_van') === wm.townId) {
                            pl.notices = pl.notices || [];
                            pl.notices.push(`💥 [ĐẠI CHIẾN YÊU THÚ] ${fighter.name} đang đại chiến với [${wm.name}] tại ${town.name}! Yêu thú bị trọng thương (còn ${wm.hp}/${wm.maxHp} KH), tu sĩ hãy chớp thời cơ xuất chiến!`);
                        }
                    }
                }
            }
        }

        // 5. NPC TƯƠNG TÁC PHƯỜNG THỊ (NPC thường mua đồ & NPC ma tu cướp trang bị)
        this.tickNpcMarket(now);

        // 6. NPC TÔNG MÔN SÁT PHẠT
        this.tickNpcSectCrusades(now);

        this.touch();
    }

    // Thiên Đạo ẩn phạt tối đa 5 tu sĩ đang hoạt động mỗi ngày Việt Nam;
    // mỗi người chỉ có thể bị chọn một lần trong ngày.
    tickHiddenHeavenPunishment(now = this.now()) {
        if (this.data.worldNpcs?.npc_thien_dao?.isDead) return;
        const today = vnDate(now);
        const daySeed = [...today].reduce((sum, char) => sum + char.charCodeAt(0), 0);
        let event = this.data.heavenPunishmentDaily;
        if (!event || event.date !== today) {
            event = this.data.heavenPunishmentDaily = {
                date: today,
                victimIds: [],
                nextAt: now + (1 + (daySeed % 4)) * 60 * 60 * 1000,
            };
            this.touch();
            return;
        }

        event.victimIds = Array.isArray(event.victimIds) ? event.victimIds.map(String) : [];
        if (event.victimIds.length >= 5 || now < (Number(event.nextAt) || 0)) return;

        const activeSince = now - 24 * 60 * 60 * 1000;
        const candidates = Object.entries(this.data.players || {}).filter(([userId, player]) => {
            const lastSeenAt = Number(player.lastSeenAt) || Date.parse(player.registeredAt || '') || 0;
            return player.registered && !player.isNpc && !this.isHiddenFromPlayers(player)
                && !event.victimIds.includes(String(userId))
                && lastSeenAt >= activeSince && lastSeenAt <= now
                && (player.hp == null || player.hp > 0)
                && (player.injuredUntil || 0) <= now
                && !this.activeBattle(userId);
        });
        if (!candidates.length) {
            event.nextAt = now + 30 * 60 * 1000;
            this.touch();
            return;
        }

        const [userId, victim] = candidates[Math.min(candidates.length - 1, Math.floor(this.rng() * candidates.length))];
        const heaven = this.data.worldNpcs.npc_thien_dao;
        const stats = this.stats(victim, now);
        victim.losses = (victim.losses || 0) + 1;
        victim.injuredUntil = now + C.RULES.injuryMs;
        const currentHp = Number.isFinite(Number(victim.hp)) ? Number(victim.hp) : stats.hp;
        victim.hp = Math.min(currentHp, Math.max(1, Math.round(stats.hp * 0.15)));
        victim.notices = victim.notices || [];
        const penalty = this.applyHeavenPunishmentPenalty(victim);
        const itemText = penalty.lostItem
            ? `Thiên Đạo thu lấy [${itemName(penalty.lostItem)}] trong túi/kho thường`
            : 'không có vật phẩm đủ điều kiện để thu lấy (đồ đang mặc, khóa hoặc trong Kho An Toàn được bảo vệ)';
        victim.notices.push(`🌩️ [THIÊN ĐẠO ẨN PHẠT] ${heaven?.name || 'Thiên Đạo'} bất ngờ giáng phạt bạn! Khí huyết còn 15%, trọng thương ${Math.round(C.RULES.injuryMs / 60000)} phút, mất ${penalty.lostExp.toLocaleString('vi-VN')} EXP; ${itemText}.`);
        if (heaven) heaven.wins = (heaven.wins || 0) + 1;

        event.victimIds.push(String(userId));
        event.nextAt = event.victimIds.length >= 5
            ? 0
            : now + (2 + ((daySeed + event.victimIds.length) % 3)) * 60 * 60 * 1000;
        this.touch();
    }

    npcActivityMonster(npc, offset = 0) {
        const town = C.TOWN_BY_ID.get(npc.townId);
        const townPool = (town?.monsterPool || [])
            .map(id => C.MONSTER_BY_ID.get(id))
            .filter(monster => monster && !monster.worldBoss);
        const source = townPool.length ? townPool : C.MONSTERS.filter(monster => !monster.worldBoss);
        const eligible = source.filter(monster => (monster.realm || 0) <= (npc.realm || 0));
        const candidates = (eligible.length ? eligible : source).slice()
            .sort((a, b) => Math.abs((npc.realm || 0) - a.realm) - Math.abs((npc.realm || 0) - b.realm));
        if (!candidates.length) return null;
        return candidates[(Math.max(0, Number(npc.activityCount) || 0) + offset) % Math.min(6, candidates.length)];
    }

    npcCombatPlan(npc, opponent, now = this.now()) {
        const enemyRealm = Math.max(0, Number(opponent?.realm) || 0);
        const enemyMon = opponent?.mon || npc?.mon || 'kiem';
        const enemyStatsPower = Number(opponent?.power) || (
            Number(opponent?.atk) || Number(opponent?.def) || Number(opponent?.hp)
                ? Math.round((Number(opponent?.atk) || 0) * 2 + (Number(opponent?.def) || 0) * 1.5 + (Number(opponent?.hp) || 0) / 10)
                : 0
        );
        const enemyPower = Math.max(1, enemyStatsPower || this.calcNpcPower({ mon: enemyMon, realm: enemyRealm, gearTier: 0 }));
        const npcPower = Math.max(1, Number(npc?.power) || this.calcNpcPower(npc));
        const powerRatio = npcPower / enemyPower;
        const beats = (attacker, defender) => Boolean(attacker && defender && C.HE[attacker]?.beats === defender);
        const targetElement = opponent?.element || opponent?.he;
        const elementMul = beats(npc?.he, targetElement) ? 1.2 : (beats(targetElement, npc?.he) ? 0.82 : 1);

        // NPC học các công pháp đúng môn phái/cảnh giới, hồi linh lực giữa
        // những chuyến lịch luyện rồi tự chọn chiêu mạnh nhất đang sẵn sàng.
        npc.skills = this.npcLearnedSkills(npc);
        npc.skillCds = npc.skillCds && typeof npc.skillCds === 'object' ? npc.skillCds : {};
        const realmScale = Math.pow(C.REALM_GROWTH, Math.max(0, Number(npc.realm) || 0));
        npc.maxMp = Math.max(C.BASE_STATS.mp, Math.round(C.BASE_STATS.mp * realmScale));
        npc.mp = npc.mp == null ? npc.maxMp : clamp(Number(npc.mp) || 0, 0, npc.maxMp);
        if (npc.mpRegenAt == null) npc.mpRegenAt = now;
        regenerateBattleMp(npc, now);

        const candidates = npc.skills.map(id => C.SKILL_BY_ID.get(id)).filter(skill => skill
            && skill.realm <= (Number(npc.realm) || 0)
            && (skill.mon === 'chung' || skill.mon === npc.mon)
            && (!skill.demonOnly || npc.isDemon)
            && (Number(skill.mp) || 0) <= npc.mp
            && (Number(npc.skillCds[skill.id]) || 0) <= now
        ).map(skill => {
            const profile = skillDamageProfile(skill);
            const skillElementMul = beats(skill.element, targetElement) ? 1.2 : (beats(targetElement, skill.element) ? 0.82 : 1);
            const control = (Number(skill.stun) || Number(skill.bind) || Number(skill.slow) || 0) > 0 ? 0.35 : 0;
            const utility = ['heal', 'buff', 'defend'].includes(skill.kind) ? 0.25 : 0;
            return { skill, score: (profile.total + control + utility) * skillElementMul };
        }).sort((a, b) => b.score - a.score);
        const chosen = candidates[0]?.skill || null;
        let skillBonus = 0;
        if (chosen) {
            npc.mp = Math.max(0, npc.mp - Math.max(0, Number(chosen.mp) || 0));
            npc.skillCds[chosen.id] = now + clamp(Number(chosen.cd) || 5, 5, 30) * 1000;
            npc.lastCombatSkillId = chosen.id;
            skillBonus = clamp(skillDamageProfile(chosen).total * 0.07, 0, 0.12);
        }

        // IQ tối đa giúp NPC xét chiến lực, khắc hệ và chiêu đang hồi trước
        // khi quyết định tiếp tục giao chiến hay rút lui dưỡng sức.
        const winChance = clamp(0.52 + Math.log2(Math.max(0.125, powerRatio)) * 0.16 + (elementMul - 1) * 0.25 + skillBonus, 0.12, 0.94);
        return { winChance, skill: chosen, powerRatio, elementMul };
    }

    recordNpcActivity(npc, type, text, success, exp, stones, now, combatPlan = null) {
        npc.activityCount = (npc.activityCount || 0) + 1;
        npc.activityCounters ||= { hunt: 0, dungeon: 0, quest: 0, duel: 0 };
        npc.activityCounters[type] = (npc.activityCounters[type] || 0) + 1;
        const skillName = combatPlan?.skill?.name || null;
        const activityText = skillName ? `${text} Dùng ${skillName} để ứng chiến.` : text;
        npc.lastActivity = { type, text: activityText, success: Boolean(success), at: now, ...(skillName ? { skillName } : {}) };
        npc.stones = Math.max(0, Number(npc.stones) || Math.max(1000, (npc.realm + 1) * 3000)) + Math.max(0, Math.round(stones || 0));
        this.grantNpcExp(npc, exp);
        if (success && npc.sectId && this.sects[npc.sectId]) {
            const sect = this.sects[npc.sectId];
            const member = sect.members?.[String(npc.id)];
            if (member) {
                const today = vnDate(now);
                if (npc.sectContributionDate !== today) {
                    npc.sectContributionDate = today;
                    npc.sectContributionToday = 0;
                }
                const remaining = Math.max(0, SECT_DAILY_CONTRIBUTION_CAP - (Number(npc.sectContributionToday) || 0));
                const base = type === 'quest' ? 100 : type === 'dungeon' ? 120 : 70;
                const points = Math.min(remaining, base + Math.max(0, Number(npc.realm) || 0) * 12);
                const added = addSectContribution(sect, points).added;
                npc.sectContributionToday = (Number(npc.sectContributionToday) || 0) + added;
                npc.sectContributed = (Number(npc.sectContributed) || 0) + added;
                member.contributed = (Number(member.contributed) || 0) + added;
            }
        }
    }

    tickNpcActivities(npcList, now) {
        for (const npc of npcList) {
            if (npc.isDead || npc.maxRealmNpc || (npc.nextActivityAt || 0) > now) continue;
            const roll = this.rng();
            const nextRealm = (Number(npc.realm) || 0) + 1;
            const missingBreakthroughSupply = nextRealm <= this.townRealmCap(npc.townId || this.npcHomeTown(npc))
                && this.npcBreakthroughRequirements(npc, nextRealm).some(item => item.have < item.need);
            const sect = npc.sectId && this.sects[npc.sectId];
            const today = vnDate(now);
            const contributionDate = npc.sectContributionDate === today ? today : '';
            const hasSectContributionRoom = Boolean(sect) && (contributionDate !== today
                || (Number(npc.sectContributionToday) || 0) < SECT_DAILY_CONTRIBUTION_CAP);
            // Chọn việc theo mục tiêu: kiếm vật liệu đột phá trước, sau đó ưu
            // tiên nhiệm vụ/cổ động để tiếp tục tu luyện và cống hiến tông môn.
            const type = missingBreakthroughSupply
                ? (roll < 0.58 ? 'hunt' : 'quest')
                : (hasSectContributionRoom
                    ? (roll < 0.50 ? 'quest' : roll < 0.78 ? 'dungeon' : 'hunt')
                    : (roll < 0.40 ? 'hunt' : roll < 0.70 ? 'dungeon' : 'quest'));
            const jitter = [...String(npc.id || '')].reduce((sum, char) => sum + char.charCodeAt(0), 0) % 4;
            npc.nextActivityAt = now + (3 + jitter) * 60 * 1000;
            const town = C.TOWN_BY_ID.get(npc.townId) || C.TOWNS[0];

            if (type === 'hunt') {
                const monster = this.npcActivityMonster(npc);
                if (!monster) continue;
                const plan = this.npcCombatPlan(npc, monster, now);
                const success = this.rng() < plan.winChance;
                const exp = success ? 700 + (monster.realm || 0) * 220 : 250;
                const stones = success ? 250 + (monster.realm || 0) * 75 : 0;
                if (success) {
                    npc.huntingWins = (npc.huntingWins || 0) + 1;
                    npc.wins = (npc.wins || 0) + 1;
                    this.collectNpcActivityLoot(npc, monster, type, now);
                }
                this.recordNpcActivity(npc, type,
                    success ? `Săn yêu tại ${town.name}: hạ ${monster.name}.` : `Săn yêu tại ${town.name}: giao chiến ${monster.name}, đang dưỡng thương.`,
                    success, exp, stones, now, plan);
                continue;
            }

            if (type === 'dungeon') {
                const localDungeon = C.DUNGEONS.find(dungeon => dungeon.townId === town.id && dungeon.realmMin <= npc.realm);
                const dungeon = localDungeon || C.DUNGEONS
                    .filter(candidate => candidate.realmMin <= npc.realm)
                    .sort((a, b) => Math.abs((npc.realm || 0) - a.realmMin) - Math.abs((npc.realm || 0) - b.realmMin))[0];
                if (!dungeon) {
                    const monster = this.npcActivityMonster(npc);
                    if (monster) this.recordNpcActivity(npc, 'dungeon', `Tìm đường tới Cổ Động tại ${town.name}.`, false, 250, 0, now);
                    continue;
                }
                const encounter = { realm: dungeon.realmMin, element: this.npcActivityMonster(npc)?.element };
                const plan = this.npcCombatPlan(npc, encounter, now);
                const success = this.rng() < plan.winChance;
                const exp = success ? 1400 + dungeon.realmMin * 500 : 350;
                const stones = success ? 700 + dungeon.realmMin * 180 : 0;
                if (success) {
                    npc.dungeonWins = (npc.dungeonWins || 0) + 1;
                    npc.wins = (npc.wins || 0) + 1;
                    this.collectNpcActivityLoot(npc, this.npcActivityMonster(npc), type, now);
                }
                this.recordNpcActivity(npc, type,
                    success ? `Phá Cổ Động ${dungeon.name} tại ${town.name}.` : `Thám hiểm Cổ Động ${dungeon.name}, tạm lui để hồi phục.`,
                    success, exp, stones, now, plan);
                continue;
            }

            const target = this.npcActivityMonster(npc, 1);
            if (!target) continue;
            const plan = this.npcCombatPlan(npc, target, now);
            const success = this.rng() < plan.winChance;
            const exp = success ? 1000 + (target.realm || 0) * 300 : 300;
            const stones = success ? 400 + (target.realm || 0) * 100 : 0;
            if (success) {
                npc.questWins = (npc.questWins || 0) + 1;
                npc.wins = (npc.wins || 0) + 1;
                this.collectNpcActivityLoot(npc, target, type, now);
            }
            this.recordNpcActivity(npc, type,
                success ? `Hoàn thành nhiệm vụ ${town.name}: truy bắt ${target.name}.` : `Nhận nhiệm vụ truy bắt ${target.name} tại ${town.name}.`,
                success, exp, stones, now, plan);
        }
    }

    tickNpcPhongThanDuels(npcList, now) {
        // Tranh hạng được mô phỏng định kỳ, không mở trận tự động với người chơi.
        if (now - (Number(this.data.lastNpcPhongThanDuelAt) || 0) < 8 * 60 * 1000) return;
        this.data.lastNpcPhongThanDuelAt = now;
        if (this.rng() >= 0.55) return;

        const eligible = npcList.filter(npc => !npc.maxRealmNpc && !npc.excludeNpcLeaderboard && !npc.isDead);
        const ranked = eligible.slice().sort((a, b) => (Number(b.pvpPoints) || 0) - (Number(a.pvpPoints) || 0)
            || (Number(b.realm) || 0) - (Number(a.realm) || 0) || (Number(b.power) || 0) - (Number(a.power) || 0));
        const pairs = [];
        for (let challengerIndex = 1; challengerIndex < ranked.length; challengerIndex += 1) {
            const challenger = ranked[challengerIndex];
            if ((challenger.rankDuelCooldownUntil || 0) > now) continue;
            for (let targetIndex = 0; targetIndex < challengerIndex; targetIndex += 1) {
                const target = ranked[targetIndex];
                if ((target.rankDuelCooldownUntil || 0) > now) continue;
                // Thiên kiêu có cảnh giới thấp hơn sẽ có cơ hội khiêu chiến
                // người tu luyện cao hơn đang đứng trên mình trên BXH NPC.
                if ((target.realm || 0) <= (challenger.realm || 0)) continue;
                pairs.push({ challenger, target });
            }
        }
        if (!pairs.length) return;

        const { challenger, target } = pairs[Math.floor(this.rng() * pairs.length)];
        const challengerPower = Math.max(1, Number(challenger.power) || 1);
        const targetPower = Math.max(1, Number(target.power) || 1);
        const relativePower = (challengerPower - targetPower) / targetPower;
        const winChance = clamp(0.32 + relativePower * 0.35, 0.12, 0.48);
        const challengerWon = this.rng() < winChance;
        const challengerPoints = Math.max(1000, Number(challenger.pvpPoints) || 1200);
        const targetPoints = Math.max(1000, Number(target.pvpPoints) || 1200);
        let resultText;

        challenger.rankDuelCooldownUntil = now + 45 * 60 * 1000;
        target.rankDuelCooldownUntil = now + 45 * 60 * 1000;
        challenger.activityCount = (challenger.activityCount || 0) + 1;
        target.activityCount = (target.activityCount || 0) + 1;
        challenger.activityCounters ||= { hunt: 0, dungeon: 0, quest: 0, duel: 0 };
        target.activityCounters ||= { hunt: 0, dungeon: 0, quest: 0, duel: 0 };
        challenger.activityCounters.duel = (challenger.activityCounters.duel || 0) + 1;
        target.activityCounters.duel = (target.activityCounters.duel || 0) + 1;

        if (challengerWon) {
            challenger.pvpPoints = challengerPoints + 24;
            target.pvpPoints = Math.max(1000, targetPoints - 12);
            challenger.rankDuelWins = (challenger.rankDuelWins || 0) + 1;
            target.rankDuelLosses = (target.rankDuelLosses || 0) + 1;
            resultText = `${challenger.name} vượt cảnh khiêu chiến và đánh bại ${target.name}, BXH Phong Thần NPC +24 điểm.`;
        } else {
            challenger.pvpPoints = Math.max(1000, challengerPoints - 6);
            target.pvpPoints = targetPoints + 10;
            challenger.rankDuelLosses = (challenger.rankDuelLosses || 0) + 1;
            target.rankDuelWins = (target.rankDuelWins || 0) + 1;
            resultText = `${challenger.name} khiêu chiến ${target.name} nhưng thất bại; thứ hạng Phong Thần NPC được cập nhật.`;
        }

        const victor = challengerWon ? challenger : target;
        const defeated = challengerWon ? target : challenger;
        victor.wins = (victor.wins || 0) + 1;
        defeated.losses = (defeated.losses || 0) + 1;
        challenger.lastActivity = { type: 'duel', text: resultText, success: challengerWon, at: now };
        target.lastActivity = { type: 'duel', text: resultText, success: !challengerWon, at: now };
    }

    tickNpcMarket(now = this.now()) {
        const npcs = this.data.worldNpcs;
        if (!npcs) return;
        const npcList = Object.values(npcs);
        const marketListings = Object.values(this.market.listings || {}).filter(l => l.expiresAt > now);
        const playerMarketListings = marketListings.filter(l => !l.seller.startsWith('system_') && !l.seller.startsWith('npc_'));
        if (!playerMarketListings.length) return;

        // A. NPC thường thi thoảng mua đồ của người chơi
        const normalNpcs = npcList.filter(n => !n.isDemon && !n.maxRealmNpc && !n.isDead);
        if (normalNpcs.length > 0 && this.rng() < 0.45) {
            const buyer = normalNpcs[Math.floor(this.rng() * normalNpcs.length)];
            buyer.stones = buyer.stones || Math.max(1000, (buyer.realm + 1) * 3000);

            const affordableListings = playerMarketListings.filter(listing => {
                const fairValue = this.sellPrice(listing.item) * Math.max(1, Number(listing.item?.qty) || 1);
                return fairValue > 0 && listing.price <= Math.ceil(fairValue * 1.25) && listing.price <= buyer.stones;
            });
            const targetListing = affordableListings.length > 0
                ? affordableListings[Math.floor(this.rng() * affordableListings.length)]
                : null;

            if (targetListing && this.market.listings[targetListing.id]) {
                delete this.market.listings[targetListing.id];
                const seller = this.player(targetListing.seller);
                const itName = itemName(targetListing.item);
                const tax = Math.ceil(targetListing.price * C.RULES.marketTax);
                const gain = targetListing.price - tax;

                buyer.stones = Math.max(0, buyer.stones - targetListing.price);
                buyer.bag = buyer.bag || [];
                buyer.bag.push(targetListing.item);

                if (seller) {
                    seller.stones = (seller.stones || 0) + gain;
                    const notice = `🛒 [PHƯỜNG THỊ GIAO DỊCH] Tu sĩ ${buyer.name} (${buyer.title || this.realmName(buyer.realm)}) đã mua ${itName}${targetListing.item.qty > 1 ? ` ×${targetListing.item.qty}` : ''} với giá ${targetListing.price.toLocaleString('vi-VN')} linh thạch. Bạn nhận ${gain.toLocaleString('vi-VN')} linh thạch sau thuế.`;
                    this.recordPersonalEvent(targetListing.seller, notice);
                    if (!seller.notices?.includes(notice)) {
                        seller.notices ||= [];
                        seller.notices.push(notice);
                    }
                }
                this.ledger(-tax);
                this.emit('sold', { seller: targetListing.seller, buyerName: buyer.name, name: itName, qty: targetListing.item.qty, price: targetListing.price, gain });
            }
        }

        // B. NPC Ma Tu cướp trang bị trên shop của người chơi
        const demonNpcs = npcList.filter(n => n.isDemon && !n.maxRealmNpc && !n.isDead);
        const equipMarketListings = playerMarketListings.filter(l => l.item && l.item.kind === 'equip' && this.market.listings[l.id]);
        if (demonNpcs.length > 0 && equipMarketListings.length > 0 && this.rng() < 0.35) {
            const robber = demonNpcs[Math.floor(this.rng() * demonNpcs.length)];
            const robbedListing = equipMarketListings[Math.floor(this.rng() * equipMarketListings.length)];

            if (robbedListing && this.market.listings[robbedListing.id]) {
                delete this.market.listings[robbedListing.id];
                const seller = this.player(robbedListing.seller);
                const eqName = itemName(robbedListing.item);

                robber.bag = robber.bag || [];
                robber.bag.push(robbedListing.item);
                robber.bounty = (robber.bounty || 1000) + Math.round(robbedListing.price * 0.5 + 500);
                if (seller) {
                    const notice = `💀 [MA TU CƯỚP PHÁ PHƯỜNG THỊ] Ma đầu ${robber.name} (${robber.title || 'Ma Đạo Cao Thủ'}) đã cướp ${eqName} khỏi sạp của bạn. Hãy khiêu chiến để đoạt lại bảo vật!`;
                    this.recordPersonalEvent(robbedListing.seller, notice);
                    if (!seller.notices?.includes(notice)) {
                        seller.notices ||= [];
                        seller.notices.push(notice);
                    }
                }
                this.emit('robbed', { seller: robbedListing.seller, robberName: robber.name, name: eqName, price: robbedListing.price });
            }
        }
        this.touch();
    }

    getNpcLeaderboard(limit = 20, maintain = true) {
        const now = this.now();
        if (maintain) {
            this.ensureWorldNpcs(now);
            this.tickNpcs(now);
        }
        const list = Object.values(this.data.worldNpcs || {}).filter(n => !n.excludeNpcLeaderboard && !n.isDead).map(n => ({
            id: n.id,
            name: n.name,
            title: n.title,
            gender: n.gender,
            mon: n.mon,
            monName: C.MON[n.mon]?.name || 'Tiên Đạo',
            he: n.he,
            heName: C.HE[n.he]?.name || 'Hỗn Độn',
            realm: n.realm,
            realmName: this.realmName(n.realm),
            power: n.power,
            pvpPoints: n.pvpPoints || 1200,
            rankDuelWins: n.rankDuelWins || 0,
            rankDuelLosses: n.rankDuelLosses || 0,
            avatar: n.avatar || '🧙',
            wins: n.wins || 0,
            losses: n.losses || 0,
            huntingWins: n.huntingWins || 0,
            dungeonWins: n.dungeonWins || 0,
            questWins: n.questWins || 0,
            activity: n.lastActivity?.text || '',
            bagCount: (n.bag || []).length,
            warehouseCount: (n.warehouse || []).length,
            breakthrough: this.npcBreakthroughSummary(n),
            sectName: n.sectId && this.sects[n.sectId]?.name || null,
            sectContribution: n.sectContributed || 0,
            stones: n.stones || 500,
        }));
        list.sort((a, b) => b.pvpPoints - a.pvpPoints || b.realm - a.realm || b.power - a.power);
        return list.slice(0, limit);
    }

    getNpcList(userId, maintain = true) {
        const now = this.now();
        if (maintain) {
            this.ensureWorldNpcs(now);
            this.tickNpcs(now);
        }
        return Object.values(this.data.worldNpcs || {}).filter(n => !n.isDead).map(n => ({
            id: n.id,
            name: n.name,
            title: n.title,
            gender: n.gender,
            mon: n.mon,
            monName: C.MON[n.mon]?.name || 'Tiên Đạo',
            he: n.he,
            heName: C.HE[n.he]?.name || 'Hỗn Độn',
            realm: n.realm,
            realmName: this.realmName(n.realm),
            power: n.power,
            avatar: n.avatar || '🧙',
            desc: n.desc,
            isMaxRealm: Boolean(n.maxRealmNpc),
            isDead: Boolean(n.isDead),
            wins: n.wins || 0,
            losses: n.losses || 0,
            bagCount: (n.bag || []).length,
            warehouseCount: (n.warehouse || []).length,
            breakthrough: this.npcBreakthroughSummary(n),
            sectName: n.sectId && this.sects[n.sectId]?.name || null,
            sectContribution: n.sectContributed || 0,
            huntingWins: n.huntingWins || 0,
            dungeonWins: n.dungeonWins || 0,
            questWins: n.questWins || 0,
            activity: n.lastActivity?.text || (n.maxRealmNpc ? 'Ngự tại cảnh giới tối cao.' : 'Đang tu luyện, chờ chuyến lịch luyện tiếp theo.'),
        }));
    }

    challengeNpc(userId, npcId) {
        // Compatibilidad para callers antiguos; NPC combat is turn-by-turn.
        return this.startNpcBattle(userId, npcId);
    }

    startNpcBattle(userId, npcId) {
        const p = this.requirePlayer(userId);
        this.requireNotKnockedOutInDungeon(p);
        const now = this.now();
        this.syncStamina(p, now);
        if (p.stamina < 10) fail('Cần 10 thể lực để khiêu chiến NPC cao thủ.');
        if (now < (p.injuredUntil || 0)) fail('Đang trọng thương, không thể khiêu chiến.');
        const daily = this.getNpcDaily(p, now);
        if (daily.remaining <= 0) {
            fail(`Hôm nay đạo hữu đã khiêu chiến NPC ${daily.max}/${daily.max} lần, linh lực cạn kiệt, ngày mai hãy trở lại!`);
        }
        this.ensureWorldNpcs(now);
        const npc = this.data.worldNpcs[npcId];
        if (!npc) fail('Không tìm thấy NPC này.');
        if (npc.isDead) fail(`${npc.name} đã vẫn lạc và không thể hồi sinh.`);
        p.stamina -= 10;
        if (p.stamina < C.RULES.staminaMax && p.staminaAt > now) p.staminaAt = now;
        p.npcDaily.count = (p.npcDaily.count || 0) + 1;

        const npcStats = this.npcCombatStats(npc);
        npcStats.maxMp = npcStats.mp;
        npcStats.power = npc.power || this.calcNpcPower(npc);

        const npcMonsterDef = {
            id: `npc_${npc.id}`,
            name: `${npc.title ? `[${npc.title}] ` : ''}${npc.name}`,
            icon: npc.icon || (npc.gender === 'nu' ? '🧝‍♀️' : '🧙‍♂️'),
            realm: npc.realm,
            // NPCs may use lore affinities such as âm/dương/tam; map those
            // to playable elements before handing them to the battle engine.
            element: C.HE[npc.he] ? npc.he : ({ am: 'thuy', duong: 'hoa', tam: 'thien' }[npc.he] || 'kim'),
            hp: npcStats.hp,
            atk: npcStats.atk,
            def: npcStats.def,
            spd: npcStats.spd,
            sense: npcStats.sense,
            crit: npcStats.crit,
            isNpc: true,
            npcId: npc.id,
            isDemon: Boolean(npc.isDemon),
            npcMon: npc.mon,
            npcSkills: Array.isArray(npc.skills) ? npc.skills.slice() : [],
            mp: npcStats.mp,
            maxMp: npcStats.maxMp,
            intelligence: NPC_MAX_INTELLIGENCE,
            tier: 'normal',
        };

        const battle = new Battle(this, [p], npcMonsterDef, now);
        battle.isNpc = true;
        battle.npcId = npc.id;
        this.battles.set(String(userId), battle);
        this.touch();
        return {
            success: true,
            battle: battle.view(now, userId),
        };
    }

    isNpcSect(s) {
        if (!s) return false;
        if (s.isNpcSect) return true;
        if (['van_kiem', 'duoc_vuong', 'tieu_dao', 'thai_huyen', 'u_minh'].includes(s.id)) return true;
        const leaderId = String(s.leaderId || '');
        if (leaderId.startsWith('system_') || leaderId.startsWith('npc_')) return true;
        if (this.data.worldNpcs && this.data.worldNpcs[leaderId]) return true;
        return false;
    }

    getNpcSects() {
        return Object.values(this.sects || {}).filter(s => this.isNpcSect(s)
            && !this.data.worldNpcs?.[String(s.leaderId || '')]?.isDead);
    }

    getNpcLeaderStats(sect) {
        if (!sect) return { id: 'npc_leader_unknown', name: 'Tông Chủ', fullName: 'Tông Chủ', realm: 10, realmName: 'Độ Kiếp', power: 8000, hp: 50000, atk: 5000, def: 3000 };
        const leaderId = String(sect.leaderId || '');
        const npc = this.data.worldNpcs?.[leaderId];
        if (npc) {
            const combat = this.npcCombatStats(npc);
            return {
                id: String(npc.id),
                name: npc.name,
                fullName: `[TÔNG CHỦ] ${npc.name} (${npc.title || 'Cao Thủ'})`,
                isNpc: true,
                isLeader: true,
                realm: npc.realm || 10,
                realmName: this.realmName(npc.realm || 10),
                power: this.calcNpcPower(npc),
                hp: combat.hp,
                atk: combat.atk,
                def: combat.def,
            };
        }
        const lvl = sect.level || 3;
        const realm = Math.min(65, 8 + lvl * 2);
        return {
            id: leaderId || `npc_leader_${sect.id}`,
            name: sect.leaderName || 'Tông Chủ Cổ Xưa',
            fullName: `[TÔNG CHỦ] ${sect.leaderName || 'Tông Chủ Cổ Xưa'}`,
            isNpc: true,
            isLeader: true,
            realm,
            realmName: this.realmName(realm),
            power: Math.round(6000 + lvl * 3000),
            hp: Math.round(60000 + lvl * 25000),
            atk: Math.round(6000 + lvl * 2000),
            def: Math.round(4000 + lvl * 1500),
        };
    }

    markWorldNpcDead(npcId, cause, now = this.now()) {
        const npc = this.data.worldNpcs?.[String(npcId)];
        if (!npc || npc.isDead) return false;
        npc.isDead = true;
        npc.deadAt = now;
        npc.deadReason = String(cause || 'Đã tử trận');
        npc.hp = 0;
        npc.sectWarRestUntil = 0;
        npc.lastActivity = { type: 'death', text: `${npc.name} đã vẫn lạc trong ${npc.deadReason}; không thể hồi sinh.`, success: false, at: now };
        return true;
    }

    getSectWarDefendersList(war) {
        if (!war) return [];
        const defSect = this.sects[war.defenderSectId];
        if (!defSect) return [];
        const defenders = [];
        const seenIds = new Set();
        const defeatedSet = new Set((war.defeatedDefenders || []).map(String));

        // 1. Thêm Tông Chủ / Chủ Tông tham chiến
        if (this.isNpcSect(defSect)) {
            const npcLeader = this.getNpcLeaderStats(defSect);
            seenIds.add(String(npcLeader.id));
            defenders.push({
                id: String(npcLeader.id),
                name: npcLeader.name,
                fullName: npcLeader.fullName,
                roleName: 'Chủ Tông',
                isLeader: true,
                isNpc: true,
                he: this.data.worldNpcs?.[String(npcLeader.id)]?.he || null,
                realm: npcLeader.realm,
                realmName: npcLeader.realmName,
                power: npcLeader.power,
                isDefeated: defeatedSet.has(String(npcLeader.id)),
            });
        } else {
            const leaderPlayer = this.player(defSect.leaderId);
            if (leaderPlayer && !this.isHiddenFromPlayers(leaderPlayer)) {
                const lStats = this.stats(leaderPlayer, this.now());
                seenIds.add(String(leaderPlayer.userId));
                defenders.push({
                    id: String(leaderPlayer.userId),
                    name: leaderPlayer.name,
                    fullName: leaderPlayer.fullName || leaderPlayer.name,
                    roleName: 'Tông Chủ',
                    isLeader: true,
                    isNpc: false,
                    he: leaderPlayer.he || null,
                    realm: this.realmOf(leaderPlayer.userId).index,
                    realmName: this.realmName(this.realmOf(leaderPlayer.userId).index),
                    power: lStats.power,
                    isDefeated: defeatedSet.has(String(leaderPlayer.userId)),
                });
            }
        }

        // 2. Thêm các thành viên trong tông
        for (const [mId, mDef] of Object.entries(defSect.members || {})) {
            const mIdStr = String(mId);
            if (seenIds.has(mIdStr)) continue;
            seenIds.add(mIdStr);

            const mPlayer = this.player(mIdStr);
            if (mPlayer?.registered && !this.isHiddenFromPlayers(mPlayer)) {
                const mStats = this.stats(mPlayer, this.now());
                defenders.push({
                    id: mIdStr,
                    name: mPlayer.name,
                    fullName: mPlayer.fullName || mPlayer.name,
                    roleName: mDef.roleName || 'Đệ Tử',
                    isLeader: false,
                    isNpc: false,
                    he: mPlayer.he || null,
                    realm: this.realmOf(mPlayer.userId).index,
                    realmName: this.realmName(this.realmOf(mPlayer.userId).index),
                    power: mStats.power,
                    isDefeated: defeatedSet.has(mIdStr),
                });
            } else if (mDef.isNpc || this.data.worldNpcs?.[mIdStr] || mIdStr.startsWith('system_') || mIdStr.startsWith('npc_')) {
                const npcObj = this.data.worldNpcs?.[mIdStr];
                const r = npcObj?.realm || 3;
                defenders.push({
                    id: mIdStr,
                    name: mDef.name || npcObj?.name || 'Đệ Tử NPC',
                    fullName: npcObj ? `${npcObj.name} (${npcObj.title || mDef.roleName || 'Hộ Tông'})` : (mDef.name || 'Đệ Tử'),
                    roleName: mDef.roleName || 'Đệ Tử',
                    isLeader: false,
                    isNpc: true,
                    he: npcObj?.he || null,
                    realm: r,
                    realmName: this.realmName(r),
                    power: npcObj ? this.calcNpcPower(npcObj) : 3000,
                    isDefeated: defeatedSet.has(mIdStr),
                });
            }
        }

        // Bù thành viên người chơi có sectId hợp lệ nhưng thiếu trong bảng
        // members do dữ liệu cũ; người chơi thật luôn là mục tiêu có thể đánh.
        for (const [playerId, member] of Object.entries(this.data.players || {})) {
            const playerKey = String(member?.userId || playerId);
            if (!member?.registered || this.isHiddenFromPlayers(member) || String(member.sectId || '') !== String(defSect.id) || seenIds.has(playerKey)) continue;
            const mStats = this.stats(member, this.now());
            seenIds.add(playerKey);
            defenders.push({
                id: playerKey,
                name: member.name,
                fullName: member.fullName || member.name,
                roleName: member.sectRole ? (SECT_ROLE_NAMES[member.sectRole] || 'Đệ Tử') : 'Đệ Tử',
                isLeader: playerKey === String(defSect.leaderId),
                isNpc: false,
                he: member.he || null,
                realm: this.realmOf(playerKey).index,
                realmName: this.realmName(this.realmOf(playerKey).index),
                power: mStats.power,
                isDefeated: defeatedSet.has(playerKey),
            });
        }

        // Nếu tông NPC chưa có thành viên nào phụ tá, tự tạo thêm 2 Trưởng Lão hộ sơn
        if (this.isNpcSect(defSect) && defenders.length === 1) {
            const leaderPower = defenders[0].power || 6000;
            const elder1Id = `${defSect.id}_elder_1`;
            const elder2Id = `${defSect.id}_elder_2`;
            defenders.push({
                id: elder1Id,
                name: 'Đại Trưởng Lão Hộ Tông',
                fullName: `Đại Trưởng Lão (${defSect.name})`,
                roleName: 'Đại Trưởng Lão',
                isLeader: false,
                isNpc: true,
                realm: Math.max(1, (defenders[0].realm || 5) - 1),
                realmName: this.realmName(Math.max(1, (defenders[0].realm || 5) - 1)),
                power: Math.round(leaderPower * 0.8),
                isDefeated: defeatedSet.has(elder1Id),
            });
            defenders.push({
                id: elder2Id,
                name: 'Chấp Sự Trưởng Lão',
                fullName: `Chấp Sự Trưởng Lão (${defSect.name})`,
                roleName: 'Trưởng Lão',
                isLeader: false,
                isNpc: true,
                realm: Math.max(1, (defenders[0].realm || 5) - 2),
                realmName: this.realmName(Math.max(1, (defenders[0].realm || 5) - 2)),
                power: Math.round(leaderPower * 0.65),
                isDefeated: defeatedSet.has(elder2Id),
            });
        }

        return defenders;
    }

    getActiveWarForSect(sectId) {
        const war = (this.data?.sectWars || []).find(w => (w.attackerSectId === sectId || w.defenderSectId === sectId) && w.status === 'active');
        if (!war) return null;
        war.defeatedAttackers = Array.isArray(war.defeatedAttackers) ? war.defeatedAttackers : [];
        war.defeatedDefenders = Array.isArray(war.defeatedDefenders) ? war.defeatedDefenders : [];
        const attackers = this.getSectWarDefendersList({
            ...war,
            defenderSectId: war.attackerSectId,
            defeatedDefenders: war.defeatedAttackers,
        });
        const defenders = this.getSectWarDefendersList(war);
        const isAttacker = String(war.attackerSectId) === String(sectId);
        const opponents = isAttacker ? defenders : attackers;
        const members = isAttacker ? attackers : defenders;
        return {
            ...war,
            // `defenders` is kept as the UI's target list for compatibility;
            // either side sees the other side's full live roster.
            defenders: opponents,
            opponents,
            members,
            attackingMembers: attackers,
            defendingMembers: defenders,
            isAttacker,
            defeatedAttackers: [...war.defeatedAttackers],
            defeatedDefenders: [...war.defeatedDefenders],
        };
    }

    getSectWarSideRoster(war, sectId) {
        if (!war || !sectId) return [];
        const isAttacker = String(war.attackerSectId) === String(sectId);
        const isDefender = String(war.defenderSectId) === String(sectId);
        if (!isAttacker && !isDefender) return [];
        return this.getSectWarDefendersList(isAttacker
            ? { ...war, defenderSectId: war.attackerSectId, defeatedDefenders: war.defeatedAttackers || [] }
            : war);
    }

    isSectWarSideEliminated(war, sectId) {
        const now = this.now();
        const roster = this.getSectWarSideRoster(war, sectId).filter(member => {
            if (member.isDefeated) return false;
            if (member.isNpc) {
                const npc = this.data.worldNpcs?.[String(member.id)];
                return (Number(npc?.sectWarRestUntil) || 0) <= now;
            }
            const player = this.player(member.id);
            return Boolean(player?.registered)
                && (Number(player.injuredUntil) || 0) <= now
                && !this.activeBattle(member.id);
        });
        return roster.length === 0;
    }

    resolveSectWarElimination(war, losingSectId, now = this.now()) {
        if (!war || war.status !== 'active') return null;
        const losingId = String(losingSectId);
        const winnerId = losingId === String(war.attackerSectId)
            ? String(war.defenderSectId)
            : String(war.attackerSectId);
        const losingSect = this.sects[losingId];
        const winningSect = this.sects[winnerId];
        if (!losingSect || !winningSect) return null;

        const losingName = losingSect.name || (losingId === String(war.attackerSectId) ? war.attackerName : war.defenderName);
        const winningName = winningSect.name || (winnerId === String(war.attackerSectId) ? war.attackerName : war.defenderName);
        const losingRoster = this.getSectWarSideRoster(war, losingId);
        war.status = winnerId === String(war.attackerSectId) ? 'attacker_win' : 'defender_win';
        war.winnerSectId = winnerId;
        war.loserSectId = losingId;
        war.log ||= [];
        war.log.push(`🏳️ [${losingName}] đã hết thành viên có thể xuất chiến. [${winningName}] chiến thắng!`);
        war.log.push(`🔥 TÔNG MÔN [${losingName}] CHÍNH THỨC BỊ DIỆT!`);

        const spoils = this.distributeSectWarSpoils(winningSect, losingSect);
        war.log.push(`💰 Tàng bảo thu được ${spoils.vaultUnits} vật phẩm; ${spoils.memberUnits} món chia cho thành viên, ${spoils.lostUnits} món thất lạc thiên địa.`);
        const splitStones = Math.floor((Number(losingSect.funds) || 0) / Math.max(1, losingRoster.length));
        for (const member of losingRoster) {
            const memberId = String(member.id);
            if (member.isNpc) this.markWorldNpcDead(memberId, `tông môn ${losingName} bị diệt`, now);
            const memberPlayer = this.player(memberId);
            if (!memberPlayer) continue;
            memberPlayer.sectId = null;
            memberPlayer.sectRole = null;
            memberPlayer.sectContributed = 0;
            memberPlayer.sectCoins = 0;
            this.sendMail(memberId, {
                title: '🥀 [DIỆT TÔNG] Tông môn đã bị hủy diệt',
                sender: 'Tông Môn Tàn Tích',
                content: `Tông môn [${losingName}] đã bị [${winningName}] đánh sập hoàn toàn.`,
                stones: splitStones,
            });
        }
        delete this.sects[losingId];
        return { winnerSectId: winnerId, loserSectId: losingId, spoils };
    }

    finishNpcSectWar(war, attackerSect, defenderSect, now = this.now()) {
        return this.resolveSectWarElimination(war, defenderSect.id, now);
    }

    tickNpcSectCrusades(now = this.now()) {
        const lastCrusade = this.data.lastNpcSectCrusade || 0;
        // Chu kỳ 10 phút kiểm tra và tiến hành chiến tranh tông môn
        if (now - lastCrusade < 10 * 60 * 1000) return;
        this.data.lastNpcSectCrusade = now;

        this.data.sectWars = this.data.sectWars || [];

        // 1. Toàn bộ thành viên NPC còn sống đều tham chiến. Mỗi người tự chọn
        // mục tiêu theo chiến lực/khắc hệ, dùng công pháp sẵn sàng rồi nghỉ 20 phút.
        const npcSectCombatants = sect => {
            const ids = new Set(Object.entries(sect?.members || {})
                .filter(([id, member]) => member?.isNpc || this.data.worldNpcs?.[String(id)])
                .map(([id]) => String(id)));
            const leaderId = String(sect?.leaderId || '');
            if (this.data.worldNpcs?.[leaderId]) ids.add(leaderId);
            const members = [...ids].map(id => this.data.worldNpcs?.[id]).filter(npc => npc && !npc.isDead);
            if (members.length) return members.sort((a, b) => (Number(b.power) || 0) - (Number(a.power) || 0));
            if ([...ids].some(id => this.data.worldNpcs?.[id])) return [];

            const leader = this.getNpcLeaderStats(sect);
            return [{
                ...leader,
                mon: 'kiem',
                he: null,
                skills: [],
                skillCds: {},
                intelligence: NPC_MAX_INTELLIGENCE,
                aiLevel: NPC_MAX_INTELLIGENCE,
                _sectWarFallback: true,
            }];
        };

        const injureWarPlayer = (target, victorName, victorSect, war) => {
            if (target.isNpc) return;
            const player = this.player(target.id);
            if (!player?.registered) return;
            player.injuredUntil = Math.max(Number(player.injuredUntil) || 0, now + C.RULES.injuryMs);
            const stats = this.stats(player, now);
            player.hp = Math.max(1, Math.round(stats.hp * 0.15));
            player.notices ||= [];
            const notice = `⚔️ [TÔNG CHIẾN] ${victorName} của [${victorSect.name}] đánh bại bạn trên chiến trường. Bạn trọng thương; trận giữa [${war.attackerName}] và [${war.defenderName}] đang tiếp diễn.`;
            player.notices.push(notice);
            this.sendMail(target.id, {
                title: '⚔️ [TÔNG CHIẾN] Bị đánh bại trên chiến trường',
                sender: 'Chiến Báo Sơn Môn',
                content: notice,
            });
        };

        const letNpcMembersFight = (war, actingSect, targetSect, attacking) => {
            const actors = npcSectCombatants(actingSect);
            if (actors.length === 0) {
                war.log ||= [];
                war.log.push(`🕯️ Tông môn [${actingSect.name}] không còn thành viên sống để xuất chiến.`);
                this.resolveSectWarElimination(war, actingSect.id, now);
                return;
            }
            const defeatedKey = attacking ? 'defeatedDefenders' : 'defeatedAttackers';
            war[defeatedKey] = Array.isArray(war[defeatedKey]) ? war[defeatedKey] : [];
            for (const actor of actors) {
                if (war.status !== 'active') break;
                if (actor.isDead || (Number(actor.sectWarRestUntil) || 0) > now) continue;
                const targetWar = attacking
                    ? war
                    : { ...war, defenderSectId: targetSect.id, defeatedDefenders: [] };
                const targets = this.getSectWarDefendersList(targetWar)
                    .filter(target => !target.isDefeated
                        && !war[defeatedKey].includes(String(target.id))
                        && (!target.isNpc || !this.data.worldNpcs?.[String(target.id)]?.isDead)
                        && (target.isNpc || ((Number(this.player(target.id)?.injuredUntil) || 0) <= now
                            && !this.activeBattle(target.id))));
                if (!targets.length) continue;

                // IQ 100 chọn đối thủ có thể hạ trước, để giảm nhanh áp lực lên tông.
                targets.sort((a, b) => (Number(a.power) || 0) - (Number(b.power) || 0));
                const target = targets[0];
                const plan = this.npcCombatPlan(actor, target, now);
                const won = this.rng() < plan.winChance;
                const actorName = actor.name || 'NPC';
                if (!actor._sectWarFallback) {
                    actor.sectWarRestUntil = now + 20 * 60 * 1000;
                    actor.lastActivity = {
                        type: 'sect_war',
                        text: `${actorName} cùng thành viên ${actingSect.name} tham chiến${plan.skill ? `, dùng ${plan.skill.name}` : ''}; đang hồi phục 20 phút.`,
                        success: won,
                        at: now,
                        ...(plan.skill ? { skillName: plan.skill.name } : {}),
                    };
                } else {
                    actingSect.npcWarRestUntil = now + 20 * 60 * 1000;
                }

                if (won) {
                    war[defeatedKey].push(String(target.id));
                    war.log ||= [];
                    war.log.push(`⚔️ [${actingSect.name}] ${actorName}${plan.skill ? ` dùng ${plan.skill.name}` : ''} đánh bại [${target.fullName || target.name}] của [${targetSect.name}]!`);
                    if (target.isNpc) {
                        const defeatedNpc = this.data.worldNpcs?.[String(target.id)];
                        if (defeatedNpc) {
                            this.markWorldNpcDead(target.id, `tông chiến ${targetSect.name}`, now);
                        }
                    } else {
                        injureWarPlayer(target, actorName, actingSect, war);
                    }
                } else {
                    war.log ||= [];
                    war.log.push(`🛡️ [${target.fullName || target.name}] của [${targetSect.name}] đẩy lùi ${actorName} bên [${actingSect.name}].`);
                }

                if (this.isSectWarSideEliminated(war, targetSect.id)) {
                    this.resolveSectWarElimination(war, targetSect.id, now);
                    break;
                }
                if (this.isSectWarSideEliminated(war, actingSect.id)) {
                    this.resolveSectWarElimination(war, actingSect.id, now);
                    break;
                }
            }
        };

        for (const war of this.data.sectWars) {
            if (war.status !== 'active') continue;
            const attackerSect = this.sects[war.attackerSectId];
            const defenderSect = this.sects[war.defenderSectId];
            if (!attackerSect || !defenderSect) continue;
            war.defeatedDefenders ||= [];
            war.defeatedAttackers ||= [];
            if (this.isNpcSect(attackerSect)) letNpcMembersFight(war, attackerSect, defenderSect, true);
            if (war.status === 'active' && this.isNpcSect(defenderSect)) {
                letNpcMembersFight(war, defenderSect, attackerSect, false);
            }
        }

        // 2. PHÁT ĐỘNG CHIẾN TRANH MỚI: tăng tần suất nhưng mỗi tông NPC
        // tối đa hai lần tuyên chiến/ngày, có thời gian nghỉ hồi phục.
        if (this.rng() > 0.55) return;
        const allSects = Object.values(this.sects || {});
        if (allSects.length < 2) return;

        const today = vnDate(now);
        const activeWars = this.data.sectWars.filter(w => w.status === 'active');
        const npcSects = this.getNpcSects().filter(sect => {
            if (sect.npcWarDate !== today) {
                sect.npcWarDate = today;
                sect.npcWarsStartedToday = 0;
            }
            const leader = this.data.worldNpcs?.[String(sect.leaderId || '')];
            return (Number(sect.npcWarsStartedToday) || 0) < 2
                && (Number(sect.npcWarCooldownUntil) || 0) <= now
                && !leader?.isDead
                && !activeWars.some(w => w.attackerSectId === sect.id || w.defenderSectId === sect.id);
        });
        if (npcSects.length === 0) return;

        const attackerSect = npcSects[Math.floor(this.rng() * npcSects.length)];
        const targetSects = allSects.filter(s => s.id !== attackerSect.id
            && !activeWars.some(w => w.attackerSectId === s.id || w.defenderSectId === s.id));
        if (targetSects.length === 0) return;
        const playerTargets = targetSects.filter(sect => !this.isNpcSect(sect));
        const targetPool = playerTargets.length && this.rng() < 0.5 ? playerTargets : targetSects;
        const defenderSect = targetPool[Math.floor(this.rng() * targetPool.length)];

        const warId = 'war_crusade_' + newId();
        const crusadeWar = {
            id: warId,
            attackerSectId: attackerSect.id,
            attackerName: attackerSect.name,
            defenderSectId: defenderSect.id,
            defenderName: defenderSect.name,
            type: this.rng() < 0.4 ? 'attack' : 'challenge',
            startedAt: now,
            defendUntil: now + 4 * 3600 * 1000,
            status: 'active',
            isNpcCrusade: true,
            defeatedDefenders: [],
            log: [`⚔️ [TÔNG MÔN CHIẾN] Chủ Tông [${attackerSect.leaderName || attackerSect.name}] đã đích thân dẫn đệ tử xuất chinh tuyên chiến với [${defenderSect.name}]!`],
        };
        this.data.sectWars.push(crusadeWar);
        attackerSect.npcWarDate = today;
        attackerSect.npcWarsStartedToday = (Number(attackerSect.npcWarsStartedToday) || 0) + 1;
        attackerSect.npcWarCooldownUntil = now + 2 * 60 * 60 * 1000;

        for (const mId of Object.keys(defenderSect.members || {})) {
            this.sendMail(mId, {
                title: `🚨 [ĐẠI HỌA SÁT PHẠT] ${attackerSect.name} Đến Đồ Sát!`,
                sender: 'Hộ Tông Trưởng Lão',
                content: `Tông môn [${attackerSect.name}] do Chủ Tông [${attackerSect.leaderName}] thống lĩnh đã xua quân sát phạt tông môn ta! Tất cả đệ tử hãy mau chóng nghênh chiến bảo vệ Sơn Môn!`,
            });
        }
        this.touch();
    }

    // ---- Xưởng Rèn (Luyện Khí Sư) & Luyện Đan Các (Đan Sư) -------------------
    craftingTool(p, profession) {
        const uid = profession === 'dan' ? p.equip?.loDinh : p.equip?.weapon;
        const item = p.items.find(it => it.uid === uid && it.kind === 'equip' && it.place === 'equip');
        const def = itemDef(item);
        const matches = profession === 'dan'
            ? Boolean(def && (def.slot === 'lo_dinh' || def.wtype === 'dinh'))
            : Boolean(def && def.wtype === C.MON[profession]?.weapon);
        return { item: matches ? item : null, def: matches ? def : null };
    }

    scribeCraftProgress(p) {
        const xp = Math.max(0, Number(p?.scribeCraftXp) || 0);
        let rank = 0;
        for (let i = 0; i < SCRIBE_CRAFT_RANKS.length; i += 1) {
            if (xp >= SCRIBE_CRAFT_RANKS[i].xp) rank = i;
        }
        const current = SCRIBE_CRAFT_RANKS[rank];
        const next = SCRIBE_CRAFT_RANKS[rank + 1] || null;
        return {
            xp,
            rank,
            title: current.name,
            nextTitle: next?.name || null,
            nextXp: next?.xp ?? null,
            progressPct: next ? Math.round((xp - current.xp) / (next.xp - current.xp) * 100) : 100,
        };
    }

    damageCraftingTool(p, profession, tool, recipeRank) {
        const lossByRank = [1, 1, 2, 2, 3, 4];
        const loss = lossByRank[clamp(Number(recipeRank) || 0, 0, lossByRank.length - 1)];
        const item = tool?.item;
        if (!item) return { loss: 0, durability: 0, broken: false, toolName: 'dụng cụ' };
        const toolName = itemName(item);
        item.dur = Math.max(0, Number(item.dur ?? 100) - loss);
        const result = { loss, durability: item.dur, broken: false, toolName };
        if (item.dur <= 0) {
            p.items = p.items.filter(it => it.uid !== item.uid);
            p.quick = (p.quick || []).map(uid => uid === item.uid ? null : uid);
            for (const slot of Object.keys(p.equip || {})) {
                if (p.equip[slot] === item.uid) p.equip[slot] = null;
            }
            this.stockAdd('item', item.id, -1);
            result.broken = true;
        }
        result.message = result.broken
            ? ` [${toolName}] đã vỡ và mất hẳn.`
            : ` [${toolName}] mất ${loss} độ bền, còn ${item.dur}/100.`;
        return result;
    }

    awardScribeCraftXp(p, recipeRank, success) {
        if (p.mon !== 'phu') return null;
        const gained = success ? 1 + clamp(Number(recipeRank) || 0, 0, 5) : 1;
        p.scribeCraftXp = Math.max(0, Number(p.scribeCraftXp) || 0) + gained;
        return { gained, progress: this.scribeCraftProgress(p) };
    }

    craftingFire(p, profession) {
        const id = p.craftingFires?.[profession];
        const def = id ? C.FIRE_BY_ID?.get(id) : null;
        if (!def) return null;
        const count = (p.items || []).filter(item => item.kind === 'mat' && item.id === id && (['bag', 'kho'].includes(item.place) || item.place === 'equip'))
            .reduce((sum, item) => sum + (Number(item.qty) || 0), 0);
        const realm = this.realmOf(p.userId).index;
        return count > 0 && realm >= (def.realmMin || 0) ? def : null;
    }

    setCraftingFire(userId, profession, fireId = null) {
        const p = this.requirePlayer(userId);
        this.requireIdle(p);
        if (!['dan', 'ren'].includes(profession)) fail('Dị Hỏa chỉ lắp vào dụng cụ của Đan Sư hoặc Thợ Rèn.');
        p.craftingFires ||= { dan: null, ren: null };
        const oldFireId = p.craftingFires[profession];
        if (!fireId) {
            if (oldFireId) {
                const equippedItem = (p.items || []).find(it => it.kind === 'mat' && it.id === oldFireId && it.place === 'equip' && (it.fireSlot === profession || !it.fireSlot));
                if (equippedItem) {
                    if (this.countPlace(p, 'bag') >= C.RULES.bagSize) {
                        fail('Túi đầy, hãy dọn bớt hành trang trước khi tháo Dị Hỏa.');
                    }
                    equippedItem.place = 'bag';
                    delete equippedItem.fireSlot;
                    this.mergePlayerStacks(p);
                }
                p.craftingFires[profession] = null;
                this.touch();
            }
            return { success: true, message: profession === 'dan' ? 'Đã tháo Dị Hỏa khỏi lô đỉnh.' : 'Đã tháo Dị Hỏa khỏi búa rèn.' };
        }
        const def = C.FIRE_BY_ID?.get(String(fireId));
        if (!def) fail('Không tìm thấy Dị Hỏa trong danh mục.');
        const other = profession === 'dan' ? 'ren' : 'dan';
        const otherFireId = p.craftingFires[other];

        if (oldFireId && oldFireId !== def.id) {
            const oldItem = (p.items || []).find(it => it.kind === 'mat' && it.id === oldFireId && it.place === 'equip' && (it.fireSlot === profession || !it.fireSlot));
            if (oldItem) {
                oldItem.place = 'bag';
                delete oldItem.fireSlot;
            }
        }

        const unequippedItem = (p.items || []).find(it => it.kind === 'mat' && it.id === def.id && ['bag', 'kho'].includes(it.place));
        const alreadyEquippedHere = (p.items || []).find(it => it.kind === 'mat' && it.id === def.id && it.place === 'equip' && it.fireSlot === profession);

        if (!unequippedItem && !alreadyEquippedHere) {
            fail(`Bạn chưa sở hữu ${def.name}.`);
        }
        if (otherFireId === def.id && !unequippedItem) {
            fail('Một Dị Hỏa chỉ có thể lắp trên một dụng cụ tại cùng thời điểm.');
        }
        if (this.realmOf(userId).index < (def.realmMin || 0)) {
            fail(`Cần cảnh giới ${this.realmName(def.realmMin)} trở lên để điều khiển Dị Hỏa này.`);
        }

        if (unequippedItem && !alreadyEquippedHere) {
            if ((Number(unequippedItem.qty) || 1) > 1) {
                unequippedItem.qty = (Number(unequippedItem.qty) || 1) - 1;
                p.items.push({
                    uid: newId(),
                    kind: 'mat',
                    id: def.id,
                    qty: 1,
                    place: 'equip',
                    fireSlot: profession,
                });
            } else {
                unequippedItem.place = 'equip';
                unequippedItem.fireSlot = profession;
            }
        }
        p.craftingFires[profession] = def.id;
        this.touch();
        return { success: true, message: `Đã lắp ${def.name} vào ${profession === 'dan' ? 'ô Dị Hỏa của lô đỉnh' : 'ô Dị Hỏa của búa rèn'}.` };
    }

    craftingGate(userId, p, profession, recipe, outputDef, toolDef) {
        const roleNames = { dan: 'Đan Sư', ren: 'Thợ Rèn', phu: 'Phù Sư' };
        const role = roleNames[profession];
        const rank = craftingTierRank(outputDef, recipe);
        const breakthroughTargetRealm = Number(outputDef.toRealm);
        const outputMinRealm = outputDef.breakthrough && Number.isFinite(breakthroughTargetRealm)
            ? Math.max(0, breakthroughTargetRealm - 1)
            : Math.max(Number(outputDef.realmMin) || 0, Number(outputDef.toRealm) || 0);
        const minRealm = Math.max(
            Number(recipe.minRealm) || 0,
            outputMinRealm,
            Number(C.TIER[recipe.tier || outputDef.tier]?.realm) || 0,
        );
        if (p.mon !== profession) {
            const reason = profession === 'ren' && (outputDef.slot === 'lo_dinh' || outputDef.wtype === 'dinh')
                ? 'Chỉ Thợ Rèn đúc lô đỉnh vì đây là pháp khí. Đan Sư không cần tự rèn: hãy trang bị lô đỉnh ở ô Lô Đỉnh để luyện đan.'
                : `Chỉ ${role} mới được chế tác.`;
            return { rank, minRealm, reason };
        }
        if (!toolDef) {
            const toolName = profession === 'dan' ? 'lô đỉnh ở ô trang bị' : profession === 'ren' ? 'búa rèn ở ô vũ khí' : 'phù bút ở ô vũ khí';
            return { rank, minRealm, reason: `Cần trang bị ${toolName}.` };
        }
        const equippedTool = this.craftingTool(p, profession).item;
        if (equippedTool && Number(equippedTool.dur ?? 100) <= 0) {
            return { rank, minRealm, reason: `Dụng cụ [${itemName(equippedTool)}] đã hỏng. Hãy sửa chữa hoặc thay dụng cụ khác.` };
        }
        if (profession === 'phu') {
            const progress = this.scribeCraftProgress(p);
            const reqCraftXp = Math.max(0, Number(recipe.reqCraftXp) || 0);
            if (progress.xp < reqCraftXp) {
                const title = SCRIBE_CRAFT_RANKS.find(step => step.xp === reqCraftXp)?.name || 'bậc nghề tiếp theo';
                return { rank, minRealm, reqCraftXp, reason: `Cần ${title} (${reqCraftXp} điểm tiến triển Phù Sư). Hiện có ${progress.xp}.` };
            }
        }
        const currentRealm = Number(this.realmOf(userId)?.index) || 0;
        if (currentRealm < minRealm) return { rank, minRealm, reason: `Cần cảnh giới ${this.realmName(minRealm)} trở lên.` };
        const reqRoleStat = Math.max(Number(recipe.reqRoleStat) || 0, Number(outputDef.reqRoleStat) || 0);
        const roleStat = Number(p.roleStats?.[profession]) || 0;
        if (reqRoleStat > roleStat) return { rank, minRealm, reqRoleStat, reason: `Cần ${C.MON[profession].statName} ${reqRoleStat} điểm.` };
        if (recipe.needLearn && !p.learnedRecipes?.[recipe.id]) return { rank, minRealm, reqRoleStat, reason: 'Cần lĩnh ngộ bản thảo công thức trước.' };
        const requiredFireRank = profession === 'dan' || profession === 'ren' ? requiredFireRankForCraft(rank) : 0;
        const fire = this.craftingFire(p, profession);
        if (requiredFireRank && (!fire || fire.fireRank < requiredFireRank)) {
            const requiredName = C.FIRE_QUALITY?.[Object.keys(C.FIRE_QUALITY).find(key => C.FIRE_QUALITY[key].rank === requiredFireRank)]?.name || 'Dị Hỏa phẩm phù hợp';
            const toolName = profession === 'dan' ? 'lô đỉnh' : 'búa rèn';
            return { rank, minRealm, reqRoleStat, roleStat, fireRank: fire?.fireRank || 0, requiredFireRank, fireName: fire?.name || null, fireBonus: 0,
                reason: `Cần lắp Dị Hỏa ${requiredName} phẩm trở lên cho ${toolName}; tìm nguồn trong Thư Các > Vật phẩm > Dị Hỏa.` };
        }
        return { rank, minRealm, reqRoleStat, roleStat, fireRank: fire?.fireRank || 0, requiredFireRank, fireName: fire?.name || null, fireBonus: fire ? (Number(fire.successBonus) || 0) : 0, reason: null };
    }

    craftingCost(p, recipe, materialMultiplier = 1) {
        const reqMats = {};
        for (const [id, qty] of Object.entries(recipe.materials || {})) {
            reqMats[id] = Math.max(1, Math.ceil(Number(qty) * materialMultiplier));
        }
        return { reqMats, stoneCost: Math.max(1, Math.ceil((Number(recipe.stones) || 0) * materialMultiplier)) };
    }

    craftingHasCost(p, cost) {
        if ((Number(p.stones) || 0) < cost.stoneCost) return false;
        return Object.entries(cost.reqMats).every(([id, need]) => p.items
            .filter(it => it.kind === 'mat' && it.id === id && ['bag', 'kho'].includes(it.place))
            .reduce((sum, it) => sum + (Number(it.qty) || 0), 0) >= need);
    }

    payCraftingCost(p, cost) {
        for (const [matId, reqQty] of Object.entries(cost.reqMats)) {
            let remain = reqQty;
            for (const place of ['bag', 'kho']) {
                for (const item of [...p.items]) {
                    if (remain <= 0) break;
                    if (item.kind !== 'mat' || item.id !== matId || item.place !== place) continue;
                    const take = Math.min(remain, Number(item.qty) || 0);
                    if (take > 0) { this.consume(p, item, take); remain -= take; }
                }
            }
        }
        p.stones -= cost.stoneCost;
        this.ledger(-cost.stoneCost);
    }

    craftingCanStore(p, kind, id, qty) {
        let remaining = Math.max(0, Number(qty) || 0);
        for (const place of ['bag', 'kho']) {
            for (const item of p.items) {
                if (item.kind === kind && item.id === id && item.place === place) {
                    remaining -= Math.max(0, C.RULES.stackMax - (Number(item.qty) || 0));
                }
            }
            remaining = Math.max(0, remaining);
        }
        const freeSlots = ['bag', 'kho'].reduce((sum, place) => sum + Math.max(0, this.capacity(place, p) - this.countPlace(p, place)), 0);
        return remaining <= freeSlots * C.RULES.stackMax;
    }

    getCraftingView(userId) {
        const p = this.requirePlayer(userId);
        const isSmith = p.mon === 'ren';
        const isAlchemist = p.mon === 'dan';
        const isScribe = p.mon === 'phu';
        const alchemyTool = this.craftingTool(p, 'dan');
        const smithTool = this.craftingTool(p, 'ren');
        const scribeTool = this.craftingTool(p, 'phu');
        const cauldronDef = alchemyTool.def;
        const roleStat = Number(p.roleStats?.[p.mon]) || 0;
        const ownedFireCounts = new Map();
        for (const item of p.items || []) {
            if (item.kind === 'mat' && C.FIRE_BY_ID?.has(item.id) && ['bag', 'kho', 'equip'].includes(item.place)) {
                ownedFireCounts.set(item.id, (ownedFireCounts.get(item.id) || 0) + (Number(item.qty) || 0));
            }
        }
        const fireSlots = Object.fromEntries(['dan', 'ren'].map(profession => {
            const fire = this.craftingFire(p, profession);
            return [profession, fire ? { id: fire.id, name: fire.name, qualityName: fire.fireQualityName, rank: fire.fireRank } : null];
        }));
        const ownedFires = (C.CRAFT_FIRES || []).filter(fire => ownedFireCounts.has(fire.id)).map(fire => ({
            id: fire.id, name: fire.name, icon: fire.icon, qualityName: fire.fireQualityName, rank: fire.fireRank,
            count: ownedFireCounts.get(fire.id), sourceMonsterId: fire.sourceMonsterId,
            sourceMonsterIds: fire.sourceMonsterIds || [fire.sourceMonsterId],
        }));
        const toolState = tool => tool.item && tool.def ? {
            name: tool.def.name,
            durability: Number(tool.item.dur ?? 100),
            repairCost: craftingToolRepairCost(tool.def, tool.item.dur),
        } : null;
        const equipCategory = e => {
            if (!e) return 'other';
            if (e.slot === 'lo_dinh' || e.wtype === 'dinh') return 'dan';
            if (e.mount) return 'toa_ky';
            if (e.slot === 'phi_kiem') return 'phapbao';
            if (e.slot === 'armor') return 'giap';
            if (e.slot === 'nhan_tru_do') return 'truvat';
            return ({ kiem: 'kiem', phapkhi: 'phap', trongkhi: 'the', quyensao: 'quyen', bua: 'ren', but: 'phu' })[e.wtype] || 'phapbao';
        };
        const equipSlotNames = { weapon: 'Vũ khí', armor: 'Đạo bào', acc: 'Trang sức', phi_kiem: 'Phi kiếm', lo_dinh: 'Lô đỉnh', nhan_tru_do: 'Nhẫn Trữ Đồ' };
        const equipTypeNames = { kiem: 'Kiếm', phapkhi: 'Pháp khí', trongkhi: 'Trọng khí', quyensao: 'Quyền sáo', dinh: 'Dược đỉnh', bua: 'Búa rèn', but: 'Phù bút' };
        const myMats = {};
        for (const it of p.items) {
            if (it.kind === 'mat' && ['bag', 'kho'].includes(it.place)) {
                myMats[it.id] = (myMats[it.id] || 0) + it.qty;
            }
        }

        const equipRecipes = C.CRAFT_EQUIP_RECIPES.map(r => {
            const eDef = C.EQUIP_BY_ID.get(r.equipId);
            const cost = this.craftingCost(p, r, isSmith ? 0.8 : 1);
            const reqMats = Object.fromEntries(Object.entries(cost.reqMats).map(([mid, need]) => [mid, { need, have: myMats[mid] || 0, name: C.MATERIAL_BY_ID.get(mid)?.name || mid, icon: C.MATERIAL_BY_ID.get(mid)?.icon || '🪨' }]));
            const gate = this.craftingGate(userId, p, 'ren', r, eDef || {}, smithTool.def);
            let blockReason = gate.reason;
            if (!blockReason && !this.craftingHasCost(p, cost)) blockReason = p.stones < cost.stoneCost ? 'Cần ' + cost.stoneCost.toLocaleString('vi-VN') + ' linh thạch.' : 'Thiếu nguyên liệu chế tác.';
            if (!blockReason && !this.craftingCanStore(p, 'equip', r.equipId, 1)) blockReason = 'Túi và kho đã đầy.';
            const toolRank = Number(smithTool.def?.qualityRank ?? C.TIER[smithTool.def?.tier]?.rank) || 0;
            const successRate = clamp(forgingSuccessRate(gate.rank || craftingTierRank(eDef || {}, r), toolRank, artisanExpertise(roleStat)) + (gate.fireBonus || 0), 0.05, 0.90);

            return {
                ...r,
                equipName: eDef?.name || r.name,
                equipIcon: eDef?.icon || '🗡️',
                equipDesc: eDef?.desc || r.desc,
                equipCategory: equipCategory(eDef),
                equipSlotName: equipSlotNames[eDef?.slot] || eDef?.slot || 'Trang bị',
                equipWtypeName: equipTypeNames[eDef?.wtype] || null,
                equipElement: eDef?.element || null,
                realmMinName: this.realmName(gate.minRealm || 0),
                reqRoleStat: eDef?.reqRoleStat || 0,
                roleStatName: eDef?.wtype === 'bua' ? C.MON.ren.statName : eDef?.wtype === 'dinh' ? C.MON.dan.statName : null,
                tierName: C.TIER[r.tier]?.name || r.tier,
                stats: eDef?.stats || {},
                minRealmName: this.realmName(gate.minRealm || 0),
                successRate,
                toolName: smithTool.def?.name || null,
                toolDurability: smithTool.item?.dur ?? null,
                toolRepairCost: smithTool.item ? craftingToolRepairCost(smithTool.def, smithTool.item.dur) : null,
                fireName: gate.fireName || null,
                fireRequiredRank: gate.requiredFireRank || 0,
                stoneCost: cost.stoneCost,
                reqMats,
                canCraft: !blockReason,
                blockReason,
            };
        });

        const potionRecipes = C.CRAFT_POTION_RECIPES.map(r => {
            const cDef = C.CONSUMABLE_BY_ID.get(r.consId);
            const cost = this.craftingCost(p, r, isAlchemist ? 0.8 : 1);
            const reqMats = Object.fromEntries(Object.entries(cost.reqMats).map(([mid, need]) => [mid, { need, have: myMats[mid] || 0, name: C.MATERIAL_BY_ID.get(mid)?.name || mid, icon: C.MATERIAL_BY_ID.get(mid)?.icon || '🌿' }]));
            const gate = this.craftingGate(userId, p, 'dan', r, cDef || {}, cauldronDef);
            let blockReason = gate.reason;
            if (!blockReason && !this.craftingHasCost(p, cost)) blockReason = p.stones < cost.stoneCost ? 'Cần ' + cost.stoneCost.toLocaleString('vi-VN') + ' linh thạch.' : 'Thiếu dược liệu.';
            const yieldQty = r.yieldQty + (isAlchemist ? 1 : 0);
            if (!blockReason && !this.craftingCanStore(p, 'cons', r.consId, yieldQty)) blockReason = 'Túi và kho không đủ chỗ.';
            const toolRank = Number(cauldronDef?.qualityRank ?? C.TIER[cauldronDef?.tier]?.rank) || 0;
            const successRate = cauldronDef ? clamp(alchemySuccessRate(cauldronDef, gate.rank || craftingTierRank(cDef || {}, r), artisanExpertise(roleStat)) + (gate.fireBonus || 0), 0.05, 0.90) : 0;

            return {
                ...r,
                consName: cDef?.name || r.name,
                consIcon: cDef?.icon || '💊',
                consDesc: cDef?.desc || r.desc,
                craftGroup: cDef?.stamina ? 'stamina' : (cDef?.heal || cDef?.mana ? 'recovery' : (cDef?.breakthrough ? 'breakthrough' : (cDef?.exp ? 'cultivation' : 'other'))),
                yieldQty,
                minRealmName: this.realmName(gate.minRealm || 0),
                successRate,
                alchemySuccessRate: successRate,
                toolName: cauldronDef?.name || null,
                stoneCost: cost.stoneCost,
                reqMats,
                cauldronName: cauldronDef?.name || null,
                toolDurability: alchemyTool.item?.dur ?? null,
                toolRepairCost: alchemyTool.item ? craftingToolRepairCost(cauldronDef, alchemyTool.item.dur) : null,
                fireName: gate.fireName || null,
                fireRequiredRank: gate.requiredFireRank || 0,
                canCraft: !blockReason,
                blockReason,
            };
        });

        const talismanRecipes = (C.CRAFT_TALISMAN_RECIPES || []).map(r => {
            const cDef = C.CONSUMABLE_BY_ID.get(r.talismanId);
            const cost = this.craftingCost(p, r, isScribe ? 0.8 : 1);
            const reqMats = Object.fromEntries(Object.entries(cost.reqMats).map(([mid, need]) => [mid, { need, have: myMats[mid] || 0, name: C.MATERIAL_BY_ID.get(mid)?.name || mid, icon: C.MATERIAL_BY_ID.get(mid)?.icon || '📜' }]));
            const gate = this.craftingGate(userId, p, 'phu', r, cDef || {}, scribeTool.def);
            let blockReason = gate.reason;
            if (!blockReason && !this.craftingHasCost(p, cost)) blockReason = p.stones < cost.stoneCost ? 'Cần ' + cost.stoneCost.toLocaleString('vi-VN') + ' linh thạch.' : 'Thiếu nguyên liệu vẽ phù.';
            const yieldQty = r.yieldQty + (isScribe ? 1 : 0);
            if (!blockReason && !this.craftingCanStore(p, 'cons', r.talismanId, yieldQty)) blockReason = 'Túi và kho không đủ chỗ.';
            const toolRank = Number(scribeTool.def?.qualityRank ?? C.TIER[scribeTool.def?.tier]?.rank) || 0;
            const successRate = scribeTool.def ? talismanSuccessRate(gate.rank || craftingTierRank(cDef || {}, r), toolRank, artisanExpertise(roleStat)) : 0;
            return {
                ...r,
                consName: cDef?.name || r.name,
                consIcon: cDef?.icon || '📜',
                consDesc: cDef?.desc || r.desc,
                tierName: C.TIER[cDef?.tier]?.name || 'Phàm',
                craftGroup: cDef?.tier || 'pham',
                yieldQty,
                minRealmName: this.realmName(gate.minRealm || 0),
                successRate,
                toolName: scribeTool.def?.name || null,
                toolDurability: scribeTool.item?.dur ?? null,
                toolRepairCost: scribeTool.item ? craftingToolRepairCost(scribeTool.def, scribeTool.item.dur) : null,
                learned: !r.needLearn || Boolean(p.learnedRecipes?.[r.id]),
                reqCraftXp: r.reqCraftXp || 0,
                scribeProgress: this.scribeCraftProgress(p),
                stoneCost: cost.stoneCost,
                reqMats,
                canCraft: !blockReason,
                blockReason,
            };
        });

        return {
            isSmith,
            isAlchemist,
            isScribe,
            roleStat,
            smithToolName: smithTool.def?.name || null,
            scribeToolName: scribeTool.def?.name || null,
            cauldronName: cauldronDef?.name || null,
            tools: { cauldron: toolState(alchemyTool), hammer: toolState(smithTool), brush: toolState(scribeTool) },
            fireSlots,
            ownedFires,
            scribeProgress: this.scribeCraftProgress(p),
            stones: p.stones,
            equipRecipes,
            potionRecipes,
            talismanRecipes,
            materials: C.MATERIALS.map(m => ({ ...m, count: myMats[m.id] || 0 })),
        };
    }

    craftEquip(userId, recipeId) {
        const p = this.requirePlayer(userId);
        this.requireIdle(p);
        const recipe = C.CRAFT_EQUIP_RECIPE_BY_ID.get(recipeId);
        if (!recipe) fail('Không tìm thấy công thức rèn đúc.');
        const equipDef = C.EQUIP_BY_ID.get(recipe.equipId);
        if (!equipDef) fail('Không tìm thấy bản vẽ trang bị.');
        const isSmith = p.mon === 'ren';
        const tool = this.craftingTool(p, 'ren');
        const gate = this.craftingGate(userId, p, 'ren', recipe, equipDef, tool.def);
        if (gate.reason) fail(gate.reason);
        if (!this.canCreate('item', equipDef.id)) fail('Vật phẩm này đã hết hạn mức tạo trong thế giới.');
        if (!this.craftingCanStore(p, 'equip', equipDef.id, 1)) fail('Túi và kho đã đầy, không thể rèn thêm trang bị.');
        const cost = this.craftingCost(p, recipe, isSmith ? 0.8 : 1);
        if (p.stones < cost.stoneCost) fail('Không đủ linh thạch. Cần ' + cost.stoneCost.toLocaleString('vi-VN') + ' linh thạch.');
        if (!this.craftingHasCost(p, cost)) fail('Không đủ nguyên liệu rèn theo công thức.');

        const toolRank = Number(tool.def.qualityRank ?? C.TIER[tool.def.tier]?.rank) || 0;
        const successRate = clamp(forgingSuccessRate(gate.rank, toolRank, artisanExpertise(p.roleStats?.ren)) + (gate.fireBonus || 0), 0.05, 0.90);
        this.payCraftingCost(p, cost);
        if (this.rng() >= successRate) {
            const wear = this.damageCraftingTool(p, 'ren', tool, gate.rank);
            this.touch();
            return { success: true, failed: true, successRate, toolDurability: wear.durability, toolBroken: wear.broken, message: 'Rèn thất bại (' + Math.round(successRate * 100) + '%). Nguyên liệu và linh thạch đã mất.' + wear.message };
        }

        const now = this.now();
        const bonusStats = {};
        for (const [k, v] of Object.entries(equipDef.stats || {})) {
            bonusStats[k] = isSmith ? Math.round(v * 1.1) : v;
        }

        const item = this.addEquip(p, {
            ...equipDef,
            stats: bonusStats,
            tierName: equipDef.tierName || C.TIER[equipDef.tier]?.name,
        }, now);

        if (!item) fail('Túi đồ và kho đã đầy, không thể chứa trang bị mới rèn.');
        if (isSmith) item.craftedByMaster = true;

        this.touch();
        const locMsg = item.place === 'kho' ? ' (Túi đầy, đã chuyển thẳng vào Kho đồ)' : '';
        return {
            success: true,
            itemName: equipDef.name,
            place: item.place,
            isSmith,
            successRate,
            message: (isSmith ? '⚡ Thợ Rèn ' : '🔨 ') + 'rèn thành công [' + equipDef.name + '] (' + Math.round(successRate * 100) + '%).' + (isSmith ? ' Giảm 20% chi phí, +10% chỉ số.' : '') + locMsg,
        };
    }

    craftPotion(userId, recipeId) {
        const p = this.requirePlayer(userId);
        this.requireIdle(p);
        const recipe = C.CRAFT_POTION_RECIPE_BY_ID.get(recipeId);
        if (!recipe) fail('Không tìm thấy đan phương.');
        const consDef = C.CONSUMABLE_BY_ID.get(recipe.consId);
        if (!consDef) fail('Không tìm thấy đan dược.');
        const isAlchemist = p.mon === 'dan';
        const tool = this.craftingTool(p, 'dan');
        const gate = this.craftingGate(userId, p, 'dan', recipe, consDef, tool.def);
        if (gate.reason) fail(gate.reason);
        const yieldQty = recipe.yieldQty + (isAlchemist ? 1 : 0);
        if (!this.craftingCanStore(p, 'cons', recipe.consId, yieldQty)) fail('Túi và kho không đủ chỗ chứa mẻ đan này.');
        const cost = this.craftingCost(p, recipe, isAlchemist ? 0.8 : 1);
        if (p.stones < cost.stoneCost) fail('Không đủ linh thạch. Cần ' + cost.stoneCost.toLocaleString('vi-VN') + ' linh thạch.');
        if (!this.craftingHasCost(p, cost)) fail('Không đủ dược liệu theo đan phương.');
        const toolRank = Number(tool.def.qualityRank ?? C.TIER[tool.def.tier]?.rank) || 0;
        const successRate = clamp(alchemySuccessRate(tool.def, gate.rank, artisanExpertise(p.roleStats?.dan)) + (gate.fireBonus || 0), 0.05, 0.90);
        this.payCraftingCost(p, cost);

        if (this.rng() >= successRate) {
            const wear = this.damageCraftingTool(p, 'dan', tool, gate.rank);
            this.touch();
            return { success: true, failed: true, successRate, toolDurability: wear.durability, toolBroken: wear.broken, message: 'Luyện đan thất bại (' + Math.round(successRate * 100) + '% với ' + tool.def.name + '); nguyên liệu và linh thạch đã mất.' + wear.message };
        }

        // Thêm đan dược vào Túi, nếu đầy tràn vào Kho
        let added = this.addStack(p, 'cons', recipe.consId, yieldQty, 'bag');
        let toKho = 0;
        if (added < yieldQty) {
            const addedKho = this.addStack(p, 'cons', recipe.consId, yieldQty - added, 'kho');
            toKho = addedKho;
            added += addedKho;
        }
        if (added <= 0) fail('Hành trang và Kho đồ đều đã đầy, không thể chứa thêm đan dược mới luyện.');

        this.touch();
        const locNotice = toKho > 0 ? ' (' + toKho + ' viên chuyển vào Kho do túi đầy)' : '';
        return {
            success: true,
            yieldQty: added,
            potionName: consDef.name,
            isAlchemist,
            successRate,
            cauldronName: tool.def.name,
            message: isAlchemist
                ? '🌿 Đan Sư luyện thành [' + consDef.name + '] ×' + added + ' (' + Math.round(successRate * 100) + '%). Giảm 20% chi phí, thêm 1 viên.' + locNotice
                : '💊 Luyện thành [' + consDef.name + '] ×' + added + ' (' + Math.round(successRate * 100) + '%).' + locNotice,
        };
    }

    craftTalisman(userId, recipeId) {
        const p = this.requirePlayer(userId);
        this.requireIdle(p);
        const recipe = C.CRAFT_TALISMAN_RECIPE_BY_ID.get(recipeId);
        if (!recipe) fail('Không tìm thấy phù phương.');
        const consDef = C.CONSUMABLE_BY_ID.get(recipe.talismanId);
        if (!consDef) fail('Không tìm thấy loại phù cần vẽ.');
        const isScribe = p.mon === 'phu';
        const tool = this.craftingTool(p, 'phu');
        const gate = this.craftingGate(userId, p, 'phu', recipe, consDef, tool.def);
        if (gate.reason) fail(gate.reason);
        const yieldQty = recipe.yieldQty + (isScribe ? 1 : 0);
        if (!this.craftingCanStore(p, 'cons', recipe.talismanId, yieldQty)) fail('Túi và kho không đủ chỗ chứa số phù này.');
        const cost = this.craftingCost(p, recipe, isScribe ? 0.8 : 1);
        if (p.stones < cost.stoneCost) fail('Không đủ linh thạch. Cần ' + cost.stoneCost.toLocaleString('vi-VN') + ' linh thạch.');
        if (!this.craftingHasCost(p, cost)) fail('Không đủ nguyên liệu vẽ phù theo phù phương.');
        const toolRank = Number(tool.def.qualityRank ?? C.TIER[tool.def.tier]?.rank) || 0;
        const successRate = talismanSuccessRate(gate.rank, toolRank, artisanExpertise(p.roleStats?.phu));
        this.payCraftingCost(p, cost);
        if (this.rng() >= successRate) {
            const wear = this.damageCraftingTool(p, 'phu', tool, gate.rank);
            const craftXp = this.awardScribeCraftXp(p, gate.rank, false);
            this.touch();
            return { success: true, failed: true, successRate, toolDurability: wear.durability, toolBroken: wear.broken, craftXpGained: craftXp?.gained || 0, scribeProgress: craftXp?.progress, message: 'Vẽ phù thất bại (' + Math.round(successRate * 100) + '% với ' + tool.def.name + '); nguyên liệu, giấy, mực và linh thạch đã mất.' + wear.message + (craftXp ? ` Tiến triển Phù Sư +${craftXp.gained} điểm.` : '') };
        }
        let added = this.addStack(p, 'cons', recipe.talismanId, yieldQty, 'bag');
        if (added < yieldQty) added += this.addStack(p, 'cons', recipe.talismanId, yieldQty - added, 'kho');
        if (added <= 0) fail('Không còn chỗ để nhận phù vừa vẽ.');
        const craftXp = this.awardScribeCraftXp(p, gate.rank, true);
        this.touch();
        return { success: true, itemName: consDef.name, yieldQty: added, successRate, craftXpGained: craftXp?.gained || 0, scribeProgress: craftXp?.progress, message: '📜 Phù Sư vẽ thành công [' + consDef.name + '] ×' + added + ' (' + Math.round(successRate * 100) + '%). Giảm 20% chi phí, thêm 1 lá.' + (craftXp ? ` Tiến triển Phù Sư +${craftXp.gained} điểm.` : '') };
    }

    partyBuff(userId, potionId = 'hoi_xuan_dan') {
        const p = this.requirePlayer(userId);
        if (p.mon !== 'dan') fail('Chỉ có Đan Sư mới có thể thi triển thuật Dưỡng Sinh cứu chữa cho đồng đội.');
        const party = this.partyOf(userId);
        if (!party) fail('Bạn chưa ở trong tổ đội nào.');

        const potionItem = p.items.find(it => it.kind === 'cons' && it.id === potionId && it.place === 'bag' && it.qty > 0);
        if (!potionItem) {
            const def = C.CONSUMABLE_BY_ID.get(potionId);
            fail(`Bạn không có [${def?.name || potionId}] trong túi để hồi phục cho đội.`);
        }

        const now = this.now();
        const def = C.CONSUMABLE_BY_ID.get(potionId);
        const healPct = def.heal || 0.3;
        const cleanse = Boolean(def.cleanse);

        this.consume(p, potionItem, 1);

        let healedCount = 0;
        for (const memId of party.members) {
            const mem = this.player(memId);
            if (!mem) continue;
            const mst = this.stats(mem, now);
            const curHp = mem.hp == null ? mst.hp : mem.hp;
            const healAmount = Math.round(mst.hp * healPct);
            mem.hp = Math.min(mst.hp, curHp + healAmount);
            if (cleanse && mem.injuredUntil && mem.injuredUntil > now) {
                mem.injuredUntil = 0;
            }
            healedCount += 1;
        }

        this.touch();
        return {
            success: true,
            potionName: def.name,
            healedCount,
            message: `🌿 Đan Sư thi triển đan dược [${def.name}], hồi phục Khí Huyết cho ${healedCount} đồng đội trong tổ đội!`,
        };
    }

    // ---- Đột Phá Cảnh Giới ----------------------------------------------------
    canBreakthrough(userId) {
        const p = this.requirePlayer(userId);
        const realm = this.realmOf(userId);
        const targetRealmIndex = realm.index + 1;
        const blockedByAscension = realm.index >= 11 && !p.ascended;
        const atBottleneck = realm.isMaxRealm ? false : (Number(realm.experience) || 0) >= realm.levelCap;
        const stageNumber = Math.min(5, (realm.subIndex || 0) + 1);
        const isMajorBreakthrough = stageNumber === SUB_STAGES.length;
        // Bốn lần đột phá tiểu cảnh dùng đan riêng của cảnh giới hiện tại.
        // Lần đột phá đại cảnh giới dùng vật phẩm BREAKTHROUGH_BY_REALM và nguyên liệu theo bậc.
        const reqItem = isMajorBreakthrough
            ? (C.BREAKTHROUGH_BY_REALM[targetRealmIndex] || null)
            : (C.SUBSTAGE_BY_REALM?.[realm.index] || null);
        const reqMaterialId = isMajorBreakthrough ? breakthroughMaterialId(targetRealmIndex) : null;
        const reqMaterials = [];
        if (reqMaterialId) {
            const material = C.MATERIAL_BY_ID?.get(reqMaterialId) || { name: reqMaterialId, icon: '🪨' };
            const need = targetRealmIndex <= 3 ? 2 : 1;
            const have = p.items.filter(it => it.kind === 'mat' && it.id === reqMaterialId && ['bag', 'kho'].includes(it.place))
                .reduce((sum, it) => sum + (it.qty || 1), 0);
            const source = `Nguyên liệu cho đột phá cảnh giới mới${targetRealmIndex <= 3 ? ' (cần 2 Yêu Đan)' : ' — rơi hiếm từ yêu thú/boss theo cảnh giới.'}`;
            reqMaterials.push({ id: reqMaterialId, name: material.name, icon: material.icon || '🪨', need, have, source });
        }
        if (targetRealmIndex === 11 && stageNumber === SUB_STAGES.length) {
            const material = C.MATERIAL_BY_ID?.get('mat_thien_dao_tinh') || { name: 'Thiên Đạo Tinh Thạch', icon: '⚡' };
            const have = p.items.filter(it => it.kind === 'mat' && it.id === 'mat_thien_dao_tinh' && ['bag', 'kho'].includes(it.place))
                .reduce((sum, it) => sum + (it.qty || 1), 0);
            reqMaterials.push({ id: 'mat_thien_dao_tinh', name: material.name, icon: material.icon || '⚡', need: 2, have, source: 'Chỉ rơi từ thủ lĩnh Cổ Động cấp cao sau khi vượt hết các ải.' });
        }

        let itemCount = 0;
        if (reqItem) {
            for (const it of p.items) {
                const isMatch = (it.id === reqItem.id) ||
                                (reqItem.id === 'truc_co_dan' && it.id === 'dan_truc_co') ||
                                (reqItem.id === 'dan_truc_co' && it.id === 'truc_co_dan');
                if (it.kind === 'cons' && isMatch && (it.place === 'bag' || it.place === 'kho')) {
                    itemCount += (it.qty || 1);
                }
            }
        }

        const now = this.now();
        const st = this.stats(p, now);

        // Lực chiến Thiên Đạo chuẩn tại mốc cảnh giới này (Lôi Kiếp áp chế tăng mạnh theo cảnh giới)
        const difficultyRealm = Math.max(1, targetRealmIndex + ((stageNumber - 1) * 0.2));
        let daoPower;
        if (targetRealmIndex <= 10) {
            // Phàm Giới: Lực chiến chuẩn từ ~290 (Luyện Thể) đến ~12.500 (Độ Kiếp)
            daoPower = Math.round(200 * Math.pow(1.46, difficultyRealm));
        } else if (targetRealmIndex === 11) {
            // Mốc Phi Thăng Kiếp (từ Độ Kiếp tầng 5 lên Bán Tiên):
            // Thiên Đạo giáng Cửu Trọng Thiên Kiếp dữ dội! Lực chiến người chơi lúc này tầm 14.000 - 16.000.
            // Lôi Kiếp ở mức 17.500 - 18.000 tạo thử thách tự nhiên, khó nhưng công bằng và hoàn toàn không cần trần 25%.
            daoPower = Math.round(15200 + (stageNumber - 1) * 650);
        } else {
            // Tiên Giới (12+): Tiên Đạo Lôi Kiếp tăng tiến theo lực chiến trang bị Tiên Khí
            daoPower = Math.round(50000 * Math.pow(1.22, difficultyRealm - 11));
        }

        // Tỷ lệ thắng Thiên Đạo: tính theo tương quan lực chiến người chơi vs Thiên Đạo
        // Khi lực chiến bằng nhau: 55%. Lực chiến vượt càng cao thì tỷ lệ càng cao (tối đa 88%)
        const powerRatio = st.power / Math.max(1, daoPower);
        let winRate = Math.round(55 + (powerRatio - 1) * 45);
        if ((p.daoScore || 0) >= 50) winRate += 5;
        if (p.linhCan && (p.linhCan.includes('Thiên') || p.linhCan.includes('Chân'))) winRate += 5;
        if (st.isThienKieu) winRate += 5;
        if (p.hp != null && p.hp < st.hp * 0.3) winRate -= 20;
        if (p.injuredUntil && p.injuredUntil > now) winRate -= 30;
        winRate -= (stageNumber - 1) * 4;
        const finalAscensionGate = targetRealmIndex === 11 && stageNumber === SUB_STAGES.length;
        // Bỏ trần cứng 25%: Thiên Đạo Lôi Đình đã đo lường sát sao theo thực lực, người chơi chuẩn bị kỹ lưỡng vẫn có thể đạt tỷ lệ cao
        winRate = Math.max(10, Math.min(88, winRate));

        const hasRequiredMaterials = reqMaterials.every(m => m.have >= m.need);
        const hasRequiredPill = !reqItem || itemCount >= 1;

        return {
            currentRealm: realm,
            targetRealmIndex,
            blockedByAscension,
            finalAscensionGate,
            isMaxRealm: Boolean(realm.isMaxRealm),
            atBottleneck,
            stageNumber,
            completedStages: realm.subIndex || 0,
            currentSubIndex: realm.subIndex || 0,
            stageCount: SUB_STAGES.length,
            requiredExp: realm.levelCap,
            reqItem,
            itemCount,
            requiredItemQty: reqItem ? 1 : 0,
            hasItem: hasRequiredPill,
            reqMaterials,
            hasRequiredMaterials,
            daoPower,
            playerPower: st.power,
            successRate: winRate,
            winRate,
            subStages: SUB_STAGES,
            isMajorBreakthrough,
            canBreakthrough: !blockedByAscension && atBottleneck && hasRequiredPill && hasRequiredMaterials,
        };
    }

    breakthrough(userId) {
        const p = this.requirePlayer(userId);
        const info = this.canBreakthrough(userId);
        if (info.isMaxRealm) fail('Bạn đã đạt tới đỉnh cao cảnh giới tu vi trong thiên địa!');
        if (info.blockedByAscension) fail('Cần hoàn thành Phi Thăng Tiên Giới trước khi đột phá cảnh giới tiếp theo.');
        if (!info.atBottleneck) fail(`Tu vi chưa đạt mốc ${info.stageNumber}/5 (${info.currentRealm.experience}/${info.requiredExp} EXP). Hãy tiếp tục tu luyện hoặc săn yêu!`);
        if (info.reqItem && !info.hasItem) {
            fail(`Thiếu đan dược đột phá: [${info.reqItem.name} ×${info.requiredItemQty}]. Hãy săn yêu quái, khám phá bí cảnh hang động hoặc nhờ Đan Sư luyện chế!`);
        }
        const missingMaterial = info.reqMaterials.find(m => m.have < m.need);
        if (missingMaterial) {
            const source = missingMaterial.source ? ` ${missingMaterial.source}` : (missingMaterial.id === 'mat_thien_dao_tinh'
                ? ' Chỉ rơi từ thủ lĩnh Cổ Động cấp cao sau khi vượt hết các ải.'
                : '');
            fail(`Thiếu vật liệu đột phá: [${missingMaterial.name} ×${missingMaterial.need}] (đang có ${missingMaterial.have}).${source}`);
        }

        const isRequiredBreakthroughPill = it => info.reqItem && it.kind === 'cons' && (
            it.id === info.reqItem.id ||
            (info.reqItem.id === 'truc_co_dan' && it.id === 'dan_truc_co') ||
            (info.reqItem.id === 'dan_truc_co' && it.id === 'truc_co_dan')
        );
        const consumeBreakthroughCosts = () => {
            const consumed = [];
            if (info.reqItem) {
                const item = p.items.find(it => isRequiredBreakthroughPill(it) && it.place === 'bag')
                    || p.items.find(it => isRequiredBreakthroughPill(it) && it.place === 'kho');
                if (item) {
                    if (item.qty == null) item.qty = 1;
                    this.consume(p, item, 1);
                    consumed.push(`${info.reqItem.name} ×1`);
                }
            }
            for (const material of info.reqMaterials) {
                let remaining = material.need;
                for (const place of ['bag', 'kho']) {
                    for (const item of [...p.items]) {
                        if (remaining <= 0) break;
                        if (item.kind !== 'mat' || item.id !== material.id || item.place !== place) continue;
                        if (item.qty == null) item.qty = 1;
                        const take = Math.min(remaining, item.qty);
                        this.consume(p, item, take);
                        consumed.push(`${material.name} ×${take}`);
                        remaining -= take;
                    }
                }
            }
            return consumed;
        };

        // Kiểm tra Thiên Đạo Phạt nếu lực chiến không thắng nổi Thiên Đạo
        const rate = (info.winRate || info.successRate || 80) / 100;
        const roll = this.rng();
        if (roll > rate) {
            // THIÊN ĐẠO PHẠT
            const now = this.now();
            const curExp = (info.currentRealm?.experience != null ? info.currentRealm.experience : p.experience) || 0;
            const lostExp = Math.max(1, Math.round(curExp * 0.10));
            const consumed = consumeBreakthroughCosts();
            if (this.realms?.loseExp) this.realms.loseExp(userId, lostExp);
            if (p.experience != null) p.experience = Math.max(0, p.experience - lostExp);
            p.hp = 1;
            p.injuredUntil = now + C.RULES.injuryMs * 2; // Nhân đôi thời gian trọng thương (6 phút)
            this.touch();
            return {
                success: false,
                penalty: true,
                expLost: lostExp,
                consumed,
                message: `⚡ [THIÊN ĐẠO PHẠT] Đột phá thất bại (tỷ lệ thành công ${info.winRate}%). Mất ${lostExp} EXP (10% tu vi hiện tại) và toàn bộ vật phẩm dùng cho lần đột phá này${consumed.length ? `: ${consumed.join(', ')}` : ''}. Bạn bị trọng thương 10 phút!`
            };
        }

        const res = this.realms.breakthrough(userId, false);
        if (!res || !res.success) {
            fail(res?.reason === 'not_enough_exp' ? 'Kinh nghiệm chưa đủ để phá vỡ bình cảnh.' : 'Đột phá thất bại.');
        }

        // Vật phẩm cần thiết được tiêu hao cho cả lần đột phá thành công lẫn thất bại.
        consumeBreakthroughCosts();

        const st = this.stats(p, this.now());
        if (res.isStageBreakthrough) {
            this.touch();
            return {
                success: true,
                stageBreakthrough: true,
                stage: res.stage,
                newRealm: res.newRealm,
                message: `⚡ [ĐỘT PHÁ TIỂU CẢNH] Vượt qua mốc ${res.stage}/5 (${SUB_STAGES[Math.min(4, res.stage)]}) của ${res.newRealm.name}! Các lần đột phá tiểu cảnh tiếp theo cần ${C.SUBSTAGE_BY_REALM?.[this.realmOf(userId).index]?.name || 'đan riêng của cảnh giới'}, còn đột phá đại cảnh giới cần vật phẩm riêng.`,
            };
        }

        p.hp = st.hp;
        this.grantRealmSkills(p);
        this.touch();

        const ascensionUnlocked = !p.ascended && this.realmOf(userId).index >= 11;

        return {
            success: true,
            newRealm: res.newRealm,
            ascensionUnlocked,
            message: `⚡ [CHIẾN THẮNG THIÊN ĐẠO] Vượt qua Thiên Đạo (tỷ lệ thành công: ${info.winRate}%)! Chúc mừng đạo hữu đã bước vào cảnh giới [${res.newRealm.name}]!${ascensionUnlocked ? ' 🌌 Đã mở nghi thức Phi Thăng Tiên Giới; hãy tìm Thiên Đạo Nguyên Ấn trong Cổ Động cấp cao.' : ''}`,
        };
    }

    canAscend(userId) {
        const p = this.requirePlayer(userId);
        const realm = this.realmOf(userId);
        const itemId = 'mat_thien_dao_nguyen_an';
        const itemDef = C.MATERIAL_BY_ID?.get(itemId) || { name: 'Thiên Đạo Nguyên Ấn', icon: '🌌' };
        const have = p.items.filter(it => it.kind === 'mat' && it.id === itemId && ['bag', 'kho'].includes(it.place))
            .reduce((sum, it) => sum + (it.qty || 1), 0);
        const realmReady = realm.index >= 11;
        const activeBattle = Boolean(this.activeBattle(userId));
        const travelling = Boolean(p.traveling);
        return {
            ascended: Boolean(p.ascended),
            available: realmReady && !p.ascended,
            canAscend: realmReady && !p.ascended && have >= 1 && !activeBattle && !travelling,
            realmReady,
            realmIndex: realm.index,
            realmName: realm.name,
            requiredRealmIndex: 11,
            requiredRealmName: this.realmName(11),
            itemId,
            itemName: itemDef.name,
            itemIcon: itemDef.icon || '🌌',
            have,
            need: 1,
            activeBattle,
            travelling,
            source: 'Rơi từ thủ lĩnh Cổ Động hoặc Thí Luyện Man Châu (Map 8: Độ Kiếp Đài, Tổ Long Đảo, Ma Quật) với tỉ lệ cao (25% - 35%) khi hoàn thành toàn bộ các ải.',
        };
    }

    ascend(userId) {
        const p = this.requirePlayer(userId);
        const info = this.canAscend(userId);
        if (p.ascended) fail('Đạo hữu đã Phi Thăng Tiên Giới rồi.');
        if (!info.realmReady) fail(`Cần đạt cảnh giới ${info.requiredRealmName} trước khi Phi Thăng.`);
        if (info.activeBattle) fail('Hãy kết thúc trận chiến trước khi thực hiện nghi thức Phi Thăng.');
        if (info.travelling) fail('Hãy tới nơi trước khi thực hiện nghi thức Phi Thăng.');
        if (info.have < 1) fail(`Thiếu [${info.itemName} ×1]. ${info.source}`);

        const item = p.items.find(it => it.kind === 'mat' && it.id === info.itemId && it.place === 'bag')
            || p.items.find(it => it.kind === 'mat' && it.id === info.itemId && it.place === 'kho');
        if (!item) fail(`Thiếu [${info.itemName} ×1]. ${info.source}`);
        const currentTown = C.TOWN_BY_ID.get(p.town || 'thanh_van');
        if (C.MAP_BY_ID.get(currentTown?.mapId)?.worldId !== 'world_pham')
            fail('Nghi thức Phi Thăng chỉ có thể hoàn thành tại Phàm Giới.');
        if (item.qty == null) item.qty = 1;
        this.consume(p, item, 1);

        p.ascended = true;
        // The ritual unlocks passage. The player must still walk to the unique
        // ascension gate in Man Châu before the displayed realm map changes.
        p.town = currentTown.id;
        p.mapId = currentTown.mapId;
        const stats = this.stats(p, this.now());
        p.hp = stats.hp;
        p.injuredUntil = 0;
        this.grantRealmSkills(p);
        this.touch();
        return {
            success: true,
            ascended: true,
            townId: currentTown.id,
            mapId: currentTown.mapId,
            message: '🌌 Thiên Đạo Nguyên Ấn đã mở Cổng Phi Thăng. Hãy tới Cổng Phi Thăng tại Man Châu để bước sang Tiên Giới.',
        };
    }

    // ---- Cổ Động Bí Cảnh & Hang Động Cơ Duyên ---------------------------------
    dungeonMonsterForBattle(dungeon, stage, stageIndex, now) {
        const stages = dungeon?.stages || [];
        if (!stages.length) return null;
        let targetIndex = Number.isInteger(stageIndex) ? stageIndex : stages.indexOf(stage);
        if (targetIndex < 0) targetIndex = 0;
        targetIndex = clamp(targetIndex, 0, stages.length - 1);
        const dState = this.data.dungeonsState?.[dungeon.id] || {};
        let previousStats = null;
        let result = null;

        // Use the same progressively increasing HP/ATK/DEF in previews, manual
        // combat, and the legacy one-request dungeon flow.
        for (let index = 0; index <= targetIndex; index++) {
            const currentStage = stages[index];
            const stageState = dState.stagesState?.[index] || {};
            const actualMonsterId = stageState.monsterId || currentStage.monsterId;
            const def = C.MONSTER_BY_ID.get(actualMonsterId) || C.MONSTER_BY_ID.get(currentStage.monsterId);
            if (!def) continue;

            const base = monsterStats(def, now);
            const isVariant = Boolean(stageState.isVariant);
            const stageRealm = Math.max(0, Number(def.realm) || 0);
            const stageMaxLevel = Math.max(1, 1 + Math.max(0, this.townRealmCap(dungeon.townId || 'thanh_van') - stageRealm) * 2);
            const combatGrowthCap = Math.min(1.35, Math.pow(1.04, stageMaxLevel - 1));
            const growth = Math.min(Number(stageState.atkMul) || 1, combatGrowthCap);
            const hpMul = (Number(stageState.hpMul) || 1) * (isVariant ? 1.25 : 1) * (currentStage.isBoss ? 1.25 : 1);
            const atkMul = growth * (isVariant ? 1.10 : 1) * (currentStage.isBoss ? 1.15 : 1.10);
            const defMul = Math.min(Number(stageState.defMul) || 1, combatGrowthCap)
                * (isVariant ? 1.15 : 1) * (currentStage.isBoss ? 1.20 : 1.15);
            const progression = Math.max(1, Number(currentStage.statProgression) || 1.08);
            const stats = {
                hp: Math.max(1, Math.round(base.hp * hpMul)),
                atk: Math.max(1, Math.round(base.atk * atkMul)),
                def: Math.max(1, Math.round(base.def * defMul)),
            };
            if (previousStats) {
                stats.hp = Math.max(stats.hp, Math.ceil(previousStats.hp * progression));
                stats.atk = Math.max(stats.atk, Math.ceil(previousStats.atk * progression));
                stats.def = Math.max(stats.def, Math.ceil(previousStats.def * progression));
            }
            previousStats = stats;
            result = {
                ...def,
                isDungeon: true,
                isBoss: Boolean(currentStage.isBoss),
                explicitStats: true,
                dungeonVariant: isVariant,
                ...stats,
                maxHp: stats.hp,
            };
        }
        return result;
    }

    cleanStaleDungeonState(p, now = this.now()) {
        if (!p?.activeDungeon) return false;
        const active = p.activeDungeon;
        const startAt = Number(active.startAt) || 0;
        const isExpired = (now - startAt) > 15 * 60 * 1000;
        const isComplete = (active.stageIndex || 0) >= (active.totalStages || 3);
        const battle = this.battles.get(String(p.userId));
        const isBattleDead = !battle || battle.over;

        if (isExpired || isComplete || (isBattleDead && (now - startAt) > 5 * 60 * 1000)) {
            const dState = this.data.dungeonsState?.[active.dungeonId];
            if (dState && dState.fightingBy && (
                dState.fightingBy.userId === String(p.userId) ||
                (dState.fightingBy.memberIds || []).includes(String(p.userId))
            )) {
                dState.fightingBy = null;
            }
            const allMemberIds = active.userIds?.length ? active.userIds.map(String) : [String(p.userId)];
            for (const mId of allMemberIds) {
                const member = this.player(mId);
                if (member?.activeDungeon) {
                    delete member.activeDungeon;
                }
            }
            if (battle && battle.over) {
                this.battles.delete(String(p.userId));
            }
            this.touch();
            return true;
        }
        return false;
    }

    abandonDungeon(userId) {
        const p = this.requirePlayer(userId);
        const active = p.activeDungeon;
        if (!active) {
            return { success: true, message: 'Hiện không có chuyến Cổ Động hay Thí Luyện nào.' };
        }
        const dId = active.dungeonId;
        const dState = this.data.dungeonsState?.[dId];
        if (dState && dState.fightingBy && (
            dState.fightingBy.userId === String(userId) ||
            (dState.fightingBy.memberIds || []).includes(String(userId))
        )) {
            dState.fightingBy = null;
        }
        const allMemberIds = active.userIds?.length ? active.userIds.map(String) : [String(userId)];
        for (const mId of allMemberIds) {
            const member = this.player(mId);
            if (member?.activeDungeon) {
                delete member.activeDungeon;
            }
        }
        const battle = this.battles.get(String(userId));
        if (battle && (battle.over || battle.isDungeon)) {
            this.battles.delete(String(userId));
        }
        this.touch();
        return { success: true, message: 'Đã rút lui và hủy chuyến thám hiểm.' };
    }

    pickEquipTrialReward(d, player) {
        const rawPool = (d.equipPool || []).map(id => C.EQUIP_BY_ID.get(id)).filter(Boolean);
        if (!rawPool.length) return C.EQUIPMENT[0];
        const monWeaponType = C.MON[player?.mon]?.weapon;
        let pool = rawPool.filter(eq => {
            if (eq.slot === 'weapon') {
                return !monWeaponType || eq.wtype === monWeaponType;
            }
            return true;
        });
        if (!pool.length) pool = rawPool;
        const weights = pool.map(eq => {
            if (eq.slot === 'weapon') return 5;
            if (eq.slot === 'armor' || eq.slot === 'acc') return 3;
            if (eq.slot === 'ring1' || eq.slot === 'ring2' || eq.slot === 'nhan_tru_do') return 2;
            return 1;
        });
        const totalWeight = weights.reduce((sum, w) => sum + w, 0);
        let roll = this.rng() * totalWeight;
        for (let i = 0; i < pool.length; i++) {
            roll -= weights[i];
            if (roll <= 0) return pool[i];
        }
        return pool[pool.length - 1];
    }

    getDungeonsView(userId) {
        const p = this.requirePlayer(userId);
        const realm = this.realmOf(userId).index;
        const now = this.now();
        this.cleanStaleDungeonState(p, now);
        const today = vnDate(now);
        const partySize = this.huntPartySize(userId);
        p.dungeonDaily = p.dungeonDaily || { date: today, count: 0 };
        if (p.dungeonDaily.date !== today) {
            p.dungeonDaily = { date: today, count: 0 };
        }
        const dailyCount = p.dungeonDaily.count || 0;
        const dailyMax = 10;
        const dailyRemaining = Math.max(0, dailyMax - dailyCount);

        p.equipTrialDaily = p.equipTrialDaily || { date: today, count: 0 };
        if (p.equipTrialDaily.date !== today) {
            p.equipTrialDaily = { date: today, count: 0 };
        }
        const equipDailyCount = p.equipTrialDaily.count || 0;
        const equipDailyMax = 10;
        const equipDailyRemaining = Math.max(0, equipDailyMax - equipDailyCount);

        this.data.dungeonsState = this.data.dungeonsState || {};
        for (const [dId, dSt] of Object.entries(this.data.dungeonsState)) {
            if (dSt.fightingBy && dSt.fightingBy.until <= now) {
                dSt.fightingBy = null;
            }
        }

        const currentTownId = p.town || 'thanh_van';
        const currentTown = C.TOWN_BY_ID.get(currentTownId);
        const currentMapId = currentTown?.mapId || 'map_1';

        const list = C.DUNGEONS.filter(d => !d.townId || d.townId === currentTownId).map(d => {
            const dState = this.data.dungeonsState[d.id] || {};
            const isTownMatch = Boolean(!d.townId || !p.town || d.townId === p.town);
            const isFighting = Boolean(dState.fightingBy && dState.fightingBy.until > now && dState.fightingBy.userId !== String(userId));
            const isCooldown = Boolean(dState.respawnAt && dState.respawnAt > now);
            const cooldownSec = isCooldown ? Math.max(0, Math.ceil((dState.respawnAt - now) / 1000)) : 0;
            const townDef = d.townId ? C.TOWN_BY_ID.get(d.townId) : null;
            const dungeonMap = C.MAP_BY_ID.get(townDef?.mapId);
            const ascensionOk = !dungeonMap?.ascensionRequired || Boolean(p.ascended);

            return {
                id: d.id,
                category: 'co_dong',
                townId: d.townId || null,
                townName: townDef?.name || d.townId || null,
                isCurrentTown: Boolean(d.townId && p.town && d.townId === p.town),
                partySize,
                requiredPartySize: minimumEncounterPartySize({ isDungeon: true, small: false }, now),
                recommendedPartySize: recommendedEncounterPartySize(now),
                name: d.name,
                novel: d.novel,
                icon: d.icon,
                realmMin: d.realmMin,
                realmMinName: this.realmName(d.realmMin),
                canEnter: isTownMatch && ascensionOk && dailyRemaining > 0 && !isFighting && !isCooldown,
                realmOk: ascensionOk,
                stamina: d.stamina,
                desc: d.desc,
                isFighting,
                fightingBy: isFighting ? dState.fightingBy.name : null,
                isCooldown,
                cooldownSec,
                respawnAt: dState.respawnAt || 0,
                lastClearedBy: dState.lastClearedBy || null,
                stages: d.stages.map((stg, sIdx) => {
                    const stageState = dState.stagesState?.[sIdx] || {};
                    const actualMonsterId = stageState.monsterId || stg.monsterId;
                    const mDef = C.MONSTER_BY_ID.get(actualMonsterId) || C.MONSTER_BY_ID.get(stg.monsterId);
                    const combatMonster = this.dungeonMonsterForBattle(d, stg, sIdx, now) || mDef || {};
                    const isVariant = Boolean(stageState.isVariant);
                    const baseMonsterName = canonicalMonsterName(mDef?.name || stageState.baseMonsterName || stg.name || stg.monsterId || 'Yêu thú');
                    const isMaHoa = Boolean(stageState.isMaHoa || String(stageState.monsterName || '').includes('🔥 [Ma Hóa'));
                    const monsterDisplayName = baseMonsterName;
                    return {
                        id: stg.id,
                        name: stg.name,
                        monsterId: actualMonsterId,
                        icon: mDef?.icon || '👾',
                        isBoss: Boolean(stg.isBoss),
                        isVariant,
                        isMaHoa,
                        monsterName: monsterDisplayName,
                        monsterIcon: mDef?.icon || '👾',
                        element: mDef?.element || 'kim',
                        elementName: C.HE[mDef?.element || 'kim']?.name || mDef?.element || 'Kim',
                        realm: mDef?.realm || 0,
                        realmName: `${this.realmName(mDef?.realm || 0)}${stg.substageName ? ` · ${stg.substageName}` : ''}`,
                        hp: combatMonster.hp || 1,
                        atk: combatMonster.atk || 1,
                        def: combatMonster.def || 1,
                        spd: mDef?.spd || 10,
                        substageName: stg.substageName || null,
                        skills: (mDef?.skills || []).map(skId => C.SKILL_BY_ID.get(skId)?.name || skId),
                        devoured: stageState.devoured || [],
                    };
                }),
                guaranteedPill: d.guaranteedPill,
                guaranteedPillName: C.CONSUMABLE_BY_ID.get(d.guaranteedPill)?.name || d.guaranteedPill,
                guaranteedPillIcon: C.CONSUMABLE_BY_ID.get(d.guaranteedPill)?.icon || '💊',
                equipDrop: C.EQUIP_BY_ID.get(d.equipDrop)?.name || null,
                stones: d.stones,
                expReward: d.expReward,
            };
        });

        // Đưa Cổ Động của thành trấn hiện tại lên đầu tiên
        list.sort((a, b) => {
            if (a.isCurrentTown && !b.isCurrentTown) return -1;
            if (!a.isCurrentTown && b.isCurrentTown) return 1;
            return a.realmMin - b.realmMin;
        });

        // 2. Thí Luyện Trang Bị (19 Maps)
        const equipTrials = (C.EQUIP_DUNGEONS || []).map(d => {
            const dState = this.data.dungeonsState[d.id] || {};
            const isMapMatch = Boolean(d.mapId === currentMapId);
            const isFighting = Boolean(dState.fightingBy && dState.fightingBy.until > now && dState.fightingBy.userId !== String(userId));
            const isCooldown = Boolean(dState.respawnAt && dState.respawnAt > now);
            const cooldownSec = isCooldown ? Math.max(0, Math.ceil((dState.respawnAt - now) / 1000)) : 0;
            const ascensionOk = !d.ascensionRequired || Boolean(p.ascended);

            return {
                id: d.id,
                category: 'thi_luyen',
                isEquipTrial: true,
                mapId: d.mapId,
                mapName: d.mapName,
                name: d.name,
                icon: d.icon,
                realmMin: d.realmMin,
                realmMax: d.realmMax,
                realmMinName: this.realmName(d.realmMin),
                canEnter: isMapMatch && ascensionOk && equipDailyRemaining > 0 && !isFighting && !isCooldown,
                realmOk: ascensionOk,
                isCurrentMap: isMapMatch,
                partySize,
                requiredPartySize: minimumEncounterPartySize({ isDungeon: true, small: false }, now),
                recommendedPartySize: recommendedEncounterPartySize(now),
                stamina: d.stamina,
                desc: d.desc,
                isFighting,
                fightingBy: isFighting ? dState.fightingBy.name : null,
                isCooldown,
                cooldownSec,
                respawnAt: dState.respawnAt || 0,
                lastClearedBy: dState.lastClearedBy || null,
                stages: d.stages.map((stg, sIdx) => {
                    const mDef = C.MONSTER_BY_ID.get(stg.monsterId) || {};
                    const combatMonster = this.dungeonMonsterForBattle(d, stg, sIdx, now) || mDef;
                    return {
                        id: stg.id,
                        name: stg.name,
                        monsterId: stg.monsterId,
                        icon: mDef.icon || '👾',
                        isBoss: Boolean(stg.isBoss),
                        monsterName: mDef.name || 'Thủ Hộ Giả',
                        monsterIcon: mDef.icon || '👾',
                        element: mDef.element || 'kim',
                        elementName: C.HE[mDef.element || 'kim']?.name || 'Kim',
                        realm: mDef.realm || d.realmMin,
                        realmName: this.realmName(mDef.realm || d.realmMin),
                        hp: combatMonster.hp || 1,
                        atk: combatMonster.atk || 1,
                        def: combatMonster.def || 1,
                        spd: mDef.spd || 10,
                        skills: (mDef?.skills || []).map(skId => C.SKILL_BY_ID.get(skId)?.name || skId),
                    };
                }),
                rewardGearNames: (d.equipPool || []).slice(0, 5).map(id => C.EQUIP_BY_ID.get(id)?.name).filter(Boolean),
                stones: d.stones,
                expReward: d.expReward,
            };
        });

        equipTrials.sort((a, b) => {
            if (a.isCurrentMap && !b.isCurrentMap) return -1;
            if (!a.isCurrentMap && b.isCurrentMap) return 1;
            return a.realmMin - b.realmMin;
        });

        list.dailyCount = dailyCount;
        list.dailyMax = dailyMax;
        list.dailyRemaining = dailyRemaining;
        list.equipTrials = equipTrials;
        list.equipTrialDaily = {
            count: equipDailyCount,
            max: equipDailyMax,
            remaining: equipDailyRemaining,
        };
        list.activeDungeon = p.activeDungeon ? {
            ...p.activeDungeon,
            dungeonName: C.DUNGEON_BY_ID.get(p.activeDungeon.dungeonId)?.name || 'Cổ Động',
            stageName: p.activeDungeon.stages?.[p.activeDungeon.stageIndex]?.name || `Ải ${(p.activeDungeon.stageIndex || 0) + 1}`,
        } : null;

        return list;
    }

    startDungeonBattle(userId, dungeonId) {
        const p = this.requirePlayer(userId);
        this.requireNotKnockedOutInDungeon(p);
        this.cleanStaleDungeonState(p);
        const key = String(userId);
        const now = this.now();
        const today = vnDate(now);

        // Deadlock resolver: Auto-resume or auto-abandon stale dungeons
        if (p.activeDungeon) {
            const current = this.battles.get(key);
            if (current && !current.over) {
                fail('Đang trong một trận chiến khác, hãy hoàn thành trước.');
            }
            if (p.activeDungeon.leaderId && p.activeDungeon.leaderId !== key) {
                fail('Bạn đang trong một chuyến Cổ Động khác.');
            }
            if (p.activeDungeon.dungeonId === dungeonId) {
                return this.nextDungeonStage(userId);
            } else {
                this.abandonDungeon(userId);
            }
        }

        const current = this.battles.get(key);
        if (current && !current.over) fail('Đang trong một trận chiến khác, hãy hoàn thành trước.');

        this.checkTravelArrival(p, now);
        if (p.traveling) fail('Đang ngự kiếm phi hành trên mây, chưa tới nơi.');

        if (now < (p.injuredUntil || 0)) {
            const secs = Math.ceil((p.injuredUntil - now) / 1000);
            fail(`Đang trọng thương, còn ${secs >= 60 ? `${Math.ceil(secs / 60)} phút` : `${secs} giây`}.`);
        }

        const d = C.DUNGEON_BY_ID.get(dungeonId);
        if (!d) fail('Không tìm thấy hang động bí cảnh này.');

        // Kiểm tra vị trí (Thành trấn cho Cổ Động, Bản đồ cho Thí Luyện Trang Bị)
        if (d.townId && p.town && d.townId !== p.town) {
            const townName = C.TOWN_BY_ID.get(d.townId)?.name || d.townId;
            fail(`[${d.name}] tọa lạc tại ${townName}. Hãy ngự kiếm phi hành tới đó mới có thể khiêu chiến!`);
        }
        if (d.mapId) {
            const curTown = C.TOWN_BY_ID.get(p.town || 'thanh_van');
            if (curTown?.mapId !== d.mapId) {
                fail(`[${d.name}] thuộc ${d.mapName || 'khu vực khác'}. Hãy ngự kiếm phi hành tới một thành trấn trong bản đồ này để khiêu chiến!`);
            }
        }

        // Giới hạn lượt mỗi ngày
        if (d.isEquipTrial) {
            p.equipTrialDaily = p.equipTrialDaily || { date: today, count: 0 };
            if (p.equipTrialDaily.date !== today) p.equipTrialDaily = { date: today, count: 0 };
            if (p.equipTrialDaily.count >= 10) {
                fail('Hôm nay đạo hữu đã thám hiểm Thí Luyện Trang Bị 10/10 lần, ngày mai hãy trở lại!');
            }
        } else {
            p.dungeonDaily = p.dungeonDaily || { date: today, count: 0 };
            if (p.dungeonDaily.date !== today) p.dungeonDaily = { date: today, count: 0 };
            if (p.dungeonDaily.count >= 10) {
                fail('Hôm nay đạo hữu đã thám hiểm Cổ Động 10/10 lần, linh lực cạn kiệt, ngày mai hãy trở lại!');
            }
        }

        // Kiểm tra cảnh giới & Phi Thăng
        if (d.ascensionRequired && !p.ascended) {
            fail(d.isEquipTrial ? 'Cần Phi Thăng trước khi vào Thí Luyện Tiên Giới.' : 'Cần Phi Thăng trước khi vào Cổ Động Tiên Giới.');
        }

        const party = this.partyOf(userId);
        let players = [p];
        if (party && party.members.length > 1) {
            if (party.leader === key) {
                const selectedIds = party.members.filter(id => id === key || Boolean(party.ready[String(id)]));
                players = selectedIds.map(id => this.player(id)).filter(Boolean);
            }
            for (const member of players) {
                this.requireNotKnockedOutInDungeon(member);
                this.cleanStaleDungeonState(member, now);
                if (member.activeDungeon && member.activeDungeon.dungeonId !== dungeonId) {
                    const mBattle = this.battles.get(String(member.userId));
                    if (!mBattle || mBattle.over) {
                        this.abandonDungeon(member.userId);
                    } else {
                        fail(`${member.name} đang trong chuyến Cổ Động khác.`);
                    }
                }
                this.checkTravelArrival(member, now);
                if (member.traveling) fail(`${member.name} đang ngự kiếm phi hành, chưa thể cùng vào.`);
                if (d.townId && (member.town || 'thanh_van') !== (p.town || 'thanh_van')) {
                    fail(`${member.name} không ở cùng thành trấn.`);
                }
                if (d.mapId) {
                    const memberTown = C.TOWN_BY_ID.get(member.town || 'thanh_van');
                    if (memberTown?.mapId !== d.mapId) fail(`${member.name} không ở cùng bản đồ.`);
                }
                if (this.activeBattle(member.userId)) fail(`${member.name} đang trong trận chiến khác.`);
                if (now < (member.injuredUntil || 0)) fail(`${member.name} đang trọng thương.`);
                if (String(member.userId) !== key && !party.ready[String(member.userId)]) fail(`${member.name} chưa bấm Sẵn sàng.`);
                if (d.ascensionRequired && !member.ascended) fail(`${member.name} cần Phi Thăng trước khi vào Tiên Giới.`);

                if (d.isEquipTrial) {
                    member.equipTrialDaily ||= { date: today, count: 0 };
                    if (member.equipTrialDaily.date !== today) member.equipTrialDaily = { date: today, count: 0 };
                    if ((member.equipTrialDaily.count || 0) >= 10) fail(`${member.name} đã dùng hết 10 lượt Thí Luyện Trang Bị hôm nay.`);
                } else {
                    member.dungeonDaily ||= { date: today, count: 0 };
                    if (member.dungeonDaily.date !== today) member.dungeonDaily = { date: today, count: 0 };
                    if ((member.dungeonDaily.count || 0) >= 10) fail(`${member.name} đã dùng hết 10 lượt Cổ Động hôm nay.`);
                }
            }
        }
        // Kiểm tra khóa dùng chung và thời gian chờ hồi
        this.data.dungeonsState = this.data.dungeonsState || {};
        const dState = this.data.dungeonsState[d.id] = this.data.dungeonsState[d.id] || {};

        if (dState.fightingBy && dState.fightingBy.until > now && dState.fightingBy.userId !== String(userId)) {
            const leftSec = Math.ceil((dState.fightingBy.until - now) / 1000);
            fail(`⚠️ [${d.name}] đang bị đạo hữu [${dState.fightingBy.name}] khiêu chiến! Hãy đợi đối phương kết thúc (còn khoảng ${leftSec}s).`);
        }

        if (dState.respawnAt && dState.respawnAt > now) {
            const leftSec = Math.ceil((dState.respawnAt - now) / 1000);
            const minLeft = Math.ceil(leftSec / 60);
            fail(`⚠️ [${d.name}] vừa bị [${dState.lastClearedBy || 'người khác'}] thám hiểm! đang tĩnh dưỡng tích tụ linh khí, hồi sinh sau ${minLeft > 1 ? minLeft + ' phút' : leftSec + ' giây'}.`);
        }

        // Kiểm tra thể lực và khí huyết
        for (const member of players) {
            this.syncStamina(member, now);
            if (member.stamina < d.stamina) fail(`${member.name} không đủ thể lực (${member.stamina}/${d.stamina}).`);
            const memberStats = this.stats(member, now);
            if (member.hp == null) member.hp = memberStats.hp;
            if (member.hp <= Math.round(memberStats.hp * 0.2)) {
                fail(`${member.name} khí huyết quá thấp (dưới 20%), hãy hồi phục trước khi tiến vào.`);
            }
        }

        // Trừ thể lực và tăng số lượt trong ngày
        for (const member of players) {
            member.stamina -= d.stamina;
            if (member.stamina < C.RULES.staminaMax && member.staminaAt > now) member.staminaAt = now;
            if (d.isEquipTrial) {
                member.equipTrialDaily.count = (member.equipTrialDaily.count || 0) + 1;
            } else {
                member.dungeonDaily.count = (member.dungeonDaily.count || 0) + 1;
            }
        }

        // Khóa Cổ Động
        dState.fightingBy = {
            userId: String(userId),
            name: p.fullName || p.name,
            memberIds: players.map(member => String(member.userId)),
            until: now + 5 * 60 * 1000,
        };

        // Khởi tạo phiên Cổ Động
        const activeDungeon = {
            dungeonId: d.id,
            stageIndex: 0,
            stages: d.stages,
            totalStages: d.stages.length,
            startAt: now,
            stageLogs: [],
            leaderId: key,
            userIds: players.map(member => String(member.userId)),
        };
        for (const member of players) member.activeDungeon = { ...activeDungeon };

        // Bắt đầu ải 1
        const stage = d.stages[0];
        const mDef = this.dungeonMonsterForBattle(d, stage, 0, now);
        if (!mDef) fail(`Không tìm thấy yêu thú trấn ải của ${d.name}.`);
        const battle = new Battle(this, players, mDef, now);
        battle.isDungeon = true;
        battle.dungeonId = d.id;
        battle.dungeonStageIndex = 0;
        battle.dungeonStageName = stage.name;
        battle.dungeonName = d.name;
        battle.dungeonLeaderId = key;
        for (const member of players) this.battles.set(String(member.userId), battle);

        this.touch();
        return {
            success: true,
            battle: battle.view(now, userId),
            activeDungeon: p.activeDungeon,
        };
    }

    nextDungeonStage(userId) {
        const p = this.requirePlayer(userId);
        const key = String(userId);
        const now = this.now();
        const active = p.activeDungeon;
        if (!active) fail('Không có phiên Cổ Động nào đang diễn ra.');
        const downUserIds = new Set((active.downUserIds || []).map(String));
        if (downUserIds.has(key)) fail('Bạn đã gục trong Cổ Động; hãy chờ tổ đội hoàn tất chuyến thám hiểm rồi mới tham chiến lại.');
        const previousLeaderId = String(active.leaderId || '');
        if (previousLeaderId && previousLeaderId !== key) {
            if (!downUserIds.has(previousLeaderId)) fail('Chờ trưởng nhóm mở ải tiếp theo của Cổ Động.');
            active.leaderId = key;
        }
        const previousBattle = this.battle(userId);
        if (previousBattle && !previousBattle.over) fail('Hãy hoàn thành ải hiện tại trước khi tiến vào ải tiếp theo.');
        if (previousBattle?.isDungeon && previousBattle.dungeonStageIndex !== active.stageIndex) {
            fail('Ải tiếp theo đã được trưởng nhóm mở rồi.');
        }

        const nextIndex = active.stageIndex + 1;
        if (nextIndex >= active.totalStages) {
            delete p.activeDungeon;
            fail('Đã vượt qua tất cả các ải của Cổ Động.');
        }

        const d = C.DUNGEON_BY_ID.get(active.dungeonId);
        if (!d) fail('Không tìm thấy Cổ Động.');

        const expeditionMembers = (active.userIds || [key]).map(id => this.player(id))
            .filter(member => member?.activeDungeon?.dungeonId === d.id);
        const players = (active.userIds || [key]).map(id => this.player(id))
            .filter(member => member?.activeDungeon?.dungeonId === d.id
                && !downUserIds.has(String(member.userId)));
        if (!players.length) fail('Đội thám hiểm Cổ Động đã giải tán.');
        for (const member of expeditionMembers) {
            member.activeDungeon.stageIndex = nextIndex;
            if (!downUserIds.has(String(member.userId))) member.activeDungeon.leaderId = String(active.leaderId || key);
        }
        for (const member of players) {
            if (this.activeBattle(member.userId)) fail(`${member.name} đang trong trận chiến khác.`);
        }
        const stage = d.stages[nextIndex] || active.stages[nextIndex];
        const dState = this.data.dungeonsState?.[d.id];
        const mDef = this.dungeonMonsterForBattle(d, stage, nextIndex, now);
        if (!mDef) fail(`Không tìm thấy yêu thú trấn ải tiếp theo của ${d.name}.`);
        const battle = new Battle(this, players, mDef, now);
        battle.isDungeon = true;
        battle.dungeonId = d.id;
        battle.dungeonStageIndex = nextIndex;
        battle.dungeonStageName = stage.name;
        battle.dungeonName = d.name;
        battle.dungeonLeaderId = active.leaderId || key;
        for (const member of players) this.battles.set(String(member.userId), battle);

        if (dState && dState.fightingBy) {
            dState.fightingBy.until = now + 5 * 60 * 1000;
        }

        this.touch();
        return {
            success: true,
            battle: battle.view(now, userId),
            activeDungeon: active,
        };
    }

    exploreDungeon(userId, dungeonId) {
        const p = this.requirePlayer(userId);
        this.requireNotKnockedOutInDungeon(p);
        const key = String(userId);
        const now = this.now();
        const today = vnDate(now);
        const current = this.battles.get(key);
        if (current && !current.over) fail('Đang trong một trận chiến khác, hãy hoàn thành trước.');

        this.checkTravelArrival(p, now);
        if (p.traveling) fail('Đang ngự kiếm phi hành trên mây, chưa tới nơi.');

        if (now < (p.injuredUntil || 0)) {
            const secs = Math.ceil((p.injuredUntil - now) / 1000);
            fail(`Đang trọng thương, còn ${secs >= 60 ? `${Math.ceil(secs / 60)} phút` : `${secs} giây`}.`);
        }

        const d = C.DUNGEON_BY_ID.get(dungeonId);
        if (!d) fail('Không tìm thấy hang động bí cảnh này.');

        // Giới hạn lượt mỗi ngày
        if (d.isEquipTrial) {
            p.equipTrialDaily = p.equipTrialDaily || { date: today, count: 0 };
            if (p.equipTrialDaily.date !== today) p.equipTrialDaily = { date: today, count: 0 };
            if (p.equipTrialDaily.count >= 10) {
                fail('Hôm nay đạo hữu đã thám hiểm Thí Luyện Trang Bị 10/10 lần, ngày mai hãy trở lại!');
            }
        } else {
            p.dungeonDaily = p.dungeonDaily || { date: today, count: 0 };
            if (p.dungeonDaily.date !== today) p.dungeonDaily = { date: today, count: 0 };
            if (p.dungeonDaily.count >= 10) {
                fail('Hôm nay đạo hữu đã thám hiểm Cổ Động 10/10 lần, linh lực cạn kiệt, ngày mai hãy trở lại!');
            }
        }

        // Kiểm tra thành trấn / bản đồ
        if (d.townId && p.town && d.townId !== p.town) {
            const townName = C.TOWN_BY_ID.get(d.townId)?.name || d.townId;
            fail(`[${d.name}] tọa lạc tại ${townName}. Hãy ngự kiếm phi hành tới đó mới có thể khiêu chiến!`);
        }
        if (d.mapId) {
            const curTown = C.TOWN_BY_ID.get(p.town || 'thanh_van');
            if (curTown?.mapId !== d.mapId) {
                fail(`[${d.name}] thuộc ${d.mapName || 'khu vực khác'}. Hãy ngự kiếm phi hành tới một thành trấn trong bản đồ này để khiêu chiến!`);
            }
        }

        const dungeonTown = C.TOWN_BY_ID.get(d.townId || p.town || 'thanh_van');
        const dungeonMap = C.MAP_BY_ID.get(dungeonTown?.mapId || d.mapId);
        if (dungeonMap?.ascensionRequired && !p.ascended) fail('Cần Phi Thăng trước khi vào Tiên Giới.');

        // Kiểm tra khóa và cooldown
        this.data.dungeonsState = this.data.dungeonsState || {};
        const dState = this.data.dungeonsState[d.id] = this.data.dungeonsState[d.id] || {};

        if (dState.fightingBy && dState.fightingBy.until > now && dState.fightingBy.userId !== String(userId)) {
            const leftSec = Math.ceil((dState.fightingBy.until - now) / 1000);
            fail(`⚠️ [${d.name}] đang bị đạo hữu [${dState.fightingBy.name}] khiêu chiến! Hãy đợi đối phương kết thúc (còn khoảng ${leftSec}s).`);
        }

        if (dState.respawnAt && dState.respawnAt > now) {
            const leftSec = Math.ceil((dState.respawnAt - now) / 1000);
            const minLeft = Math.ceil(leftSec / 60);
            fail(`⚠️ [${d.name}] vừa bị [${dState.lastClearedBy || 'người khác'}] thám hiểm! đang tĩnh dưỡng tích tụ linh khí, hồi sinh sau ${minLeft > 1 ? minLeft + ' phút' : leftSec + ' giây'}.`);
        }

        this.syncStamina(p, now);
        if (p.stamina < d.stamina) fail(`Không đủ thể lực (${p.stamina}/${d.stamina}) để thám hiểm bí cảnh.`);

        const baseSt = this.stats(p, now);
        if (p.hp == null) p.hp = baseSt.hp;
        const st = boostPvePlayerStats(baseSt);
        if (p.hp <= Math.round(baseSt.hp * 0.2)) {
            fail('Khí huyết quá thấp (dưới 20%), hãy đến Y Quán hồi phục trước khi tiến vào hang động hung hiểm!');
        }

        p.stamina -= d.stamina;
        if (p.stamina < C.RULES.staminaMax && p.staminaAt > now) p.staminaAt = now;
        if (d.isEquipTrial) {
            p.equipTrialDaily.count = (p.equipTrialDaily.count || 0) + 1;
        } else {
            p.dungeonDaily.count = (p.dungeonDaily.count || 0) + 1;
        }

        let curHp = Math.round(st.hp * clamp(p.hp / Math.max(1, baseSt.hp), 0, 1));
        const stageLogs = [];
        let winAll = true;

        for (let i = 0; i < d.stages.length; i++) {
            const stage = d.stages[i];
            const mDef = this.dungeonMonsterForBattle(d, stage, i, now) || C.MONSTER_BY_ID.get(stage.monsterId);
            if (!mDef) continue;

            const stageRes = this._simulateDungeonStage(p, st, curHp, mDef, stage, now);
            curHp = stageRes.finalHp;
            stageLogs.push({
                stageIndex: i + 1,
                stageName: stage.name,
                monsterName: mDef.name,
                monsterIcon: mDef.icon,
                win: stageRes.win,
                log: stageRes.log,
                turns: stageRes.turns,
            });

            if (!stageRes.win) {
                winAll = false;
                break;
            }

            curHp = Math.min(st.hp, Math.round(curHp + st.hp * 0.20));
        }

        const summary = {
            dungeonId: d.id,
            dungeonName: d.name,
            novel: d.novel,
            win: winAll,
            stages: stageLogs,
            rewards: { drops: [], stones: 0, exp: 0 },
            fortuitous: null,
        };

        if (!winAll) {
            p.hp = 1;
            p.injuredUntil = now + C.RULES.injuryMs;
            p.losses = (p.losses || 0) + 1;
            summary.message = `Thám hiểm ${d.name} thất bại! Bạn bị trọng thương và buộc phải rút lui.`;
            dState.fightingBy = null;
            this.checkInjuryRealmDrop(userId);
        } else {
            p.hp = Math.max(1, Math.min(baseSt.hp, Math.round(curHp * baseSt.hp / Math.max(1, st.hp))));
            p.wins = (p.wins || 0) + 1;

            p.stones = (p.stones || 0) + d.stones;
            this.realms.addExp(userId, d.expReward);
            summary.rewards.stones = d.stones;
            summary.rewards.exp = d.expReward;

            if (d.isEquipTrial) {
                const trialEquip = this.pickEquipTrialReward(d, p);
                if (trialEquip) {
                    const it = this.addEquip(p, trialEquip, now);
                    summary.rewards.drops.push(it ? `🎁 [Trang Bị] ${trialEquip.name}` : `🎁 [Trang Bị] ${trialEquip.name} (vào kho)`);
                }
                const fragQty = Math.floor(1 + this.rng() * 2);
                this.addStack(p, 'fragment', 'frag_trang_bi', fragQty);
                summary.rewards.drops.push(`Mảnh Tàn Đồ Trang Bị ×${fragQty}`);
                if (this.rng() < 0.70) {
                    this.addStack(p, 'mat', 'mat_van_thiet', 1);
                    summary.rewards.drops.push('Huyền Thiên Vẫn Thiết ×1');
                }
            } else {
                if (d.guaranteedPill && this.rng() < (d.guaranteedPillRate != null ? d.guaranteedPillRate * 0.4 : 0.025)) {
                    const pillDef = C.CONSUMABLE_BY_ID.get(d.guaranteedPill);
                    if (this.addStack(p, 'cons', d.guaranteedPill, 1) > 0) {
                        summary.rewards.drops.push(`Đan đột phá: ${pillDef?.name || d.guaranteedPill}`);
                    }
                }

                if (d.bonusPills && d.bonusPills.length > 0 && this.rng() < 0.008) {
                    const bPillId = d.bonusPills[Math.floor(this.rng() * d.bonusPills.length)];
                    const bDef = C.CONSUMABLE_BY_ID.get(bPillId);
                    if (this.addStack(p, 'cons', bPillId, 1) > 0) {
                        summary.rewards.drops.push(`Bổ sung: ${bDef?.name || bPillId}`);
                    }
                }

                // Boss Cổ Động rơi trang bị: tăng 50% tỉ lệ rơi (0.03 -> 0.045)
                if (d.equipDrop && this.rng() < 0.045) {
                    const eqDef = C.EQUIP_BY_ID.get(d.equipDrop);
                    if (eqDef) {
                        const it = this.addEquip(p, eqDef, now);
                        summary.rewards.drops.push(it ? eqDef.name : `${eqDef.name} (túi đầy)`);
                    }
                }

                if (this.rng() < 0.50) {
                    this.addStack(p, 'mat', 'mat_yeu_dan', 1);
                    summary.rewards.drops.push('Yêu Đan ×1');
                }
                if (this.rng() < 0.50) {
                    this.addStack(p, 'mat', 'mat_van_thiet', 1);
                    summary.rewards.drops.push('Huyền Thiên Vẫn Thiết ×1');
                }

                this.rollCuratedDungeonMaterialDrops(d, p, summary.rewards.drops);
            }

            this.rollCuratedDungeonMaterialDrops(d, p, summary.rewards.drops);

            // Cơ Duyên Ngộ Đạo (10% tỷ lệ)
            if (this.rng() < 0.10) {
                const burstExp = Math.round(d.expReward * 0.5);
                this.realms.addExp(userId, burstExp);
                summary.fortuitous = {
                    type: 'exp_burst',
                    text: `🌟【CƠ DUYÊN NGỘ ĐẠO】Lĩnh ngộ linh vận viễn cổ trong hang động, tu vi tăng thêm +${burstExp} EXP!`,
                    exp: burstExp,
                };
            }
            summary.message = `Thám hiểm thành công toàn bộ bí cảnh ${d.name}!`;

            // Cooldown tối đa 15 phút
            const cdMs = Math.min(15 * 60 * 1000, Math.max(3 * 60 * 1000, (180 + (d.realmMin || 0) * 60 + (d.equipDrop ? 120 : 0)) * 1000));
            dState.respawnAt = now + cdMs;
            dState.lastClearedBy = p.fullName || p.name;
            dState.fightingBy = null;
        }

        this.touch();
        return summary;
    }

    _simulateDungeonStage(p, st, curHp, mDef, stage, now) {
        const statsResolved = Boolean(mDef.explicitStats);
        const mStats = statsResolved ? mDef : monsterStats(mDef, now);
        const isVariant = statsResolved ? Boolean(mDef.dungeonVariant) : Boolean(stage.isVariant || this.rng() < 0.20);
        const varHpMul = statsResolved ? 1.0 : (isVariant ? 1.25 : 1.0);
        const varAtkMul = statsResolved ? 1.0 : (isVariant ? 1.10 : 1.0);
        const varDefMul = statsResolved ? 1.0 : (isVariant ? 1.15 : 1.0);

        let mHp = Math.round(mStats.hp * (statsResolved ? 1.0 : (stage.isBoss ? 1.25 : 1.0)) * varHpMul);
        mHp = Math.round(mHp * encounterPartyHpMul({ isDungeon: true, small: false }, 1, now));
        const mAtk = Math.round(mStats.atk * (statsResolved ? 1.0 : (stage.isBoss ? 1.15 : 1.10))
            * varAtkMul * (C.RULES.soloDungeonAtkMul || C.RULES.soloBossAtkMul || 1));
        const mDefVal = Math.round(mStats.def * (statsResolved ? 1.0 : (stage.isBoss ? 1.20 : 1.15)) * varDefMul);
        const mAccuracy = mDef.accuracy ?? clamp(90 + 8 * (mDef.realm || 0) / ((mDef.realm || 0) + 10), 90, 98);
        const mDodge = clamp(((mDef.spd || 10) / ((mDef.spd || 10) + 300)) * 40, 2, 45);
        let pHp = curHp;

        const beats = (a, b) => C.HE[a]?.beats === b;
        const mulP = beats(p.he, mDef.element) ? 1.25 : (beats(mDef.element, p.he) ? 0.8 : 1);
        const mulM = beats(mDef.element, p.he) ? 1.25 : (beats(p.he, mDef.element) ? 0.8 : 1);

        const log = [];
        const turns = [];
        log.push(`⚔️ Tiến vào ải [${stage.name}]: Đối đầu ${stage.isBoss ? 'Thủ lĩnh Cổ Động' : 'Yêu thú trấn ải'} ${mDef.name} (${C.HE[mDef.element]?.name || mDef.element} hệ)!`);

        const dungeonRealmDiff = Math.max(0, (mDef.realm || 0) - (st.realmIndex || 0));
        for (let round = 1; round <= 30; round++) {
            if (pHp <= 0 || mHp <= 0) break;

            const pHit = this.rng() < combatHitChance(
                dungeonRealmDiff > 0 ? Math.max(5, st.accuracy - dungeonRealmDiff * 18) : st.accuracy,
                dungeonRealmDiff > 0 ? Math.min(92, mDodge + dungeonRealmDiff * 18) : mDodge
            );
            const pCrit = pHit && (dungeonRealmDiff > 0 ? (this.rng() < Math.max(0, st.crit - dungeonRealmDiff * 0.25)) : (this.rng() < st.crit));
            const skillPower = 1.4 + (st.power > 500 ? 0.6 : 0.2);
            const pCritMul = pCrit ? Math.min(3.5, Math.max(1.5, Number(st.critDmg || 150) / 100)) : 1;
            let pRaw = pHit ? Math.round((st.atk * skillPower * (0.9 + this.rng() * 0.25) * mulP) * pCritMul) : 0;
            let pDmg = pHit ? Math.max(25, damageAfterDefense(pRaw, mDefVal)) : 0;
            if (dungeonRealmDiff > 0) {
                pDmg = Math.max(1, Math.round(pDmg * Math.pow(0.10, dungeonRealmDiff)));
            }
            pDmg = Math.max(0, Math.round(pDmg * (C.RULES.soloBossDamageMul || 0.55)));
            mHp -= pDmg;
            turns.push({ round, attacker: p.name, dmg: pDmg, crit: pCrit, miss: !pHit, defHp: Math.max(0, mHp) });
            log.push(pHit ? `Hiệp ${round}: ${p.name} xuất chiêu gây ${pDmg} sát thương${pCrit ? ' (BẠO KÍCH!)' : ''}${dungeonRealmDiff > 0 ? ' [Bị áp chế cảnh giới]' : ''} (Khí huyết đối thủ: ${Math.max(0, mHp)})` : `Hiệp ${round}: ${mDef.name} né được đòn đánh của ${p.name}.`);

            if (mHp <= 0) break;

            const mHit = this.rng() < combatHitChance(mAccuracy, st.dodge);
            if (!mHit) {
                turns.push({ round, attacker: mDef.name, dmg: 0, dodge: true, defHp: pHp });
                log.push(`Hiệp ${round}: ${mDef.name} phản kích nhưng bạn đã linh hoạt né đòn!`);
            } else {
                const mCrit = this.rng() < 0.1;
                let mRaw = Math.round((mAtk * (0.85 + this.rng() * 0.3) * mulM) * (mCrit ? 1.5 : 1));
                let mDmg = Math.max(10, damageAfterDefense(mRaw, st.def));
                if (dungeonRealmDiff > 0) {
                    mDmg = Math.round(mDmg * (1 + Math.pow(dungeonRealmDiff, 1.6) * 1.8));
                    // diff=1: sát thương cực kỳ cao nhưng không one-shot ngay; diff>=2: không giới hạn
                    if (dungeonRealmDiff === 1) {
                        mDmg = Math.min(mDmg, Math.max(1, Math.ceil(st.hp * 0.90)));
                    }
                } else {
                    mDmg = Math.min(mDmg, Math.max(1, Math.ceil(st.hp * (stage.isBoss ? 0.55 : 0.22))));
                }
                pHp -= mDmg;
                turns.push({ round, attacker: mDef.name, dmg: mDmg, crit: mCrit, defHp: Math.max(0, pHp) });
                log.push(`Hiệp ${round}: ${mDef.name} cuồng bạo tấn công, gây ${mDmg} sát thương${mCrit ? ' (BẠO KÍCH!)' : ''}${dungeonRealmDiff > 0 ? ' [Uy Áp Hủy Diệt]' : ''} (Khí huyết của bạn: ${Math.max(0, pHp)})`);
            }
        }

        const win = mHp <= 0 || (pHp > 0 && (pHp / Math.max(1, st.hp)) >= (mHp / Math.max(1, mStats.hp)));
        return { win, finalHp: Math.max(0, pHp), log, turns };
    }

    townLordName(town) {
        const base = String(town?.name || 'Cửu Châu').replace(/(?:Trấn|Thị Trấn|Đô Thành|Thành|Thị)$/u, '').trim();
        return `Thành chủ ${base || town?.name || 'Cửu Châu'}`;
    }

    // =========================================================================
    // CHỢ NPC THÀNH TRẤN: danh mục 20 món cố định theo thành và ngày Việt Nam.
    // =========================================================================
    townShop(townId) {
        const town = C.TOWN_BY_ID.get(townId) || C.TOWNS[0];
        const pool = C.CONSUMABLES.filter(c => {
            if (c.exp || c.breakthrough || c.permStat || c.elemMastery || c.roleStat) return false; // Không bán tu vi/thuộc tính vĩnh viễn.
            if (c.demonOnly) return false;
            if (/recipe_/.test(c.id)) return false; // Không bán công thức chế tác
            if ((c.id.startsWith('phu_') || c.talisman) && c.tier && !['pham', 'hoang'].includes(c.tier)) return false; // Chỉ bán bùa thường
            return true;
        });

        // Cảnh giới phường thị quyết định mức giá tối đa của hàng hóa.
        const now = this.now();
        const dateStr = vnDate(now);
        const realmMin = Math.max(0, Number(town.realmMin) || 0);
        const maxPrice = Math.min(40000000, Math.round(50000 * Math.pow(1.5, realmMin)));
        const seedStr = `${town.id}_${dateStr}`;
        let seed = 2166136261;
        for (let i = 0; i < seedStr.length; i++) seed = Math.imul(seed ^ seedStr.charCodeAt(i), 16777619);
        const eligible = pool.filter(c => (Number(c.price) || 0) <= maxPrice);
        const rareMaterials = [
            { id: 'mat_huyet_tinh', price: 200_000, realmMin: 4 },
            { id: 'mat_long_lan', price: 300_000, realmMin: 5 },
        ].filter(m => realmMin >= m.realmMin && maxPrice >= m.price);

        // Luôn có các vật phẩm tiện ích nếu cấp thành và danh mục cho phép.
        const guaranteed = ['hoi_xuan_dan', 'hoi_linh_dan', 'hoi_luc_dan', 'dan_bo_duong', 'dan_duong_am', 'phu_thien_co_truy_tung'];
        const selectedIds = new Set(guaranteed.filter(id => eligible.some(c => c.id === id)));
        const score = id => {
            let h = seed;
            for (let i = 0; i < id.length; i++) h = Math.imul(h ^ id.charCodeAt(i), 16777619);
            return h >>> 0;
        };
        // Bồi Nguyên Đan luân phiên xuất hiện: xác suất ổn định theo thành trấn/ngày,
        // tránh đổi hàng mỗi lần người chơi mở quầy.
        const featuredOdds = { boi_nguyen_dan: 0.65 };
        const featuredIds = new Set(Object.keys(featuredOdds));
        for (const [id, chance] of Object.entries(featuredOdds)) {
            if (eligible.some(c => c.id === id) && score(`featured_${id}`) / 0x100000000 < chance) selectedIds.add(id);
        }
        // Hàng hiếm được gieo hạt riêng theo thành/ngày; xác suất 4% ở các thành
        // đủ điều kiện cho trung bình xấp xỉ 2 lần xuất hiện/ngày mỗi loại.
        for (const material of rareMaterials) {
            if (score(`rare_${material.id}`) / 0x100000000 < 0.04) selectedIds.add(material.id);
        }
        const shuffled = eligible.filter(c => !featuredIds.has(c.id)).sort((a, b) => score(a.id) - score(b.id));
        for (const item of shuffled) {
            if (selectedIds.size >= 20) break;
            selectedIds.add(item.id);
        }

        const consumableListings = eligible.filter(c => selectedIds.has(c.id)).map(c => ({
            id: c.id,
            name: c.name,
            kind: 'cons',
            price: Math.ceil(c.price * 0.8),
            desc: c.desc,
            icon: c.icon || '💊',
            talisman: Boolean(c.talisman || c.id.startsWith('phu_')),
            realmMin,
            stock: null,
        }));
        const materialListings = rareMaterials
            .filter(material => selectedIds.has(material.id))
            .map(material => {
                const def = C.MATERIAL_BY_ID.get(material.id);
                return {
                    id: material.id,
                    name: def.name,
                    kind: 'mat',
                    price: material.price,
                    desc: def.desc,
                    icon: def.icon || '📦',
                    talisman: false,
                    realmMin,
                    stock: null,
                };
            });
        return [...consumableListings, ...materialListings];
    }

    // =========================================================================
    // HỆ THỐNG HÒM THƯ (MAILBOX / INBOX)
    // =========================================================================
    checkSystemMails(p) {
        if (!p) return;
        p.inbox = p.inbox || [];
        const inboxCountBeforeCleanup = p.inbox.length;
        p.inbox = p.inbox.filter(m => m.id !== 'mail_thien_dao_update_20260927');
        p.receivedSystemMails = p.receivedSystemMails || [];
        for (const m of p.inbox) {
            if (m.id && !p.receivedSystemMails.includes(m.id)) {
                p.receivedSystemMails.push(m.id);
            }
        }

        const systemGifts = [
            {
                id: 'mail_gift_update_cuuchau_1000kc',
                title: '🪙 Quà Đền Bù Bảo Trì: Khai Mở Cửu Châu (1.000 Linh Thạch)',
                sender: 'Thiên Đạo',
                content: 'Thiên Đạo gửi tặng 1.000 Linh Thạch + 1.000 EXP Tu Vi + 100 Điểm Cống Hiến đền bù thời gian bảo trì và chúc mừng đại lục Cửu Châu 45 Thành Trấn!',
                stones: 1000,
                exp: 1000,
                items: [
                    { id: 'hoi_xuan_dan', name: 'Hồi Xuân Đan', qty: 5, icon: '💊', kind: 'cons' },
                    { id: 'hoi_linh_dan', name: 'Hồi Linh Đan', qty: 5, icon: '🧪', kind: 'cons' },
                    { id: 'hoi_luc_dan', name: 'Hồi Lực Đan', qty: 3, icon: '⚡', kind: 'cons' },
                ],
            },
            {
                id: 'mail_gift_update_sect_farm',
                title: '🎁 Quà Tặng Cập Nhật: Bảo Các & Khu Farm Tông Môn',
                sender: 'Thiên Đạo',
                content: 'Thiên Đạo đã trùng tu hoàn tất Tàng Bảo Các, hiển thị trực tiếp Khu Farm Tông Môn và tăng mạnh tỷ lệ rơi đồ Tiểu Yêu. Ban tặng 1.000 Linh Thạch và 500 EXP tu vi chúc đạo hữu tinh tiến!',
                stones: 1000,
                exp: 500,
                items: [],
            },
            {
                id: 'mail_gift_den_bu_10k_linh_thach',
                title: '🎁 Thiên Đạo Đền Bù: 10.000 Linh Thạch Toàn Server',
                sender: 'Thiên Đạo',
                content: 'Thiên Đạo ban tặng 10.000 Linh Thạch đền bù chư vị đạo hữu trong thời gian trùng tu quy tắc cảnh giới, tối ưu hóa EXP săn yêu và giải phóng giới hạn linh tệ!',
                stones: 10000,
                exp: 0,
                items: [],
            },
            {
                id: 'mail_gift_update_hoi_luc_5_20260927',
                title: '🎁 Quà Toàn Server: 5 Hồi Lực Đan',
                sender: 'Thiên Đạo',
                content: 'Thiên Đạo ban tặng mỗi đạo hữu 5 Hồi Lực Đan. Mở thư và bấm Nhận để thêm vào hành trang.',
                stones: 0,
                exp: 0,
                items: [{ id: 'hoi_luc_dan', name: 'Hồi Lực Đan', qty: 5, icon: '⚡', kind: 'cons' }],
            },
        ];

        let added = p.inbox.length !== inboxCountBeforeCleanup;
        for (const gift of systemGifts) {
            // Đã nhận trước đây hoặc đang có trong hòm thư -> KHÔNG BAO GIỜ GỬI LẠI
            if (p.receivedSystemMails.includes(gift.id) || p.inbox.some(m => m.id === gift.id)) {
                continue;
            }
            p.receivedSystemMails.push(gift.id);
            p.inbox.unshift({
                ...gift,
                sentAt: this.now(),
                claimed: false,
                read: false,
            });
            added = true;
        }
        if (added) {
            if (p.inbox.length > 50) p.inbox = p.inbox.slice(0, 50);
            this.touch();
        }
    }

    sendMail(toUserId, { title, sender = 'Thiên Đạo', content = '', stones = 0, exp = 0, items = [] }) {
        const p = this.player(toUserId);
        if (!p) return null;
        p.inbox = p.inbox || [];
        const mail = {
            id: 'mail_' + newId(),
            title: String(title).slice(0, 60),
            sender: String(sender).slice(0, 40),
            content: String(content).slice(0, 300),
            stones: Math.max(0, Number(stones) || 0),
            exp: Math.max(0, Number(exp) || 0),
            items: Array.isArray(items) ? items : [],
            sentAt: this.now(),
            claimed: false,
            read: false,
        };
        p.inbox.unshift(mail);
        // Tối đa lưu 50 thư
        if (p.inbox.length > 50) p.inbox = p.inbox.slice(0, 50);
        this.touch();
        return mail;
    }

    getInbox(userId) {
        const p = this.requirePlayer(userId);
        this.checkSystemMails(p);
        return (p.inbox || []).map(m => ({
            id: m.id,
            title: m.title,
            sender: m.sender,
            content: m.content,
            stones: m.stones || 0,
            exp: m.exp || 0,
            items: (m.items || []).map(it => {
                if (typeof it === 'string') return { id: it, name: it, qty: 1, icon: '📦' };
                if (!it) return null;
                return { id: it.id, name: it.name || it.id, qty: it.qty || 1, icon: it.icon || '📦' };
            }).filter(Boolean),
            sentAt: m.sentAt,
            claimed: Boolean(m.claimed),
            read: Boolean(m.read || m.claimed),
            hasRewards: inboxMailHasRewards(m),
        }));
    }

    claimMail(userId, mailId = null) {
        const p = this.requirePlayer(userId);
        p.inbox = p.inbox || [];
        const canClaim = m => !m.claimed && inboxMailHasRewards(m);
        const targets = mailId ? p.inbox.filter(m => m.id === mailId && canClaim(m)) : p.inbox.filter(canClaim);
        if (!targets.length) fail('Không có thư nào để nhận thưởng.');

        let totalStones = 0;
        let totalExp = 0;
        let totalItems = 0;

        for (const mail of targets) {
            if (mail.stones > 0) {
                const grant = Math.max(0, Number(mail.stones) || 0);
                p.stones = (Number(p.stones) || 0) + grant;
                totalStones += grant;
            }
            if (mail.exp > 0) {
                this.realms.addExp(userId, mail.exp);
                totalExp += mail.exp;
            }
            if (Array.isArray(mail.items)) {
                for (const it of mail.items) {
                    if (it.itemData) {
                        const place = this.countPlace(p, 'bag') < this.capacity('bag', p) ? 'bag' : 'kho';
                        const restored = { ...it.itemData, uid: newId(), qty: 1, place, at: this.now(), refinedBy: null, refineAt: 0 };
                        p.items.push(restored);
                    } else if (it.kind !== 'equip') {
                        let added = this.addStack(p, it.kind, it.id, it.qty || 1, 'bag');
                        if (added < (it.qty || 1)) {
                            this.addStack(p, it.kind, it.id, (it.qty || 1) - added, 'kho');
                        }
                    } else if (it.kind === 'equip') {
                        const def = C.EQUIP_BY_ID.get(it.id);
                        if (def) {
                            let eq = this.addEquip(p, def, this.now(), 'bag');
                            if (!eq) this.addEquip(p, def, this.now(), 'kho');
                        }
                    }
                    totalItems += (it.qty || 1);
                }
            }
            mail.claimed = true;
            mail.read = true;
        }

        this.touch();
        return {
            success: true,
            claimedMails: targets.length,
            stones: totalStones,
            balanceAfter: Number(p.stones) || 0,
            exp: totalExp,
            itemsCount: totalItems,
            message: `Đã nhận quà từ ${targets.length} bức thư: +${totalStones} linh thạch, +${totalExp} EXP, +${totalItems} vật phẩm.`,
        };
    }

    markMailRead(userId, mailId) {
        const p = this.requirePlayer(userId);
        p.inbox = p.inbox || [];
        const mail = p.inbox.find(m => m.id === mailId);
        if (!mail) fail('Không tìm thấy thư.');
        if (inboxMailHasRewards(mail) && !mail.claimed) fail('Thư có quà cần nhận trước khi đánh dấu đã đọc.');
        const changed = !mail.read;
        mail.read = true;
        if (changed) this.touch();
        return { success: true, mailId, read: true };
    }

    deleteMail(userId, mailId) {
        const p = this.requirePlayer(userId);
        p.inbox = p.inbox || [];
        const target = p.inbox.find(m => m.id === mailId);
        if (!target) return { success: true };

        if (!target.claimed && (target.stones > 0 || target.exp > 0 || (target.items && target.items.length > 0))) {
            fail('Hãy nhận phần thưởng đính kèm trước khi xóa thư!');
        }

        p.receivedSystemMails = p.receivedSystemMails || [];
        if (!p.receivedSystemMails.includes(mailId)) {
            p.receivedSystemMails.push(mailId);
        }

        p.inbox = p.inbox.filter(m => m.id !== mailId);
        this.touch();
        return { success: true };
    }

    // =========================================================================
    // KHO TÔNG MÔN & CỐNG HIẾN ĐỒ VẬT / CÔNG PHÁP
    // =========================================================================
    sectDepositItem(userId, itemUid) {
        const p = this.requirePlayer(userId);
        if (!p.sectId) fail('Đạo hữu chưa gia nhập môn phái.');
        const s = this.sects[p.sectId];
        if (!s) fail('Không tìm thấy tông môn.');
        const item = this.findItem(p, itemUid);
        if (!item) fail('Không tìm thấy vật phẩm cần cống hiến.');
        if (item.place === 'equip') fail('Hãy tháo trang bị trước khi cống hiến.');

        s.storage = s.storage || [];
        if (s.storage.length >= sectStorageCapacity(s.level)) fail(`Kho tông môn đã đầy (${s.storage.length}/${sectStorageCapacity(s.level)} món). Nâng cấp tông môn để mở thêm chỗ.`);
        s.shopItems = s.shopItems || [];

        const def = itemDef(item) || {};
        const count = item.qty || 1;
        const basePrice = def.price || (item.kind === 'scroll' ? 300 : (item.kind === 'cons' ? 80 : 50));
        const today = vnDate(this.now());
        const daily = sectDailyContribution(p, today);
        const coinsEarned = Math.min(SECT_DAILY_CONTRIBUTION_CAP, Math.max(45, Math.round(basePrice * 0.4)) * count * 3);
        if (coinsEarned > daily.remaining) fail(`Món đồ này nhận ${coinsEarned} điểm, nhưng hôm nay bạn chỉ còn ${daily.remaining}/${SECT_DAILY_CONTRIBUTION_CAP} điểm cống hiến.`);
        addSectContribution(s, 0);
        if ((s.totalContribution || 0) + coinsEarned > SECT_MAX_CONTRIBUTION) fail('Tông môn đã đạt giới hạn 5.000.000 điểm cống hiến.');
        const defaultShopPrice = Math.max(20, Math.round(basePrice * 0.6));

        // Bỏ khỏi túi người chơi, chuyển vào Tàng Bảo Các của tông môn
        p.items = p.items.filter(it => it.uid !== itemUid);

        const shopEntry = {
            id: 'sect_shop_' + Date.now() + '_' + Math.random().toString(36).slice(2, 6),
            targetId: item.id,
            name: def.name || item.name || item.id,
            kind: item.kind,
            tier: def.tier || 'pham',
            icon: def.icon || (item.kind === 'scroll' ? '📜' : (item.kind === 'mat' ? '🌿' : (item.kind === 'cons' ? '💊' : '📦'))),
            desc: def.desc || (item.kind === 'scroll' ? 'Sách công pháp quý' : (item.kind === 'mat' ? 'Linh dược / Khoáng thạch' : 'Đan dược / Phù chú')),
            price: defaultShopPrice,
            qty: count,
            donatedBy: p.fullName || p.name,
            donatedById: String(userId),
            donatedAt: this.now(),
            custom: true,
        };
        s.shopItems.unshift(shopEntry);

        // Đồng thời lưu vào kho tông môn
        s.storage.unshift({
            id: item.id,
            name: def.name || item.name || item.id,
            kind: item.kind,
            icon: shopEntry.icon,
            tier: def.tier || 'pham',
            qty: count,
            donatedBy: p.fullName || p.name,
            donatedAt: this.now(),
        });
        p.sectCoins = (p.sectCoins || 0) + coinsEarned;
        p.sectContributed = (p.sectContributed || 0) + coinsEarned;
        p.sectContributionDate = today;
        p.sectContributionToday = daily.contributed + coinsEarned;
        if (s.members && s.members[String(userId)]) {
            s.members[String(userId)].contributed = (s.members[String(userId)].contributed || 0) + coinsEarned;
        }
        addSectContribution(s, coinsEarned);
        this.touch();
        return {
            success: true,
            coinsEarned,
            shopItem: shopEntry,
            message: `Đã cống hiến [${def.name}] vào Tàng Bảo Các (+${coinsEarned} cống hiến)!`,
        };
    }

    sectSetPrice(userId, itemId, newPrice) {
        const p = this.requirePlayer(userId);
        if (!p.sectId) fail('Đạo hữu chưa gia nhập môn phái.');
        const s = this.sects[p.sectId];
        if (!s) fail('Không tìm thấy tông môn.');
        const myRole = p.sectRole || (s.leaderId === String(userId) ? 'leader' : 'ngoai_mon');
        if (!['leader', 'vice'].includes(myRole)) {
            fail('Chỉ Tông Chủ hoặc Phó Tông Chủ mới có quyền định giá vật phẩm trong Tàng Bảo Các.');
        }
        s.shopItems = s.shopItems || [];
        const it = s.shopItems.find(item => item.id === itemId);
        if (!it) fail('Không tìm thấy vật phẩm cống hiến này trong Tàng Bảo Các.');
        const priceVal = Math.max(1, Math.min(100000, Math.floor(Number(newPrice) || 1)));
        it.price = priceVal;
        this.touch();
        return {
            success: true,
            itemId,
            price: priceVal,
            message: `Đã cập nhật giá bán [${it.name}] thành 🪙 ${priceVal} cống hiến!`,
        };
    }

    sectDepositStones(userId, stones) {
        const p = this.requirePlayer(userId);
        if (!p.sectId) fail('Đạo hữu chưa gia nhập môn phái.');
        const s = this.sects[p.sectId];
        if (!s) fail('Không tìm thấy tông môn.');
        const amt = Math.floor(Number(stones));
        if (amt < 10) fail('Tối thiểu cống hiến 10 linh thạch.');
        if (p.stones < amt) fail('Không đủ linh thạch.');
        const today = vnDate(this.now());
        const daily = sectDailyContribution(p, today);
        if (amt > daily.remainingStones) fail(`Mỗi ngày chỉ cống hiến tối đa ${SECT_DAILY_STONE_DONATION_CAP} linh thạch (hôm nay đã cống hiến ${daily.donatedStones}).`);
        const contributionPoints = amt * SECT_CONTRIBUTION_PER_STONE;
        if (contributionPoints > daily.remaining) fail(`Đã dùng ${daily.contributed}/${SECT_DAILY_CONTRIBUTION_CAP} điểm cống hiến hôm nay; còn ${daily.remaining} điểm.`);
        addSectContribution(s, 0);
        if ((s.totalContribution || 0) + contributionPoints > SECT_MAX_CONTRIBUTION) fail('Tông môn đã đạt giới hạn 5.000.000 điểm cống hiến.');

        p.stones -= amt;
        p.sectLastDonateDate = today;
        p.sectDonateToday = daily.donatedStones + amt;
        p.sectContributionDate = today;
        p.sectContributionToday = daily.contributed + contributionPoints;
        s.funds = (s.funds || 0) + amt;
        p.sectCoins = (p.sectCoins || 0) + contributionPoints;
        p.sectContributed = (p.sectContributed || 0) + contributionPoints;
        if (s.members && s.members[String(userId)]) {
            s.members[String(userId)].contributed = (s.members[String(userId)].contributed || 0) + contributionPoints;
        }
        const progress = addSectContribution(s, contributionPoints);
        this.touch();
        return { success: true, funds: s.funds, coins: p.sectCoins, contributionPoints: progress.added, message: `Đã nộp ${amt} linh thạch vào ngân quỹ tông môn, nhận ${progress.added} điểm cống hiến!` };
    }

    sectTreasuryBuy(userId, itemId, qty = 1) {
        const p = this.requirePlayer(userId);
        if (!p.sectId) fail('Chưa có môn phái.');
        const s = this.sects[p.sectId];
        if (!s) fail('Không tìm thấy môn phái.');
        if (!['leader', 'vice'].includes(p.sectRole)) fail('Chỉ Tông Chủ hoặc Phó Tông Chủ mới có quyền xuất quỹ tông môn mua vật phẩm.');

        const shopDef = (C.SECT_SHOP || []).find(it => it.id === itemId);
        if (!shopDef) fail('Vật phẩm không có trong Tàng Bảo Các.');
        const n = Math.max(1, Math.floor(Number(qty) || 1));
        const totalCost = shopDef.price * n;
        if ((s.funds || 0) < totalCost) fail(`Ngân quỹ tông môn không đủ (cần ${totalCost} linh thạch, hiện có ${s.funds || 0}).`);

        s.storage = s.storage || [];
        if (s.storage.length >= sectStorageCapacity(s.level)) fail(`Kho tông môn đã đầy (${s.storage.length}/${sectStorageCapacity(s.level)} món). Nâng cấp tông môn để mở thêm chỗ.`);
        s.funds -= totalCost;
        s.storage.push({
            id: shopDef.targetId,
            name: shopDef.name,
            kind: shopDef.kind,
            icon: shopDef.icon || '📦',
            qty: (shopDef.qty || 1) * n,
            donatedBy: `Ngân quỹ Tông Môn (${p.fullName || p.name} mua)`,
            donatedAt: this.now(),
        });
        this.touch();
        return { success: true, remainingFunds: s.funds, message: `Đã mua ${n} [${shopDef.name}] cất vào Kho Tông Môn!` };
    }

    sectStorageSell(userId, { storageIndex, itemId, qty, allJunk } = {}) {
        const p = this.requirePlayer(userId);
        if (!p.sectId) fail('Chưa có môn phái.');
        const s = this.sects[p.sectId];
        if (!s) fail('Không tìm thấy môn phái.');
        const myRole = p.sectRole || (s.leaderId === String(userId) ? 'leader' : 'ngoai_mon');
        if (!['leader', 'vice'].includes(myRole)) fail('Chỉ Tông Chủ hoặc Phó Tông Chủ mới có quyền thanh lý vật phẩm trong Kho Tông Môn.');

        s.storage = s.storage || [];
        if (!s.storage.length) fail('Kho tông môn hiện đang trống.');

        let totalEarned = 0;
        let soldItems = [];

        if (allJunk) {
            const remaining = [];
            for (const item of s.storage) {
                const count = Math.max(1, Number(item.qty) || 1);
                const tier = item.tier || 'pham';
                const isJunk = tier === 'pham' || tier === 'hoang' || item.kind === 'mat' || (item.kind === 'cons' && !item.breakthrough);
                if (isJunk) {
                    const pricePerUnit = Math.max(10, Math.round(this.sellPrice(item) || 15));
                    const earned = pricePerUnit * count;
                    totalEarned += earned;
                    soldItems.push(`${item.name} ×${count} (+${earned} 🪙)`);
                } else {
                    remaining.push(item);
                }
            }
            if (!soldItems.length) fail('Không có vật phẩm phẩm cấp thấp nào để dọn kho.');
            s.storage = remaining;
        } else {
            let idx = -1;
            if (storageIndex != null && storageIndex >= 0 && storageIndex < s.storage.length) {
                idx = storageIndex;
            } else if (itemId) {
                idx = s.storage.findIndex(it => it.id === itemId);
            }
            if (idx === -1) fail('Không tìm thấy vật phẩm cần bán trong kho tông môn.');
            const target = s.storage[idx];
            const maxCount = Math.max(1, Number(target.qty) || 1);
            const sellCount = qty ? Math.min(maxCount, Math.max(1, Math.floor(Number(qty)))) : maxCount;
            const pricePerUnit = Math.max(10, Math.round(this.sellPrice(target) || 20));
            const earned = pricePerUnit * sellCount;
            totalEarned = earned;
            soldItems.push(`${target.name} ×${sellCount} (+${earned} 🪙)`);

            if (sellCount >= maxCount) {
                s.storage.splice(idx, 1);
            } else {
                target.qty = maxCount - sellCount;
            }
        }

        s.funds = (s.funds || 0) + totalEarned;
        s.records = s.records || [];
        s.records.unshift({
            at: this.now(),
            text: `🏪 ${p.fullName || p.name} đã bán thanh lý đồ kho tông lên Chợ NPC, thu về +${totalEarned.toLocaleString('vi-VN')} linh thạch cho ngân quỹ.`,
        });
        if (s.records.length > 50) s.records.length = 50;

        this.touch();
        return {
            success: true,
            totalEarned,
            soldCount: soldItems.length,
            remainingFunds: s.funds,
            message: `Đã bán thanh lý thành công lên Chợ NPC, ngân quỹ tông môn nhận +${totalEarned.toLocaleString('vi-VN')} linh thạch!`,
        };
    }

    sectShopSell(userId, { shopItemId, qty } = {}) {
        const p = this.requirePlayer(userId);
        if (!p.sectId) fail('Chưa có môn phái.');
        const s = this.sects[p.sectId];
        if (!s) fail('Không tìm thấy môn phái.');
        const myRole = p.sectRole || (s.leaderId === String(userId) ? 'leader' : 'ngoai_mon');
        if (!['leader', 'vice'].includes(myRole)) fail('Chỉ Tông Chủ hoặc Phó Tông Chủ mới có quyền bán vật phẩm trong Tàng Bảo Các lên Chợ NPC.');

        s.shopItems = s.shopItems || [];
        const idx = s.shopItems.findIndex(it => it.id === shopItemId);
        if (idx === -1) fail('Không tìm thấy vật phẩm này trong Tàng Bảo Các.');

        const item = s.shopItems[idx];
        const maxCount = Math.max(1, Number(item.qty) || 1);
        const sellCount = qty ? Math.min(maxCount, Math.max(1, Math.floor(Number(qty)))) : maxCount;
        const pricePerUnit = Math.max(20, Math.round(this.sellPrice(item) || (item.price ? item.price * 5 : 50)));
        const totalEarned = pricePerUnit * sellCount;

        if (sellCount >= maxCount) {
            s.shopItems.splice(idx, 1);
        } else {
            item.qty = maxCount - sellCount;
        }

        s.funds = (s.funds || 0) + totalEarned;
        s.records = s.records || [];
        s.records.unshift({
            at: this.now(),
            text: `🏪 ${p.fullName || p.name} đã bán ${item.name} từ Tàng Bảo Các lên Chợ NPC, thu về +${totalEarned.toLocaleString('vi-VN')} linh thạch cho ngân quỹ.`,
        });
        if (s.records.length > 50) s.records.length = 50;

        this.touch();
        return {
            success: true,
            totalEarned,
            remainingFunds: s.funds,
            message: `Đã bán [${item.name}] lên Chợ NPC, ngân quỹ tông môn nhận +${totalEarned.toLocaleString('vi-VN')} linh thạch!`,
        };
    }

    // =========================================================================
    // TRANH ĐOẠT CHỨC VỊ TÔNG MÔN
    // =========================================================================
    sectChallengeRole(userId, targetRole) {
        const p = this.requirePlayer(userId);
        this.requireDiscoverablePlayer(userId);
        if (!p.sectId) fail('Chưa có môn phái.');
        const s = this.sects[p.sectId];
        if (!s) fail('Không tìm thấy môn phái.');
        const member = (s.members || {})[String(userId)];
        const currentRole = s.leaderId === String(userId) ? 'leader' : (member?.role || p.sectRole || 'ngoai_mon');
        const nextPromotion = sectNextPromotion(currentRole);
        if (!nextPromotion) fail('Chưởng môn và Phó chưởng môn đã ở chức vị cao nhất có thể tranh; hãy nhờ Chưởng môn điều chỉnh chức vụ.');
        if (targetRole !== nextPromotion.role) fail(`Bạn chỉ có thể tranh chức ${nextPromotion.name} tiếp theo.`);
        const contribution = Math.max(Number(p.sectContributed) || 0, Number(member?.contributed) || 0);
        if (contribution < nextPromotion.required) {
            fail(`Cần ${nextPromotion.required.toLocaleString('vi-VN')} điểm cống hiến để tranh chức ${nextPromotion.name} (hiện có ${contribution.toLocaleString('vi-VN')}).`);
        }

        const currentHolderEntry = Object.entries(s.members || {}).find(([uid, m]) => (m.role === targetRole || (targetRole === 'leader' && String(s.leaderId) === String(uid))) && String(uid) !== String(userId));
        const holderId = currentHolderEntry ? currentHolderEntry[0] : null;
        const holderUser = holderId ? this.player(holderId) : null;

        const now = this.now();
        this.syncStamina(p, now);
        if (p.stamina < 10) fail('Cần 10 thể lực để khiêu chiến chức vị.');
        if (p.injuredUntil && p.injuredUntil > now) fail('Đang trọng thương, không thể xuất chiến.');
        p.stamina -= 10;

        let oppId = holderUser ? String(holderUser.userId) : null;
        if (!oppId || !this.player(oppId)) {
            oppId = `bot_${targetRole}_${s.id}`;
            const targetPower = ({ noi_mon: 1000, dai_de_tu: 1500, elder: 1800, dai_elder: 2200, vice: 2500 })[targetRole] || 1800;
            this.data.players = this.data.players || {};
            this.data.players[oppId] = {
                userId: oppId,
                name: `${SECT_ROLE_NAMES[targetRole]} Thủ Vị [${s.name}]`,
                registered: true,
                isNpc: true,
                mon: 'kiem',
                he: 'kim',
                items: [],
                equip: { weapon: null, armor: null, acc1: null, acc2: null, phiKiem: null, nhanTruDo: null, nhanNaDi: null },
                slots: ['kiem_1', 'kiem_2'],
                stats: { hp: targetPower * 8, mp: 600, atk: Math.round(targetPower * 0.35), def: Math.round(targetPower * 0.25), spd: 35, sense: 25, crit: 0.15, power: targetPower }
            };
        }

        const battleView = this.startPvpManualBattle(userId, oppId, !holderUser || Boolean(holderUser.isNpc));
        const battle = this.pvpManualBattles?.get(String(battleView.id));
        if (!battle) fail('Không thể khởi tạo trận tranh chức, hãy thử lại.');
        battle.sectRoleChallenge = {
            sectId: p.sectId,
            targetRole,
            challengerId: String(userId),
            holderId: holderId ? String(holderId) : null,
        };

        this.touch();
        return {
            success: true,
            battle: battleView,
            message: `⚔️ Đã phát động tranh chức ${nextPromotion.name}! Hãy đánh bại người đang giữ chức vụ để được thăng vị.`
        };
    }

    sectPromote(userId) {
        const p = this.requirePlayer(userId);
        const s = p.sectId && this.sects[p.sectId];
        if (!s) fail('Đạo hữu chưa vào môn phái.');
        const member = (s.members || {})[String(userId)];
        const nextPromotion = sectNextPromotion(s.leaderId === String(userId) ? 'leader' : (member?.role || p.sectRole || 'ngoai_mon'));
        if (!nextPromotion) fail('Chức vụ hiện tại không có bậc tranh tiếp theo. Chưởng môn có thể điều chỉnh chức vụ thủ công.');
        return this.sectChallengeRole(userId, nextPromotion.role);
    }

    sectAssignRole(userId, targetUserId, newRole) {
        const p = this.requirePlayer(userId);
        if (!p.sectId) fail('Chưa có môn phái.');
        const s = this.sects[p.sectId];
        if (!s) fail('Môn phái không tồn tại.');
        if (s.leaderId !== String(userId)) fail('Chỉ Chưởng môn mới có quyền điều chỉnh thủ công chức vụ.');
        if (!Object.hasOwn(SECT_ROLE_NAMES, newRole)) fail('Chức vụ cần bổ nhiệm không hợp lệ.');

        const targetMember = (s.members || {})[String(targetUserId)];
        if (!targetMember) fail('Người chơi không ở trong môn phái của bạn.');
        const targetPlayer = this.player(targetUserId);
        if (newRole !== 'leader' && String(targetUserId) === String(s.leaderId)) {
            fail('Hãy chuyển chức Chưởng môn cho thành viên khác để đổi chức vụ của mình.');
        }

        s.members = s.members || {};
        if (newRole === 'vice') {
            const existingVice = Object.entries(s.members).find(([uid, member]) => member.role === 'vice' && uid !== String(targetUserId));
            if (existingVice) {
                existingVice[1].role = 'dai_elder';
                const oldVice = this.player(existingVice[0]);
                if (oldVice) oldVice.sectRole = 'dai_elder';
            }
        }
        targetMember.role = newRole;
        targetMember.name = targetMember.name || targetPlayer?.name;
        if (targetPlayer) targetPlayer.sectRole = newRole;
        if (newRole === 'leader') {
            const oldLeaderId = String(s.leaderId);
            const oldLeaderMember = s.members[oldLeaderId];
            if (oldLeaderMember) oldLeaderMember.role = 'vice';
            const oldLeader = this.player(oldLeaderId);
            if (oldLeader) oldLeader.sectRole = 'vice';
            s.leaderId = String(targetUserId);
            s.leaderName = targetMember.name || targetPlayer?.fullName || targetPlayer?.name;
        }
        this.touch();
        return {
            success: true,
            newRole,
            message: `✨ Chưởng môn đã bổ nhiệm [${targetMember.name || targetPlayer?.name || 'Đồng môn'}] làm ${SECT_ROLE_NAMES[newRole]}.`,
        };
    }

    distributeSectWarSpoils(attackerSect, defenderSect) {
        if (!attackerSect || !defenderSect) return { vaultUnits: 0, memberUnits: 0, lostUnits: 0 };
        const vault = attackerSect.storage = Array.isArray(attackerSect.storage) ? attackerSect.storage : [];
        const defendersLoot = Array.isArray(defenderSect.storage) ? defenderSect.storage : [];
        const eligibleMembers = Object.keys(attackerSect.members || {})
            .filter(uid => !uid.startsWith('system_') && !uid.startsWith('npc_') && this.player(uid)?.registered);
        const memberMail = new Map(eligibleMembers.map(uid => [uid, []]));
        const shareToVault = 0.5 + this.rng() * 0.3;
        let vaultUnits = 0;
        let memberUnits = 0;
        let lostUnits = 0;
        let memberCursor = 0;

        for (const item of defendersLoot) {
            const qty = Math.max(0, Math.floor(Number(item.qty) || 0));
            let captured = 0;
            let shared = 0;
            for (let i = 0; i < qty; i += 1) {
                const roll = this.rng();
                if (roll < shareToVault) captured += 1;
                else if (eligibleMembers.length && roll < shareToVault + 0.1) shared += 1;
                else lostUnits += 1;
            }
            if (captured) {
                vaultUnits += captured;
                const existing = vault.find(row => row.id === item.id && row.kind === item.kind);
                if (existing) existing.qty = (existing.qty || 0) + captured;
                else vault.unshift({ ...item, qty: captured, donatedBy: 'Chiến lợi phẩm diệt tông', donatedAt: this.now() });
            }
            if (shared) {
                memberUnits += shared;
                for (let i = 0; i < shared; i += 1) {
                    const uid = eligibleMembers[memberCursor % eligibleMembers.length];
                    memberCursor += 1;
                    const attached = memberMail.get(uid);
                    const existing = attached.find(row => row.id === item.id && row.kind === item.kind);
                    if (existing) existing.qty += 1;
                    else attached.push({ id: item.id, kind: item.kind, name: item.name, icon: item.icon, qty: 1 });
                }
            }
        }

        for (const [uid, items] of memberMail) {
            if (!items.length) continue;
            this.sendMail(uid, {
                title: '🎁 [CHIẾN LỢI PHẨM] Chia phần diệt tông',
                sender: 'Tông Môn Chiến',
                content: `Tông môn thắng trận chia đều ${memberUnits} vật phẩm được phân bổ cho thành viên tham chiến.`,
                items,
            });
        }
        return { vaultUnits, memberUnits, lostUnits };
    }

    // =========================================================================
    // TÔNG MÔN CHIẾN (Thách Đấu Hạng & Tấn Công Diệt Tông — 6 giờ thủ)
    // =========================================================================
    sectWarChallenge(userId, targetSectId, type = 'challenge') {
        const p = this.requirePlayer(userId);
        this.requireDiscoverablePlayer(userId);
        if (!p.sectId) fail('Đạo hữu chưa gia nhập môn phái.');
        if (p.sectRole !== 'leader' && p.sectRole !== 'vice') fail('Chỉ Tông Chủ hoặc Phó Tông Chủ mới có quyền phát động chiến tranh.');
        if (p.sectId === targetSectId) fail('Không thể tự tuyên chiến với môn phái của mình.');

        const s1 = this.sects[p.sectId];
        const s2 = this.sects[targetSectId];
        if (!s1 || !s2) fail('Không tìm thấy môn phái.');

        this.data.sectWars = this.data.sectWars || [];
        const existing = this.data.sectWars.find(w => w.attackerSectId === p.sectId && w.defenderSectId === targetSectId && w.status === 'active');
        if (existing) fail('Hai tông môn đang trong tình trạng chiến tranh.');

        const now = this.now();
        const resetSectNpcs = (sect) => {
            if (!sect) return;
            const ids = [sect.leaderId, ...Object.keys(sect.members || {})];
            for (const id of ids) {
                const idStr = String(id || '');
                const npc = this.data.worldNpcs?.[idStr];
                if (npc) {
                    npc.isDead = false;
                    npc.deadAt = 0;
                    npc.deadReason = '';
                    npc.sectWarRestUntil = 0;
                    npc.hp = this.npcCombatStats(npc).hp;
                }
            }
        };
        resetSectNpcs(s1);
        resetSectNpcs(s2);

        const war = {
            id: 'war_' + newId(),
            attackerSectId: s1.id,
            attackerName: s1.name,
            defenderSectId: s2.id,
            defenderName: s2.name,
            type, // 'challenge' | 'attack'
            startedAt: now,
            defendUntil: now + 6 * 3600 * 1000, // 6 giờ để thủ!
            status: 'active',
            defeatedDefenders: [],
            defeatedAttackers: [],
            log: [`⚔️ [${s1.name}] đã phát động ${type === 'attack' ? 'TẤN CÔNG DIỆT TÔNG' : 'THÁCH ĐẤU TRANH HẠNG'} nhắm vào [${s2.name}]! Thời gian phòng thủ: 6 giờ.`],
        };

        this.data.sectWars.push(war);
        // Gửi thư thông báo cho toàn bộ thành viên tông bị tấn công
        for (const mId of Object.keys(s2.members || {})) {
            this.sendMail(mId, {
                title: `🚨 [TÔNG MÔN NGUY CẤP] ${s1.name} Tấn Công!`,
                sender: 'Chưởng Môn Lệnh',
                content: `Tông môn [${s1.name}] vừa phát động lệnh ${type === 'attack' ? 'TẤN CÔNG DIỆT TÔNG' : 'THÁCH ĐẤU'}! Toàn thể đệ tử hãy mau chóng nghênh chiến, thời gian giữ thành còn 6 giờ!`,
            });
        }

        this.touch();
        return {
            success: true,
            war,
            message: `Đã phát động ${type === 'attack' ? 'Tấn Công Diệt Tông' : 'Thách Đấu Hạng'} thành công! Tông môn đối phương có 6 giờ để thủ hộ tông môn.`,
        };
    }

    sectWarFight(userId, warId, targetMemberId) {
        const p = this.requirePlayer(userId);
        this.requireDiscoverablePlayer(userId);
        if (!p.sectId) fail('Chưa có môn phái.');
        this.data.sectWars = this.data.sectWars || [];
        const war = this.data.sectWars.find(w => w.id === warId && w.status === 'active');
        if (!war) fail('Trận tông môn chiến này đã kết thúc hoặc không tồn tại.');
        war.defeatedDefenders = Array.isArray(war.defeatedDefenders) ? war.defeatedDefenders : [];
        war.defeatedAttackers = Array.isArray(war.defeatedAttackers) ? war.defeatedAttackers : [];
        const actorSectId = String(p.sectId);
        const isAttacker = actorSectId === String(war.attackerSectId);
        const isDefender = actorSectId === String(war.defenderSectId);
        if (!isAttacker && !isDefender) fail('Chỉ thành viên của hai tông môn đang giao chiến mới được tham chiến.');
        const targetSectId = isAttacker ? String(war.defenderSectId) : String(war.attackerSectId);
        const actorDefeated = isAttacker ? war.defeatedAttackers : war.defeatedDefenders;
        const targetDefeated = isAttacker ? war.defeatedDefenders : war.defeatedAttackers;
        if (actorDefeated.includes(String(userId))) fail('Bạn đã bị hạ trong trận Tông chiến này và không thể tiếp tục xuất chiến.');

        const now = this.now();

        // Kiểm tra bị thương: không thể tham chiến khi đang trọng thương
        if (p.injuredUntil && p.injuredUntil > now) {
            const remSec = Math.ceil((p.injuredUntil - now) / 1000);
            fail(`Đang trọng thương! Không thể tham chiến tông môn, hồi phục sau ${remSec}s.`);
        }
        // Kiểm tra HP < 15%: phải trị thương trước
        const pSt = this.stats(p, now);
        const hpPct = (Number(p.hp ?? pSt.hp) || 0) / Math.max(1, pSt.hp);
        if (hpPct < 0.15) {
            fail('Khí huyết quá thấp (< 15%)! Hãy trị thương hoặc nghỉ ngơi hồi phục trước khi tham chiến tông môn.');
        }

        if (now > war.defendUntil) {
            war.status = 'defender_win';
            war.log.push(`🛡️ Hết 6 giờ phòng thủ! [${war.defenderName}] đã thủ hộ thành công trước [${war.attackerName}]!`);
            this.touch();
            fail('Đã hết 6 giờ phòng thủ, bên phòng thủ đã giữ vững tông môn!');
        }

        const targetIdStr = String(targetMemberId);
        const actorSect = this.sects[actorSectId];
        const targetSect = this.sects[targetSectId];
        if (!actorSect || !targetSect) fail('Một trong hai tông môn không còn tồn tại.');
        const actorRoster = this.getSectWarSideRoster(war, actorSectId);
        const actorMember = actorRoster.find(member => String(member.id) === String(userId));
        if (!actorMember || actorMember.isDefeated) fail('Bạn không còn trong danh sách có thể xuất chiến.');
        const enemyMembers = this.getSectWarSideRoster(war, targetSectId);
        const defender = enemyMembers.find(member => String(member.id) === targetIdStr);
        if (!defender) fail('Không tìm thấy thành viên của tông môn đối phương.');
        if (defender?.isDefeated || targetDefeated.includes(targetIdStr)) fail('Thành viên này đã bị hạ trong trận Tông chiến.');
        if (defender && !defender.isNpc) {
            const targetPlayer = this.player(targetIdStr);
            if ((Number(targetPlayer?.injuredUntil) || 0) > now) fail('Đối thủ đang trọng thương và chưa thể xuất chiến.');
            const targetBattle = this.pvpManualBattles?.get(targetIdStr);
            if (targetBattle && !targetBattle.over) fail('Đối thủ đang ở trong một trận PvP khác.');
        } else if (defender?.isNpc) {
            const targetNpc = this.data.worldNpcs?.[targetIdStr];
            if ((Number(targetNpc?.sectWarRestUntil) || 0) > now) fail('Đối thủ đang hồi phục và chưa thể xuất chiến.');
        }
        this.syncStamina(p, now);
        const staminaCost = C.RULES.pvpCost;
        if (p.stamina < staminaCost) fail(`Cần ${staminaCost} thể lực để tham chiến Tông chiến.`);
        const activePlayerBattle = this.pvpManualBattles?.get(String(userId));
        if (activePlayerBattle && !activePlayerBattle.over) fail('Đang tham chiến một trận PvP khác.');

        let foePower = 300, foeFullName = 'Đối Thủ';
        const targetPlayer = this.player(targetIdStr);
        const isPlayerTarget = Boolean(targetPlayer?.registered);
        const isNpcTarget = !isPlayerTarget && (defender.isNpc || targetIdStr.startsWith('system_') || targetIdStr.startsWith('npc_') || targetIdStr.includes('_elder_') || Boolean(this.data.worldNpcs?.[targetIdStr]) || (this.isNpcSect(targetSect) && targetIdStr === String(targetSect.leaderId)));

        if (isPlayerTarget) {
            const activeDefenderBattle = this.pvpManualBattles?.get(targetIdStr);
            if (activeDefenderBattle && !activeDefenderBattle.over) fail('Đối thủ đang ở trong một trận PvP khác.');
        }

        if (isNpcTarget) {
            const npcLeader = this.getNpcLeaderStats(targetSect);
            if (targetIdStr === String(targetSect.leaderId) || targetIdStr === npcLeader.id) {
                foePower = npcLeader.power || 6000;
                foeFullName = npcLeader.fullName || npcLeader.name;
            } else if (this.data.worldNpcs?.[targetIdStr]) {
                const npcObj = this.data.worldNpcs[targetIdStr];
                foePower = this.calcNpcPower(npcObj) || 4000;
                foeFullName = `${npcObj.name} (${npcObj.title || 'Hộ Tông'})`;
            } else {
                foePower = Math.round((npcLeader.power || 6000) * 0.75);
                foeFullName = `🛡️ Trưởng Lão Hộ Tông (${targetSect.name})`;
            }
        } else {
            // Cả bên tấn công lẫn phòng thủ đều có thể chủ động chọn đối thủ.
            const foe = targetPlayer;
            if (!foe) fail('Không tìm thấy đối thủ.');
            foeFullName = foe.fullName || foe.name;

            this.startPvpManualBattle(userId, targetMemberId);
            p.stamina -= staminaCost;
            if (p.stamina < this.maxStamina(p) && p.staminaAt > now) p.staminaAt = now;
            const liveBattle = this.pvpManualBattles?.get(String(userId));
            if (!liveBattle || liveBattle.p1.userId !== String(userId) || liveBattle.p2.userId !== targetIdStr) {
                fail('Không thể khởi tạo trận Tông chiến với thành viên này.');
            }
            liveBattle.sectWarChallenge = {
                warId,
                actorUserId: String(userId),
                actorSectId,
                targetSectId,
                targetMemberId: targetIdStr,
                foeFullName,
            };
            foe.notices = foe.notices || [];
            foe.notices.push(`⚔️ [TÔNG CHIẾN] ${p.fullName || p.name} của [${actorSect.name}] đã khiêu chiến bạn thuộc [${targetSect.name}]. Mở mục Đấu pháp để tham chiến.`);
            this.touch();
            return { success: true, battle: this.getPvpBattleView(liveBattle, String(userId)), message: `Khởi động Tông chiến thủ công với ${foeFullName}! Đối thủ đã nhận thông báo; trận kết thúc khi một bên bị hạ.` };
        }

        p.stamina -= staminaCost;
        if (p.stamina < this.maxStamina(p) && p.staminaAt > now) p.staminaAt = now;
        const ownStats = this.stats(p, now);
        const won = ownStats.power >= foePower * (0.8 + this.rng() * 0.4);
        const losingSectId = won ? targetSectId : actorSectId;
        const defeatedId = won ? targetIdStr : String(userId);
        const defeatedList = losingSectId === String(war.attackerSectId) ? war.defeatedAttackers : war.defeatedDefenders;
        if (!defeatedList.includes(defeatedId)) defeatedList.push(defeatedId);
        war.log ||= [];

        if (won) {
            if (isNpcTarget) this.markWorldNpcDead(targetIdStr, `tông chiến ${targetSect.name}`, now);
            war.log.push(`⚔️ [${actorSect.name}] ${p.fullName || p.name} đã đánh bại ${foeFullName} của [${targetSect.name}]!`);
        } else {
            p.injuredUntil = now + C.RULES.injuryMs;
            p.hp = Math.max(1, Math.floor((ownStats.hp || 500) * 0.15));
            p.notices ||= [];
            p.notices.push(`🛡️ [TÔNG CHIẾN] Bạn đã bị ${foeFullName} đánh bại và tạm thời không thể xuất chiến.`);
            war.log.push(`🛡️ [${actorSect.name}] ${p.fullName || p.name} đã bị ${foeFullName} của [${targetSect.name}] đánh bại!`);
        }

        let resolution = null;
        if (this.isSectWarSideEliminated(war, losingSectId)) {
            resolution = this.resolveSectWarElimination(war, losingSectId, now);
        }
        this.touch();
        const wonWar = resolution?.winnerSectId === actorSectId;
        return {
            success: true,
            win: won,
            warStatus: war.status,
            sectWarWin: resolution ? wonWar : undefined,
            message: won
                ? `Đánh bại ${foeFullName} thành công!${resolution ? ` Tông môn [${targetSect.name}] đã bị diệt.` : ''}`
                : `Bị ${foeFullName} đánh bại! Bạn trọng thương ${Math.round(C.RULES.injuryMs / 60000)} phút.${resolution ? ` Tông môn [${actorSect.name}] đã bị diệt.` : ''}`,
        };
    }


    // =========================================================================
    // 50 KHU VỰC FARM TÔNG MÔN (Chiếm lĩnh & Nhận Bổng Lộc)
    // =========================================================================
    _calculateTerritoryAccumulated(t, now = this.now()) {
        const lastHarvest = t.lastHarvestAt || t.occupiedAt || 0;
        if (!t.occupiedBy || !lastHarvest) {
            return { stones: 0, items: 0, itemId: null, itemName: '', itemIcon: '🪙', itemKind: null, canHarvest: false };
        }
        const elapsedSec = Math.max(0, (now - lastHarvest) / 1000);
        const elapsedHours = Math.min(24, elapsedSec / 3600);
        const rate = t.yieldPerHour || 10;

        let itemId = null;
        let itemName = 'Linh Thạch';
        let itemIcon = '🪙';
        let itemKind = 'mat';
        let itemQty = 0;
        let stoneQty = 0;

        if (t.resource === 'herbs') {
            itemId = 'mat_linh_thao';
            itemName = 'Linh Thảo Ngàn Năm';
            itemIcon = '🌿';
            itemKind = 'mat';
            itemQty = Math.floor(elapsedHours * rate * 0.5);
            stoneQty = Math.floor(elapsedHours * rate * 36);
        } else if (t.resource === 'ores') {
            itemId = 'mat_van_thiet';
            itemName = 'Huyền Thiên Vẫn Thiết';
            itemIcon = '🪨';
            itemKind = 'mat';
            itemQty = Math.floor(elapsedHours * rate * 0.5);
            stoneQty = Math.floor(elapsedHours * rate * 36);
        } else if (t.resource === 'pills') {
            itemId = (t.monsterRealm >= 5) ? 'tay_tuy_dan' : 'hoi_xuan_dan';
            itemName = (t.monsterRealm >= 5) ? 'Tẩy Tủy Đan' : 'Hồi Xuân Đan';
            itemIcon = '💊';
            itemKind = 'cons';
            itemQty = Math.floor(elapsedHours * Math.max(1, rate * 0.3));
            stoneQty = Math.floor(elapsedHours * rate * 36);
        } else if (t.resource === 'mats') {
            itemId = (t.monsterRealm >= 6) ? 'mat_long_lan' : ((t.monsterRealm >= 3) ? 'mat_huyet_tinh' : 'mat_yeu_dan');
            const mDef = C.MATERIAL_BY_ID?.get(itemId);
            itemName = mDef?.name || 'Yêu Đan';
            itemIcon = mDef?.icon || '🔮';
            itemKind = 'mat';
            itemQty = Math.floor(elapsedHours * Math.max(1, rate * 0.4));
            stoneQty = Math.floor(elapsedHours * rate * 48);
        } else {
            itemId = null;
            itemName = 'Linh Thạch Thượng Phẩm';
            itemIcon = '🪙';
            itemKind = null;
            itemQty = 0;
            stoneQty = Math.floor(elapsedHours * rate * 5);
        }

        const canHarvest = (stoneQty > 0 || itemQty > 0);
        return {
            stones: stoneQty,
            items: itemQty,
            itemId,
            itemName,
            itemIcon,
            itemKind,
            elapsedHours: Math.round(elapsedHours * 10) / 10,
            canHarvest,
        };
    }

    getSectTerritories(userId) {
        this.data.sectTerritories = this.data.sectTerritories || (C.SECT_TERRITORIES || []).map(t => ({ ...t }));
        const p = this.player(userId);
        const now = this.now();
        return this.data.sectTerritories.map(t => ({
            ...t,
            isMySect: Boolean(p?.sectId && t.occupiedBy === p.sectId),
            occupiedSectName: t.occupiedBy ? (this.sects[t.occupiedBy]?.name || 'Tông môn cổ') : 'Vô chủ',
            accumulated: this._calculateTerritoryAccumulated(t, now),
        }));
    }

    sectClaimTerritory(userId, territoryId) {
        const p = this.requirePlayer(userId);
        if (!p.sectId) fail('Đạo hữu chưa có môn phái.');
        const allowedRoles = ['leader', 'vice', 'elder', 'dai_elder'];
        if (!allowedRoles.includes(p.sectRole)) {
            fail('Chỉ Tông Chủ, Phó Tông Chủ và các Trưởng Lão mới có quyền xuất chinh chiếm lĩnh khu farm tông môn.');
        }
        this.data.sectTerritories = this.data.sectTerritories || (C.SECT_TERRITORIES || []).map(t => ({ ...t }));
        const t = this.data.sectTerritories.find(item => item.id === territoryId);
        if (!t) fail('Khu vực không tồn tại.');
        if (t.occupiedBy === p.sectId) fail('Tông môn của bạn đang chiếm giữ khu vực này rồi.');

        const now = this.now();
        this.syncStamina(p, now);
        if (p.stamina < 5) fail('Cần 5 thể lực để xuất chinh chiếm lĩnh.');
        p.stamina -= 5;

        // Nếu có quái thú trấn giữ
        if (t.hasMonsters) {
            const r = this.realmOf(userId).index;
            if (r < t.monsterRealm) fail(`Cảnh giới chưa đủ để đánh đuổi quái thú trấn giữ (yêu cầu ${this.realmName(t.monsterRealm)}).`);
        }

        const oldSectId = t.occupiedBy;
        t.occupiedBy = p.sectId;
        t.occupiedAt = now;
        t.lastHarvestAt = now;

        const s = this.sects[p.sectId];
        // Phát bổng lộc tức thì cho người chiếm
        const bonusStones = t.yieldPerHour || 50;
        p.stones += bonusStones;

        this.touch();
        return {
            success: true,
            territory: t,
            message: `🚩 Đã chiếm đóng thành công [${t.name}] cho tông môn ${s.name}! Nhận thưởng chiếm cứ: +${bonusStones} linh thạch.`,
        };
    }

    sectHarvestTerritory(userId, territoryId) {
        const p = this.requirePlayer(userId);
        if (!p.sectId) fail('Đạo hữu chưa có môn phái.');
        this.data.sectTerritories = this.data.sectTerritories || (C.SECT_TERRITORIES || []).map(t => ({ ...t }));
        const t = this.data.sectTerritories.find(item => item.id === territoryId);
        if (!t) fail('Khu vực không tồn tại.');
        if (t.occupiedBy !== p.sectId) fail('Khu vực này không thuộc quyền quản lý của tông môn bạn.');

        const now = this.now();
        const accum = this._calculateTerritoryAccumulated(t, now);
        if (!accum.canHarvest) {
            fail('Chưa có đủ sản lượng tài nguyên để thu hoạch. Xin hãy đợi thêm!');
        }

        // Nhận Linh Thạch
        p.stones = (p.stones || 0) + accum.stones;

        // Nhận vật phẩm vào túi
        let itemMsg = '';
        if (accum.itemId && accum.items > 0) {
            const added = this.addStack(p, accum.itemKind, accum.itemId, accum.items);
            if (added > 0) {
                itemMsg = `, +${added} ${accum.itemName}`;
            }
        }

        // Tông Môn Cống Hiến & Tông Môn EXP
        const coins = Math.max(1, Math.round(accum.stones * 0.05));
        p.sectCoins = (p.sectCoins || 0) + coins;
        const s = this.sects[p.sectId];
        if (s) {
            addSectContribution(s, Math.max(5, Math.round(accum.stones * 0.02)));
        }

        t.lastHarvestAt = now;
        this.touch();

        return {
            success: true,
            stones: accum.stones,
            items: accum.items,
            itemName: accum.itemName,
            coins,
            message: `🌾 Đã thu hoạch tài nguyên [${t.name}]: +${accum.stones.toLocaleString('vi-VN')} linh thạch${itemMsg}, +${coins} điểm Cống Hiến!`,
        };
    }

    sectHarvestAll(userId) {
        const p = this.requirePlayer(userId);
        if (!p.sectId) fail('Đạo hữu chưa có môn phái.');
        this.data.sectTerritories = this.data.sectTerritories || (C.SECT_TERRITORIES || []).map(t => ({ ...t }));
        const myTerrs = this.data.sectTerritories.filter(t => t.occupiedBy === p.sectId);
        if (!myTerrs.length) fail('Tông môn hiện chưa chiếm lĩnh khu vực nào.');

        const now = this.now();
        let totalStones = 0;
        let totalCoins = 0;
        const itemsGathered = {};
        let harvestCount = 0;

        for (const t of myTerrs) {
            const accum = this._calculateTerritoryAccumulated(t, now);
            if (accum.canHarvest) {
                totalStones += accum.stones;
                if (accum.itemId && accum.items > 0) {
                    this.addStack(p, accum.itemKind, accum.itemId, accum.items);
                    itemsGathered[accum.itemName] = (itemsGathered[accum.itemName] || 0) + accum.items;
                }
                t.lastHarvestAt = now;
                harvestCount++;
            }
        }

        if (harvestCount === 0 || (totalStones === 0 && Object.keys(itemsGathered).length === 0)) {
            fail('Các mỏ tài nguyên chưa tích lũy đủ sản lượng để thu hoạch. Hãy quay lại sau!');
        }

        p.stones = (p.stones || 0) + totalStones;
        totalCoins = Math.max(harvestCount, Math.round(totalStones * 0.05));
        p.sectCoins = (p.sectCoins || 0) + totalCoins;
        const s = this.sects[p.sectId];
        if (s) {
            addSectContribution(s, Math.max(harvestCount * 10, Math.round(totalStones * 0.02)));
        }

        this.touch();

        const itemStr = Object.entries(itemsGathered).map(([name, qty]) => `+${qty} ${name}`).join(', ');
        return {
            success: true,
            harvestCount,
            totalStones,
            totalCoins,
            items: itemsGathered,
            message: `🌾 Thu hoạch toàn bộ ${harvestCount} khu farm: +${totalStones.toLocaleString('vi-VN')} linh thạch${itemStr ? `, ${itemStr}` : ''}, +${totalCoins} Cống Hiến!`,
        };
    }

    // =========================================================================
    // KỲ BẢO THẾ GIỚI & HANG ĐỘNG XUẤT HIỆN NGẪU NHIÊN
    // =========================================================================
    getWorldEvents(userId) {
        const now = this.now();
        this.data.worldEvents = this.data.worldEvents || [];
        // Chỉ giữ sự kiện có phạm vi toàn thế giới; thông báo người chơi thuộc
        // personalEvents, nhưng lọc thêm để dữ liệu cũ không rò vào tab chung.
        this.data.worldEvents = this.data.worldEvents.filter(e => e.expiresAt > now
            && e.scope !== 'personal' && e.isPersonal !== true
            && e.userId == null && e.playerId == null);

        // Sinh 2-3 sự kiện nếu đang trống
        if (this.data.worldEvents.length < 2) {
            const towns = C.TOWNS || [];
            const t1 = towns[Math.floor(Math.random() * towns.length)] || { id: 'thanh_van', name: 'Thanh Vân Thị Trấn' };
            const t2 = towns[Math.floor(Math.random() * towns.length)] || { id: 'lac_duong', name: 'Lạc Dương Thành' };

            this.data.worldEvents.push({
                id: 'event_ky_bao_' + newId(),
                type: 'cave',
                name: 'Kỳ Bảo Cổ Động Xuất Thế',
                townId: t1.id,
                townName: t1.name,
                icon: '🏮',
                desc: `Có tu sĩ phát hiện cổ động kỳ bảo phát quang tại vùng phụ cận ${t1.name}! Có khả năng nhặt được phi kiếm cực phẩm và thiên tài địa bảo.`,
                expiresAt: now + 3 * 3600 * 1000,
            });

            this.data.worldEvents.push({
                id: 'event_than_thu_' + newId(),
                type: 'monster',
                name: 'Thần Thú Hàng Trần',
                townId: t2.id,
                townName: t2.name,
                icon: '🐉',
                desc: `Thượng cổ Thần Thú hạ phàm tại ${t2.name}! Tu sĩ cùng nhau bao vây trảm thú đoạt nội đan.`,
                expiresAt: now + 4 * 3600 * 1000,
            });
        }

        const townsById = C.TOWN_BY_ID;
        const activeEvents = [];
        for (const monster of this.data.worldMonsters || []) {
            const isScheduledBoss = monster.isScheduledWorldBoss && Number(monster.hp) > 0;
            const isTideBoss = monster.isBeastTideBoss && Number(monster.hp) > 0;
            if (!isScheduledBoss && !isTideBoss) continue;
            const town = townsById.get(String(monster.townId));
            if (!town) continue;
            activeEvents.push({
                id: `live_${isTideBoss ? 'tide_boss' : 'world_boss'}_${monster.uid}`,
                type: isTideBoss ? 'beast_tide_boss' : 'world_boss',
                name: isTideBoss ? `Boss Thú Triều: ${monster.name}` : `Boss Thế Giới: ${monster.name}`,
                townId: town.id,
                townName: town.name,
                icon: monster.icon || '🐉',
                desc: `${monster.name} đang xuất hiện tại ${town.name}, còn ${Math.max(0, Number(monster.hp) || 0).toLocaleString('vi-VN')} khí huyết. Các tu sĩ có thể đến tham chiến.`,
                expiresAt: Number(monster.expiresAt) > now ? Number(monster.expiresAt) : now + 60 * 60 * 1000,
                canExplore: false,
            });
        }
        const tide = this.data.beastTideEvent;
        if (tide && Number(tide.endsAt) > now) {
            for (const townId of tide.townIds || []) {
                const town = townsById.get(String(townId));
                if (!town) continue;
                activeEvents.push({
                    id: `live_beast_tide_${tide.wave || tide.startsAt || town.id}_${town.id}`,
                    type: 'beast_tide',
                    name: 'Thú Triều Toàn Cõi',
                    townId: town.id,
                    townName: town.name,
                    icon: '🐾',
                    desc: `Thú triều đợt ${tide.wave || '?'} đang hoành hành tại ${town.name}. Đợt này kết thúc lúc ${new Date(tide.endsAt).toLocaleString('vi-VN')}.`,
                    expiresAt: Number(tide.endsAt),
                    canExplore: false,
                });
            }
        }
        for (const war of this.data.sectWars || []) {
            if (war.status !== 'active' || (Number(war.defendUntil) > 0 && Number(war.defendUntil) <= now)) continue;
            activeEvents.push({
                id: `live_sect_war_${war.id}`,
                type: 'sect_war',
                name: 'Tông Chiến Toàn Cõi',
                townName: 'Toàn cõi',
                icon: '⚔️',
                desc: `[${war.attackerName || this.sects?.[war.attackerSectId]?.name || 'Tông môn'}] đang giao chiến với [${war.defenderName || this.sects?.[war.defenderSectId]?.name || 'Tông môn'}]. ${war.log?.at(-1) || 'Chiến sự đang tiếp diễn.'}`,
                expiresAt: Number(war.defendUntil) > now ? Number(war.defendUntil) : now + 60 * 60 * 1000,
                canExplore: false,
            });
        }

        const p = this.player(userId);
        const list = [...activeEvents, ...this.data.worldEvents];
        return list.map(e => ({
            ...e,
            canExplore: e.canExplore ?? true,
            isCurrentTown: Boolean(p?.town && p.town === e.townId),
            timeLeftSec: Math.max(0, Math.ceil((e.expiresAt - now) / 1000)),
        }));
    }

    getPersonalEvents(userId, markRead = false) {
        const p = this.requirePlayer(userId);
        const events = Array.isArray(p.personalEvents) ? p.personalEvents : [];
        const queued = Array.isArray(p.notices) ? p.notices.splice(0) : [];
        if (queued.length) {
            p.personalEvents = events;
            for (const text of queued) {
                const str = String(text);
                if (!p.personalEvents.some(e => e.text === str && Math.abs((e.at || 0) - this.now()) < 60000)) {
                    p.personalEvents.push({ id: newId(), text: str, at: this.now(), read: false });
                }
            }
            p.personalEvents = p.personalEvents.slice(-100);
            this.touch();
        }
        if (markRead) {
            for (const event of p.personalEvents || []) event.read = true;
            this.touch();
        }
        return {
            list: (p.personalEvents || []).slice().sort((a, b) => (b.at || 0) - (a.at || 0)).slice(0, 100),
            unreadCount: (p.personalEvents || []).filter(event => !event.read).length,
        };
    }

    exploreWorldEvent(userId, eventId) {
        const p = this.requirePlayer(userId);
        this.data.worldEvents = this.data.worldEvents || [];
        const ev = this.data.worldEvents.find(e => e.id === eventId);
        if (!ev) fail('Kỳ bảo này đã tan biến hoặc có người lấy mất.');
        if (p.town !== ev.townId) fail(`Kỳ bảo đang ở ${ev.townName}. Hãy ngự kiếm phi hành tới đó trước.`);

        const now = this.now();
        this.syncStamina(p, now);
        if (p.stamina < 5) fail('Cần 5 thể lực để tầm bảo.');
        p.stamina -= 5;

        // Thưởng lớn ngẫu nhiên: Linh thạch, Phi Kiếm, hoặc Phù
        const roll = Math.random();
        if (roll < 0.35) {
            // Nhặt được Phi Kiếm!
            const swords = C.FLYING_SWORDS || [];
            const sword = swords[Math.floor(Math.random() * swords.length)];
            if (sword) {
                this.addEquip(p, sword, now);
                this.touch();
                return { success: true, message: `🌟 Vận may ngút trời! Khám phá bí bảo nhặt được [${sword.name}] (${sword.desc})!` };
            }
        }
        const stones = Math.floor(Math.random() * 500 + 200);
        p.stones += stones;
        this.realms.addExp(userId, 800);
        this.touch();
        return { success: true, message: `✨ Khám phá cơ duyên thành công! Nhận được ${stones} linh thạch và 800 EXP tu vi!` };
    }


}

function describeSkill(s) {
    const damage = skillDamageProfile(s);
    switch (s.kind) {
        case 'atk': return `Gây ${Math.round(damage.total * 100)}% công${s.stun ? `, choáng ${s.stun} giây` : ''}${s.pierce ? `, xuyên ${Math.round(s.pierce * 100)}% thủ` : ''}${s.heal ? `, hồi ${Math.round(s.heal * 100)}% khí huyết` : ''}`;
        case 'multi': return `${damage.hits} đòn, tổng ${Math.round(damage.total * 100)}% công (${Math.round(damage.perHit * 100)}% mỗi đòn)${s.stun ? `, choáng ${s.stun} giây` : ''}`;
        case 'stun': return `Choáng đối thủ ${s.stun} giây`;
        case 'shield': return `Khiên ${Math.round(s.shield * 100)}% khí huyết`;
        case 'heal': return `Hồi ${Math.round(s.heal * 100)}% khí huyết`;
        case 'mana': return `Hồi ${Math.round(s.mana * 100)}% linh lực`;
        case 'dot': return `${Math.round(damage.total * 100)}% công tổng theo thời gian trong ${s.dur} giây`;
        case 'reflect': return `Phản ${Math.round(s.reflect * 100)}% sát thương trong ${s.dur} giây`;
        case 'escape': return 'Giải khống: thoát choáng, miễn choáng 5 giây';
        case 'buff': {
            const b = s.buff;
            if (b.crit) return `+${Math.round(b.crit * 100)}% chí mạng trong ${s.dur} giây`;
            if (b.atk) return `+${Math.round(b.atk * 100)}% công trong ${s.dur} giây`;
            if (b.dmgTaken) return `Giảm ${Math.round((1 - b.dmgTaken) * 100)}% sát thương nhận trong ${s.dur} giây`;
            if (b.rage) return `Công tăng theo khí huyết đã mất trong ${s.dur} giây`;
            if (b.maxHpMul) return `Gấp đôi khí huyết trong ${s.dur} giây`;
            if (b.forceCounter) return `Mọi kỹ năng tính là khắc hệ trong ${s.dur} giây`;
            return 'Tăng sức mạnh';
        }
        default: return '';
    }


}


module.exports = { Game, Battle, GameError, vnDate, vnHour, isNight, getTimePhase, itemName, describeSkill, SUB_STAGES, bossMutationEquipMultiplier, rawMonsterDropChance };
