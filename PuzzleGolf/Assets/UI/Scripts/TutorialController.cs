using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialController : MonoBehaviour
{
    public TMP_Text tutorialText;
    public Button nextButton;

    private int currentStep = 0;
    private string[] tutorialSteps = new string[]
    {
        "Welcome to Puzzle Golf!\nLet's learn the basics on this small 2x2 grid.",
        "Your goal is to reach the Hole (the black tile with the flag).",
        "The number on your current tile (the '1') shows exactly how many spaces you will move.",
        "Swipe towards the Hole to make your first move. Good luck!"
    };

    private void Start()
    {
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextClicked);
        }
    }

    private void OnDestroy()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(OnNextClicked);
        }
    }

    private void OnEnable()
    {
        currentStep = 0;
        UpdateTutorialText();
    }

    private void OnNextClicked()
    {
        currentStep++;
        if (currentStep < tutorialSteps.Length)
        {
            UpdateTutorialText();
        }
        else
        {
            CompleteTutorial();
        }
    }

    private void UpdateTutorialText()
    {
        if (tutorialText != null)
        {
            tutorialText.text = tutorialSteps[currentStep];
        }
    }

    private void CompleteTutorial()
    {
        gameObject.SetActive(false);
        
        if (LevelManager.Instance != null && LevelManager.Instance.CurrentProfile != null)
        {
            LevelManager.Instance.CurrentProfile.hasCompletedTutorial = true;
            LevelManager.Instance.IsTutorialMode = false; // Clear manual request flag
            LevelManager.Instance.SaveProfile();
        }
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameplayHUD();
        }
    }
}
