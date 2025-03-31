using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Collections;

public class TutorialAntAI : MonoBehaviour
{
    public GameObject antSpritePrefab;
    public int antAICount = 0;
    
    private List<GameObject> antInstances = new List<GameObject>();
    public Tilemap tilemap;
    public TutorialTilemapGenerator mapGenerator;

    // Population count from HexTileMapGen
    private int population; 
    public float minedBlockCount;
    private Vector3Int spawnPosition;
    private int previousMinedBlockCount = 0;  // Track previous mined block count
    private double antsPerMinedBlock = 0.5;  // Adjustable spawn ratio
    
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float moveInterval = 0.5f;
    public float moveIntervalVariation = 0.3f;
    [Range(0, 1)]
    public float turnSmoothness = 0.5f;
    
    // Track current position of each ant
    private List<Vector3Int> currentCells = new List<Vector3Int>();
    private List<bool> isMoving = new List<bool>();
    private List<Vector3> velocities = new List<Vector3>(); // Store velocities separately
    
    // Dictionary to track global visits to each cell
    private Dictionary<Vector3Int, int> globalVisitCounts = new Dictionary<Vector3Int, int>();
    
    // Dictionary to track which ants are currently on each cell
    private Dictionary<Vector3Int, List<int>> occupiedCells = new Dictionary<Vector3Int, List<int>>();
    
    void Start()
    {
        mapGenerator = FindFirstObjectByType<TutorialTilemapGenerator>();
        tilemap = mapGenerator.tilemap;
        population = (int)mapGenerator.population;
        minedBlockCount = mapGenerator.minedBlockCount;
    }

    public void OnFirstBlockMined(Vector3Int cellPosition)
    {
        spawnPosition = cellPosition;
        Vector3 worldPosition = tilemap.GetCellCenterWorld(cellPosition);
        
        // Initialize occupation tracking for first cell
        occupiedCells[cellPosition] = new List<int>();
        
        // Spawn initial ant
        for (int i = 0; i < 1; i++)
        {
            // Create ant with slight position variation
            Vector3 spawnPosition = worldPosition + new Vector3(
                Random.Range(-0.1f, 0.1f),
                Random.Range(-0.1f, 0.1f),
                0
            );
            
            GameObject ant = Instantiate(antSpritePrefab, spawnPosition, Quaternion.identity);
            ant.name = "Ant_" + i;
            ant.transform.parent = this.transform;
            
            // Add to our lists
            antInstances.Add(ant);
            currentCells.Add(cellPosition);
            isMoving.Add(false);
            velocities.Add(Vector3.zero);
            
            // Record initial occupation
            occupiedCells[cellPosition].Add(i);
            
            // Record initial visit
            RecordVisit(cellPosition);
            
            // Start ant movement coroutine with staggered delay
            StartCoroutine(MoveAntRoutine(i, Random.Range(0f, 1f)));
        }
    }
    
    void RecordVisit(Vector3Int cell)
    {
        if (globalVisitCounts.ContainsKey(cell))
        {
            globalVisitCounts[cell]++;
        }
        else
        {
            globalVisitCounts[cell] = 1;
        }
    }
    
    int GetVisitCount(Vector3Int cell)
    {
        if (globalVisitCounts.ContainsKey(cell))
        {
            return globalVisitCounts[cell];
        }
        return 0;
    }
    
    void Update()
{
    tilemap = mapGenerator.tilemap;
    minedBlockCount = mapGenerator.minedBlockCount;
    population = (int)mapGenerator.population;
    
    // Check if more blocks have been mined
    if (minedBlockCount > previousMinedBlockCount)
    {
        int newAntsToSpawn = (int)((minedBlockCount - previousMinedBlockCount) * antsPerMinedBlock);
        SpawnAdditionalAnts(newAntsToSpawn);
        if (newAntsToSpawn >= 1) {
            previousMinedBlockCount = (int)minedBlockCount; // Update the tracked value
        }
    }
    
    // NEW CODE: Check if we need to delete excess ants
    RemoveExcessAnts();
}

// NEW FUNCTION: Delete excess ants if there are more ants than the population amount
void RemoveExcessAnts()
{
    // Count active ant instances (non-null)
    int activeAntCount = 0;
    for (int i = 0; i < antInstances.Count; i++)
    {
        if (antInstances[i] != null)
        {
            activeAntCount++;
        }
    }
    
    // If there are more ants than the population allows
    if (activeAntCount > population && population >= 0)
    {
        int excessAnts = activeAntCount - population;
        int antsRemoved = 0;
        
        // Start from the end of the list to remove the newest ants first
        for (int i = antInstances.Count - 1; i >= 0 && antsRemoved < excessAnts; i--)
        {
            if (antInstances[i] != null)
            {
                DeleteAnt(i);
                antsRemoved++;
            }
        }
    }
}
    
