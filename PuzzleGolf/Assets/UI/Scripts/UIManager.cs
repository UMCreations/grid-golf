using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Controllers")]
    public MainMenuController mainMenuController;
    public TopHUDController topHUDController;
    public GameWinController gameWinController;
    public GameOverController gameOverController;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Subscribe to GameManager state events to automatically switch UI
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameWonEvent += ShowGameWin;
            GameManager.Instance.OnGameLostEvent += ShowGameOver;
        }

        // Initially show only the Main Menu
        ShowMainMenu();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameWonEvent -= ShowGameWin;
            GameManager.Instance.OnGameLostEvent -= ShowGameOver;
        }
    }

    public void HideAllPanels()
    {
        if (mainMenuController != null) mainMenuController.gameObject.SetActive(false);
        if (topHUDController != null) topHUDController.gameObject.SetActive(false);
        if (gameWinController != null) gameWinController.gameObject.SetActive(false);
        if (gameOverController != null) gameOverController.gameObject.SetActive(false);
    }

    public void ShowMainMenu()
    {
        HideAllPanels();
        if (mainMenuController != null) mainMenuController.gameObject.SetActive(true);
    }

    public void ShowGameplayHUD()
    {
        HideAllPanels();
        if (topHUDController != null) topHUDController.gameObject.SetActive(true);
    }

    public void ShowGameWin()
    {
        HideAllPanels();
        if (gameWinController != null)
        {
            gameWinController.gameObject.SetActive(true);
            gameWinController.UpdateWinText();
        }
    }

    public void ShowGameOver()
    {
        HideAllPanels();
        if (gameOverController != null)
        {
            gameOverController.gameObject.SetActive(true);
        }
    }
}
