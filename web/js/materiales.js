import * as THREE from 'three';

// Materiales compartidos por color (equivale a MaterialesTdeA.cs).
const lit = new Map();
const litDoble = new Map();
const unlit = new Map();

function cache(mapa, color, crear) {
  if (!mapa.has(color)) mapa.set(color, crear());
  return mapa.get(color);
}

/** Material con luz y sombras (look low-poly mate). */
export const Lit = (color) => cache(lit, color, () => new THREE.MeshLambertMaterial({ color }));

/** Igual que Lit pero visible por ambas caras y con caras planas (copas de árbol). */
export const LitDoble = (color) => cache(litDoble, color, () =>
  new THREE.MeshLambertMaterial({ color, side: THREE.DoubleSide, flatShading: true }));

/** Color plano sin luz, visible por ambas caras (señales, halo, onda del impacto). */
export const Unlit = (color) => cache(unlit, color, () =>
  new THREE.MeshBasicMaterial({ color, side: THREE.DoubleSide }));
