# Baja la velocidad — Simulación de seguridad vial en RA (TdeA)

Proyecto de Unity para iPhone y Android. El código de la simulación está en
`unity/Assets/SimulacionTdeA/` (esta carpeta `unity/` es el proyecto; ábrela en Unity Hub) (ver `Assets/SimulacionTdeA/LEEME.md` para el detalle de uso,
instalación en el iPhone y solución de problemas).

## Estructura

```
Assets/SimulacionTdeA/
  Scripts/   simulación, escena low-poly, interfaz, audio y materiales (todo por código)
  Editor/    menú "TdeA" que arma la escena y la configuración del proyecto
Packages/manifest.json   AR Foundation, ARKit, ARCore, XR Management, Input System, uGUI
ProjectSettings/         Unity completa el resto al abrir el proyecto
```

El proyecto usa el **render pipeline integrado (Built-in)**, así que no hace falta URP:
los scripts usan automáticamente los shaders `Standard` y `Unlit/Color`.

## Primera vez (configuración)

1. Instala **Unity Hub** y desde ahí **Unity 6 LTS (6000.0.x)** con los módulos
   *iOS Build Support* y, si quieres Android, *Android Build Support* (con OpenJDK y SDK & NDK).
2. Unity Hub → **Add → Add project from disk** → selecciona esta carpeta.
   Si avisa que la versión del editor es distinta, elige la 6000.0.x que instalaste y continúa.
3. Al abrir, Unity descarga los paquetes. Si pregunta por el **nuevo Input System**
   ("enable the new input backends?"), responde **Yes**: Unity se reinicia.
   (Si no pregunta: *Edit → Project Settings → Player → Other Settings → Active Input Handling* = **Input System Package (New)** o **Both**.)
4. **Edit → Project Settings → XR Plug-in Management**:
   - pestaña iOS: marca **Apple ARKit**;
   - pestaña Android: marca **Google ARCore**.
5. Menú **TdeA → 1. Crear escena de la simulación**. Crea `Assets/SimulacionTdeA/SimulacionTdeA.unity`,
   la deja como escena del build y pone nombre, identificador y permiso de cámara.
6. Para Android además: *Player → Other Settings*: **Graphics APIs** solo OpenGLES3 o Vulkan,
   **Minimum API Level** 24 o más, **Scripting Backend** IL2CPP y **ARM64** marcado.

Luego sigue la sección 3 (iPhone) o 4 (Android) de `Assets/SimulacionTdeA/LEEME.md`.
