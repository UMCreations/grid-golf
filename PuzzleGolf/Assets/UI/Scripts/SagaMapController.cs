using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class SagaMapSegmentArt
{
    [Tooltip("Must match a segment theme name from AdventureSegmentResolver.")]
    public string themeName;
    [Tooltip("Sprite used by this segment's scrolling MapZone Image. Empty keeps the tinted zone.")]
    public Sprite background;
    [Tooltip("Path controls in normalized MapZone coordinates, ordered along the road from top to bottom. X goes left to right; Y goes from 0 at the top to 1 at the bottom. Clear the list to use the global sine path.")]
    public Vector2[] pathPoints =
    {
        new Vector2(0.62f, 0f),
        new Vector2(0.82f, 0.16f),
        new Vector2(0.22f, 0.34f),
        new Vector2(0.80f, 0.53f),
        new Vector2(0.70f, 0.68f),
        new Vector2(0.24f, 0.84f),
        new Vector2(0.76f, 1f)
    };
    [Tooltip("Optional exact position for every level in this segment, normalized to its MapZone. Clear this list to use Path Points instead.")]
    public Vector2[] levelPositions = new Vector2[0];
}

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

    [Header("MapZone Background Images")]
    [Tooltip("Each sprite is assigned to its matching scrolling MapZone Image. The main screen background is unchanged.")]
    public SagaMapSegmentArt[] segmentArt =
    {
        new SagaMapSegmentArt { themeName = "The Fairway" },
        new SagaMapSegmentArt { themeName = "Sandy Shores" },
        new SagaMapSegmentArt { themeName = "Deep Desert" },
        new SagaMapSegmentArt { themeName = "Frozen Peaks" },
        new SagaMapSegmentArt { themeName = "Arctic Hazard" },
        new SagaMapSegmentArt { themeName = "Launch Pad Valley" },
        new SagaMapSegmentArt { themeName = "The Gauntlet" },
        new SagaMapSegmentArt { themeName = "Master's Course" },
        new SagaMapSegmentArt { themeName = "The Final Hole" }
    };

    private List<LevelNodeController> spawnedNodes = new List<LevelNodeController>();
    private float verticalContentPadding;
    private float horizontalPathWidth;

    private void OnValidate()
    {
        EnsureSegmentArtEntries();
    }

    private void Awake()
    {
        EnsureSegmentArtEntries();

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

    public void RefreshMapLayout()
    {
        if (this == null || !Application.isPlaying || !isActiveAndEnabled) return;

        float previousScrollPosition = scrollRect != null ? scrollRect.verticalNormalizedPosition : 1f;
        PopulateMap();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = previousScrollPosition;
            Canvas.ForceUpdateCanvases();
        }
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
        
        Canvas.ForceUpdateCanvases();

        float viewportHeight = (scrollRect != null && scrollRect.viewport != null)
            ? scrollRect.viewport.rect.height
            : 0f;
        verticalContentPadding = Mathf.Max(0f, (viewportHeight - verticalSpacing) * 0.5f);
        float viewportWidth = (scrollRect != null && scrollRect.viewport != null)
            ? scrollRect.viewport.rect.width
            : contentContainer.rect.width;
        float nodeWidth = nodePrefab.GetComponent<RectTransform>().rect.width;
        horizontalPathWidth = Mathf.Min(pathWidth, Mathf.Max(0f, viewportWidth * 0.5f - nodeWidth * 0.5f - 24f));

        // Leave room above and below the route so the first and last level can center in the viewport.
        float contentHeight = LevelManager.MAX_LEVELS * verticalSpacing + verticalContentPadding * 2f;
        contentContainer.sizeDelta = new Vector2(contentContainer.sizeDelta.x, contentHeight);
        Canvas.ForceUpdateCanvases();

        CreateSegmentBackdrops();
        CreateSegmentLabels();

        for (int i = 1; i <= LevelManager.MAX_LEVELS; i++)
        {
            LevelNodeController newNode = Instantiate(nodePrefab, contentContainer);
            newNode.name = $"LevelNode_{i}";
            
            // Layout positioning - Top-Center Anchored
            RectTransform nodeRect = newNode.GetComponent<RectTransform>();
            nodeRect.anchorMin = new Vector2(0.5f, 1f);
            nodeRect.anchorMax = new Vector2(0.5f, 1f);
            nodeRect.pivot = new Vector2(0.5f, 0.5f);
            
            nodeRect.anchoredPosition = GetNodePosition(i);

            bool isLocked = i > unlockedLevel;
            int starsEarned = LevelManager.Instance.GetAdventureStars(i);
            
            newNode.Setup(i, isLocked, starsEarned, i == unlockedLevel);
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

        float scrollableHeight = totalHeight - viewportHeight;
        if (scrollableHeight <= 0f) return;

        float targetY = -GetNodePosition(Mathf.Clamp(targetLevel, 1, LevelManager.MAX_LEVELS)).y;
        float scrollOffset = Mathf.Clamp(targetY - viewportHeight * 0.5f, 0f, scrollableHeight);
        scrollRect.verticalNormalizedPosition = 1f - scrollOffset / scrollableHeight;
    }

    private void EnsureSegmentArtEntries()
    {
        string[] themeNames =
        {
            "The Fairway", "Sandy Shores", "Deep Desert", "Frozen Peaks", "Arctic Hazard",
            "Launch Pad Valley", "The Gauntlet", "Master's Course", "The Final Hole"
        };

        if (segmentArt == null) segmentArt = new SagaMapSegmentArt[0];
        if (segmentArt.Length < themeNames.Length)
            System.Array.Resize(ref segmentArt, themeNames.Length);

        for (int i = 0; i < themeNames.Length; i++)
        {
            if (segmentArt[i] == null) segmentArt[i] = new SagaMapSegmentArt();
            if (string.IsNullOrEmpty(segmentArt[i].themeName))
                segmentArt[i].themeName = themeNames[i];

            if (segmentArt[i].pathPoints == null)
                segmentArt[i].pathPoints = new SagaMapSegmentArt().pathPoints;

            if (segmentArt[i].levelPositions == null)
                segmentArt[i].levelPositions = new Vector2[0];

            for (int point = 0; point < segmentArt[i].pathPoints.Length; point++)
            {
                Vector2 pathPoint = segmentArt[i].pathPoints[point];
                pathPoint.x = Mathf.Clamp01(pathPoint.x);
                pathPoint.y = Mathf.Clamp01(pathPoint.y);
                segmentArt[i].pathPoints[point] = pathPoint;
            }

            for (int level = 0; level < segmentArt[i].levelPositions.Length; level++)
            {
                Vector2 position = segmentArt[i].levelPositions[level];
                position.x = Mathf.Clamp01(position.x);
                position.y = Mathf.Clamp01(position.y);
                segmentArt[i].levelPositions[level] = position;
            }

        }
    }

    private SagaMapSegmentArt GetSegmentArt(string themeName)
    {
        if (segmentArt != null)
        {
            foreach (SagaMapSegmentArt art in segmentArt)
            {
                if (art != null && string.Equals(art.themeName, themeName, System.StringComparison.OrdinalIgnoreCase))
                    return art;
            }
        }

        return null;
    }

    private Vector2 GetNodePosition(int level)
    {
        AdventureSegmentConfig segment = AdventureSegmentResolver.GetConfigForLevel(level);
        SagaMapSegmentArt art = GetSegmentArt(segment != null ? segment.themeName : string.Empty);
        int segmentLevelCount = segment != null ? segment.endLevel - segment.startLevel + 1 : 0;
        float normalizedProgress = segment != null
            ? (level - segment.startLevel + 0.5f) / segmentLevelCount
            : 0.5f;
        bool hasLevelPositions = segment != null && art != null && art.levelPositions != null && art.levelPositions.Length == segmentLevelCount;
        bool hasCustomPath = segment != null && !hasLevelPositions && art != null && art.pathPoints != null && art.pathPoints.Length >= 2;
        Vector2 pathPoint = hasLevelPositions
            ? art.levelPositions[level - segment.startLevel]
            : GetPathPoint(art != null ? art.pathPoints : null, normalizedProgress);

        float nodeWidth = nodePrefab != null ? nodePrefab.GetComponent<RectTransform>().rect.width : 0f;
        float maxX = Mathf.Max(0f, (contentContainer.rect.width - nodeWidth) * 0.5f);
        float xPos = hasLevelPositions || hasCustomPath
            ? Mathf.Clamp((pathPoint.x - 0.5f) * contentContainer.rect.width, -maxX, maxX)
            : Mathf.Sin(level * frequency) * horizontalPathWidth;
        float yPos = hasLevelPositions || hasCustomPath
            ? -(verticalContentPadding + (segment.startLevel - 1) * verticalSpacing + pathPoint.y * (segment.endLevel - segment.startLevel + 1) * verticalSpacing)
            : -(verticalContentPadding + (level - 1) * verticalSpacing + verticalSpacing * 0.5f);
        return new Vector2(xPos, yPos);
    }

    public bool TryGetNormalizedMapZonePosition(LevelNodeController node, out Vector2 normalizedPosition)
    {
        normalizedPosition = new Vector2(0.5f, 0.5f);
        if (node == null || contentContainer == null || verticalSpacing <= 0f) return false;

        AdventureSegmentConfig segment = AdventureSegmentResolver.GetConfigForLevel(node.LevelIndex);
        RectTransform nodeRect = node.GetComponent<RectTransform>();
        if (segment == null || nodeRect == null) return false;

        float contentWidth = contentContainer.rect.width;
        float segmentHeight = (segment.endLevel - segment.startLevel + 1) * verticalSpacing;
        if (contentWidth <= 0f || segmentHeight <= 0f) return false;

        float segmentTop = verticalContentPadding + (segment.startLevel - 1) * verticalSpacing;
        normalizedPosition.x = Mathf.Clamp01(0.5f + nodeRect.anchoredPosition.x / contentWidth);
        normalizedPosition.y = Mathf.Clamp01((-nodeRect.anchoredPosition.y - segmentTop) / segmentHeight);
        return true;
    }

    private Vector2 GetPathPoint(Vector2[] pathPoints, float progress)
    {
        if (pathPoints == null || pathPoints.Length < 2)
            return new Vector2(0.5f, 0.5f);

        float scaledProgress = Mathf.Clamp01(progress) * (pathPoints.Length - 1);
        int fromIndex = Mathf.Min(Mathf.FloorToInt(scaledProgress), pathPoints.Length - 2);
        float t = scaledProgress - fromIndex;
        Vector2 before = pathPoints[Mathf.Max(0, fromIndex - 1)];
        Vector2 from = pathPoints[fromIndex];
        Vector2 to = pathPoints[fromIndex + 1];
        Vector2 after = pathPoints[Mathf.Min(pathPoints.Length - 1, fromIndex + 2)];
        return new Vector2(
            Mathf.Clamp01(CatmullRom(before.x, from.x, to.x, after.x, t)),
            Mathf.Clamp01(CatmullRom(before.y, from.y, to.y, after.y, t)));
    }

    private float CatmullRom(float p0, float p1, float p2, float p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    private void CreateSegmentBackdrops()
    {
        AdventureSegmentConfig segment = AdventureSegmentResolver.GetConfigForLevel(1);
        int segmentStart = 1;

        for (int level = 2; level <= LevelManager.MAX_LEVELS; level++)
        {
            AdventureSegmentConfig nextSegment = AdventureSegmentResolver.GetConfigForLevel(level);
            if (nextSegment == segment) continue;

            CreateSegmentBackdrop(segmentStart, level - 1, segment);
            segmentStart = level;
            segment = nextSegment;
        }

        CreateSegmentBackdrop(segmentStart, LevelManager.MAX_LEVELS, segment);
    }

    private void CreateSegmentBackdrop(int startLevel, int endLevel, AdventureSegmentConfig segment)
    {
        GameObject backdrop = new GameObject($"MapZone_{startLevel}_{endLevel}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = backdrop.GetComponent<RectTransform>();
        rect.SetParent(contentContainer, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -(verticalContentPadding + (startLevel - 1) * verticalSpacing));
        rect.sizeDelta = new Vector2(0f, (endLevel - startLevel + 1) * verticalSpacing);

        Color tint = segment != null ? segment.segmentColor : Color.white;
        Image image = backdrop.GetComponent<Image>();
        SagaMapSegmentArt art = GetSegmentArt(segment != null ? segment.themeName : string.Empty);
        image.sprite = art != null ? art.background : null;
        image.color = image.sprite != null ? Color.white : new Color(tint.r, tint.g, tint.b, 0.10f);
        image.preserveAspect = false;
        image.raycastTarget = false;
    }

    private void CreateSegmentLabels()
    {
        AdventureSegmentConfig segment = AdventureSegmentResolver.GetConfigForLevel(1);
        int segmentStart = 1;

        for (int level = 2; level <= LevelManager.MAX_LEVELS; level++)
        {
            AdventureSegmentConfig nextSegment = AdventureSegmentResolver.GetConfigForLevel(level);
            if (nextSegment == segment) continue;

            CreateSegmentLabel(segmentStart, level - 1, segment);
            segmentStart = level;
            segment = nextSegment;
        }

        CreateSegmentLabel(segmentStart, LevelManager.MAX_LEVELS, segment);
    }

    private void CreateSegmentLabel(int startLevel, int endLevel, AdventureSegmentConfig segment)
    {
        GameObject labelObject = new GameObject($"MapZoneLabel_{startLevel}", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        RectTransform rect = label.rectTransform;
        rect.SetParent(contentContainer, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -(verticalContentPadding + (startLevel - 1) * verticalSpacing + verticalSpacing * 0.5f - 100f));
        rect.sizeDelta = new Vector2(Mathf.Max(0f, contentContainer.rect.width - 32f), 40f);

        label.font = nodePrefab.levelNumberText.font;
        label.fontSize = 24f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.raycastTarget = false;
        label.color = new Color(1f, 0.96f, 0.78f, 1f);
        label.outlineColor = new Color(0.08f, 0.18f, 0.1f, 1f);
        label.outlineWidth = 0.15f;
        string themeName = segment != null ? segment.themeName : "Course";
        label.text = $"{themeName.ToUpperInvariant()}  ·  {startLevel}–{endLevel}";
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
