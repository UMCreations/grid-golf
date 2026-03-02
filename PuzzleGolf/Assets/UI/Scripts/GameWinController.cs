using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class GameWinController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text titleText; // "VICTORY"
    public TMP_Text victoryText; // "Hole in X"
    public Button nextLevelButton;
    public Button replayButton;
    public Button menuButton;
    public CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    public float animationDelay = 0.2f;

    private void OnEnable()
    {
        PlayWinSequence();
    }

    private void PlayWinSequence()
    {
        // Reset and Hide everything initially
        if (canvasGroup != null) canvasGroup.alpha = 0;
        if (titleText != null) titleText.gameObject.SetActive(false);
        if (victoryText != null) victoryText.gameObject.SetActive(false);
        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(false);
        if (replayButton != null) replayButton.gameObject.SetActive(false);
        if (menuButton != null) menuButton.gameObject.SetActive(false);

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        // 1. Fade in the background
        if (canvasGroup != null)
        {
            seq.Append(canvasGroup.DOFade(1, 0.5f));
        }

        // 2. Pop in the Victory Title
        if (titleText != null)
        {
            seq.AppendCallback(() => UIAnimationHelper.PopIn(titleText.gameObject, 0.5f, 0.5f));
            seq.AppendInterval(0.3f);
        }

        // 3. Typewriter effect for "Hole in X"
        seq.AppendCallback(() => {
            UpdateWinText();
            if (victoryText != null) 
            {
                victoryText.gameObject.SetActive(true);
                UIAnimationHelper.DOTextTypewriter(victoryText, victoryText.text, 0.05f);
            }
        });
        seq.AppendInterval(0.5f);

        // 4. Staggered Pop In for buttons
        if (nextLevelButton != null)
            seq.AppendCallback(() => UIAnimationHelper.PopIn(nextLevelButton.gameObject, 0.4f, 0.7f));
        
        seq.AppendInterval(0.15f);

        if (replayButton != null)
            seq.AppendCallback(() => UIAnimationHelper.PopIn(replayButton.gameObject, 0.4f, 0.7f));

        seq.AppendInterval(0.1f);

        if (menuButton != null)
            seq.AppendCallback(() => UIAnimationHelper.PopIn(menuButton.gameObject, 0.4f, 0.7f));
    }

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
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadNextLevel();
        }
    }
    
    private void OnReplayClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartLevel();
        }
    }

    private void OnMenuClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        if (GridManager.Instance != null)
        {
            GridManager.Instance.ResetLevelState();
        }

        // Reloading the active scene will default to showing the Main Menu and reset the level state.
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
