using UnityEngine;
using DG.Tweening;
using TMPro;

public static class UIAnimationHelper
{
    public static Tween DOTextTypewriter(TMP_Text textElement, string fullText, float durationPerChar = 0.02f)
    {
        if (textElement == null) return null;
        
        textElement.text = "";
        return DOTween.To(() => textElement.text, x => textElement.text = x, fullText, fullText.Length * durationPerChar)
            .SetOptions(true)
            .SetEase(Ease.Linear)
            .SetUpdate(true);
    }

    public static Tween DOTextWordByWord(TMP_Text textElement, string fullText, float durationPerWord = 0.1f)
    {
        if (textElement == null) return null;

        string[] words = fullText.Split(' ');
        int wordCount = words.Length;
        int currentWordIndex = 0;

        return DOTween.To(() => currentWordIndex, x => {
            currentWordIndex = x;
            string currentProgress = "";
            for (int i = 0; i <= currentWordIndex && i < wordCount; i++)
            {
                currentProgress += words[i] + (i < currentWordIndex ? " " : "");
            }
            textElement.text = currentProgress;
        }, wordCount - 1, wordCount * durationPerWord).SetEase(Ease.Linear).SetUpdate(true);
    }

    public static void PopIn(GameObject obj, float duration = 0.3f, float startScale = 0f, float endScale = 1f, Ease ease = Ease.OutBack)
    {
        if (obj == null) return;
        
        obj.transform.localScale = Vector3.one * startScale;
        obj.SetActive(true);
        obj.transform.DOScale(endScale, duration).SetEase(ease).SetUpdate(true);
    }

    public static void PopOut(GameObject obj, float duration = 0.2f, float endScale = 0f, Ease ease = Ease.InBack, System.Action onComplete = null)
    {
        if (obj == null) return;

        obj.transform.DOScale(endScale, duration).SetEase(ease).SetUpdate(true).OnComplete(() =>
        {
            obj.SetActive(false);
            onComplete?.Invoke();
        });
    }

    public static void FadeIn(CanvasGroup canvasGroup, float duration = 0.3f, float endAlpha = 1f, Ease ease = Ease.Linear)
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(true);
        canvasGroup.DOFade(endAlpha, duration).SetEase(ease).SetUpdate(true);
    }

    public static void FadeOut(CanvasGroup canvasGroup, float duration = 0.3f, float endAlpha = 0f, Ease ease = Ease.Linear, System.Action onComplete = null)
    {
        if (canvasGroup == null) return;

        canvasGroup.DOFade(endAlpha, duration).SetEase(ease).SetUpdate(true).OnComplete(() =>
        {
            canvasGroup.gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }

    public static void SlideIn(RectTransform rectTransform, Vector2 startAnchoredPosition, float duration = 0.5f, Ease ease = Ease.OutQuint)
    {
        if (rectTransform == null) return;

        Vector2 targetPosition = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = startAnchoredPosition;
        rectTransform.DOAnchorPos(targetPosition, duration).SetEase(ease).SetUpdate(true);
    }

    public static void SlideOut(RectTransform rectTransform, Vector2 targetAnchoredPosition, float duration = 0.5f, Ease ease = Ease.InQuint, System.Action onComplete = null)
    {
        if (rectTransform == null) return;

        rectTransform.DOAnchorPos(targetAnchoredPosition, duration).SetEase(ease).SetUpdate(true).OnComplete(() =>
        {
            onComplete?.Invoke();
        });
    }
}
