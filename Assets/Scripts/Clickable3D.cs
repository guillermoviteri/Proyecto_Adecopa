using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class Clickable3D : MonoBehaviour
{
    [Header("Eventos")]
    public UnityEvent onClick;
    public UnityEvent onHoverEnter;
    public UnityEvent onHoverExit;

    [Header("Configuración")]
    public bool interactable = true;
    public float hoverScale = 1.1f;
    public float animationSpeed = 8f;

    private Vector3 originalScale;
    private bool isHovered;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        Vector3 target = isHovered && interactable ? originalScale * hoverScale : originalScale;
        transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * animationSpeed);
    }

    void OnMouseEnter()
    {
        if (!interactable) return;
        isHovered = true;
        onHoverEnter?.Invoke();
    }

    void OnMouseExit()
    {
        isHovered = false;
        onHoverExit?.Invoke();
    }

    void OnMouseDown()
    {
        if (!interactable) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        onClick?.Invoke();
    }

    public void SetInteractable(bool value) => interactable = value;
}
