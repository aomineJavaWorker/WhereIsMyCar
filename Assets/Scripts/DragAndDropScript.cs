using UnityEngine;
using UnityEngine.EventSystems;

public class DragAndDropScript : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameObjectsScript gameObjectsScript;
    private CanvasGroup canvasGroup;
    private GameManager gameManager;
    private bool GameEnded => gameManager != null && gameManager.GameEnded;
    private RectTransform rectTransform;
    private Vector3 startPosition;
    private bool hasStartPosition;
    private bool placedThisDrag;
    [HideInInspector]
    public bool isPlaced = false;
    public ScreenBounderiesScript screenBounderiesScript;

    public void SaveStartPosition()
    {
        // May be called by the spawner before this component's Awake.
        rectTransform = GetComponent<RectTransform>();
        startPosition = rectTransform.localPosition;
        hasStartPosition = true;
    }

    public void ReturnToStart()
    {
        if (GameEnded)
            return;
        rectTransform.localPosition = startPosition;
    }

    public bool ConfirmPlacement()
    {
        if (GameEnded || isPlaced)
            return false;

        isPlaced = true;
        placedThisDrag = true;

        return true;
    }

    void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        rectTransform = GetComponent<RectTransform>();

        gameObjectsScript = FindFirstObjectByType<GameObjectsScript>();
        screenBounderiesScript = FindFirstObjectByType<ScreenBounderiesScript>();
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (GameEnded || isPlaced)
            return;
        if (Input.GetMouseButtonDown(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2))
        {
            Debug.Log("Left mouse button clicked on " + gameObject.name);
            gameObjectsScript.carSoundSource.PlayOneShot(gameObjectsScript.sounds[0]);
        }
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (GameEnded || isPlaced)
            return;
        if (Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2))
        {
            GameObjectsScript.isDragging = true;
            placedThisDrag = false;
            gameObjectsScript.inRightPlace = false;
            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;
            int lastIndex = transform.parent.childCount - 1;
            int positionIndex = Mathf.Max(0, lastIndex - 1);
            transform.SetSiblingIndex(positionIndex);

            //Izstrdsim ScrennBounderis un tad vares taisit parvietosanu
            Vector3 cursorWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(
                Input.mousePosition.x, Input.mousePosition.y,
                screenBounderiesScript.screenPoint.z));
            rectTransform.position = cursorWorldPos;
            screenBounderiesScript.screenPoint =
                Camera.main.WorldToScreenPoint(rectTransform.position);

            screenBounderiesScript.offset = rectTransform.position -
                Camera.main.ScreenToWorldPoint(new Vector3(
                    Input.mousePosition.x, Input.mousePosition.y,
                    screenBounderiesScript.screenPoint.z));
            GameObjectsScript.lastDragged = eventData.pointerDrag;
        }

        
    }


    public void OnDrag(PointerEventData eventData)
    {
        if (GameEnded || isPlaced)
            return;

        if (Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2))
        {
            Vector3 cursScreenPoint =
                new Vector3(Input.mousePosition.x,
                Input.mousePosition.y,
                screenBounderiesScript.screenPoint.z);
            Vector3 curPosition
            = Camera.main.ScreenToWorldPoint(cursScreenPoint) +
            screenBounderiesScript.offset;

            rectTransform.position = screenBounderiesScript.GetClampedPosition(curPosition);

        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (GameEnded)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = !isPlaced;
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            GameObjectsScript.isDragging = false;
            Debug.Log("OnEndDrag called for " + gameObject.name);
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;

            if (placedThisDrag)
            {
                canvasGroup.blocksRaycasts = false;
                GameObjectsScript.lastDragged = null;
            }

            // Only DropPlaceScript returns a car to spawn when its tag is wrong.
            // Other unsuccessful drops keep the current position.

            gameObjectsScript.inRightPlace = false;
        }
    }

    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // All scene Awake callbacks, including the random spawn, have finished.
        if (!hasStartPosition)
            SaveStartPosition();
    }
}
