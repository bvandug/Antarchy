using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TileTooltip : MonoBehaviour
{
    public HexTilemapGenerator generator;
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText; 
    public Camera cam;

    void Start()
    {
        cam = Camera.main;
        tooltipPanel.SetActive(true);
        

    }

    void Update()
{
    Plane tilemapPlane = new Plane(Vector3.forward, Vector3.zero); // Match the mining function
    Ray ray = cam.ScreenPointToRay(Input.mousePosition);

    
    

    if (tilemapPlane.Raycast(ray, out float enter))
    {
        Vector3 worldPos = ray.GetPoint(enter);
        

        Vector3Int cellPos = generator.tilemap.WorldToCell(worldPos);
        

        if (generator.hexMapData.TryGetValue(cellPos, out HexTileData tileData))
        {
            int cost = generator.GetMiningCost(cellPos);
            tooltipPanel.SetActive(true);
            tooltipText.text = $"Cost to Mine: {cost} ants";
            
            return;
        }

    tooltipPanel.SetActive(false);
}



  }  }

