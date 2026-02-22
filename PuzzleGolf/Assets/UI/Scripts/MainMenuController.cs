using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public Button playButton;
    public Button settingsButton;
    public Button levelSelectionButton;

    private void Start()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
        }
        
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        if (levelSelectionButton != null)
        {
            levelSelectionButton.onClick.AddListener(OnLevelSelectionClicked);
        }
    }

    private void OnDestroy()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(OnPlayClicked);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OnSettingsClicked);
        }

        if (levelSelectionButton != null)
        {
            levelSelectionButton.onClick.RemoveListener(OnLevelSelectionClicked);
        }
    }

    private void OnPlayClicked()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameplayHUD();
        }
        
        if (GridManager.Instance != null)
        {
            GridManager.Instance.InitializeGame();
        }
    }

    private void OnSettingsClicked()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowSettings();
        }
    }

    private void OnLevelSelectionClicked()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowLevelSelection();
        }
    }
}
