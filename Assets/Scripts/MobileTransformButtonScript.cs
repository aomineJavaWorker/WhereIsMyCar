using UnityEngine;
using UnityEngine.EventSystems;

// Attach to each UI button. PointerDown starts a continuous action;
// PointerUp/Exit stops it. Flip is a one-tap action.
public class MobileTransformButtonScript : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public enum Action
    {
        RotateClockwise,
        RotateCounterclockwise,
        WidthPlus,
        WidthMinus,
        HeightPlus,
        HeightMinus,
        Flip
    }

    public Action action;
    public ObjectTransformationScript transformation;
    private bool isPressed;

    private void Awake()
    {
        if (transformation == null)
            transformation = FindFirstObjectByType<ObjectTransformationScript>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (transformation == null || isPressed) return;
        if (action == Action.Flip)
        {
            transformation.FlipSelected();
            return;
        }
        isPressed = true;
        SetPressed(true);
    }

    public void OnPointerUp(PointerEventData eventData) => Release();
    public void OnPointerExit(PointerEventData eventData) => Release();
    private void OnDisable() => Release();

    private void Release()
    {
        if (!isPressed) return;
        isPressed = false;
        SetPressed(false);
    }

    private void SetPressed(bool state)
    {
        if (transformation == null) return;
        switch (action)
        {
            case Action.RotateClockwise:
                if (state) transformation.StartRotateCW();
                else transformation.StopRotateCW();
                break;
            case Action.RotateCounterclockwise:
                if (state) transformation.StartRotateCCW();
                else transformation.StopRotateCCW();
                break;
            case Action.WidthPlus:
                if (state) transformation.StartScaleUpX();
                else transformation.StopScaleUpX();
                break;
            case Action.WidthMinus:
                if (state) transformation.StartScaleDownX();
                else transformation.StopScaleDownX();
                break;
            case Action.HeightPlus:
                if (state) transformation.StartScaleUpY();
                else transformation.StopScaleUpY();
                break;
            case Action.HeightMinus:
                if (state) transformation.StartScaleDownY();
                else transformation.StopScaleDownY();
                break;
        }
    }
}
