'use strict';

// Standalone IPA game server. This process is separate from the Telegram bot
// server (server.js) and uses a separate player save file.
const fs = require('fs');
const path = require('path');
const http = require('http');
const net = require('net');
const { Game, GameError } = require('./ipa_core/engine');
const { GameStore } = require('./ipa_core/store');
const C = require('./ipa_core/catalog');
const { getBattleMapSets } = require('./ipa_core/mode_maps');
const { EmailAuthStore } = require('./email_auth_store');
const { createGmailMailer } = require('./gmail_mailer');
const { createAccountOAuth } = require('./account_oauth');

const ROOT = __dirname;
const DATA_DIR = path.resolve(process.env.IPA_DATA_DIR || path.join(ROOT, 'server_data'));
const MAX_BODY_BYTES = 32 * 1024;

function clientAddress(req, trustedProxy = process.env.IPA_TRUSTED_PROXY_IP || '172.26.2.254') {
    const peer = String(req.socket.remoteAddress || 'unknown').replace(/^::ffff:/, '');
    if (peer !== trustedProxy) return peer;
    const forwarded = String(req.headers['x-forwarded-for'] || '').split(',').pop().trim();
    return net.isIP(forwarded) ? forwarded : peer;
}

function createRealmProgress(store) {
    const rows = store.data.realmProgress || (store.data.realmProgress = {});
    const maxIndex = Math.max(0, (C.CULTIVATION_REALM_NAMES || []).length - 1);
    const levelCap = index => {
        const safe = Math.max(0, Math.min(maxIndex, Math.floor(Number(index) || 0)));
        if (safe <= 30) return Math.round(1000 + ((5000000 - 1000) * safe / 30));
        const steps = Math.max(1, maxIndex - 30);
        return Math.round(5000000 + ((10000000 - 5000000) * (safe - 30) / steps));
    };
    const realmInfo = index => {
        const safe = Math.max(0, Math.min(maxIndex, Math.floor(Number(index) || 0)));
        return {
            index: safe,
            name: C.CULTIVATION_REALM_NAMES?.[safe] || 'Phàm Nhân',
            levelCap: Math.ceil(levelCap(safe) / 5),
            realmLevelCap: levelCap(safe),
            subStageCount: 5,
            isMaxRealm: safe === maxIndex,
        };
    };
    const row = id => {
        const key = String(id);
        return rows[key] || (rows[key] = { index: 0, experience: 0, subStage: 0 });
    };
    const persist = () => store.save();
    return {
        info: realmInfo,
        get(id) {
            const value = row(id);
            return { ...realmInfo(value.index), index: value.index, experience: value.experience, subStage: value.subStage };
        },
        addExp(id, amount) {
            const value = row(id);
            const gained = Math.min(Math.max(0, Number(amount) || 0), Math.max(0, realmInfo(value.index).levelCap - value.experience));
            value.experience += gained;
            persist();
            return { gained, levelUps: 0, atBottleneck: value.experience >= realmInfo(value.index).levelCap };
        },
        loseExp(id, amount) {
            const value = row(id);
            const lost = Math.min(value.experience, Math.max(0, Number(amount) || 0));
            value.experience -= lost;
            persist();
            return lost;
        },
        reset(id) {
            rows[String(id)] = { index: 0, experience: 0, subStage: 0 };
            persist();
        },
        set(id, state) {
            rows[String(id)] = {
                index: Math.max(0, Math.min(maxIndex, Number(state.index) || 0)),
                experience: Math.max(0, Number(state.experience) || 0),
                subStage: Math.max(0, Math.min(4, Number(state.subStage) || 0)),
            };
            persist();
        },
        breakthrough(id, force = false) {
            const value = row(id);
            if (value.index >= maxIndex) return { success: false, reason: 'max_realm' };
            if (!force && value.experience < realmInfo(value.index).levelCap) return { success: false, reason: 'not_enough_exp' };
            value.experience = 0;
            if (value.subStage < 4) {
                value.subStage += 1;
                persist();
                return { success: true, isStageBreakthrough: true, stage: value.subStage, newRealm: realmInfo(value.index) };
            }
            value.index += 1;
            value.subStage = 0;
            persist();
            return { success: true, isMajorBreakthrough: true, newRealm: realmInfo(value.index) };
        },
    };
}

