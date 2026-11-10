// Generates the built-in "misspelled" signal sounds (WAV) and the app icons (ICO) for Linter.
// Everything is synthesized here, so the assets carry no third-party licence.
// Usage: node tools/gen-assets.js [projectDir]      (default: src/Linter)
'use strict';
const fs = require('fs');
const path = require('path');
const zlib = require('zlib');

const projectDir = process.argv[2] || path.join(__dirname, '..', 'src', 'Linter');
const soundsDir = path.join(projectDir, 'Sounds');
const assetsDir = path.join(projectDir, 'Assets');
fs.mkdirSync(soundsDir, { recursive: true });
fs.mkdirSync(assetsDir, { recursive: true });

// ---------------------------------------------------------------- audio
// The signal fires while the user is typing, so every sound is short
// (well under half a second) and has a soft attack — no startling clicks.
const SR = 44100;
const TAU = Math.PI * 2;

function render(duration, fn) {
  const n = Math.floor(duration * SR);
  const out = new Float64Array(n);
  for (let i = 0; i < n; i++) out[i] = fn(i / SR);
  return out;
}

// Linear attack, exponential decay, and a short fade so the tail never clicks.
function env(t, attack, decay, end) {
  const a = attack > 0 && t < attack ? t / attack : 1;
  let v = a * Math.exp(-decay * t);
  if (end && t > end - 0.01) v *= Math.max(0, (end - t) / 0.01);
  return v;
}

function mix(parts) {
  let len = 0;
  for (const p of parts) len = Math.max(len, Math.floor(p.at * SR) + p.data.length);
  const out = new Float64Array(len);
  for (const p of parts) {
    const off = Math.floor(p.at * SR);
    for (let i = 0; i < p.data.length; i++) out[off + i] += p.data[i];
  }
  return out;
}

function writeWav(name, data, level = 0.7) {
  let peak = 0;
  for (const v of data) peak = Math.max(peak, Math.abs(v));
  const gain = peak > 0 ? level / peak : 1;
  const buf = Buffer.alloc(44 + data.length * 2);
  buf.write('RIFF', 0); buf.writeUInt32LE(36 + data.length * 2, 4); buf.write('WAVE', 8);
  buf.write('fmt ', 12); buf.writeUInt32LE(16, 16); buf.writeUInt16LE(1, 20); buf.writeUInt16LE(1, 22);
  buf.writeUInt32LE(SR, 24); buf.writeUInt32LE(SR * 2, 28); buf.writeUInt16LE(2, 32); buf.writeUInt16LE(16, 34);
  buf.write('data', 36); buf.writeUInt32LE(data.length * 2, 40);
  for (let i = 0; i < data.length; i++) {
    const s = Math.max(-1, Math.min(1, data[i] * gain));
    buf.writeInt16LE(Math.round(s * 32767), 44 + i * 2);
  }
  fs.writeFileSync(path.join(soundsDir, name), buf);
  console.log('wrote', name, (data.length / SR).toFixed(2) + 's');
}

function tone(f, dur, decay, harmonics = []) {
  return render(dur, t => {
    let v = Math.sin(TAU * f * t);
    for (const [mul, amp] of harmonics) v += amp * Math.sin(TAU * f * mul * t);
    return v * env(t, 0.004, decay, dur);
  });
}

// Tick: a tiny high click, the least intrusive option.
writeWav('tick.wav', tone(1900, 0.07, 55, [[2, 0.3]]), 0.55);

// Blip: one short mid-range note.
writeWav('blip.wav', tone(880, 0.12, 22, [[2, 0.25]]));

// Pop: a quick downward sweep, like a bubble.
writeWav('pop.wav', render(0.11, t => {
  const f0 = 720, f1 = 240, k = t / 0.11;
  const phase = TAU * (f0 * t + (f1 - f0) * t * k / 2);
  return Math.sin(phase) * env(t, 0.003, 26, 0.11);
}));

// Knock: a low woody thud.
writeWav('knock.wav', tone(196, 0.16, 30, [[2.4, 0.5], [5.1, 0.2]]));

// Double: two quick blips — reads clearly as "something is wrong".
writeWav('double.wav', mix([
  { at: 0.00, data: tone(660, 0.08, 28, [[2, 0.25]]) },
  { at: 0.11, data: tone(660, 0.08, 28, [[2, 0.25]]) },
]));

// Drop: two descending notes.
writeWav('drop.wav', mix([
  { at: 0.00, data: tone(784, 0.11, 22, [[2, 0.2]]) },
  { at: 0.10, data: tone(523.25, 0.16, 16, [[2, 0.2]]) },
]));

// Buzz: a low, slightly rough "error" tone.
writeWav('buzz.wav', render(0.18, t => {
  const f = 155;
  const v = Math.sin(TAU * f * t) + 0.5 * Math.sin(TAU * f * 3 * t) + 0.3 * Math.sin(TAU * f * 5 * t);
  return v * Math.min(1, t / 0.008, (0.18 - t) / 0.02);
}), 0.6);

// Ding: a brighter, longer bell-like hit.
writeWav('ding.wav', tone(1318.5, 0.45, 9, [[2, 0.3], [2.76, 0.15]]));

