using System.Collections.Generic;
using UnityEngine;

public class PopupQueueManager : MonoBehaviour
{
    public static PopupQueueManager Instance { get; private set; }

    [SerializeField] private QueuedPopupView popupPrefab;
    [SerializeField] private Transform popupParent;

    private readonly Queue<PopupRequest> requests = new Queue<PopupRequest>();
    private QueuedPopupView activePopup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (popupParent == null) popupParent = transform;
    }

    public void Enqueue(string title, string message, Sprite icon = null, float displayDuration = 2f)
    {
        requests.Enqueue(new PopupRequest(title, message, icon, displayDuration));
        ShowNext();
    }

    public void SetPopupPrefab(QueuedPopupView prefab)
    {
        popupPrefab = prefab;
        ShowNext();
    }

    public void ClearQueue()
    {
        requests.Clear();
        if (activePopup == null) return;

        activePopup.Cancel();
        Destroy(activePopup.gameObject);
        activePopup = null;
    }

    private void ShowNext()
    {
        if (activePopup != null || requests.Count == 0 || popupPrefab == null) return;

        PopupRequest request = requests.Dequeue();
        activePopup = Instantiate(popupPrefab, popupParent, false);
        activePopup.Show(request, OnPopupFinished);
    }

    private void OnPopupFinished()
    {
        if (activePopup != null)
        {
            Destroy(activePopup.gameObject);
            activePopup = null;
        }

        ShowNext();
    }

    private void OnDestroy()
    {
        ClearQueue();
        if (Instance == this) Instance = null;
    }
}
