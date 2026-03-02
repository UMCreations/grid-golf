using UnityEngine;

/// <summary>
/// Central resolver that maps a TileType to its ITileEffect implementation.
/// Only used in Adventure Mode. Classic mode calls this and gets null back,
/// so zero classic behavior is changed.
/// </summary>
public static class TileEffectResolver
{
    /// <summary>
    /// Returns the ITileEffect for a given tile type, or null if this tile
    /// has no special effect (Standard, Start, Hole, Wall, Water, etc.)
    /// </summary>
    public static ITileEffect GetEffect(TileType tileType)
    {
        switch (tileType)
        {
            case TileType.Ice:   return new IceTileEffect();
            case TileType.Sand:  return new SandTileEffect();
            case TileType.Boost: return new BoostTileEffect();
            default:             return null; // classic tiles have no effect
        }
    }

    /// <summary>
    /// True if this tile type is exclusive to Adventure Mode.
    /// Used by the generator to know which types it can place.
    /// </summary>
    public static bool IsAdventureTile(TileType tileType)
    {
        return tileType == TileType.Ice ||
               tileType == TileType.Sand ||
               tileType == TileType.Boost;
    }
}
