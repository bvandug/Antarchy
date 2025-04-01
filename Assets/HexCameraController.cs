using UnityEngine;

public class HexCameraController : MonoBehaviour
{
    public Camera cam;
    private float scrollSpeed = 10f;  
    private float tileHeight = 0.75f; 
    private int gridHeight = 300;      

    private float minY;
    private float maxY;
    private bool scrollingEnabled = true;

    void Start()
    {
        if (cam == null)
        {
            cam = Camera.main; 
        }

        if (FindFirstObjectByType<TutorialTilemapGenerator>() != null)
        {
            scrollingEnabled = false; // Disable scrolling for tutorial maps
            Debug.Log("Tutorial map detected. Scrolling disabled.");
        }

        minY = 0f;
        maxY = -(gridHeight) * tileHeight;

        CenterCamera();
    }

    void Update()
    {
        if (scrollingEnabled)
        {
            HandleScrolling();
        }
    }

    void HandleScrolling()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel") * scrollSpeed;
        Vector3 newPos = cam.transform.position + new Vector3(0, scroll, 0);
        newPos.y = Mathf.Clamp(newPos.y, maxY, minY); // Constrain movement
        cam.transform.position = newPos;
    }

    void CenterCamera()
    {
        cam.transform.position = new Vector3(15.81f, minY, -15);
    }
}
