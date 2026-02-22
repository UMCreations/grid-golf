using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    [Header("Audio Settings")]
    public Toggle soundEffectsToggle;
    public Toggle musicToggle;

    [Header("Gameplay Settings")]
    public Toggle vibrationToggle;

    [Header("Account Settings")]
    public Button resetProgressButton;
    public Button backButton;

    // Optional: a reference to the main menu or whatever panel needs to be enabled upon hitting "back"
    [Header("Navigation")]
    public GameObject previousMenuPanel; 

    private void Start()
    {
        // Add listeners
        if (soundEffectsToggle != null)
            soundEffectsToggle.onValueChanged.AddListener(OnSoundEffectsToggled);
            
        if (musicToggle != null)
            musicToggle.onValueChanged.AddListener(OnMusicToggled);
            
        if (vibrationToggle != null)
            vibrationToggle.onValueChanged.AddListener(OnVibrationToggled);
            
        if (resetProgressButton != null)
            resetProgressButton.onClick.AddListener(OnResetProgressClicked);
            
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }

    private void OnEnable()
    {
        // Load values from LevelManager into UI
        if (LevelManager.Instance != null && LevelManager.Instance.CurrentProfile != null)
        {
            if (soundEffectsToggle != null)
                soundEffectsToggle.isOn = LevelManager.Instance.CurrentProfile.soundEffectsEnabled;

            if (musicToggle != null)
                musicToggle.isOn = LevelManager.Instance.CurrentProfile.musicEnabled;

            if (vibrationToggle != null)
                vibrationToggle.isOn = LevelManager.Instance.CurrentProfile.vibrationEnabled;
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

    private void OnResetProgressClicked()
    {
        // Note: For a production app, you might want to add a secondary "Are you sure?" popup panel here.
        // For now, based on the MVP image layout, we trigger it directly since the text says "ACTION CANNOT BE UNDONE".
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.ResetProgress();
            
            // Refresh toggle UI just in case defaults changed
            OnEnable();
            
            Debug.Log("Account Progress completely reset!");
        }
    }

    private void OnBackClicked()
    {
        // Close the settings screen
        gameObject.SetActive(false);
        
        // Turn on previous Menu/Panel if assigned
        if (previousMenuPanel != null)
        {
            previousMenuPanel.SetActive(true);
        }
    }
}
