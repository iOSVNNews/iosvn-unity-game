'use strict';

const crypto = require('crypto');
const fs = require('fs');
const path = require('path');
const { promisify } = require('util');

const scrypt = promisify(crypto.scrypt);
const SESSION_TTL_MS = 7 * 24 * 60 * 60 * 1000;
const VERIFICATION_TTL_MS = 10 * 60 * 1000;
const VERIFICATION_RESEND_COOLDOWN_MS = 60 * 1000;
const MAX_VERIFICATION_ATTEMPTS = 5;

class EmailAuthStore {
    constructor(filePath, { clock = Date.now } = {}) {
        this.filePath = path.resolve(filePath);
        this.clock = clock;
        this.data = { version: 2, accounts: {}, sessions: {} };
        this._load();
    }

    _load() {
        if (!fs.existsSync(this.filePath)) return;
        const parsed = JSON.parse(fs.readFileSync(this.filePath, 'utf8'));
        if (!parsed || typeof parsed !== 'object' || ![1, 2].includes(parsed.version)) throw new Error('Unsupported IPA auth data version.');
        this.data.accounts = parsed.accounts && typeof parsed.accounts === 'object' ? parsed.accounts : {};
        this.data.sessions = parsed.sessions && typeof parsed.sessions === 'object' ? parsed.sessions : {};
        this.data.version = 2;
        if (parsed.version === 1) {
            for (const account of Object.values(this.data.accounts)) account.emailVerified = true;
            this._save();
        }
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
        const verificationCode = makeVerificationCode();
        const now = this.clock();
        this.data.accounts[cleanEmail] = {
            id,
            email: cleanEmail,
            passwordSalt: salt.toString('base64url'),
            passwordHash: Buffer.from(derived).toString('base64url'),
            createdAt: new Date(this.clock()).toISOString(),
            emailVerified: false,
            verificationCodeHash: hashVerificationCode(id, verificationCode),
            verificationExpiresAt: now + VERIFICATION_TTL_MS,
            verificationAttempts: 0,
            verificationSentAt: now,
        };
        this._save();
        return { ok: true, email: cleanEmail, verificationRequired: true, verificationCode };
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
        if (account.emailVerified !== true) {
            const error = authError('Email chưa được xác minh. Nhập mã đã gửi tới hộp thư Gmail.', 403, 'email_not_verified');
            error.email = cleanEmail;
            error.verificationRequired = true;
            throw error;
        }
        return this._newSession(account);
    }

    verifyEmail(email, code) {
        const cleanEmail = normalizeEmail(email);
        const account = this.data.accounts[cleanEmail];
        if (!account) throw authError('Email hoặc mã xác minh chưa đúng.', 400);
        if (account.emailVerified === true) return this._newSession(account);
        if (Number(account.verificationExpiresAt) <= this.clock()) {
            clearVerification(account);
            this._save();
            throw authError('Mã xác minh đã hết hạn. Hãy gửi mã mới.', 400, 'verification_expired');
        }
        if (Number(account.verificationAttempts) >= MAX_VERIFICATION_ATTEMPTS)
            throw authError('Bạn đã nhập sai mã quá số lần cho phép. Hãy gửi mã mới.', 429, 'verification_attempts_exceeded');
        const suppliedCode = String(code || '').trim();
        if (!/^\d{6}$/.test(suppliedCode)) throw authError('Mã xác minh phải gồm 6 chữ số.');
        const expected = Buffer.from(String(account.verificationCodeHash || ''), 'hex');
        const actual = Buffer.from(hashVerificationCode(account.id, suppliedCode), 'hex');
        const matches = expected.length === actual.length && crypto.timingSafeEqual(expected, actual);
        if (!matches) {
            account.verificationAttempts = (Number(account.verificationAttempts) || 0) + 1;
            this._save();
            throw authError('Mã xác minh chưa đúng.', 400, 'verification_code_invalid');
        }
        account.emailVerified = true;
        clearVerification(account);
        this._save();
        return this._newSession(account);
    }

    createVerificationCode(email) {
        let cleanEmail;
        try { cleanEmail = normalizeEmail(email); } catch (_) { return null; }
        const account = this.data.accounts[cleanEmail];
        if (!account || account.emailVerified === true) return null;
        const now = this.clock();
        const waitMs = VERIFICATION_RESEND_COOLDOWN_MS - (now - Number(account.verificationSentAt || 0));
        if (waitMs > 0) throw authError(`Hãy chờ ${Math.ceil(waitMs / 1000)} giây rồi gửi lại mã.`, 429, 'verification_resend_limited');
        const verificationCode = makeVerificationCode();
        account.verificationCodeHash = hashVerificationCode(account.id, verificationCode);
        account.verificationExpiresAt = now + VERIFICATION_TTL_MS;
        account.verificationAttempts = 0;
        account.verificationSentAt = now;
        this._save();
        return { email: cleanEmail, verificationCode };
    }

    removeUnverified(email) {
        let cleanEmail;
        try { cleanEmail = normalizeEmail(email); } catch (_) { return false; }
        const account = this.data.accounts[cleanEmail];
        if (!account || account.emailVerified === true) return false;
        delete this.data.accounts[cleanEmail];
        this._save();
        return true;
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
function makeVerificationCode() { return String(crypto.randomInt(0, 1_000_000)).padStart(6, '0'); }
function hashVerificationCode(accountId, code) { return crypto.createHash('sha256').update(`${accountId}:${code}`).digest('hex'); }
function clearVerification(account) {
    delete account.verificationCodeHash;
    delete account.verificationExpiresAt;
    delete account.verificationAttempts;
    delete account.verificationSentAt;
}
function authError(message, status = 400, code = '') { return Object.assign(new Error(message), { status, code }); }

module.exports = { EmailAuthStore, normalizeEmail, validatePassword, SESSION_TTL_MS };
