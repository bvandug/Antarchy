using System;
using UnityEngine;
using UnityEngine.Tilemaps;


public class AttackManager : MonoBehaviour
{
    private Tilemap tilemap;
    private HexTilemapGenerator mapGenerator;
    private int attackCount = 0;
    public Boolean gameOver = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       mapGenerator = FindFirstObjectByType<HexTilemapGenerator>(); 
       tilemap = mapGenerator.tilemap;

    }

    // Update is called once per frame
   
    // This function is called when the timer runs out
    // This kills some of the ant population and triggers a random attack
    public void PopulationKilled()
    {while (gameOver==false){
        attackCount++;
        int antsKilled = 5*attackCount*attackCount;

        Debug.Log("Population before attack " + mapGenerator.population);
        
        if (antsKilled > mapGenerator.population){
            Debug.Log("Not enough ants, Game Over");
            gameOver = true;
            mapGenerator.TriggerGameOver("Not enough ants to withstand attack!");

        }else {
            mapGenerator.population -= antsKilled;
        }
        Debug.Log("Population after attack " + mapGenerator.population);

        //Randomly trigger different attacks
        int randomAttack = UnityEngine.Random.Range(1, 7); // Generates a number between 1 and 6
        Debug.Log(randomAttack);
        switch (randomAttack)
        {
            case 1:
                termiteAttack();
                break;
            case 2:
                antEaterAttack();
                break;
            case 3:
                spiderAttack();
                break;
            case 4:
                lizardAttack();
                break;
            case 5:
                snakeAttack();
                break;
            case 6:
                terminatorAttack();
                break;
        }

    }}

    void termiteAttack() 
    {
        Debug.Log("Attack1");
    }

    void antEaterAttack()
    {
        Debug.Log("Attack2");
    }

    void spiderAttack()
    {
        Debug.Log("Attack3");
    }

    void lizardAttack()
    {
        Debug.Log("Attack4");
    }

    void snakeAttack()
    {
        Debug.Log("Attack5");
    }

    void terminatorAttack()
    {
        Debug.Log("Attack6");
    }

    
}
