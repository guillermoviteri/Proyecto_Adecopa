using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NivelContinente : MonoBehaviour
{
    [Header("Continente de esta escena (vacío = el que salió en la ruleta)")]
    public string clave;
    public bool mezclarOpciones = true;
    [Tooltip("0 = usa preguntasPorNivel del JSON. Pon 3 para demos cortas.")]
    public int maximoDesafios = 0;

    [Header("Referencias (las arma el menú AlpaBalance)")]
    public PanelIndicadores indicadores;
    public TMP_Text textoContinente;
    public TMP_Text textoInfo;
    public RectTransform tarjeta;
    public TMP_Text textoMaterias;
    public TMP_Text textoTitulo;
    public TMP_Text textoCuerpo;
    public GameObject globo;
    public TMP_Text textoQuien;
    public TMP_Text textoDice;
    public Transform cajaOpciones;
    public Button plantillaOpcion;
    public GameObject panelRetro;
    public TMP_Text textoRetro;
    public Button botonSiguiente;
    public GameObject panelMensaje;
    public RectTransform cajaMensaje;
    public TMP_Text mensajeTitulo;
    public TMP_Text mensajeCuerpo;
    public Button mensajeBotonA;
    public Button mensajeBotonB;

    [Header("Sonidos (opcional)")]
    public AudioClip sonidoBien;
    public AudioClip sonidoMal;
    public AudioClip sonidoDecision;

    static readonly Color azulOpcion = new Color(0.11f, 0.17f, 0.32f, 0.95f);
    static readonly Color apagada = new Color(0.11f, 0.17f, 0.32f, 0.45f);
    static readonly Color verde = new Color(0.16f, 0.62f, 0.36f, 1f);
    static readonly Color rojo = new Color(0.75f, 0.22f, 0.25f, 1f);
    static readonly Color celeste = new Color(0.13f, 0.52f, 0.85f, 1f);

    Continente cont;
    readonly List<Ejercicio> lista = new List<Ejercicio>();
    readonly List<Button> botones = new List<Button>();
    readonly List<Opcion> mostradas = new List<Opcion>();
    readonly List<Opcion> consecuencias = new List<Opcion>();
    Ejercicio ej;
    int indice;
    bool respondido;
    AudioSource audioFuente;

    void Start()
    {
        UIAlpa.ArreglarFuentes(gameObject.scene);
        string k = string.IsNullOrEmpty(clave) ? Juego.actual : clave;
        Juego.AsegurarPartida(k);
        audioFuente = gameObject.AddComponent<AudioSource>();
        audioFuente.playOnAwake = false;
        if (plantillaOpcion != null) plantillaOpcion.gameObject.SetActive(false);
        if (tarjeta != null) tarjeta.gameObject.SetActive(false);

        cont = Juego.Buscar(k);
        if (cont == null)
        {
            Debug.LogError("[AlpaBalance] No existe el continente '" + k + "' en misiones.json. Escribe la clave en el componente NivelContinente.");
            Mensaje("<color=#FF5A5A>FALTA CONFIGURAR</color>", "Escribe la clave del continente (por ejemplo <b>Europa</b>) en el componente NivelContinente.", "Menú", Juego.IrAlMenu, null, null);
            return;
        }
        Juego.actual = cont.clave;

        ElegirDesafios();

        if (textoContinente != null) textoContinente.text = cont.nombre.ToUpperInvariant();
        Info();
        MostrarIntro();
    }

    void ElegirDesafios()
    {
        int nivel = Juego.Nivel;
        int cuantas = maximoDesafios > 0 ? maximoDesafios : Juego.PreguntasDelNivel(nivel);
        var porNivel = new List<Ejercicio>[nivel + 1];
        for (int n = 1; n <= nivel; n++) porNivel[n] = new List<Ejercicio>();
        foreach (var e in cont.ejercicios ?? new Ejercicio[0])
            if (e != null && e.Nivel <= nivel && e.opciones != null && e.opciones.Length > 0) porNivel[e.Nivel].Add(e);

        for (int n = nivel; n >= 1 && lista.Count < cuantas; n--)
        {
            var grupo = porNivel[n];
            if (Juego.Ajustes.elegirAlAzar) Mezclar(grupo);
            for (int i = 0; i < grupo.Count && lista.Count < cuantas; i++) lista.Add(grupo[i]);
        }

        var orden = new List<Ejercicio>(lista);
        lista.Sort((a, b) => a.Nivel != b.Nivel ? a.Nivel.CompareTo(b.Nivel) : orden.IndexOf(a).CompareTo(orden.IndexOf(b)));
    }

    void Info()
    {
        if (textoInfo == null || cont == null) return;
        int actual = Mathf.Min(indice + 1, Mathf.Max(1, lista.Count));
        textoInfo.text = "Nivel " + Juego.Nivel + " de " + Juego.Total + "   ·   País: " + cont.pais + "   ·   Desafío " + actual + " de " + lista.Count;
    }

    void MostrarIntro()
    {
        Mensaje("<color=#FF5A5A>ALERTA INTERNACIONAL</color>",
            "<b>AÑO 2100 · " + cont.nombre.ToUpperInvariant() + "</b>\n\n" + cont.crisis +
            "\n\n<size=80%><color=#9FB3D9>Nivel " + Juego.Nivel + " de " + Juego.Total + " · " + lista.Count +
            " desafíos. Cada respuesta incorrecta baja al menos " + Juego.CastigoActual() + " puntos un indicador, y el castigo crece con cada error seguido." +
            " Si un indicador baja a " + Juego.limiteColapso + "% o menos, la partida termina.</color></size>",
            "Aceptar misión", Empezar, null, null);
    }

    void Empezar()
    {
        panelMensaje.SetActive(false);
        indice = 0;
        Mostrar();
    }

    void Mostrar()
    {
        if (indice >= lista.Count)
        {
            Terminar();
            return;
        }
        ej = lista[indice];
        respondido = false;
        Info();
        tarjeta.gameObject.SetActive(true);
        textoMaterias.text = Etiqueta(ej);
        textoTitulo.text = ej.titulo;
        textoCuerpo.text = ej.texto;
        bool hayGlobo = !string.IsNullOrEmpty(ej.dice);
        globo.SetActive(hayGlobo);
        if (hayGlobo)
        {
            textoQuien.text = string.IsNullOrEmpty(ej.quien) ? "Mensaje" : ej.quien;
            textoDice.text = "\"" + ej.dice + "\"";
        }
        panelRetro.SetActive(false);

        foreach (var b in botones)
        {
            b.gameObject.SetActive(false);
            Destroy(b.gameObject);
        }
        botones.Clear();
        mostradas.Clear();
        var ops = new List<Opcion>(ej.opciones);
        if (mezclarOpciones && !ej.EsDecision) Mezclar(ops);

        char letra = 'A';
        foreach (var op in ops)
        {
            if (op == null) continue;
            var b = Instantiate(plantillaOpcion, cajaOpciones);
            b.gameObject.SetActive(true);
            b.name = "Opcion " + letra;
            var t = b.GetComponentInChildren<TMP_Text>(true);
            if (t != null) t.text = "<b>" + letra + ".</b>  " + op.texto;
            UIAlpa.Colores(b, azulOpcion);
            b.interactable = true;
            var o = op;
            b.onClick.AddListener(() => Elegir(o, b));
            if (ej.EsDecision && indicadores != null)
            {
                var h = b.GetComponent<OpcionHover>();
                if (h == null) h = b.gameObject.AddComponent<OpcionHover>();
                h.entrar = () => { if (!respondido) indicadores.Previa(Juego.EfectosDe(o, ej)); };
                h.salir = () => { if (!respondido) indicadores.QuitarPrevia(); };
            }
            botones.Add(b);
            mostradas.Add(op);
            letra++;
        }
        StartCoroutine(Aparecer());
    }

    string Etiqueta(Ejercicio e)
    {
        var sb = new StringBuilder();
        if (e.materias != null)
        {
            foreach (var m in e.materias)
            {
                if (string.IsNullOrEmpty(m)) continue;
                if (sb.Length > 0) sb.Append(" + ");
                sb.Append(m.ToUpperInvariant());
            }
        }
        if (e.EsDecision)
        {
            if (sb.Length > 0) sb.Append("   ·   ");
            sb.Append("DECISIÓN  <color=#9FB3D9><size=80%>(pasa el mouse por una opción para ver sus efectos)</size></color>");
        }
        return sb.ToString();
    }

    void Elegir(Opcion op, Button elegido)
    {
        if (respondido) return;
        respondido = true;
        if (indicadores != null) indicadores.QuitarPrevia();

        var d = Juego.EfectosDe(op, ej);
        int castigo = Juego.CastigoActual();
        bool castigado = Juego.EsFallo(op, ej) && castigo > 0;
        Juego.RegistrarRespuesta(op, ej);
        Juego.Aplicar(d);
        if (indicadores != null) indicadores.Mostrar(d);
        Juego.Anotar(ej.materias, op.puntos);
        Juego.Anotar(ej.competencias, op.puntos);
        if (!string.IsNullOrEmpty(op.luego)) consecuencias.Add(op);

        Opcion mejor = null;
        foreach (var o in mostradas) if (mejor == null || o.puntos > mejor.puntos) mejor = o;
        bool bien = op.puntos >= Juego.PuntosCorrecto;

        for (int i = 0; i < botones.Count; i++)
        {
            var b = botones[i];
            Color c;
            if (ej.EsDecision) c = b == elegido ? celeste : apagada;
            else if (b == elegido) c = bien ? verde : rojo;
            else if (mostradas[i] == mejor) c = verde;
            else c = apagada;
            var cb = b.colors;
            cb.disabledColor = c;
            b.colors = cb;
            b.interactable = false;
        }

        var sb = new StringBuilder();
        if (ej.EsDecision) sb.Append("<color=#7CC8FF><b>Decisión tomada.</b></color> ");
        else if (bien) sb.Append("<color=#5CFF9D><b>¡Correcto!</b></color> ");
        else sb.Append("<color=#FF7B7B><b>No es correcto.</b></color> ");
        if (!string.IsNullOrEmpty(op.explicacion)) sb.Append(op.explicacion);
        string ef = TextoEfectos(d);
        if (ef.Length > 0) sb.Append("\n<size=85%>").Append(ef).Append("</size>");
        if (castigado)
        {
            sb.Append("\n<size=85%><color=#FF7B7B>Castigo por error: mínimo -").Append(castigo).Append('.');
            if (Juego.erroresSeguidos > 1) sb.Append(" Llevas ").Append(Juego.erroresSeguidos).Append(" errores seguidos.");
            if (Juego.Ajustes.castigoPorRacha > 0) sb.Append(" Si vuelves a fallar, el castigo será mayor.");
            sb.Append("</color></size>");
        }
        if (!string.IsNullOrEmpty(op.luego)) sb.Append("\n<size=85%><color=#FFC83D>Esta decisión tendrá consecuencias con los años...</color></size>");
        textoRetro.text = sb.ToString();
        panelRetro.SetActive(true);

        bool cayo = Juego.Colapso() >= 0;
        string textoBoton = cayo ? "Ver qué pasó" : indice + 1 < lista.Count ? "Siguiente" : "Terminar";
        Configurar(botonSiguiente, textoBoton, cayo ? (Action)Perdiste : Avanzar);
        Sonar(ej.EsDecision ? sonidoDecision : bien ? sonidoBien : sonidoMal);
        StartCoroutine(Reacomodar());
    }

    void Avanzar()
    {
        indice++;
        Mostrar();
    }

    void Terminar()
    {
        tarjeta.gameObject.SetActive(false);
        var sb = new StringBuilder();
        sb.Append("Resolviste los desafíos de <b>").Append(cont.nombre).Append("</b>.\n");
        if (consecuencias.Count > 0)
        {
            sb.Append("\n<color=#FFC83D><b>AÑOS DESPUÉS...</b></color>\n");
            var total = new float[5];
            foreach (var op in consecuencias)
            {
                var d = Juego.LeerEfectos(op.efectosLuego);
                for (int i = 0; i < 5; i++) total[i] += d[i];
                sb.Append("- ").Append(op.luego);
                string ef = TextoEfectos(d);
                if (ef.Length > 0) sb.Append("  <size=85%>").Append(ef).Append("</size>");
                sb.Append('\n');
            }
            Juego.Aplicar(total);
            if (indicadores != null) indicadores.Mostrar(total);
        }

        if (Juego.Colapso() >= 0)
        {
            sb.Append("\n<color=#FF7B7B>Las consecuencias rompieron el equilibrio del planeta.</color>");
            Mensaje("<color=#FFC83D>AÑOS DESPUÉS...</color>", sb.ToString(), "Continuar", Perdiste, null, null);
            return;
        }

        Juego.Completar(cont.clave);
        sb.Append("\n<size=85%><color=#9FB3D9>Continentes completados: ").Append(Juego.completados.Count).Append(" de ").Append(Juego.Total).Append("</color></size>");
        bool fin = Juego.Pendientes().Count == 0;
        Mensaje("<color=#5CFF9D>¡" + cont.nombre.ToUpperInvariant() + " SUPERADO!</color>", sb.ToString(),
            fin ? "Ver resultados finales" : "Girar la ruleta", fin ? (Action)IrAlFinal : VolverARuleta, null, null);
    }

    void Perdiste()
    {
        tarjeta.gameObject.SetActive(false);
        int i = Juego.Colapso();
        string que = i >= 0 ? Juego.indicadoresNombres[i] : "El planeta";
        int valor = Mathf.RoundToInt(Juego.indicadores[Mathf.Max(0, i)]);
        Mensaje("<color=#FF5A5A>EL EQUILIBRIO SE ROMPIÓ</color>",
            "<b>" + que + "</b> bajó a " + valor + "%.\n\nToda decisión tiene consecuencias. Como en un roguelike, empiezas de nuevo y la ruleta elegirá <b>otro orden</b> de continentes.",
            "Nueva partida", NuevaPartida, "Menú principal", MenuPrincipal);
    }

    void NuevaPartida()
    {
        Juego.NuevaPartida();
        Juego.saltarIntro = true;
        Juego.IrAlMenu();
    }

    void MenuPrincipal()
    {
        Juego.enPartida = false;
        Juego.IrAlMenu();
    }

    void VolverARuleta()
    {
        Juego.saltarIntro = true;
        Juego.IrAlMenu();
    }

    void IrAlFinal()
    {
        Transicion.IrA(Juego.escenaFinal);
    }

    void Mensaje(string titulo, string cuerpo, string a, Action accionA, string b, Action accionB)
    {
        if (panelMensaje == null) return;
        panelMensaje.SetActive(true);
        mensajeTitulo.text = titulo;
        mensajeCuerpo.text = cuerpo;
        Configurar(mensajeBotonA, a, accionA);
        Configurar(mensajeBotonB, b, accionB);
        if (cajaMensaje != null) StartCoroutine(Rehacer(cajaMensaje));
    }

    void Configurar(Button b, string texto, Action accion)
    {
        if (b == null) return;
        b.gameObject.SetActive(!string.IsNullOrEmpty(texto));
        b.onClick.RemoveAllListeners();
        if (accion != null) b.onClick.AddListener(() => accion());
        var t = b.GetComponentInChildren<TMP_Text>(true);
        if (t != null) t.text = texto;
    }

    static string TextoEfectos(float[] d)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < 5; i++)
        {
            string f = PanelIndicadores.Formato(d[i]);
            if (f.Length == 0) continue;
            if (sb.Length > 0) sb.Append("    ");
            sb.Append(Juego.indicadoresNombres[i]).Append(' ').Append(f);
        }
        return sb.ToString();
    }

    static void Mezclar<T>(List<T> l)
    {
        for (int i = l.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var x = l[i];
            l[i] = l[j];
            l[j] = x;
        }
    }

    IEnumerator Aparecer()
    {
        var cg = tarjeta.GetComponent<CanvasGroup>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(tarjeta);
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 4f;
            if (cg != null) cg.alpha = Mathf.Clamp01(t);
            tarjeta.localScale = Vector3.one * Mathf.Lerp(0.97f, 1f, Mathf.Clamp01(t));
            if (t < 0.3f) LayoutRebuilder.ForceRebuildLayoutImmediate(tarjeta);
            yield return null;
        }
        if (cg != null) cg.alpha = 1;
        tarjeta.localScale = Vector3.one;
    }

    IEnumerator Reacomodar()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(tarjeta);
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(tarjeta);
    }

    IEnumerator Rehacer(RectTransform rt)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    void Sonar(AudioClip c)
    {
        if (c != null && audioFuente != null) audioFuente.PlayOneShot(c);
    }
}
