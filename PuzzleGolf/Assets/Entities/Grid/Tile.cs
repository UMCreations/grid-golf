using UnityEngine;
using TMPro;

public enum TileType
{
    Standard,
    Start,
    Hole,
    Wall,
    Water
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
        // We will update sprite and text based on type and powerCount here
        if (powerText != null)
        {
            if (type == TileType.Standard || type == TileType.Start)
            {
                powerText.text = powerCount > 0 ? powerCount.ToString() : "";
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
