using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class SagaMapController : MonoBehaviour
{
    [Header("UI Dependencies")]
    public RectTransform contentContainer;
    public LevelNodeController nodePrefab;
    
    [Header("Navigation")]
    public Button backButton;
    public ScrollRect scrollRect;

    private List<LevelNodeController> spawnedNodes = new List<LevelNodeController>();

    private void Awake()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
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

        int unlockedLevel = LevelManager.Instance.CurrentProfile.adventureUnlocked;

        for (int i = 1; i <= LevelManager.MAX_LEVELS; i++)
        {
            LevelNodeController newNode = Instantiate(nodePrefab, contentContainer);
            
            bool isLocked = i > unlockedLevel;
            int starsEarned = LevelManager.Instance.GetAdventureStars(i);

            newNode.Setup(i, isLocked, starsEarned);
            spawnedNodes.Add(newNode);
        }

        // Optional: Implement auto-scrolling to the highest unlocked level
        ScrollToCurrentLevel(unlockedLevel);
    }

    private void ScrollToCurrentLevel(int targetLevel)
    {
        if (scrollRect == null || contentContainer == null) return;
        
        // Ensure Unity completes layout updates before scrolling
        Canvas.ForceUpdateCanvases();

        float targetNormalizedPos = 1f - ((float)targetLevel / LevelManager.MAX_LEVELS);
        // Clamp between 0 and 1
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(targetNormalizedPos);
    }

    private void OnBackClicked()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMainMenu();
        }
    }
}
