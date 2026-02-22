using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public Button playButton;
    public Button settingsButton;

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
    }

    private void OnPlayClicked()
    {
        // Tell the central UI Manager to transition states
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameplayHUD();
        }
    }

    private void OnSettingsClicked()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowSettings();
        }
    }
}
