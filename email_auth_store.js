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
        this.data = { version: 3, accounts: {}, sessions: {}, emailLinks: {}, emailLinkRequests: {} };
        this._load();
    }

    _load() {
        if (!fs.existsSync(this.filePath)) return;
        const parsed = JSON.parse(fs.readFileSync(this.filePath, 'utf8'));
        if (!parsed || typeof parsed !== 'object' || ![1, 2, 3].includes(parsed.version)) throw new Error('Unsupported IPA auth data version.');
        this.data.accounts = parsed.accounts && typeof parsed.accounts === 'object' ? parsed.accounts : {};
        this.data.sessions = parsed.sessions && typeof parsed.sessions === 'object' ? parsed.sessions : {};
        this.data.emailLinks = parsed.emailLinks || {};
        this.data.emailLinkRequests = parsed.emailLinkRequests || {};
        this.data.version = 3;
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
        if (this._findIdentity(cleanEmail)) throw authError('Email này đã có tài khoản.', 409);

        const id = crypto.randomBytes(18).toString('base64url');
        const salt = crypto.randomBytes(16);
        const derived = await scrypt(password, salt, 64, { N: 1 << 15, r: 8, p: 1, maxmem: 64 * 1024 * 1024 });
        if (this._findIdentity(cleanEmail)) throw authError('Email này đã có tài khoản.', 409);
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

    async registerUsername(username, password) {
        const clean = normalizeUsername(username);
        validatePassword(password);
        const key = `username:${clean}`;
        if (this.data.accounts[key]) throw authError('Tên tài khoản này đã được sử dụng.', 409);
        const salt = crypto.randomBytes(16);
        const derived = await scrypt(password, salt, 64, { N: 1 << 15, r: 8, p: 1, maxmem: 64 * 1024 * 1024 });
        if (this.data.accounts[key]) throw authError('Tên tài khoản này đã được sử dụng.', 409);
        const account = {
            id: crypto.randomBytes(18).toString('base64url'), username: clean, email: '', emailVerified: false,
            passwordSalt: salt.toString('base64url'), passwordHash: Buffer.from(derived).toString('base64url'),
            createdAt: new Date(this.clock()).toISOString(), providers: {},
        };
        this.data.accounts[key] = account;
        return this._newSession(account);
    }

    _findIdentity(identity) {
        const value = String(identity || '').trim().toLowerCase();
        const key = value.includes('@') ? (this.data.emailLinks[value] || value) : `username:${value}`;
        return Object.hasOwn(this.data.accounts, key) ? this.data.accounts[key] : null;
    }

    _accountForUser(userId) {
        const id = String(userId || '').replace(/^ipa_/, '');
        const account = Object.values(this.data.accounts).find(row => row.id === id);
        if (!account) throw authError('Phiên đăng nhập không hợp lệ.', 401);
        return account;
    }

    profile(userId) {
        const account = this._accountForUser(userId);
        return { ok: true, username: account.username || '', email: account.email || '', emailVerified: account.emailVerified === true,
            googleLinked: Boolean(account.providers?.google), facebookLinked: Boolean(account.providers?.facebook) };
    }

    requestEmailLink(userId, email) {
        const account = this._accountForUser(userId);
        const clean = normalizeEmail(email);
        const existing = this._findIdentity(clean);
        if (existing && existing.id !== account.id) throw authError('Email này đã thuộc một tài khoản khác.', 409);
        const previous = this.data.emailLinkRequests[account.id];
        if (previous && this.clock() - previous.sentAt < VERIFICATION_RESEND_COOLDOWN_MS)
            throw authError('Hãy chờ một phút rồi gửi lại mã.', 429);
        const code = makeVerificationCode();
        this.data.emailLinkRequests[account.id] = { email: clean, codeHash: hashVerificationCode(account.id, code),
            expiresAt: this.clock() + VERIFICATION_TTL_MS, attempts: 0, sentAt: this.clock() };
        this._save();
        return { email: clean, verificationCode: code };
    }

    verifyEmailLink(userId, email, code) {
        const account = this._accountForUser(userId);
        const clean = normalizeEmail(email);
        const pending = this.data.emailLinkRequests[account.id];
        if (!pending || pending.email !== clean || pending.expiresAt <= this.clock()) throw authError('Mã đã hết hạn. Hãy yêu cầu mã mới.');
        if (pending.attempts >= MAX_VERIFICATION_ATTEMPTS) throw authError('Đã nhập sai mã quá nhiều lần. Hãy yêu cầu mã mới.', 429);
        const actual = Buffer.from(hashVerificationCode(account.id, String(code || '').trim()), 'hex');
        const expected = Buffer.from(pending.codeHash, 'hex');
        if (!/^\d{6}$/.test(String(code || '').trim()) || !crypto.timingSafeEqual(actual, expected)) {
            pending.attempts++;
            this._save();
            throw authError('Mã xác minh chưa đúng.');
        }
        this._linkVerifiedEmail(account, clean);
        delete this.data.emailLinkRequests[account.id];
        this._save();
        return { ok: true, message: 'Đã liên kết email với hồ sơ hiện tại.' };
    }

    _linkVerifiedEmail(account, email) {
        const existing = this._findIdentity(email);
        if (existing && existing.id !== account.id) throw authError('Email này đã thuộc một tài khoản khác.', 409);
        if (account.email && account.email !== email) throw authError('Tài khoản đã có email khác. Hãy dùng email đã liên kết.', 409);
        account.email = email;
        account.emailVerified = true;
        this.data.emailLinks[email] = account.username ? `username:${account.username}` : account.email;
    }

    linkProvider(userId, provider, subject, verifiedEmail = '') {
        if (!['google', 'facebook'].includes(provider) || !subject) throw authError('Nhà cung cấp không hợp lệ.');
        const account = this._accountForUser(userId);
        for (const other of Object.values(this.data.accounts))
            if (other.id !== account.id && other.providers?.[provider] === subject) throw authError('Tài khoản này đã liên kết với hồ sơ khác.', 409);
        if (account.providers?.[provider] && account.providers[provider] !== subject) throw authError('Hồ sơ đã liên kết với một tài khoản khác của dịch vụ này.', 409);
        if (verifiedEmail) this._linkVerifiedEmail(account, normalizeEmail(verifiedEmail));
        account.providers ||= {};
        account.providers[provider] = subject;
        this._save();
        return { ok: true };
    }

    async login(email, password) {
        validatePassword(password);
        const account = this._findIdentity(email);
        const salt = account ? Buffer.from(account.passwordSalt, 'base64url') : Buffer.alloc(16, 7);
        const expected = account ? Buffer.from(account.passwordHash, 'base64url') : Buffer.alloc(64, 3);
        const actual = Buffer.from(await scrypt(String(password || ''), salt, 64, { N: 1 << 15, r: 8, p: 1, maxmem: 64 * 1024 * 1024 }));
        const match = actual.length === expected.length && crypto.timingSafeEqual(actual, expected);
        if (!account || !match) throw authError('Email hoặc mật khẩu chưa đúng.', 401);
        if (!account.username && account.emailVerified !== true) {
            const error = authError('Email chưa được xác minh. Nhập mã đã gửi tới hộp thư Gmail.', 403, 'email_not_verified');
            error.email = account.email;
            error.verificationRequired = true;
            throw error;
        }
        return this._newSession(account);
    }

    verifyEmail(email, code) {
        const cleanEmail = normalizeEmail(email);
        const account = this.data.accounts[cleanEmail];
        if (!account) throw authError('Email hoặc mã xác minh chưa đúng.', 400);
        if (account.emailVerified === true) throw authError('Email đã được xác minh. Hãy đăng nhập bằng mật khẩu.', 409);
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
        const account = this.data.accounts[session.accountKey || session.email];
        if (!account || account.id !== session.accountId) return null;
        return { id: `ipa_${account.id}`, email: account.email || '', first_name: account.username || account.email.split('@')[0] };
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
        this.data.sessions[hashToken(accessToken)] = { accountId: account.id, accountKey: account.username ? `username:${account.username}` : account.email, expiresAt };
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

function normalizeUsername(username) {
    const value = String(username || '').trim().toLowerCase();
    if (!/^[a-z0-9][a-z0-9_.]{2,23}$/.test(value)) throw authError('Tên tài khoản cần 3–24 ký tự: chữ không dấu, số, dấu chấm hoặc gạch dưới.');
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
