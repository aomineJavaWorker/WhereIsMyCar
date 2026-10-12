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
        startPosition = rectTransform.localPosition;
        hasStartPosition = true;
    }

    public void ReturnToStart()
    {
        if (GameEnded) return;
        if (rectTransform != null && hasStartPosition)
            rectTransform.localPosition = startPosition;
    }

    public bool ConfirmPlacement()
    {
        if (GameEnded || isPlaced) return false;
        isPlaced = true;
        placedThisDrag = true;
        return true;
    }

    private static bool IsPrimaryPointer(PointerEventData eventData)
    {
        // On touchscreen, Unity's pointer events still use Left for the primary finger.
        return eventData.button == PointerEventData.InputButton.Left;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (GameEnded || isPlaced || !IsPrimaryPointer(eventData)) return;
        if (gameObjectsScript != null && gameObjectsScript.carSoundSource != null &&
            gameObjectsScript.sounds != null && gameObjectsScript.sounds.Length > 0 &&
            gameObjectsScript.sounds[0] != null)
        {
            gameObjectsScript.carSoundSource.PlayOneShot(gameObjectsScript.sounds[0]);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (GameEnded || isPlaced || !IsPrimaryPointer(eventData)) return;
        if (rectTransform == null || canvasGroup == null) return;

        GameObjectsScript.isDragging = true;
        GameObjectsScript.lastDragged = eventData.pointerDrag;
        placedThisDrag = false;
        if (gameObjectsScript != null) gameObjectsScript.inRightPlace = false;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        if (transform.parent != null)
        {
            int lastIndex = transform.parent.childCount - 1;
            transform.SetSiblingIndex(Mathf.Max(0, lastIndex - 1));
        }

        // Use the actual Canvas plane instead of Camera.ScreenToWorldPoint(z=0).
        // That works for Screen Space - Camera on both desktop and Android.
        Vector3 worldPointer;
        if (TryGetWorldPointer(eventData, out worldPointer) && screenBounderiesScript != null)
        {
            screenBounderiesScript.screenPoint = new Vector3(eventData.position.x,
                eventData.position.y, Mathf.Abs(worldPointer.z - CameraDepth()));
            screenBounderiesScript.offset = rectTransform.position - worldPointer;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (GameEnded || isPlaced || !IsPrimaryPointer(eventData)) return;
        if (screenBounderiesScript == null || rectTransform == null) return;

        Vector3 pointer;
        if (!TryGetWorldPointer(eventData, out pointer)) return;

        Vector3 newPosition = pointer + screenBounderiesScript.offset;
        Vector2 clamped = screenBounderiesScript.GetClampedPosition(newPosition);
        rectTransform.position = new Vector3(clamped.x, clamped.y, rectTransform.position.z);
    }

    private bool TryGetWorldPointer(PointerEventData eventData, out Vector3 world)
    {
        world = Vector3.zero;
        RectTransform parentRect = rectTransform.parent as RectTransform;
        if (parentRect == null) return false;

        Camera uiCamera = eventData.pressEventCamera;
        if (uiCamera == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }
        return RectTransformUtility.ScreenPointToWorldPointInRectangle(
            parentRect, eventData.position, uiCamera, out world);
    }

    private float CameraDepth()
    {
        Camera camera = Camera.main;
        return camera != null ? camera.transform.position.z : 0f;
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

        // Wrong tag -> DropPlaceScript returns car; wrong size / background -> stays.
    }
}
