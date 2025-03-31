using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;


public class TutorialTilemapGenerator : MonoBehaviour
{
    public bool canStartMining = false;
    private int width = 28;
    private int height = 300;
    public Tilemap tilemap;
    public TileBase dirtTile, stoneTile, minedTile,
        WaterTile0, WaterTile25, WaterTile50, WaterTile75, WaterTile100,
        FoodTile0, FoodTile25, FoodTile50, FoodTile75, FoodTile100,
        CrackTile1, CrackTile2, CrackTile3,
        SpawnTile0, SpawnTile25, SpawnTile50, SpawnTile75, SpawnTile100,
        highlightedTutorialTile;

    public float noiseScale = 0.3f; // Lower for bigger clusters
    public float stoneNoiseScale = 0.15f; // Stone uses separate noise for better clustering
    public float waterThreshold = 0.6f;
    public float stoneThreshold = 0.3f;
    
    public int minedBlockCount =0;
    public ProgressBar foodProgressBar;
    public ProgressBar waterProgressBar;
    public ProgressBar populationProgressBar;
    public ProgressBar satisfactionProgressBar;

    public Dictionary<Vector3Int, HexTileData> hexMapData = new Dictionary<Vector3Int, HexTileData>();  //important
    private int currentTutorialStage = 0;
    private Vector3Int[] tutorialTilePositions = new Vector3Int[4]; // To store the positions of tutorial tiles
    private TileBase[] originalTileTypes = new TileBase[4]; //new 
    public float population = 50;
    private float food = 1000;
    private float water = 2000;
    public TextMeshProUGUI antCountText;

    private int waterGenerator = 0;
    private int foodGenerator =0;
    private int SpawnGenerator = 0;

    //This code is to notify AntAI when the first tile has been mined.
    private bool firstBlockMined = false;
    public Vector3Int firstMinedBlockPosition;
    public TutorialAntAI antAI; // Reference to AntAI script

    private float minAntsPerBlock = 5f;
    private float maxAntsPerBlock = 30f;

    float spaceRatio=100;
    float foodRatio=100;
    float waterRatio=100;
    float satisfactionRatio = 100;

    public GameObject gameOverPanel;
    public GameObject cannotMinePanel;
    public TextMeshProUGUI gameOverText;
    private bool gameOverTriggered = false;
    public Timer time;
    public GameObject mineFirstBlockPanel;
    public GameObject mineSecondBlockPanel;
    public GameObject mineThirdBlockPanel;
    public GameObject mineFourthBlockPanel;
    public GameObject regenerateResourcesPanel;
    

    void Start()
    {
        gameOverPanel.SetActive(false);
        cannotMinePanel.SetActive(false);
        GenerateDemo();
        StartCoroutine(FillGenerators());
        time.isPaused = true;
        //StartCoroutine(UpdateResourcesCoroutine());
        UpdateAntText();
        SetupNextTutorialTile();

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
    public void startGame(){
        canStartMining = true;
        
        
    }
        // Setup the next tutorial tile to be highlighted
    private void SetupTutorialTilePositions()
    {
        // Define the positions for each tutorial stage
        // First row, specific column (change as needed)
        tutorialTilePositions[0] = new Vector3Int(6, 0, 0);
        
        // Second row, specific column
        tutorialTilePositions[1] = new Vector3Int(6, -1, 0);
        
        // Third row, specific column
        tutorialTilePositions[2] = new Vector3Int(7, -2, 0);
        tutorialTilePositions[3] = new Vector3Int(6,-3,0);
        
        // Store the original tile types
        for (int i = 0; i < tutorialTilePositions.Length; i++)
        {
            if (hexMapData.TryGetValue(tutorialTilePositions[i], out HexTileData tileData))
            {
                originalTileTypes[i] = tileData.Tile;
            }
        }
    }
    private void SetupNextTutorialTile()
    {
    // If this is the first call, set up the positions
    if (currentTutorialStage == 0 && tutorialTilePositions[0] == Vector3Int.zero)
    {
        SetupTutorialTilePositions();
    }
    
    // Ensure we don't go out of bounds
    if (currentTutorialStage < tutorialTilePositions.Length)
    {
        Vector3Int tilePos = tutorialTilePositions[currentTutorialStage];
        
        // Highlight the tutorial tile visually
        tilemap.SetTile(tilePos, highlightedTutorialTile);
        
        // Update the dictionary to match the visual state
        if (hexMapData.TryGetValue(tilePos, out HexTileData tileData))
        {
            // Store the original tile type but update the current tile reference
            originalTileTypes[currentTutorialStage] = tileData.Tile;
            tileData.Tile = highlightedTutorialTile;
            hexMapData[tilePos] = tileData;
        }
        
        Debug.Log($"Tutorial stage {currentTutorialStage + 1}: Highlighted tile at {tilePos}");
    }
}
    

    private void ProgressTutorial()
    {
        // Restore the current tutorial tile to its original appearance and update dictionary
        if (currentTutorialStage < tutorialTilePositions.Length)
        {
            Vector3Int currentPos = tutorialTilePositions[currentTutorialStage];
            tilemap.SetTile(currentPos, minedTile); // Set to mined visually
            
            // This is the key fix - update the dictionary to match the visual state
            if (hexMapData.TryGetValue(currentPos, out HexTileData tileData))
            {
                tileData.Tile = minedTile; // Update the reference in the dictionary
                hexMapData[currentPos] = tileData;
            }
        }
        if (currentTutorialStage == 1){
            mineThirdBlockPanel.SetActive(true);
            mineSecondBlockPanel.SetActive(false);
        }
        else if (currentTutorialStage == 2){
            mineThirdBlockPanel.SetActive(false);
            mineFourthBlockPanel.SetActive(true);
        }
        else if(currentTutorialStage == 3){
            mineFourthBlockPanel.SetActive(false);
            regenerateResourcesPanel.SetActive(true);
        }
        
        // Move to the next stage
        currentTutorialStage++;
        
        
        // Setup the next tile if we're not done
        if (currentTutorialStage < tutorialTilePositions.Length)
        {
            SetupNextTutorialTile();
        }
        else
        {
            Debug.Log("Tutorial completed! Normal mining can now begin.");
            // Any code for transitioning from tutorial to normal play
        }
    }
    



    
    void GenerateDemo(int seed = 1)
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
                    selectedTile = dirtTile;

                tilemap.SetTile(tilePosition, selectedTile);  //changes tile IRL
                hexMapData[tilePosition] = new HexTileData(selectedTile); //updating d

            }
            Vector3Int Food1  = new Vector3Int(5, -(3), 0); //3,4
            tilemap.SetTile(Food1, FoodTile100);
            hexMapData[Food1] = new HexTileData(FoodTile100);

