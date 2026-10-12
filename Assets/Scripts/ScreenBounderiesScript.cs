using UnityEngine;

public class ScreenBounderiesScript : MonoBehaviour
{
    [HideInInspector]
    public Vector3 screenPoint, offset;
    [HideInInspector]
    public Rect worldBounds = new Rect(-10f, -5f, 20f, 10f);
    [Range(0f, 0.5f)]
    public float reductionFactor = 0.02f; //2% no ekrāna izmēra
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public Camera mainCamera;
    public float minCamX { get; private set;}
    public float maxCamX { get; private set;}
    public float minCamY { get; private set;}
    public float maxCamY { get; private set; }

    float lastOrthographicSize;
    float lastAspect;
    Vector3 lastCamPos;

    void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        RecalculateBounds();
    }

    void Update()
    {
        if (mainCamera == null)
            return;
        bool changed = false;
        if (mainCamera.orthographic)
        {
            if (!Mathf.Approximately(mainCamera.orthographicSize, lastOrthographicSize))
            {
                changed = true;
            }

        }

        if (!Mathf.Approximately(mainCamera.aspect, lastAspect))
        {
            changed = true;
        }
        if (mainCamera.transform.position != lastCamPos)
        {
            changed = true;
        }
        if (changed)
        {
            RecalculateBounds();
        }
    }

    public void RecalculateBounds()
    {
        if (mainCamera == null)
            return;

        float wbMinX = worldBounds.xMin;
        float wbMaxX = worldBounds.xMax;
        float wbMinY = worldBounds.yMin;
        float wbMaxY = worldBounds.yMax;

        if(mainCamera.orthographic)
        {
            float halfHeight = mainCamera.orthographicSize;
            float halfWidth = halfHeight * mainCamera.aspect;

            if (halfWidth * 2f >= (wbMaxX - wbMinX))
            {
                minCamX = maxCamX = (wbMinX + wbMaxX) * 0.5f;
            }
            else
            {
                minCamX = wbMinX + halfWidth;
                maxCamX = wbMaxX - halfWidth;
            }

            if (halfHeight * 2f >= (wbMaxY - wbMinY))
            {
                minCamY = maxCamY = (wbMinY + wbMaxY) * 0.5f;
            }
            else
            {
                minCamY = wbMinY + halfWidth;
                maxCamY = wbMaxY - halfWidth;
            }
        }

        lastOrthographicSize = mainCamera.orthographicSize;
        lastAspect = mainCamera.aspect;
        lastCamPos = mainCamera.transform.position;
    }

    public Vector2 GetClampedPosition(Vector3 curPosition)
    {
        float shrinkW = worldBounds.width * reductionFactor;
        float shrinkH = worldBounds.height * reductionFactor;
        float wbMinX = worldBounds.xMin + shrinkW;
        float wbMaxX = worldBounds.xMax - shrinkW;
        float wbMinY = worldBounds.yMin + shrinkH;
        float wbMaxY = worldBounds.yMax + shrinkH;

        float clampedX = Mathf.Clamp(curPosition.x, wbMinX, wbMaxX);
        float clampedY = Mathf.Clamp(curPosition.y, wbMinY, wbMaxY);
        return new Vector2(clampedX, clampedY);
    }

    public Vector3 GetClampedCameraPosition(Vector3 desireCameraCenter)
    {
        float clampedX = Mathf.Clamp(desireCameraCenter.x, minCamX, maxCamX);
        float clampedY = Mathf.Clamp(desireCameraCenter.y, minCamY, maxCamY);
        return new Vector3(clampedX, clampedY, desireCameraCenter.z);
    }


}
