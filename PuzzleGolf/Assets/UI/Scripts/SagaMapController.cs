using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class SagaMapController : MonoBehaviour
{
    [Header("UI Dependencies")]
    public RectTransform contentContainer;
    public LevelNodeController nodePrefab;
    public Button backButton;
    public Button playButton;
    public ScrollRect scrollRect;

    [Header("Path Settings")]
    public float verticalSpacing = 200f;
    public float pathWidth = 300f;
    public float frequency = 0.5f;

    private List<LevelNodeController> spawnedNodes = new List<LevelNodeController>();

    private void Awake()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
        }
    }

    private void OnDestroy()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackClicked);
        }

        if (playButton != null)
        {
            playButton.onClick.RemoveListener(OnPlayClicked);
        }
    }

    private void OnEnable()
    {
        PopulateMap();
    }

    private void PopulateMap()
    {
        if (nodePrefab == null || contentContainer == null || LevelManager.Instance == null)
        {
            Debug.LogError("SagaMapController is missing references or LevelManager is not initialized.");
            return;
        }

        // Clear existing nodes if repopulating
        foreach (var node in spawnedNodes)
        {
            if (node != null) Destroy(node.gameObject);
        }
        spawnedNodes.Clear();

        // Also clear children of contentContainer that might be left from editor or previous runs
        for (int i = contentContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(contentContainer.GetChild(i).gameObject);
        }

        int unlockedLevel = LevelManager.Instance.CurrentProfile.adventureUnlocked;
        
        // Update content height - strictly based on levels and spacing
        contentContainer.sizeDelta = new Vector2(contentContainer.sizeDelta.x, LevelManager.MAX_LEVELS * verticalSpacing);

        for (int i = 1; i <= LevelManager.MAX_LEVELS; i++)
        {
            LevelNodeController newNode = Instantiate(nodePrefab, contentContainer);
            
            // Layout positioning - Top-Center Anchored
            RectTransform nodeRect = newNode.GetComponent<RectTransform>();
            nodeRect.anchorMin = new Vector2(0.5f, 1f);
            nodeRect.anchorMax = new Vector2(0.5f, 1f);
            nodeRect.pivot = new Vector2(0.5f, 0.5f);
            
            float xPos = Mathf.Sin(i * frequency) * pathWidth;
            float yPos = -((i - 1) * verticalSpacing) - (verticalSpacing * 0.5f);
            nodeRect.anchoredPosition = new Vector2(xPos, yPos);

            bool isLocked = i > unlockedLevel;
            int starsEarned = LevelManager.Instance.GetAdventureStars(i);
            
            // Get segment color for background
            AdventureSegmentConfig config = AdventureSegmentResolver.GetConfigForLevel(i);
            Color segmentColor = (config != null) ? config.segmentColor : Color.white;

            newNode.Setup(i, isLocked, starsEarned, segmentColor);
            spawnedNodes.Add(newNode);
        }

        // Optional: Implement auto-scrolling to the highest unlocked level
        ScrollToCurrentLevel(unlockedLevel);
    }

    private void ScrollToCurrentLevel(int targetLevel)
    {
        if (scrollRect == null || contentContainer == null) return;
        
        Canvas.ForceUpdateCanvases();

        float totalHeight = contentContainer.sizeDelta.y;
        float viewportHeight = scrollRect.viewport.rect.height;
        
        if (totalHeight <= viewportHeight) return;

        // Calculate position in pixels (relative to top)
        float targetY = ((targetLevel - 1) * verticalSpacing) + (verticalSpacing * 0.5f);
        
        // Normalized position: 0 is bottom, 1 is top
        // Content top is at y=0, bottom is at y = -totalHeight
        // We want targetY to be in the middle of the viewport
        float normalizedPos = 1f - (targetY / (totalHeight - viewportHeight));
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPos);
    }

    private void OnPlayClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

        if (LevelManager.Instance != null)
        {
            // Start the highest unlocked level for the user
            int levelToPlay = LevelManager.Instance.CurrentProfile.adventureUnlocked;
            LevelManager.Instance.SetAdventureLevelToPlay(levelToPlay);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameplayHUD();
        }

        if (GridManager.Instance != null)
        {
            GridManager.Instance.InitializeGame();
        }
    }

    private void OnBackClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMainMenu();
        }
    }
}
