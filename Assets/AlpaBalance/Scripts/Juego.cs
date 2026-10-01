using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Rendering.STP;

public static class Juego
{
    public static readonly string[] indicadoresNombres = { "Medio ambiente", "Economía", "Sociedad", "Recursos", "Tecnología" };
    public static readonly Color[] indicadoresColores =
    {
        new Color(0.24f, 0.86f, 0.52f),
        new Color(1f, 0.76f, 0.03f),
        new Color(1f, 0.48f, 0.71f),
        new Color(0.22f, 0.71f, 1f),
        new Color(0.70f, 0.53f, 1f)
    };
    public static readonly string[] materias = { "Matemática", "Física", "Química", "Biología", "Ciencias Sociales", "Comunicación", "Idiomas", "Tecnología" };
    public static readonly string[] competencias = { "Pensamiento crítico", "Resolución de problemas", "Toma de decisiones", "Análisis de información", "Resistencia a presión social", "Negociación", "Creatividad", "Estrategia" };

    static readonly Color[] paleta =
    {
        new Color(0.24f, 0.86f, 0.52f),
        new Color(0.22f, 0.71f, 1f),
        new Color(1f, 0.62f, 0.26f),
        new Color(1f, 0.35f, 0.48f),
        new Color(0.70f, 0.53f, 1f)
    };

    public static string escenaFinal = "Final";

    public static bool enPartida;
    public static bool saltarIntro;
    public static bool guardado;
    public static string actual = "";
    public static string escenaMenu = "";
    public static List<string> completados = new List<string>();
    public static float[] indicadores = new float[5];
    public static Dictionary<string, Vector2> notas = new Dictionary<string, Vector2>();

    static DatosJuego datos;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar()
    {
        enPartida = false;
        saltarIntro = false;
        guardado = false;
        actual = "";
        escenaMenu = "";
        completados = new List<string>();
        indicadores = new float[5];
        notas = new Dictionary<string, Vector2>();
        datos = null;
    }

    public static DatosJuego Datos
    {
        get
        {
            if (datos == null) Cargar();
            return datos;
        }
    }

    public static void Cargar()
    {
        datos = new DatosJuego { continentes = new Continente[0] };
        var archivo = Resources.Load<TextAsset>("misiones");
        if (archivo == null)
        {
            Debug.LogError("[AlpaBalance] No encuentro Assets/AlpaBalance/Resources/misiones.json");
            return;
        }
        try
        {
            var d = JsonUtility.FromJson<DatosJuego>(archivo.text);
            if (d != null && d.continentes != null) datos = d;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[AlpaBalance] misiones.json tiene un error (revisa comas, comillas y llaves): " + e.Message);
        }
        var c = datos.config ?? new Config();
        if (c.preguntasPorNivel == null || c.preguntasPorNivel.Length == 0) c.preguntasPorNivel = new[] { 6, 7, 8, 9, 10 };
        c.castigoPorError = Mathf.Clamp(c.castigoPorError, 0, 100);
        c.indicadorInicialMin = Mathf.Clamp(c.indicadorInicialMin, 1, 100);
        c.indicadorInicialMax = Mathf.Clamp(Mathf.Max(c.indicadorInicialMax, c.indicadorInicialMin), 1, 100);
        c.limiteColapso = Mathf.Clamp(c.limiteColapso, 0, 99);
        if (c.puntosParaCorrecto <= 0) c.puntosParaCorrecto = 70;
        datos.config = c;
    }

    public static Config Ajustes => Datos.config;
    public static int limiteColapso => Ajustes.limiteColapso;
    public static int PuntosCorrecto => Ajustes.puntosParaCorrecto;

