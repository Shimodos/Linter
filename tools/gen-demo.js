// Generates docs/demo.svg — the animated "wrong layout → one key → fixed" picture
// at the top of the README. GitHub shows SVG files as images, where scripts do not
// run, so the animation is plain CSS: every distinct line of text is its own <text>
// element that is switched on for its slice of the loop.
// Usage: node tools/gen-demo.js
'use strict';
const fs = require('fs');
const path = require('path');

const WRONG = 'ghbdtn? rfr ltkf'; // "привет, как дела" typed with the English layout on
const RIGHT = 'привет, как дела';

// ---------------------------------------------------------------- timeline
// Each frame: what the text line shows, whether the key is held, the caption, and for how long (ms).
const frames = [];
const add = (text, ms, { key = false, note = '' } = {}) => frames.push({ text, ms, key, note });

const TYPING = 'Typed with the wrong layout on…';
for (let i = 1; i <= WRONG.length; i++) add(WRONG.slice(0, i), 105, { note: TYPING });
add(WRONG, 900, { note: TYPING });
add(WRONG, 320, { key: true, note: TYPING });
for (const n of [12, 8, 4, 0]) add(WRONG.slice(0, n), 55);
for (let i = 1; i <= RIGHT.length; i++) add(RIGHT.slice(0, i), 40);
add(RIGHT, 2600, { note: 'Retyped in Russian. The layout is switched too.' });
add(RIGHT, 320, { key: true, note: 'Retyped in Russian. The layout is switched too.' });
add(WRONG, 1900, { note: 'Pressed again: undone.' });
add(WRONG, 320, { key: true, note: 'Pressed again: undone.' });
add(RIGHT, 2800, { note: 'Retyped in Russian. The layout is switched too.' });

const total = frames.reduce((sum, f) => sum + f.ms, 0);

// ---------------------------------------------------------------- keyframes
/** Collects, per distinct value, the [start, end] slices (in % of the loop) during which it is shown. */
function slices(pick) {
  const map = new Map();
  let t = 0;
  for (const f of frames) {
    const value = pick(f);
    const start = t;
    t += f.ms;
    if (value === '' || value === false) continue;
    const list = map.get(value) ?? [];
    const last = list[list.length - 1];
    if (last && last[1] === start) last[1] = t; // merge back-to-back slices
    else list.push([start, t]);
    map.set(value, list);
  }
  return map;
}

const pct = (ms) => `${((ms / total) * 100).toFixed(3)}%`;

/** step-end keyframes: invisible, except inside the given slices. */
function keyframes(name, list, on = 'opacity:1', off = 'opacity:0') {
  const stops = [];
  if (list[0][0] !== 0) stops.push(`0%{${off}}`);
  for (const [start, end] of list) {
    stops.push(`${pct(start)}{${on}}`);
    if (end < total) stops.push(`${pct(end)}{${off}}`);
  }
  return `@keyframes ${name}{${stops.join('')}}`;
}

const xml = (s) => s.replace(/[&<>"]/g, (ch) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[ch]);

// ---------------------------------------------------------------- svg
const W = 760, H = 250;
const css = [];
const body = [];
const animated = (name) => `animation:${name} ${total}ms step-end infinite`;

let n = 0;
const lastText = frames[frames.length - 1].text;
for (const [text, list] of slices((f) => f.text)) {
  const name = `t${n++}`;
  css.push(keyframes(name, list));
  body.push(`<text class="line${text === lastText ? ' still' : ''}" x="40" y="128" style="${animated(name)}">${xml(text)}</text>`);
}

const lastNote = frames[frames.length - 1].note;
for (const [note, list] of slices((f) => f.note)) {
  const name = `n${n++}`;
  css.push(keyframes(name, list));
  body.push(`<text class="note${note === lastNote ? ' still' : ''}" x="150" y="213" style="${animated(name)}">${xml(note)}</text>`);
}

const keyDown = slices((f) => f.key).get(true);
css.push(keyframes('keyfill', keyDown, 'fill:#5b8def', 'fill:#262d44'));
css.push(keyframes('keytext', keyDown, 'fill:#ffffff', 'fill:#c9cfdd'));

const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${W} ${H}" width="${W}" height="${H}" role="img" aria-label="Animation: the text “${WRONG}” is typed with the wrong keyboard layout, Right Alt is pressed, and it becomes “${RIGHT}”. A second press undoes it.">
<style>
.line{font:26px Consolas,"Cascadia Mono","DejaVu Sans Mono",Menlo,monospace;fill:#fff;opacity:0;white-space:pre}
.note{font:15px "Segoe UI",system-ui,-apple-system,sans-serif;fill:#9aa3b8;opacity:0}
.title{font:600 13px "Segoe UI",system-ui,-apple-system,sans-serif;fill:#9aa3b8}
.keycap{fill:#262d44;animation:keyfill ${total}ms step-end infinite}
.keylabel{font:600 13px "Segoe UI",system-ui,-apple-system,sans-serif;fill:#c9cfdd;animation:keytext ${total}ms step-end infinite}
${css.join('\n')}
@media (prefers-reduced-motion:reduce){
.line,.note,.keycap,.keylabel{animation:none!important}
.still{opacity:1}
}
</style>
<rect width="${W}" height="${H}" rx="16" fill="#181e30"/>
<rect x="0.5" y="0.5" width="${W - 1}" height="${H - 1}" rx="15.5" fill="none" stroke="#2c3550"/>
<circle cx="30" cy="30" r="5.5" fill="#39425e"/><circle cx="50" cy="30" r="5.5" fill="#39425e"/><circle cx="70" cy="30" r="5.5" fill="#39425e"/>
<text class="title" x="92" y="34.5">Any app you type in</text>
<line x1="0" y1="58" x2="${W}" y2="58" stroke="#2c3550"/>
${body.filter((b) => b.includes('class="line')).join('\n')}
<line x1="0" y1="178" x2="${W}" y2="178" stroke="#2c3550"/>
<rect class="keycap" x="40" y="194" width="92" height="30" rx="7" stroke="#39425e"/>
<text class="keylabel" x="86" y="213.5" text-anchor="middle">Right Alt</text>
${body.filter((b) => b.includes('class="note')).join('\n')}
</svg>
`;

const out = path.join(__dirname, '..', 'docs', 'demo.svg');
fs.mkdirSync(path.dirname(out), { recursive: true });
fs.writeFileSync(out, svg);
console.log(`wrote docs/demo.svg: ${frames.length} frames, ${(total / 1000).toFixed(1)}s loop, ${(svg.length / 1024).toFixed(1)} KB`);
