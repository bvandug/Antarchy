using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;


public class AttackManager : MonoBehaviour
{
    private Tilemap tilemap;
    private HexTilemapGenerator mapGenerator;
    private TutorialTilemapGenerator tutorialMapGenerator;
    private int attackCount = 0;
    public bool gameOver = false;
    public int nextAttackType;
    private int nextAntsKilled;

    public TextMeshProUGUI populationNeeded;
    public TextMeshProUGUI populationNeeded1;
    public TextMeshProUGUI attackerName;
    public TextMeshProUGUI remainingPopulation;
    public TextMeshProUGUI attackDamage;
    public TextMeshProUGUI attackHint;
    private List<int> availableNumbers;
    private int previousNumber = -1;
    private int currentNumber = -1;
    public TileBase toxic;

    public GameObject AntEater1;
    public GameObject Termite1;
    public GameObject Lizard1;
    public GameObject Exterminator1;
    public GameObject Spider1;

    public GameObject AntEater2;
    public GameObject Termite2;
    public GameObject Lizard2;
    public GameObject Exterminator2;
    public GameObject Spider2;

    public GameObject AntEater3;
    public GameObject Termite3;
    public GameObject Lizard3;
    public GameObject Exterminator3;
    public GameObject Spider3;
    
    

    void Start()
    {
        mapGenerator = FindFirstObjectByType<HexTilemapGenerator>();
        if (mapGenerator == null)
        {
            Debug.LogWarning("HexTilemapGenerator not found. Checking for TutorialTilemapGenerator...");
            tutorialMapGenerator = FindFirstObjectByType<TutorialTilemapGenerator>();
            tilemap = tutorialMapGenerator.tilemap;
        }
        else { tilemap = mapGenerator.tilemap; }


        ResetAvailableNumbers();
        for (int i=1; i < 6; i++){
            hideAttackerImage(i);
        }
        DecideNextAttack();    
    }
    

    public void DecideNextAttack(){
        attackCount++;
        nextAntsKilled = 5 * attackCount * attackCount;
        nextAttackType = GetNextNumber();
        showAttackerImage(nextAttackType);
        if (nextAttackType == 2){
            nextAntsKilled = 2*nextAntsKilled;
        }
        populationNeeded.text = string.Format("Population Needed: "+ nextAntsKilled + " ants");
        populationNeeded1.text = string.Format(nextAntsKilled + " ants");
        attackerName.text = string.Format("Next Attacker: "+ GetAttackName(nextAttackType));
        attackHint.text = string.Format("Hint: "+ GetAttackDamage(nextAttackType));
        

    }

    public void showAttackerImage(int attackType){
        switch (attackType)
        {
            case 1: 
                Termite1.SetActive(true);
                Termite2.SetActive(true);
                Termite3.SetActive(true);
                break;
            case 2: 
                AntEater1.SetActive(true);
                AntEater2.SetActive(true);
                AntEater3.SetActive(true);
                break;
            case 3: 
                Spider1.SetActive(true);
                Spider2.SetActive(true);
                Spider3.SetActive(true);
                break;
            case 4: 
                Lizard1.SetActive(true);
                Lizard2.SetActive(true);
                Lizard3.SetActive(true);
                break;
            case 5: 
                Exterminator1.SetActive(true);
                Exterminator2.SetActive(true);
                Exterminator3.SetActive(true);
                break;
            default: break;
        }

    }

    public void hideAttackerImage(int attackType){
        switch (attackType)
        {
            case 1: 
                Termite1.SetActive(false);
                Termite2.SetActive(false);
                Termite3.SetActive(false);
                break;
            case 2: 
                AntEater1.SetActive(false);
                AntEater2.SetActive(false);
                AntEater3.SetActive(false);
                break;
            case 3: 
                Spider1.SetActive(false);
                Spider2.SetActive(false);
                Spider3.SetActive(false);
                break;
            case 4: 
                Lizard1.SetActive(false);
                Lizard2.SetActive(false);
                Lizard3.SetActive(false);
                break;
            case 5: 
                Exterminator1.SetActive(false);
                Exterminator2.SetActive(false);
                Exterminator3.SetActive(false);
                break;
            default: break;
        }

    }

    public void ExecuteAttack(){
        if (gameOver) return;
        TriggerAttack(nextAttackType);

        if(nextAntsKilled >= mapGenerator.population){
            Debug.Log("Not enough ants, Game Over");
            gameOver = true;
            mapGenerator.TriggerGameOver("Not enough ants to withstand attack!");

        }else {
            mapGenerator.population -= nextAntsKilled;
            remainingPopulation.text = string.Format(GetAttackName(nextAttackType)+ " killed " + nextAntsKilled + " ants");
            Debug.Log("Population after attack: " + mapGenerator.population + " ants");


            // Trigger the selected attack
            

        }
        //DecideNextAttack();
    }

    private void TriggerAttack(int attackType){
        switch (attackType)
        {
            case 1: termiteAttack(); break;
            case 2: antEaterAttack(); break;
            case 3: spiderAttack(); break;
            case 4: lizardAttack(); break;
            case 5: terminatorAttack(); break;

    }}

