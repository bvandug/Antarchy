using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using System.Threading.Tasks;
using static UnityEngine.RuleTile.TilingRuleOutput;
using Unity.VisualScripting;


public class HexTilemapGenerator : MonoBehaviour
{
    private int width = 28;
    private int height = 300;
    public Tilemap tilemap;
    public TileBase dirtTile1, dirtTile2, dirtTile3,
        stoneTile, minedTile,
        WaterTile0, WaterTile25, WaterTile50, WaterTile75, WaterTile100,
        FoodTile0, FoodTile25, FoodTile50, FoodTile75, FoodTile100,
        CrackTile1, CrackTile2, CrackTile3,
        Dirt2crack1, Dirt2crack2, Dirt2crack3,
        Dirt3crack1, Dirt3crack2, Dirt3crack3,
        SpawnTile0, SpawnTile25, SpawnTile50, SpawnTile75, SpawnTile100;

    public float noiseScale = 0.3f; // Lower for bigger clusters
    public float stoneNoiseScale = 0.15f; // Stone uses separate noise for better clustering
    public float waterThreshold = 0.6f;
    public float stoneThreshold = 0.3f;
    private int seed;
    public int minedBlockCount =0;
    public ProgressBar foodProgressBar;
    public ProgressBar waterProgressBar;
    public ProgressBar populationProgressBar;
    public ProgressBar satisfactionProgressBar;

    public Dictionary<Vector3Int, HexTileData> hexMapData = new Dictionary<Vector3Int, HexTileData>();

    public float population = 100;
    private float food = 1000;
    private float water = 2000;
    public TextMeshProUGUI antCountText;

    private int waterGenerator = 0;
    private int foodGenerator =0;
    private int SpawnGenerator = 0;

    //This code is to notify AntAI when the first tile has been mined.
    private bool firstBlockMined = false;
    public Vector3Int firstMinedBlockPosition;
    public AntAI antAI; // Reference to AntAI script

    private float minAntsPerBlock = 5f;
    private float maxAntsPerBlock = 10f;

    float spaceRatio=100;
    float foodRatio=100;
    float waterRatio=100;
    float satisfactionRatio = 100;

    public GameObject gameOverPanel;
    public GameObject cannotMinePanel;
    public TextMeshProUGUI gameOverText;
    private bool gameOverTriggered = false;
    

    void Start()
    {
        gameOverPanel.SetActive(false);
        cannotMinePanel.SetActive(false);
        seed = UnityEngine.Random.Range(0, 10000);
        GenerateMap(seed);
        //GenerateDemo();
        StartCoroutine(FillGenerators());
        StartCoroutine(UpdateResourcesCoroutine());
        UpdateAntText();

        FindFirstObjectByType<AudioManager>().Play("Theme");
        

    }

