import * as THREE from 'three';
import { construirEscena } from './escena.js';
import { Simulacion } from './simulacion.js';
import { Interfaz } from './interfaz.js';
import { activarAudio } from './audio.js';

// Orquesta la app: modo RA con marcador (MindAR) y modo 3D sin marcador (equivale a la parte de
// colocación y flujo de SimulacionVial.cs).

const VIEW_YAW = Math.atan2(1, 0.45); // se ve de lado, un poco en diagonal
// Ancho de la escena medido en "anchos del marcador" (la base mide 20 m).
const TAMANOS = [
  { nombre: 'Pequeña', anchos: 1.6 },
  { nombre: 'Mediana', anchos: 2.6 },
  { nombre: 'Grande', anchos: 4 },
];
const ANCHO_BASE = 20;

const ui = new Interfaz();
let tamano = 1;
let modo = null;          // null | 'ar' | 'demo'
let estado = 'inicio';    // inicio | buscando | corriendo
let mindar = null, ancla = null, renderer = null, camara = null, escena3D = null, controles = null;
const reloj = new THREE.Clock();

// ---------- Escena compartida ----------
// colocacion: marco con Y hacia arriba. En RA se gira para quedar acostado sobre el marcador.
const colocacion = new THREE.Group();
const giro = new THREE.Group();
giro.rotation.y = -VIEW_YAW;
colocacion.add(giro);
const piezas = construirEscena(giro);
const sim = new Simulacion(piezas, ui);

// Luces: van dentro de la colocación para moverse con el marcador.
colocacion.add(new THREE.AmbientLight(0xffffff, 1.15));
const sol = new THREE.DirectionalLight(0xffffff, 2.3);
sol.position.set(-8, 16, 10);
sol.target.position.set(0, 0, 0);
sol.castShadow = true;
sol.shadow.mapSize.set(1024, 1024);
sol.shadow.bias = -0.0005;
sol.shadow.normalBias = 0.02;
colocacion.add(sol, sol.target);
const relleno = new THREE.DirectionalLight(0xdfe8ff, 0.6);
relleno.position.set(10, 6, -8);
colocacion.add(relleno, relleno.target);

let escalaSombra = 0;
function ajustarSombra() {
  // La cámara de sombras trabaja en unidades del mundo; en RA esas unidades cambian con el marcador.
  const k = colocacion.matrixWorld.getMaxScaleOnAxis();
  if (!k || Math.abs(k - escalaSombra) / k < 0.02) return;
  escalaSombra = k;
  const c = sol.shadow.camera;
  c.left = c.bottom = -19 * k;
  c.right = c.top = 19 * k;
  c.near = 0.5 * k;
  c.far = 45 * k;
  c.updateProjectionMatrix();
}

function aplicarTamano() {
  if (modo === 'ar') colocacion.scale.setScalar(TAMANOS[tamano].anchos / ANCHO_BASE);
  ui.textoTamano(TAMANOS[tamano].nombre);
  ui.marcarEscala(tamano);
}

// ---------- Flujo ----------
ui.onEscala = (i) => { tamano = i; aplicarTamano(); };
ui.onTamano = () => { tamano = (tamano + 1) % TAMANOS.length; aplicarTamano(); };
ui.onRepetir = () => { if (estado === 'corriendo') sim.reiniciar(sim.seguro); };
ui.onFinal = (seguro) => sim.reiniciar(seguro);
ui.onComenzar = () => { activarAudio(); iniciarAR(); };
ui.onDemo = () => { activarAudio(); iniciarDemo(); };
ui.onInicio = volverAlInicio;
aplicarTamano();

if (!window.isSecureContext) {
  const aviso = document.querySelector('#aviso');
  aviso.textContent = 'La cámara solo funciona si la página se abre con https://. El modo 3D sin marcador sí funciona.';
  aviso.hidden = false;
}

function empezarSimulacion(conTamano) {
  estado = 'corriendo';
  ui.mostrarIndicacion(false);
  ui.mostrarControles(true, conTamano);
  sim.reiniciar(false);
}

function bucle() {
  const dt = reloj.getDelta();
  if (controles) controles.update();
  sim.actualizar(dt, camara);
  colocacion.updateMatrixWorld(true);
  ajustarSombra();
  renderer.render(escena3D, camara);
}

