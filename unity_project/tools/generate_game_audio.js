'use strict';

// Creates the three original, loopable audio clips used by the IPA prototype.
// No downloaded or third-party samples are included.
const fs = require('fs');
const path = require('path');

const output = path.resolve(__dirname, '..', 'Assets', 'Resources', 'Audio');
const sampleRate = 22050;

function midi(note) { return 440 * Math.pow(2, (note - 69) / 12); }
function clamp(value) { return Math.max(-0.96, Math.min(0.96, value)); }

function writeWav(name, samples) {
    const dataBytes = samples.length * 2;
    const header = Buffer.alloc(44);
    header.write('RIFF', 0);
    header.writeUInt32LE(36 + dataBytes, 4);
    header.write('WAVE', 8);
    header.write('fmt ', 12);
    header.writeUInt32LE(16, 16);
    header.writeUInt16LE(1, 20);
    header.writeUInt16LE(1, 22);
    header.writeUInt32LE(sampleRate, 24);
    header.writeUInt32LE(sampleRate * 2, 28);
    header.writeUInt16LE(2, 32);
    header.writeUInt16LE(16, 34);
    header.write('data', 36);
    header.writeUInt32LE(dataBytes, 40);
    const pcm = Buffer.alloc(dataBytes);
    for (let i = 0; i < samples.length; i++) pcm.writeInt16LE(Math.round(clamp(samples[i]) * 32767), i * 2);
    const file = path.join(output, name);
    fs.mkdirSync(path.dirname(file), { recursive: true });
    fs.writeFileSync(file, Buffer.concat([header, pcm]));
    console.log(`${file} (${(fs.statSync(file).size / 1024).toFixed(0)} KB)`);
}

function pluck(t, start, hz, length, color = 1) {
    const local = t - start;
    if (local < 0 || local >= length) return 0;
    const attack = Math.min(1, local / 0.008);
    const decay = Math.exp(-local * (2.6 + color * 1.5));
    const tone = Math.sin(2 * Math.PI * hz * local) +
        0.32 * Math.sin(2 * Math.PI * hz * 2.01 * local) * Math.exp(-local * 5.5) +
        0.12 * Math.sin(2 * Math.PI * hz * 3.97 * local) * Math.exp(-local * 9);
    return tone * attack * decay * 0.13;
}

