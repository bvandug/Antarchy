using UnityEngine;
using UnityEngine.Tilemaps;

public class HexTilemapGenerator : MonoBehaviour
{
    private int width = 30;
    private int height = 300;
    public Tilemap tilemap;
    public TileBase dirtTile, waterTile, stoneTile, foodTile, mineTile;

    public float noiseScale = 0.3f; // Lower for bigger clusters
    public float stoneNoiseScale = 0.15f; // Stone uses separate noise for better clustering
    public float waterThreshold = 0.6f;
    public float stoneThreshold = 0.3f;
    private int seed;

    void Start()
    {
        GenerateMap();
    }

    private void Update()
    {
        MineBlock();
    }

    void GenerateMap()
    {
        seed = Random.Range(0, 10000);
        tilemap.ClearAllTiles();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float baseNoise = Mathf.PerlinNoise((x + seed) * noiseScale, (y + seed) * noiseScale);
                float stoneNoise = Mathf.PerlinNoise((x + seed + 500) * stoneNoiseScale, (y + seed + 500) * stoneNoiseScale);

                Vector3Int tilePosition = new Vector3Int(x, -y, 0);

                if (baseNoise > waterThreshold)
                {
                    tilemap.SetTile(tilePosition, waterTile);
                }
                else if (stoneNoise > stoneThreshold) 
                {
                    tilemap.SetTile(tilePosition, stoneTile);
                }
                else
                {
                    tilemap.SetTile(tilePosition, dirtTile);
                }
            }
        }

        EnsureFoodPlacement();
    }

    void EnsureFoodPlacement()
    {
        for (int y = 0; y < height; y += 5)
        {
            int randomX = Random.Range(0, width);
            Vector3Int tilePosition = new Vector3Int(randomX, -y, 0);
            tilemap.SetTile(tilePosition, foodTile);
        }
    }

    void MineBlock()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Raycast from camera to the tilemap plane (assuming Z = 0)
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane tilemapPlane = new Plane(Vector3.forward, Vector3.zero); // Adjust if needed

            if (tilemapPlane.Raycast(ray, out float distance))
            {
                Vector3 mousePosWorld = ray.GetPoint(distance); // Get the intersection point
                Vector3Int mouseCell = tilemap.WorldToCell(mousePosWorld);

                // Check if there is a tile at the clicked position
                if (tilemap.HasTile(mouseCell))
                {
                    Debug.Log($"Mining tile at: {mouseCell} (World Pos: {mousePosWorld})");
                    tilemap.SetTile(mouseCell, mineTile); // Remove the tile
                }
                else
                {
                    Debug.Log("No tile to mine at this position.");
                }
            }
        }
    }


}
