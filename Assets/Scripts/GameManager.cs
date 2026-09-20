using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Escenas")]
    public string introSceneName = "Intro";
    public string rouletteSceneName = "Roulette";
    public string gameSceneName = "World";

    [Header("Estado")]
    public bool hasSaveData = false;
    public string selectedContinent = "";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartNewGame()
    {
        Debug.Log("[Earth2100] Nueva partida iniciada.");
        SceneManager.LoadScene(introSceneName);
    }

    public void ContinueGame()
    {
        if (!hasSaveData) { Debug.LogWarning("No hay partida guardada."); return; }
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSettings() => Debug.Log("Abrir ajustes");
    public void OpenCredits() => Debug.Log("Abrir créditos");

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void GoToRoulette() => SceneManager.LoadScene(rouletteSceneName);
}