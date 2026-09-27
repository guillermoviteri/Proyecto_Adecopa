using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PantallaFinal : MonoBehaviour
{
    public PanelIndicadores indicadores;
    public TMP_Text textoVeredicto;
    public TMP_Text textoAcademico;
    public TMP_Text textoCompetencias;
    public TMP_Text textoTalentos;
    public TMP_Text textoEvolucion;
    public GraficoRadar radar;
    public RectTransform etiquetasRadar;
    public Button botonOtraVez;
    public Button botonMenu;

    static readonly string[] cortos = { "Matemática", "Física", "Química", "Biología", "C. Sociales", "Comunicación", "Idiomas", "Tecnología" };

    void Start()
    {
        if (!Juego.enPartida) Juego.NuevaPartida();
        else if (Juego.completados.Count >= Juego.Total) Juego.GuardarResultado();

        Veredicto();
        if (textoAcademico != null) textoAcademico.text = Lista("PERFIL ACADÉMICO", Juego.materias);
        if (textoCompetencias != null) textoCompetencias.text = Lista("PERFIL DE COMPETENCIAS", Juego.competencias);
        Talentos();
        Radar();
        Evolucion();

        if (botonOtraVez != null) botonOtraVez.onClick.AddListener(() =>
        {
            Juego.NuevaPartida();
            Juego.saltarIntro = true;
            Juego.IrAlMenu();
        });
        if (botonMenu != null) botonMenu.onClick.AddListener(() =>
        {
            Juego.enPartida = false;
            Juego.IrAlMenu();
        });
    }

    void Veredicto()
    {
        if (textoVeredicto == null) return;
        float prom = Juego.indicadores.Average();
        float min = Juego.indicadores.Min();
        string v;
        string col;
        if (prom >= 65 && min >= 40) { v = "PLANETA EN EQUILIBRIO"; col = "#5CFF9D"; }
        else if (prom >= 45 && min >= 20) { v = "PLANETA ESTABLE, PERO FRÁGIL"; col = "#FFC83D"; }
        else { v = "PLANETA EN RIESGO"; col = "#FF6B6B"; }
        textoVeredicto.text = "<color=" + col + "><b>" + v + "</b></color>\n<size=70%><color=#9FB3D9>Promedio mundial: " + Mathf.RoundToInt(prom) + "%</color></size>";
    }

    static string ColorNota(float p)
    {
        return p >= 80 ? "#5CFF9D" : p >= 60 ? "#FFC83D" : "#FF7B7B";
    }

    string Lista(string titulo, string[] nombres)
    {
        var sb = new StringBuilder();
        sb.Append("<b>").Append(titulo).Append("</b>\n");
        foreach (var n in nombres)
        {
            float p = Juego.Porcentaje(n);
            sb.Append("<size=88%>").Append(n).Append("<pos=80%>");
            if (p < 0) sb.Append("<color=#6F7C99>-</color>");
            else sb.Append("<color=").Append(ColorNota(p)).Append('>').Append(Mathf.RoundToInt(p)).Append("%</color>");
            sb.Append("</size>\n");
        }
        return sb.ToString();
    }

    void Talentos()
    {
        if (textoTalentos == null) return;
        var todos = new List<KeyValuePair<string, float>>();
        foreach (var n in Juego.materias.Concat(Juego.competencias))
        {
            float p = Juego.Porcentaje(n);
            if (p >= 0) todos.Add(new KeyValuePair<string, float>(n, p));
        }
        todos.Sort((a, b) => b.Value.CompareTo(a.Value));
        var sb = new StringBuilder("<b>TUS TALENTOS DESTACADOS</b>\n");
        if (todos.Count == 0) sb.Append("<size=85%>Completa una partida para descubrir tus talentos.</size>");
        for (int i = 0; i < todos.Count && i < 4; i++)
            sb.Append("<size=92%>").Append(i + 1).Append(". ").Append(todos[i].Key).Append("<pos=80%><color=#FFC83D>").Append(Mathf.RoundToInt(todos[i].Value)).Append("%</color></size>\n");
        textoTalentos.text = sb.ToString();
    }

    void Radar()
    {
        if (radar == null) return;
        int n = Juego.materias.Length;
        var v = new float[n];
        for (int i = 0; i < n; i++)
        {
            float p = Juego.Porcentaje(Juego.materias[i]);
            v[i] = p < 0 ? 0 : p / 100f;
        }
        radar.Poner(v);
        if (etiquetasRadar == null) return;
        foreach (Transform t in etiquetasRadar) Destroy(t.gameObject);
        float r = radar.Radio + 42;
        for (int i = 0; i < n; i++)
        {
            float p = Juego.Porcentaje(Juego.materias[i]);
            string nombre = i < cortos.Length ? cortos[i] : Juego.materias[i];
            var t = UIAlpa.Texto("Etiqueta" + i, etiquetasRadar, nombre + "\n<size=85%><color=" + (p < 0 ? "#6F7C99>-" : ColorNota(p) + ">" + Mathf.RoundToInt(p) + "%") + "</color></size>", 20, Color.white, TextAlignmentOptions.Center, true);
            UIAlpa.Poner(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), radar.Punta(i, r), new Vector2(170, 56));
        }
    }

    void Evolucion()
    {
        if (textoEvolucion == null) return;
        var h = Juego.LeerHistorial();
        var anios = h.Keys.OrderBy(a => a).ToList();
        if (anios.Count > 4) anios = anios.Skip(anios.Count - 4).ToList();
        var sb = new StringBuilder("<b>EVOLUCIÓN POR AÑO</b> <size=70%><color=#9FB3D9>(promedio de las partidas guardadas en esta PC)</color></size>\n");
        if (anios.Count == 0)
        {
            sb.Append("<size=85%>Todavía no hay partidas completas guardadas.</size>");
            textoEvolucion.text = sb.ToString();
            return;
        }
        sb.Append("<size=85%><b>Área");
        for (int j = 0; j < anios.Count; j++) sb.Append("<pos=").Append(42 + j * 15).Append("%>").Append(anios[j]);
        sb.Append("</b></size>\n");
        var filas = Juego.materias.Concat(new[] { "Pensamiento crítico" });
        foreach (var f in filas)
        {
            string k = Juego.Normal(f);
            sb.Append("<size=85%>").Append(f);
            for (int j = 0; j < anios.Count; j++)
            {
                sb.Append("<pos=").Append(42 + j * 15).Append("%>");
                if (h[anios[j]].TryGetValue(k, out Vector2 v) && v.y > 0) sb.Append(Mathf.RoundToInt(v.x / v.y)).Append('%');
                else sb.Append('-');
            }
            sb.Append("</size>\n");
        }
        int partidas = 0;
        if (h[anios[anios.Count - 1]].TryGetValue("_partidas", out Vector2 c)) partidas = Mathf.RoundToInt(c.y);
        sb.Append("<size=65%><color=#6F7C99>Partidas en ").Append(anios[anios.Count - 1]).Append(": ").Append(partidas).Append("  ·  Archivo: ").Append(Juego.RutaCsv).Append("</color></size>");
        textoEvolucion.text = sb.ToString();
    }
}