    public static float[] EfectosDe(Opcion op, Ejercicio ej)
    {
        var d = LeerEfectos(op.efectos);
        int castigo = Ajustes.castigoPorError;
        bool fallo = op.puntos < PuntosCorrecto && (!ej.EsDecision || Ajustes.castigoEnDecisiones);
        if (!fallo || castigo <= 0) return d;

        int peor = -1;
        for (int i = 0; i < d.Length; i++) if (d[i] < 0 && (peor < 0 || d[i] < d[peor])) peor = i;
        if (peor < 0)
        {
            Opcion mejor = null;
            if (ej.opciones != null) foreach (var o in ej.opciones) if (o != null && (mejor == null || o.puntos > mejor.puntos)) mejor = o;
            var m = LeerEfectos(mejor != null ? mejor.efectos : null);
            for (int i = 0; i < m.Length; i++) if (m[i] > 0 && (peor < 0 || m[i] > m[peor])) peor = i;
        }
        if (peor < 0)
        {
            peor = 0;
            for (int i = 1; i < indicadores.Length; i++) if (indicadores[i] < indicadores[peor]) peor = i;
        }
        d[peor] = Mathf.Min(d[peor], -castigo);
        return d;
    }

    public static int PreguntasDelNivel(int nivel)
    {
        var p = Ajustes.preguntasPorNivel;
        return Mathf.Max(1, p[Mathf.Clamp(nivel - 1, 0, p.Length - 1)]);
    }

    public static int Total => Datos.continentes.Length;
    public static int Nivel => Mathf.Clamp(completados.Count + 1, 1, Mathf.Max(1, Total));

    public static void NuevaPartida()
    {
        enPartida = true;
        guardado = false;
        actual = "";
        completados.Clear();
        notas.Clear();
        for (int i = 0; i < indicadores.Length; i++) indicadores[i] = Random.Range(Ajustes.indicadorInicialMin, Ajustes.indicadorInicialMax + 1);
    }

    public static void AsegurarPartida(string clave)
    {
        if (!enPartida) NuevaPartida();
        if (!string.IsNullOrEmpty(clave)) actual = clave;
    }

    public static Continente Buscar(string clave)
    {
        string k = Normal(clave).Replace(" ", "");
        if (k.Length == 0) return null;
        foreach (var c in Datos.continentes)
            if (c != null && Normal(c.clave).Replace(" ", "") == k) return c;
        return null;
    }

    public static bool Completado(string clave)
    {
        string k = Normal(clave);
        foreach (var c in completados) if (Normal(c) == k) return true;
        return false;
    }

    public static void Completar(string clave)
    {
        if (!Completado(clave)) completados.Add(clave);
    }

    public static List<Continente> Pendientes()
    {
        var l = new List<Continente>();
        foreach (var c in Datos.continentes) if (c != null && !Completado(c.clave)) l.Add(c);
        return l;
    }

    public static Color ColorDe(Continente c, int i)
    {
        if (c != null && !string.IsNullOrEmpty(c.color) && ColorUtility.TryParseHtmlString(c.color, out Color col)) return col;
        return paleta[Mathf.Abs(i) % paleta.Length];
    }

    public static float[] LeerEfectos(string s)
    {
        var d = new float[5];
        if (string.IsNullOrEmpty(s)) return d;
        foreach (Match m in Regex.Matches(Normal(s), @"([a-z]+)\s*([+-]\s*\d+)"))
        {
            int i = IndiceIndicador(m.Groups[1].Value);
            if (i < 0)
            {
                Debug.LogWarning("[AlpaBalance] Indicador desconocido en misiones.json: " + m.Groups[1].Value);
                continue;
            }
            d[i] += int.Parse(m.Groups[2].Value.Replace(" ", ""), CultureInfo.InvariantCulture);
        }
        return d;
    }

    static int IndiceIndicador(string k)
    {
        if (k.StartsWith("amb") || k.StartsWith("medio") || k.StartsWith("ecolog") || k.StartsWith("natur")) return 0;
        if (k.StartsWith("econ") || k.StartsWith("dinero")) return 1;
        if (k.StartsWith("soc") || k.StartsWith("bienestar")) return 2;
        if (k.StartsWith("recur") || k.StartsWith("agua")) return 3;
        if (k.StartsWith("tecn")) return 4;
        return -1;
    }

    public static void Aplicar(float[] d)
    {
        for (int i = 0; i < indicadores.Length && i < d.Length; i++)
            indicadores[i] = Mathf.Clamp(indicadores[i] + d[i], 0, 100);
    }

    public static int Colapso()
    {
        for (int i = 0; i < indicadores.Length; i++) if (indicadores[i] <= limiteColapso) return i;
        return -1;
    }

