'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { once } = require('node:events');
const { EmailAuthStore } = require('../email_auth_store');
const { createIpaServer, clientAddress } = require('../ipa_server');
const { createAccountOAuth } = require('../account_oauth');
const password = 'Integration-test-password';

test('forwarded client addresses are accepted only from the configured proxy', () => {
    assert.equal(clientAddress({socket:{remoteAddress:'127.0.0.1'},headers:{'x-forwarded-for':'192.0.2.9'}}), '127.0.0.1');
    assert.equal(clientAddress({socket:{remoteAddress:'::ffff:172.26.2.254'},headers:{'x-forwarded-for':'192.0.2.9'}}), '192.0.2.9');
    assert.equal(clientAddress({socket:{remoteAddress:'172.26.2.254'},headers:{'x-forwarded-for':'invalid'}}), '172.26.2.254');
});

function temporary(t) {
    const directory = fs.mkdtempSync(path.join(os.tmpdir(), 'ipa-account-test-'));
    t.after(() => {
        assert.ok(path.resolve(directory).startsWith(path.resolve(os.tmpdir()) + path.sep));
        assert.ok(path.basename(directory).startsWith('ipa-account-test-'));
        fs.rmSync(directory, { recursive: true, force: true });
    });
    return directory;
}

test('username sessions survive restart, reject wrong passwords, and revoke logout', async t => {
    const file = path.join(temporary(t), 'auth.json');
    let auth = new EmailAuthStore(file);
    const signup = await auth.registerUsername('DaoHuu_01', password);
    const user = auth.authenticate(signup.accessToken);
    assert.equal(user.first_name, 'daohuu_01');
    assert.equal(auth.profile(user.id).emailVerified, false);
    await assert.rejects(auth.registerUsername('DAOHUU_01', password), { status: 409 });
    await assert.rejects(auth.login('daohuu_01', 'incorrect-password'), { status: 401 });
    auth = new EmailAuthStore(file);
    assert.equal(auth.authenticate(signup.accessToken).id, user.id);
    const login = await auth.login('DAOHUU_01', password);
    auth.revoke(login.accessToken);
    assert.equal(auth.authenticate(login.accessToken), null);
    const saved = fs.readFileSync(file, 'utf8');
    assert.ok(!saved.includes(password));
    assert.ok(!saved.includes(signup.accessToken));
});

test('email verification cannot be replayed to log into a verified account', async t => {
    const auth = new EmailAuthStore(path.join(temporary(t), 'auth.json'));
    const pending = await auth.register('first@example.com', password);
    await assert.rejects(auth.login('first@example.com', password), { code: 'email_not_verified' });
    assert.throws(() => auth.verifyEmail('first@example.com', 'wrong'), { status: 400 });
    const verified = auth.verifyEmail('first@example.com', pending.verificationCode);
    assert.ok(auth.authenticate(verified.accessToken));
    assert.throws(() => auth.verifyEmail('first@example.com', pending.verificationCode), { status: 409 });
    assert.throws(() => auth.verifyEmail('first@example.com', ''), { status: 409 });
    assert.ok((await auth.login('first@example.com', password)).accessToken);
});

test('linking verified email preserves the username account and rejects other owners', async t => {
    const auth = new EmailAuthStore(path.join(temporary(t), 'auth.json'));
    const a = auth.authenticate((await auth.registerUsername('first_player', password)).accessToken);
    const b = auth.authenticate((await auth.registerUsername('other_player', password)).accessToken);
    const link = auth.requestEmailLink(a.id, 'linked@example.com');
    assert.throws(() => auth.verifyEmailLink(a.id, link.email, 'wrong'), { status: 400 });
    auth.verifyEmailLink(a.id, link.email, link.verificationCode);
    assert.equal(auth.authenticate((await auth.login(link.email, password)).accessToken).id, a.id);
    assert.throws(() => auth.requestEmailLink(b.id, link.email), { status: 409 });
    assert.throws(() => auth.verifyEmailLink(a.id, link.email, link.verificationCode), { status: 400 });
    auth.linkProvider(a.id, 'facebook', 'provider-subject');
    assert.throws(() => auth.linkProvider(b.id, 'facebook', 'provider-subject'), { status: 409 });
});

test('verification expiry and maximum guesses apply to account email links', async t => {
    let now = Date.now();
    const auth = new EmailAuthStore(path.join(temporary(t), 'auth.json'), { clock: () => now });
    const user = auth.authenticate((await auth.registerUsername('test_player', password)).accessToken);
    const link = auth.requestEmailLink(user.id, 'expiry@example.com');
    for (let i = 0; i < 5; i++) assert.throws(() => auth.verifyEmailLink(user.id, link.email, 'not-digits'), { status: 400 });
    assert.throws(() => auth.verifyEmailLink(user.id, link.email, link.verificationCode), { status: 429 });
    now += 61_000;
    const newer = auth.requestEmailLink(user.id, link.email);
    now += 10 * 60_000;
    assert.throws(() => auth.verifyEmailLink(user.id, newer.email, newer.verificationCode), { status: 400 });
});

