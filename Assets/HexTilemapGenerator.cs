using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class HexTilemapGenerator : MonoBehaviour
{
    private int width = 30;
    private int height = 300;
    public Tilemap tilemap;
    public TileBase dirtTile, waterTile, stoneTile, foodTile, minedTile, spawnTile;

    public float noiseScale = 0.3f; // Lower for bigger clusters
    public float stoneNoiseScale = 0.15f; // Stone uses separate noise for better clustering
    public float waterThreshold = 0.6f;
    public float stoneThreshold = 0.3f;
    private int seed;
    private int minedBlockCount =0;
    public ProgressBar foodProgressBar;
    public ProgressBar waterProgressBar;
    public ProgressBar populationProgressBar;
    public ProgressBar satisfactionProgressBar;

    public Dictionary<Vector3Int, HexTileData> hexMapData = new Dictionary<Vector3Int, HexTileData>();

    public float population = 10;
    private float food = 100;
    private float water = 200;
    public TextMeshProUGUI antCountText;

    private int waterGenerator = 0;
    private int foodGenerator =0;
    private int SpawnGenerator = 0;

    //This code is to notify AntAI when the first tile has been mined.
    private bool firstBlockMined = false;
    private Vector3Int firstMinedBlockPosition;
    public AntAI antAI; // Reference to AntAI script

    private float minAntsPerBlock = 5f;
    private float maxAntsPerBlock = 10f;

    float spaceRatio=100;
    float foodRatio=100;
    float waterRatio=100;
    float satisfactionRatio = 100;

    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    private bool gameOverTriggered = false;
    

    void Start()
    {
        gameOverPanel.SetActive(false);
        seed = UnityEngine.Random.Range(0, 10000);
        GenerateMap(seed);
        StartCoroutine(FillGenerators());
        StartCoroutine(UpdateResourcesCoroutine());
        UpdateAntText();
        

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
                    if (hexMapData[mouseCell].Tile == foodTile || hexMapData[mouseCell].Tile == waterTile|| hexMapData[mouseCell].Tile == spawnTile)
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
        EnsureSpawnPlacement();
    }


    void EnsureFoodPlacement()
    {
        for (int y = 5; y < height; y += 10)
        {
            int randomX = UnityEngine.Random.Range(0, width);
            int randomYOffset = UnityEngine.Random.Range(-3, 3);
            Vector3Int tilePosition = new Vector3Int(randomX, -(y + randomYOffset), 0);
            tilemap.SetTile(tilePosition, foodTile);
            hexMapData[tilePosition].Tile = foodTile;

        }
    }

    void EnsureSpawnPlacement()
    {
        //ensure spawn with ant nest on row 2 with surronding stone
        int randomStartX = UnityEngine.Random.Range(0, width);
        Vector3Int TilePosStart = new Vector3Int(randomStartX, -1, 0);
        tilemap.SetTile(TilePosStart, spawnTile);
        hexMapData[TilePosStart].Tile = spawnTile;
        //make surrounding tiles-> dirt
        Vector3Int[] neighbors = GetHexNeighbors(TilePosStart);
        foreach (Vector3Int neighbor in neighbors)
        {
            if (hexMapData.TryGetValue(neighbor, out HexTileData tileData1))
            {
                if (tileData1.Tile != dirtTile) // Ensure at least one adjacent mined tile
                {
                    tilemap.SetTile(neighbor, dirtTile);
                    hexMapData[neighbor].Tile = dirtTile;
                }
            }
        }

        for (int y = 10; y < height; y += 15)
        {
            int randomX = UnityEngine.Random.Range(0, width);
            int randomYOffset = UnityEngine.Random.Range(-3, 3);
            Vector3Int tilePosition = new Vector3Int(randomX, -(y + randomYOffset), 0);
            tilemap.SetTile(tilePosition, spawnTile);
            hexMapData[tilePosition].Tile = spawnTile;

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

                if (neighborTile.Tile == spawnTile)
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

                if (tileData.Tile == waterTile && tileData.IsActivated)
                {
                    if (tileData.FillLevel < tileData.MaxFill)
                    {
                        tileData.FillLevel += 1f; // Increase fill level
                        float fillRatio = tileData.FillLevel / tileData.MaxFill;
                        // Color newColor = Color.Lerp(Color.white, Color.blue, fillRatio);
                        // tilemap.SetColor(tilePos, newColor);
                        if (tileData.Tile == waterTile)
                        {
                            tilemap.SetColor(tilePos, Color.Lerp(Color.white, Color.blue, fillRatio));
                            }
                        else if (tileData.Tile == foodTile)
                        {
                           tilemap.SetColor(tilePos, Color.Lerp(Color.white, Color.red, fillRatio));
                         }
                        Debug.Log($"Filling Water at {tileData.Tile.name}: {tileData.FillLevel}/{tileData.MaxFill}");
                    }
                }
                if (tileData.Tile == foodTile && tileData.IsActivated)
                {
                    if (tileData.FillLevel < tileData.MaxFill)
                    {
                        tileData.FillLevel += 5f; // Increase fill level
                        float fillRatio = tileData.FillLevel / tileData.MaxFill;
                        Color newColor = Color.Lerp(Color.white, new Color(0.5f, 0, 0.5f), fillRatio); // Purple
                        tilemap.SetColor(tilePos, newColor);
                        Debug.Log($"Filling Food at {tileData.Tile.name}: {tileData.FillLevel}/{tileData.MaxFill}");
                    }
                }
                if (tileData.Tile == spawnTile && tileData.IsActivated)
                {
                    if (tileData.FillLevel < tileData.MaxFill)
                    {
                        tileData.FillLevel += 2f; // Increase fill level
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
            if (tileData.Tile == waterTile)
            {
                water += tileData.FillLevel;
                Debug.Log($"Collected {tileData.FillLevel} water from tile {cell}, water = {water} ");
                tileData.FillLevel = 0;
                tilemap.SetColor(cell, Color.white);
                FindFirstObjectByType<AudioManager>().Play("waterSound");
                
            }

            if (tileData.Tile == foodTile)
            {
                food += tileData.FillLevel;
                Debug.Log($"Collected {tileData.FillLevel} food from tile {cell}, food= {food} ");
                tileData.FillLevel = 0;
                tilemap.SetColor(cell, Color.white);
                FindFirstObjectByType<AudioManager>().Play("foodSound");
            }

            if (tileData.Tile == spawnTile)
            {
                population += tileData.FillLevel;
                UpdateAntText();
                Debug.Log($"Spawned {tileData.FillLevel} ants from tile {cell}, food= {food} ");
                tileData.FillLevel = 0;
            }
        }
    }

    private IEnumerator UpdateResourcesCoroutine(){
        while (true){
            UpdateProgressBars();
            yield return new WaitForSeconds(1f);
        }
    }

    public void UpdateProgressBars(){

        
        UpdateResourceBar();
        UpdatePopulationBar();
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

    


    public void UpdatePopulationBar(){
        populationProgressBar.maximum = 100;
        
        if (minedBlockCount == 0)
    {
        spaceRatio = 100;
        populationProgressBar.SetProgress(100); // Avoid divide-by-zero, no space yet
        return;
    }

        float avgAntsPerBlock = (float)population / minedBlockCount;
            if (avgAntsPerBlock <= minAntsPerBlock){
                spaceRatio = 100;
                populationProgressBar.SetProgress(100);  // Full space
    }
            else if (avgAntsPerBlock >= maxAntsPerBlock)
    {
                spaceRatio = 0;
                populationProgressBar.SetProgress(0); // No space
    }
             else{
                float overuseRatio = (avgAntsPerBlock - minAntsPerBlock) / (maxAntsPerBlock - minAntsPerBlock);
                spaceRatio = Mathf.Clamp01(1f - overuseRatio) * 100f;
                populationProgressBar.SetProgress((int)spaceRatio);
                }

    }

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
        Time.timeScale = 0f;

    }
    public void UpdateAntText(){
        if (antCountText != null){
           antCountText.text = population.ToString(); 
        }
    }

    
















}