    public static void Anotar(string[] nombres, float puntos)
    {
        if (nombres == null) return;
        foreach (var n in nombres)
        {
            if (string.IsNullOrEmpty(n)) continue;
            string k = Normal(n);
            notas.TryGetValue(k, out Vector2 v);
            notas[k] = new Vector2(v.x + Mathf.Clamp(puntos, 0, 100), v.y + 1);
        }
    }

    public static float Porcentaje(string nombre)
    {
        if (notas.TryGetValue(Normal(nombre), out Vector2 v) && v.y > 0) return v.x / v.y;
        return -1;
    }

    public static string Normal(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Trim().ToLowerInvariant();
        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
        {
            switch (c)
            {
                case 'á': case 'à': case 'ä': case 'â': sb.Append('a'); break;
                case 'é': case 'è': case 'ë': case 'ê': sb.Append('e'); break;
                case 'í': case 'ì': case 'ï': case 'î': sb.Append('i'); break;
                case 'ó': case 'ò': case 'ö': case 'ô': sb.Append('o'); break;
                case 'ú': case 'ù': case 'ü': case 'û': sb.Append('u'); break;
                case 'ñ': sb.Append('n'); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    public static string NombreEscenaMenu()
    {
        if (!string.IsNullOrEmpty(escenaMenu)) return escenaMenu;
        string ruta = SceneUtility.GetScenePathByBuildIndex(0);
        return string.IsNullOrEmpty(ruta) ? "" : Path.GetFileNameWithoutExtension(ruta);
    }

    public static void IrAlMenu()
    {
        Transicion.IrA(NombreEscenaMenu());
    }

    public static string RutaCsv => Path.Combine(Application.persistentDataPath, "alpabalance_resultados.csv");

    public static void GuardarResultado()
    {
        if (guardado) return;
        guardado = true;
        try
        {
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            if (!File.Exists(RutaCsv))
            {
                sb.Append("fecha;anio");
                foreach (var m in materias) sb.Append(';').Append(m);
                foreach (var c in competencias) sb.Append(';').Append(c);
                foreach (var n in indicadoresNombres) sb.Append(';').Append(n);
                sb.AppendLine();
            }
            var ahora = System.DateTime.Now;
            sb.Append(ahora.ToString("yyyy-MM-dd HH:mm", ci)).Append(';').Append(ahora.Year.ToString(ci));
            foreach (var m in materias) sb.Append(';').Append(Porcentaje(m).ToString("0.0", ci));
            foreach (var c in competencias) sb.Append(';').Append(Porcentaje(c).ToString("0.0", ci));
            foreach (var v in indicadores) sb.Append(';').Append(v.ToString("0.0", ci));
            sb.AppendLine();
            File.AppendAllText(RutaCsv, sb.ToString(), Encoding.UTF8);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[AlpaBalance] No pude guardar los resultados: " + e.Message);
        }
    }

    public static Dictionary<int, Dictionary<string, Vector2>> LeerHistorial()
    {
        var r = new Dictionary<int, Dictionary<string, Vector2>>();
        try
        {
            if (!File.Exists(RutaCsv)) return r;
            var lineas = File.ReadAllLines(RutaCsv, Encoding.UTF8);
            if (lineas.Length < 2) return r;
            var cab = lineas[0].Split(';');
            for (int f = 1; f < lineas.Length; f++)
            {
                var p = lineas[f].Split(';');
                if (p.Length < 3 || !int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int anio)) continue;
                if (!r.TryGetValue(anio, out var dic))
                {
                    dic = new Dictionary<string, Vector2>();
                    r[anio] = dic;
                }
                dic.TryGetValue("_partidas", out Vector2 cuenta);
                dic["_partidas"] = new Vector2(0, cuenta.y + 1);
                for (int c = 2; c < p.Length && c < cab.Length; c++)
                {
                    if (!float.TryParse(p[c], NumberStyles.Float, CultureInfo.InvariantCulture, out float v) || v < 0) continue;
                    string k = Normal(cab[c]);
                    dic.TryGetValue(k, out Vector2 a);
                    dic[k] = new Vector2(a.x + v, a.y + 1);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[AlpaBalance] No pude leer el historial: " + e.Message);
        }
        return r;
    }
}