function createRealmMusic({ seconds, bpm, root, airy, seed }) {
    const length = Math.round(seconds * sampleRate);
    const beat = 60 / bpm;
    const bar = beat * 4;
    const pentatonic = [0, 2, 4, 7, 9];
    let randomSeed = seed >>> 0;
    const random = () => {
        randomSeed = (1664525 * randomSeed + 1013904223) >>> 0;
        return randomSeed / 4294967296;
    };
    const events = [];
    const chordDegrees = airy ? [0, 2, 4, 1, 3, 0, 4, 2] : [0, 3, 4, 2, 0, 4, 3, 1];
    for (let measure = 0; measure < Math.ceil(seconds / bar); measure++) {
        const start = measure * bar;
        const chordRoot = root + pentatonic[chordDegrees[measure % chordDegrees.length]];
        if (airy) {
            events.push({ at: start, note: chordRoot + 12, length: bar * 1.1, kind: 'bell' });
            events.push({ at: start + beat * 2, note: chordRoot + 19, length: bar * 0.95, kind: 'bell' });
            events.push({ at: start + beat * (measure % 2 ? 1 : 3), note: chordRoot + 24 + pentatonic[Math.floor(random() * 5)], length: beat * 2.5, kind: 'bell' });
        } else {
            events.push({ at: start, note: chordRoot - 12, length: beat * 2.4, kind: 'pluck' });
            events.push({ at: start + beat * 2, note: chordRoot, length: beat * 2.2, kind: 'pluck' });
        }
        const rhythm = airy ? [0.5, 1.75, 3.0] : [0.5, 1.25, 2.5, 3.25];
        for (let r = 0; r < rhythm.length; r++) {
            const at = start + beat * rhythm[r] + (random() - 0.5) * beat * 0.035;
            const degree = pentatonic[Math.floor(random() * pentatonic.length)];
            events.push({ at, note: root + 12 + degree + (random() < 0.24 ? 12 : 0), length: airy ? beat * 2 : beat * 1.35, kind: airy ? 'bell' : 'pluck' });
        }
    }

    const out = new Float32Array(length);
    for (let i = 0; i < length; i++) {
        const t = i / sampleRate;
        let sample = 0;
        for (const event of events) {
            const local = t - event.at;
            if (local < 0 || local >= event.length) continue;
            const hz = midi(event.note);
            if (event.kind === 'pluck') sample += pluck(t, event.at, hz, event.length, 0.7);
            else {
                const attack = Math.min(1, local / 0.35);
                const release = Math.min(1, (event.length - local) / 0.55);
                const env = Math.max(0, Math.min(attack, release)) * Math.exp(-local * 0.5);
                const shimmer = 1 + 0.012 * Math.sin(2 * Math.PI * 0.31 * t);
                sample += (Math.sin(2 * Math.PI * hz * local) + 0.18 * Math.sin(2 * Math.PI * hz * 2.76 * local)) * env * shimmer * 0.055;
            }
        }
        const lowHz = midi(root - (airy ? 12 : 24));
        const slowMod = 0.5 + 0.5 * Math.sin(2 * Math.PI * (airy ? 0.055 : 0.085) * t);
        sample += Math.sin(2 * Math.PI * lowHz * t) * (airy ? 0.018 : 0.025) * (0.72 + slowMod * 0.28);
        if (airy) {
            sample += Math.sin(2 * Math.PI * midi(root + 7) * t + 0.16 * Math.sin(t * 0.7)) * 0.014;
            sample += (random() - 0.5) * 0.0022 * (0.25 + slowMod * 0.75);
        }
        const edge = Math.min(1, t / 0.12, (seconds - t) / 0.12);
        out[i] = sample * Math.max(0, edge) * (airy ? 0.88 : 1);
    }
    return out;
}

function createSkillEffect() {
    const seconds = 1.15;
    const length = Math.round(seconds * sampleRate);
    const out = new Float32Array(length);
    let seed = 77123;
    const random = () => {
        seed = (1103515245 * seed + 12345) & 0x7fffffff;
        return seed / 0x7fffffff;
    };
    let filteredNoise = 0;
    for (let i = 0; i < length; i++) {
        const t = i / sampleRate;
        const progress = t / seconds;
        const noise = random() * 2 - 1;
        filteredNoise = filteredNoise * 0.84 + noise * 0.16;
        const sweepHz = 180 + progress * progress * 720;
        const sweep = Math.sin(2 * Math.PI * sweepHz * t) * Math.exp(-t * 2.6);
        const impactEnv = t < 0.055 ? Math.exp(-t * 55) : 0;
        const impact = Math.sin(2 * Math.PI * (82 - t * 360) * t) * impactEnv;
        const shimmer = Math.sin(2 * Math.PI * (760 + progress * 1500) * t) * Math.exp(-Math.max(0, t - 0.12) * 4.8) * 0.13;
        const whoosh = filteredNoise * Math.sin(Math.PI * Math.max(0, progress)) * Math.exp(-t * 1.8) * 0.32;
        out[i] = sweep * 0.31 + impact * 0.45 + shimmer + whoosh;
    }
    return out;
}

writeWav('Effects/SkillEffect.wav', createSkillEffect());
writeWav('Music/PhamGioiTheme.wav', createRealmMusic({ seconds: 20, bpm: 96, root: 50, airy: false, seed: 9201 }));
writeWav('Music/TienGioiTheme.wav', createRealmMusic({ seconds: 24, bpm: 80, root: 57, airy: true, seed: 27182 }));
