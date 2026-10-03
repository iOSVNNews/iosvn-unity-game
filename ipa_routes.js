'use strict';

// Gameplay API for the IPA client. Every system of the shared engine is exposed
// here so the native Unity screens can drive it. Conventions:
// - GET routes return the system view.
// - POST routes return { ok, toast?, state, ...payload } where `state` is the
//   refreshed player view (game.view) so the client can redraw its HUD.
// Route names follow the original game server so content stays traceable.

const vn = value => (Number(value) || 0).toLocaleString('vi-VN');

function createGameplayRoutes({ game, C, GameError, requireTownRealm, withBattleMap, pveModeForBattle, pvpModeForBattle }) {
    const view = userId => game.view(userId);
    const done = (userId, payload = {}, toast) => {
        const out = { ok: true, ...payload, state: view(userId) };
        if (toast || payload.message) out.toast = toast || payload.message;
        return out;
    };
    const text = value => String(value ?? '').trim();
    const int = (value, fallback = 0) => {
        const n = Math.floor(Number(value));
        return Number.isFinite(n) ? n : fallback;
    };
    const pveBattleView = userId => {
        const battle = game.battle(userId);
        if (!battle) return null;
        return withBattleMap(battle.view(game.now(), userId), battle, userId, pveModeForBattle(battle));
    };
    const pvpBattleView = userId => {
        const instance = game.pvpManualBattles?.get(String(userId));
        const battleView = game.getPvpBattle(userId);
        if (!battleView || battleView.none) return null;
        return withBattleMap(battleView, instance, userId, pvpModeForBattle(instance));
    };
    // After any action that may open a fight, tell the client which battle screen to show.
    const battleOf = userId => {
        const pve = pveBattleView(userId);
        if (pve && !pve.none) return { battleKind: 'pve', battle: pve };
        const pvp = pvpBattleView(userId);
        if (pvp) return { battleKind: 'pvp', battle: pvp };
        return { battleKind: null, battle: null };
    };
    const requireTarget = (userId, townId) => { if (townId) requireTownRealm(userId, townId); };

    return {
        // ------------------------------------------------------------- overview
        'GET /api/live': ({ user }) => game.liveState(user.id),
        'GET /api/world/status': ({ user }) => game.worldBossSnapshot(user.id),
        'GET /api/battle/any': ({ user }) => battleOf(user.id),
        'GET /api/leaderboard': ({ user }) => ({ list: game.leaderboard(50), isViewerHidden: game.isHiddenFromPlayers(user.id) }),
        'GET /api/codex': ({ user }) => game.codex(user.id),
        'POST /api/item/sources': ({ body }) => ({ results: game.searchItemDropSources(text(body.query || body.target)) }),

        // ------------------------------------------------------------- character
        'POST /api/linh-can/change': ({ user, body }) => { const r = game.changeLinhCan(user.id, body.id); return done(user.id, r, r.message); },
        'POST /api/reset-mon': ({ user, body }) => {
            if (body.confirm !== 'DOI MON') throw new GameError('Cần xác nhận đổi môn.');
            game.resetMon(user.id);
            return done(user.id, {}, 'Đã tán công, hãy chọn lại môn phái và hệ.');
        },
        'POST /api/player/change-gender': ({ user }) => { const r = game.changeGender(user.id); return done(user.id, r, r.message); },
        'POST /api/ascend': ({ user }) => { const r = game.ascend(user.id); return done(user.id, r, r.message); },

        // ------------------------------------------------------------- bag & equipment
        'POST /api/equip': ({ user, body }) => {
            const r = game.equip(user.id, text(body.uid), body.slot);
            return done(user.id, {}, r?.refineAt ? 'Đang luyện hóa: 50% chỉ số cho tới khi xong.' : 'Đã trang bị.');
        },
        'POST /api/unequip': ({ user, body }) => { game.unequip(user.id, body.slot); return done(user.id, {}, 'Đã tháo trang bị.'); },
        'POST /api/move': ({ user, body }) => { game.move(user.id, text(body.uid), body.to); return done(user.id); },
        'POST /api/quick': ({ user, body }) => { game.setQuick(user.id, int(body.i), body.uid || null); return done(user.id); },
        'POST /api/skill-slot': ({ user, body }) => { game.setSkillSlot(user.id, int(body.i), body.id || null); return done(user.id); },
        'POST /api/learn': ({ user, body }) => {
            const r = game.learn(user.id, body.uid || body.itemUid || body.id);
            return done(user.id, {}, (r && typeof r === 'object' && r.message) ? r.message : 'Đã lĩnh ngộ công pháp.');
        },
        'POST /api/use': ({ user, body }) => {
            const r = game.use(user.id, text(body.uid));
            if (r && r.tracker) return done(user.id, { tracker: r.tracker }, r.message);
            return done(user.id, {}, (r && typeof r === 'object') ? r.message : `Thể lực ${r}.`);
        },
        'POST /api/sell': ({ user, body }) => { const gain = game.sell(user.id, text(body.uid), body.qty); return done(user.id, { gain }, `+${vn(gain)} linh thạch.`); },
        'POST /api/buy': ({ user, body }) => { const n = game.buy(user.id, body.id, body.qty); return done(user.id, {}, `Đã mua ${n}.`); },
        'POST /api/repair': ({ user, body }) => { const cost = game.repair(user.id, text(body.uid)); return done(user.id, { cost }, `Đã sửa, −${vn(cost)} linh thạch.`); },
        'POST /api/safe-storage/rent': ({ user }) => {
            const r = game.rentSafeStorage(user.id);
            return done(user.id, r, `Đã thuê Kho An Toàn 10 ô thêm 30 ngày (−${vn(r.cost)} linh thạch).`);
        },
        'POST /api/fragment/combine': ({ user, body }) => { const r = game.combineFragment(user.id, body.fragmentId || body.id); return done(user.id, r, r.message); },
        'POST /api/heal-potion': ({ user, body }) => {
            const r = game.healPotion(user.id, text(body.uid));
            return done(user.id, { healResult: r }, `Đã dùng ${r.itemName}, hồi ${r.healPct}% khí huyết (+${vn(r.healed)}).`);
        },
        'POST /api/beast/mount': ({ user, body }) => { const r = game.mountBeast(user.id, body.beastId); return done(user.id, r, r.message); },
        'POST /api/beast/capture-choice': ({ user, body }) => { const r = game.resolveBeastCapture(user.id, Boolean(body.accept)); return done(user.id, r, r.message); },

        // ------------------------------------------------------------- crafting
        'GET /api/crafting': ({ user }) => game.getCraftingView(user.id),
        'POST /api/craft/fire': ({ user, body }) => {
            const r = game.setCraftingFire(user.id, body.profession, body.fireId || null);
            return done(user.id, { ...r, crafting: game.getCraftingView(user.id) }, r.message);
        },
        'POST /api/craft/equip': ({ user, body }) => { const r = game.craftEquip(user.id, body.recipeId); return done(user.id, { ...r, crafting: game.getCraftingView(user.id) }, r.message); },
        'POST /api/craft/potion': ({ user, body }) => { const r = game.craftPotion(user.id, body.recipeId); return done(user.id, { ...r, crafting: game.getCraftingView(user.id) }, r.message); },
        'POST /api/craft/talisman': ({ user, body }) => { const r = game.craftTalisman(user.id, body.recipeId); return done(user.id, { ...r, crafting: game.getCraftingView(user.id) }, r.message); },

        // ------------------------------------------------------------- market & travel
        'GET /api/market': ({ user }) => game.marketView(user.id),
        'POST /api/market/list': ({ user, body }) => {
            const r = game.marketList(user.id, text(body.uid), body.price, body.qty);
            return done(user.id, { market: game.marketView(user.id) }, `Đã treo bán ${r.name}${r.qty > 1 ? ` ×${r.qty}` : ''} giá ${vn(r.price)} linh thạch.`);
        },
        'POST /api/market/buy': ({ user, body }) => {
            const r = game.marketBuy(user.id, body.id);
            return done(user.id, { market: game.marketView(user.id) }, `Đã mua ${r.name}${r.qty > 1 ? ` ×${r.qty}` : ''}.`);
        },
        'POST /api/market/cancel': ({ user, body }) => {
            const name = game.marketCancel(user.id, body.id);
            return done(user.id, { market: game.marketView(user.id) }, `Đã gỡ ${name} về túi.`);
        },
        'POST /api/market/teleport': ({ user, body }) => {
            requireTarget(user.id, body.toTownId);
            const r = game.marketTeleport(user.id, body.toTownId);
            return done(user.id, { teleportResult: r }, `Đã truyền tống đến ${r.toTownName} (−${vn(r.cost)} linh thạch).`);
        },
        'POST /api/teleport': ({ user, body }) => {
            requireTarget(user.id, body.toTownId);
            const r = game.teleportWithRing(user.id, body.toTownId);
            return done(user.id, { teleportResult: r }, `Nhẫn Na Di đưa bạn đến ${r.toTownName}.`);
        },

        // ------------------------------------------------------------- town services
        'POST /api/world/enter-town': ({ user, body }) => {
            const player = game.player(user.id);
            const currentTown = C.TOWN_BY_ID.get(player?.town || '');
            const targetTown = C.TOWN_BY_ID.get(text(body.townId));
            if (currentTown && targetTown && currentTown.mapId === targetTown.mapId) requireTownRealm(user.id, body.townId);
            if (body.mapId !== undefined && Number.isInteger(Number(body.x)) && Number.isInteger(Number(body.y))) {
                game.moveWorldPosition(user.id, { mapId: text(body.mapId), x: Number(body.x), y: Number(body.y) });
            }
            const r = game.enterTownOnFoot(user.id, text(body.townId));
            return done(user.id, r, r.changed ? `Đã vào ${r.townName}.` : null);
        },
        'POST /api/world/province-gate': ({ user, body }) => {
            const r = game.crossProvinceGate(user.id, text(body.gateId));
            return done(user.id, r, r.message);
        },
        'POST /api/world/ascension-gate': ({ user }) => {
            const r = game.crossAscensionGate(user.id);
            return done(user.id, r, r.message);
        },
        'POST /api/player/look': ({ user, body }) => { const r = game.setLook(user.id, body.look); return done(user.id, r, 'Đã đổi diện mạo.'); },
        'POST /api/town/heal': ({ user }) => {
            const r = game.townHeal(user.id);
            return done(user.id, { healResult: r }, `Y Quán đã trị thương, hồi phục 100% khí huyết (−${vn(r.cost)} linh thạch).`);
        },
        'GET /api/town/bounties': ({ user }) => ({ board: game.getTownBountyBoard(user.id) }),
        'POST /api/town/bounty/accept': ({ user, body }) => {
            const r = game.acceptTownBounty(user.id, body.bountyId);
            return done(user.id, { ...r, board: game.getTownBountyBoard(user.id) }, r.message);
        },
        'POST /api/town/bounty/claim': ({ user, body }) => {
            const r = body?.bountyId ? game.claimTownBounty(user.id, body.bountyId) : game.claimBounty(user.id, body?.type || 'town', body?.taskId);
            return done(user.id, { ...r, board: game.getTownBountyBoard(user.id) }, r.message);
        },
        'POST /api/town/enter': ({ user }) => { const r = game.enterTown(user.id); return done(user.id, r, r.message); },
        'POST /api/town/leave': ({ user }) => { const r = game.leaveTown(user.id); return done(user.id, r, r.message); },
        'POST /api/town/attack': ({ user, body }) => { const r = game.townAttack(user.id, body.targetId); return done(user.id, { ...r, ...battleOf(user.id) }, r.message); },
        'POST /api/town/flee': ({ user }) => { const r = game.townFlee(user.id); return done(user.id, r, r.message); },
        'POST /api/roam/attack': ({ user, body }) => { const r = game.roamAttack(user.id, body.targetId); return done(user.id, { ...r, ...battleOf(user.id) }, r.message); },
        'POST /api/demon/repent': ({ user }) => { const r = game.demonRepent(user.id); return done(user.id, r, r.message); },

        // ------------------------------------------------------------- dungeons & hunts
        'GET /api/dungeons': ({ user }) => {
            const list = game.getDungeonsView(user.id);
            return {
                list,
                dailyCount: list.dailyCount || 0,
                dailyMax: list.dailyMax || 10,
                dailyRemaining: list.dailyRemaining != null ? list.dailyRemaining : 10,
                equipTrials: list.equipTrials || [],
                equipTrialDaily: list.equipTrialDaily || { count: 0, max: 10, remaining: 10 },
                activeDungeon: list.activeDungeon || null,
            };
        },
        'POST /api/dungeon/abandon': ({ user }) => { const r = game.abandonDungeon(user.id); return done(user.id, r, r.message); },
        'POST /api/hunt': ({ user, body }) => {
            requireTownRealm(user.id, game.player(user.id)?.town);
            game.startHunt(user.id, body.monsterId);
            return done(user.id, battleOf(user.id));
        },
        'GET /api/npcs': ({ user }) => { const p = game.player(user.id); return { list: game.getNpcList(user.id), daily: p ? game.getNpcDaily(p) : null }; },
        'POST /api/npc/manual_battle': ({ user, body }) => { const r = game.startNpcBattle(user.id, body.npcId); return done(user.id, { ...r, ...battleOf(user.id) }, r.message); },

        // ------------------------------------------------------------- pvp extras
        'GET /api/pvp/refresh': ({ user }) => game.pvpList(user.id, true),
        'POST /api/pvp/challenge': ({ user, body }) => { const r = game.pvpChallenge(user.id, body.targetId); return done(user.id, { ...r, pvp: game.pvpList(user.id) }, r.message); },
        'POST /api/pvp/respond': ({ user, body }) => {
            const r = game.pvpRespond(user.id, body.challengeId, Boolean(body.accept));
            return done(user.id, { ...r, pvp: game.pvpList(user.id), ...battleOf(user.id) }, r.message);
        },
        'POST /api/pvp/bot-fight': ({ user, body }) => {
            const r = game.pvpStartBotBattle(user.id, body.challengeId);
            return done(user.id, { ...r, pvp: game.pvpList(user.id), ...battleOf(user.id) }, r.message);
        },
        'POST /api/pvp/bot-takeover': ({ user, body }) => done(user.id, { result: game.pvpManualBotTakeover(user.id, body.battleId), ...battleOf(user.id) }),
        'POST /api/pvp/battle/claim': ({ user, body }) => done(user.id, { result: game.pvpClaimBattleResult(user.id, body.battleId) }),
        'GET /api/pvp/leaderboard': () => ({ list: game.pvpLeaderboard(30) }),
        'GET /api/pvp/town-rank': ({ user }) => { const p = game.player(user.id); return { list: game.pvpTownLeaderboard(p?.town || 'thanh_van'), town: p?.town }; },
        'GET /api/pvp/demon-rank': () => ({ list: game.pvpDemonLeaderboard() }),
        'GET /api/pvp/dai-phong-than': () => ({ list: game.pvpDaiPhongThanLeaderboard() }),
        'GET /api/pvp/immortal-rank': () => ({ list: game.pvpImmortalLeaderboard() }),

        // ------------------------------------------------------------- party
        'GET /api/party/status': ({ user }) => ({ now: game.now(), inBattle: Boolean(game.activeBattle(user.id)), party: game.partyView(user.id) }),
        'POST /api/party/create': ({ user }) => { const pt = game.partyCreate(user.id); return done(user.id, { party: game.partyView(user.id) }, `Đã lập tổ đội, mã ${pt.code}.`); },
        'POST /api/party/join': ({ user, body }) => { game.partyJoin(user.id, text(body.code)); return done(user.id, { party: game.partyView(user.id) }, 'Đã vào tổ đội.'); },
        'POST /api/party/leave': ({ user }) => { game.partyLeave(user.id); return done(user.id, { party: null }, 'Đã rời tổ đội.'); },
        'POST /api/party/ready': ({ user, body }) => { game.partyReady(user.id, Boolean(body.ready)); return done(user.id, { party: game.partyView(user.id) }); },
        'POST /api/party/kick': ({ user, body }) => { game.partyKick(user.id, body.userId); return done(user.id, { party: game.partyView(user.id) }, 'Đã mời thành viên rời tổ đội.'); },
        'POST /api/party/buff': ({ user, body }) => { const r = game.partyBuff(user.id, body.potionId); return done(user.id, { ...r, party: game.partyView(user.id) }, r.message); },

        // ------------------------------------------------------------- sect
        'GET /api/sect': ({ user }) => game.sectList(user.id),
        'POST /api/sect/create': ({ user, body }) => {
            const s = game.sectCreate(user.id, body);
            return done(user.id, { sect: game.sectView(user.id) }, `Đã sáng lập tông môn ${s.name}!`);
        },
        'POST /api/sect/join': ({ user, body }) => { const s = game.sectJoin(user.id, body.sectId); return done(user.id, { sect: game.sectView(user.id) }, `Đã gia nhập ${s.name}.`); },
        'POST /api/sect/leave': ({ user }) => {
            const r = game.sectLeave(user.id);
            return done(user.id, { sect: null }, r.wasDuringWar ? `Đã rời tông môn trong lúc tông chiến; bị phạt ${vn(r.penalty)} linh thạch.` : 'Đã rời tông môn.');
        },
        'POST /api/sect/donate': ({ user, body }) => {
            const r = game.sectDonate(user.id, body.stones);
            return done(user.id, { ...r, sect: game.sectView(user.id) }, r.leveledUp ? `Tông môn đã thăng cấp ${r.sectLevel}!` : (r.message || 'Đã cống hiến.'));
        },
        'POST /api/sect/daily': ({ user }) => {
            const r = game.sectClaimDaily(user.id);
            return done(user.id, { ...r, sect: game.sectView(user.id) }, `Bổng lộc: +${vn(r.stones)} linh thạch, +${vn(r.exp)} tu vi, +${vn(r.coins)} cống hiến.`);
        },
        'POST /api/sect/buy': ({ user, body }) => { const r = game.sectBuy(user.id, body.itemId); return done(user.id, { sect: game.sectView(user.id) }, `Đã đổi ${r.item?.name || 'vật phẩm'} từ Tàng Bảo Các.`); },
        'POST /api/sect/deposit-item': ({ user, body }) => { const r = game.sectDepositItem(user.id, body.itemUid); return done(user.id, { ...r, sect: game.sectView(user.id) }, r.message); },
        'POST /api/sect/set-price': ({ user, body }) => { const r = game.sectSetPrice(user.id, body.itemId, body.price); return done(user.id, { ...r, sect: game.sectView(user.id) }, r.message); },
        'POST /api/sect/deposit-stones': ({ user, body }) => { const r = game.sectDepositStones(user.id, body.stones); return done(user.id, { ...r, sect: game.sectView(user.id) }, r.message); },
        'POST /api/sect/treasury-buy': ({ user, body }) => { const r = game.sectTreasuryBuy(user.id, body.itemId, body.qty); return done(user.id, { ...r, sect: game.sectView(user.id) }, r.message); },
        'POST /api/sect/storage/sell': ({ user, body }) => { const r = game.sectStorageSell(user.id, body); return done(user.id, { ...r, sect: game.sectView(user.id) }, r.message); },
        'POST /api/sect/shop/sell': ({ user, body }) => { const r = game.sectShopSell(user.id, body); return done(user.id, { ...r, sect: game.sectView(user.id) }, r.message); },
        'POST /api/sect/challenge-role': ({ user, body }) => {
            const r = game.sectChallengeRole(user.id, body.targetRole);
            return done(user.id, { ...r, sect: game.sectView(user.id), ...battleOf(user.id) }, r.message);
        },
        'POST /api/sect/promote': ({ user }) => { const r = game.sectPromote(user.id); return done(user.id, { ...r, sect: game.sectView(user.id) }, r.message); },
        'POST /api/sect/assign-role': ({ user, body }) => { const r = game.sectAssignRole(user.id, body.targetUserId, body.newRole); return done(user.id, { ...r, sect: game.sectView(user.id) }, r.message); },
        'POST /api/sect/war/challenge': ({ user, body }) => { const r = game.sectWarChallenge(user.id, body.targetSectId, body.type); return done(user.id, { ...r, sect: game.sectView(user.id) }, r.message); },
        'POST /api/sect/war/fight': ({ user, body }) => {
            const r = game.sectWarFight(user.id, body.warId, body.targetMemberId);
            return done(user.id, { ...r, sect: game.sectView(user.id), ...battleOf(user.id) }, r.message);
        },
        'GET /api/sect/territories': ({ user }) => ({ list: game.getSectTerritories(user.id) }),
        'POST /api/sect/territory/claim': ({ user, body }) => { const r = game.sectClaimTerritory(user.id, body.territoryId); return done(user.id, { ...r, territories: game.getSectTerritories(user.id) }, r.message); },
        'POST /api/sect/territory/harvest': ({ user, body }) => { const r = game.sectHarvestTerritory(user.id, body.territoryId); return done(user.id, { ...r, territories: game.getSectTerritories(user.id) }, r.message); },
        'POST /api/sect/territory/harvest_all': ({ user }) => { const r = game.sectHarvestAll(user.id); return done(user.id, { ...r, territories: game.getSectTerritories(user.id) }, r.message); },

        // ------------------------------------------------------------- social & family
        'GET /api/social': ({ user }) => game.socialView(user.id),
        'POST /api/social/profile': ({ user, body }) => game.socialProfileView(user.id, body.targetId),
        'POST /api/social/friend/request': ({ user, body }) => { const r = game.socialFriendRequest(user.id, body.targetId); return done(user.id, { ...r, social: game.socialView(user.id) }, r.message); },
        'POST /api/social/friend/accept': ({ user, body }) => { const r = game.socialFriendAccept(user.id, body.fromId); return done(user.id, { ...r, social: game.socialView(user.id) }, r.message || 'Đã kết bạn.'); },
        'POST /api/social/friend/reject': ({ user, body }) => { game.socialFriendReject(user.id, body.fromId); return done(user.id, { social: game.socialView(user.id) }, 'Đã từ chối lời mời kết bạn.'); },
        'POST /api/social/friend/cancel': ({ user, body }) => { game.socialFriendCancel(user.id, body.targetId); return done(user.id, { social: game.socialView(user.id) }, 'Đã thu hồi lời mời kết bạn.'); },
        'POST /api/social/friend/remove': ({ user, body }) => { game.socialFriendRemove(user.id, body.targetId); return done(user.id, { social: game.socialView(user.id) }, 'Đã hủy kết bạn.'); },
        'POST /api/social/friend/gift': ({ user, body }) => {
            const r = game.socialFriendGiftItem(user.id, body.targetId, body.itemUid, body.qty);
            return done(user.id, { ...r, social: game.socialView(user.id) }, r.message);
        },
        'POST /api/social/relation/request': ({ user, body }) => { const r = game.socialRelationRequest(user.id, body.targetId, body.kind); return done(user.id, { ...r, social: game.socialView(user.id) }, r.message); },
        'POST /api/social/relation/accept': ({ user, body }) => { game.socialRelationAccept(user.id, body.fromId, body.kind); return done(user.id, { social: game.socialView(user.id) }, 'Đã xác nhận quan hệ.'); },
        'POST /api/social/relation/reject': ({ user, body }) => { game.socialRelationReject(user.id, body.fromId, body.kind); return done(user.id, { social: game.socialView(user.id) }, 'Đã từ chối lời thỉnh cầu.'); },
        'POST /api/social/relation/break': ({ user, body }) => { const r = game.socialRelationBreak(user.id, body.targetId, body.kind); return done(user.id, { ...r, social: game.socialView(user.id) }, r.message); },

        // ------------------------------------------------------------- companion (Đạo Lữ)
        'GET /api/companion': ({ user }) => game.companionView(user.id),
        'POST /api/companion/propose': ({ user, body }) => { const r = game.companionPropose(user.id, body.targetId); return done(user.id, { companion: game.companionView(user.id) }, r.message); },
        'POST /api/companion/accept': ({ user, body }) => { game.companionAccept(user.id, body.fromId); return done(user.id, { companion: game.companionView(user.id) }, 'Chúc mừng! Hai vị đã kết thành đạo lữ.'); },
        'POST /api/companion/reject': ({ user, body }) => { game.companionReject(user.id, body.fromId); return done(user.id, { companion: game.companionView(user.id) }, 'Đã từ chối lời cầu duyên.'); },
        'POST /api/companion/divorce': ({ user }) => { game.companionDivorce(user.id); return done(user.id, { companion: game.companionView(user.id) }, 'Đã cắt đứt duyên nợ.'); },
        'POST /api/companion/gift': ({ user, body }) => {
            const r = game.companionGift(user.id, body.type);
            return done(user.id, { companion: game.companionView(user.id) }, `Đã tặng ${r.gift}! Thân mật: ${vn(r.newIntimacy)}.`);
        },
        'POST /api/companion/songtu': ({ user }) => {
            const r = game.companionSongTu(user.id);
            return done(user.id, { companion: game.companionView(user.id) }, `Song tu viên mãn! +${vn(r.expGain)} tu vi, +${vn(r.staminaGain)} thể lực.`);
        },
        'POST /api/companion/rob': ({ user, body }) => {
            const r = game.startCompanionRobBattle(user.id, body.targetId);
            return done(user.id, { companion: game.companionView(user.id), ...battleOf(user.id) }, r.message);
        },

        // ------------------------------------------------------------- inbox & events
        'GET /api/inbox': ({ user }) => ({ list: game.getInbox(user.id) }),
        'POST /api/inbox/claim': ({ user, body }) => {
            const r = game.claimMail(user.id, body.mailId);
            return done(user.id, { ...r, inbox: game.getInbox(user.id) }, `${r.message} Số dư: ${vn(r.balanceAfter)} linh thạch.`);
        },
        'POST /api/inbox/read': ({ user, body }) => { const r = game.markMailRead(user.id, body.mailId); return done(user.id, { ...r, inbox: game.getInbox(user.id) }); },
        'POST /api/inbox/delete': ({ user, body }) => { const r = game.deleteMail(user.id, body.mailId); return done(user.id, { ...r, inbox: game.getInbox(user.id) }, 'Đã xóa thư.'); },
        'GET /api/world/events': ({ user }) => ({ list: game.getWorldEvents(user.id) }),
        'POST /api/world/event/explore': ({ user, body }) => { const r = game.exploreWorldEvent(user.id, body.eventId); return done(user.id, { ...r, ...battleOf(user.id) }, r.message); },
        'GET /api/personal/events': ({ user }) => game.getPersonalEvents(user.id),
        'POST /api/personal/events/read': ({ user }) => game.getPersonalEvents(user.id, true),
    };
}

module.exports = { createGameplayRoutes };
