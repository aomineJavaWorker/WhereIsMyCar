using UnityEngine;

// The name is intentionally kept as ScreenBounderiesScript: it is used by the scene.
public class ScreenBounderiesScript : MonoBehaviour
{
    [HideInInspector] public Vector3 screenPoint, offset;
    [HideInInspector] public float minX, maxX, minY, maxY;

    public Rect worldBounds = new Rect(-10f, -5f, 20f, 10f);
    [Range(0f, 0.5f)] public float reductionFactor = 0.02f;
    public Camera mainCamera;

    public float minCamX { get; private set; }
    public float maxCamX { get; private set; }
    public float minCamY { get; private set; }
    public float maxCamY { get; private set; }

    private float lastOrthographicSize;
    private float lastAspect;
    private Vector3 lastCamPos;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        InitializeWorldBounds();
        RecalculateBounds();
    }

    private void Start()
    {
        // Canvas layout may not be calculated yet during Awake.
        Canvas.ForceUpdateCanvases();
        InitializeWorldBounds();
        RecalculateBounds();
    }

    private void InitializeWorldBounds()
    {
        GameObject map = GameObject.Find("backgroundImage");
        if (map == null) map = GameObject.Find("Karte");

        if (map != null && map.TryGetComponent(out RectTransform mapRect))
        {
            Vector3[] corners = new Vector3[4];
            mapRect.GetWorldCorners(corners);
            if (corners[2].x - corners[0].x > 1f &&
                corners[2].y - corners[0].y > 1f)
            {
                worldBounds = Rect.MinMaxRect(corners[0].x, corners[0].y,
                    corners[2].x, corners[2].y);
                return;
            }
        }

        if (mainCamera != null && mainCamera.orthographic)
        {
            float h = mainCamera.orthographicSize;
            float w = h * mainCamera.aspect;
            Vector3 p = mainCamera.transform.position;
            worldBounds = Rect.MinMaxRect(p.x - w, p.y - h, p.x + w, p.y + h);
        }
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        if (!Mathf.Approximately(mainCamera.orthographicSize, lastOrthographicSize) ||
            !Mathf.Approximately(mainCamera.aspect, lastAspect) ||
            mainCamera.transform.position != lastCamPos)
        {
            RecalculateBounds();
        }
    }

    public void RecalculateBounds()
    {
        if (mainCamera == null) return;

        float shrinkW = worldBounds.width * reductionFactor;
        float shrinkH = worldBounds.height * reductionFactor;
        minX = worldBounds.xMin + shrinkW;
        maxX = worldBounds.xMax - shrinkW;
        minY = worldBounds.yMin + shrinkH;
        maxY = worldBounds.yMax - shrinkH;

        if (mainCamera.orthographic)
        {
            float halfHeight = mainCamera.orthographicSize;
            float halfWidth = halfHeight * mainCamera.aspect;

            if (halfWidth * 2f >= worldBounds.width)
                minCamX = maxCamX = worldBounds.center.x;
            else
            {
                minCamX = worldBounds.xMin + halfWidth;
                maxCamX = worldBounds.xMax - halfWidth;
            }

            if (halfHeight * 2f >= worldBounds.height)
                minCamY = maxCamY = worldBounds.center.y;
            else
            {
                minCamY = worldBounds.yMin + halfHeight;
                maxCamY = worldBounds.yMax - halfHeight;
            }
        }
        else
        {
            minCamX = minX;
            maxCamX = maxX;
            minCamY = minY;
            maxCamY = maxY;
        }

        lastOrthographicSize = mainCamera.orthographicSize;
        lastAspect = mainCamera.aspect;
        lastCamPos = mainCamera.transform.position;
    }

    public Vector2 GetClampedPosition(Vector3 curPosition)
    {
        return new Vector2(Mathf.Clamp(curPosition.x, minX, maxX),
            Mathf.Clamp(curPosition.y, minY, maxY));
    }

    public Vector3 GetClampedCameraPosition(Vector3 desiredCameraCenter)
    {
        return new Vector3(
            Mathf.Clamp(desiredCameraCenter.x, minCamX, maxCamX),
            Mathf.Clamp(desiredCameraCenter.y, minCamY, maxCamY),
            desiredCameraCenter.z);
    }
}
