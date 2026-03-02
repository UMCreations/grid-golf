using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    [Header("Audio Settings")]
    public CustomUIToggle soundEffectsToggle;
    public CustomUIToggle musicToggle;

    [Header("Gameplay Settings")]
    public CustomUIToggle vibrationToggle;

    [Header("Navigation")]
    public Button backButton;

    private void Start()
    {
        // Add listeners
        if (soundEffectsToggle != null)
            soundEffectsToggle.onValueChanged.AddListener(OnSoundEffectsToggled);
            
        if (musicToggle != null)
            musicToggle.onValueChanged.AddListener(OnMusicToggled);
            
        if (vibrationToggle != null)
            vibrationToggle.onValueChanged.AddListener(OnVibrationToggled);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }

    private void OnEnable()
    {
        // Load values from LevelManager into UI
        if (LevelManager.Instance != null && LevelManager.Instance.CurrentProfile != null)
        {
            if (soundEffectsToggle != null)
                soundEffectsToggle.SetIsOnWithoutNotify(LevelManager.Instance.CurrentProfile.soundEffectsEnabled);

            if (musicToggle != null)
                musicToggle.SetIsOnWithoutNotify(LevelManager.Instance.CurrentProfile.musicEnabled);

            if (vibrationToggle != null)
                vibrationToggle.SetIsOnWithoutNotify(LevelManager.Instance.CurrentProfile.vibrationEnabled);
        }
    }

    private void OnSoundEffectsToggled(bool value)
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.CurrentProfile.soundEffectsEnabled = value;
            LevelManager.Instance.SaveProfile();
        }
    }

    private void OnMusicToggled(bool value)
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.CurrentProfile.musicEnabled = value;
            LevelManager.Instance.SaveProfile();
        }
    }

    private void OnVibrationToggled(bool value)
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.CurrentProfile.vibrationEnabled = value;
            LevelManager.Instance.SaveProfile();
            
            // Provide instant haptic feedback to confirm vibration is ON
            if (value)
            {
#if UNITY_ANDROID || UNITY_IOS
                Handheld.Vibrate();
#endif
            }
        }
    }

    private void OnBackClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

        if (UIManager.Instance != null)
        {
            // Fully delegate navigation to the central UIManager
            UIManager.Instance.ShowMainMenu();
        }
        else
        {
            // Close settings directly if there is no UIManager 
            gameObject.SetActive(false);
        }
    }
}
