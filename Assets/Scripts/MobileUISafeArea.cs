using UnityEngine;

/// <summary>
/// Keeps HUD and gameplay buttons inside the phone's safe area
/// (cutouts, rounded corners, gesture insets). Attach to a RectTransform
/// directly under a Screen Space Overlay Canvas.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MobileUISafeArea : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea;
    private int lastWidth = -1;
    private int lastHeight = -1;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (Application.isPlaying) Apply();
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        if (Screen.width <= 0 || Screen.height <= 0) return;
        if (Screen.width != lastWidth || Screen.height != lastHeight || Screen.safeArea != lastSafeArea)
            Apply();
    }

    private void Apply()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Rect safe = Screen.safeArea;
        if (safe.width <= 0 || safe.height <= 0)
            safe = new Rect(0, 0, Screen.width, Screen.height);

        rectTransform.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        rectTransform.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        lastSafeArea = safe;
        lastWidth = Screen.width;
        lastHeight = Screen.height;
    }
}
