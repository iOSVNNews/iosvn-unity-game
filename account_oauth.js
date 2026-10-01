'use strict';

const crypto = require('node:crypto');
const { OAuth2Client } = require('google-auth-library');

function createAccountOAuth(auth, env = process.env, { clock = Date.now, fetchImpl = fetch } = {}) {
    const pending = new Map();
    const base = String(env.IPA_PUBLIC_API_URL || '').trim().replace(/\/$/, '');
    const secureBase = /^https:\/\/[^\s?#]+$/.test(base);
    const facebookVersion = String(env.FACEBOOK_GRAPH_VERSION || '').trim();
    const configured = {
        google: Boolean(secureBase && env.GOOGLE_CLIENT_ID && env.GOOGLE_CLIENT_SECRET),
        facebook: Boolean(secureBase && env.FACEBOOK_APP_ID && env.FACEBOOK_APP_SECRET && /^v\d+\.\d+$/.test(facebookVersion)),
    };
    const callback = provider => `${base}/auth/link/${provider}/callback`;
    const google = configured.google ? new OAuth2Client(env.GOOGLE_CLIENT_ID, env.GOOGLE_CLIENT_SECRET, callback('google')) : null;
    const error = (message, status = 400) => Object.assign(new Error(message), { status });

    async function jsonRequest(url, options = {}) {
        try {
            const result = await fetchImpl(url, { ...options, signal: AbortSignal.timeout(12000) });
            const json = await result.json();
            if (!result.ok || json.error) throw new Error('provider_failed');
            return json;
        } catch { throw error('Dịch vụ liên kết chưa phản hồi. Hãy quay lại game và thử lại.', 502); }
    }

    return {
        isConfigured: provider => configured[provider] === true,
        start(provider, userId, sessionToken) {
            if (!configured[provider]) throw error('Dịch vụ liên kết này chưa được mở. Bạn vẫn có thể dùng tài khoản và mật khẩu.', 503);
            for (const [key, row] of pending) if (row.expiresAt <= clock()) pending.delete(key);
            if (pending.size >= 5000) throw error('Có quá nhiều yêu cầu. Hãy thử lại sau.', 503);
            const state = crypto.randomBytes(32).toString('base64url');
            const verifier = crypto.randomBytes(48).toString('base64url');
            const nonce = crypto.randomBytes(24).toString('base64url');
            pending.set(state, { provider, userId, sessionToken, verifier, nonce, expiresAt: clock() + 10 * 60_000 });
            let url;
            if (provider === 'google') {
                url = google.generateAuthUrl({ scope: ['openid', 'email'], state, nonce, prompt: 'select_account',
                    code_challenge_method: 'S256', code_challenge: crypto.createHash('sha256').update(verifier).digest('base64url') });
            } else {
                const query = new URLSearchParams({ client_id: env.FACEBOOK_APP_ID, redirect_uri: callback(provider), state,
                    response_type: 'code', scope: 'public_profile' });
                url = `https://www.facebook.com/${facebookVersion}/dialog/oauth?${query}`;
            }
            return { ok: true, url };
        },
        async complete(provider, query) {
            const state = String(query.get('state') || '');
            const row = pending.get(state);
            pending.delete(state);
            if (!row || row.provider !== provider || row.expiresAt <= clock()) throw error('Yêu cầu liên kết đã hết hạn. Hãy mở lại từ game.');
            const user = auth.authenticate(row.sessionToken);
            if (!user || user.id !== row.userId) throw error('Phiên game đã hết hạn. Hãy đăng nhập lại.', 401);
            if (query.get('error')) throw error('Bạn đã hủy liên kết. Hồ sơ trong game vẫn được giữ nguyên.');
            const code = String(query.get('code') || '');
            if (!code || code.length > 4096) throw error('Không nhận được mã xác thực từ dịch vụ.');
            if (provider === 'google') {
                let payload;
                try {
                    const { tokens } = await google.getToken({ code, codeVerifier: row.verifier });
                    const ticket = await google.verifyIdToken({ idToken: tokens.id_token, audience: env.GOOGLE_CLIENT_ID });
                    payload = ticket.getPayload();
                } catch { throw error('Không xác thực được tài khoản Google. Hãy thử lại từ game.', 502); }
                if (!payload?.sub || payload.nonce !== row.nonce || payload.email_verified !== true) throw error('Google chưa xác minh email của tài khoản này.');
                return auth.linkProvider(user.id, 'google', payload.sub, payload.email);
            }
            const queryToken = new URLSearchParams({ client_id: env.FACEBOOK_APP_ID, client_secret: env.FACEBOOK_APP_SECRET,
                redirect_uri: callback(provider), code });
            const tokens = await jsonRequest(`https://graph.facebook.com/${facebookVersion}/oauth/access_token?${queryToken}`);
            if (!tokens.access_token) throw error('Facebook chưa cấp quyền liên kết.');
            const proof = crypto.createHmac('sha256', env.FACEBOOK_APP_SECRET).update(tokens.access_token).digest('hex');
            const identity = await jsonRequest(`https://graph.facebook.com/${facebookVersion}/me?fields=id&appsecret_proof=${proof}`,
                { headers: { Authorization: `Bearer ${tokens.access_token}` } });
            if (!identity.id) throw error('Facebook chưa trả thông tin tài khoản.');
            return auth.linkProvider(user.id, 'facebook', String(identity.id));
        },
        clear() { pending.clear(); },
    };
}

module.exports = { createAccountOAuth };