function createIpaServer({ port = Number(process.env.IPA_PORT || 8788), host = process.env.IPA_HOST || '127.0.0.1', logger = console,
    dataDir = DATA_DIR, mailer = null, oauthEnv = process.env } = {}) {
    fs.mkdirSync(dataDir, { recursive: true });
    const store = new GameStore(path.join(dataDir, 'ipa_game_data.json'));
    const auth = new EmailAuthStore(path.join(dataDir, 'ipa_auth_data.json'));
    const gmail = mailer || createGmailMailer();
    const oauth = createAccountOAuth(auth, oauthEnv);
    const realms = createRealmProgress(store);
    const game = new Game({ store, realms });
    const rates = new Map();

    const requireTownRealm = (userId, townId) => {
        const town = C.TOWN_BY_ID.get(String(townId || ''));
        if (!town) return;
        const realm = game.realmOf(userId);
        if (realm.index < town.realmMin)
            throw new GameError(`Cần đạt ${town.realmMinName || `cảnh giới ${town.realmMin}`} mới được tham gia nội dung ở ${town.name}.`);
    };

    const battleMapFor = (userId, battle, modeId) => {
        const player = game.player(userId);
        const currentTown = C.TOWN_BY_ID.get(String(player?.town || ''));
        const currentWorld = C.MAP_BY_ID.get(String(currentTown?.mapId || ''));
        const immortal = currentWorld ? Boolean(currentWorld.ascensionRequired) : Boolean(player?.ascended);
        const set = getBattleMapSets(game.now()).find(item => item.requiresAscension === immortal);
        const mode = set?.modes.find(item => item.id === modeId);
        if (!mode?.maps?.length) return null;
        battle.modeBattleMapId ||= mode.activeMapId;
        const selected = mode.maps.find(item => item.id === battle.modeBattleMapId) || mode.maps.find(item => item.id === mode.activeMapId);
        return selected ? { ...selected, isActive: true, modeId: mode.id, modeName: mode.name, realmSetId: set.id, realmSetName: set.name } : null;
    };
    const withBattleMap = (view, battle, userId, modeId) => {
        if (!view || view.none) return view;
        const battleMap = battleMapFor(userId, battle || {}, modeId);
        return battleMap ? { ...view, battleMap } : view;
    };
    const pveModeForBattle = battle => battle?.dungeonLeaderId
        ? 'pve_ancient_cave'
        : battle?.monsterDef?.worldBoss
            ? 'pve_world_boss'
            : battle?.monsterDef?.small
                ? 'pve_small_monster'
                : 'pve_elite_boss';
    const pvpModeForBattle = battle => battle?.sectWarChallenge
        ? 'pvp_sect'
        : ['roam_attack', 'town_attack'].includes(battle?.purpose)
            ? 'pvp_sat_phat'
            : 'pvp_duel';

    const isDemon = player => Boolean(player?.isDemon) ||
        ((Number(player?.maScore) || 0) > 0 && (Number(player?.maScore) || 0) > Math.max(0, Number(player?.daoScore ?? player?.daoTam ?? 100)));
    const getTitleState = player => {
        if (!player?.registered || player.isNpc) return [];
        const candidates = Object.values(game.data.players || {}).filter(other =>
            other?.registered && !other.isNpc && !game.isHiddenFromPlayers(other) &&
            Boolean(other.ascended) === Boolean(player.ascended) && !isDemon(other) &&
            game.isPvpActive(other, game.now())
        );
        candidates.sort((a, b) =>
            (Number(b.pvp?.points) || 1000) - (Number(a.pvp?.points) || 1000) ||
            (Number(b.pvp?.wins) || 0) - (Number(a.pvp?.wins) || 0) ||
            String(a.userId).localeCompare(String(b.userId))
        );
        const rank = candidates.findIndex(other => String(other.userId) === String(player.userId)) + 1;
        const pvp = player.pvp || {};
        const wins = Number(pvp.wins) || 0;
        const points = Number(pvp.points) || 1000;
        const realmIndex = Number(game.realmOf(player.userId)?.index) || 0;
        const maScore = Number(player.maScore) || 0;
        return [
            {
                id: 'nhan_hoang', name: 'Nhân Hoàng', active: !isDemon(player) && wins >= 10 && rank === 1,
                requirement: 'Thắng ít nhất 10 trận PvP và đứng hạng 1 bảng đấu cùng giới.',
                maintain: 'Giữ hạng 1; mất hạng sẽ mất danh hiệu và buff.', buff: '+12% khí huyết, +12% phòng ngự',
            },
            {
                id: 'thien_kieu', name: 'Thiên Kiêu', active: !isDemon(player) && wins >= 5 && realmIndex >= 5 && points >= 1200 && rank <= 10,
                requirement: 'Đạt Nguyên Anh (cảnh giới 5), có 5 trận thắng, 1.200 điểm và lọt top 10.',
                maintain: 'Duy trì cảnh giới, 1.200 điểm và top 10; tụt điều kiện sẽ mất danh hiệu và buff.', buff: '+10% công kích, +10% tốc độ',
            },
            {
                id: 'thien_ma', name: 'Thiên Ma', active: maScore >= 1000,
                requirement: 'Tích lũy ít nhất 1.000 Ma Tính.',
                maintain: 'Giữ Ma Tính từ 1.000 trở lên; dưới ngưỡng sẽ mất danh hiệu và buff.', buff: '+15% công kích, +10% khí huyết',
            },
        ].map(title => ({ ...title, rank: title.id === 'thien_ma' ? 0 : rank }));
    };

    const baseStats = game.stats.bind(game);
    game.stats = (player, now) => {
        const result = baseStats(player, now);
        const titles = getTitleState(player);
        const active = new Set(titles.filter(title => title.active).map(title => title.id));
        const multiply = (key, factor) => { result[key] = Math.round((Number(result[key]) || 0) * factor); };
        if (active.has('nhan_hoang')) { multiply('hp', 1.12); multiply('def', 1.12); }
        if (active.has('thien_kieu')) { multiply('atk', 1.10); multiply('spd', 1.10); }
        if (active.has('thien_ma')) { multiply('atk', 1.15); multiply('hp', 1.10); }
        if (active.size) {
            const realmIndex = Number(result.realmIndex) || 0;
            result.power = Math.round(result.atk * 2 + result.def * 1.5 + result.hp / 10 + result.spd + result.sense + realmIndex * 150 + Math.pow(realmIndex, 2) * 20);
        }
        result.titleBuffs = titles.filter(title => title.active).map(title => title.id);
        return result;
    };

    const baseView = game.view.bind(game);
    game.view = userId => {
        const view = baseView(userId);
        if (view.player) view.player.titles = getTitleState(game.player(userId));
        return view;
    };

    function send(res, status, body) {
        res.writeHead(status, {
            'Cache-Control': 'no-store',
            'X-Content-Type-Options': 'nosniff',
            'Content-Type': 'application/json; charset=utf-8',
        });
        res.end(JSON.stringify(body));
    }

    function limited(key, allowance, windowMs = 60_000) {
        const now = Date.now();
        const hits = (rates.get(key) || []).filter(at => now - at < windowMs);
        hits.push(now);
        rates.set(key, hits);
        if (rates.size > 10_000) {
            for (const [oldKey, times] of rates) if (times.every(at => now - at >= windowMs)) rates.delete(oldKey);
        }
        return hits.length > allowance;
    }

    function readBody(req) {
        return new Promise((resolve, reject) => {
            let size = 0;
            const chunks = [];
            req.on('data', chunk => {
                size += chunk.length;
                if (size > MAX_BODY_BYTES) { reject(Object.assign(new Error('Yêu cầu quá lớn.'), { status: 413 })); req.destroy(); return; }
                chunks.push(chunk);
            });
            req.on('end', () => {
                if (!chunks.length) return resolve({});
                try { resolve(JSON.parse(Buffer.concat(chunks).toString('utf8'))); }
                catch (_) { reject(Object.assign(new Error('Dữ liệu không hợp lệ.'), { status: 400 })); }
            });
            req.on('error', reject);
        });
    }

    function getBearer(req) {
        const header = String(req.headers.authorization || '');
        const match = /^Bearer\s+([A-Za-z0-9_-]{32,256})$/i.exec(header);
        return match?.[1] || '';
    }

    const routes = {
        'GET /api/auth/account/profile': ({ user }) => ({ ...auth.profile(user.id), emailAvailable: gmail.isConfigured(),
            googleAvailable: oauth.isConfigured('google'), facebookAvailable: oauth.isConfigured('facebook') }),
        'POST /api/auth/account/email/link': async ({ user, body }) => {
            if (!gmail.isConfigured()) throw Object.assign(new Error('Liên kết email hiện chưa được mở. Hãy thử lại sau.'), { status: 503 });
            const result = auth.requestEmailLink(user.id, body.email);
            try { await gmail.sendVerificationCode(result.email, result.verificationCode); }
            catch { throw Object.assign(new Error('Không gửi được mã xác minh. Hãy thử lại sau.'), { status: 503 }); }
            return { ok: true, email: result.email, message: 'Đã gửi mã xác minh tới email của bạn.' };
        },
        'POST /api/auth/account/email/verify': ({ user, body }) => auth.verifyEmailLink(user.id, body.email, body.code),
        'GET /api/state': ({ user }) => game.view(user.id),
        'GET /api/map/catalog': () => ({
            maps: C.MAPS,
            towns: C.TOWNS,
            dungeons: C.DUNGEONS.map(({ id, name, icon, townId, realmMin, stamina, desc }) => ({ id, name, icon, townId, realmMin, stamina, desc })),
            monsters: Array.from(C.MONSTER_BY_ID.values()).map(({ id, name, icon, realm, element }) => ({ id, name, icon, realm, element })),
            battleMapSets: getBattleMapSets(),
        }),
        'POST /api/travel': ({ user, body }) => {
            const townId = String(body.toTownId || '');
            const target = C.TOWN_BY_ID.get(townId);
            if (target) requireTownRealm(user.id, townId);
            const travel = game.travel(user.id, townId);
            return { travel, state: game.view(user.id) };
        },
        'POST /api/world/move': ({ user, body }) => ({ ok: true, position: game.moveWorldPosition(user.id, body) }),
        'POST /api/register': ({ user, body }) => {
            const profile = { ...user, first_name: String(body.name || user.first_name).slice(0, 40) };
            const result = game.register(profile, body);
            return { ...game.view(user.id), welcome: { linhCan: result.linhCan?.name, skills: result.skills } };
        },
        'POST /api/breakthrough': ({ user }) => {
            const result = game.breakthrough(user.id);
            return { ...result, state: game.view(user.id), toast: result.message };
        },
        'GET /api/world/monsters': ({ user }) => ({ list: game.getWorldMonsters(user.id) }),
        'POST /api/world/hunt': ({ user, body }) => {
            requireTownRealm(user.id, game.player(user.id)?.town);
            const battle = game.startWorldHunt(user.id, String(body.monsterUid || ''));
            const modeId = pveModeForBattle(battle);
            const view = withBattleMap(battle.view(game.now(), user.id), battle, user.id, modeId);
            return { ...view, state: game.view(user.id) };
        },
        'POST /api/dungeon/enter': ({ user, body }) => {
            const dungeon = C.DUNGEON_BY_ID.get(String(body.dungeonId || ''));
            requireTownRealm(user.id, dungeon?.townId || game.player(user.id)?.town);
            const result = game.startDungeonBattle(user.id, String(body.dungeonId || ''));
            const battle = game.battle(user.id);
            return { ...result, battle: withBattleMap(result.battle, battle, user.id, 'pve_ancient_cave'), state: game.view(user.id) };
        },
        'POST /api/dungeon/next-stage': ({ user }) => {
            const active = game.player(user.id)?.activeDungeon;
            if (!active || Number(active.stageIndex) + 1 >= Number(active.totalStages))
                return { completed: true, message: 'Đã vượt qua toàn bộ bí cảnh.' };
            const result = game.nextDungeonStage(user.id);
            const battle = game.battle(user.id);
            return { ...result, battle: withBattleMap(result.battle, battle, user.id, 'pve_ancient_cave'), state: game.view(user.id) };
        },
        'GET /api/battle/current': ({ user }) => {
            const battle = game.battle(user.id);
            const view = battle ? battle.view(game.now(), user.id) : null;
            return { battle: withBattleMap(view, battle, user.id, battle ? pveModeForBattle(battle) : '') };
        },
        'POST /api/battle/act': ({ user, body }) => {
            const battle = game.battle(user.id);
            if (!battle) throw Object.assign(new GameError('Không có trận đấu đang diễn ra.'), { status: 409 });
            const result = battle.act(game.now(), body, user.id);
            const view = withBattleMap(battle.view(game.now(), user.id), battle, user.id, pveModeForBattle(battle));
            return { result, battle: view, state: game.view(user.id) };
        },
        'GET /api/pvp': ({ user }) => game.pvpList(user.id),
        'POST /api/pvp/fight': ({ user, body }) => {
            const result = game.pvpManualFight(user.id, String(body.targetId || ''));
            const battle = game.pvpManualBattles?.get(String(user.id));
            const modeId = pvpModeForBattle(battle);
            return { ...result, battle: withBattleMap(result.battle, battle, user.id, modeId), state: game.view(user.id) };
        },
        'GET /api/pvp/battle': ({ user }) => {
            const battle = game.pvpManualBattles?.get(String(user.id));
            const view = game.getPvpBattle(user.id);
            return { battle: withBattleMap(view, battle, user.id, pvpModeForBattle(battle)) };
        },
        'POST /api/pvp/action': ({ user, body }) => {
            const instance = game.pvpManualBattles?.get(String(user.id));
            const battle = game.pvpManualAction(user.id, String(body.battleId || ''), String(body.act || 'attack'), body.skillId || null);
            return { battle: withBattleMap(battle, instance, user.id, pvpModeForBattle(instance)), state: game.view(user.id) };
        },
    };

    const server = http.createServer(async (req, res) => {
        const url = new URL(req.url, 'http://local');
        if (req.method === 'GET' && url.pathname === '/health') return send(res, 200, { ok: true, service: 'iosvn-ipa-game-server' });
        if (!url.pathname.startsWith('/api/')) return send(res, 404, { error: 'Không tìm thấy API.' });
        try {
            const body = req.method === 'POST' ? await readBody(req) : {};
            const peer = clientAddress(req);
            const providerCallback = /^\/api\/auth\/link\/(google|facebook)\/callback$/.exec(url.pathname);
            if (providerCallback && req.method === 'GET') {
                let status = 200;
                let message = 'Đã liên kết tài khoản. Hãy quay lại Tu Tiên Giới để tiếp tục.';
                try { await oauth.complete(providerCallback[1], url.searchParams); }
                catch (error) { status = Number(error.status) || 400; message = error.message; }
                const escaped = String(message).replace(/[&<>"']/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[char]));
                res.writeHead(status, { 'Content-Type': 'text/html; charset=utf-8', 'Cache-Control': 'no-store', 'Referrer-Policy': 'no-referrer',
                    'Content-Security-Policy': "default-src 'none'; style-src 'unsafe-inline'", 'X-Content-Type-Options': 'nosniff' });
                return res.end(`<!doctype html><html lang="vi"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Tu Tiên Giới</title><body style="background:#101c24;color:#efdfbf;font:18px system-ui;padding:12vh 8vw;text-align:center"><h1>Tu Tiên Giới</h1><p>${escaped}</p></body></html>`);
            }
            const accountRegistration = url.pathname === '/api/auth/account/register';
            if ((url.pathname === '/api/auth/email/register' || accountRegistration) && req.method === 'POST') {
                if (limited(`signup:${peer}`, 10)) return send(res, 429, { error: 'Thử tạo tài khoản quá nhiều lần. Hãy chờ rồi thử lại.' });
                const identity = accountRegistration ? String(body.identity || '').trim() : body.email;
                if (accountRegistration && !identity.includes('@')) return send(res, 201, await auth.registerUsername(identity, body.password));
                if (!gmail.isConfigured()) return send(res, 503, { ok: false, code: 'email_delivery_not_configured',
                    error: 'Đăng ký email hiện chưa mở. Hãy dùng tên tài khoản hoặc thử lại sau.' });
                const result = await auth.register(identity, body.password);
                try {
                    await gmail.sendVerificationCode(result.email, result.verificationCode);
                } catch (error) {
                    auth.removeUnverified(result.email);
                    logger.error('[IPA mail] Verification email could not be sent:', error.code || 'smtp_error');
                    throw Object.assign(new Error(error.status === 503
                        ? error.message
                        : 'Không gửi được email xác minh. Hãy thử lại sau.'), { status: 503, code: error.code || 'email_delivery_failed' });
                }
                delete result.verificationCode;
                result.message = 'Đã gửi mã xác minh 6 số tới email của bạn.';
                return send(res, 201, result);
            }
            if (url.pathname === '/api/auth/email/verify' && req.method === 'POST') {
                if (limited(`verify:${peer}`, 15, 10 * 60_000)) return send(res, 429, { ok: false, error: 'Thử xác minh quá nhiều lần. Hãy chờ rồi thử lại.' });
                return send(res, 200, auth.verifyEmail(body.email, body.code));
            }
            if (url.pathname === '/api/auth/email/resend' && req.method === 'POST') {
                if (limited(`resend:${peer}`, 5, 10 * 60_000)) return send(res, 429, { ok: false, error: 'Thử gửi mã quá nhiều lần. Hãy chờ rồi thử lại.' });
                const request = auth.createVerificationCode(body.email);
                if (request) {
                    try {
                        await gmail.sendVerificationCode(request.email, request.verificationCode);
                    } catch (error) {
                        logger.error('[IPA mail] Verification email could not be resent:', error.code || 'smtp_error');
                        throw Object.assign(new Error(error.status === 503
                            ? error.message
                            : 'Không gửi được email xác minh. Hãy thử lại sau.'), { status: 503, code: error.code || 'email_delivery_failed' });
                    }
                }
                return send(res, 200, { ok: true, message: 'Nếu email đang chờ xác minh, mã mới đã được gửi.' });
            }
            if ((url.pathname === '/api/auth/email/login' || url.pathname === '/api/auth/account/login') && req.method === 'POST') {
                if (limited(`login:${peer}`, 20)) return send(res, 429, { error: 'Đăng nhập quá nhiều lần. Hãy chờ rồi thử lại.' });
                const result = await auth.login(body.identity ?? body.email, body.password);
                return send(res, 200, result);
            }

            const user = auth.authenticate(getBearer(req));
            if (!user) return send(res, 401, { error: 'Phiên email không hợp lệ hoặc đã hết hạn.' });
            if (limited(`game:${user.id}`, 60, 10_000)) return send(res, 429, { error: 'Thao tác quá nhanh.' });
            const providerStart = /^\/api\/auth\/link\/(google|facebook)\/start$/.exec(url.pathname);
            if (providerStart && req.method === 'POST') {
                if (limited(`link:${user.id}`, 5)) return send(res, 429, { ok: false, error: 'Hãy chờ rồi thử liên kết lại.' });
                return send(res, 200, oauth.start(providerStart[1], user.id, getBearer(req)));
            }
            if (req.method === 'POST' && url.pathname === '/api/auth/logout') {
                auth.revoke(getBearer(req));
                return send(res, 200, { ok: true });
            }
            const handler = routes[`${req.method} ${url.pathname}`];
            if (!handler) return send(res, 404, { error: 'Không có API này.' });
            return send(res, 200, await handler({ user, body }));
        } catch (error) {
            const status = Number(error.status) || (error instanceof GameError ? 400 : 500);
            if (status >= 500) logger.error('[IPA server]', error.stack || error);
            return send(res, status, {
                ok: false,
                error: status >= 500 ? (error.status === 503 ? error.message : 'Máy chủ gặp lỗi.') : error.message,
                code: error.code || '',
                verificationRequired: Boolean(error.verificationRequired),
                email: error.email || '',
            });
        }
    });

    const ticker = setInterval(() => game.tickAll(game.now()), 250);
    ticker.unref?.();
    server.on('error', error => logger.error('[IPA server]', error.message));
    server.listen(port, host, () => logger.log(`[IPA server] Listening on http://${host}:${port}; saves: ${dataDir}`));
    return { server, game, store, auth, close() { clearInterval(ticker); store.flush(); gmail.close(); oauth.clear(); server.close(); } };
}

if (require.main === module) createIpaServer();
module.exports = { createIpaServer, createRealmProgress, clientAddress };
