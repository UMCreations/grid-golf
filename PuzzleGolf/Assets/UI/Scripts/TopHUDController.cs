using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TopHUDController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text levelText;
    public TMP_Text strokesText;
    public Button menuButton;
    public Button restartButton;

    private void OnEnable()
    {
        // Add listener to the UI button so it calls GameManager when clicked
        if (menuButton != null)
        {
            menuButton.onClick.AddListener(OnMenuClicked);
        }
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        // Subscribe to GameManager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStrokeMadeEvent += UpdateStrokesUI;

            // Optional: Initially sync just in case we missed the startup event
            UpdateStrokesUI(GameManager.Instance.CurrentStrokes, GameManager.Instance.MaxStrokes);
        }
        else
        {
            Debug.LogWarning("TopHUDController could not find GameManager.");
        }

        UpdateLevelText();
    }

    private void OnDisable()
    {
        // Very important: Unsubscribe from events to prevent memory leaks!
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStrokeMadeEvent -= UpdateStrokesUI;
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(OnMenuClicked);
        }
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartClicked);
        }
    }

    private void UpdateStrokesUI(int currentStrokes, int maxStrokes)
    {
        if (strokesText != null)
        {
            strokesText.text = $"Strokes: {currentStrokes} / {maxStrokes}";
        }
    }

    private void UpdateLevelText()
    {
        if (levelText != null && LevelManager.Instance != null)
        {
            levelText.text = $"Level {LevelManager.Instance.SelectedLevelIndex} ({LevelManager.Instance.SelectedDifficulty})";
        }
        else if (levelText != null)
        {
            levelText.text = "Level ? (-)";
        }
    }

    private void OnMenuClicked()
    {
        // Reloading the active scene will default to showing the Main Menu and reset the level state.
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private void OnRestartClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartLevel();
        }
    }
}
