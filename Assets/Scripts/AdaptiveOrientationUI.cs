using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Where Is My Car: switches the existing screen-space controls, HUD and results
/// between landscape/portrait while preserving all existing Button.onClick,
/// MobileTransformButtonScript actions, GameManager references and sprites.
/// Add on an ALWAYS ACTIVE GameObject (the MobileHUDCanvas is recommended).
/// </summary>
[DefaultExecutionOrder(200)]
public class AdaptiveOrientationUI : MonoBehaviour
{
    [Header("Canvas references (assigned by Setup Auto Rotate UI)")]
    public Canvas controlsCanvas;
    public Canvas hudCanvas;
    public Canvas resultCanvas;
    public RectTransform controlsSafeArea;
    public RectTransform hudSafeArea;

    [Header("Existing HUD")]
    public RectTransform topMenuButton;
    public RectTransform timerSign;
    public RectTransform livesPanel;

    [Header("Existing result window")]
    public RectTransform victoryWindow;

    private int lastWidth = -1;
    private int lastHeight = -1;
    private ScreenOrientation lastOrientation;
    private Rect lastSafeArea;

    private void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Permit both portrait and landscape on a physical Android phone.
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
#endif
    }

    private void Start() => ApplyLayout(true);

    private void LateUpdate()
    {
        if (Screen.width != lastWidth || Screen.height != lastHeight ||
            Screen.orientation != lastOrientation || Screen.safeArea != lastSafeArea)
            ApplyLayout(true);
    }

    private void ApplyLayout(bool force)
    {
        if (Screen.width < 1 || Screen.height < 1) return;
        bool portrait = Screen.height > Screen.width;

        SetScaler(controlsCanvas, portrait);
        SetScaler(hudCanvas, portrait);
        SetScaler(resultCanvas, portrait);
        Canvas.ForceUpdateCanvases();
        FitSafeArea(controlsSafeArea);
        FitSafeArea(hudSafeArea);

        RectTransform rootControls = controlsSafeArea != null ? controlsSafeArea : controlsCanvas?.transform as RectTransform;
        if (rootControls != null)
        {
            if (portrait)
            {
                PositionControl(rootControls, "RotateCounterclockwise", false, 140, 430, 140);
                PositionControl(rootControls, "RotateClockwise", false, 315, 430, 140);
                PositionControl(rootControls, "Flip", false, 225, 252, 140);
                PositionControl(rootControls, "WidthPlus", true, 315, 430, 140);
                PositionControl(rootControls, "WidthMinus", true, 140, 430, 140);
                PositionControl(rootControls, "HeightPlus", true, 315, 252, 140);
                PositionControl(rootControls, "HeightMinus", true, 140, 252, 140);
            }
            else
            {
                PositionControl(rootControls, "RotateCounterclockwise", false, 140, 250, 118);
                PositionControl(rootControls, "RotateClockwise", false, 285, 250, 118);
                PositionControl(rootControls, "Flip", false, 210, 110, 118);
                PositionControl(rootControls, "WidthPlus", true, 285, 250, 118);
                PositionControl(rootControls, "WidthMinus", true, 140, 250, 118);
                PositionControl(rootControls, "HeightPlus", true, 285, 110, 118);
                PositionControl(rootControls, "HeightMinus", true, 140, 110, 118);
            }
        }

        if (portrait)
        {
            Place(topMenuButton, 0, 1, 184, -82, 295, 86);
            Place(timerSign, .5f, 1, 0, -196, 380, 89);
            Place(livesPanel, 1, 1, -235, -82, 365, 80);
            UpdateHearts(70, 12);

            Place(victoryWindow, .5f, .5f, 0, 0, 825, 1040);
            PlaceChild("VictoryRibbon", 0, 392, 570, 95);
            PlaceChild("VictoryTitle", 0, 392, 735, 110);
            PlaceChild("Star1", -178, 225, 138, 138);
            PlaceChild("Star2", 0, 225, 138, 138);
            PlaceChild("Star3", 178, 225, 138, 138);
            PlaceChild("TimeLabel", 0, 67, 560, 62);
            PlaceChild("TimeValue", 0, -20, 610, 76);
            PlaceChild("RestartButton", 0, -210, 675, 110);
            PlaceChild("MainMenuButton", 0, -367, 675, 110);
            SetResultText(42, 53, 41);
        }
        else
        {
            Place(topMenuButton, 0, 1, 195, -90, 300, 84);
            Place(timerSign, .5f, 1, 0, -90, 325, 88);
            Place(livesPanel, 1, 1, -190, -90, 310, 76);
            UpdateHearts(58, 11);

            Place(victoryWindow, .5f, .5f, 0, 0, 900, 630);
            PlaceChild("VictoryRibbon", 0, 228, 550, 100);
            PlaceChild("VictoryTitle", 0, 225, 740, 100);
            PlaceChild("Star1", -158, 104, 116, 116);
            PlaceChild("Star2", 0, 104, 116, 116);
            PlaceChild("Star3", 158, 104, 116, 116);
            PlaceChild("TimeLabel", -145, -20, 250, 68);
            PlaceChild("TimeValue", 152, -20, 340, 74);
            PlaceChild("RestartButton", -225, -190, 400, 92);
            PlaceChild("MainMenuButton", 225, -190, 400, 92);
            SetResultText(40, 51, 37);
        }

        StyleText(topMenuButton, portrait ? 38f : 38f);
        StyleText(timerSign, portrait ? 48f : 43f);

        lastWidth = Screen.width;
        lastHeight = Screen.height;
        lastOrientation = Screen.orientation;
        lastSafeArea = Screen.safeArea;
    }

    private static void SetScaler(Canvas canvas, bool portrait)
    {
        if (canvas == null) return;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) return;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = portrait ? new Vector2(1080, 1920) : new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = portrait ? 0f : 1f;
    }

    private static void FitSafeArea(RectTransform t)
    {
        if (t == null || Screen.width < 1 || Screen.height < 1) return;
        Rect safe = Screen.safeArea;
        if (safe.width < 1 || safe.height < 1) safe = new Rect(0, 0, Screen.width, Screen.height);
        t.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        t.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        t.offsetMin = Vector2.zero;
        t.offsetMax = Vector2.zero;
        t.localScale = Vector3.one;
    }

    private static void PositionControl(Transform root, string name, bool right, float offsetX, float y, float size)
    {
        Transform t = root.Find(name);
        if (t == null) return;
        Place(t as RectTransform, right ? 1 : 0, 0,
            right ? -offsetX : offsetX, y, size, size);
        Image img = t.GetComponent<Image>();
        if (img != null) img.preserveAspect = true;
    }

    private static void Place(RectTransform rt, float anchorX, float anchorY,
        float x, float y, float width, float height)
    {
        if (rt == null) return;
        Vector2 anchor = new Vector2(anchorX, anchorY);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition3D = new Vector3(x, y, 0f);
        rt.sizeDelta = new Vector2(width, height);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    private void PlaceChild(string name, float x, float y, float w, float h)
    {
        if (victoryWindow == null) return;
        Place(victoryWindow.Find(name) as RectTransform, .5f, .5f, x, y, w, h);
    }

    private void UpdateHearts(float iconSize, float spacing)
    {
        if (livesPanel == null) return;
        HorizontalLayoutGroup layout = livesPanel.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            layout.spacing = spacing;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;
        }
        foreach (Transform t in livesPanel)
        {
            RectTransform rt = t as RectTransform;
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(iconSize, iconSize);
                rt.localScale = Vector3.one;
            }
            Image img = t.GetComponent<Image>();
            if (img != null) { img.preserveAspect = true; img.raycastTarget = false; }
        }
    }

    private void SetResultText(float buttonSize, float timeSize, float labelSize)
    {
        if (victoryWindow == null) return;
        StyleText(victoryWindow.Find("RestartButton"), buttonSize);
        StyleText(victoryWindow.Find("MainMenuButton"), buttonSize);
        StyleText(victoryWindow.Find("TimeValue"), timeSize);
        StyleText(victoryWindow.Find("TimeLabel"), labelSize);
        StyleText(victoryWindow.Find("VictoryTitle"), Screen.height > Screen.width ? 67f : 65f);
    }

    private static void StyleText(Transform t, float size)
    {
        if (t == null) return;
        TMP_Text txt = t.GetComponentInChildren<TMP_Text>(true);
        if (txt == null) return;
        txt.enableAutoSizing = true;
        txt.fontSize = size;
        txt.fontSizeMax = size;
        txt.fontSizeMin = Mathf.Min(23, size);
        txt.alignment = TextAlignmentOptions.Center;
    }
}
