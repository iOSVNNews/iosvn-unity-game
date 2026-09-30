'use strict';

// Mode battlegrounds are separate from the explorable province/town catalog.
// Each activity owns a dedicated arena; ancient caves rotate as a shared,
// server-clocked random cycle so every player sees the same active cave.
const CAVE_ROTATION_SECONDS = 30 * 60;

const makeMap = (id, name, description, terrain, layout, palette, weather, visualThemeId) => Object.freeze({
    id,
    name,
    description,
    terrain,
    layout,
    palette: Object.freeze(palette),
    weather,
    visualThemeId,
});

const fixedMode = (id, name, maps) => Object.freeze({
    id,
    name,
    activityType: id.startsWith('pvp_') ? 'pvp' : 'pve',
    rotationStrategy: 'fixed',
    rotationPeriodSeconds: 0,
    maps: Object.freeze(maps),
});

const caveMode = maps => Object.freeze({
    id: 'pve_ancient_cave',
    name: 'PvE Cổ Động',
    activityType: 'pve',
    rotationStrategy: 'random_cycle',
    rotationPeriodSeconds: CAVE_ROTATION_SECONDS,
    maps: Object.freeze(maps),
});

const BATTLE_MAP_SETS = Object.freeze([
    Object.freeze({
        id: 'pham_gioi',
        name: 'Phàm Giới',
        requiresAscension: false,
        realmMin: 0,
        realmMax: 10,
        modes: Object.freeze([
            fixedMode('pvp_ranked', 'PvP Xếp Hạng', [makeMap(
                'pham_ranked_thienha_dai', 'Đài Thiên Hạ Thanh Vân',
                'Đấu trường xếp hạng đặt giữa Thanh Vân Sơn; bệ đấu cân xứng, khán đài vây quanh và cột cờ phân thứ hạng.',
                'Thanh Vân Sơn môn', 'symmetric_stone_ring', ['#263A45', '#668F7B', '#D9B46C'], 'Sương sớm', 'battle/pham/ranked'
            )]),
            fixedMode('pvp_duel', 'PvP Quyết Đấu', [makeMap(
                'pham_duel_song_nguyet', 'Lôi Đài Song Nguyệt',
                'Sàn đấu đá xanh nhỏ giữa hai người, có vạch xuất phát đối xứng và đèn lồng dọc lan can.',
                'Lôi đài thành trấn', 'compact_duel_platform', ['#313745', '#8C6655', '#E5C17B'], 'Trời quang', 'battle/pham/duel'
            )]),
            fixedMode('pvp_sat_phat', 'PvP Sát Phạt', [makeMap(
                'pham_satphat_huyetnguyet', 'Huyết Nguyệt Chiến Trường',
                'Bãi chiến trường hoang tàn với chiến kỳ gãy, đá vụn và lối áp sát hai bên.',
                'Hoang nguyên chiến trường', 'broken_crossing', ['#351F28', '#75423B', '#D18B59'], 'Trăng đỏ', 'battle/pham/sat-phat'
            )]),
            fixedMode('pvp_sect', 'PvP Tông Môn', [makeMap(
                'pham_sect_vankiem', 'Vạn Kiếm Luận Kiếm Đài',
                'Đại diễn võ trường của tông môn, bốn góc có kiếm trụ và khu vực quan chiến của đệ tử.',
                'Sơn môn tông phái', 'sect_square_courtyard', ['#283B37', '#537769', '#D6B46C'], 'Gió nhẹ', 'battle/pham/sect'
            )]),
            fixedMode('pve_small_monster', 'PvE Tiểu Yêu', [makeMap(
                'pham_pve_truc_lam', 'Rừng Trúc Thanh Vân',
                'Lối rừng hẹp có bụi trúc, đá rêu và khoảng trống nhỏ để chạm trán tiểu yêu.',
                'Rừng trúc', 'winding_forest_path', ['#23372C', '#567A48', '#B8A46A'], 'Mù sương', 'battle/pham/small-monster'
            )]),
            fixedMode('pve_elite_boss', 'PvE Đại Boss', [makeMap(
                'pham_pve_duongkhang', 'Sào Huyệt Đương Khang',
                'Hang núi rộng có nền đất nứt, tinh thạch ven vách và một bệ boss trung tâm.',
                'Hang núi', 'wide_boss_cavern', ['#342A24', '#68503A', '#C18A49'], 'Bụi đá', 'battle/pham/elite-boss'
            )]),
            fixedMode('pve_world_boss', 'PvE Boss Thế Giới', [makeMap(
                'pham_worldboss_loitranh', 'Cổ Chiến Trường Lôi Trạch',
                'Chiến trường ngoài trời rộng cho nhiều người, có vùng xuất hiện boss và các điểm đứng quanh rìa.',
                'Lôi Trạch', 'large_world_boss_field', ['#27323B', '#536D78', '#D1A64F'], 'Mưa sấm', 'battle/pham/world-boss'
            )]),
            caveMode([
                makeMap('pham_cave_huyet_sac', 'Huyết Sắc Cấm Địa', 'Cổ động đỏ sẫm với lối đá ngoằn ngoèo, bệ cơ duyên nằm sâu bên trong.', 'Cấm địa', 'branching_cave', ['#351D26', '#74403F', '#D08D54'], 'Khí huyết', 'battle/pham/cave-huyet-sac'),
                makeMap('pham_cave_that_huyen', 'Bí Cảnh Thất Huyền', 'Động phủ đá nhiều tầng, có cầu gỗ, phòng thủ hộ và sân đấu cuối đường.', 'Động phủ', 'layered_cave', ['#26352E', '#66764D', '#C3A467'], 'Sương núi', 'battle/pham/cave-that-huyen'),
                makeMap('pham_cave_van_mong', 'Động Thiên Vân Mộng', 'Thủy động xanh lục với hồ cạn, cầu đá và những đảo nhỏ nối thành đường đi.', 'Thủy động', 'island_cavern', ['#1E3A3B', '#4F8173', '#C1B66F'], 'Hơi nước', 'battle/pham/cave-van-mong'),
                makeMap('pham_cave_hac_phong', 'Phong Ấn Hắc Phong', 'Khe núi tối có cổng phong ấn, các luồng gió tạo lối giao chiến hẹp.', 'Khe núi phong ấn', 'sealed_passage', ['#252A35', '#525364', '#BA885C'], 'Hắc phong', 'battle/pham/cave-hac-phong'),
                makeMap('pham_cave_tinh_van', 'Cổ Động Tinh Vẫn', 'Hang thiên thạch lấp lánh với hố va chạm và mảnh tinh thạch rải quanh bệ thủ lĩnh.', 'Hang thiên thạch', 'impact_crater_cave', ['#252C3D', '#596C89', '#D5B56D'], 'Bụi sao', 'battle/pham/cave-tinh-van'),
            ]),
        ]),
    }),
    Object.freeze({
        id: 'tien_gioi',
        name: 'Tiên Giới',
        requiresAscension: true,
        realmMin: 11,
        realmMax: 65,
        modes: Object.freeze([
            fixedMode('pvp_ranked', 'PvP Xếp Hạng', [makeMap(
                'tien_ranked_van_tien_dai', 'Vạn Tiên Thiên Bảng Đài',
                'Đài xếp hạng lơ lửng giữa tầng mây, có bốn trụ tiên quang đánh dấu ranh giới và khán đài nhiều tầng.',
                'Thiên bảng tiên vực', 'floating_ranked_platform', ['#202B49', '#687BAA', '#E6C979'], 'Mây vàng', 'battle/tien/ranked'
            )]),
            fixedMode('pvp_duel', 'PvP Quyết Đấu', [makeMap(
                'tien_duel_cuu_thien', 'Cửu Thiên Luận Đạo Đài',
                'Bệ quyết đấu đối xứng trên mây, đường viền khắc tiên văn và hai điểm nhập trận riêng.',
                'Tiên đài', 'floating_duel_circle', ['#27334F', '#8196B6', '#EAD493'], 'Mây tĩnh', 'battle/tien/duel'
            )]),
            fixedMode('pvp_sat_phat', 'PvP Sát Phạt', [makeMap(
                'tien_satphat_hu_khong', 'Hư Không Huyết Chiến Trường',
                'Chiến trường nứt vỡ giữa hư không, các mảnh đảo đá tạo nhiều hướng áp sát.',
                'Hư không', 'shattered_void_field', ['#211F3C', '#653C68', '#D77C72'], 'Tinh vân đỏ', 'battle/tien/sat-phat'
            )]),
            fixedMode('pvp_sect', 'PvP Tông Môn', [makeMap(
                'tien_sect_chinh_tien_dai', 'Tiên Tông Chinh Tiên Đài',
                'Diễn võ trường của tiên tông với trận văn phát sáng, cờ hiệu hai phe và quảng trường quan chiến.',
                'Tiên tông', 'celestial_sect_courtyard', ['#26394B', '#4F8390', '#E4C476'], 'Tiên vụ', 'battle/tien/sect'
            )]),
            fixedMode('pve_small_monster', 'PvE Tiểu Yêu', [makeMap(
                'tien_pve_tinh_khong', 'Tinh Lộ Ngoại Vực',
                'Đường đá trên tiểu hành tinh, có tinh thạch và khoảng giao chiến dành cho nhóm yêu thú nhỏ.',
                'Tinh không ngoại vực', 'asteroid_trail', ['#1E2B40', '#42627B', '#BBAF83'], 'Bụi tinh', 'battle/tien/small-monster'
            )]),
            fixedMode('pve_elite_boss', 'PvE Đại Boss', [makeMap(
                'tien_pve_tinhthu_sao', 'Tinh Thú Sào Huyệt',
                'Sào huyệt của cự thú tiên giới, nền tinh thạch rộng và vòng trận pháp bao quanh thủ lĩnh.',
                'Hang tinh thạch', 'celestial_boss_cavern', ['#202944', '#4B5F8B', '#D8AC62'], 'Tàn quang', 'battle/tien/elite-boss'
            )]),
            fixedMode('pve_world_boss', 'PvE Boss Thế Giới', [makeMap(
                'tien_worldboss_van_gioi', 'Vạn Giới Phong Thần Chiến Trường',
                'Không gian liên giới quy mô lớn với các cổng dịch chuyển và nhiều vị trí tập kết tổ đội.',
                'Chiến trường vạn giới', 'multi_gate_world_field', ['#20243E', '#52658E', '#E6C66E'], 'Cực quang', 'battle/tien/world-boss'
            )]),
            caveMode([
                makeMap('tien_cave_hon_don', 'Hỗn Độn Tinh Khư Cổ Động', 'Cổ động hỗn độn có những mảng địa hình đảo nổi và khe sáng giữa hư không.', 'Tinh khư hỗn độn', 'floating_cave_islands', ['#252543', '#625F91', '#D0B979'], 'Hỗn độn lưu quang', 'battle/tien/cave-hon-don'),
                makeMap('tien_cave_to_long', 'Tổ Long Thánh Động', 'Thánh động cổ có vảy long thạch, đường vòng quanh long cốt và điện thờ trung tâm.', 'Long động', 'dragonbone_cavern', ['#302B3C', '#75634D', '#D5B15F'], 'Long tức', 'battle/tien/cave-to-long'),
                makeMap('tien_cave_van_kiep', 'Vạn Kiếp Phong Lôi Cổ Động', 'Động phủ sấm gió với các cột lôi thạch và cầu hẹp bắc qua vực sâu.', 'Phong lôi động', 'storm_bridge_cave', ['#222F48', '#4E7390', '#D8C078'], 'Lôi vân', 'battle/tien/cave-van-kiep'),
                makeMap('tien_cave_thai_thanh', 'Thái Thanh Đạo Khư', 'Đạo khư thanh tịnh có hành lang vòng, hồ tiên khí và đài ngộ đạo ở cuối map.', 'Đạo khư', 'serene_temple_cave', ['#293A42', '#71928E', '#E0D49A'], 'Tiên vụ', 'battle/tien/cave-thai-thanh'),
                makeMap('tien_cave_truong_sinh', 'Trường Sinh Bí Động', 'Bí động cổ thụ với rễ linh mộc, dòng suối phát sáng và sân thủ hộ rộng.', 'Linh mộc tiên động', 'ancient_tree_grotto', ['#243A38', '#668A69', '#D6C77C'], 'Linh lộ', 'battle/tien/cave-truong-sinh'),
            ]),
        ]),
    }),
]);

