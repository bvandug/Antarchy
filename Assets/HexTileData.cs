using UnityEngine;
using UnityEngine.Tilemaps;

public class HexTileData
{
    public TileBase Tile { get; set; }
    public bool IsActivated { get; set; }
    public float FillLevel { get; set; } // Current fill level
    public float MaxFill { get;} // Max before giving resource
    public bool IsDisabled{get; set;}

    public HexTileData(TileBase tile)
    {
        Tile = tile;
        MaxFill = 100;
        FillLevel = 0f;
        IsActivated = false;
        IsDisabled = false;
        

    }
}

