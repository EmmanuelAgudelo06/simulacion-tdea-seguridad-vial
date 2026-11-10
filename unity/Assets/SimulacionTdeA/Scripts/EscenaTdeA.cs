using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// Referencias a las piezas animadas de la escena.
public class RefsEscena
{
    public Transform world, car, phone, ped, pivot, legL, legR, armL, armR, ring;
    public Transform[] wheels, skids;
}

/// Construye por código la escena low-poly de la salida vehicular del TdeA Sede Robledo.
/// Unidades en metros. Ejes: el carro sale del campus hacia +Z; el peatón camina por el andén hacia +X (Z = 0).
public static class EscenaTdeA
{
    static Font fuente;
    public static Font Fuente
    {
        get
        {
            if (fuente == null) fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return fuente;
        }
    }

    static Color32 C(uint h) => MaterialesTdeA.Hex(h);

    // ---------- Utilidades ----------
    public static Transform Grupo(Transform parent, string name, Vector3 pos)
    {
        var g = new GameObject(name).transform;
        g.SetParent(parent, false);
        g.localPosition = pos;
        return g;
    }

    public static Transform Prim(PrimitiveType t, Transform parent, Vector3 scale, uint color, Vector3 pos, bool sombra = true)
    {
        var g = GameObject.CreatePrimitive(t);
        var col = g.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        var r = g.GetComponent<MeshRenderer>();
        r.sharedMaterial = MaterialesTdeA.Lit(C(color));
        r.shadowCastingMode = sombra ? ShadowCastingMode.On : ShadowCastingMode.Off;
        r.receiveShadows = true;
        return g.transform;
    }

    public static Transform Box(Transform p, float w, float h, float d, uint color, float x, float y, float z)
        => Prim(PrimitiveType.Cube, p, new Vector3(w, h, d), color, new Vector3(x, y, z));

