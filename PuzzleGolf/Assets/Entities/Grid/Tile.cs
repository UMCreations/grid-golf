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

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TMP_Text powerText;

    public void Init(Vector2Int gridPos, int power, TileType tileType)
    {
        gridPosition = gridPos;
        powerCount = power;
        type = tileType;

        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        // We will update sprite and text based on type and powerCount here
        if (powerText != null)
        {
            if (type == TileType.Standard)
            {
                powerText.text = powerCount > 0 ? powerCount.ToString() : "";
            }
            else if (type == TileType.Start)
            {
                powerText.text = "S";
            }
            else if (type == TileType.Hole)
            {
                powerText.text = "H";
            }
            else
            {
                powerText.text = "";
            }
        }
    }
}
