using UnityEngine;

public class RotatingEarth : MonoBehaviour
{
    [Header("Rotación")]
    public float rotationSpeed = 5f;
    public Vector3 rotationAxis = Vector3.up;

    [Header("Zoom opcional")]
    public bool enableIdleZoom = true;
    public float zoomAmplitude = 0.05f;
    public float zoomSpeed = 0.5f;

    private Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);

        if (enableIdleZoom)
        {
            float factor = 1f + Mathf.Sin(Time.time * zoomSpeed) * zoomAmplitude;
            transform.localScale = baseScale * factor;
        }
    }
}