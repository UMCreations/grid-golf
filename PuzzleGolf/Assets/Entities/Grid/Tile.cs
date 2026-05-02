using UnityEngine;
using TMPro;
using DG.Tweening;

public enum TileType
{
    Standard,
    Start,
    Hole,
    Wall,
    Water,
    Ice,    // Adventure: auto-slides in same direction
    Sand,   // Adventure: reduces next shot power by 1
    Boost   // Adventure: increases next shot power by 1
}

public class Tile : MonoBehaviour
{
    [Header("Tile Data")]
    public Vector2Int gridPosition;
    public int powerCount;
    public TileType type;

    [Header("Visuals (Optional)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private GameObject highlightBorder;

    public void Init(Vector2Int gridPos, int power, TileType tileType, Sprite tileSprite = null)
    {
        gridPosition = gridPos;
        powerCount = power;
        type = tileType;

        if (spriteRenderer != null && tileSprite != null)
        {
            spriteRenderer.sprite = tileSprite;
        }

        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (powerText != null)
        {
            bool shouldShowPower = (type == TileType.Standard || type == TileType.Start ||
                                   type == TileType.Ice || type == TileType.Sand ||
                                   type == TileType.Boost);

            if (shouldShowPower && powerCount > 0)
            {
                powerText.text = powerCount.ToString();

                // Color-code power number by tile type
                if      (type == TileType.Ice)   powerText.color = new Color(0.6f, 0.85f, 1f);   // Ice Blue
                else if (type == TileType.Sand)  powerText.color = new Color(1f,  0.9f,  0.55f); // Sand Yellow
                else if (type == TileType.Boost) powerText.color = new Color(0.4f, 1f,   0.6f);  // Boost Green
                else                             powerText.color = Color.white;
            }
            else
            {
                powerText.text = "";
            }
        }
    }

    public void SetHighlight(bool isHighlighted)
    {
        if (highlightBorder != null)
        {
            highlightBorder.SetActive(isHighlighted);
        }
        else if (spriteRenderer != null)
        {
            // Fallback if highlightBorder is not assigned yet
            spriteRenderer.color = isHighlighted ? new Color(0.6f, 1f, 0.6f, 1f) : Color.white;
        }
    }

    public void SetPulse(bool shouldPulse)
    {
        transform.DOKill();
        if (shouldPulse)
        {
            transform.DOScale(1.2f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
        else
        {
            // Immediate reset followed by short smoothing to ensure it stops exactly at 1.0
            transform.localScale = Vector3.one; 
        }
    }

    public void SetPowerHighlight(bool isHighlighted)
    {
        if (powerText != null)
        {
            powerText.transform.DOKill(); // Kill transform tweens
            powerText.DOKill();           // Kill text-specific tweens
            
            if (isHighlighted)
            {
                powerText.color = Color.yellow;
                powerText.transform.DOScale(1.5f, 0.4f).SetLoops(-1, LoopType.Yoyo);
            }
            else
            {
                UpdateVisuals(); // Reverts to default color
                powerText.transform.localScale = Vector3.one;
            }
        }
    }
    public void AnimateSpawn(float delay)
    {
        // 1. Initial State (Hidden)
        transform.localScale = Vector3.zero;
        if (powerText != null)
        {
            Color c = powerText.color;
            c.a = 0f;
            powerText.color = c;
        }

        // 2. Tile Pop Animation
        transform.DOScale(1.0f, 0.5f)
            .SetDelay(delay)
            .SetEase(Ease.OutBack)
            .OnStart(() => {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayTilePlaceSound();
                }
            });

        // 3. Number Fade-in Animation (slightly delayed after the pop)
        if (powerText != null)
        {
            powerText.DOFade(1f, 0.5f).SetDelay(delay + 0.3f);
        }
    }
}
