using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public Button playButton;
    public Button settingsButton;
    public Button levelSelectionButton;
    public Button tutorialButton;

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

        if (tutorialButton != null)
        {
            tutorialButton.onClick.AddListener(OnTutorialClicked);
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

        if (tutorialButton != null)
        {
            tutorialButton.onClick.RemoveListener(OnTutorialClicked);
        }
    }

    private void OnPlayClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.IsTutorialMode = false;
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameplayHUD();
        }
        
        if (GridManager.Instance != null)
        {
            GridManager.Instance.InitializeGame();
        }
    }

    private void OnTutorialClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.IsTutorialMode = true;
        }

        if (GridManager.Instance != null)
        {
            GridManager.Instance.InitializeGame();
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowTutorial();
        }
    }

    private void OnSettingsClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowSettings();
        }
    }

    private void OnLevelSelectionClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowLevelSelection();
        }
    }
}
