using UnityEngine;
using UnityEngine.Tilemaps; // Add this import for Tilemap

public class AntAI : MonoBehaviour
{
    public GameObject antSpritePrefab; // Assign your ant sprite prefab in the Inspector
    private GameObject antInstance;
    private Tilemap tilemap; // Reference to the same tilemap
    
    void Start()
    {
        // Get reference to the tilemap
        tilemap = FindFirstObjectByType<HexTilemapGenerator>().tilemap;
    }
    
    public void OnFirstBlockMined(Vector3Int cellPosition)
    {
        // Convert cell position to world position
        Vector3 worldPosition = tilemap.GetCellCenterWorld(cellPosition);
        
        // Instantiate the ant sprite at the mined block position
        antInstance = Instantiate(antSpritePrefab, worldPosition, Quaternion.identity);
        
        // You can parent the ant to this object if needed
        antInstance.transform.parent = this.transform;
        
        Debug.Log("Ant spawned at the first mined block: " + cellPosition);
    }
}