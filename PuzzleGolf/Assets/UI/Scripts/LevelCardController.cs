using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    public void Setup(int levelIndex, Difficulty difficulty, LevelSelectionController parent)
    {
        myLevelIndex = levelIndex;
        myDifficulty = difficulty;
        parentController = parent;
        
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
        if (parentController != null)
        {
            parentController.OnLevelSelected(myDifficulty, myLevelIndex);
        }
    }
}
