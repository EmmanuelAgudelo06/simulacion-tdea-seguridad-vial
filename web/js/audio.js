// Sonidos generados por código (equivale a AudioTdeA.cs). No hay archivos de audio.
let ctx = null;
let frenazo = null;
let golpe = null;

function aleatorio(semilla) {
  let s = semilla >>> 0;
  return () => {
    s = (s + 0x6d2b79f5) >>> 0;
    let t = s;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

function crearFrenazo(sr) {
  const dur = 1.3, n = Math.floor(dur * sr);
  const buf = ctx.createBuffer(1, n, sr), d = buf.getChannelData(0);
  const rnd = aleatorio(3);
  let y = 0;
  for (let i = 0; i < n; i++) {
    const t = i / sr, k = i / n;
    const env = Math.min(1, t * 25) * (1 - k) * (1 - k);
    const chirrido = Math.sin(2 * Math.PI * (1750 + 90 * Math.sin(2 * Math.PI * 23 * t)) * t);
    y += 0.25 * ((rnd() * 2 - 1) - y);
    d[i] = (0.35 * chirrido + 0.5 * y) * env;
  }
  return buf;
}

function crearGolpe(sr) {
  const dur = 0.45, n = Math.floor(dur * sr);
  const buf = ctx.createBuffer(1, n, sr), d = buf.getChannelData(0);
  const rnd = aleatorio(9);
  let y = 0;
  for (let i = 0; i < n; i++) {
    const t = i / sr;
    const grave = Math.sin(2 * Math.PI * (95 - 40 * t) * t) * Math.exp(-t * 9);
    y += 0.35 * ((rnd() * 2 - 1) - y);
    d[i] = Math.max(-1, Math.min(1, 0.9 * grave + 0.7 * y * Math.exp(-t * 28)));
  }
  return buf;
}

/** Debe llamarse dentro de un toque del usuario (los navegadores bloquean el audio hasta entonces). */
export function activarAudio() {
  try {
    if (!ctx) {
      ctx = new (window.AudioContext || window.webkitAudioContext)();
      frenazo = crearFrenazo(ctx.sampleRate);
      golpe = crearGolpe(ctx.sampleRate);
    }
    if (ctx.state === 'suspended') ctx.resume();
  } catch (err) {
    console.warn('[TdeA] Audio no disponible', err);
  }
}

function sonar(buf, volumen) {
  if (!ctx || !buf) return;
  const src = ctx.createBufferSource();
  src.buffer = buf;
  const g = ctx.createGain();
  g.gain.value = volumen;
  src.connect(g).connect(ctx.destination);
  src.start();
}

export const sonarFrenazo = () => sonar(frenazo, 0.6);
export const sonarGolpe = () => sonar(golpe, 1);

export function vibrar(ms = 200) {
  if (navigator.vibrate) navigator.vibrate(ms);
}
