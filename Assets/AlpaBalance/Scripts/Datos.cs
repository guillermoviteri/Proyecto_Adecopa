using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class Boton3D : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public UnityEvent alClic = new UnityEvent();
    public bool activo = true;
    public float escalaHover = 1.12f;
    public float velocidad = 10f;
    public bool cambiarColor = true;
    public bool soloAclarar = true;
    public Color colorHover = new Color(0.4f, 0.9f, 1f);

    Vector3 escalaBase;
    bool encima;
    Renderer rend;
    Color colorBase;
    bool puedeColor;

    void Awake()
    {
        escalaBase = transform.localScale;
        if (GetComponentInChildren<Collider>() == null) PonerCollider();
        rend = GetComponent<Renderer>();
        if (rend == null) rend = GetComponentInChildren<Renderer>();
        puedeColor = rend != null && rend.sharedMaterial != null && (rend.sharedMaterial.HasProperty("_BaseColor") || rend.sharedMaterial.HasProperty("_Color"));
        if (puedeColor) colorBase = rend.material.color;
    }

    void PonerCollider()
    {
        var bc = gameObject.AddComponent<BoxCollider>();
        var mf = GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            bc.center = mf.sharedMesh.bounds.center;
            bc.size = mf.sharedMesh.bounds.size;
            return;
        }
        var rs = GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;
        var b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        Vector3 s = transform.lossyScale;
        bc.center = transform.InverseTransformPoint(b.center);
        bc.size = new Vector3(b.size.x / Mathf.Max(0.0001f, Mathf.Abs(s.x)), b.size.y / Mathf.Max(0.0001f, Mathf.Abs(s.y)), b.size.z / Mathf.Max(0.0001f, Mathf.Abs(s.z)));
    }

    void Update()
    {
        Vector3 meta = encima && activo ? escalaBase * escalaHover : escalaBase;
        transform.localScale = Vector3.Lerp(transform.localScale, meta, Time.deltaTime * velocidad);
    }

    void OnDisable()
    {
        encima = false;
        transform.localScale = escalaBase;
        Pintar(false);
    }

    void Pintar(bool hover)
    {
        if (!cambiarColor || !puedeColor || rend == null) return;
        Color c = soloAclarar ? Color.Lerp(colorBase, Color.white, 0.35f) : colorHover;
        rend.material.color = hover ? c : colorBase;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        encima = true;
        if (activo) Pintar(true);
    }

    public void OnPointerExit(PointerEventData e)
    {
        encima = false;
        Pintar(false);
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (!activo) return;
        encima = false;
        Pintar(false);
        alClic.Invoke();
    }
}
