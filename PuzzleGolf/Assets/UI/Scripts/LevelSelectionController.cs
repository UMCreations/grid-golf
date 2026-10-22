using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;

[Serializable]
public class CommunityLevelCatalog
{
    public int catalogVersion;
    public CommunityLevelEntry[] levels;
}

[Serializable]
public class CommunityLevelEntry
{
    public string id;
    public string name;
    public string mode;
    public string difficulty;
    public int levelPar;
    public string path;
}

public class LevelSelectionController : MonoBehaviour
{
    private const string CatalogUrl = "https://raw.githubusercontent.com/UMCreations/grid-golf-levels/main/catalog.json";
    private const string PublishedContentBaseUrl = "https://raw.githubusercontent.com/UMCreations/grid-golf-levels/main/";
    private const string CatalogPathPattern = @"\Alevels/published/(classic|adventure)/[a-z0-9][a-z0-9-]*\.json\z";

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
    private Button communityTabBtn;
    private bool showingCommunityLevels;
    private bool communityCatalogLoaded;
    private CommunityLevelEntry[] communityLevels = Array.Empty<CommunityLevelEntry>();
    private Coroutine communityRequest;
    private Coroutine communityLevelRequest;

    // Cache to avoid destroying/instantiating 100 prefabs over and over
    private readonly List<LevelCardController> pooledCards = new List<LevelCardController>();

    private void Start()
    {
        if (easyTabBtn != null) easyTabBtn.onClick.AddListener(() => { if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick(); ChangeTab(Difficulty.Easy); });
        if (mediumTabBtn != null) mediumTabBtn.onClick.AddListener(() => { if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick(); ChangeTab(Difficulty.Medium); });
        if (hardTabBtn != null) hardTabBtn.onClick.AddListener(() => { if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick(); ChangeTab(Difficulty.Hard); });
        CreateCommunityTab();
        
        if (backButton != null) backButton.onClick.AddListener(OnBackClicked);

        // Default initialize standard View
        ChangeTab(currentDifficultyTab);
    }

    private void OnEnable()
    {
        if (pooledCards != null)
        {
            if (showingCommunityLevels) ShowCommunityTab();
            else ChangeTab(currentDifficultyTab);
        }
    }

    private void CreateCommunityTab()
    {
        if (hardTabBtn == null) return;
        Canvas.ForceUpdateCanvases();
        communityTabBtn = Instantiate(hardTabBtn, hardTabBtn.transform.parent);
        communityTabBtn.name = "CommunityTab";
        communityTabBtn.onClick.RemoveAllListeners();
        communityTabBtn.onClick.AddListener(() =>
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            ShowCommunityTab();
        });

