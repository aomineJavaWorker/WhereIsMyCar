using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The class name must match CameraZoomScript.cs for Unity to attach the component.
public class CameraZoomScript : MonoBehaviour
{
    [Header("Zoom")]
    public float maxZoom = 300f;
    public float minZoom = 150f;
    public float panSpeed = 6f;
    public float zoomJump = 50f;
    public float pinchZoomSpeed = 0.9f;
    public float mouseScroolSpeed = 150f; // Serialized field spelling retained.

    [Header("Pan")]
    public float mouseFollowSpeed = 1f;
    public float touchPanSpeed = 1f;

    [Header("References")]
    public Camera mainCamera;
    public ScreenBounderiesScript screenBoundariesScript;

    public float doubleTapMaxDelay = 0.4f;
    public float doubleTapMaxDistance = 100f;

    private float startZoom;
    private Vector2 lastTouchPos;
    private int panFingerId = -1;
    private bool isTouchPan;
    private float lastTapTime = -10f;
    private Vector2 previousTapPosition;
    private float lastPinchDistance;
    private Coroutine resetZoomCoroutine;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
            mainCamera = Camera.main;
        if (screenBoundariesScript == null)
            screenBoundariesScript = FindFirstObjectByType<ScreenBounderiesScript>();
    }

    private void Start()
    {
        if (mainCamera == null) return;

        startZoom = mainCamera.orthographicSize;
        maxZoom = Mathf.Max(maxZoom, startZoom);
        minZoom = Mathf.Min(minZoom, maxZoom);
        UpdateMaxZoom();
        ClampCamera();
    }

    private void Update()
    {
        if (mainCamera == null) return;
        if (ObjectTransformationScript.isTransforming) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        HandleTouch();
#else
        DesktopFollowCursor();
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > Mathf.Epsilon)
        {
            StopZoomReset();
            mainCamera.orthographicSize -= scroll * mouseScroolSpeed;
        }