            Vector3Int Spawn  = new Vector3Int(7, -(1), 0);
            tilemap.SetTile(Spawn, SpawnTile100);
            hexMapData[Spawn] = new HexTileData(SpawnTile100);

            Vector3Int Water1 = new Vector3Int(7, -(3), 0);
            tilemap.SetTile(Water1, WaterTile100);
            hexMapData[Water1] = new HexTileData(WaterTile100);
        }

        
    }

    void MineBlock(Vector3Int mouseCell)
    {
        int costToMine = GetMiningCost(mouseCell);
        bool isTutorialTile = currentTutorialStage < tutorialTilePositions.Length && 
                              mouseCell == tutorialTilePositions[currentTutorialStage];


        if (tilemap.HasTile(mouseCell) && (isTutorialTile || (CanMineTile(mouseCell) && currentTutorialStage >= tutorialTilePositions.Length)) && canStartMining)
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

                // Check for flooding
                Vector3Int[] floodNeighbors = getFloodNeighbors(mouseCell);
                foreach (Vector3Int floodNeighbor in floodNeighbors)
                {
                    if (hexMapData.TryGetValue(floodNeighbor, out HexTileData neighborTile))
                    {
                        if (CheckWaterTile(floodNeighbor))
                        {
                            tilemap.SetTile(floodNeighbor, dirtTile); // Destroy water tile
                            hexMapData[floodNeighbor].Tile = stoneTile; // Update dictionary
                            floodTiles(mouseCell);
                        }
                    }
                }

                if (!firstBlockMined)
                {
                    time.isPaused = false;
                    firstBlockMined = true;
                    StartCoroutine(UpdateResourcesCoroutine());
                    firstMinedBlockPosition = mouseCell;
                    ProgressTutorial();
                    mineFirstBlockPanel.SetActive(false);
                    mineSecondBlockPanel.SetActive(true);

                    // Notify AntAI about the first mined block
                    if (antAI != null)
                    {
                        antAI.OnFirstBlockMined(mouseCell);
                    }
                }
                else if (isTutorialTile)
                {
                    ProgressTutorial();
                }
            }
        }
    }

    // Coroutine for delayed tile updates
    private IEnumerator MineTileWithDelay(Vector3Int mouseCell)
    {
        tilemap.SetTile(mouseCell, CrackTile1);
        yield return new WaitForSeconds(0.2f);

        tilemap.SetTile(mouseCell, CrackTile2);
        yield return new WaitForSeconds(0.2f);

        tilemap.SetTile(mouseCell, CrackTile3);
        yield return new WaitForSeconds(0.2f);

        tilemap.SetTile(mouseCell, minedTile);
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

    public void floodTiles(Vector3Int cell)
    {
        tilemap.SetTile(cell, dirtTile); //destroy water tile
        hexMapData[cell].Tile = stoneTile;
        
        Vector3Int[] neighbors = GetHexNeighbors(cell);
        foreach(Vector3Int neighbor in neighbors)
        {
            tilemap.SetTile(neighbor, dirtTile);
            hexMapData[neighbor].Tile = stoneTile;
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
        if (currentTutorialStage < tutorialTilePositions.Length)
        {
            return cell == tutorialTilePositions[currentTutorialStage];
        }
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

        if (satisfactionRatio < 30 && !gameOverTriggered){
            

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

    public void PlayGame(){
        SceneManager.LoadScene("GameScene");
        
    }


}