test('HTTP registration, protected profile, email delivery and logout use real account routes', async t => {
    const delivered = [];
    const app = createIpaServer({ port: 0, dataDir: temporary(t), oauthEnv: {}, logger: { log() {}, error() {} },
        mailer: { isConfigured: () => true, sendVerificationCode: async (email, code) => delivered.push({email, code}), close() {} } });
    await once(app.server, 'listening');
    t.after(() => app.close());
    const base = `http://127.0.0.1:${app.server.address().port}/api`;
    const request = async (route, body, token) => {
        const res = await fetch(base + route, { method: body ? 'POST' : 'GET',
            headers: { 'Content-Type': 'application/json', ...(token ? {Authorization: `Bearer ${token}`} : {}) },
            ...(body ? {body: JSON.stringify(body)} : {}) });
        return { status: res.status, body: await res.json() };
    };
    assert.equal((await request('/state')).status, 401);
    const signup = await request('/auth/account/register', {identity:'http_player', password});
    assert.equal(signup.status, 201);
    const token = signup.body.accessToken;
    assert.ok(token);
    assert.equal((await request('/state', null, token)).status, 200);
    assert.equal((await request('/auth/account/profile', null, token)).body.username, 'http_player');
    const emailSignup = await request('/auth/account/register', {identity:'mail@example.com', password});
    assert.equal(emailSignup.body.verificationRequired, true);
    assert.equal(emailSignup.body.verificationCode, undefined);
    const verified = await request('/auth/email/verify', {email:'mail@example.com', code:delivered[0].code});
    assert.equal(verified.body.ok, true);
    assert.equal((await request('/auth/email/verify', {email:'mail@example.com', code:delivered[0].code})).status, 409);
    const sent = await request('/auth/account/email/link', {email:'linked@example.com'}, token);
    assert.equal(sent.body.ok, true);
    assert.equal((await request('/auth/account/email/verify', {email:'linked@example.com', code:delivered[1].code}, token)).body.ok, true);
    assert.equal((await request('/auth/link/google/start', {}, token)).status, 503);
    assert.equal((await request('/auth/logout', {}, token)).body.ok, true);
    assert.equal((await request('/state', null, token)).status, 401);
    const login = await request('/auth/account/login', {identity:'http_player', password});
    assert.ok(login.body.accessToken);
});

test('missing Gmail configuration blocks only email signup and stores no unusable account', async t => {
    const app = createIpaServer({port:0, dataDir:temporary(t), oauthEnv:{}, logger:{log(){},error(){}},
        mailer:{isConfigured:()=>false,close(){}}});
    await once(app.server, 'listening');
    t.after(()=>app.close());
    const response = await fetch(`http://127.0.0.1:${app.server.address().port}/api/auth/account/register`,
        {method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({identity:'blocked@example.com',password})});
    assert.equal(response.status,503);
    assert.equal(app.auth.data.accounts['blocked@example.com'],undefined);
    assert.ok((await app.auth.registerUsername('available_user',password)).accessToken);
});

test('OAuth state is one-time, provider-bound and session-bound', async t => {
    const auth = new EmailAuthStore(path.join(temporary(t), 'auth.json'));
    const token = (await auth.registerUsername('provider_player', password)).accessToken;
    const user = auth.authenticate(token);
    const oauth = createAccountOAuth(auth, {IPA_PUBLIC_API_URL:'https://example.com/ipa/api',FACEBOOK_APP_ID:'test-app',FACEBOOK_APP_SECRET:'test-secret',FACEBOOK_GRAPH_VERSION:'v99.0'},
        {fetchImpl:async url=>({ok:true,json:async()=>url.includes('/oauth/access_token') ? {access_token:'test-provider-token'} : {id:'test-subject'}})});
    const start = oauth.start('facebook', user.id, token);
    const state = new URL(start.url).searchParams.get('state');
    await oauth.complete('facebook',new URLSearchParams({state,code:'test-code'}));
    assert.equal(auth.profile(user.id).facebookLinked,true);
    await assert.rejects(oauth.complete('facebook',new URLSearchParams({state,code:'test-code'})),{status:400});
    const revoked = new URL(oauth.start('facebook',user.id,token).url).searchParams.get('state');
    auth.revoke(token);
    await assert.rejects(oauth.complete('facebook',new URLSearchParams({state:revoked,code:'test-code'})),{status:401});
});
