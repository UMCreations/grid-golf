using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameWinController : MonoBehaviour
{
    public TMP_Text victoryText;
    public Button nextLevelButton;
    public Button menuButton;

    private void Start()
    {
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);
    }

    private void OnDestroy()
    {
        if (nextLevelButton != null)
            nextLevelButton.onClick.RemoveListener(OnNextLevelClicked);
    }

    public void UpdateWinText()
    {
        if (victoryText != null && GameManager.Instance != null)
        {
            victoryText.text = $"Hole in {GameManager.Instance.CurrentStrokes}!";
        }
    }

    private void OnNextLevelClicked()
    {
        // For MVP, restarting the scene serves as restarting
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartLevel();
        }
    }
}
