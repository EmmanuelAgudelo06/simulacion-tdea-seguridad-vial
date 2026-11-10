using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Interfaz en pantalla construida por código: inicio, subtítulos, velocímetro, mensaje final y controles.
public class InterfazTdeA : MonoBehaviour
{
    public Action<float> OnEscala;
    public Action OnComenzar, OnRepetir, OnMover;
    public Action<bool> OnFinal; // true = versión segura

    static readonly Color Asfalto = new Color32(22, 24, 28, 235);
    static readonly Color Asfalto2 = new Color32(40, 43, 50, 255);
    static readonly Color Amarillo = new Color32(255, 210, 63, 255);
    static readonly Color Rojo = new Color32(229, 72, 47, 255);
    static readonly Color Verde = new Color32(63, 191, 111, 255);
    static readonly Color Blanco = new Color32(244, 242, 236, 255);
    static readonly Color Gris = new Color32(169, 173, 181, 255);
    static readonly Color Oscuro = new Color32(20, 20, 20, 255);

    RectTransform safe;
    GameObject inicio, hud, hint, final, controles;
    Text caption, kmh, hintText, fEyebrow, fTitle, fText, fTips, fBtnALabel, fBtnBLabel;
    Image speedBar, flash;
    Image[] segImgs; Text[] segTxts;
    float flashA;
    Rect lastSafe;

    static Font F => EscenaTdeA.Fuente;

    public static InterfazTdeA Crear()
    {
        var go = new GameObject("InterfazTdeA", typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var sc = go.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1080, 1920);
        sc.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
        var ui = go.AddComponent<InterfazTdeA>();
        ui.Construir((RectTransform)go.transform);
        return ui;
    }

