using System.Collections;
using System.Collections.Generic;
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

    private Dictionary<Vector3Int, HexTileData> hexMapData = new Dictionary<Vector3Int, HexTileData>();


    private int population = 1000;
    private int foodGenerator = 0;
    private int food = 0;
    private int waterGenerator =0;
    private int water = 0;

    //This code is to notify AntAI when the first tile has been mined.
    private bool firstBlockMined = false;
    private Vector3Int firstMinedBlockPosition;
    public AntAI antAI; // Reference to your AntAI script

    void Start()
    {
        seed = Random.Range(0, 10000);
        GenerateMap(seed);
        StartCoroutine(GenerateResources());

    }

    private void Update()
    {
        MineBlock();
    }

    void GenerateMap(int seed)
    {
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
                hexMapData[tilePosition] = new HexTileData(selectedTile);
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
            hexMapData[tilePosition].Tile = foodTile;
       
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

                if (hexMapData.TryGetValue(mouseCell, out HexTileData tileData))
                {
                    bool stone = false;
                    if (hexMapData[mouseCell].Tile == stoneTile)
                        stone = true;

                    int costToMine = GetMiningCost(mouseCell.y, stone);

                    if (tilemap.HasTile(mouseCell) && CanMineTile(mouseCell))
                    {
                        if (population >= costToMine)
                        {
                            population -= costToMine;

                            Debug.Log($"Mining tile at: {mouseCell} (World Pos: {mousePosWorld})");
                            Debug.Log($"Mining cost {costToMine}. Your new population is {population}");

                            tilemap.SetTile(mouseCell, minedTile); // Set mined tile
                            hexMapData[mouseCell].Tile = minedTile; // Update dictionary
                            CheckResourceTile(mouseCell);
                            FindFirstObjectByType<AudioManager>().Play("DigTunnel");

                            if (!firstBlockMined)
                            {
                                firstBlockMined = true;
                                firstMinedBlockPosition = mouseCell;
                                
                                // Notify AntAI about the first mined block
                                if (antAI != null)
                                {
                                    antAI.OnFirstBlockMined(mouseCell);
                                }
                            }
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
        if (!hexMapData.TryGetValue(cell, out HexTileData tileData ))
            return false; // No tile present

        if (hexMapData[cell].Tile != stoneTile && hexMapData[cell].Tile != dirtTile)
        {
            Debug.Log("not stone or dirt tile cannot mine");
            return false;
        }
            

            if (cell.y == 0)
        {
            if (tileData.Tile != dirtTile && tileData.Tile != stoneTile)
                return false;
            return true;
        }
        

        Vector3Int[] neighbors = GetHexNeighbors(cell);

        foreach (Vector3Int neighbor in neighbors)
        {
            if (hexMapData.TryGetValue(neighbor, out HexTileData tileData1))
            {
                if (tileData1.Tile == minedTile) // Ensure at least one adjacent mined tile
                {
                    return true;
                }
            }
        }

        return false;
    }


    //this just returns number coords not actual tiles
    public Vector3Int[] GetHexNeighbors(Vector3Int cell) 
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

    // This method checks whether a tile has been mined or not
    public bool IsTileMined(Vector3Int cell)
    {
        if (hexMapData.TryGetValue(cell, out HexTileData tileData))
        {
            return tileData.Tile == minedTile;
        }
        return false;
    }

    //find and store the amount of resource generators
    private void CheckResourceTile(Vector3Int cell)
    {
        Vector3Int[] neighbors = GetHexNeighbors(cell);

        foreach (Vector3Int neighbor in neighbors)
        {
            if (hexMapData.TryGetValue(neighbor, out HexTileData neighborTile))
            {
                if(neighborTile.Tile == waterTile)
                {
                    if (!neighborTile.IsActivated)
                    {
                        neighborTile.IsActivated = true;
                        waterGenerator += 1;
                        Debug.Log($"Water Generators amount increased to:{waterGenerator}");
                    }
                }

                if(neighborTile.Tile == foodTile)
                {
                    if (!neighborTile.IsActivated)
                    {
                        neighborTile.IsActivated = true;
                        foodGenerator += 1;
                        Debug.Log($"Food Generators amount increased to {foodGenerator}");
                    }
                }
            }
        }
    }

    private IEnumerator GenerateResources()
    {
        while (true) // Runs indefinitely
        {
            if (waterGenerator > 0)
            {
                water += waterGenerator;
            }

            if (foodGenerator > 0)
            {
                food += foodGenerator;   
            }

            Debug.Log($"food: {food}, water {water}");

            yield return new WaitForSeconds(1f);
        }
    }



}





