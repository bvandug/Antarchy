using UnityEngine;
using UnityEngine.Tilemaps;

public class HexTileData
{
    public TileBase Tile { get; set; }
    public bool IsActivated { get; set; }
    public float FillLevel { get; set; } // Current fill level
    public float MaxFill { get;} // Max before giving resource

    public HexTileData(TileBase tile)
    {
        Tile = tile;
        MaxFill = 200;
        FillLevel = 0f;
        IsActivated = false;

    }
}

