using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

public class HexTilemapGenerator : MonoBehaviour
{
    private int width = 30;
    private int height = 300;
    public Tilemap tilemap;
    public TileBase dirtTile, waterTile, stoneTile, foodTile, minedTile;

    public float noiseScale = 0.3f; // Lower for bigger clusters
    public float stoneNoiseScale = 0.15f; // Stone uses separate noise for better clustering
    public float waterThreshold = 0.6f;
    public float stoneThreshold = 0.3f;
    private int seed;

    private Dictionary<TileBase, string> tileNames;
    int population = 1000;

    void Start()
    {
        // Initialize dictionary with tile names
        tileNames = new Dictionary<TileBase, string>
        {
            { dirtTile, "Dirt" },
            { waterTile, "Water" },
            { stoneTile, "Stone" },
            { foodTile, "Food" },
            { minedTile, "Mined" }
        };

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
                //perlin noise for stone and base tile
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
            // Raycast from camera to the tilemap plane(z=0)
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane tilemapPlane = new Plane(Vector3.forward, Vector3.zero);

            if (tilemapPlane.Raycast(ray, out float distance))
            {
                Vector3 mousePosWorld = ray.GetPoint(distance); 
                Vector3Int mouseCell = tilemap.WorldToCell(mousePosWorld);
                TileBase clickedTile = tilemap.GetTile(mouseCell);
                Debug.Log(clickedTile);

                int CostToMine = GetMiningCost(mouseCell.y); //give the yposition of the cell

                // Check if there is a tile at the clicked position
                if (tilemap.HasTile(mouseCell) && CanMineTile(mouseCell))
                {
                    if(population >= CostToMine)
                    {
                        population -= CostToMine;
  
                        Debug.Log($"Mining tile at: {mouseCell} (World Pos: {mousePosWorld})");
                        Debug.Log($"Mining cost {CostToMine}.Your new population is {population}");
                        tilemap.SetTile(mouseCell, minedTile); // Remove the tile
                        FindFirstObjectByType<AudioManager>().Play("DigTunnel"); // Play digtunnel sound
                    }

                    else
                    {
                        Debug.Log("Not enough ants!!");
                    }
  
                }
                else
                {
                    Debug.Log("No available to tile to mine at this position.");
                }
            }
        }
    }

    int GetMiningCost(int y)
    {
        int baseCost = 5; 
        int increasePerStep = 10; 

        return baseCost + ((Mathf.Abs(y) / 10) * increasePerStep);
    }

    bool CanMineTile(Vector3Int cell)
    {
        if (cell.y == 0) return true; // Allow mining at the top row

        Vector3Int[] neighbors = GetHexNeighbors(cell);

        foreach (Vector3Int neighbor in neighbors)
        {
            TileBase neighborTile = tilemap.GetTile(neighbor);

            if (neighborTile == minedTile) // Check if adjacent tile is a mined tile
            {
                return true;
            }
        }

        return false;
    }


    Vector3Int[] GetHexNeighbors(Vector3Int cell)
    {
        bool isEvenRow = (cell.y % 2 == 0);

        if (isEvenRow)
        {

            return new Vector3Int[]
            {
            new Vector3Int(cell.x - 1, cell.y, 0), // Left
            new Vector3Int(cell.x + 1, cell.y, 0), // Right
            new Vector3Int(cell.x - 1, cell.y - 1, 0), // Bottom Left
            new Vector3Int(cell.x, cell.y - 1, 0), // Bottom Right
            new Vector3Int(cell.x - 1, cell.y + 1, 0), // Top Left
            new Vector3Int(cell.x, cell.y + 1, 0) // Top Right
            }; 
        }
        else
        {
            return new Vector3Int[]
            {
            new Vector3Int(cell.x - 1, cell.y, 0), // Left
            new Vector3Int(cell.x + 1, cell.y, 0), // Right
            new Vector3Int(cell.x, cell.y - 1, 0), // Bottom Left
            new Vector3Int(cell.x + 1, cell.y - 1, 0), // Bottom Right
            new Vector3Int(cell.x, cell.y + 1, 0), // Top Left
            new Vector3Int(cell.x + 1, cell.y + 1, 0) // Top Right
            };
        }
    }

   
}





