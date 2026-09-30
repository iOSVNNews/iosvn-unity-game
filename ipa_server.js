'use strict';

// Standalone IPA game server. This process is separate from the Telegram bot
// server (server.js) and uses a separate player save file.
const fs = require('fs');
const path = require('path');
const http = require('http');
const { Game, GameError } = require('./ipa_core/engine');
const { GameStore } = require('./ipa_core/store');
const C = require('./ipa_core/catalog');
const { EmailAuthStore } = require('./email_auth_store');

const ROOT = __dirname;
const DATA_DIR = path.resolve(process.env.IPA_DATA_DIR || path.join(ROOT, 'server_data'));
const MAX_BODY_BYTES = 32 * 1024;

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

function createIpaServer({ port = Number(process.env.IPA_PORT || 8788), host = process.env.IPA_HOST || '127.0.0.1', logger = console } = {}) {
    fs.mkdirSync(DATA_DIR, { recursive: true });
    const store = new GameStore(path.join(DATA_DIR, 'ipa_game_data.json'));
    const auth = new EmailAuthStore(path.join(DATA_DIR, 'ipa_auth_data.json'));
    const realms = createRealmProgress(store);
    const game = new Game({ store, realms });
    const rates = new Map();

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
        'GET /api/state': ({ user }) => game.view(user.id),
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
            const battle = game.startWorldHunt(user.id, String(body.monsterUid || ''));
            return { ...battle.view(game.now(), user.id), state: game.view(user.id) };
        },
        'GET /api/battle/current': ({ user }) => {
            const battle = game.battle(user.id);
            return { battle: battle ? battle.view(game.now(), user.id) : null };
        },
        'POST /api/battle/act': ({ user, body }) => {
            const battle = game.battle(user.id);
            if (!battle) throw Object.assign(new GameError('Không có trận đấu đang diễn ra.'), { status: 409 });
            const result = battle.act(game.now(), body, user.id);
            return { result, battle: battle.view(game.now(), user.id), state: game.view(user.id) };
        },
    };

    const server = http.createServer(async (req, res) => {
        const url = new URL(req.url, 'http://local');
        if (req.method === 'GET' && url.pathname === '/health') return send(res, 200, { ok: true, service: 'iosvn-ipa-game-server' });
        if (!url.pathname.startsWith('/api/')) return send(res, 404, { error: 'Không tìm thấy API.' });
        try {
            const body = req.method === 'POST' ? await readBody(req) : {};
            const peer = req.socket.remoteAddress || 'unknown';
            if (url.pathname === '/api/auth/email/register' && req.method === 'POST') {
                if (limited(`signup:${peer}`, 10)) return send(res, 429, { error: 'Thử tạo tài khoản quá nhiều lần. Hãy chờ rồi thử lại.' });
                const result = await auth.register(body.email, body.password);
                return send(res, 201, result);
            }
            if (url.pathname === '/api/auth/email/login' && req.method === 'POST') {
                if (limited(`login:${peer}`, 20)) return send(res, 429, { error: 'Đăng nhập quá nhiều lần. Hãy chờ rồi thử lại.' });
                const result = await auth.login(body.email, body.password);
                return send(res, 200, result);
            }

            const user = auth.authenticate(getBearer(req));
            if (!user) return send(res, 401, { error: 'Phiên email không hợp lệ hoặc đã hết hạn.' });
            if (limited(`game:${user.id}`, 60, 10_000)) return send(res, 429, { error: 'Thao tác quá nhanh.' });
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
            return send(res, status, { error: status >= 500 ? 'Máy chủ gặp lỗi.' : error.message });
        }
    });

    const ticker = setInterval(() => game.tickAll(game.now()), 250);
    ticker.unref?.();
    server.on('error', error => logger.error('[IPA server]', error.message));
    server.listen(port, host, () => logger.log(`[IPA server] Listening on http://${host}:${port}; saves: ${DATA_DIR}`));
    return { server, game, store, auth, close() { clearInterval(ticker); store.flush(); server.close(); } };
}

if (require.main === module) createIpaServer();
module.exports = { createIpaServer, createRealmProgress };
