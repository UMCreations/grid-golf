using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class GameOverController : MonoBehaviour
{
    public TMP_Text failureText;
    public TMP_Text gameoverText;
    public Button retryButton;
    public Button menuButton;
    public CanvasGroup canvasGroup;

    private void OnEnable()
    {
        PlayGameOverSequence();
    }

    private void PlayGameOverSequence()
    {
        // Reset and Hide everything initially
        if (canvasGroup != null) canvasGroup.alpha = 0;
        if (gameoverText != null) gameoverText.gameObject.SetActive(false);
        if (failureText != null) {
            // Save the message, then clear text for typewriter
            string message = failureText.text;
            failureText.text = "GAME OVER";
            failureText.gameObject.SetActive(false);
        }
        if (retryButton != null) retryButton.gameObject.SetActive(false);
        if (menuButton != null) menuButton.gameObject.SetActive(false);

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        // 1. Fade in the background
        if (canvasGroup != null)
        {
            seq.Append(canvasGroup.DOFade(1, 0.5f));
        }

         // 2. Pop in the Victory Title
        if (failureText != null)
        {
            seq.AppendCallback(() => UIAnimationHelper.PopIn(failureText.gameObject, 0.5f, 0.5f));
            seq.AppendInterval(0.3f);
        }

        // 2. Shake and Pop in the Game Over Title
        if (gameoverText != null)
        {
            seq.AppendCallback(() => {
            if (gameoverText != null) 
            {
                gameoverText.gameObject.SetActive(true);
                UIAnimationHelper.DOTextTypewriter(gameoverText, gameoverText.text, 0.05f);
            }
        });
        }
        // 4. Staggered Pop In for buttons
        if (retryButton != null)
            seq.AppendCallback(() => UIAnimationHelper.PopIn(retryButton.gameObject, 0.4f, 0.7f));
        
        seq.AppendInterval(0.15f);

        if (menuButton != null)
            seq.AppendCallback(() => UIAnimationHelper.PopIn(menuButton.gameObject, 0.4f, 0.7f));
    }

    private void Start()
    {
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryClicked);
            
        if (menuButton != null)
            menuButton.onClick.AddListener(OnMenuClicked);
    }

    private void OnDestroy()
    {
        if (retryButton != null)
            retryButton.onClick.RemoveListener(OnRetryClicked);
            
        if (menuButton != null)
            menuButton.onClick.RemoveListener(OnMenuClicked);
    }

    private void OnRetryClicked()
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