    private void Update()
    {
       CheckInput();
       UpdateAntText();
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
                    if (CheckFoodTile(mouseCell)|| CheckWaterTile(mouseCell) || CheckSpawnTile(mouseCell))
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
                TileBase selectedTile = dirtTile1;

                if (baseNoise > waterThreshold)
                    selectedTile = WaterTile100;
                else if (stoneNoise > stoneThreshold)
                    selectedTile = stoneTile;
                else
                {
                    // Loop through dirt tiles every 30 rows (0-9 -> dirt1, 10-19 -> dirt2, 20-29 -> dirt3, then repeat)
                    switch ((y / 10) % 3)
                    {
                        case 0:
                            selectedTile = dirtTile1;
                            break;
                        case 1:
                            selectedTile = dirtTile2;
                            break;
                        case 2:
                            selectedTile = dirtTile3;
                            break;
                    }
                }

                tilemap.SetTile(tilePosition, selectedTile);
                hexMapData[tilePosition] = new HexTileData(selectedTile);
            }
        }

        EnsureFoodPlacement();
        EnsureSpawnPlacement();
    }


    void GenerateDemo(int seed =1)
    {
        tilemap.ClearAllTiles();
        hexMapData.Clear(); // Reset the dictionary

        for (int y = 0; y < 11; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float baseNoise = Mathf.PerlinNoise((x + seed) * noiseScale, (y + seed) * noiseScale);
                float stoneNoise = Mathf.PerlinNoise((x + seed + 500) * stoneNoiseScale, (y + seed + 500) * stoneNoiseScale);

                Vector3Int tilePosition = new Vector3Int(x, -y, 0);
                TileBase selectedTile;

                if (baseNoise > waterThreshold)
                    selectedTile = WaterTile100;
                else if (stoneNoise > stoneThreshold)
                    selectedTile = stoneTile;
                else
                    selectedTile = dirtTile1;

                tilemap.SetTile(tilePosition, selectedTile);
                hexMapData[tilePosition] = new HexTileData(selectedTile);

            }
            Vector3Int Food1  = new Vector3Int(3, -(3), 0);
            tilemap.SetTile(Food1, FoodTile100);
            hexMapData[Food1] = new HexTileData(FoodTile100);

            Vector3Int Spawn  = new Vector3Int(7, -(1), 0);
            tilemap.SetTile(Spawn, SpawnTile100);
            hexMapData[Spawn] = new HexTileData(SpawnTile100);

            Vector3Int Water1 = new Vector3Int(8, -(4), 0);
            tilemap.SetTile(Water1, WaterTile100);
            hexMapData[Water1] = new HexTileData(WaterTile100);
        }

        
    }



    void EnsureFoodPlacement()
    {
        for (int y = 5; y < height; y += 7)
        {
            int randomX = UnityEngine.Random.Range(0, width);
            int randomYOffset = UnityEngine.Random.Range(-3, 3);
            int clampedY = Mathf.Clamp(y + randomYOffset, 0, height-1);

            Vector3Int tilePosition = new Vector3Int(randomX, -clampedY, 0);
            tilemap.SetTile(tilePosition, FoodTile100);
            hexMapData[tilePosition].Tile = FoodTile100;
        }
    }


    void EnsureSpawnPlacement()
    {
        //ensure spawn with ant nest on row 2 with surronding stone
        int randomStartX = UnityEngine.Random.Range(0, width);
        Vector3Int TilePosStart = new Vector3Int(randomStartX, -1, 0);
        tilemap.SetTile(TilePosStart, SpawnTile100);
        hexMapData[TilePosStart].Tile = SpawnTile100;
        //make surrounding tiles-> dirt
        Vector3Int[] neighbors = GetHexNeighbors(TilePosStart);
        foreach (Vector3Int neighbor in neighbors)
        {
            if (hexMapData.TryGetValue(neighbor, out HexTileData tileData1))
            {
                if (tileData1.Tile != dirtTile1) // Ensure at least one adjacent mined tile
                {
                    tilemap.SetTile(neighbor, dirtTile1);
                    hexMapData[neighbor].Tile = dirtTile1;
                }
            }
        }

        for (int y = 10; y < height; y += 15)
        {
            int randomX = UnityEngine.Random.Range(0, width);
            int randomYOffset = UnityEngine.Random.Range(-3, 3);
            int clampedY = Mathf.Clamp(y + randomYOffset, 0, height-1);
            Vector3Int tilePosition = new Vector3Int(randomX, -(clampedY), 0);
            tilemap.SetTile(tilePosition, SpawnTile100);
            hexMapData[tilePosition].Tile = SpawnTile100;

        }
    }

    void MineBlock(Vector3Int mouseCell)
    {
        int costToMine = GetMiningCost(mouseCell);

        if (tilemap.HasTile(mouseCell) && CanMineTile(mouseCell))
        {
            if (population == costToMine)
            {
                cannotMinePanel.SetActive(true);
            }
            else if (population > costToMine)
            {
                population -= costToMine;
                Debug.Log($"Mining cost {costToMine}. Your new population is {population}");

                // Start the tile mining animation with delays
                StartCoroutine(MineTileWithDelay(mouseCell));

                hexMapData[mouseCell].Tile = minedTile; // Update dictionary
                minedBlockCount++;
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

    // Coroutine for delayed tile updates
    private IEnumerator MineTileWithDelay(Vector3Int mouseCell)
    {
        if (hexMapData[mouseCell].Tile == dirtTile1 )
        {

            tilemap.SetTile(mouseCell, CrackTile1);
            yield return new WaitForSeconds(0.2f);

            tilemap.SetTile(mouseCell, CrackTile2);
            yield return new WaitForSeconds(0.2f);

            tilemap.SetTile(mouseCell, CrackTile3);
            yield return new WaitForSeconds(0.2f);

            tilemap.SetTile(mouseCell, minedTile);
        }

        if (hexMapData[mouseCell].Tile == dirtTile2)
        {

            tilemap.SetTile(mouseCell, Dirt2crack1);
            yield return new WaitForSeconds(0.2f);

            tilemap.SetTile(mouseCell, Dirt2crack2);
            yield return new WaitForSeconds(0.2f);

            tilemap.SetTile(mouseCell, Dirt2crack3);
            yield return new WaitForSeconds(0.2f);

            tilemap.SetTile(mouseCell, minedTile);
        }

        if (hexMapData[mouseCell].Tile == dirtTile3)
        {

            tilemap.SetTile(mouseCell, Dirt3crack1);
            yield return new WaitForSeconds(0.2f);

            tilemap.SetTile(mouseCell, Dirt3crack2);
            yield return new WaitForSeconds(0.2f);

            tilemap.SetTile(mouseCell, Dirt3crack3);
            yield return new WaitForSeconds(0.2f);

            tilemap.SetTile(mouseCell, minedTile);
        }

        if (hexMapData[mouseCell].Tile == stoneTile) 
        { 
            tilemap.SetTile(mouseCell, minedTile);
        }



        // Check for flooding
        Vector3Int[] floodNeighbors = getFloodNeighbors(mouseCell);
        foreach (Vector3Int floodNeighbor in floodNeighbors)
        {
            if (hexMapData.TryGetValue(floodNeighbor, out HexTileData neighborTile))
            {
                if (CheckWaterTile(floodNeighbor))
                {
                    StartCoroutine(FloodTiles(floodNeighbor));
                }
            }
        }
    }


    // This method returns the neighboring tiles that can cause a flood
    public Vector3Int[] getFloodNeighbors(Vector3Int cell)
    {
        bool isEvenRow = (cell.y % 2 == 0);

        if (isEvenRow)
        {
            return new Vector3Int[] 
            {
                new Vector3Int(cell.x - 1, cell.y + 1, 0), // Top Left
                new Vector3Int(cell.x, cell.y + 1, 0) // Top Right
            };
        }
        else {
            return new Vector3Int[]
            {
                new Vector3Int(cell.x, cell.y + 1, 0), // Top Left
                new Vector3Int(cell.x + 1, cell.y + 1, 0) // Top Right
            };
        }
    }

    private IEnumerator FloodTiles(Vector3Int cell)
    {
        hexMapData[cell].Tile = dirtTile1;
        tilemap.SetTile(cell, WaterTile100); //destroy water tile
        yield return new WaitForSeconds(0.3f);
        Vector3Int[] firstwave = FindFirstFloodTiles(cell);
        Vector3Int[] secondwave = FindSecondFloodTiles(cell);

        foreach(Vector3Int floodTile1 in firstwave)
        {
            tilemap.SetTile(floodTile1, WaterTile100);
            hexMapData[floodTile1].Tile = dirtTile1;
        }
        yield return new WaitForSeconds(0.3f);

        foreach (Vector3Int floodTile2 in secondwave)
        {
            tilemap.SetTile(floodTile2, WaterTile100);
            hexMapData[floodTile2].Tile = dirtTile1;
        }

        yield return new WaitForSeconds(0.3f);

        tilemap.SetTile(cell, dirtTile1); //destroy water tile
        yield return new WaitForSeconds(0.3f);

        foreach (Vector3Int floodTile1 in firstwave)
        {
            tilemap.SetTile(floodTile1, dirtTile1);
            hexMapData[floodTile1].Tile = dirtTile1;
        }

        yield return new WaitForSeconds(0.3f);

        foreach (Vector3Int floodTile2 in secondwave)
        {
            tilemap.SetTile(floodTile2, dirtTile1);
            hexMapData[floodTile2].Tile = dirtTile1;
        }
    }
      
    public int GetMiningCost(Vector3Int mouseCell) 
    { 
        int baseCost = 5; 
        int increasePerStep = 10;

        if (hexMapData[mouseCell].Tile == stoneTile)
        {
            return 10*(baseCost + ((Mathf.Abs(mouseCell.y) / 10) * increasePerStep));
        }

            return baseCost + ((Mathf.Abs(mouseCell.y) / 10) * increasePerStep);
    }

    public bool CanMineTile(Vector3Int cell)
    {
        if (!hexMapData.TryGetValue(cell, out HexTileData tileData ))
            return false; // No tile present

        if (hexMapData[cell].Tile != stoneTile && !CheckDirtTile(cell))
        {
            Debug.Log("not stone or dirt tile cannot mine");
            return false;
        }
            

        if (cell.y == 0)
        {
            if (!CheckDirtTile(cell) && tileData.Tile != stoneTile)
            {  return false; }
            else { 
                return true;
            }
            
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

    //this just returns number coords not actual tiles
    public Vector3Int[] FindFirstFloodTiles(Vector3Int cell)
    {
        bool isEvenRow = (cell.y % 2 == 0);
        List<Vector3Int> validTiles = new List<Vector3Int>();
        Vector3Int[] potentialTiles;

        if (isEvenRow)
        {
            potentialTiles = new Vector3Int[]
            {
                new Vector3Int(cell.x - 1, cell.y, 0), // Left
                new Vector3Int(cell.x + 1, cell.y, 0), // Right
                new Vector3Int(cell.x - 1, cell.y - 1, 0), // Bottom Left
                new Vector3Int(cell.x, cell.y - 1, 0), // Bottom Right
            };
        }
        else
        {
            potentialTiles = new Vector3Int[]
            {
                new Vector3Int(cell.x - 1, cell.y, 0), // Left
                new Vector3Int(cell.x + 1, cell.y, 0), // Right
                new Vector3Int(cell.x, cell.y - 1, 0), // Bottom Left
                new Vector3Int(cell.x + 1, cell.y - 1, 0), // Bottom Right
            };
        }

        // Filter out tiles that are out of bounds
        foreach (var tile in potentialTiles)
        {
            if (tile.x >= 0 && tile.x < width)
            {
                validTiles.Add(tile);
            }
        }

        return validTiles.ToArray();
    }


    public Vector3Int[] FindSecondFloodTiles(Vector3Int cell)
    {
        bool isEvenRow = (cell.y % 2 == 0);
        List<Vector3Int> validTiles = new List<Vector3Int>();

        Vector3Int[] potentialTiles;

        if (isEvenRow)
        {
            Debug.LogFormat("EVEN ROW");
            potentialTiles = new Vector3Int[]
            {
                new Vector3Int(cell.x - 2, cell.y, 0), // Left -> Left
                new Vector3Int(cell.x + 2, cell.y, 0), // Right -> Right

                new Vector3Int(cell.x, cell.y - 2, 0), // Bottom Left -> Bottom Left
                new Vector3Int(cell.x + 1, cell.y - 2, 0), // Bottom Left -> Right

                new Vector3Int(cell.x - 2, cell.y - 1, 0), // Bottom Left ->left
                new Vector3Int(cell.x+1, cell.y - 1, 0), // Bottom Right -> right

                new Vector3Int(cell.x - 1, cell.y - 2, 0), // Bottom Right -> Bottom Right
            };
        }
        else
        {
            Debug.LogFormat("ODD ROW");
            potentialTiles = new Vector3Int[]
            {
                /*
                new Vector3Int(cell.x - 1, cell.y, 0), // Left
                new Vector3Int(cell.x + 1, cell.y, 0), // Right
                new Vector3Int(cell.x, cell.y - 1, 0), // Bottom Left
                new Vector3Int(cell.x + 1, cell.y - 1, 0), // Bottom Right
                */

                new Vector3Int(cell.x - 2, cell.y, 0), // Left-> Left
                new Vector3Int(cell.x + 2, cell.y, 0), // Right-> Right

                new Vector3Int(cell.x - 1, cell.y - 2, 0), // Bottom Left -> Bottom Left
                new Vector3Int(cell.x-1, cell.y - 1, 0), // Bottom Left -> right
                new Vector3Int(cell.x, cell.y-2, 0), // Bottom Left -> Bottom Left
                //new Vector3Int(cell.x, cell.y - 1, 0), // Bottom Left

                new Vector3Int(cell.x + 1, cell.y - 1, 0), // Bottom Right -> Left
                new Vector3Int(cell.x + 2, cell.y - 1, 0), // Bottom Right -> right
                new Vector3Int(cell.x + 1, cell.y - 2, 0), // Bottom Right -> Bottom Right
            };
        }

        // Filter out tiles that are out of bounds
        foreach (var tile in potentialTiles)
        {
            if (tile.x >= 0 && tile.x < width)
            {
                validTiles.Add(tile);
            }
        }

        return validTiles.ToArray();
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
                if(CheckWaterTile(neighbor))
                {
                    
                    if (!neighborTile.IsActivated)
                    {
                        neighborTile.IsActivated = true;
                        waterGenerator += 1;
                        Debug.Log($"Water Generators amount increased to:{waterGenerator}");
                    }
                }

                if(CheckFoodTile(neighbor))
                {
                    if (!neighborTile.IsActivated)
                    {
                        neighborTile.IsActivated = true;
                        foodGenerator += 1;
                        Debug.Log($"Food Generators amount increased to {foodGenerator}");
                    }
                }

                if (CheckSpawnTile(neighbor))
                {
                    if (!neighborTile.IsActivated)
                    {
                        neighborTile.IsActivated = true;
                        SpawnGenerator += 1;
                        Debug.Log($"Spawn Generators amount increased to {SpawnGenerator}");
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
                Vector3Int tilePos = kvp.Key;

                if (CheckWaterTile(tilePos) && tileData.IsActivated && !tileData.IsDisabled)
                {
                    if (tileData.FillLevel < tileData.MaxFill)
                    {
                        tileData.FillLevel += 2f; // Increase fill level
                        if (tileData.FillLevel <= 50f)
                        {
                            tilemap.SetTile(tilePos, WaterTile0);
                        }

                        if (tileData.FillLevel > 50f && tileData.FillLevel <=100f)
                        {
                            tilemap.SetTile(tilePos, WaterTile25);
                        }

                        if (tileData.FillLevel > 100f && tileData.FillLevel <= 150f)
                        {
                            tilemap.SetTile(tilePos, WaterTile50);
                        }

                        if (tileData.FillLevel > 150f && tileData.FillLevel <= 199f)
                        {
                            tilemap.SetTile(tilePos, WaterTile75);
                        }

                        if (tileData.FillLevel == 200f)
                        {
                            tilemap.SetTile(tilePos, WaterTile100);
                        }
                        Debug.Log($"Collecting water at {tileData.Tile.name}: {tileData.FillLevel}/{tileData.MaxFill}");

                    }
                }
                if (CheckFoodTile(tilePos) && tileData.IsActivated && !tileData.IsDisabled)
                {
                    if (tileData.FillLevel < tileData.MaxFill)
                    {
                        tileData.FillLevel += 2f; // Increase fill level
                        if (tileData.FillLevel <= 50f)
                        {
                            tilemap.SetTile(tilePos, FoodTile0);
                        }

                        if (tileData.FillLevel > 50f && tileData.FillLevel <= 100f)
                        {
                            tilemap.SetTile(tilePos, FoodTile25);
                        }

                        if (tileData.FillLevel > 100f && tileData.FillLevel <= 150f)
                        {
                            tilemap.SetTile(tilePos, FoodTile50);
                        }

                        if (tileData.FillLevel > 150f && tileData.FillLevel <= 199f)
                        {
                            tilemap.SetTile(tilePos, FoodTile75);
                        }

                        if (tileData.FillLevel == 200f)
                        {
                            tilemap.SetTile(tilePos, FoodTile100);
                        }
                        Debug.Log($"Collecting food at {tileData.Tile.name}: {tileData.FillLevel}/{tileData.MaxFill}");

                    }
                }
                if (CheckSpawnTile(tilePos) && tileData.IsActivated && !tileData.IsDisabled)
                {
                    if (tileData.FillLevel < tileData.MaxFill)
                    {
                        tileData.FillLevel += 5f; // Increase fill level
                        if (tileData.FillLevel <= 50f)
                        {
                            tilemap.SetTile(tilePos, SpawnTile0);
                        }

                        if (tileData.FillLevel > 50f && tileData.FillLevel <= 100f)
                        {
                            tilemap.SetTile(tilePos, SpawnTile25);
                        }

                        if (tileData.FillLevel > 100f && tileData.FillLevel <= 150f)
                        {
                            tilemap.SetTile(tilePos, SpawnTile50);
                        }

                        if (tileData.FillLevel > 150f && tileData.FillLevel <= 199f)
                        {
                            tilemap.SetTile(tilePos, SpawnTile75);
                        }

                        if (tileData.FillLevel == 200f)
                        {
                            tilemap.SetTile(tilePos, SpawnTile100);
                        }
                        Debug.Log($"Spawning ants at {tileData.Tile.name}: {tileData.FillLevel}/{tileData.MaxFill}");
                        
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
            if (tileData.IsDisabled)
            {
                Debug.Log($"Tile at {cell} is disabled! Cannot collect resources.");
                return;
                }
            if (CheckWaterTile(cell))
            {
                foreach (var kvp in hexMapData)
                {
                    HexTileData tileInfo = kvp.Value;
                    Vector3Int tilePos = kvp.Key;

                    if (CheckWaterTile(tilePos) && tileInfo.IsActivated)
                    {
                        water += tileInfo.FillLevel;
                        Debug.Log($"Collected {tileInfo.FillLevel} water from tile {tilePos}, water = {water} ");
                        tileInfo.FillLevel = 0;
                        tilemap.SetTile(tilePos, WaterTile0);  
                        
                    }
                    FindFirstObjectByType<AudioManager>().Play("waterSound");
                }
            }

            if (CheckFoodTile(cell))
            {
                foreach (var kvp in hexMapData)
                {
                    HexTileData tileInfo = kvp.Value;
                    Vector3Int tilePos = kvp.Key;

                    if (CheckFoodTile(tilePos) && tileInfo.IsActivated)
                    {
                        food += tileInfo.FillLevel;
                        Debug.Log($"Collected {tileInfo.FillLevel} food from tile {tilePos}, food = {water} ");
                        tileInfo.FillLevel = 0;
                        tilemap.SetTile(tilePos, FoodTile0);

                    }
                    FindFirstObjectByType<AudioManager>().Play("foodSound");
                }
            }

            if (CheckSpawnTile(cell))
            {
                foreach (var kvp in hexMapData)
                {
                    HexTileData tileInfo = kvp.Value;
                    Vector3Int tilePos = kvp.Key;
                   

                    if (CheckSpawnTile(tilePos))
                    {
                        population += tileInfo.FillLevel;
                        UpdateAntText();
                        Debug.Log($"Spawned {tileInfo.FillLevel} ants from tile {tilePos}, food= {food} ");
                        tileInfo.FillLevel = 0;
                        tilemap.SetTile(tilePos, SpawnTile0);
                        
                    }

                    FindFirstObjectByType<AudioManager>().Play("eggHatching");
                }
            }
        }
    }

    private IEnumerator UpdateResourcesCoroutine(){
        while (true){
            UpdateProgressBars();
            yield return new WaitForSeconds(1f);
        }
    }

    public bool CheckWaterTile(Vector3Int tile)
    {
        if (hexMapData.TryGetValue(tile, out var tileData))
        {
            if (tileData.Tile == WaterTile0 || tileData.Tile == WaterTile25 || tileData.Tile == WaterTile50 || tileData.Tile == WaterTile75 || tileData.Tile == WaterTile100)
            {
                return true;
            }
        }
        return false;
    }

    public bool CheckFoodTile(Vector3Int tile)
    {
        if (hexMapData.TryGetValue(tile, out var tileData))
        {
            if (tileData.Tile == FoodTile0 || tileData.Tile == FoodTile25 || tileData.Tile == FoodTile50 || tileData.Tile == FoodTile75 || tileData.Tile == FoodTile100)
            {
                return true;
            }
        }
        return false;
    }

    public bool CheckSpawnTile(Vector3Int tile)
    {
        if (hexMapData.TryGetValue(tile, out var tileData))
        {
            if (tileData.Tile == SpawnTile0 || tileData.Tile == SpawnTile25 || tileData.Tile == SpawnTile50 || tileData.Tile == SpawnTile75 || tileData.Tile == SpawnTile100)
            {
                return true;
            }
        }
        return false;
    }

    public bool CheckDirtTile(Vector3Int tile)
    {
        if (hexMapData.TryGetValue(tile, out var tileData))
        {
            if (tileData.Tile == dirtTile1 || tileData.Tile == dirtTile2 || tileData.Tile == dirtTile3)
            {
                return true;
            }
        }
        return false;
    }

    //UI
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    public void UpdateProgressBars(){

        
        UpdateResourceBar();
        //UpdatePopulationBar();
        UpdateSatisfaction();
    
    }

    public void UpdateResourceBar(){

        float totalFoodNeeded = (1f/6f)*population*60;  
        float totalWaterNeeded = (1f/3f)*population*60;
        
        if (food > 0){
            food -= (1f/6f)*population;}
        if (water >0){
            water -= (1f/3f)*population;}

        foodRatio = Mathf.Clamp01(food/ totalFoodNeeded )*100;
        waterRatio = Mathf.Clamp01(water/ totalWaterNeeded)*100;
        
        

        // Set the ProgressBar maximum to 100 for percentage display
        foodProgressBar.maximum = 100;
        waterProgressBar.maximum = 100;
        

        foodProgressBar.SetProgress((int)(foodRatio ));
        waterProgressBar.SetProgress((int)(waterRatio ));

    }

    


    // public void UpdatePopulationBar(){
    //     populationProgressBar.maximum = 100;
        
    //     if (minedBlockCount == 0)
    // {
    //     spaceRatio = 100;
    //     populationProgressBar.SetProgress(100); // Avoid divide-by-zero, no space yet
    //     return;
    // }

    //     float avgAntsPerBlock = (float)population / minedBlockCount;
    //         if (avgAntsPerBlock <= minAntsPerBlock){
    //             spaceRatio = 100;
    //             populationProgressBar.SetProgress(100);  // Full space
    // }
    //         else if (avgAntsPerBlock >= maxAntsPerBlock)
    // {
    //             spaceRatio = 0;
    //             populationProgressBar.SetProgress(0); // No space
    // }
    //          else{
    //             float overuseRatio = (avgAntsPerBlock - minAntsPerBlock) / (maxAntsPerBlock - minAntsPerBlock);
    //             spaceRatio = Mathf.Clamp01(1f - overuseRatio) * 100f;
    //             populationProgressBar.SetProgress((int)spaceRatio);
    //             }

    // }

    public void UpdateSatisfaction(){

        satisfactionRatio = (spaceRatio + foodRatio + waterRatio)/3;
        satisfactionProgressBar.maximum=100;
        satisfactionProgressBar.SetProgress((int)satisfactionRatio);

        if (satisfactionRatio < 40 && !gameOverTriggered){
            

            TriggerGameOver("Satisfaction too low, the colony killed you!");
            Debug.Log("Game Over: Satisfaction too low!");


        }

    }

    public void TriggerGameOver(String reason){
        gameOverText.text = reason.ToString();
        gameOverTriggered = true;
        gameOverPanel.SetActive(true);
        // game over sound
        FindFirstObjectByType<AudioManager>().Pause("Theme");
        FindFirstObjectByType<AudioManager>().Pause("eggHatching");
        FindFirstObjectByType<AudioManager>().Pause("foodSound");
        FindFirstObjectByType<AudioManager>().Pause("waterSound");
        
        FindFirstObjectByType<AudioManager>().Play("gameOver");
        
        //FindFirstObjectByType<AudioManager>().Resume("Theme");
        Time.timeScale = 0f;

    }
    public void UpdateAntText(){
        if (antCountText != null){
           antCountText.text = population.ToString(); 
        }
    }


}





