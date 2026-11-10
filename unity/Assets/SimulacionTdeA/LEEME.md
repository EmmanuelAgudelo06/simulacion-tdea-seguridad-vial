# Baja la velocidad — App de RA (iPhone y Android)

Simulación de seguridad vial en realidad aumentada: salida vehicular del TdeA Sede Robledo.
Se coloca la escena en el piso, un carro sale sin bajar la velocidad (conductor con el celular),
atropella a un estudiante en el andén y aparece el mensaje. Luego se puede ver "cómo debió ser".

Toda la escena (carro, peatón, portería, letreros, sonidos e interfaz) se construye por código,
así que no hay que importar modelos ni audios.

---

## 1. Lo que necesitas

- Un Mac con **Xcode** (de la App Store) y un cable para el iPhone.
- **Unity Hub** y **Unity 6 LTS** (6000.0.x), instalado con los módulos:
  - *iOS Build Support*
  - *Android Build Support* (con OpenJDK y Android SDK & NDK), solo si también lo quieres en Android.
- Tu **Apple ID** (gratis). No hace falta pagar la cuenta de desarrollador para instalarla en tu propio iPhone.
- Un iPhone con ARKit (iPhone XS / XR o más nuevo; sirve casi cualquiera de los últimos años).

## 2. Crear el proyecto

1. En Unity Hub: **New project → plantilla "AR Mobile"** (si no aparece, descárgala desde el mismo Hub).
   Nómbralo `BajaLaVelocidad` y créalo.
   La plantilla ya trae AR Foundation, ARKit, ARCore, Input System y URP configurados.
2. Copia la carpeta **`SimulacionTdeA`** (esta carpeta, completa) dentro de la carpeta **`Assets`** del proyecto.
3. Espera a que Unity compile. Debe aparecer un menú nuevo arriba llamado **TdeA**.
4. Haz clic en **TdeA → 1. Crear escena de la simulación**.
   Esto crea y guarda la escena `Assets/SimulacionTdeA/SimulacionTdeA.unity`, la pone como única escena del build
   y ajusta el nombre de la app ("Baja la velocidad"), el identificador y el permiso de cámara.
5. Revisa **Edit → Project Settings → XR Plug-in Management**:
   - pestaña de iOS (manzana): **Apple ARKit** marcado;
   - pestaña de Android (robot): **Google ARCore** marcado.

### Probar en el computador (opcional)
Dale **Play** en Unity, pulsa **Comenzar** y haz clic en la vista del juego: la escena aparece frente a la cámara
y corre la animación. Sirve para revisar que todo se ve bien antes de compilar.

## 3. Instalar en el iPhone

1. **File → Build Profiles** (o *Build Settings*) → selecciona **iOS** → **Switch Platform**.
2. Pulsa **Build**, elige una carpeta nueva (por ejemplo `BuildiOS`) y espera.
3. Abre `BuildiOS/Unity-iPhone.xcodeproj` en **Xcode**.
4. En Xcode, selecciona el proyecto **Unity-iPhone** → pestaña **Signing & Capabilities**:
   - marca *Automatically manage signing*;
   - en **Team** elige tu Apple ID (*Personal Team*). Si no aparece: *Add an Account…* e inicia sesión;
   - si dice que el *Bundle Identifier* ya está en uso, cámbialo por algo único, p. ej. `com.tunombre.bajalavelocidad`.
5. Conecta el iPhone por cable, desbloquéalo y acepta **"Confiar en este computador"**.
6. En el iPhone activa **Ajustes → Privacidad y seguridad → Modo de desarrollador** (se reinicia).
7. En Xcode, arriba, elige tu iPhone como destino y pulsa **▶ (Run)**.
8. La primera vez el iPhone no deja abrirla: ve a **Ajustes → General → VPN y gestión de dispositivos**,
   toca tu Apple ID y pulsa **Confiar**. Ábrela de nuevo y acepta el permiso de cámara.

**Importante:** con Apple ID gratuito la app deja de abrir **7 días** después de instalarla.
Instálala pocos días antes de la presentación. Si vence, conecta el iPhone y vuelve a darle ▶ en Xcode.

## 4. Instalar en Android (opcional)

1. En el teléfono: activa *Opciones de desarrollador* (toca 7 veces "Número de compilación") y la *Depuración USB*.
2. Debe tener instalado **Servicios de Google Play para RA** (ARCore). La mayoría de teléfonos recientes lo tienen.
3. En Unity: **Build Profiles → Android → Switch Platform → Build And Run** con el teléfono conectado.

## 5. Cómo se usa

1. Elige el tamaño: **Maqueta** (cabe en una mesa), **Piso** (unos 3 × 3 m) o **Tamaño real**.
2. Pulsa **Comenzar**, apunta al piso y mueve el teléfono despacio hasta ver el **círculo amarillo**.
3. Toca la pantalla: la escena aparece de lado frente a ti y empieza la simulación.
4. Al final: **Ver cómo debió ser** (versión segura) o **Repetir**. Abajo están **Repetir** y **Mover escena**.

## 6. Consejos para la presentación

- Piso **con textura y buena luz** (baldosa, tapete, cemento). Los pisos blancos, lisos o brillantes cuestan.
- Sube el volumen: hay sonido de frenazo y del golpe, y el teléfono vibra.
- Para que el profesor y el grupo vean la pantalla: en iPhone usa **Duplicar pantalla** (AirPlay) hacia un TV/Apple TV,
  o un adaptador Lightning/USB-C a HDMI. También puedes grabar la pantalla como respaldo por si falla el piso o la conexión.
- Practica una vez en el salón antes de presentar.

## 7. Si algo sale mal

| Problema | Solución |
|---|---|
| No aparece el menú **TdeA** | Hay un error de compilación. Mira la ventana *Console*. Asegúrate de haber usado la plantilla **AR Mobile** (necesita AR Foundation e Input System). |
| Los objetos se ven **rosados** | Ejecuta **TdeA → 2. Recrear materiales** y vuelve a compilar. |
| En el iPhone la cámara se ve **negra** | Revisa que ARKit esté marcado en XR Plug-in Management (iOS) y que hayas aceptado el permiso de cámara. |
| Nunca aparece el círculo amarillo | Más luz, piso con textura, mueve el teléfono de lado a lado unos segundos. |
| Xcode pide un "Team" o falla la firma | Paso 3.4: elige tu *Personal Team* y cambia el Bundle Identifier por uno único. |
| "Untrusted Developer" en el iPhone | Paso 3.8: confiar en tu Apple ID en Ajustes. |

## Archivos

- `Scripts/SimulacionVial.cs` — colocación en el piso, tiempos de la animación, versión de peligro y versión segura.
- `Scripts/EscenaTdeA.cs` — modelos low-poly: portería, andén, calle, carro, celular, estudiante, letreros.
- `Scripts/InterfazTdeA.cs` — pantallas, subtítulos, velocímetro y mensaje final (los textos se editan aquí).
- `Scripts/AudioTdeA.cs` — sonidos generados por código.
- `Scripts/MaterialesTdeA.cs` — colores y materiales.
- `Editor/ConfigurarSimulacion.cs` — el menú **TdeA** que arma la escena.
