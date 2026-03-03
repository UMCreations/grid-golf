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

    public void Setup(int level, bool locked, int starsEarned)
    {
        levelIndex = level;
        isLocked = locked;

        if (levelNumberText != null)
            levelNumberText.text = level.ToString();

        // Update visuals based on state
        if (lockedIcon != null) lockedIcon.SetActive(isLocked);
        if (starContainer != null) starContainer.SetActive(!isLocked && starsEarned > 0);

        if (backgroundImage != null)
        {
            if (isLocked)
                backgroundImage.color = lockedColor;
            else if (starsEarned > 0)
                backgroundImage.color = completedColor;
            else
                backgroundImage.color = unlockedColor;
        }

        // Update Stars
        if (!isLocked && starsEarned > 0)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                // Simple logic: if i < starsEarned, star is 'on' (white/yellow), else 'off' (dark/transparent)
                // Assuming the sprite itself is a white star, we just change color alpha.
                if (stars[i] != null)
                {
                    Color c = stars[i].color;
                    c.a = (i < starsEarned) ? 1.0f : 0.3f;
                    stars[i].color = c;
                }
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
