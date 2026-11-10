import * as THREE from 'three';
import { sonarFrenazo, sonarGolpe, vibrar } from './audio.js';

// Tiempos y física de la simulación (equivale a la parte de animación de SimulacionVial.cs).
// Cinemática en metros y segundos.
const CAR_FRONT = 2, PED_R = 0.3, Z0 = -13, T0 = 0.5, ACC = 3.5, VMAX = 8.9, WALK = 1.25;
const SAFE_PED_T = 5.3;   // en la versión segura el peatón pasa justo cuando el carro hace el pare
const LINEA_PARE = -1.45;
const GRAD = Math.PI / 180;

const tAcc = VMAX / ACC;
const dAcc = 0.5 * ACC * tAcc * tAcc;
const dHit = -PED_R - (Z0 + CAR_FRONT);
const tImpacto = T0 + (dHit <= dAcc ? Math.sqrt(2 * dHit / ACC) : tAcc + (dHit - dAcc) / VMAX);

export class Simulacion {
  /** e: piezas de la escena (construirEscena); ui: interfaz (Interfaz). */
  constructor(e, ui) {
    this.e = e;
    this.ui = ui;
    this.corriendo = false;
    this.pausada = false;
    this.seguro = false;
    this.ringT = -1;
    this.pos = new THREE.Vector3();
    this.vel = new THREE.Vector3();
    this._cam = new THREE.Vector3();
  }

  reiniciar(modoSeguro) {
    const e = this.e;
    this.seguro = modoSeguro;
    this.t = 0; this.carZ = Z0; this.carV = 0; this.fase = 'walk';
    this.impacto = false; this.final = false; this.detenido = false; this.skidStart = null; this.ringT = -1;
    e.car.position.set(0, 0, Z0);
    e.phone.visible = !modoSeguro;
    e.pivot.rotation.set(0, 0, 0);
    e.pivot.position.set(0, 0.95, 0);
    e.ped.rotation.set(0, 90 * GRAD, 0);
    for (const l of [e.legL, e.legR, e.armL, e.armR]) l.rotation.set(0, 0, 0);
    for (const s of e.skids) s.visible = false;
    e.ring.visible = false;
    this.setPedX(WALK * (0 - (modoSeguro ? SAFE_PED_T : tImpacto)));
    this.ui.ocultarFinal();
    this.ui.mostrarHud(true);
    this.ui.subtitulo('');
    this.ui.velocidad(0);
    this.corriendo = true;
    this.pausada = false;
  }

  /** dt real en segundos; camara: la cámara de Three.js (para que el celular siempre la mire). */
  actualizar(dt, camara) {
    const e = this.e;
    if (this.corriendo && !this.pausada) {
      const rdt = Math.min(dt, 0.05);
      const ts = this.escalaTiempo();
      for (let i = 0; i < 3; i++) this.paso(rdt * ts / 3);
      if (this.ringT >= 0) {
        this.ringT += rdt;
        const k = this.ringT / 0.7;
        e.ring.scale.setScalar(1 + k * 4);
        if (k >= 1) { this.ringT = -1; e.ring.visible = false; }
      }
    }
    if (camara && e.phone.visible) {
      camara.getWorldPosition(this._cam);
      e.phone.lookAt(this._cam);
    }
  }

  escalaTiempo() {
    // cámara lenta en el impacto
    if (!this.seguro && this.t > tImpacto - 0.3 && this.t < tImpacto + 0.6) return 0.28;
    return 1;
  }

  setPedX(x) { this.pedX = x; this.e.ped.position.set(x, 0, 0); }

  caminar(tt, amp = 0.55) {
    const e = this.e;
    const s = Math.sin(tt * 7.5) * amp;
    e.legL.rotation.set(s, 0, 0);
    e.legR.rotation.set(-s, 0, 0);
    e.armL.rotation.set(-s * 0.8, 0, 0);
    e.armR.rotation.set(s * 0.8, 0, 0);
    e.pivot.position.set(0, 0.95 + Math.abs(Math.cos(tt * 7.5)) * 0.03 * (amp > 0 ? 1 : 0), 0);
  }

  carroPeligro(tt) {
    const tau = Math.max(0, tt - T0);
    if (tau <= tAcc) return [Z0 + 0.5 * ACC * tau * tau, ACC * tau];
    return [Z0 + dAcc + VMAX * (tau - tAcc), VMAX];
  }

  paso(dt) {
    const e = this.e;
    this.t += dt;
    if (this.seguro) this.pasoSeguro(dt); else this.pasoPeligro(dt);
    e.car.position.set(0, 0, this.carZ);
    const giro = this.carV * dt / 0.34;
    for (const w of e.wheels) w.rotateX(giro);
    e.phone.position.set(-0.4, 2.35 + Math.sin(this.t * 3) * 0.08, -0.1);
    this.ui.velocidad(this.carV);
  }

