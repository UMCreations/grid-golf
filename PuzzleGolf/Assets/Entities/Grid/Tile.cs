using UnityEngine;
using TMPro;

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
}
