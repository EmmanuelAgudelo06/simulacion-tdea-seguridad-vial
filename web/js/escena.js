import * as THREE from 'three';
import { Lit, LitDoble, Unlit } from './materiales.js';

// Construye por código la escena low-poly de la salida vehicular del TdeA Sede Robledo.
// Unidades en metros. Ejes: el carro sale del campus hacia +Z; el peatón camina por el andén hacia +X (Z = 0).
// Los ángulos de Euler usan el orden 'YXZ', el mismo de Unity, para que los valores del proyecto original sirvan tal cual.

const GRAD = Math.PI / 180;
const geoCubo = new THREE.BoxGeometry(1, 1, 1);
const geoEsfera = new THREE.SphereGeometry(0.5, 18, 12);
const geoCilindro = new THREE.CylinderGeometry(0.5, 0.5, 2, 18);

// ---------- Utilidades ----------
export function Grupo(padre, nombre, x = 0, y = 0, z = 0) {
  const g = new THREE.Group();
  g.name = nombre;
  g.position.set(x, y, z);
  g.rotation.order = 'YXZ';
  padre.add(g);
  return g;
}

function Malla(padre, geo, mat, sombra = true) {
  const m = new THREE.Mesh(geo, mat);
  m.rotation.order = 'YXZ';
  m.castShadow = sombra;
  m.receiveShadow = true;
  padre.add(m);
  return m;
}

export function Box(p, w, h, d, color, x, y, z) {
  const m = Malla(p, geoCubo, Lit(color));
  m.scale.set(w, h, d);
  m.position.set(x, y, z);
  return m;
}

/** Anillo (o disco si rIn = 0). horizontal = en el plano XZ; si no, en XY. */
function Anillo(rIn, rOut, seg, horizontal) {
  const g = rIn > 0 ? new THREE.RingGeometry(rIn, rOut, seg) : new THREE.CircleGeometry(rOut, seg);
  if (horizontal) g.rotateX(-Math.PI / 2);
  return g;
}

function MeshObj(nombre, padre, geo, mat, x, y, z, sombra = false) {
  const m = Malla(padre, geo, mat, sombra);
  m.name = nombre;
  m.position.set(x, y, z);
  return m;
}

/**
 * Letrero de texto en el mundo. tam en metros; fontSize en las mismas unidades que el Canvas de Unity (1 = 1 cm).
 * euler en grados, como en Unity. Se añade un giro de 180° porque en Unity el frente de un Canvas mira a -Z.
 */
function Letrero(padre, texto, pos, euler, tam, fondo, color, fontSize) {
  const ppm = 200; // píxeles por metro de la textura
  const c = document.createElement('canvas');
  c.width = Math.round(tam[0] * ppm);
  c.height = Math.round(tam[1] * ppm);
  const ctx = c.getContext('2d');
  if (fondo) { ctx.fillStyle = fondo; ctx.fillRect(0, 0, c.width, c.height); }
  ctx.fillStyle = color;
  let px = Math.round(fontSize / 100 * ppm);
  const fuente = (n) => `bold ${n}px "Arial Black", "Helvetica Neue", Arial, sans-serif`;
  ctx.font = fuente(px);
  const ancho = ctx.measureText(texto).width;
  if (ancho > c.width * 0.92) { px = Math.floor(px * c.width * 0.92 / ancho); ctx.font = fuente(px); }
  ctx.textAlign = 'center';
  ctx.textBaseline = 'middle';
  ctx.fillText(texto, c.width / 2, c.height / 2 + px * 0.05);

  const tex = new THREE.CanvasTexture(c);
  tex.colorSpace = THREE.SRGBColorSpace;
  tex.anisotropy = 4;
  const mat = new THREE.MeshBasicMaterial({
    map: tex, transparent: !fondo, polygonOffset: true, polygonOffsetFactor: -2, polygonOffsetUnits: -2,
  });
  const m = new THREE.Mesh(new THREE.PlaneGeometry(tam[0], tam[1]), mat);
  m.name = 'Letrero_' + texto;
  m.position.set(...pos);
  const q = new THREE.Quaternion().setFromEuler(new THREE.Euler(euler[0] * GRAD, euler[1] * GRAD, euler[2] * GRAD, 'YXZ'));
  q.multiply(new THREE.Quaternion().setFromAxisAngle(new THREE.Vector3(0, 1, 0), Math.PI));
  m.quaternion.copy(q);
  padre.add(m);
  return m;
}

function Arbol(w, x, z, s) {
  Box(w, 0.25 * s, 1.2 * s, 0.25 * s, 0x6b4f3a, x, 0.6 * s, z);
  const h = 2.2 * s;
  const geo = new THREE.ConeGeometry(1.0 * s, h, 7, 1, true);
  geo.translate(0, h / 2, 0);
  MeshObj('Copa', w, geo, LitDoble(0x3f7d3a), x, 1.0 * s, z, true);
}

