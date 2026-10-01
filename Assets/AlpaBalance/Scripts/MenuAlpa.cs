using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class MenuAlpa : MonoBehaviour
{
    [Header("Escena")]
    public Transform tierra;
    public Camera cam;
    public float giroTierra = 8f;

    [Header("UI que ya tienes en el menú")]
    public Button botonComenzar;
    public Button botonSalir;
    public Boton3D comenzar3D;
    public Boton3D salir3D;
    public TMP_Text titulo;
    public TMP_Text subtitulos;

    [Header("Subtítulos (una línea a la vez)")]
    [TextArea(1, 3)]
    public string[] lineas =
    {
        "Año 2100.",
        "100 años. Un planeta.",
        "Cada decisión tiene consecuencias.",
        "El planeta enfrenta diferentes crisis.",
        "Tu misión será recorrer cinco continentes,",
        "resolver problemas y tomar decisiones...",
        "...que determinarán el futuro de la humanidad."
    };
    public float segundosPorLinea = 2.2f;
    public float fundido = 0.5f;
    public bool maquinaDeEscribir = true;

    [Header("Ruleta")]
    public int vueltas = 3;
    public float pasoRapido = 0.06f;
    public float pasoLento = 0.45f;
    public float intensidadLuz = 6f;

    [Header("Zoom al continente")]
    public float duracionZoom = 2.4f;
    public float distanciaFinal = 1.35f;
    public Color colorFlash = new Color(0.85f, 0.95f, 1f);

    [Header("Sonidos (opcional)")]
    public AudioClip sonidoTic;
    public AudioClip sonidoElegido;
    public AudioClip sonidoZoom;

    static readonly Dictionary<string, Vector2> latLon = new Dictionary<string, Vector2>
    {
        { "americasur", new Vector2(-15, -60) },
        { "europa", new Vector2(50, 15) },
        { "africa", new Vector2(5, 20) },
        { "asia", new Vector2(40, 90) },
        { "americanorte", new Vector2(45, -100) }
    };

    class Marca
    {
        public Continente cont;
        public Transform punto;
        public Vector3 local;
        public Color color;
        public Image brillo;
        public Image nucleo;
        public Image chip;
        public TMP_Text chipTexto;
        public bool hecho;
    }

    readonly List<Marca> marcas = new List<Marca>();
    Marca encendida;
    RectTransform capa;
    CanvasGroup grupoMarcas;
    Button botonSaltar;
    Light luz;
    AudioSource audioFuente;
    float radio = 1f;
    bool girando = true;
    bool saltar;
    bool empezado;

    void Awake()
    {
        foreach (var r in gameObject.scene.GetRootGameObjects())
        {
            foreach (var mb in r.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                string n = mb.GetType().Name;
                if (n != "IntroSequence" && n != "ContinentRoulette") continue;
                mb.StopAllCoroutines();
                mb.enabled = false;
                Debug.Log("[AlpaBalance] Apagué el script viejo " + n + " en '" + mb.name + "' para que no escriba encima de los subtítulos.");
            }
        }
    }

    void Start()
    {
        Juego.escenaMenu = gameObject.scene.name;
        UIAlpa.ArreglarFuentes(gameObject.scene);
        if (cam == null) cam = Camera.main;
        if (tierra != null) radio = Radio(tierra);
        audioFuente = gameObject.AddComponent<AudioSource>();
        audioFuente.playOnAwake = false;
        ArmarRuleta();
        ApagarViejos(botonComenzar);
        ApagarViejos(botonSalir);
        if (botonComenzar != null) botonComenzar.onClick.AddListener(Comenzar);
        if (botonSalir != null) botonSalir.onClick.AddListener(Salir);
        if (comenzar3D != null) comenzar3D.alClic.AddListener(Comenzar);
        if (salir3D != null) salir3D.alClic.AddListener(Salir);
        if ((comenzar3D != null || salir3D != null) && cam != null && cam.GetComponent<PhysicsRaycaster>() == null) cam.gameObject.AddComponent<PhysicsRaycaster>();
        if (titulo != null) titulo.raycastTarget = false;
        if (subtitulos != null) subtitulos.raycastTarget = false;
        if (subtitulos != null) subtitulos.text = "";
        if (Juego.enPartida && Juego.saltarIntro)
        {
            Juego.saltarIntro = false;
            empezado = true;
            MostrarBotones(false);
            StartCoroutine(SiguienteContinente());
        }
    }

    void Update()
    {
        if (tierra != null && girando) tierra.Rotate(Vector3.up, giroTierra * Time.deltaTime, Space.World);
        MoverMarcas();
    }

    public void Comenzar()
    {
        if (empezado) return;
        empezado = true;
        Juego.NuevaPartida();
        MostrarBotones(false);
        StartCoroutine(Intro());
    }

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void ApagarViejos(Button b)
    {
        if (b == null) return;
        for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
            b.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
    }

    void MostrarBotones(bool si)
    {
        if (botonComenzar != null) botonComenzar.gameObject.SetActive(si);
        if (botonSalir != null) botonSalir.gameObject.SetActive(si);
        Mostrar3D(comenzar3D, si);
        Mostrar3D(salir3D, si);
    }

    void Mostrar3D(Boton3D b, bool si)
    {
        if (b == null) return;
        bool esTierra = tierra != null && (b.transform == tierra || b.transform.IsChildOf(tierra) || tierra.IsChildOf(b.transform));
        if (esTierra) b.activo = si;
        else b.gameObject.SetActive(si);
    }

    IEnumerator Intro()
    {
        saltar = false;
        botonSaltar.gameObject.SetActive(true);
        foreach (var l in lineas)
        {
            if (saltar) break;
            yield return StartCoroutine(Linea(l));
        }
        botonSaltar.gameObject.SetActive(false);
        yield return StartCoroutine(Ruleta());
    }

    IEnumerator SiguienteContinente()
    {
        if (Juego.Pendientes().Count == 0)
        {
            Transicion.IrA(Juego.escenaFinal, colorFlash, 0.6f);
            yield break;
        }
        yield return StartCoroutine(Linea("Continentes superados: " + Juego.completados.Count + " de " + Juego.Total));
        yield return StartCoroutine(Ruleta());
    }

    IEnumerator Linea(string l)
    {
        if (subtitulos == null) yield break;
        subtitulos.text = l;
        subtitulos.alpha = 0;
        subtitulos.maxVisibleCharacters = maquinaDeEscribir ? 0 : 99999;
        float dur = Mathf.Max(fundido, maquinaDeEscribir ? l.Length / 40f : 0);
        float t = 0;
        while (t < dur && !saltar)
        {
            t += Time.deltaTime;
            subtitulos.alpha = Mathf.Clamp01(t / fundido);
            if (maquinaDeEscribir) subtitulos.maxVisibleCharacters = Mathf.CeilToInt(l.Length * Mathf.Clamp01(t / dur));
            yield return null;
        }
        subtitulos.alpha = 1;
        subtitulos.maxVisibleCharacters = 99999;
        float espera = segundosPorLinea + l.Length * 0.02f;
        t = 0;
        while (t < espera && !saltar)
        {
            t += Time.deltaTime;
            yield return null;
        }
        float salida = saltar ? fundido * 0.3f : fundido;
        t = 0;
        while (t < salida)
        {
            t += Time.deltaTime;
            subtitulos.alpha = 1 - Mathf.Clamp01(t / salida);
            yield return null;
        }
        subtitulos.alpha = 0;
    }

    void ArmarRuleta()
    {
        if (UIAlpa.redondo == null) UIAlpa.redondo = UIAlpa.SpriteRedondo();
        var cv = UIAlpa.CrearCanvas("CanvasRuleta", 50);
        capa = UIAlpa.Llenar(UIAlpa.Nuevo("Marcas", cv.transform));
        grupoMarcas = capa.gameObject.AddComponent<CanvasGroup>();
        grupoMarcas.alpha = 0;
        grupoMarcas.blocksRaycasts = false;
        grupoMarcas.interactable = false;

        var fila = UIAlpa.Poner(UIAlpa.Nuevo("Fila", capa), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(1500, 52));
        var h = UIAlpa.Horizontal(fila.gameObject, 0, 14);
        h.childForceExpandWidth = true;
        h.childForceExpandHeight = true;

        var spr = UIAlpa.SpriteBrillo();
        int i = 0;
        foreach (var c in Juego.Datos.continentes)
        {
            if (c == null) continue;
            var m = new Marca { cont = c, color = Juego.ColorDe(c, i) };
            m.punto = BuscarPunto(c.clave);
            if (m.punto == null) m.local = DireccionDefecto(c.clave);
            m.brillo = UIAlpa.Panel("Luz_" + c.clave, capa, new Color(m.color.r, m.color.g, m.color.b, 0), false);
            m.brillo.sprite = spr;
            m.brillo.raycastTarget = false;
            UIAlpa.Poner(m.brillo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150));
            m.nucleo = UIAlpa.Panel("Nucleo", m.brillo.transform, new Color(1, 1, 1, 0), false);
            m.nucleo.sprite = spr;
            m.nucleo.raycastTarget = false;
            UIAlpa.Poner(m.nucleo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
            m.chip = UIAlpa.Panel("Chip_" + c.clave, fila, new Color(0.05f, 0.08f, 0.16f, 0.85f));
            m.chip.raycastTarget = false;
            m.chipTexto = UIAlpa.Texto("Texto", m.chip.transform, c.nombre, 24, Color.white, TextAlignmentOptions.Center, true);
            UIAlpa.Llenar(m.chipTexto.rectTransform, 4);
            marcas.Add(m);
            i++;
        }

        botonSaltar = UIAlpa.Boton("Saltar", cv.transform, "Saltar  »", new Color(1, 1, 1, 0.18f), Color.white, 24);
        UIAlpa.Poner((RectTransform)botonSaltar.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 96), new Vector2(210, 56));
        botonSaltar.onClick.AddListener(() => saltar = true);
        botonSaltar.gameObject.SetActive(false);

        luz = new GameObject("LuzRuleta").AddComponent<Light>();
        luz.type = LightType.Point;
        luz.range = radio * 1.6f;
        luz.intensity = 0;
    }

    void Pintar()
    {
        foreach (var m in marcas)
        {
            bool on = m == encendida;
            if (m.hecho)
            {
                m.chip.color = new Color(0.25f, 0.25f, 0.3f, 0.5f);
                m.chipTexto.text = "<s>" + m.cont.nombre + "</s>";
                m.chipTexto.color = new Color(1, 1, 1, 0.45f);
            }
            else if (on)
            {
                m.chip.color = m.color;
                m.chipTexto.text = m.cont.nombre;
                m.chipTexto.color = new Color(0.03f, 0.05f, 0.1f);
            }
            else
            {
                m.chip.color = new Color(0.05f, 0.08f, 0.16f, 0.85f);
                m.chipTexto.text = m.cont.nombre;
                m.chipTexto.color = Color.white;
            }
        }
    }

    void MoverMarcas()
    {
        if (marcas.Count == 0 || cam == null || tierra == null || capa == null) return;
        Vector3 haciaCam = (cam.transform.position - tierra.position).normalized;
        foreach (var m in marcas)
        {
            Vector3 p = Posicion(m);
            Vector3 sp = cam.WorldToScreenPoint(p);
            bool delante = sp.z > 0 && Vector3.Dot((p - tierra.position).normalized, haciaCam) > 0.05f;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(capa, sp, null, out Vector2 lp);
            m.brillo.rectTransform.anchoredPosition = lp;
            bool on = m == encendida;
            float meta = !delante ? 0 : m.hecho ? 0.15f : on ? 1f : 0.4f;
            Color col = m.hecho ? Color.gray : m.color;
            float a = Mathf.MoveTowards(m.brillo.color.a, meta, Time.deltaTime * 6);
            m.brillo.color = new Color(col.r, col.g, col.b, a);
            m.nucleo.color = new Color(1, 1, 1, a);
            float s = on ? 1.25f + Mathf.Sin(Time.time * 12) * 0.12f : 0.7f;
            m.brillo.rectTransform.localScale = Vector3.one * s;
        }
        if (luz != null && encendida != null)
        {
            Vector3 p = Posicion(encendida);
            luz.transform.position = p + (p - tierra.position).normalized * radio * 0.35f;
        }
    }

    Vector3 Posicion(Marca m)
    {
        if (m.punto != null) return m.punto.position;
        return tierra.position + tierra.TransformDirection(m.local) * radio;
    }

    Transform BuscarPunto(string clave)
    {
        if (tierra == null) return null;
        string k = Juego.Normal(clave).Replace(" ", "");
        foreach (var t in tierra.GetComponentsInChildren<Transform>(true))
            if (t != tierra && Juego.Normal(t.name).Replace(" ", "") == k) return t;
        Debug.LogWarning("[AlpaBalance] Crea un objeto vacío llamado '" + clave + "' dentro de la Tierra, encima de ese continente.");
        return null;
    }

    IEnumerator Ruleta()
    {
        if (tierra == null || cam == null)
        {
            Debug.LogError("[AlpaBalance] Asigna 'tierra' y 'cam' en MenuAlpa.");
            yield break;
        }
        var pend = new List<Marca>();
        foreach (var m in marcas)
        {
            m.hecho = Juego.Completado(m.cont.clave);
            if (!m.hecho) pend.Add(m);
        }
        if (pend.Count == 0)
        {
            Transicion.IrA(Juego.escenaFinal, colorFlash, 0.6f);
            yield break;
        }
        encendida = null;
        Pintar();
        yield return StartCoroutine(Fundir(grupoMarcas, 1, 0.6f));
        if (subtitulos != null)
        {
            subtitulos.text = "RULETA DE CONTINENTES";
            subtitulos.maxVisibleCharacters = 99999;
            subtitulos.alpha = 1;
        }
        yield return new WaitForSeconds(0.8f);

        var elegida = pend[Random.Range(0, pend.Count)];
        int n = pend.Count;
        int total = vueltas * n + pend.IndexOf(elegida) + 1;
        luz.intensity = intensidadLuz;
        for (int i = 0; i < total; i++)
        {
            encendida = pend[i % n];
            luz.color = encendida.color;
            Pintar();
            Sonar(sonidoTic);
            float k = total <= 1 ? 1 : i / (float)(total - 1);
            yield return new WaitForSeconds(Mathf.Lerp(pasoRapido, pasoLento, k * k * k));
        }
        Sonar(sonidoElegido);
        if (subtitulos != null) subtitulos.text = "» " + elegida.cont.nombre.ToUpperInvariant() + " «";
        yield return StartCoroutine(Latir(elegida));
        yield return new WaitForSeconds(0.5f);

        if (!Transicion.Existe(elegida.cont.Escena))
        {
            if (subtitulos != null) subtitulos.text = "Falta la escena '" + elegida.cont.Escena + "' en Build Settings";
            Debug.LogError("[AlpaBalance] Falta la escena '" + elegida.cont.Escena + "'. Usa el menú AlpaBalance > 2.");
            yield break;
        }
        yield return StartCoroutine(Zoom(elegida));
    }

    IEnumerator Latir(Marca m)
    {
        var rt = m.chip.rectTransform;
        var st = subtitulos != null ? subtitulos.rectTransform : null;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 2f;
            float s = 1 + Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 0.18f;
            rt.localScale = Vector3.one * s;
            if (st != null) st.localScale = Vector3.one * s;
            yield return null;
        }
        rt.localScale = Vector3.one;
        if (st != null) st.localScale = Vector3.one;
    }

    IEnumerator Zoom(Marca m)
    {
        girando = false;
        Sonar(sonidoZoom);
        Juego.actual = m.cont.clave;
        Vector3 centro = tierra.position;
        Vector3 dirP = (Posicion(m) - centro).normalized;
        Vector3 dirC = (cam.transform.position - centro).normalized;
        Quaternion r0 = tierra.rotation;
        Quaternion r1 = Quaternion.FromToRotation(dirP, dirC) * r0;
        Vector3 p0 = cam.transform.position;
        Quaternion q0 = cam.transform.rotation;
        Vector3 p1 = centro + dirC * radio * distanciaFinal;
        Quaternion q1 = Quaternion.LookRotation(centro - p1, Vector3.up);
        float f0 = cam.fieldOfView;
        float o0 = cam.orthographicSize;
        float a0 = titulo != null ? titulo.alpha : 0;
        bool yaSeFue = false;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / Mathf.Max(0.1f, duracionZoom);
            float giro = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t * 1.8f));
            float acerca = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 0.2f) / 0.8f));
            tierra.rotation = Quaternion.Slerp(r0, r1, giro);
            cam.transform.position = Vector3.Lerp(p0, p1, acerca * acerca);
            cam.transform.rotation = Quaternion.Slerp(q0, q1, giro);
            if (cam.orthographic) cam.orthographicSize = Mathf.Lerp(o0, radio * 0.7f, acerca);
            else cam.fieldOfView = Mathf.Lerp(f0, f0 * 0.85f, acerca);
            if (titulo != null) titulo.alpha = Mathf.Lerp(a0, 0, giro);
            if (subtitulos != null) subtitulos.alpha = 1 - acerca;
            grupoMarcas.alpha = 1 - acerca;
            if (!yaSeFue && t > 0.72f)
            {
                yaSeFue = true;
                Transicion.IrA(m.cont.Escena, colorFlash, 0.5f);
            }
            yield return null;
        }
    }

    IEnumerator Fundir(CanvasGroup g, float meta, float d)
    {
        float a = g.alpha;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, d);
            g.alpha = Mathf.Lerp(a, meta, t);
            yield return null;
        }
        g.alpha = meta;
    }

    void Sonar(AudioClip c)
    {
        if (c != null && audioFuente != null) audioFuente.PlayOneShot(c);
    }

    public static float Radio(Transform t)
    {
        var rs = t.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return Mathf.Max(0.5f, t.lossyScale.x * 0.5f);
        var b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return Mathf.Max(b.extents.x, b.extents.y, b.extents.z);
    }

    public static Vector3 DireccionDefecto(string clave)
    {
        if (!latLon.TryGetValue(Juego.Normal(clave).Replace(" ", ""), out Vector2 ll)) ll = new Vector2(Random.Range(-40f, 40f), Random.Range(-180f, 180f));
        float la = ll.x * Mathf.Deg2Rad;
        float lo = ll.y * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(la) * Mathf.Sin(lo), Mathf.Sin(la), -Mathf.Cos(la) * Mathf.Cos(lo));
    }

    void OnDrawGizmos()
    {
        if (tierra == null) return;
        float r = Radio(tierra) * 0.06f;
        foreach (var t in tierra.GetComponentsInChildren<Transform>(true))
        {
            if (t == tierra || !latLon.ContainsKey(Juego.Normal(t.name).Replace(" ", ""))) continue;
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(t.position, r);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(t.position, t.name);
#endif
        }
    }
}
