using UnityEngine;

public class GirarLento : MonoBehaviour
{
    public float velocidad = 10f;

    void Update()
    {
        transform.Rotate(0, velocidad * Time.deltaTime, 0, Space.Self);
    }
}
