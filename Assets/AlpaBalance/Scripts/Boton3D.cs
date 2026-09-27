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
    public Color colorHover = new Color(0.4f, 0.9f, 1f);

    Vector3 escalaBase;
    bool encima;
    Renderer rend;
    Color colorBase;
    bool puedeColor;

    void Awake()
    {
        escalaBase = transform.localScale;
        if (GetComponent<Collider>() == null)
        {
            var mf = GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            else gameObject.AddComponent<BoxCollider>();
        }
        rend = GetComponent<Renderer>();
        if (rend == null) rend = GetComponentInChildren<Renderer>();
        puedeColor = rend != null && rend.sharedMaterial != null && (rend.sharedMaterial.HasProperty("_BaseColor") || rend.sharedMaterial.HasProperty("_Color"));
        if (puedeColor) colorBase = rend.material.color;
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
        if (cambiarColor && puedeColor && rend != null) rend.material.color = hover ? colorHover : colorBase;
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
