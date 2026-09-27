using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class ConstructorAlpa
{
    const string carpeta = "Assets/AlpaBalance/Escenas";
    static readonly Color fondoPanel = new Color(0.03f, 0.05f, 0.12f, 0.82f);
    static readonly Color fondoTarjeta = new Color(0.04f, 0.07f, 0.16f, 0.92f);
    static readonly Color cian = new Color(0.36f, 0.85f, 1f);
    static readonly Color gris = new Color(0.72f, 0.78f, 0.9f);
    static readonly Color oscuro = new Color(0.02f, 0.05f, 0.1f);
    static readonly Color azulOpcion = new Color(0.11f, 0.17f, 0.32f, 0.95f);

    static Vector2 V(float x, float y) => new Vector2(x, y);

    static void Aviso(string s) => EditorUtility.DisplayDialog("AlpaBalance", s, "OK");

    static void Sprites()
    {
        UIAlpa.redondo = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
    }

    [MenuItem("AlpaBalance/1. Preparar el menú (escena abierta)", false, 1)]
    static void PrepararMenu()
    {
        Juego.Cargar();
        var escena = SceneManager.GetActiveScene();
        var log = new StringBuilder();
        var menu = Primero<MenuAlpa>();
        if (menu == null)
        {
            var go = new GameObject("MenuAlpa");
            Undo.RegisterCreatedObjectUndo(go, "Crear MenuAlpa");
            menu = go.AddComponent<MenuAlpa>();
            log.AppendLine("+ Creé el objeto MenuAlpa.");
        }
        Undo.RecordObject(menu, "Preparar MenuAlpa");

        foreach (var b in Todos<Button>())
        {
            string txt = TextoDe(b.gameObject);
            if (menu.botonComenzar == null && Contiene(txt, "comenz", "jugar", "start", "inicia", "play")) menu.botonComenzar = b;
            else if (menu.botonSalir == null && Contiene(txt, "salir", "exit", "quit")) menu.botonSalir = b;
        }
        foreach (var t in Todos<Transform>())
        {
            if (t is RectTransform || t.GetComponentInParent<Canvas>() != null) continue;
            var viejo = t.GetComponent("MenuButton3D");
            bool candidato = viejo != null || t.GetComponent("Clickable3D") != null || t.GetComponent<Boton3D>() != null || t.GetComponent<Renderer>() != null;
            if (!candidato || (viejo == null && t.GetComponent<TMP_Text>() != null)) continue;
            string acc = Accion(viejo);
            string txt = TextoDe(t.gameObject) + " " + (t.parent != null ? t.parent.name.ToLowerInvariant() : "");
            bool com = acc == "startgame" || (acc == "" && Contiene(txt, "comenz", "jugar", "start", "inicia", "play"));
            bool sal = acc == "exit" || (acc == "" && Contiene(txt, "salir", "exit", "quit"));
            if (menu.comenzar3D == null && com) menu.comenzar3D = Convertir3D(t.gameObject, log);
            else if (menu.salir3D == null && sal) menu.salir3D = Convertir3D(t.gameObject, log);
        }
        foreach (var t in Todos<TMP_Text>())
        {
            if (t.GetComponentInParent<Button>() != null || t.GetComponentInParent<Boton3D>() != null) continue;
            string s = (t.text + " " + t.name).ToLowerInvariant();
            if (menu.subtitulos == null && s.Contains("subt")) menu.subtitulos = t;
            else if (menu.titulo == null && Contiene(s, "alpabalance", "titulo", "title", "2100")) menu.titulo = t;
        }
        if (menu.cam == null) menu.cam = Camera.main;
        if (menu.tierra == null) menu.tierra = BuscarTierra();

        log.AppendLine(menu.comenzar3D != null ? Estado("Botón 3D Comenzar", menu.comenzar3D) : Estado("Botón Comenzar", menu.botonComenzar));
        log.AppendLine(menu.salir3D != null ? Estado("Botón 3D Salir", menu.salir3D) : Estado("Botón Salir", menu.botonSalir));
        log.AppendLine(Estado("Texto del título", menu.titulo));
        log.AppendLine(Estado("Texto de subtítulos (TextMeshPro)", menu.subtitulos));
        log.AppendLine(Estado("Cámara", menu.cam));
        log.AppendLine(Estado("Tierra", menu.tierra));

        if (menu.tierra != null)
        {
            var viejo = menu.tierra.GetComponent("RotatingEarth") as Behaviour;
            if (viejo != null && viejo.enabled)
            {
                Undo.RecordObject(viejo, "Desactivar RotatingEarth");
                viejo.enabled = false;
                log.AppendLine("- Desactivé RotatingEarth (MenuAlpa ya hace girar la Tierra).");
            }
            float r = MenuAlpa.Radio(menu.tierra);
            int creados = 0;
            foreach (var c in Juego.Datos.continentes)
            {
                if (c == null || TienePunto(menu.tierra, c.clave)) continue;
                var p = new GameObject(c.clave);
                Undo.RegisterCreatedObjectUndo(p, "Punto de continente");
                p.transform.position = menu.tierra.position + menu.tierra.TransformDirection(MenuAlpa.DireccionDefecto(c.clave)) * r;
                p.transform.SetParent(menu.tierra, true);
                creados++;
            }
            if (creados > 0) log.AppendLine("\n+ Creé " + creados + " puntos dentro de la Tierra (esferas amarillas en la vista Scene). MUEVE cada uno encima de su continente.");
        }
        log.AppendLine("\nLo que diga FALTA, arrástralo al componente MenuAlpa en el Inspector. Luego guarda (Ctrl+S) y usa el paso 2.");

        EditorSceneManager.MarkSceneDirty(escena);
        Selection.activeGameObject = menu.gameObject;
        Aviso(log.ToString());
    }

    [MenuItem("AlpaBalance/2. Crear escenas de continentes y final", false, 2)]
    static void CrearEscenas()
    {
        Juego.Cargar();
        var datos = Juego.Datos;
        if (datos.continentes.Length == 0)
        {
            Aviso("misiones.json está vacío o tiene un error. Revisa la consola.");
            return;
        }
        var menu = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(menu.path))
        {
            Aviso("Primero guarda la escena del menú (Ctrl+S) y vuelve a intentarlo con ella abierta.");
            return;
        }
        if (Primero<MenuAlpa>() == null && !EditorUtility.DisplayDialog("AlpaBalance", "La escena abierta (" + menu.name + ") no tiene MenuAlpa. ¿Seguro que es tu menú?", "Sí, es el menú", "Cancelar")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string rutaMenu = menu.path;
        Material cielo = RenderSettings.skybox;
        Directory.CreateDirectory(carpeta);

        var rutas = new List<string>();
        bool preguntado = false;
        bool pisar = false;
        var pendientes = new List<Continente>();
        foreach (var c in datos.continentes) if (c != null) pendientes.Add(c);

        foreach (var c in pendientes)
        {
            string ruta = carpeta + "/" + c.Escena + ".unity";
            rutas.Add(ruta);
            if (File.Exists(ruta))
            {
                if (!preguntado)
                {
                    pisar = EditorUtility.DisplayDialog("AlpaBalance", "Algunas escenas ya existen. ¿Reemplazarlas? (se pierde lo que les agregaste)", "Reemplazar", "Conservarlas");
                    preguntado = true;
                }
                if (!pisar) continue;
            }
            var esc = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Base(cielo);
            Modelo();
            ArmarNivel(c.clave, c);
            EditorSceneManager.SaveScene(esc, ruta);
        }

        string rutaFinal = carpeta + "/" + Juego.escenaFinal + ".unity";
        rutas.Add(rutaFinal);
        bool hacerFinal = true;
        if (File.Exists(rutaFinal))
        {
            if (!preguntado)
            {
                pisar = EditorUtility.DisplayDialog("AlpaBalance", "La escena Final ya existe. ¿Reemplazarla?", "Reemplazar", "Conservarla");
                preguntado = true;
            }
            hacerFinal = pisar;
        }
        if (hacerFinal)
        {
            var esc = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Base(cielo);
            ArmarFinal();
            EditorSceneManager.SaveScene(esc, rutaFinal);
        }

        var lista = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(rutaMenu, true) };
        foreach (var r in rutas) if (r != rutaMenu) lista.Add(new EditorBuildSettingsScene(r, true));
        foreach (var s in EditorBuildSettings.scenes) if (!lista.Exists(x => x.path == s.path)) lista.Add(s);
        EditorBuildSettings.scenes = lista.ToArray();

        EditorSceneManager.OpenScene(rutaMenu);
        Aviso("Listo. Se crearon las escenas en " + carpeta + " y se agregaron a Build Settings (el menú quedó primero).\n\nAbre cada escena de continente y pon tu modelo 3D dentro de 'ModeloContinente'.\n\nDale Play en el menú para probar.");
    }

    [MenuItem("AlpaBalance/3. Agregar UI de nivel a la escena abierta", false, 3)]
    static void AgregarNivelAqui()
    {
        Juego.Cargar();
        if (Primero<EventSystem>() == null) CrearEventSystem();
        var n = ArmarNivel("", null);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = n.gameObject;
        Aviso("Listo. Escribe en 'clave' el continente de esta escena (AmericaSur, Europa, Africa, Asia o AmericaNorte) o déjalo vacío para usar el que salga en la ruleta.");
    }

    [MenuItem("AlpaBalance/Revisar misiones.json", false, 20)]
    static void Revisar()
    {
        Juego.Cargar();
        var resumen = new StringBuilder();
        var errores = new StringBuilder();
        var nombres = new HashSet<string>();
        foreach (var m in Juego.materias) nombres.Add(Juego.Normal(m));
        foreach (var m in Juego.competencias) nombres.Add(Juego.Normal(m));
        int problemas = 0;
        foreach (var c in Juego.Datos.continentes)
        {
            if (c == null) continue;
            var porNivel = new int[6];
            int n = 0;
            foreach (var e in c.ejercicios ?? new Ejercicio[0])
            {
                if (e == null) continue;
                n++;
                porNivel[Mathf.Clamp(e.Nivel, 1, 5)]++;
                if (e.opciones == null || e.opciones.Length < 2)
                {
                    errores.AppendLine("! '" + e.titulo + "' tiene menos de 2 opciones");
                    problemas++;
                }
                foreach (var nombre in Unir(e.materias, e.competencias))
                {
                    if (nombres.Contains(Juego.Normal(nombre))) continue;
                    errores.AppendLine("! Nombre desconocido '" + nombre + "' en '" + e.titulo + "'");
                    problemas++;
                }
            }
            resumen.AppendLine(c.nombre + " (" + c.clave + "): " + n + " desafíos   [N1:" + porNivel[1] + " N2:" + porNivel[2] + " N3:" + porNivel[3] + " N4:" + porNivel[4] + " N5:" + porNivel[5] + "]");
        }
        if (Juego.Datos.continentes.Length == 0) errores.AppendLine("! No se pudo leer ningún continente (mira la consola).");
        Aviso(resumen + "\n" + (problemas == 0 && Juego.Datos.continentes.Length > 0 ? "Todo bien." : errores.ToString()));
    }

    static IEnumerable<string> Unir(string[] a, string[] b)
    {
        if (a != null) foreach (var x in a) if (!string.IsNullOrEmpty(x)) yield return x;
        if (b != null) foreach (var x in b) if (!string.IsNullOrEmpty(x)) yield return x;
    }

    static void Base(Material cielo)
    {
        foreach (var r in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var cam = r.GetComponent<Camera>();
            if (cam == null) continue;
            cam.transform.position = new Vector3(0, 0.5f, -7);
            cam.transform.rotation = Quaternion.identity;
            if (cielo != null)
            {
                RenderSettings.skybox = cielo;
                cam.clearFlags = CameraClearFlags.Skybox;
            }
            else
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.02f, 0.03f, 0.08f);
            }
        }
        CrearEventSystem();
    }

    static void CrearEventSystem()
    {
        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var tipo = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (tipo != null)
        {
            es.AddComponent(tipo);
            return;
        }
#endif
        es.AddComponent<StandaloneInputModule>();
    }

    static void Modelo()
    {
        var modelo = new GameObject("ModeloContinente (pon aquí tu modelo)");
        modelo.transform.position = new Vector3(-4.3f, 1.1f, 0);
        modelo.transform.rotation = Quaternion.Euler(-20, 0, 0);
        modelo.AddComponent<GirarLento>();
        var temporal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        temporal.name = "BORRAME (temporal)";
        temporal.transform.SetParent(modelo.transform, false);
        temporal.transform.localScale = new Vector3(2.4f, 0.15f, 2.4f);
    }

    static NivelContinente ArmarNivel(string clave, Continente c)
    {
        Sprites();
        var cv = UIAlpa.CrearCanvas("Canvas", 0);
        var raiz = cv.transform;
        var nivel = new GameObject("Nivel").AddComponent<NivelContinente>();
        nivel.clave = clave;

        nivel.textoContinente = UIAlpa.Texto("Continente", raiz, c != null ? c.nombre.ToUpperInvariant() : "CONTINENTE", 56, Color.white, TextAlignmentOptions.TopLeft, true);
        UIAlpa.Poner(nivel.textoContinente.rectTransform, V(0, 1), V(0, 1), V(0, 1), V(50, -34), V(900, 72));
        nivel.textoInfo = UIAlpa.Texto("Info", raiz, "Nivel 1 de 5   ·   País   ·   Desafío 1 de 3", 24, gris);
        UIAlpa.Poner(nivel.textoInfo.rectTransform, V(0, 1), V(0, 1), V(0, 1), V(52, -106), V(900, 40));

        nivel.indicadores = ArmarIndicadores(raiz, V(0, 0), V(0, 0), V(0, 0), V(40, 40), V(600, 370));

        var tarjeta = UIAlpa.Panel("Tarjeta", raiz, fondoTarjeta);
        var tr = tarjeta.rectTransform;
        UIAlpa.Poner(tr, V(1, 0.5f), V(1, 0.5f), V(1, 0.5f), V(-40, -10), V(1120, 600));
        UIAlpa.Vertical(tarjeta.gameObject, 40, 18);
        tarjeta.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        tarjeta.gameObject.AddComponent<CanvasGroup>();
        nivel.tarjeta = tr;
        nivel.textoMaterias = UIAlpa.Texto("Materias", tr, "MATEMÁTICA + COMUNICACIÓN", 22, cian, TextAlignmentOptions.TopLeft, true);
        nivel.textoTitulo = UIAlpa.Texto("Titulo", tr, "Título del desafío", 40, Color.white, TextAlignmentOptions.TopLeft, true);
        nivel.textoCuerpo = UIAlpa.Texto("Cuerpo", tr, "Texto del desafío.", 28, new Color(0.88f, 0.92f, 1f));

        var globo = UIAlpa.Panel("Globo", tr, new Color(0.4f, 0.26f, 0.05f, 0.55f));
        UIAlpa.Vertical(globo.gameObject, 20, 6);
        nivel.globo = globo.gameObject;
        nivel.textoQuien = UIAlpa.Texto("Quien", globo.transform, "Diplomático", 24, new Color(1f, 0.78f, 0.3f), TextAlignmentOptions.TopLeft, true);
        nivel.textoDice = UIAlpa.Texto("Dice", globo.transform, "\"...\"", 26, Color.white);
        nivel.textoDice.fontStyle = FontStyles.Italic;

        var caja = UIAlpa.Nuevo("Opciones", tr);
        UIAlpa.Vertical(caja.gameObject, 0, 12);
        nivel.cajaOpciones = caja;
        var plantilla = UIAlpa.Boton("PlantillaOpcion", caja, "A.  Opción", azulOpcion, Color.white, 26);
        var pt = plantilla.GetComponentInChildren<TMP_Text>();
        pt.alignment = TextAlignmentOptions.MidlineLeft;
        pt.fontStyle = FontStyles.Normal;
        var vb = UIAlpa.Vertical(plantilla.gameObject, 0, 0, TextAnchor.MiddleLeft);
        vb.padding = new RectOffset(24, 24, 16, 16);
        UIAlpa.Elemento(plantilla.gameObject, minAlto: 66);
        nivel.plantillaOpcion = plantilla;

        var retro = UIAlpa.Panel("Retro", tr, new Color(0.01f, 0.02f, 0.06f, 0.75f));
        var hr = UIAlpa.Horizontal(retro.gameObject, 0, 20, TextAnchor.MiddleLeft);
        hr.padding = new RectOffset(22, 22, 18, 18);
        nivel.panelRetro = retro.gameObject;
        nivel.textoRetro = UIAlpa.Texto("TextoRetro", retro.transform, "¡Correcto!", 25, Color.white);
        UIAlpa.Elemento(nivel.textoRetro.gameObject, minAncho: 200, flexAncho: 1);
        nivel.botonSiguiente = UIAlpa.Boton("Siguiente", retro.transform, "Siguiente", cian, oscuro, 26);
        UIAlpa.Elemento(nivel.botonSiguiente.gameObject, minAlto: 64, prefAlto: 64, minAncho: 250, prefAncho: 250);

        var velo = UIAlpa.Panel("PanelMensaje", raiz, new Color(0, 0, 0, 0.62f), false);
        UIAlpa.Llenar(velo.rectTransform);
        nivel.panelMensaje = velo.gameObject;
        var cajaMsg = UIAlpa.Panel("Caja", velo.transform, fondoTarjeta);
        UIAlpa.Poner(cajaMsg.rectTransform, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), V(0, 0), V(1000, 400));
        UIAlpa.Vertical(cajaMsg.gameObject, 48, 26, TextAnchor.UpperCenter);
        cajaMsg.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        nivel.cajaMensaje = cajaMsg.rectTransform;
        nivel.mensajeTitulo = UIAlpa.Texto("Titulo", cajaMsg.transform, "ALERTA INTERNACIONAL", 44, Color.white, TextAlignmentOptions.Top, true);
        nivel.mensajeCuerpo = UIAlpa.Texto("Cuerpo", cajaMsg.transform, "Texto del mensaje.", 28, new Color(0.9f, 0.93f, 1f));
        var fila = UIAlpa.Nuevo("Botones", cajaMsg.transform);
        UIAlpa.Horizontal(fila.gameObject, 0, 24, TextAnchor.MiddleCenter);
        nivel.mensajeBotonA = UIAlpa.Boton("BotonA", fila, "Aceptar misión", cian, oscuro, 28);
        UIAlpa.Elemento(nivel.mensajeBotonA.gameObject, minAlto: 70, prefAlto: 70, minAncho: 340, prefAncho: 340);
        nivel.mensajeBotonB = UIAlpa.Boton("BotonB", fila, "Menú principal", new Color(1, 1, 1, 0.15f), Color.white, 26);
        UIAlpa.Elemento(nivel.mensajeBotonB.gameObject, minAlto: 70, prefAlto: 70, minAncho: 340, prefAncho: 340);

        return nivel;
    }

    static PanelIndicadores ArmarIndicadores(Transform padre, Vector2 aMin, Vector2 aMax, Vector2 piv, Vector2 pos, Vector2 tam)
    {
        var p = UIAlpa.Panel("Indicadores", padre, fondoPanel);
        UIAlpa.Poner(p.rectTransform, aMin, aMax, piv, pos, tam);
        var comp = p.gameObject.AddComponent<PanelIndicadores>();
        var t = UIAlpa.Texto("Titulo", p.transform, "ESTADO DEL PLANETA", 24, cian, TextAlignmentOptions.TopLeft, true);
        UIAlpa.Poner(t.rectTransform, V(0, 1), V(1, 1), V(0.5f, 1), V(0, -22), V(-56, 34));
        for (int i = 0; i < 5; i++)
        {
            float y = -72 - i * 58;
            var nom = UIAlpa.Texto("Nombre" + i, p.transform, Juego.indicadoresNombres[i], 23, Color.white);
            UIAlpa.Poner(nom.rectTransform, V(0, 1), V(1, 1), V(0.5f, 1), V(0, y), V(-56, 30));
            var val = UIAlpa.Texto("Valor" + i, p.transform, "60%", 23, Color.white, TextAlignmentOptions.TopRight, true);
            UIAlpa.Poner(val.rectTransform, V(0, 1), V(1, 1), V(0.5f, 1), V(0, y), V(-56, 30));
            var cam = UIAlpa.Texto("Cambio" + i, p.transform, "", 22, Color.white, TextAlignmentOptions.TopRight, true);
            UIAlpa.Poner(cam.rectTransform, V(0, 1), V(1, 1), V(0.5f, 1), V(-40, y), V(-136, 30));
            var barra = UIAlpa.Panel("Barra" + i, p.transform, new Color(1, 1, 1, 0.1f), false);
            UIAlpa.Poner(barra.rectTransform, V(0, 1), V(1, 1), V(0.5f, 1), V(0, y - 32), V(-56, 12));
            var rel = UIAlpa.Panel("Relleno", barra.transform, Juego.indicadoresColores[i], false);
            var rr = rel.rectTransform;
            rr.anchorMin = V(0, 0);
            rr.anchorMax = V(0.6f, 1);
            rr.pivot = V(0, 0.5f);
            rr.offsetMin = Vector2.zero;
            rr.offsetMax = Vector2.zero;
            comp.rellenos[i] = rr;
            comp.valores[i] = val;
            comp.cambios[i] = cam;
        }
        return comp;
    }

    static void ArmarFinal()
    {
        Sprites();
        var cv = UIAlpa.CrearCanvas("Canvas", 0);
        var raiz = cv.transform;
        var f = new GameObject("Final").AddComponent<PantallaFinal>();

        var titulo = UIAlpa.Texto("Titulo", raiz, "AÑO 2100 · RESULTADO FINAL", 54, Color.white, TextAlignmentOptions.Top, true);
        UIAlpa.Poner(titulo.rectTransform, V(0.5f, 1), V(0.5f, 1), V(0.5f, 1), V(0, -28), V(1400, 70));
        f.textoVeredicto = UIAlpa.Texto("Veredicto", raiz, "PLANETA EN EQUILIBRIO", 38, Color.white, TextAlignmentOptions.Top);
        UIAlpa.Poner(f.textoVeredicto.rectTransform, V(0.5f, 1), V(0.5f, 1), V(0.5f, 1), V(0, -102), V(1400, 90));

        f.indicadores = ArmarIndicadores(raiz, V(0, 1), V(0, 1), V(0, 1), V(40, -210), V(560, 370));

        var tal = UIAlpa.Panel("PanelTalentos", raiz, fondoPanel);
        UIAlpa.Poner(tal.rectTransform, V(0, 1), V(0, 1), V(0, 1), V(40, -600), V(560, 280));
        f.textoTalentos = UIAlpa.Texto("Talentos", tal.transform, "TUS TALENTOS DESTACADOS", 25, Color.white);
        UIAlpa.Llenar(f.textoTalentos.rectTransform, 26);

        var rad = UIAlpa.Nuevo("Radar", raiz);
        UIAlpa.Poner(rad, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), V(-20, 120), V(340, 340));
        f.radar = rad.gameObject.AddComponent<GraficoRadar>();
        f.radar.raycastTarget = false;
        f.etiquetasRadar = UIAlpa.Llenar(UIAlpa.Nuevo("Etiquetas", rad));

        var ev = UIAlpa.Panel("PanelEvolucion", raiz, fondoPanel);
        UIAlpa.Poner(ev.rectTransform, V(0.5f, 0), V(0.5f, 0), V(0.5f, 0), V(-20, 110), V(660, 290));
        f.textoEvolucion = UIAlpa.Texto("Evolucion", ev.transform, "EVOLUCIÓN POR AÑO", 19, Color.white);
        UIAlpa.Llenar(f.textoEvolucion.rectTransform, 20);

        var ac = UIAlpa.Panel("PanelAcademico", raiz, fondoPanel);
        UIAlpa.Poner(ac.rectTransform, V(1, 1), V(1, 1), V(1, 1), V(-40, -210), V(520, 380));
        f.textoAcademico = UIAlpa.Texto("Academico", ac.transform, "PERFIL ACADÉMICO", 24, Color.white);
        UIAlpa.Llenar(f.textoAcademico.rectTransform, 26);

        var co = UIAlpa.Panel("PanelCompetencias", raiz, fondoPanel);
        UIAlpa.Poner(co.rectTransform, V(1, 1), V(1, 1), V(1, 1), V(-40, -610), V(520, 380));
        f.textoCompetencias = UIAlpa.Texto("Competencias", co.transform, "PERFIL DE COMPETENCIAS", 24, Color.white);
        UIAlpa.Llenar(f.textoCompetencias.rectTransform, 26);

        f.botonOtraVez = UIAlpa.Boton("JugarDeNuevo", raiz, "Jugar de nuevo", cian, oscuro, 28);
        UIAlpa.Poner((RectTransform)f.botonOtraVez.transform, V(0.5f, 0), V(0.5f, 0), V(0.5f, 0), V(-195, 24), V(360, 70));
        f.botonMenu = UIAlpa.Boton("Menu", raiz, "Menú principal", new Color(1, 1, 1, 0.15f), Color.white, 28);
        UIAlpa.Poner((RectTransform)f.botonMenu.transform, V(0.5f, 0), V(0.5f, 0), V(0.5f, 0), V(195, 24), V(360, 70));
    }

    static List<T> Todos<T>() where T : Component
    {
        var l = new List<T>();
        foreach (var r in SceneManager.GetActiveScene().GetRootGameObjects()) l.AddRange(r.GetComponentsInChildren<T>(true));
        return l;
    }

    static T Primero<T>() where T : Component
    {
        var l = Todos<T>();
        return l.Count > 0 ? l[0] : null;
    }

    static string TextoDe(GameObject go)
    {
        var t = go.GetComponentInChildren<TMP_Text>(true);
        if (t != null) return (t.text + " " + go.name).ToLowerInvariant();
        var l = go.GetComponentInChildren<Text>(true);
        if (l != null) return (l.text + " " + go.name).ToLowerInvariant();
        return go.name.ToLowerInvariant();
    }

    static bool Contiene(string s, params string[] partes)
    {
        foreach (var p in partes) if (s.Contains(p)) return true;
        return false;
    }

    static string Accion(Component c)
    {
        if (c == null) return "";
        var f = c.GetType().GetField("action");
        var v = f != null ? f.GetValue(c) : null;
        return v != null ? v.ToString().ToLowerInvariant() : "";
    }

    static Boton3D Convertir3D(GameObject go, StringBuilder log)
    {
        foreach (var nombre in new[] { "MenuButton3D", "Clickable3D" })
        {
            var c = go.GetComponent(nombre);
            if (c != null) Undo.DestroyObjectImmediate(c);
        }
        if (go.GetComponent<Collider>() == null)
        {
            var mf = go.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) Undo.AddComponent<MeshCollider>(go).sharedMesh = mf.sharedMesh;
            else Undo.AddComponent<BoxCollider>(go);
        }
        var b = go.GetComponent<Boton3D>();
        if (b == null) b = Undo.AddComponent<Boton3D>(go);
        log.AppendLine("+ '" + go.name + "' ahora es un Boton3D.");
        return b;
    }

    static string Estado(string nombre, Object o)
    {
        return o != null ? "OK     " + nombre + ": " + o.name : "FALTA  " + nombre;
    }

    static Transform BuscarTierra()
    {
        foreach (var t in Todos<Transform>())
            if (t.GetComponent("RotatingEarth") != null) return t;
        foreach (var t in Todos<Transform>())
        {
            string n = t.name.ToLowerInvariant();
            if (Contiene(n, "tierra", "earth", "planet", "globo", "mundo") && t.GetComponentInChildren<Renderer>() != null) return t;
        }
        return null;
    }

    static bool TienePunto(Transform tierra, string clave)
    {
        string k = Juego.Normal(clave).Replace(" ", "");
        foreach (var t in tierra.GetComponentsInChildren<Transform>(true))
            if (t != tierra && Juego.Normal(t.name).Replace(" ", "") == k) return true;
        return false;
    }
}
