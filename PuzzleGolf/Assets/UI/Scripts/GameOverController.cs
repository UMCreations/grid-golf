using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverController : MonoBehaviour
{
    public TMP_Text failureText;
    public Button retryButton;
    public Button menuButton;

    private void Start()
    {
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryClicked);
    }

    private void OnDestroy()
    {
        if (retryButton != null)
            retryButton.onClick.RemoveListener(OnRetryClicked);
    }



    private void OnRetryClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartLevel();
        }
    }
}
