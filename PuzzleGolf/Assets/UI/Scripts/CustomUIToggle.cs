using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections;

public class CustomUIToggle : MonoBehaviour, IPointerClickHandler
{
    [Header("State")]
    public bool isOn = true;

    [Header("References")]
    [Tooltip("The moving circle inside the toggle.")]
    public RectTransform handle;
    [Tooltip("The background image of the toggle.")]
    public Image backgroundImage;

    [Header("Animation Settings")]
    public float animationDuration = 0.2f;

    [Header("Positions")]
    [Tooltip("Local position of the handle when ON.")]
    public Vector2 onPosition;
    [Tooltip("Local position of the handle when OFF.")]
    public Vector2 offPosition;

    [Header("Colors")]
    [Tooltip("Background color when ON.")]
    public Color onColor = new Color(0.4f, 0.7f, 0.9f); // Light blue
    [Tooltip("Background color when OFF.")]
    public Color offColor = new Color(0.3f, 0.3f, 0.3f); // Dark grey

    [Header("Events")]
    public UnityEvent<bool> onValueChanged;

    private Coroutine animationCoroutine;

    private void Start()
    {
        // Set initial state without animating or firing events
        UpdateVisuals(isOn, false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Toggle();
    }

    public void Toggle()
    {
        isOn = !isOn;
        UpdateVisuals(isOn, true);
        onValueChanged?.Invoke(isOn);
    }

    /// <summary>
    /// Programmatically set the toggle state without triggering the onValueChanged event.
    /// Useful for loading saved settings.
    /// </summary>
    public void SetIsOnWithoutNotify(bool value)
    {
        isOn = value;
        UpdateVisuals(isOn, false);
    }

    private void UpdateVisuals(bool state, bool animate)
    {
        if (handle == null || backgroundImage == null) return;

        Vector2 targetPos = state ? onPosition : offPosition;
        Color targetColor = state ? onColor : offColor;

        if (animate && gameObject.activeInHierarchy)
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }
            animationCoroutine = StartCoroutine(AnimateToggle(targetPos, targetColor));
        }
        else
        {
            handle.anchoredPosition = targetPos;
            backgroundImage.color = targetColor;
        }
    }

    private IEnumerator AnimateToggle(Vector2 targetPos, Color targetColor)
    {
        Vector2 startPos = handle.anchoredPosition;
        Color startColor = backgroundImage.color;
        
        float timeElapsed = 0f;

        while (timeElapsed < animationDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / animationDuration;
            
            // Smooth step for nicer easing
            float smoothT = t * t * (3f - 2f * t);

            handle.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);
            backgroundImage.color = Color.Lerp(startColor, targetColor, smoothT);

            yield return null;
        }

        handle.anchoredPosition = targetPos;
        backgroundImage.color = targetColor;
    }
}