    // ---------- Utilidades de UI ----------
    static RectTransform Nuevo(string n, Transform p)
    {
        var r = new GameObject(n, typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(p, false);
        return r;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
    }

    static Image Fondo(RectTransform r, Color c)
    {
        var i = r.gameObject.AddComponent<Image>();
        i.color = c;
        return i;
    }

    static RectTransform Panel(Transform p, string n, Color bg, int pad, int spacing)
    {
        var r = Nuevo(n, p);
        Fondo(r, bg);
        var v = r.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(pad, pad, pad, pad);
        v.spacing = spacing;
        v.childControlWidth = true; v.childControlHeight = true;
        v.childForceExpandWidth = true; v.childForceExpandHeight = false;
        var f = r.gameObject.AddComponent<ContentSizeFitter>();
        f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return r;
    }

    static Text Texto(Transform p, string s, int size, Color c, FontStyle st = FontStyle.Normal, TextAnchor a = TextAnchor.UpperLeft)
    {
        var r = Nuevo("Texto", p);
        var t = r.gameObject.AddComponent<Text>();
        t.font = F; t.text = s; t.fontSize = size; t.color = c; t.fontStyle = st; t.alignment = a;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.lineSpacing = 1.05f;
        t.raycastTarget = false;
        return t;
    }

    static void Franja(Transform p, Color c)
    {
        var r = Nuevo("Franja", p);
        Fondo(r, c).raycastTarget = false;
        var le = r.gameObject.AddComponent<LayoutElement>();
        le.minHeight = 14; le.preferredHeight = 14;
    }

    static RectTransform Fila(Transform p, int spacing)
    {
        var r = Nuevo("Fila", p);
        var h = r.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = spacing;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = true; h.childForceExpandHeight = false;
        return r;
    }

    static Button Boton(Transform p, string s, Color bg, Color fg, Action click, out Text label, out Image img, int alto = 130, int fontSize = 42)
    {
        var r = Nuevo("Boton", p);
        img = Fondo(r, bg);
        var b = r.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(() => click());
        var le = r.gameObject.AddComponent<LayoutElement>();
        le.minHeight = alto; le.preferredHeight = alto; le.flexibleWidth = 1;
        label = Texto(r, s.ToUpperInvariant(), fontSize, fg, FontStyle.Bold, TextAnchor.MiddleCenter);
        Stretch(label.rectTransform);
        return b;
    }

    // ---------- Construcción ----------
    void Construir(RectTransform canvasRT)
    {
        var fl = Nuevo("Destello", canvasRT); Stretch(fl);
        flash = Fondo(fl, new Color(Rojo.r, Rojo.g, Rojo.b, 0));
        flash.raycastTarget = false;

        safe = Nuevo("AreaSegura", canvasRT); Stretch(safe);
        AplicarAreaSegura();

        // Inicio
        var ini = Panel(safe, "Inicio", Asfalto, 56, 22);
        ini.anchorMin = ini.anchorMax = ini.pivot = new Vector2(0.5f, 0.5f);
        ini.sizeDelta = new Vector2(980, 0);
        inicio = ini.gameObject;
        Franja(ini, Amarillo);
        Texto(ini, "SEGURIDAD VIAL · TdeA SEDE ROBLEDO", 32, Amarillo, FontStyle.Bold);
        Texto(ini, "BAJA LA VELOCIDAD", 96, Blanco, FontStyle.Bold);
        Texto(ini, "Una salida vehicular, un conductor distraído y un peatón en el andén. Coloca la escena en el piso y mira lo que pasa en un segundo.", 40, new Color32(216, 214, 207, 255));
        Texto(ini, "Tamaño de la escena", 32, Gris);
        var seg = Fila(ini, 0);
        string[] nombres = { "Maqueta", "Piso", "Tamaño real" };
        float[] escalas = { 0.05f, 0.12f, 1f };
        segImgs = new Image[3]; segTxts = new Text[3];
        for (int i = 0; i < 3; i++)
        {
            int k = i;
            Boton(seg, nombres[i], Asfalto2, Blanco, () => { MarcarEscala(k); OnEscala?.Invoke(escalas[k]); }, out segTxts[i], out segImgs[i], 110, 36);
        }
        MarcarEscala(1);
        Boton(ini, "Comenzar", Amarillo, Oscuro, () => OnComenzar?.Invoke(), out _, out _);
        Texto(ini, "Busca un piso con buena luz y textura (no un piso brillante o blanco liso). Para \"Piso\" deja unos 3 × 3 metros libres.", 30, Gris);

        // HUD superior
        var h = Nuevo("HUD", safe);
        h.anchorMin = new Vector2(0, 1); h.anchorMax = new Vector2(1, 1); h.pivot = new Vector2(0.5f, 1);
        h.offsetMin = new Vector2(40, -260); h.offsetMax = new Vector2(-40, -40);
        var hl = h.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 20; hl.childControlWidth = true; hl.childControlHeight = true;
        hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;
        hl.childAlignment = TextAnchor.UpperRight;
        hud = h.gameObject;
        var cap = Panel(h, "Subtitulo", Asfalto, 30, 0);
        cap.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        caption = Texto(cap, "", 44, Blanco, FontStyle.Bold);
        var sp = Panel(h, "Velocimetro", Asfalto, 20, 4);
        var spLe = sp.gameObject.AddComponent<LayoutElement>();
        spLe.minWidth = 230; spLe.preferredWidth = 230;
        kmh = Texto(sp, "0", 84, Blanco, FontStyle.Bold, TextAnchor.MiddleCenter);
        Texto(sp, "KM/H", 26, Gris, FontStyle.Normal, TextAnchor.MiddleCenter);
        var bar = Nuevo("Barra", sp);
        speedBar = Fondo(bar, Verde);
        var barLe = bar.gameObject.AddComponent<LayoutElement>();
        barLe.minHeight = 10; barLe.preferredHeight = 10;
        hud.SetActive(false);

        // Indicación para colocar la escena
        var hi = Panel(safe, "Indicacion", Asfalto, 36, 0);
        hi.anchorMin = hi.anchorMax = hi.pivot = new Vector2(0.5f, 0);
        hi.anchoredPosition = new Vector2(0, 260);
        hi.sizeDelta = new Vector2(940, 0);
        hintText = Texto(hi, "Apunta la cámara al piso y muévela despacio. Cuando veas el círculo amarillo, toca la pantalla para colocar la escena.", 40, Blanco, FontStyle.Normal, TextAnchor.MiddleCenter);
        hint = hi.gameObject;
        hint.SetActive(false);

        // Mensaje final
        var fi = Panel(safe, "Final", Asfalto, 52, 22);
        fi.anchorMin = new Vector2(0, 0); fi.anchorMax = new Vector2(1, 0); fi.pivot = new Vector2(0.5f, 0);
        fi.offsetMin = new Vector2(40, 0); fi.offsetMax = new Vector2(-40, 0);
        fi.anchoredPosition = new Vector2(0, 210);
        final = fi.gameObject;
        Franja(fi, Amarillo);
        fEyebrow = Texto(fi, "", 34, Amarillo, FontStyle.Bold);
        fTitle = Texto(fi, "", 70, Blanco, FontStyle.Bold);
        fText = Texto(fi, "", 38, new Color32(216, 214, 207, 255));
        fTips = Texto(fi, "", 38, Blanco);
        var fr = Fila(fi, 20);
        Boton(fr, "-", Amarillo, Oscuro, () => OnFinal?.Invoke(true), out fBtnALabel, out _, 130, 36);
        Boton(fr, "-", Asfalto2, Blanco, () => OnFinal?.Invoke(false), out fBtnBLabel, out _, 130, 36);
        final.SetActive(false);

        // Controles inferiores
        var co = Fila(safe, 20);
        co.anchorMin = new Vector2(0, 0); co.anchorMax = new Vector2(1, 0); co.pivot = new Vector2(0.5f, 0);
        co.offsetMin = new Vector2(40, 40); co.offsetMax = new Vector2(-40, 170);
        co.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
        Boton(co, "Repetir", Asfalto, Blanco, () => OnRepetir?.Invoke(), out _, out _, 120, 38);
        Boton(co, "Mover escena", Asfalto, Blanco, () => OnMover?.Invoke(), out _, out _, 120, 38);
        controles = co.gameObject;
        controles.SetActive(false);
    }

    void MarcarEscala(int k)
    {
        for (int i = 0; i < segImgs.Length; i++)
        {
            segImgs[i].color = i == k ? Blanco : Asfalto2;
            segTxts[i].color = i == k ? Oscuro : Blanco;
        }
    }

    void AplicarAreaSegura()
    {
        var sa = Screen.safeArea;
        lastSafe = sa;
        if (Screen.width <= 0 || Screen.height <= 0) return;
        safe.anchorMin = new Vector2(sa.x / Screen.width, sa.y / Screen.height);
        safe.anchorMax = new Vector2((sa.x + sa.width) / Screen.width, (sa.y + sa.height) / Screen.height);
        safe.offsetMin = Vector2.zero; safe.offsetMax = Vector2.zero;
    }

    void Update()
    {
        if (Screen.safeArea != lastSafe) AplicarAreaSegura();
        if (flashA > 0)
        {
            flashA = Mathf.MoveTowards(flashA, 0, Time.unscaledDeltaTime * 1.1f);
            var c = flash.color; c.a = flashA; flash.color = c;
        }
    }

    // ---------- API usada por la simulación ----------
    public void MostrarInicio(bool v) => inicio.SetActive(v);
    public void MostrarHud(bool v) => hud.SetActive(v);
    public void MostrarControles(bool v) => controles.SetActive(v);
    public void MostrarIndicacion(bool v, string texto = null)
    {
        if (texto != null) hintText.text = texto;
        hint.SetActive(v);
    }
    public void OcultarFinal() => final.SetActive(false);
    public void Flash() { flashA = 0.55f; }

    public void Subtitulo(string s)
    {
        if (caption.text != s) caption.text = s;
        caption.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(s));
    }

