// Lưu dữ liệu game vào game_data.json, tách khỏi reputation_data.json để mỗi
// lượt đánh không phải ghi lại cả file uy tín. Ghi gộp (debounce) và atomic:
// file tạm -> fsync -> rename, nên tắt máy giữa chừng không làm hỏng file.
'use strict';
const fs = require('fs');
const path = require('path');

const SAVE_DEBOUNCE_MS = 2000;

function emptyData() {
    return {
        version: 1,
        players: {},          // userId -> nhân vật
        stock: { items: {}, skills: {} }, // số bản đang tồn tại của món/kỹ năng có giới hạn
        punished: {},         // userId -> { at, reason, snapshot }
        ledger: {},           // 'YYYY-MM-DD' -> { in, out }
        meta: { createdAt: new Date().toISOString() },
    };
}

class GameStore {
    constructor(filePath) {
        this.filePath = path.resolve(filePath);
        this.data = emptyData();
        this._timer = null;
        this._load();
        const flush = () => { if (this._timer) { try { this.flush(); } catch (_) {} } };
        process.once('exit', flush);
    }

    _load() {
        if (!fs.existsSync(this.filePath)) return;
        const raw = fs.readFileSync(this.filePath, 'utf8');
        const parsed = JSON.parse(raw); // lỗi thì dừng hẳn, không ghi đè dữ liệu hỏng
        const base = emptyData();
        this.data = { ...base, ...parsed, stock: { ...base.stock, ...(parsed.stock || {}) } };
        for (const key of ['players', 'punished', 'ledger', 'meta']) {
            if (!this.data[key] || typeof this.data[key] !== 'object') this.data[key] = base[key];
        }
    }

    save() {
        if (this._timer) return;
        this._timer = setTimeout(() => { this._timer = null; this.flush(); }, SAVE_DEBOUNCE_MS);
        this._timer.unref?.();
    }

    flush() {
        if (this._timer) { clearTimeout(this._timer); this._timer = null; }
        const dir = path.dirname(this.filePath);
        if (!fs.existsSync(dir)) return;
        const tmp = `${this.filePath}.tmp`;
        const fd = fs.openSync(tmp, 'w', 0o600);
        try {
            fs.writeSync(fd, JSON.stringify(this.data));
            fs.fsyncSync(fd);
        } finally {
            fs.closeSync(fd);
        }
        fs.renameSync(tmp, this.filePath);
    }
}

module.exports = { GameStore, emptyData };
