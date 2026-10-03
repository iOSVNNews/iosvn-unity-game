// Danh mục cố định của hệ thống Tu Tiên: môn, hệ, linh căn, kỹ năng, vật phẩm,
// yêu thú, cửa hàng. Chỉnh số ở đây, không cần sửa logic.
'use strict';

// Tỉ lệ thấp nhất theo yêu cầu: 0,00000001% = 1e-10.
const RAREST = 1e-10;

const MON = Object.freeze({
    kiem:  { id: 'kiem',  name: 'Kiếm tu',                  weapon: 'kiem',     weaponName: 'Kiếm', statName: 'Kiếm Ý',
             mul: { hp: 0.95, mp: 1.0,  atk: 1.15, def: 0.9,  spd: 1.2,  sense: 1.1 } },
    phap:  { id: 'phap',  name: 'Pháp tu',                  weapon: 'phapkhi',  weaponName: 'Pháp khí', statName: 'Pháp Lực',
             mul: { hp: 0.9,  mp: 1.4,  atk: 1.2,  def: 0.85, spd: 1.0,  sense: 1.15 } },
    the:   { id: 'the',   name: 'Thể tu',                   weapon: 'trongkhi', weaponName: 'Trọng khí', statName: 'Thể Phách',
             mul: { hp: 1.35, mp: 0.8,  atk: 1.0,  def: 1.3,  spd: 0.9,  sense: 0.9 } },
    quyen: { id: 'quyen', name: 'Quyền tu',                 weapon: 'quyensao', weaponName: 'Quyền sáo', statName: 'Quyền Kình',
             mul: { hp: 1.1,  mp: 0.9,  atk: 1.2,  def: 1.05, spd: 1.05, sense: 0.95 } },
    dan:   { id: 'dan',   name: 'Đan sư (Luyện Đan)',       weapon: 'dinh',     weaponName: 'Dược đỉnh', statName: 'Đan Đạo',
             mul: { hp: 1.05, mp: 1.35, atk: 1.0,  def: 1.0,  spd: 1.0,  sense: 1.25 } },
    ren:   { id: 'ren',   name: 'Luyện khí sư (Thợ rèn)',   weapon: 'bua',      weaponName: 'Búa rèn', statName: 'Khí Đạo',
             mul: { hp: 1.2,  mp: 0.9,  atk: 1.15, def: 1.3,  spd: 0.95, sense: 1.1 } },
    phu:   { id: 'phu',   name: 'Phù sư (Luyện Phù)',       weapon: 'but',      weaponName: 'Phù Văn', statName: 'Phù Văn',
             mul: { hp: 1.0,  mp: 1.3,  atk: 1.1,  def: 0.95, spd: 1.1,  sense: 1.2 } },
    thu:   { id: 'thu',   name: 'Ngự Thú Sư',              weapon: 'quyensao', weaponName: 'Thú Ấn', statName: 'Ngự Thú Ấn',
             mul: { hp: 1.1,  mp: 1.0,  atk: 1.05, def: 1.05, spd: 1.05, sense: 1.15 } },
});

// beats: hệ bị mình khắc. Đánh vào hệ mình khắc: +25%; bị khắc: −20%.
const HE = Object.freeze({
    kim:   { id: 'kim',   name: 'Kim',   beats: 'moc',   effect: 'Xuyên 15% thủ' },
    moc:   { id: 'moc',   name: 'Mộc',   beats: 'tho',   effect: 'Đánh trúng hồi 5% khí huyết' },
    thuy:  { id: 'thuy',  name: 'Thủy',  beats: 'hoa',   effect: 'Giảm 10% công đối thủ 6 giây' },
    hoa:   { id: 'hoa',   name: 'Hỏa',   beats: 'kim',   effect: 'Đốt 2% khí huyết mỗi giây trong 5 giây' },
    tho:   { id: 'tho',   name: 'Thổ',   beats: 'thuy',  effect: 'Giảm 10% sát thương nhận 6 giây' },
    loi:   { id: 'loi',   name: 'Lôi',   beats: 'phong', effect: '+10% chí mạng, 10% tê liệt 2 giây' },
    phong: { id: 'phong', name: 'Phong', beats: 'bang',  effect: '15% đánh thêm một đòn' },
    bang:  { id: 'bang',  name: 'Băng',  beats: 'loi',   effect: 'Làm chậm đối thủ 5 giây' },
    thien: { id: 'thien', name: 'Thiện', beats: 'ma',    effect: 'Tịnh hóa ma khí, tăng 20% sát thương lên Ma Tu' },
    ma:    { id: 'ma',    name: 'Ma',    beats: 'thien', effect: 'Hấp huyết 15% sát thương chuyển thành sinh lực' },
});
const DAY_ELEMENTS = Object.freeze(['hoa', 'loi', 'thien']);
const NIGHT_ELEMENTS = Object.freeze(['thuy', 'bang', 'ma']);

const LINH_CAN = Object.freeze([
    { id: 'thien', name: 'Thiên linh căn', weight: 2, expMul: 1.08, statMul: { atk: 1.04, sense: 1.05 }, elemBonus: 0.05 },
    { id: 'hon_don', name: 'Hỗn Độn linh căn', weight: 1, expMul: 1.10, statMul: { hp: 1.05, atk: 1.04, sense: 1.06 }, elemBonus: 0.05 },
    { id: 'ngu_hanh', name: 'Ngũ Hành linh căn', weight: 8, expMul: 1.03, statMul: { hp: 1.03, mp: 1.03 }, elemBonus: 0.04 },
    { id: 'kiem', name: 'Kiếm linh căn', weight: 7, expMul: 1.02, statMul: { atk: 1.06, spd: 1.03 }, elemBonus: 0.02 },
    { id: 'phap', name: 'Pháp linh căn', weight: 6, expMul: 1.03, statMul: { mp: 1.08, sense: 1.04 }, elemBonus: 0.03 },
    { id: 'the', name: 'Thể linh căn', weight: 6, expMul: 0.99, statMul: { hp: 1.1, def: 1.06 }, daoTam: 105 },
    { id: 'dan', name: 'Đan linh căn', weight: 5, expMul: 1.02, statMul: { mp: 1.05, sense: 1.06 } },
    { id: 'ren', name: 'Khí linh căn', weight: 5, expMul: 1.01, statMul: { def: 1.06, atk: 1.02 } },
    { id: 'phu', name: 'Phù linh căn', weight: 5, expMul: 1.02, statMul: { mp: 1.04, spd: 1.04, sense: 1.04 } },
    { id: 'kim', name: 'Kim linh căn', weight: 6, expMul: 1.0, statMul: { atk: 1.05, def: 1.02 }, elemBonus: 0.04 },
    { id: 'moc', name: 'Mộc linh căn', weight: 6, expMul: 1.0, statMul: { hp: 1.04, def: 1.02 }, elemBonus: 0.03 },
    { id: 'thuy', name: 'Thủy linh căn', weight: 6, expMul: 1.01, statMul: { mp: 1.05, hp: 1.02 }, elemBonus: 0.03 },
    { id: 'hoa', name: 'Hỏa linh căn', weight: 6, expMul: 0.99, statMul: { atk: 1.06, spd: 1.02 }, elemBonus: 0.04 },
    { id: 'tho', name: 'Thổ linh căn', weight: 6, expMul: 0.98, statMul: { def: 1.07, hp: 1.04 }, elemBonus: 0.03 },
    { id: 'loi', name: 'Lôi linh căn', weight: 5, expMul: 1.02, statMul: { spd: 1.06, atk: 1.03 }, elemBonus: 0.04 },
    { id: 'phong', name: 'Phong linh căn', weight: 5, expMul: 1.02, statMul: { spd: 1.08, sense: 1.03 }, elemBonus: 0.03 },
    { id: 'bang', name: 'Băng linh căn', weight: 5, expMul: 1.0, statMul: { def: 1.04, mp: 1.04 }, elemBonus: 0.04 },
    { id: 'am', name: 'Âm linh căn', weight: 4, expMul: 1.0, statMul: { sense: 1.07, spd: 1.02 }, daoTam: 115, elemBonus: 0.03 },
    { id: 'duong', name: 'Dương linh căn', weight: 4, expMul: 1.0, statMul: { hp: 1.05, atk: 1.03 }, daoTam: 110, elemBonus: 0.03 },
    { id: 'khong_gian', name: 'Không Gian linh căn', weight: 2, expMul: 1.04, statMul: { spd: 1.05, sense: 1.05 }, elemBonus: 0.04 },
]);

// Chỉ số người chơi Phàm Nhân chưa trang bị; mỗi cảnh giới ×1,18.
const BASE_STATS = Object.freeze({ hp: 500, mp: 200, atk: 50, def: 30, spd: 10, sense: 10 });
const REALM_GROWTH = 1.18;
const POST_TIEN_DE_REALMS = Object.freeze([
    'Thiên Tiên Đế', 'Thiên Đế', 'Thánh Nhân', 'Thánh Vương', 'Thánh Hoàng', 'Thánh Đế',
    'Đạo Quân', 'Đạo Vương', 'Đạo Hoàng', 'Đạo Đế', 'Đạo Tôn', 'Đạo Chủ', 'Chí Tôn',
    'Thiên Chí Tôn', 'Hỗn Nguyên Chí Tôn', 'Hỗn Độn Chí Tôn', 'Vô Thượng Chí Tôn',
    'Chí Cao Chí Tôn', 'Hỗn Độn Đạo Quân', 'Hỗn Độn Đạo Tôn', 'Hỗn Độn Đạo Chủ',
    'Bản Nguyên Cảnh', 'Bản Nguyên Chúa Tể', 'Vạn Đạo Chúa Tể', 'Hư Vô Chúa Tể',
    'Thời Không Chúa Tể', 'Siêu Thoát Giả', 'Đại Siêu Thoát', 'Vô Cực Cảnh',
    'Vô Thượng Cảnh', 'Chư Thiên Chí Cao', 'Hỗn Nguyên Vô Cực', 'Sáng Thế Cảnh',
    'Sáng Thế Chúa Tể', 'Vạn Giới Chi Chủ'
]);
const PRE_TIEN_DE_REALMS = Object.freeze([
    'Phàm Nhân', 'Luyện Thể', 'Luyện Khí', 'Trúc Cơ', 'Kim Đan', 'Nguyên Anh', 'Hóa Thần', 'Luyện Hư', 'Hợp Thể', 'Đại Thừa',
    'Độ Kiếp', 'Bán Tiên', 'Đăng Tiên', 'Tán Tiên', 'Địa Tiên', 'Nhân Tiên', 'Chân Tiên', 'Huyền Tiên', 'Thiên Tiên', 'Kim Tiên',
    'Thái Ất Chân Tiên', 'Thái Ất Huyền Tiên', 'Thái Ất Kim Tiên', 'Đại La Chân Tiên', 'Đại La Kim Tiên', 'Hỗn Nguyên Kim Tiên',
    'Tiên Quân', 'Tiên Tôn', 'Chuẩn Tiên Vương', 'Tiên Vương', 'Tiên Đế',
]);
const CULTIVATION_REALM_NAMES = Object.freeze([...PRE_TIEN_DE_REALMS, ...POST_TIEN_DE_REALMS]);
const POST_TIEN_DE_MAP_CONFIGS = Object.freeze([
    { id: 'map_10', name: 'Thiên Ngoại Tiên Vực', range: [31, 34], towns: ['Thiên Ngoại Thành', 'Tinh Hà Tiên Trấn'], icon: '🌠' },
    { id: 'map_11', name: 'Thánh Linh Đại Lục', range: [35, 37], towns: ['Thánh Linh Thành', 'Vạn Thánh Tiên Đô'], icon: '🪷' },
    { id: 'map_12', name: 'Đạo Nguyên Thiên', range: [38, 41], towns: ['Đạo Nguyên Thành', 'Vô Cực Đạo Trấn'], icon: '☯️' },
    { id: 'map_13', name: 'Chí Tôn Thần Giới', range: [42, 44], towns: ['Chí Tôn Thần Thành', 'Thiên Khuyết Tiên Đô'], icon: '👑' },
    { id: 'map_14', name: 'Hỗn Độn Cổ Giới', range: [45, 48], towns: ['Hỗn Độn Thành', 'Cổ Giới Thần Đô'], icon: '🌌' },
    { id: 'map_15', name: 'Bản Nguyên Hải', range: [49, 51], towns: ['Bản Nguyên Tiên Thành', 'Khởi Nguyên Thánh Trấn'], icon: '💠' },
    { id: 'map_16', name: 'Vạn Đạo Thần Vực', range: [52, 55], towns: ['Vạn Đạo Thành', 'Chúa Tể Tiên Đô'], icon: '🌀' },
    { id: 'map_17', name: 'Thời Không Trường Hà', range: [56, 58], towns: ['Thời Không Thành', 'Tuế Nguyệt Tiên Trấn'], icon: '⌛' },
    { id: 'map_18', name: 'Siêu Thoát Thiên', range: [59, 62], towns: ['Siêu Thoát Thần Thành', 'Vô Cực Thiên Đô'], icon: '✨' },
    { id: 'map_19', name: 'Sáng Thế Thần Quốc', range: [63, 65], towns: ['Sáng Thế Thánh Thành', 'Vạn Giới Đế Đô'], icon: '🌍' },
].map(m => Object.freeze({ ...m, realmMin: m.range[0], realmMax: m.range[1], townIds: [`${m.id}_town_1`, `${m.id}_town_2`] })));
const postTienDeMonsterIds = (map, kind) => Array.from({ length: map.realmMax - map.realmMin + 1 }, (_, i) => `${kind}_${map.realmMin + i}`);
const POST_TIEN_DE_TOWNS = Object.freeze(POST_TIEN_DE_MAP_CONFIGS.flatMap(map => map.towns.map((name, i) => ({
    id: map.townIds[i], mapId: map.id, name, icon: i === 0 ? '🏯' : '🏙️',
    desc: `${name} trấn giữ ${map.name}, nơi tu sĩ hậu Tiên Đế lĩnh ngộ đại đạo và săn yêu thú viễn cổ.`,
    realmMin: i === 0 ? map.realmMin : Math.ceil((map.realmMin + map.realmMax) / 2),
    realmMinName: POST_TIEN_DE_REALMS[(i === 0 ? map.realmMin : Math.ceil((map.realmMin + map.realmMax) / 2)) - 31],
    realmCap: map.realmMax,
    x: 20 + i * 160, y: 20 + Math.floor((map.realmMin - 31) / 4) * 18,
    monsterPool: [...postTienDeMonsterIds(map, 'tiengioi_tieuyeu'), ...postTienDeMonsterIds(map, 'tiengioi_daiyeu'), ...postTienDeMonsterIds(map, 'tiengioi_boss')],
    healingCost: 50000 + map.realmMin * 500,
}))));

const RARITY = Object.freeze({
    pt:      { id: 'pt',      name: 'Phổ thông' },
    hiem:    { id: 'hiem',    name: 'Hiếm' },
    cuchiem: { id: 'cuchiem', name: 'Cực hiếm' },
    tt:      { id: 'tt',      name: 'Truyền thuyết' },
    cam:     { id: 'cam',     name: 'Sử thi' },
    vang:    { id: 'vang',    name: 'Thần thoại' },
    docban:  { id: 'docban',  name: 'Độc bản' },
});

// Quy tắc chung: phẩm chất tăng sát thương, hồi chiêu và mức tiêu hao linh lực.
const SKILL_RULES = Object.freeze({
    rarityRank: Object.freeze({ pt: 0, hiem: 1, cuchiem: 2, tt: 3, cam: 4, vang: 5, docban: 6 }),
    damageCap: Object.freeze({ pt: 1, hiem: 1.25, cuchiem: 1.5, tt: 2, cam: 2.4, vang: 2.7, docban: 3 }),
    baseCooldownSeconds: 5,
    cooldownStepSeconds: 5,
    secondaryEffectCooldownSeconds: 2,
    bigSkillCooldownSeconds: 5,
    maxCooldownSeconds: 30,
    realmStepSize: 10,
    realmCooldownReductionPerStep: 1,
    realmMpCostIncreasePerStep: 0.08,
    realmDamageIncreasePerStep: 0.03,
    maxRealmDamageIncrease: 0.18,
    minSkillMp: 10,
    maxBaseSkillMp: 40,
    mpCostTierMultiplier: 1.5,
    battleMpRegenFractionPerSecond: 0.01,
});

// Một tuyệt kỹ mới mở khóa ở mỗi cảnh giới Tiên Giới, với phẩm chất tăng dần.
const IMMORTAL_REALM_SKILLS = Object.freeze(CULTIVATION_REALM_NAMES.slice(11).map((realmName, index) => {
    const realm = index + 11;
    const rarity = realm <= 15 ? 'hiem' : realm <= 23 ? 'cuchiem' : realm <= 30 ? 'tt' : realm <= 40 ? 'cam' : realm <= 50 ? 'vang' : 'docban';
    const icons = ['🌠', '⚔️', '🌌', '🌀', '💠', '☯️', '✨'];
    const power = Math.min(2.95, 1.65 + (realm - 11) * 0.025);
    return Object.freeze({
        id: `tien_quyet_canh_${realm}`, mon: 'chung', name: `${realmName} Tiên Quyết`, icon: icons[index % icons.length],
        rarity, realm, kind: 'atk', power, pierce: Math.min(0.45, 0.12 + (realm - 11) * 0.006),
        cd: Math.min(180, 24 + Math.floor((realm - 11) / 4) * 12), big: realm % 5 === 0,
        mp: Math.min(90, 28 + (realm - 11) * 1.1), drop: Math.max(RAREST, 0.0012 * Math.pow(0.86, realm - 11)),
        stock: realm <= 30 ? 100 : 20, desc: `Tiên quyết truyền thừa của cảnh giới ${realmName}, vận chuyển tiên nguyên xuyên phá hộ thể.`
    });
}));

const ITEM_QUALITY_REALM_BANDS = Object.freeze([
    Object.freeze({ min: 0, max: 2, rank: 0 }),
    Object.freeze({ min: 3, max: 5, rank: 1 }),
    Object.freeze({ min: 6, max: 9, rank: 2 }),
    Object.freeze({ min: 10, max: 15, rank: 3 }),
    Object.freeze({ min: 16, max: 23, rank: 4 }),
    Object.freeze({ min: 24, max: 65, rank: 5 }),
]);
function qualityRankForRealm(realm) {
    const index = Math.max(0, Math.floor(Number(realm) || 0));
    return ITEM_QUALITY_REALM_BANDS.find(band => index >= band.min && index <= band.max)?.rank ?? 0;
}

// kind: atk | multi | stun | shield | heal | buff | escape | dot | reflect | mana
// cd: giây theo đồng hồ thật. big: chiêu lớn. realm: cảnh giới tối thiểu để học.
// drop: tỉ lệ rơi ngọc giản mỗi lần hạ yêu thú có cảnh giới ≥ realm.
// stock: số lượng tối đa trong game (null = không giới hạn).
const SKILLS = Object.freeze([
    { id: 'thu_an_tran', mon: 'thu', name: 'Ngự Thú Ấn', icon: '🐾', rarity: 'pt', realm: 0, kind: 'buff', buff: { atk: 0.15, dmgTaken: 0.9 }, dur: 10, cd: 30, mp: 20, drop: 0.02, stock: null, desc: 'Ấn quyết khống thú, tăng chiến ý cho linh thú đang trợ chiến.' },
    { id: 'trieu_hoan_linh_thu', mon: 'thu', name: 'Triệu Hoán Linh Thú', icon: '🐉', rarity: 'pt', realm: 0, kind: 'atk', power: 1.6, cd: 12, mp: 25, drop: 0.02, stock: null, summonBeast: true, desc: 'Triệu hồi linh thú đã thu phục hợp kích cùng một đòn.' },
    // Kiếm tu (Kiếm Lai & Phàm Nhân Tu Tiên)
    { id: 'ngu_kiem', mon: 'kiem', name: 'Ngự Kiếm Thuật', icon: '🗡️', rarity: 'pt', realm: 0, kind: 'atk', power: 1.1, critBonus: 0.15, cd: 3, mp: 8, drop: 0.02, stock: null },
    { id: 'kiem_khi_tram', mon: 'kiem', name: 'Kiếm Khí Trảm', icon: '⚡', rarity: 'pt', realm: 0, kind: 'atk', power: 1.35, cd: 6, mp: 14, drop: 0.02, stock: null },
    { id: 'kiem_tam', mon: 'kiem', name: 'Kiếm Tâm Thông Minh', icon: '🧘', rarity: 'pt', realm: 0, kind: 'buff', buff: { crit: 0.2 }, dur: 8, cd: 20, mp: 20, drop: 0.015, stock: null },
    { id: 'kiem_don', mon: 'kiem', name: 'Kiếm Độn Vạn Lý', icon: '💨', rarity: 'pt', realm: 0, kind: 'escape', nextCrit: true, cd: 45, mp: 15, drop: 0.01, stock: null },
    { id: 'phi_kiem_lien_tram', mon: 'kiem', name: 'Thanh Trúc Kiếm Trận', icon: '✨', rarity: 'hiem', realm: 3, kind: 'multi', power: 0.7, hits: 3, cd: 8, mp: 24, drop: 0.004, stock: 100 },
    { id: 'kiem_khi_ho_the', mon: 'kiem', name: 'Kiếm Khí Ngân Hà', icon: '🛡️', rarity: 'hiem', realm: 4, kind: 'reflect', reflect: 0.15, dur: 8, cd: 25, mp: 22, drop: 0.003, stock: 100 },
    { id: 'nhat_kiem_pha_khong', mon: 'kiem', name: 'Trảm Tiên Kiếm Quyết', icon: '⚔️', rarity: 'cuchiem', realm: 5, kind: 'atk', power: 2.6, pierceShield: true, cd: 90, big: true, mp: 50, drop: 0.0003, stock: 20 },
    { id: 'kiem_vuc', mon: 'kiem', name: 'Vạn Kiếm Quy Tông', icon: '🌪️', rarity: 'tt', realm: 6, kind: 'multi', power: 0.8, hits: 5, stun: 1, cd: 300, big: true, mp: 90, drop: 0, stock: 3 },
    // Pháp tu (Phàm Nhân Tu Tiên & Tiên Nghịch)
    { id: 'hoa_cau', mon: 'phap', name: 'Xích Diễm Chân Hỏa', icon: '🔥', rarity: 'pt', realm: 0, kind: 'atk', power: 1.2, cd: 4, mp: 12, drop: 0.02, stock: null },
    { id: 'huyen_quy_thuan', mon: 'phap', name: 'Huyền Quy Hộ Thuẫn', icon: '🛡️', rarity: 'pt', realm: 0, kind: 'shield', shield: 0.25, cd: 20, mp: 25, drop: 0.015, stock: null },
    { id: 'ngung_linh_tien', mon: 'phap', name: 'Tích Tà Thần Lôi', icon: '⚡', rarity: 'pt', realm: 0, kind: 'atk', power: 1.0, doubleEffect: true, cd: 5, mp: 12, drop: 0.02, stock: null },
    { id: 'thuan_di', mon: 'phap', name: 'Phong Lôi Độn', icon: '💨', rarity: 'pt', realm: 0, kind: 'escape', dodge: 1, cd: 45, mp: 15, drop: 0.01, stock: null },
    { id: 'dinh_than', mon: 'phap', name: 'Định Thân Thuật', icon: '👁️', rarity: 'hiem', realm: 4, kind: 'stun', stun: 2, cd: 25, mp: 30, drop: 0.003, stock: 100 },
    { id: 'hoa_long', mon: 'phap', name: 'Đại Diễn Thần Quyết', icon: '🐉', rarity: 'cuchiem', realm: 5, kind: 'atk', power: 1.8, cd: 12, mp: 35, drop: 0.0003, stock: 20 },
    { id: 'ngu_hanh_luan_chuyen', mon: 'phap', name: 'Chân Ngôn Hóa Thần Luân', icon: '☸️', rarity: 'tt', realm: 6, kind: 'buff', buff: { forceCounter: true }, dur: 10, cd: 180, big: true, mp: 60, drop: 0, stock: 3 },
    // Thể tu (Phàm Nhân Tu Tiên & Tiên Nghịch)
    { id: 'khai_son_chuong', mon: 'the', name: 'Phạn Thánh Chân Ma Công', icon: '🥊', rarity: 'pt', realm: 0, kind: 'atk', power: 1.3, cd: 5, mp: 10, drop: 0.02, stock: null },
    { id: 'thiet_bi', mon: 'the', name: 'Bách Mạch Luyện Thể', icon: '🥋', rarity: 'pt', realm: 0, kind: 'buff', buff: { dmgTaken: 0.8 }, dur: 8, cd: 20, mp: 15, drop: 0.015, stock: null },
    { id: 'chan_dia_kich', mon: 'the', name: 'Bát Cực Ma Thể', icon: '💥', rarity: 'pt', realm: 0, kind: 'atk', power: 1.0, stunChance: 0.2, stun: 1, cd: 7, mp: 14, drop: 0.02, stock: null },
    { id: 'ba_the', mon: 'the', name: 'Cổ Thần Nhục Thân', icon: '🗿', rarity: 'pt', realm: 0, kind: 'escape', immune: 6, cd: 45, mp: 15, drop: 0.01, stock: null },
    { id: 'huyet_khi_cuong_bao', mon: 'the', name: 'Cổ Thần Toàn Phong', icon: '🌪️', rarity: 'hiem', realm: 4, kind: 'buff', buff: { rage: true }, dur: 10, cd: 40, mp: 25, drop: 0.003, stock: 100 },
    { id: 'kim_cang_bat_hoai', mon: 'the', name: 'Bát Tinh Bất Hoại Thể', icon: '🛡️', rarity: 'cuchiem', realm: 5, kind: 'shield', shield: 0.3, cd: 60, big: true, mp: 40, drop: 0.0003, stock: 20 },
    { id: 'phap_tuong', mon: 'the', name: 'Chân Ma Pháp Tướng', icon: '👹', rarity: 'tt', realm: 6, kind: 'buff', buff: { maxHpMul: 2 }, dur: 10, cd: 300, big: true, mp: 70, drop: 0, stock: 3 },
    // Quyền tu (Kiếm Lai)
    { id: 'pha_son_quyen', mon: 'quyen', name: 'Sơn Nhạc Trọng Quyền', icon: '🏔️', rarity: 'pt', realm: 0, kind: 'atk', power: 1.5, cd: 6, mp: 14, drop: 0.02, stock: null },
    { id: 'tam_lien_quyen', mon: 'quyen', name: 'Thần Đao Bạt Lực', icon: '🥊', rarity: 'pt', realm: 0, kind: 'multi', power: 0.5, hits: 3, cd: 5, mp: 12, drop: 0.02, stock: null },
    { id: 'thiet_quyen_the', mon: 'quyen', name: 'Bất Bại Quyền Ý', icon: '👊', rarity: 'pt', realm: 0, kind: 'buff', buff: { atk: 0.15 }, dur: 8, cd: 20, mp: 18, drop: 0.015, stock: null },
    { id: 'chan_khi', mon: 'quyen', name: 'Lục Cực Chấn Địa', icon: '⚡', rarity: 'pt', realm: 0, kind: 'escape', stunEnemy: 1, cd: 45, mp: 15, drop: 0.01, stock: null },
    { id: 'bat_hoang_pha', mon: 'quyen', name: 'Cửu Trọng Quyền Cương', icon: '💥', rarity: 'hiem', realm: 3, kind: 'atk', power: 1.5, pierce: 0.3, cd: 8, mp: 22, drop: 0.004, stock: 100 },
    { id: 'phan_chan', mon: 'quyen', name: 'Trảm Nhân Quả Quyền', icon: '☯️', rarity: 'hiem', realm: 4, kind: 'reflect', reflect: 1, dur: 2, cd: 20, mp: 20, drop: 0.003, stock: 100 },
    { id: 'lien_hoan_quyen', mon: 'quyen', name: 'Vạn Lý Sơn Hà Quyền', icon: '🌊', rarity: 'cuchiem', realm: 5, kind: 'multi', power: 0.6, hits: 4, cd: 12, mp: 30, drop: 0.0003, stock: 20 },
    { id: 'quyen_y_hop_nhat', mon: 'quyen', name: 'Thập Cảnh Vô Địch Quyền', icon: '👑', rarity: 'tt', realm: 6, kind: 'atk', power: 3.5, stun: 2, cd: 240, big: true, mp: 80, drop: 0, stock: 3 },
    // Đan sư (Phàm Nhân Tu Tiên & Tiên Nghịch)
    { id: 'hoi_xuan_thuat', mon: 'dan', name: 'Thảo Mộc Dưỡng Sinh', icon: '🌿', rarity: 'pt', realm: 0, kind: 'heal', heal: 0.25, cd: 15, mp: 22, drop: 0.015, stock: null },
    { id: 'doc_vu', mon: 'dan', name: 'Cửu U Độc Hỏa', icon: '🧪', rarity: 'pt', realm: 0, kind: 'dot', dot: 0.35, dur: 6, cd: 12, mp: 18, drop: 0.02, stock: null },
    { id: 'linh_cham', mon: 'dan', name: 'Vạn Thảo Linh Châm', icon: '🪡', rarity: 'pt', realm: 0, kind: 'atk', power: 1.15, cd: 4, mp: 10, drop: 0.02, stock: null },
    { id: 'thanh_tam_chu', mon: 'dan', name: 'Thanh Tâm Linh Chú', icon: '🪷', rarity: 'pt', realm: 0, kind: 'escape', heal: 0.15, cd: 45, mp: 15, drop: 0.01, stock: null },
    { id: 'dan_hoa', mon: 'dan', name: 'Thiên Địa Đan Hỏa', icon: '🔥', rarity: 'hiem', realm: 4, kind: 'dot', dot: 0.55, dur: 6, cd: 12, mp: 24, drop: 0.003, stock: 100 },
    { id: 'khoi_loi', mon: 'dan', name: 'Đại Diễn Khôi Lỗi', icon: '🤖', rarity: 'cuchiem', realm: 6, kind: 'shield', shield: 0.4, cd: 60, big: true, mp: 45, drop: 0.0002, stock: 20 },
    // Phù sư (Kiếm Lai & Phàm Nhân Tu Tiên)
    { id: 'phu_loi_hoa', mon: 'phu', name: 'Lôi Hỏa Phù', icon: '⚡', rarity: 'pt', realm: 0, kind: 'atk', power: 1.25, cd: 4, mp: 12, drop: 0.02, stock: null, desc: 'Ném ra lôi hỏa đạo phù bộc phá linh lực thiêu đốt đối thủ.' },
    { id: 'phu_ho_than', mon: 'phu', name: 'Kim Quang Hộ Phù', icon: '🛡️', rarity: 'pt', realm: 0, kind: 'shield', shield: 0.25, cd: 20, mp: 20, drop: 0.015, stock: null, desc: 'Triệu hoán kim quang phù văn hộ thể, chống đỡ 25% sát thương.' },
    { id: 'phu_dinh_than_chu', mon: 'phu', name: 'Định Thân Phù Chú', icon: '👁️', rarity: 'pt', realm: 0, kind: 'stun', stun: 1.5, cd: 22, mp: 25, drop: 0.015, stock: null, desc: 'Dán phù phong tỏa kinh mạch mục tiêu trong 1.5 giây.' },
    { id: 'phu_don_quyet', mon: 'phu', name: 'Vạn Dặm Thần Hành Phù', icon: '💨', rarity: 'pt', realm: 0, kind: 'escape', dodge: 1.5, cd: 45, mp: 15, drop: 0.01, stock: null, desc: 'Đốt phù thần hành dịch chuyển né tránh công kích chí tử.' },
    { id: 'phu_chu_sa_tran', mon: 'phu', name: 'Bát Quái Phù Trận', icon: '☸️', rarity: 'hiem', realm: 4, kind: 'multi', power: 0.75, hits: 4, cd: 16, mp: 35, drop: 0.003, stock: 100, desc: 'Bố trí phù trận bát quái giáng lôi hỏa cuồng bạo lên quân địch.' },
    { id: 'phu_tram_tien_dai_phap', mon: 'phu', name: 'Trảm Tiên Đạo Phù', icon: '⚔️', rarity: 'cuchiem', realm: 6, kind: 'atk', power: 3.8, pierceShield: true, cd: 120, big: true, mp: 80, drop: 0.0002, stock: 20, desc: 'Thiêu đốt tinh huyết thúc giục Trảm Tiên Đạo Phù cái thế trảm diệt thiên kiêu.' },

    // Luyện khí sư (Kiếm Lai & Tiên Nghịch)
    { id: 'chuy_phong_tram', mon: 'ren', name: 'Lò Luyện Không Thiên', icon: '🔨', rarity: 'pt', realm: 0, kind: 'atk', power: 1.25, cd: 4, mp: 10, drop: 0.02, stock: null },
    { id: 'luyen_khi_cuong_the', mon: 'ren', name: 'Khí Linh Phụ Thể', icon: '🛡️', rarity: 'pt', realm: 0, kind: 'shield', shield: 0.25, cd: 20, mp: 20, drop: 0.015, stock: null },
    { id: 'bach_binh_pha_giap', mon: 'ren', name: 'Bách Luyện Thần Binh', icon: '⚔️', rarity: 'pt', realm: 0, kind: 'atk', power: 1.4, cd: 6, mp: 14, drop: 0.02, stock: null },
    { id: 'lo_luyen_kim_than', mon: 'ren', name: 'Thiên Hỏa Đúc Thể', icon: '🔥', rarity: 'pt', realm: 0, kind: 'escape', immune: 6, cd: 45, mp: 15, drop: 0.01, stock: null },
    { id: 'thien_hoa_luyen_khi', mon: 'ren', name: 'Chân Kim Hóa Khí', icon: '⚒️', rarity: 'hiem', realm: 4, kind: 'buff', buff: { atk: 0.25, def: 0.15 }, dur: 8, cd: 25, mp: 25, drop: 0.003, stock: 100 },
    { id: 'van_binh_trieu_tong', mon: 'ren', name: 'Thiên Địa Vạn Khí Trận', icon: '🌌', rarity: 'cuchiem', realm: 6, kind: 'multi', power: 0.85, hits: 4, cd: 90, big: true, mp: 60, drop: 0.0002, stock: 20 },
    // Tuyệt kỹ chuyên tu: bổ sung nhánh phối hợp và mục tiêu săn ngọc giản theo từng hệ phái.
    { id: 'kiem_kiem_vu_phan_quang', mon: 'kiem', name: 'Kiếm Vũ Phân Quang', icon: '✨', rarity: 'hiem', realm: 3, reqRoleStat: 8, kind: 'multi', power: 0.62, hits: 4, cd: 12, mp: 28, drop: 0.0035, stock: 120 },
    { id: 'kiem_kiem_tam_nhat_niem', mon: 'kiem', name: 'Kiếm Tâm Nhất Niệm', icon: '🗡️', rarity: 'cuchiem', realm: 5, reqRoleStat: 24, kind: 'buff', buff: { crit: 0.22, atk: 0.12 }, dur: 8, cd: 55, mp: 42, drop: 0.0008, stock: 24 },
    { id: 'phap_ngu_linh_luan', mon: 'phap', name: 'Ngũ Linh Luân Chuyển', icon: '☸️', rarity: 'hiem', realm: 3, reqRoleStat: 8, kind: 'atk', power: 1.6, doubleEffect: true, cd: 10, mp: 28, drop: 0.0035, stock: 120 },
    { id: 'phap_thien_linh_bao_y', mon: 'phap', name: 'Thiên Linh Bảo Y', icon: '🛡️', rarity: 'cuchiem', realm: 5, reqRoleStat: 24, kind: 'shield', shield: 0.28, cd: 48, mp: 40, drop: 0.0008, stock: 24 },
    { id: 'the_long_tuong_bo_sat', mon: 'the', name: 'Long Tượng Bộ Sát', icon: '🐘', rarity: 'hiem', realm: 3, reqRoleStat: 8, kind: 'atk', power: 1.7, stunChance: 0.18, stun: 1, cd: 11, mp: 24, drop: 0.0035, stock: 120 },
    { id: 'the_bat_hoang_kim_than', mon: 'the', name: 'Bát Hoang Kim Thân', icon: '🗿', rarity: 'cuchiem', realm: 5, reqRoleStat: 24, kind: 'buff', buff: { dmgTaken: 0.72 }, dur: 9, cd: 60, mp: 38, drop: 0.0008, stock: 24 },
    { id: 'quyen_phuc_hai_quyen', mon: 'quyen', name: 'Phục Hải Liên Hoàn Quyền', icon: '🌊', rarity: 'hiem', realm: 3, reqRoleStat: 8, kind: 'multi', power: 0.58, hits: 4, cd: 12, mp: 26, drop: 0.0035, stock: 120 },
    { id: 'quyen_pha_gioi_kinh', mon: 'quyen', name: 'Phá Giới Kình', icon: '💥', rarity: 'cuchiem', realm: 5, reqRoleStat: 24, kind: 'atk', power: 2.65, pierce: 0.35, cd: 50, mp: 42, drop: 0.0008, stock: 24 },
    { id: 'dan_van_moc_huan_duong', mon: 'dan', name: 'Vạn Mộc Hoàn Xuân', icon: '🌿', rarity: 'hiem', realm: 3, reqRoleStat: 8, kind: 'heal', heal: 0.22, cd: 28, mp: 30, drop: 0.0035, stock: 120 },
    { id: 'dan_bach_doc_an', mon: 'dan', name: 'Bách Độc Thực Tâm', icon: '🧪', rarity: 'cuchiem', realm: 5, reqRoleStat: 24, kind: 'dot', dot: 0.8, dur: 6, cd: 42, mp: 38, drop: 0.0008, stock: 24 },
    { id: 'phu_lien_tam_phong_an', mon: 'phu', name: 'Liên Tâm Phong Ấn', icon: '📜', rarity: 'hiem', realm: 3, reqRoleStat: 8, kind: 'stun', stun: 1.35, cd: 24, mp: 28, drop: 0.0035, stock: 120 },
    { id: 'phu_thien_luo_van_phu', mon: 'phu', name: 'Thiên La Vạn Phù', icon: '🧿', rarity: 'cuchiem', realm: 5, reqRoleStat: 24, kind: 'multi', power: 0.68, hits: 4, cd: 48, mp: 44, drop: 0.0008, stock: 24 },
    { id: 'ren_bach_luyen_khi', mon: 'ren', name: 'Bách Luyện Khí Cương', icon: '⚒️', rarity: 'hiem', realm: 3, reqRoleStat: 8, kind: 'buff', buff: { atk: 0.18, dmgTaken: 0.9 }, dur: 9, cd: 38, mp: 28, drop: 0.0035, stock: 120 },
    { id: 'ren_van_binh_quy_nhat', mon: 'ren', name: 'Vạn Binh Quy Nhất', icon: '⚔️', rarity: 'cuchiem', realm: 5, reqRoleStat: 24, kind: 'atk', power: 2.6, critBonus: 0.12, cd: 48, mp: 42, drop: 0.0008, stock: 24 },

    // Chung
    { id: 'tu_linh_quyet', mon: 'chung', name: 'Tụ Linh Quyết', icon: '✨', rarity: 'pt', realm: 0, kind: 'mana', mana: 0.3, cd: 30, mp: 0, drop: 0.01, stock: null },
    { id: 'cuu_chuyen_an', mon: 'chung', name: 'Cửu Chuyển Luân Hồi Ấn', icon: '🔱', rarity: 'docban', realm: 8, kind: 'atk', power: 8.5, heal: 0.3, cd: 600, big: true, mp: 100, drop: RAREST, stock: 1 },

    // Công pháp theo cảnh giới: mọi môn đều học được. grant: tự lĩnh ngộ khi đột phá
    // Luyện Thể (cảnh giới 1)
    { id: 'lt_thiet_cot', mon: 'chung', set: 1, grant: true, name: 'Kim Cương Quyết', icon: '🛡️', rarity: 'pt', realm: 1, kind: 'buff', buff: { dmgTaken: 0.85 }, dur: 8, cd: 25, mp: 10, drop: 0, stock: null },
    { id: 'lt_bang_son', mon: 'chung', set: 1, name: 'Băng Sơn Kích', icon: '❄️', rarity: 'pt', realm: 1, kind: 'atk', power: 1.4, stunChance: 0.15, stun: 1, cd: 7, mp: 12, drop: 0.012, stock: null },
    { id: 'lt_luyen_bi', mon: 'chung', set: 1, name: 'Luyện Bì Thuật', icon: '🥋', rarity: 'pt', realm: 1, kind: 'shield', shield: 0.18, cd: 20, mp: 15, drop: 0.01, stock: null },
    { id: 'lt_ho_khieu', mon: 'chung', set: 1, name: 'Hổ Khiếu Sơn Lâm', icon: '🐅', rarity: 'hiem', realm: 1, kind: 'stun', stun: 1.5, cd: 22, mp: 18, drop: 0.003, stock: 300 },
    // Luyện Khí (cảnh giới 2)
    { id: 'lk_dan_khi', mon: 'chung', set: 2, grant: true, name: 'Dẫn Khí Quyết', icon: '🌀', rarity: 'pt', realm: 2, kind: 'mana', mana: 0.35, cd: 25, mp: 0, drop: 0, stock: null },
    { id: 'lk_khi_nhan', mon: 'chung', set: 2, name: 'Khí Nhận', icon: '🗡️', rarity: 'pt', realm: 2, kind: 'atk', power: 1.3, pierce: 0.15, cd: 5, mp: 12, drop: 0.012, stock: null },
    { id: 'lk_cuong_khi', mon: 'chung', set: 2, name: 'Hộ Thể Cương Khí', icon: '🛡️', rarity: 'pt', realm: 2, kind: 'shield', shield: 0.22, cd: 22, mp: 18, drop: 0.01, stock: null },
    { id: 'lk_ngu_khi', mon: 'chung', set: 2, name: 'Ngự Khí Phi Hành', icon: '🦅', rarity: 'hiem', realm: 2, kind: 'escape', dodge: 1.5, cd: 50, mp: 15, drop: 0.003, stock: 300 },
    // Trúc Cơ (cảnh giới 3)
    { id: 'tc_dao_the', mon: 'chung', set: 3, grant: true, name: 'Trúc Cơ Đạo Thể', icon: '✨', rarity: 'pt', realm: 3, kind: 'buff', buff: { atk: 0.12 }, dur: 10, cd: 30, mp: 20, drop: 0, stock: null },
    { id: 'tc_hoi_nguyen', mon: 'chung', set: 3, name: 'Hồi Nguyên Thuật', icon: '💊', rarity: 'pt', realm: 3, kind: 'heal', heal: 0.15, cd: 25, mp: 25, drop: 0.008, stock: null },
    { id: 'tc_linh_ap', mon: 'chung', set: 3, name: 'Linh Áp', icon: '⚡', rarity: 'hiem', realm: 3, kind: 'atk', power: 1.6, cd: 9, mp: 22, drop: 0.004, stock: 300 },
    // Kim Đan (cảnh giới 4)
    { id: 'kd_ho_the', mon: 'chung', set: 4, grant: true, name: 'Kim Đan Hộ Thể', icon: '🛡️', rarity: 'pt', realm: 4, kind: 'shield', shield: 0.25, cd: 30, mp: 25, drop: 0, stock: null },
    { id: 'kd_dan_hoa', mon: 'chung', set: 4, name: 'Đan Hỏa Phần Thiên', icon: '🔥', rarity: 'hiem', realm: 4, kind: 'dot', dot: 0.6, dur: 6, cd: 15, mp: 30, drop: 0.003, stock: 200 },
    { id: 'kd_kim_quang', mon: 'chung', set: 4, name: 'Kim Quang Trảm', icon: '⚔️', rarity: 'cuchiem', realm: 4, kind: 'atk', power: 2, cd: 12, mp: 35, drop: 0.0005, stock: 50 },
    // Nguyên Anh (cảnh giới 5)
    { id: 'na_xuat_khieu', mon: 'chung', set: 5, grant: true, name: 'Nguyên Anh Xuất Khiếu', icon: '🪷', rarity: 'hiem', realm: 5, kind: 'escape', dodge: 2, cd: 60, mp: 30, drop: 0, stock: null },
    { id: 'na_linh_vu', mon: 'chung', set: 5, name: 'Thiên Địa Linh Vũ', icon: '🌧️', rarity: 'hiem', realm: 5, kind: 'heal', heal: 0.3, cd: 30, mp: 40, drop: 0.002, stock: 100 },
    { id: 'na_tam_muoi', mon: 'chung', set: 5, name: 'Tam Muội Chân Hỏa', icon: '🔥', rarity: 'cuchiem', realm: 5, kind: 'atk', power: 2.8, cd: 15, mp: 50, drop: 0.0003, stock: 25 },
    // Hóa Thần (cảnh giới 6)
    { id: 'ht_y_canh', mon: 'chung', set: 6, grant: true, name: 'Sinh Tử Ý Cảnh', icon: '🌟', rarity: 'cuchiem', realm: 6, kind: 'buff', buff: { crit: 0.3, atk: 0.2 }, dur: 10, cd: 90, big: true, mp: 60, drop: 0, stock: null },
    { id: 'ht_quy_nguyen', mon: 'chung', set: 6, name: 'Vạn Vật Quy Nguyên', icon: '🌌', rarity: 'tt', realm: 6, kind: 'atk', power: 4.2, stun: 2, cd: 200, big: true, mp: 90, drop: 0.0001, stock: 10 },

    // ---- 50 BỘ KỸ NĂNG MA TU ĐỘC QUYỀN (Rơi khi Ma Tu đánh quái & Tu Luyện Ma Đạo) ----
    {"id":"ma_sat_luc","mon":"chung","demonOnly":true,"name":"Sát Lục Tiên Quyết","icon":"🩸","rarity":"tt","realm":3,"kind":"atk","power":3.2,"critBonus":0.25,"cd":10,"mp":30,"drop":0.005,"stock":10000,"desc":"Vương Lâm sát lục ý cảnh, ngưng tụ huyết vụ ngút trời đồ sát địch thủ."},
    {"id":"ma_hoa_huyet","mon":"chung","demonOnly":true,"name":"Hóa Huyết Thần Công","icon":"🩸","rarity":"cuchiem","realm":2,"kind":"atk","power":2.2,"heal":0.25,"cd":8,"mp":25,"drop":0.01,"stock":20000,"desc":"Ma công hút cạn tinh huyết đối phương, chuyển hóa thành khí huyết bản thân."},
    {"id":"ma_cuu_u","mon":"chung","demonOnly":true,"name":"Cửu U Ma Diễm","icon":"🔥","rarity":"hiem","realm":2,"kind":"dot","dot":0.8,"dur":5,"cd":12,"mp":28,"drop":0.015,"stock":30000,"desc":"Ma diễm U Minh thiêu đốt linh lực và kinh mạch kẻ địch liên tục."},
    {"id":"ma_thien_ma_don","mon":"chung","demonOnly":true,"name":"Thiên Ma Độn Pháp","icon":"💨","rarity":"hiem","realm":1,"kind":"escape","dodge":2.2,"cd":40,"mp":20,"drop":0.02,"stock":50000,"desc":"Hóa thành huyết quang hư ảo né tránh toàn bộ công kích trong chớp mắt."},
    {"id":"ma_nghich_chi","mon":"chung","demonOnly":true,"name":"Nghịch Ma Chỉ","icon":"👉","rarity":"tt","realm":5,"kind":"atk","power":2.0,"stun":2,"big":true,"cd":150,"mp":75,"drop":0.001,"stock":5000,"desc":"Một chỉ nghịch thiên phá toái hư không, gây sát thương hủy diệt và gây choáng."},
    {"id":"ma_phe_hon","mon":"chung","demonOnly":true,"name":"Vạn Hồn Phệ Cắn","icon":"👻","rarity":"cuchiem","realm":4,"kind":"multi","power":0.8,"hits":4,"cd":14,"mp":40,"drop":0.003,"stock":15000,"desc":"Triệu hoán ức vạn lệ quỷ từ Hồn Phiên lao vào cắn xé linh hồn đối phương."},
    {"id":"ma_thac_thien","mon":"chung","demonOnly":true,"name":"Thác Thiên Ma Công","icon":"🛡️","rarity":"tt","realm":6,"kind":"shield","shield":0.5,"cd":60,"big":true,"mp":60,"drop":0.0005,"stock":3000,"desc":"Hóa thân Ma Tổ viễn cổ, dựng lên ma giáp bất khả xâm phạm."},
    {"id":"ma_huyet_anh","mon":"chung","demonOnly":true,"name":"Huyết Anh Tán Thân","icon":"👶","rarity":"hiem","realm":3,"kind":"escape","dodge":2,"heal":0.2,"cd":45,"mp":30,"drop":0.008,"stock":20000,"desc":"Phân tách huyết anh đào thoát, hồi phục 20% sinh lực."},
    {"id":"ma_tuyet_tinh","mon":"chung","demonOnly":true,"name":"Tuyệt Tình Ý Cảnh","icon":"🖤","rarity":"cuchiem","realm":4,"kind":"buff","buff":{"crit":0.3,"atk":0.2},"dur":10,"cd":40,"mp":35,"drop":0.004,"stock":15000,"desc":"Dứt bỏ thất tình lục dục, công kích cuồng bạo tăng vọt chí mạng."},
    {"id":"ma_am_sat","mon":"chung","demonOnly":true,"name":"Ma Đạo Ám Sát Thuật","icon":"🗡️","rarity":"hiem","realm":2,"kind":"atk","power":2,"pierce":0.4,"cd":7,"mp":22,"drop":0.012,"stock":30000,"desc":"Ám kích từ bóng tối, xuyên thấu 40% phòng ngự kẻ địch."},
    {"id":"ma_hoang_tuyen","mon":"chung","demonOnly":true,"name":"Hoàng Tuyền Huyết Hải","icon":"🌊","rarity":"tt","realm":5,"kind":"multi","power":0.9,"hits":4,"cd":16,"mp":50,"drop":0.001,"stock":6000,"desc":"Biển máu Hoàng Tuyền dâng trào nuốt chửng vạn vật."},
    {"id":"ma_van_cot_chuong","mon":"chung","demonOnly":true,"name":"Vạn Cốt U Hồn Chưởng","icon":"🦴","rarity":"hiem","realm":2,"kind":"atk","power":1.9,"stunChance":0.25,"stun":1,"cd":8,"mp":24,"drop":0.01,"stock":25000,"desc":"Chưởng lực bạch cốt mang theo hàn khí u hồn làm tê liệt đối phương."},
    {"id":"ma_luyen_thi","mon":"chung","demonOnly":true,"name":"Luyện Thi Phụ Thể","icon":"🧟","rarity":"hiem","realm":3,"kind":"shield","shield":0.35,"cd":25,"mp":30,"drop":0.008,"stock":20000,"desc":"Mượn thi khí đồng giáp thi bảo hộ thân thể kiên cố."},
    {"id":"ma_huyet_loi","mon":"chung","demonOnly":true,"name":"Ma Huyết Thần Lôi","icon":"⚡","rarity":"cuchiem","realm":4,"kind":"atk","power":2.8,"critBonus":0.2,"cd":11,"mp":40,"drop":0.003,"stock":12000,"desc":"Sét máu tà dị bộc phát uy lực hủy diệt thần thức."},
    {"id":"ma_u_minh_kiem","mon":"kiem","demonOnly":true,"name":"U Minh Ma Kiếm","icon":"⚔️","rarity":"cuchiem","realm":4,"kind":"atk","power":3,"pierceShield":true,"cd":10,"mp":38,"drop":0.003,"stock":12000,"desc":"Kiếm khí U Minh bỏ qua hoàn toàn hộ thuẫn của đối thủ."},
    {"id":"ma_quy_vuong_an","mon":"chung","demonOnly":true,"name":"Quỷ Vương Phá Thiên Ấn","icon":"👑","rarity":"cuchiem","realm":4,"kind":"atk","power":2.9,"stunChance":0.3,"stun":1.5,"cd":12,"mp":42,"drop":0.0025,"stock":10000,"desc":"Đại ấn Quỷ Vương nghiền ép thân xác đối phương."},
    {"id":"ma_thao_thiet","mon":"chung","demonOnly":true,"name":"Thao Thiết Thôn Phệ Quyết","icon":"🐲","rarity":"tt","realm":5,"kind":"atk","power":3.5,"heal":0.35,"cd":15,"mp":55,"drop":0.0008,"stock":5000,"desc":"Thôn phệ sinh mệnh đối phương, hồi phục 35% HP bản thân."},
    {"id":"ma_tu_khi_quyet","mon":"chung","demonOnly":true,"name":"Cửu U Tụ Khí Quyết","icon":"🌀","rarity":"hiem","realm":2,"kind":"mana","mana":0.45,"cd":25,"mp":0,"drop":0.015,"stock":35000,"desc":"Hút ma khí thiên địa hồi phục 45% Linh lực trong trận."},
    {"id":"ma_hon_bien","mon":"chung","demonOnly":true,"name":"Tụ Hồn Luyện Ma Biến","icon":"🔥","rarity":"cuchiem","realm":4,"kind":"buff","buff":{"atk":0.3},"dur":10,"cd":35,"mp":40,"drop":0.003,"stock":12000,"desc":"Hấp thu tàn hồn tăng 30% lực công kích bạo phát."},
    {"id":"ma_am_phong","mon":"phap","demonOnly":true,"name":"U Minh Hắc Phong","icon":"🌪️","rarity":"hiem","realm":3,"kind":"dot","dot":1,"dur":5,"cd":14,"mp":32,"drop":0.008,"stock":20000,"desc":"Gió đen U Minh bào mòn kinh mạch gây độc liên tục."},
    {"id":"ma_huyet_don","mon":"chung","demonOnly":true,"name":"Huyết Độn Đại Pháp","icon":"🩸","rarity":"pt","realm":1,"kind":"escape","dodge":2.5,"cd":35,"mp":18,"drop":0.02,"stock":null,"desc":"Đốt cháy tinh huyết đào tẩu cực nhanh trong nguy hiểm."},
    {"id":"ma_doc_sat","mon":"dan","demonOnly":true,"name":"Ma Độc Thực Cốt","icon":"🧪","rarity":"hiem","realm":3,"kind":"dot","dot":1.2,"dur":6,"cd":15,"mp":35,"drop":0.006,"stock":18000,"desc":"Kịch độc tà tu ngấm vào xương tủy thiêu đốt sinh mạng."},
    {"id":"ma_bat_diet","mon":"the","demonOnly":true,"name":"Thiên Ma Bất Diệt Thể","icon":"🛡️","rarity":"cuchiem","realm":4,"kind":"buff","buff":{"dmgTaken":0.7},"dur":10,"cd":45,"mp":40,"drop":0.003,"stock":12000,"desc":"Nhục thân ma đạo cứng cáp giảm 30% toàn bộ sát thương gánh chịu."},
    {"id":"ma_sat_linh","mon":"quyen","demonOnly":true,"name":"Sát Khí Thấu Cốt Quyền","icon":"🥊","rarity":"hiem","realm":2,"kind":"atk","power":1.95,"critBonus":0.2,"cd":6,"mp":20,"drop":0.01,"stock":25000,"desc":"Nắm đấm cuồn cuộn sát khí thấu xương xuyên phá phòng thủ."},
    {"id":"ma_thien_quy_trao","mon":"the","demonOnly":true,"name":"Thiên Quỷ Liệt Không Trảo","icon":"🦅","rarity":"hiem","realm":3,"kind":"multi","power":0.75,"hits":3,"cd":8,"mp":26,"drop":0.007,"stock":20000,"desc":"Móng vuốt quỷ xé rách không gian cào cấu đối phương."},
    {"id":"ma_co_ma_chu","mon":"phap","demonOnly":true,"name":"Cổ Ma Chú Thuật","icon":"👁️","rarity":"cuchiem","realm":4,"kind":"stun","stun":2.2,"cd":28,"mp":38,"drop":0.0025,"stock":10000,"desc":"Lời nguyền Cổ Ma giam hãm hành động kẻ địch trong 2.2 giây."},
    {"id":"ma_nghich_huyet","mon":"chung","demonOnly":true,"name":"Nghịch Huyết Phần Ma Quyết","icon":"🔥","rarity":"tt","realm":5,"kind":"buff","buff":{"atk":0.45,"crit":0.2},"dur":8,"cd":60,"big":true,"mp":60,"drop":0.0008,"stock":5000,"desc":"Đốt cháy toàn bộ huyết mạch hóa thành cuồng ma hủy diệt."},
    {"id":"ma_vong_linh","mon":"phap","demonOnly":true,"name":"Vong Linh Thiên Quân Trận","icon":"👻","rarity":"tt","realm":6,"kind":"multi","power":0.95,"hits":5,"cd":20,"big":true,"mp":75,"drop":0.0004,"stock":3000,"desc":"Triệu hồi quân đoàn vong linh thiên binh áp đảo chiến trường."},
    {"id":"ma_hoa_linh","mon":"chung","demonOnly":true,"name":"Đoạt Linh Hóa Ma Thuật","icon":"🌀","rarity":"hiem","realm":3,"kind":"mana","mana":0.5,"cd":30,"mp":0,"drop":0.008,"stock":22000,"desc":"Cưỡng đoạt linh khí đối phương chuyển hóa thành ma năng."},
    {"id":"ma_hac_nhat","mon":"phap","demonOnly":true,"name":"Hắc Nhật Ma Quang","icon":"🌑","rarity":"cuchiem","realm":5,"kind":"atk","power":3.3,"pierce":0.35,"cd":13,"mp":48,"drop":0.0018,"stock":8000,"desc":"Tia sáng mặt trời đen tối tăm xuyên thấu vạn vật."},
    {"id":"ma_ta_long","mon":"chung","demonOnly":true,"name":"Tà Long Phệ Thiên","icon":"🐉","rarity":"tt","realm":6,"kind":"atk","power":4.2,"stun":1.5,"big":true,"cd":120,"mp":80,"drop":0.0003,"stock":2500,"desc":"Triệu hoán rồng tà viễn cổ giáng lâm cắn xé đối thủ."},
    {"id":"ma_van_quy","mon":"chung","demonOnly":true,"name":"Vạn Quỷ Triều Tông","icon":"🧟","rarity":"tt","realm":6,"kind":"multi","power":1,"hits":5,"cd":22,"big":true,"mp":85,"drop":0.0002,"stock":2000,"desc":"Muôn vàn ác quỷ gào thét triều bái, oanh kích liên hoàn."},
    {"id":"ma_huyet_ha_tran","mon":"chung","demonOnly":true,"name":"Huyết Hải Tru Tiên Trận","icon":"🩸","rarity":"tt","realm":7,"kind":"reflect","reflect":0.35,"dur":8,"cd":90,"big":true,"mp":80,"drop":0.0001,"stock":1000,"desc":"Trận pháp huyết hải phản kích 35% sát thương nhận vào."},
    {"id":"ma_chu_tuoc_ta","mon":"the","demonOnly":true,"name":"Chu Tước Tà Ma Biến","icon":"🦅","rarity":"tt","realm":6,"kind":"buff","buff":{"atk":0.45,"spd":8},"dur":12,"cd":110,"big":true,"mp":75,"drop":0.00025,"stock":2000,"desc":"Chu Tước hóa tà ma, tốc độ và công kích đạt cảnh giới tột cùng."},
    {"id":"ma_loan_tam","mon":"phap","demonOnly":true,"name":"Loạn Tâm Huyễn Ma Âm","icon":"🎶","rarity":"cuchiem","realm":4,"kind":"stun","stun":2.5,"cd":32,"mp":45,"drop":0.002,"stock":8000,"desc":"Âm thanh mê hoặc làm rối loạn tâm can khiến đối thủ bất động."},
    {"id":"ma_sinh_tu_y","mon":"chung","demonOnly":true,"name":"Sinh Tử Tịch Diệt Ma Chỉ","icon":"👉","rarity":"tt","realm":6,"kind":"atk","power":4.6,"cd":18,"big":true,"mp":85,"drop":0.0003,"stock":2000,"desc":"Một chỉ phân định sinh tử, hủy diệt sinh cơ địch nhân."},
    {"id":"ma_cuc_am","mon":"dan","demonOnly":true,"name":"Cực Âm Ma Hỏa","icon":"🔥","rarity":"cuchiem","realm":5,"kind":"dot","dot":1.5,"dur":5,"cd":16,"mp":50,"drop":0.0015,"stock":7000,"desc":"Lửa ma Cực Âm thiêu rụi cả sinh lực lẫn linh lực."},
    {"id":"ma_luc_duc","mon":"chung","demonOnly":true,"name":"Lục Dục Mê Hồn Quyết","icon":"🎭","rarity":"hiem","realm":3,"kind":"buff","buff":{"dmgTaken":0.8},"dur":10,"cd":35,"mp":30,"drop":0.007,"stock":18000,"desc":"Dùng lục dục mê hoặc làm suy giảm ý chí chiến đấu đối phương."},
    {"id":"ma_huyet_te","mon":"the","demonOnly":true,"name":"Huyết Tế Đại Pháp","icon":"🩸","rarity":"cuchiem","realm":4,"kind":"atk","power":3.2,"critBonus":0.3,"cd":9,"mp":35,"drop":0.003,"stock":11000,"desc":"Hiến tế huyết nhục bộc phát đòn đánh cuồng bạo chí mạng."},
    {"id":"ma_cot_giap","mon":"the","demonOnly":true,"name":"Bạch Cốt Huyền Giáp","icon":"🛡️","rarity":"cuchiem","realm":4,"kind":"shield","shield":0.45,"cd":30,"mp":38,"drop":0.003,"stock":12000,"desc":"Áo giáp xương trắng ngưng tụ che chở 45% HP sát thương."},
    {"id":"ma_thien_tai","mon":"chung","demonOnly":true,"name":"Thiên Ma Giáng Lâm","icon":"👹","rarity":"tt","realm":7,"kind":"buff","buff":{"atk":0.5,"crit":0.25},"dur":12,"cd":150,"big":true,"mp":90,"drop":0.0001,"stock":800,"desc":"Thiên Ma hạ giới nhập thể, chiến lực tăng tiến kinh thiên."},
    {"id":"ma_am_anh","mon":"chung","demonOnly":true,"name":"Ám Ảnh Quỷ Độn","icon":"💨","rarity":"hiem","realm":3,"kind":"escape","dodge":2.2,"cd":42,"mp":25,"drop":0.008,"stock":20000,"desc":"Biến vào bóng tối trốn thoát khỏi vòng vây hiểm nghèo."},
    {"id":"ma_huyet_ngoc","mon":"quyen","demonOnly":true,"name":"Huyết Ngọc Toái Hồn Quyền","icon":"🥊","rarity":"cuchiem","realm":5,"kind":"atk","power":3.4,"critBonus":0.35,"cd":10,"mp":45,"drop":0.0018,"stock":8000,"desc":"Quyền ý ngưng huyết ngọc đập nát linh hồn quân thù."},
    {"id":"ma_u_hon_chuyen","mon":"chung","demonOnly":true,"name":"U Hồn Chuyển Sinh Quyết","icon":"💖","rarity":"cuchiem","realm":5,"kind":"heal","heal":0.45,"cd":40,"mp":50,"drop":0.0015,"stock":7000,"desc":"Dùng oán khí u hồn tái sinh thân xác, hồi phục 45% HP."},
    {"id":"ma_hon_phi_phach_tan","mon":"phap","demonOnly":true,"name":"Hồn Phi Phách Tán Quyết","icon":"💀","rarity":"tt","realm":7,"kind":"atk","power":5,"stun":2,"big":true,"cd":180,"mp":95,"drop":0.0001,"stock":600,"desc":"Thần thông ma đạo tối thượng đánh cho địch thủ hồn bay phách tán."},
    {"id":"ma_do_ty","mon":"the","demonOnly":true,"name":"Đồ Ty Cổ Ma Thần Lực","icon":"🗿","rarity":"tt","realm":7,"kind":"buff","buff":{"maxHpMul":1.8,"atk":0.35},"dur":15,"cd":200,"big":true,"mp":100,"drop":0.0001,"stock":500,"desc":"Sức mạnh Cổ Ma Đồ Ty tái hiện trong huyết mạch."},
    {"id":"ma_hoang_tuyen_lo","mon":"chung","demonOnly":true,"name":"Hoàng Tuyền Lạc Hồn Lộ","icon":"🌌","rarity":"tt","realm":8,"kind":"multi","power":1.2,"hits":4,"stun":2,"big":true,"cd":240,"mp":110,"drop":0.00005,"stock":200,"desc":"Mở ra con đường Hoàng Tuyền kéo linh hồn đối phương vào luân hồi vĩnh cửu."},
    {"id":"ma_thai_so_ta","mon":"chung","demonOnly":true,"name":"Thái Sơ Tà Thần Ấn","icon":"🔱","rarity":"docban","realm":9,"kind":"atk","power":3.0,"stun":3,"big":true,"cd":400,"mp":130,"drop":1e-10,"stock":1,"desc":"Đại ấn Tà Thần viễn cổ phá toái thiên địa càn khôn."},
    {"id":"ma_nghich_thien_sat","mon":"chung","demonOnly":true,"name":"Nghịch Thiên Sát Lục Vực","icon":"🩸","rarity":"docban","realm":10,"kind":"buff","buff":{"crit":0.5,"atk":0.5},"dur":15,"cd":500,"big":true,"mp":150,"drop":1e-10,"stock":1,"desc":"Mở ra lĩnh vực sát lục nghịch thiên bất diệt, vạn ma triều bái."},
    {"id":"ma_thien_ma_to","mon":"chung","demonOnly":true,"name":"Vạn Cổ Thiên Ma Tổ Thể","icon":"👑","rarity":"docban","realm":12,"kind":"atk","power":12,"stun":3,"heal":0.5,"big":true,"cd":600,"mp":200,"drop":1e-10,"stock":1,"desc":"Thần thông chí cao của Ma Tu, hóa thân Thiên Ma Thủy Tổ vạn cổ bất diệt."},
    // ---- CÔNG PHÁP CHÍNH ĐẠO & THẦN THÔNG MỞ RỘNG (100+ CÔNG PHÁP) ----
    {"id":"kiem_khi_truong_thanh_kiem","mon":"kiem","name":"Kiếm Khí Vạn Dặm","icon":"⚔️","rarity":"cuchiem","realm":4,"kind":"atk","power":3.2,"critBonus":0.25,"cd":10,"mp":35,"drop":0.002,"stock":15000,"desc":"Kiếm khí ngút trời của Kiếm Khí Trường Thành chấn nhiếp đại yêu man hoang."},
    {"id":"van_thoa_phi_kiem","mon":"kiem","name":"Vạn Thoa Thần Kiếm","icon":"✨","rarity":"tt","realm":6,"kind":"multi","power":0.9,"hits":5,"cd":120,"big":true,"mp":80,"drop":0.0003,"stock":2000,"desc":"Mười ngàn phi kiếm ảo ảnh đan xen như mắc cửi giáng xuống đầu địch."},
    {"id":"ngu_long_quyet","mon":"kiem","name":"Ngự Long Kiếm Quyết","icon":"🐉","rarity":"cuchiem","realm":5,"kind":"atk","power":3.5,"pierce":0.3,"cd":13,"mp":45,"drop":0.0015,"stock":8000,"desc":"Kiếm khí hóa rồng vàng gầm thét xé tan hộ giáp đối phương."},
    {"id":"to_phu_kiem_y","mon":"kiem","name":"Tổ Phu Kiếm Ý","icon":"🗡️","rarity":"tt","realm":7,"kind":"buff","buff":{"crit":0.4,"atk":0.35},"dur":12,"cd":180,"big":true,"mp":90,"drop":0.0001,"stock":500,"desc":"Ý cảnh kiếm đạo thủy tổ sơ khai chấn cổ thước kim."},
    {"id":"truong_sinh_kiem","mon":"kiem","name":"Trường Sinh Kiếm Pháp","icon":"🌸","rarity":"hiem","realm":3,"kind":"heal","heal":0.3,"cd":25,"mp":30,"drop":0.006,"stock":25000,"desc":"Kiếm ý mộc linh lưu chuyển, sinh cơ dạt dào hồi phục thương thế."},
    {"id":"luc_chau_kiem_khi","mon":"kiem","name":"Lục Châu Kiếm Khí","icon":"🌪️","rarity":"hiem","realm":2,"kind":"atk","power":1.85,"cd":6,"mp":18,"drop":0.01,"stock":30000,"desc":"Kiếm khí lưu chuyển như sóng xanh sông Lục Châu."},
    {"id":"tram_long_thuc","mon":"quyen","name":"Trảm Long Tam Thức","icon":"🥊","rarity":"cuchiem","realm":4,"kind":"multi","power":0.85,"hits":3,"stunChance":0.3,"stun":1.5,"cd":12,"mp":35,"drop":0.002,"stock":12000,"desc":"Ba đòn quyền thế trảm long hủy thiên diệt địa của thuần túy võ phu."},
    {"id":"vo_dich_quyen_y","mon":"quyen","name":"Vô Địch Quyền Ý","icon":"👑","rarity":"tt","realm":6,"kind":"buff","buff":{"atk":0.5,"crit":0.3},"dur":10,"cd":150,"big":true,"mp":85,"drop":0.0002,"stock":1500,"desc":"Quyền ý chí tôn vô địch một cõi, không gì ngăn cản nổi."},
    {"id":"son_thuy_y_canh","mon":"chung","name":"Sơn Thủy Ý Cảnh","icon":"⛰️","rarity":"hiem","realm":3,"kind":"shield","shield":0.35,"cd":25,"mp":28,"drop":0.008,"stock":20000,"desc":"Ngưng tụ khí tượng non sông đất trời thành hộ thuẫn phòng hộ."},
    {"id":"dang_son_quyen","mon":"quyen","name":"Đăng Sơn Bát Quyền","icon":"👊","rarity":"pt","realm":1,"kind":"atk","power":1.45,"cd":5,"mp":12,"drop":0.02,"stock":null,"desc":"Bộ quyền pháp căn cơ của võ phu Trần Bình An."},
    {"id":"tich_ta_than_loi_tran","mon":"phap","name":"Tích Tà Thần Lôi Trận","icon":"⚡","rarity":"tt","realm":6,"kind":"multi","power":1.1,"hits":4,"stun":2,"cd":160,"big":true,"mp":85,"drop":0.0003,"stock":2000,"desc":"Lôi trận Kim Lôi Trúc khắc chế tuyệt đối mọi loại ma khí và u hồn."},
    {"id":"xuan_hoang_nhat_khi","mon":"the","name":"Huyền Hoàng Nhất Khí Quyết","icon":"🛡️","rarity":"hiem","realm":3,"kind":"buff","buff":{"dmgTaken":0.75},"dur":10,"cd":35,"mp":30,"drop":0.007,"stock":22000,"desc":"Huyền hoàng chi khí tôi luyện cơ thể vững chắc như bàn thạch."},
    {"id":"can_khon_ngu_hanh","mon":"phap","name":"Càn Khôn Ngũ Hành Quyết","icon":"☯️","rarity":"cuchiem","realm":4,"kind":"buff","buff":{"forceCounter":true,"atk":0.2},"dur":10,"cd":45,"mp":40,"drop":0.0025,"stock":12000,"desc":"Vận hành ngũ hành sinh khắc áp chế thuộc tính đối thủ."},
    {"id":"thanh_truc_kiem_vuc","mon":"kiem","name":"Thanh Trúc Kiếm Vực","icon":"🎋","rarity":"docban","realm":10,"kind":"multi","power":2.4,"hits":5,"stun":3,"cd":600,"big":true,"mp":150,"drop":1e-10,"stock":1,"desc":"Lĩnh vực kiếm đạo tối thượng của Hàn Lập xưng bá Linh Giới."},
    {"id":"chan_ma_kim_than","mon":"the","name":"Phạn Thánh Kim Thân","icon":"🗿","rarity":"tt","realm":7,"kind":"buff","buff":{"maxHpMul":1.6,"atk":0.4},"dur":15,"cd":200,"big":true,"mp":100,"drop":0.0001,"stock":800,"desc":"Ba đầu sáu tay Pháp tướng Kim Thân uy chấn chư thiên."},
    {"id":"minh_vuong_quyet","mon":"the","name":"Minh Vương Quyết","icon":"🥋","rarity":"cuchiem","realm":5,"kind":"shield","shield":0.55,"cd":50,"mp":50,"drop":0.0015,"stock":6000,"desc":"Công pháp thể tu ma đạo tối cao dung hợp phật môn phòng ngự."},
    {"id":"hoa_nguyen_chan_quyet","mon":"phap","name":"Hóa Nguyên Chân Quyết","icon":"🌀","rarity":"hiem","realm":2,"kind":"mana","mana":0.4,"cd":22,"mp":0,"drop":0.012,"stock":30000,"desc":"Chuyển hóa thiên địa nguyên khí hồi phục linh lực tức thời."},
    {"id":"dai_dien_phan_than","mon":"phap","name":"Đại Diễn Phân Thần Thuật","icon":"🧘","rarity":"cuchiem","realm":4,"kind":"escape","dodge":2.2,"cd":40,"mp":35,"drop":0.003,"stock":15000,"desc":"Phân tách thần thức hóa hư vô né tránh đòn công kích hiểm ác."},
    {"id":"kim_quang_chuong","mon":"the","name":"Kim Quang Diệt Ma Chưởng","icon":"🖐️","rarity":"pt","realm":2,"kind":"atk","power":1.6,"cd":6,"mp":16,"drop":0.015,"stock":null,"desc":"Chưởng pháp kim quang rực rỡ trừng trị yêu ma."},
    {"id":"ngu_loi_chinh_phap","mon":"phap","name":"Ngũ Lôi Chánh Pháp","icon":"🌩️","rarity":"cuchiem","realm":4,"kind":"atk","power":2.8,"stun":1.5,"cd":14,"mp":40,"drop":0.0025,"stock":10000,"desc":"Ngũ lôi hợp nhất oanh tạc địch thủ."},
    {"id":"co_than_quyen","mon":"the","name":"Cổ Thần Khai Thiên Quyền","icon":"🗿","rarity":"tt","realm":7,"kind":"atk","power":4.8,"stun":2,"cd":160,"big":true,"mp":90,"drop":0.0001,"stock":600,"desc":"Nắm đấm của Cổ Thần bát tinh một quyền vỡ nát tinh cầu."},
    {"id":"sinh_tu_luan_hoi","mon":"chung","name":"Sinh Tử Luân Hồi Ý Cảnh","icon":"☯️","rarity":"tt","realm":6,"kind":"atk","power":4.2,"heal":0.3,"cd":140,"big":true,"mp":85,"drop":0.0003,"stock":2500,"desc":"Nắm giữ quy tắc sinh tử của vạn vật trong Tiên Nghịch."},
    {"id":"mong_dao_quyet","mon":"chung","name":"Mộng Đạo Hóa Thần Quyết","icon":"🌌","rarity":"cuchiem","realm":5,"kind":"heal","heal":0.5,"cleanse":true,"cd":50,"mp":55,"drop":0.001,"stock":8000,"desc":"Mộng trăm năm hóa phàm ngộ đạo, tâm cảnh viên mãn xóa tan mọi thương thế."},
    {"id":"van_hoa_nhan_qua","mon":"chung","name":"Nhân Quả Chân Ý","icon":"🧶","rarity":"tt","realm":8,"kind":"reflect","reflect":0.45,"dur":10,"cd":200,"big":true,"mp":100,"drop":0.00005,"stock":300,"desc":"Sợi tơ nhân quả vô hình phản chấn 45% sát thương gánh chịu."},
    {"id":"chan_da_chi","mon":"chung","name":"Chấn Dạ Thần Chỉ","icon":"👉","rarity":"tt","realm":6,"kind":"atk","power":4.3,"stun":1.5,"cd":120,"big":true,"mp":75,"drop":0.0004,"stock":3000,"desc":"Một chỉ xé toạc màn đêm vô tận của Vương Lâm."},
    {"id":"bat_tinh_co_than","mon":"the","name":"Bát Tinh Cổ Thần Biến","icon":"👑","rarity":"docban","realm":9,"kind":"buff","buff":{"maxHpMul":2,"atk":0.5},"dur":15,"cd":450,"big":true,"mp":130,"drop":1e-10,"stock":1,"desc":"Hóa thân Cổ Thần hoàng tộc tám sao đứng đầu vũ trụ."},
    {"id":"thien_van_quy_nhat","mon":"chung","name":"Thiên Vấn Quy Nhất Chỉ","icon":"☝️","rarity":"cuchiem","realm":5,"kind":"atk","power":3.4,"pierceShield":true,"cd":15,"mp":48,"drop":0.0015,"stock":7500,"desc":"Một chỉ vấn thiên phá vỡ mọi phòng ngự hư ảo."},
    {"id":"dao_tam_thong_minh","mon":"chung","name":"Đạo Tâm Thông Minh Quyết","icon":"🧘","rarity":"pt","realm":1,"kind":"buff","buff":{"crit":0.15},"dur":10,"cd":20,"mp":12,"drop":0.015,"stock":null,"desc":"Đạo tâm kiên định không dời trước sóng gió cuộc đời."},

    // ---- THẦN THÔNG KIẾM LAI ----
    { id: 'ham_son_quyen', mon: 'quyen', name: 'Hám Sơn Quyền', icon: '👊', rarity: 'hiem', realm: 2, kind: 'atk', power: 1.8, cd: 7, mp: 16, drop: 0.008, stock: 30000, desc: 'Quyền ý Trần Bình An, nhất quyền khai sơn toái thạch.' },
    { id: 'kiem_khi_thap_bat_dinh', mon: 'kiem', name: 'Kiếm Khí Thập Bát Đình', icon: '🗡️', rarity: 'cuchiem', realm: 4, kind: 'atk', power: 3.4, critBonus: 0.3, cd: 12, mp: 38, drop: 0.002, stock: 15000, desc: 'Kiếm khí ngưng tụ mười tám nhịp thở, bộc phát sát thương kinh thiên động địa.' },
    { id: 'tho_nap_dang_son', mon: 'chung', name: 'Thổ Nạp Đăng Sơn Pháp', icon: '🏔️', rarity: 'hiem', realm: 2, kind: 'buff', buff: { atk: 0.2, def: 0.15 }, dur: 10, cd: 30, mp: 25, drop: 0.01, stock: 30000, desc: 'Pháp môn thở đăng sơn của võ phu, tăng cường gân cốt thể phách.' },
    { id: 'than_nhan_loi_co', mon: 'quyen', name: 'Thần Nhân Lôi Cổ Thức', icon: '🥁', rarity: 'cuchiem', realm: 3, kind: 'multi', power: 0.7, hits: 3, cd: 9, mp: 28, drop: 0.004, stock: 20000, desc: 'Quyền thế dồn dập tựa thần nhân gióng trống lôi đình, áp bức địch thủ.' },
    { id: 'on_duong_phi_kiem', mon: 'kiem', name: 'Ôn Dưỡng Phi Kiếm', icon: '⚔️', rarity: 'tt', realm: 5, kind: 'buff', buff: { crit: 0.35, atk: 0.25 }, dur: 12, cd: 75, big: true, mp: 55, drop: 0.0005, stock: 5000, desc: 'Nuôi dưỡng kiếm khí trong Dưỡng Kiếm Hồ, xuất kiếm tất trảm cường địch.' },

    // ---- THẦN THÔNG TIÊN NGHỊCH ----
    { id: 'tich_diet_chi', mon: 'chung', name: 'Tịch Diệt Chỉ', icon: '👉', rarity: 'hiem', realm: 3, kind: 'atk', power: 2.4, cd: 8, mp: 26, drop: 0.006, stock: 25000, desc: 'Một chỉ tịch diệt vạn vật, mang theo tử khí lạnh thấu xương tủy.' },
    { id: 'dinh_than_thuat', mon: 'phap', name: 'Định Thân Thuật Tiên Nghịch', icon: '✋', rarity: 'cuchiem', realm: 4, kind: 'stun', stun: 2.5, cd: 35, mp: 40, drop: 0.002, stock: 12000, desc: 'Cố định thời không và thân xác đối phương trong khoảnh khắc.' },
    { id: 'ho_phong_hoan_vu', mon: 'phap', name: 'Hô Phong Hoán Vũ', icon: '🌪️', rarity: 'tt', realm: 5, kind: 'multi', power: 1.1, hits: 3, cd: 16, big: true, mp: 65, drop: 0.0008, stock: 8000, desc: 'Hắc phong xé trời, linh vũ như đao, quét sạch cõi cửu châu.' },
    { id: 'tan_da', mon: 'chung', name: 'Tàn Dạ', icon: '🌅', rarity: 'tt', realm: 7, kind: 'atk', power: 5.2, stun: 2, cd: 220, big: true, mp: 95, drop: 0.0001, stock: 500, desc: 'Mặt trời xé toạc màn đêm vô tận, thần thông Thái Sơ diệt thế của Vương Lâm.' },
    { id: 'luu_nguyet', mon: 'chung', name: 'Lưu Nguyệt Thần Thông', icon: '⏳', rarity: 'cuchiem', realm: 5, kind: 'heal', heal: 0.45, cleanse: true, cd: 45, mp: 50, drop: 0.001, stock: 8000, desc: 'Nghịch chuyển thời gian, xóa bỏ thương thế và hồi phục khí huyết thần tốc.' },
    { id: 'chu_tuoc_bien', mon: 'the', name: 'Chu Tước Cửu Huyền Biến', icon: '🦅', rarity: 'tt', realm: 6, kind: 'buff', buff: { atk: 0.4, spd: 5 }, dur: 10, cd: 100, big: true, mp: 70, drop: 0.0003, stock: 3000, desc: 'Chu Tước thức tỉnh cửu biến, hóa thân thần điểu đốt cháy thiên địa.' },

    // ---- THẦN THÔNG PHÀM NHÂN TU TIÊN ----
    { id: 'thanh_nguyen_kiem_quyet', mon: 'kiem', name: 'Thanh Nguyên Kiếm Quyết', icon: '⚔️', rarity: 'hiem', realm: 3, kind: 'atk', power: 2.1, pierce: 0.2, cd: 6, mp: 20, drop: 0.008, stock: 35000, desc: 'Tuyệt học phi kiếm trấn phái của Hàn Lập, kiếm mang ngưng tụ sắc bén dị thường.' },
    { id: 'dai_canh_kiem_tran', mon: 'kiem', name: 'Đại Canh Kiếm Trận', icon: '🕸️', rarity: 'tt', realm: 6, kind: 'multi', power: 1.0, hits: 5, stun: 1.5, cd: 180, big: true, mp: 85, drop: 0.0002, stock: 2000, desc: 'Trận pháp 72 thanh Thanh Trúc Kiếm bao vây giam cầm và tiêu diệt vạn địch.' },
    { id: 'nguyen_tu_than_quang', mon: 'phap', name: 'Nguyên Từ Thần Quang', icon: '🌈', rarity: 'cuchiem', realm: 5, kind: 'atk', power: 3.6, pierceShield: true, cd: 14, mp: 50, drop: 0.0015, stock: 10000, desc: 'Thần quang khắc chế ngũ hành pháp bảo, xuyên phá mọi lớp hộ thuẫn.' },
    { id: 'tam_chuyen_trong_nguyen', mon: 'the', name: 'Tam Chuyển Trọng Nguyên Công', icon: '🔄', rarity: 'cuchiem', realm: 4, kind: 'buff', buff: { atk: 0.25, maxHpMul: 1.25 }, dur: 12, cd: 50, mp: 35, drop: 0.003, stock: 15000, desc: 'Tán công trọng tu ba lần, rèn đúc đan điền và thể phách thâm hậu vô song.' },
    { id: 'dai_ngu_hanh_cam_tien', mon: 'phap', name: 'Đại Ngũ Hành Cầm Tiên Thủ', icon: '🖐️', rarity: 'tt', realm: 7, kind: 'atk', power: 4.8, stun: 2, cd: 160, big: true, mp: 90, drop: 0.0001, stock: 800, desc: 'Bàn tay khổng lồ ngưng tụ ngũ hành nguyên lực, bắt trọn tiên nhân trần thế.' },
    { id: 'can_lam_bang_diem', mon: 'dan', name: 'Càn Lam Băng Diễm', icon: '🧊', rarity: 'cuchiem', realm: 5, kind: 'dot', dot: 1.4, dur: 4, cd: 15, mp: 45, drop: 0.001, stock: 10000, desc: 'Ngọn lửa hàn băng màu xanh ngọc từ Hư Thiên Đỉnh đóng băng linh hồn kẻ địch.' },
    // Kỹ năng bổ sung đối chiếu từ Bách khoa Kiếm Lai, Tiên Nghịch và Phàm Nhân.
    { id: 'ham_son_pho', mon: 'the', name: 'Hám Sơn Phổ', icon: '🏔️', rarity: 'hiem', realm: 2, kind: 'atk', power: 1.55, cd: 8, mp: 18, drop: 0.006, stock: 8000, desc: 'Quyền phổ rèn thân thể, mượn sơn thế áp chế đối thủ.' },
    { id: 'luc_bo_tau_thung', mon: 'the', name: 'Lục Bộ Tẩu Thung', icon: '🥋', rarity: 'pt', realm: 1, kind: 'buff', buff: { spd: 0.12 }, dur: 8, cd: 32, mp: 15, drop: 0.009, stock: 18000, desc: 'Bộ pháp căn bản của võ phu, tăng thân pháp trong giao chiến.' },
    { id: 'kiem_thuat_chinh_kinh', mon: 'kiem', name: 'Kiếm Thuật Chính Kinh', icon: '⚔️', rarity: 'hiem', realm: 2, kind: 'atk', power: 1.65, pierce: 0.1, cd: 7, mp: 20, drop: 0.006, stock: 8000, desc: 'Kiếm quyết chính tông, chú trọng đường kiếm chuẩn xác và phá giáp.' },
    { id: 'van_thuy_than', mon: 'phap', name: 'Vân Thủy Thân', icon: '🌊', rarity: 'hiem', realm: 3, kind: 'buff', buff: { spd: 0.15 }, dur: 8, cd: 36, mp: 24, drop: 0.004, stock: 5000, desc: 'Thân pháp biến chuyển như mây nước, giúp né tránh đòn đánh.' },
    { id: 'bach_ma_quyen_phap', mon: 'quyen', name: 'Bạch Ma Quyền Pháp', icon: '👊', rarity: 'hiem', realm: 3, kind: 'atk', power: 1.8, stunChance: 0.12, stun: 1, cd: 9, mp: 24, drop: 0.004, stock: 5000, desc: 'Quyền ý cương mãnh, có cơ hội làm chấn động kinh mạch địch.' },
    { id: 'kiem_quyen_hop_y', mon: 'chung', name: 'Kiếm Ý Quyền Ý Hợp Dụng', icon: '☯️', rarity: 'cuchiem', realm: 5, kind: 'buff', buff: { atk: 0.15 }, dur: 9, cd: 48, mp: 38, drop: 0.001, stock: 1200, desc: 'Dung hợp kiếm ý và quyền ý, tăng công kích trong thời gian ngắn.' },
    { id: 'loi_cuc', mon: 'phap', name: 'Lôi Cục', icon: '⚡', rarity: 'hiem', realm: 4, kind: 'atk', power: 1.75, stun: 1, cd: 12, mp: 30, drop: 0.003, stock: 3500, desc: 'Bố trí lôi cục, dẫn lôi lực đánh trúng và làm tê liệt mục tiêu.' },
    { id: 'dan_thu_chan_tich', mon: 'dan', name: 'Đan Thư Chân Tích', icon: '📖', rarity: 'hiem', realm: 3, kind: 'heal', heal: 0.18, cleanse: true, cd: 45, mp: 32, drop: 0.003, stock: 3000, desc: 'Dùng dược khí điều tức, hồi phục một phần khí huyết và thanh lọc dị trạng.' },
    { id: 'phu_luc_an_than', mon: 'phu', name: 'Ẩn Thân Phù Lục Thuật', icon: '🌫️', rarity: 'cuchiem', realm: 4, kind: 'escape', dodge: 1, cd: 75, mp: 30, drop: 0.001, stock: 1200, desc: 'Phù pháp che giấu thân hình trong một nhịp, né đòn kế tiếp.' },
    { id: 'dan_luc_thuat', mon: 'phap', name: 'Dẫn Lực Thuật', icon: '🌀', rarity: 'hiem', realm: 3, kind: 'atk', power: 1.7, pierce: 0.12, cd: 9, mp: 23, drop: 0.004, stock: 4500, desc: 'Dẫn động lực trường, kéo chân nguyên đối phương và xuyên thủ.' },
    // The generated immortal skills are individually frozen above; clone them
    // before applying the shared balancing normalization below.
    ...IMMORTAL_REALM_SKILLS.map(skill => ({ ...skill })),
]);

// Phẩm cấp trang bị: cảnh giới tối thiểu để dùng, thời gian luyện hóa (giây),
// giá bán lại và giá sửa mỗi điểm độ bền.
const TIER = Object.freeze({
    pham:  { id: 'pham',  name: 'Phàm',  rank: 0, realm: 0,  refine: 60,    sell: 200,     repair: 0.2 },
    hoang: { id: 'hoang', name: 'Hoàng', rank: 1, realm: 2,  refine: 300,   sell: 600,     repair: 1 },
    huyen: { id: 'huyen', name: 'Huyền', rank: 2, realm: 3,  refine: 1800,  sell: 1800,    repair: 4 },
    dia:   { id: 'dia',   name: 'Địa',   rank: 3, realm: 5,  refine: 7200,  sell: 6000,    repair: 20 },
    thien: { id: 'thien', name: 'Thiên', rank: 4, realm: 7,  refine: 21600, sell: 20000,   repair: 80 },
    tien:  { id: 'tien',  name: 'Tiên',  rank: 5, realm: 11, refine: 86400, sell: 60000,   repair: 300 },
});

// slot: weapon | armor | acc. wtype: loại vũ khí (sai loại với môn: 50% chỉ số).
// src: mã yêu thú rơi ra; srcMinRealm/srcMaxRealm: mọi yêu thú trong khoảng
// cảnh giới đó; elite: chỉ yêu thú thủ lĩnh. stock null = không giới hạn.

// ---- 100 PHI KIẾM (FLYING SWORDS) ----
const FLYING_SWORDS = [
    {
        "id": "phi_kiem_moc_loi_phi_kiem",
        "name": "Mộc Lôi Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "pham",
        "icon": "🗡️",
        "flySpeed": 0.15,
        "stats": {
            "atk": 15,
            "spd": 5
        },
        "drop": 0.04,
        "srcMinRealm": 0,
        "srcMaxRealm": 3,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 15% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_thanh_phong_phi_kiem",
        "name": "Thanh Phong Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "pham",
        "icon": "🗡️",
        "flySpeed": 0.16,
        "stats": {
            "atk": 17,
            "spd": 6
        },
        "drop": 0.04,
        "srcMinRealm": 0,
        "srcMaxRealm": 3,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 16% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_tat_phong_phi_kiem",
        "name": "Tật Phong Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "pham",
        "icon": "🗡️",
        "flySpeed": 0.16,
        "stats": {
            "atk": 19,
            "spd": 8
        },
        "drop": 0.04,
        "srcMinRealm": 0,
        "srcMaxRealm": 3,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 16% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_thiet_tinh_phi_kiem",
        "name": "Thiết Tinh Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "pham",
        "icon": "🗡️",
        "flySpeed": 0.17,
        "stats": {
            "atk": 20,
            "spd": 9
        },
        "drop": 0.04,
        "srcMinRealm": 0,
        "srcMaxRealm": 3,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 17% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_dong_lan_phi_kiem",
        "name": "Đồng Lân Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "pham",
        "icon": "🗡️",
        "flySpeed": 0.17,
        "stats": {
            "atk": 22,
            "spd": 11
        },
        "drop": 0.04,
        "srcMinRealm": 0,
        "srcMaxRealm": 3,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 17% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_bach_vu_phi_kiem",
        "name": "Bạch Vũ Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "pham",
        "icon": "🗡️",
        "flySpeed": 0.18,
        "stats": {
            "atk": 24,
            "spd": 12
        },
        "drop": 0.04,
        "srcMinRealm": 0,
        "srcMaxRealm": 3,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 18% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_bich_ba_kiem",
        "name": "Bích Ba Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "pham",
        "icon": "🗡️",
        "flySpeed": 0.18,
        "stats": {
            "atk": 26,
            "spd": 14
        },
        "drop": 0.04,
        "srcMinRealm": 0,
        "srcMaxRealm": 3,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 18% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_luu_van_phi_kiem",
        "name": "Lưu Vân Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "pham",
        "icon": "🗡️",
        "flySpeed": 0.19,
        "stats": {
            "atk": 28,
            "spd": 15
        },
        "drop": 0.04,
        "srcMinRealm": 0,
        "srcMaxRealm": 3,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 19% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_phi_yen_kiem",
        "name": "Phi Yến Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "pham",
        "icon": "🗡️",
        "flySpeed": 0.19,
        "stats": {
            "atk": 29,
            "spd": 16
        },
        "drop": 0.04,
        "srcMinRealm": 0,
        "srcMaxRealm": 3,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 19% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_han_phong_phi_kiem",
        "name": "Hàn Phong Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "pham",
        "icon": "🗡️",
        "flySpeed": 0.2,
        "stats": {
            "atk": 31,
            "spd": 18
        },
        "drop": 0.04,
        "srcMinRealm": 0,
        "srcMaxRealm": 3,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 20% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_xich_dong_kiem",
        "name": "Xích Đồng Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "pham",
        "icon": "🗡️",
        "flySpeed": 0.2,
        "stats": {
            "atk": 33,
            "spd": 19
        },
        "drop": 0.04,
        "srcMinRealm": 0,
        "srcMaxRealm": 3,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 20% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_lac_nhan_phi_kiem",
        "name": "Lạc Nhạn Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "pham",
        "icon": "🗡️",
        "flySpeed": 0.21,
        "stats": {
            "atk": 35,
            "spd": 21
        },
        "drop": 0.04,
        "srcMinRealm": 0,
        "srcMaxRealm": 3,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 21% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_thanh_truc_phi_kiem",
        "name": "Thanh Trúc Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "pham",
        "icon": "🗡️",
        "flySpeed": 0.21,
        "stats": {
            "atk": 36,
            "spd": 22
        },
        "drop": 0.04,
        "srcMinRealm": 0,
        "srcMaxRealm": 3,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 21% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_tung_phong_kiem",
        "name": "Tùng Phong Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "pham",
        "icon": "🗡️",
        "flySpeed": 0.22,
        "stats": {
            "atk": 38,
            "spd": 24
        },
        "drop": 0.04,
        "srcMinRealm": 0,
        "srcMaxRealm": 3,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 22% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_bach_luyen_phi_kiem",
        "name": "Bách Luyện Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "pham",
        "icon": "🗡️",
        "flySpeed": 0.22,
        "stats": {
            "atk": 40,
            "spd": 25
        },
        "drop": 0.04,
        "srcMinRealm": 0,
        "srcMaxRealm": 3,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 22% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_tu_dien_phi_kiem",
        "name": "Tử Điện Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "hoang",
        "icon": "⚔️",
        "flySpeed": 0.25,
        "stats": {
            "atk": 50,
            "spd": 5
        },
        "drop": 0.034,
        "srcMinRealm": 2,
        "srcMaxRealm": 5,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 25% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_liet_hoa_phi_kiem",
        "name": "Liệt Hỏa Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "hoang",
        "icon": "⚔️",
        "flySpeed": 0.26,
        "stats": {
            "atk": 54,
            "spd": 6
        },
        "drop": 0.034,
        "srcMinRealm": 2,
        "srcMaxRealm": 5,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 26% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_bang_phach_phi_kiem",
        "name": "Băng Phách Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "hoang",
        "icon": "⚔️",
        "flySpeed": 0.26,
        "stats": {
            "atk": 57,
            "spd": 7
        },
        "drop": 0.034,
        "srcMinRealm": 2,
        "srcMaxRealm": 5,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 26% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_huyen_thiet_phi_kiem",
        "name": "Huyền Thiết Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "hoang",
        "icon": "⚔️",
        "flySpeed": 0.27,
        "stats": {
            "atk": 61,
            "spd": 8
        },
        "drop": 0.034,
        "srcMinRealm": 2,
        "srcMaxRealm": 5,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 27% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_kim_xa_phi_kiem",
        "name": "Kim Xà Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "hoang",
        "icon": "⚔️",
        "flySpeed": 0.27,
        "stats": {
            "atk": 65,
            "spd": 9
        },
        "drop": 0.034,
        "srcMinRealm": 2,
        "srcMaxRealm": 5,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 27% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_xich_viem_phi_kiem",
        "name": "Xích Viêm Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "hoang",
        "icon": "⚔️",
        "flySpeed": 0.28,
        "stats": {
            "atk": 68,
            "spd": 10
        },
        "drop": 0.034,
        "srcMinRealm": 2,
        "srcMaxRealm": 5,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 28% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_bich_lac_kiem",
        "name": "Bích Lạc Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "hoang",
        "icon": "⚔️",
        "flySpeed": 0.28,
        "stats": {
            "atk": 72,
            "spd": 11
        },
        "drop": 0.034,
        "srcMinRealm": 2,
        "srcMaxRealm": 5,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 28% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_ngan_nguyet_phi_kiem",
        "name": "Ngân Nguyệt Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "hoang",
        "icon": "⚔️",
        "flySpeed": 0.29,
        "stats": {
            "atk": 76,
            "spd": 12
        },
        "drop": 0.034,
        "srcMinRealm": 2,
        "srcMaxRealm": 5,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 29% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_ho_phach_phi_kiem",
        "name": "Hổ Phách Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "hoang",
        "icon": "⚔️",
        "flySpeed": 0.29,
        "stats": {
            "atk": 79,
            "spd": 13
        },
        "drop": 0.034,
        "srcMinRealm": 2,
        "srcMaxRealm": 5,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 29% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_thanh_tieu_phi_kiem",
        "name": "Thanh Tiêu Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "hoang",
        "icon": "⚔️",
        "flySpeed": 0.3,
        "stats": {
            "atk": 83,
            "spd": 14
        },
        "drop": 0.034,
        "srcMinRealm": 2,
        "srcMaxRealm": 5,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 30% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_cu_khuyet_phi_kiem",
        "name": "Cự Khuyết Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "hoang",
        "icon": "⚔️",
        "flySpeed": 0.3,
        "stats": {
            "atk": 87,
            "spd": 16
        },
        "drop": 0.034,
        "srcMinRealm": 2,
        "srcMaxRealm": 5,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 30% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_thua_anh_phi_kiem",
        "name": "Thừa Ảnh Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "hoang",
        "icon": "⚔️",
        "flySpeed": 0.31,
        "stats": {
            "atk": 91,
            "spd": 17
        },
        "drop": 0.034,
        "srcMinRealm": 2,
        "srcMaxRealm": 5,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 31% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_tram_lu_phi_kiem",
        "name": "Trạm Lư Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "hoang",
        "icon": "⚔️",
        "flySpeed": 0.31,
        "stats": {
            "atk": 94,
            "spd": 18
        },
        "drop": 0.034,
        "srcMinRealm": 2,
        "srcMaxRealm": 5,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 31% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_bao_hap_phi_kiem",
        "name": "Bảo Hạp Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "hoang",
        "icon": "⚔️",
        "flySpeed": 0.32,
        "stats": {
            "atk": 98,
            "spd": 19
        },
        "drop": 0.034,
        "srcMinRealm": 2,
        "srcMaxRealm": 5,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 32% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_long_ngam_phi_kiem",
        "name": "Long Ngâm Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "hoang",
        "icon": "⚔️",
        "flySpeed": 0.32,
        "stats": {
            "atk": 102,
            "spd": 20
        },
        "drop": 0.034,
        "srcMinRealm": 2,
        "srcMaxRealm": 5,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 32% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_that_tinh_phi_kiem",
        "name": "Thất Tinh Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "hoang",
        "icon": "⚔️",
        "flySpeed": 0.33,
        "stats": {
            "atk": 105,
            "spd": 21
        },
        "drop": 0.034,
        "srcMinRealm": 2,
        "srcMaxRealm": 5,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 33% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_pha_khong_phi_kiem",
        "name": "Phá Không Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "hoang",
        "icon": "⚔️",
        "flySpeed": 0.33,
        "stats": {
            "atk": 109,
            "spd": 22
        },
        "drop": 0.034,
        "srcMinRealm": 2,
        "srcMaxRealm": 5,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 33% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_lanh_nguyet_phi_kiem",
        "name": "Lãnh Nguyệt Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "hoang",
        "icon": "⚔️",
        "flySpeed": 0.34,
        "stats": {
            "atk": 113,
            "spd": 23
        },
        "drop": 0.034,
        "srcMinRealm": 2,
        "srcMaxRealm": 5,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 34% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_thu_thuy_kiem",
        "name": "Thu Thủy Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "hoang",
        "icon": "⚔️",
        "flySpeed": 0.34,
        "stats": {
            "atk": 116,
            "spd": 24
        },
        "drop": 0.034,
        "srcMinRealm": 2,
        "srcMaxRealm": 5,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 34% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_tu_tieu_kiem",
        "name": "Tử Tiêu Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "hoang",
        "icon": "⚔️",
        "flySpeed": 0.35,
        "stats": {
            "atk": 120,
            "spd": 25
        },
        "drop": 0.034,
        "srcMinRealm": 2,
        "srcMaxRealm": 5,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 35% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_u_minh_phi_kiem",
        "name": "U Minh Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "huyen",
        "icon": "✨",
        "flySpeed": 0.38,
        "stats": {
            "atk": 150,
            "spd": 5
        },
        "drop": 0.028,
        "srcMinRealm": 4,
        "srcMaxRealm": 7,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 38% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_xich_huyet_phi_kiem",
        "name": "Xích Huyết Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "huyen",
        "icon": "✨",
        "flySpeed": 0.39,
        "stats": {
            "atk": 159,
            "spd": 6
        },
        "drop": 0.028,
        "srcMinRealm": 4,
        "srcMaxRealm": 7,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 39% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_cuu_loi_phi_kiem",
        "name": "Cửu Lôi Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "huyen",
        "icon": "✨",
        "flySpeed": 0.39,
        "stats": {
            "atk": 168,
            "spd": 7
        },
        "drop": 0.028,
        "srcMinRealm": 4,
        "srcMaxRealm": 7,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 39% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_thien_cang_phi_kiem",
        "name": "Thiên Cang Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "huyen",
        "icon": "✨",
        "flySpeed": 0.4,
        "stats": {
            "atk": 177,
            "spd": 8
        },
        "drop": 0.028,
        "srcMinRealm": 4,
        "srcMaxRealm": 7,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 40% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_bach_chien_phi_kiem",
        "name": "Bách Chiến Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "huyen",
        "icon": "✨",
        "flySpeed": 0.41,
        "stats": {
            "atk": 186,
            "spd": 9
        },
        "drop": 0.028,
        "srcMinRealm": 4,
        "srcMaxRealm": 7,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 41% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_chan_vu_phi_kiem",
        "name": "Chân Vũ Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "huyen",
        "icon": "✨",
        "flySpeed": 0.41,
        "stats": {
            "atk": 195,
            "spd": 10
        },
        "drop": 0.028,
        "srcMinRealm": 4,
        "srcMaxRealm": 7,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 41% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_thai_cuc_phi_kiem",
        "name": "Thái Cực Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "huyen",
        "icon": "✨",
        "flySpeed": 0.42,
        "stats": {
            "atk": 204,
            "spd": 11
        },
        "drop": 0.028,
        "srcMinRealm": 4,
        "srcMaxRealm": 7,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 42% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_huyen_bang_phi_kiem",
        "name": "Huyền Băng Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "huyen",
        "icon": "✨",
        "flySpeed": 0.42,
        "stats": {
            "atk": 213,
            "spd": 12
        },
        "drop": 0.028,
        "srcMinRealm": 4,
        "srcMaxRealm": 7,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 42% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_phong_than_phi_kiem",
        "name": "Phong Thần Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "huyen",
        "icon": "✨",
        "flySpeed": 0.43,
        "stats": {
            "atk": 222,
            "spd": 13
        },
        "drop": 0.028,
        "srcMinRealm": 4,
        "srcMaxRealm": 7,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 43% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_loi_quang_phi_kiem",
        "name": "Lôi Quang Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "huyen",
        "icon": "✨",
        "flySpeed": 0.44,
        "stats": {
            "atk": 231,
            "spd": 14
        },
        "drop": 0.028,
        "srcMinRealm": 4,
        "srcMaxRealm": 7,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 44% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_bang_tinh_phi_kiem",
        "name": "Băng Tinh Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "huyen",
        "icon": "✨",
        "flySpeed": 0.44,
        "stats": {
            "atk": 239,
            "spd": 16
        },
        "drop": 0.028,
        "srcMinRealm": 4,
        "srcMaxRealm": 7,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 44% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_viem_ma_phi_kiem",
        "name": "Viêm Ma Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "huyen",
        "icon": "✨",
        "flySpeed": 0.45,
        "stats": {
            "atk": 248,
            "spd": 17
        },
        "drop": 0.028,
        "srcMinRealm": 4,
        "srcMaxRealm": 7,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 45% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_luc_ma_phi_kiem",
        "name": "Lục Ma Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "huyen",
        "icon": "✨",
        "flySpeed": 0.46,
        "stats": {
            "atk": 257,
            "spd": 18
        },
        "drop": 0.028,
        "srcMinRealm": 4,
        "srcMaxRealm": 7,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 46% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_minh_ha_phi_kiem",
        "name": "Minh Hà Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "huyen",
        "icon": "✨",
        "flySpeed": 0.46,
        "stats": {
            "atk": 266,
            "spd": 19
        },
        "drop": 0.028,
        "srcMinRealm": 4,
        "srcMaxRealm": 7,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 46% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_hoang_tuyen_kiem",
        "name": "Hoàng Tuyền Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "huyen",
        "icon": "✨",
        "flySpeed": 0.47,
        "stats": {
            "atk": 275,
            "spd": 20
        },
        "drop": 0.028,
        "srcMinRealm": 4,
        "srcMaxRealm": 7,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 47% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_doat_hon_phi_kiem",
        "name": "Đoạt Hồn Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "huyen",
        "icon": "✨",
        "flySpeed": 0.47,
        "stats": {
            "atk": 284,
            "spd": 21
        },
        "drop": 0.028,
        "srcMinRealm": 4,
        "srcMaxRealm": 7,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 47% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_tu_vi_phi_kiem",
        "name": "Tử Vi Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "huyen",
        "icon": "✨",
        "flySpeed": 0.48,
        "stats": {
            "atk": 293,
            "spd": 22
        },
        "drop": 0.028,
        "srcMinRealm": 4,
        "srcMaxRealm": 7,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 48% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_huyen_thien_phi_kiem",
        "name": "Huyền Thiên Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "huyen",
        "icon": "✨",
        "flySpeed": 0.49,
        "stats": {
            "atk": 302,
            "spd": 23
        },
        "drop": 0.028,
        "srcMinRealm": 4,
        "srcMaxRealm": 7,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 49% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_van_lan_phi_kiem",
        "name": "Vạn Lân Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "huyen",
        "icon": "✨",
        "flySpeed": 0.49,
        "stats": {
            "atk": 311,
            "spd": 24
        },
        "drop": 0.028,
        "srcMinRealm": 4,
        "srcMaxRealm": 7,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 49% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_thanh_loan_phi_kiem",
        "name": "Thanh Loan Phi Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "huyen",
        "icon": "✨",
        "flySpeed": 0.5,
        "stats": {
            "atk": 320,
            "spd": 25
        },
        "drop": 0.028,
        "srcMinRealm": 4,
        "srcMaxRealm": 7,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 50% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_cuu_u_than_kiem",
        "name": "Cửu U Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "dia",
        "icon": "⚡",
        "flySpeed": 0.52,
        "stats": {
            "atk": 400,
            "spd": 5
        },
        "drop": 0.022,
        "srcMinRealm": 6,
        "srcMaxRealm": 9,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 52% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_chu_tuoc_than_kiem",
        "name": "Chu Tước Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "dia",
        "icon": "⚡",
        "flySpeed": 0.53,
        "stats": {
            "atk": 421,
            "spd": 6
        },
        "drop": 0.022,
        "srcMinRealm": 6,
        "srcMaxRealm": 9,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 53% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_bach_ho_than_kiem",
        "name": "Bạch Hổ Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "dia",
        "icon": "⚡",
        "flySpeed": 0.53,
        "stats": {
            "atk": 442,
            "spd": 7
        },
        "drop": 0.022,
        "srcMinRealm": 6,
        "srcMaxRealm": 9,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 53% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_huyen_vu_than_kiem",
        "name": "Huyền Vũ Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "dia",
        "icon": "⚡",
        "flySpeed": 0.54,
        "stats": {
            "atk": 463,
            "spd": 8
        },
        "drop": 0.022,
        "srcMinRealm": 6,
        "srcMaxRealm": 9,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 54% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_thanh_long_than_kiem",
        "name": "Thanh Long Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "dia",
        "icon": "⚡",
        "flySpeed": 0.55,
        "stats": {
            "atk": 484,
            "spd": 9
        },
        "drop": 0.022,
        "srcMinRealm": 6,
        "srcMaxRealm": 9,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 55% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_pha_quan_than_kiem",
        "name": "Phá Quân Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "dia",
        "icon": "⚡",
        "flySpeed": 0.55,
        "stats": {
            "atk": 505,
            "spd": 10
        },
        "drop": 0.022,
        "srcMinRealm": 6,
        "srcMaxRealm": 9,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 55% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_that_sat_than_kiem",
        "name": "Thất Sát Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "dia",
        "icon": "⚡",
        "flySpeed": 0.56,
        "stats": {
            "atk": 526,
            "spd": 11
        },
        "drop": 0.022,
        "srcMinRealm": 6,
        "srcMaxRealm": 9,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 56% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_tham_lang_than_kiem",
        "name": "Tham Lang Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "dia",
        "icon": "⚡",
        "flySpeed": 0.57,
        "stats": {
            "atk": 547,
            "spd": 12
        },
        "drop": 0.022,
        "srcMinRealm": 6,
        "srcMaxRealm": 9,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 57% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_bac_dau_than_kiem",
        "name": "Bắc Đẩu Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "dia",
        "icon": "⚡",
        "flySpeed": 0.57,
        "stats": {
            "atk": 568,
            "spd": 13
        },
        "drop": 0.022,
        "srcMinRealm": 6,
        "srcMaxRealm": 9,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 57% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_nam_minh_ly_hoa_kiem",
        "name": "Nam Minh Ly Hỏa Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "dia",
        "icon": "⚡",
        "flySpeed": 0.58,
        "stats": {
            "atk": 589,
            "spd": 14
        },
        "drop": 0.022,
        "srcMinRealm": 6,
        "srcMaxRealm": 9,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 58% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_hon_nguyen_than_kiem",
        "name": "Hỗn Nguyên Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "dia",
        "icon": "⚡",
        "flySpeed": 0.59,
        "stats": {
            "atk": 611,
            "spd": 16
        },
        "drop": 0.022,
        "srcMinRealm": 6,
        "srcMaxRealm": 9,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 59% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_loi_dinh_van_quan_kiem",
        "name": "Lôi Đình Vạn Quân Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "dia",
        "icon": "⚡",
        "flySpeed": 0.6,
        "stats": {
            "atk": 632,
            "spd": 17
        },
        "drop": 0.022,
        "srcMinRealm": 6,
        "srcMaxRealm": 9,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 60% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_thien_ma_cuc_lac_kiem",
        "name": "Thiên Ma Cực Lạc Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "dia",
        "icon": "⚡",
        "flySpeed": 0.6,
        "stats": {
            "atk": 653,
            "spd": 18
        },
        "drop": 0.022,
        "srcMinRealm": 6,
        "srcMaxRealm": 9,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 60% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_thai_duong_chan_hoa_kiem",
        "name": "Thái Dương Chân Hỏa Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "dia",
        "icon": "⚡",
        "flySpeed": 0.61,
        "stats": {
            "atk": 674,
            "spd": 19
        },
        "drop": 0.022,
        "srcMinRealm": 6,
        "srcMaxRealm": 9,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 61% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_thai_am_huyen_bang_kiem",
        "name": "Thái Âm Huyền Băng Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "dia",
        "icon": "⚡",
        "flySpeed": 0.62,
        "stats": {
            "atk": 695,
            "spd": 20
        },
        "drop": 0.022,
        "srcMinRealm": 6,
        "srcMaxRealm": 9,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 62% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_bat_hoang_than_kiem",
        "name": "Bát Hoang Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "dia",
        "icon": "⚡",
        "flySpeed": 0.62,
        "stats": {
            "atk": 716,
            "spd": 21
        },
        "drop": 0.022,
        "srcMinRealm": 6,
        "srcMaxRealm": 9,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 62% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_quy_nguyen_than_kiem",
        "name": "Quy Nguyên Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "dia",
        "icon": "⚡",
        "flySpeed": 0.63,
        "stats": {
            "atk": 737,
            "spd": 22
        },
        "drop": 0.022,
        "srcMinRealm": 6,
        "srcMaxRealm": 9,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 63% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_thien_co_than_kiem",
        "name": "Thiên Cơ Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "dia",
        "icon": "⚡",
        "flySpeed": 0.64,
        "stats": {
            "atk": 758,
            "spd": 23
        },
        "drop": 0.022,
        "srcMinRealm": 6,
        "srcMaxRealm": 9,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 64% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_tuyet_tien_than_kiem",
        "name": "Tuyệt Tiên Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "dia",
        "icon": "⚡",
        "flySpeed": 0.64,
        "stats": {
            "atk": 779,
            "spd": 24
        },
        "drop": 0.022,
        "srcMinRealm": 6,
        "srcMaxRealm": 9,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 64% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_ham_tien_than_kiem",
        "name": "Hãm Tiên Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "dia",
        "icon": "⚡",
        "flySpeed": 0.65,
        "stats": {
            "atk": 800,
            "spd": 25
        },
        "drop": 0.022,
        "srcMinRealm": 6,
        "srcMaxRealm": 9,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 65% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_luc_tien_than_kiem",
        "name": "Lục Tiên Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "thien",
        "icon": "🌟",
        "flySpeed": 0.68,
        "stats": {
            "atk": 1000,
            "spd": 5
        },
        "drop": 0.016,
        "srcMinRealm": 8,
        "srcMaxRealm": 11,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 68% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_tru_tien_than_kiem",
        "name": "Tru Tiên Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "thien",
        "icon": "🌟",
        "flySpeed": 0.69,
        "stats": {
            "atk": 1086,
            "spd": 6
        },
        "drop": 0.016,
        "srcMinRealm": 8,
        "srcMaxRealm": 11,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 69% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_cuu_cuc_than_kiem",
        "name": "Cửu Cực Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "thien",
        "icon": "🌟",
        "flySpeed": 0.7,
        "stats": {
            "atk": 1171,
            "spd": 8
        },
        "drop": 0.016,
        "srcMinRealm": 8,
        "srcMaxRealm": 11,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 70% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_thien_menh_than_kiem",
        "name": "Thiên Mệnh Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "thien",
        "icon": "🌟",
        "flySpeed": 0.71,
        "stats": {
            "atk": 1257,
            "spd": 9
        },
        "drop": 0.016,
        "srcMinRealm": 8,
        "srcMaxRealm": 11,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 71% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_vo_thuong_than_kiem",
        "name": "Vô Thượng Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "thien",
        "icon": "🌟",
        "flySpeed": 0.71,
        "stats": {
            "atk": 1343,
            "spd": 11
        },
        "drop": 0.016,
        "srcMinRealm": 8,
        "srcMaxRealm": 11,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 71% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_can_khon_than_kiem",
        "name": "Càn Khôn Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "thien",
        "icon": "🌟",
        "flySpeed": 0.72,
        "stats": {
            "atk": 1429,
            "spd": 12
        },
        "drop": 0.016,
        "srcMinRealm": 8,
        "srcMaxRealm": 11,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 72% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_am_duong_hon_thien_kiem",
        "name": "Âm Dương Hỗn Thiên Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "thien",
        "icon": "🌟",
        "flySpeed": 0.73,
        "stats": {
            "atk": 1514,
            "spd": 14
        },
        "drop": 0.016,
        "srcMinRealm": 8,
        "srcMaxRealm": 11,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 73% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_van_kiem_quy_tong_kiem",
        "name": "Vạn Kiếm Quy Tông Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "thien",
        "icon": "🌟",
        "flySpeed": 0.74,
        "stats": {
            "atk": 1600,
            "spd": 15
        },
        "drop": 0.016,
        "srcMinRealm": 8,
        "srcMaxRealm": 11,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 74% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_thai_hu_than_kiem",
        "name": "Thái Hư Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "thien",
        "icon": "🌟",
        "flySpeed": 0.75,
        "stats": {
            "atk": 1686,
            "spd": 16
        },
        "drop": 0.016,
        "srcMinRealm": 8,
        "srcMaxRealm": 11,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 75% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_tram_ma_dao_kiem",
        "name": "Trảm Ma Đạo Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "thien",
        "icon": "🌟",
        "flySpeed": 0.76,
        "stats": {
            "atk": 1771,
            "spd": 18
        },
        "drop": 0.016,
        "srcMinRealm": 8,
        "srcMaxRealm": 11,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 76% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_hu_khong_pha_diet_kiem",
        "name": "Hư Không Phá Diệt Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "thien",
        "icon": "🌟",
        "flySpeed": 0.77,
        "stats": {
            "atk": 1857,
            "spd": 19
        },
        "drop": 0.016,
        "srcMinRealm": 8,
        "srcMaxRealm": 11,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 77% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_cuc_quang_hoa_than_kiem",
        "name": "Cực Quang Hóa Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "thien",
        "icon": "🌟",
        "flySpeed": 0.77,
        "stats": {
            "atk": 1943,
            "spd": 21
        },
        "drop": 0.016,
        "srcMinRealm": 8,
        "srcMaxRealm": 11,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 77% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_bat_diet_tien_kiem",
        "name": "Bất Diệt Tiên Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "thien",
        "icon": "🌟",
        "flySpeed": 0.78,
        "stats": {
            "atk": 2029,
            "spd": 22
        },
        "drop": 0.016,
        "srcMinRealm": 8,
        "srcMaxRealm": 11,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 78% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_hong_mong_than_kiem",
        "name": "Hồng Mông Thần Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "thien",
        "icon": "🌟",
        "flySpeed": 0.79,
        "stats": {
            "atk": 2114,
            "spd": 24
        },
        "drop": 0.016,
        "srcMinRealm": 8,
        "srcMaxRealm": 11,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 79% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_tao_hoa_tien_kiem",
        "name": "Tạo Hóa Tiên Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "thien",
        "icon": "🌟",
        "flySpeed": 0.8,
        "stats": {
            "atk": 2200,
            "spd": 25
        },
        "drop": 0.016,
        "srcMinRealm": 8,
        "srcMaxRealm": 11,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 80% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_thuong_co_hien_vien_kiem",
        "name": "Thượng Cổ Hiên Viên Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "tien",
        "icon": "💫",
        "flySpeed": 0.82,
        "stats": {
            "atk": 2800,
            "spd": 5
        },
        "drop": 0.007,
        "srcMinRealm": 11,
        "srcMaxRealm": 14,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 82% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_thai_so_hon_don_kiem",
        "name": "Thái Sơ Hỗn Độn Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "tien",
        "icon": "💫",
        "flySpeed": 0.83,
        "stats": {
            "atk": 3156,
            "spd": 7
        },
        "drop": 0.007,
        "srcMinRealm": 11,
        "srcMaxRealm": 14,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 83% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_cuu_thien_huyen_nu_kiem",
        "name": "Cửu Thiên Huyền Nữ Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "tien",
        "icon": "💫",
        "flySpeed": 0.84,
        "stats": {
            "atk": 3511,
            "spd": 9
        },
        "drop": 0.007,
        "srcMinRealm": 11,
        "srcMaxRealm": 14,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 84% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_ban_co_khai_thien_kiem",
        "name": "Bàn Cổ Khai Thiên Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "tien",
        "icon": "💫",
        "flySpeed": 0.85,
        "stats": {
            "atk": 3867,
            "spd": 12
        },
        "drop": 0.007,
        "srcMinRealm": 11,
        "srcMaxRealm": 14,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 85% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_van_dao_tien_de_kiem",
        "name": "Vạn Đạo Tiên Đế Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "tien",
        "icon": "💫",
        "flySpeed": 0.86,
        "stats": {
            "atk": 4222,
            "spd": 14
        },
        "drop": 0.007,
        "srcMinRealm": 11,
        "srcMaxRealm": 14,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 86% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_cuu_tieu_chi_ton_kiem",
        "name": "Cửu Tiêu Chí Tôn Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "tien",
        "icon": "💫",
        "flySpeed": 0.86,
        "stats": {
            "atk": 4578,
            "spd": 16
        },
        "drop": 0.007,
        "srcMinRealm": 11,
        "srcMaxRealm": 14,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 86% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_thien_dao_vinh_hang_kiem",
        "name": "Thiên Đạo Vĩnh Hằng Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "tien",
        "icon": "💫",
        "flySpeed": 0.87,
        "stats": {
            "atk": 4933,
            "spd": 18
        },
        "drop": 0.007,
        "srcMinRealm": 11,
        "srcMaxRealm": 14,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 87% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_than_ma_tuyet_diet_kiem",
        "name": "Thần Ma Tuyệt Diệt Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "tien",
        "icon": "💫",
        "flySpeed": 0.88,
        "stats": {
            "atk": 5289,
            "spd": 21
        },
        "drop": 0.007,
        "srcMinRealm": 11,
        "srcMaxRealm": 14,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 88% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_chan_tien_hang_the_kiem",
        "name": "Chân Tiên Hàng Thế Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "tien",
        "icon": "💫",
        "flySpeed": 0.89,
        "stats": {
            "atk": 5644,
            "spd": 23
        },
        "drop": 0.007,
        "srcMinRealm": 11,
        "srcMaxRealm": 14,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 89% thời gian phi hành liên thành."
    },
    {
        "id": "phi_kiem_vo_cuc_tien_kiem",
        "name": "Vô Cực Tiên Kiếm",
        "slot": "phi_kiem",
        "wtype": "kiem",
        "tier": "tien",
        "icon": "💫",
        "flySpeed": 0.9,
        "stats": {
            "atk": 6000,
            "spd": 25
        },
        "drop": 0.007,
        "srcMinRealm": 11,
        "srcMaxRealm": 14,
        "desc": "Phi kiếm ngự không, gia tăng tốc độ, rút ngắn 90% thời gian phi hành liên thành."
    }
];

// Các bộ vật phẩm mở rộng cho nghề nghiệp. Dùng danh mục sinh theo bậc để dễ cân bằng,
// đồng thời giữ ID ổn định cho kho đồ và dữ liệu người chơi.
const TIER_LADDER = ['pham', 'hoang', 'huyen', 'dia', 'thien', 'tien'];
const TIER_VN = ['Phàm', 'Hoàng', 'Huyền', 'Địa', 'Thiên', 'Tiên'];
const TIER_REALM = [0, 2, 4, 7, 11, 16];
const ELEMENTS = ['moc', 'hoa', 'tho', 'kim', 'thuy', 'phong', 'loi'];
const TAMER_RELICS = ['Hồn Thú', 'Ngự Linh', 'Bách Thú', 'Vạn Linh', 'Sơn Hải', 'Thái Cổ', 'Thiên Khế', 'Cửu U', 'Long Tâm', 'Hỗn Nguyên'];
const ALCHEMY_VESSELS = ['Thanh Mộc', 'Xích Diễm', 'Băng Ngọc', 'Tử Kim', 'Huyền Hoàng', 'Cửu Long', 'Vạn Dược', 'Thái Ất', 'Thái Sơ', 'Hỗn Độn'];
const FORGE_RELICS = ['Khai Sơn', 'Bách Luyện', 'Địa Hỏa', 'Thiên Công', 'Xích Lôi', 'Vạn Quân', 'Trấn Nhạc', 'Phá Giới', 'Chú Thần', 'Khai Thiên'];
const ROLE_GEAR = Array.from({ length: 50 }, (_, i) => {
    const tierIndex = Math.floor(i / 9);
    const tier = TIER_LADDER[Math.min(5, tierIndex)];
    const realm = TIER_REALM[Math.min(5, tierIndex)];
    const n = i + 1;
    const slots = ['weapon', 'armor', 'acc', 'acc', 'armor'];
    const slot = slots[i % slots.length];
    const base = Math.max(8, Math.round(12 * Math.pow(1.72, tierIndex) * (1 + (i % 9) * 0.065)));
    const label = TAMER_RELICS[i % TAMER_RELICS.length];
    const names = slot === 'weapon' ? ['Thú Hồn Trượng', 'Ngự Thú Tiên', 'Bách Linh Cốt Tiên', 'Vạn Thú Linh Tiên', 'Sơn Hải Thú Kích']
        : slot === 'armor' ? ['Linh Thú Bào', 'Ngự Linh Giáp', 'Bách Thú Chiến Y', 'Vạn Linh Hộ Giáp', 'Sơn Hải Thú Khải']
            : ['Thú Tâm Linh Bội', 'Ngự Thú Hồn Hoàn', 'Bách Thú Linh Châu', 'Vạn Linh Khế Ấn', 'Sơn Hải Thú Phù'];
    const name = `${TIER_VN[Math.min(5, tierIndex)]} ${label} ${names[i % names.length]} ${n}`;
    const stats = slot === 'weapon' ? { atk: base, sense: Math.ceil(base * 0.16) }
        : slot === 'armor' ? { def: Math.ceil(base * 0.58), hp: base * 5 }
            : { hp: base * 3, sense: Math.ceil(base * 0.24), spd: tierIndex >= 3 ? 1 + tierIndex : 0 };
    return { id: `ngu_thu_phap_bao_${String(n).padStart(2, '0')}`, name, slot, wtype: slot === 'weapon' ? 'phapkhi' : undefined,
        tier, element: ELEMENTS[i % ELEMENTS.length], icon: slot === 'weapon' ? '🪄' : slot === 'armor' ? '🛡️' : '🔮', stats,
        drop: 0, srcMinRealm: realm, srcMaxRealm: realm + 2, stock: null,
        desc: `Pháp bảo chuyên dụng cho Ngự Thú Sư bậc ${TIER_VN[Math.min(5, tierIndex)]}, cộng hưởng với linh thú.` };
});
const ALCHEMY_CAULDRONS = Array.from({ length: 50 }, (_, i) => {
    const tierIndex = Math.floor(i / 9);
    const tier = TIER_LADDER[Math.min(5, tierIndex)];
    const realm = TIER_REALM[Math.min(5, tierIndex)];
    const n = i + 1;
    const tierBaseRate = [0.64, 0.70, 0.76, 0.82, 0.87, 0.91][tierIndex];
    const rate = Math.min(0.94, tierBaseRate + (i % 9) * 0.003);
    const defense = Math.round(8 * Math.pow(1.8, tierIndex) * (1 + (i % 9) * 0.055));
    const name = `${TIER_VN[Math.min(5, tierIndex)]} ${ALCHEMY_VESSELS[i % ALCHEMY_VESSELS.length]} Đan Lô ${String(n).padStart(2, '0')}`;
    return { id: `lo_dinh_dan_su_${String(n).padStart(2, '0')}`, name, slot: 'lo_dinh', tier,
        icon: i % 2 ? '⚱️' : '🏺', stats: { def: defense, sense: Math.ceil(defense * 0.3) }, alchemyRate: rate,
        desc: `Lô đỉnh Đan Sư bậc ${TIER_VN[Math.min(5, tierIndex)]}; năng lực nền ${Math.round(rate * 100)}%. Đan phẩm cao và chênh bậc với lô làm giảm tỷ lệ thực tế.`,
        drop: 0, srcMinRealm: realm, srcMaxRealm: realm + 2, stock: null };
});
const FORGE_GEAR = Array.from({ length: 50 }, (_, i) => {
    const tierIndex = Math.floor(i / 9);
    const tier = TIER_LADDER[Math.min(5, tierIndex)];
    const realm = TIER_REALM[Math.min(5, tierIndex)];
    const n = i + 1;
    const slots = ['weapon', 'weapon', 'armor', 'acc', 'weapon'];
    const slot = slots[i % slots.length];
    const base = Math.max(10, Math.round(14 * Math.pow(1.78, tierIndex) * (1 + (i % 9) * 0.07)));
    const name = `${TIER_VN[Math.min(5, tierIndex)]} ${FORGE_RELICS[i % FORGE_RELICS.length]} ${slot === 'weapon' ? 'Rèn Chùy' : slot === 'armor' ? 'Luyện Khí Giáp' : 'Luyện Khí Bội'} ${String(n).padStart(2, '0')}`;
    const stats = slot === 'weapon' ? { atk: base, hp: Math.ceil(base * 1.3) }
        : slot === 'armor' ? { def: Math.ceil(base * 0.7), hp: base * 6 }
            : { atk: Math.ceil(base * 0.35), def: Math.ceil(base * 0.22), hp: base * 2 };
    return { id: `luyen_khi_phap_bao_${String(n).padStart(2, '0')}`, name, slot,
        wtype: slot === 'weapon' ? 'bua' : undefined, tier, element: ELEMENTS[(i + 3) % ELEMENTS.length],
        icon: slot === 'weapon' ? '🔨' : slot === 'armor' ? '🦺' : '💠', stats, drop: 0,
        srcMinRealm: realm, srcMaxRealm: realm + 2, stock: null,
        desc: `Pháp khí do Luyện Khí Sư bậc ${TIER_VN[Math.min(5, tierIndex)]} tự tay chế tạo.` };
});
const WORLD_MOUNTS = Array.from({ length: 100 }, (_, i) => {
    const tierIndex = Math.floor(i / 17);
    const tier = TIER_LADDER[Math.min(5, tierIndex)];
    const realm = Math.floor(i / 5);
    const names = ['Thanh Vân Linh Hạc', 'Xích Diễm Chiến Hổ', 'Băng Nguyên Tuyết Lang', 'Tử Điện Lôi Ưng', 'Huyền Giáp Linh Quy', 'Kim Đồng Ngọc Sư', 'Cửu U Minh Báo', 'Thương Hải Giao Long', 'Ngũ Sắc Loan Phượng', 'Thái Cổ Kỳ Lân'];
    const beast = names[i % names.length];
    const n = String(i + 1).padStart(3, '0');
    const power = Math.round(3 + Math.pow(realm + 1, 1.42) * 1.6);
    return { id: `toa_ky_thien_dao_${n}`, name: `${beast} ${TIER_VN[Math.min(5, tierIndex)]} ${n}`, slot: 'phi_kiem', tier,
        icon: ['🕊️', '🐅', '🐺', '🦅', '🐢', '🦁', '🐆', '🐉', '🦚', '🦄'][i % 10],
        stats: { spd: Math.min(38, 3 + Math.floor(realm * 1.15)), hp: power * 32, def: power * 3 },
        flySpeed: Math.min(0.48, 0.14 + realm * 0.012), mount: true, bossOnly: true,
        drop: 0.0015, elite: tierIndex >= 3, srcMinRealm: realm, srcMaxRealm: Math.min(20, realm + 1), stock: 1 };
});

// Trang bị Tiên phẩm cho đoạn cuối map 9 và toàn bộ bản đồ hậu Tiên Đế.
// Nguồn rơi được giới hạn theo đúng dải cảnh giới để loot không dừng ở cấp 20.
const IMMORTAL_GEAR_BANDS = [
    ...Array.from({ length: 10 }, (_, index) => ({
        id: `tien_canh_${index + 11}`, min: index + 11, max: index + 11, name: PRE_TIEN_DE_REALMS[index + 11],
    })),
    { id: 'map_9', min: 21, max: 30, name: 'Tiên Đế' },
    ...POST_TIEN_DE_MAP_CONFIGS.map(map => ({ id: map.id, min: map.realmMin, max: map.realmMax, name: map.name })),
];
const IMMORTAL_GEAR_WEAPONS = [
    ['kiem', 'Đế Kiếm', '⚔️', 'kim'],
    ['phapkhi', 'Tiên Phiên', '🚩', 'phong'],
    ['trongkhi', 'Thần Chùy', '🔨', 'tho'],
    ['quyensao', 'Đạo Quyền', '👊', 'loi'],
    ['dinh', 'Tiên Đỉnh', '⚱️', 'moc'],
    ['bua', 'Luyện Khí Thần Chùy', '⚒️', 'hoa'],
    ['but', 'Phù Đạo Tiên Bút', '🖋️', 'thien'],
];
const IMMORTAL_MAP_GEAR = IMMORTAL_GEAR_BANDS.flatMap(band => {
    const power = Math.round(2400 * Math.pow(REALM_GROWTH, band.min - 11));
    const common = { tier: 'tien', realmMin: band.min, srcMinRealm: band.min, srcMaxRealm: band.max, drop: 0.001, elite: true, stock: null };
    const weapons = IMMORTAL_GEAR_WEAPONS.map(([wtype, label, icon, element], index) => {
        const atk = Math.round(power * (1 + index * 0.035));
        return {
            ...common,
            id: `tien_${band.id}_vu_khi_${wtype}`,
            name: `${band.name} ${label}`,
            slot: 'weapon', wtype, element, icon,
            stats: { atk, hp: Math.round(atk * 1.5), ...(wtype === 'phapkhi' || wtype === 'but' ? { mp: Math.round(atk * 2.5), sense: Math.round(atk * 0.15) } : {}) },
            desc: `Pháp bảo Tiên phẩm dành cho cảnh giới ${band.min}–${band.max}; rơi từ tinh anh, đại yêu hoặc boss trong ${band.name}.`,
        };
    });
    return [
        ...weapons,
        {
            ...common, id: `tien_${band.id}_ho_than_giap`, name: `${band.name} Hộ Giới Tiên Giáp`, slot: 'armor', element: 'tho', icon: '🛡️',
            stats: { def: Math.round(power * 0.75), hp: power * 6, mp: Math.round(power * 0.8) },
            desc: `Tiên giáp tôi luyện cho cảnh giới ${band.min}–${band.max}; rơi từ tinh anh, đại yêu hoặc boss trong ${band.name}.`,
        },
        {
            ...common, id: `tien_${band.id}_dao_nguyen_boi`, name: `${band.name} Đạo Nguyên Tiên Bội`, slot: 'acc', element: 'thien', icon: '🔮',
            stats: { atk: Math.round(power * 0.45), def: Math.round(power * 0.35), hp: power * 4, sense: Math.round(power * 0.2) },
            desc: `Tiên bội cộng hưởng đạo nguyên cảnh giới ${band.min}–${band.max}; rơi từ tinh anh, đại yêu hoặc boss trong ${band.name}.`,
        },
        {
            ...common, id: `tien_${band.id}_tien_lo`, name: `${band.name} Hỗn Nguyên Tiên Lô`, slot: 'lo_dinh', element: 'hoa', icon: '🏺',
            stats: { def: Math.round(power * 0.4), hp: power * 3, sense: Math.round(power * 0.3) }, alchemyRate: 0.92,
            desc: `Lô đỉnh Tiên phẩm cho Đan Sư cảnh giới ${band.min}–${band.max}; rơi từ tinh anh, đại yêu hoặc boss trong ${band.name}.`,
        },
        {
            ...common, id: `tien_${band.id}_nguyet_hanh_thu`, name: `${band.name} Nguyệt Hành Thú`, slot: 'phi_kiem', element: 'phong', icon: '🐉',
            stats: { spd: Math.min(65, 30 + Math.floor(band.min / 2)), hp: power * 4, def: Math.round(power * 0.35) },
            flySpeed: Math.min(0.55, 0.42 + band.min * 0.002), mount: true,
            desc: `Tọa kỵ Tiên phẩm đồng hành cùng tu sĩ cảnh giới ${band.min}–${band.max}; rơi từ tinh anh, đại yêu hoặc boss trong ${band.name}.`,
        },
    ];
});

const EQUIPMENT = Object.freeze([
    { id: 'moc_kiem', name: 'Mộc Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'pham', icon: '🗡️', stats: { atk: 12 }, drop: 0.04, srcMinRealm: 0, srcMaxRealm: 2, stock: null },
    { id: 'moc_truong', name: 'Mộc Trượng', slot: 'weapon', wtype: 'phapkhi', tier: 'pham', icon: '🦯', stats: { atk: 11, mp: 30 }, drop: 0.04, srcMinRealm: 0, srcMaxRealm: 2, stock: null },
    { id: 'thiet_chuy', name: 'Thiết Chùy', slot: 'weapon', wtype: 'trongkhi', tier: 'pham', icon: '🔨', stats: { atk: 10, hp: 40 }, drop: 0.04, srcMinRealm: 0, srcMaxRealm: 2, stock: null },
    { id: 'bo_quyen_sao', name: 'Bố Quyền Sáo', slot: 'weapon', wtype: 'quyensao', tier: 'pham', icon: '🥊', stats: { atk: 12 }, drop: 0.04, srcMinRealm: 0, srcMaxRealm: 2, stock: null },
    { id: 'dong_dinh', name: 'Đồng Đỉnh Nhỏ', slot: 'weapon', wtype: 'dinh', tier: 'pham', icon: '🫖', stats: { atk: 9, sense: 3 }, drop: 0.04, srcMinRealm: 0, srcMaxRealm: 2, stock: null },
    { id: 'bo_y', name: 'Bố Y', slot: 'armor', tier: 'pham', icon: '🥋', stats: { def: 8, hp: 30 }, drop: 0.05, srcMinRealm: 0, srcMaxRealm: 2, stock: null },
    { id: 'dong_boi', name: 'Đồng Bội', slot: 'acc', tier: 'pham', icon: '📿', stats: { hp: 40, sense: 2 }, drop: 0.03, srcMinRealm: 0, srcMaxRealm: 2, stock: null },

    // ---- NHẪN TRỮ ĐỒ (Tăng tối đa 200 slot kho đồ) ----
    { id: 'nhan_tru_do_pham', name: 'Nhẫn Trữ Đồ Sơ Cấp', slot: 'nhan_tru_do', tier: 'pham', icon: '💍', storageBonus: 30, stats: { def: 10, hp: 50 }, desc: 'Nhẫn trữ đồ nhập môn tạo từ nạp giới sa, mở rộng thêm +30 ô sức chứa kho đồ.', drop: 0.03, srcMinRealm: 0, srcMaxRealm: 2, stock: null },
    { id: 'nhan_tru_do_hoang', name: 'Nhẫn Trữ Đồ Hoàng Giai', slot: 'nhan_tru_do', tier: 'hoang', icon: '💍', storageBonus: 60, stats: { def: 25, hp: 150 }, desc: 'Trữ vật giới chỉ đúc bằng uẩn không thạch, mở rộng thêm +60 ô sức chứa kho đồ.', drop: 0.02, srcMinRealm: 2, srcMaxRealm: 4, stock: 200 },
    { id: 'nhan_tru_do_huyen', name: 'Nhẫn Trữ Đồ Huyền Giai', slot: 'nhan_tru_do', tier: 'huyen', icon: '💍', storageBonus: 100, stats: { def: 50, hp: 350 }, desc: 'Nhẫn trữ vật huyền giai kết nối không gian thứ nguyên, mở rộng thêm +100 ô sức chứa kho đồ.', drop: 0.008, srcMinRealm: 3, srcMaxRealm: 6, stock: 50 },
    { id: 'nhan_tru_do_dia', name: 'Hư Không Nạp Giới', slot: 'nhan_tru_do', tier: 'dia', icon: '💍', storageBonus: 150, stats: { def: 100, hp: 800 }, desc: 'Pháp bảo trữ vật địa phẩm chế tác từ Không Gian Tinh Thạch, mở rộng thêm +150 ô sức chứa kho đồ.', drop: 0.001, srcMinRealm: 5, srcMaxRealm: 8, stock: 10 },
    { id: 'nhan_tru_do_thien', name: 'Càn Khôn Trữ Vật Giới', slot: 'nhan_tru_do', tier: 'thien', icon: '💍', storageBonus: 200, stats: { def: 200, hp: 1800, spd: 5 }, desc: 'Chí bảo trữ vật thượng cổ đạt cảnh giới cực hạn, mở rộng tối đa +200 ô sức chứa kho đồ!', drop: 0.0001, srcMinRealm: 7, srcMaxRealm: 13, stock: 5 },
    { id: 'nhan_tru_do_tien', name: 'Thái Hư Tu Di Giới', slot: 'nhan_tru_do', tier: 'tien', icon: '💍', storageBonus: 200, stats: { def: 1200, hp: 14000, spd: 20 }, desc: 'Tiên giới tu di giới chỉ chứa trọn cả một tiểu thiên thế giới, mở rộng kịch trần +200 ô sức chứa kho đồ!', drop: 0.00001, srcMinRealm: 11, srcMaxRealm: 20, stock: 2 },

    { id: 'thanh_phong_kiem', name: 'Thanh Phong Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'hoang', element: 'phong', icon: '🗡️', stats: { atk: 30, spd: 2 }, drop: 0.02, src: ['phong_lang'], stock: 200 },
    { id: 'hoa_van_truong', name: 'Hỏa Vân Trượng', slot: 'weapon', wtype: 'phapkhi', tier: 'hoang', element: 'hoa', icon: '🦯', stats: { atk: 28, mp: 60 }, drop: 0.02, src: ['hoa_ho'], stock: 200 },
    { id: 'hac_thiet_chuy', name: 'Hắc Thiết Chùy', slot: 'weapon', wtype: 'trongkhi', tier: 'hoang', element: 'tho', icon: '🔨', stats: { atk: 26, hp: 90 }, drop: 0.02, src: ['thach_hau'], stock: 150 },
    { id: 'thach_hau_quyen_sao', name: 'Thạch Hầu Quyền Sáo', slot: 'weapon', wtype: 'quyensao', tier: 'hoang', element: 'tho', icon: '🥊', stats: { atk: 30 }, drop: 0.02, src: ['thach_hau'], stock: 150 },
    { id: 'thanh_moc_dinh', name: 'Thanh Mộc Đỉnh', slot: 'weapon', wtype: 'dinh', tier: 'hoang', element: 'moc', icon: '🫖', stats: { atk: 22, sense: 6 }, drop: 0.02, src: ['thanh_xa'], stock: 150 },
    { id: 'ho_tam_kinh', name: 'Hộ Tâm Kính', slot: 'armor', tier: 'hoang', icon: '🛡️', stats: { def: 20, hp: 80 }, drop: 0.015, src: ['hoa_ho'], stock: 200 },
    { id: 'linh_xa_boi', name: 'Linh Xà Bội', slot: 'acc', tier: 'hoang', element: 'moc', icon: '💎', stats: { hp: 90, sense: 4 }, drop: 0.02, src: ['thanh_xa'], stock: 200 },

    { id: 'phong_hanh_kiem', name: 'Phong Hành Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'huyen', element: 'phong', icon: '⚔️', stats: { atk: 60, spd: 4 }, drop: 0.004, src: ['phong_lang'], stock: 30 },
    { id: 'bang_phach_truong', name: 'Băng Phách Trượng', slot: 'weapon', wtype: 'phapkhi', tier: 'huyen', element: 'bang', icon: '🦯', stats: { atk: 56, mp: 120 }, drop: 0.004, src: ['huyen_bang_mang'], stock: 30 },
    { id: 'te_bi_huyen_giap', name: 'Tê Bì Huyền Giáp', slot: 'armor', tier: 'huyen', element: 'tho', icon: '🛡️', stats: { def: 40, hp: 160 }, drop: 0.005, src: ['thiet_giap_te'], stock: 30 },
    { id: 'bang_tam_boi', name: 'Băng Tâm Bội', slot: 'acc', tier: 'huyen', element: 'bang', icon: '❄️', stats: { hp: 150, sense: 8, mp: 40 }, drop: 0.003, src: ['huyen_bang_mang'], stock: 20 },

    { id: 'loi_ung_linh_vu', name: 'Lôi Ưng Linh Vũ', slot: 'acc', tier: 'dia', element: 'loi', icon: '🪶', stats: { spd: 8, sense: 20, atk: 40 }, drop: 0.0008, src: ['loi_ung'], stock: 5 },
    { id: 'huyet_vien_quyen_sao', name: 'Huyết Viên Quyền Sáo', slot: 'weapon', wtype: 'quyensao', tier: 'dia', element: 'hoa', icon: '🥊', stats: { atk: 140, hp: 200 }, drop: 0.0005, src: ['huyet_ma_vien'], elite: true, stock: 3 },
    { id: 'kim_giap_chien_y', name: 'Kim Giáp Chiến Y', slot: 'armor', tier: 'dia', element: 'kim', icon: '🦺', stats: { def: 95, hp: 380 }, drop: 0.001, src: ['kim_giap_ngac'], stock: 5 },

    { id: 'thuy_ky_lan_giap', name: 'Thủy Kỳ Lân Giáp', slot: 'armor', tier: 'thien', element: 'thuy', icon: '🛡️', stats: { def: 220, hp: 900 }, drop: 0.00005, src: ['thuy_ky_lan'], stock: 2 },
    { id: 'cuu_vi_ho_chau', name: 'Cửu Vĩ Hồ Châu', slot: 'acc', tier: 'thien', element: 'hoa', icon: '🔮', stats: { atk: 150, sense: 40 }, drop: 0.00003, src: ['cuu_vi_ho'], stock: 2 },

    // Pháp bảo & Thần binh (Phàm Nhân Tu Tiên, Tiên Nghịch, Kiếm Lai)
    { id: 'tu_bac_dao', name: 'Tứ Bác Đao', slot: 'weapon', wtype: 'trongkhi', tier: 'hoang', element: 'kim', icon: '🗡️', stats: { atk: 34, hp: 110 }, drop: 0.03, srcMinRealm: 2, srcMaxRealm: 4, stock: 100 },
    { id: 'cuu_khuc_bao', name: 'Cửu Khúc Linh Ẩn Bào', slot: 'armor', tier: 'hoang', element: 'phong', icon: '🥋', stats: { def: 25, spd: 3, hp: 90 }, drop: 0.02, srcMinRealm: 2, srcMaxRealm: 4, stock: 150 },
    { id: 'duong_kiem_ho', name: 'Dưỡng Kiếm Hồ', slot: 'acc', tier: 'huyen', element: 'phong', icon: '🍶', stats: { atk: 45, spd: 6, sense: 15 }, drop: 0.008, srcMinRealm: 3, srcMaxRealm: 6, stock: 50 },
    { id: 'cam_hon_phien', name: 'Cấm Hồn Phiên', slot: 'weapon', wtype: 'phapkhi', tier: 'huyen', element: 'thuy', icon: '🚩', stats: { atk: 72, mp: 160, sense: 12 }, drop: 0.006, srcMinRealm: 4, srcMaxRealm: 6, stock: 30 },
    { id: 'dia_hoa_chuy', name: 'Địa Hỏa Kim Long Chùy', slot: 'weapon', wtype: 'trongkhi', tier: 'huyen', element: 'hoa', icon: '🔨', stats: { atk: 68, hp: 180, def: 20 }, drop: 0.005, srcMinRealm: 4, srcMaxRealm: 6, stock: 30 },
    { id: 'luc_bao_kiem_y', name: 'Lục Bào Kiếm Y', slot: 'armor', tier: 'huyen', element: 'moc', icon: '🥋', stats: { def: 55, hp: 220, atk: 15 }, drop: 0.006, srcMinRealm: 4, srcMaxRealm: 6, stock: 30 },
    { id: 'thanh_truc_kiem', name: 'Thanh Trúc Phong Vân Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'dia', element: 'loi', icon: '⚡', stats: { atk: 150, spd: 8, sense: 15 }, drop: 0.003, srcMinRealm: 5, srcMaxRealm: 8, stock: 15 },
    { id: 'thap_nhi_co_than', name: 'Thập Nhị Cổ Thần Giáp', slot: 'armor', tier: 'dia', element: 'tho', icon: '🛡️', stats: { def: 140, hp: 650, atk: 30 }, drop: 0.001, srcMinRealm: 6, srcMaxRealm: 9, stock: 5 },
    { id: 'tram_tien_kiem', name: 'Trảm Tiên Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'thien', element: 'kim', icon: '⚔️', stats: { atk: 320, spd: 14, sense: 30 }, drop: 0.0003, srcMinRealm: 8, srcMaxRealm: 13, stock: 3 },
    { id: 'hu_thien_dinh', name: 'Hư Thiên Đỉnh', slot: 'weapon', wtype: 'dinh', tier: 'thien', element: 'moc', icon: '🏺', stats: { atk: 260, hp: 1100, sense: 45 }, drop: 0.0002, srcMinRealm: 8, srcMaxRealm: 13, stock: 3 },
    { id: 'thien_loi_ngan_khai', name: 'Thiên Lôi Ngân Khải', slot: 'armor', tier: 'thien', element: 'loi', icon: '🦺', stats: { def: 250, hp: 1100, spd: 10 }, drop: 0.0001, srcMinRealm: 9, srcMaxRealm: 14, stock: 2 },
    { id: 'chuong_thien_binh', name: 'Chưởng Thiên Bình', slot: 'acc', tier: 'tien', icon: '🍶', unique: true, stats: { atk: 1100, def: 800, hp: 5000, mp: 2000, sense: 120 }, drop: RAREST, srcMinRealm: 7, srcMaxRealm: 20, stock: 1 },
    { id: 'thien_nghich_chau', name: 'Thiên Nghịch Châu', slot: 'acc', tier: 'tien', icon: '🔮', unique: true, stats: { atk: 1400, def: 750, hp: 4800, spd: 20, sense: 140 }, drop: RAREST, srcMinRealm: 7, srcMaxRealm: 20, stock: 1 },

    // Bộ đồ Tân Thủ: quà cho mọi người, luyện hóa sẵn, không giao dịch được.
    { id: 'tt_kiem', name: 'Tân Thủ Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'pham', icon: '🗡️', bound: true, stats: { atk: 16, spd: 1 }, drop: 0, stock: null },
    { id: 'tt_truong', name: 'Tân Thủ Trượng', slot: 'weapon', wtype: 'phapkhi', tier: 'pham', icon: '🦯', bound: true, stats: { atk: 15, mp: 40 }, drop: 0, stock: null },
    { id: 'tt_chuy', name: 'Tân Thủ Chùy', slot: 'weapon', wtype: 'trongkhi', tier: 'pham', icon: '🔨', bound: true, stats: { atk: 14, hp: 60 }, drop: 0, stock: null },
    { id: 'tt_quyen_sao', name: 'Tân Thủ Quyền Sáo', slot: 'weapon', wtype: 'quyensao', tier: 'pham', icon: '🥊', bound: true, stats: { atk: 16, def: 2 }, drop: 0, stock: null },
    { id: 'tt_dinh', name: 'Tân Thủ Đỉnh', slot: 'weapon', wtype: 'dinh', tier: 'pham', icon: '🫖', bound: true, stats: { atk: 13, sense: 4 }, drop: 0, stock: null },
    { id: 'tt_dao_bao', name: 'Tân Thủ Đạo Bào', slot: 'armor', tier: 'pham', icon: '🥋', bound: true, stats: { def: 10, hp: 50 }, drop: 0, stock: null },
    { id: 'tt_ngoc_boi', name: 'Tân Thủ Ngọc Bội', slot: 'acc', tier: 'pham', icon: '📿', bound: true, stats: { hp: 50, sense: 2 }, drop: 0, stock: null },
    { id: 'tt_ho_phu', name: 'Tân Thủ Hộ Phù', slot: 'acc', tier: 'pham', icon: '📜', bound: true, stats: { def: 4, hp: 30 }, drop: 0, stock: null },
    { id: 'hon_don_linh_chau', name: 'Hỗn Độn Linh Châu', slot: 'acc', tier: 'tien', icon: '🌌', unique: true, stats: { atk: 800, def: 550, hp: 4000, sense: 80, spd: 10 }, drop: RAREST, srcMinRealm: 6, srcMaxRealm: 20, stock: 10 },

    // ==================== BÁCH KHOA PHÁP BẢO & TRANG BỊ: KIẾM LAI ====================
    // Vũ khí khởi đầu & Phàm phẩm chuyên biệt cho Đan, Rèn, Phù
    { id: 'moc_bua', name: 'Mộc Búa', slot: 'weapon', wtype: 'bua', tier: 'pham', icon: '🔨', stats: { atk: 11, hp: 45, def: 3 }, drop: 0.04, srcMinRealm: 0, srcMaxRealm: 2, stock: null },
    { id: 'truc_but', name: 'Thanh Trúc Phù Bút', slot: 'weapon', wtype: 'but', tier: 'pham', icon: '🖌️', stats: { atk: 10, sense: 4, mp: 25 }, drop: 0.04, srcMinRealm: 0, srcMaxRealm: 2, stock: null },
    { id: 'tt_bua', name: 'Tân Thủ Búa Rèn', slot: 'weapon', wtype: 'bua', tier: 'pham', icon: '🔨', bound: true, stats: { atk: 15, hp: 65, def: 4 }, drop: 0, stock: null },
    { id: 'tt_but', name: 'Tân Thủ Phù Bút', slot: 'weapon', wtype: 'but', tier: 'pham', icon: '🖌️', bound: true, stats: { atk: 14, sense: 4, mp: 35 }, drop: 0, stock: null },

    // Vũ khí Hoàng phẩm cho Đan, Rèn, Phù
    { id: 'xich_diem_dinh', name: 'Xích Diễm Dược Đỉnh', slot: 'weapon', wtype: 'dinh', tier: 'hoang', element: 'hoa', icon: '🫖', stats: { atk: 28, sense: 8, mp: 50 }, drop: 0.02, srcMinRealm: 1, srcMaxRealm: 3, stock: 200 },
    { id: 'o_thiet_doan_chuy', name: 'Ô Thiết Đoán Chùy', slot: 'weapon', wtype: 'bua', tier: 'hoang', element: 'kim', icon: '🔨', stats: { atk: 30, hp: 120, def: 10 }, drop: 0.02, srcMinRealm: 1, srcMaxRealm: 3, stock: 200 },
    { id: 'hac_thach_ren_bua', name: 'Hắc Thạch Rèn Búa', slot: 'weapon', wtype: 'bua', tier: 'hoang', element: 'tho', icon: '🔨', stats: { atk: 28, hp: 140, def: 12 }, drop: 0.02, srcMinRealm: 1, srcMaxRealm: 3, stock: 200 },
    { id: 'chu_sa_lang_hao', name: 'Chu Sa Lang Hào Bút', slot: 'weapon', wtype: 'but', tier: 'hoang', element: 'hoa', icon: '🖌️', stats: { atk: 26, sense: 8, mp: 60 }, drop: 0.02, srcMinRealm: 1, srcMaxRealm: 3, stock: 200 },
    { id: 'bach_ngoc_phu_but', name: 'Bạch Ngọc Phù Bút', slot: 'weapon', wtype: 'but', tier: 'hoang', element: 'kim', icon: '🖌️', stats: { atk: 28, sense: 7, spd: 2 }, drop: 0.02, srcMinRealm: 1, srcMaxRealm: 3, stock: 200 },

    // Vũ khí Huyền phẩm cho Đan, Rèn, Phù
    { id: 'cuu_chuyen_dinh', name: 'Cửu Chuyển Hóa Linh Đỉnh', slot: 'weapon', wtype: 'dinh', tier: 'huyen', element: 'moc', icon: '🫖', stats: { atk: 65, sense: 20, mp: 120 }, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 50 },
    { id: 'dia_hoa_thien_cong_chuy', name: 'Địa Hỏa Thiên Công Chùy', slot: 'weapon', wtype: 'bua', tier: 'huyen', element: 'hoa', icon: '🔨', stats: { atk: 72, hp: 220, def: 25 }, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 50 },
    { id: 'xich_loi_doan_than_chuy', name: 'Xích Lôi Đoán Thần Chùy', slot: 'weapon', wtype: 'bua', tier: 'huyen', element: 'loi', icon: '🔨', stats: { atk: 78, hp: 180, spd: 4 }, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 50 },
    { id: 'huyen_tinh_phan_quan_but', name: 'Huyền Tinh Phán Quan Bút', slot: 'weapon', wtype: 'but', tier: 'huyen', element: 'ma', icon: '🖌️', stats: { atk: 68, sense: 18, mp: 140 }, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 50 },
    { id: 'thien_hac_phu_but', name: 'Thiên Hạc Linh Vũ Bút', slot: 'weapon', wtype: 'but', tier: 'huyen', element: 'phong', icon: '🖌️', stats: { atk: 62, sense: 22, spd: 5 }, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 50 },

    // Vũ khí Địa phẩm cho Đan, Rèn, Phù
    { id: 'thai_ap_than_dinh', name: 'Thái Ất Dược Vương Đỉnh', slot: 'weapon', wtype: 'dinh', tier: 'dia', element: 'moc', icon: '🫖', stats: { atk: 175, sense: 35, mp: 300 }, drop: 0.002, srcMinRealm: 5, srcMaxRealm: 8, stock: 15 },
    { id: 'bat_cuc_kim_cang_bua', name: 'Bát Cực Kim Cang Búa', slot: 'weapon', wtype: 'bua', tier: 'dia', element: 'kim', icon: '🔨', stats: { atk: 180, hp: 750, def: 55 }, drop: 0.002, srcMinRealm: 5, srcMaxRealm: 8, stock: 15 },
    { id: 'van_thiet_quy_phu_chuy', name: 'Vạn Thiết Quỷ Phủ Chùy', slot: 'weapon', wtype: 'bua', tier: 'dia', element: 'tho', icon: '🔨', stats: { atk: 190, hp: 900, def: 65 }, drop: 0.002, srcMinRealm: 5, srcMaxRealm: 8, stock: 15 },
    { id: 'cuu_u_nhiep_hon_but', name: 'Cửu U Nhiếp Hồn Bút', slot: 'weapon', wtype: 'but', tier: 'dia', element: 'ma', icon: '🖌️', stats: { atk: 165, sense: 38, mp: 350, crit: 0.05 }, drop: 0.002, srcMinRealm: 5, srcMaxRealm: 8, stock: 15 },
    { id: 'thai_cuc_ngu_sac_but', name: 'Thái Cực Ngũ Sắc Bút', slot: 'weapon', wtype: 'but', tier: 'dia', element: 'thien', icon: '🖌️', stats: { atk: 170, sense: 40, hp: 600, mp: 400 }, drop: 0.002, srcMinRealm: 5, srcMaxRealm: 8, stock: 15 },

    // Vũ khí Thiên phẩm cho Đan, Rèn, Phù
    { id: 'bat_quai_dan_lo', name: 'Bát Quái Cửu Khiếu Lô', slot: 'weapon', wtype: 'dinh', tier: 'thien', element: 'hoa', icon: '🏺', stats: { atk: 310, hp: 1200, sense: 55 }, drop: 0.0003, srcMinRealm: 7, srcMaxRealm: 13, stock: 5 },
    { id: 'cuu_chuyen_khai_thien_chuy', name: 'Cửu Chuyển Khai Thiên Chùy', slot: 'weapon', wtype: 'bua', tier: 'thien', element: 'hoa', icon: '🔨', stats: { atk: 340, hp: 1400, def: 110, sense: 30 }, drop: 0.0003, srcMinRealm: 7, srcMaxRealm: 13, stock: 5 },
    { id: 'thai_thuong_than_phu_but', name: 'Thái Thượng Thần Phù Bút', slot: 'weapon', wtype: 'but', tier: 'thien', element: 'loi', icon: '🖌️', stats: { atk: 320, sense: 60, mp: 800, spd: 12 }, drop: 0.0003, srcMinRealm: 7, srcMaxRealm: 13, stock: 5 },

    // Vũ khí Tiên phẩm cho Đan, Rèn, Phù
    { id: 'hon_don_tao_hoa_dinh', name: 'Hỗn Độn Tạo Hóa Thần Đỉnh', slot: 'weapon', wtype: 'dinh', tier: 'tien', element: 'hoa', icon: '🏺', stats: { atk: 750, hp: 3600, sense: 110, mp: 1000 }, drop: RAREST, srcMinRealm: 8, srcMaxRealm: 20, stock: 2, desc: 'Lò luyện đan khởi nguyên viễn cổ, nghịch chuyển sinh tử, đan thành dẫn động thiên kiếp.' },
    { id: 'thai_hu_than_cong_chuy', name: 'Thái Hư Thần Công Chùy', slot: 'weapon', wtype: 'bua', tier: 'tien', element: 'kim', icon: '🔨', stats: { atk: 720, hp: 3800, def: 250, spd: 10 }, drop: RAREST, srcMinRealm: 8, srcMaxRealm: 20, stock: 2, desc: 'Chiếc búa rèn đúc nhật nguyệt tinh thần của Thái Hư Thần Tông, một búa phá vạn pháp.' },
    { id: 'van_co_hoa_phu_tien_but', name: 'Vạn Cổ Hóa Đạo Tiên Bút', slot: 'weapon', wtype: 'but', tier: 'tien', element: 'thien', icon: '🖌️', stats: { atk: 680, sense: 120, mp: 2500, hp: 2000, spd: 16 }, drop: RAREST, srcMinRealm: 8, srcMaxRealm: 20, stock: 2, desc: 'Bút vẽ nên thiên đạo càn khôn của Thượng Cổ Chân Tiên, họa phù trảm diệt thần ma.' },

    { id: 'so_nhat_kiem', name: 'Sơ Nhất (Tiểu Phong Đô)', slot: 'weapon', wtype: 'kiem', tier: 'tien', element: 'kim', icon: '🗡️', stats: { atk: 650, spd: 15, crit: 0.1 }, drop: RAREST, srcMinRealm: 8, srcMaxRealm: 20, stock: 50, desc: 'Bản mệnh phi kiếm của Trần Bình An, sắc bén cực hạn, phá giáp vô song.' },
    { id: 'thap_ngu_kiem', name: 'Thập Ngũ Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'tien', element: 'phong', icon: '⚔️', stats: { atk: 620, spd: 22, crit: 0.15 }, drop: RAREST, srcMinRealm: 8, srcMaxRealm: 20, stock: 50, desc: 'Bản mệnh phi kiếm nhanh như chớp giật, chuyên kích sát kết liễu cường địch.' },
    { id: 'lung_trung_tuoc', name: 'Lung Trung Tước', slot: 'acc', tier: 'tien', element: 'kim', icon: '🪶', stats: { atk: 350, def: 300, hp: 2000, sense: 50 }, drop: 0.0002, srcMinRealm: 7, srcMaxRealm: 20, stock: 100, desc: 'Phi kiếm tạo kiếm vực giam cầm, phong tỏa không gian địch thủ.' },
    { id: 'tinh_de_nguyet', name: 'Tỉnh Để Nguyệt', slot: 'weapon', wtype: 'kiem', tier: 'tien', element: 'thuy', icon: '🌙', stats: { atk: 580, hp: 1500, sense: 60 }, drop: 0.0002, srcMinRealm: 7, srcMaxRealm: 20, stock: 100, desc: 'Bản mệnh phi kiếm phân hóa ảo ảnh, trăng trong đáy giếng ảo diệu vô cùng.' },
    { id: 'tung_cham', name: 'Tùng Châm Phi Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'huyen', element: 'moc', icon: '🌲', stats: { atk: 65, spd: 5 }, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 25000 },
    { id: 'dam_loi', name: 'Đạm Lôi Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'hoang', element: 'loi', icon: '⚡', stats: { atk: 35, spd: 3 }, drop: 0.015, srcMinRealm: 2, srcMaxRealm: 4, stock: 50000 },
    { id: 'thien_chan_kiem', name: 'Tiên Kiếm Thiên Chân', slot: 'weapon', wtype: 'kiem', tier: 'dia', element: 'kim', icon: '⚔️', stats: { atk: 180, spd: 9, sense: 20 }, drop: 0.002, srcMinRealm: 5, srcMaxRealm: 9, stock: 5000, desc: 'Thần kiếm hộ thân của Ninh Diêu, kiếm ý thuần túy thông thiên triệt địa.' },
    { id: 'thuy_vi_kiem', name: 'Thúy Vi Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'dia', element: 'moc', icon: '🗡️', stats: { atk: 160, hp: 600 }, drop: 0.003, srcMinRealm: 5, srcMaxRealm: 8, stock: 8000 },
    { id: 'thanh_thuong_kiem', name: 'Thanh Thương Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'dia', element: 'phong', icon: '🗡️', stats: { atk: 170, spd: 11 }, drop: 0.003, srcMinRealm: 5, srcMaxRealm: 8, stock: 8000 },
    { id: 'tan_hoa_kiem', name: 'Tân Hỏa Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'thien', element: 'hoa', icon: '🔥', stats: { atk: 340, spd: 12, hp: 1200 }, drop: 0.0003, srcMinRealm: 7, srcMaxRealm: 13, stock: 2000 },
    { id: 'phap_bao_kim_le', name: 'Pháp Bào Kim Lễ', slot: 'armor', tier: 'tien', element: 'kim', icon: '🥋', stats: { def: 1800, hp: 16000, spd: 12 }, drop: RAREST, srcMinRealm: 8, srcMaxRealm: 20, stock: 80, desc: 'Pháp bảo tự chữa lành, phòng ngự cực đạo của Trần Bình An.' },
    { id: 'phap_bao_mac_truc', name: 'Pháp Bào Mặc Trúc Lâm', slot: 'armor', tier: 'pham', element: 'moc', icon: '🥋', stats: { def: 18, hp: 90 }, drop: 0.04, srcMinRealm: 0, srcMaxRealm: 2, stock: null },
    { id: 'tram_long_thach', name: 'Trảm Long Thạch', slot: 'acc', tier: 'tien', element: 'tho', icon: '💎', stats: { atk: 1200, def: 900, hp: 10000, sense: 300 }, drop: RAREST, srcMinRealm: 7, srcMaxRealm: 20, stock: 60, desc: 'Bảo thạch mài giũa phi kiếm chí bảo viễn cổ, mở ra kiếm đạo cực hạn.' },
    { id: 'chi_xich_vat', name: 'Ngọc Bài Chỉ Xích Vật', slot: 'acc', tier: 'dia', icon: '📿', stats: { def: 80, hp: 500, sense: 30 }, drop: 0.004, srcMinRealm: 5, srcMaxRealm: 8, stock: 12000, desc: 'Bảo vật chứa đựng không gian ngàn dặm thu nhỏ trong gang tấc.' },
    { id: 'chuoi_hach_dieu', name: 'Chuỗi Hạt Hạch Điêu', slot: 'acc', tier: 'thien', icon: '📿', stats: { atk: 180, def: 150, hp: 1000, sense: 45 }, drop: 0.0005, srcMinRealm: 7, srcMaxRealm: 11, stock: 3000 },
    { id: 'giap_vien', name: 'Viên Ma Giáp', slot: 'armor', tier: 'tien', element: 'kim', icon: '🛡️', stats: { def: 1600, hp: 14000 }, drop: 0.0002, srcMinRealm: 8, srcMaxRealm: 15, stock: 300 },
    { id: 'thien_tien_dong_y', name: 'Thiên Tiên Động Y', slot: 'armor', tier: 'tien', element: 'phong', icon: '🦺', stats: { def: 1900, hp: 17000, spd: 15 }, drop: RAREST, srcMinRealm: 9, srcMaxRealm: 20, stock: 50 },
    { id: 'thanh_xa_tai_hap', name: 'Thanh Xà Tại Hạp', slot: 'weapon', wtype: 'kiem', tier: 'thien', element: 'moc', icon: '🐍', stats: { atk: 330, spd: 14, crit: 0.12 }, drop: 0.0003, srcMinRealm: 7, srcMaxRealm: 13, stock: 1500 },

    // ==================== BÁCH KHOA PHÁP BẢO & TRANG BỊ: TIÊN NGHỊCH ====================
    { id: 'phong_tien_an', name: 'Phong Tiên Ấn', slot: 'weapon', wtype: 'dinh', tier: 'tien', element: 'kim', icon: '👑', stats: { atk: 2600, hp: 12000, sense: 350 }, drop: RAREST, srcMinRealm: 8, srcMaxRealm: 20, stock: 50, desc: 'Đại ấn phong tiên ngưng kết ức vạn sinh linh, trấn áp vạn cổ thiên kiêu.' },
    { id: 'thap_uc_ton_hon_phien', name: 'Thập Ức Tôn Hồn Phiên', slot: 'weapon', wtype: 'phapkhi', tier: 'tien', element: 'thuy', icon: '🚩', stats: { atk: 2800, mp: 9000, sense: 400 }, drop: RAREST, srcMinRealm: 8, srcMaxRealm: 20, stock: 50, desc: 'Ma khí đỉnh cao Tiên Nghịch của Vương Lâm, chứa 10 ức hồn phách cường giả.' },
    { id: 'thien_ho_ky', name: 'Thiên Hổ Kỳ', slot: 'weapon', wtype: 'phapkhi', tier: 'huyen', element: 'phong', icon: '🏴', stats: { atk: 75, mp: 180 }, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 25000 },
    { id: 'xa_than_chien_xa', name: 'Xạ Thần Chiến Xa', slot: 'weapon', wtype: 'trongkhi', tier: 'tien', element: 'loi', icon: '🏹', stats: { atk: 820, hp: 3000, spd: 5 }, drop: RAREST, srcMinRealm: 8, srcMaxRealm: 20, stock: 40, desc: 'Chiến xa viễn cổ đồ thần diệt ma, tích lực bắn một phát toái diệt tinh cầu.' },
    { id: 'tam_xoa_kich', name: 'Tam Xoa Thần Kích', slot: 'weapon', wtype: 'trongkhi', tier: 'dia', element: 'thuy', icon: '🔱', stats: { atk: 185, hp: 800 }, drop: 0.002, srcMinRealm: 5, srcMaxRealm: 8, stock: 6000 },
    { id: 'ly_minh_thao_mao', name: 'Ly Minh Thảo Mạo', slot: 'acc', tier: 'tien', element: 'tho', icon: '👒', stats: { def: 320, hp: 2400, sense: 70 }, drop: 0.0002, srcMinRealm: 8, srcMaxRealm: 20, stock: 150, desc: 'Nón cỏ thô sơ ẩn giấu thiên địa quy tắc của Chu Tước Tinh.' },
    { id: 'thiet_kiem_gi', name: 'Thiết Kiếm Gỉ', slot: 'weapon', wtype: 'kiem', tier: 'hoang', element: 'kim', icon: '🗡️', stats: { atk: 32, crit: 0.08 }, drop: 0.02, srcMinRealm: 1, srcMaxRealm: 3, stock: 80000 },
    { id: 'huyet_sat_kiem', name: 'Huyết Sát Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'huyen', element: 'hoa', icon: '🩸', stats: { atk: 80, spd: 4 }, drop: 0.006, srcMinRealm: 3, srcMaxRealm: 6, stock: 20000 },
    { id: 'that_thai_than_khong_dinh', name: 'Thất Thải Thần Không Đinh', slot: 'acc', tier: 'huyen', element: 'loi', icon: '📌', stats: { atk: 50, sense: 18 }, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 20000 },
    { id: 'co_than_ho_uyen', name: 'Cổ Thần Hộ Uyển', slot: 'armor', tier: 'hoang', element: 'tho', icon: '🥊', stats: { def: 28, hp: 120 }, drop: 0.015, srcMinRealm: 2, srcMaxRealm: 4, stock: 50000 },
    { id: 'thien_hoang_lo', name: 'Thiên Hoàng Lô', slot: 'weapon', wtype: 'dinh', tier: 'huyen', element: 'hoa', icon: '🫖', stats: { atk: 70, sense: 16 }, drop: 0.006, srcMinRealm: 3, srcMaxRealm: 6, stock: 20000 },
    { id: 'chu_tuoc_vu', name: 'Chu Tước Vũ', slot: 'acc', tier: 'dia', element: 'hoa', icon: '🪶', stats: { atk: 120, spd: 10, sense: 25 }, drop: 0.002, srcMinRealm: 5, srcMaxRealm: 9, stock: 8000 },
    { id: 'vu_ma_thuong', name: 'Vụ Ma Thương', slot: 'weapon', wtype: 'trongkhi', tier: 'hoang', element: 'thuy', icon: '🔱', stats: { atk: 38, hp: 130 }, drop: 0.018, srcMinRealm: 2, srcMaxRealm: 4, stock: 60000 },
    { id: 'sat_luc_kiem', name: 'Sát Lục Bản Thể Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'dia', element: 'kim', icon: '⚔️', stats: { atk: 210, spd: 12, crit: 0.18 }, drop: 0.0015, srcMinRealm: 5, srcMaxRealm: 9, stock: 5000 },
    { id: 'nhan_qua_kiem', name: 'Nhân Quả Đạo Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'dia', element: 'phong', icon: '☯️', stats: { atk: 200, sense: 35 }, drop: 0.0018, srcMinRealm: 5, srcMaxRealm: 9, stock: 5000 },
    { id: 'sinh_tu_kiem', name: 'Sinh Tử Luân Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'hoang', element: 'moc', icon: '🗡️', stats: { atk: 36, hp: 100 }, drop: 0.02, srcMinRealm: 2, srcMaxRealm: 4, stock: 50000 },
    { id: 'chan_gia_kiem', name: 'Chân Giả Đạo Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'dia', element: 'bang', icon: '⚔️', stats: { atk: 195, hp: 700 }, drop: 0.002, srcMinRealm: 5, srcMaxRealm: 9, stock: 5000 },
    { id: 'am_dao', name: 'Âm Sát Bảo Đao', slot: 'weapon', wtype: 'trongkhi', tier: 'dia', element: 'thuy', icon: '🔪', stats: { atk: 190, hp: 750 }, drop: 0.002, srcMinRealm: 5, srcMaxRealm: 9, stock: 5000 },
    { id: 'dai_thien_ton_chi_duong', name: 'Đại Thiên Tôn Chi Dương', slot: 'acc', tier: 'thien', element: 'hoa', icon: '☀️', stats: { atk: 380, def: 280, hp: 2000, sense: 60 }, drop: 0.0002, srcMinRealm: 8, srcMaxRealm: 15, stock: 1000 },

    // ==================== BÁCH KHOA PHÁP BẢO & TRANG BỊ: PHÀM NHÂN TU TIÊN ====================
    { id: 'noan_duong_bao_ngoc', name: 'Noãn Dương Bảo Ngọc', slot: 'acc', tier: 'pham', element: 'hoa', icon: '💎', stats: { hp: 80, def: 6 }, drop: 0.04, srcMinRealm: 0, srcMaxRealm: 2, stock: null },
    { id: 'dan_hon_chung', name: 'Dẫn Hồn Chung', slot: 'weapon', wtype: 'phapkhi', tier: 'dia', element: 'thuy', icon: '🔔', stats: { atk: 160, mp: 400, sense: 28 }, drop: 0.0025, srcMinRealm: 5, srcMaxRealm: 8, stock: 6000 },
    { id: 'thang_tien_lenh', name: 'Thăng Tiên Lệnh', slot: 'acc', tier: 'thien', icon: '🏷️', stats: { hp: 1200, sense: 40, spd: 6 }, drop: 0.0004, srcMinRealm: 6, srcMaxRealm: 11, stock: 2000, desc: 'Lệnh bài bái nhập danh môn đại phái trong Phàm Nhân Tu Tiên.' },
    { id: 'thanh_giao_ky', name: 'Thanh Giao Kỳ', slot: 'weapon', wtype: 'phapkhi', tier: 'hoang', element: 'thuy', icon: '🚩', stats: { atk: 32, mp: 80 }, drop: 0.02, srcMinRealm: 1, srcMaxRealm: 3, stock: 50000 },
    { id: 'huyen_thiet_phi_thien_thuan', name: 'Huyền Thiết Phi Thiên Thuẫn', slot: 'armor', tier: 'hoang', element: 'kim', icon: '🛡️', stats: { def: 24, hp: 110 }, drop: 0.02, srcMinRealm: 1, srcMaxRealm: 3, stock: 50000 },
    { id: 'hoang_la_tan', name: 'Hoàng La Tán', slot: 'armor', tier: 'hoang', element: 'tho', icon: '☂️', stats: { def: 26, hp: 130 }, drop: 0.02, srcMinRealm: 2, srcMaxRealm: 4, stock: 50000 },
    { id: 'vo_hinh_cham', name: 'Vô Hình Châm', slot: 'weapon', wtype: 'kiem', tier: 'hoang', element: 'phong', icon: '🪡', stats: { atk: 36, spd: 4 }, drop: 0.015, srcMinRealm: 2, srcMaxRealm: 4, stock: 40000 },
    { id: 'dap_van_ngoa', name: 'Đạp Vân Ngoa', slot: 'acc', tier: 'pham', element: 'phong', icon: '👢', stats: { spd: 3, hp: 40 }, drop: 0.03, srcMinRealm: 0, srcMaxRealm: 2, stock: null },
    { id: 'kim_phu_tu_mau_nhan', name: 'Kim Phù Tử Mẫu Nhận', slot: 'weapon', wtype: 'kiem', tier: 'huyen', element: 'kim', icon: '🗡️', stats: { atk: 72, crit: 0.1 }, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 20000 },
    { id: 'tuyet_hong_lang', name: 'Tuyết Hồng Lăng', slot: 'acc', tier: 'tien', element: 'bang', icon: '🧣', stats: { def: 280, hp: 2200, spd: 10 }, drop: 0.0002, srcMinRealm: 8, srcMaxRealm: 20, stock: 200 },
    { id: 'nguyet_duong_bao_chau', name: 'Nguyệt Dương Bảo Châu', slot: 'acc', tier: 'huyen', element: 'hoa', icon: '🔮', stats: { atk: 45, mp: 150 }, drop: 0.006, srcMinRealm: 3, srcMaxRealm: 6, stock: 25000 },
    { id: 'chu_tuoc_hoan', name: 'Chu Tước Hoàn', slot: 'acc', tier: 'huyen', element: 'hoa', icon: '⭕', stats: { atk: 55, hp: 200 }, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 20000 },
    { id: 'tu_hon_bat', name: 'Tụ Hồn Bát', slot: 'weapon', wtype: 'dinh', tier: 'hoang', element: 'thuy', icon: '🥣', stats: { atk: 25, sense: 8 }, drop: 0.02, srcMinRealm: 2, srcMaxRealm: 4, stock: 50000 },
    { id: 'than_phong_chu', name: 'Thần Phong Chu', slot: 'acc', tier: 'tien', element: 'phong', icon: '⛵', stats: { spd: 25, hp: 2000, def: 200 }, drop: RAREST, srcMinRealm: 7, srcMaxRealm: 20, stock: 100, desc: 'Chiến chu ngự phong phi hành ngàn dặm cực tốc của Hàn Lập.' },
    { id: 'o_long_doat', name: 'Ô Long Đoạt', slot: 'weapon', wtype: 'trongkhi', tier: 'tien', element: 'kim', icon: '🔱', stats: { atk: 720, hp: 2800 }, drop: RAREST, srcMinRealm: 8, srcMaxRealm: 20, stock: 80 },
    { id: 'phong_van_phien', name: 'Phong Vân Phiên', slot: 'weapon', wtype: 'phapkhi', tier: 'tien', element: 'phong', icon: '🚩', stats: { atk: 680, mp: 2200 }, drop: RAREST, srcMinRealm: 8, srcMaxRealm: 20, stock: 90 },
    { id: 'dai_na_di_lenh', name: 'Đại Na Di Lệnh', slot: 'acc', tier: 'dia', icon: '🏷️', stats: { def: 110, hp: 700, sense: 25 }, drop: 0.002, srcMinRealm: 5, srcMaxRealm: 8, stock: 10000 },
    { id: 'luc_hoang_kiem', name: 'Lục Hoàng Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'huyen', element: 'moc', icon: '🗡️', stats: { atk: 78, hp: 250 }, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 25000 },
    { id: 'hon_nguyen_bat', name: 'Hỗn Nguyên Bát', slot: 'weapon', wtype: 'dinh', tier: 'dia', element: 'tho', icon: '🥣', stats: { atk: 155, def: 60, hp: 800 }, drop: 0.0025, srcMinRealm: 5, srcMaxRealm: 8, stock: 8000 },
    { id: 'han_le_chau', name: 'Hàn Lê Châu', slot: 'acc', tier: 'tien', element: 'bang', icon: '❄️', stats: { atk: 350, def: 300, hp: 2500, sense: 60 }, drop: RAREST, srcMinRealm: 8, srcMaxRealm: 20, stock: 100 },
    { id: 'huyen_am_hoan', name: 'Huyền Âm Hoàn', slot: 'acc', tier: 'thien', element: 'thuy', icon: '⭕', stats: { def: 180, hp: 1300, mp: 600 }, drop: 0.0004, srcMinRealm: 7, srcMaxRealm: 11, stock: 3000 },
    { id: 'thanh_hoa_loi', name: 'Thanh Hỏa Lôi', slot: 'acc', tier: 'thien', element: 'hoa', icon: '💣', stats: { atk: 260, crit: 0.15 }, drop: 0.0005, srcMinRealm: 7, srcMaxRealm: 11, stock: 3000 },
    { id: 'ba_la_chau', name: 'Bà La Châu', slot: 'acc', tier: 'thien', element: 'kim', icon: '📿', stats: { def: 220, hp: 1500, sense: 50 }, drop: 0.0004, srcMinRealm: 7, srcMaxRealm: 11, stock: 3000 },
    { id: 'hoang_lan_giap', name: 'Hoàng Lân Giáp', slot: 'armor', tier: 'hoang', element: 'kim', icon: '🛡️', stats: { def: 30, hp: 140 }, drop: 0.02, srcMinRealm: 2, srcMaxRealm: 4, stock: 60000 },
    { id: 'lang_thu_khoi_loi', name: 'Lang Thủ Khôi Lỗi', slot: 'weapon', wtype: 'quyensao', tier: 'hoang', element: 'tho', icon: '🐺', stats: { atk: 36, hp: 120 }, drop: 0.015, srcMinRealm: 2, srcMaxRealm: 4, stock: 50000 },
    { id: 'xa_ve_khoi_loi', name: 'Xà Vệ Khôi Lỗi', slot: 'weapon', wtype: 'phapkhi', tier: 'dia', element: 'moc', icon: '🐍', stats: { atk: 175, mp: 450 }, drop: 0.002, srcMinRealm: 5, srcMaxRealm: 8, stock: 8000 },
    { id: 'huyen_hoang_kinh', name: 'Huyền Hoàng Kính', slot: 'armor', tier: 'hoang', element: 'tho', icon: '🪞', stats: { def: 28, hp: 130 }, drop: 0.02, srcMinRealm: 1, srcMaxRealm: 3, stock: 60000 },
    { id: 'ngu_hanh_hoan', name: 'Ngũ Hành Hoàn', slot: 'acc', tier: 'hoang', icon: '⭕', stats: { atk: 20, def: 15, hp: 100 }, drop: 0.02, srcMinRealm: 2, srcMaxRealm: 4, stock: 60000 },
    { id: 'ngoc_nhu_y', name: 'Ngọc Như Ý', slot: 'acc', tier: 'dia', icon: '🪄', stats: { def: 90, hp: 650, sense: 30 }, drop: 0.0025, srcMinRealm: 5, srcMaxRealm: 8, stock: 10000 },
    { id: 'quan_nhat_kiem', name: 'Quán Nhật Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'tien', element: 'hoa', icon: '⚔️', stats: { atk: 760, spd: 16, crit: 0.15 }, drop: RAREST, srcMinRealm: 9, srcMaxRealm: 20, stock: 60 },
    { id: 'phong_loi_si', name: 'Phong Lôi Sí', slot: 'acc', tier: 'tien', element: 'loi', icon: '🪽', stats: { spd: 30, atk: 350, hp: 2500 }, drop: RAREST, srcMinRealm: 8, srcMaxRealm: 20, stock: 80, desc: 'Bảo vật cánh chim ngưng tụ phong lôi thần thông của Hàn Lập.' },
    { id: 'cu_chung', name: 'Cự Chung Cổ Bảo', slot: 'weapon', wtype: 'dinh', tier: 'huyen', element: 'kim', icon: '🔔', stats: { atk: 72, def: 35, hp: 300 }, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 25000 },
    { id: 'bat_mon_kim_quang_kinh', name: 'Bát Môn Kim Quang Kính', slot: 'armor', tier: 'hoang', element: 'kim', icon: '🪞', stats: { def: 32, hp: 150 }, drop: 0.018, srcMinRealm: 2, srcMaxRealm: 4, stock: 60000 },
    { id: 'luong_nghi_hoan', name: 'Lưỡng Nghi Hoàn', slot: 'acc', tier: 'dia', icon: '⭕', stats: { atk: 90, def: 75, hp: 600 }, drop: 0.0025, srcMinRealm: 5, srcMaxRealm: 8, stock: 10000 },
    { id: 'ma_tran_phien', name: 'Ma Trần Phiên', slot: 'weapon', wtype: 'phapkhi', tier: 'huyen', element: 'tho', icon: '🚩', stats: { atk: 76, mp: 190 }, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 25000 },
    { id: 'thien_trong_phong', name: 'Thiên Trọng Phong', slot: 'weapon', wtype: 'trongkhi', tier: 'thien', element: 'tho', icon: '⛰️', stats: { atk: 330, hp: 1400, def: 80 }, drop: 0.0003, srcMinRealm: 7, srcMaxRealm: 13, stock: 2000 },
    { id: 'huyet_ma_kiem_pn', name: 'Huyết Ma Kiếm Phàm Nhân', slot: 'weapon', wtype: 'kiem', tier: 'huyen', element: 'hoa', icon: '🗡️', stats: { atk: 79, crit: 0.1 }, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 25000 },
    { id: 'lam_quang_thuan', name: 'Lam Quang Thuẫn', slot: 'armor', tier: 'dia', element: 'thuy', icon: '🛡️', stats: { def: 120, hp: 800 }, drop: 0.0025, srcMinRealm: 5, srcMaxRealm: 8, stock: 10000 },
    { id: 'ngung_quang_bao_kinh', name: 'Ngưng Quang Bảo Kính', slot: 'acc', tier: 'tien', element: 'kim', icon: '🪞', stats: { def: 360, hp: 2600, sense: 80 }, drop: RAREST, srcMinRealm: 8, srcMaxRealm: 20, stock: 100 },
    { id: 'tru_ta_thich', name: 'Tru Tà Thích', slot: 'weapon', wtype: 'kiem', tier: 'dia', element: 'loi', icon: '🗡️', stats: { atk: 185, spd: 8 }, drop: 0.0025, srcMinRealm: 5, srcMaxRealm: 8, stock: 10000 },
    { id: 'hoang_linh_dinh', name: 'Hoàng Linh Đỉnh', slot: 'weapon', wtype: 'dinh', tier: 'thien', element: 'tho', icon: '🫖', stats: { atk: 280, hp: 1200, sense: 45 }, drop: 0.0003, srcMinRealm: 7, srcMaxRealm: 13, stock: 2000 },
    { id: 'tu_viet_dau', name: 'Tử Việt Đâu', slot: 'armor', tier: 'huyen', element: 'kim', icon: '🪖', stats: { def: 50, hp: 220 }, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 25000 },
    { id: 'quy_la_phien', name: 'Quỷ La Phiên', slot: 'weapon', wtype: 'phapkhi', tier: 'huyen', element: 'thuy', icon: '🚩', stats: { atk: 78, mp: 200 }, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 25000 },
    { id: 'me_tien_chung', name: 'Mê Tiên Chung', slot: 'weapon', wtype: 'phapkhi', tier: 'dia', element: 'phong', icon: '🔔', stats: { atk: 170, mp: 420 }, drop: 0.0025, srcMinRealm: 5, srcMaxRealm: 8, stock: 10000 },
    { id: 'pha_cam_chau', name: 'Phá Cấm Châu', slot: 'acc', tier: 'dia', icon: '🔮', stats: { atk: 80, sense: 30 }, drop: 0.0025, srcMinRealm: 5, srcMaxRealm: 8, stock: 10000 },
    { id: 'thien_hoa_than_lien', name: 'Thiên Hỏa Thần Liên', slot: 'weapon', wtype: 'trongkhi', tier: 'dia', element: 'hoa', icon: '⛓️', stats: { atk: 195, hp: 750 }, drop: 0.002, srcMinRealm: 5, srcMaxRealm: 9, stock: 8000 },
    { id: 'thien_cuc_lenh', name: 'Thiên Cực Lệnh', slot: 'acc', tier: 'thien', icon: '🏷️', stats: { atk: 220, hp: 1400, sense: 50 }, drop: 0.0003, srcMinRealm: 7, srcMaxRealm: 13, stock: 2000 },
    { id: 'phan_thuy_ky', name: 'Phân Thủy Kỳ', slot: 'weapon', wtype: 'phapkhi', tier: 'thien', element: 'thuy', icon: '🚩', stats: { atk: 310, mp: 1000 }, drop: 0.0003, srcMinRealm: 7, srcMaxRealm: 13, stock: 2000 },
    { id: 'can_khon_thap', name: 'Càn Khôn Tháp', slot: 'armor', tier: 'huyen', element: 'tho', icon: '🗼', stats: { def: 60, hp: 260 }, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 25000 },
    { id: 'thanh_dinh', name: 'Thánh Đỉnh', slot: 'weapon', wtype: 'dinh', tier: 'thien', element: 'moc', icon: '🫖', stats: { atk: 290, hp: 1300 }, drop: 0.0003, srcMinRealm: 7, srcMaxRealm: 13, stock: 2000 },
    { id: 'thanh_thiem_chung', name: 'Thanh Thiền Chung', slot: 'weapon', wtype: 'phapkhi', tier: 'tien', element: 'kim', icon: '🔔', stats: { atk: 2500, mp: 8000, def: 500 }, drop: RAREST, srcMinRealm: 8, srcMaxRealm: 20, stock: 100 },
    { id: 'thach_linh', name: 'Thạch Linh Thần Tướng', slot: 'armor', tier: 'thien', element: 'tho', icon: '🗿', stats: { def: 240, hp: 1500 }, drop: 0.0003, srcMinRealm: 7, srcMaxRealm: 13, stock: 2000 },
    { id: 'hoa_long_ti', name: 'Hóa Long Tỉ', slot: 'acc', tier: 'tien', element: 'kim', icon: '👑', stats: { atk: 1200, def: 900, hp: 11000, sense: 300 }, drop: RAREST, srcMinRealm: 8, srcMaxRealm: 20, stock: 80 },
    { id: 'huyet_ma_chau', name: 'Huyết Ma Châu', slot: 'acc', tier: 'tien', element: 'hoa', icon: '🔴', stats: { atk: 1300, hp: 10000, crit: 0.15, sense: 320 }, drop: RAREST, srcMinRealm: 8, srcMaxRealm: 20, stock: 80 },
    { id: 'that_diem_phien', name: 'Thất Diễm Phiến', slot: 'weapon', wtype: 'phapkhi', tier: 'huyen', element: 'hoa', icon: '🪭', stats: { atk: 85, mp: 210 }, drop: 0.004, srcMinRealm: 3, srcMaxRealm: 6, stock: 25000 },
    { id: 'bat_linh_xich', name: 'Bát Linh Xích', slot: 'weapon', wtype: 'phapkhi', tier: 'huyen', element: 'moc', icon: '📏', stats: { atk: 80, sense: 20 }, drop: 0.004, srcMinRealm: 3, srcMaxRealm: 6, stock: 25000 },
    { id: 'hac_phong_ky', name: 'Hắc Phong Kỳ', slot: 'weapon', wtype: 'phapkhi', tier: 'huyen', element: 'phong', icon: '🚩', stats: { atk: 82, spd: 6 }, drop: 0.004, srcMinRealm: 3, srcMaxRealm: 6, stock: 25000 },
    { id: 'hoa_long_trao', name: 'Hỏa Long Tráo', slot: 'armor', tier: 'dia', element: 'hoa', icon: '🏮', stats: { def: 110, hp: 750 }, drop: 0.002, srcMinRealm: 5, srcMaxRealm: 8, stock: 10000 },
    { id: 'tu_tuong_xich', name: 'Tứ Tượng Xích', slot: 'weapon', wtype: 'phapkhi', tier: 'thien', element: 'tho', icon: '📏', stats: { atk: 300, hp: 1100 }, drop: 0.0003, srcMinRealm: 7, srcMaxRealm: 13, stock: 2000 },
    { id: 'van_yeu_phien', name: 'Vạn Yêu Phiên', slot: 'weapon', wtype: 'phapkhi', tier: 'huyen', element: 'moc', icon: '🚩', stats: { atk: 84, mp: 220 }, drop: 0.004, srcMinRealm: 3, srcMaxRealm: 6, stock: 25000 },
    { id: 'binh_son_an', name: 'Bình Sơn Ấn', slot: 'weapon', wtype: 'dinh', tier: 'dia', element: 'tho', icon: '🪨', stats: { atk: 180, hp: 850 }, drop: 0.002, srcMinRealm: 5, srcMaxRealm: 8, stock: 10000 },
    { id: 'cam_ma_hoan', name: 'Cấm Ma Hoàn', slot: 'acc', tier: 'thien', element: 'kim', icon: '⭕', stats: { def: 200, hp: 1400, sense: 45 }, drop: 0.0003, srcMinRealm: 7, srcMaxRealm: 13, stock: 2000 },
    { id: 'thien_a_than_kiem', name: 'Thiên A Thần Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'dia', element: 'kim', icon: '⚔️', stats: { atk: 205, spd: 10 }, drop: 0.0018, srcMinRealm: 5, srcMaxRealm: 9, stock: 8000 },
    { id: 'hac_huyet_nhan', name: 'Hắc Huyết Nhận', slot: 'weapon', wtype: 'kiem', tier: 'huyen', element: 'thuy', icon: '🗡️', stats: { atk: 86, spd: 5 }, drop: 0.004, srcMinRealm: 3, srcMaxRealm: 6, stock: 25000 },
    { id: 'tran_hai_chung', name: 'Trấn Hải Chung', slot: 'weapon', wtype: 'phapkhi', tier: 'thien', element: 'thuy', icon: '🔔', stats: { atk: 320, def: 100, hp: 1200 }, drop: 0.0003, srcMinRealm: 7, srcMaxRealm: 13, stock: 2000 },
    { id: 'can_lam_dinh', name: 'Càn Lam Đỉnh', slot: 'weapon', wtype: 'dinh', tier: 'thien', element: 'bang', icon: '🫖', stats: { atk: 290, hp: 1250, sense: 50 }, drop: 0.0003, srcMinRealm: 7, srcMaxRealm: 13, stock: 2000 },
    { id: 'huyen_ngoc_lenh_bai', name: 'Huyền Ngọc Lệnh Bài', slot: 'acc', tier: 'tien', element: 'bang', icon: '🏷️', stats: { def: 1200, hp: 12000, sense: 300 }, drop: 0.0002, srcMinRealm: 8, srcMaxRealm: 20, stock: 300 },
    { id: 'lam_kim_chuyen', name: 'Lam Kim Chuyên', slot: 'weapon', wtype: 'trongkhi', tier: 'thien', element: 'kim', icon: '🧱', stats: { atk: 350, hp: 1300 }, drop: 0.0003, srcMinRealm: 7, srcMaxRealm: 13, stock: 2000, desc: 'Bảo gạch Lam Kim trứ danh nện đâu nát đấy.' },
    { id: 'kim_khuyet_ngoc_thu', name: 'Kim Khuyết Ngọc Thư', slot: 'acc', tier: 'thien', element: 'kim', icon: '📜', stats: { atk: 250, def: 200, hp: 1600, sense: 60 }, drop: 0.0003, srcMinRealm: 7, srcMaxRealm: 15, stock: 2000, desc: 'Ngọc thư Tiên Giới truyền thừa công pháp và bí thuật thượng cổ.' },

    // ---- TRANG BỊ MA TU ĐỘC QUYỀN ----
    {"id":"ma_huyet_kiem","name":"Ma Huyết Kiếm","slot":"weapon","wtype":"kiem","tier":"huyen","element":"ma","icon":"🗡️","stats":{"atk":125,"crit":0.15},"demonOnly":true,"drop":0.003,"srcMinRealm":3,"srcMaxRealm":6,"stock":15000,"desc":"Ma kiếm tắm máu vạn địch trong Tiên Nghịch, sát khí ngút trời."},
    {"id":"van_hon_phien","name":"Vạn Hồn Phiên","slot":"weapon","wtype":"phapkhi","tier":"dia","element":"ma","icon":"🚩","stats":{"atk":260,"mp":650},"demonOnly":true,"drop":0.001,"srcMinRealm":5,"srcMaxRealm":8,"stock":5000,"desc":"Bảo vật cấm kỵ thu nạp ức vạn oan hồn của Vương Lâm, vung lên quỷ khóc thần gào."},
    {"id":"am_ma_giap","name":"U Minh Hắc Ma Giáp","slot":"armor","tier":"dia","element":"ma","icon":"🛡️","stats":{"def":190,"hp":1300},"demonOnly":true,"drop":0.001,"srcMinRealm":5,"srcMaxRealm":8,"stock":5000,"desc":"Áo giáp ngưng tụ từ hắc ma khí u minh bảo hộ thân thể đao thương bất nhập."},
    {"id":"u_minh_quy_trao","name":"U Minh Quỷ Vuốt","slot":"weapon","wtype":"quyensao","tier":"huyen","element":"ma","icon":"🦅","stats":{"atk":120,"spd":8},"demonOnly":true,"drop":0.003,"srcMinRealm":3,"srcMaxRealm":6,"stock":15000,"desc":"Móng vuốt quỷ u minh sắc bén xé rách linh hồn địch thủ."},
    {"id":"huyet_ma_ngoc_boi","name":"Huyết Ma Ngọc Bội","slot":"acc","tier":"dia","element":"ma","icon":"📿","stats":{"atk":110,"def":85,"hp":750},"demonOnly":true,"drop":0.0015,"srcMinRealm":5,"srcMaxRealm":8,"stock":6000,"desc":"Ngọc bội đỏ rực tỏa ra huyết quang nhiếp nhân tâm phách."},
    {"id":"van_cot_ho_phu","name":"Vạn Cốt Hộ Phù","slot":"acc","tier":"thien","element":"ma","icon":"🦴","stats":{"def":270,"hp":1900,"sense":55},"demonOnly":true,"drop":0.0003,"srcMinRealm":7,"srcMaxRealm":12,"stock":1500,"desc":"Bạch cốt vạn năm ngưng kết thành bùa hộ mệnh ma đạo chí bảo."},
    {"id":"ma_linh_dinh","name":"Cửu U Ma Linh Đỉnh","slot":"weapon","wtype":"dinh","tier":"dia","element":"ma","icon":"🫖","stats":{"atk":220,"hp":1050},"demonOnly":true,"drop":0.0012,"srcMinRealm":5,"srcMaxRealm":8,"stock":5000,"desc":"Ma đỉnh luyện chế huyết đan tà dược kinh thiên động địa."},
    // ---- 20 TRANG BỊ VÒNG (Đeo tại 2 ô Nhẫn / Vòng tăng chỉ số) ----
    { id: 'vong_thanh_dong', name: 'Vòng Thanh Đồng', slot: 'vong', tier: 'pham', element: 'kim', icon: '⭕', stats: { def: 8, hp: 60 }, drop: 0.035, srcMinRealm: 0, srcMaxRealm: 2, stock: null, desc: 'Chiếc vòng đúc bằng đồng xanh cổ kính, có khắc phù văn hộ thân đơn giản.' },
    { id: 'vong_tram_huong', name: 'Vòng Trầm Hương', slot: 'vong', tier: 'pham', element: 'moc', icon: '📿', stats: { mp: 50, sense: 3 }, drop: 0.035, srcMinRealm: 0, srcMaxRealm: 2, stock: null, desc: 'Vòng chuỗi hạt trầm hương trăm năm, tỏa hương thơm dịu giúp thanh tâm định khí.' },
    { id: 'vong_te_ngoc', name: 'Vòng Tế Ngọc', slot: 'vong', tier: 'pham', element: 'thuy', icon: '💫', stats: { hp: 50, def: 6, spd: 2 }, drop: 0.035, srcMinRealm: 0, srcMaxRealm: 2, stock: null, desc: 'Vòng ngọc bích nước trong veo, giúp dưỡng thể và tăng nhẹ tốc độ thân pháp.' },
    { id: 'vong_thiet_cot', name: 'Vòng Thiết Cốt', slot: 'vong', tier: 'pham', element: 'kim', icon: '⛓️', stats: { atk: 12, hp: 45 }, drop: 0.035, srcMinRealm: 0, srcMaxRealm: 2, stock: null, desc: 'Vòng rèn từ huyền thiết thô ráp, tăng thêm sức nặng cho quyền chưởng.' },

    { id: 'vong_bach_ngoc', name: 'Vòng Bạch Ngọc Dưỡng Thần', slot: 'vong', tier: 'hoang', element: 'thuy', icon: '⚪', stats: { hp: 160, sense: 8, mp: 80 }, drop: 0.015, srcMinRealm: 2, srcMaxRealm: 4, stock: 500, desc: 'Được tạc từ ngọc tủy ngàn năm tại Cực Bắc, ngưng tụ thần thức và dưỡng thần bổ khí.' },
    { id: 'vong_xich_nhiem', name: 'Vòng Xích Diễm Liệt Hỏa', slot: 'vong', tier: 'hoang', element: 'hoa', icon: '🔥', stats: { atk: 35, crit: 0.05, hp: 120 }, drop: 0.015, srcMinRealm: 2, srcMaxRealm: 4, stock: 500, desc: 'Vòng đúc từ quặng đồng núi lửa, luôn tỏa nhiệt khí bừng bừng kích phát sát thương đòn đánh.' },
    { id: 'vong_loi_phong', name: 'Vòng Lôi Phong Toàn', slot: 'vong', tier: 'hoang', element: 'loi', icon: '⚡', stats: { spd: 8, atk: 28, sense: 6 }, drop: 0.015, srcMinRealm: 2, srcMaxRealm: 4, stock: 500, desc: 'Mang theo khí tức phong lôi cuồn cuộn, giúp hành động chớp nhoáng như lôi điện.' },
    { id: 'vong_hoang_tho', name: 'Vòng Hoàng Thổ Hộ Thể', slot: 'vong', tier: 'hoang', element: 'tho', icon: '🟤', stats: { def: 35, hp: 220 }, drop: 0.015, srcMinRealm: 2, srcMaxRealm: 4, stock: 500, desc: 'Chứa trầm tích đại địa chi lực, tạo tầng thổ thuẫn kiên cố giảm chấn thương.' },

    { id: 'vong_am_duong', name: 'Vòng Âm Dương Lưỡng Nghi', slot: 'vong', tier: 'huyen', element: 'thuy', icon: '☯️', stats: { atk: 65, def: 55, hp: 350, mp: 150 }, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 100, desc: 'Hai nửa âm dương tương hỗ, cân bằng cương nhu, sinh sôi linh lực không dứt.' },
    { id: 'vong_bang_phach', name: 'Vòng Băng Phách Huyền Minh', slot: 'vong', tier: 'huyen', element: 'bang', icon: '❄️', stats: { def: 60, spd: 10, hp: 380, sense: 15 }, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 100, desc: 'Luyện chế từ huyền băng vạn trượng, tỏa hàn khí đông kết sát ý của đối thủ.' },
    { id: 'vong_tu_tieu_loi', name: 'Vòng Tử Tiêu Thần Lôi', slot: 'vong', tier: 'huyen', element: 'loi', icon: '⚡', stats: { atk: 85, crit: 0.08, sense: 18 }, drop: 0.004, srcMinRealm: 3, srcMaxRealm: 6, stock: 80, desc: 'Chứa lôi kiếp tử sắc tinh túy, mỗi đòn tấn công đều kèm theo lôi đình bạo kích.' },
    { id: 'vong_huyen_am', name: 'Vòng Cửu U Huyền Âm', slot: 'vong', tier: 'huyen', element: 'ma', reqMa: 50, icon: '🔮', stats: { atk: 75, def: 45, hp: 320, sense: 20 }, drop: 0.004, srcMinRealm: 3, srcMaxRealm: 6, stock: 80, desc: 'Cổ vật đoạt từ Cửu U ma uyên, kích phát ma khí khiến đối phương khiếp đảm.' },

    { id: 'vong_kim_cang', name: 'Vòng Kim Cang Phục Ma', slot: 'vong', tier: 'dia', element: 'kim', reqDao: 60, icon: '🟡', stats: { def: 140, hp: 900, atk: 90, sense: 25 }, drop: 0.001, srcMinRealm: 5, srcMaxRealm: 8, stock: 30, desc: 'Bảo vật Phật môn Kim Cang Tông, bất hoại chi thân, trừ tà trảm yêu trấn áp ma chướng.' },
    { id: 'vong_phong_vu_cuc', name: 'Vòng Cực Phong Vạn Dặm', slot: 'vong', tier: 'dia', element: 'phong', icon: '🌪️', stats: { spd: 22, atk: 130, hp: 750, sense: 30 }, drop: 0.001, srcMinRealm: 5, srcMaxRealm: 8, stock: 30, desc: 'Ngưng kết từ phong nhãn cuồng phong dã ngoại, di chuyển phiêu dật vô tung vô ảnh.' },
    { id: 'vong_huyet_ngoc', name: 'Vòng Huyết Ngọc Thôn Linh', slot: 'vong', tier: 'dia', element: 'ma', reqMa: 80, icon: '🩸', stats: { atk: 160, hp: 850, crit: 0.10 }, drop: 0.001, srcMinRealm: 5, srcMaxRealm: 8, stock: 30, desc: 'Huyết sắc lấp lánh như máu tươi, thôn phệ huyết khí đối thủ để bổ dưỡng chủ nhân.' },
    { id: 'vong_can_khon', name: 'Vòng Càn Khôn Đảo Chuyển', slot: 'vong', tier: 'dia', element: 'thien', reqDao: 80, icon: '🪐', stats: { atk: 120, def: 120, hp: 1000, mp: 400, sense: 35 }, drop: 0.001, srcMinRealm: 5, srcMaxRealm: 8, stock: 30, desc: 'Bảo hoàn nghịch chuyển Càn Khôn, linh khí cương nhu hòa quyện đỉnh phong.' },

    { id: 'vong_ngu_sac_than', name: 'Vòng Ngũ Sắc Thần Hoàn', slot: 'vong', tier: 'thien', element: 'thien', icon: '🌈', stats: { atk: 320, def: 240, hp: 2000, sense: 55, spd: 15 }, drop: 0.0002, srcMinRealm: 7, srcMaxRealm: 13, stock: 10, desc: 'Hào quang ngũ sắc rực rỡ trời đất, bao quát quy luật Ngũ Hành tương sinh tương khắc.' },
    { id: 'vong_cuu_long', name: 'Vòng Cửu Long Phệ Thiên', slot: 'vong', tier: 'thien', element: 'kim', icon: '🐲', stats: { atk: 420, def: 200, hp: 2200, crit: 0.12, sense: 50 }, drop: 0.00015, srcMinRealm: 7, srcMaxRealm: 13, stock: 10, desc: 'Điêu khắc hình tượng cửu long gầm thét, ẩn chứa chân long uy áp chấn nhiếp chư thiên.' },

    { id: 'vong_hon_don', name: 'Vòng Hỗn Độn Vô Cực', slot: 'vong', tier: 'tien', element: 'thien', reqDao: 120, icon: '🌌', stats: { atk: 900, def: 650, hp: 4500, mp: 1800, spd: 25, sense: 100 }, drop: 0.00002, srcMinRealm: 10, srcMaxRealm: 20, stock: 5, desc: 'Hỗn Độn sơ khai chí bảo, vạn pháp bất xâm, lực phá hư không chấn động Cửu Châu.' },
    { id: 'vong_thai_hu_than', name: 'Vòng Thái Hư Trấn Giới', slot: 'vong', tier: 'tien', element: 'thien', icon: '👑', stats: { atk: 1200, def: 850, hp: 5500, mp: 2200, spd: 30, sense: 130, crit: 0.15 }, drop: 0.00001, srcMinRealm: 11, srcMaxRealm: 20, stock: 2, desc: 'Tiên Thiên Linh Bảo vô thượng, phong ấn một góc Thái Hư thế giới, trấn áp chư thiên vạn giới.' },
    { id: 'nhan_na_di', name: 'Nhẫn Na Di Vạn Giới', slot: 'vong', tier: 'tien', element: 'thien', icon: '🌀', stats: { atk: 1800, def: 1300, hp: 9000, mp: 4500, spd: 40, sense: 180 }, drop: 0.00002, srcMinRealm: 31, srcMaxRealm: 65, worldBossOnly: true, bossOnly: true, teleportRing: true, superRare: true, stock: null, desc: 'Siêu phẩm cực hiếm chỉ rơi từ Boss Thế Giới. Dịch chuyển miễn phí giữa các thành cùng giới; mỗi lần dùng mất 1/100 độ bền và hồi chiêu 30 phút.' },
    // Binh khí chuyên tu cho các hệ phái ngoài Kiếm tu, cân bằng từ Huyền đến Thiên phẩm.
    { id: 'phap_tu_linh_phien', name: 'Tụ Linh Pháp Phiên', slot: 'weapon', wtype: 'phapkhi', tier: 'huyen', element: 'phong', icon: '🚩', stats: { atk: 76, mp: 175, sense: 14 }, reqRoleStat: 8, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 18000 },
    { id: 'phap_van_tinh_kiem', name: 'Vạn Tinh Pháp Kiếm', slot: 'weapon', wtype: 'phapkhi', tier: 'dia', element: 'thien', icon: '✨', stats: { atk: 188, mp: 420, sense: 30 }, reqRoleStat: 24, drop: 0.0018, srcMinRealm: 5, srcMaxRealm: 9, stock: 7000 },
    { id: 'the_tran_nhac_chuy', name: 'Trấn Nhạc Cự Chùy', slot: 'weapon', wtype: 'trongkhi', tier: 'huyen', element: 'tho', icon: '🔨', stats: { atk: 70, hp: 330, def: 30 }, reqRoleStat: 8, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 18000 },
    { id: 'the_khai_thien_phu', name: 'Khai Thiên Cự Phủ', slot: 'weapon', wtype: 'trongkhi', tier: 'dia', element: 'hoa', icon: '🪓', stats: { atk: 195, hp: 780, def: 42 }, reqRoleStat: 24, drop: 0.0018, srcMinRealm: 5, srcMaxRealm: 9, stock: 7000 },
    { id: 'quyen_bach_huyen_thu', name: 'Bách Luyện Quyền Sáo', slot: 'weapon', wtype: 'quyensao', tier: 'huyen', element: 'kim', icon: '🥊', stats: { atk: 82, spd: 5, def: 20 }, reqRoleStat: 8, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 18000 },
    { id: 'quyen_tinh_hai', name: 'Tinh Hải Chấn Quyền', slot: 'weapon', wtype: 'quyensao', tier: 'dia', element: 'thuy', icon: '👊', stats: { atk: 202, spd: 9, hp: 500 }, reqRoleStat: 24, drop: 0.0018, srcMinRealm: 5, srcMaxRealm: 9, stock: 7000 },
    { id: 'dan_tu_hon_linh_dinh', name: 'Tụ Hồn Linh Đỉnh', slot: 'weapon', wtype: 'dinh', tier: 'huyen', element: 'moc', icon: '🫖', stats: { atk: 68, mp: 150, sense: 24 }, reqRoleStat: 8, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 18000 },
    { id: 'dan_thien_moc_bao_lo', name: 'Thiên Mộc Bảo Lô', slot: 'weapon', wtype: 'dinh', tier: 'dia', element: 'hoa', icon: '🏺', stats: { atk: 170, mp: 390, sense: 45 }, reqRoleStat: 24, drop: 0.0018, srcMinRealm: 5, srcMaxRealm: 9, stock: 7000 },
    { id: 'phu_linh_phong_but', name: 'Linh Phong Đạo Bút', slot: 'weapon', wtype: 'but', tier: 'huyen', element: 'phong', icon: '🖌️', stats: { atk: 72, mp: 175, sense: 22 }, reqRoleStat: 8, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 18000 },
    { id: 'phu_thien_phu_than_but', name: 'Thiên Phù Thần Bút', slot: 'weapon', wtype: 'but', tier: 'dia', element: 'thien', icon: '🖋️', stats: { atk: 180, mp: 460, sense: 38 }, reqRoleStat: 24, drop: 0.0018, srcMinRealm: 5, srcMaxRealm: 9, stock: 7000 },
    { id: 'ren_huyet_loan_chuy', name: 'Huyết Loan Đoán Chùy', slot: 'weapon', wtype: 'bua', tier: 'huyen', element: 'hoa', icon: '🔨', stats: { atk: 80, hp: 260, def: 24 }, reqRoleStat: 8, drop: 0.005, srcMinRealm: 3, srcMaxRealm: 6, stock: 18000 },
    { id: 'ren_van_tuong_thien_cong', name: 'Vạn Tượng Thiên Công Chùy', slot: 'weapon', wtype: 'bua', tier: 'dia', element: 'kim', icon: '⚒️', stats: { atk: 198, hp: 720, def: 55 }, reqRoleStat: 24, drop: 0.0018, srcMinRealm: 5, srcMaxRealm: 9, stock: 7000 },
    // Pháp bảo mới được Việt hóa từ các mục chưa có trong bách khoa PDF.
    { id: 'am_gia_kiem', name: 'Ẩm Giả Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'hoang', element: 'thuy', icon: '🗡️', stats: { atk: 42, spd: 3 }, drop: 0.001, srcMinRealm: 2, srcMaxRealm: 4, stock: 5000 },
    { id: 'sat_giao_kiem', name: 'Sát Giao Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'huyen', element: 'kim', icon: '⚔️', stats: { atk: 82, spd: 4 }, drop: 0.0008, srcMinRealm: 4, srcMaxRealm: 7, stock: 2400 },
    { id: 'tram_kham_dao', name: 'Trảm Khám Đao', slot: 'weapon', wtype: 'trongkhi', tier: 'huyen', element: 'hoa', icon: '🗡️', stats: { atk: 88, def: 18 }, drop: 0.0007, srcMinRealm: 4, srcMaxRealm: 7, stock: 2000 },
    { id: 'kiem_sao_tinh_la', name: 'Tinh La Kiếm Sao', slot: 'acc', tier: 'huyen', element: 'phong', icon: '📿', stats: { spd: 7, sense: 15, hp: 180 }, drop: 0.0006, srcMinRealm: 5, srcMaxRealm: 8, stock: 1800 },
    { id: 'con_cuc_tien', name: 'Côn Cực Tiên', slot: 'weapon', wtype: 'phapkhi', tier: 'dia', element: 'thuy', icon: '🪄', stats: { atk: 165, mp: 240, sense: 24 }, reqRoleStat: 10, drop: 0.0006, srcMinRealm: 7, srcMaxRealm: 11, stock: 700 },
    { id: 'thanh_quang_thuan', name: 'Thanh Quang Thuẫn', slot: 'armor', tier: 'dia', element: 'thien', icon: '🛡️', stats: { def: 138, hp: 620 }, drop: 0.0007, srcMinRealm: 7, srcMaxRealm: 11, stock: 800 },
    { id: 'diem_tien_but', name: 'Điểm Tiên Bút', slot: 'weapon', wtype: 'but', tier: 'dia', element: 'thien', icon: '🖋️', stats: { atk: 170, mp: 300, sense: 45 }, reqRoleStat: 12, drop: 0.0005, srcMinRealm: 8, srcMaxRealm: 12, stock: 500 },
    { id: 'co_than_dinh', name: 'Cổ Thần Đỉnh', slot: 'weapon', wtype: 'dinh', tier: 'thien', element: 'tho', icon: '⚱️', stats: { atk: 290, def: 92, hp: 700 }, reqRoleStat: 18, drop: 0.00035, srcMinRealm: 12, srcMaxRealm: 16, stock: 180 },
    { id: 'hoa_ban_nguyen_kiem', name: 'Hỏa Bản Nguyên Chi Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'thien', element: 'hoa', icon: '🔥', stats: { atk: 330, spd: 12 }, drop: 0.0003, srcMinRealm: 13, srcMaxRealm: 17, stock: 120 },
    { id: 'nhan_qua_kiem', name: 'Nhân Quả Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'tien', element: 'thien', icon: '☯️', stats: { atk: 6800, sense: 800, spd: 25 }, elite: true, drop: 0.00012, srcMinRealm: 18, srcMaxRealm: 23, stock: 40 },
    { id: 'sinh_tu_kiem', name: 'Sinh Tử Kiếm', slot: 'weapon', wtype: 'kiem', tier: 'tien', element: 'ma', icon: '⚔️', stats: { atk: 14000, hp: 30000, spd: 30 }, elite: true, drop: 0.00008, srcMinRealm: 23, srcMaxRealm: 30, stock: 20 },
    { id: 'am_nguyet_dao', name: 'Âm Nguyệt Đao', slot: 'weapon', wtype: 'trongkhi', tier: 'dia', element: 'ma', icon: '🌙', stats: { atk: 188, hp: 300, spd: 7 }, drop: 0.00055, srcMinRealm: 8, srcMaxRealm: 12, stock: 400 },
    { id: 'ren_khai_son_chuy', name: 'Khai Sơn Huyền Chùy', slot: 'weapon', wtype: 'bua', tier: 'hoang', element: 'tho', icon: '🔨', stats: { atk: 55, def: 18, hp: 120 }, reqRoleStat: 8, drop: 0.002, srcMinRealm: 2, srcMaxRealm: 5, stock: 9000 },
    { id: 'ren_thien_cong_chuy', name: 'Thiên Công Phá Giáp Chùy', slot: 'weapon', wtype: 'bua', tier: 'huyen', element: 'kim', icon: '⚒️', stats: { atk: 112, def: 35, hp: 250 }, reqRoleStat: 12, drop: 0.001, srcMinRealm: 5, srcMaxRealm: 9, stock: 3500 },
    { id: 'dan_van_linh_dinh', name: 'Vạn Linh Luyện Đan Đỉnh', slot: 'weapon', wtype: 'dinh', tier: 'huyen', element: 'moc', icon: '🏺', stats: { atk: 68, mp: 180, sense: 24 }, reqRoleStat: 8, drop: 0.001, srcMinRealm: 4, srcMaxRealm: 8, stock: 3000 },
    { id: 'lo_dinh_pham', name: 'Thanh Đồng Lô Đỉnh', slot: 'lo_dinh', tier: 'pham', icon: '🏺', stats: { def: 8 }, alchemyRate: 0.62, desc: 'Lô đỉnh nhập môn; năng lực nền 62%, đan phẩm cao làm giảm tỷ lệ thực tế.', drop: 0.01, srcMinRealm: 0, srcMaxRealm: 3, stock: 5000 },
    { id: 'lo_dinh_hoang', name: 'Bách Thảo Hoàng Đỉnh', slot: 'lo_dinh', tier: 'hoang', icon: '⚱️', stats: { def: 25, sense: 5 }, alchemyRate: 0.68, desc: 'Lô đỉnh Hoàng phẩm; năng lực nền 68%, tỷ lệ còn phụ thuộc bậc đan.', drop: 0.006, srcMinRealm: 2, srcMaxRealm: 5, stock: 2000 },
    { id: 'lo_dinh_huyen', name: 'Tử Kim Huyền Đỉnh', slot: 'lo_dinh', tier: 'huyen', icon: '⚱️', stats: { def: 70, sense: 14 }, alchemyRate: 0.74, desc: 'Lô đỉnh Huyền phẩm; năng lực nền 74%, tỷ lệ còn phụ thuộc bậc đan.', drop: 0.0025, srcMinRealm: 4, srcMaxRealm: 8, stock: 900 },
    { id: 'lo_dinh_dia', name: 'Cửu Long Địa Hỏa Đỉnh', slot: 'lo_dinh', tier: 'dia', icon: '🏺', stats: { def: 180, hp: 350, sense: 30 }, alchemyRate: 0.80, desc: 'Lô đỉnh Địa phẩm; năng lực nền 80%, tỷ lệ còn phụ thuộc bậc đan.', drop: 0.001, srcMinRealm: 7, srcMaxRealm: 13, stock: 350 },
    { id: 'lo_dinh_thien', name: 'Thái Sơ Thiên Đỉnh', slot: 'lo_dinh', tier: 'thien', icon: '⚱️', stats: { def: 420, hp: 900, sense: 55 }, alchemyRate: 0.86, desc: 'Lô đỉnh Thiên phẩm; năng lực nền 86%, đan phẩm cao vẫn khó luyện thành.', drop: 0.00035, elite: true, srcMinRealm: 12, srcMaxRealm: 20, stock: 80 },
    { id: 'lo_dinh_tien', name: 'Hỗn Độn Tiên Lô', slot: 'lo_dinh', tier: 'tien', icon: '🌌', stats: { def: 2500, hp: 16000, sense: 400 }, alchemyRate: 0.91, desc: 'Tiên lô Hỗn Độn; năng lực nền 91%, không bảo đảm thành công với đạo đan tối cao.', drop: 0.00008, elite: true, srcMinRealm: 18, srcMaxRealm: 30, stock: 20 },
    { id: 'toa_ky_bach_van', name: 'Bạch Vân Linh Hạc', slot: 'phi_kiem', tier: 'hoang', icon: '🕊️', stats: { spd: 4, sense: 5 }, flySpeed: 0.2, mount: true, bossOnly: true, drop: 0.025, srcMinRealm: 1, srcMaxRealm: 5, stock: 900 },
    { id: 'toa_ky_linh_huou', name: 'Thanh Giác Linh Lộc', slot: 'phi_kiem', tier: 'huyen', icon: '🦌', stats: { spd: 8, hp: 100 }, flySpeed: 0.27, mount: true, bossOnly: true, drop: 0.012, srcMinRealm: 4, srcMaxRealm: 10, stock: 400 },
    { id: 'toa_ky_loan_phuong', name: 'Cửu Sắc Loan Phượng', slot: 'phi_kiem', tier: 'dia', icon: '🦚', stats: { spd: 14, atk: 75 }, flySpeed: 0.34, mount: true, bossOnly: true, drop: 0.005, elite: true, srcMinRealm: 9, srcMaxRealm: 18, stock: 120 },
    { id: 'toa_ky_ky_lan', name: 'Thái Cổ Kỳ Lân', slot: 'phi_kiem', tier: 'thien', icon: '🦄', stats: { spd: 22, hp: 800, def: 110 }, flySpeed: 0.42, mount: true, bossOnly: true, drop: 0.001, elite: true, src: ['cung_ky_hoang_thu'], srcMinRealm: 15, srcMaxRealm: 30, stock: 30 },
    ...ROLE_GEAR,
    ...ALCHEMY_CAULDRONS,
    ...FORGE_GEAR,
    ...WORLD_MOUNTS,
    ...FLYING_SWORDS,
    ...IMMORTAL_MAP_GEAR,
]);

// Vật phẩm tiêu hao (Đan dược, phù lục chuẩn Phàm Nhân Tu Tiên, Tiên Nghịch, Kiếm Lai)
// Mỗi cảnh giới có một đan tiểu cảnh dùng chung cho các lần đột phá tiểu cảnh.
const SUBSTAGE_BREAKTHROUGH_ITEMS = Object.freeze(CULTIVATION_REALM_NAMES.slice(0, -1).map((realmName, realm) => ({
    id: `dan_tieu_canh_${realm}`, name: `${realmName} Tiểu Cảnh Đan`,
    price: 2500 + realm * 1800, battle: false, breakthrough: true, substageBreakthrough: true,
    realmMin: realm, icon: ['🟢', '🔵', '🟣', '🟠', '🔴', '💠'][Math.min(5, Math.floor(realm / 11))],
    desc: `Đan dược chuyên dụng giúp vượt bình cảnh tiểu cảnh trong ${realmName}; không thể thay thế đan đột phá đại cảnh giới.`,
})));
const SUBSTAGE_BY_REALM = Object.freeze(Object.fromEntries(SUBSTAGE_BREAKTHROUGH_ITEMS.map(item => [item.realmMin, item])));
const CONSUMABLES = Object.freeze([
    { id: 'phu_thien_co_truy_tung', name: 'Thiên Cơ Truy Tung Phù', price: 1200, battle: false, icon: '🧭', talisman: true, locateMonster: true, locatePlayers: true, desc: 'Dò tìm yêu thú, boss đang xuất hiện, Cổ Động còn mở và vị trí thành trấn của người chơi.' },
    { id: 'hoi_xuan_dan', name: 'Hồi Xuân Đan', price: 1000, battle: true, icon: '💊', heal: 0.35, desc: 'Hồi 35% khí huyết trong trận hoặc ngoài trận' },
    { id: 'boi_nguyen_dan', name: 'Bồi Nguyên Đan', price: 1350, battle: true, icon: '🧪', heal: 0.65, desc: 'Hồi 65% khí huyết trong trận hoặc ngoài trận' },
    { id: 'hoi_linh_dan', name: 'Hồi Linh Đan', price: 1000, battle: true, icon: '💧', mana: 0.4, desc: 'Hồi 40% linh lực trong trận' },
    { id: 'truc_co_dan', name: 'Trúc Cơ Đan', price: 2700, battle: false, icon: '💊', breakthrough: true, toRealm: 3, exp: 500, desc: 'Thần đan phá cảnh của Hàn Lập, dùng đột phá Trúc Cơ hoặc tăng 500 EXP tu vi' },
    { id: 'dan_truc_co', name: 'Trúc Cơ Đan', price: 2700, battle: false, icon: '💊', breakthrough: true, toRealm: 3, exp: 500, desc: 'Thần đan phá cảnh của Hàn Lập, dùng đột phá Trúc Cơ hoặc tăng 500 EXP tu vi' },
    { id: 'tay_tuy_dan', name: 'Tẩy Tủy Đan', price: 6000, battle: false, icon: '🧪', desc: 'Tẩy kinh phạt tủy, gia tăng 10% tu vi của tiểu cảnh hiện tại' },
    { id: 'dinh_nhan_dan', name: 'Định Nhan Đan', price: 3750, battle: false, icon: '🌸', intimacy: 100, desc: 'Giữ dung nhan bất lão, tặng cho đạo lữ tăng ngay 100 điểm thân mật' },
    { id: 'cuu_chuyen_dan', name: 'Cửu Chuyển Hồi Hồn Đan', price: 7500, battle: true, icon: '✨', heal: 1.0, cleanse: true, desc: 'Hồi 100% Khí Huyết và xóa bỏ hoàn toàn trạng thái trọng thương' },
    { id: 'kim_cang_phu', name: 'Kim Cang Phù', price: 1000, battle: true, icon: '🛡️', shield: 0.15, desc: 'Khiên bằng 15% khí huyết' },
    { id: 'hoa_cau_phu', name: 'Hỏa Cầu Phù', price: 1000, battle: true, icon: '🔥', burst: 1.5, desc: 'Gây 150% công, hệ Hỏa' },
    { id: 'don_phu', name: 'Độn Phù', price: 2250, battle: true, icon: '💨', safeFlee: true, desc: 'Rút khỏi trận săn không bị phạt' },
    { id: 'hoi_luc_dan', name: 'Hồi Lực Đan', price: 1800, battle: false, icon: '⚡', stamina: 40, dailyMax: 50, desc: 'Hồi 40 thể lực, tối đa 50 lần dùng mỗi ngày' },

    // Đan dược Đột Phá Cảnh Giới (rơi từ quái, Boss và Cổ Động Bí Cảnh) - 31 Cảnh Giới
    { id: 'dan_tay_tuy_hoan', name: 'Tẩy Tủy Hoàn', price: 1000, battle: false, breakthrough: true, toRealm: 1, icon: '💊', desc: 'Đan dược gột rửa phàm cốt, giúp phàm nhân bước vào Luyện Thể cảnh' },
    { id: 'dan_pha_chuong', name: 'Phá Chướng Đan', price: 2000, battle: false, breakthrough: true, toRealm: 2, icon: '🧪', desc: 'Phá vỡ chướng ngại kinh mạch, đả thông khí hải tiến vào Luyện Khí' },
    { id: 'dan_giang_tran', name: 'Giáng Trần Đan', price: 6000, battle: false, breakthrough: true, toRealm: 4, icon: '🌟', desc: 'Linh đan ngưng kết Kim Đan cửu chuyển trong Phàm Nhân Tu Tiên' },
    { id: 'dan_ket_anh', name: 'Kết Anh Đan', price: 12000, battle: false, breakthrough: true, toRealm: 5, icon: '✨', desc: 'Đan dược phá toái Kim Đan hóa thành Nguyên Anh thần thông quảng đại' },
    { id: 'dan_hoa_than', name: 'Hóa Thần Đan', price: 25000, battle: false, breakthrough: true, toRealm: 6, icon: '🔮', desc: 'Thần đan đốn ngộ sinh tử ý cảnh trong Tiên Nghịch của Vương Lâm, bước vào Hóa Thần' },
    { id: 'dan_ngu_hanh_lo', name: 'Ngũ Hành Linh Lộ', price: 50000, battle: false, breakthrough: true, toRealm: 7, icon: '💧', desc: 'Tinh hoa ngũ hành tương sinh, giúp dung hợp thiên địa nguyên khí tiến vào Luyện Hư' },
    { id: 'dan_co_than_tinh_huyet', name: 'Cổ Thần Tinh Huyết', price: 100000, battle: false, breakthrough: true, toRealm: 8, icon: '🩸', desc: 'Giọt tinh huyết Cổ Thần bát tinh, rèn đúc thần thể tiến vào Hợp Thể kỳ' },
    { id: 'dan_dai_thua', name: 'Đại Thừa Vấn Đạo Đan', price: 200000, battle: false, breakthrough: true, toRealm: 9, icon: '👑', desc: 'Vấn đạo thiên địa, lĩnh ngộ pháp tắc quy tắc, thành tựu Đại Thừa tôn sư' },
    { id: 'dan_do_kiep', name: 'Độ Kiếp Thần Đan', price: 400000, battle: false, breakthrough: true, toRealm: 10, icon: '⚡', desc: 'Nghịch thiên kháng lôi kiếp, tẩy lễ thân hồn tiến vào Độ Kiếp kỳ' },
    { id: 'dan_ban_tien', name: 'Bán Tiên Hóa Cốt Đan', price: 600000, battle: false, breakthrough: true, toRealm: 11, icon: '✨', desc: 'Bước đầu thoát thai hoán cốt khỏi phàm trần, chạm tay tới Tiên đạo sơ cảnh' },
    { id: 'dan_dang_tien', name: 'Đăng Tiên Lôi Tâm Đan', price: 700000, battle: false, breakthrough: true, toRealm: 12, icon: '🌠', desc: 'Lôi tâm ngưng tụ qua chín tầng thiên kiếp, mở cánh cửa Đăng Tiên.' },
    { id: 'dan_bo_thien', name: 'Bổ Thiên Thần Đan', price: 800000, battle: false, breakthrough: true, toRealm: 13, icon: '🌌', desc: 'Đan dược đúc lại tiên thể từ tro tàn lôi kiếp, chứng đạo Tán Tiên' },
    { id: 'dan_dia_tien', name: 'Địa Tiên Ngưng Thể Đan', price: 1000000, battle: false, breakthrough: true, toRealm: 14, icon: '⛰️', desc: 'Hấp thu địa mạch phúc địa tiên khí, ngưng tụ Địa Tiên pháp thân' },
    { id: 'dan_nhan_tien', name: 'Nhân Tiên Thuần Dương Dịch', price: 1300000, battle: false, breakthrough: true, toRealm: 15, icon: '☀️', desc: 'Thuần dương tiên khí gột rửa linh hồn, thành tựu Nhân Tiên tôn vị' },
    { id: 'dan_chan_tien', name: 'Chân Tiên Hóa Cảnh Đan', price: 1600000, battle: false, breakthrough: true, toRealm: 16, icon: '💎', desc: 'Tiên đan ngưng tụ Chân Tiên pháp tắc, đạp phá trần gian phi thăng Tiên Giới' },
    { id: 'dan_huyen_tien', name: 'Huyền Tiên Ngọc Tủy', price: 2000000, battle: false, breakthrough: true, toRealm: 17, icon: '💠', desc: 'Ngọc tủy huyền diệu quán triệt huyền tiên chi lực' },
    { id: 'dan_thien_tien', name: 'Thiên Tiên Dịch', price: 2500000, battle: false, breakthrough: true, toRealm: 18, icon: '🍃', desc: 'Linh dịch hấp thụ thiên tiên khí, đột phá Thiên Tiên cảnh' },
    { id: 'dan_kim_tien', name: 'Kim Tiên Thái Ất Đan', price: 3200000, battle: false, breakthrough: true, toRealm: 19, icon: '🟡', desc: 'Bất hủ kim tính, chứng đạo Kim Tiên bất diệt' },
    { id: 'dan_thai_at_chan_tien', name: 'Thái Ất Chân Tiên Lộ', price: 4000000, battle: false, breakthrough: true, toRealm: 20, icon: '🌀', desc: 'Giọt sương Thái Ất đốn ngộ Chân Tiên đạo, siêu việt phàm trần' },
    { id: 'dan_thai_at_huyen_tien', name: 'Thái Ất Huyền Tiên Tinh', price: 5000000, battle: false, breakthrough: true, toRealm: 21, icon: '🔮', desc: 'Huyền tiên tinh hoa huyền ảo, phá vỡ hư không bước vào Thái Ất Huyền Tiên' },
    { id: 'dan_thai_at', name: 'Thái Ất Hỗn Nguyên Đan', price: 6500000, battle: false, breakthrough: true, toRealm: 22, icon: '☯️', desc: 'Hỗn nguyên quy nhất, thành tựu Thái Ất Kim Tiên' },
    { id: 'dan_dai_la_chan_tien', name: 'Đại La Chân Tiên Quả', price: 8000000, battle: false, breakthrough: true, toRealm: 23, icon: '🍏', desc: 'Tiên quả đơm hoa nơi vĩnh hằng, mở ra con đường Đại La vô lượng' },
    { id: 'dan_dai_la', name: 'Đại La Đạo Quả', price: 10000000, battle: false, breakthrough: true, toRealm: 24, icon: '🍎', desc: 'Đạo quả viên mãn, siêu thoát thời không luân hồi' },
    { id: 'dan_hon_nguyen', name: 'Hỗn Nguyên Kim Tiên Thai', price: 13000000, battle: false, breakthrough: true, toRealm: 25, icon: '🥚', desc: 'Đạo thai Hỗn Nguyên sơ khai, ngưng tụ pháp tắc vô thượng' },
    { id: 'dan_tien_quan', name: 'Tiên Quân Pháp Tắc Đan', price: 16000000, battle: false, breakthrough: true, toRealm: 26, icon: '⚔️', desc: 'Thống ngự tiên binh, xưng hào Tiên Quân uy chấn bát hoang' },
    { id: 'dan_tien_ton', name: 'Tiên Tôn Hóa Đạo Châu', price: 20000000, battle: false, breakthrough: true, toRealm: 27, icon: '🔮', desc: 'Viên ngọc Tiên Tôn hóa đạo, chuyển dời muôn vàn tinh cầu' },
    { id: 'dan_chuan_tien_vuong', name: 'Chuẩn Tiên Vương Huyết Tinh', price: 25000000, battle: false, breakthrough: true, toRealm: 28, icon: '🩸', desc: 'Tinh huyết vương giả cổ xưa, nửa bước vào cảnh giới Vương Giả' },
    { id: 'dan_tien_vuong', name: 'Chân Linh Vương Tinh', price: 30000000, battle: false, breakthrough: true, toRealm: 29, icon: '👑', desc: 'Tinh hoa vương giả tiên giới, xưng vương chư thiên' },
    { id: 'dan_tien_de', name: 'Hỗn Độn Tiên Đế Thai', price: 40000000, battle: false, breakthrough: true, toRealm: 30, icon: '🌌', desc: 'Hỗn độn sơ khai, ngự trị vạn giới, Tiên Đế vô thượng' },
    ...POST_TIEN_DE_REALMS.map((realmName, index) => {
        const toRealm = index + 31;
        const id = `dan_hau_tien_de_${toRealm}`;
        return { id, name: `${realmName} Đạo Đan`, price: 50000000 + index * 3000000, battle: false, breakthrough: true, toRealm, icon: '💠', desc: `Đạo đan ngưng tụ pháp tắc ${realmName}, dùng để đột phá cảnh giới hậu Tiên Đế.` };
    }),
    ...SUBSTAGE_BREAKTHROUGH_ITEMS,

    // ==================== BÁCH KHOA LINH ĐAN: PHÀM NHÂN, TIÊN NGHỊCH, KIẾM LAI ====================
    { id: 'dan_hoang_long', name: 'Hoàng Long Đan', price: 1200, battle: false, icon: '💊', exp: 350, desc: 'Linh đan nhập môn nổi tiếng của Mặc đại phu và Hàn Lập, tăng 350 EXP tu vi.' },
    { id: 'dan_kim_tuy', name: 'Kim Tủy Hoàn', price: 2250, battle: false, icon: '🟡', exp: 700, desc: 'Dược hoàn tôi luyện cốt tủy, tăng 700 EXP tu vi.' },
    { id: 'dan_thanh_linh', name: 'Thanh Linh Tán', price: 7500, battle: true, icon: '✨', heal: 0.85, cleanse: true, desc: 'Tiên dược thanh lọc độc tố kinh mạch, hồi 85% HP và xóa trọng thương.' },
    { id: 'dan_duong_tinh', name: 'Dưỡng Tinh Đan', price: 4500, battle: false, icon: '🧪', stamina: 50, dailyMax: 50, desc: 'Bổ sung tinh khí thần, chỉ hồi 50 thể lực, tối đa 50 lần dùng mỗi ngày.' },
    { id: 'dan_hu_tam', name: 'Hủ Tâm Hoàn', price: 3750, battle: true, icon: '🖤', desc: 'Độc đan làm suy yếu phòng ngự đối phương trong trận.' },
    { id: 'dan_hoang_lat', name: 'Hoàng Lật Hoàn', price: 1500, battle: false, icon: '🌰', exp: 4000, desc: 'Đan dược trân quý của tu sĩ Nguyên Anh, tăng 4000 EXP tu vi.' },
    { id: 'dan_hop_hoan', name: 'Hợp Hoan Đan', price: 9000, battle: false, icon: '💕', intimacy: 250, desc: 'Linh đan gắn kết duyên nợ, tặng đạo lữ tăng ngay 250 thân mật.' },
    { id: 'dan_vong_tran', name: 'Vong Trần Đan', price: 15000, battle: false, icon: '🌌', exp: 5000, cleanse: true, desc: 'Thần đan dứt bỏ hồng trần, tăng mạnh mẽ 5000 EXP tu vi.' },
    { id: 'dan_ngu_hanh_huyet_ngung', name: 'Ngũ Hành Huyết Ngưng Đan', price: 8000, battle: false, icon: '🩸', heal: 1.0, exp: 5000, desc: 'Đan dược tà dị ngưng kết tinh hoa ngũ hành, tăng 5000 EXP tu vi.' },
    { id: 'dan_chan_nguyen', name: 'Chân Nguyên Đan', price: 5250, battle: true, icon: '🌀', mana: 0.7, heal: 0.35, desc: 'Hồi phục 70% linh lực và 35% khí huyết trong trận.' },
    { id: 'dan_tu_linh', name: 'Tụ Linh Đan', price: 9000, battle: false, icon: '💠', exp: 1800, desc: 'Tụ hội thiên địa linh khí, tăng 1800 EXP tu vi.' },
    { id: 'dan_luyen_khi_tan', name: 'Luyện Khí Tán', price: 1500, battle: false, icon: '💨', exp: 450, desc: 'Dược tán trợ lực Luyện Khí kỳ, tăng 450 EXP tu vi.' },
    { id: 'dan_boi_anh', name: 'Bồi Anh Đan', price: 4000, battle: false, icon: '👶', exp: 5000, desc: 'Bồi dưỡng Nguyên Anh vững chắc, gia tăng 5000 EXP tu vi.' },
    { id: 'dan_thanh_hu', name: 'Thanh Hư Đan', price: 12000, battle: false, icon: '🍃', exp: 5000, desc: 'Tiên đan hóa giải trọc khí, bứt phá 5000 EXP tu vi.' },
    { id: 'dan_van_nam_linh_nhu', name: 'Vạn Năm Linh Nhũ', price: 3000, battle: true, icon: '🥛', heal: 1.0, mana: 1.0, desc: 'Bảo vật hồi phục 100% Khí Huyết và Linh Lực tức khắc trong trận.' },
    { id: 'dan_hoi_duong_chan_thuy', name: 'Hồi Dương Chân Thủy', price: 5000, battle: true, icon: '💧', heal: 0.9, cleanse: true, desc: 'Thần thủy khởi tử hồi sinh, hồi 90% HP và trừ bỏ trọng thương.' },
    // ==================== BÁCH KHOA PHÙ LỤC & BÙA CHÚ (50 PHÙ LỤC THẦN THÔNG) ====================
    {"id":"phu_binh_an","name":"Bình An Phù","tier":"pham","price": 1000,"battle":true,"talisman":true,"icon":"📜","shield":0.25,"desc":"Tạo hộ thuẫn bình an tương đương 25% HP tối đa."},
    {"id":"phu_an_than","name":"Ẩn Thân Phù","tier":"pham","price": 1200,"battle":true,"talisman":true,"icon":"🌫️","safeFlee":true,"desc":"Ẩn giấu thân hình, thoát khỏi giao tranh an toàn."},
    {"id":"phu_don_dia","name":"Độn Địa Phù","tier":"pham","price": 1200,"battle":true,"talisman":true,"icon":"🕳️","safeFlee":true,"desc":"Độn thổ nghìn dặm đào thoát tức khắc."},
    {"id":"phu_hoa_cau","name":"Hỏa Cầu Phù","tier":"pham","price": 1050,"battle":true,"talisman":true,"icon":"🔥","burst":1.8,"desc":"Phóng ra cầu lửa thiêu đốt đối thủ gây 180% sát thương."},
    {"id":"phu_bang_tien","name":"Băng Tiễn Phù","tier":"pham","price": 1050,"battle":true,"talisman":true,"icon":"❄️","burst":1.6,"slow":3,"desc":"Bắn ra mưa tên băng làm chậm và gây 160% sát thương."},
    {"id":"phu_kim_cang","name":"Kim Cang Phù","tier":"pham","price": 1350,"battle":true,"talisman":true,"icon":"🛡️","shield":0.3,"desc":"Kim cang hộ thể chắn 30% sát thương."},
    {"id":"phu_than_hanh","name":"Thần Hành Phù","tier":"pham","price": 1275,"battle":true,"talisman":true,"icon":"👟","buff":{"spd":4},"dur":8,"desc":"Gia tăng thân pháp tốc độ di chuyển trong 8 giây."},
    {"id":"phu_cuong_luc","name":"Cương Lực Phù","tier":"pham","price": 1125,"battle":true,"talisman":true,"icon":"💪","buff":{"atk":0.15},"dur":8,"desc":"Tăng 15% sức tấn công trong 8 giây."},
    {"id":"phu_te_liet","name":"Tê Liệt Phù","tier":"pham","price": 1350,"battle":true,"talisman":true,"icon":"⚡","stun":1.5,"desc":"Lôi điện làm tê liệt địch thủ trong 1.5 giây."},
    {"id":"phu_thanh_tam","name":"Thanh Tâm Phù","tier":"pham","price": 1200,"battle":true,"talisman":true,"icon":"🪷","heal":0.2,"cleanse":true,"desc":"Thanh lọc tạp niệm, hồi 20% HP."},
    {"id":"phu_lien_chau_loi","name":"Liên Châu Lôi Phù","tier":"hoang","price": 2250,"battle":true,"talisman":true,"icon":"⚡","burst":2.2,"desc":"Phóng ra chuỗi sấm sét oanh tạc địch thủ gây 220% sát thương."},
    {"id":"phu_dinh_than","name":"Định Thần Phù","tier":"hoang","price": 2700,"battle":true,"talisman":true,"icon":"🪢","stun":2,"desc":"Định thân phong tỏa hành động của đối thủ trong 2 giây."},
    {"id":"phu_hoa_linh","name":"Hóa Linh Phù","tier":"hoang","price": 3000,"battle":true,"talisman":true,"icon":"🔥","burst":2.5,"desc":"Hóa khí thành hỏa linh thiêu đốt địch nhân gây 250% sát thương."},
    {"id":"phu_luc_dinh","name":"Lục Đinh Thiên Giáp Phù","tier":"hoang","price": 3300,"battle":true,"talisman":true,"icon":"🛡️","shield":0.35,"desc":"Triệu hoán thiên binh giáp trụ chắn 35% sát thương."},
    {"id":"phu_truyen_tong","name":"Truyền Tống Phù","tier":"hoang","price": 3750,"battle":true,"talisman":true,"icon":"🌀","safeFlee":true,"desc":"Truyền tống xuyên không gian rút lui khỏi chiến trường."},
    {"id":"phu_tram_yeu","name":"Trảm Yêu Phù","tier":"hoang","price": 3150,"battle":true,"talisman":true,"icon":"🗡️","burst":2.6,"desc":"Khắc chế yêu khí, gây 260% sát thương lên yêu thú."},
    {"id":"phu_phong_nhan","name":"Phong Nhận Loạn Vũ Phù","tier":"hoang","price": 2850,"battle":true,"talisman":true,"icon":"🌪️","burst":2.3,"desc":"Hàng trăm lưỡi đao gió xé rách phòng ngự kẻ địch."},
    {"id":"phu_cam_lo","name":"Cam Lộ Phù","tier":"hoang","price": 2700,"battle":true,"talisman":true,"icon":"💧","heal":0.35,"desc":"Mưa cam lộ hồi phục 35% HP tối đa."},
    {"id":"phu_bat_quai","name":"Bát Quái Hộ Thân Phù","tier":"hoang","price": 3600,"battle":true,"talisman":true,"icon":"☯️","shield":0.38,"desc":"Bát quái trận chuyển hóa sát thương thành hư vô."},
    {"id":"phu_huyet_quang","name":"Huyết Quang Độn Phù","tier":"hoang","price": 3900,"battle":true,"talisman":true,"icon":"🩸","safeFlee":true,"desc":"Độn quang máu đào thoát thần tốc trong nháy mắt."},
    {"id":"phu_diet_hon","name":"Diệt Hồn Phù","tier":"huyen","price": 6750,"battle":true,"talisman":true,"icon":"💀","burst":3.2,"desc":"Phù lục ma đạo sát thương linh hồn gây 320% công kích."},
    {"id":"phu_cuu_thien_loi","name":"Cửu Thiên Lôi Đình Phù","tier":"huyen","price": 7500,"battle":true,"talisman":true,"icon":"⚡","burst":3.5,"stun":1.5,"desc":"Sấm sét cửu thiên giáng thế, sát thương 350% và làm choáng."},
    {"id":"phu_tam_muoi","name":"Tam Muội Chân Hỏa Phù","tier":"huyen","price": 7200,"battle":true,"talisman":true,"icon":"🔥","burst":3.4,"desc":"Chân hỏa bất diệt thiêu rụi hộ giáp đối phương."},
    {"id":"phu_toa_hon","name":"Tỏa Hồn Liễm Khí Phù","tier":"huyen","price": 6300,"battle":true,"talisman":true,"icon":"⛓️","stun":2.5,"desc":"Dây xích vô hình trói chặt nguyên thần đối thủ 2.5 giây."},
    {"id":"phu_van_kiem","name":"Vạn Kiếm Quy Tông Phù","tier":"huyen","price": 7800,"battle":true,"talisman":true,"icon":"⚔️","burst":3.6,"desc":"Muôn vàn phi kiếm ảo ảnh cùng lao tới oanh tạc."},
    {"id":"phu_thuc_cot","name":"Thực Cốt Ma Độc Phù","tier":"huyen","price": 6900,"battle":true,"talisman":true,"icon":"🧪","dot":0.9,"dur":5,"desc":"Chất độc ma đạo ăn mòn tận xương tủy."},
    {"id":"phu_thai_cuc","name":"Thái Cực Huyền Hộ Phù","tier":"huyen","price": 8250,"battle":true,"talisman":true,"icon":"🛡️","shield":0.45,"desc":"Lớp khiên thái cực hư vô hấp thụ 45% HP sát thương."},
    {"id":"phu_cuu_chuyen_hoi","name":"Cửu Chuyển Hồi Mệnh Phù","tier":"huyen","price": 9000,"battle":true,"talisman":true,"icon":"💖","heal":0.6,"cleanse":true,"desc":"Cứu vãn tính mạng bên bờ sinh tử, hồi 60% HP."},
    {"id":"phu_phong_loi_don","name":"Phong Lôi Siêu Độn Phù","tier":"huyen","price": 8700,"battle":true,"talisman":true,"icon":"💨","safeFlee":true,"desc":"Xé toạc không gian phong lôi rút lui an toàn tuyệt đối."},
    {"id":"phu_can_khon_tran","name":"Càn Khôn Trấn Ma Phù","tier":"huyen","price": 9750,"battle":true,"talisman":true,"icon":"🏔️","burst":3.8,"desc":"Khí thế càn khôn như núi đè bẹp đối thủ."},
    {"id":"phu_ngu_loi_chinh","name":"Ngũ Lôi Chánh Pháp Phù","tier":"dia","price":1200,"battle":true,"talisman":true,"icon":"🌩️","burst":4.2,"stun":2,"desc":"Ngũ lôi hợp nhất oanh kích kinh thiên động địa."},
    {"id":"phu_thai_at_than","name":"Thái Ất Thần Hỏa Phù","tier":"dia","price":1300,"battle":true,"talisman":true,"icon":"🔥","burst":4.5,"desc":"Thần hỏa Thái Ất đốt cháy vạn dặm hư vô."},
    {"id":"phu_huyen_thien_ho","name":"Huyền Thiên Hộ Thể Phù","tier":"dia","price":1500,"battle":true,"talisman":true,"icon":"🌟","shield":0.55,"desc":"Bảo hộ huyền thiên che chắn 55% HP sát thương."},
    {"id":"phu_u_minh_quy","name":"U Minh Quỷ Vương Phù","tier":"dia","price":1400,"battle":true,"talisman":true,"icon":"👹","burst":4.3,"desc":"Triệu hoán Quỷ Vương u minh giáng đòn hủy diệt."},
    {"id":"phu_hoan_nguyen_thanh","name":"Hoán Nguyên Thánh Dịch Phù","tier":"dia","price":1600,"battle":true,"talisman":true,"icon":"💧","heal":0.8,"cleanse":true,"desc":"Thánh dịch hồi sinh 80% HP và xua tan mọi tà khí."},
    {"id":"phu_pha_khong_di","name":"Phá Không Di Hình Phù","tier":"dia","price":1800,"battle":true,"talisman":true,"icon":"🌌","safeFlee":true,"desc":"Xuyên toa hư không dời hình đổi bóng tức thời."},
    {"id":"phu_van_cot_ma","name":"Vạn Cốt Huyết Ma Phù","tier":"dia","price":1450,"battle":true,"talisman":true,"icon":"🦴","burst":4.4,"desc":"Vạn cốt ma thương xé toạc sinh mạng kẻ thù."},
    {"id":"phu_thien_cuong_an","name":"Thiên Cương Bắc Đẩu Phù","tier":"dia","price":1700,"battle":true,"talisman":true,"icon":"✨","burst":4.6,"desc":"Ánh sáng Bắc Đẩu thất tinh thanh trừng yêu tà."},
    {"id":"phu_tu_linh_quy","name":"Tụ Linh Quy Nguyên Phù","tier":"dia","price":1550,"battle":true,"talisman":true,"icon":"🌀","mana":1,"desc":"Hồi phục tức thì 100% Linh lực trong giao tranh."},
    {"id":"phu_chu_tuoc_than","name":"Chu Tước Liệt Diễm Phù","tier":"dia","price":1750,"battle":true,"talisman":true,"icon":"🦅","burst":4.8,"desc":"Lửa Chu Tước phần diệt mọi chướng ngại vật."},
    {"id":"phu_pha_gioi","name":"Phá Giới Phù","tier":"thien","price":4000,"battle":true,"talisman":true,"icon":"🌌","safeFlee":true,"desc":"Phù lục chí bảo xé rách giới bích đào thoát khỏi mọi tuyệt cảnh."},
    {"id":"phu_tram_tien_dao","name":"Trảm Tiên Đạo Phù","tier":"thien","price":4500,"battle":true,"talisman":true,"icon":"🗡️","burst":5.5,"stun":2,"desc":"Đạo phù trảm tiên sát thánh chấn nhiếp bát hoang."},
    {"id":"phu_hon_don_quy","name":"Hỗn Độn Quy Nhất Phù","tier":"thien","price":5000,"battle":true,"talisman":true,"icon":"🌀","shield":0.7,"desc":"Khí hỗn độn bao phủ dựng hộ thuẫn 70% HP tối thượng."},
    {"id":"phu_khoi_tu_hoi","name":"Khởi Tử Hồi Sinh Phù","tier":"thien","price":5500,"battle":true,"talisman":true,"icon":"💖","heal":1,"cleanse":true,"desc":"Nghịch chuyển âm dương, hồi phục hoàn toàn 100% Khí Huyết."},
    {"id":"phu_thien_ma_to","name":"Thiên Ma Tổ Phù","tier":"thien","price":4800,"battle":true,"talisman":true,"icon":"💀","burst":5.8,"desc":"Uy lực của Thiên Ma Thủy Tổ giáng lâm tàn sát vạn giới."},
    {"id":"phu_chu_tuoc_than_thong","name":"Chu Tước Thần Hỏa Phù","tier":"thien","price":4700,"battle":true,"talisman":true,"icon":"🔥","burst":5.4,"desc":"Hỏa diễm Chu Tước thiêu đốt hư không vạn trượng."},
    {"id":"phu_dai_la_kim","name":"Đại La Kim Quang Phù","tier":"tien","price":10000,"battle":true,"talisman":true,"icon":"✨","shield":0.85,"desc":"Kim quang Đại La che chở vạn kiếp bất diệt."},
    {"id":"phu_thai_so_diet","name":"Thái Sơ Diệt Thế Phù","tier":"tien","price":12000,"battle":true,"talisman":true,"icon":"🔱","burst":7.5,"stun":3,"desc":"Phù lục Thái Sơ hủy diệt càn khôn trong một kích."},
    {"id":"phu_van_kiep_luan","name":"Vạn Kiếp Luân Hồi Phù","tier":"tien","price":11000,"battle":true,"talisman":true,"icon":"☸️","heal":1,"cleanse":true,"desc":"Luân hồi sinh tử cứu mạng tức khắc từ cõi chết."},
    {"id":"phu_tien_de_sac","name":"Hỗn Độn Tiên Đế Sắc Phù","tier":"tien","price":20000,"battle":true,"talisman":true,"icon":"👑","burst":9,"stun":3,"desc":"Sắc lệnh chí tôn của Tiên Đế, vạn linh cúi đầu."},

    // ---- ĐAN DƯỢC MỞ RỘNG (ÂM DƯƠNG, MA TU, THUỘC TÍNH) ----
    {"id":"dan_bo_duong","name":"Bổ Dương Linh Đan","price": 3750,"battle":false,"icon":"☀️","duongKhi":50,"desc":"Linh đan ngưng tụ dương cương thiên địa, bổ sung 50 Dương Khí cho Nam tu sĩ."},
    {"id":"dan_duong_am","name":"Dưỡng Âm Huyền Đan","price": 3750,"battle":false,"icon":"🌙","amKhi":50,"desc":"Huyền đan chiết xuất từ ánh trăng hàn ngọc, bổ sung 50 Âm Khí cho Nữ tu sĩ."},
    {"id":"dan_huyet_linh","name":"Huyết Linh Thần Đan","price": 12000,"battle":false,"icon":"🩸","demonOnly":true,"permStat":{"atk":15},"desc":"Đan dược Ma Tu luyện từ huyết tinh, tăng vĩnh viễn 15 điểm Công kích."},
    {"id":"dan_ma_cot","name":"Hắc Ma Đoạn Cốt Đan","price": 12000,"battle":false,"icon":"☠️","demonOnly":true,"permStat":{"def":12},"desc":"Đan dược Ma Tu tôi rèn ma cốt, tăng vĩnh viễn 12 điểm Phòng ngự."},
    {"id":"dan_u_minh","name":"U Minh Hóa Khí Đan","price": 9000,"battle":false,"icon":"👻","demonOnly":true,"exp":2500,"desc":"Đan dược Ma Tu hút u linh ma khí, tăng 2500 EXP tu vi."},
    {"id":"dan_cuong_bao","name":"Cuồng Bạo Đan","price": 5250,"battle":true,"icon":"💥","buff":{"atk":0.25},"dur":10,"desc":"Kích phát tiềm năng cuồng bạo, tăng 25% công kích trong 10 giây."},
    {"id":"dan_kim_giap","name":"Kim Giáp Ngự Ma Đan","price": 5250,"battle":true,"icon":"🛡️","buff":{"def":0.3},"dur":10,"desc":"Ngưng tụ kim giáp hộ thân tăng 30% phòng ngự trong 10 giây."},
    {"id":"dan_thien_linh","name":"Thiên Linh Khí Đan","price": 6750,"battle":true,"icon":"🌀","mana":1,"desc":"Hồi phục 100% linh lực tức thì trong chiến đấu."},
    {"id":"dan_nghich_menh","name":"Nghịch Mệnh Hoàn Hồn Đan","price":1500,"battle":true,"icon":"💖","heal":0.75,"cleanse":true,"desc":"Cứu vớt bên bờ sinh tử, hồi 75% HP và hóa giải trạng thái bất lợi."},
    {"id":"dan_bach_thao","name":"Bách Thảo Thối Cốt Đan","price": 13500,"battle":false,"icon":"🌿","exp":3500,"desc":"Chiết xuất từ trăm loại linh thảo quý hiếm, tăng 3500 EXP tu vi."},
    {"id":"dan_hoa_ma","name":"Đoạt Hồn Hóa Ma Đan","price":1200,"battle":true,"icon":"🔥","demonOnly":true,"burst":3.5,"desc":"Đan dược Ma Tu ném ra phát nổ đoạt hồn gây 350% sát thương."},
    {"id":"dan_thai_hu","name":"Thái Hư Thanh Thần Đan","price":2000,"battle":false,"icon":"💎","permStat":{"crit":0.02},"desc":"Tiên đan thanh lọc thần niệm, tăng vĩnh viễn 2% Chí mạng."},
    {"id":"dan_phong_hanh","name":"Phong Hành Thần Đan","price":2000,"battle":false,"icon":"💨","permStat":{"spd":5},"desc":"Tiên đan tăng cường căn cơ thân pháp, tăng vĩnh viễn 5 điểm Tốc độ."},
    {"id":"ngoc_gian_ngo_tinh","name":"Ngọc Giản Ngộ Tính","price":25000,"battle":false,"icon":"📘","roleStat":1,"desc":"Tiêu hao để tăng 1 điểm chuyên môn hệ phái hiện tại; chuyên môn đến từ vật phẩm, không phải hạ yêu."},
    ...[
        { element: 'kim', name: 'Kim Nguyên Đan', icon: '⚔️', price: 9000 },
        { element: 'moc', name: 'Mộc Linh Đan', icon: '🌿', price: 9000 },
        { element: 'thuy', name: 'Thủy Linh Đan', icon: '💧', price: 9000 },
        { element: 'hoa', name: 'Hỏa Linh Đan', icon: '🔥', price: 9000 },
        { element: 'tho', name: 'Địa Mạch Đan', icon: '🪨', price: 9000 },
        { element: 'loi', name: 'Lôi Linh Đan', icon: '⚡', price: 12000 },
        { element: 'phong', name: 'Phong Linh Đan', icon: '🌪️', price: 12000 },
        { element: 'bang', name: 'Hàn Băng Đan', icon: '❄️', price: 12000 },
        { element: 'thien', name: 'Thiên Nguyên Đan', icon: '🌟', price: 25000 },
        { element: 'ma', name: 'Thiên Ma Nguyên Đan', icon: '🌑', price: 25000 },
    ].map(({ element, name, icon, price }) => ({
        id: `dan_thong_thao_${element}`, name, price, battle: false, icon,
        elemMastery: { element, amount: 2 }, qualityRank: 1, qualityName: 'Hoàng',
        desc: `Tăng vĩnh viễn 2% thông thạo hệ ${HE[element].name}, tối đa 100%; tăng sát thương kỹ năng cùng hệ trong giới hạn phẩm chất.`,
    })),
    ...[
        { mon: 'kiem', name: 'Kiếm Ý Đan', icon: '🗡️', statName: 'Kiếm Ý' },
        { mon: 'phap', name: 'Pháp Lực Đan', icon: '🔮', statName: 'Pháp Lực' },
        { mon: 'the', name: 'Thể Phách Đan', icon: '🥋', statName: 'Thể Phách' },
        { mon: 'quyen', name: 'Quyền Kình Đan', icon: '🥊', statName: 'Quyền Kình' },
        { mon: 'dan', name: 'Đan Tâm Đan', icon: '🏺', statName: 'Đan Đạo' },
        { mon: 'ren', name: 'Khí Đạo Đan', icon: '🔨', statName: 'Khí Đạo' },
        { mon: 'phu', name: 'Phù Văn Đan', icon: '📜', statName: 'Phù Văn' },
        { mon: 'thu', name: 'Ngự Thú Ấn Đan', icon: '🐾', statName: 'Ngự Thú Ấn' },
    ].map(({ mon, name, icon, statName }) => ({
        id: `dan_chuyen_mon_${mon}`, name, price: 18000, battle: false, icon,
        roleStat: 1, roleStatMon: mon, qualityRank: 1, qualityName: 'Hoàng',
        desc: `Tăng vĩnh viễn 1 điểm ${statName} (tối đa 200); chuyên môn tăng nhẹ sức mạnh kỹ năng phái trong giới hạn phẩm chất và góp vào điều kiện lĩnh ngộ.`,
    })),
    // Bùa lục mới đối chiếu từ các mục còn thiếu trong PDF; giá và kho giới hạn.
    { id: 'phu_bao_thap_tran_yeu', name: 'Bùa Bảo Tháp Trấn Yêu', price: 18000, stock: 300, battle: true, talisman: true, tier: 'huyen', icon: '🗼', stun: 1, desc: 'Trấn áp yêu khí trong một nhịp, có cơ hội làm choáng yêu thú.' },
    { id: 'phu_tho_bia', name: 'Bùa Thồ Bia', price: 12000, stock: 500, battle: true, talisman: true, tier: 'hoang', icon: '🪨', shield: 0.1, desc: 'Triệu thỉnh bia đá hộ thể, tạo khiên bằng 10% khí huyết.' },
    { id: 'phu_nhat_tam_cau_tu', name: 'Nhất Tâm Cầu Tử Phù', price: 28000, stock: 180, battle: true, talisman: true, tier: 'huyen', icon: '📜', burst: 1.65, desc: 'Đốt phù bộc phát một đòn sát phạt; công pháp phù sư có thể tăng hiệu quả chế tác.' },
    { id: 'phu_chan_khi', name: 'Chân Khí Phù', price: 9000, stock: 700, battle: true, talisman: true, tier: 'hoang', icon: '💠', mana: 0.15, desc: 'Dẫn chân khí trở về, hồi 15% linh lực trong trận.' },
    { id: 'phu_kho_tinh', name: 'Khô Tỉnh Phù', price: 16000, stock: 250, battle: true, talisman: true, tier: 'huyen', icon: '🕳️', cleanse: true, desc: 'Khơi thông thần trí, hóa giải choáng, độc thương và suy yếu.' },
    { id: 'phu_tieu_chu', name: 'Tiểu Chu Phù', price: 22000, stock: 160, battle: true, talisman: true, tier: 'huyen', icon: '⛵', safeFlee: true, desc: 'Hóa phù thành thuyền nhỏ, rút khỏi trận săn an toàn.' },

    // ---- BÍ KÍP CÔNG THỨC CHẾ TẠO ----
    {"id":"recipe_ren_kiem_hoang","name":"Bí Kíp: Thanh Phong Kiếm","price": 3000,"recipeId":"ren_kiem_hoang","icon":"📜","recipeType":"equip","desc":"Bản vẽ đúc rèn Thanh Phong Kiếm."},
    {"id":"recipe_ren_dao_hoang","name":"Bí Kíp: Tứ Bác Đao","price": 3000,"recipeId":"ren_dao_hoang","icon":"📜","recipeType":"equip","desc":"Bản vẽ rèn Tứ Bác Đao từ Kiếm Lai."},
    {"id":"recipe_ren_ho_huyen","name":"Bí Kíp: Dưỡng Kiếm Hồ","price": 7500,"recipeId":"ren_ho_huyen","icon":"📜","recipeType":"equip","desc":"Bản vẽ chế tạo Dưỡng Kiếm Hồ."},
    {"id":"recipe_ren_phien_huyen","name":"Bí Kíp: Cấm Hồn Phiên","price": 7500,"recipeId":"ren_phien_huyen","icon":"📜","recipeType":"equip","desc":"Bản vẽ chế tạo Cấm Hồn Phiên của Vương Lâm."},
    {"id":"recipe_ren_kiem_dia","name":"Bí Kíp: Thanh Trúc Phong Vân Kiếm","price":1500,"recipeId":"ren_kiem_dia","icon":"📜","recipeType":"equip","desc":"Bản vẽ đúc rèn phi kiếm bản mệnh của Hàn Lập."},
    {"id":"recipe_ren_kiem_thien","name":"Bí Kíp: Trảm Tiên Kiếm","price":4000,"recipeId":"ren_kiem_thien","icon":"📜","recipeType":"equip","desc":"Bản vẽ rèn Trảm Tiên Kiếm sát phạt viễn cổ."},
    {"id":"recipe_luyen_dan_truc_co","name":"Đan Phương: Trúc Cơ Đan","price": 4500,"recipeId":"luyen_dan_truc_co","icon":"📜","recipeType":"potion","desc":"Đan phương luyện chế Trúc Cơ Đan của Hàn Lập."},
    {"id":"recipe_luyen_dan_tay_tuy","name":"Đan Phương: Tẩy Tủy Đan","price": 7500,"recipeId":"luyen_dan_tay_tuy","icon":"📜","recipeType":"potion","desc":"Đan phương luyện chế Tẩy Tủy Đan gột rửa căn cốt."},
    {"id":"recipe_luyen_dan_giang_tran","name":"Đan Phương: Giáng Trần Đan","price":1000,"recipeId":"luyen_dan_giang_tran","icon":"📜","recipeType":"potion","desc":"Đan phương ngưng kết Kim Đan cửu chuyển."},
    {"id":"recipe_luyen_dan_ket_anh","name":"Đan Phương: Kết Anh Đan","price":2000,"recipeId":"luyen_dan_ket_anh","icon":"📜","recipeType":"potion","desc":"Đan phương phá toái Kim Đan hóa Nguyên Anh."},
    {"id":"recipe_luyen_dan_hoa_than","name":"Đan Phương: Hóa Thần Đan","price":4000,"recipeId":"luyen_dan_hoa_than","icon":"📜","recipeType":"potion","desc":"Đan phương Hóa Thần Đan đốn ngộ ý cảnh."},
    {"id":"recipe_luyen_dan_cuu_chuyen","name":"Đan Phương: Cửu Chuyển Hồi Hồn Đan","price":1500,"recipeId":"luyen_dan_cuu_chuyen","icon":"📜","recipeType":"potion","desc":"Đan phương khởi tử hồi sinh."},
    {"id":"recipe_che_phu_lien_chau_loi","name":"Phù Lục Bí Yếu: Liên Châu Lôi","price": 3750,"recipeId":"che_phu_lien_chau_loi","icon":"📜","recipeType":"talisman","desc":"Phương pháp chế tác Liên Châu Lôi Phù."},
    {"id":"recipe_che_phu_dinh_than","name":"Phù Lục Bí Yếu: Định Thần Phù","price": 4500,"recipeId":"che_phu_dinh_than","icon":"📜","recipeType":"talisman","desc":"Bí thuật khắc vẽ Định Thần Phù giam cầm đối thủ."},
    {"id":"recipe_che_phu_tam_muoi","name":"Phù Lục Bí Yếu: Tam Muội Chân Hỏa","price": 12000,"recipeId":"che_phu_tam_muoi","icon":"📜","recipeType":"talisman","desc":"Phương pháp ngưng tụ chân hỏa vào bùa chú."},
    {"id":"recipe_che_phu_ngu_loi_chinh","name":"Phù Lục Bí Yếu: Ngũ Lôi Chánh Pháp","price":2000,"recipeId":"che_phu_ngu_loi_chinh","icon":"📜","recipeType":"talisman","desc":"Bí truyền vẽ bùa Ngũ Lôi Chánh Pháp uy chấn cửu thiên."},
    {"id":"recipe_che_phu_tram_tien_dao","name":"Phù Lục Bí Yếu: Trảm Tiên Đạo Phù","price":5000,"recipeId":"che_phu_tram_tien_dao","icon":"📜","recipeType":"talisman","desc":"Cấm thuật chế tạo Đạo phù trảm tiên sát thánh."},
]);

// Thành Trấn & Vị Trí Địa Lý (Cửu Châu - 9 Bản Đồ × 5 Thành Trấn = 45 Thành Trấn)
const TOWNS = Object.freeze([
    // MAP 1: Thanh Châu (Phàm Trần Giới)
    {
        id: 'thanh_van',
        mapId: 'map_1',
        name: 'Thanh Vân Trấn',
        icon: '🏡',
        desc: 'Trấn nhỏ thanh bình dưới chân núi, khởi đầu tiên đạo cho tân thủ.',
        realmMin: 0,
        x: 10, y: 10,
        monsterPool: ['ty_tho_yeu', 'ty_chuot_dat', 'ty_ran_co', 'ty_ong_vang', 'da_lang', 'doc_phong', 'thanh_xa', 'phe_kim_trung', 'kim_si_dieu', 'loi_oa_thu', 'lac_phach_chuy_quy', 'ty_linh_meo', 'bach_ngoc_chu', 'thanh_lang_vuong'],
        healingCost: 1000,
    },
    {
        id: 'trieu_quoc',
        mapId: 'map_1',
        name: 'Triệu Quốc Đô Thành',
        icon: '🏯',
        desc: 'Đô thành phàm trần trong Tiên Nghịch, nơi Hằng Nhạc Phái thanh tu, Vương Lâm bái sư nhập môn.',
        realmMin: 0,
        x: 18, y: 15,
        monsterPool: ['ty_tho_yeu', 'ty_chuot_dat', 'ty_ran_co', 'da_lang', 'doc_phong', 'thanh_xa', 'loi_oa_thu', 'trieu_quoc_ma_chu', 'bach_ngoc_chu'],
        healingCost: 1000,
    },
    {
        id: 'hoang_phong_coc',
        mapId: 'map_1',
        name: 'Hoàng Phong Thị Trấn',
        icon: '🍁',
        desc: 'Thị trấn ngoại vi Hoàng Phong Cốc trong Phàm Nhân Tu Tiên, tu sĩ tụ hội mua sắm đan dược.',
        realmMin: 0,
        x: 28, y: 14,
        monsterPool: ['ty_ong_vang', 'da_lang', 'doc_phong', 'thanh_xa', 'phe_kim_trung', 'kim_si_dieu', 'ty_linh_meo', 'thanh_lang_vuong'],
        healingCost: 1000,
    },
    {
        id: 'lam_an',
        mapId: 'map_1',
        name: 'Lâm An Thành',
        icon: '🏘️',
        desc: 'Thành trấn thương nhân phàm trần trù phú, nơi giao thương hàng hóa giữa tu sĩ và phàm nhân.',
        realmMin: 0,
        x: 22, y: 8,
        monsterPool: ['ty_tho_yeu', 'ty_ran_co', 'ty_ong_vang', 'da_lang', 'thanh_xa'],
        healingCost: 1000,
    },
    {
        id: 'that_huyen_mon',
        mapId: 'map_1',
        name: 'Thất Huyền Sơn Trấn',
        icon: '⚔️',
        desc: 'Trấn nhỏ dưới chân núi Thất Huyền Môn trong Phàm Nhân, nơi khởi đầu duyên kỳ ngộ của Hàn Lập.',
        realmMin: 0,
        x: 12, y: 18,
        monsterPool: ['ty_tho_yeu', 'ty_chuot_dat', 'doc_phong', 'thanh_xa', 'thanh_lang_vuong'],
        healingCost: 1000,
    },

    // MAP 2: U Châu (Luyện Khí Giới)
    {
        id: 'thien_nam',
        mapId: 'map_2',
        name: 'Thiên Nam Cổ Thành',
        icon: '🏰',
        desc: 'Cổ thành rộng lớn thuộc Thiên Nam đại lục, nơi khởi nguồn huyền thoại tu tiên của Hàn Lập.',
        realmMin: 1,
        x: 25, y: 28,
        monsterPool: ['ty_hai_sam', 'ty_kiem_diep', 'thach_hau', 'thanh_xa', 'kim_si_dieu', 'phe_kim_trung', 'lac_phach_chuy_quy', 'thanh_lang_vuong', 'trieu_quoc_ma_chu'],
        healingCost: 1000,
    },
    {
        id: 'ly_chau',
        mapId: 'map_2',
        name: 'Ly Châu Động Thiên',
        icon: '🏺',
        desc: 'Trấn nung ngói nhỏ bé của Trần Bình An trong Kiếm Lai, ẩn giấu vô số cơ duyên viễn cổ.',
        realmMin: 1,
        x: 38, y: 22,
        monsterPool: ['ty_hai_sam', 'ty_kiem_diep', 'thach_hau', 'thanh_xa', 'loi_oa_thu', 'thao_thiet_tan_hon', 'nga_hoang_kiem_thu'],
        healingCost: 1200,
    },
    {
        id: 'nga_hoang',
        mapId: 'map_2',
        name: 'Ngã Hoàng Động Thiên',
        icon: '⛰️',
        desc: 'Sơn thủy hữu tình trong Kiếm Lai, nơi hiệp khách kiếm tiên luận bàn đạo pháp.',
        realmMin: 2,
        x: 55, y: 30,
        monsterPool: ['ty_ech_doc', 'ty_ho_ly_con', 'ty_hoa_ho', 'hoa_ho', 'u_hon', 'phong_lang', 'ban_son_vien', 'thao_thiet_tan_hon', 'thac_nguyet_ho', 'nga_hoang_kiem_thu'],
        healingCost: 1800,
    },
    {
        id: 'gia_nguyen',
        mapId: 'map_2',
        name: 'Gia Nguyên Thành',
        icon: '⛵',
        desc: 'Bến cảng sầm uất trên sông Mặc Thủy, nơi Hàn Lập kết ân oán với Kinh Đô và Mặc gia.',
        realmMin: 1,
        x: 32, y: 25,
        monsterPool: ['ty_hai_sam', 'ty_kiem_diep', 'thanh_xa', 'thach_hau', 'loi_oa_thu'],
        healingCost: 1000,
    },
    {
        id: 'thai_nhac',
        mapId: 'map_2',
        name: 'Thái Nhạc Sơn Trấn',
        icon: '🌲',
        desc: 'Thị trấn bao quanh mạch khoáng linh thạch dưới chân Hoàng Phong Cốc, linh khí nồng đậm.',
        realmMin: 2,
        x: 42, y: 28,
        monsterPool: ['thach_hau', 'thanh_xa', 'kim_si_dieu', 'nga_hoang_kiem_thu'],
        healingCost: 1200,
    },

    // MAP 3: Vân Châu (Trúc Cơ Vực)
    {
        id: 'lac_duong',
        mapId: 'map_3',
        name: 'Lạc Dương Thành',
        icon: '🏯',
        desc: 'Đô thành cổ kính sầm uất, thương khách tu sĩ khắp nơi tụ hội, địa bàn của Trúc Cơ và Kim Đan.',
        realmMin: 2,
        x: 45, y: 35,
        monsterPool: ['ty_ech_doc', 'ty_ho_ly_con', 'ty_soi_con', 'ty_nhen_to', 'ty_hoa_ho', 'ty_loi_trung', 'thach_hau', 'hoa_ho', 'u_hon', 'phong_lang', 'ban_son_vien', 'mac_giao', 'u_minh_thu', 'doc_giac_bao', 'thiet_giap_cu_te', 'hac_ma_nghi', 'thao_thiet_tan_hon', 'huyet_nhuc_khoi_loi', 'bat_trao_hoa_thu', 'sat_luc_huyet_buc', 'bach_cot_lang', 'thac_nguyet_ho', 'am_cot_ma_xa', 'ngu_loi_hac_ung'],
        healingCost: 1500,
    },
    {
        id: 'thai_nham_dien',
        mapId: 'map_3',
        name: 'Thái Nhậm Động Thiên',
        icon: '⛩️',
        desc: 'Phồn hoa đạo môn đô hội của Kiếm Lai, danh gia vọng tộc cùng chư tử bách gia tụ hội.',
        realmMin: 3,
        x: 62, y: 45,
        monsterPool: ['ty_bang_thiem', 'ty_quy_anh', 'phong_lang', 'thiet_giap_te', 'huyen_bang_mang', 'thiet_giap_cu_te', 'huyet_hoang_thu', 'ban_son_trung'],
        healingCost: 2500,
    },
    {
        id: 'yen_vu_giang',
        mapId: 'map_3',
        name: 'Yên Vũ Giang Trấn',
        icon: '🌊',
        desc: 'Thị trấn sông nước mờ ảo trong khói sương, tu sĩ dừng chân thưởng trà ngộ đạo.',
        realmMin: 3,
        x: 50, y: 40,
        monsterPool: ['ty_ech_doc', 'ty_ho_ly_con', 'phong_lang', 'mac_giao', 'u_minh_thu'],
        healingCost: 2000,
    },
    {
        id: 'kinh_do_viet_quoc',
        mapId: 'map_3',
        name: 'Việt Quốc Hoàng Thành',
        icon: '👑',
        desc: 'Hoàng cung tráng lệ nơi Hắc Sát Giáo âm thầm thao túng huyết tế tu sĩ phàm nhân.',
        realmMin: 3,
        x: 58, y: 38,
        monsterPool: ['ty_bang_thiem', 'ty_hoa_ho', 'hoa_ho', 'thiet_giap_cu_te', 'ban_son_trung'],
        healingCost: 2200,
    },
    {
        id: 'van_mong_coc',
        mapId: 'map_3',
        name: 'Vân Mộng Sơn Trấn',
        icon: '🌫️',
        desc: 'Thung lũng ngập tràn sương mù hư ảo, nơi ẩn giấu nhiều động phủ tiền nhân để lại.',
        realmMin: 3,
        x: 48, y: 48,
        monsterPool: ['ty_soi_con', 'ty_nhen_to', 'u_hon', 'thac_nguyet_ho', 'ngu_loi_hac_ung'],
        healingCost: 2200,
    },

    // MAP 4: Hải Châu (Kim Đan Giới)
    {
        id: 'loan_tinh_hai',
        mapId: 'map_4',
        name: 'Loạn Tinh Hải Quần Đảo',
        icon: '🏝️',
        desc: 'Quần đảo hải ngoại bao la trong Phàm Nhân Tu Tiên, nơi Hàn Lập săn yêu thú lấy nội đan.',
        realmMin: 3,
        x: 80, y: 20,
        monsterPool: ['ty_bang_thiem', 'ty_quy_anh', 'phong_lang', 'thiet_giap_te', 'mac_giao', 'u_minh_thu', 'doc_giac_bao', 'thiet_giap_cu_te', 'bat_trao_hoa_thu', 'loan_tinh_hai_giao', 'huyet_hoang_thu', 'bach_cot_lang'],
        healingCost: 2200,
    },
    {
        id: 'thien_co',
        mapId: 'map_4',
        name: 'Thiên Cơ Thành',
        icon: '🏯',
        desc: 'Thành trì phiêu phù giữa biển mây, linh trận bảo hộ, chốn giao lưu của cường giả Kim Đan & Hóa Thần.',
        realmMin: 4,
        x: 85, y: 55,
        monsterPool: ['ty_ret_lua', 'ty_luon_dien', 'ty_qua_den', 'ty_khi_lua', 'ty_kim_giap_trung', 'ty_u_hon_xa', 'thiet_giap_te', 'huyen_bang_mang', 'loi_ung', 'huyet_ma_vien', 'thanh_y_thuy_giao', 'van_thu', 'de_hon_thu', 'song_dong_thu', 'thon_thien_mang', 'liet_phong_thu', 'u_lam_hoa_diep', 'ban_son_trung', 'ngu_sac_khong_tuoc', 'tu_la_ma_thu', 'kiem_khi_bach_vien', 'co_ma_hoa_than', 'tram_long_thach_vien'],
        healingCost: 3500,
    },
    {
        id: 'luc_ma_hai',
        mapId: 'map_4',
        name: 'Lục Ma Hải Ma Thành',
        icon: '🌊',
        desc: 'Vùng biển ma đạo hiểm ác tanh máu trong Tiên Nghịch, nơi mạnh được yếu thua tàn nhẫn bậc nhất.',
        realmMin: 4,
        x: 52, y: 65,
        monsterPool: ['ty_ret_lua', 'ty_luon_dien', 'ty_kim_giap_trung', 'ty_u_hon_xa', 'huyen_bang_mang', 'loi_ung', 'huyet_ma_vien', 'thon_thien_mang', 'liet_phong_thu', 'u_lam_hoa_diep', 'co_ma_hoa_than'],
        healingCost: 3800,
    },
    {
        id: 'khoi_tinh_dao',
        mapId: 'map_4',
        name: 'Khôi Tinh Đảo',
        icon: '⚓',
        desc: 'Hòn đảo yên bình ngoài khơi Loạn Tinh Hải, nơi Hàn Lập bế quan tu luyện Trúc Cơ viên mãn.',
        realmMin: 4,
        x: 75, y: 25,
        monsterPool: ['ty_bang_thiem', 'ty_quy_anh', 'thiet_giap_te', 'loan_tinh_hai_giao', 'huyet_hoang_thu'],
        healingCost: 3200,
    },
    {
        id: 'tinh_cung',
        mapId: 'map_4',
        name: 'Thiên Tinh Thành',
        icon: '✨',
        desc: 'Đô thành trung tâm thống trị Loạn Tinh Hải, Tinh Cung Song Thánh uy chấn bốn phương.',
        realmMin: 4,
        x: 88, y: 35,
        monsterPool: ['ty_ret_lua', 'ty_luon_dien', 'huyen_bang_mang', 'loi_ung', 'tram_long_thach_vien'],
        healingCost: 3500,
    },

    // MAP 5: Lôi Châu (Nguyên Anh Vực)
    {
        id: 'am_la_tong',
        mapId: 'map_5',
        name: 'Âm La Quỷ Vực',
        icon: '💀',
        desc: 'Cấm địa ma tu ma khí cuồn cuộn trong Phàm Nhân, nơi Âm La Tông ngưng tụ vạn quỷ câu hồn.',
        realmMin: 5,
        x: 72, y: 72,
        monsterPool: ['ty_tu_la_dieu', 'loi_ung', 'huyet_ma_vien', 'van_thu', 'de_hon_thu', 'thon_thien_mang', 'tu_la_ma_thu', 'son_nhac_cu_vien', 'am_la_quy_vuong', 'thien_nam_co_dieu'],
        healingCost: 4500,
    },
    {
        id: 'tien_di_chau',
        mapId: 'map_5',
        name: 'Tiên Di Cổ Thành',
        icon: '🗿',
        desc: 'Cổ địa bí ẩn của Tiên Di tộc trong Tiên Nghịch, phong ấn bí mật Cổ Thần viễn cổ.',
        realmMin: 6,
        x: 68, y: 82,
        monsterPool: ['ty_loi_long_con', 'huyet_ma_vien', 'son_nhac_cu_vien', 'bat_hoang_long', 'thon_hu_kinh', 'tinh_khong_cu_thu', 'tu_la_huyet_long', 'co_than_do_tu_ve'],
        healingCost: 5500,
    },
    {
        id: 'bac_cau_lo',
        mapId: 'map_5',
        name: 'Bắc Câu Lô Châu',
        icon: '❄️',
        desc: 'Vùng đất băng hàn nghìn dặm trong Kiếm Lai, nơi tôi luyện ý chí của kiếm tu và võ phu.',
        realmMin: 6,
        x: 30, y: 88,
        monsterPool: ['ty_loi_long_con', 'son_nhac_cu_vien', 'bat_hoang_long', 'thon_hu_kinh', 'bach_cau_bang_hung', 'tinh_khong_cu_thu'],
        healingCost: 5500,
    },
    {
        id: 'lac_hon_coc',
        mapId: 'map_5',
        name: 'Lạc Hồn Cốc',
        icon: '⚡',
        desc: 'Hẻm núi sấm sét liên miên, lôi linh khí cuồn cuộn, nơi ngưng tụ Lôi Anh của tu sĩ.',
        realmMin: 5,
        x: 70, y: 76,
        monsterPool: ['ty_tu_la_dieu', 'loi_ung', 'huyet_ma_vien', 'de_hon_thu', 'am_la_quy_vuong'],
        healingCost: 4800,
    },
    {
        id: 'van_diep_thanh',
        mapId: 'map_5',
        name: 'Vạn Diệp Cổ Trấn',
        icon: '🍃',
        desc: 'Rừng sâu ngàn năm bao bọc thành trì, yêu thụ tu luyện hóa hình trấn thủ bốn bề.',
        realmMin: 5,
        x: 65, y: 85,
        monsterPool: ['ty_loi_long_con', 'van_thu', 'thon_thien_mang', 'thien_nam_co_dieu'],
        healingCost: 5000,
    },

    // MAP 6: Viêm Châu (Hóa Thần Giới)
    {
        id: 'chu_tuoc_quoc',
        mapId: 'map_6',
        name: 'Chu Tước Tinh Đô',
        icon: '🔥',
        desc: 'Đô thành bậc năm của Chu Tước Tinh trong Tiên Nghịch, nơi Vương Lâm danh chấn thiên hạ.',
        realmMin: 7,
        x: 92, y: 68,
        monsterPool: ['ty_hon_don_trung', 'kim_giap_ngac', 'thuy_ky_lan', 'cuu_vi_ho', 'hon_don_thu', 'huyet_ky_lan', 'thon_hu_kinh', 'chu_tuoc_hoa_dieu', 'co_than_do_tu_ve'],
        healingCost: 7000,
    },
    {
        id: 'van_yeu',
        mapId: 'map_6',
        name: 'Vạn Yêu Thành',
        icon: '🌋',
        desc: 'Biên giới hung hiểm ngập tràn ma khí, yêu thú dị biến hoành hành, hiểm nguy trùng trùng.',
        realmMin: 8,
        x: 125, y: 85,
        monsterPool: ['ty_bo_cap', 'ty_rua_nuoc', 'ty_hac_bang', 'ty_linh_ho', 'ty_thai_co_trung', 'kim_giap_ngac', 'thuy_ky_lan', 'cuu_vi_ho', 'loi_giao', 'bang_phuong', 'hon_don_thu', 'thai_co_loi_long', 'du_thien_con_bang', 'la_hau_co_thu', 'hinh_nha_thu', 'kim_thien_ong', 'huyet_ky_lan', 'thon_hu_kinh', 'thien_si_chu_hac', 'cuu_muc_thiem', 'tien_di_thanh_thu'],
        healingCost: 8000,
    },
    {
        id: 'tuyet_tinh_coc',
        mapId: 'map_6',
        name: 'Tuyệt Tình Cốc',
        icon: '💔',
        desc: 'Cốc sâu dứt bỏ hồng trần, nơi Hóa Thần đại năng hóa phàm ngộ sinh tử luân hồi.',
        realmMin: 6,
        x: 98, y: 72,
        monsterPool: ['kim_giap_ngac', 'cuu_vi_ho', 'chu_tuoc_hoa_dieu', 'co_than_do_tu_ve'],
        healingCost: 6500,
    },
    {
        id: 'hoang_tuyen_thanh',
        mapId: 'map_6',
        name: 'Hoàng Tuyền Ma Đô',
        icon: '👹',
        desc: 'Thành trì ma đạo dựng trên huyết hải Hoàng Tuyền, nơi quy tụ những kẻ nghịch thiên kháng đạo.',
        realmMin: 6,
        x: 105, y: 80,
        monsterPool: ['ty_hon_don_trung', 'thuy_ky_lan', 'hon_don_thu', 'huyet_ky_lan'],
        healingCost: 7500,
    },
    {
        id: 'phu_tang_dao',
        mapId: 'map_6',
        name: 'Phù Tang Cổ Đảo',
        icon: '🌅',
        desc: 'Cổ đảo phương đông nơi mặt trời mọc, ngưng tụ Thái Dương Chân Hỏa tôi luyện thần thông.',
        realmMin: 6,
        x: 115, y: 75,
        monsterPool: ['kim_giap_ngac', 'cuu_vi_ho', 'chu_tuoc_hoa_dieu', 'thon_hu_kinh'],
        healingCost: 7500,
    },

    // MAP 7: Cương Châu (Luyện Hư Giới)
    {
        id: 'kiem_khi_truong_thanh',
        mapId: 'map_7',
        name: 'Kiếm Khí Trường Thành',
        icon: '⚔️',
        desc: 'Vạn dặm trường thành chắn ngang Man Hoang trong Kiếm Lai, kiếm khí ngút trời trảm sát đại yêu.',
        realmMin: 8,
        x: 110, y: 95,
        monsterPool: ['ty_thai_co_trung', 'thai_co_loi_long', 'du_thien_con_bang', 'la_hau_co_thu', 'hinh_nha_thu', 'kim_thien_ong', 'huyet_ky_lan', 'truong_thanh_kiem_linh', 'tien_di_thanh_thu'],
        healingCost: 9000,
    },
    {
        id: 'dong_linh_tinh',
        mapId: 'map_7',
        name: 'Đông Lâm Tinh Đô',
        icon: '🪐',
        desc: 'Cổ Tinh thần bí bậc tám trong Tiên Nghịch, tứ đại gia tộc hùng cứ, pháp tắc ngưng đọng.',
        realmMin: 9,
        x: 140, y: 60,
        monsterPool: ['ty_thai_co_trung', 'du_thien_con_bang', 'la_hau_co_thu', 'dong_linh_co_giao', 'truong_thanh_kiem_linh', 'man_hoang_dai_yeu'],
        healingCost: 11000,
    },
    {
        id: 'quy_nguyen_tong',
        mapId: 'map_7',
        name: 'Quy Nguyên Tiên Thành',
        icon: '☯️',
        desc: 'Tiên thành đại phái tu chân bậc sáu, đệ tử vạn người, trận pháp hộ thành kiên cố vô song.',
        realmMin: 7,
        x: 120, y: 65,
        monsterPool: ['ty_thai_co_trung', 'thai_co_loi_long', 'du_thien_con_bang', 'truong_thanh_kiem_linh'],
        healingCost: 9500,
    },
    {
        id: 'thien_yeu_thanh',
        mapId: 'map_7',
        name: 'Thiên Yêu Vương Thành',
        icon: '🦅',
        desc: 'Thành lũy của Thiên Yêu bộ tộc, nơi các đại yêu viễn cổ hóa hình tụ hội xưng hùng.',
        realmMin: 7,
        x: 132, y: 70,
        monsterPool: ['ty_thai_co_trung', 'la_hau_co_thu', 'kim_thien_ong', 'dong_linh_co_giao'],
        healingCost: 10000,
    },
    {
        id: 'tinh_khong_trang',
        mapId: 'map_7',
        name: 'Tinh Không Dịch Trạm',
        icon: '🌌',
        desc: 'Trạm dừng chân giữa các tinh cầu mênh mông, nơi các tu sĩ viễn chinh nghỉ ngơi tiếp tế.',
        realmMin: 8,
        x: 138, y: 80,
        monsterPool: ['du_thien_con_bang', 'la_hau_co_thu', 'dong_linh_co_giao', 'man_hoang_dai_yeu'],
        healingCost: 11500,
    },

    // MAP 8: Man Châu (Hợp Thể Độ Kiếp Vực)
    {
        id: 'man_hoang_thien_dia',
        mapId: 'map_8',
        name: 'Man Hoang Thiên Địa',
        icon: '🌲',
        desc: 'Lãnh địa của mười bốn cảnh đại yêu man dại vô tận trong Kiếm Lai, hung hiểm cùng cực.',
        realmMin: 10,
        x: 145, y: 105,
        monsterPool: ['ty_thai_co_trung', 'la_hau_co_thu', 'man_hoang_dai_yeu', 'dong_linh_co_giao', 'bac_han_tien_hac', 'chan_tien_ma_khi'],
        healingCost: 13000,
    },
    {
        id: 'bac_han_tien_cung',
        mapId: 'map_8',
        name: 'Bắc Hàn Cung Vực',
        icon: '❄️',
        desc: 'Băng hàn cực độ đóng băng vạn dặm, nơi đại năng ngộ băng hệ pháp tắc phá cảnh.',
        realmMin: 8,
        x: 148, y: 95,
        monsterPool: ['la_hau_co_thu', 'man_hoang_dai_yeu', 'dong_linh_co_giao', 'bac_han_tien_hac'],
        healingCost: 12500,
    },
    {
        id: 'thien_dao_tong',
        mapId: 'map_8',
        name: 'Thiên Đạo Tiên Thành',
        icon: '⚡',
        desc: 'Thành trì ngự trị trên đỉnh thiên hà, gần nhất với ý chí Thiên Đạo và lôi kiếp vũ trụ.',
        realmMin: 9,
        x: 150, y: 102,
        monsterPool: ['man_hoang_dai_yeu', 'bac_han_tien_hac', 'chan_tien_ma_khi'],
        healingCost: 13000,
    },
    {
        id: 'to_long_dao',
        mapId: 'map_8',
        name: 'Tổ Long Thánh Vực',
        icon: '🐉',
        desc: 'Tổ địa của Thượng Cổ Long Tộc, long khí ngút trời, tôi luyện nhục thân bất tử bất diệt.',
        realmMin: 9,
        x: 154, y: 108,
        monsterPool: ['la_hau_co_thu', 'bac_han_tien_hac', 'chan_tien_ma_khi', 'hon_don_to_long'],
        healingCost: 13500,
    },
    {
        id: 'do_kiep_dai',
        mapId: 'map_8',
        name: 'Vạn Kiếp Phong Lôi Trấn',
        icon: '🌪️',
        desc: 'Khu vực độ kiếp nghịch thiên, phong lôi cuộn trào, nơi chuẩn bị phi thăng bước vào Tiên Giới.',
        realmMin: 10,
        x: 158, y: 112,
        monsterPool: ['chan_tien_ma_khi', 'thien_dao_loi_thu', 'tien_de_tan_niem'],
        healingCost: 14000,
    },

    // MAP 9: Cửu Thiên Tiên Giới (Yêu cầu Phi Thăng)
    {
        id: 'tien_gioi_khoi_nguyen',
        mapId: 'map_9',
        name: 'Bắc Hàn Tiên Vực',
        icon: '🌌',
        desc: 'Tiên giới mênh mông trong Phàm Nhân Tiên Giới Thiên, tiên đạo đỉnh phong, vạn kiếp bất diệt.',
        realmMin: 11,
        x: 160, y: 120,
        monsterPool: ['ty_tuyet_vien', 'chan_tien_ma_khi', 'bang_phuong', 'linh_te_khong_tuoc', 'tien_canh_12_huyen_hai_giao', 'tien_canh_12_tinh_thu', 'tien_canh_12_kiem_linh', 'ty_loi_thu', 'dai_la_kim_long', 'thien_kiep_loi_ngao', 'thanh_long_anh'],
        healingCost: 16000,
    },
    {
        id: 'lao_quan_dien',
        mapId: 'map_9',
        name: 'Thái Thanh Tiên Điện',
        icon: '🏛️',
        desc: 'Tiên điện đạo gia chí cao, đan hỏa cửu chuyển, nơi Thái Thượng Đạo Tổ ban phát tiên đan.',
        realmMin: 13,
        x: 168, y: 125,
        monsterPool: ['ty_kim_dieu', 'bach_ho_anh', 'kim_thien_ong', 'hon_don_to_long', 'ty_moc_tinh', 'huyen_vu_anh', 'hinh_nha_thu', 'thien_dao_loi_thu', 'ty_thach_linh', 'chu_tuoc_anh', 'du_thien_con_bang', 'thai_co_to_than'],
        healingCost: 18000,
    },
    {
        id: 'dai_la_thanh_do',
        mapId: 'map_9',
        name: 'Đại La Thiên Đô',
        icon: '👑',
        desc: 'Đô thành ngự trị trên Cửu Trọng Thiên, nơi chư vị Đại La Kim Tiên hội tụ luận bàn quy tắc vạn cổ.',
        realmMin: 14,
        x: 175, y: 130,
        monsterPool: ['ty_phong_linh', 'hon_don_thu', 'la_hau_co_thu', 'tien_de_tan_niem', 'hon_don_thuc_tinh', 'loi_bang_thien_tac', 'ty_thuy_linh', 'thao_thiet', 'thao_thiet_co_than', 'ty_hoa_linh', 'cung_ky', 'cung_ky_hoang_thu', 'thien_ma', 'hu_khong_thien_ma_ton'],
        healingCost: 20000,
    },
    {
        id: 'hon_don_tien_dinh',
        mapId: 'map_9',
        name: 'Hỗn Độn Tiên Đình',
        icon: '☸️',
        desc: 'Tiên đình cổ xưa cai quản chư thiên vạn giới, chấp chưởng lôi kiếp luân hồi.',
        realmMin: 15,
        x: 182, y: 135,
        monsterPool: ['ty_huyen_tinh_xa', 'thai_at_huyen_lan', 'van_co_huyet_phuong', 'ty_kim_tuy_dieu', 'thai_at_kim_long', 'ty_dai_la_yeu_tinh', 'dai_la_hac_nguu', 'ty_thien_hoa_vuong', 'dai_la_bach_ho', 'ty_hon_nguyen_ma_linh', 'hon_don_to_long_vi_dai', 'vo_cuc_tinh_thu'],
        healingCost: 22000,
    },
    {
        id: 'truong_sinh_gioi',
        mapId: 'map_9',
        name: 'Trường Sinh Thánh Cảnh',
        icon: '🌸',
        desc: 'Cảnh giới tối cao siêu thoát thời không luân hồi, đồng thọ cùng trời đất, vĩnh hằng bất diệt.',
        realmMin: 16,
        realmCap: 30,
        x: 190, y: 140,
        monsterPool: ['ty_tien_quan_ho_ve', 'u_minh_tien_quan', 'ty_van_co_oan_hon', 'cuu_u_tien_ton', 'ty_loan_co_hung_thu', 'chuan_tien_vuong_thu', 'ty_tien_vuong_chien_linh', 'bat_hoang_tien_vuong', 'ty_hon_don_diet_the', 'thai_so_tien_de_hon'],
        healingCost: 25000,
    },
    ...POST_TIEN_DE_TOWNS
].map(town => ({ ...town, realmMinName: town.realmMinName || CULTIVATION_REALM_NAMES[town.realmMin] || 'Phàm Nhân' })));

const TOWN_BY_ID = new Map(TOWNS.map(t => [t.id, t]));

function getTravelSec(fromTownId, toTownId) {
    if (fromTownId === toTownId) return 0;
    const t1 = TOWN_BY_ID.get(fromTownId) || TOWNS[0];
    const t2 = TOWN_BY_ID.get(toTownId) || TOWNS[0];
    const dist = Math.hypot(t2.x - t1.x, t2.y - t1.y);
    return Math.min(1800, Math.max(60, Math.round(dist * 12)));
}

function getTeleportCost(fromTownId, toTownId) {
    const travelSec = getTravelSec(fromTownId, toTownId);
    const distanceRatio = Math.max(0, Math.min(1, travelSec / 1800));
    // Phí khởi điểm 500 LL, tăng đều theo khoảng cách tới tối đa 2.000 LL.
    return Math.max(500, Math.min(2_000, Math.round((500 + distanceRatio * 1500) / 50) * 50));
}

// Đội ngũ NPC Phản Diện Ma Tu
const DEMON_NPCS = Object.freeze([
    { id: 'demon_cuc_am', name: 'Cực Âm Tổ Sư', title: 'Huyền Âm Lão Ma', gender: 'nam', mon: 'ma', he: 'am', baseRealm: 5, powerBase: 5200, avatar: '💀', desc: 'Ma đầu Loạn Tinh Hải tu luyện Thiên Ma Quyết, âm hiểm ngoan độc, chuyên phục kích tu sĩ cướp bảo.', skills: ['ma_cuu_u', 'ma_thien_ma_don', 'ma_phe_hon'], isDemon: true },
    { id: 'demon_luc_duc', name: 'Lục Dục Ma Quân', title: 'Dục Vọng Chi Chủ', gender: 'nam', mon: 'ma', he: 'tam', baseRealm: 5, powerBase: 5000, avatar: '👺', desc: 'Cao thủ Lục Dục Ma Môn, thao túng lục dục thất tình, tâm ma nan phòng.', skills: ['ma_cuu_u', 'ma_sat_luc'], isDemon: true },
    { id: 'demon_huyet_tich', name: 'Huyết Tích Tử', title: 'Thiết Huyết Cuồng Ma', gender: 'nam', mon: 'ma', he: 'hoa', baseRealm: 4, powerBase: 4200, avatar: '🩸', desc: 'Tu sĩ Huyết Sát Tông cuồng sát khát máu, tắm trong biển máu tăng tiến tu vi.', skills: ['ma_hoa_huyet', 'ma_cuu_u'], isDemon: true },
    { id: 'demon_cot_ma', name: 'Cốt Ma Lão Tổ', title: 'Bạch Cốt Thần Ma', gender: 'nam', mon: 'ma', he: 'tho', baseRealm: 6, powerBase: 7800, avatar: '☠️', desc: 'Luyện Bạch Cốt Thần Ma Khí, thân thể bạch cốt đao thương bất nhập, sát phạt chư thiên.', skills: ['ma_thac_thien', 'ma_phe_hon'], isDemon: true },
    { id: 'demon_sat_luc', name: 'Sát Lục Tàn Niệm', title: 'Sát Đạo Ma Hoàng', gender: 'nam', mon: 'ma', he: 'kim', baseRealm: 7, powerBase: 12000, avatar: '⚔️', desc: 'Tàn niệm sát lục viễn cổ thức tỉnh, vô tình trảm diệt thiên hạ sinh linh.', skills: ['ma_sat_luc', 'ma_nghich_chi'], isDemon: true },
    { id: 'demon_u_minh', name: 'U Minh Ma Nữ', title: 'Bách Quỷ U Hồn', gender: 'nu', mon: 'ma', he: 'bang', baseRealm: 4, powerBase: 3800, avatar: '👻', desc: 'Ma nữ U Minh Cung quyến rũ ma mị, hút cạn nguyên dương và hồn phách tu sĩ.', skills: ['ma_cuu_u', 'ma_hoa_huyet'], isDemon: true },
    { id: 'demon_van_cot', name: 'Vạn Cốt Ma Hoàng', title: 'Chí Tôn Ma Đế', gender: 'nam', mon: 'ma', he: 'am', baseRealm: 8, powerBase: 25000, avatar: '👑', desc: 'Ma đạo chí tôn thống lĩnh cửu u ma giới, xuất thế là thiên địa biến sắc, sinh linh đồ thán.', skills: ['ma_thac_thien', 'ma_nghich_chi', 'ma_sat_luc'], isDemon: true },
    { id: 'demon_huyet_ba', name: 'Huyết Ba Lão Tổ', title: 'Huyết Hải Giáo Chủ', gender: 'nam', mon: 'ma', he: 'thuy', baseRealm: 9, powerBase: 32000, avatar: '🍷', desc: 'Huyết hải bất khô, Huyết Ma bất tử. Nắm giữ thần thông Huyết Thần Tử vô song.', skills: ['ma_hoa_huyet', 'ma_sat_luc'], isDemon: true },
    { id: 'demon_cuu_u_lao', name: 'Cửu U Minh Chủ', title: 'U Minh Diêm La', gender: 'nam', mon: 'ma', he: 'bang', baseRealm: 10, powerBase: 42000, avatar: '🖤', desc: 'Chưởng quản Cửu U Minh Giới, cửu u lãnh diễm đóng băng vạn dặm.', skills: ['ma_cuu_u', 'ma_thac_thien'], isDemon: true },
    { id: 'demon_bach_cot_tien', name: 'Bạch Cốt Yêu Cơ', title: 'Họa Quốc Yêu Nữ', gender: 'nu', mon: 'ma', he: 'am', baseRealm: 6, powerBase: 7600, avatar: '🥀', desc: 'Yêu nữ tuyệt sắc tu luyện mị thuật ma đạo, mê hoặc lòng người, hút cạn tu vi.', skills: ['ma_phe_hon', 'ma_thien_ma_don'], isDemon: true },
    { id: 'demon_thien_ma_ton', name: 'Thiên Ma Đạo Chủ', title: 'Hỗn Độn Ma Tổ', gender: 'nam', mon: 'ma', he: 'tam', baseRealm: 13, powerBase: 65000, avatar: '👿', desc: 'Ma tổ thượng cổ viễn phong, nhất niệm hóa vạn ma, khống chế tâm ma chư thiên.', skills: ['ma_nghich_chi', 'ma_thac_thien', 'ma_sat_luc'], isDemon: true },
    { id: 'demon_da_xoa', name: 'Dạ Xoa Quỷ Vương', title: 'Tu La Chiến Thần', gender: 'nam', mon: 'ma', he: 'hoa', baseRealm: 7, powerBase: 13500, avatar: '👹', desc: 'Chiến quỷ Tu La Đạo, cuồng bạo hiếu sát, càng chiến càng mạnh.', skills: ['ma_sat_luc', 'ma_thien_ma_don'], isDemon: true },
]);

// Danh Sách NPC Thế Giới & Chưởng Quản Cửa Hàng
const WORLD_NPCS = Object.freeze([
    { id: 'npc_han_lap', name: 'Hàn Lập', title: 'Hàn Lão Ma', gender: 'nam', mon: 'dan', he: 'moc', baseRealm: 4, powerBase: 3200, avatar: '🧙‍♂️', shop: 'dan', desc: 'Chưởng quỹ Tiệm Đan Thanh Vân Trấn, sát phạt quả quyết, nắm giữ vô số đan dược nghịch thiên.' },
    { id: 'npc_tuyet_ky', name: 'Lục Tuyết Kỳ', title: 'Thanh Vân Kiếm Tiên', gender: 'nu', mon: 'kiem', he: 'loi', baseRealm: 5, powerBase: 4800, avatar: '🗡️', shop: 'ren', desc: 'Trưởng các Binh Khí Các Thiên Cơ Thành, băng thanh ngọc khiết, kiếm ý lăng lệ, rèn đúc phi kiếm tuyệt phẩm.' },
    { id: 'npc_tieu_viem', name: 'Tiêu Viêm', title: 'Viêm Đế', gender: 'nam', mon: 'phap', he: 'hoa', baseRealm: 6, powerBase: 6500, avatar: '🔥', shop: 'bao', desc: 'Chủ trì Dị Bảo Các Vạn Yêu Thành, nắm giữ dị hỏa thiên địa, bán các loại bí bảo và phù lục kỳ môn.' },
    { id: 'npc_bach_tieu_thuan', name: 'Bạch Tiểu Thuần', title: 'Trường Sinh Chân Quân', gender: 'nam', mon: 'the', he: 'thuy', baseRealm: 3, powerBase: 2500, avatar: '🛡️', shop: 'dan', desc: 'Sợ chết cầu trường sinh, đan đạo đại tài, cung cấp thuốc trị thương và đan bồi bổ.' },
    { id: 'npc_le_phi_vu', name: 'Lệ Phi Vũ', title: 'Bá Đao Thần Quyền', gender: 'nam', mon: 'quyen', he: 'kim', baseRealm: 3, powerBase: 2200, avatar: '🥋', shop: 'ren', desc: 'Đại sư Xưởng Rèn Lạc Dương Thành, bằng hữu chí cốt của Hàn Lập, chuyên rèn đúc bảo đao chiến giáp.' },
    { id: 'npc_tuyet_co', name: 'Băng Dao Tiên Tử', title: 'Tuyết Sơn Thần Nữ', gender: 'nu', mon: 'kiem', he: 'bang', baseRealm: 4, powerBase: 3600, avatar: '🌸', shop: 'bao', desc: 'Thần nữ Tuyết Sơn, thanh tao thoát tục, bán kỳ trân dị bảo vùng cực bắc tuyết sơn.' },
    { id: 'npc_van_phieu', name: 'Vân Mộng Đạo Quân', title: 'Tiêu Sái Kiếm Tiên', gender: 'nam', mon: 'kiem', he: 'phong', baseRealm: 5, powerBase: 4500, avatar: '🎋', shop: 'ren', desc: 'Kiếm tu phong hoa tuyệt đại, tiêu sái tự tại, truyền thụ bí quyết rèn kiếm phi hành.' },
    { id: 'npc_thanh_ha', name: 'Thanh Hà Thần Nữ', title: 'Dược Thánh Truyền Nhân', gender: 'nu', mon: 'dan', he: 'moc', baseRealm: 3, powerBase: 2100, avatar: '🪷', shop: 'dan', desc: 'Dược thánh đệ tử, dịu dàng thanh thuần, am hiểu đan dược linh thảo ngàn năm.' },
    // Các NPC Thiên Kiêu Thế Giới Bổ Sung
    { id: 'npc_lam_kinh_vu', name: 'Lâm Kinh Vũ', title: 'Trảm Long Kiếm Tôn', gender: 'nam', mon: 'kiem', he: 'kim', baseRealm: 5, powerBase: 5100, avatar: '⚔️', desc: 'Thanh Vân Môn kỳ tài, tay cầm Trảm Long Kiếm, ngạo cốt thiên thành, kiếm trảm chư ma.' },
    { id: 'npc_bich_dao', name: 'Bích Dao', title: 'U Hoàng Thánh Nữ', gender: 'nu', mon: 'phap', he: 'am', baseRealm: 4, powerBase: 3700, avatar: '🌺', desc: 'Quỷ Vương Tông thánh nữ, hoạt bát thông minh, mang theo Thương Tâm Hoa chu du thiên hạ.' },
    { id: 'npc_duong_qua', name: 'Dương Quá', title: 'Độc Cô Thần Kiếm', gender: 'nam', mon: 'kiem', he: 'kim', baseRealm: 6, powerBase: 7100, avatar: '🦅', desc: 'Kiếm hiệp kỳ tài, lãnh ngộ Huyền Thiết Trọng Kiếm và Ám Nhiên Tiêu Hồn Chưởng.' },
    { id: 'npc_tieu_long_nu', name: 'Tiểu Long Nữ', title: 'Ngọc Nữ Tiên Nữ', gender: 'nu', mon: 'kiem', he: 'bang', baseRealm: 6, powerBase: 6900, avatar: '🤍', desc: 'Cổ Mộ truyền nhân, bạch y như tuyết, thanh lãnh thoát tục, Ngọc Nữ Kiếm Pháp xuất thần nhập hóa.' },
    { id: 'npc_thach_hao', name: 'Thạch Hạo', title: 'Hoang Thiên Đế', gender: 'nam', mon: 'the', he: 'loi', baseRealm: 10, powerBase: 45000, avatar: '⚡', desc: 'Độc đoán vạn cổ Hoang Thiên Đế, một quyền trấn áp cửu thiên thập địa.' },
    { id: 'npc_diep_pham', name: 'Diệp Phàm', title: 'Hoang Cổ Thánh Thể', gender: 'nam', mon: 'the', he: 'kim', baseRealm: 9, powerBase: 36000, avatar: '✨', desc: 'Thánh Thể đại thành, đỉnh đầu Vạn Vật Mẫu Khí Đỉnh, quét ngang tinh không viễn cổ.' },
    { id: 'npc_nang_cung_uyen', name: 'Nam Cung Uyển', title: 'Nguyệt Hoa Chân Nhân', gender: 'nu', mon: 'dan', he: 'am', baseRealm: 5, powerBase: 4900, avatar: '🌙', desc: 'Yểm Nguyệt Tông trưởng lão, pháp lực cao thâm, đạo lữ hồng nhan tri kỷ của Hàn Lập.' },
    { id: 'npc_tu_truc', name: 'Hư Trúc', title: 'Tiêu Dao Chưởng Môn', gender: 'nam', mon: 'phap', he: 'thuy', baseRealm: 5, powerBase: 4700, avatar: '📿', desc: 'Tiêu Dao Phái chưởng môn, nắm giữ Bắc Minh Thần Công cùng Sinh Tử Phù huyền diệu.' },
    { id: 'npc_dong_phuong', name: 'Đông Phương Cô Nương', title: 'Nhật Nguyệt Giáo Chủ', gender: 'nu', mon: 'phap', he: 'hoa', baseRealm: 7, powerBase: 11500, avatar: '🏮', desc: 'Tuyệt thế tài hoa, chưởng quản ma môn giáo phái, tú hoa châm xé toạc hư không.' },
    { id: 'npc_doc_co', name: 'Độc Cô Cầu Bại', title: 'Kiếm Ma Vô Địch', gender: 'nam', mon: 'kiem', he: 'kim', baseRealm: 8, powerBase: 22000, avatar: '🗡️', desc: 'Tung hoành thiên hạ hơn ba mươi năm, giết hết cừu khấu, thiên hạ vô địch thủ.' },
    { id: 'npc_thanh_van_tu', name: 'Thanh Vân Tử', title: 'Khai Sơn Đạo Tổ', gender: 'nam', mon: 'phap', he: 'loi', baseRealm: 11, powerBase: 54000, avatar: '⛅', desc: 'Thái Thượng trưởng lão đắc đạo ngàn năm, nhất niệm dẫn động cửu thiên thần lôi.' },
    { id: 'npc_da_nguyet', name: 'Dạ Nguyệt Tiên Cô', title: 'Âm Dương Đạo Tôn', gender: 'nu', mon: 'phap', he: 'duong', baseRealm: 7, powerBase: 12500, avatar: '🌕', desc: 'Nữ tu thông tuệ âm dương nhị khí, dạo chơi ngũ hồ tứ hải, ban phát cơ duyên cho hậu bối.' },
    { id: 'npc_phu_thanh', name: 'Cửu Thiên Phù Thánh', title: 'Phù Đạo Tông Sư', gender: 'nam', mon: 'phu', he: 'phong', baseRealm: 6, powerBase: 7300, avatar: '📜', desc: 'Đại tông sư phù lục, vẽ một lá phù phong ấn vạn dặm sơn hà, thiên lôi giáng thế.' },
    { id: 'npc_luyen_khi_ton', name: 'Thần Chùy Tôn Giả', title: 'Thiên Công Đệ Nhất', gender: 'nam', mon: 'ren', he: 'kim', baseRealm: 7, powerBase: 13000, avatar: '🔨', desc: 'Đại sư rèn đúc thần binh tuyệt phẩm của Cửu Châu, từng đúc ra thượng cổ thần kiếm.' },
    { id: 'npc_dan_hoang', name: 'Dược Thần Đan Hoàng', title: 'Thái Ất Dược Tôn', gender: 'nam', mon: 'dan', he: 'moc', baseRealm: 8, powerBase: 21000, avatar: '🌿', desc: 'Luyện đan xuất thần nhập hóa, có khả năng luyện ra Cửu Chuyển Hoàn Hồn Đan cứu sống người chết.' },
    { id: 'npc_thien_hoa', name: 'Thiên Hỏa Chân Quân', title: 'Tam Muội Thần Hỏa', gender: 'nam', mon: 'phap', he: 'hoa', baseRealm: 6, powerBase: 6800, avatar: '🔥', desc: 'Thuần phục thiên địa linh hỏa, một chưởng đốt cháy thiên địa, vạn vật hóa tro tàn.' },
    { id: 'npc_huyen_vu', name: 'Huyền Vũ Thần Tướng', title: 'Bất Hoại Kim Thân', gender: 'nam', mon: 'the', he: 'tho', baseRealm: 7, powerBase: 11800, avatar: '🐢', desc: 'Luyện thể đại thừa, thân thể tựa Huyền Vũ thần thú vững chãi tựa thái sơn bất hoại.' },
    { id: 'npc_linh_lung', name: 'Cửu Vĩ Linh Lung', title: 'Thanh Khâu Nữ Đế', gender: 'nu', mon: 'phap', he: 'hoa', baseRealm: 8, powerBase: 24000, avatar: '🦊', desc: 'Nữ đế Thanh Khâu Hồ Tộc, nắm giữ Huyễn Mộng Linh Thuật mê hoặc cửu thiên chư thần.' },
    { id: 'npc_tieu_dao', name: 'Tiêu Dao Chân Nhân', title: 'Bắc Minh Thần Tôn', gender: 'nam', mon: 'phap', he: 'thuy', baseRealm: 9, powerBase: 31000, avatar: '🌊', desc: 'Lấy biển lớn làm nhà, tiêu sái ngao du cõi trần, hấp thu linh khí tứ hải dưỡng thần.' },
    { id: 'npc_tuyet_lien', name: 'Tuyết Liên Thánh Nữ', title: 'Băng Tâm Ngọc Nữ', gender: 'nu', mon: 'dan', he: 'bang', baseRealm: 5, powerBase: 4600, avatar: '❄️', desc: 'Thánh nữ Thiên Sơn Tuyết Liên, băng thanh ngọc khiết, đan dược chữa lành vạn vết thương.' },
    { id: 'npc_phuong_hoang', name: 'Phượng Hoàng Tiên Nữ', title: 'Niết Bàn Thần Nữ', gender: 'nu', mon: 'phap', he: 'hoa', baseRealm: 9, powerBase: 33000, avatar: '🦚', desc: 'Kế thừa huyết mạch Phượng Hoàng Niết Bàn, dục hỏa trùng sinh, thần thông bất diệt.' },
    { id: 'npc_loi_chan', name: 'Lôi Chấn Tôn Giả', title: 'Cửu Tiêu Lôi Thần', gender: 'nam', mon: 'kiem', he: 'loi', baseRealm: 8, powerBase: 23500, avatar: '⚡', desc: 'Cầm trong tay Lôi Thần Kiếm, dẫn dắt lôi đình chấn nhiếp bát hoang yêu ma.' },
    { id: 'npc_bach_hoa', name: 'Bách Hoa Tiên Tử', title: 'Vạn Dược Tiên Cô', gender: 'nu', mon: 'dan', he: 'moc', baseRealm: 4, powerBase: 3400, avatar: '💐', desc: 'Bách hoa nở rộ theo từng bước chân, am tường dược thảo hoa cỏ, dịu dàng thanh nhã.' },
    { id: 'npc_tam_thien', name: 'Tam Thiên Kiếm Khách', title: 'Vô Nhai Kiếm Khách', gender: 'nam', mon: 'kiem', he: 'phong', baseRealm: 6, powerBase: 7400, avatar: '🗡️', desc: 'Kiếm xuất như cuồng phong bão táp, ba ngàn kiếm ảnh hư thực bất định.' },
    { id: 'npc_vo_cuc', name: 'Vô Cực Chân Nhân', title: 'Thái Cực Chân Quân', gender: 'nam', mon: 'the', he: 'am', baseRealm: 7, powerBase: 12200, avatar: '☯️', desc: 'Lấy nhu thắng cương, Thái Cực Bát Quái trận đồ hộ thân, bất khả xâm phạm.' },
    // Thực thể tối cao: luôn ở cảnh giới tối đa, có thể quyết đấu nhưng không vào bảng Thiên Kiêu.
    { id: 'npc_thien_dao', name: 'Thiên Đạo', title: 'Chí Cao Chúa Tể', gender: 'nam', mon: 'phap', he: 'thien', baseRealm: 65, maxRealmNpc: true, excludeNpcLeaderboard: true, gearTier: 10, avatar: '🌌', desc: 'Ý chí tối cao của vạn giới, ngự tại đỉnh cảnh giới và quan sát chúng sinh tu hành.' },
    ...DEMON_NPCS,
]);

// Nguyên liệu rơi từ yêu thú để rèn và luyện đan
const CORE_MATERIALS = [
    { id: 'mat_yeu_dan', name: 'Yêu Đan', tier: 'pham', icon: '🔮', desc: 'Nội đan của yêu thú ngưng tụ tinh hoa, dùng để luyện đan và rèn đúc pháp bảo.', sell: 15 },
    { id: 'mat_linh_thao', name: 'Linh Thảo Ngàn Năm', tier: 'pham', icon: '🌿', desc: 'Dược thảo hấp thu linh khí nhật nguyệt, nguyên liệu chính để đan sư phối chế linh đan.', sell: 12 },
    { id: 'mat_van_thiet', name: 'Huyền Thiên Vẫn Thiết', tier: 'pham', icon: '🪨', desc: 'Quặng sắt rơi từ cửu thiên hoặc đáy thâm uyên, dùng để rèn thần binh hộ giáp.', sell: 15 },
    { id: 'mat_huyet_tinh', name: 'Cổ Thần Huyết Tinh', tier: 'huyen', icon: '🩸', desc: 'Huyết tinh ngưng kết từ tinh huyết viễn cổ, rèn bảo vật uy lực vô song.', sell: 50 },
    { id: 'mat_long_lan', name: 'Chân Long Nghịch Lân', tier: 'dia', icon: '🐉', desc: 'Vảy rồng cứng cỏi bất hoại, dùng tạo chiến bào và trọng thuẫn tuyệt phẩm.', sell: 150 },
    { id: 'mat_tien_thach', name: 'Tiên Thiên Đạo Thạch', tier: 'thien', icon: '🌠', desc: 'Đạo thạch kết tinh trong tiên vực, chỉ yêu thú từ Độ Kiếp trở lên mới có thể ngưng tụ.', sell: 500 },
    { id: 'mat_phap_tac_tinh', name: 'Pháp Tắc Tinh Tủy', tier: 'thien', icon: '🌀', desc: 'Tinh tủy ngưng tụ từ pháp tắc Đại La, rơi cực hiếm ở tiên vực thượng tầng.', sell: 1500 },
    { id: 'mat_hon_don_tinh', name: 'Hỗn Độn Nguyên Tinh', tier: 'thien', icon: '🔮', desc: 'Nguyên tinh từ khe nứt hỗn độn, gần như chỉ thấy sau khi đánh bại cự thú viễn cổ.', sell: 5000 },
    { id: 'mat_thien_dao_tinh', name: 'Thiên Đạo Tinh Thạch', tier: 'thien', icon: '⚡', desc: 'Mảnh tinh thạch kết tụ từ lôi ý Thiên Đạo. Chỉ rơi khi hoàn thành Cổ Động cấp cao; cần cho lần đột phá cuối trước Phi Thăng.', sell: 0 },
    { id: 'mat_thien_dao_nguyen_an', name: 'Thiên Đạo Nguyên Ấn', tier: 'thien', icon: '🌌', desc: 'Nguyên ấn ghi dấu thiên mệnh, chỉ có thể nhận khi hạ thủ lĩnh Cổ Động cấp cao. Dâng ấn này cho Thiên Đạo để mở đường Phi Thăng Tiên Giới.', sell: 0 },
    // Mảnh tàn quyển bản thảo
    { id: 'mat_frag_tram_tien_kiem', name: 'Mảnh Đồ Phổ: Trảm Tiên Kiếm', tier: 'thien', icon: '📜', desc: 'Mảnh tàn đồ đúc kiếm Trảm Tiên viễn cổ.', sell: 100 },
    { id: 'mat_frag_hu_thien_dinh', name: 'Mảnh Đồ Phổ: Hư Thiên Đỉnh', tier: 'thien', icon: '📜', desc: 'Mảnh tàn đồ đúc Hư Thiên Đỉnh.', sell: 100 },
    { id: 'mat_frag_cuu_chuyen_chuy', name: 'Mảnh Đồ Phổ: Khai Thiên Chùy', tier: 'thien', icon: '📜', desc: 'Mảnh tàn đồ rèn Khai Thiên Chùy.', sell: 100 },
    { id: 'mat_frag_thai_thuong_but', name: 'Mảnh Đồ Phổ: Thái Thượng Phù Bút', tier: 'thien', icon: '📜', desc: 'Mảnh tàn đồ chế tác Thái Thượng Thần Phù Bút.', sell: 100 },
    { id: 'mat_frag_skill_kiem_vuc', name: 'Mảnh Tàn Thiên: Vạn Kiếm Quy Tông', tier: 'dia', icon: '🧩', desc: 'Mảnh tàn thiên ghi chép tuyệt kỹ Vạn Kiếm Quy Tông.', sell: 80 },
    { id: 'mat_frag_skill_chan_ma', name: 'Mảnh Tàn Thiên: Chân Ma Pháp Tướng', tier: 'dia', icon: '🧩', desc: 'Mảnh tàn thiên ngưng tụ Chân Ma Pháp Tướng.', sell: 80 },
    { id: 'mat_frag_skill_sat_luc', name: 'Mảnh Tàn Thiên: Sát Lục Tiên Quyết', tier: 'huyen', icon: '🧩', desc: 'Mảnh tàn thiên ma đạo Sát Lục Tiên Quyết.', sell: 50 },
    { id: 'mat_frag_dan_truc_co', name: 'Mảnh Đan Phương: Trúc Cơ Đan', tier: 'hoang', icon: '📜', desc: 'Mảnh tàn diệp đan phương Trúc Cơ Đan.', sell: 30 },
    { id: 'mat_frag_dan_giang_tran', name: 'Mảnh Đan Phương: Giáng Trần Đan', tier: 'huyen', icon: '📜', desc: 'Mảnh tàn diệp đan phương Giáng Trần Đan.', sell: 50 },
    { id: 'mat_frag_dan_ket_anh', name: 'Mảnh Đan Phương: Kết Anh Đan', tier: 'dia', icon: '📜', desc: 'Mảnh tàn diệp đan phương Kết Anh Đan.', sell: 80 },
    { id: 'mat_frag_phu_tram_tien', name: 'Mảnh Phù Lục: Trảm Tiên Đạo Phù', tier: 'dia', icon: '📜', desc: 'Mảnh tàn quyển phù lục Trảm Tiên Đạo Phù.', sell: 80 },
    { id: 'mat_frag_phu_dinh_than', name: 'Mảnh Phù Lục: Định Thân Phù', tier: 'hoang', icon: '📜', desc: 'Mảnh tàn quyển phù lục Định Thân Phù.', sell: 30 },
    // Dược liệu/khoáng vật còn thiếu được xác định từ các trang 32–38 của PDF.
    { id: 'mat_cuu_khuc_linh_sam', name: 'Cửu Khúc Linh Sâm', tier: 'huyen', icon: '🌿', desc: 'Linh sâm chín đốt, nguyên liệu luyện đan bồi dưỡng thần niệm.', sell: 90 },
    { id: 'mat_thien_loi_truc', name: 'Thiên Lôi Trúc', tier: 'dia', icon: '🎋', desc: 'Trúc hấp thu lôi kiếp, dùng chế tạo phù lục và pháp bảo hệ Lôi.', sell: 220 },
    { id: 'mat_duong_hon_moc', name: 'Dưỡng Hồn Mộc', tier: 'huyen', icon: '🪵', desc: 'Mộc tâm nuôi dưỡng hồn phách, nguyên liệu luyện đan và pháp khí.', sell: 110 },
    { id: 'mat_linh_nhan_chi_thu', name: 'Linh Nhãn Chi Thụ', tier: 'thien', icon: '🌳', desc: 'Cổ thụ tụ linh, chỉ sinh trưởng trong động thiên tiên vực.', sell: 700 },
    { id: 'mat_thiet_moc', name: 'Thiết Mộc', tier: 'hoang', icon: '🪵', desc: 'Gỗ linh cứng như sắt, dùng làm cán pháp khí và phù cốt.', sell: 28 },
    { id: 'mat_tuyet_linh_thuy', name: 'Tuyết Linh Thủy', tier: 'huyen', icon: '💧', desc: 'Linh tuyền kết tinh ở hàn vực, dùng luyện dược và phù mực.', sell: 75 },
    { id: 'mat_kim_diem_thach', name: 'Kim Diễm Thạch', tier: 'dia', icon: '🔥', desc: 'Khoáng thạch mang kim hỏa, nguyên liệu tôi luyện binh khí.', sell: 180 },
    { id: 'mat_van_nien_huyen_ngoc', name: 'Vạn Niên Huyền Ngọc', tier: 'thien', icon: '💠', desc: 'Ngọc thạch cổ sinh trong linh mạch sâu, dùng rèn pháp bảo phẩm cao.', sell: 800 },
];

// Bổ sung nguyên liệu theo 80 chủng loại × 6 phẩm chất; giữ tổng danh mục ở đúng 500 món.
const MATERIAL_CORES = [
    ...['Thanh Linh Thảo', 'Tử Diệp Lan', 'Nguyệt Nha Hoa', 'Hàn Tủy Liên', 'Xích Diễm Chi', 'Cửu Tinh Thảo', 'Huyết Linh Chi', 'Bích Ngọc Chi', 'Tinh Thần Quả', 'U Minh Hoa', 'Phượng Tiên Quả', 'Long Tiên Thảo', 'Thiên Tâm Quả', 'Vân Mộng Thảo', 'Lưu Ly Quả', 'Bạch Ngọc Lan', 'Tử Tâm Sâm', 'Cửu Diệp Linh Chi', 'Thanh Mộc Tủy', 'Ngũ Sắc Linh Hoa'].map(name => ({ name, family: 'dược liệu', icon: '🌿' })),
    ...['Thanh Cương Thạch', 'Tử Kim Sa', 'Hàn Thiết Tinh', 'Xích Diễm Tinh', 'Lôi Văn Thạch', 'Huyền Băng Tinh', 'Tinh Ngân Thiết', 'Cửu U Hắc Thạch', 'Ngũ Hành Ngọc', 'Hư Không Thạch', 'Tinh Hà Sa', 'Địa Mạch Ngọc', 'Long Văn Kim', 'Phượng Tủy Thạch', 'Thiên Hà Tinh', 'Thái Sơ Thạch', 'Hỗn Độn Cát', 'Bản Nguyên Tinh', 'Đạo Ngân Ngọc', 'Vô Cực Huyền Tinh'].map(name => ({ name, family: 'khoáng thạch', icon: '🪨' })),
    ...['Thiết Mộc Tâm', 'Thanh Đằng Cốt', 'Tử Trúc Tủy', 'Huyền Tùng Chi', 'Long Huyết Mộc', 'Lôi Kích Đào Mộc', 'Nguyệt Quế Linh Mộc', 'Bất Tử Thần Mộc', 'Cửu U Âm Mộc', 'Kiến Mộc Chi Tâm'].map(name => ({ name, family: 'linh mộc', icon: '🪵' })),
    ...['Linh Tuyền Thủy', 'Băng Tâm Ngọc Lộ', 'Huyền Âm Chân Thủy', 'Xích Dương Linh Dịch', 'Thanh Liên Cam Lộ', 'Tinh Hà Chi Thủy', 'Cửu U Minh Thủy', 'Thái Âm Chân Lộ', 'Long Tủy Thánh Thủy', 'Hỗn Nguyên Đạo Dịch'].map(name => ({ name, family: 'linh dịch', icon: '💧' })),
    ...['Yêu Hổ Cốt', 'Huyền Quy Giáp', 'Lôi Ưng Vũ', 'Băng Lang Nha', 'Xích Giao Cân', 'Thiên Hồ Linh Vĩ', 'Kim Sư Tâm Hạch', 'Cửu U Minh Trảo', 'Phượng Hoàng Linh Vũ', 'Thái Cổ Kỳ Lân Huyết'].map(name => ({ name, family: 'linh thú liệu', icon: '🦴' })),
    ...['Hồn Tinh', 'Thần Niệm Kết Tinh', 'Địa Hỏa Tinh Phách', 'Phong Linh Tủy', 'Lôi Kiếp Tinh Sa', 'Ngũ Hành Linh Châu', 'Sinh Mệnh Nguyên Tinh', 'Tử Vong Huyền Tinh', 'Nhân Quả Đạo Tinh', 'Hỗn Độn Tâm Thạch'].map(name => ({ name, family: 'tinh phách', icon: '💠' })),
    ...['Kim Cương Linh Sa', 'Mộc Linh Nguyên Diệp', 'Thủy Nguyệt Hàn Châu', 'Ly Hỏa Tinh Hạch', 'Hậu Thổ Địa Tâm', 'Canh Kim Kiếm Phách', 'Thanh Phong Linh Tủy', 'Tử Điện Lôi Tâm', 'Băng Phách Huyền Châu', 'Thái Dương Chân Tinh'].map(name => ({ name, family: 'ngũ hành linh vật', icon: '✨' })),
    ...['Tịnh Hồn Hương', 'Ngưng Thần Sa', 'Dưỡng Mạch Linh Cao', 'Tẩy Tủy Ngọc Tủy', 'Hộ Tâm Linh Phấn', 'Tụ Khí Linh Lộ', 'Phá Chướng Tinh Trần', 'Hợp Đạo Linh Tủy', 'Trấn Ma Phù Sa', 'Vạn Tượng Đạo Cốt'].map(name => ({ name, family: 'phụ liệu tu luyện', icon: '🔮' })),
];
const MATERIAL_TIER_BANDS = [
    { minRealm: 0, maxRealm: 2 }, { minRealm: 3, maxRealm: 5 },
    { minRealm: 6, maxRealm: 9 }, { minRealm: 10, maxRealm: 15 },
    { minRealm: 16, maxRealm: 23 }, { minRealm: 24, maxRealm: 65 },
];
const GENERATED_MATERIALS = TIER_LADDER.flatMap((tier, tierIndex) => MATERIAL_CORES.map((core, coreIndex) => {
    const band = MATERIAL_TIER_BANDS[tierIndex];
    return {
        id: `mat_ext_${tier}_${String(coreIndex + 1).padStart(2, '0')}`,
        name: `${TIER_VN[tierIndex]} ${core.name}`,
        tier,
        icon: core.icon,
        desc: `${core.family} phẩm ${TIER_VN[tierIndex]}, thu được từ yêu thú cùng tầng cảnh giới; có thể trao đổi, cống hiến hoặc dùng làm nguyên liệu chế tác.`,
        sell: [20, 45, 100, 240, 600, 1500][tierIndex],
        generated: true,
        family: core.family,
        element: ELEMENTS[coreIndex % ELEMENTS.length],
        minRealm: band.minRealm,
        maxRealm: band.maxRealm,
    };
})).slice(0, 500 - CORE_MATERIALS.length);
const MATERIALS = Object.freeze([...CORE_MATERIALS, ...GENERATED_MATERIALS]);
if (MATERIALS.length !== 500) throw new Error(`Danh mục nguyên liệu phải có 500 món, hiện có ${MATERIALS.length}.`);

// Danh mục Bản Thảo & Mảnh Ghép Hợp Nhất
const BLUEPRINTS = Object.freeze([
    { id: 'frag_tram_tien_kiem', matId: 'mat_frag_tram_tien_kiem', name: 'Bản Thảo: Trảm Tiên Kiếm', targetKind: 'equip', targetId: 'tram_tien_kiem', reqCount: 5, minRealm: 8, element: 'kim', desc: 'Gom đủ 5 mảnh đồ phổ để đúc thành Trảm Tiên Kiếm (yêu cầu cảnh giới Hợp Thể trở lên).' },
    { id: 'frag_hu_thien_dinh', matId: 'mat_frag_hu_thien_dinh', name: 'Bản Thảo: Hư Thiên Đỉnh', targetKind: 'equip', targetId: 'hu_thien_dinh', reqCount: 5, minRealm: 8, element: 'moc', desc: 'Gom đủ 5 mảnh đồ phổ để đúc thành Hư Thiên Đỉnh (yêu cầu cảnh giới Hợp Thể trở lên).' },
    { id: 'frag_cuu_chuyen_chuy', matId: 'mat_frag_cuu_chuyen_chuy', name: 'Bản Thảo: Cửu Chuyển Khai Thiên Chùy', targetKind: 'equip', targetId: 'cuu_chuyen_khai_thien_chuy', reqCount: 5, minRealm: 7, element: 'hoa', desc: 'Gom đủ 5 mảnh đồ phổ để đúc thành Cửu Chuyển Khai Thiên Chùy (yêu cầu Luyện Hư trở lên).' },
    { id: 'frag_thai_thuong_but', matId: 'mat_frag_thai_thuong_but', name: 'Bản Thảo: Thái Thượng Thần Phù Bút', targetKind: 'equip', targetId: 'thai_thuong_than_phu_but', reqCount: 5, minRealm: 7, element: 'loi', desc: 'Gom đủ 5 mảnh đồ phổ để chế tác Thái Thượng Thần Phù Bút (yêu cầu Luyện Hư trở lên).' },
    { id: 'frag_skill_kiem_vuc', matId: 'mat_frag_skill_kiem_vuc', name: 'Bản Thảo: Vạn Kiếm Quy Tông', targetKind: 'skill', targetId: 'kiem_vuc', reqCount: 5, minRealm: 6, mon: 'kiem', desc: 'Gom đủ 5 tàn thiên để lĩnh ngộ kiếm chiêu Vạn Kiếm Quy Tông (yêu cầu Kiếm tu Hóa Thần).' },
    { id: 'frag_skill_chan_ma', matId: 'mat_frag_skill_chan_ma', name: 'Bản Thảo: Chân Ma Pháp Tướng', targetKind: 'skill', targetId: 'phap_tuong', reqCount: 5, minRealm: 6, mon: 'the', desc: 'Gom đủ 5 tàn thiên để lĩnh ngộ Chân Ma Pháp Tướng (yêu cầu Thể tu Hóa Thần).' },
    { id: 'frag_skill_sat_luc', matId: 'mat_frag_skill_sat_luc', name: 'Bản Thảo: Sát Lục Tiên Quyết', targetKind: 'skill', targetId: 'ma_sat_luc', reqCount: 3, minRealm: 3, reqMa: 30, desc: 'Gom đủ 3 tàn thiên để lĩnh ngộ ma công Sát Lục Tiên Quyết (yêu cầu Trúc Cơ và Ma Tính ≥ 30).' },
    { id: 'frag_dan_truc_co', matId: 'mat_frag_dan_truc_co', name: 'Bản Thảo: Trúc Cơ Đan', targetKind: 'cons', targetId: 'truc_co_dan', yieldQty: 2, reqCount: 3, minRealm: 2, desc: 'Gom đủ 3 mảnh đan phương để phối chế thành công 2 viên Trúc Cơ Đan (yêu cầu Luyện Khí).' },
    { id: 'frag_dan_giang_tran', matId: 'mat_frag_dan_giang_tran', name: 'Bản Thảo: Giáng Trần Đan', targetKind: 'cons', targetId: 'dan_giang_tran', yieldQty: 1, reqCount: 4, minRealm: 3, desc: 'Gom đủ 4 mảnh đan phương để ngưng kết 1 viên Giáng Trần Đan (yêu cầu Trúc Cơ).' },
    { id: 'frag_dan_ket_anh', matId: 'mat_frag_dan_ket_anh', name: 'Bản Thảo: Kết Anh Đan', targetKind: 'cons', targetId: 'dan_ket_anh', yieldQty: 1, reqCount: 5, minRealm: 4, desc: 'Gom đủ 5 mảnh đan phương để ngưng kết 1 viên Kết Anh Đan (yêu cầu Kim Đan).' },
    { id: 'frag_phu_tram_tien', matId: 'mat_frag_phu_tram_tien', name: 'Bản Thảo: Trảm Tiên Đạo Phù', targetKind: 'cons', targetId: 'phu_tram_tien_dao', yieldQty: 3, reqCount: 4, minRealm: 4, desc: 'Gom đủ 4 tàn quyển để vẽ thành 3 đạo Trảm Tiên Đạo Phù (yêu cầu Kim Đan).' },
    { id: 'frag_phu_dinh_than', matId: 'mat_frag_phu_dinh_than', name: 'Bản Thảo: Định Thân Phù', targetKind: 'cons', targetId: 'phu_dinh_than', yieldQty: 5, reqCount: 3, minRealm: 2, desc: 'Gom đủ 3 tàn quyển để vẽ 5 đạo Định Thân Phù (yêu cầu Luyện Khí).' },
]);
const BLUEPRINT_BY_MAT_ID = new Map(BLUEPRINTS.map(b => [b.matId, b]));
const BLUEPRINT_BY_ID = new Map(BLUEPRINTS.map(b => [b.id, b]));

const ROLE_CRAFT_RECIPES = [...ROLE_GEAR, ...ALCHEMY_CAULDRONS, ...FORGE_GEAR].map((item, i) => {
    const tierIndex = Math.min(5, Math.floor(i % 50 / 9));
    const scale = Math.pow(3, tierIndex);
    const materials = { mat_yeu_dan: Math.max(1, 1 + tierIndex * 2), mat_van_thiet: Math.max(1, 1 + tierIndex * 2) };
    if (tierIndex >= 2) materials.mat_huyet_tinh = Math.max(1, tierIndex - 1);
    if (tierIndex >= 3) materials.mat_long_lan = tierIndex - 2;
    if (item.slot === 'lo_dinh') materials.mat_linh_thao = Math.max(1, 2 + tierIndex * 2);
    return { id: `che_tac_${item.id}`, name: item.name, equipId: item.id, tier: item.tier, materials,
        stones: Math.round(750 * scale), desc: `Công thức chế tác ${item.name}; ${item.slot === 'lo_dinh' ? `năng lực nền ${Math.round(item.alchemyRate * 100)}%, tỷ lệ thành công thực tế còn giảm theo phẩm chất đan.` : 'dành cho nghề nghiệp tương ứng.'}` };
});

// Công thức Xưởng Rèn (Luyện Khí Sư / Thợ Rèn)
const CRAFT_EQUIP_RECIPES = Object.freeze([
    { id: 'ren_lo_dinh_pham', name: 'Thanh Đồng Lô Đỉnh', equipId: 'lo_dinh_pham', tier: 'pham', materials: { mat_yeu_dan: 1, mat_van_thiet: 1 }, stones: 500, desc: 'Rèn lô đỉnh nhập môn; tỷ lệ đan thực tế phụ thuộc phẩm chất đan và chuyên môn.' },
    { id: 'ren_lo_dinh_hoang', name: 'Bách Thảo Hoàng Đỉnh', equipId: 'lo_dinh_hoang', tier: 'hoang', materials: { mat_yeu_dan: 4, mat_van_thiet: 5, mat_linh_thao: 2 }, stones: 2500, desc: 'Đúc lô đỉnh Hoàng phẩm; tỷ lệ đan thực tế phụ thuộc phẩm chất đan và chuyên môn.' },
    { id: 'ren_lo_dinh_huyen', name: 'Tử Kim Huyền Đỉnh', equipId: 'lo_dinh_huyen', tier: 'huyen', materials: { mat_yeu_dan: 8, mat_van_thiet: 8, mat_huyet_tinh: 2 }, stones: 8000, desc: 'Đúc lô đỉnh Huyền phẩm; tỷ lệ đan thực tế phụ thuộc phẩm chất đan và chuyên môn.' },
    { id: 'ren_lo_dinh_dia', name: 'Cửu Long Địa Hỏa Đỉnh', equipId: 'lo_dinh_dia', tier: 'dia', materials: { mat_yeu_dan: 15, mat_van_thiet: 14, mat_huyet_tinh: 5, mat_long_lan: 2 }, stones: 30000, desc: 'Đúc lô đỉnh Địa phẩm; tỷ lệ đan thực tế phụ thuộc phẩm chất đan và chuyên môn.' },
    { id: 'ren_lo_dinh_thien', name: 'Thái Sơ Thiên Đỉnh', equipId: 'lo_dinh_thien', tier: 'thien', materials: { mat_yeu_dan: 25, mat_van_thiet: 24, mat_huyet_tinh: 9, mat_long_lan: 5 }, stones: 95000, desc: 'Đúc lô đỉnh Thiên phẩm; đan phẩm cao vẫn cần chuyên môn luyện chế.' },
    { id: 'ren_lo_dinh_tien', name: 'Hỗn Độn Tiên Lô', equipId: 'lo_dinh_tien', tier: 'tien', materials: { mat_yeu_dan: 40, mat_van_thiet: 38, mat_huyet_tinh: 15, mat_long_lan: 10 }, stones: 250000, desc: 'Đúc tiên lô; đạo đan tối cao vẫn có rủi ro thất bại.' },
    { id: 'ren_chuy_khai_son', name: 'Khai Sơn Huyền Chuy', equipId: 'ren_khai_son_chuy', tier: 'hoang', materials: { mat_yeu_dan: 3, mat_van_thiet: 4 }, stones: 2200, desc: 'Trọng chuy rèn luyện dành cho Luyện Khí Sư.' },
    { id: 'ren_thien_cong', name: 'Thiên Công Phá Giáp Chùy', equipId: 'ren_thien_cong_chuy', tier: 'huyen', materials: { mat_yeu_dan: 8, mat_van_thiet: 8, mat_huyet_tinh: 2 }, stones: 9000, desc: 'Chùy phá giáp do thợ rèn bậc cao chế tác.' },
    { id: 'ren_van_linh_dinh', name: 'Vạn Linh Luyện Đan Đỉnh', equipId: 'dan_van_linh_dinh', tier: 'huyen', materials: { mat_linh_thao: 8, mat_yeu_dan: 6, mat_van_thiet: 5 }, stones: 7500, desc: 'Dược đỉnh chuyên dụng cho Đan Sư.' },
    { id: 'ren_kiem_hoang', name: 'Thanh Phong Kiếm', equipId: 'thanh_phong_kiem', tier: 'hoang', materials: { mat_yeu_dan: 2, mat_van_thiet: 3 }, stones: 1500, desc: 'Kiếm sắc bén lưu chuyển thanh phong.' },
    { id: 'ren_dao_hoang', name: 'Tứ Bác Đao', equipId: 'tu_bac_dao', tier: 'hoang', materials: { mat_yeu_dan: 2, mat_van_thiet: 3 }, stones: 1500, desc: 'Chiến đao chém đứt nhân quả từ Kiếm Lai.' },
    { id: 'ren_giap_hoang', name: 'Cửu Khúc Linh Ẩn Bào', equipId: 'cuu_khuc_bao', tier: 'hoang', materials: { mat_yeu_dan: 2, mat_van_thiet: 3 }, stones: 1500, desc: 'Áo bào ẩn nấp khí tức của Hàn Lập.' },
    { id: 'ren_ho_huyen', name: 'Dưỡng Kiếm Hồ', equipId: 'duong_kiem_ho', tier: 'huyen', materials: { mat_yeu_dan: 5, mat_van_thiet: 5, mat_huyet_tinh: 1 }, stones: 5000, desc: 'Hồ lô dưỡng kiếm linh tính từ Kiếm Lai.' },
    { id: 'ren_phien_huyen', name: 'Cấm Hồn Phiên', equipId: 'cam_hon_phien', tier: 'huyen', materials: { mat_yeu_dan: 5, mat_van_thiet: 5, mat_huyet_tinh: 1 }, stones: 5000, desc: 'Pháp bảo hồn phiên cái thế của Vương Lâm.' },
    { id: 'ren_giap_huyen', name: 'Lục Bào Kiếm Y', equipId: 'luc_bao_kiem_y', tier: 'huyen', materials: { mat_yeu_dan: 5, mat_van_thiet: 5, mat_huyet_tinh: 1 }, stones: 5000, desc: 'Áo xanh kiếm khí hộ thân từ Kiếm Lai.' },
    { id: 'ren_chuy_huyen', name: 'Địa Hỏa Kim Long Chùy', equipId: 'dia_hoa_chuy', tier: 'huyen', materials: { mat_yeu_dan: 5, mat_van_thiet: 6, mat_huyet_tinh: 1 }, stones: 6000, desc: 'Búa rèn ngưng tụ địa hỏa uy lực vô song.' },
    { id: 'ren_kiem_dia', name: 'Thanh Trúc Phong Vân Kiếm', equipId: 'thanh_truc_kiem', tier: 'dia', materials: { mat_yeu_dan: 10, mat_van_thiet: 10, mat_huyet_tinh: 3, mat_long_lan: 1 }, stones: 25000, desc: 'Phi kiếm bản mệnh bằng Kim Lôi Trúc của Hàn Lập.' },
    { id: 'ren_giap_dia', name: 'Thập Nhị Cổ Thần Giáp', equipId: 'thap_nhi_co_than', tier: 'dia', materials: { mat_yeu_dan: 10, mat_van_thiet: 10, mat_huyet_tinh: 3, mat_long_lan: 1 }, stones: 25000, desc: 'Chiến giáp ngưng tụ nhục thân Cổ Thần của Vương Lâm.' },
    { id: 'ren_nhan_hoang', name: 'Nhẫn Trữ Đồ Hoàng Giai', equipId: 'nhan_tru_do_hoang', tier: 'hoang', materials: { mat_yeu_dan: 2, mat_van_thiet: 3 }, stones: 2500, desc: 'Trữ vật giới chỉ đúc bằng uẩn không thạch, mở rộng thêm +60 ô kho đồ.' },
    { id: 'ren_kiem_thien', name: 'Trảm Tiên Kiếm', equipId: 'tram_tien_kiem', tier: 'thien', materials: { mat_yeu_dan: 20, mat_van_thiet: 15, mat_huyet_tinh: 6, mat_long_lan: 3 }, stones: 100000, desc: 'Bảo kiếm trảm tiên sát thần của Vương Lâm.' },
    { id: 'ren_vong_hoang', name: 'Vòng Bạch Ngọc Dưỡng Thần', equipId: 'vong_bach_ngoc', tier: 'hoang', materials: { mat_yeu_dan: 2, mat_van_thiet: 3 }, stones: 2000, desc: 'Được tạc từ ngọc tủy ngàn năm, ngưng tụ thần thức và dưỡng thần bổ khí.' },
    { id: 'ren_vong_huyen', name: 'Vòng Âm Dương Lưỡng Nghi', equipId: 'vong_am_duong', tier: 'huyen', materials: { mat_yeu_dan: 5, mat_van_thiet: 5, mat_huyet_tinh: 1 }, stones: 6000, desc: 'Hai nửa âm dương tương hỗ, cân bằng cương nhu, sinh sôi linh lực.' },
    { id: 'ren_vong_dia', name: 'Vòng Kim Cang Phục Ma', equipId: 'vong_kim_cang', tier: 'dia', materials: { mat_yeu_dan: 10, mat_van_thiet: 10, mat_huyet_tinh: 3, mat_long_lan: 1 }, stones: 25000, desc: 'Bảo vật Phật môn Kim Cang Tông, bất hoại chi thân trừ tà trảm ma.' },
    { id: 'ren_vong_thien', name: 'Vòng Ngũ Sắc Thần Hoàn', equipId: 'vong_ngu_sac_than', tier: 'thien', materials: { mat_yeu_dan: 18, mat_van_thiet: 15, mat_huyet_tinh: 5, mat_long_lan: 3 }, stones: 80000, desc: 'Hào quang ngũ sắc rực rỡ trời đất, bao quát quy luật Ngũ Hành.' },
    { id: 'ren_vong_tien', name: 'Vòng Thái Hư Trấn Giới', equipId: 'vong_thai_hu_than', tier: 'tien', materials: { mat_yeu_dan: 35, mat_van_thiet: 30, mat_huyet_tinh: 10, mat_long_lan: 8 }, stones: 250000, desc: 'Tiên Thiên Linh Bảo vô thượng phong ấn một góc Thái Hư thế giới.' },
    ...ROLE_CRAFT_RECIPES,
]);

// Công thức Luyện Đan (Đan Sư)
const CRAFT_POTION_RECIPES = Object.freeze([
    { id: 'luyen_dan_hoi_xuan', name: 'Hồi Xuân Đan', consId: 'hoi_xuan_dan', yieldQty: 2, materials: { mat_linh_thao: 2, mat_yeu_dan: 1 }, stones: 1000, desc: 'Đan dược trị thương phục hồi 35% HP.' },
    { id: 'luyen_dan_boi_nguyen', name: 'Bồi Nguyên Đan', consId: 'boi_nguyen_dan', yieldQty: 1, materials: { mat_linh_thao: 3, mat_yeu_dan: 2 }, stones: 1750, desc: 'Bồi bổ khí huyết phục hồi 65% HP.' },
    { id: 'luyen_dan_hoi_linh', name: 'Hồi Linh Đan', consId: 'hoi_linh_dan', yieldQty: 2, materials: { mat_linh_thao: 2, mat_yeu_dan: 1 }, stones: 1000, desc: 'Hồi phục linh lực 40% LL trong trận.' },
    { id: 'luyen_dan_truc_co', name: 'Trúc Cơ Đan', consId: 'truc_co_dan', yieldQty: 1, materials: { mat_linh_thao: 4, mat_yeu_dan: 3, mat_huyet_tinh: 1 }, stones: 4000, desc: 'Đan dược phá cảnh của Hàn Lập, tăng 500 EXP.' },
    { id: 'luyen_dan_tay_tuy', name: 'Tẩy Tủy Đan', consId: 'tay_tuy_dan', yieldQty: 1, materials: { mat_linh_thao: 6, mat_yeu_dan: 5, mat_huyet_tinh: 2, mat_long_lan: 1 }, stones: 9000, desc: 'Tẩy kinh phạt tủy, gia tăng ~10% EXP tu vi của cảnh giới hiện tại.' },
    { id: 'luyen_dan_tu_linh', name: 'Tụ Linh Đan', consId: 'dan_tu_linh', yieldQty: 1, materials: { mat_linh_thao: 8, mat_yeu_dan: 6, mat_huyet_tinh: 3, mat_long_lan: 1 }, stones: 17500, desc: 'Tụ hội thiên địa linh khí, tăng 1800 EXP tu vi.' },
    { id: 'luyen_dan_hoang_lat', name: 'Hoàng Lật Hoàn', consId: 'dan_hoang_lat', yieldQty: 1, materials: { mat_linh_thao: 12, mat_yeu_dan: 10, mat_huyet_tinh: 5, mat_long_lan: 2 }, stones: 40000, desc: 'Linh đan Nguyên Anh trân quý, tăng 4000 EXP tu vi.' },
    { id: 'luyen_dan_boi_anh', name: 'Bồi Anh Đan', consId: 'dan_boi_anh', yieldQty: 1, materials: { mat_linh_thao: 16, mat_yeu_dan: 14, mat_huyet_tinh: 7, mat_long_lan: 3 }, stones: 75000, desc: 'Bồi dưỡng Nguyên Anh vững chắc, tăng tối đa 5000 EXP tu vi.' },
    { id: 'luyen_dan_dinh_nhan', name: 'Định Nhan Đan', consId: 'dinh_nhan_dan', yieldQty: 1, materials: { mat_linh_thao: 3, mat_yeu_dan: 2, mat_huyet_tinh: 1 }, stones: 4000, desc: 'Đan dược dung nhan bất lão, tặng đạo lữ +100 thân mật.' },
    { id: 'luyen_dan_cuu_chuyen', name: 'Cửu Chuyển Hồi Hồn Đan', consId: 'cuu_chuyen_dan', yieldQty: 1, materials: { mat_linh_thao: 8, mat_yeu_dan: 6, mat_huyet_tinh: 3, mat_long_lan: 2 }, stones: 15000, desc: 'Cứu mạng tức thì, hồi 100% HP và xóa bỏ trọng thương.' },
    // Đan dược đột phá cảnh giới cho Đan Sư luyện chế
    { id: 'luyen_dan_tay_tuy_hoan', name: 'Tẩy Tủy Hoàn', consId: 'dan_tay_tuy_hoan', yieldQty: 1, materials: { mat_linh_thao: 2, mat_yeu_dan: 1 }, stones: 1000, desc: 'Đan dược gột rửa phàm cốt, đột phá Luyện Thể cảnh.' },
    { id: 'luyen_dan_pha_chuong', name: 'Phá Chướng Đan', consId: 'dan_pha_chuong', yieldQty: 1, materials: { mat_linh_thao: 3, mat_yeu_dan: 2 }, stones: 2000, desc: 'Phá vỡ chướng ngại kinh mạch, đột phá Luyện Khí cảnh.' },
    { id: 'luyen_dan_giang_tran', name: 'Giáng Trần Đan', consId: 'dan_giang_tran', yieldQty: 1, materials: { mat_linh_thao: 4, mat_yeu_dan: 3, mat_huyet_tinh: 1 }, stones: 5000, desc: 'Linh đan ngưng kết Kim Đan cửu chuyển.' },
    { id: 'luyen_dan_ket_anh', name: 'Kết Anh Đan', consId: 'dan_ket_anh', yieldQty: 1, materials: { mat_linh_thao: 6, mat_yeu_dan: 4, mat_huyet_tinh: 2 }, stones: 10000, desc: 'Phá toái Kim Đan hóa thành Nguyên Anh.' },
    { id: 'luyen_dan_hoa_than', name: 'Hóa Thần Đan', consId: 'dan_hoa_than', yieldQty: 1, materials: { mat_linh_thao: 8, mat_yeu_dan: 6, mat_huyet_tinh: 3 }, stones: 20000, desc: 'Lĩnh ngộ ý cảnh tiến vào Hóa Thần.' },
    { id: 'luyen_dan_ngu_hanh_lo', name: 'Ngũ Hành Linh Lộ', consId: 'dan_ngu_hanh_lo', yieldQty: 1, materials: { mat_linh_thao: 10, mat_yeu_dan: 8, mat_huyet_tinh: 4, mat_long_lan: 1 }, stones: 40000, desc: 'Dung hợp thiên địa nguyên khí tiến vào Luyện Hư.' },
    { id: 'luyen_dan_co_than_tinh_huyet', name: 'Cổ Thần Tinh Huyết', consId: 'dan_co_than_tinh_huyet', yieldQty: 1, materials: { mat_linh_thao: 12, mat_yeu_dan: 10, mat_huyet_tinh: 6, mat_long_lan: 2 }, stones: 75000, desc: 'Rèn đúc thần thể tiến vào Hợp Thể kỳ.' },
    { id: 'luyen_dan_dai_thua', name: 'Đại Thừa Vấn Đạo Đan', consId: 'dan_dai_thua', yieldQty: 1, materials: { mat_linh_thao: 15, mat_yeu_dan: 12, mat_huyet_tinh: 8, mat_long_lan: 3 }, stones: 150000, desc: 'Vấn đạo thiên địa, thành tựu Đại Thừa tôn sư.' },
    { id: 'luyen_dan_do_kiep', name: 'Độ Kiếp Thần Đan', consId: 'dan_do_kiep', yieldQty: 1, materials: { mat_linh_thao: 18, mat_yeu_dan: 15, mat_huyet_tinh: 10, mat_long_lan: 4 }, stones: 200000, desc: 'Nghịch thiên kháng lôi kiếp, tẩy lễ thân hồn tiến vào Độ Kiếp kỳ.' },
    { id: 'luyen_dan_ban_tien', name: 'Bán Tiên Hóa Cốt Đan', consId: 'dan_ban_tien', yieldQty: 1, materials: { mat_linh_thao: 20, mat_yeu_dan: 18, mat_huyet_tinh: 12, mat_long_lan: 5 }, stones: 250000, desc: 'Thoát thai hoán cốt khỏi phàm trần, chạm tay tới Tiên đạo sơ cảnh.' },
    { id: 'luyen_dan_dang_tien', name: 'Đăng Tiên Lôi Tâm Đan', consId: 'dan_dang_tien', yieldQty: 1, materials: { mat_linh_thao: 22, mat_yeu_dan: 20, mat_huyet_tinh: 14, mat_long_lan: 6, mat_tien_thach: 1 }, stones: 350000, desc: 'Dùng Đạo Thạch dẫn lôi kiếp, luyện thành Lôi Tâm Đan mở cửa Đăng Tiên.' },
    { id: 'luyen_dan_bo_thien', name: 'Bổ Thiên Thần Đan', consId: 'dan_bo_thien', yieldQty: 1, materials: { mat_linh_thao: 22, mat_yeu_dan: 20, mat_huyet_tinh: 14, mat_long_lan: 6 }, stones: 300000, desc: 'Đúc lại tiên thể từ tro tàn lôi kiếp, chứng đạo Tán Tiên bất tử.' },
    { id: 'luyen_dan_dia_tien', name: 'Địa Tiên Ngưng Thể Đan', consId: 'dan_dia_tien', yieldQty: 1, materials: { mat_linh_thao: 25, mat_yeu_dan: 22, mat_huyet_tinh: 16, mat_long_lan: 7 }, stones: 350000, desc: 'Hấp thu địa mạch phúc địa tiên khí, ngưng tụ Địa Tiên pháp thân.' },
    { id: 'luyen_dan_nhan_tien', name: 'Nhân Tiên Thuần Dương Dịch', consId: 'dan_nhan_tien', yieldQty: 1, materials: { mat_linh_thao: 28, mat_yeu_dan: 25, mat_huyet_tinh: 18, mat_long_lan: 8 }, stones: 400000, desc: 'Thuần dương tiên khí gột rửa linh hồn, thành tựu Nhân Tiên tôn vị.' },
    { id: 'luyen_dan_chan_tien', name: 'Chân Tiên Hóa Cảnh Đan', consId: 'dan_chan_tien', yieldQty: 1, materials: { mat_linh_thao: 30, mat_yeu_dan: 28, mat_huyet_tinh: 20, mat_long_lan: 9 }, stones: 450000, desc: 'Tiên đan ngưng tụ Chân Tiên pháp tắc, đạp phá trần gian phi thăng Tiên Giới.' },
    { id: 'luyen_dan_huyen_tien', name: 'Huyền Tiên Ngọc Tủy', consId: 'dan_huyen_tien', yieldQty: 1, materials: { mat_linh_thao: 32, mat_yeu_dan: 30, mat_huyet_tinh: 22, mat_long_lan: 10 }, stones: 500000, desc: 'Ngọc tủy huyền diệu quán triệt huyền tiên chi lực tinh thuần.' },
    { id: 'luyen_dan_thien_tien', name: 'Thiên Tiên Dịch', consId: 'dan_thien_tien', yieldQty: 1, materials: { mat_linh_thao: 35, mat_yeu_dan: 32, mat_huyet_tinh: 24, mat_long_lan: 11 }, stones: 550000, desc: 'Linh dịch hấp thụ thiên tiên khí, đột phá Thiên Tiên cảnh giới.' },
    { id: 'luyen_dan_kim_tien', name: 'Kim Tiên Thái Ất Đan', consId: 'dan_kim_tien', yieldQty: 1, materials: { mat_linh_thao: 38, mat_yeu_dan: 35, mat_huyet_tinh: 26, mat_long_lan: 12 }, stones: 600000, desc: 'Bất hủ kim tính, chứng đạo Kim Tiên bất hoại bất diệt.' },
    { id: 'luyen_dan_thai_at_chan_tien', name: 'Thái Ất Chân Tiên Lộ', consId: 'dan_thai_at_chan_tien', yieldQty: 1, materials: { mat_linh_thao: 40, mat_yeu_dan: 38, mat_huyet_tinh: 28, mat_long_lan: 13 }, stones: 650000, desc: 'Giọt sương Thái Ất đốn ngộ Chân Tiên đạo, siêu việt phàm trần chư thiên.' },
    { id: 'luyen_dan_thai_at_huyen_tien', name: 'Thái Ất Huyền Tiên Tinh', consId: 'dan_thai_at_huyen_tien', yieldQty: 1, materials: { mat_linh_thao: 42, mat_yeu_dan: 40, mat_huyet_tinh: 30, mat_long_lan: 14 }, stones: 700000, desc: 'Huyền tiên tinh hoa huyền ảo, phá vỡ hư không bước vào Thái Ất Huyền Tiên.' },
    { id: 'luyen_dan_thai_at', name: 'Thái Ất Hỗn Nguyên Đan', consId: 'dan_thai_at', yieldQty: 1, materials: { mat_linh_thao: 45, mat_yeu_dan: 42, mat_huyet_tinh: 32, mat_long_lan: 15 }, stones: 750000, desc: 'Hỗn nguyên quy nhất, thành tựu Thái Ất Kim Tiên vạn kiếp bất ma.' },
    { id: 'luyen_dan_dai_la_chan_tien', name: 'Đại La Chân Tiên Quả', consId: 'dan_dai_la_chan_tien', yieldQty: 1, materials: { mat_linh_thao: 48, mat_yeu_dan: 45, mat_huyet_tinh: 34, mat_long_lan: 16 }, stones: 800000, desc: 'Tiên quả đơm hoa nơi vĩnh hằng, mở ra con đường Đại La vô lượng.' },
    { id: 'luyen_dan_dai_la', name: 'Đại La Đạo Quả', consId: 'dan_dai_la', yieldQty: 1, materials: { mat_linh_thao: 50, mat_yeu_dan: 48, mat_huyet_tinh: 36, mat_long_lan: 17 }, stones: 850000, desc: 'Đạo quả viên mãn, siêu thoát tam giới ngũ hành thời không luân hồi.' },
    { id: 'luyen_dan_hon_nguyen', name: 'Hỗn Nguyên Kim Tiên Thai', consId: 'dan_hon_nguyen', yieldQty: 1, materials: { mat_linh_thao: 52, mat_yeu_dan: 50, mat_huyet_tinh: 38, mat_long_lan: 18 }, stones: 900000, desc: 'Đạo thai Hỗn Nguyên sơ khai, ngưng tụ pháp tắc vô thượng.' },
    { id: 'luyen_dan_tien_quan', name: 'Tiên Quân Pháp Tắc Đan', consId: 'dan_tien_quan', yieldQty: 1, materials: { mat_linh_thao: 55, mat_yeu_dan: 52, mat_huyet_tinh: 40, mat_long_lan: 19 }, stones: 950000, desc: 'Thống ngự ngàn vạn tiên binh tiên tướng, xưng hào Tiên Quân.' },
    { id: 'luyen_dan_tien_ton', name: 'Tiên Tôn Hóa Đạo Châu', consId: 'dan_tien_ton', yieldQty: 1, materials: { mat_linh_thao: 58, mat_yeu_dan: 55, mat_huyet_tinh: 42, mat_long_lan: 20 }, stones: 1000000, desc: 'Viên ngọc Tiên Tôn hóa đạo, một ý niệm chuyển dời muôn vàn tinh cầu.' },
    { id: 'luyen_dan_chuan_tien_vuong', name: 'Chuẩn Tiên Vương Huyết Tinh', consId: 'dan_chuan_tien_vuong', yieldQty: 1, materials: { mat_linh_thao: 60, mat_yeu_dan: 58, mat_huyet_tinh: 45, mat_long_lan: 22 }, stones: 1100000, desc: 'Tinh huyết vương giả cổ xưa, nửa bước bước vào cảnh giới Vương Giả.' },
    { id: 'luyen_dan_tien_vuong', name: 'Chân Linh Vương Tinh', consId: 'dan_tien_vuong', yieldQty: 1, materials: { mat_linh_thao: 65, mat_yeu_dan: 60, mat_huyet_tinh: 48, mat_long_lan: 24 }, stones: 1200000, desc: 'Tinh hoa vương giả tiên giới, xưng vương chư thiên vạn cõi.' },
    ...POST_TIEN_DE_REALMS.map((realmName, index) => {
        const toRealm = index + 31;
        const consId = `dan_hau_tien_de_${toRealm}`;
        return {
            id: `luyen_${consId}`, name: `${realmName} Đạo Đan`, consId, yieldQty: 1,
            materials: {
                mat_linh_thao: 75 + index * 2, mat_yeu_dan: 70 + index * 2,
                mat_tien_thach: 3 + Math.floor(index / 5), mat_phap_tac_tinh: 1 + Math.floor(index / 8),
                ...(toRealm >= 39 ? { mat_hon_don_tinh: 1 + Math.floor((toRealm - 39) / 8) } : {}),
            },
            stones: 2000000 + index * 500000,
            desc: `Dùng lô đỉnh luyện đạo đan ngưng tụ pháp tắc ${realmName}.`,
        };
    }),
    { id: 'luyen_dan_tien_de', name: 'Hỗn Độn Tiên Đế Thai', consId: 'dan_tien_de', yieldQty: 1, materials: { mat_linh_thao: 70, mat_yeu_dan: 65, mat_huyet_tinh: 50, mat_long_lan: 25 }, stones: 1500000, desc: 'Hỗn độn sơ khai, ngự trị vạn giới, Tiên Đế chí cao vô thượng vô địch thiên hạ.' },
]);

const NPC_BY_ID = new Map(WORLD_NPCS.map(n => [n.id, n]));

// Danh mục Đan Dược Đột Phá Cảnh Giới
const BREAKTHROUGH_ITEMS = Object.freeze([
    { toRealm: 1, id: 'dan_tay_tuy_hoan', name: 'Tẩy Tủy Hoàn', icon: '💊', realmName: 'Luyện Thể', desc: 'Đan dược gột rửa phàm cốt, giúp phàm nhân bước vào Luyện Thể cảnh.' },
    { toRealm: 2, id: 'dan_pha_chuong', name: 'Phá Chướng Đan', icon: '🧪', realmName: 'Luyện Khí', desc: 'Phá vỡ chướng ngại kinh mạch, đả thông thiên địa khí hải tiến vào Luyện Khí.' },
    { toRealm: 3, id: 'truc_co_dan', name: 'Trúc Cơ Đan', icon: '💊', realmName: 'Trúc Cơ', desc: 'Thần đan nghịch thiên danh chấn Việt Quốc, bảo vật đột phá Trúc Cơ cảnh của Hàn Lập.' },
    { toRealm: 4, id: 'dan_giang_tran', name: 'Giáng Trần Đan', icon: '🌟', realmName: 'Kim Đan', desc: 'Linh đan ngưng kết Kim Đan cửu chuyển trong Phàm Nhân Tu Tiên.' },
    { toRealm: 5, id: 'dan_ket_anh', name: 'Kết Anh Đan', icon: '✨', realmName: 'Nguyên Anh', desc: 'Đan dược phá toái Kim Đan hóa thành Nguyên Anh thần thông quảng đại.' },
    { toRealm: 6, id: 'dan_hoa_than', name: 'Hóa Thần Đan', icon: '🔮', realmName: 'Hóa Thần', desc: 'Thần đan đốn ngộ sinh tử ý cảnh trong Tiên Nghịch của Vương Lâm, bước vào Hóa Thần.' },
    { toRealm: 7, id: 'dan_ngu_hanh_lo', name: 'Ngũ Hành Linh Lộ', icon: '💧', realmName: 'Luyện Hư', desc: 'Tinh hoa ngũ hành tương sinh, giúp dung hợp thiên địa nguyên khí tiến vào Luyện Hư.' },
    { toRealm: 8, id: 'dan_co_than_tinh_huyet', name: 'Cổ Thần Tinh Huyết', icon: '🩸', realmName: 'Hợp Thể', desc: 'Giọt tinh huyết Cổ Thần bát tinh, rèn đúc thần thể tiến vào Hợp Thể kỳ.' },
    { toRealm: 9, id: 'dan_dai_thua', name: 'Đại Thừa Vấn Đạo Đan', icon: '👑', realmName: 'Đại Thừa', desc: 'Vấn đạo thiên địa, lĩnh ngộ pháp tắc quy tắc, thành tựu Đại Thừa tôn sư.' },
    { toRealm: 10, id: 'dan_do_kiep', name: 'Độ Kiếp Thần Đan', icon: '⚡', realmName: 'Độ Kiếp', desc: 'Nghịch thiên kháng lôi kiếp, tẩy lễ thân hồn tiến vào Độ Kiếp kỳ.' },
    { toRealm: 11, id: 'dan_ban_tien', name: 'Bán Tiên Hóa Cốt Đan', icon: '✨', realmName: 'Bán Tiên', desc: 'Bước đầu thoát thai hoán cốt khỏi phàm trần, chạm tay tới Tiên đạo sơ cảnh.' },
    { toRealm: 12, id: 'dan_dang_tien', name: 'Đăng Tiên Lôi Tâm Đan', icon: '🌠', realmName: 'Đăng Tiên', desc: 'Lôi tâm ngưng tụ qua chín tầng thiên kiếp, mở cánh cửa Đăng Tiên.' },
    { toRealm: 13, id: 'dan_bo_thien', name: 'Bổ Thiên Thần Đan', icon: '🌌', realmName: 'Tán Tiên', desc: 'Đan dược đúc lại tiên thể từ tro tàn lôi kiếp, chứng đạo Tán Tiên bất tử.' },
    { toRealm: 14, id: 'dan_dia_tien', name: 'Địa Tiên Ngưng Thể Đan', icon: '⛰️', realmName: 'Địa Tiên', desc: 'Hấp thu địa mạch phúc địa tiên khí, ngưng tụ Địa Tiên pháp thân.' },
    { toRealm: 15, id: 'dan_nhan_tien', name: 'Nhân Tiên Thuần Dương Dịch', icon: '☀️', realmName: 'Nhân Tiên', desc: 'Thuần dương tiên khí gột rửa linh hồn, thành tựu Nhân Tiên tôn vị.' },
    { toRealm: 16, id: 'dan_chan_tien', name: 'Chân Tiên Hóa Cảnh Đan', icon: '💎', realmName: 'Chân Tiên', desc: 'Tiên đan ngưng tụ Chân Tiên pháp tắc, đạp phá trần gian phi thăng Tiên Giới.' },
    { toRealm: 17, id: 'dan_huyen_tien', name: 'Huyền Tiên Ngọc Tủy', icon: '💠', realmName: 'Huyền Tiên', desc: 'Ngọc tủy huyền diệu quán triệt huyền tiên chi lực tinh thuần.' },
    { toRealm: 18, id: 'dan_thien_tien', name: 'Thiên Tiên Dịch', icon: '🍃', realmName: 'Thiên Tiên', desc: 'Linh dịch hấp thụ thiên tiên khí, đột phá Thiên Tiên cảnh giới.' },
    { toRealm: 19, id: 'dan_kim_tien', name: 'Kim Tiên Thái Ất Đan', icon: '🟡', realmName: 'Kim Tiên', desc: 'Bất hủ kim tính, chứng đạo Kim Tiên bất hoại bất diệt.' },
    { toRealm: 20, id: 'dan_thai_at_chan_tien', name: 'Thái Ất Chân Tiên Lộ', icon: '🌀', realmName: 'Thái Ất Chân Tiên', desc: 'Giọt sương Thái Ất đốn ngộ Chân Tiên đạo, siêu việt phàm trần chư thiên.' },
    { toRealm: 21, id: 'dan_thai_at_huyen_tien', name: 'Thái Ất Huyền Tiên Tinh', icon: '🔮', realmName: 'Thái Ất Huyền Tiên', desc: 'Huyền tiên tinh hoa huyền ảo, phá vỡ hư không bước vào Thái Ất Huyền Tiên.' },
    { toRealm: 22, id: 'dan_thai_at', name: 'Thái Ất Hỗn Nguyên Đan', icon: '☯️', realmName: 'Thái Ất Kim Tiên', desc: 'Hỗn nguyên quy nhất, thành tựu Thái Ất Kim Tiên vạn kiếp bất ma.' },
    { toRealm: 23, id: 'dan_dai_la_chan_tien', name: 'Đại La Chân Tiên Quả', icon: '🍏', realmName: 'Đại La Chân Tiên', desc: 'Tiên quả đơm hoa nơi vĩnh hằng, mở ra con đường Đại La vô lượng.' },
    { toRealm: 24, id: 'dan_dai_la', name: 'Đại La Đạo Quả', icon: '🍎', realmName: 'Đại La Kim Tiên', desc: 'Đạo quả viên mãn, siêu thoát tam giới ngũ hành thời không luân hồi.' },
    { toRealm: 25, id: 'dan_hon_nguyen', name: 'Hỗn Nguyên Kim Tiên Thai', icon: '🥚', realmName: 'Hỗn Nguyên Kim Tiên', desc: 'Đạo thai Hỗn Nguyên sơ khai, ngưng tụ pháp tắc vô thượng.' },
    { toRealm: 26, id: 'dan_tien_quan', name: 'Tiên Quân Pháp Tắc Đan', icon: '⚔️', realmName: 'Tiên Quân', desc: 'Thống ngự ngàn vạn tiên binh tiên tướng, xưng hào Tiên Quân uy chấn bát hoang.' },
    { toRealm: 27, id: 'dan_tien_ton', name: 'Tiên Tôn Hóa Đạo Châu', icon: '🔮', realmName: 'Tiên Tôn', desc: 'Viên ngọc Tiên Tôn hóa đạo, một ý niệm chuyển dời muôn vàn tinh cầu.' },
    { toRealm: 28, id: 'dan_chuan_tien_vuong', name: 'Chuẩn Tiên Vương Huyết Tinh', icon: '🩸', realmName: 'Chuẩn Tiên Vương', desc: 'Tinh huyết vương giả cổ xưa, nửa bước bước vào cảnh giới Vương Giả.' },
    { toRealm: 29, id: 'dan_tien_vuong', name: 'Chân Linh Vương Tinh', icon: '👑', realmName: 'Tiên Vương', desc: 'Tinh hoa vương giả tiên giới, xưng vương chư thiên vạn cõi.' },
    { toRealm: 30, id: 'dan_tien_de', name: 'Hỗn Độn Tiên Đế Thai', icon: '🌌', realmName: 'Tiên Đế', desc: 'Hỗn độn sơ khai, ngự trị vạn giới, Tiên Đế chí cao vô thượng vô địch thiên hạ.' },
    ...POST_TIEN_DE_REALMS.map((realmName, index) => ({
        toRealm: index + 31, id: `dan_hau_tien_de_${index + 31}`, name: `${realmName} Đạo Đan`, icon: '💠', realmName,
        desc: `Đạo đan ngưng tụ pháp tắc ${realmName}, dùng để đột phá cảnh giới hậu Tiên Đế.`,
    })),
]);
const BREAKTHROUGH_BY_REALM = Object.freeze(
    Object.fromEntries(BREAKTHROUGH_ITEMS.map(b => [b.toRealm, b]))
);

function makeMonsterDrops(id, realm, element, isSmall, isBoss, isWorldBoss, customDrops = []) {
    const list = customDrops && customDrops.length > 0 ? [...customDrops] : [];
    const hasCustomDrops = customDrops && customDrops.length > 0;
    const substagePill = SUBSTAGE_BY_REALM[realm];
    // Đan đột phá tiểu cảnh — cực hiếm, không thể farming theo cào
    if (substagePill) list.push({ kind: 'cons', id: substagePill.id, rate: 0.008 * Math.pow(0.96, realm), qty: 1 });
    const breakthroughPill = BREAKTHROUGH_BY_REALM[Math.min(65, realm + 1)] || BREAKTHROUGH_BY_REALM[realm];
    if (breakthroughPill && isBoss) {
        const rarityScale = Math.pow(0.90, Math.max(0, breakthroughPill.toRealm - 3));
        // Đan đột phá đại cảnh giới — tỷ lệ rơi được cân bằng nghiêm ngặt theo thế giới kiếm hiệp
        list.push({ kind: 'cons', id: breakthroughPill.id, rate: Math.max(RAREST, (isWorldBoss ? 0.004 : 0.0015) * rarityScale), qty: 1 });
    }
    const masteryDrop = element && HE[element] ? {
        kind: 'cons', id: `dan_thong_thao_${element}`,
        rate: isWorldBoss ? 0.025 : (isSmall ? 0.01 : 0.015),
        qty: 1, directChance: true, elementMastery: true,
    } : null;
    const addMasteryDrop = () => { if (masteryDrop) list.push(masteryDrop); };
    // Nguyên liệu cấp cao — hiếm, cần nhiều trận mới có được
    if (realm >= 10) list.push({ kind: 'mat', id: 'mat_tien_thach', rate: isWorldBoss ? 0.035 : (isBoss ? 0.012 : 0.002), qty: 1 });
    if (realm >= 18) list.push({ kind: 'mat', id: 'mat_phap_tac_tinh', rate: isWorldBoss ? 0.018 : (isBoss ? 0.006 : 0.0008), qty: 1 });
    if (realm >= 24) list.push({ kind: 'mat', id: 'mat_hon_don_tinh', rate: isWorldBoss ? 0.008 : (isBoss ? 0.003 : 0.0004), qty: 1 });
    if (hasCustomDrops) {
        addMasteryDrop();
        return list;
    }
    if (isWorldBoss) {
        list.push({ kind: 'mat', id: 'mat_yeu_dan', rate: 0.85, min: 1, max: 2 });
        list.push({ kind: 'mat', id: 'mat_linh_thao', rate: 0.70, min: 1, max: 2 });
        list.push({ kind: 'mat', id: 'mat_van_thiet', rate: 0.70, min: 1, max: 2 });
    } else if (isSmall) {
        list.push({ kind: 'mat', id: 'mat_yeu_dan', rate: 0.30, qty: 1 });
        list.push({ kind: 'mat', id: 'mat_linh_thao', rate: ['moc', 'thuy', 'hoa'].includes(element) ? 0.50 : 0.30, qty: 1 });
        list.push({ kind: 'mat', id: 'mat_van_thiet', rate: ['kim', 'tho', 'loi', 'bang', 'phong'].includes(element) ? 0.50 : 0.30, qty: 1 });
    } else {
        list.push({ kind: 'mat', id: 'mat_yeu_dan', rate: 0.60, qty: 1 });
        list.push({ kind: 'mat', id: 'mat_linh_thao', rate: 0.50, qty: 1 });
        list.push({ kind: 'mat', id: 'mat_van_thiet', rate: 0.50, qty: 1 });
    }

    if (realm >= 3) {
        const rareEra = realm >= 18 ? 2 : (realm >= 10 ? 1 : 0);
        const rate = rareEra === 2
            ? (isWorldBoss ? 0.04 : (isSmall ? 0.0008 : 0.008))
            : (rareEra === 1
                ? (isWorldBoss ? 0.04 : (isSmall ? 0.001 : 0.008))
                : (isWorldBoss ? 0.04 : (isSmall ? 0.008 : 0.018)));
        list.push({ kind: 'mat', id: 'mat_huyet_tinh', rate, qty: 1 });
    }
    if (realm >= 5 || isWorldBoss) {
        const rareEra = realm >= 18 ? 2 : (realm >= 10 ? 1 : 0);
        const rate = rareEra === 2
            ? (isWorldBoss ? 0.025 : (isSmall ? 0.0004 : 0.004))
            : (rareEra === 1
                ? (isWorldBoss ? 0.02 : (isSmall ? 0.0005 : 0.004))
                : (isWorldBoss ? 0.025 : (isSmall ? 0.004 : 0.01)));
        // Chân Long Nghịch Lân là nguyên liệu hiếm; hạ toàn bộ nguồn rơi xuống 1/4.
        list.push({ kind: 'mat', id: 'mat_long_lan', rate: rate * 0.25, qty: 1 });
    }

    if (isSmall) {
        list.push({ kind: 'cons', id: 'hoi_xuan_dan', rate: 0.15, qty: 1 });
        list.push({ kind: 'cons', id: 'hoi_linh_dan', rate: 0.10, qty: 1 });
        list.push({ kind: 'cons', id: 'hoi_luc_dan', rate: 0.08, qty: 1 });
        if (element === 'hoa') list.push({ kind: 'cons', id: 'phu_hoa_cau', rate: 0.10, qty: 1 });
        else if (element === 'bang' || element === 'thuy') list.push({ kind: 'cons', id: 'phu_bang_tien', rate: 0.10, qty: 1 });
        else if (element === 'loi') list.push({ kind: 'cons', id: 'phu_te_liet', rate: 0.10, qty: 1 });
        else if (element === 'kim') list.push({ kind: 'cons', id: 'phu_kim_cang', rate: 0.10, qty: 1 });
        else if (element === 'tho') list.push({ kind: 'cons', id: 'phu_binh_an', rate: 0.10, qty: 1 });
        else list.push({ kind: 'cons', id: 'phu_cuong_luc', rate: 0.10, qty: 1 });
    } else {
        list.push({ kind: 'cons', id: 'hoi_xuan_dan', rate: 0.12, qty: 1 });
        list.push({ kind: 'cons', id: 'hoi_linh_dan', rate: 0.08, qty: 1 });
        if (realm >= 2) list.push({ kind: 'cons', id: 'boi_nguyen_dan', rate: 0.05, qty: 1 });
        list.push({ kind: 'cons', id: 'phu_dinh_than', rate: 0.05, qty: 1 });
    }

    // Rơi mảnh bản thảo ngẫu nhiên từ yêu thú
    if (isWorldBoss) {
        list.push({ kind: 'mat', id: 'mat_frag_tram_tien_kiem', rate: 0.15, qty: 1 });
        list.push({ kind: 'mat', id: 'mat_frag_hu_thien_dinh', rate: 0.15, qty: 1 });
        list.push({ kind: 'mat', id: 'mat_frag_cuu_chuyen_chuy', rate: 0.15, qty: 1 });
        list.push({ kind: 'mat', id: 'mat_frag_thai_thuong_but', rate: 0.15, qty: 1 });
    } else if (isBoss && !isSmall) {
        if (realm >= 5) {
            list.push({ kind: 'mat', id: 'mat_frag_skill_kiem_vuc', rate: 0.08, qty: 1 });
            list.push({ kind: 'mat', id: 'mat_frag_skill_chan_ma', rate: 0.08, qty: 1 });
            list.push({ kind: 'mat', id: 'mat_frag_dan_ket_anh', rate: 0.10, qty: 1 });
            list.push({ kind: 'mat', id: 'mat_frag_phu_tram_tien', rate: 0.10, qty: 1 });
        } else if (realm >= 3) {
            list.push({ kind: 'mat', id: 'mat_frag_skill_sat_luc', rate: 0.10, qty: 1 });
            list.push({ kind: 'mat', id: 'mat_frag_dan_giang_tran', rate: 0.12, qty: 1 });
            list.push({ kind: 'mat', id: 'mat_frag_phu_dinh_than', rate: 0.12, qty: 1 });
        }
    } else if (isSmall) {
        if (realm === 0) {
            list.push({ kind: 'cons', id: 'dan_tay_tuy_hoan', rate: 0.08, qty: 1 });
        } else if (realm === 1) {
            list.push({ kind: 'cons', id: 'dan_pha_chuong', rate: 0.06, qty: 1 });
        }
        if (realm >= 2) {
            list.push({ kind: 'mat', id: 'mat_frag_dan_truc_co', rate: 0.06, qty: 1 });
            list.push({ kind: 'mat', id: 'mat_frag_phu_dinh_than', rate: 0.06, qty: 1 });
        }
        // Quái rơi vũ khí cho Đan, Rèn, Phù
        if (realm === 0) {
            list.push({ kind: 'equip', id: 'moc_bua', rate: 0.05, qty: 1 });
            list.push({ kind: 'equip', id: 'truc_but', rate: 0.05, qty: 1 });
            list.push({ kind: 'equip', id: 'dong_dinh', rate: 0.05, qty: 1 });
        } else if (realm === 1 || realm === 2) {
            list.push({ kind: 'equip', id: 'o_thiet_doan_chuy', rate: 0.03, qty: 1 });
            list.push({ kind: 'equip', id: 'chu_sa_lang_hao', rate: 0.03, qty: 1 });
            list.push({ kind: 'equip', id: 'thanh_moc_dinh', rate: 0.03, qty: 1 });
        }
    }

    addMasteryDrop();
    return list;
}

// Yêu thú. realm: chỉ số cảnh giới. night: chỉ ra ban đêm (18:00–6:00 GMT+7).
// trait: burn | double | armor | slow | crit | lifesteal. bigStun: chiêu lớn gây choáng (giây).
function monster(id, name, realm, element, stats, extra = {}, realmAlreadyShifted = false) {
    const finalRealm = realmAlreadyShifted ? realm : (realm >= 12 ? realm + 1 : realm);
    const isSmall = Boolean(extra.small);
    const isBoss = !isSmall;
    const isWorldBoss = Boolean(extra.worldBoss);
    const drops = extra.drops || makeMonsterDrops(id, finalRealm, element, isSmall, isBoss, isWorldBoss, extra.customDrops);
    return Object.freeze({ id, name, realm: finalRealm, element, ...stats, ...extra, drops });
}
function scaledMonster(id, name, realm, element, extra = {}) {
    const finalRealm = realm >= 12 ? realm + 1 : realm;
    const k = Math.pow(REALM_GROWTH, finalRealm);
    return monster(id, name, finalRealm, element, {
        hp: Math.round(BASE_STATS.hp * k * 0.85),
        atk: Math.round(BASE_STATS.atk * k * 0.8),
        def: Math.round(BASE_STATS.def * k * 0.8),
        spd: Math.round(10 + finalRealm * 0.6),
    }, extra, true);
}
const POST_TIEN_DE_MONSTERS = Object.freeze(POST_TIEN_DE_REALMS.flatMap((realmName, index) => {
    const realm = index + 31;
    const k = Math.pow(REALM_GROWTH, realm);
    const smallId = `tiengioi_tieuyeu_${realm}`;
    const localBossId = `tiengioi_daiyeu_${realm}`;
    const bossId = `tiengioi_boss_${realm}`;
    return [
        monster(smallId, `${realmName} Linh Thú`, realm, ['kim', 'moc', 'thuy', 'hoa', 'tho', 'loi', 'phong'][index % 7], {
            hp: Math.round(BASE_STATS.hp * k * 0.85), atk: Math.round(BASE_STATS.atk * k * 0.8),
            def: Math.round(BASE_STATS.def * k * 0.8), spd: Math.round(10 + realm * 0.6),
        }, { small: true, icon: ['🦊', '🐉', '🦅', '🐍', '🦁', '🐢', '🦄'][index % 7] }, true),
        monster(localBossId, `${realmName} Cổ Thú`, realm, ['kim', 'hoa', 'loi', 'tho', 'phong'][index % 5], {
            hp: Math.round(BASE_STATS.hp * k * 1.1), atk: Math.round(BASE_STATS.atk * k * 0.72),
            def: Math.round(BASE_STATS.def * k * 1.15), spd: Math.round(9 + realm * 0.5),
        }, { trait: index % 2 ? 'lifesteal' : 'armor', icon: ['🦣', '🐅', '🐊', '🦏', '🦍'][index % 5] }, true),
        monster(bossId, `${realmName} Trấn Giới Cự Thú`, realm, ['thien', 'kim', 'loi', 'tho', 'thuy'][index % 5], {
            hp: Math.round(BASE_STATS.hp * k * 1.2), atk: Math.round(BASE_STATS.atk * k * 0.65),
            def: Math.round(BASE_STATS.def * k * 1.35), spd: Math.round(8 + realm * 0.45),
        }, { worldBoss: true, trait: index % 2 ? 'armor' : 'crit', icon: ['🐲', '🗿', '👑', '🐉', '🦣'][index % 5] }, true),
    ];
}));
// Three local bosses fill the exact realm-12 gap between the shifted legacy
// roster and the later post-ascension realms, and provide distinct sources for
// the realm-12 equipment set.
const REALM_12_LOOT_BOSSES = (() => {
    const realm = 12;
    const k = Math.pow(REALM_GROWTH, realm);
    return [
        monster('tien_canh_12_huyen_hai_giao', 'Huyền Hải Trấn Giới Giao', realm, 'thuy', {
            hp: Math.round(BASE_STATS.hp * k * 1.05), atk: Math.round(BASE_STATS.atk * k * 0.86),
            def: Math.round(BASE_STATS.def * k * 1.10), spd: Math.round(9 + realm * 0.5),
        }, { trait: 'armor', icon: '🐉' }, true),
        monster('tien_canh_12_tinh_thu', 'Tinh Hà Thủ Giới Thú', realm, 'kim', {
            hp: Math.round(BASE_STATS.hp * k * 1.15), atk: Math.round(BASE_STATS.atk * k * 0.82),
            def: Math.round(BASE_STATS.def * k * 1.22), spd: Math.round(8 + realm * 0.45),
        }, { trait: 'crit', icon: '🦁' }, true),
        monster('tien_canh_12_kiem_linh', 'Vạn Kiếm Hộ Đạo Linh', realm, 'phong', {
            hp: Math.round(BASE_STATS.hp * k * 0.96), atk: Math.round(BASE_STATS.atk * k * 0.96),
            def: Math.round(BASE_STATS.def * k * 0.94), spd: Math.round(11 + realm * 0.65),
        }, { trait: 'double', icon: '🗡️' }, true),
    ];
})();
const MONSTERS = Object.freeze([
    // Đại yêu thường (solo được hoặc tổ đội): tên lấy từ Phàm Nhân Tu Tiên, Tiên Nghịch, Kiếm Lai
    monster('da_lang', 'Độc Giác Thanh Giao', 0, 'thuy', { hp: 380, atk: 38, def: 20, spd: 9 }, { icon: '🐉' }),
    monster('doc_phong', 'Kim Giác Tê', 0, 'kim', { hp: 300, atk: 44, def: 14, spd: 14 }, { trait: 'double', icon: '🦏', solo: true }),
    monster('thanh_xa', 'Hắc Thủy Huyền Xà', 1, 'moc', { hp: 430, atk: 42, def: 22, spd: 11 }, { icon: '🐍' }),
    monster('thach_hau', 'Man Hoang Cự Viên', 1, 'tho', { hp: 520, atk: 36, def: 34, spd: 8 }, { icon: '🦍', solo: true }),
    monster('hoa_ho', 'Hỏa Diễm Ma Điệp', 2, 'hoa', { hp: 560, atk: 55, def: 30, spd: 11 }, { trait: 'burn', icon: '🦋' }),
    monster('u_hon', 'Dạ Xoa Minh Hà', 2, 'thuy', { hp: 480, atk: 62, def: 24, spd: 13 }, { night: true, bigStun: 1, icon: '👺', solo: true }),
    monster('phong_lang', 'Phong Lôi Hống', 3, 'phong', { hp: 600, atk: 85, def: 30, spd: 18 }, { trait: 'double', icon: '🦁' }),
    monster('thiet_giap_te', 'Cổ Thần Cự Tê', 3, 'tho', { hp: 900, atk: 55, def: 90, spd: 6 }, { trait: 'armor', icon: '🦏', solo: true }),
    monster('huyen_bang_mang', 'Cửu U Băng Mãng', 4, 'bang', { hp: 1200, atk: 90, def: 60, spd: 9 }, { trait: 'slow', icon: '❄️' }),
    monster('loi_ung', 'Lôi Trì Kim Ưng', 5, 'loi', { hp: 1000, atk: 130, def: 50, spd: 25 }, { trait: 'crit', night: true, bigStun: 1.5, icon: '🦅', solo: true }),
    monster('huyet_ma_vien', 'Kiếm Khí Huyết Viên', 6, 'kim', { hp: 1900, atk: 140, def: 90, spd: 14 }, { trait: 'lifesteal', night: true, icon: '🦍' }),
    scaledMonster('kim_giap_ngac', 'Thôn Thiên Tích Dịch', 7, 'kim', { trait: 'armor', icon: '🐊', solo: true }),
    scaledMonster('thuy_ky_lan', 'Minh Hải Giao Long', 8, 'thuy', { trait: 'slow', icon: '🐉' }),
    scaledMonster('cuu_vi_ho', 'Cửu Vĩ Ma Hồ', 9, 'hoa', { trait: 'burn', night: true, icon: '🦊', solo: true }),
    scaledMonster('loi_giao', 'Huyền Kim Lôi Giao', 10, 'loi', { trait: 'crit', bigStun: 1.5, icon: '🐉' }),
    scaledMonster('bang_phuong', 'Băng Tinh Phượng Hoàng', 11, 'bang', { trait: 'slow', icon: '🦚', solo: true }),

    // Đại Boss Thế Giới (bắt buộc lập tổ đội mới khiêu chiến được)
    scaledMonster('thanh_long_anh', 'Thanh Long Chân Linh', 12, 'moc', { trait: 'lifesteal', icon: '🐲', worldBoss: true }),
    scaledMonster('bach_ho_anh', 'Bạch Hổ Sát Thần', 13, 'kim', { trait: 'crit', icon: '🐯', worldBoss: true }),
    scaledMonster('huyen_vu_anh', 'Huyền Vũ Thần Quân', 14, 'thuy', { trait: 'armor', icon: '🐢', worldBoss: true }),
    scaledMonster('chu_tuoc_anh', 'Chu Tước Thần Điểu', 15, 'hoa', { trait: 'burn', icon: '🔥', worldBoss: true }),
    scaledMonster('hon_don_thu', 'Vọng Nguyệt Cự Thú', 16, 'tho', { trait: 'armor', bigStun: 2, icon: '🌕', worldBoss: true }),
    scaledMonster('thao_thiet', 'Lạc Phách Thao Thiết', 17, 'tho', { trait: 'lifesteal', icon: '🦁', worldBoss: true }),
    scaledMonster('cung_ky', 'Man Hoang Cùng Kỳ', 18, 'phong', { trait: 'double', icon: '🐅', worldBoss: true }),
    scaledMonster('thien_ma', 'Hư Không Thiên Ma', 19, 'loi', { trait: 'crit', night: true, bigStun: 2, icon: '👿', worldBoss: true }),

    // Tiểu yêu (Phàm Nhân Tu Tiên, Tiên Nghịch, Kiếm Lai) - Solo thoải mái
    scaledMonster('ty_tho_yeu', 'Độc Hỏa Nghĩ', 0, 'hoa', { small: true, icon: '🐜' }),
    scaledMonster('ty_chuot_dat', 'U Minh Thử', 0, 'tho', { small: true, icon: '🐀' }),
    scaledMonster('ty_ran_co', 'Hắc Thủy Xà', 1, 'thuy', { small: true, icon: '🐍' }),
    scaledMonster('ty_ong_vang', 'Thiết Giáp Phong', 1, 'kim', { small: true, icon: '🐝' }),
    scaledMonster('ty_ech_doc', 'Lôi Oa Ấu Thú', 2, 'loi', { small: true, icon: '🐸' }),
    scaledMonster('ty_ho_ly_con', 'Băng Tinh Điệp', 2, 'bang', { small: true, icon: '🦋' }),
    scaledMonster('ty_soi_con', 'U Minh Lang', 3, 'phong', { small: true, icon: '🐺' }),
    scaledMonster('ty_nhen_to', 'Huyết Ngọc Chu', 3, 'moc', { small: true, icon: '🕷️' }),
    scaledMonster('ty_ret_lua', 'Lục Dực Sương Ngô', 4, 'bang', { small: true, icon: '🦂' }),
    scaledMonster('ty_luon_dien', 'Lôi Trì Ngạc', 4, 'loi', { small: true, icon: '⚡' }),
    scaledMonster('ty_qua_den', 'Ô Cưu Điêu', 5, 'phong', { small: true, icon: '🦅' }),
    scaledMonster('ty_khi_lua', 'Bạch Viên Hầu', 6, 'kim', { small: true, icon: '🐒' }),
    scaledMonster('ty_bo_cap', 'Độc Giác Hạt', 7, 'kim', { small: true, icon: '🦂' }),
    scaledMonster('ty_rua_nuoc', 'Huyền Hà Quy', 8, 'thuy', { small: true, icon: '🐢' }),
    scaledMonster('ty_hac_bang', 'Tuyết Sơn Linh Điêu', 9, 'bang', { small: true, icon: '🦩' }),
    scaledMonster('ty_linh_ho', 'Huyễn Cảnh Hồ', 10, 'hoa', { small: true, icon: '🦊' }),
    scaledMonster('ty_tuyet_vien', 'Kiếm Khí Vượn', 11, 'kim', { small: true, icon: '🦍' }),
    scaledMonster('ty_loi_thu', 'Tinh Vân Lôi Thử', 12, 'loi', { small: true, icon: '🐹' }),
    scaledMonster('ty_kim_dieu', 'Sát Lục Huyết Điêu', 13, 'phong', { small: true, icon: '🦅' }),
    scaledMonster('ty_moc_tinh', 'Hòe Ma Tinh Quỷ', 14, 'moc', { small: true, icon: '🌿' }),
    scaledMonster('ty_thach_linh', 'Cửu Khiếu Thạch Tinh', 15, 'tho', { small: true, icon: '🪨' }),
    scaledMonster('ty_phong_linh', 'Phong Lôi Linh Thể', 16, 'phong', { small: true, icon: '🌪️' }),
    scaledMonster('ty_thuy_linh', 'Minh Hải Thủy Linh', 17, 'thuy', { small: true, icon: '💧' }),
    scaledMonster('ty_hoa_linh', 'Địa Hỏa Yêu Tinh', 18, 'hoa', { small: true, icon: '🔥' }),
    scaledMonster('ty_hu_anh', 'Hư Không Tàn Ảnh', 19, 'loi', { small: true, icon: '👤' }),
    scaledMonster('ty_huyen_tinh_xa', 'Huyền Tinh U Xà', 20, 'bang', { small: true, icon: '🐍' }),
    scaledMonster('ty_kim_tuy_dieu', 'Kim Tủy Thần Điêu', 21, 'kim', { small: true, icon: '🦅' }),
    scaledMonster('ty_dai_la_yeu_tinh', 'Đại La Hư Không Tinh', 22, 'phong', { small: true, icon: '✨' }),
    scaledMonster('ty_thien_hoa_vuong', 'Bất Diệt Hỏa Tinh', 23, 'hoa', { small: true, icon: '🔥' }),
    scaledMonster('ty_hon_nguyen_ma_linh', 'Hỗn Nguyên Tà Ma Linh', 24, 'ma', { small: true, icon: '👁️' }),
    scaledMonster('ty_tien_quan_ho_ve', 'Tiên Triều Đọa Lạc Vệ', 25, 'thien', { small: true, icon: '💂' }),
    scaledMonster('ty_van_co_oan_hon', 'Vạn Cổ Chiến Hồn', 26, 'bang', { small: true, icon: '👻' }),
    scaledMonster('ty_loan_co_hung_thu', 'Loạn Cổ Hung Thú Ấu Thể', 27, 'tho', { small: true, icon: '🦖' }),
    scaledMonster('ty_tien_vuong_chien_linh', 'Tiên Vương Đạo Binh', 28, 'thien', { small: true, icon: '⚔️' }),
    scaledMonster('ty_hon_don_diet_the', 'Hỗn Độn Diệt Thế Trùng', 29, 'ma', { small: true, icon: '🐛' }),

    // ==================== BÁCH KHOA YÊU THÚ: PHÀM NHÂN, TIÊN NGHỊCH, KIẾM LAI ====================
    scaledMonster('phe_kim_trung', 'Phệ Kim Trùng', 4, 'kim', { trait: 'armor', icon: '🪲', solo: true }),
    scaledMonster('ban_son_vien', 'Bàn Sơn Viên', 5, 'tho', { trait: 'double', icon: '🦍', solo: true }),
    scaledMonster('thanh_y_thuy_giao', 'Thanh Y Thủy Giao', 6, 'thuy', { trait: 'slow', icon: '🐉', solo: true }),
    scaledMonster('van_thu', 'Văn Thú Tiên Nghịch', 6, 'phong', { trait: 'lifesteal', icon: '🦟', solo: true }),
    scaledMonster('thai_co_loi_long', 'Thái Cổ Lôi Long', 9, 'loi', { trait: 'crit', bigStun: 2, icon: '⚡', worldBoss: true }),
    scaledMonster('u_minh_thu', 'U Minh Cổ Thú', 5, 'thuy', { trait: 'slow', icon: '🐺', solo: true }),
    scaledMonster('de_hon_thu', 'Đề Hồn Thần Thú', 7, 'loi', { trait: 'crit', icon: '🐒', solo: true }),
    scaledMonster('mac_giao', 'Mặc Giao Đầm Lầy', 5, 'thuy', { trait: 'slow', icon: '🐉', solo: true }),
    scaledMonster('song_dong_thu', 'Song Đồng Thử', 8, 'tho', { trait: 'double', icon: '🐀', solo: true }),
    scaledMonster('du_thien_con_bang', 'Du Thiên Côn Bằng', 15, 'phong', { trait: 'crit', icon: '🦅', worldBoss: true }),
    scaledMonster('la_hau_co_thu', 'La Hầu Thần Thú', 16, 'thuy', { trait: 'armor', bigStun: 2, icon: '🐋', worldBoss: true }),
    scaledMonster('hinh_nha_thu', 'Hinh Nha Thú', 14, 'tho', { trait: 'double', icon: '🦖', worldBoss: true }),
    scaledMonster('kim_thien_ong', 'Kim Thiền Ong Vương', 13, 'kim', { trait: 'lifesteal', icon: '🐝', worldBoss: true }),

    // Mở rộng thêm Yêu Thú đặc sắc từ Phàm Nhân Tu Tiên, Tiên Nghịch, Kiếm Lai
    scaledMonster('kim_si_dieu', 'Kim Sí Điêu', 1, 'phong', { trait: 'crit', icon: '🦅', solo: true }),
    scaledMonster('doc_giac_bao', 'Độc Giác Hắc Báo', 2, 'kim', { trait: 'double', icon: '🐆', solo: true }),
    scaledMonster('thiet_giap_cu_te', 'Thiết Giáp Cự Tê', 2, 'tho', { trait: 'armor', icon: '🦏', solo: true }),
    scaledMonster('huyet_nhuc_khoi_loi', 'Huyết Ma Khôi Lỗi', 3, 'hoa', { trait: 'lifesteal', icon: '👹', solo: true }),
    scaledMonster('bat_trao_hoa_thu', 'Bát Trảo Hỏa Liệt Thú', 3, 'hoa', { trait: 'burn', icon: '🐙', solo: true }),
    scaledMonster('thon_thien_mang', 'Thôn Thiên Cự Mãng', 4, 'thuy', { trait: 'slow', icon: '🐍', solo: true }),
    scaledMonster('liet_phong_thu', 'Liệt Phong Thú', 4, 'phong', { trait: 'double', icon: '🦇', solo: true }),
    scaledMonster('ngu_sac_khong_tuoc', 'Ngũ Sắc Khổng Tước', 5, 'moc', { trait: 'crit', icon: '🦚', solo: true }),
    scaledMonster('son_nhac_cu_vien', 'Sơn Nhạc Cự Viên', 6, 'tho', { trait: 'armor', icon: '🦍', solo: true }),
    scaledMonster('huyet_ky_lan', 'Huyết Sát Kỳ Lân', 7, 'hoa', { trait: 'burn', icon: '🦄', solo: true }),
    scaledMonster('thien_si_chu_hac', 'Thiên Sí Chu Hạc', 8, 'phong', { trait: 'double', icon: '🦩', solo: true }),
    scaledMonster('cuu_muc_thiem', 'Cửu Mục Huyết Thiềm', 9, 'thuy', { trait: 'lifesteal', icon: '🐸', solo: true }),
    scaledMonster('loi_oa_thu', 'Lôi Oa Cổ Thú', 1, 'loi', { trait: 'crit', icon: '🐸', solo: true }),
    scaledMonster('hac_ma_nghi', 'Hắc Ma Nghĩ', 2, 'kim', { trait: 'armor', icon: '🐜', solo: true }),
    scaledMonster('sat_luc_huyet_buc', 'Sát Lục Huyết Bức', 3, 'phong', { trait: 'lifesteal', icon: '🦇', solo: true }),
    scaledMonster('bach_cot_lang', 'U Minh Bạch Cốt Lang', 3, 'phong', { trait: 'double', icon: '🐺', solo: true }),
    scaledMonster('u_lam_hoa_diep', 'U Lam Hỏa Điệp', 4, 'hoa', { trait: 'burn', icon: '🦋', solo: true }),
    scaledMonster('tu_la_ma_thu', 'Tu La Ma Thú', 5, 'hoa', { trait: 'crit', icon: '👺', solo: true }),
    scaledMonster('bat_hoang_long', 'Bát Hoang Thú Long', 6, 'tho', { trait: 'armor', icon: '🐉', solo: true }),
    scaledMonster('thon_hu_kinh', 'Thôn Hư Ma Kình', 7, 'thuy', { trait: 'slow', icon: '🐋', solo: true }),
    scaledMonster('lac_phach_chuy_quy', 'Lạc Phách Chùy Quỷ', 1, 'tho', { trait: 'double', icon: '🧟', solo: true }),
    scaledMonster('thao_thiet_tan_hon', 'Thao Thiết Tàn Hồn', 2, 'tho', { trait: 'lifesteal', icon: '🦁', solo: true }),
    scaledMonster('thac_nguyet_ho', 'Thác Nguyệt Hồ Yêu', 3, 'hoa', { trait: 'burn', icon: '🦊', solo: true }),
    scaledMonster('ban_son_trung', 'Bàn Sơn Cự Trùng', 4, 'kim', { trait: 'armor', icon: '🪱', solo: true }),
    scaledMonster('kiem_khi_bach_vien', 'Kiếm Khí Bạch Viên', 5, 'kim', { trait: 'crit', icon: '🐒', solo: true }),
    // --- TIỂU YÊU MỞ RỘNG (Phàm Nhân, Tiên Nghịch, Kiếm Lai) ---
    scaledMonster('ty_linh_meo', 'Linh Miêu Thú', 0, 'phong', { small: true, trait: 'double', icon: '🐱' }),
    scaledMonster('ty_hai_sam', 'Hàn Hải Thạch Sâm', 1, 'thuy', { small: true, trait: 'slow', icon: '🪸' }),
    scaledMonster('ty_kiem_diep', 'Kiếm Khí Điệp', 1, 'kim', { small: true, trait: 'crit', icon: '🦋' }),
    scaledMonster('ty_hoa_ho', 'Hỏa Nha Điểu', 2, 'hoa', { small: true, trait: 'burn', icon: '🐦' }),
    scaledMonster('ty_loi_trung', 'Lôi Dẫn Trùng', 2, 'loi', { small: true, trait: 'crit', icon: '🪲' }),
    scaledMonster('ty_bang_thiem', 'Băng Tinh Thiềm', 3, 'bang', { small: true, trait: 'slow', icon: '🐸' }),
    scaledMonster('ty_quy_anh', 'Minh Hà Quỷ Anh', 3, 'thuy', { small: true, trait: 'lifesteal', icon: '👻' }),
    scaledMonster('ty_kim_giap_trung', 'Kim Giáp Trùng', 4, 'kim', { small: true, trait: 'armor', icon: '🦗' }),
    scaledMonster('ty_u_hon_xa', 'U Hồn Độc Xà', 4, 'moc', { small: true, trait: 'burn', icon: '🐍' }),
    scaledMonster('ty_tu_la_dieu', 'Tu La Ấu Điêu', 5, 'hoa', { small: true, trait: 'double', icon: '🦅' }),
    scaledMonster('ty_loi_long_con', 'Lôi Long Ấu Thú', 6, 'loi', { small: true, trait: 'crit', icon: '🐲' }),
    scaledMonster('ty_hon_don_trung', 'Hỗn Độn Trùng', 7, 'tho', { small: true, trait: 'armor', icon: '🐛' }),
    scaledMonster('ty_thai_co_trung', 'Thái Cổ Hung Điệp', 8, 'phong', { small: true, trait: 'double', icon: '🦋' }),

    // --- ĐẠI YÊU & THỦ LĨNH MỞ RỘNG (100+ Quái vật kinh điển) ---
    scaledMonster('bach_ngoc_chu', 'Bạch Ngọc Chu Trùng', 0, 'kim', { trait: 'armor', icon: '🕷️', solo: true }),
    scaledMonster('thanh_lang_vuong', 'Thanh Giác Lang Vương', 1, 'phong', { trait: 'double', icon: '🐺' }),
    scaledMonster('trieu_quoc_ma_chu', 'Triệu Quốc Oán Ma Nhện', 1, 'thuy', { trait: 'slow', icon: '🕷️', solo: true }),
    scaledMonster('am_cot_ma_xa', 'Âm Cốt Ma Xà', 2, 'bang', { trait: 'slow', icon: '🐍' }),
    scaledMonster('ngu_loi_hac_ung', 'Ngũ Lôi Hắc Ưng', 2, 'loi', { trait: 'crit', icon: '🦅', solo: true }),
    scaledMonster('loan_tinh_hai_giao', 'Loạn Tinh Hải Cổ Giao', 3, 'thuy', { trait: 'lifesteal', icon: '🐉' }),
    scaledMonster('huyet_hoang_thu', 'Huyết Hoang Thú', 3, 'hoa', { trait: 'burn', icon: '👹', solo: true }),
    scaledMonster('nga_hoang_kiem_thu', 'Ngã Hoàng Kiếm Thú', 3, 'kim', { trait: 'crit', icon: '🗡️' }),
    scaledMonster('co_ma_hoa_than', 'Cổ Ma Phân Thần', 4, 'hoa', { trait: 'burn', icon: '😈', solo: true }),
    scaledMonster('tram_long_thach_vien', 'Trảm Long Thạch Viên', 4, 'tho', { trait: 'armor', icon: '🦍' }),
    scaledMonster('bach_cau_bang_hung', 'Bắc Câu Băng Hùng', 5, 'bang', { trait: 'slow', icon: '🐻', solo: true }),
    scaledMonster('am_la_quy_vuong', 'Âm La Quỷ Vương', 5, 'thuy', { trait: 'lifesteal', icon: '💀' }),
    scaledMonster('thien_nam_co_dieu', 'Thiên Nam Cổ Kim Điêu', 5, 'kim', { trait: 'crit', icon: '🦅', solo: true }),
    scaledMonster('tinh_khong_cu_thu', 'Tinh Không Cự Thú', 6, 'tho', { trait: 'armor', icon: '🦣', solo: true }),
    scaledMonster('tu_la_huyet_long', 'Tu La Huyết Ma Long', 6, 'hoa', { trait: 'burn', icon: '🐉' }),
    scaledMonster('chu_tuoc_hoa_dieu', 'Chu Tước Thần Điểu', 7, 'hoa', { trait: 'burn', icon: '🦚', solo: true }),
    scaledMonster('co_than_do_tu_ve', 'Cổ Thần Vệ Khôi Lỗi', 7, 'tho', { trait: 'armor', icon: '🗿' }),
    scaledMonster('truong_thanh_kiem_linh', 'Trường Thành Kiếm Linh', 8, 'kim', { trait: 'crit', icon: '⚔️', solo: true }),
    scaledMonster('tien_di_thanh_thu', 'Tiên Di Cổ Thần Thú', 8, 'loi', { trait: 'crit', icon: '⚡' }),
    scaledMonster('dong_linh_co_giao', 'Đông Lâm Cổ Giao Long', 9, 'thuy', { trait: 'lifesteal', icon: '🐉', solo: true }),
    scaledMonster('man_hoang_dai_yeu', 'Man Hoang Thập Tứ Cảnh Cự Yêu', 9, 'tho', { trait: 'armor', icon: '👹' }),
    scaledMonster('bac_han_tien_hac', 'Bắc Hàn Tiên Vực Bạch Hạc', 10, 'bang', { trait: 'slow', icon: '🕊️', solo: true }),
    scaledMonster('chan_tien_ma_khi', 'Chân Tiên Biến Dị Ma Khôi', 11, 'kim', { trait: 'armor', icon: '🤖' }),
    scaledMonster('dai_la_kim_long', 'Đại La Kim Long', 12, 'kim', { trait: 'crit', icon: '🐲', solo: true }),
    scaledMonster('hon_don_to_long', 'Hỗn Độn Thủy Tổ Ma Long', 13, 'hoa', { trait: 'burn', icon: '🐉' }),
    scaledMonster('thien_dao_loi_thu', 'Thiên Đạo Lôi Đình Thú', 14, 'loi', { trait: 'crit', icon: '⚡', solo: true }),
    scaledMonster('thai_co_to_than', 'Thái Cổ Tổ Thần', 15, 'tho', { trait: 'armor', icon: '👑', solo: true }),
    scaledMonster('tien_de_tan_niem', 'Tiên Đế Hủy Diệt Tàn Niệm', 16, 'kim', { trait: 'lifesteal', icon: '🔱', solo: true }),
    scaledMonster('thao_thiet_co_than', 'Thái Cổ Thao Thiết Thần', 17, 'tho', { trait: 'armor', icon: '👺', worldBoss: true }),
    scaledMonster('cung_ky_hoang_thu', 'Cùng Kỳ Viễn Cổ Hoàng', 18, 'hoa', { trait: 'burn', icon: '🦁', worldBoss: true }),
    scaledMonster('hu_khong_thien_ma_ton', 'Hư Không Vạn Ma Tôn', 19, 'ma', { trait: 'lifesteal', icon: '👿', worldBoss: true }),
    scaledMonster('thai_at_huyen_lan', 'Thái Ất Huyền Lân Thú', 20, 'thuy', { trait: 'slow', icon: '🦄', worldBoss: true }),
    scaledMonster('thai_at_kim_long', 'Thái Ất Chân Kim Long', 21, 'kim', { trait: 'crit', icon: '🐉', worldBoss: true }),
    scaledMonster('dai_la_hac_nguu', 'Đại La Hắc Ma Ngưu', 22, 'tho', { trait: 'armor', icon: '🐂', worldBoss: true }),
    scaledMonster('dai_la_bach_ho', 'Đại La Bạch Hổ Thần Quân', 23, 'kim', { trait: 'crit', icon: '🐅', worldBoss: true }),
    scaledMonster('hon_don_to_long_vi_dai', 'Hỗn Nguyên Viễn Cổ Tổ Long', 24, 'loi', { trait: 'crit', icon: '🐲', worldBoss: true }),
    scaledMonster('u_minh_tien_quan', 'U Minh Sát Tiên Quân', 25, 'ma', { trait: 'lifesteal', icon: '👑', worldBoss: true }),
    scaledMonster('cuu_u_tien_ton', 'Cửu U Ma Đạo Tiên Tôn', 26, 'ma', { trait: 'lifesteal', icon: '🧙', worldBoss: true }),
    scaledMonster('chuan_tien_vuong_thu', 'Bất Tử Chuẩn Tiên Vương Thú', 27, 'kim', { trait: 'armor', icon: '🦣', worldBoss: true }),
    scaledMonster('bat_hoang_tien_vuong', 'Bát Hoang Bất Diệt Tiên Vương', 28, 'loi', { trait: 'crit', icon: '🔱', worldBoss: true }),
    scaledMonster('thai_so_tien_de_hon', 'Thái Sơ Hỗn Độn Tiên Đế', 29, 'thien', { trait: 'lifesteal', icon: '👑', worldBoss: true }),
    // Dị chủng bổ sung để các mốc cảnh giới có thêm lựa chọn săn đơn và phối đội.
    scaledMonster('ty_hac_linh_tho', 'Hắc Linh Thố', 0, 'phong', { small: true, trait: 'double', icon: '🐇' }),
    scaledMonster('ty_thach_tinh_xa', 'Thạch Tinh Xà', 1, 'tho', { small: true, trait: 'armor', icon: '🐍' }),
    scaledMonster('ty_tuyet_vu_dieu', 'Tuyết Vũ Điêu', 2, 'bang', { small: true, trait: 'slow', icon: '🦅' }),
    scaledMonster('ty_huyet_van_buc', 'Huyết Văn Bức', 3, 'ma', { small: true, trait: 'lifesteal', icon: '🦇' }),
    scaledMonster('thanh_dong_gia_vien', 'Thanh Đồng Giáp Viên', 2, 'kim', { trait: 'armor', icon: '🦍', solo: true }),
    scaledMonster('hoa_lien_xa_vuong', 'Hỏa Liên Xà Vương', 4, 'hoa', { trait: 'burn', icon: '🐍', solo: true }),
    scaledMonster('bach_suong_huyen_hac', 'Bạch Sương Huyền Hạc', 6, 'bang', { trait: 'slow', icon: '🕊️', solo: true }),
    scaledMonster('tinh_hai_cuu_dau_giao', 'Tinh Hải Cửu Đầu Giao', 9, 'thuy', { trait: 'double', bigStun: 1.5, icon: '🐉' }),
    scaledMonster('thien_kiep_loi_ngao', 'Thiên Kiếp Lôi Ngao', 12, 'loi', { trait: 'crit', bigStun: 2, icon: '🐺', solo: true }),
    scaledMonster('hon_don_thuc_tinh', 'Hỗn Độn Thức Tỉnh Cự Linh', 16, 'thien', { trait: 'armor', icon: '🗿', worldBoss: true }),
    scaledMonster('van_co_huyet_phuong', 'Vạn Cổ Huyết Phượng', 20, 'hoa', { trait: 'lifesteal', icon: '🦚', worldBoss: true }),
    scaledMonster('vo_cuc_tinh_thu', 'Vô Cực Tinh Thú', 24, 'thien', { trait: 'crit', icon: '🌌', worldBoss: true }),
    // Yêu thú mới từ các mục PDF còn thiếu; mỗi loài có vùng cảnh giới và chiến lợi phẩm riêng.
    scaledMonster('bach_loc_linh_thu', 'Bạch Lộc Linh Thú', 2, 'moc', { small: true, icon: '🦌', customDrops: [{ kind: 'mat', id: 'mat_tuyet_linh_thuy', rate: 0.06, qty: 1 }, { kind: 'mat', id: 'mat_yeu_dan', rate: 0.35, qty: 1 }] }),
    scaledMonster('ngan_quang_thu', 'Ngân Quang Thử', 4, 'loi', { small: true, icon: '🐁', customDrops: [{ kind: 'mat', id: 'mat_thiet_moc', rate: 0.08, qty: 1 }, { kind: 'mat', id: 'mat_yeu_dan', rate: 0.3, qty: 1 }] }),
    scaledMonster('bang_tuyet_thiem', 'Băng Tuyết Thiềm', 6, 'bang', { small: true, icon: '🐸', customDrops: [{ kind: 'mat', id: 'mat_cuu_khuc_linh_sam', rate: 0.045, qty: 1 }, { kind: 'mat', id: 'mat_yeu_dan', rate: 0.25, qty: 1 }] }),
    scaledMonster('tinh_ngan_dieu', 'Tinh Ngân Điêu', 7, 'phong', { trait: 'double', icon: '🦅', solo: true, customDrops: [{ kind: 'mat', id: 'mat_duong_hon_moc', rate: 0.04, qty: 1 }, { kind: 'mat', id: 'mat_yeu_dan', rate: 0.3, qty: 1 }] }),
    scaledMonster('li_hoa_giao', 'Ly Hỏa Giao', 10, 'hoa', { trait: 'burn', icon: '🐉', solo: true, customDrops: [{ kind: 'mat', id: 'mat_kim_diem_thach', rate: 0.025, qty: 1 }, { kind: 'mat', id: 'mat_thien_loi_truc', rate: 0.018, qty: 1 }, { kind: 'mat', id: 'mat_yeu_dan', rate: 0.28, qty: 1 }] }),
    scaledMonster('linh_te_khong_tuoc', 'Linh Tê Khổng Tước', 11, 'moc', { trait: 'crit', icon: '🦚', solo: true, customDrops: [{ kind: 'mat', id: 'mat_linh_nhan_chi_thu', rate: 0.012, qty: 1 }, { kind: 'mat', id: 'mat_cuu_khuc_linh_sam', rate: 0.035, qty: 1 }, { kind: 'mat', id: 'mat_yeu_dan', rate: 0.25, qty: 1 }] }),
    scaledMonster('tuyet_vien_linh_thu', 'Tuyết Viên Linh Thú', 9, 'bang', { small: true, icon: '🐒', customDrops: [{ kind: 'mat', id: 'mat_van_nien_huyen_ngoc', rate: 0.004, qty: 1 }, { kind: 'mat', id: 'mat_duong_hon_moc', rate: 0.04, qty: 1 }, { kind: 'mat', id: 'mat_yeu_dan', rate: 0.3, qty: 1 }] }),
    scaledMonster('loi_bang_thien_tac', 'Lôi Bằng Thiên Tước', 16, 'loi', { trait: 'crit', bigStun: 1.5, icon: '🦅', worldBoss: true, customDrops: [{ kind: 'mat', id: 'mat_van_nien_huyen_ngoc', rate: 0.008, qty: 1 }, { kind: 'mat', id: 'mat_thien_loi_truc', rate: 0.025, qty: 1 }, { kind: 'mat', id: 'mat_yeu_dan', rate: 0.25, qty: 1 }] }),
    ...REALM_12_LOOT_BOSSES,
    ...POST_TIEN_DE_MONSTERS,
]);

const RULES = Object.freeze({
    staminaMax: 200,
    staminaRegenMs: 2 * 60 * 1000,
    hpRegenMs: 60 * 1000,  // Tự hồi máu thụ động: 1 phút
    hpRegenPct: 0.01,      // Hồi 1% Khí Huyết tối đa mỗi phút
    huntCost: 8,           // giảm thể lực đại yêu/đại boss xuống còn 8
    smallCost: 3,          // tiểu yêu tốn ít thể lực để khuyến khích săn thường xuyên
    dungeonCost: 15,       // tăng thể lực cổ động lên 15
    pvpCost: 20,           // PvP tiêu hao gấp đôi thể lực
    sectCreateCost: 50000, // khai sơn lập phái 50.000 linh thạch
    proposeCost: 20000,    // cầu duyên sính lễ 20.000 linh thạch
    changeGenderCost: 1000,// phí đổi giới tính 1.000 linh thạch
    maxDailyStaminaItems: 50, // giới hạn mua 50 bình thể lực/ngày
    maxDailyStaminaUses: 50, // giới hạn dùng 50 lần tăng thể lực/ngày
    maxDailyNpcBattles: 15,  // giới hạn khiêu chiến NPC 15 lần/ngày
    maxEscapesPerDay: 2,   // tối đa 2 lần bỏ chạy khi bị đột kích/ngày
    songTuQiCost: 25,      // tiêu hao 25 điểm Dương Khí (Nam) / Âm Khí (Nữ) mỗi lần song tu
    bossHpMul: 2.5,        // đại yêu: tăng máu để chiến đấu kịch tính
    bossAtkMul: 1.1,       // giữ boss nguy hiểm nhưng không kết liễu cả đội bằng vài đòn
    smallHpMul: 0.35,      // tiểu yêu so với chỉ số gốc
    smallAtkMul: 0.5,
    smallDefMul: 0.8,
    smallExpPerWin: 0.003, // tỷ lệ nhận EXP tiểu yêu gần bằng như cũ (~0.3% levelCap)
    bossExpPerWin: 0.008,   // tỷ lệ nhận EXP đại yêu gần bằng như cũ (~0.8% levelCap, x2 khi thú triều)
    smallLossExp: 0.02,
    // Đại yêu đánh theo tổ đội: khí huyết nhân theo số người.
    partyMax: 5,
    partyHpMul: [1, 1.8, 2.5, 3.1, 3.6],
    // Nội dung boss/Cổ Động được cân theo quy mô đội: 2–3 người ban ngày,
    // 4–5 người từ buổi tối tới rạng sáng.
    bossDayTargetPartySize: 3,
    bossNightTargetPartySize: 5,
    bossDayMinPartySize: 2,
    bossNightMinPartySize: 4,
    soloBossAtkMul: 2.0,
    soloWorldBossAtkMul: 2.4,
    soloDungeonAtkMul: 2.2,
    soloBossDamageMul: 0.55,
    teamBossDamagePerMember: 0.08,
    // Quà cho mọi người (mỗi nhân vật nhận một lần).
    gift: { id: 'qua_hong_hoang', stones: 25000 },
    bagSize: 30,
    khoSize: 50,
    safeStorageSlots: 10,
    safeStorageMonthlyCost: 10000,
    safeStorageMonthMs: 30 * 24 * 60 * 60 * 1000,
    stackMax: 99,
    skillSlots: 5,
    // Cảnh giới cần để mở từng ô kỹ năng: 3 ô từ Phàm Nhân, ô 4 ở Luyện Thể, ô 5 ở Luyện Khí.
    skillSlotRealms: [0, 0, 0, 1, 2],
    quickSlots: 2,
    // Phường thị: người chơi bán đồ cho nhau.
    marketTax: 0.05,
    marketMaxListings: 10,
    marketDurationMs: 72 * 60 * 60 * 1000,
    marketMaxPrice: 1000000000,
    maxBigSkills: 2,
    maxEscapeSkills: 1,
    battleMaxMs: 180 * 1000,
    idleFleeMs: 30 * 1000,
    actionsPerSecond: 15,
    // Độ khó chung của yêu thú (nhân vào chỉ số trong MONSTERS).
    monsterHpMul: 4.8,
    monsterAtkMul: 1.2,
    monsterDefMul: 0.85,
    // Boss thế giới: ưu tiên sức bền và giáp, giảm sát thương để nhóm 2–5 người có thể phối hợp.
    worldBossHpMul: 1.8,
    worldBossAtkMul: 0.85,
    worldBossDefMul: 1.5,
    // Ban đêm chỉ số tiểu yêu Tiên Giới giữ nguyên giới hạn nhẹ; boss tăng rõ hơn.
    tienBossNightHpMul: 1.6,
    tienBossNightAtkMul: 1.5,
    monsterAttackMs: 2600,
    dodgeWindowMs: 1000,
    dodgeCdMs: 4000,
    telegraphMs: 1500,
    stunImmuneMs: 5000,
    expPerWin: 0.005,
    lossExp: 0.05,
    lossDurability: 10,
    injuryMs: 3 * 60 * 1000,
    startStones: 5000,
    starterSet: { kiem: 'tt_kiem', phapkhi: 'tt_truong', trongkhi: 'tt_chuy', quyensao: 'tt_quyen_sao', dinh: 'tt_dinh', bua: 'tt_bua', but: 'tt_but', armor: 'tt_dao_bao', acc1: 'tt_ngoc_boi', acc2: 'tt_ho_phu', nhanTruDo: 'nhan_tru_do_pham' },
    punishKeepMs: 7 * 24 * 60 * 60 * 1000,
    reRegisterWaitMs: 7 * 24 * 60 * 60 * 1000,
    eliteChance: 0.1,
    bossChance: 0.01,
    stoneMinPerWin: 3,
});

// Chợ Tông Môn & Tàng Bảo Các (Mua bằng Điểm Cống Hiến Tông Môn / sectCoins)
const SECT_SHOP = Object.freeze([
    { id: 'sect_dan_pha_chuong', name: 'Phá Chướng Đan (Tông Môn)', kind: 'cons', targetId: 'dan_pha_chuong', price: 1000, desc: 'Đan dược phá cảnh Luyện Khí do tông môn luyện chế.', icon: '🧪' },
    { id: 'sect_dan_truc_co', name: 'Trúc Cơ Đan (Tông Môn)', kind: 'cons', targetId: 'truc_co_dan', price: 1800, desc: 'Thần đan hỗ trợ đệ tử đột phá Trúc Cơ phẩm chất cao.', icon: '💊' },
    { id: 'sect_dan_giang_tran', name: 'Giáng Trần Đan (Tông Môn)', kind: 'cons', targetId: 'dan_giang_tran', price: 3750, desc: 'Linh đan ngưng kết Kim Đan cửu chuyển.', icon: '🌟' },
    { id: 'sect_dan_ket_anh', name: 'Kết Anh Đan (Tông Môn)', kind: 'cons', targetId: 'dan_ket_anh', price: 7500, desc: 'Đan dược cực phẩm phá toái Kim Đan hóa Nguyên Anh.', icon: '✨' },
    { id: 'sect_dan_hoa_than', name: 'Hóa Thần Đan (Tông Môn)', kind: 'cons', targetId: 'dan_hoa_than', price: 1000, desc: 'Thần đan ngộ ý cảnh sinh tử, thành tựu Hóa Thần.', icon: '🔮' },
    { id: 'sect_dan_tay_tuy', name: 'Tẩy Tủy Đan (x1)', kind: 'cons', targetId: 'tay_tuy_dan', qty: 1, price: 1200, desc: 'Tẩy kinh phạt tủy, gia tăng ~10% EXP tu vi cảnh giới hiện tại.', icon: '📜' },
    { id: 'sect_hoi_xuan', name: 'Hồi Xuân Đan Cực Phẩm (x5)', kind: 'cons', targetId: 'hoi_xuan_dan', qty: 5, price: 1000, desc: 'Gói 5 viên Hồi Xuân Đan hồi phục khí huyết nhanh chóng.', icon: '🧪' },
    { id: 'sect_hoi_linh', name: 'Hồi Linh Đan Cực Phẩm (x5)', kind: 'cons', targetId: 'hoi_linh_dan', qty: 5, price: 1000, desc: 'Gói 5 viên Hồi Linh Đan hồi phục linh lực chiến đấu.', icon: '💧' },
    { id: 'sect_kim_cang_phu', name: 'Kim Cang Hộ Thân Phù (x3)', kind: 'cons', targetId: 'kim_cang_phu', qty: 3, price: 1000, desc: 'Gói 3 phù hộ thân tăng phòng thủ khi săn yêu.', icon: '📜' },
    { id: 'sect_cuu_chuyen', name: 'Cửu Chuyển Hồi Hồn Đan', kind: 'cons', targetId: 'cuu_chuyen_dan', qty: 1, price: 2250, desc: 'Cứu mạng tức thì, hồi 100% HP và xóa bỏ trọng thương.', icon: '💖' },
    { id: 'sect_ao_dao_bao', name: 'Cửu Khúc Linh Ẩn Bào', kind: 'equip', targetId: 'cuu_khuc_bao', price: 3000, desc: 'Áo bào ẩn nấp khí tức, phòng thủ kiên cố.', icon: '🥋' },
    { id: 'sect_phi_kiem', name: 'Thanh Phong Kiếm', kind: 'equip', targetId: 'thanh_phong_kiem', price: 3000, desc: 'Phi kiếm sắc bén lưu chuyển thanh phong.', icon: '🗡️' },
    { id: 'sect_nhan_huyen', name: 'Nhẫn Trữ Đồ Huyền Giai', kind: 'equip', targetId: 'nhan_tru_do_huyen', price: 7500, desc: 'Trữ vật giới chỉ mở rộng thêm +100 ô sức chứa kho đồ!', icon: '💍' },
    { id: 'sect_nhan_dia', name: 'Hư Không Nạp Giới (Địa)', kind: 'equip', targetId: 'nhan_tru_do_dia', price: 1200, desc: 'Bảo vật trữ vật địa giai, mở rộng thêm +150 ô sức chứa kho đồ!', icon: '💍' },
    { id: 'sect_dan_tay_tuy_hoan', name: 'Tẩy Tủy Hoàn (Tông Môn)', kind: 'cons', targetId: 'dan_tay_tuy_hoan', price: 600, desc: 'Đan dược gột rửa phàm cốt, đột phá Luyện Thể.', icon: '💊' },
    { id: 'sect_dan_ngu_hanh_lo', name: 'Ngũ Hành Linh Lộ (Tông Môn)', kind: 'cons', targetId: 'dan_ngu_hanh_lo', price: 1500, desc: 'Tinh hoa ngũ hành tương sinh, đột phá Luyện Hư.', icon: '💧' },
    { id: 'sect_dan_co_than', name: 'Cổ Thần Tinh Huyết (Tông Môn)', kind: 'cons', targetId: 'dan_co_than_tinh_huyet', price: 2500, desc: 'Giọt tinh huyết Cổ Thần, đột phá Hợp Thể.', icon: '🩸' },
    { id: 'sect_dan_dai_thua', name: 'Đại Thừa Vấn Đạo Đan (Tông Môn)', kind: 'cons', targetId: 'dan_dai_thua', price: 4000, desc: 'Vấn đạo thiên địa, đột phá Đại Thừa.', icon: '👑' },
    { id: 'sect_dan_do_kiep', name: 'Độ Kiếp Thần Đan (Tông Môn)', kind: 'cons', targetId: 'dan_do_kiep', price: 6000, desc: 'Nghịch thiên kháng lôi kiếp, đột phá Độ Kiếp.', icon: '⚡' },
    { id: 'sect_dan_ban_tien', name: 'Bán Tiên Hóa Cốt Đan (Tông Môn)', kind: 'cons', targetId: 'dan_ban_tien', price: 8000, desc: 'Thoát thai hoán cốt khỏi phàm trần, đột phá Bán Tiên.', icon: '✨' },
    { id: 'sect_dan_dang_tien', name: 'Đăng Tiên Lôi Tâm Đan (Tông Môn)', kind: 'cons', targetId: 'dan_dang_tien', price: 12000, desc: 'Dẫn thiên lôi vào đan tâm, mở cửa Đăng Tiên.', icon: '🌠' },
    { id: 'sect_dan_bo_thien', name: 'Bổ Thiên Thần Đan (Tông Môn)', kind: 'cons', targetId: 'dan_bo_thien', price: 10000, desc: 'Đúc lại tiên thể từ tro tàn lôi kiếp, đột phá Tán Tiên.', icon: '🌌' },
    { id: 'sect_dan_dia_tien', name: 'Địa Tiên Ngưng Thể Đan (Tông Môn)', kind: 'cons', targetId: 'dan_dia_tien', price: 12000, desc: 'Hấp thu địa mạch tiên khí, đột phá Địa Tiên.', icon: '⛰️' },
    { id: 'sect_dan_nhan_tien', name: 'Nhân Tiên Thuần Dương Dịch (Tông Môn)', kind: 'cons', targetId: 'dan_nhan_tien', price: 15000, desc: 'Thuần dương tiên khí, đột phá Nhân Tiên.', icon: '☀️' },
    { id: 'sect_dan_chan_tien', name: 'Chân Tiên Hóa Cảnh Đan (Tông Môn)', kind: 'cons', targetId: 'dan_chan_tien', price: 18000, desc: 'Ngưng tụ Chân Tiên pháp tắc, đột phá Chân Tiên.', icon: '💎' },
    { id: 'sect_dan_huyen_tien', name: 'Huyền Tiên Ngọc Tủy (Tông Môn)', kind: 'cons', targetId: 'dan_huyen_tien', price: 22000, desc: 'Quán triệt huyền tiên chi lực, đột phá Huyền Tiên.', icon: '💠' },
    { id: 'sect_dan_thien_tien', name: 'Thiên Tiên Dịch (Tông Môn)', kind: 'cons', targetId: 'dan_thien_tien', price: 26000, desc: 'Hấp thụ thiên tiên khí, đột phá Thiên Tiên.', icon: '🍃' },
    { id: 'sect_dan_kim_tien', name: 'Kim Tiên Thái Ất Đan (Tông Môn)', kind: 'cons', targetId: 'dan_kim_tien', price: 30000, desc: 'Bất hủ kim tính, đột phá Kim Tiên.', icon: '🟡' },
    { id: 'sect_dan_thai_at_chan_tien', name: 'Thái Ất Chân Tiên Lộ (Tông Môn)', kind: 'cons', targetId: 'dan_thai_at_chan_tien', price: 35000, desc: 'Đốn ngộ Chân Tiên đạo, đột phá Thái Ất Chân Tiên.', icon: '🌀' },
    { id: 'sect_dan_thai_at_huyen_tien', name: 'Thái Ất Huyền Tiên Tinh (Tông Môn)', kind: 'cons', targetId: 'dan_thai_at_huyen_tien', price: 40000, desc: 'Huyền tiên tinh hoa, đột phá Thái Ất Huyền Tiên.', icon: '🔮' },
    { id: 'sect_dan_thai_at', name: 'Thái Ất Hỗn Nguyên Đan (Tông Môn)', kind: 'cons', targetId: 'dan_thai_at', price: 46000, desc: 'Hỗn nguyên quy nhất, đột phá Thái Ất Kim Tiên.', icon: '☯️' },
    { id: 'sect_dan_dai_la_chan_tien', name: 'Đại La Chân Tiên Quả (Tông Môn)', kind: 'cons', targetId: 'dan_dai_la_chan_tien', price: 52000, desc: 'Mở ra con đường Đại La, đột phá Đại La Chân Tiên.', icon: '🍏' },
    { id: 'sect_dan_dai_la', name: 'Đại La Đạo Quả (Tông Môn)', kind: 'cons', targetId: 'dan_dai_la', price: 60000, desc: 'Đạo quả viên mãn, đột phá Đại La Kim Tiên.', icon: '🍎' },
    { id: 'sect_dan_hon_nguyen', name: 'Hỗn Nguyên Kim Tiên Thai (Tông Môn)', kind: 'cons', targetId: 'dan_hon_nguyen', price: 70000, desc: 'Đạo thai Hỗn Nguyên, đột phá Hỗn Nguyên Kim Tiên.', icon: '🥚' },
    { id: 'sect_dan_tien_quan', name: 'Tiên Quân Pháp Tắc Đan (Tông Môn)', kind: 'cons', targetId: 'dan_tien_quan', price: 80000, desc: 'Thống ngự tiên binh, đột phá Tiên Quân.', icon: '⚔️' },
    { id: 'sect_dan_tien_ton', name: 'Tiên Tôn Hóa Đạo Châu (Tông Môn)', kind: 'cons', targetId: 'dan_tien_ton', price: 95000, desc: 'Chuyển dời muôn vàn tinh cầu, đột phá Tiên Tôn.', icon: '🔮' },
    { id: 'sect_dan_chuan_tien_vuong', name: 'Chuẩn Tiên Vương Huyết Tinh (Tông Môn)', kind: 'cons', targetId: 'dan_chuan_tien_vuong', price: 110000, desc: 'Tinh huyết vương giả, đột phá Chuẩn Tiên Vương.', icon: '🩸' },
    { id: 'sect_dan_tien_vuong', name: 'Chân Linh Vương Tinh (Tông Môn)', kind: 'cons', targetId: 'dan_tien_vuong', price: 130000, desc: 'Xưng vương chư thiên, đột phá Tiên Vương.', icon: '👑' },
    { id: 'sect_dan_tien_de', name: 'Hỗn Độn Tiên Đế Thai (Tông Môn)', kind: 'cons', targetId: 'dan_tien_de', price: 160000, desc: 'Hỗn độn sơ khai, chí cao vô thượng, đột phá Tiên Đế.', icon: '🌌' },
    { id: 'sect_scroll_tu_linh', name: 'Ngọc Giản: Tụ Linh Quyết', kind: 'scroll', targetId: 'tu_linh_quyet', price: 2250, desc: 'Ngọc giản tâm pháp tụ nạp linh khí thiên địa.', icon: '📖' },
]);
const SECT_SHOP_BY_ID = new Map(SECT_SHOP.map(it => [it.id, it]));

// Hệ thống Hang Động & Cổ Động Bí Cảnh (Cơ Duyên từ 3 bộ truyện)
const POST_TIEN_DE_DUNGEONS = Object.freeze(POST_TIEN_DE_MAP_CONFIGS.flatMap(map => map.townIds.map((townId, townIndex) => {
    const townRealm = townIndex === 0 ? map.realmMin : Math.ceil((map.realmMin + map.realmMax) / 2);
    const nextRealm = Math.min(map.realmMax, townRealm + 1);
    const guaranteedPill = (map.id === 'map_10' && townIndex === 0) ? 'dan_hau_tien_de_31' : `dan_hau_tien_de_${Math.min(65, townRealm + 1)}`;
    const bonusPill1 = (map.id === 'map_10' && townIndex === 0) ? 'dan_hau_tien_de_32' : `dan_hau_tien_de_${Math.min(65, townRealm + 2)}`;
    const bonusPill2 = (map.id === 'map_10' && townIndex === 0) ? 'dan_hau_tien_de_33' : `dan_hau_tien_de_${Math.min(65, townRealm + 3)}`;
    const subPill = `dan_tieu_canh_${townRealm}`;
    return {
        id: `dong_${townId}`, townId, name: `${map.towns[townIndex]} Cổ Động`, novel: 'Tiên Giới', icon: map.icon,
        realmMin: townRealm, stamina: 55 + townIndex * 5,
        desc: `Bí cảnh ${map.name}, vượt qua yêu thú trấn giới để tìm đạo đan và cơ duyên hậu Tiên Đế.`,
        stages: [
            { id: 'stg_1', name: 'Tiên Linh Ngoại Vi', monsterId: `tiengioi_tieuyeu_${townRealm}` },
            { id: 'stg_2', name: 'Đạo Tắc Thâm Uyên', monsterId: `tiengioi_tieuyeu_${nextRealm}` },
            { id: 'boss', name: 'Trấn Giới Cự Thú', monsterId: `tiengioi_boss_${map.realmMax}`, isBoss: true },
        ],
        guaranteedPill,
        bonusPills: [bonusPill1, bonusPill2, subPill],
        stones: 250000 + townRealm * 25000, expReward: 1000000 + townRealm * 50000,
    };
})));

const RAW_DUNGEONS = [
    {
        "id": "dong_ly_chau",
        "name": "Ly Châu Động Thiên",
        "novel": "Kiếm Lai",
        "icon": "🏮",
        "realmMin": 0,
        "stamina": 10,
        "desc": "Bản đồ cơ duyên thời niên thiếu của Trần Bình An, phong ấn vô số kiếm khí và kỳ ngộ viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Trấn Ma Thạch Kiều",
                "monsterId": "ty_chuot_dat"
            },
            {
                "id": "stg_2",
                "name": "Long Tuyền Cổ Tỉnh",
                "monsterId": "thanh_xa"
            },
            {
                "id": "boss",
                "name": "Chân Long Di Hài",
                "monsterId": "thanh_lang_vuong",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_tay_tuy_hoan",
        "bonusPills": [
            "dan_pha_chuong"
        ],
        "equipDrop": "thanh_phong_kiem",
        "stones": 5000,
        "expReward": 500,
        "townId": "thanh_van"
    },
    {
        "id": "dong_thien_phu",
        "name": "Thiên Phù Mật Động",
        "novel": "Tiên Nghịch",
        "icon": "📜",
        "realmMin": 2,
        "stamina": 20,
        "desc": "Mật động tu tiên cổ xưa tại Triệu Quốc chôn vùi vô vàn đạo phù và kiếm khí di tích của tiền nhân.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Triệu Quốc Ma Quật",
                "monsterId": "am_cot_ma_xa"
            },
            {
                "id": "stg_2",
                "name": "Huyết Sát Trận Pháp",
                "monsterId": "sat_luc_huyet_buc"
            },
            {
                "id": "boss",
                "name": "Thiên Phù Ảo Ảnh",
                "monsterId": "thac_nguyet_ho",
                "isBoss": true
            }
        ],
        "guaranteedPill": "truc_co_dan",
        "bonusPills": [
            "dan_giang_tran"
        ],
        "equipDrop": "cuu_khuc_bao",
        "stones": 17500,
        "expReward": 2000,
        "townId": "trieu_quoc"
    },
    {
        "id": "dong_hoang_phong_coc",
        "townId": "hoang_phong_coc",
        "name": "Hoàng Phong Cổ Động",
        "novel": "Tiên Nghịch",
        "icon": "🍁",
        "realmMin": 0,
        "stamina": 10,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Hoàng Phong Thị Trấn, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Hoàng Phong Trận Pháp",
                "monsterId": "ty_ong_vang"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "da_lang"
            },
            {
                "id": "boss",
                "name": "Hoàng Phong Trấn Sơn Thú",
                "monsterId": "thanh_lang_vuong",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_tay_tuy_hoan",
        "bonusPills": [
            "dan_pha_chuong"
        ],
        "equipDrop": "thanh_phong_kiem",
        "stones": 7500,
        "expReward": 600
    },
    {
        "id": "dong_lam_an",
        "townId": "lam_an",
        "name": "Lâm An Cổ Động",
        "novel": "Kiếm Lai",
        "icon": "🏘️",
        "realmMin": 0,
        "stamina": 10,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Lâm An Thành, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Lâm An Trận Pháp",
                "monsterId": "ty_tho_yeu"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "ty_ran_co"
            },
            {
                "id": "boss",
                "name": "Lâm An Trấn Sơn Thú",
                "monsterId": "thanh_xa",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_tay_tuy_hoan",
        "bonusPills": [
            "dan_pha_chuong"
        ],
        "equipDrop": "thanh_phong_kiem",
        "stones": 7500,
        "expReward": 600
    },
    {
        "id": "dong_that_huyen_mon",
        "townId": "that_huyen_mon",
        "name": "Thất Huyền Sơn Cổ Động",
        "novel": "Phàm Nhân Tu Tiên",
        "icon": "⚔️",
        "realmMin": 0,
        "stamina": 10,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Thất Huyền Sơn Trấn, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Thất Huyền Sơn Trận Pháp",
                "monsterId": "ty_tho_yeu"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "ty_chuot_dat"
            },
            {
                "id": "boss",
                "name": "Thất Huyền Sơn Trấn Sơn Thú",
                "monsterId": "thanh_lang_vuong",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_tay_tuy_hoan",
        "bonusPills": [
            "dan_pha_chuong"
        ],
        "equipDrop": "thanh_phong_kiem",
        "stones": 7500,
        "expReward": 600
    },
    {
        "id": "dong_huyet_sac",
        "name": "Huyết Sắc Thử Luyện",
        "novel": "Phàm Nhân Tu Tiên",
        "icon": "🩸",
        "realmMin": 1,
        "stamina": 15,
        "desc": "Cấm địa thí luyện của thất đại tông phái Việt Quốc, hung hiểm dị thường, nơi Hàn Lập đoạt Trúc Cơ Kỳ duyên.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Cấm Địa Kết Giới",
                "monsterId": "ty_ran_co"
            },
            {
                "id": "stg_2",
                "name": "Thụ Lâm Mai Phục",
                "monsterId": "kim_si_dieu"
            },
            {
                "id": "boss",
                "name": "Mặc Giao Động Quật",
                "monsterId": "mac_giao",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_pha_chuong",
        "bonusPills": [
            "truc_co_dan"
        ],
        "equipDrop": "tu_bac_dao",
        "stones": 10000,
        "expReward": 1200,
        "townId": "thien_nam"
    },
    {
        "id": "dong_ly_chau_kiem_tuyen",
        "townId": "ly_chau",
        "name": "Kiếm Tuyền Cổ Động",
        "novel": "Kiếm Lai",
        "icon": "🏺",
        "realmMin": 1,
        "stamina": 13,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Ly Châu Động Thiên, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Kiếm Tuyền Trận Pháp",
                "monsterId": "ty_hai_sam"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "ty_kiem_diep"
            },
            {
                "id": "boss",
                "name": "Kiếm Tuyền Trấn Sơn Thú",
                "monsterId": "nga_hoang_kiem_thu",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_pha_chuong",
        "bonusPills": [
            "truc_co_dan"
        ],
        "equipDrop": "tu_bac_dao",
        "stones": 20000,
        "expReward": 2400
    },
    {
        "id": "dong_nga_hoang",
        "name": "Ngạ Quỷ Sơn Cổ Quật",
        "novel": "Kiếm Lai",
        "icon": "⛰️",
        "realmMin": 3,
        "stamina": 30,
        "desc": "Nơi âm sát và kiếm khí giao tranh ngàn năm, lưu lại xương cốt của vô vàn kiếm tu ngã xuống.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Quỷ Lộ Bạch Cốt",
                "monsterId": "bach_cot_lang"
            },
            {
                "id": "stg_2",
                "name": "Sơn Trạch Kiếm Khí",
                "monsterId": "nga_hoang_kiem_thu"
            },
            {
                "id": "boss",
                "name": "Trảm Long Thạch Viên",
                "monsterId": "tram_long_thach_vien",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_giang_tran",
        "bonusPills": [
            "dan_ket_anh"
        ],
        "equipDrop": "luc_bao_kiem_y",
        "stones": 35000,
        "expReward": 5000,
        "townId": "nga_hoang"
    },
    {
        "id": "dong_gia_nguyen",
        "townId": "gia_nguyen",
        "name": "Gia Nguyên Cổ Động",
        "novel": "Tiên Nghịch",
        "icon": "⛵",
        "realmMin": 1,
        "stamina": 13,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Gia Nguyên Thành, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Gia Nguyên Trận Pháp",
                "monsterId": "ty_hai_sam"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "ty_kiem_diep"
            },
            {
                "id": "boss",
                "name": "Gia Nguyên Trấn Sơn Thú",
                "monsterId": "loi_oa_thu",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_pha_chuong",
        "bonusPills": [
            "truc_co_dan"
        ],
        "equipDrop": "tu_bac_dao",
        "stones": 20000,
        "expReward": 2400
    },
    {
        "id": "dong_thai_nhac",
        "townId": "thai_nhac",
        "name": "Thái Nhạc Sơn Cổ Động",
        "novel": "Kiếm Lai",
        "icon": "🌲",
        "realmMin": 2,
        "stamina": 16,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Thái Nhạc Sơn Trấn, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Thái Nhạc Sơn Trận Pháp",
                "monsterId": "thach_hau"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "thanh_xa"
            },
            {
                "id": "boss",
                "name": "Thái Nhạc Sơn Trấn Sơn Thú",
                "monsterId": "nga_hoang_kiem_thu",
                "isBoss": true
            }
        ],
        "guaranteedPill": "truc_co_dan",
        "bonusPills": [
            "dan_giang_tran"
        ],
        "equipDrop": "cuu_khuc_bao",
        "stones": 32500,
        "expReward": 4200
    },
    {
        "id": "dong_hu_thien",
        "name": "Hư Thiên Điện",
        "novel": "Phàm Nhân Tu Tiên",
        "icon": "🏛️",
        "realmMin": 2,
        "stamina": 25,
        "desc": "Cổ điện thượng cổ xuất hiện ngoài Loạn Tinh Hải, nơi Hàn Lập đoạt Hư Thiên Đỉnh.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Băng Hỏa Đạo Quan",
                "monsterId": "hoa_ho"
            },
            {
                "id": "stg_2",
                "name": "Quỷ Oán Khô Lâu Các",
                "monsterId": "u_hon"
            },
            {
                "id": "boss",
                "name": "Cực Âm Lão Tổ Phân Thân",
                "monsterId": "phong_lang",
                "isBoss": true
            }
        ],
        "guaranteedPill": "truc_co_dan",
        "bonusPills": [
            "dan_giang_tran",
            "dan_ket_anh"
        ],
        "equipDrop": "duong_kiem_ho",
        "stones": 25000,
        "expReward": 3000,
        "townId": "lac_duong"
    },
    {
        "id": "dong_thai_nham_dien",
        "townId": "thai_nham_dien",
        "name": "Thái Nhậm Động Thiên Cổ Động",
        "novel": "Tiên Nghịch",
        "icon": "⛩️",
        "realmMin": 3,
        "stamina": 19,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Thái Nhậm Động Thiên, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Thái Nhậm Động Thiên Trận Pháp",
                "monsterId": "ty_bang_thiem"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "ty_quy_anh"
            },
            {
                "id": "boss",
                "name": "Thái Nhậm Động Thiên Trấn Sơn Thú",
                "monsterId": "ban_son_trung",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_giang_tran",
        "bonusPills": [
            "dan_ket_anh"
        ],
        "equipDrop": "duong_kiem_ho",
        "stones": 45000,
        "expReward": 6000
    },
    {
        "id": "dong_yen_vu_giang",
        "townId": "yen_vu_giang",
        "name": "Yên Vũ Giang Cổ Động",
        "novel": "Kiếm Lai",
        "icon": "🌊",
        "realmMin": 3,
        "stamina": 19,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Yên Vũ Giang Trấn, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Yên Vũ Giang Trận Pháp",
                "monsterId": "ty_ech_doc"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "ty_ho_ly_con"
            },
            {
                "id": "boss",
                "name": "Yên Vũ Giang Trấn Sơn Thú",
                "monsterId": "u_minh_thu",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_giang_tran",
        "bonusPills": [
            "dan_ket_anh"
        ],
        "equipDrop": "duong_kiem_ho",
        "stones": 45000,
        "expReward": 6000
    },
    {
        "id": "dong_kinh_do_viet_quoc",
        "townId": "kinh_do_viet_quoc",
        "name": "Việt Quốc Hoàng Cổ Động",
        "novel": "Phàm Nhân Tu Tiên",
        "icon": "👑",
        "realmMin": 3,
        "stamina": 19,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Việt Quốc Hoàng Thành, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Việt Quốc Hoàng Trận Pháp",
                "monsterId": "ty_bang_thiem"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "ty_hoa_ho"
            },
            {
                "id": "boss",
                "name": "Việt Quốc Hoàng Trấn Sơn Thú",
                "monsterId": "ban_son_trung",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_giang_tran",
        "bonusPills": [
            "dan_ket_anh"
        ],
        "equipDrop": "duong_kiem_ho",
        "stones": 45000,
        "expReward": 6000
    },
    {
        "id": "dong_van_mong_coc",
        "townId": "van_mong_coc",
        "name": "Vân Mộng Sơn Cổ Động",
        "novel": "Tiên Nghịch",
        "icon": "🌫️",
        "realmMin": 3,
        "stamina": 19,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Vân Mộng Sơn Trấn, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Vân Mộng Sơn Trận Pháp",
                "monsterId": "ty_soi_con"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "ty_nhen_to"
            },
            {
                "id": "boss",
                "name": "Vân Mộng Sơn Trấn Sơn Thú",
                "monsterId": "ngu_loi_hac_ung",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_giang_tran",
        "bonusPills": [
            "dan_ket_anh"
        ],
        "equipDrop": "duong_kiem_ho",
        "stones": 45000,
        "expReward": 6000
    },
    {
        "id": "dong_loan_tinh_hai",
        "name": "Ngoại Hải Yêu Quật",
        "novel": "Phàm Nhân Tu Tiên",
        "icon": "🌊",
        "realmMin": 3,
        "stamina": 25,
        "desc": "Vực sâu ngoài vạn dặm Loạn Tinh Hải, nơi các tu sĩ Kim Đan săn lùng yêu thú bát giai lấy đan.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Hải Để Thâm Uyên",
                "monsterId": "loan_tinh_hai_giao"
            },
            {
                "id": "stg_2",
                "name": "Diệt Yêu Phong Vân",
                "monsterId": "huyet_hoang_thu"
            },
            {
                "id": "boss",
                "name": "Bát Trảo Hỏa Thú Chúa",
                "monsterId": "bat_trao_hoa_thu",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_giang_tran",
        "bonusPills": [
            "dan_ket_anh"
        ],
        "equipDrop": "cam_hon_phien",
        "stones": 30000,
        "expReward": 4000,
        "townId": "loan_tinh_hai"
    },
    {
        "id": "dong_thien_co",
        "townId": "thien_co",
        "name": "Thiên Cơ Cổ Động",
        "novel": "Phàm Nhân Tu Tiên",
        "icon": "🏯",
        "realmMin": 4,
        "stamina": 22,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Thiên Cơ Thành, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Thiên Cơ Trận Pháp",
                "monsterId": "ty_ret_lua"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "ty_luon_dien"
            },
            {
                "id": "boss",
                "name": "Thiên Cơ Trấn Sơn Thú",
                "monsterId": "tram_long_thach_vien",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_ket_anh",
        "bonusPills": [
            "dan_hoa_than"
        ],
        "equipDrop": "cam_hon_phien",
        "stones": 57500,
        "expReward": 7800
    },
    {
        "id": "dong_luc_ma_hai",
        "townId": "luc_ma_hai",
        "name": "Lục Ma Hải Ma Cổ Động",
        "novel": "Tiên Nghịch",
        "icon": "🌊",
        "realmMin": 4,
        "stamina": 22,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Lục Ma Hải Ma Thành, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Lục Ma Hải Ma Trận Pháp",
                "monsterId": "ty_ret_lua"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "ty_luon_dien"
            },
            {
                "id": "boss",
                "name": "Lục Ma Hải Ma Trấn Sơn Thú",
                "monsterId": "co_ma_hoa_than",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_ket_anh",
        "bonusPills": [
            "dan_hoa_than"
        ],
        "equipDrop": "cam_hon_phien",
        "stones": 57500,
        "expReward": 7800
    },
    {
        "id": "dong_khoi_tinh_dao",
        "townId": "khoi_tinh_dao",
        "name": "Khôi Tinh Cổ Động",
        "novel": "Kiếm Lai",
        "icon": "⚓",
        "realmMin": 4,
        "stamina": 22,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Khôi Tinh Đảo, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Khôi Tinh Trận Pháp",
                "monsterId": "ty_bang_thiem"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "ty_quy_anh"
            },
            {
                "id": "boss",
                "name": "Khôi Tinh Trấn Sơn Thú",
                "monsterId": "huyet_hoang_thu",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_ket_anh",
        "bonusPills": [
            "dan_hoa_than"
        ],
        "equipDrop": "cam_hon_phien",
        "stones": 57500,
        "expReward": 7800
    },
    {
        "id": "dong_tinh_cung",
        "townId": "tinh_cung",
        "name": "Thiên Tinh Cổ Động",
        "novel": "Phàm Nhân Tu Tiên",
        "icon": "✨",
        "realmMin": 4,
        "stamina": 22,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Thiên Tinh Thành, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Thiên Tinh Trận Pháp",
                "monsterId": "ty_ret_lua"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "ty_luon_dien"
            },
            {
                "id": "boss",
                "name": "Thiên Tinh Trấn Sơn Thú",
                "monsterId": "tram_long_thach_vien",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_ket_anh",
        "bonusPills": [
            "dan_hoa_than"
        ],
        "equipDrop": "cam_hon_phien",
        "stones": 57500,
        "expReward": 7800
    },
    {
        "id": "dong_am_minh",
        "name": "Âm Minh Chi Địa",
        "novel": "Phàm Nhân Tu Tiên",
        "icon": "🌑",
        "realmMin": 4,
        "stamina": 30,
        "desc": "Cấm địa tuyệt linh khí, chỉ có thể dựa vào thể phách và định lực để chiến đấu với quỷ vật.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Tuyệt Linh Hắc Vực",
                "monsterId": "phe_kim_trung"
            },
            {
                "id": "stg_2",
                "name": "Cốt Nhục Tái Sinh Trận",
                "monsterId": "u_lam_hoa_diep"
            },
            {
                "id": "boss",
                "name": "Âm La Quỷ Vương",
                "monsterId": "am_la_quy_vuong",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_ket_anh",
        "bonusPills": [
            "dan_hoa_than"
        ],
        "equipDrop": "dia_hoa_chuy",
        "stones": 45000,
        "expReward": 6500,
        "townId": "am_la_tong"
    },
    {
        "id": "dong_tien_di",
        "name": "Vũ Trụ Tiên Di Cổ Giới",
        "novel": "Tiên Nghịch & Phàm Nhân",
        "icon": "🪐",
        "realmMin": 6,
        "stamina": 40,
        "desc": "Mảnh vỡ Tiên Giới cổ đại phiêu dạt giữa các vì sao, chôn giấu tiên tích bất tử.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Tinh Thần Phế Tích",
                "monsterId": "son_nhac_cu_vien"
            },
            {
                "id": "stg_2",
                "name": "Tiên Giới Toái Phiến",
                "monsterId": "de_hon_thu"
            },
            {
                "id": "boss",
                "name": "Tiên Di Thánh Thú",
                "monsterId": "tien_di_thanh_thu",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_ngu_hanh_lo",
        "bonusPills": [
            "dan_co_than_tinh_huyet"
        ],
        "equipDrop": "thap_nhi_co_than",
        "stones": 100000,
        "expReward": 15000,
        "townId": "tien_di_chau"
    },
    {
        "id": "dong_bac_cau_lo",
        "townId": "bac_cau_lo",
        "name": "Bắc Câu Lô Cổ Động",
        "novel": "Phàm Nhân Tu Tiên",
        "icon": "❄️",
        "realmMin": 6,
        "stamina": 28,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Bắc Câu Lô Châu, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Bắc Câu Lô Trận Pháp",
                "monsterId": "ty_loi_long_con"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "son_nhac_cu_vien"
            },
            {
                "id": "boss",
                "name": "Bắc Câu Lô Trấn Sơn Thú",
                "monsterId": "tinh_khong_cu_thu",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_ngu_hanh_lo",
        "bonusPills": [
            "dan_co_than_tinh_huyet"
        ],
        "equipDrop": "dia_hoa_chuy",
        "stones": 82500,
        "expReward": 11400
    },
    {
        "id": "dong_lac_hon_coc",
        "townId": "lac_hon_coc",
        "name": "Lạc Hồn Cổ Động",
        "novel": "Tiên Nghịch",
        "icon": "⚡",
        "realmMin": 5,
        "stamina": 25,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Lạc Hồn Cốc, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Lạc Hồn Trận Pháp",
                "monsterId": "ty_tu_la_dieu"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "loi_ung"
            },
            {
                "id": "boss",
                "name": "Lạc Hồn Trấn Sơn Thú",
                "monsterId": "am_la_quy_vuong",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_hoa_than",
        "bonusPills": [
            "dan_ngu_hanh_lo"
        ],
        "equipDrop": "luc_bao_kiem_y",
        "stones": 70000,
        "expReward": 9600
    },
    {
        "id": "dong_van_diep_thanh",
        "townId": "van_diep_thanh",
        "name": "Vạn Diệp Cổ Cổ Động",
        "novel": "Kiếm Lai",
        "icon": "🍃",
        "realmMin": 5,
        "stamina": 25,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Vạn Diệp Cổ Trấn, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Vạn Diệp Cổ Trận Pháp",
                "monsterId": "ty_loi_long_con"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "van_thu"
            },
            {
                "id": "boss",
                "name": "Vạn Diệp Cổ Trấn Sơn Thú",
                "monsterId": "thien_nam_co_dieu",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_hoa_than",
        "bonusPills": [
            "dan_ngu_hanh_lo"
        ],
        "equipDrop": "luc_bao_kiem_y",
        "stones": 70000,
        "expReward": 9600
    },
    {
        "id": "dong_chu_tuoc_tinh",
        "name": "Chu Tước Cửu Huyền Động",
        "novel": "Tiên Nghịch",
        "icon": "🔥",
        "realmMin": 7,
        "stamina": 45,
        "desc": "Hỏa động phong ấn bổn nguyên Chu Tước, linh khí hỏa diễm thiêu đốt vạn vật.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Tu Chân Liên Minh Trận",
                "monsterId": "huyet_ky_lan"
            },
            {
                "id": "stg_2",
                "name": "Hỏa Diễm Tinh Hạch",
                "monsterId": "thon_hu_kinh"
            },
            {
                "id": "boss",
                "name": "Chu Tước Hỏa Điểu",
                "monsterId": "chu_tuoc_hoa_dieu",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_co_than_tinh_huyet",
        "bonusPills": [
            "dan_dai_thua"
        ],
        "equipDrop": "tram_tien_kiem",
        "stones": 120000,
        "expReward": 18000,
        "townId": "chu_tuoc_quoc"
    },
    {
        "id": "dong_van_yeu",
        "townId": "van_yeu",
        "name": "Vạn Yêu Cổ Động",
        "novel": "Tiên Nghịch",
        "icon": "🌋",
        "realmMin": 8,
        "stamina": 34,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Vạn Yêu Thành, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Vạn Yêu Trận Pháp",
                "monsterId": "ty_bo_cap"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "ty_rua_nuoc"
            },
            {
                "id": "boss",
                "name": "Vạn Yêu Trấn Sơn Thú",
                "monsterId": "tien_di_thanh_thu",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_dai_thua",
        "bonusPills": [
            "dan_do_kiep"
        ],
        "equipDrop": "thap_nhi_co_than",
        "stones": 107500,
        "expReward": 15000
    },
    {
        "id": "dong_tuyet_tinh_coc",
        "townId": "tuyet_tinh_coc",
        "name": "Tuyệt Tình Cổ Động",
        "novel": "Kiếm Lai",
        "icon": "💔",
        "realmMin": 6,
        "stamina": 28,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Tuyệt Tình Cốc, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Tuyệt Tình Trận Pháp",
                "monsterId": "kim_giap_ngac"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "cuu_vi_ho"
            },
            {
                "id": "boss",
                "name": "Tuyệt Tình Trấn Sơn Thú",
                "monsterId": "co_than_do_tu_ve",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_ngu_hanh_lo",
        "bonusPills": [
            "dan_co_than_tinh_huyet"
        ],
        "equipDrop": "dia_hoa_chuy",
        "stones": 82500,
        "expReward": 11400
    },
    {
        "id": "dong_hoang_tuyen_thanh",
        "townId": "hoang_tuyen_thanh",
        "name": "Hoàng Tuyền Ma Cổ Động",
        "novel": "Phàm Nhân Tu Tiên",
        "icon": "👹",
        "realmMin": 6,
        "stamina": 28,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Hoàng Tuyền Ma Đô, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Hoàng Tuyền Ma Trận Pháp",
                "monsterId": "ty_hon_don_trung"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "thuy_ky_lan"
            },
            {
                "id": "boss",
                "name": "Hoàng Tuyền Ma Trấn Sơn Thú",
                "monsterId": "huyet_ky_lan",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_ngu_hanh_lo",
        "bonusPills": [
            "dan_co_than_tinh_huyet"
        ],
        "equipDrop": "dia_hoa_chuy",
        "stones": 82500,
        "expReward": 11400
    },
    {
        "id": "dong_phu_tang_dao",
        "townId": "phu_tang_dao",
        "name": "Phù Tang Cổ Cổ Động",
        "novel": "Tiên Nghịch",
        "icon": "🌅",
        "realmMin": 6,
        "stamina": 28,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Phù Tang Cổ Đảo, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Phù Tang Cổ Trận Pháp",
                "monsterId": "kim_giap_ngac"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "cuu_vi_ho"
            },
            {
                "id": "boss",
                "name": "Phù Tang Cổ Trấn Sơn Thú",
                "monsterId": "thon_hu_kinh",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_ngu_hanh_lo",
        "bonusPills": [
            "dan_co_than_tinh_huyet"
        ],
        "equipDrop": "dia_hoa_chuy",
        "stones": 82500,
        "expReward": 11400
    },
    {
        "id": "dong_kiem_khi_truong_thanh",
        "name": "Kiếm Khí Trường Thành Di Tích",
        "novel": "Kiếm Lai",
        "icon": "⚔️",
        "realmMin": 5,
        "stamina": 35,
        "desc": "Đầu tường vạn dặm ngâm kiếm khí vạn năm, nơi các đại kiếm tiên trảm yêu quyết tử.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Man Hoang Tiền Tuyến",
                "monsterId": "kiem_khi_bach_vien"
            },
            {
                "id": "stg_2",
                "name": "Đầu Tường Kiếm Ngân",
                "monsterId": "tu_la_ma_thu"
            },
            {
                "id": "boss",
                "name": "Trường Thành Kiếm Linh",
                "monsterId": "truong_thanh_kiem_linh",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_hoa_than",
        "bonusPills": [
            "dan_ngu_hanh_lo"
        ],
        "equipDrop": "thanh_truc_kiem",
        "stones": 70000,
        "expReward": 10000,
        "townId": "kiem_khi_truong_thanh"
    },
    {
        "id": "dong_co_than",
        "name": "Cổ Thần Chi Địa",
        "novel": "Tiên Nghịch",
        "icon": "🗿",
        "realmMin": 5,
        "stamina": 35,
        "desc": "Thi hài Cổ Thần Đồ Tư ngàn vạn dặm, nơi Vương Lâm đoạt Ức Niệm Thần Thông và truyền thừa.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Khí Hải Huyết Hải",
                "monsterId": "huyen_bang_mang"
            },
            {
                "id": "stg_2",
                "name": "Đoạt Hồn Hắc Vụ",
                "monsterId": "loi_ung"
            },
            {
                "id": "boss",
                "name": "Cổ Thần Đồ Tư Thị Vệ",
                "monsterId": "co_than_do_tu_ve",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_hoa_than",
        "bonusPills": [
            "dan_ngu_hanh_lo",
            "dan_co_than_tinh_huyet"
        ],
        "equipDrop": "thanh_truc_kiem",
        "stones": 60000,
        "expReward": 8000,
        "townId": "dong_linh_tinh"
    },
    {
        "id": "dong_quy_nguyen_tong",
        "townId": "quy_nguyen_tong",
        "name": "Quy Nguyên Cổ Động",
        "novel": "Tiên Nghịch",
        "icon": "☯️",
        "realmMin": 7,
        "stamina": 31,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Quy Nguyên Tiên Thành, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Quy Nguyên Trận Pháp",
                "monsterId": "ty_thai_co_trung"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "thai_co_loi_long"
            },
            {
                "id": "boss",
                "name": "Quy Nguyên Trấn Sơn Thú",
                "monsterId": "truong_thanh_kiem_linh",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_co_than_tinh_huyet",
        "bonusPills": [
            "dan_dai_thua"
        ],
        "equipDrop": "thanh_truc_kiem",
        "stones": 95000,
        "expReward": 13200
    },
    {
        "id": "dong_thien_yeu_thanh",
        "townId": "thien_yeu_thanh",
        "name": "Thiên Yêu Vương Cổ Động",
        "novel": "Kiếm Lai",
        "icon": "🦅",
        "realmMin": 7,
        "stamina": 31,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Thiên Yêu Vương Thành, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Thiên Yêu Vương Trận Pháp",
                "monsterId": "ty_thai_co_trung"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "la_hau_co_thu"
            },
            {
                "id": "boss",
                "name": "Thiên Yêu Vương Trấn Sơn Thú",
                "monsterId": "dong_linh_co_giao",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_co_than_tinh_huyet",
        "bonusPills": [
            "dan_dai_thua"
        ],
        "equipDrop": "thanh_truc_kiem",
        "stones": 95000,
        "expReward": 13200
    },
    {
        "id": "dong_tinh_khong_trang",
        "townId": "tinh_khong_trang",
        "name": "Tinh Không Dịch Trạm Cổ Động",
        "novel": "Phàm Nhân Tu Tiên",
        "icon": "🌌",
        "realmMin": 8,
        "stamina": 34,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Tinh Không Dịch Trạm, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Tinh Không Dịch Trạm Trận Pháp",
                "monsterId": "du_thien_con_bang"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "la_hau_co_thu"
            },
            {
                "id": "boss",
                "name": "Tinh Không Dịch Trạm Trấn Sơn Thú",
                "monsterId": "man_hoang_dai_yeu",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_dai_thua",
        "bonusPills": [
            "dan_do_kiep"
        ],
        "equipDrop": "thap_nhi_co_than",
        "stones": 107500,
        "expReward": 15000
    },
    {
        "id": "dong_man_hoang",
        "name": "Man Hoang Thánh Sơn Mật Cảnh",
        "novel": "Phàm Nhân Tu Tiên",
        "icon": "🏔️",
        "realmMin": 9,
        "stamina": 55,
        "desc": "Cực hiểm cấm địa sâu trong Man Hoang thế giới Linh Giới, nơi chân linh thượng cổ qua lại.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Linh Giới Thần Đỉnh",
                "monsterId": "cuu_muc_thiem"
            },
            {
                "id": "stg_2",
                "name": "Thái Cổ Lôi Uyên",
                "monsterId": "thai_co_loi_long"
            },
            {
                "id": "boss",
                "name": "Đông Linh Cổ Giao",
                "monsterId": "dong_linh_co_giao",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_dai_thua",
        "bonusPills": [
            "dan_do_kiep"
        ],
        "equipDrop": "tram_tien_kiem",
        "stones": 190000,
        "expReward": 32000,
        "townId": "man_hoang_thien_dia"
    },
    {
        "id": "dong_tu_la_huyet_hai",
        "name": "Tu La Huyết Hải Cổ Động",
        "novel": "Tiên Nghịch",
        "icon": "🩸",
        "realmMin": 6,
        "stamina": 40,
        "desc": "Biển máu vô tận ma khí dâng trào, nơi rèn luyện sát đạo bản nguyên và ma tâm.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Vạn Cốt Huyết Thủy",
                "monsterId": "tinh_khong_cu_thu"
            },
            {
                "id": "stg_2",
                "name": "Sát Lục Bản Nguyên",
                "monsterId": "van_thu"
            },
            {
                "id": "boss",
                "name": "Tu La Huyết Long",
                "monsterId": "tu_la_huyet_long",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_ngu_hanh_lo",
        "bonusPills": [
            "dan_co_than_tinh_huyet"
        ],
        "equipDrop": "thap_nhi_co_than",
        "stones": 90000,
        "expReward": 13000,
        "townId": "bac_han_tien_cung"
    },
    {
        "id": "dong_ma_quat",
        "name": "Vạn Kiếp Ma Quật",
        "novel": "Tiên Nghịch & Kiếm Lai",
        "icon": "🌋",
        "realmMin": 8,
        "stamina": 50,
        "desc": "Vực sâu ma khí tụ tán, chôn vùi thần ma thượng cổ, hung hiểm vạn phần nhưng ẩn chứa chí bảo nghịch thiên.",
        "stages": [
            {
                "id": "stg_1",
                "name": "U Minh Địa Ngục",
                "monsterId": "kim_giap_ngac"
            },
            {
                "id": "stg_2",
                "name": "Hoang Cổ Ma Tê",
                "monsterId": "thuy_ky_lan"
            },
            {
                "id": "boss",
                "name": "Hắc Ma Cự Thần",
                "monsterId": "cuu_vi_ho",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_co_than_tinh_huyet",
        "bonusPills": [
            "dan_dai_thua",
            "dan_do_kiep",
            "dan_bo_thien"
        ],
        "equipDrop": "thap_nhi_co_than",
        "stones": 150000,
        "expReward": 25000,
        "townId": "thien_dao_tong"
    },
    {
        "id": "dong_to_long_dao",
        "townId": "to_long_dao",
        "name": "Tổ Long Cổ Động",
        "novel": "Tiên Nghịch",
        "icon": "🐉",
        "realmMin": 9,
        "stamina": 37,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Tổ Long Thánh Vực, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Tổ Long Trận Pháp",
                "monsterId": "la_hau_co_thu"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "bac_han_tien_hac"
            },
            {
                "id": "boss",
                "name": "Tổ Long Trấn Sơn Thú",
                "monsterId": "hon_don_to_long",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_do_kiep",
        "bonusPills": [
            "dan_bo_thien"
        ],
        "equipDrop": "tram_tien_kiem",
        "stones": 120000,
        "expReward": 16800
    },
    {
        "id": "dong_do_kiep_dai",
        "townId": "do_kiep_dai",
        "name": "Vạn Kiếp Phong Lôi Cổ Động",
        "novel": "Kiếm Lai",
        "icon": "🌪️",
        "realmMin": 10,
        "stamina": 40,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Vạn Kiếp Phong Lôi Trấn, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Vạn Kiếp Phong Lôi Trận Pháp",
                "monsterId": "chan_tien_ma_khi"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "thien_dao_loi_thu"
            },
            {
                "id": "boss",
                "name": "Vạn Kiếp Phong Lôi Trấn Sơn Thú",
                "monsterId": "tien_de_tan_niem",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_ban_tien",
        "bonusPills": [
            "dan_dang_tien",
            "dan_bo_thien",
            "dan_tieu_canh_10"
        ],
        "equipDrop": "thien_tien_dong_y",
        "stones": 132500,
        "expReward": 18600
    },
    {
        "id": "dong_thai_so",
        "name": "Thái Sơ Hỗn Độn Động",
        "novel": "Tiên Nghịch & Kiếm Lai",
        "icon": "🌌",
        "realmMin": 10,
        "stamina": 60,
        "desc": "Nơi khởi nguyên thiên địa càn khôn, chư thần hỗn độn ngủ say cùng đại đạo quy nhất.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Bắc Hàn Tiên Vực",
                "monsterId": "bac_han_tien_hac"
            },
            {
                "id": "stg_2",
                "name": "Chân Tiên Ma Khí",
                "monsterId": "chan_tien_ma_khi"
            },
            {
                "id": "boss",
                "name": "Hỗn Độn Tổ Long Thần",
                "monsterId": "hon_don_to_long",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_dang_tien",
        "bonusPills": [
            "dan_ban_tien",
            "dan_bo_thien",
            "dan_dia_tien",
            "dan_tieu_canh_11",
            "dan_tieu_canh_12"
        ],
        "equipDrop": "tram_tien_kiem",
        "stones": 250000,
        "expReward": 50000,
        "townId": "tien_gioi_khoi_nguyen"
    },
    {
        "id": "dong_lao_quan_dien",
        "townId": "lao_quan_dien",
        "name": "Thái Thanh Cổ Động",
        "novel": "Tiên Nghịch",
        "icon": "🏛️",
        "realmMin": 12,
        "stamina": 46,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Thái Thanh Tiên Điện, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Thái Thanh Trận Pháp",
                "monsterId": "chan_tien_ma_khi"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "dai_la_kim_long"
            },
            {
                "id": "boss",
                "name": "Thái Thanh Trấn Sơn Thú",
                "monsterId": "thai_co_to_than",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_bo_thien",
        "bonusPills": [
            "dan_dia_tien",
            "dan_nhan_tien",
            "dan_chan_tien",
            "dan_tieu_canh_13",
            "dan_tieu_canh_14",
            "dan_tieu_canh_15"
        ],
        "equipDrop": "thien_tien_dong_y",
        "stones": 157500,
        "expReward": 22200
    },
    {
        "id": "dong_dai_la_thanh_do",
        "townId": "dai_la_thanh_do",
        "name": "Đại La Cổ Động",
        "novel": "Kiếm Lai",
        "icon": "👑",
        "realmMin": 13,
        "stamina": 49,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Đại La Thiên Đô, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Đại La Trận Pháp",
                "monsterId": "dai_la_kim_long"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "hon_don_to_long"
            },
            {
                "id": "boss",
                "name": "Đại La Trấn Sơn Thú",
                "monsterId": "tien_de_tan_niem",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_chan_tien",
        "bonusPills": [
            "dan_huyen_tien",
            "dan_thien_tien",
            "dan_kim_tien",
            "dan_tieu_canh_16",
            "dan_tieu_canh_17",
            "dan_tieu_canh_18",
            "dan_tieu_canh_19",
            "dan_tieu_canh_20"
        ],
        "equipDrop": "thien_tien_dong_y",
        "stones": 170000,
        "expReward": 24000
    },
    {
        "id": "dong_hon_don_tien_dinh",
        "townId": "hon_don_tien_dinh",
        "name": "Hỗn Độn Cổ Động",
        "novel": "Phàm Nhân Tu Tiên",
        "icon": "☸️",
        "realmMin": 14,
        "stamina": 52,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Hỗn Độn Tiên Đình, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Hỗn Độn Trận Pháp",
                "monsterId": "hon_don_to_long"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "thien_dao_loi_thu"
            },
            {
                "id": "boss",
                "name": "Hỗn Độn Trấn Sơn Thú",
                "monsterId": "tien_de_tan_niem",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_thai_at",
        "bonusPills": [
            "dan_thai_at_chan_tien",
            "dan_thai_at_huyen_tien",
            "dan_dai_la_chan_tien",
            "dan_dai_la",
            "dan_hon_nguyen",
            "dan_tieu_canh_21",
            "dan_tieu_canh_22",
            "dan_tieu_canh_23",
            "dan_tieu_canh_24",
            "dan_tieu_canh_25"
        ],
        "equipDrop": "thien_tien_dong_y",
        "stones": 182500,
        "expReward": 25800
    },
    {
        "id": "dong_truong_sinh_gioi",
        "townId": "truong_sinh_gioi",
        "name": "Trường Sinh Cổ Động",
        "novel": "Tiên Nghịch",
        "icon": "🌸",
        "realmMin": 15,
        "stamina": 55,
        "desc": "Cổ động bí cảnh tọa lạc phụ cận Trường Sinh Thánh Cảnh, nơi ngưng tụ linh khí ngàn năm và cất giữ cơ duyên viễn cổ.",
        "stages": [
            {
                "id": "stg_1",
                "name": "Trường Sinh Trận Pháp",
                "monsterId": "thien_dao_loi_thu"
            },
            {
                "id": "stg_2",
                "name": "Động Quật Thâm Uyên",
                "monsterId": "thai_co_to_than"
            },
            {
                "id": "boss",
                "name": "Trường Sinh Trấn Sơn Thú",
                "monsterId": "tien_de_tan_niem",
                "isBoss": true
            }
        ],
        "guaranteedPill": "dan_tien_quan",
        "bonusPills": [
            "dan_tien_ton",
            "dan_chuan_tien_vuong",
            "dan_tien_vuong",
            "dan_tien_de",
            "dan_hau_tien_de_31",
            "dan_tieu_canh_26",
            "dan_tieu_canh_27",
            "dan_tieu_canh_28",
            "dan_tieu_canh_29",
            "dan_tieu_canh_30"
        ],
        "equipDrop": "thien_tien_dong_y",
        "stones": 195000,
        "expReward": 27600
    },
    ...POST_TIEN_DE_DUNGEONS
];
// Các châu là vùng nội bộ thuộc một trong hai thế giới Phàm Giới hoặc Tiên Giới.
const MAPS = Object.freeze([
    {
        id: "map_1",
        name: "Thanh Châu (Phàm Trần Giới)",
        provinceName: "Thanh Châu",
        realmMin: 0,
        realmMax: 1,
        desc: "Cõi phàm tục, linh khí mỏng manh, nơi bắt đầu con đường tu tiên.",
        townIds: ["thanh_van", "trieu_quoc", "hoang_phong_coc", "lam_an", "that_huyen_mon"]
    },
    {
        id: "map_2",
        name: "U Châu (Luyện Khí Giới)",
        provinceName: "U Châu",
        realmMin: 1,
        realmMax: 2,
        desc: "Linh khí sơ khai, chư phái tranh hùng, trăm sông đổ về một biển.",
        townIds: ["thien_nam", "ly_chau", "nga_hoang", "gia_nguyen", "thai_nhac"]
    },
    {
        id: "map_3",
        name: "Vân Châu (Trúc Cơ Vực)",
        provinceName: "Vân Châu",
        realmMin: 2,
        realmMax: 3,
        desc: "Đúc thành Đạo Cơ, bước vào hàng ngũ tu chân chân chính.",
        townIds: ["lac_duong", "thai_nham_dien", "yen_vu_giang", "kinh_do_viet_quoc", "van_mong_coc"]
    },
    {
        id: "map_4",
        name: "Hải Châu (Kim Đan Giới)",
        provinceName: "Hải Châu",
        realmMin: 4,
        realmMax: 4,
        desc: "Kim Đan nhất chuyển định càn khôn, sóng gió Loạn Tinh Hải.",
        townIds: ["loan_tinh_hai", "thien_co", "luc_ma_hai", "khoi_tinh_dao", "tinh_cung"]
    },
    {
        id: "map_5",
        name: "Lôi Châu (Nguyên Anh Vực)",
        provinceName: "Lôi Châu",
        realmMin: 5,
        realmMax: 5,
        desc: "Nguyên Anh xuất khiếu di sơn đảo hải, thọ nguyên ngàn năm.",
        townIds: ["am_la_tong", "tien_di_chau", "bac_cau_lo", "lac_hon_coc", "van_diep_thanh"]
    },
    {
        id: "map_6",
        name: "Viêm Châu (Hóa Thần Giới)",
        provinceName: "Viêm Châu",
        realmMin: 6,
        realmMax: 7,
        desc: "Hóa Thần ngộ đạo, tiếp cận quy tắc thiên địa sơ khai.",
        townIds: ["chu_tuoc_quoc", "van_yeu", "tuyet_tinh_coc", "hoang_tuyen_thanh", "phu_tang_dao"]
    },
    {
        id: "map_7",
        name: "Cương Châu (Luyện Hư Giới)",
        provinceName: "Cương Châu",
        realmMin: 7,
        realmMax: 8,
        desc: "Luyện Hư hợp đạo, vạn dặm kiếm khí trường thành trấn thủ biên thùy.",
        townIds: ["kiem_khi_truong_thanh", "dong_linh_tinh", "quy_nguyen_tong", "thien_yeu_thanh", "tinh_khong_trang"]
    },
    {
        id: "map_8",
        name: "Man Châu (Hợp Thể Độ Kiếp Vực)",
        provinceName: "Man Châu",
        realmMin: 8,
        realmMax: 10,
        desc: "Hợp Thể đại năng, Man Hoang vô tận, Độ Kiếp nghịch lôi kinh thiên động địa.",
        townIds: ["man_hoang_thien_dia", "bac_han_tien_cung", "thien_dao_tong", "to_long_dao", "do_kiep_dai"]
    },
    {
        id: "map_9",
        name: "Cửu Thiên Tiên Giới (Phi Thăng Giới)",
        provinceName: "Cửu Thiên Tiên Giới",
        realmMin: 11,
        realmMax: 30,
        ascensionRequired: true,
        realmMinName: 'Bán Tiên',
        desc: "Tiên Giới chí cao vô thượng, chỉ mở cửa sau khi đạt Bán Tiên và hoàn thành nghi thức dâng Thiên Đạo Nguyên Ấn.",
        townIds: ["tien_gioi_khoi_nguyen", "lao_quan_dien", "dai_la_thanh_do", "hon_don_tien_dinh", "truong_sinh_gioi"]
    },
    ...POST_TIEN_DE_MAP_CONFIGS.map(map => ({
        id: map.id, name: map.name, provinceName: map.name, realmMin: map.realmMin, realmMax: map.realmMax,
        realmMinName: POST_TIEN_DE_REALMS[map.realmMin - 31],
        ascensionRequired: true,
        desc: `${map.name} mở ra sau Tiên Đế, phân bổ cảnh giới ${map.realmMin}–${map.realmMax}; có hai thành trấn và bí cảnh riêng.`,
        townIds: map.townIds,
    }))
].map(map => {
    const townRealms = map.townIds.map(id => TOWN_BY_ID.get(id)?.realmMin).filter(Number.isFinite);
    const realmMin = Math.min(map.realmMin, ...townRealms);
    const realmMax = Math.max(map.realmMax, ...townRealms);
    return {
        ...map,
        worldId: map.ascensionRequired ? 'world_tien' : 'world_pham',
        worldName: map.ascensionRequired ? 'Tiên Giới' : 'Phàm Giới',
        realmMin,
        realmMax,
        realmMinName: map.realmMinName || CULTIVATION_REALM_NAMES[realmMin] || 'Phàm Nhân',
        realmMaxName: map.realmMaxName || CULTIVATION_REALM_NAMES[realmMax] || 'Vạn Giới Chi Chủ',
    };
}));

const MAP_BY_ID = new Map(MAPS.map(m => [m.id, m]));

// Province gates join neighbouring regions inside each realm. Their map-space
// coordinates mirror the row-major province layout used by the Unity world map.
const WORLD_PROVINCE_GATES = Object.freeze((() => {
    const gates = [];
    const add = (worldId, from, to, direction) => {
        const worldMaps = MAPS.filter(map => map.worldId === worldId);
        const columns = worldId === 'world_tien' ? 4 : 3;
        const fromIndex = worldMaps.findIndex(map => map.id === from.id);
        const toIndex = worldMaps.findIndex(map => map.id === to.id);
        const fromOffsetX = (fromIndex % columns) * 256;
        const fromOffsetY = Math.floor(fromIndex / columns) * 160;
        const toOffsetX = (toIndex % columns) * 256;
        const toOffsetY = Math.floor(toIndex / columns) * 160;
        const horizontal = direction === 'right';
        gates.push(Object.freeze({
            id: `gate_${from.id}_${to.id}`,
            worldId,
            a: Object.freeze({
                mapId: from.id,
                x: fromOffsetX + (horizontal ? 251 : 128),
                y: fromOffsetY + (horizontal ? 80 : 155),
            }),
            b: Object.freeze({
                mapId: to.id,
                x: toOffsetX + (horizontal ? 4 : 128),
                y: toOffsetY + (horizontal ? 80 : 4),
            }),
            realmMin: to.realmMin,
        }));
    };
    for (const worldId of ['world_pham', 'world_tien']) {
        const worldMaps = MAPS.filter(map => map.worldId === worldId);
        const columns = worldId === 'world_tien' ? 4 : 3;
        for (let index = 0; index < worldMaps.length; index++) {
            const map = worldMaps[index];
            const column = index % columns;
            if (column + 1 < columns && index + 1 < worldMaps.length) add(worldId, map, worldMaps[index + 1], 'right');
            if (index + columns < worldMaps.length) add(worldId, map, worldMaps[index + columns], 'down');
        }
    }
    return gates;
})());

// One dedicated pair of gates connects the two separate realm maps.
const WORLD_ASCENSION_GATES = Object.freeze({
    world_pham: Object.freeze({ mapId: 'map_8', townId: 'man_hoang_thien_dia', x: 502, y: 400 }),
    world_tien: Object.freeze({ mapId: 'map_9', townId: 'tien_gioi_khoi_nguyen', x: 14, y: 80 }),
});

// Mỗi Cổ Động có ba thủ hộ riêng biệt, theo hệ khác nhau và nằm trong trần
// cảnh giới thật của thành/map. Những thành chỉ có một hoặc hai bậc realm sẽ
// dùng thêm tiểu cảnh giới để ba ải vẫn có tiến trình tăng dần.
const DUNGEON_SUBSTAGE_NAMES = Object.freeze(['Sơ kỳ', 'Trung kỳ', 'Hậu kỳ', 'Viên mãn', 'Đại viên mãn']);
const DUNGEON_MONSTER_BY_ID = new Map(MONSTERS.map(monster => [monster.id, monster]));
const dungeonTownRealmCap = town => {
    const townRealm = Math.max(0, Number(town?.realmMin) || 0);
    const mapMax = Number(MAP_BY_ID.get(town?.mapId)?.realmMax);
    const mapCeiling = Number.isFinite(mapMax) ? Math.max(townRealm, mapMax) : 65;
    const explicitCap = Number.isFinite(Number(town?.realmCap)) ? Number(town.realmCap) : null;
    const configuredCap = explicitCap ?? Math.max(townRealm + 3, (town?.monsterPool || []).reduce((max, id) => Math.max(max, DUNGEON_MONSTER_BY_ID.get(id)?.realm || 0), 0));
    const hardCeiling = Math.min(65, mapCeiling, explicitCap ?? townRealm + 3);
    return Math.max(townRealm, Math.min(configuredCap, hardCeiling));
};

const chooseDungeonGuardians = (town, cap) => {
    const minRealm = Math.max(0, Number(town?.realmMin) || 0);
    const spread = Math.max(0, cap - minRealm);
    const candidates = MONSTERS.filter(monster => !monster.worldBoss
        && Number.isInteger(Number(monster.realm))
        && monster.realm >= minRealm && monster.realm <= cap
        && HE[monster.element]);
    const byRealm = new Map();
    for (const monster of candidates) {
        const list = byRealm.get(monster.realm) || [];
        list.push(monster);
        byRealm.set(monster.realm, list);
    }

    const targets = spread >= 2
        ? [minRealm, minRealm + 1, minRealm + 2]
        : spread === 1 ? [minRealm, cap, cap] : [minRealm, minRealm, minRealm];
    let best = null;
    const realms = [...byRealm.keys()].sort((a, b) => a - b);
    const requireDistinctRealms = spread >= 2;
    for (const realm1 of realms) for (const realm2 of realms) for (const realm3 of realms) {
        if (realm1 < minRealm || realm3 > cap || realm2 < realm1 || realm3 < realm2) continue;
        if (requireDistinctRealms && !(realm1 < realm2 && realm2 < realm3)) continue;
        const stagePools = [realm1, realm2, realm3].map(realm => byRealm.get(realm) || []);
        for (const first of stagePools[0]) for (const second of stagePools[1]) for (const third of stagePools[2]) {
            if (first.id === second.id || first.id === third.id || second.id === third.id) continue;
            if (first.element === second.element || first.element === third.element || second.element === third.element) continue;
            let score = [realm1, realm2, realm3].reduce((sum, realm, i) => sum + Math.abs(realm - targets[i]) * 1000, 0);
            // Make the final encounter a true guardian whenever the realm pool allows it.
            if (third.small) score += 250;
            // Prefer natural stat progression; combat still enforces a small minimum rise.
            const stats = [first, second, third].map(monster => [monster.hp || 0, monster.atk || 0, monster.def || 0]);
            for (let i = 1; i < stats.length; i++) {
                for (let j = 0; j < 3; j++) {
                    if (stats[i][j] <= stats[i - 1][j]) score += Math.min(150, Math.round((stats[i - 1][j] - stats[i][j] + 1) / Math.max(1, stats[i - 1][j]) * 150));
                }
            }
            const ids = [first.id, second.id, third.id].join('|');
            let varietyHash = 2166136261;
            for (const char of `${town?.id || ''}:${ids}`) varietyHash = Math.imul(varietyHash ^ char.charCodeAt(0), 16777619) >>> 0;
            score += varietyHash % 1000;
            if (!best || score < best.score || (score === best.score && ids < best.guardians.map(monster => monster.id).join('|'))) {
                best = { score, guardians: [first, second, third] };
            }
        }
    }

    if (best) return best.guardians;

    // Defensive fallback: the catalogue currently has a valid unique set for every
    // town; keep cap/system integrity if a future data change removes a realm pool.
    const fallback = candidates.slice().sort((a, b) => a.realm - b.realm || Number(a.small) - Number(b.small));
    const picked = [];
    for (const monster of fallback) {
        if (picked.some(other => other.id === monster.id || other.element === monster.element)) continue;
        picked.push(monster);
        if (picked.length === 3) break;
    }
    if (picked.length !== 3) throw new Error(`Không đủ 3 thủ hộ khác hệ cho Cổ Động ${town?.id || 'unknown'}`);
    return picked.sort((a, b) => a.realm - b.realm);
};

const normalizeDungeonStages = (dungeon, town, cap) => {
    const guardians = chooseDungeonGuardians(town, cap);
    const distinctRealms = new Set(guardians.map(monster => monster.realm)).size === 3;
    const needsSubstages = cap - town.realmMin < 2 || !distinctRealms;
    const substageOrder = needsSubstages ? [0, 2, 4] : [null, null, null];
    return guardians.map((monster, index) => ({
        ...(dungeon.stages?.[index] || {}),
        id: index === 2 ? 'boss' : `stg_${index + 1}`,
        monsterId: monster.id,
        isBoss: index === 2,
        ...(needsSubstages ? {
            substageIndex: substageOrder[index],
            substageName: DUNGEON_SUBSTAGE_NAMES[substageOrder[index]],
        } : { substageIndex: null, substageName: null }),
        statProgression: index === 0 ? 1 : 1.08,
    }));
};

const DUNGEONS = Object.freeze(RAW_DUNGEONS.map(raw => {
    const townId = raw.townId || TOWN_BY_ID.get(raw.townId)?.id || null;
    const town = TOWN_BY_ID.get(townId);
    const cap = town ? dungeonTownRealmCap(town) : 65;
    return {
        ...raw,
        townId,
        realmMin: town ? Math.max(0, Number(town.realmMin) || 0) : raw.realmMin,
        expReward: Math.max(30, Math.round(raw.expReward * 0.1)),
        stamina: Math.max(15, raw.stamina || 15),
        stages: town ? normalizeDungeonStages(raw, town, cap) : raw.stages,
    };
}));
const DUNGEON_BY_TOWN_ID = new Map(DUNGEONS.map(d => [d.townId, d]));
let DUNGEON_BY_ID = new Map(DUNGEONS.map(d => [d.id, d]));

// Công thức Chế Phù (Bàn Chế Phù)
const CRAFT_TALISMAN_RECIPES_BASE = Object.freeze([
    {
        "id": "che_phu_binh_an",
        "name": "Bình An Phù",
        "talismanId": "phu_binh_an",
        "yieldQty": 2,
        "materials": {
            "mat_linh_thao": 1,
            "mat_yeu_dan": 1
        },
        "stones": 1000,
        "desc": "Tạo hộ thuẫn bình an 25% HP."
    },
    {
        "id": "che_phu_an_than",
        "name": "Ẩn Thân Phù",
        "talismanId": "phu_an_than",
        "yieldQty": 1,
        "materials": {
            "mat_linh_thao": 2,
            "mat_yeu_dan": 1
        },
        "stones": 1000,
        "desc": "Độn quang ẩn nấp đào thoát."
    },
    {
        "id": "che_phu_hoa_cau",
        "name": "Hỏa Cầu Phù",
        "talismanId": "phu_hoa_cau",
        "yieldQty": 2,
        "materials": {
            "mat_linh_thao": 1,
            "mat_van_thiet": 1
        },
        "stones": 1000,
        "desc": "Phóng hỏa cầu thiêu đốt đối thủ."
    },
    {
        "id": "che_phu_lien_chau_loi",
        "name": "Liên Châu Lôi Phù",
        "talismanId": "phu_lien_chau_loi",
        "yieldQty": 1,
        "materials": {
            "mat_van_thiet": 2,
            "mat_yeu_dan": 2
        },
        "stones": 2000,
        "needLearn": true,
        "desc": "Chuỗi sấm sét oanh tạc."
    },
    {
        "id": "che_phu_dinh_than",
        "name": "Định Thần Phù",
        "talismanId": "phu_dinh_than",
        "yieldQty": 1,
        "materials": {
            "mat_linh_thao": 3,
            "mat_yeu_dan": 2
        },
        "stones": 2500,
        "needLearn": true,
        "desc": "Định thân đối thủ 2 giây."
    },
    {
        "id": "che_phu_tam_muoi",
        "name": "Tam Muội Chân Hỏa Phù",
        "talismanId": "phu_tam_muoi",
        "yieldQty": 1,
        "materials": {
            "mat_linh_thao": 4,
            "mat_yeu_dan": 3,
            "mat_huyet_tinh": 1
        },
        "stones": 6000,
        "needLearn": true,
        "desc": "Chân hỏa bộc phát uy lực."
    },
    {
        "id": "che_phu_ngu_loi_chinh",
        "name": "Ngũ Lôi Chánh Pháp Phù",
        "talismanId": "phu_ngu_loi_chinh",
        "yieldQty": 1,
        "materials": {
            "mat_van_thiet": 6,
            "mat_yeu_dan": 5,
            "mat_huyet_tinh": 2
        },
        "stones": 15000,
        "needLearn": true,
        "desc": "Ngũ lôi chính pháp oanh kích kinh thiên."
    },
    {
        "id": "che_phu_tram_tien_dao",
        "name": "Trảm Tiên Đạo Phù",
        "talismanId": "phu_tram_tien_dao",
        "yieldQty": 1,
        "materials": {
            "mat_van_thiet": 10,
            "mat_yeu_dan": 8,
            "mat_huyet_tinh": 4,
            "mat_long_lan": 1
        },
        "stones": 50000,
        "needLearn": true,
        "desc": "Trảm tiên sát thánh chấn nhiếp bát hoang."
    }
]);
const TALISMAN_CRAFT_PROGRESS = Object.freeze({ pham: 0, hoang: 10, huyen: 30, dia: 60, thien: 100, tien: 150 });
const TALISMAN_CRAFT_MATERIALS = Object.freeze({
    pham: { mat_linh_thao: 1, mat_van_thiet: 1, mat_yeu_dan: 1 },
    hoang: { mat_linh_thao: 2, mat_van_thiet: 2, mat_yeu_dan: 2 },
    huyen: { mat_linh_thao: 3, mat_van_thiet: 3, mat_yeu_dan: 2, mat_cuu_khuc_linh_sam: 1, mat_tuyet_linh_thuy: 1 },
    dia: { mat_linh_thao: 5, mat_van_thiet: 5, mat_yeu_dan: 4, mat_huyet_tinh: 1, mat_thien_loi_truc: 1 },
    thien: { mat_linh_thao: 8, mat_van_thiet: 8, mat_yeu_dan: 6, mat_huyet_tinh: 3, mat_long_lan: 1, mat_tien_thach: 1 },
    tien: { mat_linh_thao: 12, mat_van_thiet: 12, mat_yeu_dan: 10, mat_huyet_tinh: 5, mat_long_lan: 3, mat_tien_thach: 2, mat_phap_tac_tinh: 1 },
});
const TALISMAN_CRAFT_STONES = Object.freeze({ pham: 1000, hoang: 2500, huyen: 6000, dia: 15000, thien: 50000, tien: 150000 });
const TALISMAN_CRAFT_YIELD = Object.freeze({ pham: 3, hoang: 2, huyen: 2, dia: 1, thien: 1, tien: 1 });
const CRAFT_TALISMAN_BASE_BY_OUTPUT = new Map(CRAFT_TALISMAN_RECIPES_BASE.map(r => [r.talismanId, r]));
const CRAFT_TALISMAN_RECIPES = Object.freeze(CONSUMABLES
    .filter(item => item.talisman && item.id.startsWith('phu_') && TALISMAN_CRAFT_MATERIALS[item.tier])
    .map(item => {
        const tier = item.tier;
        const base = CRAFT_TALISMAN_BASE_BY_OUTPUT.get(item.id) || {};
        return {
            ...base,
            id: base.id || `che_${item.id}`,
            name: base.name || item.name,
            talismanId: item.id,
            tier,
            minRealm: TIER[tier]?.realm || 0,
            reqCraftXp: TALISMAN_CRAFT_PROGRESS[tier] || 0,
            yieldQty: base.yieldQty || TALISMAN_CRAFT_YIELD[tier],
            materials: base.materials || TALISMAN_CRAFT_MATERIALS[tier],
            stones: base.stones || TALISMAN_CRAFT_STONES[tier],
            desc: base.desc || item.desc || `Phù phương ${TIER[tier]?.name || tier} phẩm.`,
        };
    }));
const CRAFT_TALISMAN_RECIPE_BY_ID = new Map(CRAFT_TALISMAN_RECIPES.map(r => [r.id, r]));

const SKILL_BY_ID = new Map(SKILLS.map(s => [s.id, s]));
const EQUIP_BY_ID = new Map(EQUIPMENT.map(e => [e.id, e]));
const CONSUMABLE_BY_ID = new Map(CONSUMABLES.map(c => [c.id, c]));
const MONSTER_BY_ID = new Map(MONSTERS.map(m => [m.id, m]));
const MATERIAL_BY_ID = new Map(MATERIALS.map(m => [m.id, m]));

// Gán biên giới cảnh giới cho các trang bị gốc chưa có srcMinRealm / srcMaxRealm
for (const item of EQUIPMENT) {
    if (item.srcMinRealm === undefined) {
        if (item.tier === 'pham') item.srcMinRealm = 0;
        else if (item.tier === 'hoang') item.srcMinRealm = 2;
        else if (item.tier === 'huyen') item.srcMinRealm = 3;
        else if (item.tier === 'dia') item.srcMinRealm = 5;
        else if (item.tier === 'thien') item.srcMinRealm = 7;
        else if (item.tier === 'tien') item.srcMinRealm = 11;
        else item.srcMinRealm = 0;
    }
    if (item.srcMaxRealm === undefined) {
        if (item.tier === 'pham') item.srcMaxRealm = 1;
        else if (item.tier === 'hoang') item.srcMaxRealm = 3;
        else if (item.tier === 'huyen') item.srcMaxRealm = 4;
        else if (item.tier === 'dia') item.srcMaxRealm = 6;
        else if (item.tier === 'thien') item.srcMaxRealm = 10;
        else if (item.tier === 'tien') item.srcMaxRealm = 65;
        else item.srcMaxRealm = 65;
    }
}

const MAP_TIER_ALLOWLIST = {
    map_1: ['pham'],
    map_2: ['pham', 'hoang'],
    map_3: ['hoang'],
    map_4: ['huyen'],
    map_5: ['dia'],
    map_6: ['dia'],
    map_7: ['thien'],
    map_8: ['thien'],
    map_9: ['tien'],
};

// PA 1: Bí Cảnh Thí Luyện Trang Bị theo từng Bản Đồ (19 Maps)
// Tách biệt hoàn toàn với Cổ Động Đan Dược: Chuyên thí luyện rèn luyện và săn trang bị theo cảnh giới bản đồ
const EQUIP_DUNGEONS = Object.freeze(MAPS.map(map => {
    const townIds = map.townIds || [];
    const pool = [];
    for (const tId of townIds) {
        const t = TOWN_BY_ID.get(tId);
        if (t?.monsterPool) {
            for (const mId of t.monsterPool) {
                const m = MONSTER_BY_ID.get(mId);
                if (m && !m.worldBoss && !pool.some(x => x.id === m.id)) {
                    pool.push(m);
                }
            }
        }
    }
    pool.sort((a, b) => (a.realm || 0) - (b.realm || 0) || (a.atk || 0) - (b.atk || 0));

    let m1 = pool[0] || MONSTERS[0];
    let m2 = pool[Math.floor(pool.length / 2)] || pool[0] || MONSTERS[0];
    if (m2.id === m1.id && pool.length > 1) m2 = pool[1];
    let m3 = pool[pool.length - 1] || pool[0] || MONSTERS[0];
    if (m3.id === m2.id && pool.length > 2) m3 = pool[pool.length - 1];

    const allowedTiers = MAP_TIER_ALLOWLIST[map.id] || ['tien'];
    const equipPool = EQUIPMENT.filter(eq => {
        if (!allowedTiers.includes(eq.tier)) return false;
        const minR = eq.srcMinRealm ?? (eq.tier === 'pham' ? 0 : (TIER[eq.tier]?.realm ?? 0));
        const maxR = eq.srcMaxRealm ?? (eq.tier === 'pham' ? 1 : (minR + 3));
        return (minR <= map.realmMax && maxR >= map.realmMin);
    }).map(eq => eq.id);

    return {
        id: `thi_luyen_${map.id}`,
        mapId: map.id,
        mapName: map.provinceName || map.name,
        name: `Thí Luyện ${map.provinceName || map.name}`,
        category: 'thi_luyen',
        isEquipTrial: true,
        icon: map.ascensionRequired ? '⚔️' : '🛡️',
        realmMin: map.realmMin,
        realmMax: map.realmMax,
        stamina: 20,
        ascensionRequired: Boolean(map.ascensionRequired),
        desc: `Bí cảnh thí luyện trang bị trấn thủ tại ${map.provinceName || map.name}. Trảm sát 3 ải thủ hộ chắc chắn nhận 1 Trang Bị theo cảnh giới và Mảnh Tàn Đồ!`,
        stages: [
            { id: 'stg_1', name: `Ngoại Vi: ${m1.name}`, monsterId: m1.id, isBoss: false },
            { id: 'stg_2', name: `Thâm Uyên: ${m2.name}`, monsterId: m2.id, isBoss: false },
            { id: 'boss', name: `Thần Tướng: ${m3.name}`, monsterId: m3.id, isBoss: true },
        ],
        equipPool: equipPool.length ? equipPool : ['moc_kiem', 'bo_y', 'dong_boi'],
        stones: 5000 + map.realmMin * 2000,
        expReward: 15000 + map.realmMin * 5000,
    };
}));

const EQUIP_DUNGEON_BY_ID = new Map(EQUIP_DUNGEONS.map(d => [d.id, d]));
const EQUIP_DUNGEON_BY_MAP_ID = new Map(EQUIP_DUNGEONS.map(d => [d.mapId, d]));

// Cập nhật DUNGEON_BY_ID bao gồm cả 65 Cổ Động và 19 Thí Luyện Trang Bị
DUNGEON_BY_ID = new Map([
    ...DUNGEONS.map(d => [d.id, d]),
    ...EQUIP_DUNGEONS.map(d => [d.id, d]),
]);

// Dị hỏa chỉ có bốn phẩm: Tím, Vàng, Cam, Đỏ. Không có phẩm Thường/Phổ thông.
// Nguồn rơi gắn với đúng yêu thú/boss để Thư Các và hệ thống săn dùng cùng dữ liệu.
const FIRE_QUALITY = Object.freeze({ tim: { rank: 1, name: 'Tím' }, vang: { rank: 2, name: 'Vàng' }, cam: { rank: 3, name: 'Cam' }, do: { rank: 4, name: 'Đỏ' } });
const FIRE_SPECS = [
    ['di_hoa_phuc_linh_tu_diem', 'Phục Linh Tử Diễm', 'tim', 'ty_hoa_ho', 0],
    ['di_hoa_huyet_ma_tam_hoa', 'Huyết Ma Tâm Hỏa', 'tim', 'huyet_nhuc_khoi_loi', 0],
    ['di_hoa_u_lam_tinh_hoa', 'U Lam Tinh Hỏa', 'tim', 'u_lam_hoa_diep', 0],
    ['di_hoa_hoa_lien_dia_tam', 'Hỏa Liên Địa Tâm', 'tim', 'hoa_lien_xa_vuong', 0],
    ['di_hoa_chu_tuoc_ly_hoa', 'Chu Tước Ly Hỏa', 'tim', 'chu_tuoc_hoa_dieu', 0],
    ['di_hoa_tu_la_huyet_viem', 'Tu La Huyết Viêm', 'tim', 'tu_la_huyet_long', 0],
    ['di_hoa_huyet_sat_ky_lan', 'Huyết Sát Kỳ Lân Hỏa', 'tim', 'huyet_ky_lan', 0],
    ['di_hoa_cuu_vi_ho_hoa', 'Cửu Vĩ Hồ Hỏa', 'tim', 'cuu_vi_ho', 0],
    ['di_hoa_ly_hoa_giao_viem', 'Ly Hỏa Giao Viêm', 'tim', 'li_hoa_giao', 0],
    ['di_hoa_phuong_hoang_niet_ban', 'Phượng Hoàng Niết Bàn Hỏa', 'tim', 'chu_tuoc_anh', 0],
    ['di_hoa_bat_diet_thien_hoa', 'Bất Diệt Thiên Hỏa', 'vang', 'ty_thien_hoa_vuong', 1, ['thai_co_loi_long']],
    ['di_hoa_thai_co_than_lo', 'Thái Cổ Thần Lô Hỏa', 'vang', 'thai_co_to_than', 1],
    ['di_hoa_chan_tien_ma_viem', 'Chân Tiên Ma Viêm', 'vang', 'chan_tien_ma_khi', 1],
    ['di_hoa_dao_de_tinh_hoa', 'Đạo Đế Tinh Hỏa', 'vang', 'tiengioi_boss_40', 1],
    ['di_hoa_vo_cuc_tinh_diem', 'Vô Cực Tinh Diễm', 'vang', 'vo_cuc_tinh_thu', 1],
    ['di_hoa_hon_nguyen_hu_viem', 'Hỗn Nguyên Hư Viêm', 'cam', 'hon_don_to_long_vi_dai', 2],
    ['di_hoa_hu_vo_than_diem', 'Hư Vô Thần Diễm', 'cam', 'tiengioi_boss_55', 2],
    ['di_hoa_hu_khong_ma_hoa', 'Hư Không Ma Hỏa', 'cam', 'hu_khong_thien_ma_ton', 2, ['huyen_vu_anh']],
    ['di_hoa_hong_mong_khai_thien', 'Hồng Mông Khai Thiên Dị Hỏa', 'do', 'thai_so_tien_de_hon', 3, ['dai_la_hac_nguu']],
    ['di_hoa_van_gioi_chi_tam', 'Vạn Giới Chí Cao Tâm Hỏa', 'do', 'tiengioi_boss_65', 3],
];
const FIRE_DROP_RATES = Object.freeze({ tim: 0.25, vang: 0.10, cam: 0.04, do: 0.01 });
const CRAFT_FIRES = Object.freeze(FIRE_SPECS.map(([id, name, quality, sourceMonsterId, bonusStep, extraSourceMonsterIds = []]) => {
    const grade = FIRE_QUALITY[quality];
    const sourceMonsterIds = [...new Set([sourceMonsterId, ...extraSourceMonsterIds])];
    const sources = sourceMonsterIds.map(sourceId => {
        const source = MONSTER_BY_ID.get(sourceId);
        if (!source) throw new Error(`Không tìm thấy quái nguồn Dị Hỏa ${id}: ${sourceId}`);
        return source;
    });
    return Object.freeze({
        id, name, icon: '🔥', kind: 'fire', fireQuality: quality, fireQualityName: grade.name,
        fireRank: grade.rank, qualityName: grade.name, qualityRank: grade.rank,
        realmMin: Math.min(...sources.map(source => Number(source.realm) || 0)),
        sourceMonsterId, sourceMonsterIds: Object.freeze(sourceMonsterIds), dropChance: FIRE_DROP_RATES[quality],
        successBonus: grade.rank * 0.02,
        desc: `Dị Hỏa phẩm ${grade.name}, dùng cho Thợ Rèn hoặc Đan Sư. Rơi từ ${sources.map(source => source.name).join(' hoặc ')}; phẩm càng cao càng hiếm.`,
    });
}));
const FIRE_BY_ID = new Map(CRAFT_FIRES.map(fire => [fire.id, fire]));

// Hồ lô khởi đầu được tặng khi tạo nhân vật; các phẩm cao hơn là chiến lợi phẩm.
const BEAST_GOURDS = Object.freeze([
    { id: 'ho_lo_thu', name: 'Hồ Lô Thu Thú', icon: '🏺', tier: 'pham', tierName: 'Phàm', qualityRank: 0, realmMin: 0, captureBonus: 0, sourceMonsterId: null, dropChance: 0, starter: true, desc: 'Hồ lô nhập môn được tặng khi tạo nhân vật. Tỷ lệ thu phục còn tùy Ngự Thú Sư và chênh lệch cảnh giới.' },
    { id: 'ho_lo_thu_hoang', name: 'Hoàng Văn Ngự Thú Hồ', icon: '🏺', tier: 'hoang', tierName: 'Hoàng', qualityRank: 1, realmMin: 2, captureBonus: 0.04, sourceMonsterId: 'hoa_ho', dropChance: 0.0015, desc: 'Tăng 4 điểm phần trăm tỷ lệ thu phục; rơi từ yêu thú ở Ngã Hoàng Động Thiên.' },
    { id: 'ho_lo_thu_huyen', name: 'Huyền Tâm Phược Yêu Hồ', icon: '⚱️', tier: 'huyen', tierName: 'Huyền', qualityRank: 2, realmMin: 7, captureBonus: 0.08, sourceMonsterId: 'chu_tuoc_hoa_dieu', dropChance: 0.0006, desc: 'Tăng 8 điểm phần trăm tỷ lệ thu phục; rơi từ Chu Tước Thần Điểu.' },
    { id: 'ho_lo_thu_dia', name: 'Địa Mạch Trấn Yêu Hồ', icon: '⚱️', tier: 'dia', tierName: 'Địa', qualityRank: 3, realmMin: 12, captureBonus: 0.12, sourceMonsterId: 'ty_thien_hoa_vuong', dropChance: 0.0002, desc: 'Tăng 12 điểm phần trăm tỷ lệ thu phục; rơi từ Bất Diệt Hỏa Tinh.' },
    { id: 'ho_lo_thu_thien', name: 'Thiên Đạo Ngự Linh Hồ', icon: '🌌', tier: 'thien', tierName: 'Thiên', qualityRank: 4, realmMin: 30, captureBonus: 0.16, sourceMonsterId: 'tiengioi_boss_40', dropChance: 0.00005, desc: 'Tăng 16 điểm phần trăm tỷ lệ thu phục; chỉ có thể rơi từ boss Tiên Giới cấp cao.' },
    { id: 'ho_lo_thu_tien', name: 'Hỗn Độn Tiên Linh Hồ', icon: '🌌', tier: 'tien', tierName: 'Tiên', qualityRank: 5, realmMin: 55, captureBonus: 0.20, sourceMonsterId: 'tiengioi_boss_65', dropChance: 0.000005, desc: 'Tăng 20 điểm phần trăm tỷ lệ thu phục; chiến lợi phẩm siêu hiếm của boss Vạn Giới Chi Chủ.' },
].map(gourd => Object.freeze(gourd)));
const BEAST_GOURD_BY_ID = new Map(BEAST_GOURDS.map(gourd => [gourd.id, gourd]));
const CRAFT_EQUIP_RECIPE_BY_ID = new Map(CRAFT_EQUIP_RECIPES.map(r => [r.id, r]));
const CRAFT_POTION_RECIPE_BY_ID = new Map(CRAFT_POTION_RECIPES.map(r => [r.id, r]));


// ---- 50 KHU VỰC FARM TÔNG MÔN ----
const SECT_TERRITORIES = [
    {
        "id": "territory_1",
        "name": "Thanh Trúc Phong Dược Viên",
        "mapId": "map_1",
        "resource": "herbs",
        "yieldPerHour": 15,
        "hasMonsters": true,
        "monsterRealm": 1,
        "monsterName": "Lục Diệp Thanh Mãng",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Lục Diệp Thanh Mãng]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_2",
        "name": "Lạc Nhạn Hạp Quặng Sắt",
        "mapId": "map_1",
        "resource": "ores",
        "yieldPerHour": 8,
        "hasMonsters": false,
        "monsterRealm": 0,
        "monsterName": "Không có yêu thú",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Mỏ tài nguyên tự nhiên [ores], thu hoạch an toàn mà không có yêu thú quấy nhiễu."
    },
    {
        "id": "territory_3",
        "name": "Triệu Quốc Ngoại Vi Linh Điền",
        "mapId": "map_1",
        "resource": "stones",
        "yieldPerHour": 120,
        "hasMonsters": false,
        "monsterRealm": 0,
        "monsterName": "Không có yêu thú",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Mỏ tài nguyên tự nhiên [stones], thu hoạch an toàn mà không có yêu thú quấy nhiễu."
    },
    {
        "id": "territory_4",
        "name": "Hoàng Phong Cốc Mật Thất",
        "mapId": "map_1",
        "resource": "pills",
        "yieldPerHour": 6,
        "hasMonsters": true,
        "monsterRealm": 1,
        "monsterName": "Phong Bộc Hùng Yêu",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Phong Bộc Hùng Yêu]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_5",
        "name": "Bạch Thạch Huyết Mạch",
        "mapId": "map_1",
        "resource": "mats",
        "yieldPerHour": 12,
        "hasMonsters": true,
        "monsterRealm": 1,
        "monsterName": "Bạch Thạch Cự Viên",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực khoáng mạch ngưng kết tinh huyết yêu thú, sản sinh Yêu Đan và Huyết Tinh."
    },
    {
        "id": "territory_6",
        "name": "Hàn Thủy Đàm Dược Cảnh",
        "mapId": "map_1",
        "resource": "herbs",
        "yieldPerHour": 15,
        "hasMonsters": true,
        "monsterRealm": 1,
        "monsterName": "Hàn Băng Thủy Điêu",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Hàn Băng Thủy Điêu]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_7",
        "name": "Thiên Nam Linh Mạch Mỏ Vàng",
        "mapId": "map_2",
        "resource": "stones",
        "yieldPerHour": 280,
        "hasMonsters": true,
        "monsterRealm": 2,
        "monsterName": "Kim Tinh Thú",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Kim Tinh Thú]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_8",
        "name": "Lý Châu Dược Thảo Viên",
        "mapId": "map_2",
        "resource": "herbs",
        "yieldPerHour": 20,
        "hasMonsters": false,
        "monsterRealm": 2,
        "monsterName": "Không có yêu thú",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Mỏ tài nguyên tự nhiên [herbs], thu hoạch an toàn mà không có yêu thú quấy nhiễu."
    },
    {
        "id": "territory_9",
        "name": "Nga Hoàng Thiết Quáng Trại",
        "mapId": "map_2",
        "resource": "ores",
        "yieldPerHour": 16,
        "hasMonsters": true,
        "monsterRealm": 2,
        "monsterName": "Thiết Giáp Tê Ngưu",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Thiết Giáp Tê Ngưu]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_10",
        "name": "Vân Mộng Sơn Linh Điền",
        "mapId": "map_2",
        "resource": "herbs",
        "yieldPerHour": 20,
        "hasMonsters": true,
        "monsterRealm": 2,
        "monsterName": "Vân Mộng Linh Lộc",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Linh điền sương mây che phủ ngàn năm, nơi sinh trưởng của các loài Linh Thảo quý hiếm."
    },
    {
        "id": "territory_11",
        "name": "Xích Viêm Mỏ Đồng",
        "mapId": "map_2",
        "resource": "ores",
        "yieldPerHour": 16,
        "hasMonsters": true,
        "monsterRealm": 2,
        "monsterName": "Xích Hỏa Độc Hạt",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Xích Hỏa Độc Hạt]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_12",
        "name": "Thanh Hư Động Thiên Tuyền",
        "mapId": "map_2",
        "resource": "pills",
        "yieldPerHour": 8,
        "hasMonsters": true,
        "monsterRealm": 2,
        "monsterName": "Thanh Linh Hồ Ly",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Thanh Linh Hồ Ly]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_13",
        "name": "Lạc Dương Phù Sơn Khoáng Địa",
        "mapId": "map_3",
        "resource": "ores",
        "yieldPerHour": 20,
        "hasMonsters": true,
        "monsterRealm": 3,
        "monsterName": "Phù Không Nham Thạch Thú",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Phù Không Nham Thạch Thú]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_14",
        "name": "Thái Nhậm Bí Cảnh Dược Các",
        "mapId": "map_3",
        "resource": "herbs",
        "yieldPerHour": 25,
        "hasMonsters": true,
        "monsterRealm": 3,
        "monsterName": "Thực Nhân Ma Đằng",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Thực Nhân Ma Đằng]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_15",
        "name": "Hắc Thủy Hàn Đàm",
        "mapId": "map_3",
        "resource": "stones",
        "yieldPerHour": 280,
        "hasMonsters": false,
        "monsterRealm": 2,
        "monsterName": "Không có yêu thú",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Mỏ tài nguyên tự nhiên [stones], thu hoạch an toàn mà không có yêu thú quấy nhiễu."
    },
    {
        "id": "territory_16",
        "name": "U Cốc Thiết Quáng",
        "mapId": "map_3",
        "resource": "ores",
        "yieldPerHour": 18,
        "hasMonsters": true,
        "monsterRealm": 3,
        "monsterName": "U Minh Thiết Giáp Tê",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Mỏ Huyền Thiên Vẫn Thiết ẩn sâu dưới đáy vực U Cốc, giàu khoáng thạch đúc kiếm."
    },
    {
        "id": "territory_17",
        "name": "Vạn Trượng Thâm Uyên Mỏ Tinh",
        "mapId": "map_3",
        "resource": "ores",
        "yieldPerHour": 20,
        "hasMonsters": true,
        "monsterRealm": 3,
        "monsterName": "Thâm Uyên Ma Chu",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Thâm Uyên Ma Chu]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_18",
        "name": "Linh Tê Thảo Nguyên",
        "mapId": "map_3",
        "resource": "herbs",
        "yieldPerHour": 25,
        "hasMonsters": false,
        "monsterRealm": 3,
        "monsterName": "Không có yêu thú",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Mỏ tài nguyên tự nhiên [herbs], thu hoạch an toàn mà không có yêu thú quấy nhiễu."
    },
    {
        "id": "territory_19",
        "name": "Loạn Tinh Tinh Thạch Hải Vực",
        "mapId": "map_4",
        "resource": "stones",
        "yieldPerHour": 440,
        "hasMonsters": true,
        "monsterRealm": 4,
        "monsterName": "Bát Trảo Cự Mặc Hải Yêu",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Bát Trảo Cự Mặc Hải Yêu]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_20",
        "name": "Thiên Cơ Thần Kim Mỏ Quặng",
        "mapId": "map_4",
        "resource": "ores",
        "yieldPerHour": 24,
        "hasMonsters": true,
        "monsterRealm": 4,
        "monsterName": "Cơ Quan Thần Khuyển",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Cơ Quan Thần Khuyển]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_21",
        "name": "Lục Ma Hải Cực Âm Hoang Cốc",
        "mapId": "map_4",
        "resource": "pills",
        "yieldPerHour": 12,
        "hasMonsters": true,
        "monsterRealm": 4,
        "monsterName": "Huyết Lân Ma Giao",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Huyết Lân Ma Giao]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_22",
        "name": "Hải Thượng Tinh Mạch",
        "mapId": "map_4",
        "resource": "stones",
        "yieldPerHour": 350,
        "hasMonsters": true,
        "monsterRealm": 4,
        "monsterName": "Hải Thú Ba Đào",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Mỏ Linh Thạch thượng phẩm ngưng tụ dưới đáy biển sâu Cửu Châu."
    },
    {
        "id": "territory_23",
        "name": "San Hô Đảo Linh Ngư Đàm",
        "mapId": "map_4",
        "resource": "stones",
        "yieldPerHour": 440,
        "hasMonsters": false,
        "monsterRealm": 4,
        "monsterName": "Không có yêu thú",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Mỏ tài nguyên tự nhiên [stones], thu hoạch an toàn mà không có yêu thú quấy nhiễu."
    },
    {
        "id": "territory_24",
        "name": "Thần Mộc Lâm Dược Cực",
        "mapId": "map_4",
        "resource": "herbs",
        "yieldPerHour": 30,
        "hasMonsters": true,
        "monsterRealm": 4,
        "monsterName": "Thụ Tinh Cổ Thụ Yêu",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Thụ Tinh Cổ Thụ Yêu]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_25",
        "name": "Âm La Ma Quáng Mỏ Huyết Thạch",
        "mapId": "map_5",
        "resource": "ores",
        "yieldPerHour": 28,
        "hasMonsters": true,
        "monsterRealm": 5,
        "monsterName": "Huyết Sát Hồn Thú",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Huyết Sát Hồn Thú]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_26",
        "name": "Tiên Di Châu Bách Thảo Lĩnh",
        "mapId": "map_5",
        "resource": "herbs",
        "yieldPerHour": 35,
        "hasMonsters": true,
        "monsterRealm": 5,
        "monsterName": "Cửu Diệp Linh Chi Thú",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Cửu Diệp Linh Chi Thú]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_27",
        "name": "Bắc Cầu Lô Hàn Băng Tuyền",
        "mapId": "map_5",
        "resource": "stones",
        "yieldPerHour": 520,
        "hasMonsters": true,
        "monsterRealm": 5,
        "monsterName": "Bắc Cực Băng Hùng",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Bắc Cực Băng Hùng]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_28",
        "name": "Hàn Phong Đan Quật",
        "mapId": "map_5",
        "resource": "pills",
        "yieldPerHour": 15,
        "hasMonsters": true,
        "monsterRealm": 5,
        "monsterName": "Hàn Băng Tuyết Dực Điêu",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Cổ quật lưu truyền đan dược thượng cổ từ thời phong ma đại chiến."
    },
    {
        "id": "territory_29",
        "name": "Vạn Độc Hắc Thủy Đàm",
        "mapId": "map_5",
        "resource": "pills",
        "yieldPerHour": 14,
        "hasMonsters": true,
        "monsterRealm": 5,
        "monsterName": "Ngũ Độc Ma Cáp",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Ngũ Độc Ma Cáp]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_30",
        "name": "Cổ Ma Động Khoáng Địa",
        "mapId": "map_5",
        "resource": "ores",
        "yieldPerHour": 28,
        "hasMonsters": true,
        "monsterRealm": 5,
        "monsterName": "Thao Thiết Ma Tôn Tàn Hồn",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Thao Thiết Ma Tôn Tàn Hồn]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_31",
        "name": "Chu Tước Hỏa Diệm Sơn Khâu",
        "mapId": "map_6",
        "resource": "ores",
        "yieldPerHour": 32,
        "hasMonsters": true,
        "monsterRealm": 6,
        "monsterName": "Hỏa Kỳ Lân Yêu Thú",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Hỏa Kỳ Lân Yêu Thú]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_32",
        "name": "Vạn Yêu Thánh Cảnh Dược Sơn",
        "mapId": "map_6",
        "resource": "herbs",
        "yieldPerHour": 40,
        "hasMonsters": true,
        "monsterRealm": 6,
        "monsterName": "Bạch Hổ Thần Tướng",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Bạch Hổ Thần Tướng]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_33",
        "name": "Liệt Diễm Thao Trường Mỏ Ngọc",
        "mapId": "map_6",
        "resource": "stones",
        "yieldPerHour": 600,
        "hasMonsters": true,
        "monsterRealm": 6,
        "monsterName": "Viêm Ma Cự Nhân",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Viêm Ma Cự Nhân]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_34",
        "name": "Tử Sa Long Sào",
        "mapId": "map_6",
        "resource": "mats",
        "yieldPerHour": 20,
        "hasMonsters": true,
        "monsterRealm": 6,
        "monsterName": "Tử Sa Xích Hỏa Long",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Tổ rồng cổ xưa vương lại Chân Long Nghịch Lân và Cổ Thần Huyết Tinh tuyệt phẩm."
    },
    {
        "id": "territory_35",
        "name": "Đoạt Phách Động Đan Quật",
        "mapId": "map_6",
        "resource": "pills",
        "yieldPerHour": 16,
        "hasMonsters": true,
        "monsterRealm": 6,
        "monsterName": "Cửu Đầu Ma Xà",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Cửu Đầu Ma Xà]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_36",
        "name": "Thần Mộc Thảo Thính",
        "mapId": "map_6",
        "resource": "herbs",
        "yieldPerHour": 40,
        "hasMonsters": false,
        "monsterRealm": 6,
        "monsterName": "Không có yêu thú",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Mỏ tài nguyên tự nhiên [herbs], thu hoạch an toàn mà không có yêu thú quấy nhiễu."
    },
    {
        "id": "territory_37",
        "name": "Kiếm Khí Trường Thành Tiên Thiết Trại",
        "mapId": "map_7",
        "resource": "ores",
        "yieldPerHour": 40,
        "hasMonsters": true,
        "monsterRealm": 8,
        "monsterName": "Huyền Kiếm Kiếm Linh",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Huyền Kiếm Kiếm Linh]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_38",
        "name": "Đông Linh Tinh Huyền Ngọc Quáng",
        "mapId": "map_7",
        "resource": "stones",
        "yieldPerHour": 760,
        "hasMonsters": true,
        "monsterRealm": 8,
        "monsterName": "Đông Hải Linh Giao",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Đông Hải Linh Giao]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_39",
        "name": "Man Hoang Dược Hải Cổ Thụ",
        "mapId": "map_7",
        "resource": "herbs",
        "yieldPerHour": 50,
        "hasMonsters": true,
        "monsterRealm": 8,
        "monsterName": "Thái Cổ Hung Thú Đào Ngột",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Thái Cổ Hung Thú Đào Ngột]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_40",
        "name": "Hư Vô Thần Mạch",
        "mapId": "map_7",
        "resource": "stones",
        "yieldPerHour": 800,
        "hasMonsters": true,
        "monsterRealm": 8,
        "monsterName": "Hư Không Thần Thú",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Mỏ Linh Thạch cực phẩm Thái Hư cuồn cuộn không dứt."
    },
    {
        "id": "territory_41",
        "name": "Cửu Chuyển Đan Đỉnh Quật",
        "mapId": "map_7",
        "resource": "pills",
        "yieldPerHour": 20,
        "hasMonsters": true,
        "monsterRealm": 8,
        "monsterName": "Hóa Đan Ma Viên",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Hóa Đan Ma Viên]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_42",
        "name": "Chấn Thiên Thạch Quán",
        "mapId": "map_7",
        "resource": "ores",
        "yieldPerHour": 40,
        "hasMonsters": true,
        "monsterRealm": 8,
        "monsterName": "Thạch Cự Thần Nhân",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Thạch Cự Thần Nhân]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_43",
        "name": "Tiên Giới Cực Phẩm Linh Mạch",
        "mapId": "map_8",
        "resource": "stones",
        "yieldPerHour": 1000,
        "hasMonsters": true,
        "monsterRealm": 11,
        "monsterName": "Thần Thú Thanh Long Hoàng",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Thần Thú Thanh Long Hoàng]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_44",
        "name": "Cửu Thiên Dao Trì Dược Uyển",
        "mapId": "map_8",
        "resource": "herbs",
        "yieldPerHour": 65,
        "hasMonsters": true,
        "monsterRealm": 11,
        "monsterName": "Thiên Tiên Dực Điểu",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Thiên Tiên Dực Điểu]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_45",
        "name": "Thái Hư Hỗn Độn Tiên Kim Mỏ",
        "mapId": "map_8",
        "resource": "ores",
        "yieldPerHour": 56,
        "hasMonsters": true,
        "monsterRealm": 12,
        "monsterName": "Hỗn Độn Thao Thiết Hoàng",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Hỗn Độn Thao Thiết Hoàng]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_46",
        "name": "Hư Không Tiên Uyển",
        "mapId": "map_8",
        "resource": "herbs",
        "yieldPerHour": 60,
        "hasMonsters": true,
        "monsterRealm": 9,
        "monsterName": "Hư Không Tiên Hạc",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Tiên uyển phiêu bạt trong hư không sản sinh vô vàn linh thảo ngàn năm."
    },
    {
        "id": "territory_47",
        "name": "Bất Tử Tiên Đan Các",
        "mapId": "map_8",
        "resource": "pills",
        "yieldPerHour": 28,
        "hasMonsters": true,
        "monsterRealm": 12,
        "monsterName": "Thái Thượng Đan Linh",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Thái Thượng Đan Linh]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_48",
        "name": "Vạn Kiếp Thần Sơn Quáng Lĩnh",
        "mapId": "map_8",
        "resource": "ores",
        "yieldPerHour": 52,
        "hasMonsters": true,
        "monsterRealm": 11,
        "monsterName": "Vạn Kiếp Thần Viên",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Vạn Kiếp Thần Viên]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_49",
        "name": "Thiên Mệnh Thần Cảnh Dược Cốc",
        "mapId": "map_8",
        "resource": "herbs",
        "yieldPerHour": 65,
        "hasMonsters": true,
        "monsterRealm": 11,
        "monsterName": "Thiên Mệnh Phượng Hoàng",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Thiên Mệnh Phượng Hoàng]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    },
    {
        "id": "territory_50",
        "name": "Hồng Mông Khởi Nguyên Mỏ Tinh",
        "mapId": "map_8",
        "resource": "stones",
        "yieldPerHour": 1080,
        "hasMonsters": true,
        "monsterRealm": 12,
        "monsterName": "Hồng Mông Cự Thú",
        "occupiedBy": null,
        "occupiedAt": 0,
        "desc": "Khu vực giàu tài nguyên canh giữ bởi [Hồng Mông Cự Thú]. Chiếm lĩnh để nhận cống phẩm định kỳ."
    }
];
const SECT_TERRITORY_BY_ID = new Map(SECT_TERRITORIES.map(t => [t.id, t]));

// Chuẩn hóa kỹ năng tại nguồn để mô tả, PvE, quyết đấu và NPC dùng chung cân bằng.
for (const s of SKILLS) {
    // Mở thêm hai bậc trên Truyền thuyết; kỹ năng cảnh giới cao nhất được xếp cao hơn.
    if (s.rarity === 'tt' && s.realm >= 8) s.rarity = 'vang';
    else if (s.rarity === 'tt' && s.realm >= 7) s.rarity = 'cam';
    const rank = SKILL_RULES.rarityRank[s.rarity] || 0;
    const realm = Math.max(0, Math.floor(Number(s.realm) || 0));
    const realmBand = Math.floor(realm / SKILL_RULES.realmStepSize);
    const sourceCooldown = Math.max(SKILL_RULES.baseCooldownSeconds, Number(s.cd) || SKILL_RULES.baseCooldownSeconds);
    const sourceMp = Number(s.mp) || 0;
    const hasSecondaryEffect = Boolean(s.stun || s.bind || s.dot || s.heal || s.shield || s.buff || s.reflect || s.kind === 'escape');
    const baseCooldown = SKILL_RULES.baseCooldownSeconds + rank * SKILL_RULES.cooldownStepSeconds;
    // Kỹ năng hiếm mạnh hơn nhưng khó vận dụng; cảnh giới cao giúp giảm nhẹ hồi chiêu.
    // Độ lệch nhỏ từ hồi chiêu gốc giữ khác biệt giữa các chiêu cùng phẩm chất.
    const individualCooldownOffset = Math.min(3, Math.max(0, Math.floor(Math.log2(sourceCooldown / SKILL_RULES.baseCooldownSeconds))));
    s.cd = Math.min(SKILL_RULES.maxCooldownSeconds,
        Math.max(SKILL_RULES.baseCooldownSeconds,
            baseCooldown + individualCooldownOffset +
            (hasSecondaryEffect ? SKILL_RULES.secondaryEffectCooldownSeconds : 0) +
            (s.big ? SKILL_RULES.bigSkillCooldownSeconds : 0) -
            realmBand * SKILL_RULES.realmCooldownReductionPerStep));
    // Mỗi bậc phẩm chất tốn linh lực hơn; kỹ năng cảnh giới cao có thêm 8% mỗi 10 cảnh.
    const baseMp = Math.max(SKILL_RULES.minSkillMp,
        Math.min(SKILL_RULES.maxBaseSkillMp, sourceMp || SKILL_RULES.minSkillMp));
    const realmMpMultiplier = 1 + Math.min(0.48, realmBand * SKILL_RULES.realmMpCostIncreasePerStep);
    s.mp = Math.ceil(baseMp * (SKILL_RULES.mpCostTierMultiplier ** rank) * realmMpMultiplier);
    s.qualityRank = rank;
    s.realmBand = realmBand;
    // Cảnh giới mở khóa cao hơn tăng nhẹ công lực, vẫn chịu trần sát thương theo phẩm chất.
    const damageCap = SKILL_RULES.damageCap[s.rarity] || 3;
    const realmPowerMultiplier = 1 + Math.min(SKILL_RULES.maxRealmDamageIncrease,
        realmBand * SKILL_RULES.realmDamageIncreasePerStep);
    if (s.power) s.power = Math.min(damageCap, s.power * realmPowerMultiplier);
    if (s.dot) s.dot = Math.min(damageCap, s.dot * realmPowerMultiplier);
    if (s.rarity === 'docban') {
        s.drop = 0.001; // 0,1% mỗi lần đủ điều kiện rơi
        s.stock = 1; // Mỗi ngọc giản đỏ chỉ có một bản toàn server.
    }
    if (s.heal && s.heal > 0.25) s.heal = 0.25;
    if (s.shield && s.shield > 0.3) s.shield = 0.3;
    if (s.stun && s.stun > 1.5) s.stun = 1.5;
}

// Gắn cùng một cấp phẩm chất vào đồ trang bị và nguyên liệu. Chỉ số cụ thể vẫn
// lấy từ từng món trong danh mục; bậc và cảnh giới quyết định mức dùng/tìm kiếm.
for (const item of EQUIPMENT) {
    const tier = TIER[item.tier];
    item.qualityRank = tier?.rank ?? 0;
    item.qualityName = tier?.name || 'Phàm';
    item.realmMin ??= tier?.realm ?? item.srcMinRealm ?? 0;
}
for (const material of MATERIALS) {
    const tier = TIER[material.tier];
    material.qualityRank = tier?.rank ?? 0;
    material.qualityName = tier?.name || 'Phàm';
    material.realmMin ??= material.minRealm ?? tier?.realm ?? 0;
}
for (const consumable of CONSUMABLES) {
    const progressionRealm = Number(consumable.toRealm ?? consumable.realm ?? 0);
    let rank = consumable.tier && TIER[consumable.tier] ? TIER[consumable.tier].rank : qualityRankForRealm(progressionRealm);
    if (!consumable.breakthrough && progressionRealm === 0) {
        const recoveryPower = Math.max(Number(consumable.heal) || 0, Number(consumable.mana) || 0, Number(consumable.shield) || 0);
        if (recoveryPower >= 0.9) rank = 4;
        else if (recoveryPower >= 0.6) rank = 2;
        else if (recoveryPower >= 0.3) rank = 1;
        else if (Number(consumable.price) >= 1_000_000) rank = 5;
        else if (Number(consumable.price) >= 100_000) rank = 4;
        else if (Number(consumable.price) >= 10_000) rank = 3;
        else if (Number(consumable.price) >= 3_000) rank = 2;
    }
    const tier = Object.values(TIER).find(entry => entry.rank === rank) || TIER.pham;
    consumable.qualityRank = rank;
    consumable.qualityName = tier.name;
    consumable.realmMin ??= progressionRealm;
}

// Giới hạn trần EXP đan dược: tối đa 5.000 EXP
for (const c of CONSUMABLES) {
    if (c.exp && c.exp > 5000) c.exp = 5000;
}

module.exports = {
    CULTIVATION_REALM_NAMES,
    FLYING_SWORDS,
    SECT_TERRITORIES,
    SECT_TERRITORY_BY_ID,
    RAREST, MON, HE, DAY_ELEMENTS, NIGHT_ELEMENTS, LINH_CAN, BASE_STATS, REALM_GROWTH, RARITY,
    SKILLS, SKILL_RULES, ITEM_QUALITY_REALM_BANDS, qualityRankForRealm, TIER, EQUIPMENT, CONSUMABLES, MONSTERS, RULES,
    SKILL_BY_ID, EQUIP_BY_ID, CONSUMABLE_BY_ID, MONSTER_BY_ID,
    TOWNS, TOWN_BY_ID, getTravelSec, getTeleportCost, WORLD_NPCS, NPC_BY_ID, DEMON_NPCS,
    MATERIALS, MATERIAL_BY_ID, CRAFT_FIRES, FIRE_BY_ID, FIRE_QUALITY, BEAST_GOURDS, BEAST_GOURD_BY_ID,
    CRAFT_EQUIP_RECIPES, CRAFT_EQUIP_RECIPE_BY_ID,
    CRAFT_POTION_RECIPES, CRAFT_POTION_RECIPE_BY_ID,
    CRAFT_TALISMAN_RECIPES, CRAFT_TALISMAN_RECIPE_BY_ID,
    MAPS, MAP_BY_ID,
    WORLD_PROVINCE_GATES, WORLD_ASCENSION_GATES,
    BLUEPRINTS, BLUEPRINT_BY_MAT_ID, BLUEPRINT_BY_ID,
    BREAKTHROUGH_ITEMS, BREAKTHROUGH_BY_REALM, SUBSTAGE_BREAKTHROUGH_ITEMS, SUBSTAGE_BY_REALM, DUNGEONS, DUNGEON_BY_ID, DUNGEON_BY_TOWN_ID,
    EQUIP_DUNGEONS, EQUIP_DUNGEON_BY_ID, EQUIP_DUNGEON_BY_MAP_ID,
    SECT_SHOP, SECT_SHOP_BY_ID,
};
