using UnityEngine;
using UnityEngine.EventSystems;

public class DragAndDropScript : MonoBehaviour,
    IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameObjectsScript gameObjectsScript;
    public ScreenBounderiesScript screenBounderiesScript;

    private CanvasGroup canvasGroup;
    private GameManager gameManager;
    private RectTransform rectTransform;
    private Vector3 startPosition;
    private Vector3 dragOffsetWorld;
    private bool hasStartPosition;
    private bool placedThisDrag;
    private bool ownsDrag;
    private int activePointerId;
    private Vector2 dragPointerPosition;
    private readonly Vector3[] corners = new Vector3[4];
    [HideInInspector] public bool isPlaced = false;
    public bool IsBeingDestroyed { get; private set; }

    private bool GameEnded => gameManager != null && gameManager.GameEnded;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        if (gameObjectsScript == null)
            gameObjectsScript = FindFirstObjectByType<GameObjectsScript>();
        if (screenBounderiesScript == null)
            screenBounderiesScript = FindFirstObjectByType<ScreenBounderiesScript>();
    }

    private void Start()
    {
        if (!hasStartPosition) SaveStartPosition();
    }

    public void SaveStartPosition()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null) return;
        startPosition = rectTransform.localPosition;
        hasStartPosition = true;
    }

    public void ReturnToStart()
    {
        if (GameEnded || IsBeingDestroyed || !hasStartPosition || rectTransform == null) return;
        rectTransform.localPosition = startPosition;
    }

    public bool ConfirmPlacement()
    {
        if (GameEnded || isPlaced || IsBeingDestroyed) return false;
        isPlaced = true;
        placedThisDrag = true;
        return true;
    }

    private static bool IsPrimaryPointer(PointerEventData data)
    {
        return data.button == PointerEventData.InputButton.Left;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (GameEnded || isPlaced || IsBeingDestroyed || !IsPrimaryPointer(eventData)) return;
        if (GameObjectsScript.isDragging) return;

        // A simple tap selects the car too, so the phone buttons work
        // even before the player starts dragging it.
        GameObjectsScript.lastDragged = gameObject;

        if (gameObjectsScript != null && gameObjectsScript.carSoundSource != null &&
            gameObjectsScript.sounds != null && gameObjectsScript.sounds.Length > 0 &&
            gameObjectsScript.sounds[0] != null)
        {
            gameObjectsScript.carSoundSource.PlayOneShot(gameObjectsScript.sounds[0]);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (GameEnded || isPlaced || IsBeingDestroyed || !IsPrimaryPointer(eventData) ||
            rectTransform == null || canvasGroup == null) return;
        if (GameObjectsScript.isDragging || ownsDrag) return;

        ownsDrag = true;
        activePointerId = eventData.pointerId;
        dragPointerPosition = eventData.position;
        GameObjectsScript.isDragging = true;
        GameObjectsScript.lastDragged = gameObject;
        placedThisDrag = false;
        if (gameObjectsScript != null) gameObjectsScript.inRightPlace = false;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        if (transform.parent != null)
        {
            int lastIndex = transform.parent.childCount - 1;
            transform.SetSiblingIndex(Mathf.Max(0, lastIndex - 1));
        }

        Vector3 pointer;
        dragOffsetWorld = TryGetWorldPointer(eventData, out pointer)
            ? rectTransform.position - pointer : Vector3.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (GameEnded || isPlaced || IsBeingDestroyed || !GameObjectsScript.isDragging ||
            !ownsDrag || eventData.pointerId != activePointerId ||
            !IsPrimaryPointer(eventData) || rectTransform == null) return;

        Vector3 pointer;
        dragPointerPosition = eventData.position;
        if (!TryGetWorldPointer(eventData, out pointer)) return;
        Vector3 desired = pointer + dragOffsetWorld;
        desired.z = rectTransform.position.z;

        if (screenBounderiesScript != null)
        {
            screenBounderiesScript.RecalculateBounds();
            Vector2 clamped = screenBounderiesScript.GetClampedPosition(desired);
            desired.x = clamped.x;
            desired.y = clamped.y;
        }
        rectTransform.position = desired;
        ClampToMap();
    }

    public void ClampToMap()
    {
        if (rectTransform == null || screenBounderiesScript == null) return;
        Rect bounds = screenBounderiesScript.worldBounds;
        rectTransform.GetWorldCorners(corners);
        Vector2 min = corners[0], max = corners[0];
        for (int i = 1; i < corners.Length; i++)
        {
            min = Vector2.Min(min, corners[i]);
            max = Vector2.Max(max, corners[i]);
        }
        Vector3 shift = Vector3.zero;
        shift.x = max.x - min.x > bounds.width ? bounds.center.x - (min.x + max.x) * .5f :
            Mathf.Max(0f, bounds.xMin - min.x) + Mathf.Min(0f, bounds.xMax - max.x);
        shift.y = max.y - min.y > bounds.height ? bounds.center.y - (min.y + max.y) * .5f :
            Mathf.Max(0f, bounds.yMin - min.y) + Mathf.Min(0f, bounds.yMax - max.y);
        rectTransform.position += shift;
    }

    public bool TryGetDragPointer(out Vector2 position)
    {
        // InputSystemUIInputModule encodes device IDs in pointerId; it is not
        // necessarily Input.GetTouch(i).fingerId. Use this drag's event position.
        position = dragPointerPosition;
        return ownsDrag;
    }

    public void BeginDestruction()
    {
        IsBeingDestroyed = true;
        ownsDrag = false;
        if (canvasGroup != null) { canvasGroup.alpha = 1f; canvasGroup.blocksRaycasts = false; }
        if (GameObjectsScript.lastDragged == gameObject)
        {
            GameObjectsScript.lastDragged = null;
            GameObjectsScript.isDragging = false;
        }
    }

    private bool TryGetWorldPointer(PointerEventData eventData, out Vector3 world)
    {
        world = Vector3.zero;
        if (rectTransform == null) return false;
        Vector2 screen = eventData.position;
        if (Screen.width <= 0 || Screen.height <= 0 ||
            float.IsNaN(screen.x) || float.IsInfinity(screen.x) ||
            float.IsNaN(screen.y) || float.IsInfinity(screen.y)) return false;
        screen.x = Mathf.Clamp(screen.x, 0f, Screen.width);
        screen.y = Mathf.Clamp(screen.y, 0f, Screen.height);
        RectTransform parentRect = rectTransform.parent as RectTransform;
        if (parentRect == null) return false;

        Camera uiCamera = eventData.pressEventCamera;
        if (uiCamera == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }
        if (uiCamera == null && GetComponentInParent<Canvas>()?.renderMode == RenderMode.WorldSpace)
            return false;

        return RectTransformUtility.ScreenPointToWorldPointInRectangle(
            parentRect, screen, uiCamera, out world) &&
            IsFinite(world);
    }

    private static bool IsFinite(Vector3 p)
    {
        return !float.IsNaN(p.x) && !float.IsInfinity(p.x) &&
               !float.IsNaN(p.y) && !float.IsInfinity(p.y) &&
               !float.IsNaN(p.z) && !float.IsInfinity(p.z);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!ownsDrag || eventData.pointerId != activePointerId) return;
        ownsDrag = false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = !isPlaced;
        }
        GameObjectsScript.isDragging = false;
        if (isPlaced || placedThisDrag || GameEnded)
            GameObjectsScript.lastDragged = null;
        if (gameObjectsScript != null)
            gameObjectsScript.inRightPlace = false;
        // Wrong tag -> returns to start; right tag with wrong size -> stays.
        // Dropping on the background -> stays.
    }

    private void OnDisable()
    {
        if (!ownsDrag) return;
        ownsDrag = false;
        if (canvasGroup != null) { canvasGroup.alpha = 1f; canvasGroup.blocksRaycasts = !isPlaced; }
        if (GameObjectsScript.lastDragged == gameObject)
        {
            GameObjectsScript.isDragging = false;
            GameObjectsScript.lastDragged = null;
        }
    }

    private void OnApplicationFocus(bool focused) { if (!focused) OnDisable(); }
    private void OnApplicationPause(bool paused) { if (paused) OnDisable(); }
}
