using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;


public class AttackManager : MonoBehaviour
{
    private Tilemap tilemap;
    private HexTilemapGenerator mapGenerator;
    private int attackCount = 0;
    public bool gameOver = false;
    private int nextAttackType;
    private int nextAntsKilled;

    public TextMeshProUGUI populationNeeded;
    public TextMeshProUGUI attackInfo;
    public TextMeshProUGUI remainingPopulation;
    public TextMeshProUGUI attackDamage;
    public TileBase toxic;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       mapGenerator = FindFirstObjectByType<HexTilemapGenerator>(); 
       tilemap = mapGenerator.tilemap;
       DecideNextAttack();

    }

    public void DecideNextAttack(){
        attackCount++;
        if (attackCount == 1) {
            nextAntsKilled = 25;
            nextAttackType = 5;
        }if (attackCount == 2)
        {
            nextAntsKilled = 50;
            nextAttackType = 5;
        }
        else
        {
            nextAntsKilled = 1 * attackCount * attackCount;
            nextAttackType = UnityEngine.Random.Range(1, 6);
        }
        populationNeeded.text = string.Format("Population Needed: "+ nextAntsKilled + " ants");
        
        attackInfo.text = string.Format("Next Attacker: "+ GetAttackName(nextAttackType)+" "+ GetAttackDamage(nextAttackType));

    }

    public void ExecuteAttack(){
        if (gameOver) return;
        TriggerAttack(nextAttackType);

        if(nextAntsKilled > mapGenerator.population){
            Debug.Log("Not enough ants, Game Over");
            gameOver = true;
            mapGenerator.TriggerGameOver("Not enough ants to withstand attack!");

        }else {
            mapGenerator.population -= nextAntsKilled;
            remainingPopulation.text = string.Format(GetAttackName(nextAttackType)+ "killed " + nextAntsKilled + " ants");
            Debug.Log("Population after attack: " + mapGenerator.population + " ants");


            // Trigger the selected attack
            

        }
        DecideNextAttack();
    }

    private void TriggerAttack(int attackType){
        switch (attackType)
        {
            case 1: break;
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
            case 5: return "The Terminator";
            default: return "Unknown Attack";
        }
    }

    private string GetAttackDamage(int attackType)
    {
        switch (attackType)
        {
            case 1: return "Gets hungry";
            case 2: return "Loves the taste of Ants ";
            case 3: return "Loves the taste of Eggs";
            case 4: return "Gets thirsty";
            case 5: return "Has poison on hand";
            default: return "Unknown Attack";
        }
    }


    
    /*
    void termiteAttack() 
    {
        attackDamage.text = string.Format("The Termite ate all your food from your food supplies");
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
    }*/

    void antEaterAttack()
    {
        attackDamage.text = string.Format("The antEater ate an extra "+ nextAntsKilled+ " ants");
        mapGenerator.population -= nextAntsKilled;


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
        attackDamage.text = string.Format("The Lizard drank all your water from your water supplies");
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
        attackDamage.text = string.Format("The Exterminator poisened your food and water for the next 10 seconds");
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
    }


    

