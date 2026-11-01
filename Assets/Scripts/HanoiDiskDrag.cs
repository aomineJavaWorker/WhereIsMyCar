using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Pointer-based drag works with Unity Input System UI for mouse and Android touch.</summary>
public class HanoiDiskDrag : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Range(1, 6)] public int diskNumber = 1;

    private HanojasTornis game;
    private RectTransform rect;
    private Vector3 pointerOffset;
    private int sourceRod = -1;
    private int activePointerId;
    private bool dragging;

    private void Awake()
    {
        game = GetComponentInParent<HanojasTornis>();
        rect = GetComponent<RectTransform>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (game == null || !game.CanDragDisk(diskNumber, out int rod)) return;
        if (!PointerWorld(eventData, out Vector3 pointer)) return;

        sourceRod = rod;
        activePointerId = eventData.pointerId;
        pointerOffset = rect.position - pointer;
        dragging = true;
        rect.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || eventData.pointerId != activePointerId) return;
        if (PointerWorld(eventData, out Vector3 pointer))
            rect.position = pointer + pointerOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging || eventData.pointerId != activePointerId) return;
        dragging = false;
        game.DropDraggedDisk(diskNumber, sourceRod,
            eventData.position, eventData.pressEventCamera);
        sourceRod = -1;
    }

    private bool PointerWorld(PointerEventData eventData, out Vector3 point)
    {
        // All disk movement uses the board's canvas coordinate system, even when
        // this disk is temporarily parented to the little bonus holder.
        return RectTransformUtility.ScreenPointToWorldPointInRectangle(
            game.board, eventData.position, eventData.pressEventCamera, out point);
    }

    private void OnDisable()
    {
        dragging = false;
        sourceRod = -1;
    }
}