function hash32(value) {
    let hash = 0x811c9dc5;
    for (let i = 0; i < value.length; i++) {
        hash ^= value.charCodeAt(i);
        hash = Math.imul(hash, 0x01000193);
    }
    return hash >>> 0;
}

function shuffledIndexes(realmId, cycle, count) {
    const values = Array.from({ length: count }, (_, index) => index);
    let state = hash32(`${realmId}:${cycle}`) || 0x6d2b79f5;
    for (let i = count - 1; i > 0; i--) {
        state ^= state << 13;
        state ^= state >>> 17;
        state ^= state << 5;
        const j = (state >>> 0) % (i + 1);
        [values[i], values[j]] = [values[j], values[i]];
    }
    return values;
}

function activeCaveIndex(realmId, count, slot) {
    const cycle = Math.floor(slot / count);
    const positionInCycle = slot % count;
    const currentOrder = shuffledIndexes(realmId, cycle, count);
    if (cycle > 0) {
        const previousOrder = shuffledIndexes(realmId, cycle - 1, count);
        if (currentOrder[0] === previousOrder[count - 1]) {
            [currentOrder[0], currentOrder[1]] = [currentOrder[1], currentOrder[0]];
        }
    }
    return currentOrder[positionInCycle];
}

function getBattleMapSets(nowMs = Date.now()) {
    const nowSeconds = Math.floor(nowMs / 1000);
    const slot = Math.floor(nowSeconds / CAVE_ROTATION_SECONDS);
    return BATTLE_MAP_SETS.map(realm => ({
        id: realm.id,
        name: realm.name,
        requiresAscension: realm.requiresAscension,
        realmMin: realm.realmMin,
        realmMax: realm.realmMax,
        modes: realm.modes.map(mode => {
            const isRotating = mode.rotationStrategy === 'random_cycle';
            const activeIndex = isRotating ? activeCaveIndex(realm.id, mode.maps.length, slot) : 0;
            const activeMapId = mode.maps[activeIndex]?.id || '';
            return {
                id: mode.id,
                name: mode.name,
                activityType: mode.activityType,
                rotation: {
                    strategy: mode.rotationStrategy,
                    periodSeconds: mode.rotationPeriodSeconds,
                    slot: isRotating ? slot : 0,
                    nextRotationAt: isRotating ? (slot + 1) * CAVE_ROTATION_SECONDS : 0,
                },
                activeMapId,
                maps: mode.maps.map((map, index) => ({ ...map, isActive: index === activeIndex })),
            };
        }),
    }));
}

module.exports = { getBattleMapSets };
