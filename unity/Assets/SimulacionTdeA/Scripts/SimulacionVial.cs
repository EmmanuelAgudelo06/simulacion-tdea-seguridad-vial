using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// Simulación de seguridad vial en RA: salida vehicular del TdeA Sede Robledo.
/// Va en un GameObject de la escena (el menú "TdeA > Crear escena de la simulación" lo crea).
public class SimulacionVial : MonoBehaviour
{
    // Cinemática (metros, segundos)
    const float CAR_FRONT = 2f, PED_R = 0.3f, Z0 = -13f, T0 = 0.5f, ACC = 3.5f, VMAX = 8.9f, WALK = 1.25f;
    const float SAFE_PED_T = 5.3f;     // en la versión segura el peatón pasa justo cuando el carro hace el pare
    const float LINEA_PARE = -1.45f;
    static readonly float VIEW_YAW = Mathf.Atan2(1f, 0.45f) * Mathf.Rad2Deg; // se ve de lado, un poco en diagonal

    enum Estado { Inicio, Buscando, Corriendo }
    Estado estado = Estado.Inicio;
    float escala = 0.12f;
    float bloqueoToque;

    Transform root, reticula;
    RefsEscena e;
    InterfazTdeA ui;
    AudioTdeA audioFx;
    ARRaycastManager raycaster;
    Camera cam;
    static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    // Estado de la simulación
    bool seguro, corriendo, impacto, final, detenido;
    float t, carZ, carV, pedX, brakeAt, stopT, ringT = -1f, tImpacto, tAcc, dAcc;
    float? skidStart;
    string fase;
    Vector3 pos, vel;

    void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        tAcc = VMAX / ACC;
        dAcc = 0.5f * ACC * tAcc * tAcc;
        float dHit = (-PED_R) - (Z0 + CAR_FRONT);
        tImpacto = T0 + (dHit <= dAcc ? Mathf.Sqrt(2f * dHit / ACC) : tAcc + (dHit - dAcc) / VMAX);

        root = new GameObject("EscenaTdeA").transform;
        e = EscenaTdeA.Construir(root);
        root.gameObject.SetActive(false);
        reticula = EscenaTdeA.CrearReticula();
        reticula.gameObject.SetActive(false);

        ui = InterfazTdeA.Crear();
        ui.OnEscala = s => escala = s;
        ui.OnComenzar = EmpezarBusqueda;
        ui.OnMover = EmpezarBusqueda;
        ui.OnRepetir = () => { if (estado == Estado.Corriendo) Reiniciar(seguro); };
        ui.OnFinal = s => Reiniciar(s);

        audioFx = gameObject.AddComponent<AudioTdeA>();

