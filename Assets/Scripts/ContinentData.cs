using UnityEngine;

[CreateAssetMenu(fileName = "Continent", menuName = "Earth2100/Continent")]
public class ContinentData : ScriptableObject
{
    public string continentName;
    public Vector3 cameraFocusPoint; // punto hacia donde hará zoom la cámara
    public float cameraZoomDistance = 5f;
    public GameObject highlight3D;   // resaltado del continente en el globo
    public Color lightColor = Color.cyan;
}