        TMP_Text label = communityTabBtn.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = "COMMUNITY";
            label.enableAutoSizing = true;
            label.fontSizeMin = 16;
            label.fontSizeMax = label.fontSize;
        }

        RectTransform parent = communityTabBtn.transform.parent as RectTransform;
        HorizontalLayoutGroup layout = communityTabBtn.transform.parent.GetComponent<HorizontalLayoutGroup>();
        if (parent != null && layout != null)
        {
            float tabWidth = Mathf.Max(72f, (parent.rect.width - layout.spacing * 3) / 4f);
            foreach (Button tab in new[] { easyTabBtn, mediumTabBtn, hardTabBtn, communityTabBtn })
            {
                if (tab == null) continue;
                RectTransform rect = tab.transform as RectTransform;
                if (rect != null) rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tabWidth);
                TMP_Text tabLabel = tab.GetComponentInChildren<TMP_Text>(true);
                if (tabLabel != null)
                {
                    tabLabel.enableAutoSizing = true;
                    tabLabel.fontSizeMin = 16;
                    tabLabel.fontSizeMax = tabLabel.fontSize;
                }
            }
        }
    }

    private void ChangeTab(Difficulty diff)
    {
        showingCommunityLevels = false;
        if (communityRequest != null)
        {
            StopCoroutine(communityRequest);
            communityRequest = null;
        }
        if (communityLevelRequest != null)
        {
            StopCoroutine(communityLevelRequest);
            communityLevelRequest = null;
        }
        currentDifficultyTab = diff;
        
        // Highlight active tab visually
        HighlightTab(easyTabBtn, diff == Difficulty.Easy);
        HighlightTab(mediumTabBtn, diff == Difficulty.Medium);
        HighlightTab(hardTabBtn, diff == Difficulty.Hard);
        HighlightTab(communityTabBtn, false);

        PopulateGrid();
    }

    private void ShowCommunityTab()
    {
        showingCommunityLevels = true;
        HighlightTab(easyTabBtn, false);
        HighlightTab(mediumTabBtn, false);
        HighlightTab(hardTabBtn, false);
        HighlightTab(communityTabBtn, true);

        if (communityCatalogLoaded)
        {
            PopulateCommunityGrid();
            return;
        }

        ShowCommunityMessage("Loading levels...");
        if (communityRequest == null) communityRequest = StartCoroutine(LoadCommunityCatalog());
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
            LevelCardController card = GetPooledCard(i);
            card.gameObject.SetActive(true);
            card.Setup(levelNum, currentDifficultyTab, this);
        }
        HideCardsFrom(LevelManager.MAX_LEVELS);
    }

    private IEnumerator LoadCommunityCatalog()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(CatalogUrl))
        {
            request.timeout = 20;
            yield return request.SendWebRequest();
            if (!showingCommunityLevels) yield break;
            if (request.result != UnityWebRequest.Result.Success)
            {
                communityRequest = null;
                ShowCommunityMessage("Could not load levels. Tap Community to retry.");
                Debug.LogWarning($"Community catalog request failed: {request.error}");
                yield break;
            }

            try
            {
                CommunityLevelCatalog catalog = JsonUtility.FromJson<CommunityLevelCatalog>(request.downloadHandler.text);
                if (catalog == null || catalog.catalogVersion != 1 || catalog.levels == null)
                    throw new InvalidOperationException("Catalog format is not supported.");

                var validLevels = new List<CommunityLevelEntry>();
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (CommunityLevelEntry level in catalog.levels)
                {
                    if (IsValidCatalogEntry(level) && ids.Add(level.id)) validLevels.Add(level);
                    else Debug.LogWarning("Ignoring invalid or duplicate community catalog entry.");
                }

                communityLevels = validLevels.ToArray();
                communityCatalogLoaded = true;
                PopulateCommunityGrid();
            }
            catch (Exception error)
            {
                communityRequest = null;
                ShowCommunityMessage("Could not read level catalog. Tap Community to retry.");
                Debug.LogWarning($"Community catalog is invalid: {error.Message}");
                yield break;
            }
        }
        communityRequest = null;
    }

    private bool IsValidCatalogEntry(CommunityLevelEntry level)
    {
        if (level == null || string.IsNullOrWhiteSpace(level.id) || string.IsNullOrWhiteSpace(level.name)) return false;
        if (!Regex.IsMatch(level.path ?? string.Empty, CatalogPathPattern) || level.levelPar < 1) return false;
        if (!LevelAuthoringValidator.TryParseMode(level.mode, out GameMode mode) ||
            !LevelAuthoringValidator.TryParseDifficulty(level.difficulty, out _)) return false;
        return (mode == GameMode.Classic && level.path.StartsWith("levels/published/classic/", StringComparison.Ordinal)) ||
               (mode == GameMode.Adventure && level.path.StartsWith("levels/published/adventure/", StringComparison.Ordinal));
    }

    private void PopulateCommunityGrid()
    {
        if (levelCardPrefab == null || gridContentParent == null) return;
        if (communityLevels.Length == 0)
        {
            ShowCommunityMessage("No published levels yet.");
            return;
        }

        for (int i = 0; i < communityLevels.Length; i++)
        {
            LevelCardController card = GetPooledCard(i);
            card.gameObject.SetActive(true);
            card.SetupCommunity(communityLevels[i], this);
        }
        HideCardsFrom(communityLevels.Length);
    }

    private void ShowCommunityMessage(string message, Action onClick = null)
    {
        if (levelCardPrefab == null || gridContentParent == null) return;
        LevelCardController card = GetPooledCard(0);
        card.gameObject.SetActive(true);
        card.SetupMessage(message, onClick);
        HideCardsFrom(1);
    }

    private LevelCardController GetPooledCard(int index)
    {
        while (pooledCards.Count <= index)
            pooledCards.Add(Instantiate(levelCardPrefab, gridContentParent));
        return pooledCards[index];
    }

    private void HideCardsFrom(int index)
    {
        for (int i = index; i < pooledCards.Count; i++)
            if (pooledCards[i] != null) pooledCards[i].gameObject.SetActive(false);
    }

    public void OnCommunityLevelSelected(CommunityLevelEntry entry)
    {
        if (communityLevelRequest != null) StopCoroutine(communityLevelRequest);
        communityLevelRequest = StartCoroutine(LoadCommunityLevel(entry));
    }

    private IEnumerator LoadCommunityLevel(CommunityLevelEntry entry)
    {
        ShowCommunityMessage("Loading " + entry.name + "...");
        string url = PublishedContentBaseUrl + entry.path;
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 20;
            yield return request.SendWebRequest();
            if (!showingCommunityLevels) yield break;
            if (request.result != UnityWebRequest.Result.Success)
            {
                ShowCommunityMessage("Could not load level. Tap to retry.", () => OnCommunityLevelSelected(entry));
                Debug.LogWarning($"Community level request failed: {request.error}");
                communityLevelRequest = null;
                yield break;
            }

            try
            {
                LevelAuthoringDto dto = JsonUtility.FromJson<LevelAuthoringDto>(request.downloadHandler.text);
                LevelAuthoringValidationResult validation = LevelAuthoringValidator.Validate(dto);
                if (!validation.IsValid || dto.id != entry.id || dto.name != entry.name || dto.mode != entry.mode ||
                    dto.difficulty != entry.difficulty || dto.levelPar != entry.levelPar)
                    throw new InvalidOperationException(validation.IsValid ? "Level does not match catalog metadata." : string.Join(" ", validation.Errors));

                LevelData level = LevelAuthoringConverter.ToCommunityLevelData(dto);
                SolveResult solve = PuzzleSolver.Solve(level);
                if (!solve.IsSolvable) throw new InvalidOperationException("Level has no solution.");
                level.goldenPath = solve.Path;

                if (LevelManager.Instance == null || UIManager.Instance == null || GridManager.Instance == null)
                    throw new InvalidOperationException("Game scene is not ready.");

                LevelManager.Instance.SetCommunityLevelToPlay(level);
                GridManager.Instance.InitializeGame();
                UIManager.Instance.ShowGameplayHUD();
            }
            catch (Exception error)
            {
                ShowCommunityMessage("Level is invalid. Tap to return to levels.", PopulateCommunityGrid);
                Debug.LogWarning($"Community level rejected: {error.Message}");
            }
        }
        communityLevelRequest = null;
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
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMainMenu();
        }
    }
}