        if (FindFirstObjectByType<Light>() == null)
        {
            var l = new GameObject("Sol").AddComponent<Light>();
            l.type = LightType.Directional; l.shadows = LightShadows.Soft; l.intensity = 1.2f;
            l.transform.rotation = Quaternion.Euler(50, -30, 0);
        }
    }

    void Start()
    {
        raycaster = FindFirstObjectByType<ARRaycastManager>();
        cam = Camera.main;
        if (cam != null && FindFirstObjectByType<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
        if (raycaster == null) Debug.LogWarning("[TdeA] No hay ARRaycastManager en la escena. Ejecuta el menú TdeA > Crear escena de la simulación.");
    }

    // ---------- Colocación en el piso ----------
    void EmpezarBusqueda()
    {
        estado = Estado.Buscando;
        corriendo = false;
        bloqueoToque = Time.time + 0.4f;
        root.gameObject.SetActive(false);
        ui.MostrarInicio(false);
        ui.OcultarFinal();
        ui.MostrarHud(false);
        ui.MostrarControles(false);
        ui.MostrarIndicacion(true, Application.isEditor
            ? "Prueba en el editor: haz clic para colocar la escena frente a la cámara."
            : "Apunta la cámara al piso y muévela despacio. Cuando veas el círculo amarillo, toca la pantalla para colocar la escena.");
    }

    void ActualizarReticula()
    {
        if (raycaster != null && raycaster.Raycast(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), hits, TrackableType.PlaneWithinPolygon))
        {
            reticula.SetPositionAndRotation(hits[0].pose.position, Quaternion.identity);
            reticula.gameObject.SetActive(true);
        }
        else reticula.gameObject.SetActive(false);
    }

    void Colocar()
    {
        Vector3 p;
        if (reticula.gameObject.activeSelf) p = reticula.position;
        else if (Application.isEditor && cam != null)
        {
            var f = cam.transform.forward; f.y = 0;
            if (f.sqrMagnitude < 0.001f) f = Vector3.forward;
            f.Normalize();
            p = cam.transform.position + f * 4f + Vector3.down * 1.4f;
        }
        else return;

        root.position = p;
        root.localScale = Vector3.one * escala;
        var cp = cam != null ? cam.transform.position : p + Vector3.back;
        float yaw = Mathf.Atan2(cp.x - p.x, cp.z - p.z) * Mathf.Rad2Deg - VIEW_YAW;
        root.rotation = Quaternion.Euler(0, yaw, 0);
        root.gameObject.SetActive(true);
        reticula.gameObject.SetActive(false);
        estado = Estado.Corriendo;
        ui.MostrarIndicacion(false);
        ui.MostrarControles(true);
        Reiniciar(false);
    }

    static bool Toque(out Vector2 p)
    {
        p = default;
        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.wasPressedThisFrame) { p = ts.primaryTouch.position.ReadValue(); return true; }
        var m = Mouse.current;
        if (m != null && m.leftButton.wasPressedThisFrame) { p = m.position.ReadValue(); return true; }
        return false;
    }

    static bool SobreUI(Vector2 p)
    {
        var es = EventSystem.current;
        if (es == null) return false;
        var data = new PointerEventData(es) { position = p };
        var res = new List<RaycastResult>();
        es.RaycastAll(data, res);
        return res.Count > 0;
    }

    // ---------- Bucle ----------
    void Update()
    {
        if (cam == null) cam = Camera.main;

        if (estado == Estado.Buscando)
        {
            ActualizarReticula();
            if (Time.time > bloqueoToque && Toque(out var tp) && !SobreUI(tp)) Colocar();
        }

        if (corriendo)
        {
            float rdt = Mathf.Min(Time.deltaTime, 0.05f);
            float ts = EscalaTiempo();
            for (int i = 0; i < 3; i++) Paso(rdt * ts / 3f);
            if (ringT >= 0)
            {
                ringT += rdt;
                float k = ringT / 0.7f;
                e.ring.localScale = Vector3.one * (1 + k * 4);
                if (k >= 1) { ringT = -1; e.ring.gameObject.SetActive(false); }
            }
        }

        if (cam != null && e.phone.gameObject.activeInHierarchy)
            e.phone.rotation = Quaternion.LookRotation(e.phone.position - cam.transform.position, Vector3.up);
    }

    float EscalaTiempo()
    {
        if (!seguro && t > tImpacto - 0.3f && t < tImpacto + 0.6f) return 0.28f; // cámara lenta en el impacto
        return 1f;
    }

    void Reiniciar(bool modoSeguro)
    {
        seguro = modoSeguro;
        t = 0; carZ = Z0; carV = 0; fase = "walk";
        impacto = false; final = false; detenido = false; skidStart = null; ringT = -1;
        e.car.localPosition = new Vector3(0, 0, Z0);
        e.phone.gameObject.SetActive(!seguro);
        e.pivot.localRotation = Quaternion.identity;
        e.pivot.localPosition = new Vector3(0, 0.95f, 0);
        e.ped.localRotation = Quaternion.Euler(0, 90, 0);
        foreach (var l in new[] { e.legL, e.legR, e.armL, e.armR }) l.localRotation = Quaternion.identity;
        foreach (var s in e.skids) s.gameObject.SetActive(false);
        e.ring.gameObject.SetActive(false);
        SetPedX(WALK * (0 - (seguro ? SAFE_PED_T : tImpacto)));
        ui.OcultarFinal();
        ui.MostrarHud(true);
        ui.Subtitulo("");
        ui.Velocidad(0);
        corriendo = true;
    }

    void SetPedX(float x) { pedX = x; e.ped.localPosition = new Vector3(x, 0, 0); }

    void Caminar(float tt, float amp = 0.55f)
    {
        float s = Mathf.Sin(tt * 7.5f) * amp * Mathf.Rad2Deg;
        e.legL.localRotation = Quaternion.Euler(s, 0, 0);
        e.legR.localRotation = Quaternion.Euler(-s, 0, 0);
        e.armL.localRotation = Quaternion.Euler(-s * 0.8f, 0, 0);
        e.armR.localRotation = Quaternion.Euler(s * 0.8f, 0, 0);
        e.pivot.localPosition = new Vector3(0, 0.95f + Mathf.Abs(Mathf.Cos(tt * 7.5f)) * 0.03f * (amp > 0 ? 1 : 0), 0);
    }

    void CarroPeligro(float tt, out float z, out float v)
    {
        float tau = Mathf.Max(0, tt - T0);
        if (tau <= tAcc) { z = Z0 + 0.5f * ACC * tau * tau; v = ACC * tau; }
        else { z = Z0 + dAcc + VMAX * (tau - tAcc); v = VMAX; }
    }

    void Paso(float dt)
    {
        t += dt;
        if (seguro) PasoSeguro(dt); else PasoPeligro(dt);
        e.car.localPosition = new Vector3(0, 0, carZ);
        float giro = carV * dt / 0.34f * Mathf.Rad2Deg;
        foreach (var w in e.wheels) w.localRotation *= Quaternion.Euler(giro, 0, 0);
        e.phone.localPosition = new Vector3(-0.4f, 2.35f + Mathf.Sin(t * 3f) * 0.08f, -0.1f);
        ui.Velocidad(carV);
    }

    void PasoPeligro(float dt)
    {
        if (!impacto)
        {
            CarroPeligro(Mathf.Min(t, tImpacto), out carZ, out carV);
            SetPedX(WALK * (t - tImpacto));
            Caminar(t);
            ui.Subtitulo(t < 1.1f ? "Salida vehicular · TdeA Sede Robledo" : "El conductor va mirando el celular y no baja la velocidad");
            if (t >= tImpacto)
            {
                impacto = true; fase = "air"; SetPedX(0);
                pos = new Vector3(0, 0.95f, 0);
                vel = new Vector3(0.5f, 4.3f, carV * 0.85f);
                brakeAt = t + 0.18f;
                ringT = 0; e.ring.localScale = Vector3.one; e.ring.gameObject.SetActive(true);
                ui.Flash(); audioFx.Golpe(); Vibrar();
                ui.Subtitulo("¡No alcanzó a frenar!");
            }
            return;
        }

        // Carro: reacción tardía y frenado en seco
        if (t >= brakeAt && carV > 0)
        {
            if (!skidStart.HasValue)
            {
                skidStart = carZ - 1.3f;
                foreach (var s in e.skids) s.gameObject.SetActive(true);
                audioFx.Frenazo();
            }
            carV = Mathf.Max(0, carV - 7.5f * dt);
        }
        carZ += carV * dt;
        if (skidStart.HasValue)
        {
            float L = Mathf.Max(0.01f, (carZ - 1.3f) - skidStart.Value + 0.4f);
            foreach (var s in e.skids)
            {
                s.localScale = new Vector3(0.24f, 0.012f, L);
                s.localPosition = new Vector3(s.localPosition.x, 0.09f, skidStart.Value + L / 2f);
            }
        }

        // Peatón proyectado por el impacto
        if (fase == "air")
        {
            vel.y -= 9.8f * dt;
            pos += vel * dt;
            e.pivot.localRotation *= Quaternion.Euler(0, 0, 8.5f * dt * Mathf.Rad2Deg);
            e.legL.localRotation = Quaternion.Euler(52, 0, 0);
            e.legR.localRotation = Quaternion.Euler(-29, 0, 0);
            e.armL.localRotation = Quaternion.Euler(0, 0, -69);
            e.armR.localRotation = Quaternion.Euler(0, 0, 80);
            if (pos.y <= 0.2f && vel.y < 0)
            {
                fase = "slide"; pos.y = 0.2f;
                e.pivot.localRotation = Quaternion.Euler(0, 0, 90);
                Vibrar();
            }
        }
        else if (fase == "slide")
        {
            float sp = new Vector2(vel.x, vel.z).magnitude;
            if (sp > 0.01f)
            {
                float k = Mathf.Max(0, sp - 7f * dt) / sp;
                vel.x *= k; vel.z *= k;
                pos.x += vel.x * dt; pos.z += vel.z * dt;
            }
            else fase = "down";
        }
        e.ped.localPosition = new Vector3(pos.x, pos.y - 0.95f, pos.z);
        e.pivot.localPosition = new Vector3(0, 0.95f, 0);

        if (t > tImpacto + 1.4f) ui.Subtitulo("");
        if (!final && t > tImpacto + 2.4f) { final = true; ui.MostrarFinal(false); }
    }

    void PasoSeguro(float dt)
    {
        const float vmax = 2.8f; // 10 km/h
        float front = carZ + CAR_FRONT;
        bool pedEnZona = pedX > -2.6f && pedX < 3.0f;
        float aLinea = LINEA_PARE - front;
        float? objetivo = t < T0 ? 0f : vmax;

        if (!detenido)
        {
            // Se acerca a la línea de pare y frena hasta detenerse por completo
            float necesita = carV * carV / (2f * Mathf.Max(aLinea, 0.01f));
            if (t > T0 && (necesita > 0.9f || aLinea < 0.08f))
            {
                carV = Mathf.Max(0, carV - Mathf.Max(necesita, 2.2f) * dt);
                objetivo = null;
                if (carV < 0.05f) { carV = 0; detenido = true; stopT = t; }
            }
        }
        else if (pedEnZona || t - stopT < 1.2f) { carV = 0; objetivo = null; } // espera a que el peatón pase

        if (objetivo.HasValue)
        {
            float o = objetivo.Value;
            carV = carV < o ? Mathf.Min(o, carV + 3f * dt) : Mathf.Max(o, carV - 3f * dt);
        }
        carZ += carV * dt;

        if (pedX < 8f) { SetPedX(WALK * (t - SAFE_PED_T)); Caminar(t); }
        else Caminar(0, 0);

        if (t < 1.1f) ui.Subtitulo("La misma salida, otra decisión");
        else if (!detenido) ui.Subtitulo("Celular guardado, 10 km/h y mirada en el andén");
        else if (carV == 0) ui.Subtitulo("Pare total: el peatón en el andén tiene la prioridad");
        else ui.Subtitulo("Ahora sí: salida segura");

        if (!final && t > SAFE_PED_T + 5.2f) { final = true; ui.MostrarFinal(true); }
    }

    static void Vibrar()
    {
#if UNITY_IOS || UNITY_ANDROID
        Handheld.Vibrate();
#endif
    }
}
