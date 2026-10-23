using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class QueuedPopupView : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Transform animatedContent;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Image iconImage;
    [SerializeField, Min(0f)] private float enterDuration = 0.25f;
    [SerializeField, Min(0f)] private float exitDuration = 0.2f;

    private Sequence animation;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (animatedContent == null) animatedContent = transform;
    }

    public void Show(PopupRequest request, Action onFinished)
    {
        if (titleText != null)
        {
            titleText.text = request.title;
            titleText.gameObject.SetActive(!string.IsNullOrEmpty(request.title));
        }

        if (messageText != null)
        {
            messageText.text = request.message;
            messageText.gameObject.SetActive(!string.IsNullOrEmpty(request.message));
        }

        if (iconImage != null)
        {
            iconImage.sprite = request.icon;
            iconImage.gameObject.SetActive(request.icon != null);
        }

        Vector3 targetScale = animatedContent.localScale;
        canvasGroup.alpha = 0f;
        animatedContent.localScale = targetScale * 0.85f;

        animation = DOTween.Sequence().SetUpdate(true)
            .Append(canvasGroup.DOFade(1f, enterDuration))
            .Join(animatedContent.DOScale(targetScale, enterDuration).SetEase(Ease.OutBack))
            .AppendInterval(request.displayDuration)
            .Append(canvasGroup.DOFade(0f, exitDuration))
            .Join(animatedContent.DOScale(targetScale * 0.9f, exitDuration).SetEase(Ease.InSine))
            .OnComplete(() => onFinished?.Invoke());
    }

    public void Cancel()
    {
        animation?.Kill();
    }

    private void OnDestroy()
    {
        animation?.Kill();
    }
}