  pasoPeligro(dt) {
    const e = this.e, ui = this.ui;
    const t = this.t;
    if (!this.impacto) {
      [this.carZ, this.carV] = this.carroPeligro(Math.min(t, tImpacto));
      this.setPedX(WALK * (t - tImpacto));
      this.caminar(t);
      ui.subtitulo(t < 1.1 ? 'Salida vehicular · TdeA Sede Robledo' : 'El conductor va mirando el celular y no baja la velocidad');
      if (t >= tImpacto) {
        this.impacto = true; this.fase = 'air'; this.setPedX(0);
        this.pos.set(0, 0.95, 0);
        this.vel.set(0.5, 4.3, this.carV * 0.85);
        this.brakeAt = t + 0.18;
        this.ringT = 0; e.ring.scale.setScalar(1); e.ring.visible = true;
        ui.destello(); sonarGolpe(); vibrar(300);
        ui.subtitulo('¡No alcanzó a frenar!');
      }
      return;
    }

    // Carro: reacción tardía y frenado en seco
    if (t >= this.brakeAt && this.carV > 0) {
      if (this.skidStart === null) {
        this.skidStart = this.carZ - 1.3;
        for (const s of e.skids) s.visible = true;
        sonarFrenazo();
      }
      this.carV = Math.max(0, this.carV - 7.5 * dt);
    }
    this.carZ += this.carV * dt;
    if (this.skidStart !== null) {
      const L = Math.max(0.01, (this.carZ - 1.3) - this.skidStart + 0.4);
      for (const s of e.skids) {
        s.scale.set(0.24, 0.012, L);
        s.position.set(s.position.x, 0.09, this.skidStart + L / 2);
      }
    }

    // Peatón proyectado por el impacto
    const pos = this.pos, vel = this.vel;
    if (this.fase === 'air') {
      vel.y -= 9.8 * dt;
      pos.addScaledVector(vel, dt);
      e.pivot.rotateZ(8.5 * dt);
      e.legL.rotation.set(52 * GRAD, 0, 0);
      e.legR.rotation.set(-29 * GRAD, 0, 0);
      e.armL.rotation.set(0, 0, -69 * GRAD);
      e.armR.rotation.set(0, 0, 80 * GRAD);
      if (pos.y <= 0.2 && vel.y < 0) {
        this.fase = 'slide'; pos.y = 0.2;
        e.pivot.rotation.set(0, 0, 90 * GRAD);
        vibrar(150);
      }
    } else if (this.fase === 'slide') {
      const sp = Math.hypot(vel.x, vel.z);
      if (sp > 0.01) {
        const k = Math.max(0, sp - 7 * dt) / sp;
        vel.x *= k; vel.z *= k;
        pos.x += vel.x * dt; pos.z += vel.z * dt;
      } else this.fase = 'down';
    }
    e.ped.position.set(pos.x, pos.y - 0.95, pos.z);
    e.pivot.position.set(0, 0.95, 0);

    if (t > tImpacto + 1.4) ui.subtitulo('');
    if (!this.final && t > tImpacto + 2.4) { this.final = true; ui.mostrarFinal(false); }
  }

  pasoSeguro(dt) {
    const ui = this.ui;
    const t = this.t;
    const vmax = 2.8; // 10 km/h
    const front = this.carZ + CAR_FRONT;
    const pedEnZona = this.pedX > -2.6 && this.pedX < 3.0;
    const aLinea = LINEA_PARE - front;
    let objetivo = t < T0 ? 0 : vmax;

    if (!this.detenido) {
      // Se acerca a la línea de pare y frena hasta detenerse por completo
      const necesita = this.carV * this.carV / (2 * Math.max(aLinea, 0.01));
      if (t > T0 && (necesita > 0.9 || aLinea < 0.08)) {
        this.carV = Math.max(0, this.carV - Math.max(necesita, 2.2) * dt);
        objetivo = null;
        if (this.carV < 0.05) { this.carV = 0; this.detenido = true; this.stopT = t; }
      }
    } else if (pedEnZona || t - this.stopT < 1.2) { this.carV = 0; objetivo = null; } // espera a que el peatón pase

    if (objetivo !== null) {
      this.carV = this.carV < objetivo ? Math.min(objetivo, this.carV + 3 * dt) : Math.max(objetivo, this.carV - 3 * dt);
    }
    this.carZ += this.carV * dt;

    if (this.pedX < 8) { this.setPedX(WALK * (t - SAFE_PED_T)); this.caminar(t); }
    else this.caminar(0, 0);

    if (t < 1.1) ui.subtitulo('La misma salida, otra decisión');
    else if (!this.detenido) ui.subtitulo('Celular guardado, 10 km/h y mirada en el andén');
    else if (this.carV === 0) ui.subtitulo('Pare total: el peatón en el andén tiene la prioridad');
    else ui.subtitulo('Ahora sí: salida segura');

    if (!this.final && t > SAFE_PED_T + 5.2) { this.final = true; ui.mostrarFinal(true); }
  }
}