    void SpawnAdditionalAnts(int antCount)
    {
        if (spawnPosition == null) return;

        Vector3 worldPosition = tilemap.GetCellCenterWorld(spawnPosition);

        for (int i = 0; i < antCount; i++)
        {
            Vector3 spawnPos = worldPosition + new Vector3(
                Random.Range(-0.1f, 0.1f),
                Random.Range(-0.1f, 0.1f),
                0
            );

            GameObject ant = Instantiate(antSpritePrefab, spawnPos, Quaternion.identity);
            ant.name = "Ant_" + (antInstances.Count + i);
            ant.transform.parent = this.transform;

            antInstances.Add(ant);
            currentCells.Add(spawnPosition);
            isMoving.Add(false);
            velocities.Add(Vector3.zero);

            if (!occupiedCells.ContainsKey(spawnPosition))
            {
                occupiedCells[spawnPosition] = new List<int>();
            }
            occupiedCells[spawnPosition].Add(antInstances.Count - 1);
            RecordVisit(spawnPosition);

            StartCoroutine(MoveAntRoutine(antInstances.Count - 1, Random.Range(0f, 1f)));
        }
    }

    IEnumerator MoveAntRoutine(int antIndex, float initialDelay)
    {
        // Initial delay to stagger ant movement
        yield return new WaitForSeconds(initialDelay);
        
        // Create a unique movement interval for each ant
        float individualMoveInterval = moveInterval + Random.Range(-moveIntervalVariation, moveIntervalVariation);
        
        while (true)
        {
            // First check if current cell is still a mined block
            if (antIndex < antInstances.Count && antInstances[antIndex] != null)
            {
                Vector3Int currentCell = currentCells[antIndex];
                
                // Check if the current block is no longer a mined block
                if (!mapGenerator.IsTileMined(currentCell))
                {
                    // Delete ant if its current block changed from mined to dirt
                    DeleteAnt(antIndex);
                    yield break; // Exit the coroutine
                }
                
                // Only move when the ant isn't already moving
                if (!isMoving[antIndex])
                {
                    // Find best neighboring cell to move to
                    Vector3Int nextCell = FindBestNeighborCell(antIndex, currentCell);
                    
                    // Move to that cell if one was found and it's different from the current cell
                    if (nextCell != currentCell)
                    {
                        StartCoroutine(MoveAntToCell(antIndex, nextCell));
                    }
                }
            }
            else
            {
                yield break; // Exit if ant has been destroyed
            }
            
            yield return new WaitForSeconds(individualMoveInterval);
        }
    }
    
    void DeleteAnt(int antIndex)
    {
        if (antIndex >= antInstances.Count || antInstances[antIndex] == null)
            return;
            
        // Get current cell to update occupation tracking
        Vector3Int currentCell = currentCells[antIndex];
        
        // Remove from occupied cells dictionary
        if (occupiedCells.ContainsKey(currentCell) && occupiedCells[currentCell].Contains(antIndex))
        {
            occupiedCells[currentCell].Remove(antIndex);
        }
        
        // Destroy the GameObject
        Destroy(antInstances[antIndex]);
        
        // Option 1: Keep the ant in the lists but mark as null
        antInstances[antIndex] = null;
        isMoving[antIndex] = false;
        
        // Option 2 (alternative): Remove the ant from all lists
        // This is more complex and requires updating all ant indices
        // Not implemented here to avoid complexity
    }
    
    Vector3Int FindBestNeighborCell(int antIndex, Vector3Int currentCell)
    {
        List<Vector3Int> availableNeighbors = GetAvailableNeighbors(currentCell);
        
        // If no neighbors available, stay in current cell
        if (availableNeighbors.Count == 0)
            return currentCell;
            
        // Choose best cell based on exploration and ant distribution
        Vector3Int bestCell = ChooseBestCell(antIndex, availableNeighbors);
        
        return bestCell;
    }
    