function Extremidad(pivot, px, py, w, h, d, color) {
  const p = Grupo(pivot, 'Extremidad', px, py, 0);
  Box(p, w, h, d, color, 0, -h / 2, 0);
  return p;
}

// ---------- Escena ----------
export function construirEscena(root) {
  const e = {};
  // Centro de la escena: la base queda centrada sobre el marcador
  const w = Grupo(root, 'Mundo', 0, 0, 3.5);
  e.world = w;

  Box(w, 20, 0.12, 27, 0x6f9a52, 0, -0.06, -3.5);               // base / zonas verdes
  Box(w, 3.8, 0.03, 16, 0x3b3e44, 0, 0.015, -8);                // vía interna de salida
  Box(w, 20, 0.08, 2, 0xcac5b8, 0, 0.04, 0);                    // andén
  Box(w, 20, 0.1, 0.18, 0xe9e4d6, 0, 0.05, 1.05);               // sardinel
  Box(w, 20, 0.03, 7, 0x34373d, 0, 0.015, 4.6);                 // calle
  for (let x = -9; x < 10; x += 2.4) Box(w, 1.2, 0.035, 0.14, 0xf2c94c, x, 0.02, 4.6); // línea central
  Box(w, 3.5, 0.035, 0.28, 0xf4f2ec, 0, 0.02, -1.4);            // línea de pare
  Letrero(w, 'PARE', [0, 0.04, -2.6], [90, 0, 0], [2.6, 1.2], null, '#f4f2ec', 95);

  // Portería
  Box(w, 0.55, 3.4, 0.55, 0xe9e4d6, -2.3, 1.7, -3.2);
  Box(w, 0.55, 3.4, 0.55, 0xe9e4d6, 2.3, 1.7, -3.2);
  Box(w, 5.4, 0.9, 0.5, 0xe9e4d6, 0, 3.6, -3.2);
  Letrero(w, 'TdeA · SEDE ROBLEDO', [0, 3.6, -2.94], [0, 180, 0], [5.0, 0.78], '#1f7a3d', '#ffffff', 46);
  Box(w, 5.4, 0.06, 0.06, 0xffd23f, 0, 3.08, -2.95);
  // Cerramiento del campus
  for (const sd of [-1, 1]) {
    Box(w, 7.3, 1.7, 0.3, 0xe9e4d6, sd * 6.25, 0.85, -3.2);
    Box(w, 7.3, 0.12, 0.34, 0x1f7a3d, sd * 6.25, 1.74, -3.2);
  }
  // Caseta de vigilancia
  Box(w, 1.5, 2.3, 1.5, 0xf4f2ec, 3.6, 1.15, -4.6);
  Box(w, 1.9, 0.18, 1.9, 0x1f7a3d, 3.6, 2.38, -4.6);
  Box(w, 1.0, 0.6, 0.05, 0x7fa7c2, 3.6, 1.5, -3.84);
  // Señal de velocidad máxima 10 km/h
  Box(w, 0.08, 2.2, 0.08, 0x9aa0a8, -2.75, 1.1, -1.9);
  MeshObj('SenalFondo', w, Anillo(0, 0.42, 32, false), Unlit(0xffffff), -2.75, 2.2, -1.85);
  MeshObj('SenalBorde', w, Anillo(0.33, 0.42, 32, false), Unlit(0xd23a2a), -2.75, 2.2, -1.84);
  Letrero(w, '10', [-2.75, 2.2, -1.83], [0, 180, 0], [0.6, 0.5], null, '#111111', 34);
  // Edificios y árboles
  Box(w, 6, 4, 4, 0xe9e4d6, -6.5, 2, -11); Box(w, 6.1, 0.3, 4.1, 0x1f7a3d, -6.5, 4.1, -11);
  for (let i = 0; i < 4; i++) Box(w, 0.9, 0.9, 0.05, 0x7fa7c2, -8.5 + i * 1.4, 2.6, -8.97);
  Box(w, 5, 3, 5, 0xdcd5c4, 6.5, 1.5, -12); Box(w, 5.1, 0.25, 5.1, 0x1f7a3d, 6.5, 3.1, -12);
  const arboles = [[-4.5, -6], [-8.5, -5.2], [4.6, -7.5], [8.5, -6], [-3.5, -14.5], [3.5, -15.5], [-8.8, -15], [8.8, -16]];
  arboles.forEach(([x, z], i) => Arbol(w, x, z, 0.8 + (i % 3) * 0.15));

  // ---------- Carro ----------
  const car = Grupo(w, 'Carro');
  e.car = car;
  Box(car, 1.9, 0.62, 4.0, 0xc8402b, 0, 0.6, 0);
  Box(car, 1.62, 0.55, 2.1, 0xc8402b, 0, 1.18, -0.25);
  Box(car, 1.64, 0.42, 0.05, 0x223042, 0, 1.2, 0.81);
  Box(car, 1.64, 0.42, 0.05, 0x223042, 0, 1.2, -1.31);
  Box(car, 0.05, 0.38, 1.7, 0x223042, -0.82, 1.2, -0.25);
  Box(car, 0.05, 0.38, 1.7, 0x223042, 0.82, 1.2, -0.25);
  Box(car, 0.36, 0.14, 0.06, 0xfff6c8, -0.62, 0.66, 2.01);
  Box(car, 0.36, 0.14, 0.06, 0xfff6c8, 0.62, 0.66, 2.01);
  Box(car, 0.36, 0.12, 0.06, 0x8a1010, -0.62, 0.68, -2.01);
  Box(car, 0.36, 0.12, 0.06, 0x8a1010, 0.62, 0.68, -2.01);
  Box(car, 1.95, 0.16, 0.12, 0x2a2a2a, 0, 0.36, 2.0);
  Box(car, 1.95, 0.16, 0.12, 0x2a2a2a, 0, 0.36, -2.0);
  e.wheels = [[0.92, 1.3], [-0.92, 1.3], [0.92, -1.3], [-0.92, -1.3]].map(([x, z]) => {
    const g = Grupo(car, 'Rueda', x, 0.34, z);
    const llanta = Malla(g, geoCilindro, Lit(0x1c1c1c));
    llanta.scale.set(0.68, 0.13, 0.68);
    llanta.rotation.z = Math.PI / 2;
    Box(g, 0.28, 0.4, 0.08, 0x9aa0a8, 0, 0, 0);
    return g;
  });

  // Celular del conductor (la distracción): siempre mira a la cámara
  const phone = Grupo(car, 'Celular', -0.4, 2.35, -0.1);
  e.phone = phone;
  // En Three.js lookAt apunta +Z hacia la cámara, así que el halo va detrás en -Z
  MeshObj('Halo', phone, Anillo(0.34, 0.44, 28, false), Unlit(0x9fd0ff), 0, 0, -0.02);
  Box(phone, 0.3, 0.56, 0.04, 0x111111, 0, 0, 0).castShadow = false;
  const pantalla = Box(phone, 0.24, 0.46, 0.05, 0x59a8ff, 0, 0, 0);
  pantalla.material = Unlit(0x59a8ff);
  pantalla.castShadow = false;

  // Marcas de frenado
  e.skids = [-0.92, 0.92].map((x) => {
    const s = Box(w, 0.24, 0.012, 1, 0x111111, x, 0.09, 0);
    s.castShadow = false;
    s.visible = false;
    return s;
  });

  // ---------- Peatón: estudiante con morral ----------
  const ped = Grupo(w, 'Peaton');
  e.ped = ped;
  const pivot = Grupo(ped, 'Centro', 0, 0.95, 0);
  e.pivot = pivot;
  e.legL = Extremidad(pivot, -0.12, 0, 0.17, 0.92, 0.19, 0x2c3a55);
  e.legR = Extremidad(pivot, 0.12, 0, 0.17, 0.92, 0.19, 0x2c3a55);
  Box(e.legL, 0.2, 0.08, 0.28, 0xf4f2ec, 0, -0.9, 0.04);
  Box(e.legR, 0.2, 0.08, 0.28, 0xf4f2ec, 0, -0.9, 0.04);
  Box(pivot, 0.46, 0.62, 0.26, 0xf2b630, 0, 0.31, 0);            // camiseta
  const cabeza = Malla(pivot, geoEsfera, Lit(0xb9805a));
  cabeza.scale.setScalar(0.3);
  cabeza.position.set(0, 0.78, 0);
  Box(pivot, 0.3, 0.1, 0.3, 0x2a1c14, 0, 0.9, -0.01);            // cabello
  e.armL = Extremidad(pivot, -0.29, 0.58, 0.11, 0.6, 0.13, 0xf2b630);
  e.armR = Extremidad(pivot, 0.29, 0.58, 0.11, 0.6, 0.13, 0xf2b630);
  Box(pivot, 0.36, 0.44, 0.18, 0x2f6db5, 0, 0.32, -0.22);        // morral

  // Onda del impacto
  e.ring = MeshObj('OndaImpacto', w, Anillo(0.6, 0.75, 40, true), Unlit(0xffd23f), 0, 0.12, 0);
  e.ring.visible = false;
  return e;
}
