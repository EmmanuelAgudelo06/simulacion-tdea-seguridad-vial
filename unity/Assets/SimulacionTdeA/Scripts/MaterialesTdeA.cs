using System.Collections.Generic;
using UnityEngine;

/// Materiales de la escena. Usa las plantillas creadas por el menú
/// "TdeA > Crear escena de la simulación" (carpeta Resources) para que
/// los shaders se incluyan en la app compilada.
public static class MaterialesTdeA
{
    static Material baseLit, baseUnlit;
    static readonly Dictionary<int, Material> litCache = new Dictionary<int, Material>();
    static readonly Dictionary<int, Material> unlitCache = new Dictionary<int, Material>();

    public static Color32 Hex(uint h) => new Color32((byte)(h >> 16), (byte)(h >> 8), (byte)h, 255);

    static int Key(Color32 c) => (c.r << 16) | (c.g << 8) | c.b;

    public static Material Lit(Color32 c)
    {
        int k = Key(c);
        if (litCache.TryGetValue(k, out var m)) return m;
        if (baseLit == null)
        {
            baseLit = Resources.Load<Material>("TdeA_Lit");
            if (baseLit == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh == null) sh = Shader.Find("Standard");
                baseLit = new Material(sh);
            }
        }
        m = new Material(baseLit);
        SetColor(m, c);
        litCache[k] = m;
        return m;
    }

    public static Material Unlit(Color32 c)
    {
        int k = Key(c);
        if (unlitCache.TryGetValue(k, out var m)) return m;
        if (baseUnlit == null)
        {
            baseUnlit = Resources.Load<Material>("TdeA_Unlit");
            if (baseUnlit == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Unlit");
                if (sh == null) sh = Shader.Find("Unlit/Color");
                baseUnlit = new Material(sh);
            }
        }
        m = new Material(baseUnlit);
        SetColor(m, c);
        unlitCache[k] = m;
        return m;
    }

    static void SetColor(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }
}