// ---------- Modo RA con marcador ----------
async function iniciarAR() {
  modo = 'ar';
  estado = 'buscando';
  ui.mostrarInicio(false);
  ui.mostrarIndicacion(true, 'Abriendo la cámara…');
  pantallaEncendida();
  aplicarTamano();
  try {
    if (!mindar) {
      const { MindARThree } = await import('mindar-image-three');
      mindar = new MindARThree({
        container: document.querySelector('#ar'),
        imageTargetSrc: 'marcador/marcador.mind',
        uiLoading: 'no', uiScanning: 'no', uiError: 'no',
        filterMinCF: 0.0001, filterBeta: 0.01,   // menos temblor
        missTolerance: 8,
      });
      mindar.renderer.shadowMap.enabled = true;
      mindar.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
      mindar.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
      ancla = mindar.addAnchor(0);
      ancla.onTargetFound = () => {
        if (estado === 'buscando') empezarSimulacion(true);
        else if (estado === 'corriendo') { sim.pausada = false; ui.mostrarIndicacion(false); }
      };
      ancla.onTargetLost = () => {
        if (estado !== 'corriendo') return;
        sim.pausada = true;
        ui.mostrarIndicacion(true, 'Se perdió el marcador. Vuelve a apuntar a la hoja para continuar.', true);
      };
    }
    colocacion.rotation.set(Math.PI / 2, 0, 0); // Y de la escena = normal del marcador
    ancla.group.add(colocacion);
    renderer = mindar.renderer; camara = mindar.camera; escena3D = mindar.scene; controles = null;
    document.querySelector('#demo').hidden = true;
    await mindar.start();
    document.querySelector('#ar').hidden = false;
    reloj.getDelta();
    renderer.setAnimationLoop(bucle);
    if (estado === 'buscando') {
      ui.mostrarIndicacion(true, 'Apunta la cámara al marcador impreso. Muévete despacio hasta que aparezca la escena.', true);
    }
  } catch (err) {
    console.error('[TdeA]', err);
    estado = 'inicio';
    ui.mostrarIndicacion(false);
    ui.mostrarInicio(true);
    const aviso = document.querySelector('#aviso');
    aviso.textContent = 'No se pudo abrir la cámara. Revisa que la página esté en https:// y que hayas dado permiso de cámara. También puedes usar «Ver en 3D sin marcador».';
    aviso.hidden = false;
  }
}

// ---------- Modo 3D sin marcador (respaldo / proyector) ----------
async function iniciarDemo() {
  modo = 'demo';
  ui.mostrarInicio(false);
  pantallaEncendida();
  if (!demo) {
    const { OrbitControls } = await import('three/addons/controls/OrbitControls.js');
    const cont = document.querySelector('#demo');
    renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.setSize(cont.clientWidth, cont.clientHeight);
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    cont.appendChild(renderer.domElement);
    escena3D = new THREE.Scene();
    escena3D.background = new THREE.Color(0xb9d3e6);
    escena3D.fog = new THREE.Fog(0xb9d3e6, 45, 90);
    const suelo = new THREE.Mesh(new THREE.CircleGeometry(80, 48), new THREE.MeshLambertMaterial({ color: 0x8fae74 }));
    suelo.rotation.x = -Math.PI / 2;
    suelo.position.y = -0.13;
    suelo.receiveShadow = true;
    escena3D.add(suelo);
    camara = new THREE.PerspectiveCamera(40, cont.clientWidth / cont.clientHeight, 0.1, 300);
    controles = new OrbitControls(camara, renderer.domElement);
    controles.enableDamping = true;
    controles.maxPolarAngle = Math.PI * 0.47;
    controles.minDistance = 8;
    controles.maxDistance = 80;
    window.addEventListener('resize', () => {
      if (modo !== 'demo') return;
      camara.aspect = cont.clientWidth / cont.clientHeight;
      camara.updateProjectionMatrix();
      renderer.setSize(cont.clientWidth, cont.clientHeight);
    });
    demo = { renderer, escena3D, camara, controles };
  }
  ({ renderer, escena3D, camara, controles } = demo);
  colocacion.rotation.set(0, 0, 0);
  colocacion.scale.setScalar(1);
  escena3D.add(colocacion);
  // Encuadre: centrado entre la portería y el sitio donde cae el peatón, visto desde el frente.
  colocacion.updateMatrixWorld(true);
  const centro = giro.localToWorld(new THREE.Vector3(0, 0.5, 2.5));
  controles.target.copy(centro);
  const lejos = window.innerWidth < window.innerHeight ? 42 : 30;
  camara.position.set(centro.x + lejos * 0.12, centro.y + lejos * 0.6, centro.z + lejos);
  controles.update();
  document.querySelector('#demo').hidden = false;
  document.querySelector('#ar').hidden = true;
  reloj.getDelta();
  renderer.setAnimationLoop(bucle);
  empezarSimulacion(false);
}
let demo = null;

function volverAlInicio() {
  if (renderer) renderer.setAnimationLoop(null);
  if (modo === 'ar' && mindar) {
    try { mindar.stop(); } catch (err) { console.warn(err); }
    mindar.anchors.forEach((a) => { a.visible = false; a.group.visible = false; });
  }
  if (modo === 'demo') document.querySelector('#demo').hidden = true;
  controles = null;
  modo = null;
  estado = 'inicio';
  sim.corriendo = false;
  ui.mostrarHud(false);
  ui.mostrarControles(false);
  ui.ocultarFinal();
  ui.mostrarIndicacion(false);
  ui.mostrarInicio(true);
  liberarPantalla();
}

// ---------- Mantener la pantalla encendida ----------
let bloqueo = null;
async function pantallaEncendida() {
  try { bloqueo = await navigator.wakeLock?.request('screen'); } catch { /* no soportado */ }
}
function liberarPantalla() { bloqueo?.release?.(); bloqueo = null; }
document.addEventListener('visibilitychange', () => {
  if (document.visibilityState === 'visible' && modo) pantallaEncendida();
});
