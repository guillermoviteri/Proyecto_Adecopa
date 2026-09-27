using UnityEngine;
using UnityEngine.UI;

public class GraficoRadar : MaskableGraphic
{
    public float[] valores = { 0.9f, 0.7f, 0.8f, 0.6f, 0.75f, 0.85f, 0.65f, 0.95f };
    public Color colorRelleno = new Color(0.22f, 0.71f, 1f, 0.35f);
    public Color colorLinea = new Color(0.36f, 0.85f, 1f, 1f);
    public Color colorRejilla = new Color(1f, 1f, 1f, 0.15f);
    public float grosor = 3f;
    public int anillos = 4;

    public void Poner(float[] v)
    {
        valores = v;
        SetVerticesDirty();
    }

    public Vector2 Punta(int i, float r)
    {
        int n = Mathf.Max(3, valores != null ? valores.Length : 3);
        float a = Mathf.PI / 2f - i * Mathf.PI * 2f / n;
        Rect rc = rectTransform.rect;
        return rc.center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
    }

    public float Radio => Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (valores == null || valores.Length < 3) return;
        int n = valores.Length;
        float r = Radio;
        Vector2 c = rectTransform.rect.center;

        for (int k = 1; k <= anillos; k++)
        {
            float rr = r * k / anillos;
            for (int i = 0; i < n; i++) Linea(vh, Punta(i, rr), Punta((i + 1) % n, rr), 1.5f, colorRejilla);
        }
        for (int i = 0; i < n; i++) Linea(vh, c, Punta(i, r), 1.5f, colorRejilla);

        int centro = vh.currentVertCount;
        Vertice(vh, c, colorRelleno);
        for (int i = 0; i < n; i++) Vertice(vh, Punta(i, r * Mathf.Clamp01(valores[i])), colorRelleno);
        for (int i = 0; i < n; i++) vh.AddTriangle(centro, centro + 1 + i, centro + 1 + (i + 1) % n);

        for (int i = 0; i < n; i++)
            Linea(vh, Punta(i, r * Mathf.Clamp01(valores[i])), Punta((i + 1) % n, r * Mathf.Clamp01(valores[(i + 1) % n])), grosor, colorLinea);
    }

    static void Vertice(VertexHelper vh, Vector2 p, Color col)
    {
        var v = UIVertex.simpleVert;
        v.position = p;
        v.color = col;
        vh.AddVert(v);
    }

    static void Linea(VertexHelper vh, Vector2 a, Vector2 b, float g, Color col)
    {
        Vector2 d = b - a;
        if (d.sqrMagnitude < 0.0001f) return;
        Vector2 nrm = new Vector2(-d.y, d.x).normalized * g * 0.5f;
        int i = vh.currentVertCount;
        Vertice(vh, a - nrm, col);
        Vertice(vh, a + nrm, col);
        Vertice(vh, b + nrm, col);
        Vertice(vh, b - nrm, col);
        vh.AddTriangle(i, i + 1, i + 2);
        vh.AddTriangle(i, i + 2, i + 3);
    }
}