// ---------------------------------------------------------------- icon
const crcTable = new Int32Array(256);
for (let n = 0; n < 256; n++) {
  let c = n;
  for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
  crcTable[n] = c;
}
function crc32(buf) {
  let c = -1;
  for (let i = 0; i < buf.length; i++) c = crcTable[(c ^ buf[i]) & 0xff] ^ (c >>> 8);
  return (c ^ -1) >>> 0;
}
function chunk(type, data) {
  const len = Buffer.alloc(4); len.writeUInt32BE(data.length);
  const td = Buffer.concat([Buffer.from(type, 'ascii'), data]);
  const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(td));
  return Buffer.concat([len, td, crc]);
}
function png(w, h, rgba) {
  const raw = Buffer.alloc((w * 4 + 1) * h);
  for (let y = 0; y < h; y++) {
    raw[y * (w * 4 + 1)] = 0;
    rgba.copy(raw, y * (w * 4 + 1) + 1, y * w * 4, (y + 1) * w * 4);
  }
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4);
  ihdr[8] = 8; ihdr[9] = 6; ihdr[10] = 0; ihdr[11] = 0; ihdr[12] = 0;
  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    chunk('IHDR', ihdr),
    chunk('IDAT', zlib.deflateSync(raw, { level: 9 })),
    chunk('IEND', Buffer.alloc(0)),
  ]);
}

// Geometry in unit coordinates (0..1): a rounded tile, a capital "A" built
// from three round-capped strokes, and the spell-checker's wavy underline.
function distToSegment(px, py, ax, ay, bx, by) {
  const dx = bx - ax, dy = by - ay;
  const t = Math.max(0, Math.min(1, ((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy)));
  return Math.hypot(px - (ax + t * dx), py - (ay + t * dy));
}

const LETTER = [
  [0.50, 0.17, 0.27, 0.66],
  [0.50, 0.17, 0.73, 0.66],
  [0.36, 0.50, 0.64, 0.50],
];
const WAVE = [];
{
  const steps = 48, x0 = 0.20, x1 = 0.80;
  let prev = null;
  for (let i = 0; i <= steps; i++) {
    const x = x0 + (x1 - x0) * i / steps;
    const y = 0.835 + 0.038 * Math.sin(TAU * (x - x0) / 0.2);
    if (prev) WAVE.push([prev[0], prev[1], x, y]);
    prev = [x, y];
  }
}

const PALETTES = {
  on:  { top: [45, 55, 82],   bottom: [24, 30, 48],   letter: [255, 255, 255], wave: [255, 82, 82] },
  off: { top: [120, 124, 132], bottom: [92, 96, 104], letter: [214, 217, 222], wave: [160, 164, 172] },
};

function drawIcon(size, palette) {
  const ss = 4; // supersampling
  const rgba = Buffer.alloc(size * size * 4);
  // Small sizes get heavier strokes so the glyph survives at 16 px.
  const small = size <= 24;
  const letterW = small ? 0.15 : 0.115;
  const waveW = small ? 0.11 : 0.075;
  const inset = 0.03, radius = 0.22;

  function sample(u, v) {
    // Rounded-rectangle tile.
    const qx = Math.abs(u - 0.5) - (0.5 - inset - radius);
    const qy = Math.abs(v - 0.5) - (0.5 - inset - radius);
    const outside = Math.hypot(Math.max(qx, 0), Math.max(qy, 0)) + Math.min(Math.max(qx, qy), 0) - radius;
    if (outside > 0) return null;

    for (const s of WAVE) {
      if (distToSegment(u, v, s[0], s[1], s[2], s[3]) <= waveW / 2) return palette.wave;
    }
    for (const s of LETTER) {
      if (distToSegment(u, v, s[0], s[1], s[2], s[3]) <= letterW / 2) return palette.letter;
    }
    return [0, 1, 2].map(i => palette.top[i] + (palette.bottom[i] - palette.top[i]) * v);
  }

  for (let py = 0; py < size; py++) {
    for (let px = 0; px < size; px++) {
      let r = 0, g = 0, b = 0, a = 0;
      for (let sy = 0; sy < ss; sy++) for (let sx = 0; sx < ss; sx++) {
        const c = sample((px + (sx + 0.5) / ss) / size, (py + (sy + 0.5) / ss) / size);
        if (c) { r += c[0]; g += c[1]; b += c[2]; a += 1; }
      }
      const n = ss * ss;
      const i = (py * size + px) * 4;
      if (a > 0) { rgba[i] = r / a; rgba[i + 1] = g / a; rgba[i + 2] = b / a; rgba[i + 3] = 255 * a / n; }
    }
  }
  return png(size, size, rgba);
}

function writeIco(name, palette) {
  const sizes = [256, 64, 48, 32, 24, 16];
  const images = sizes.map(s => ({ size: s, data: drawIcon(s, palette) }));
  const header = Buffer.alloc(6);
  header.writeUInt16LE(0, 0); header.writeUInt16LE(1, 2); header.writeUInt16LE(images.length, 4);
  const entries = [];
  let offset = 6 + 16 * images.length;
  for (const img of images) {
    const e = Buffer.alloc(16);
    e[0] = img.size >= 256 ? 0 : img.size;
    e[1] = img.size >= 256 ? 0 : img.size;
    e[2] = 0; e[3] = 0;
    e.writeUInt16LE(1, 4); e.writeUInt16LE(32, 6);
    e.writeUInt32LE(img.data.length, 8); e.writeUInt32LE(offset, 12);
    offset += img.data.length;
    entries.push(e);
  }
  fs.writeFileSync(path.join(assetsDir, name + '.ico'), Buffer.concat([header, ...entries, ...images.map(i => i.data)]));
  fs.writeFileSync(path.join(assetsDir, name + '-256.png'), images[0].data);
  console.log('wrote', name + '.ico');
}

writeIco('app', PALETTES.on);
writeIco('app-off', PALETTES.off);
// The grey variant is only needed inside the .ico.
fs.rmSync(path.join(assetsDir, 'app-off-256.png'));
