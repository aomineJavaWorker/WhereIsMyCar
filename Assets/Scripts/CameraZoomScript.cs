using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CameraZoomScript : MonoBehaviour
{
    [Header("Zoom - existing Inspector field names kept")]
    public float maxZoom = 300f;
    public float minZoom = 150f;
    public float panSpeed = 6f;
    public float pinchZoomSpeed = 0.9f;
    public float mouseScroolSpeed = 150f;

    [Header("Emulator / on-screen zoom buttons")]
    [SerializeField] private bool showZoomButtons = true;
    [SerializeField, Range(0.02f, 0.5f)] private float zoomButtonStep = 0.15f;

    [Header("Pan")]
    public float mouseFollowSpeed = 1f;
    public float touchPanSpeed = 1f;

    [Header("References")]
    public Camera mainCamera;
    public ScreenBounderiesScript screenBoundariesScript;

    [Header("Double tap")]
    public float doubleTapMaxDelay = 0.4f;
    public float doubleTapMaxDistance = 100f;

    private float startZoom;
    private float configuredMaxZoom;
    private Vector2 lastTouchPos;
    private int panFingerId = -1;
    private bool isTouchPan;
    private float lastTapTime = -10f;
    private Vector2 previousTapPosition;
    private float lastPinchDistance;
    private Coroutine resetZoomCoroutine;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>(32);
    private PointerEventData pointerData;
    private EventSystem pointerEventSystem;
    private GameManager gameManager;
    private float configuredMinZoom;
    private int lastWidth, lastHeight;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        if (mainCamera == null) mainCamera = GetComponent<Camera>();
        if (mainCamera == null) mainCamera = Camera.main;
        if (screenBoundariesScript == null)
            screenBoundariesScript = FindFirstObjectByType<ScreenBounderiesScript>();
    }

    private void Start()
    {
        if (mainCamera == null) { enabled = false; return; }
        startZoom = mainCamera.orthographicSize;
        configuredMaxZoom = Mathf.Max(maxZoom, startZoom);
        minZoom = Mathf.Max(0.01f, minZoom);
        configuredMinZoom = minZoom;
        UpdateMaxZoom();
        mainCamera.orthographicSize = DefaultZoom();
        ClampCamera();
        lastWidth = Screen.width;
        lastHeight = Screen.height;
    }

    private void Update()
    {
        if (mainCamera == null) return;
        if (Screen.width <= 0 || Screen.height <= 0 || mainCamera.pixelWidth <= 0 || mainCamera.pixelHeight <= 0) return;
        UpdateMaxZoom();
        if (lastWidth != Screen.width || lastHeight != Screen.height)
        {
            StopZoomReset();
            // Keep the visible horizontal map extent when the phone rotates.
            float oldAspect = lastHeight > 0 ? (float)lastWidth / lastHeight : mainCamera.aspect;
            mainCamera.orthographicSize *= oldAspect / Mathf.Max(.01f, mainCamera.aspect);
            mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize, minZoom, maxZoom);
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            isTouchPan = false;
            panFingerId = -1;
            lastPinchDistance = 0f;
            lastTapTime = -10f;
            ClampCamera();
        }
        if (gameManager != null && gameManager.GameEnded) { StopZoomReset(); return; }
        if (GameObjectsScript.isDragging) { lastPinchDistance = 0f; isTouchPan = false; return; }
        if (ObjectTransformationScript.isTransforming) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        HandleTouch();
#else
        if (Input.touchCount > 0)
            HandleTouch();
        else
            DesktopFollowCursor();
