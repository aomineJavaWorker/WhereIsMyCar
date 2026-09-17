using UnityEngine;

public class CameraZoomScr : MonoBehaviour
{
    public float maxZoom = 300f;
    public float minZoom = 150f;
    public float panSpeed = 6f;
    public float zoomJump = 50f;

    Vector3 bottomLeft;
    Vector3 topRight;

    float cameraMaxX;
    float cameraMinX;
    float cameraMaxY;
    float cameraMinY;

    float x, y;

    public Camera mainCamera;

    void Awake()
    {
        mainCamera = GetComponent<Camera>();

        topRight = mainCamera.ScreenToWorldPoint(
            new Vector3(
                mainCamera.pixelWidth,
                mainCamera.pixelHeight,
                -transform.position.z
            )
        );

        bottomLeft = mainCamera.ScreenToWorldPoint(
            new Vector3( 0,0, -transform.position.z
            )
        );

        cameraMaxX = topRight.x;
        cameraMinX = bottomLeft.x;
        cameraMaxY = topRight.y;
        cameraMinY = bottomLeft.y;
    }

    void Update()
    {
        // Camera movement
        x = Input.GetAxis("Mouse X") * panSpeed;
        y = Input.GetAxis("Mouse Y") * panSpeed;

        transform.Translate(x, y, 0);

        // Zoom in
        if (Input.GetAxis("Mouse ScrollWheel") > 0 &&
            mainCamera.orthographicSize > minZoom)
        {
            mainCamera.orthographicSize -= zoomJump;
        }

        // Zoom out
        if (Input.GetAxis("Mouse ScrollWheel") < 0 &&
            mainCamera.orthographicSize < maxZoom)
        {
            mainCamera.orthographicSize += zoomJump;
        }

        // Keep zoom inside min/max values
        mainCamera.orthographicSize = Mathf.Clamp(
            mainCamera.orthographicSize,
            minZoom,
            maxZoom
        );

        // Recalculate camera borders
        topRight = mainCamera.ScreenToWorldPoint(
            new Vector3(
                mainCamera.pixelWidth,
                mainCamera.pixelHeight,
                -transform.position.z
            )
        );

        bottomLeft = mainCamera.ScreenToWorldPoint(
            new Vector3(
                0,
                0,
                -transform.position.z
            )
        );

        // Right border
        if (topRight.x > cameraMaxX)
        {
            transform.position = new Vector3(
                transform.position.x - (topRight.x - cameraMaxX),
                transform.position.y,
                transform.position.z
            );
        }

        // Left border
        if (bottomLeft.x < cameraMinX)
        {
            transform.position = new Vector3(
                transform.position.x + (cameraMinX - bottomLeft.x),
                transform.position.y,
                transform.position.z
            );
        }

        // Top border
        if (topRight.y > cameraMaxY)
        {
            transform.position = new Vector3(
                transform.position.x,
                transform.position.y - (topRight.y - cameraMaxY),
                transform.position.z
            );
        }

        // Bottom border
        if (bottomLeft.y < cameraMinY)
        {
            transform.position = new Vector3(
                transform.position.x,
                transform.position.y + (cameraMinY - bottomLeft.y),
                transform.position.z
            );
        }
    }
}