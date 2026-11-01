
using UnityEngine;
using UnityEngine.EventSystems;

public class HanoiDiskDrag : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Range(1, 4)]
    public int diskNumber = 1;

    private HanojasTornis game;
    private RectTransform rect;
    private Vector3 grabOffset;
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
        if (game == null ||
            !game.CanDragDisk(diskNumber, out int rod))
            return;

        if (!GetPointerWorld(eventData, out Vector3 pointer))
            return;

        sourceRod = rod;
        activePointerId = eventData.pointerId;
        grabOffset = rect.position - pointer;
        dragging = true;

        // Draw the dragged disk over the other disks.
        rect.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging ||
            eventData.pointerId != activePointerId)
            return;

        if (GetPointerWorld(eventData, out Vector3 pointer))
            rect.position = pointer + grabOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging ||
            eventData.pointerId != activePointerId)
            return;

        dragging = false;

        game.DropDraggedDisk(
            diskNumber,
            sourceRod,
            eventData.position,
            eventData.pressEventCamera
        );

        sourceRod = -1;
    }

    private bool GetPointerWorld(
        PointerEventData eventData,
        out Vector3 world)
    {
        return RectTransformUtility
            .ScreenPointToWorldPointInRectangle(
                game.board,
                eventData.position,
                eventData.pressEventCamera,
                out world
            );
    }

    private void OnDisable()
    {
        dragging = false;
        sourceRod = -1;
    }
}
