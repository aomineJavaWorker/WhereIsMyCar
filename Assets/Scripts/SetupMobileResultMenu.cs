#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Run from Tools > Where Is My Car > Setup Mobile Win/Lose Menu.
/// Reuses the original scene objects and preserves GameManager's references.
/// Unity 6 / UGUI.
/// </summary>
public static class SetupMobileResultMenu
{
    [MenuItem("Tools/Where Is My Car/Setup Mobile Win-Lose Menu")]
    public static void Run()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Mobile Result Menu", "Stop Play Mode before changing the scene.", "OK");
            return;
        }

        GameManager manager = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        if (manager == null || manager.winPanel == null)
        {
            EditorUtility.DisplayDialog("Mobile Result Menu", "Open CityScene and check GameManager > Win Panel.", "OK");
            return;
        }

        GameObject panel = manager.winPanel;
        Transform window = panel.transform.Find("VictoryWindow");
        Transform darkBackground = panel.transform.Find("DarkBackground");
        Transform menuButton = panel.transform.Find("MainMenuButton");
        if (window == null || darkBackground == null || menuButton == null)
        {
            EditorUtility.DisplayDialog("Mobile Result Menu", "Could not find VictoryWindow, DarkBackground, or MainMenuButton under WinPanel. Scene hierarchy was not changed.", "OK");
            return;
        }

        string[] elements = { "VictoryTitle", "Star1", "Star2", "Star3", "TimeLabel", "TimeValue", "RestartButton" };
        foreach (string element in elements)
        {
            if (window.Find(element) == null)
            {
                EditorUtility.DisplayDialog("Mobile Result Menu", "Missing WinPanel/VictoryWindow/" + element + ". Scene hierarchy was not changed.", "OK");
                return;
            }
        }

        GameObject canvasObject = GameObject.Find("MobileResultCanvas");
        if (canvasObject == null)
        {
            canvasObject = new GameObject("MobileResultCanvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create mobile results Canvas");
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas == null) canvas = Undo.AddComponent<Canvas>(canvasObject);
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = Undo.AddComponent<CanvasScaler>(canvasObject);
        if (canvasObject.GetComponent<GraphicRaycaster>() == null)
            Undo.AddComponent<GraphicRaycaster>(canvasObject);

        Undo.RecordObject(canvas, "Configure result Canvas");
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 200; // Above MobileControlsCanvas (100)

        Undo.RecordObject(scaler, "Configure result Canvas scaling");
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        Undo.SetTransformParent(panel.transform, canvasObject.transform, "Move WinPanel to overlay Canvas");
        Stretch(panel.GetComponent<RectTransform>());
        panel.transform.SetAsLastSibling();
        SetImage(panel.transform, new Color(0f, 0f, 0f, 0f), true);

        Stretch(darkBackground.GetComponent<RectTransform>());
        SetImage(darkBackground, new Color(0.055f, 0.082f, 0.125f, 0.77f), true);

        Center(window.GetComponent<RectTransform>(), 0, 0, 840, 980);
        SetImage(window, new Color(0.94f, 0.865f, 0.72f, 1f), true);

        Transform ribbon = window.Find("VictoryRibbon");
        if (ribbon != null)
        {
            Center(ribbon.GetComponent<RectTransform>(), 0, 338, 500, 110);
            Image ribbonImage = ribbon.GetComponent<Image>();
            if (ribbonImage != null)
            {
                Undo.RecordObject(ribbonImage, "Ribbon raycast");
                ribbonImage.raycastTarget = false;
            }
        }

        Transform title = window.Find("VictoryTitle");
        Center(title.GetComponent<RectTransform>(), 0, 335, 710, 115);
        StyleText(title, 72, new Color(0.32f, 0.23f, 0.16f, 1f));

        Center(window.Find("Star1").GetComponent<RectTransform>(), -185, 170, 144, 144);
        Center(window.Find("Star2").GetComponent<RectTransform>(), 0, 170, 144, 144);
        Center(window.Find("Star3").GetComponent<RectTransform>(), 185, 170, 144, 144);

        Transform timeLabel = window.Find("TimeLabel");
        Center(timeLabel.GetComponent<RectTransform>(), 0, 30, 520, 66);
        StyleText(timeLabel, 48, new Color(0.42f, 0.33f, 0.25f, 1f));

        Transform timeValue = window.Find("TimeValue");
        Center(timeValue.GetComponent<RectTransform>(), 0, -55, 610, 90);
        StyleText(timeValue, 58, new Color(0.29f, 0.23f, 0.17f, 1f));

        Transform restart = window.Find("RestartButton");
        Center(restart.GetComponent<RectTransform>(), 0, -225, 660, 120);
        SetImage(restart, new Color(0.29f, 0.57f, 0.36f, 1f), true);
        StyleText(restart, 44, Color.white, "SPĒLĒT VĒLREIZ");

        // This button is originally a direct child of WinPanel, unlike RestartButton.
        // Keep its existing OnClick() pointing at GameManager.GoToMainMenu().
        Undo.SetTransformParent(menuButton, window, "Move result menu button into window");
        Center(menuButton.GetComponent<RectTransform>(), 0, -365, 660, 120);
        SetImage(menuButton, new Color(0.52f, 0.39f, 0.29f, 1f), true);
        StyleText(menuButton, 42, Color.white, "GALVENĀ IZVĒLNE");

        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(canvasObject);
        EditorSceneManager.MarkSceneDirty(panel.scene);
        EditorUtility.DisplayDialog("Mobile Result Menu",
            "Mobile win/lose menu configured. Save CityScene (Ctrl+S). To preview: temporarily enable WinPanel, then disable it again before Play.", "OK");
    }

    private static void Stretch(RectTransform rect)
    {
        if (rect == null) return;
        Undo.RecordObject(rect, "Stretch results background");
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchoredPosition3D = Vector3.zero;
    }

    private static void Center(RectTransform rect, float x, float y, float width, float height)
    {
        if (rect == null) return;
        Undo.RecordObject(rect, "Place mobile result menu item");
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition3D = new Vector3(x, y, 0f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void SetImage(Transform obj, Color color, bool raycast)
    {
        if (obj == null) return;
        Image image = obj.GetComponent<Image>();
        if (image == null) return;
        Undo.RecordObject(image, "Set mobile result UI color");
        image.color = color;
        image.raycastTarget = raycast;
    }

    private static void StyleText(Transform obj, float size, Color color, string newText = null)
    {
        if (obj == null) return;
        TMP_Text text = obj.GetComponentInChildren<TMP_Text>(true);
        if (text == null) return;
        Undo.RecordObject(text, "Style mobile result menu text");
        if (newText != null) text.text = newText;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Min(size, 28f);
        text.fontSizeMax = size;
        text.fontSize = size;
    }
}
#endif
