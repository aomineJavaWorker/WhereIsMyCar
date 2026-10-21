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
    public RectTransform adsPanel;

    [Header("Existing result window")]
    public RectTransform victoryWindow;

    private int lastWidth = -1;
    private int lastHeight = -1;
    private ScreenOrientation lastOrientation;
    private Rect lastSafeArea;
    private RectTransform leftControls, rightControls;
    private RectTransform[] landscapeSpaces;
    private readonly string[] controlNames = { "RotateCounterclockwise", "RotateClockwise",
        "Flip", "WidthPlus", "WidthMinus", "HeightPlus", "HeightMinus" };
    private RectTransform[] buttons;

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

    private void Start() => ApplyLayout();

    private void LayoutControls(bool portrait)
    {
        RectTransform root = controlsSafeArea != null ? controlsSafeArea : controlsCanvas?.transform as RectTransform;
        if (root == null) return;
        if (buttons == null)
        {
            buttons = new RectTransform[controlNames.Length];
            for (int i = 0; i < buttons.Length; i++)
                buttons[i] = root.Find(controlNames[i]) as RectTransform;
            leftControls = CreateGrid(root, "LeftControls");
            rightControls = CreateGrid(root, "RightControls");
            // Keep the five surviving landscape buttons in their existing cells.
            // These empty RectTransforms have no graphics or input handlers.
            landscapeSpaces = new RectTransform[2];
            for (int i = 0; i < landscapeSpaces.Length; i++)
            {
                GameObject space = new GameObject("LandscapeSpacing" + i, typeof(RectTransform));
                space.transform.SetParent(rightControls, false);
                landscapeSpaces[i] = space.GetComponent<RectTransform>();
            }
        }

        float width = root.rect.width;
        float height = root.rect.height;
        float size = portrait ? Mathf.Min(156f, (width - 96f) / 4f, (height - 560f - 48f) / 5f) :
            Mathf.Min(144f, (height - 230f) / 3f, (width - 96f) / 5f);
        size = Mathf.Max(48f, size);
        const float gap = 12f;
        foreach (RectTransform space in landscapeSpaces) space.gameObject.SetActive(!portrait);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            MobileTransformButtonScript held = buttons[i].GetComponent<MobileTransformButtonScript>();
            if (held != null) held.CancelPress();
            buttons[i].SetParent(i >= 2 ? rightControls : leftControls, false);
            buttons[i].SetAsLastSibling();
            buttons[i].localScale = Vector3.one;
            buttons[i].localRotation = Quaternion.identity;
            Image image = buttons[i].GetComponent<Image>();
            if (image != null) image.preserveAspect = true;
        }
        ConfigureGrid(leftControls, portrait ? 1 : 2, size, gap);
        ConfigureGrid(rightControls, portrait ? 1 : 3, size, gap);
        if (adsPanel == null) adsPanel = controlsCanvas.transform.Find("Ads") as RectTransform;
        if (adsPanel != null)
        {
            if (adsPanel.parent != root) adsPanel.SetParent(root, false);
            HorizontalLayoutGroup adsLayout = adsPanel.GetComponent<HorizontalLayoutGroup>();
            if (adsLayout == null) adsLayout = adsPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            adsLayout.spacing = gap;
            adsLayout.childControlWidth = adsLayout.childControlHeight = false;
            adsLayout.childForceExpandWidth = adsLayout.childForceExpandHeight = false;
            adsLayout.childAlignment = TextAnchor.MiddleCenter;
            foreach (RectTransform child in adsPanel)
            {
                child.sizeDelta = new Vector2(112f, 112f);
                child.localScale = Vector3.one;
                Image image = child.GetComponent<Image>();
                if (image != null) image.preserveAspect = true;
            }
            Place(adsPanel, portrait ? .5f : 0f, 1f, portrait ? 0f : 204f,
                portrait ? -408f : -220f, 360f, 112f);
        }
        if (portrait)
        {
            float centerY = (height - 480f + 24f) * .5f;
            float leftHeight = size * 2f + gap;
            float rightHeight = size * 5f + gap * 4f;
            Place(leftControls, 0f, 0f, 24f + size / 2f, centerY, size, leftHeight);
            Place(rightControls, 1f, 0f, -24f - size / 2f, centerY, size, rightHeight);
        }
        else
        {
            Place(leftControls, 0f, 0f, 24f + (size * 2f + gap) / 2f,
                24f + size / 2f, size * 2f + gap, size);
            float w = size * 3f + gap * 2f;
            float h = size * 3f + gap * 2f;
            Place(rightControls, 1f, 0f, -24f - w / 2f, 24f + h / 2f, w, h);
        }
    }
    private void LayoutTimer(bool portrait)
    {
        if (timerSign == null) return;
        Image background = timerSign.GetComponent<Image>();
        float aspect = background != null && background.sprite != null
            ? background.sprite.rect.width / background.sprite.rect.height : 844f / 373f;
        float width = portrait ? 380f : 325f;
        if (hudSafeArea != null) width = Mathf.Min(width, hudSafeArea.rect.width * .44f);
        Place(timerSign, .5f, 1f, 0f, portrait ? -254f : -90f, width, width / aspect);
        if (background != null)
        {
            background.type = Image.Type.Simple;
            background.preserveAspect = true;
            background.raycastTarget = false;
        }
        TMP_Text text = timerSign.GetComponentInChildren<TMP_Text>(true);
        if (text == null) return;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, .5f);
        rect.offsetMin = new Vector2(16f, 12f);
        rect.offsetMax = new Vector2(-16f, -12f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        StyleText(text.transform, portrait ? 48f : 43f);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
    }

    private static RectTransform CreateGrid(Transform root, string name)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(GridLayoutGroup));
        panel.transform.SetParent(root, false);
        return panel.GetComponent<RectTransform>();
    }

    private static void ConfigureGrid(RectTransform panel, int columns, float size, float gap)
    {
        GridLayoutGroup layout = panel.GetComponent<GridLayoutGroup>();
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = columns;
        layout.cellSize = new Vector2(size, size);
        layout.spacing = new Vector2(gap, gap);
        layout.childAlignment = TextAnchor.MiddleCenter;
    }

    private void FitResultToSafeArea()
    {
        if (victoryWindow == null || resultCanvas == null) return;
        Rect safe = Screen.safeArea;
        if (safe.width <= 0 || safe.height <= 0) safe = new Rect(0, 0, Screen.width, Screen.height);
        RectTransform canvasRect = resultCanvas.transform as RectTransform;
        float width = canvasRect.rect.width * safe.width / Screen.width;
        float height = canvasRect.rect.height * safe.height / Screen.height;
        float scale = Mathf.Min(1f, (width - 48f) / victoryWindow.sizeDelta.x,
            (height - 48f) / victoryWindow.sizeDelta.y);
        victoryWindow.anchorMin = victoryWindow.anchorMax = new Vector2(safe.center.x / Screen.width, safe.center.y / Screen.height);
        victoryWindow.localScale = Vector3.one * Mathf.Max(.1f, scale);
    }

    private void LateUpdate()
    {
        if (Screen.width != lastWidth || Screen.height != lastHeight ||
            Screen.orientation != lastOrientation || Screen.safeArea != lastSafeArea)
            ApplyLayout();
    }

    private void ApplyLayout()
    {
        if (Screen.width < 1 || Screen.height < 1) return;
        bool portrait = Screen.height > Screen.width;

        SetScaler(controlsCanvas, portrait);
        SetScaler(hudCanvas, portrait);
        SetScaler(resultCanvas, portrait);
        Canvas.ForceUpdateCanvases();
        FitSafeArea(controlsSafeArea);
        FitSafeArea(hudSafeArea);
        Canvas.ForceUpdateCanvases();

        LayoutControls(portrait);

        if (portrait)
        {
            Place(topMenuButton, 0, 1, 184, -82, 295, 144);
            LayoutTimer(true);
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
            PlaceChild("RestartButton", 0, -210, 675, 144);
            PlaceChild("MainMenuButton", 0, -367, 675, 144);
            SetResultText(42, 53, 41);
        }
        else
        {
            Place(topMenuButton, 0, 1, 195, -90, 300, 144);
            LayoutTimer(false);
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
            PlaceChild("RestartButton", -225, -190, 400, 144);
            PlaceChild("MainMenuButton", 225, -190, 400, 144);
            SetResultText(40, 51, 37);
        }

        StyleText(topMenuButton, portrait ? 38f : 38f);


        FitResultToSafeArea();

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
