using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;

    [Header("Clips - Gameplay")]
    public AudioClip strokeClip;
    public AudioClip winClip;
    public AudioClip loseClip;
    public AudioClip invalidMoveClip;
    public AudioClip tilePlaceClip;

    [Header("Clips - UI")]
    public AudioClip buttonClickClip;

    [Header("Settings")]
    public float pitchIncrementPerStroke = 0.05f;
    public float basePitch = 1.0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Auto-create audio sources if not assigned
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayStrokeSound(int currentStrokes)
    {
        if (LevelManager.Instance != null && !LevelManager.Instance.CurrentProfile.soundEffectsEnabled) return;
        if (strokeClip == null) return;

        // Pitch increases with every stroke to build tension
        float finalPitch = basePitch + (currentStrokes * pitchIncrementPerStroke);
        sfxSource.pitch = Mathf.Clamp(finalPitch, 0.8f, 2.0f);
        sfxSource.PlayOneShot(strokeClip);
    }

    public void PlayWinSound()
    {
        if (LevelManager.Instance != null && !LevelManager.Instance.CurrentProfile.soundEffectsEnabled) return;
        if (winClip == null) return;

        sfxSource.pitch = 1.0f; // Reset pitch for victory
        sfxSource.PlayOneShot(winClip);
    }

    public void PlayLoseSound()
    {
        if (LevelManager.Instance != null && !LevelManager.Instance.CurrentProfile.soundEffectsEnabled) return;
        if (loseClip == null) return;

        sfxSource.pitch = 1.0f;
        sfxSource.PlayOneShot(loseClip);
    }

    public void PlayButtonClick()
    {
        if (LevelManager.Instance != null && !LevelManager.Instance.CurrentProfile.soundEffectsEnabled) return;
        if (buttonClickClip == null) return;

        sfxSource.pitch = 1.0f;
        sfxSource.PlayOneShot(buttonClickClip);
    }

    public void PlayInvalidMove()
    {
        if (LevelManager.Instance != null && !LevelManager.Instance.CurrentProfile.soundEffectsEnabled) return;
        if (invalidMoveClip == null) return;

        sfxSource.pitch = 0.8f; // Low pitch for error
        sfxSource.PlayOneShot(invalidMoveClip);
    }

    public void PlayTilePlaceSound()
    {
        if (LevelManager.Instance != null && !LevelManager.Instance.CurrentProfile.soundEffectsEnabled) return;
        if (tilePlaceClip == null) return;

        // Slight random pitch for variety
        sfxSource.pitch = 1.0f + Random.Range(-0.1f, 0.1f);
        sfxSource.PlayOneShot(tilePlaceClip);
    }

    public void UpdateSettings()
    {
        if (LevelManager.Instance == null) return;
        
        musicSource.mute = !LevelManager.Instance.CurrentProfile.musicEnabled;
        sfxSource.mute = !LevelManager.Instance.CurrentProfile.soundEffectsEnabled;
    }
}
