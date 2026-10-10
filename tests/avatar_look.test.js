'use strict';

// The layered QCBH-style avatar: the creator's look string is sanitised by the server, stored on
// the player, returned in the state view, and can be changed later.
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { once } = require('node:events');
const { createIpaServer } = require('../ipa_server');

const password = 'Integration-test-password';
const quiet = { log() {}, warn() {}, error() {} };

async function startServer(t) {
    const dataDir = fs.mkdtempSync(path.join(os.tmpdir(), 'ipa-look-test-'));
    const app = createIpaServer({ port: 0, dataDir, oauthEnv: {}, logger: quiet, mailer: { isConfigured: () => false, close() {} } });
    await once(app.server, 'listening');
    t.after(() => { app.close(); fs.rmSync(dataDir, { recursive: true, force: true }); });
    const base = `http://127.0.0.1:${app.server.address().port}`;
    return async (method, route, body, token) => {
        const res = await fetch(base + route, { method,
            headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
            ...(method === 'POST' ? { body: JSON.stringify(body || {}) } : {}) });
        return { status: res.status, body: await res.json() };
    };
}

const LOOK = 'g=m;bo=3;fa=1;ea=2;ey=0;ec=#3a8f7a;br=0;no=1;mo=3;bd=2;ha=0;hc=#1e1a1e;ti=0;tc=#e8e2d4;to=1;oc=#2f5f63;ac=#c8a050;' +
    'pa=0;pc=#303038;sh=1;sc=#2a2a30;be=1;bc=#20242a;hat=1;hac=#c8a050;sk=#f0d2b4;wp=2;au=5;auc=#8fe0ff;evil=1;wp2=9';

test('creator look with aura and accent colours is stored, sanitised and editable', async t => {
    const request = await startServer(t);
    const signup = await request('POST', '/api/auth/account/register', { identity: 'look_tester', password });
    assert.equal(signup.status, 201, JSON.stringify(signup.body));
    const token = signup.body.accessToken;
    const created = await request('POST', '/api/register', { gender: 'nu', mon: 'kiem', he: 'kim', name: 'Lăng Sương',
        appearance: 'thanh_ngoc', talents: ['dao_the', 'kiem_tam', 'tu_linh'], look: LOOK }, token);
    assert.equal(created.status, 200, JSON.stringify(created.body));
    const state = await request('GET', '/api/state', null, token);
    const look = state.body.player?.look || state.body.state?.player?.look || JSON.stringify(state.body).match(/"look":"([^"]+)"/)?.[1];
    assert.ok(look, 'state exposes the stored look');
    assert.match(look, /^g=f;/, 'gender comes from the character, not the string');
    assert.match(look, /au=5/);
    assert.match(look, /bo=3/, 'the chosen body shape survives creation');
    assert.match(look, /sk=#f0d2b4/, 'the chosen skin tone survives creation');
    assert.match(look, /wp=2/);
    assert.match(look, /ac=#c8a050/);
    assert.match(look, /auc=#8fe0ff/);
    assert.match(look, /bd=0/, 'female characters never keep a beard');
    assert.doesNotMatch(look, /evil|wp2/, 'unknown keys are dropped');
    const changed = await request('POST', '/api/player/look', { look: LOOK.replace('au=5', 'au=2').replace('auc=#8fe0ff', 'auc=#ff8a3a') }, token);
    assert.ok(changed.status < 300, JSON.stringify(changed.body));
    const after = await request('GET', '/api/state', null, token);
    assert.match(JSON.stringify(after.body), /bo=3/);
    assert.match(JSON.stringify(after.body), /au=2[^"]*auc=#ff8a3a/);
    const bad = await request('POST', '/api/player/look', { look: 'g=m;au=99' }, token);
    assert.ok(bad.status < 500);
});

test('illustrated presets and expanded outfit options survive server storage', async t => {
    const request = await startServer(t);
    const signup = await request('POST', '/api/auth/account/register', { identity: 'look_preset_tester', password });
    assert.equal(signup.status, 201, JSON.stringify(signup.body));
    const token = signup.body.accessToken;
    const created = await request('POST', '/api/register', { gender: 'nam', mon: 'kiem', he: 'kim', name: 'Thanh Phong',
        appearance: 'thanh_ngoc', talents: ['dao_the', 'kiem_tam', 'tu_linh'],
        look: 'g=m;preset=9;to=5;tot=5;wp=10;au=5;auc=#8fe0ff' }, token);
    assert.equal(created.status, 200, JSON.stringify(created.body));
    const state = await request('GET', '/api/state', null, token);
    const look = state.body.player?.look || state.body.state?.player?.look || JSON.stringify(state.body).match(/"look":"([^"]+)"/)?.[1];
    assert.ok(look, 'state exposes the stored look');
    assert.match(look, /preset=9/);
    assert.match(look, /tot=5/);
    assert.match(look, /wp=10/);

    const changed = await request('POST', '/api/player/look', { look: 'g=m;preset=0;tot=1;wp=4;au=1;auc=#bfe8ff' }, token);
    assert.ok(changed.status < 300, JSON.stringify(changed.body));
    const after = await request('GET', '/api/state', null, token);
    assert.match(JSON.stringify(after.body), /preset=0[^\"]*tot=1[^\"]*wp=4/);
});

test('layered figure keys: forehead mark, mark colour and feature sliders', async t => {
    const request = await startServer(t);
    const signup = await request('POST', '/api/auth/account/register', { identity: 'look_layered_tester', password });
    assert.equal(signup.status, 201, JSON.stringify(signup.body));
    const token = signup.body.accessToken;
    const look = 'g=m;preset=-1;fa=2;ey=7;br=2;no=1;mo=4;bd=3;ha=8;to=3;hat=2;ma=1;wp=4;au=2;ez=14;es=6;eh=12;bh=9;nh=11;mh=8;' +
        'hc=#1e1a1e;sk=#f0d2b4;ec=#3a2a24;oc=#7a2a3a;tc=#c8a050;pc=#2a2022;sc=#221a1c;bc=#c8a050;hac=#c8a050;mc=#e0a030;auc=#ff8a3a;ez2=3';
    const created = await request('POST', '/api/register', { gender: 'nam', mon: 'kiem', he: 'kim', name: 'Xích Viêm',
        appearance: 'thanh_ngoc', talents: ['dao_the', 'kiem_tam', 'tu_linh'], look }, token);
    assert.equal(created.status, 200, JSON.stringify(created.body));
    const state = await request('GET', '/api/state', null, token);
    const stored = state.body.player?.look || state.body.state?.player?.look || JSON.stringify(state.body).match(/"look":"([^"]+)"/)?.[1];
    for (const part of ['ma=1', 'ez=14', 'es=6', 'eh=12', 'bh=9', 'nh=11', 'mh=8', 'mc=#e0a030', 'to=3', 'bd=3'])
        assert.ok(stored.includes(part), part + ' kept in ' + stored);
    assert.doesNotMatch(stored, /ez2/);
    const bad = await request('POST', '/api/player/look', { look: 'g=m;ez=99;ma=12' }, token);
    assert.ok(bad.status < 500);
});
