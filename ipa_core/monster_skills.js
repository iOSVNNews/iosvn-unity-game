'use strict';

// Every monster fights with five named moves of its own: four regular moves and one ultimate
// (the telegraphed "chiêu lớn"). The kit is derived from the monster's body type, element and id,
// so it is stable for a monster and different between monsters. The moves are presentation only:
// damage keeps following the battle rules, the client plays each move's own effect.

const ELEMENT_WORD = {
    kim: 'Kim Cương', moc: 'Thanh Mộc', thuy: 'Huyền Thủy', hoa: 'Liệt Hỏa', tho: 'Hậu Thổ',
    phong: 'Cuồng Phong', loi: 'Lôi Đình', bang: 'Hàn Băng', ma: 'U Ma', thien: 'Thiên Quang',
};

const ULTIMATE = {
    kim: 'Vạn Nhận Quy Tông', moc: 'Vạn Mộc Triền Sát', thuy: 'Thao Thiên Cự Lãng', hoa: 'Phần Thiên Diệt Thế',
    tho: 'Sơn Băng Địa Liệt', phong: 'Diệt Thế Cuồng Phong', loi: 'Cửu Thiên Lôi Phạt', bang: 'Vạn Lý Băng Phong',
    ma: 'Vạn Ma Phệ Hồn', thien: 'Thiên Đạo Thẩm Phán',
};

// fx is the effect family the client stages (see BattleFx.MonsterMove).
const POOLS = {
    beast: [['claw', 'Liệt Trảo'], ['bite', 'Phệ Cốt Giảo'], ['charge', 'Man Hoang Xung'], ['roar', 'Chấn Thiên Hống'], ['tail', 'Thiết Vĩ Tảo'], ['quake', 'Đạp Địa Chấn']],
    bird: [['gust', 'Cương Phong Dực'], ['talon', 'Thiết Trảo Kích'], ['dive', 'Lược Không Trảm'], ['volley', 'Vạn Vũ Tiễn'], ['roar', 'Phá Vân Khiếu'], ['vortex', 'Toàn Phong Vũ']],
    serpent: [['bite', 'Độc Nha Giảo'], ['coil', 'Triền Thân Tỏa'], ['breath', 'Thổ Tức'], ['tail', 'Bãi Vĩ Kích'], ['spit', 'Độc Tiễn'], ['roar', 'Long Ngâm']],
    bug: [['sting', 'Độc Châm'], ['web', 'Thiên Ty Võng'], ['bite', 'Thực Tủy Giảo'], ['swarm', 'Vạn Trùng Phệ'], ['breath', 'Hủ Thực Vụ'], ['claw', 'Liêm Đao Trảm']],
    fiend: [['slash', 'Đoạn Hồn Trảm'], ['palm', 'Toái Tâm Chưởng'], ['curse', 'Nhiếp Hồn Chú'], ['seal', 'Trấn Ngục Ấn'], ['quake', 'Phá Sơn Kích'], ['coil', 'Tỏa Hồn Liên']],
    dragon: [['breath', 'Long Tức'], ['claw', 'Long Trảo'], ['tail', 'Thần Long Bãi Vĩ'], ['roar', 'Long Ngâm'], ['dive', 'Long Đằng Kích'], ['coil', 'Bàn Long Giảo']],
    spirit: [['bolt', 'Linh Quang Đạn'], ['vortex', 'Toàn Linh Trận'], ['burst', 'Bạo Linh'], ['volley', 'Linh Vũ'], ['curse', 'Nhiếp Phách'], ['seal', 'Tụ Nguyên Trụ']],
};

