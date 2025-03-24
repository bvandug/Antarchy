using UnityEngine;
using UnityEngine.Tilemaps;


public class AttackManager : MonoBehaviour
{
    private Tilemap tilemap;
    private HexTilemapGenerator mapGenerator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       mapGenerator = FindFirstObjectByType<HexTilemapGenerator>(); 
       tilemap = mapGenerator.tilemap;

    }

    // Update is called once per frame
   
    // This function is called when the timer runs out
    // This kills some of the ant population and triggers a random attack
    public void populationKilled()
    {
        Debug.Log("Population before attack " + mapGenerator.population);
        mapGenerator.population = mapGenerator.population/2;
        Debug.Log("Population after attack " + mapGenerator.population);

        //Randomly trigger different attacks
        int randomAttack = Random.Range(1, 7); // Generates a number between 1 and 6
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

    }

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
