using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Classic Mode Buttons")]
    public Button playButton;
    public Button settingsButton;
    public Button levelSelectionButton;
    public Button tutorialButton;

    [Header("Adventure Mode Button")]
    public Button adventureButton;

    private void Start()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);

        if (levelSelectionButton != null)
            levelSelectionButton.onClick.AddListener(OnLevelSelectionClicked);

        if (tutorialButton != null)
            tutorialButton.onClick.AddListener(OnTutorialClicked);

        if (adventureButton != null)
            adventureButton.onClick.AddListener(OnAdventureClicked);
    }

    private void OnDestroy()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayClicked);

        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OnSettingsClicked);

        if (levelSelectionButton != null)
            levelSelectionButton.onClick.RemoveListener(OnLevelSelectionClicked);

        if (tutorialButton != null)
            tutorialButton.onClick.RemoveListener(OnTutorialClicked);

        if (adventureButton != null)
            adventureButton.onClick.RemoveListener(OnAdventureClicked);
    }

    private void OnPlayClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.IsTutorialMode = false;
            // Ensure we're in Classic mode
            LevelManager.Instance.SetLevelToPlay(
                LevelManager.Instance.SelectedDifficulty,
                LevelManager.Instance.SelectedLevelIndex,
                GameMode.Classic
            );
        }

        if (UIManager.Instance != null)
            UIManager.Instance.ShowGameplayHUD();

        if (GridManager.Instance != null)
            GridManager.Instance.InitializeGame();
    }

    private void OnAdventureClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.IsTutorialMode = false;
            // Set Adventure mode — this switches the generator strategy automatically
            LevelManager.Instance.SetLevelToPlay(
                LevelManager.Instance.SelectedDifficulty,
                LevelManager.Instance.SelectedLevelIndex,
                GameMode.Adventure
            );
        }

        if (UIManager.Instance != null)
            UIManager.Instance.ShowGameplayHUD();

        if (GridManager.Instance != null)
            GridManager.Instance.InitializeGame();
    }

    private void OnTutorialClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

        if (LevelManager.Instance != null)
            LevelManager.Instance.IsTutorialMode = true;

        if (GridManager.Instance != null)
            GridManager.Instance.InitializeGame();

        if (UIManager.Instance != null)
            UIManager.Instance.ShowTutorial();
    }

    private void OnSettingsClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

        if (UIManager.Instance != null)
            UIManager.Instance.ShowSettings();
    }

    private void OnLevelSelectionClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

        if (UIManager.Instance != null)
            UIManager.Instance.ShowLevelSelection();
    }
}

