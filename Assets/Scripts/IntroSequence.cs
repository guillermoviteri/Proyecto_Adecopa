using System.Collections;
using UnityEngine;
using TMPro;

public class IntroSequence : MonoBehaviour
{
    [Header("Referencias")]
    public RotatingEarth earth;
    public TextMeshPro titleText;
    public TextMeshPro subtitleText;
    public GameObject rouletteButton; // botón 3D para continuar

    [Header("Textos")]
    [TextArea] public string title = "AlPABALANCE 2100";
    [TextArea]
    public string subtitle =
        "100 años. Un planeta. Cada decisión tiene consecuencias.\n\n" +
        "El planeta enfrenta diferentes crisis. Tu misión será recorrer " +
        "cinco continentes, resolver problemas y tomar decisiones que " +
        "determinarán el futuro de la humanidad.";

    void Start()
    {
        if (rouletteButton != null) rouletteButton.SetActive(false);
        StartCoroutine(PlayIntro());
    }

    IEnumerator PlayIntro()
    {
        titleText.text = "";
        subtitleText.text = "";

        // Fade-in del título
        yield return StartCoroutine(TypeText(titleText, title, 0.05f));
        yield return new WaitForSeconds(1.5f);

        // Fade-in del subtítulo
        yield return StartCoroutine(TypeText(subtitleText, subtitle, 0.02f));
        yield return new WaitForSeconds(2f);

        if (rouletteButton != null) rouletteButton.SetActive(true);
    }

    IEnumerator TypeText(TextMeshPro tmp, string fullText, float delay)
    {
        tmp.text = "";
        foreach (char c in fullText)
        {
            tmp.text += c;
            yield return new WaitForSeconds(delay);
        }
    }
}