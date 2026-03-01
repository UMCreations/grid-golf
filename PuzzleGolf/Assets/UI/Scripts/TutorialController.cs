using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

[System.Serializable]
public class TutorialStep
{
    public string title;
    [TextArea(2, 5)]
    public string message;
    public Sprite characterSprite; // Optional character expression
}

public class TutorialController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text titleText;
    public TMP_Text tutorialText;
    public Image characterImage;
    public Button nextButton;
    public GameObject CharBox;

    [Header("Animations")]
    public float popDuration = 0.3f;
    public float popScale = 1.1f;
    public float textAnimDurationPerWord = 0.1f;
    private Tween textTween;

    [Header("Tutorial Data")]
    public TutorialStep[] tutorialSteps;
    private int currentStep = 0;

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
        
        if (textTween != null)
            textTween.Kill();
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
        if (tutorialSteps != null && currentStep < tutorialSteps.Length)
        {
            TutorialStep step = tutorialSteps[currentStep];

            if (titleText != null)
                titleText.text = step.title;

            if (tutorialText != null)
            {
                if (textTween != null) textTween.Kill();
                textTween = UIAnimationHelper.DOTextWordByWord(tutorialText, step.message, textAnimDurationPerWord);
            }

            if (characterImage != null)
            {
                if (step.characterSprite != null)
                {
                    characterImage.sprite = step.characterSprite;
                    characterImage.enabled = true;
                }
                else
                {
                    characterImage.enabled = false;
                }
            }

            // Animate CharBox when step changes
            if (CharBox != null)
            {
                UIAnimationHelper.PopIn(CharBox, popDuration, 0.8f, 1f);
            }
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
