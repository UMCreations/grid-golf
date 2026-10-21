using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelNodeController : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text levelNumberText;
    public Image backgroundImage;
    public Button nodeButton;
    public GameObject lockedIcon;
    public GameObject starContainer;
    public Image[] stars; // Array of 3 star images

    [Header("Colors / Visuals")]
    public Color lockedColor = Color.gray;
    public Color unlockedColor = Color.white;
    public Color completedColor = new Color(0.8f, 1f, 0.8f);

    private int levelIndex;
    private bool isLocked;

    public int LevelIndex { get { return levelIndex; } }

    public void Setup(int level, bool locked, int starsEarned, Color themeColor, bool isCurrentLevel = false)
    {
        levelIndex = level;
        isLocked = locked;

        if (levelNumberText != null)
            levelNumberText.text = level.ToString();

        // Update visuals based on state
        if (lockedIcon != null) lockedIcon.SetActive(isLocked);
        if (starContainer != null) starContainer.SetActive(!isLocked);

        if (backgroundImage != null)
        {
            if (isLocked)
                backgroundImage.color = lockedColor;
            else
                backgroundImage.color = isCurrentLevel ? Color.Lerp(themeColor, Color.white, 0.2f) : themeColor;
        }

        RectTransform nodeRect = GetComponent<RectTransform>();
        if (nodeRect != null)
            nodeRect.localScale = isCurrentLevel ? Vector3.one * 1.12f : Vector3.one;

        if (stars != null)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] != null)
                    stars[i].color = !isLocked && i < starsEarned
                        ? Color.white
                        : new Color(0.45f, 0.48f, 0.42f, 0.6f);
            }
        }

        if (nodeButton != null)
        {
            nodeButton.interactable = !isLocked;
            nodeButton.onClick.RemoveAllListeners();
            if (!isLocked)
            {
                nodeButton.onClick.AddListener(OnNodeClicked);
            }
        }
    }

    private void OnNodeClicked()
    {
        Debug.Log($"Adventure Level {levelIndex} selected!");
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.SetAdventureLevelToPlay(levelIndex);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameplayHUD();
        }

        if (GridManager.Instance != null && LevelManager.Instance != null)
        {
            SaveManager.ClearSave();
            GridManager.Instance.GenerateAndLoadNewLevel(LevelManager.Instance.SelectedDifficulty, levelIndex);
        }
    }
}
