using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public enum TutorialActionType
{
    None,            // Advances with "Next" button
    MoveToPosition,  // Advances when ball reaches specific grid pos
    MoveToAnyTile
}

public enum TutorialVisualBehavior
{
    None,
    HighlightHole,
    HighlightTargetTile,
    HighlightAllTileNumbers
}

[System.Serializable]
public class TutorialStep
{
    public string title;
    [TextArea(2, 5)]
    public string message;
    public Sprite characterSprite; // Optional character expression
    
    [Header("Action Requirements")]
    public TutorialActionType actionType = TutorialActionType.None;
    public Vector2Int requiredTargetPosition; // For MoveToPosition
    
    [Header("Visual Behavior")]
    public TutorialVisualBehavior visualBehavior = TutorialVisualBehavior.None;
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
    private bool isTyping = false;
    
    public bool IsTyping => isTyping;

    [Header("Tutorial Data")]
    public TutorialStep[] tutorialSteps;
    private int currentStep = 0;

    public int CurrentStepIndex => currentStep;
    public TutorialStep CurrentStepData => (tutorialSteps != null && currentStep < tutorialSteps.Length) ? tutorialSteps[currentStep] : null;

    private void Start()
    {
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(NextStep);
        }
    }

    private void OnDestroy()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(NextStep);
        }
        
        if (textTween != null)
            textTween.Kill();
    }

    public void NextStep()
    {
        // Don't allow manual "Next" if the current step requires a specific gameplay action
        if (currentStep < tutorialSteps.Length && tutorialSteps[currentStep].actionType != TutorialActionType.None)
            return;

        // If still typing, skip to the end of the current message first
        if (isTyping)
        {
            if (textTween != null)
            {
                textTween.Complete(); // This triggers OnComplete and sets isTyping = false
            }
            return;
        }

        AdvanceTutorial();
    }

    private void OnEnable()
    {
        currentStep = 0;
        UpdateTutorialText();
    }


    public void OnActionPerformed(TutorialActionType action, Vector2Int position = default)
    {
        if (currentStep >= tutorialSteps.Length) return;

        TutorialStep step = tutorialSteps[currentStep];
        
        // If the action performed was a move (MoveToPosition), 
        // it can satisfy either a specific MoveToPosition step or a general MoveToAnyTile step.
        if (action == TutorialActionType.MoveToPosition)
        {
            if (step.actionType == TutorialActionType.MoveToPosition)
            {
                if (position == step.requiredTargetPosition)
                {
                    AdvanceTutorial();
                }
            }
            else if (step.actionType == TutorialActionType.MoveToAnyTile)
            {
                AdvanceTutorial();
            }
        }
        else if (step.actionType == action)
        {
            AdvanceTutorial();
        }
    }

    private void AdvanceTutorial()
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
                isTyping = true;
                textTween = UIAnimationHelper.DOTextWordByWord(tutorialText, step.message, textAnimDurationPerWord)
                    .OnComplete(() => isTyping = false);
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

            // Show/Hide Next button based on whether an action is required
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(step.actionType == TutorialActionType.None);
            }

            // Animate CharBox when step changes
            if (CharBox != null)
            {
                UIAnimationHelper.PopIn(CharBox, popDuration, 0.8f, 1f);
            }

            // Apply Visual Behaviors
            ApplyVisualBehaviors(step);
        }
    }

    private void ApplyVisualBehaviors(TutorialStep step)
    {
        if (GridManager.Instance == null) return;

        // Reset all special highlights first
        GridManager.Instance.ClearTutorialVisuals();

        switch (step.visualBehavior)
        {
            case TutorialVisualBehavior.HighlightHole:
                GridManager.Instance.HighlightHoleForTutorial(true);
                break;
            case TutorialVisualBehavior.HighlightTargetTile:
                if (step.actionType == TutorialActionType.MoveToPosition)
                {
                    GridManager.Instance.HighlightTileForTutorial(step.requiredTargetPosition, true);
                }
                break;
            case TutorialVisualBehavior.HighlightAllTileNumbers:
                GridManager.Instance.HighlightAllPowerNumbers(true);
                break;
        }
    }

    private void CompleteTutorial()
    {
        // Clear any leftover tutorial highlights before exiting
        if (GridManager.Instance != null)
        {
            GridManager.Instance.ClearTutorialVisuals();
        }

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
