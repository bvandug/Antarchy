using UnityEngine;
using UnityEngine.Tilemaps;

public class HexTilemapGenerator : MonoBehaviour
{
    public int width = 30;
    public int height = 50;
    public Tilemap tilemap;
    public TileBase dirtTile, waterTile, stoneTile, foodTile;

    public float noiseScale = 0.5f; // Controls clustering
    public float waterThreshold = 0.6f;
    public float stoneThreshold = 0.2f; // Lower stone chance
    private int seed;

    void Start()
    {
        seed = Random.Range(0, 10000);
        GenerateMap();
    }

    void GenerateMap()
    {
        tilemap.ClearAllTiles();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Generate base Perlin noise
                float noiseValue = Mathf.PerlinNoise((x + seed) * noiseScale, (y + seed) * noiseScale);

                // Introduce slight warping for better clustering
                float warpX = Mathf.PerlinNoise((x + seed + 100) * 0.05f, (y + seed) * 0.05f) * 2 - 1;
                float warpY = Mathf.PerlinNoise((x + seed) * 0.05f, (y + seed + 100) * 0.05f) * 2 - 1;
                float warpedNoise = Mathf.PerlinNoise((x + warpX + seed) * noiseScale, (y + warpY + seed) * noiseScale);

                Vector3Int tilePosition = new Vector3Int(x, -y, 0);

                if (warpedNoise > waterThreshold)
                {
                    tilemap.SetTile(tilePosition, waterTile);
                }
                else if (warpedNoise > stoneThreshold && Random.value > 0.7f) // More isolated stone
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
}
