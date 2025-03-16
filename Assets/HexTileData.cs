using UnityEngine;
using UnityEngine.Tilemaps;

public class HexTileData
{
    public TileBase Tile { get; set; }
    public bool IsActivated { get; set; }

    public HexTileData(TileBase tile)
    {
        Tile = tile;
        IsActivated = false;
    }
}
