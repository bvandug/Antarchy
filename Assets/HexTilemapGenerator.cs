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

    private Dictionary<Vector3Int, TileBase> hexMapData = new Dictionary<Vector3Int, TileBase>();

    int population = 1000;

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
        hexMapData.Clear(); // Reset the dictionary

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float baseNoise = Mathf.PerlinNoise((x + seed) * noiseScale, (y + seed) * noiseScale);
                float stoneNoise = Mathf.PerlinNoise((x + seed + 500) * stoneNoiseScale, (y + seed + 500) * stoneNoiseScale);

                Vector3Int tilePosition = new Vector3Int(x, -y, 0);
                TileBase selectedTile;

                if (baseNoise > waterThreshold)
                    selectedTile = waterTile;
                else if (stoneNoise > stoneThreshold)
                    selectedTile = stoneTile;
                else
                    selectedTile = dirtTile;

                tilemap.SetTile(tilePosition, selectedTile);
                hexMapData[tilePosition] = selectedTile; // Store in dictionary
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
            hexMapData[tilePosition] = foodTile;
       
        }
    }

    void MineBlock()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane tilemapPlane = new Plane(Vector3.forward, Vector3.zero);

            if (tilemapPlane.Raycast(ray, out float distance))
            {
                Vector3 mousePosWorld = ray.GetPoint(distance);
                Vector3Int mouseCell = tilemap.WorldToCell(mousePosWorld);

                if (hexMapData.TryGetValue(mouseCell, out TileBase clickedTile))
                {
<<<<<<< Updated upstream
                    if(population >= CostToMine)
                    {
                        population -= CostToMine;
  
                        Debug.Log($"Mining tile at: {mouseCell} (World Pos: {mousePosWorld})");
                        Debug.Log($"Mining cost {CostToMine}.Your new population is {population}");
                        tilemap.SetTile(mouseCell, minedTile); // Remove the tile
                    }
=======
                    bool stone = false;
                    if (hexMapData[mouseCell] == stoneTile)
                        stone = true;
>>>>>>> Stashed changes

                    int costToMine = GetMiningCost(mouseCell.y, stone);

                    if (tilemap.HasTile(mouseCell) && CanMineTile(mouseCell))
                    {
                        if (population >= costToMine)
                        {
                            population -= costToMine;

                            Debug.Log($"Mining tile at: {mouseCell} (World Pos: {mousePosWorld})");
                            Debug.Log($"Mining cost {costToMine}. Your new population is {population}");

                            tilemap.SetTile(mouseCell, minedTile); // Set mined tile
                            hexMapData[mouseCell] = minedTile; // Update dictionary
                            FindFirstObjectByType<AudioManager>().Play("DigTunnel");
                        }
                        else
                        {
                            Debug.Log("Not enough ants!!");
                        }
                    }
                    else
                    {
                        Debug.Log("No available tile to mine at this position.");
                    }
                }
            }
        }
    }


    int GetMiningCost(int y, bool stone)
    {
        int baseCost = 5; 
        int increasePerStep = 10;

        if (stone)
        {
            return 10*(baseCost + ((Mathf.Abs(y) / 10) * increasePerStep));
        }

            return baseCost + ((Mathf.Abs(y) / 10) * increasePerStep);
    }

    bool CanMineTile(Vector3Int cell)
    {
        if (!hexMapData.TryGetValue(cell, out TileBase targetTile))
            return false; // No tile present

        if (hexMapData[cell] != stoneTile && hexMapData[cell] != dirtTile)
        {
            Debug.Log("not stone or dirt tile cannot mine");
            return false;
        }
            

            if (cell.y == 0)
        {
            if (targetTile != dirtTile && targetTile != stoneTile)
                return false;
            return true;
        }
        

        Vector3Int[] neighbors = GetHexNeighbors(cell);

        foreach (Vector3Int neighbor in neighbors)
        {
            if (hexMapData.TryGetValue(neighbor, out TileBase neighborTile))
            {
                if (neighborTile == minedTile) // Ensure at least one adjacent mined tile
                {
                    return true;
                }
            }
        }

        return false;
    }


    //this just returns number coords not actual tiles
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





