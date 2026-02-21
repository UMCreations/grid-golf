using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public Button playButton;

    private void Start()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
        }
    }

    private void OnDestroy()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(OnPlayClicked);
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
}
