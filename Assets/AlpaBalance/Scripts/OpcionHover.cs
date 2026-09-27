using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class OpcionHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Action entrar;
    public Action salir;

    public void OnPointerEnter(PointerEventData e)
    {
        entrar?.Invoke();
    }

    public void OnPointerExit(PointerEventData e)
    {
        salir?.Invoke();
    }
}
