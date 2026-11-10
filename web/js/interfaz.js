// Interfaz en pantalla (equivale a InterfazTdeA.cs). Los textos del mensaje final se editan aquí.
const $ = (sel) => document.querySelector(sel);

const TEXTOS_FINAL = {
  peligro: {
    eyebrow: 'RECUERDA',
    titulo: 'UN SEGUNDO DE DISTRACCIÓN CAMBIA UNA VIDA.',
    texto: 'A 30 km/h un carro avanza más de 8 metros cada segundo y necesita unos 13 metros para detenerse. En la entrada del TdeA, baja la velocidad: aquí caminamos todos.',
    tips: ['Sal del campus a máximo 10 km/h', 'El peatón en el andén siempre tiene la prioridad', 'El celular, guardado mientras manejas'],
    botonA: 'Ver cómo debió ser',
    botonB: 'Repetir',
  },
  seguro: {
    eyebrow: 'ASÍ SÍ',
    titulo: 'FRENAR A TIEMPO TOMA DOS SEGUNDOS. UNA VIDA NO TIENE REPUESTO.',
    texto: 'Bajar a 10 km/h en la salida casi no cambia tu tiempo de viaje y le da a todos el tiempo de reaccionar. Conduce como si quien cruza fuera alguien que quieres.',
    tips: ['Baja la velocidad antes de la portería', 'Haz el pare completo antes del andén', 'Mira a los dos lados antes de salir'],
    botonA: 'Repetir esta versión',
    botonB: 'Repetir el accidente',
  },
};

export class Interfaz {
  constructor() {
    this.el = {
      inicio: $('#inicio'), hud: $('#hud'), caption: $('#subtitulo'), kmh: $('#kmh'), barra: $('#barra'),
      vel: $('#velocimetro'), hint: $('#indicacion'), hintText: $('#indicacion-texto'), hintImg: $('#indicacion-marcador'),
      final: $('#final'), controles: $('#controles'), destello: $('#destello'), tamano: $('#btn-tamano'),
    };
    this._kmh = -1;
    this._caption = null;

    // Botones: la lógica la conecta main.js asignando estas funciones.
    this.onComenzar = this.onDemo = this.onRepetir = this.onInicio = this.onTamano = () => {};
    this.onEscala = () => {};
    this.onFinal = () => {};
    $('#btn-comenzar').addEventListener('click', () => this.onComenzar());
    $('#btn-demo').addEventListener('click', () => this.onDemo());
    $('#btn-repetir').addEventListener('click', () => this.onRepetir());
    $('#btn-inicio').addEventListener('click', () => this.onInicio());
    this.el.tamano.addEventListener('click', () => this.onTamano());
    $('#btn-final-a').addEventListener('click', () => this.onFinal(true));
    $('#btn-final-b').addEventListener('click', () => this.onFinal(false));
    document.querySelectorAll('#tamanos button').forEach((b, i) =>
      b.addEventListener('click', () => { this.marcarEscala(i); this.onEscala(i); }));
  }

  marcarEscala(i) {
    document.querySelectorAll('#tamanos button').forEach((b, k) => b.classList.toggle('activo', k === i));
  }

  textoTamano(nombre) { this.el.tamano.textContent = 'Tamaño: ' + nombre; }

  mostrarInicio(v) { this.el.inicio.hidden = !v; }
  mostrarHud(v) { this.el.hud.hidden = !v; }
  mostrarControles(v, conTamano = true) {
    this.el.controles.hidden = !v;
    this.el.tamano.hidden = !conTamano;
  }
  mostrarIndicacion(v, texto = null, conMarcador = false) {
    if (texto !== null) this.el.hintText.textContent = texto;
    this.el.hintImg.hidden = !conMarcador;
    this.el.hint.hidden = !v;
  }
  ocultarFinal() { this.el.final.hidden = true; }

  destello() {
    const d = this.el.destello;
    d.style.transition = 'none';
    d.style.opacity = '0.55';
    void d.offsetWidth; // fuerza el reinicio de la transición
    d.style.transition = 'opacity 0.5s linear';
    d.style.opacity = '0';
  }

  subtitulo(s) {
    if (this._caption === s) return;
    this._caption = s;
    this.el.caption.textContent = s;
    this.el.caption.hidden = !s;
  }

  velocidad(ms) {
    const k = Math.round(ms * 3.6);
    if (k === this._kmh) return;
    this._kmh = k;
    this.el.kmh.textContent = k;
    const mal = k > 12;
    this.el.vel.classList.toggle('mal', mal);
    this.el.barra.style.width = Math.min(100, (k / 35) * 100) + '%';
  }

  mostrarFinal(seguro) {
    const t = seguro ? TEXTOS_FINAL.seguro : TEXTOS_FINAL.peligro;
    const f = this.el.final;
    f.classList.toggle('seguro', seguro);
    f.querySelector('.eyebrow').textContent = t.eyebrow;
    f.querySelector('h2').textContent = t.titulo;
    f.querySelector('.texto').textContent = t.texto;
    const ul = f.querySelector('ul');
    ul.replaceChildren(...t.tips.map((s) => Object.assign(document.createElement('li'), { textContent: s })));
    f.querySelector('#btn-final-a').textContent = t.botonA;
    f.querySelector('#btn-final-b').textContent = t.botonB;
    f.hidden = false;
  }
}
