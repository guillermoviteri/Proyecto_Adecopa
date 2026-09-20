using UnityEngine;
using TMPro;

public class MenuButton3D : Clickable3D
{
    public enum MenuAction { StartGame, Continue, Settings, Credits, Exit }

    [Header("Acción del botón")]
    public MenuAction action;

    [Header("Referencias visuales")]
    public TextMeshPro label3D;
    public Renderer buttonRenderer;
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(0.3f, 0.8f, 1f);

    private Material matInstance;

    void Awake()
    {
        if (buttonRenderer != null)
        {
            matInstance = buttonRenderer.material;
            matInstance.color = normalColor;
        }

        onHoverEnter.AddListener(() => SetColor(hoverColor));
        onHoverExit.AddListener(() => SetColor(normalColor));
        onClick.AddListener(HandleClick);
    }

    void SetColor(Color c)
    {
        if (matInstance != null) matInstance.color = c;
    }

    void HandleClick()
    {
        switch (action)
        {
            case MenuAction.StartGame:
                GameManager.Instance.StartNewGame();
                break;
            case MenuAction.Continue:
                GameManager.Instance.ContinueGame();
                break;
            case MenuAction.Settings:
                GameManager.Instance.OpenSettings();
                break;
            case MenuAction.Credits:
                GameManager.Instance.OpenCredits();
                break;
            case MenuAction.Exit:
                GameManager.Instance.QuitGame();
                break;
        }
    }
}