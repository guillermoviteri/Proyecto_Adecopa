using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ContinentRoulette : MonoBehaviour
{
    [Header("Datos")]
    public List<ContinentData> continents = new List<ContinentData>();

    [Header("Luces de la ruleta")]
    public List<Light> rouletteLights = new List<Light>();
    public float lightStepDelay = 0.12f;
    public int spinLoops = 3;

    [Header("Cámara")]
    public Camera mainCamera;
    public Transform earthTransform;
    public float zoomDuration = 2.5f;

    [Header("UI")]
    public TextMeshPro resultText;

    private bool spinning;

    public void SpinRoulette()
    {
        if (spinning) return;
        StartCoroutine(SpinRoutine());
    }

    IEnumerator SpinRoutine()
    {
        spinning = true;
        if (resultText != null) resultText.text = "";

        int totalSteps = spinLoops * rouletteLights.Count + Random.Range(0, rouletteLights.Count);
        int selectedIndex = 0;

        for (int i = 0; i < totalSteps; i++)
        {
            int lightIndex = i % rouletteLights.Count;
            HighlightLight(lightIndex);
            selectedIndex = lightIndex % continents.Count;
            yield return new WaitForSeconds(lightStepDelay);
        }

        yield return new WaitForSeconds(0.6f);

        ContinentData chosen = continents[selectedIndex];
        if (resultText != null)
            resultText.text = "→ " + chosen.continentName.ToUpper();

        yield return StartCoroutine(ZoomToContinent(chosen));

        GameManager.Instance.selectedContinent = chosen.continentName;
        spinning = false;
    }

    void HighlightLight(int index)
    {
        for (int i = 0; i < rouletteLights.Count; i++)
            rouletteLights[i].intensity = (i == index) ? 5f : 0.3f;
    }

    IEnumerator ZoomToContinent(ContinentData data)
    {
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        Vector3 dir = (data.cameraFocusPoint - earthTransform.position).normalized;
        Vector3 endPos = data.cameraFocusPoint - dir * data.cameraZoomDistance;
        Quaternion endRot = Quaternion.LookRotation(data.cameraFocusPoint - endPos);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / zoomDuration;
            float smooth = Mathf.SmoothStep(0f, 1f, t);
            mainCamera.transform.position = Vector3.Lerp(startPos, endPos, smooth);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, smooth);
            yield return null;
        }
    }
}