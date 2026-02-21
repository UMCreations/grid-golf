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
        gameObject.SetActive(false); // Hide by default

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameWonEvent += ShowWinScreen;
        }

        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameWonEvent -= ShowWinScreen;
        }

        if (nextLevelButton != null)
            nextLevelButton.onClick.RemoveListener(OnNextLevelClicked);
    }

    private void ShowWinScreen()
    {
        gameObject.SetActive(true);
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