    Vector3Int ChooseBestCell(int antIndex, List<Vector3Int> neighbors)
    {
        // Create a score for each cell based on exploration and ant distribution
        Dictionary<Vector3Int, float> cellScores = new Dictionary<Vector3Int, float>();
        
        float maxVisitCount = 1f;
        float maxOccupationCount = 1f;
        
        // Find maximum values to normalize scores
        foreach (Vector3Int cell in neighbors)
        {
            int visitCount = GetVisitCount(cell);
            int occupationCount = occupiedCells.ContainsKey(cell) ? occupiedCells[cell].Count : 0;
            
            if (visitCount > maxVisitCount) maxVisitCount = visitCount;
            if (occupationCount > maxOccupationCount) maxOccupationCount = occupationCount;
        }
        
        // Score each cell
        foreach (Vector3Int cell in neighbors)
        {
            int visitCount = GetVisitCount(cell);
            int occupationCount = occupiedCells.ContainsKey(cell) ? occupiedCells[cell].Count : 0;
            
            // Lower scores are better
            float explorationScore = visitCount / maxVisitCount;
            float distributionScore = occupationCount / maxOccupationCount;  
            
            // Weight exploration vs distribution
            float totalScore = (explorationScore * 0.6f) + (distributionScore * 0.4f);
            
            cellScores[cell] = totalScore;
        }
        
        // Find cells with the lowest scores (best options)
        float minScore = float.MaxValue;
        List<Vector3Int> bestCells = new List<Vector3Int>();
        
        foreach (KeyValuePair<Vector3Int, float> pair in cellScores)
        {
            if (pair.Value < minScore)
            {
                minScore = pair.Value;
                bestCells.Clear();
                bestCells.Add(pair.Key);
            }
            else if (Mathf.Approximately(pair.Value, minScore))
            {
                bestCells.Add(pair.Key);
            }
        }
        
        // Choose randomly among the best options
        return bestCells[Random.Range(0, bestCells.Count)];
    }
    
    IEnumerator MoveAntToCell(int antIndex, Vector3Int targetCell)
    {
        if (antIndex >= antInstances.Count || antInstances[antIndex] == null)
            yield break;
            
        isMoving[antIndex] = true;
        
        Vector3Int currentCell = currentCells[antIndex];
        
        // Remove ant from current cell's occupancy list
        if (occupiedCells.ContainsKey(currentCell))
        {
            occupiedCells[currentCell].Remove(antIndex);
        }
        
        // Initialize target cell's occupancy list if needed
        if (!occupiedCells.ContainsKey(targetCell))
        {
            occupiedCells[targetCell] = new List<int>();
        }
        
        Vector3 startPosition = antInstances[antIndex].transform.position;
        Vector3 targetPosition = tilemap.GetCellCenterWorld(targetCell);
        
        Vector3 direction = targetPosition - startPosition;
        
        // Add a small random offset to prevent perfect stacking
        targetPosition += new Vector3(
            Random.Range(-0.05f, 0.05f),
            Random.Range(-0.05f, 0.05f),
            0
        );
        
        Vector3 currentVelocity = velocities[antIndex];
        
        float smoothTime = 0.1f * (1 - turnSmoothness);
        
        while (antInstances[antIndex] != null && 
               Vector3.Distance(antInstances[antIndex].transform.position, targetPosition) > 0.01f)
        {
            antInstances[antIndex].transform.position = Vector3.SmoothDamp(
                antInstances[antIndex].transform.position,
                targetPosition,
                ref currentVelocity,
                smoothTime,
                moveSpeed
            );
            
            velocities[antIndex] = currentVelocity;
            
            if (direction.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                
                angle += -180f;
                
                Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
                antInstances[antIndex].transform.rotation = Quaternion.Slerp(
                    antInstances[antIndex].transform.rotation,
                    targetRotation,
                    10f * Time.deltaTime
                );
            }
            
            yield return null;
        }
        
        // Check if the ant still exists
        if (antIndex < antInstances.Count && antInstances[antIndex] != null)
        {
            // Update current cell and occupancy tracking
            currentCells[antIndex] = targetCell;
            occupiedCells[targetCell].Add(antIndex);
            RecordVisit(targetCell);
            
            isMoving[antIndex] = false;
        }
    }
    
    List<Vector3Int> GetAvailableNeighbors(Vector3Int cell)
    {
        List<Vector3Int> availableNeighbors = new List<Vector3Int>();
        Vector3Int[] neighbors = mapGenerator.GetHexNeighbors(cell);
        
        foreach (Vector3Int neighbor in neighbors)
        {
            if (mapGenerator.IsTileMined(neighbor))
            {
                // Don't go to cells that are too crowded
                int currentOccupants = occupiedCells.ContainsKey(neighbor) ? occupiedCells[neighbor].Count : 0;
                if (currentOccupants < 2) // Allow up to 2 ants per cell
                {
                    availableNeighbors.Add(neighbor);
                }
            }
        }
        
        return availableNeighbors;
    }
}