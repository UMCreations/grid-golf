using UnityEngine;

[System.Serializable]
public struct PopupRequest
{
    public string title;
    public string message;
    public Sprite icon;
    public float displayDuration;

    public PopupRequest(string title, string message, Sprite icon, float displayDuration)
    {
        this.title = title;
        this.message = message;
        this.icon = icon;
        this.displayDuration = Mathf.Max(0f, displayDuration);
    }
}