const FAMILY_IDS = {
    beast: 'bach_cau_bang_hung bach_cot_lang bach_ho_anh bach_loc_linh_thu ban_son_vien chuan_tien_vuong_thu cung_ky cung_ky_hoang_thu cuu_vi_ho dai_la_bach_ho dai_la_hac_nguu de_hon_thu doc_giac_bao doc_phong hon_don_thu huyen_vu_anh huyet_ky_lan huyet_ma_vien kiem_khi_bach_vien ngan_quang_thu phong_lang son_nhac_cu_vien song_dong_thu thac_nguyet_ho thach_hau thai_at_huyen_lan thanh_dong_gia_vien thanh_lang_vuong thao_thiet thao_thiet_tan_hon thien_dao_loi_thu thien_kiep_loi_ngao thiet_giap_cu_te thiet_giap_te tien_canh_12_tinh_thu tien_di_thanh_thu tiengioi_boss_35 tiengioi_boss_40 tiengioi_boss_45 tiengioi_boss_50 tiengioi_boss_55 tiengioi_boss_60 tiengioi_boss_65 tiengioi_daiyeu_31 tiengioi_daiyeu_32 tiengioi_daiyeu_34 tiengioi_daiyeu_35 tiengioi_daiyeu_36 tiengioi_daiyeu_37 tiengioi_daiyeu_39 tiengioi_daiyeu_40 tiengioi_daiyeu_41 tiengioi_daiyeu_42 tiengioi_daiyeu_44 tiengioi_daiyeu_45 tiengioi_daiyeu_46 tiengioi_daiyeu_47 tiengioi_daiyeu_49 tiengioi_daiyeu_50 tiengioi_daiyeu_51 tiengioi_daiyeu_52 tiengioi_daiyeu_54 tiengioi_daiyeu_55 tiengioi_daiyeu_56 tiengioi_daiyeu_57 tiengioi_daiyeu_59 tiengioi_daiyeu_60 tiengioi_daiyeu_61 tiengioi_daiyeu_62 tiengioi_daiyeu_64 tiengioi_daiyeu_65 tiengioi_tieuyeu_31 tiengioi_tieuyeu_35 tiengioi_tieuyeu_36 tiengioi_tieuyeu_37 tiengioi_tieuyeu_38 tiengioi_tieuyeu_42 tiengioi_tieuyeu_43 tiengioi_tieuyeu_44 tiengioi_tieuyeu_45 tiengioi_tieuyeu_49 tiengioi_tieuyeu_50 tiengioi_tieuyeu_51 tiengioi_tieuyeu_52 tiengioi_tieuyeu_56 tiengioi_tieuyeu_57 tiengioi_tieuyeu_58 tiengioi_tieuyeu_59 tiengioi_tieuyeu_63 tiengioi_tieuyeu_64 tiengioi_tieuyeu_65 tinh_khong_cu_thu tram_long_thach_vien tuyet_vien_linh_thu ty_chuot_dat ty_hac_linh_tho ty_khi_lua ty_linh_ho ty_linh_meo ty_loi_thu ty_rua_nuoc ty_soi_con ty_tuyet_vien u_minh_thu vo_cuc_tinh_thu',
    bird: 'bac_han_tien_hac bach_suong_huyen_hac bang_phuong chu_tuoc_anh chu_tuoc_hoa_dieu du_thien_con_bang hoa_ho kim_si_dieu liet_phong_thu linh_te_khong_tuoc loi_bang_thien_tac loi_ung ngu_loi_hac_ung ngu_sac_khong_tuoc sat_luc_huyet_buc thien_nam_co_dieu thien_si_chu_hac tiengioi_tieuyeu_33 tiengioi_tieuyeu_40 tiengioi_tieuyeu_47 tiengioi_tieuyeu_54 tiengioi_tieuyeu_61 tinh_ngan_dieu ty_hac_bang ty_ho_ly_con ty_hoa_ho ty_huyet_van_buc ty_kiem_diep ty_kim_dieu ty_kim_tuy_dieu ty_qua_den ty_thai_co_trung ty_tu_la_dieu ty_tuyet_vu_dieu u_lam_hoa_diep van_co_huyet_phuong',
    bug: 'bach_ngoc_chu hac_ma_nghi kim_thien_ong phe_kim_trung trieu_quoc_ma_chu ty_bo_cap ty_kim_giap_trung ty_loi_trung ty_nhen_to ty_ong_vang ty_ret_lua ty_tho_yeu van_thu',
    dragon: 'bat_hoang_long da_lang dai_la_kim_long dong_linh_co_giao hon_don_to_long hon_don_to_long_vi_dai li_hoa_giao loan_tinh_hai_giao loi_giao mac_giao thai_at_kim_long thai_co_loi_long thanh_long_anh thanh_y_thuy_giao thuy_ky_lan tien_canh_12_huyen_hai_giao tiengioi_boss_31 tiengioi_boss_34 tiengioi_boss_36 tiengioi_boss_39 tiengioi_boss_41 tiengioi_boss_44 tiengioi_boss_46 tiengioi_boss_49 tiengioi_boss_51 tiengioi_boss_54 tiengioi_boss_56 tiengioi_boss_59 tiengioi_boss_61 tiengioi_boss_64 tiengioi_tieuyeu_32 tiengioi_tieuyeu_39 tiengioi_tieuyeu_46 tiengioi_tieuyeu_53 tiengioi_tieuyeu_60 tinh_hai_cuu_dau_giao tu_la_huyet_long ty_loi_long_con',
    fiend: 'am_la_quy_vuong bat_hoang_tien_vuong chan_tien_ma_khi co_ma_hoa_than co_than_do_tu_ve cuu_u_tien_ton hon_don_thuc_tinh hu_khong_thien_ma_ton huyet_hoang_thu huyet_nhuc_khoi_loi lac_phach_chuy_quy man_hoang_dai_yeu nga_hoang_kiem_thu thai_co_to_than thai_so_tien_de_hon thao_thiet_co_than thien_ma tien_canh_12_kiem_linh tien_de_tan_niem tiengioi_boss_32 tiengioi_boss_33 tiengioi_boss_37 tiengioi_boss_38 tiengioi_boss_42 tiengioi_boss_43 tiengioi_boss_47 tiengioi_boss_48 tiengioi_boss_52 tiengioi_boss_53 tiengioi_boss_57 tiengioi_boss_58 tiengioi_boss_62 tiengioi_boss_63 truong_thanh_kiem_linh tu_la_ma_thu ty_hu_anh ty_quy_anh ty_thach_linh ty_tien_quan_ho_ve ty_tien_vuong_chien_linh ty_van_co_oan_hon u_hon u_minh_tien_quan',
    serpent: 'am_cot_ma_xa ban_son_trung bang_tuyet_thiem bat_trao_hoa_thu cuu_muc_thiem hinh_nha_thu hoa_lien_xa_vuong huyen_bang_mang kim_giap_ngac la_hau_co_thu loi_oa_thu thanh_xa thon_hu_kinh thon_thien_mang tiengioi_daiyeu_33 tiengioi_daiyeu_38 tiengioi_daiyeu_43 tiengioi_daiyeu_48 tiengioi_daiyeu_53 tiengioi_daiyeu_58 tiengioi_daiyeu_63 tiengioi_tieuyeu_34 tiengioi_tieuyeu_41 tiengioi_tieuyeu_48 tiengioi_tieuyeu_55 tiengioi_tieuyeu_62 ty_bang_thiem ty_ech_doc ty_hai_sam ty_hon_don_diet_the ty_hon_don_trung ty_huyen_tinh_xa ty_loan_co_hung_thu ty_luon_dien ty_ran_co ty_thach_tinh_xa ty_u_hon_xa',
    spirit: 'ty_dai_la_yeu_tinh ty_hoa_linh ty_hon_nguyen_ma_linh ty_moc_tinh ty_phong_linh ty_thien_hoa_vuong ty_thuy_linh',
};
const FAMILY_BY_ID = new Map();
for (const [family, ids] of Object.entries(FAMILY_IDS)) for (const id of ids.split(' ')) FAMILY_BY_ID.set(id, family);

