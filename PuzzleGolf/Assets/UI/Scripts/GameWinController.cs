using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameWinController : MonoBehaviour
{
    public TMP_Text victoryText;
    public Button nextLevelButton;
    public Button replayButton;
    public Button menuButton;

    private void Start()
    {
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);
            
        if (replayButton != null)
            replayButton.onClick.AddListener(OnReplayClicked);
            
        if (menuButton != null)
            menuButton.onClick.AddListener(OnMenuClicked);
    }

    private void OnDestroy()
    {
        if (nextLevelButton != null)
            nextLevelButton.onClick.RemoveListener(OnNextLevelClicked);
            
        if (replayButton != null)
            replayButton.onClick.RemoveListener(OnReplayClicked);
            
        if (menuButton != null)
            menuButton.onClick.RemoveListener(OnMenuClicked);
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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadNextLevel();
        }
    }
    
    private void OnReplayClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartLevel();
        }
    }

    private void OnMenuClicked()
    {
        if (GridManager.Instance != null)
        {
            GridManager.Instance.ResetLevelState();
        }

        // Reloading the active scene will default to showing the Main Menu and reset the level state.
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
