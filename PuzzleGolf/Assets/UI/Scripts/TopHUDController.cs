using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TopHUDController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text strokesText;
    public Button restartButton;

    private void Start()
    {
        // Add listener to the UI button so it calls GameManager when clicked
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
    }

    private void OnDestroy()
    {
        // Very important: Unsubscribe from events to prevent memory leaks!
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStrokeMadeEvent -= UpdateStrokesUI;
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

    private void OnRestartClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartLevel();
        }
    }
}
