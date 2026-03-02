using UnityEngine;

/// <summary>
/// ICE TILE: When the ball lands here, it automatically slides one
/// more step in the same direction (if the target tile is valid).
/// Gives a fast, slippery feeling — player must plan ahead.
/// </summary>
public class IceTileEffect : ITileEffect
{
    public string EffectName => "Ice";
    public bool CausesAutoSlide => true;

    public int ApplyEffect(int basePower, Vector2Int direction)
    {
        // Ice doesn't change power — it just triggers an auto-slide.
        // The auto-slide is handled by CausesAutoSlide = true in BallController.
        return basePower;
    }
}

/// <summary>
/// SAND TILE: Landing here reduces the ball's next shot power by 1.
/// Minimum power is 1 so the ball is never fully stuck.
/// Simulates a "soft ground" slowdown mechanic.
/// </summary>
public class SandTileEffect : ITileEffect
{
    public string EffectName => "Sand";
    public bool CausesAutoSlide => false;

    public int ApplyEffect(int basePower, Vector2Int direction)
    {
        // Reduce next power, never below 1
        return Mathf.Max(1, basePower - 1);
    }
}

/// <summary>
/// BOOST TILE: Landing here increases the ball's next shot power by 1.
/// Creates exciting long-range shot opportunities.
/// Simulates a launch pad or firm green surface.
/// </summary>
public class BoostTileEffect : ITileEffect
{
    public string EffectName => "Boost";
    public bool CausesAutoSlide => false;

    public int ApplyEffect(int basePower, Vector2Int direction)
    {
        // Add 1 to next power
        return basePower + 1;
    }
}
