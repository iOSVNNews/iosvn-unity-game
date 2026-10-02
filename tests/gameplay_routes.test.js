'use strict';

// Smoke-tests every gameplay route of the IPA server against a real engine:
// GET views must answer 200 and POST actions must fail cleanly (4xx) or succeed,
// never crash with a 500.
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { once } = require('node:events');
const { createIpaServer } = require('../ipa_server');

const password = 'Integration-test-password';
const noisyLogger = { log() {}, warn() {}, error() {} };

async function startServer(t) {
    const dataDir = fs.mkdtempSync(path.join(os.tmpdir(), 'ipa-routes-test-'));
    const app = createIpaServer({ port: 0, dataDir, oauthEnv: {}, logger: noisyLogger,
        mailer: { isConfigured: () => false, close() {} } });
    await once(app.server, 'listening');
    t.after(() => { app.close(); fs.rmSync(dataDir, { recursive: true, force: true }); });
    const base = `http://127.0.0.1:${app.server.address().port}`;
    const request = async (method, route, body, token) => {
        const res = await fetch(base + route, { method,
            headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
            ...(method === 'POST' ? { body: JSON.stringify(body || {}) } : {}) });
        return { status: res.status, body: await res.json() };
    };
    return { app, request };
}

async function registeredPlayer(request, name = 'route_tester') {
    const signup = await request('POST', '/api/auth/account/register', { identity: name, password });
    assert.equal(signup.status, 201, JSON.stringify(signup.body));
    const token = signup.body.accessToken;
    const created = await request('POST', '/api/register', { gender: 'nam', mon: 'kiem', he: 'kim', name: 'Lăng Vân',
        appearance: 'bach_van', talents: ['dao_the', 'kiem_tam', 'tu_linh'] }, token);
    assert.equal(created.status, 200, JSON.stringify(created.body));
    return token;
}

test('every gameplay GET view answers without a server error', async t => {
    const { app, request } = await startServer(t);
    const token = await registeredPlayer(request);
    const getRoutes = Object.keys(app.routes).filter(key => key.startsWith('GET ') && !key.includes('/auth/'));
    assert.ok(getRoutes.length >= 30, `expected the full gameplay surface, got ${getRoutes.length} GET routes`);
    for (const key of getRoutes) {
        const route = key.slice(4);
        const res = await request('GET', route, null, token);
        assert.ok(res.status < 500, `${key} -> ${res.status} ${JSON.stringify(res.body).slice(0, 200)}`);
        assert.equal(res.status, 200, `${key} -> ${res.status} ${JSON.stringify(res.body).slice(0, 200)}`);
    }
});

test('every gameplay POST action rejects bad input cleanly instead of crashing', async t => {
    const { app, request } = await startServer(t);
    const token = await registeredPlayer(request);
    const postRoutes = Object.keys(app.routes).filter(key => key.startsWith('POST ') && !key.includes('/auth/') &&
        key !== 'POST /api/register' && key !== 'POST /api/reset-mon');
    assert.ok(postRoutes.length >= 80, `expected the full action surface, got ${postRoutes.length} POST routes`);
    const failures = [];
    for (const key of postRoutes) {
        const res = await request('POST', key.slice(5), {}, token);
        if (res.status >= 500) failures.push(`${key} -> ${res.status} ${res.body.error}`);
    }
    assert.deepEqual(failures, []);
});

test('core loops work end to end: bag, shop, heal, sect, party, inbox, social', async t => {
    const { app, request } = await startServer(t);
    const token = await registeredPlayer(request, 'loop_tester');
    const state = (await request('GET', '/api/state', null, token)).body;
    assert.ok(state.player, 'player view');
    assert.ok(Array.isArray(state.player.bag), 'bag listing present');
    assert.ok(state.player.equip && Array.isArray(state.player.skills), 'equipment and skills present');

    const heal = await request('POST', '/api/town/heal', {}, token);
    assert.ok(heal.status < 500);

    const party = await request('POST', '/api/party/create', {}, token);
    assert.equal(party.status, 200, JSON.stringify(party.body));
    assert.ok(party.body.party && party.body.state);
    const left = await request('POST', '/api/party/leave', {}, token);
    assert.equal(left.status, 200);

    const inbox = await request('GET', '/api/inbox', null, token);
    assert.equal(inbox.status, 200);
    assert.ok(Array.isArray(inbox.body.list));

    const social = await request('GET', '/api/social', null, token);
    assert.equal(social.status, 200);

    const crafting = await request('GET', '/api/crafting', null, token);
    assert.equal(crafting.status, 200);

    const market = await request('GET', '/api/market', null, token);
    assert.equal(market.status, 200);

    const sect = await request('GET', '/api/sect', null, token);
    assert.equal(sect.status, 200);

    const battle = await request('GET', '/api/battle/any', null, token);
    assert.equal(battle.status, 200);
    assert.equal(battle.body.battleKind, null);
});
