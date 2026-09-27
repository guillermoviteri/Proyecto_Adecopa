using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class UIAlpa
{
    public static Sprite redondo;

    public static RectTransform Nuevo(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.layer = 5;
        var rt = (RectTransform)go.transform;
        if (padre != null) rt.SetParent(padre, false);
        return rt;
    }

    public static RectTransform Poner(RectTransform rt, Vector2 anclaMin, Vector2 anclaMax, Vector2 pivote, Vector2 pos, Vector2 tam)
    {
        rt.anchorMin = anclaMin;
        rt.anchorMax = anclaMax;
        rt.pivot = pivote;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        return rt;
    }

    public static RectTransform Llenar(RectTransform rt, float margen = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(margen, margen);
        rt.offsetMax = new Vector2(-margen, -margen);
        return rt;
    }

    public static Canvas CrearCanvas(string nombre, int orden)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.layer = 5;
        var cv = go.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = orden;
        var cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return cv;
    }

    public static Image Panel(string nombre, Transform padre, Color color, bool redondeado = true)
    {
        var rt = Nuevo(nombre, padre);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        if (redondeado && redondo != null)
        {
            img.sprite = redondo;
            img.type = Image.Type.Sliced;
        }
        return img;
    }

    public static TextMeshProUGUI Texto(string nombre, Transform padre, string texto, float tam, Color color, TextAlignmentOptions alineacion = TextAlignmentOptions.TopLeft, bool negrita = false)
    {
        var rt = Nuevo(nombre, padre);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = texto;
        t.fontSize = tam;
        t.color = color;
        t.alignment = alineacion;
        t.fontStyle = negrita ? FontStyles.Bold : FontStyles.Normal;
        t.raycastTarget = false;
        return t;
    }

    public static Button Boton(string nombre, Transform padre, string texto, Color fondo, Color colorTexto, float tam = 28)
    {
        var img = Panel(nombre, padre, Color.white);
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        Colores(b, fondo);
        var t = Texto("Texto", img.transform, texto, tam, colorTexto, TextAlignmentOptions.Center, true);
        Llenar(t.rectTransform, 8);
        return b;
    }

    public static void Colores(Button b, Color fondo)
    {
        var cb = b.colors;
        cb.normalColor = fondo;
        cb.highlightedColor = Color.Lerp(fondo, Color.white, 0.25f);
        cb.pressedColor = Color.Lerp(fondo, Color.black, 0.25f);
        cb.selectedColor = fondo;
        cb.disabledColor = new Color(fondo.r, fondo.g, fondo.b, fondo.a * 0.5f);
        cb.colorMultiplier = 1;
        cb.fadeDuration = 0.08f;
        b.colors = cb;
    }

    public static VerticalLayoutGroup Vertical(GameObject go, int relleno, float espacio, TextAnchor alineacion = TextAnchor.UpperLeft)
    {
        var v = go.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(relleno, relleno, relleno, relleno);
        v.spacing = espacio;
        v.childAlignment = alineacion;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        return v;
    }

    public static HorizontalLayoutGroup Horizontal(GameObject go, int relleno, float espacio, TextAnchor alineacion = TextAnchor.MiddleCenter)
    {
        var h = go.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(relleno, relleno, relleno, relleno);
        h.spacing = espacio;
        h.childAlignment = alineacion;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        return h;
    }

    public static LayoutElement Elemento(GameObject go, float minAlto = -1, float prefAlto = -1, float minAncho = -1, float prefAncho = -1, float flexAncho = -1)
    {
        var e = go.GetComponent<LayoutElement>();
        if (e == null) e = go.AddComponent<LayoutElement>();
        e.minHeight = minAlto;
        e.preferredHeight = prefAlto;
        e.minWidth = minAncho;
        e.preferredWidth = prefAncho;
        e.flexibleWidth = flexAncho;
        return e;
    }

    public static Sprite SpriteRedondo()
    {
        int n = 64;
        float r = 18;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = Mathf.Max(0, Mathf.Max(r - x - 0.5f, x + 0.5f - (n - r)));
                float dy = Mathf.Max(0, Mathf.Max(r - y - 0.5f, y + 0.5f - (n - r)));
                float a = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        float b = r + 2;
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
    }

    public static Sprite SpriteBrillo()
    {
        int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[n * n];
        Vector2 c = new Vector2(n / 2f, n / 2f);
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) / (n / 2f);
                float a = Mathf.Pow(Mathf.Clamp01(1 - d), 2.2f) + Mathf.Clamp01((0.22f - d) * 12f);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100);
    }
}
