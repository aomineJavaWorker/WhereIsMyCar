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
    [HideInInspector] public bool isPlaced = false;

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
        if (GameEnded || !hasStartPosition || rectTransform == null) return;
        rectTransform.localPosition = startPosition;
    }

    public bool ConfirmPlacement()
    {
        if (GameEnded || isPlaced) return false;
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
        if (GameEnded || isPlaced || !IsPrimaryPointer(eventData)) return;

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
        if (GameEnded || isPlaced || !IsPrimaryPointer(eventData) ||
            rectTransform == null || canvasGroup == null) return;

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
        if (GameEnded || isPlaced || !GameObjectsScript.isDragging ||
            !IsPrimaryPointer(eventData) || rectTransform == null) return;

        Vector3 pointer;
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
    }

    private bool TryGetWorldPointer(PointerEventData eventData, out Vector3 world)
    {
        world = Vector3.zero;
        if (rectTransform == null) return false;
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
            parentRect, eventData.position, uiCamera, out world) &&
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
}
