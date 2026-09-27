using UnityEngine;
using TMPro;

public class PanelIndicadores : MonoBehaviour
{
    public RectTransform[] rellenos = new RectTransform[5];
    public TMP_Text[] valores = new TMP_Text[5];
    public TMP_Text[] cambios = new TMP_Text[5];
    public float velocidad = 40f;
    public float peligro = 30f;

    readonly float[] vistos = new float[5];
    readonly float[] tiempos = new float[5];

    void Start()
    {
        for (int i = 0; i < 5; i++) vistos[i] = Juego.indicadores[i];
        Refrescar();
    }

    void Update()
    {
        for (int i = 0; i < 5; i++)
        {
            vistos[i] = Mathf.MoveTowards(vistos[i], Juego.indicadores[i], velocidad * Time.deltaTime);
            if (tiempos[i] > 0)
            {
                tiempos[i] -= Time.deltaTime;
                if (i < cambios.Length && cambios[i] != null) cambios[i].alpha = Mathf.Clamp01(tiempos[i]);
            }
        }
        Refrescar();
    }

    void Refrescar()
    {
        for (int i = 0; i < 5; i++)
        {
            if (i < rellenos.Length && rellenos[i] != null)
                rellenos[i].anchorMax = new Vector2(Mathf.Clamp01(vistos[i] / 100f), 1);
            if (i < valores.Length && valores[i] != null)
            {
                valores[i].text = Mathf.RoundToInt(vistos[i]) + "%";
                valores[i].color = vistos[i] < peligro
                    ? Color.Lerp(Color.white, new Color(1f, 0.3f, 0.3f), 0.5f + 0.5f * Mathf.Sin(Time.time * 8))
                    : Color.white;
            }
        }
    }

    public void Previa(float[] d)
    {
        for (int i = 0; i < 5 && i < cambios.Length; i++)
        {
            if (cambios[i] == null) continue;
            tiempos[i] = 0;
            cambios[i].text = Formato(d[i]);
            cambios[i].alpha = 1;
        }
    }

    public void QuitarPrevia()
    {
        for (int i = 0; i < 5 && i < cambios.Length; i++)
            if (cambios[i] != null && tiempos[i] <= 0) cambios[i].text = "";
    }

    public void Mostrar(float[] d)
    {
        for (int i = 0; i < 5 && i < cambios.Length; i++)
        {
            if (cambios[i] == null) continue;
            cambios[i].text = Formato(d[i]);
            cambios[i].alpha = 1;
            tiempos[i] = Mathf.Approximately(d[i], 0) ? 0 : 2.5f;
        }
    }

    public static string Formato(float v)
    {
        int n = Mathf.RoundToInt(v);
        if (n == 0) return "";
        return n > 0 ? "<color=#5CFF9D>+" + n + "</color>" : "<color=#FF6B6B>" + n + "</color>";
    }
}