    public void Velocidad(float ms)
    {
        int k = Mathf.RoundToInt(ms * 3.6f);
        kmh.text = k.ToString();
        bool mal = k > 12;
        kmh.color = mal ? Rojo : Blanco;
        speedBar.color = mal ? Rojo : Verde;
    }

    public void MostrarFinal(bool seguro)
    {
        if (!seguro)
        {
            fEyebrow.text = "RECUERDA"; fEyebrow.color = Amarillo;
            fTitle.text = "UN SEGUNDO DE DISTRACCIÓN CAMBIA UNA VIDA.";
            fText.text = "A 30 km/h un carro avanza más de 8 metros cada segundo y necesita unos 13 metros para detenerse. En la entrada del TdeA, baja la velocidad: aquí caminamos todos.";
            fTips.text = "•  Sal del campus a máximo 10 km/h\n•  El peatón en el andén siempre tiene la prioridad\n•  El celular, guardado mientras manejas";
            fBtnALabel.text = "VER CÓMO DEBIÓ SER";
            fBtnBLabel.text = "REPETIR";
        }
        else
        {
            fEyebrow.text = "ASÍ SÍ"; fEyebrow.color = Verde;
            fTitle.text = "FRENAR A TIEMPO TOMA DOS SEGUNDOS. UNA VIDA NO TIENE REPUESTO.";
            fText.text = "Bajar a 10 km/h en la salida casi no cambia tu tiempo de viaje y le da a todos el tiempo de reaccionar. Conduce como si quien cruza fuera alguien que quieres.";
            fTips.text = "•  Baja la velocidad antes de la portería\n•  Haz el pare completo antes del andén\n•  Mira a los dos lados antes de salir";
            fBtnALabel.text = "REPETIR ESTA VERSIÓN";
            fBtnBLabel.text = "REPETIR EL ACCIDENTE";
        }
        final.SetActive(true);
    }
}
