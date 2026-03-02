using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public class FeedbackManager : MonoBehaviour
{
    public static FeedbackManager Instance { get; private set; }

    [Header("UI References")]
    public TMP_Text feedbackText;
    public CanvasGroup feedbackCanvasGroup;

    [Header("Settings")]
    public float displayDuration = 1.5f;
    public float moveDistance = 50f;

    private readonly string[] goodShots = { "GOOD SHOT!", "NIICE!", "KEEP GOING!", "SOLID!", "GREAT!" };
    private readonly string[] amazingShots = { "AMAZING!", "PRO!", "INCREDIBLE!", "BEST SHOT!", "WOW!" };
    private readonly string[] genericVictory = { "BULLSEYE!", "PERFECT!", "VICTORY!", "GOAL!" };
    private readonly string holeInOneMessage = "HOLE IN ONE!";

    // Theme Colors: Vibrant Cyan and Vibrant Green
    private readonly Color[] feedbackColors = {
        new Color(0.1f, 0.8f, 1f), // Cyan Blue
        new Color(0.2f, 1f, 0.4f)  // Vibrant Green
    };

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (feedbackCanvasGroup != null) feedbackCanvasGroup.alpha = 0f;
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
    }

    public void ShowFeedback(Vector2Int targetPos, int distanceTraveled, bool reachedHole)
    {
        // BRAIN: Only show feedback if the move is on the winning (Golden) path!
        if (GridManager.Instance == null || GridManager.Instance.CurrentLevelData == null) return;
        
        var goldenPath = GridManager.Instance.CurrentLevelData.goldenPath;
        if (!goldenPath.Contains(targetPos)) return;

        if (feedbackText == null || feedbackCanvasGroup == null) return;

        string message = "";
        
        if (reachedHole)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentStrokes == 1)
                message = holeInOneMessage;
            else
                message = genericVictory[Random.Range(0, genericVictory.Length)];
        }
        else if (distanceTraveled >= 3)
        {
            message = amazingShots[Random.Range(0, amazingShots.Length)];
        }
        else
        {
            message = goodShots[Random.Range(0, goodShots.Length)];
        }

        // Randomize color from our theme palette
        feedbackText.color = feedbackColors[Random.Range(0, feedbackColors.Length)];

        feedbackText.text = message;
        PlayFeedbackSequence();
    }

    private void PlayFeedbackSequence()
    {
        // Kill any existing twins to reset
        feedbackCanvasGroup.DOKill();
        feedbackText.transform.DOKill();

        feedbackText.gameObject.SetActive(true);
        feedbackCanvasGroup.alpha = 0f;
        feedbackText.transform.localScale = Vector3.zero;
        
        // Reset position
        Vector3 originalPos = feedbackText.transform.localPosition;

        Sequence seq = DOTween.Sequence().SetUpdate(true);
        
        // 1. Pop & Fade In
        seq.Append(feedbackCanvasGroup.DOFade(1f, 0.2f));
        seq.Join(feedbackText.transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack));
        
        // 2. Slow Float Up
        seq.Append(feedbackText.transform.DOLocalMoveY(originalPos.y + moveDistance, displayDuration).SetEase(Ease.OutSine));
        
        // 3. Fade Out
        seq.Insert(displayDuration - 0.3f, feedbackCanvasGroup.DOFade(0f, 0.3f));
        
        seq.OnComplete(() => {
            feedbackText.gameObject.SetActive(false);
            feedbackText.transform.localPosition = originalPos;
        });
    }
}
