# Baja la velocidad — Simulación de seguridad vial en RA (TdeA)

Salida vehicular del TdeA Sede Robledo: un carro sale sin bajar la velocidad (conductor mirando el celular),
atropella a un estudiante en el andén y aparece el mensaje. Luego se puede ver «cómo debió ser».

Es una **página web**: se abre desde el navegador del celular (iPhone o Android), sin instalar nada.
La realidad aumentada funciona con un **marcador impreso** (MindAR + Three.js).

## Estructura

```
web/                 ← el sitio que se publica (todo lo necesario está aquí, sin CDN)
  index.html
  css/estilos.css
  js/main.js         flujo de la app: modo RA (marcador) y modo 3D sin marcador
  js/simulacion.js   tiempos y física: versión de peligro y versión segura
  js/escena.js       modelos low-poly: portería, andén, calle, carro, celular, estudiante, letreros
  js/interfaz.js     textos en pantalla y mensaje final (los textos se editan aquí)
  js/audio.js        frenazo y golpe generados por código
  js/materiales.js   colores
  marcador/          marcador.png, marcador.mind (datos de rastreo) e imprimir.html
  vendor/            three.js 0.160 y MindAR 1.2.5 (licencia MIT)
herramientas/generar-marcador.html   vuelve a dibujar y compilar el marcador
unity/               versión original en Unity (referencia; no se necesita para la web)
```

## Probar en el computador

```bash
cd simulacion-tdea-seguridad-vial
python3 -m http.server 8000
```

Abre <http://localhost:8000/web/> y pulsa **Ver en 3D sin marcador**. En `localhost` también funciona la
cámara del portátil: imprime o muestra el marcador en otra pantalla y pulsa **Comenzar**.

## Usarla en el celular

La cámara del celular solo se puede usar si la página está en **https://**. Hay que publicar la carpeta
`web/` en un hosting gratuito (GitHub Pages, Netlify o Vercel) y abrir ese link en el celular.

1. Imprime el marcador: abre `web/marcador/imprimir.html` y dale **Imprimir** (A4 o carta, horizontal, a color).
2. Pon la hoja plana en una mesa o en el piso, con buena luz.
3. Abre el link, elige el tamaño y pulsa **Comenzar**. Acepta el permiso de cámara.
4. Párate en el lado que dice «párate aquí» y apunta a la hoja: la escena aparece encima y empieza.
5. Al final: **Ver cómo debió ser** o **Repetir**. Si se pierde el marcador, la simulación se pausa sola.

**Respaldo:** si algo falla con la cámara o la luz, **Ver en 3D sin marcador** muestra la misma simulación
en 3D (se puede girar con el dedo). Sirve también para proyectar desde el portátil.

## Consejos para la presentación

- En iPhone, quita el **modo silencio** (el interruptor lateral) para que se oigan el frenazo y el golpe.
- Para que el grupo vea la pantalla: **Duplicar pantalla** (AirPlay) o un adaptador a HDMI.
- Ver desde unos 40–80 cm de altura y en diagonal; si la escena se sale de la pantalla, usa **Tamaño: Pequeña**
  o aléjate. Una impresión más grande (pliego, o el marcador en la pantalla de un portátil) se ve desde más lejos.
- Practica una vez en el salón antes de presentar.

## Cambiar el marcador

Edita el dibujo en `herramientas/generar-marcador.html`, ábrelo con un servidor local
(`python3 -m http.server` en la carpeta del proyecto → `http://localhost:8000/herramientas/generar-marcador.html`),
pulsa **Compilar y guardar** y copia los dos archivos descargados a `web/marcador/`.
