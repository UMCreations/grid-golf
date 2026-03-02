using UnityEngine;

/// <summary>
/// Modular interface for special tile effects in Adventure Mode.
/// Each tile type that has a special behavior implements this.
/// Classic mode tiles NEVER use this system.
/// </summary>
public interface ITileEffect
{
    /// <summary>
    /// Called when the ball lands on a tile that has this effect.
    /// Returns a modified power value for the NEXT move.
    /// </summary>
    /// <param name="basePower">The tile's base power count</param>
    /// <param name="direction">The direction the ball was traveling</param>
    /// <returns>Modified power for the next shot</returns>
    int ApplyEffect(int basePower, Vector2Int direction);

    /// <summary>
    /// If true, the ball should automatically slide again in the same direction.
    /// Used by Ice tiles.
    /// </summary>
    bool CausesAutoSlide { get; }

    /// <summary>
    /// The name of this effect (for debug/UI purposes).
    /// </summary>
    string EffectName { get; }
}
