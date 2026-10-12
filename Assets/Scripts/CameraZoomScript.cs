
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;

public class CameraZoomScr : MonoBehaviour
{
    [Header("Zoom")]
    public float maxZoom = 300f;
    public float minZoom = 150f;
    public float panSpeed = 6f;
    public float zoomJump = 50f;
    public float pinchZoomSpeed = 0.9f;
    public float mouseScroolSpeed = 150f;
    [Header("Pan")]
    public float mouseFollowSpeed = 1f;
    public float touchPanSpeed = 1f;

    [Header("References")]
    public Camera mainCamera;
    public ScreenBounderiesScript screenBoundariesScript;
    float startZoom;
    Vector2 lastTouchPos;
    int panFingerId = -1;
    bool isTouchPan = false;

    float lastTapTime = 0f;
    public float doubleTapMaxDelay = 0.4f;
    public float doubleTapMaxDistance = 100f;

    void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = GetComponent<Camera>();
        }

        if (screenBoundariesScript = null)
        {
            screenBoundariesScript = FindFirstObjectByType<ScreenBounderiesScript>();
        }
    }

    private void Start()
    {
        startZoom = mainCamera.orthographicSize;
        screenBoundariesScript.RecalculateBounds();
        transform.position = screenBoundariesScript.GetClampedPosition(transform.position);
    }



    void Update()
    {
        if (ObjectTransformationScript.isTransforming)
        {
            return;
        }
#if UNITY_EDITOR || UNITY_STANDALONE
        DekstopFollowCursor();
        float scroll = Input.GetAxis("Mouse ScroolWheel");
        if (Mathf.Abs(scroll) > Mathf.Epsilon)
            mainCamera.orthographic -= scroll * mouseScrollSpeed;
#else
    HandleTouch();
#endif
        if (Input.touchCount == 2)
            HandlePinch();

        UpDateMaxZoom();
        mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize, minZoom, maxZoom);
        screenBoundariesScript.RecalculateBounds();
        transform.position = screenBoundariesScript.GetClampedPosition(transform.position);

    }
    void DekstopFollowCursor()
    {
        Vector3 mousePos = Input.mousePosition;
        if (mousePos.x < 0 || mousePos.x > Screen.width || mousePos.y < 0 || mousePos.y > Screen.height)
        {
            return;
        }
        Vector3 screenPoint = new Vector3(mousePos.x, mousePos.y, mainCamera.nearClipPlane);
        Vector3 targetWorld = mainCamera.ScreenToWorldPoint(screenPoint);
        Vector3 targetPosition = new Vector3(targetWorld.x, targetWorld.y, transform.position.z);

        transform.position = Vector3.Lerp(transform.position, targetPosition, mouseFollowSpeed * Time.unscaledDeltaTime);
    }

    void HandlePinch()
    {
        if (Input.touchCount != 1)
            return;
        Touch touch = Input.GetTouch(0);

        if (IsTouchUIButton(touch.position))
        {
            return;
        }
        if (touch.phase == TouchPhase.Began)
        {
            float dt = Time.time - lastTapTime;
            if (dt <= doubleTapMaxDelay &&
                Vector2.Distance(touch.position, lastTouchPos) <= doubleTapMaxDistance
            {

                StartCoroutine(RestZoomSmooth());
                lastTapTime = 0f;
            } else
            {
                lastTapTime = Time.time;
            }
            lastTouchPos = touch.position;
            panFingerId = touch.fingerId;
            isTouchPan = true;
        } else if (touch.phase == TouchPhase.Moved && isTouchPan &&
            touch.fingerId == panFingerId)
        {
            Vector2 delta = touch.position - lastTouchPos;
            transform.Translate(ScreenDeltaToWorldDelta(delta) * touchPanSpeed, Space.World);
            lastTouchPos = touch.position;
        } else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            isTouchPan = false;
            panFingerId = -1;
        }
      
        }
    bool IsTouchUIButton(Vector2 touchPosition)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = touchPosition;
        List<RaycastResult> results = new List<RaycastResult()>;
        EventSystem.current.RaycastAll(pointerData, results);

        foreach(RaycastResult result in results)
        {
            if(result.gameObject.GetComponent<UnityEngine.UI.Button>() != null)
            {
                return true;
            }
        }
        return false;
    }

    Vector3 ScreenDeltaToWorldDelta(Vector2 delta)
    {
        float worldPerPixel = (2f * mainCamera.orthographicSize) / Screen.height;
        return new Vector3(delta.x * worldPerPixel, delta.y * worldPerPixel, 0f);
    }

    IEnumerator ResetZoomSmooth()
    {
        float duration = 0.25f;
        float elpased = 0f;
        float currentZoom = mainCamera.orthographicSize;
        float targetZoom = maxZoom;

        while (elpased < duration)
        {
            elpased += Time.unscaledDeltaTime;
            mainCamera.orthographicSize =
                Mathf.Lerp(currentZoom, targetZoom, elpased / duration);
            screenBoundariesScript.RecalculateBounds();
            transform.position = screenBoundariesScript.GetClampedPosition(transform.position);

            yield return null;
        }

        mainCamera.orthographicSize = targetZoom;
        screenBoundariesScript.RecalculateBounds();
        transform.position =
            screenBoundariesScript.GetClampedPosition(transform.position);

        void UpdateMaxZoom()
        {
            if (screenBoundariesScript == null || mainCamera == null)
                return;

            Rect wb = screenBoundariesScript.worldBounds;

            float maxZoomByHeight = wb.height / 2f;
            float maxZoomByWidth = (wb.width / 2F) / mainCamera.aspect;
            maxZoom = Mathf.Min(maxZoomByHeight, maxZoomByWidth);
        }
    }
}