    private string GetAttackName(int attackType)
    {
        switch (attackType)
        {
            case 1: return "The Termite";
            case 2: return "The Ant Eater";
            case 3: return "The Spider";
            case 4: return "The Lizard";
            case 5: return "The Exterminator";
            default: return "Unknown Attack";
        }
    }

    private string GetAttackDamage(int attackType)
    {
        switch (attackType)
        {
            case 1: return "Termites follow the same diet";
            case 2: return "The Anteater eats DOUBLE";
            case 3: return "Loves the taste of eggs";
            case 4: return "The Lizard is feeling thirsty";
            case 5: return "The Exterminator has poison on hand";
            default: return "Unknown Attack";
        }
    }


    
    void termiteAttack() 
    {
        attackDamage.text = string.Format("The Termite ate all the food from your food supplies");
        foreach (var kvp in mapGenerator.hexMapData)
    {
            HexTileData tileData = kvp.Value;
            Vector3Int TilePos = kvp.Key;
        
            if (mapGenerator.CheckFoodTile(TilePos) && tileData.IsActivated)
            {
                tileData.FillLevel = 0; // Reset FillLevel
                Debug.Log($"Food tile at {kvp.Key} FillLevel set to 0.");
        }}


        Debug.Log("Attack1");
    }

    void antEaterAttack()
    {
        attackDamage.text = string.Format("The antEater ate an extra "+ nextAntsKilled/2 + " ants");
        //mapGenerator.population -= nextAntsKilled;


        Debug.Log("Attack2");
    }

    void spiderAttack()
    {
        attackDamage.text = string.Format("The Spider ate all your ant eggs");
        Debug.Log("Attack3");
        foreach (var kvp in mapGenerator.hexMapData)
    {
            HexTileData tileData = kvp.Value;
            Vector3Int TilePos = kvp.Key;

        // Check if it's an active spawn tile
            if (mapGenerator.CheckSpawnTile(TilePos) && tileData.IsActivated)
            {
                tileData.FillLevel = 0; // Reset FillLevel
                Debug.Log($"Spawn tile at {kvp.Key} FillLevel set to 0.");
        }
    }
    }

    void lizardAttack()
    {
        attackDamage.text = string.Format("The Lizard drank all the water from your water supplies");
        Debug.Log("Attack4");
        foreach (var kvp in mapGenerator.hexMapData)
    {
            HexTileData tileData = kvp.Value;
            Vector3Int TilePos = kvp.Key;

        // Check if it's an active water tile
            if (mapGenerator.CheckWaterTile(TilePos) && tileData.IsActivated)
            {
                tileData.FillLevel = 0; // Reset FillLevel
                Debug.Log($"Water tile at {kvp.Key} FillLevel set to 0.");
        }
    }
    }


    void terminatorAttack()
    {
        attackDamage.text = string.Format("The Exterminator poisoned your food and water for the next 10 seconds");
        Debug.Log("Attack6");
        foreach (var kvp in mapGenerator.hexMapData)
    {
        HexTileData tileData = kvp.Value;
        Vector3Int TilePos = kvp.Key;

        // Disable all water, food, and spawn tiles
        if (mapGenerator.CheckWaterTile(TilePos) && tileData.IsActivated || mapGenerator.CheckFoodTile(TilePos ) && tileData.IsActivated)

        {
            tileData.IsDisabled = true; // Mark as disabled
            tilemap.SetTile(TilePos, toxic);
            Debug.Log($"Tile at {kvp.Key} is now disabled.");
        }
    }

    // Start coroutine to re-enable after 10 seconds
    StartCoroutine(ReenableResourceTiles());
}

private IEnumerator ReenableResourceTiles()
{
    yield return new WaitForSeconds(10f);

    foreach (var kvp in mapGenerator.hexMapData)
    {
        HexTileData tileData = kvp.Value;

        if (tileData.IsDisabled)
        {
            tileData.IsDisabled = false;
            Debug.Log($"Tile at {kvp.Key} is now re-enabled.");
        }
    }

    Debug.Log("Exterminator Attack is over! You can use resources again.");
}


public int GetNextNumber()
    {
        // If we've used all numbers, reset the pool
        if (availableNumbers.Count == 0)
        {
            ResetAvailableNumbers();
            
            // Make sure we don't pick the same number twice in a row
            // when starting a new cycle
            if (availableNumbers.Contains(previousNumber))
            {
                availableNumbers.Remove(previousNumber);
                
                // Edge case: if we removed the last number, add it back
                // and just accept the repeat (can't avoid it with only 1 number)
                if (availableNumbers.Count == 0)
                {
                    availableNumbers.Add(previousNumber);
                }
            }
        }
        
        // Get random index from remaining available numbers
        int randomIndex = UnityEngine.Random.Range(0, availableNumbers.Count);
        
        // Get the number at that index
        currentNumber = availableNumbers[randomIndex];
        
        // Remove the used number from the pool
        availableNumbers.RemoveAt(randomIndex);
        
        // Update previous number
        previousNumber = currentNumber;
        
        return currentNumber;
    }
    private void ResetAvailableNumbers()
    {
        // Refill the pool with numbers 1-5
        availableNumbers = new List<int> { 1, 2, 3, 4, 5 };
    }
    }




    

