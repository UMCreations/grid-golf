using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class LevelCardController : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text levelNumberText;
    public Image backgroundImage;
    public Button cardButton;
    
    [Header("Icons")]
    public GameObject completedIcon;
    public GameObject lockedIcon;
    public GameObject playIcon;

    [Header("Colors (Matches Image)")]
    public Color completedColor = new Color(0.2f, 0.8f, 0.4f); // Green
    public Color nextLevelColor = new Color(0.4f, 0.6f, 0.8f); // Blue
    public Color lockedColor = new Color(0.2f, 0.2f, 0.3f);    // Dark grey

    private int myLevelIndex;
    private Difficulty myDifficulty;
    private LevelSelectionController parentController;
    private CommunityLevelEntry communityLevel;

    public void Setup(int levelIndex, Difficulty difficulty, LevelSelectionController parent)
    {
        myLevelIndex = levelIndex;
        myDifficulty = difficulty;
        parentController = parent;
        communityLevel = null;
        
        if (levelNumberText != null)
        {
            levelNumberText.text = levelIndex.ToString();
        }

        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(OnCardClicked);
        }

        RefreshVisuals();
    }

    public void SetupCommunity(CommunityLevelEntry level, LevelSelectionController parent)
    {
        communityLevel = level;
        parentController = parent;
        if (levelNumberText != null)
        {
            levelNumberText.text = level.name;
            levelNumberText.enableAutoSizing = true;
            levelNumberText.fontSizeMin = 12;
            levelNumberText.fontSizeMax = levelNumberText.fontSize;
        }
        SetCommunityVisuals(true);
    }

    public void SetupMessage(string message, Action onClick = null)
    {
        communityLevel = null;
        parentController = null;
        if (levelNumberText != null)
        {
            levelNumberText.text = message;
            levelNumberText.enableAutoSizing = true;
            levelNumberText.fontSizeMin = 12;
            levelNumberText.fontSizeMax = levelNumberText.fontSize;
        }
        SetCommunityVisuals(false);
        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            if (onClick != null) cardButton.onClick.AddListener(() => onClick());
            cardButton.interactable = onClick != null;
        }
    }

    private void SetCommunityVisuals(bool canPlay)
    {
        if (completedIcon != null) completedIcon.SetActive(false);
        if (lockedIcon != null) lockedIcon.SetActive(false);
        if (playIcon != null) playIcon.SetActive(canPlay);
        if (backgroundImage != null) backgroundImage.color = canPlay ? nextLevelColor : lockedColor;
        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(OnCardClicked);
            cardButton.interactable = canPlay;
        }
    }

    private void RefreshVisuals()
    {
        if (LevelManager.Instance == null) return;

        int unlockedUpTo = LevelManager.Instance.GetUnlockedLevelCount(myDifficulty);
        bool isLocked = myLevelIndex > unlockedUpTo;
        bool isCompleted = myLevelIndex < unlockedUpTo;
        bool isCurrentNextLevel = myLevelIndex == unlockedUpTo;

        if (lockedIcon != null) lockedIcon.SetActive(isLocked);
        if (completedIcon != null) completedIcon.SetActive(isCompleted);
        if (playIcon != null) playIcon.SetActive(isCurrentNextLevel);

        if (backgroundImage != null)
        {
            if (isLocked) backgroundImage.color = lockedColor;
            else if (isCompleted) backgroundImage.color = completedColor;
            else backgroundImage.color = nextLevelColor; 
        }

        if (cardButton != null)
        {
            cardButton.interactable = !isLocked;
        }
    }

    private void OnCardClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        if (communityLevel != null)
        {
            if (parentController != null) parentController.OnCommunityLevelSelected(communityLevel);
            return;
        }
        if (parentController != null)
        {
            parentController.OnLevelSelected(myDifficulty, myLevelIndex);
        }
    }
}
