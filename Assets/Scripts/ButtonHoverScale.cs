using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class ButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, Min(1f)] private float hoverScale = 1.08f;
    [SerializeField, Min(0.01f)] private float duration = 0.15f;

    private Button button;
    private Vector3 originalScale;
    private Vector3 fromScale;
    private Vector3 targetScale;
    private float elapsed;
    private bool animating;
    internal bool interactable;

    private void Awake()
    {
        button = GetComponent<Button>();
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Application.isMobilePlatform) return;
        if (button.IsInteractable())
            AnimateTo(originalScale * Mathf.Max(1f, hoverScale));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (Application.isMobilePlatform) return;
        AnimateTo(originalScale);
    }

    private void AnimateTo(Vector3 scale)
    {
        fromScale = transform.localScale;
        targetScale = scale;
        elapsed = 0f;
        animating = true;
    }

    private void Update()
    {
        if (!animating)
            return;

        elapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
        transform.localScale = Vector3.Lerp(
            fromScale, targetScale, Mathf.SmoothStep(0f, 1f, progress));
        animating = progress < 1f;
    }

    private void OnDisable()
    {
        animating = false;
        transform.localScale = originalScale;
    }
}