#endif
        // Read the wheel on all platforms, even when Android emulates a mouse as
        // a single touch. Do not apply wheel zoom during a two-finger pinch.
        if (Input.touchCount < 2)
            HandleMouseWheel();
        // Keyboard zoom works on desktop and with a keyboard attached to an emulator.
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
            ChangeZoom(-1);
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            ChangeZoom(1);
        UpdateMaxZoom();
        mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize, minZoom, maxZoom);
        ClampCamera();
    }

    private void HandleMouseWheel()
    {
        // First try the legacy input (keeps the original desktop wheel speed).
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) < 0.0001f)
            scroll = Input.mouseScrollDelta.y * 0.1f;

        // Some Android emulators forward wheel events only to the new Input
        // System. Its scroll value is usually in pixels (120 per wheel step),
        // whereas the legacy "Mouse ScrollWheel" axis is around 0.1 per step.
        if (Mathf.Abs(scroll) < 0.0001f && Mouse.current != null)
        {
            float wheel = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(wheel) > 0.001f)
            {
                float steps = Mathf.Clamp(Mathf.Abs(wheel) / 120f, 0.25f, 3f);
                scroll = Mathf.Sign(wheel) * steps * 0.1f;
            }
        }

        // Only one source is used per frame so zoom is never applied twice.
        if (Mathf.Abs(scroll) < 0.0001f)
            return;

        StopZoomReset();
        mainCamera.orthographicSize -= scroll * mouseScroolSpeed;
    }

    // A visible fallback on phones and Android emulators where two-finger touch is unavailable.
    private void OnGUI()
    {
        if (!showZoomButtons || mainCamera == null || !enabled ||
            GameObjectsScript.isDragging || ObjectTransformationScript.isTransforming ||
            (gameManager != null && gameManager.GameEnded)) return;

        float size = Mathf.Clamp(Screen.height * 0.09f, 48f, 90f);
        float margin = Mathf.Max(12f, Screen.width * 0.025f);
        float x = Screen.width - size - margin;
        float y = Screen.height * 0.34f;
        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.fontSize = Mathf.RoundToInt(size * 0.48f);
        style.fontStyle = FontStyle.Bold;
        if (GUI.Button(new Rect(x, y, size, size), "+", style)) ChangeZoom(-1);
        if (GUI.Button(new Rect(x, y + size + 10f, size, size), "-", style)) ChangeZoom(1);
    }

    private void ChangeZoom(int direction)
    {
        StopZoomReset();
        UpdateMaxZoom();
        float factor = direction < 0 ? (1f - zoomButtonStep) : (1f + zoomButtonStep);
        mainCamera.orthographicSize =
            Mathf.Clamp(mainCamera.orthographicSize * factor, minZoom, maxZoom);
        ClampCamera();
    }

    private void DesktopFollowCursor()
    {
        Vector3 mousePos = Input.mousePosition;
        if (!IsValidScreenPosition(mousePos)) return;
        if (Input.GetMouseButton(0) && IsPointerOverInteractiveUI(mousePos)) return;

        float cameraDistance = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 screenPoint = new Vector3(mousePos.x, mousePos.y, cameraDistance);
        Vector3 world = mainCamera.ScreenToWorldPoint(screenPoint);
        if (float.IsNaN(world.x) || float.IsInfinity(world.x)) return;
        Vector3 target = new Vector3(world.x, world.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, target,
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
            panFingerId = -1;
            Touch first = Input.GetTouch(0);
            Touch second = Input.GetTouch(1);
            if (!IsValidScreenPosition(first.position) || !IsValidScreenPosition(second.position) ||
                first.phase == TouchPhase.Ended || first.phase == TouchPhase.Canceled ||
                second.phase == TouchPhase.Ended || second.phase == TouchPhase.Canceled)
            { lastPinchDistance = 0f; return; }
            if (IsPointerOverInteractiveUI(first.position) ||
                IsPointerOverInteractiveUI(second.position))
            {
                lastPinchDistance = 0f;
                return;
            }

            float distance = Vector2.Distance(first.position, second.position);
            if (lastPinchDistance > 0f && distance > 0f)
            {
                StopZoomReset();
                mainCamera.orthographicSize *= Mathf.Max(.01f,
                    1f - (distance - lastPinchDistance) / Mathf.Max(1, Screen.height) * pinchZoomSpeed * 2f);
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
                panFingerId = -1;
                return;
            }
            bool doubleTap = Time.unscaledTime - lastTapTime <= doubleTapMaxDelay &&
                Vector2.Distance(touch.position, previousTapPosition) <= doubleTapMaxDistance;
            if (doubleTap)
            {
                StopZoomReset();
                resetZoomCoroutine = StartCoroutine(ResetZoomSmooth());
                lastTapTime = -10f;
            }
            else lastTapTime = Time.unscaledTime;

            previousTapPosition = touch.position;
            lastTouchPos = touch.position;
            panFingerId = touch.fingerId;
            isTouchPan = true;
        }
        else if (touch.phase == TouchPhase.Moved && isTouchPan &&
                 touch.fingerId == panFingerId)
        {
            Vector2 delta = touch.position - lastTouchPos;
            // Move camera in the opposite direction to drag the map with the finger.
            transform.position -= ScreenDeltaToWorldDelta(delta) * touchPanSpeed;
            lastTouchPos = touch.position;
        }
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            isTouchPan = false;
            panFingerId = -1;
        }
    }

    private bool IsPointerOverInteractiveUI(Vector2 position)
    {
        if (EventSystem.current == null) return false;
        if (pointerData == null || pointerEventSystem != EventSystem.current)
        {
            pointerEventSystem = EventSystem.current;
            pointerData = new PointerEventData(EventSystem.current);
        }
        pointerData.Reset();
        pointerData.position = position;
        uiHits.Clear();
        EventSystem.current.RaycastAll(pointerData, uiHits);
        foreach (RaycastResult hit in uiHits)
        {
            if (hit.gameObject.GetComponentInParent<Button>() != null ||
                hit.gameObject.GetComponentInParent<MobileTransformButtonScript>() != null ||
                hit.gameObject.GetComponentInParent<DragAndDropScript>() != null)
                return true;
        }
        return false;
    }

    private static bool IsValidScreenPosition(Vector2 p)
    {
        return !float.IsNaN(p.x) && !float.IsNaN(p.y) &&
            !float.IsInfinity(p.x) && !float.IsInfinity(p.y) &&
            p.x >= 0f && p.x <= Screen.width && p.y >= 0f && p.y <= Screen.height;
    }

    private Vector3 ScreenDeltaToWorldDelta(Vector2 delta)
    {
        if (Screen.height <= 0) return Vector3.zero;
        float perPixel = 2f * mainCamera.orthographicSize / Screen.height;
        return new Vector3(delta.x * perPixel, delta.y * perPixel, 0f);
    }

    private IEnumerator ResetZoomSmooth()
    {
        float duration = 0.25f;
        float time = 0f;
        float from = mainCamera.orthographicSize;
        float to = DefaultZoom();
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            mainCamera.orthographicSize = Mathf.Lerp(from, to, Mathf.Clamp01(time / duration));
            ClampCamera();
            yield return null;
        }
        mainCamera.orthographicSize = to;
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
        {
            maxZoom = Mathf.Max(minZoom, configuredMaxZoom);
            return;
        }
        Rect bounds = screenBoundariesScript.worldBounds;
        if (bounds.width <= 0f || bounds.height <= 0f) return;
        float limit = Mathf.Min(bounds.height / 2f,
            bounds.width / (2f * mainCamera.aspect));
        // Use the original Inspector limit, NOT last frame's maxZoom.
        // Otherwise rotation/resize can permanently shrink the allowed zoom range.
        float orientationScale = Mathf.Max(1f, (16f / 9f) / mainCamera.aspect);
        maxZoom = Mathf.Max(.01f, Mathf.Min(configuredMaxZoom * orientationScale, limit * .994f));
        minZoom = Mathf.Min(configuredMinZoom, maxZoom);
    }

    private float DefaultZoom()
    {
        float orientationScale = Mathf.Max(1f, (16f / 9f) / Mathf.Max(.01f, mainCamera.aspect));
        return Mathf.Clamp(startZoom * orientationScale, minZoom, maxZoom);
    }

    private void OnDisable()
    {
        StopZoomReset();
        isTouchPan = false;
        lastPinchDistance = 0f;
    }

    private void ClampCamera()
    {
        if (screenBoundariesScript == null) return;
        screenBoundariesScript.RecalculateBounds();
        transform.position = screenBoundariesScript.GetClampedCameraPosition(transform.position);
    }
}
