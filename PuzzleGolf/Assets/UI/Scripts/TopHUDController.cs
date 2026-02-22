using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TopHUDController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text strokesText;
    public Button menuButton;

    private void OnEnable()
    {
        // Add listener to the UI button so it calls GameManager when clicked
        if (menuButton != null)
        {
            menuButton.onClick.AddListener(OnMenuClicked);
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
    }

    private void UpdateStrokesUI(int currentStrokes, int maxStrokes)
    {
        if (strokesText != null)
        {
            strokesText.text = $"Strokes: {currentStrokes} / {maxStrokes}";
        }
    }

    private void OnMenuClicked()
    {
        // Reloading the active scene will default to showing the Main Menu and reset the level state.
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