// Fallback for monsters added later: body type from the icon.
const FAMILY_BY_ICON = [
    ['bird', '🦅🦉🐦🦇🦚🕊🦋🐝🦟🦜🦢🦩🐔🪶'],
    ['dragon', '🐉🐲'],
    ['serpent', '🐍🐊🦎🐛🪱🐋🐙🦑🐟🦈🐸'],
    ['bug', '🕷🦂🐜🪲🦗🐞🪳🕸'],
    ['fiend', '👹👺👻💀☠🧟🧙👑🔱💂🗿🤖⚔🗡🪨👿😈🧛🧞'],
    ['spirit', '🌿💧🌪✨👁🔥❄⚡🌊🌸🍃🌳🌑🌕⭐'],
];

function hash(text) {
    let h = 2166136261;
    for (let i = 0; i < text.length; i += 1) {
        h ^= text.charCodeAt(i);
        h = Math.imul(h, 16777619);
    }
    return h >>> 0;
}

function familyOf(def) {
    const known = FAMILY_BY_ID.get(String(def.id || ''));
    if (known) return known;
    const icon = String(def.icon || '').replace(/\uFE0F/g, '');
    for (const [family, icons] of FAMILY_BY_ICON) {
        for (const candidate of Array.from(icons)) if (icon.includes(candidate)) return family;
    }
    return 'beast';
}

const cache = new Map();

/** The five moves of a monster: [{ i, name, fx, v, big }] — index 4 is the ultimate. */
function monsterSkillKit(def) {
    const key = String(def?.id || '');
    if (cache.has(key)) return cache.get(key);
    const element = ELEMENT_WORD[def.element] ? def.element : 'kim';
    const family = familyOf(def || {});
    const pool = POOLS[family].slice();
    let h = hash(`${key}|${def.name || ''}`);
    const moves = [];
    for (let i = 0; i < 4; i += 1) {
        const pick = pool.splice(h % pool.length, 1)[0];
        h = Math.imul(h ^ (h >>> 13), 2654435761) >>> 0;
        moves.push({ i, name: `${ELEMENT_WORD[element]} ${pick[1]}`, fx: pick[0], v: h % 4, big: false });
    }
    // The ultimate carries the monster's own name, so no two monsters share a signature move.
    const sign = String(def.name || '').replace(/[0-9]+/g, '').replace(/\s+/g, ' ').trim();
    moves.push({ i: 4, name: sign ? `${sign} · ${ULTIMATE[element]}` : ULTIMATE[element], fx: 'ult', v: h % 3, big: true });
    const kit = Object.freeze(moves.map(move => Object.freeze(move)));
    cache.set(key, kit);
    return kit;
}

module.exports = { monsterSkillKit, familyOf };
