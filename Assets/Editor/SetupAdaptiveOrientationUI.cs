using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-time editor setup for existing CityScene objects, compatible with
/// both the earlier landscape setup and the original portrait layout.
/// Does not add duplicate buttons or replace scene event callbacks.
/// </summary>
public static class SetupAdaptiveOrientationUI
{
    [MenuItem("Tools/Where Is My Car/Setup Auto Rotate UI (Portrait + Landscape)")]
    public static void Setup()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Auto Rotate UI", "Exit Play Mode before running setup.", "OK");
            return;
        }

        GameManager game = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        Canvas world = GameObject.Find("Canvas")?.GetComponent<Canvas>();
        Canvas controls = GameObject.Find("MobileControlsCanvas")?.GetComponent<Canvas>();
        if (game == null || game.winPanel == null || world == null || controls == null)
        {
            EditorUtility.DisplayDialog("Auto Rotate UI",
                "Open CityScene first. Missing GameManager/WinPanel, Canvas or MobileControlsCanvas.", "OK");
            return;
        }

        Transform win = game.winPanel.transform.Find("VictoryWindow");
        if (win == null || win.Find("RestartButton") == null)
        {
            EditorUtility.DisplayDialog("Auto Rotate UI", "WinPanel/VictoryWindow/RestartButton was not found.", "OK");
            return;
        }

        Canvas hud = GetOrCreateCanvas("MobileHUDCanvas", 150);
        Canvas result = GetOrCreateCanvas("MobileResultCanvas", 200);
        Configure(controls, 100);
        Configure(hud, 150);
        Configure(result, 200);

        RectTransform controlsArea = GetOrCreateSafeArea(controls);
        RectTransform hudArea = GetOrCreateSafeArea(hud);

        string[] controlNames = { "RotateCounterclockwise", "RotateClockwise", "Flip", "WidthPlus",
            "WidthMinus", "HeightPlus", "HeightMinus" };
        foreach (string name in controlNames)
        {
            Transform t = FindRecursive(controls.transform, name);
            if (t == null)
            {
                EditorUtility.DisplayDialog("Auto Rotate UI", "Missing control button: " + name + ". No new buttons will be created.", "OK");
                return;
            }
            Undo.SetTransformParent(t, controlsArea, "Move existing UI button into SafeArea");
        }

        Transform menu = FindRecursive(world.transform, "MainMenuButton");
        if (menu == null) menu = FindRecursive(hud.transform, "MainMenuButton");
        Transform lives = FindRecursive(world.transform, "LivesPanel");
        if (lives == null) lives = FindRecursive(hud.transform, "LivesPanel");
        Transform timer = FindRecursive(world.transform, "Izkartne");
        if (timer == null) timer = FindRecursive(hud.transform, "Izkartne");
        if (menu == null || lives == null || timer == null)
        {
            EditorUtility.DisplayDialog("Auto Rotate UI", "Missing gameplay MainMenuButton, LivesPanel or Izkartne. Check whether the previous landscape setup completed successfully.", "OK");
            return;
        }
        Undo.SetTransformParent(menu, hudArea, "Move Main Menu to screen HUD");
        Undo.SetTransformParent(lives, hudArea, "Move Hearts to screen HUD");
        Undo.SetTransformParent(timer, hudArea, "Move Timer to screen HUD");

        Undo.SetTransformParent(game.winPanel.transform, result.transform, "Move existing WinPanel into results overlay");
        Stretch(game.winPanel.GetComponent<RectTransform>());
        Transform dim = game.winPanel.transform.Find("DarkBackground");
        if (dim != null) Stretch(dim.GetComponent<RectTransform>());

        Transform winMenu = win.Find("MainMenuButton");
        if (winMenu == null) winMenu = game.winPanel.transform.Find("MainMenuButton");
        if (winMenu != null)
            Undo.SetTransformParent(winMenu, win, "Keep results Main Menu inside results window");
        else
            Debug.LogWarning("Result MainMenuButton not found. Check WinPanel hierarchy.");

        // Avoid invisible UIImage objects intercepting clicks in the HUD.
        Image heartsBg = lives.GetComponent<Image>();
        if (heartsBg != null)
        {
            Undo.RecordObject(heartsBg, "HUD raycast");
            heartsBg.raycastTarget = false;
        }
        Image timerImage = timer.GetComponent<Image>();
        if (timerImage != null)
        {
            Undo.RecordObject(timerImage, "HUD timer raycast");
            timerImage.raycastTarget = false;
        }
        TMP_Text timerText = timer.GetComponentInChildren<TMP_Text>(true);
        if (timerText != null)
        {
            Undo.RecordObject(timerText, "HUD timer text raycast");
            timerText.raycastTarget = false;
        }

        AdaptiveOrientationUI ui = hud.GetComponent<AdaptiveOrientationUI>();
        if (ui == null) ui = Undo.AddComponent<AdaptiveOrientationUI>(hud.gameObject);
        Undo.RecordObject(ui, "Configure auto rotate layout");
        ui.controlsCanvas = controls;
        ui.hudCanvas = hud;
        ui.resultCanvas = result;
        ui.controlsSafeArea = controlsArea;
        ui.hudSafeArea = hudArea;
        ui.topMenuButton = menu as RectTransform;
        ui.livesPanel = lives as RectTransform;
        ui.timerSign = timer as RectTransform;
        ui.victoryWindow = win as RectTransform;
        EditorUtility.SetDirty(ui);

        Camera camera = Camera.main;
        if (camera == null) camera = Object.FindFirstObjectByType<Camera>();
        if (camera != null)
        {
            CameraMapViewportFill fill = camera.GetComponent<CameraMapViewportFill>();
            if (fill == null) fill = Undo.AddComponent<CameraMapViewportFill>(camera.gameObject);
            Undo.RecordObject(fill, "Configure map viewport fill");
            GameObject map = GameObject.Find("backgroundImage");
            if (map != null) fill.mapImage = map.GetComponent<RectTransform>();
            fill.boundaries = Object.FindFirstObjectByType<ScreenBounderiesScript>();
            fill.edgeSafety = .994f;
            EditorUtility.SetDirty(fill);
            Undo.RecordObject(camera, "Full screen camera viewport");
            camera.rect = new Rect(0, 0, 1, 1);
        }
        else Debug.LogWarning("No main camera found; map viewport fill wasn't installed.");

        // The older portrait-only and landscape-only SETUP menu items are now obsolete.
        // Their Editor scripts can remain; do not invoke them after this setup.
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        EditorUtility.DisplayDialog("Auto Rotate UI",
            "Setup complete! Save CityScene (Ctrl+S). Test in Play Mode at 1920x1080 AND 1080x1920. Enable Android Auto Rotation with Portrait and Landscape Left/Right in Player Settings.", "OK");
    }

    private static Canvas GetOrCreateCanvas(string name, int sortingOrder)
    {
        GameObject obj = GameObject.Find(name);
        if (obj == null)
        {
            obj = new GameObject(name, typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
        }
        Canvas canvas = obj.GetComponent<Canvas>();
        if (canvas == null) canvas = Undo.AddComponent<Canvas>(obj);
        if (obj.GetComponent<CanvasScaler>() == null) Undo.AddComponent<CanvasScaler>(obj);
        if (obj.GetComponent<GraphicRaycaster>() == null) Undo.AddComponent<GraphicRaycaster>(obj);
        Configure(canvas, sortingOrder);
        return canvas;
    }

    private static void Configure(Canvas canvas, int order)
    {
        Undo.RecordObject(canvas, "Configure auto rotate Canvas");
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = order;
        RectTransform rt = canvas.GetComponent<RectTransform>();
        if (rt != null)
        {
            Undo.RecordObject(rt, "Reset screen canvas transform");
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) return;
        Undo.RecordObject(scaler, "Configure responsive CanvasScaler");
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
    }

    private static RectTransform GetOrCreateSafeArea(Canvas canvas)
    {
        Transform t = canvas.transform.Find("SafeArea");
        if (t == null)
        {
            GameObject safe = new GameObject("SafeArea", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(safe, "Create SafeArea");
            Undo.SetTransformParent(safe.transform, canvas.transform, "Create SafeArea");
            t = safe.transform;
        }
        RectTransform rect = t as RectTransform;
        Stretch(rect);
        return rect;
    }

    private static void Stretch(RectTransform rt)
    {
        if (rt == null) return;
        Undo.RecordObject(rt, "Stretch screen UI");
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(.5f, .5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition3D = Vector3.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    private static Transform FindRecursive(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
