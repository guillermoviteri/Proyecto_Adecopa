using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Transicion : MonoBehaviour
{
    static Transicion inst;

    Image velo;
    CanvasGroup grupo;
    bool ocupado;

    static Transicion Inst
    {
        get
        {
            if (inst == null)
            {
                var cv = UIAlpa.CrearCanvas("Transicion", 9999);
                DontDestroyOnLoad(cv.gameObject);
                inst = cv.gameObject.AddComponent<Transicion>();
                inst.grupo = cv.gameObject.AddComponent<CanvasGroup>();
                inst.velo = UIAlpa.Panel("Velo", cv.transform, Color.white, false);
                UIAlpa.Llenar(inst.velo.rectTransform);
                inst.grupo.alpha = 0;
                inst.grupo.blocksRaycasts = false;
            }
            return inst;
        }
    }

    public static bool Existe(string escena)
    {
        return !string.IsNullOrEmpty(escena) && Application.CanStreamedLevelBeLoaded(escena);
    }

    public static void IrA(string escena)
    {
        IrA(escena, new Color(0.02f, 0.03f, 0.08f), 0.5f);
    }

    public static void IrA(string escena, Color color, float duracion)
    {
        if (!Existe(escena))
        {
            Debug.LogError("[AlpaBalance] La escena '" + escena + "' no está en File > Build Settings. Usa el menú AlpaBalance > 2.");
            return;
        }
        var t = Inst;
        if (t.ocupado) return;
        t.StartCoroutine(t.Rutina(escena, color, duracion));
    }

    IEnumerator Rutina(string escena, Color color, float duracion)
    {
        ocupado = true;
        velo.color = color;
        grupo.blocksRaycasts = true;
        yield return StartCoroutine(Fundir(0, 1, duracion));
        var op = SceneManager.LoadSceneAsync(escena);
        while (!op.isDone) yield return null;
        yield return null;
        yield return StartCoroutine(Fundir(1, 0, duracion * 1.4f));
        grupo.blocksRaycasts = false;
        ocupado = false;
    }

    IEnumerator Fundir(float a, float b, float d)
    {
        float t = 0;
        while (t < 1)
        {
            t += Mathf.Min(Time.unscaledDeltaTime, 0.05f) / Mathf.Max(0.01f, d);
            grupo.alpha = Mathf.Lerp(a, b, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
        grupo.alpha = b;
    }
}
