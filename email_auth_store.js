'use strict';

const crypto = require('crypto');
const fs = require('fs');
const path = require('path');
const { promisify } = require('util');

const scrypt = promisify(crypto.scrypt);
const SESSION_TTL_MS = 7 * 24 * 60 * 60 * 1000;

class EmailAuthStore {
    constructor(filePath, { clock = Date.now } = {}) {
        this.filePath = path.resolve(filePath);
        this.clock = clock;
        this.data = { version: 1, accounts: {}, sessions: {} };
        this._load();
    }

    _load() {
        if (!fs.existsSync(this.filePath)) return;
        const parsed = JSON.parse(fs.readFileSync(this.filePath, 'utf8'));
        if (!parsed || typeof parsed !== 'object' || parsed.version !== 1) throw new Error('Unsupported IPA auth data version.');
        this.data.accounts = parsed.accounts && typeof parsed.accounts === 'object' ? parsed.accounts : {};
        this.data.sessions = parsed.sessions && typeof parsed.sessions === 'object' ? parsed.sessions : {};
    }

    _save() {
        const dir = path.dirname(this.filePath);
        fs.mkdirSync(dir, { recursive: true });
        const temp = `${this.filePath}.${process.pid}.tmp`;
        const fd = fs.openSync(temp, 'w', 0o600);
        try {
            fs.writeSync(fd, JSON.stringify(this.data));
            fs.fsyncSync(fd);
        } finally {
            fs.closeSync(fd);
        }
        fs.renameSync(temp, this.filePath);
    }

    async register(email, password) {
        const cleanEmail = normalizeEmail(email);
        validatePassword(password);
        if (this.data.accounts[cleanEmail]) throw authError('Email này đã có tài khoản.', 409);

        const id = crypto.randomBytes(18).toString('base64url');
        const salt = crypto.randomBytes(16);
        const derived = await scrypt(password, salt, 64, { N: 1 << 15, r: 8, p: 1, maxmem: 64 * 1024 * 1024 });
        if (this.data.accounts[cleanEmail]) throw authError('Email này đã có tài khoản.', 409);
        this.data.accounts[cleanEmail] = {
            id,
            email: cleanEmail,
            passwordSalt: salt.toString('base64url'),
            passwordHash: Buffer.from(derived).toString('base64url'),
            createdAt: new Date(this.clock()).toISOString(),
        };
        return this._newSession(this.data.accounts[cleanEmail]);
    }

    async login(email, password) {
        validatePassword(password);
        let cleanEmail;
        try { cleanEmail = normalizeEmail(email); } catch (_) { cleanEmail = ''; }
        const account = this.data.accounts[cleanEmail];
        const salt = account ? Buffer.from(account.passwordSalt, 'base64url') : Buffer.alloc(16, 7);
        const expected = account ? Buffer.from(account.passwordHash, 'base64url') : Buffer.alloc(64, 3);
        const actual = Buffer.from(await scrypt(String(password || ''), salt, 64, { N: 1 << 15, r: 8, p: 1, maxmem: 64 * 1024 * 1024 }));
        const match = actual.length === expected.length && crypto.timingSafeEqual(actual, expected);
        if (!account || !match) throw authError('Email hoặc mật khẩu chưa đúng.', 401);
        return this._newSession(account);
    }

    authenticate(token) {
        if (typeof token !== 'string' || token.length < 32 || token.length > 256) return null;
        const hash = hashToken(token);
        const session = this.data.sessions[hash];
        if (!session) return null;
        if (Number(session.expiresAt) <= this.clock()) {
            delete this.data.sessions[hash];
            this._save();
            return null;
        }
        const account = this.data.accounts[session.email];
        if (!account || account.id !== session.accountId) return null;
        return { id: `ipa_${account.id}`, email: account.email, first_name: account.email.split('@')[0] };
    }

    revoke(token) {
        if (typeof token !== 'string') return false;
        const hash = hashToken(token);
        if (!this.data.sessions[hash]) return false;
        delete this.data.sessions[hash];
        this._save();
        return true;
    }

    _newSession(account) {
        const accessToken = crypto.randomBytes(32).toString('base64url');
        const expiresAt = this.clock() + SESSION_TTL_MS;
        this.data.sessions[hashToken(accessToken)] = { accountId: account.id, email: account.email, expiresAt };
        this._pruneSessions();
        this._save();
        return { ok: true, accessToken, expiresAt };
    }

    _pruneSessions() {
        const now = this.clock();
        for (const [hash, session] of Object.entries(this.data.sessions))
            if (Number(session.expiresAt) <= now) delete this.data.sessions[hash];
    }
}

function normalizeEmail(email) {
    const value = String(email || '').trim().toLowerCase();
    if (value.length > 254 || !/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(value)) throw authError('Hãy nhập địa chỉ email hợp lệ.');
    return value;
}

function validatePassword(password) {
    const value = String(password || '');
    if (value.length < 10 || value.length > 128) throw authError('Mật khẩu cần có từ 10 đến 128 ký tự.');
}

function hashToken(token) { return crypto.createHash('sha256').update(token).digest('hex'); }
function authError(message, status = 400) { return Object.assign(new Error(message), { status }); }

module.exports = { EmailAuthStore, normalizeEmail, validatePassword, SESSION_TTL_MS };