#endif
        UpdateMaxZoom();
        mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize, minZoom, maxZoom);
        ClampCamera();
    }

    private void DesktopFollowCursor()
    {
        Vector3 mousePos = Input.mousePosition;
        if (!IsValidScreenPosition(mousePos)) return;
        if (Input.GetMouseButton(0) && IsPointerOverUIButton(mousePos)) return;

        // Keep the original desktop cursor-follow behaviour.
        Vector3 screenPoint = new Vector3(mousePos.x, mousePos.y,
            Mathf.Abs(mainCamera.transform.position.z));
        Vector3 targetWorld = mainCamera.ScreenToWorldPoint(screenPoint);
        Vector3 targetPosition = new Vector3(targetWorld.x, targetWorld.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition,
            mouseFollowSpeed * Time.unscaledDeltaTime);
    }

    private void HandleTouch()
    {
        if (Input.touchCount == 0)
        {
            isTouchPan = false;
            panFingerId = -1;
            lastPinchDistance = 0f;
            return;
        }

        if (Input.touchCount >= 2)
        {
            isTouchPan = false;
            Touch a = Input.GetTouch(0);
            Touch b = Input.GetTouch(1);
            if (IsPointerOverUIButton(a.position) || IsPointerOverUIButton(b.position))
            {
                lastPinchDistance = 0f;
                return;
            }

            float distance = Vector2.Distance(a.position, b.position);
            if (lastPinchDistance > 0f && distance > 0f)
            {
                StopZoomReset();
                float zoomDelta = (distance - lastPinchDistance) * pinchZoomSpeed;
                mainCamera.orthographicSize -= zoomDelta;
            }
            lastPinchDistance = distance;
            return;
        }

        lastPinchDistance = 0f;
        Touch touch = Input.GetTouch(0);
        if (!IsValidScreenPosition(touch.position)) return;

        if (touch.phase == TouchPhase.Began)
        {
            if (IsPointerOverInteractiveUI(touch.position))
            {
                isTouchPan = false;
                return;
            }
            if (Time.unscaledTime - lastTapTime <= doubleTapMaxDelay &&
                Vector2.Distance(touch.position, previousTapPosition) <= doubleTapMaxDistance)
            {
                StopZoomReset();
                resetZoomCoroutine = StartCoroutine(ResetZoomSmooth());
                lastTapTime = -10f;
            }
            else
                lastTapTime = Time.unscaledTime;

            previousTapPosition = touch.position;
            lastTouchPos = touch.position;
            panFingerId = touch.fingerId;
            isTouchPan = true;
        }
        else if (touch.phase == TouchPhase.Moved && isTouchPan && touch.fingerId == panFingerId)
        {
            Vector2 delta = touch.position - lastTouchPos;
            transform.position -= ScreenDeltaToWorldDelta(delta) * touchPanSpeed;
            lastTouchPos = touch.position;
        }
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            isTouchPan = false;
            panFingerId = -1;
        }
    }

    private bool IsPointerOverUIButton(Vector2 position)
    {
        return IsPointerOverInteractiveUI(position, onlyButtons: true);
    }

    private bool IsPointerOverInteractiveUI(Vector2 position, bool onlyButtons = false)
    {
        if (EventSystem.current == null) return false;
        var pointer = new PointerEventData(EventSystem.current) { position = position };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        foreach (RaycastResult hit in hits)
        {
            if (hit.gameObject.GetComponentInParent<Button>() != null)
                return true;
            if (!onlyButtons && hit.gameObject.GetComponentInParent<DragAndDropScript>() != null)
                return true;
        }
        return false;
    }

    private bool IsValidScreenPosition(Vector2 position)
    {
        return !float.IsNaN(position.x) && !float.IsNaN(position.y) &&
            !float.IsInfinity(position.x) && !float.IsInfinity(position.y) &&
            position.x >= 0f && position.x <= Screen.width &&
            position.y >= 0f && position.y <= Screen.height;
    }

    private Vector3 ScreenDeltaToWorldDelta(Vector2 delta)
    {
        if (Screen.height <= 0) return Vector3.zero;
        float unitsPerPixel = 2f * mainCamera.orthographicSize / Screen.height;
        return new Vector3(delta.x * unitsPerPixel, delta.y * unitsPerPixel, 0f);
    }

    private IEnumerator ResetZoomSmooth()
    {
        float duration = 0.25f;
        float elapsed = 0f;
        float fromZoom = mainCamera.orthographicSize;
        float toZoom = Mathf.Clamp(startZoom, minZoom, maxZoom);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            mainCamera.orthographicSize = Mathf.Lerp(fromZoom, toZoom,
                Mathf.Clamp01(elapsed / duration));
            ClampCamera();
            yield return null;
        }
        mainCamera.orthographicSize = toZoom;
        ClampCamera();
        resetZoomCoroutine = null;
    }

    private void StopZoomReset()
    {
        if (resetZoomCoroutine == null) return;
        StopCoroutine(resetZoomCoroutine);
        resetZoomCoroutine = null;
    }

    private void UpdateMaxZoom()
    {
        if (screenBoundariesScript == null || mainCamera == null || mainCamera.aspect <= 0f)
            return;
        Rect bounds = screenBoundariesScript.worldBounds;
        if (bounds.width <= 0f || bounds.height <= 0f) return;
        float limit = Mathf.Min(bounds.height / 2f, bounds.width / (2f * mainCamera.aspect));
        // On portrait devices a map may be larger than the viewport: only shrink
        // the maximum if the calculated value makes sense for the min zoom.
        maxZoom = Mathf.Max(minZoom, Mathf.Min(Mathf.Max(maxZoom, startZoom), limit));
    }

    private void ClampCamera()
    {
        if (screenBoundariesScript == null) return;
        screenBoundariesScript.RecalculateBounds();
        transform.position = screenBoundariesScript.GetClampedCameraPosition(transform.position);
    }
}
