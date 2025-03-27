using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class AntAI : MonoBehaviour
{
    public GameObject antSpritePrefab;
    public int antAICount = 0;
    
    
    private List<GameObject> antInstances = new List<GameObject>();
    private Tilemap tilemap;
    private HexTilemapGenerator mapGenerator;

// Population count from HexTileMapGen
    private int population; 
    public float minedBlockCount;
    private Vector3Int spawnPosition;
private int previousMinedBlockCount = 0;  // Track previous mined block count
private double antsPerMinedBlock = 0.5;  // Adjustable spawn ratio
    
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float pathPlanningInterval = 0.5f;
    public float pathPlanningVariation = 0.3f;
    [Range(0, 1)]
    public float turnSmoothness = 0.5f;
    public int maxPathLength = 3; // Maximum cells to plan ahead
    
    // Track current position and target paths of each ant
    private List<Vector3Int> currentCells = new List<Vector3Int>();
    private List<Queue<Vector3Int>> antPaths = new List<Queue<Vector3Int>>();
    private List<bool> isMoving = new List<bool>();
    private List<Vector3> velocities = new List<Vector3>(); // Need to store these separately
    
    // Dictionary to track global visits to each cell
    private Dictionary<Vector3Int, int> globalVisitCounts = new Dictionary<Vector3Int, int>();
    
    // Dictionary to track which ants are currently on each cell
    private Dictionary<Vector3Int, List<int>> occupiedCells = new Dictionary<Vector3Int, List<int>>();
    
    // Dictionary to track target cells for path planning
    private Dictionary<Vector3Int, List<int>> targetCells = new Dictionary<Vector3Int, List<int>>();
    
    void Start()
    {
        mapGenerator = FindFirstObjectByType<HexTilemapGenerator>();
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
        
        // Spawn multiple ants
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
            antPaths.Add(new Queue<Vector3Int>());
            
            // Record initial occupation
            occupiedCells[cellPosition].Add(i);
            
            // Record initial visit
            RecordVisit(cellPosition);
            
            // Start path planning coroutine for this ant with staggered delay
            StartCoroutine(PlanAntPath(i, Random.Range(0f, 1f))); // Staggered start
        }
        
        //Debug.Log($"Spawned {antCount} ants at the first mined block: {cellPosition}");
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
    
    // Update is called once per frame
    void Update()
    {
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

        // Move all ants according to their paths and velocities
        for (int i = 0; i < antInstances.Count; i++)
        {
            if (antPaths[i].Count > 0 && !isMoving[i])
            {
                // Start moving to the next cell in path
                Vector3Int nextCell = antPaths[i].Dequeue();
                StartCoroutine(MoveAntToCell(i, nextCell));
            }
        }
        // Check if the number of ants exceeds the population
    int excessAnts = antInstances.Count - population;
    // if (excessAnts > 0)
    // {
    //     RemoveAnts(excessAnts);
    // }
        
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
        antPaths.Add(new Queue<Vector3Int>());

        if (!occupiedCells.ContainsKey(spawnPosition))
        {
            occupiedCells[spawnPosition] = new List<int>();
        }
        occupiedCells[spawnPosition].Add(antInstances.Count - 1);
        RecordVisit(spawnPosition);

        StartCoroutine(PlanAntPath(antInstances.Count - 1, Random.Range(0f, 1f)));
    }
}

// void RemoveAnts(int numToRemove)
// {
//     int numRemoved = 0;

//     while (numRemoved < numToRemove && antInstances.Count > 0)
//     {
//         int lastIndex = antInstances.Count - 1; // Always remove the last element

//         Debug.Log($"Removing ant at index: {lastIndex}, Total ants: {antInstances.Count}");

//         // Destroy the ant GameObject
//         Destroy(antInstances[lastIndex]);

//         // Get the cell position before removal
//         Vector3Int cellPosition = currentCells[lastIndex];

//         // Remove from all lists
//         antInstances.RemoveAt(lastIndex);
//         currentCells.RemoveAt(lastIndex);
//         isMoving.RemoveAt(lastIndex);
//         velocities.RemoveAt(lastIndex);
//         antPaths.RemoveAt(lastIndex);

//         // Ensure occupiedCells is updated correctly
//         if (occupiedCells.ContainsKey(cellPosition))
//         {
//             occupiedCells[cellPosition].Remove(lastIndex);
//             if (occupiedCells[cellPosition].Count == 0)
//             {
//                 occupiedCells.Remove(cellPosition);
//             }
//         }

//         numRemoved++;
//     }

