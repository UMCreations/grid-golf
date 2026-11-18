using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class TopHUDController : MonoBehaviour
{
    private const int PowerUpTestCost = 10;

    [Header("UI References")]
    public TMP_Text levelText;
    public TMP_Text strokesText;
    public Button menuButton;
    public Button restartButton;
    [SerializeField] private Button openStoreButton;
    [SerializeField] private StoreController storeController;
    [SerializeField] private Button undoPowerUpButton;
    [SerializeField] private Button hintPowerUpButton;
    [SerializeField] private TMP_Text hintPowerUpLabel;
    [SerializeField] private TMP_Text golfCoinsText;
    [SerializeField] private TMP_Text undoPowerUpCostText;
    [SerializeField] private TMP_Text hintPowerUpCostText;

    private int lastCurrentStrokes = -1;
    private Tween criticalPulseTween;
    private WalletService subscribedWallet;

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
        if (openStoreButton != null)
            openStoreButton.onClick.AddListener(OnOpenStoreClicked);

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
        SubscribeToWallet();
        RefreshWalletUI();
        RefreshPowerUpButtons();
    }

    private void Update()
    {
        // Wallet may be initialized after this HUD enables during scene startup.
        SubscribeToWallet();
        RefreshPowerUpButtons();
    }

    private void OnDisable()
    {
        StopCriticalPulse();
        CancelInvoke(nameof(ResetHintLabel));
        ResetHintLabel();
        
        // Very important: Unsubscribe from events to prevent memory leaks!
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStrokeMadeEvent -= UpdateStrokesUI;
        }
        if (subscribedWallet != null)
        {
            subscribedWallet.GolfCoinsChanged -= OnGolfCoinsChanged;
            subscribedWallet = null;
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(OnMenuClicked);
        }
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartClicked);
        }
        if (openStoreButton != null)
            openStoreButton.onClick.RemoveListener(OnOpenStoreClicked);
    }

    private void UpdateStrokesUI(int currentStrokes, int maxStrokes)
    {
        if (strokesText != null)
        {
            bool hasStrokeLimit = GameManager.Instance == null || GameManager.Instance.HasStrokeLimit;
            strokesText.text = hasStrokeLimit
                ? $"{currentStrokes} / {maxStrokes}"
                : currentStrokes.ToString();
            strokesText.color = Color.white;
            
            // 1. Critical Stroke Feedback
            int strokesLeft = maxStrokes - currentStrokes;
            if (hasStrokeLimit && strokesLeft <= 1)
            {
                StartCriticalPulse();
            }
            else
            {
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
        RefreshPowerUpButtons();
    }

    private void RefreshPowerUpButtons()
    {
        BallController ball = GridManager.Instance != null ? GridManager.Instance.CurrentBall : null;
        WalletService wallet = LevelManager.Instance != null ? LevelManager.Instance.Wallet : null;
        bool canAffordPowerUp = wallet != null && wallet.GolfCoins >= PowerUpTestCost;
        if (undoPowerUpButton != null)
            undoPowerUpButton.interactable = canAffordPowerUp && ball != null && ball.CanUndoLastMove;
        if (hintPowerUpButton != null)
            hintPowerUpButton.interactable = canAffordPowerUp && ball != null && ball.CanUsePowerUps;
    }

    private void OnOpenStoreClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

        if (storeController != null)
            storeController.OpenStore();
        else
            Debug.LogWarning("TopHUDController has no StoreController assigned.");
    }

    private void SubscribeToWallet()
    {
        WalletService wallet = LevelManager.Instance != null ? LevelManager.Instance.Wallet : null;
        if (wallet == subscribedWallet) return;

        if (subscribedWallet != null)
            subscribedWallet.GolfCoinsChanged -= OnGolfCoinsChanged;
        subscribedWallet = wallet;
        if (subscribedWallet != null)
            subscribedWallet.GolfCoinsChanged += OnGolfCoinsChanged;
        RefreshWalletUI();
    }

    private void OnGolfCoinsChanged(int _) => RefreshWalletUI();

    private void RefreshWalletUI()
    {
        WalletService wallet = LevelManager.Instance != null ? LevelManager.Instance.Wallet : null;
        if (golfCoinsText != null)
            golfCoinsText.text = wallet != null ? wallet.GolfCoins.ToString() : "0";

        if (undoPowerUpCostText != null)
            undoPowerUpCostText.text = PowerUpTestCost.ToString();
        if (hintPowerUpCostText != null)
            hintPowerUpCostText.text = PowerUpTestCost.ToString();
    }

    public void OnUndoPowerUpClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        BallController ball = GridManager.Instance != null ? GridManager.Instance.CurrentBall : null;
        WalletService wallet = LevelManager.Instance != null ? LevelManager.Instance.Wallet : null;
        if (ball != null && ball.CanUndoLastMove && wallet != null &&
            wallet.TrySpendGolfCoins(PowerUpTestCost) && !ball.TryUndoLastMove())
            wallet.TryGrantGolfCoins(PowerUpTestCost);
        RefreshPowerUpButtons();
    }

    public void OnHintPowerUpClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        BallController ball = GridManager.Instance != null ? GridManager.Instance.CurrentBall : null;
        WalletService wallet = LevelManager.Instance != null ? LevelManager.Instance.Wallet : null;
        if (ball == null || wallet == null || !ball.CanUsePowerUps) return;
        if (wallet.GolfCoins < PowerUpTestCost)
        {
            SetHintLabel("NEED 10 COINS");
            return;
        }

        if (!wallet.TrySpendGolfCoins(PowerUpTestCost)) return;
        if (ball.TryShowHint())
        {
            RefreshPowerUpButtons();
            return;
        }

        wallet.TryGrantGolfCoins(PowerUpTestCost);
        SetHintLabel("NO HINT");
        RefreshPowerUpButtons();
    }

    private void SetHintLabel(string message)
    {
        if (hintPowerUpLabel != null)
        {
            hintPowerUpLabel.text = message;
            CancelInvoke(nameof(ResetHintLabel));
            Invoke(nameof(ResetHintLabel), 1.2f);
        }
    }

    private void ResetHintLabel()
    {
        if (hintPowerUpLabel != null) hintPowerUpLabel.text = "HINT";
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
            LevelData currentLevel = GridManager.Instance != null ? GridManager.Instance.CurrentLevelData : null;
            if (currentLevel != null && !string.IsNullOrEmpty(currentLevel.communityLevelId))
            {
                levelText.text = currentLevel.communityLevelName;
                return;
            }

            bool isAdventure = LevelManager.Instance.SelectedGameMode == GameMode.Adventure;
            int levelNum = isAdventure ? LevelManager.Instance.AdventureLevelIndex : LevelManager.Instance.SelectedLevelIndex;
            string modeName = isAdventure ? "Adventure" : LevelManager.Instance.SelectedDifficulty.ToString();
            
            levelText.text = $"Level {levelNum} ({modeName})";
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
