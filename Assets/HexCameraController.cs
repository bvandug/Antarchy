using UnityEngine;

public class HexCameraController : MonoBehaviour
{
    public Camera cam;
    private float scrollSpeed = 10f;   // Speed of vertical scrolling
    public float tileHeight = 1.732f; // Approximate height of a hex tile
    public int gridHeight = 50;      // Number of hex rows

    private float minY;
    private float maxY;

    void Start()
    {
        if (cam == null)
        {
            cam = Camera.main; // Get the main camera if not assigned
        }

        minY = 0f;  // Start at the top of the grid
        maxY = -gridHeight * tileHeight; // Move downward instead of upward

        CenterCamera();
    }

    void Update()
    {
        HandleScrolling();
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
        // Centers the camera at the top of the grid
        cam.transform.position = new Vector3(15, minY, -15);
    }
}
