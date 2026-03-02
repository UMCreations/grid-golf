using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class TopHUDController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text levelText;
    public TMP_Text strokesText;
    public Button menuButton;
    public Button restartButton;

    [Header("Strokes Colors")]
    public Color safeColor = new Color(0.2f, 1f, 0.4f);   // Green
    public Color warningColor = new Color(1f, 0.8f, 0.2f); // Yellow/Orange
    public Color criticalColor = new Color(1f, 0.2f, 0.2f); // Red

    private int lastCurrentStrokes = -1;
    private Tween criticalPulseTween;

    private void OnEnable()
    {
        // Add listener to the UI button so it calls GameManager when clicked
        if (menuButton != null)
        {
            menuButton.onClick.AddListener(OnMenuClicked);
        }
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        // Subscribe to GameManager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStrokeMadeEvent += UpdateStrokesUI;

            // Optional: Initially sync just in case we missed the startup event
            UpdateStrokesUI(GameManager.Instance.CurrentStrokes, GameManager.Instance.MaxStrokes);
        }
        else
        {
            Debug.LogWarning("TopHUDController could not find GameManager.");
        }

        UpdateLevelText();
    }

    private void OnDisable()
    {
        StopCriticalPulse();
        
        // Very important: Unsubscribe from events to prevent memory leaks!
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStrokeMadeEvent -= UpdateStrokesUI;
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(OnMenuClicked);
        }
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartClicked);
        }
    }

    private void UpdateStrokesUI(int currentStrokes, int maxStrokes)
    {
        if (strokesText != null)
        {
            strokesText.text = $"Strokes: {currentStrokes} / {maxStrokes}";
            
            // 1. Color Feedback
            int strokesLeft = maxStrokes - currentStrokes;
            if (strokesLeft <= 1)
            {
                strokesText.color = criticalColor;
                StartCriticalPulse();
            }
            else if (strokesLeft <= 3)
            {
                strokesText.color = warningColor;
                StopCriticalPulse();
            }
            else
            {
                strokesText.color = safeColor;
                StopCriticalPulse();
            }

            // 2. Punch Animation on change (Juice!)
            if (currentStrokes != lastCurrentStrokes && lastCurrentStrokes != -1)
            {
                strokesText.transform.DOPunchScale(Vector3.one * 0.3f, 0.4f, 10, 1f).SetUpdate(true);
                
                // If critical, also do a tiny shake
                if (strokesLeft <= 1)
                {
                    strokesText.transform.DOShakePosition(0.4f, 10f, 20, 90f, false, true).SetUpdate(true);
                }
            }
            
            lastCurrentStrokes = currentStrokes;
        }
    }

    private void StartCriticalPulse()
    {
        if (criticalPulseTween != null && criticalPulseTween.IsActive()) return;

        criticalPulseTween = strokesText.transform.DOScale(1.2f, 0.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
    }

    private void StopCriticalPulse()
    {
        if (criticalPulseTween != null)
        {
            criticalPulseTween.Kill();
            criticalPulseTween = null;
            if (strokesText != null) strokesText.transform.localScale = Vector3.one;
        }
    }

    private void UpdateLevelText()
    {
        if (levelText != null && LevelManager.Instance != null)
        {
            levelText.text = $"Level {LevelManager.Instance.SelectedLevelIndex} ({LevelManager.Instance.SelectedDifficulty})";
        }
        else if (levelText != null)
        {
            levelText.text = "Level ? (-)";
        }
    }

    private void OnMenuClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        
        // Reloading the active scene will default to showing the Main Menu and reset the level state.
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private void OnRestartClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartLevel();
        }
    }
}
