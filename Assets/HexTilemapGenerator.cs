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
    private float food = 0;
    private float water = 0;

    private int waterGenerator = 0;
    private int foodGenerator =0;

    //This code is to notify AntAI when the first tile has been mined.
    private bool firstBlockMined = false;
    private Vector3Int firstMinedBlockPosition;
    public AntAI antAI; // Reference to your AntAI script

    void Start()
    {
        seed = Random.Range(0, 10000);
        GenerateMap(seed);
        StartCoroutine(FillGenerators());

    }

    private void Update()
    {
       CheckInput();
    }

    void CheckInput()
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
                    if (hexMapData[mouseCell].Tile == foodTile || hexMapData[mouseCell].Tile == waterTile)
                    {
                        CollectResource(mouseCell);
                    }
                    else
                    {
                        MineBlock(mouseCell);
                    }

                }
            }
        }
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

    void MineBlock(Vector3Int mouseCell)
    {
        
        int costToMine = GetMiningCost(mouseCell);

        if (tilemap.HasTile(mouseCell) && CanMineTile(mouseCell))
        {
            if (population >= costToMine)
            {
                population -= costToMine;
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
        }
    }
      
    


    int GetMiningCost(Vector3Int mouseCell) 
    { 
        int baseCost = 5; 
        int increasePerStep = 10;

        if (hexMapData[mouseCell].Tile == stoneTile)
        {
            return 10*(baseCost + ((Mathf.Abs(mouseCell.y) / 10) * increasePerStep));
        }

            return baseCost + ((Mathf.Abs(mouseCell.y) / 10) * increasePerStep);
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

    //check and activate resource tiles
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

    private IEnumerator FillGenerators()
    {
        while (true)
        {
            foreach (var kvp in hexMapData)
            {
                HexTileData tileData = kvp.Value;

                if (tileData.Tile == waterTile && tileData.IsActivated)
                {
                    if (tileData.FillLevel < tileData.MaxFill)
                    {
                        tileData.FillLevel += 1f; // Increase fill level
                        Debug.Log($"Filling Water at {tileData.Tile.name}: {tileData.FillLevel}/{tileData.MaxFill}");
                    }
                }
                if (tileData.Tile == foodTile && tileData.IsActivated)
                {
                    if (tileData.FillLevel < tileData.MaxFill)
                    {
                        tileData.FillLevel += 5f; // Increase fill level
                        Debug.Log($"Filling Food at {tileData.Tile.name}: {tileData.FillLevel}/{tileData.MaxFill}");
                    }
                }
            }
            yield return new WaitForSeconds(1f); // Adjust fill speed
        }
    }

    public void CollectResource(Vector3Int cell)
    {
        if (hexMapData.TryGetValue(cell, out var tileData)) 
        {
            if (tileData.Tile == waterTile)
            {
                water += tileData.FillLevel;
                Debug.Log($"Collected {tileData.FillLevel} water from tile {cell}, water = {water} ");
                tileData.FillLevel = 0;
                
            }

            if (tileData.Tile == foodTile)
            {
                food += tileData.FillLevel;
                Debug.Log($"Collected {tileData.FillLevel} food from tile {cell}, food= {food} ");
                tileData.FillLevel = 0;
            }
        }
    }











}





