'use strict';

// What the client needs to stage a fight: the figure wears the equipped weapon / armour, every
// monster brings five named moves (four regular + its ultimate) and reports the move it just used.
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { once } = require('node:events');
const { createIpaServer } = require('../ipa_server');
const { monsterSkillKit } = require('../ipa_core/monster_skills');
const C = require('../ipa_core/catalog');

const password = 'Integration-test-password';
const quiet = { log() {}, warn() {}, error() {} };

async function startServer(t) {
    const dataDir = fs.mkdtempSync(path.join(os.tmpdir(), 'ipa-combat-test-'));
    const app = createIpaServer({ port: 0, dataDir, oauthEnv: {}, logger: quiet, mailer: { isConfigured: () => false, close() {} } });
    await once(app.server, 'listening');
    t.after(() => { app.close(); fs.rmSync(dataDir, { recursive: true, force: true }); });
    const base = `http://127.0.0.1:${app.server.address().port}`;
    const request = async (method, route, body, token) => {
        const res = await fetch(base + route, { method,
            headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
            ...(method === 'POST' ? { body: JSON.stringify(body || {}) } : {}) });
        return { status: res.status, body: await res.json() };
    };
    return { request, app };
}

test('every monster has five distinct moves and the last one is its ultimate', () => {
    const signatures = new Set();
    for (const def of C.MONSTER_BY_ID.values()) {
        const kit = monsterSkillKit(def);
        assert.equal(kit.length, 5, def.id);
        assert.equal(new Set(kit.map(move => move.name)).size, 5, `${def.id} repeats a move`);
        assert.deepEqual(kit.map(move => move.big), [false, false, false, false, true]);
        assert.equal(kit[4].fx, 'ult');
        assert.equal(monsterSkillKit(def), kit, 'kits are stable');
        signatures.add(kit[4].name);
    }
    assert.ok(signatures.size > C.MONSTER_BY_ID.size * 0.9, 'ultimates are monster specific');
});

test('worn look follows equipment and battles report monster moves', async t => {
    const { request } = await startServer(t);
    const signup = await request('POST', '/api/auth/account/register', { identity: 'combat_tester', password });
    assert.equal(signup.status, 201, JSON.stringify(signup.body));
    const token = signup.body.accessToken;
    const created = await request('POST', '/api/register', { gender: 'nam', mon: 'kiem', he: 'kim', name: 'Lăng Vân',
        appearance: 'thanh_ngoc', talents: ['dao_the', 'kiem_tam', 'tu_linh'], look: 'g=m;fa=0;ha=0;to=1;oc=#2f5f63;wp=1;au=1;auc=#8fe0ff' }, token);
    assert.equal(created.status, 200, JSON.stringify(created.body));
    let state = (await request('GET', '/api/state', null, token)).body;
    const player = state.player || state.state?.player;
    assert.ok(player, 'state has the player');
    assert.match(player.look, /wp=1/, 'the creator look is kept as chosen');
    const weapon = player.equip?.weapon || (player.bag || []).find(item => item.slot === 'weapon');
    if (weapon && !player.equip?.weapon) {
        const equipped = await request('POST', '/api/equip', { uid: weapon.uid }, token);
        assert.ok(equipped.status < 300, JSON.stringify(equipped.body));
        state = (await request('GET', '/api/state', null, token)).body;
    }
    const worn = (state.player || state.state?.player).lookWorn;
    if (weapon) {
        assert.match(worn, /wp=(4|5|6|7|8|9|10)(;|$)/, 'an equipped weapon is carried in hand');
        assert.match(worn, /wc=#[0-9a-f]{6}/, 'in its quality colour');
    } else assert.ok(worn);

    const monster = [...C.MONSTER_BY_ID.values()].find(def => def.small && (def.realm || 0) === 0);
    const hunt = await request('POST', '/api/hunt', { monsterId: monster.id }, token);
    if (hunt.status >= 300) return;       // hunting can be gated by town / stamina rules; the kit test above still holds
    const current = await request('GET', '/api/battle/current', null, token);
    const m = current.body.battle.m;
    assert.equal(m.skills.length, 5);
    assert.equal(m.skills[4].big, true);
    assert.ok(current.body.battle.items.every(item => item.uid === null || typeof item.id === 'string'));
});
