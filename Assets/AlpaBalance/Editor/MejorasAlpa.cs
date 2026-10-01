using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MejorasAlpa
{
    const string rutaMisiones = "Assets/AlpaBalance/Resources/misiones.json";
    const string rutaBanco = "Assets/AlpaBalance/Editor/BancoPreguntas.json";
    const string carpetaRespaldo = "Assets/AlpaBalance/Respaldos";
    const string letrasEspanol = "áéíóúÁÉÍÓÚñÑ¿¡";
    static readonly int[] preguntasMinimas = { 6, 7, 8, 9, 10 };

    static readonly Dictionary<string, string> tildes = new Dictionary<string, string>
    {
        { "ano", "año" }, { "anos", "años" }, { "decision", "decisión" }, { "mision", "misión" },
        { "sera", "será" }, { "determinaran", "determinarán" }, { "america", "américa" }, { "africa", "áfrica" },
        { "tecnologia", "tecnología" }, { "economia", "economía" }, { "energia", "energía" }, { "eleccion", "elección" },
        { "tambien", "también" }, { "aqui", "aquí" }, { "facil", "fácil" }, { "dificil", "difícil" },
        { "ultimo", "último" }, { "proximo", "próximo" }, { "rapido", "rápido" }, { "region", "región" },
        { "poblacion", "población" }, { "recorreras", "recorrerás" }, { "tomaras", "tomarás" }, { "podras", "podrás" },
        { "deberas", "deberás" }, { "seras", "serás" }, { "comenzara", "comenzará" }
    };

    [MenuItem("AlpaBalance/Aplicar mejoras (texto, más preguntas, -10 por error)", false, 0)]
    static void Aplicar()
    {
        if (!EditorUtility.DisplayDialog("AlpaBalance",
            "Esto va a:\n\n- Arreglar las tildes y la ñ que no se ven en los textos\n- Corregir la ortografía de los subtítulos y nombres de continentes\n- Agregar las preguntas que falten (sin borrar las de ustedes)\n- Hacer que cada error baje mínimo 10 y que sea más fácil perder\n\nNO toca escenas de continentes ni modelos. Antes guarda un respaldo de misiones.json.",
            "Aplicar", "Cancelar")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var log = new StringBuilder();
        Fuentes(log);
        Escenas(log);
        Misiones(log);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Juego.Cargar();
        EditorUtility.DisplayDialog("AlpaBalance - Listo", log.ToString() + "\nDale Play para probar.", "OK");
    }

    static void Fuentes(StringBuilder log)
    {
        var normal = TMP_Settings.defaultFontAsset;
        int arregladas = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets" }))
        {
            var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (f == null || f == normal) continue;
            string faltan = Faltan(f);
            if (faltan.Length == 0) continue;
            if (normal == null)
            {
                log.AppendLine("! La fuente '" + f.name + "' no tiene: " + faltan + "  (cámbiala por LiberationSans SDF)");
                continue;
            }
            if (f.fallbackFontAssetTable == null) f.fallbackFontAssetTable = new List<TMP_FontAsset>();
            if (!f.fallbackFontAssetTable.Contains(normal)) f.fallbackFontAssetTable.Add(normal);
            EditorUtility.SetDirty(f);
            arregladas++;
            log.AppendLine("- Fuente '" + f.name + "': no tenía " + faltan + " -> ahora las toma de " + normal.name + ".");
            if (Faltan(normal).Length > 0) log.AppendLine("  ! " + normal.name + " tampoco tiene todas. Usa LiberationSans SDF en ese texto.");
        }
        if (arregladas == 0) log.AppendLine("- Fuentes: todas tienen tildes y ñ.");
    }

    static string Faltan(TMP_FontAsset f)
    {
        var sb = new StringBuilder();
        foreach (char c in letrasEspanol) if (!f.HasCharacter(c, true, true)) sb.Append(c);
        return sb.ToString();
    }

    static void Escenas(StringBuilder log)
    {
        var activa = SceneManager.GetActiveScene();
        bool hayMenu = Arreglar(activa, log);
        if (hayMenu) return;
        string rutaMenu = null;
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.enabled && !string.IsNullOrEmpty(s.path))
            {
                rutaMenu = s.path;
                break;
            }
        }
        if (string.IsNullOrEmpty(rutaMenu) || rutaMenu == activa.path)
        {
            log.AppendLine("! No encontré la escena del menú. Abre el menú y vuelve a usar esta opción para corregir los subtítulos.");
            return;
        }
        var menu = EditorSceneManager.OpenScene(rutaMenu, OpenSceneMode.Additive);
        Arreglar(menu, log);
        EditorSceneManager.CloseScene(menu, true);
    }

    static bool Arreglar(Scene escena, StringBuilder log)
    {
        if (!escena.IsValid() || !escena.isLoaded) return false;
        bool hayMenu = false;
        bool cambio = false;
        foreach (var r in escena.GetRootGameObjects())
        {
            foreach (var mb in r.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                string n = mb.GetType().Name;
                if ((n != "IntroSequence" && n != "ContinentRoulette") || !mb.enabled) continue;
                Undo.RecordObject(mb, "Apagar script viejo");
                mb.enabled = false;
                cambio = true;
                log.AppendLine("- Apagué el script viejo " + n + " en '" + mb.name + "' (escribía su propio texto encima).");
            }
            foreach (var menu in r.GetComponentsInChildren<MenuAlpa>(true))
            {
                hayMenu = true;
                if (menu.lineas == null) continue;
                Undo.RecordObject(menu, "Corregir subtítulos");
                for (int i = 0; i < menu.lineas.Length; i++)
                {
                    string bien = Corregir(menu.lineas[i]);
                    if (bien == menu.lineas[i]) continue;
                    log.AppendLine("- Subtítulo: \"" + menu.lineas[i] + "\"  ->  \"" + bien + "\"");
                    menu.lineas[i] = bien;
                    cambio = true;
                }
            }
        }
        if (cambio && !string.IsNullOrEmpty(escena.path))
        {
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            log.AppendLine("- Guardé la escena " + escena.name + ".");
        }
        if (hayMenu && !cambio) log.AppendLine("- Subtítulos del menú: sin errores.");
        return hayMenu;
    }

    public static string Corregir(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return Regex.Replace(s, @"\b[A-Za-z]+\b", m =>
        {
            string w = m.Value;
            if (!tildes.TryGetValue(w.ToLowerInvariant(), out string bien)) return w;
            if (w.Length > 1 && w.ToUpperInvariant() == w) return bien.ToUpperInvariant();
            if (char.IsUpper(w[0])) return char.ToUpperInvariant(bien[0]) + bien.Substring(1);
            return bien;
        });
    }

    static void Misiones(StringBuilder log)
    {
        DatosJuego datos = null;
        if (File.Exists(rutaMisiones))
        {
            string texto = File.ReadAllText(rutaMisiones, Encoding.UTF8);
            try
            {
                datos = JsonUtility.FromJson<DatosJuego>(texto);
            }
            catch (System.Exception e)
            {
                log.AppendLine("! misiones.json tiene un error de escritura y no lo toqué: " + e.Message);
                return;
            }
            Directory.CreateDirectory(carpetaRespaldo);
            string respaldo = carpetaRespaldo + "/misiones_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
            File.WriteAllText(respaldo, texto, new UTF8Encoding(false));
            log.AppendLine("- Respaldo de sus preguntas: " + respaldo);
        }
        if (datos == null) datos = new DatosJuego();
        if (datos.continentes == null) datos.continentes = new Continente[0];

        var lista = new List<Continente>();
        foreach (var c in datos.continentes) if (c != null) lista.Add(c);

        DatosJuego banco = null;
        if (File.Exists(rutaBanco)) banco = JsonUtility.FromJson<DatosJuego>(File.ReadAllText(rutaBanco, Encoding.UTF8));
        int agregadas = 0;
        if (banco != null && banco.continentes != null)
        {
            foreach (var cb in banco.continentes)
            {
                if (cb == null) continue;
                var c = lista.Find(x => Juego.Normal(x.clave) == Juego.Normal(cb.clave));
                if (c == null)
                {
                    lista.Add(cb);
                    agregadas += cb.ejercicios != null ? cb.ejercicios.Length : 0;
                    continue;
                }
                var ej = new List<Ejercicio>();
                var titulos = new HashSet<string>();
                foreach (var e in c.ejercicios ?? new Ejercicio[0])
                {
                    if (e == null) continue;
                    ej.Add(e);
                    titulos.Add(Juego.Normal(e.titulo));
                }
                int antes = ej.Count;
                foreach (var e in cb.ejercicios ?? new Ejercicio[0])
                    if (e != null && titulos.Add(Juego.Normal(e.titulo))) ej.Add(e);
                ej.Sort((a, b) => a.Nivel.CompareTo(b.Nivel));
                c.ejercicios = ej.ToArray();
                agregadas += ej.Count - antes;
            }
        }
        foreach (var c in lista)
        {
            string bien = Corregir(c.nombre);
            if (bien == c.nombre) continue;
            log.AppendLine("- Nombre de continente: \"" + c.nombre + "\"  ->  \"" + bien + "\"");
            c.nombre = bien;
        }
        datos.continentes = lista.ToArray();
        log.AppendLine("- Preguntas nuevas agregadas: " + agregadas + " (las suyas siguen ahí).");

        var cfg = datos.config ?? new Config();
        var ppn = new int[preguntasMinimas.Length];
        for (int i = 0; i < ppn.Length; i++)
        {
            int actual = cfg.preguntasPorNivel != null && i < cfg.preguntasPorNivel.Length ? cfg.preguntasPorNivel[i] : 0;
            ppn[i] = Mathf.Max(actual, preguntasMinimas[i]);
        }
        cfg.preguntasPorNivel = ppn;
        cfg.castigoPorError = Mathf.Max(cfg.castigoPorError, 10);
        cfg.limiteColapso = Mathf.Max(cfg.limiteColapso, 15);
        cfg.indicadorInicialMin = Mathf.Min(cfg.indicadorInicialMin, 40);
        cfg.indicadorInicialMax = Mathf.Max(cfg.indicadorInicialMin, Mathf.Min(cfg.indicadorInicialMax, 55));
        if (cfg.puntosParaCorrecto <= 0) cfg.puntosParaCorrecto = 70;
        datos.config = cfg;
        log.AppendLine("- Preguntas por nivel: " + string.Join(", ", ppn));
        log.AppendLine("- Cada error baja mínimo " + cfg.castigoPorError + ". Las barras empiezan entre " + cfg.indicadorInicialMin + " y " + cfg.indicadorInicialMax + " y se pierde al llegar a " + cfg.limiteColapso + ".");

        foreach (var c in lista)
        {
            int n1 = 0;
            foreach (var e in c.ejercicios ?? new Ejercicio[0]) if (e != null && e.Nivel <= 1) n1++;
            if (n1 < ppn[0]) log.AppendLine("  (" + c.nombre + " tiene " + n1 + " preguntas de nivel 1; saldrán todas las que haya)");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(rutaMisiones));
        File.WriteAllText(rutaMisiones, JsonUtility.ToJson(datos, true), new UTF8Encoding(false));
        AssetDatabase.ImportAsset(rutaMisiones);
    }
}