//     Debug.Log($"Successfully removed {numRemoved} ants.");
// }
    IEnumerator PlanAntPath(int antIndex, float initialDelay)
    {
        // Initial delay to stagger ant movement
        yield return new WaitForSeconds(initialDelay);
        
        // Create a unique planning interval for each ant
        float individualPlanningInterval = pathPlanningInterval + Random.Range(-pathPlanningVariation, pathPlanningVariation);
        
        while (true)
        {
            // Only plan new paths when the ant isn't moving or has few planned steps left
            if (!isMoving[antIndex] || antPaths[antIndex].Count < 1)
            {
                Vector3Int currentCell = currentCells[antIndex];
                
                // Plan a multi-step path
                PlanPathForAnt(antIndex, currentCell);
            }
            
            yield return new WaitForSeconds(individualPlanningInterval);
        }
    }
    
    void PlanPathForAnt(int antIndex, Vector3Int startCell)
    {
        // Clear any existing path
        if (isMoving[antIndex])
        {
            // Don't clear the path if the ant is already moving
            return;
        }
        
        // Clear existing target cell reservations for this ant
        foreach (var kvp in targetCells)
        {
            if (kvp.Value.Contains(antIndex))
            {
                kvp.Value.Remove(antIndex);
            }
        }
        
        Queue<Vector3Int> newPath = new Queue<Vector3Int>();
        Vector3Int currentPathCell = startCell;
        
        // Plan up to maxPathLength steps ahead
        for (int step = 0; step < maxPathLength; step++)
        {
            List<Vector3Int> availableNeighbors = GetAvailableNeighbors(currentPathCell);
            
            // If no neighbors available, stop planning
            if (availableNeighbors.Count == 0)
                break;
                
            // Choose next cell based on exploration and ant distribution
            Vector3Int nextCell = ChooseBestCell(antIndex, availableNeighbors);
            
            // Reserve this cell for path planning purposes
            if (!targetCells.ContainsKey(nextCell))
            {
                targetCells[nextCell] = new List<int>();
            }
            targetCells[nextCell].Add(antIndex);
            
            // Add to path
            newPath.Enqueue(nextCell);
            
            // Update for next iteration
            currentPathCell = nextCell;
        }
        
        // Set the new path
        antPaths[antIndex] = newPath;
    }
    
    Vector3Int ChooseBestCell(int antIndex, List<Vector3Int> neighbors)
    {
        // Create a score for each cell based on exploration and ant distribution
        Dictionary<Vector3Int, float> cellScores = new Dictionary<Vector3Int, float>();
        
        float maxVisitCount = 1f;
        float maxOccupationCount = 1f;
        float maxTargetCount = 1f;
        
        // Find maximum values to normalize scores
        foreach (Vector3Int cell in neighbors)
        {
            int visitCount = GetVisitCount(cell);
            int occupationCount = occupiedCells.ContainsKey(cell) ? occupiedCells[cell].Count : 0;
            int targetCount = targetCells.ContainsKey(cell) ? targetCells[cell].Count : 0;
            
            if (visitCount > maxVisitCount) maxVisitCount = visitCount;
            if (occupationCount > maxOccupationCount) maxOccupationCount = occupationCount;
            if (targetCount > maxTargetCount) maxTargetCount = targetCount;
        }
        
        // Score each cell
        foreach (Vector3Int cell in neighbors)
        {
            int visitCount = GetVisitCount(cell);
            int occupationCount = occupiedCells.ContainsKey(cell) ? occupiedCells[cell].Count : 0;
            int targetCount = targetCells.ContainsKey(cell) ? targetCells[cell].Count : 0;
            
            // Lower scores are better
            float explorationScore = visitCount / maxVisitCount;
            float distributionScore = occupationCount / maxOccupationCount;  
            float targetScore = targetCount / maxTargetCount;
            
            // Weight exploration vs distribution vs target avoidance
            float totalScore = (explorationScore * 0.5f) + (distributionScore * 0.3f) + (targetScore * 0.2f);
            
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
    isMoving[antIndex] = true;
    
    Vector3Int currentCell = currentCells[antIndex];
    
    occupiedCells[currentCell].Remove(antIndex);
    
    if (!occupiedCells.ContainsKey(targetCell))
    {
        occupiedCells[targetCell] = new List<int>();
    }
    
    Vector3 startPosition = antInstances[antIndex].transform.position;
    Vector3 targetPosition = tilemap.GetCellCenterWorld(targetCell);
    
    Vector3 direction = targetPosition - startPosition;
    
    targetPosition += new Vector3(
        Random.Range(-0.05f, 0.05f),
        Random.Range(-0.05f, 0.05f),
        0
    );
    
    Vector3 currentVelocity = velocities[antIndex];
    
    float smoothTime = 0.1f * (1 - turnSmoothness);
    
    while (Vector3.Distance(antInstances[antIndex].transform.position, targetPosition) > 0.01f)
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
    
    currentCells[antIndex] = targetCell;
    
    occupiedCells[targetCell].Add(antIndex);
    
    if (targetCells.ContainsKey(targetCell) && targetCells[targetCell].Contains(antIndex))
    {
        targetCells[targetCell].Remove(antIndex);
    }
    
    RecordVisit(targetCell);
    
    isMoving[antIndex] = false;
    
    if (antPaths[antIndex].Count > 0)
    {
        Vector3Int nextCell = antPaths[antIndex].Peek();
        if (IsCellSafe(nextCell))
        {
            nextCell = antPaths[antIndex].Dequeue();
            StartCoroutine(MoveAntToCell(antIndex, nextCell));
        }
    }
}


    
    bool IsCellSafe(Vector3Int cell)
    {
        // Check if any ants are currently transitioning to this cell
        if (targetCells.ContainsKey(cell) && targetCells[cell].Count > 0)
            return false;
            
        // Check current occupation but allow some stacking for more natural movement
        int currentOccupants = occupiedCells.ContainsKey(cell) ? occupiedCells[cell].Count : 0;
        return currentOccupants < 2; // Allow up to 2 ants per cell
    }
    
    List<Vector3Int> GetAvailableNeighbors(Vector3Int cell)
    {
        List<Vector3Int> availableNeighbors = new List<Vector3Int>();
        Vector3Int[] neighbors = mapGenerator.GetHexNeighbors(cell);
        
        foreach (Vector3Int neighbor in neighbors)
        {
            if (mapGenerator.IsTileMined(neighbor))
            {
                availableNeighbors.Add(neighbor);
            }
        }
        
        return availableNeighbors;
    }
}