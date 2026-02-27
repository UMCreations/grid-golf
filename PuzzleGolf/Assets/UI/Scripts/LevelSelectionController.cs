using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelSelectionController : MonoBehaviour
{
    [Header("Prefabs")]
    public LevelCardController levelCardPrefab;
    
    [Header("Grid Layout")]
    public Transform gridContentParent;

    [Header("Category Tabs")]
    public Button easyTabBtn;
    public Button mediumTabBtn;
    public Button hardTabBtn;

    [Header("Navigation")]
    public Button backButton;

    private Difficulty currentDifficultyTab = Difficulty.Easy;

    // Cache to avoid destroying/instantiating 100 prefabs over and over
    private LevelCardController[] pooledCards;

    private void Awake()
    {
        pooledCards = new LevelCardController[LevelManager.MAX_LEVELS];
    }

    private void Start()
    {
        if (easyTabBtn != null) easyTabBtn.onClick.AddListener(() => ChangeTab(Difficulty.Easy));
        if (mediumTabBtn != null) mediumTabBtn.onClick.AddListener(() => ChangeTab(Difficulty.Medium));
        if (hardTabBtn != null) hardTabBtn.onClick.AddListener(() => ChangeTab(Difficulty.Hard));
        
        if (backButton != null) backButton.onClick.AddListener(OnBackClicked);

        // Default initialize standard View
        ChangeTab(currentDifficultyTab);
    }

    private void OnEnable()
    {
        if (pooledCards != null)
        {
            ChangeTab(currentDifficultyTab);
        }
    }

    private void ChangeTab(Difficulty diff)
    {
        currentDifficultyTab = diff;
        
        // Highlight active tab visually
        HighlightTab(easyTabBtn, diff == Difficulty.Easy);
        HighlightTab(mediumTabBtn, diff == Difficulty.Medium);
        HighlightTab(hardTabBtn, diff == Difficulty.Hard);

        PopulateGrid();
    }

    private void HighlightTab(Button btn, bool isActive)
    {
        if (btn == null) return;
        
        Image img = btn.GetComponent<Image>();
        if (img != null)
        {
            // Simple MVP visual feedback => Orange for active, Grey for inactive
            img.color = isActive ? new Color(1f, 0.8f, 0.4f) : new Color(0.4f, 0.5f, 0.6f); 
        }
    }

    private void PopulateGrid()
    {
        if (levelCardPrefab == null || gridContentParent == null) return;

        for (int i = 0; i < LevelManager.MAX_LEVELS; i++)
        {
            int levelNum = i + 1; // 1 to 100
            
            if (pooledCards[i] == null)
            {
                LevelCardController newCard = Instantiate(levelCardPrefab, gridContentParent);
                pooledCards[i] = newCard;
            }
            
            pooledCards[i].gameObject.SetActive(true);
            pooledCards[i].Setup(levelNum, currentDifficultyTab, this);
        }
    }

    public void OnLevelSelected(Difficulty diff, int levelIndex)
    {
        if (LevelManager.Instance != null && UIManager.Instance != null)
        {
            // Clear any mid-level cache from previous attempts since we are opening a specific level
            SaveManager.ClearSave();
            
            LevelManager.Instance.SetLevelToPlay(diff, levelIndex);
            
            // Go to gameplay and generate level
            bool showTutorial = false;
            if (LevelManager.Instance.CurrentProfile != null &&
                diff == Difficulty.Easy &&
                levelIndex == 1 &&
                !LevelManager.Instance.CurrentProfile.hasCompletedTutorial)
            {
                showTutorial = true;
            }

            if (showTutorial)
            {
                UIManager.Instance.ShowTutorial();
            }
            else
            {
                UIManager.Instance.ShowGameplayHUD();
            }
            
            if (GridManager.Instance != null)
            {
                GridManager.Instance.InitializeGame();
            }
        }
    }

    private void OnBackClicked()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMainMenu();
        }
    }
}