    public static Transform MeshObj(string name, Transform parent, Mesh mesh, Material mat, Vector3 pos, bool sombra = false)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = g.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = sombra ? ShadowCastingMode.On : ShadowCastingMode.Off;
        return g.transform;
    }

    /// Anillo (o disco si rIn = 0), visible por ambas caras. horizontal = en el plano XZ; si no, en XY.
    public static Mesh Anillo(float rIn, float rOut, int seg, bool horizontal)
    {
        var v = new List<Vector3>();
        var tris = new List<int>();
        for (int i = 0; i <= seg; i++)
        {
            float a = i * Mathf.PI * 2f / seg, c = Mathf.Cos(a), s = Mathf.Sin(a);
            v.Add(horizontal ? new Vector3(c * rIn, 0, s * rIn) : new Vector3(c * rIn, s * rIn, 0));
            v.Add(horizontal ? new Vector3(c * rOut, 0, s * rOut) : new Vector3(c * rOut, s * rOut, 0));
        }
        for (int i = 0; i < seg; i++)
        {
            int a = i * 2, b = a + 1, c2 = a + 2, d = a + 3;
            tris.AddRange(new[] { a, c2, b, b, c2, d, a, b, c2, b, d, c2 });
        }
        var m = new Mesh();
        m.SetVertices(v);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    /// Cono low-poly (copa de árbol), caras planas, visible por ambos lados.
    public static Mesh Cono(float r, float h, int seg)
    {
        var v = new List<Vector3>();
        var tris = new List<int>();
        var top = new Vector3(0, h, 0);
        for (int i = 0; i < seg; i++)
        {
            float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
            var p0 = new Vector3(Mathf.Cos(a0) * r, 0, Mathf.Sin(a0) * r);
            var p1 = new Vector3(Mathf.Cos(a1) * r, 0, Mathf.Sin(a1) * r);
            int n = v.Count;
            v.Add(p0); v.Add(top); v.Add(p1);
            v.Add(p1); v.Add(top); v.Add(p0);
            tris.AddRange(new[] { n, n + 1, n + 2, n + 3, n + 4, n + 5 });
        }
        var m = new Mesh();
        m.SetVertices(v);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    /// Letrero de texto en el mundo (Canvas en World Space). tam en metros.
    public static void Letrero(Transform parent, string texto, Vector3 pos, Vector3 euler, Vector2 tam, Color? fondo, Color color, int fontSize)
    {
        var go = new GameObject("Letrero_" + texto, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var sc = go.AddComponent<CanvasScaler>();
        sc.dynamicPixelsPerUnit = 4;
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = tam * 100f;
        rt.localPosition = pos;
        rt.localEulerAngles = euler;
        rt.localScale = Vector3.one * 0.01f;
        if (fondo.HasValue)
        {
            var img = go.AddComponent<Image>();
            img.color = fondo.Value;
            img.raycastTarget = false;
        }
        var t = new GameObject("Texto", typeof(RectTransform)).GetComponent<RectTransform>();
        t.SetParent(go.transform, false);
        t.anchorMin = Vector2.zero; t.anchorMax = Vector2.one;
        t.offsetMin = Vector2.zero; t.offsetMax = Vector2.zero;
        var tx = t.gameObject.AddComponent<Text>();
        tx.font = Fuente;
        tx.text = texto;
        tx.fontSize = fontSize;
        tx.fontStyle = FontStyle.Bold;
        tx.alignment = TextAnchor.MiddleCenter;
        tx.color = color;
        tx.horizontalOverflow = HorizontalWrapMode.Overflow;
        tx.verticalOverflow = VerticalWrapMode.Overflow;
        tx.raycastTarget = false;
    }

    static void Arbol(Transform w, float x, float z, float s)
    {
        Box(w, 0.25f * s, 1.2f * s, 0.25f * s, 0x6b4f3a, x, 0.6f * s, z);
        var copa = MeshObj("Copa", w, Cono(1.0f * s, 2.2f * s, 7), MaterialesTdeA.Lit(C(0x3f7d3a)), new Vector3(x, 1.0f * s, z), true);
        copa.GetComponent<MeshRenderer>().receiveShadows = true;
    }

    static Transform Extremidad(Transform pivot, float px, float py, float w, float h, float d, uint color)
    {
        var p = Grupo(pivot, "Extremidad", new Vector3(px, py, 0));
        Box(p, w, h, d, color, 0, -h / 2f, 0);
        return p;
    }

    // ---------- Escena ----------
    public static RefsEscena Construir(Transform root)
    {
        var e = new RefsEscena();
        // Centro de la escena: la acción queda sobre el punto donde se toca el piso
        var w = Grupo(root, "Mundo", new Vector3(0, 0, 3.5f));
        e.world = w;

        Box(w, 20, 0.12f, 27, 0x6f9a52, 0, -0.06f, -3.5f);              // base / zonas verdes
        Box(w, 3.8f, 0.03f, 16, 0x3b3e44, 0, 0.015f, -8);               // vía interna de salida
        Box(w, 20, 0.08f, 2, 0xcac5b8, 0, 0.04f, 0);                    // andén
        Box(w, 20, 0.1f, 0.18f, 0xe9e4d6, 0, 0.05f, 1.05f);             // sardinel
        Box(w, 20, 0.03f, 7, 0x34373d, 0, 0.015f, 4.6f);                // calle
        for (float x = -9; x < 10; x += 2.4f) Box(w, 1.2f, 0.035f, 0.14f, 0xf2c94c, x, 0.02f, 4.6f); // línea central
        Box(w, 3.5f, 0.035f, 0.28f, 0xf4f2ec, 0, 0.02f, -1.4f);        // línea de pare
        Letrero(w, "PARE", new Vector3(0, 0.04f, -2.6f), new Vector3(90, 0, 0), new Vector2(2.6f, 1.2f), null, new Color32(244, 242, 236, 255), 95);

        // Portería
        Box(w, 0.55f, 3.4f, 0.55f, 0xe9e4d6, -2.3f, 1.7f, -3.2f);
        Box(w, 0.55f, 3.4f, 0.55f, 0xe9e4d6, 2.3f, 1.7f, -3.2f);
        Box(w, 5.4f, 0.9f, 0.5f, 0xe9e4d6, 0, 3.6f, -3.2f);
        Letrero(w, "TdeA · SEDE ROBLEDO", new Vector3(0, 3.6f, -2.94f), new Vector3(0, 180, 0), new Vector2(5.0f, 0.78f), new Color32(31, 122, 61, 255), Color.white, 46);
        Box(w, 5.4f, 0.06f, 0.06f, 0xffd23f, 0, 3.08f, -2.95f);
        // Cerramiento del campus
        foreach (int sd in new[] { -1, 1 })
        {
            Box(w, 7.3f, 1.7f, 0.3f, 0xe9e4d6, sd * 6.25f, 0.85f, -3.2f);
            Box(w, 7.3f, 0.12f, 0.34f, 0x1f7a3d, sd * 6.25f, 1.74f, -3.2f);
        }
        // Caseta de vigilancia
        Box(w, 1.5f, 2.3f, 1.5f, 0xf4f2ec, 3.6f, 1.15f, -4.6f);
        Box(w, 1.9f, 0.18f, 1.9f, 0x1f7a3d, 3.6f, 2.38f, -4.6f);
        Box(w, 1.0f, 0.6f, 0.05f, 0x7fa7c2, 3.6f, 1.5f, -3.84f);
        // Señal de velocidad máxima 10 km/h
        Box(w, 0.08f, 2.2f, 0.08f, 0x9aa0a8, -2.75f, 1.1f, -1.9f);
        MeshObj("SenalFondo", w, Anillo(0f, 0.42f, 32, false), MaterialesTdeA.Unlit(C(0xffffff)), new Vector3(-2.75f, 2.2f, -1.85f));
        MeshObj("SenalBorde", w, Anillo(0.33f, 0.42f, 32, false), MaterialesTdeA.Unlit(C(0xd23a2a)), new Vector3(-2.75f, 2.2f, -1.84f));
        Letrero(w, "10", new Vector3(-2.75f, 2.2f, -1.83f), new Vector3(0, 180, 0), new Vector2(0.6f, 0.5f), null, new Color32(17, 17, 17, 255), 34);
        // Edificios y árboles
        Box(w, 6, 4, 4, 0xe9e4d6, -6.5f, 2, -11); Box(w, 6.1f, 0.3f, 4.1f, 0x1f7a3d, -6.5f, 4.1f, -11);
        for (int i = 0; i < 4; i++) Box(w, 0.9f, 0.9f, 0.05f, 0x7fa7c2, -8.5f + i * 1.4f, 2.6f, -8.97f);
        Box(w, 5, 3, 5, 0xdcd5c4, 6.5f, 1.5f, -12); Box(w, 5.1f, 0.25f, 5.1f, 0x1f7a3d, 6.5f, 3.1f, -12);
        float[,] arboles = { { -4.5f, -6 }, { -8.5f, -5.2f }, { 4.6f, -7.5f }, { 8.5f, -6 }, { -3.5f, -14.5f }, { 3.5f, -15.5f }, { -8.8f, -15 }, { 8.8f, -16 } };
        for (int i = 0; i < arboles.GetLength(0); i++) Arbol(w, arboles[i, 0], arboles[i, 1], 0.8f + (i % 3) * 0.15f);

        // ---------- Carro ----------
        var car = Grupo(w, "Carro", Vector3.zero);
        e.car = car;
        Box(car, 1.9f, 0.62f, 4.0f, 0xc8402b, 0, 0.6f, 0);
        Box(car, 1.62f, 0.55f, 2.1f, 0xc8402b, 0, 1.18f, -0.25f);
        Box(car, 1.64f, 0.42f, 0.05f, 0x223042, 0, 1.2f, 0.81f);
        Box(car, 1.64f, 0.42f, 0.05f, 0x223042, 0, 1.2f, -1.31f);
        Box(car, 0.05f, 0.38f, 1.7f, 0x223042, -0.82f, 1.2f, -0.25f);
        Box(car, 0.05f, 0.38f, 1.7f, 0x223042, 0.82f, 1.2f, -0.25f);
        Box(car, 0.36f, 0.14f, 0.06f, 0xfff6c8, -0.62f, 0.66f, 2.01f);
        Box(car, 0.36f, 0.14f, 0.06f, 0xfff6c8, 0.62f, 0.66f, 2.01f);
        Box(car, 0.36f, 0.12f, 0.06f, 0x8a1010, -0.62f, 0.68f, -2.01f);
        Box(car, 0.36f, 0.12f, 0.06f, 0x8a1010, 0.62f, 0.68f, -2.01f);
        Box(car, 1.95f, 0.16f, 0.12f, 0x2a2a2a, 0, 0.36f, 2.0f);
        Box(car, 1.95f, 0.16f, 0.12f, 0x2a2a2a, 0, 0.36f, -2.0f);
        var ruedas = new List<Transform>();
        float[,] pr = { { 0.92f, 1.3f }, { -0.92f, 1.3f }, { 0.92f, -1.3f }, { -0.92f, -1.3f } };
        for (int i = 0; i < 4; i++)
        {
            var g = Grupo(car, "Rueda", new Vector3(pr[i, 0], 0.34f, pr[i, 1]));
            var llanta = Prim(PrimitiveType.Cylinder, g, new Vector3(0.68f, 0.13f, 0.68f), 0x1c1c1c, Vector3.zero);
            llanta.localEulerAngles = new Vector3(0, 0, 90);
            Box(g, 0.28f, 0.4f, 0.08f, 0x9aa0a8, 0, 0, 0);
            ruedas.Add(g);
        }
        e.wheels = ruedas.ToArray();

        // Celular del conductor (la distracción): siempre mira a la cámara
        var phone = Grupo(car, "Celular", new Vector3(-0.4f, 2.35f, -0.1f));
        e.phone = phone;
        MeshObj("Halo", phone, Anillo(0.34f, 0.44f, 28, false), MaterialesTdeA.Unlit(C(0x9fd0ff)), new Vector3(0, 0, 0.02f));
        var cuerpo = Box(phone, 0.3f, 0.56f, 0.04f, 0x111111, 0, 0, 0);
        cuerpo.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        var pantalla = Box(phone, 0.24f, 0.46f, 0.05f, 0x59a8ff, 0, 0, 0);
        pantalla.GetComponent<MeshRenderer>().sharedMaterial = MaterialesTdeA.Unlit(C(0x59a8ff));
        pantalla.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

        // Marcas de frenado
        e.skids = new Transform[2];
        for (int i = 0; i < 2; i++)
        {
            var s = Box(w, 0.24f, 0.012f, 1, 0x111111, i == 0 ? -0.92f : 0.92f, 0.09f, 0);
            s.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            s.gameObject.SetActive(false);
            e.skids[i] = s;
        }

        // ---------- Peatón: estudiante con morral ----------
        var ped = Grupo(w, "Peaton", Vector3.zero);
        e.ped = ped;
        var pivot = Grupo(ped, "Centro", new Vector3(0, 0.95f, 0));
        e.pivot = pivot;
        e.legL = Extremidad(pivot, -0.12f, 0, 0.17f, 0.92f, 0.19f, 0x2c3a55);
        e.legR = Extremidad(pivot, 0.12f, 0, 0.17f, 0.92f, 0.19f, 0x2c3a55);
        Box(e.legL, 0.2f, 0.08f, 0.28f, 0xf4f2ec, 0, -0.9f, 0.04f);
        Box(e.legR, 0.2f, 0.08f, 0.28f, 0xf4f2ec, 0, -0.9f, 0.04f);
        Box(pivot, 0.46f, 0.62f, 0.26f, 0xf2b630, 0, 0.31f, 0);          // camiseta
        Prim(PrimitiveType.Sphere, pivot, Vector3.one * 0.3f, 0xb9805a, new Vector3(0, 0.78f, 0));
        Box(pivot, 0.3f, 0.1f, 0.3f, 0x2a1c14, 0, 0.9f, -0.01f);          // cabello
        e.armL = Extremidad(pivot, -0.29f, 0.58f, 0.11f, 0.6f, 0.13f, 0xf2b630);
        e.armR = Extremidad(pivot, 0.29f, 0.58f, 0.11f, 0.6f, 0.13f, 0xf2b630);
        Box(pivot, 0.36f, 0.44f, 0.18f, 0x2f6db5, 0, 0.32f, -0.22f);      // morral

        // Onda del impacto
        e.ring = MeshObj("OndaImpacto", w, Anillo(0.6f, 0.75f, 40, true), MaterialesTdeA.Unlit(C(0xffd23f)), new Vector3(0, 0.12f, 0));
        e.ring.gameObject.SetActive(false);
        return e;
    }

    public static Transform CrearReticula()
    {
        var t = MeshObj("Reticula", null, Anillo(0.09f, 0.12f, 32, true), MaterialesTdeA.Unlit(C(0xffd23f)), Vector3.zero);
        MeshObj("Punto", t, Anillo(0f, 0.015f, 12, true), MaterialesTdeA.Unlit(C(0xffd23f)), Vector3.zero);
        return t;
    }
}
