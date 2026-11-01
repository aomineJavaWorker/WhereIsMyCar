using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SetupHanojasTornis
{
    private const string ScenePath = "Assets/Scenes/HanojasTornis.unity";
    private static readonly Color Ink = new Color(.22f, .13f, .08f);
    private static TMP_FontAsset font;
    private static Sprite round;

    [MenuItem("Tools/Where Is My Car/Create or Rebuild Hanojas Tornis")]
    public static void Create()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        font = CreateFont();
        round = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // Copy the existing configured SDK components, preserving placement IDs and test mode.
        var city = EditorSceneManager.OpenScene("Assets/Scenes/CityScene.unity", OpenSceneMode.Single);
        var source = city.GetRootGameObjects().Single(x => x.GetComponent<AdManager>() != null);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var ads = Object.Instantiate(source);
        ads.name = "AdsScriptHolder";
        SceneManager.MoveGameObjectToScene(ads, scene);
        ClearReference(ads.GetComponent<InterstitialAd>(), "_interstitialAdButton");
        ClearReference(ads.GetComponent<RewardedAds>(), "_rewardedAdButton");
        ClearReference(ads.GetComponent<BannerAd>(), "_bannerButton");
        EditorSceneManager.CloseScene(city, true);

        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.96f, .92f, .83f);
        camera.orthographic = true;
        camera.transform.position = new Vector3(0, 0, -10);
        var canvas = new GameObject("HanojasCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var game = canvas.gameObject.AddComponent<HanojasTornis>();
        Panel("Background", canvas.transform, new Color(.96f, .92f, .83f), 0, 0, 1, 1);
        var safe = Rect("SafeArea", canvas.transform, 0, 0, 1, 1);
        safe.gameObject.AddComponent<MobileUISafeArea>();
        game.safeArea = safe;
        Text("Title", safe, "HANOJAS TORNIS", .04f, .84f, .96f, .96f, 76, true);
        game.movesText = Text("Moves", safe, "Gājieni: 0", .12f, .75f, .88f, .82f, 46, true);
        var board = Panel("Board", safe, new Color(.89f, .80f, .65f), .035f, .30f, .965f, .72f);
        game.board = board.rectTransform;
        game.rods = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            var area = Panel("Rod" + (i + 1), board.transform,
                new Color(1f, .98f, .89f, .22f), i / 3f + .008f, .02f, (i + 1) / 3f - .008f, .98f);
            area.raycastTarget = true;
            var button = area.gameObject.AddComponent<Button>();
            button.targetGraphic = area;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            UnityEventTools.AddIntPersistentListener(button.onClick, game.SelectRod, i);
            game.rods[i] = button;
            Panel("VerticalRod", area.transform, new Color(.47f, .29f, .14f), .475f, .15f, .525f, .85f);
            Panel("Base", area.transform, new Color(.38f, .23f, .12f), .04f, .12f, .96f, .16f);
            Text("RodLabel", area.transform, i == 2 ? "3 · MĒRĶIS" : (i + 1).ToString(), .02f, .005f, .98f, .11f, 28, true);
        }
        Color[] colors = { new Color(.22f, .60f, .72f), new Color(.41f, .66f, .32f),
            new Color(.94f, .65f, .17f), new Color(.84f, .35f, .26f) };
        game.disks = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            var disk = Panel("Disk" + (i + 1), board.transform, colors[i], 0, 0, 0, 0);
            var outline = disk.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, .85f);
            outline.effectDistance = new Vector2(6, -6);
            outline.enabled = false;
            Text("Size", disk.transform, (i + 1).ToString(), 0, .04f, 1, .96f, 32, true).color = Color.white;
            game.disks[i] = disk;
        }
        game.hintText = Text("Instructions", safe,
            "Pārvieto visus diskus uz 3. stieni. Nospied disku, tad stieni.", .06f, .195f, .94f, .28f, 32, false);
        var restart = Button("RestartButton", safe, "Restart", .06f, .025f, .48f, .18f);
        var menu = Button("MainMenuButton", safe, "Main Menu", .52f, .025f, .94f, .18f);
        UnityEventTools.AddPersistentListener(restart.onClick, game.RestartGame);
        UnityEventTools.AddPersistentListener(menu.onClick, game.OpenMainMenu);

        var overlay = Panel("VictoryOverlay", canvas.transform, new Color(.10f, .07f, .04f, .78f), 0, 0, 1, 1);
        overlay.raycastTarget = true;
        var victorySafe = Rect("VictorySafeArea", overlay.transform, 0, 0, 1, 1);
        victorySafe.gameObject.AddComponent<MobileUISafeArea>();
        var window = Panel("VictoryWindow", victorySafe, new Color(1f, .96f, .85f), .08f, .10f, .92f, .90f);
        Text("VictoryTitle", window.transform, "UZVARA!", .05f, .75f, .95f, .94f, 80, true);
        game.victoryMovesText = Text("VictoryMoves", window.transform, "Gājieni: 0\nMinimums: 15", .06f, .52f, .94f, .72f, 46, false);
        var again = Button("PlayAgainButton", window.transform, "Spēlēt vēlreiz", .09f, .31f, .91f, .50f);
        var back = Button("VictoryMenuButton", window.transform, "Galvenā izvēlne", .09f, .075f, .91f, .265f);
        UnityEventTools.AddPersistentListener(again.onClick, game.RestartGame);
        UnityEventTools.AddPersistentListener(back.onClick, game.OpenMainMenu);
        game.victoryOverlay = overlay.gameObject;
        overlay.gameObject.SetActive(false);
        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        Canvas.ForceUpdateCanvases();
        game.RefreshDisks();
        EditorSceneManager.SaveScene(scene, ScenePath);

        var main = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        var mainMenu = Object.FindFirstObjectByType<MainMenu>();
        var old = mainMenu.transform.Find("HanojasButton");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var start = mainMenu.transform.Find("StartButton");
        var entry = Object.Instantiate(start.gameObject, mainMenu.transform);
        entry.name = "HanojasButton";
        entry.GetComponentInChildren<TMP_Text>().text = "Hanojas Tornis";
        var entryButton = entry.GetComponent<Button>();
        entryButton.onClick = new Button.ButtonClickedEvent();
        UnityEventTools.AddPersistentListener(entryButton.onClick, mainMenu.StartHanojasTornis);
        // Save a useful editor preview too; MainMenu applies its safe-area layout at runtime.
        PlaceMenu(start as RectTransform, .53f);
        PlaceMenu(entry.transform as RectTransform, .36f);
        PlaceMenu(mainMenu.transform.Find("ExitButton") as RectTransform, .19f);
        EditorSceneManager.SaveScene(main);
        var scenes = EditorBuildSettings.scenes.ToList();
        scenes.RemoveAll(x => x.path == ScenePath);
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("HANOI: saved scene, menu button, font and build settings.");
    }

    private static void PlaceMenu(RectTransform item, float y)
    {
        item.anchorMin = item.anchorMax = new Vector2(.5f, y);
        item.pivot = new Vector2(.5f, .5f);
        item.anchoredPosition = Vector2.zero;
        item.sizeDelta = new Vector2(700, 120);
    }

    private static void ClearReference(Object component, string property)
    {
        var serialized = new SerializedObject(component);
        serialized.FindProperty(property).objectReferenceValue = null;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TMP_FontAsset CreateFont()
    {
        const string path = "Assets/Fonts/HanojasSans.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (existing != null) return existing;
        var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
        var asset = TMP_FontAsset.CreateFontAsset(source);
        asset.name = "HanojasSans";
        AssetDatabase.CreateAsset(asset, path);
        string characters = string.Concat(Enumerable.Range(32, 95).Select(x => (char)x)) + "ĀāČčĒēĢģĪīĶķĻļŅņŠšŪūŽž·";
        if (!asset.TryAddCharacters(characters, out string missing))
            throw new InvalidOperationException("Missing font characters: " + missing);
        asset.atlasPopulationMode = AtlasPopulationMode.Static;
        foreach (var texture in asset.atlasTextures) AssetDatabase.AddObjectToAsset(texture, asset);
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static RectTransform Rect(string name, Transform parent, float x0, float y0, float x1, float y1)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(x0, y0);
        rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static Image Panel(string name, Transform parent, Color color, float x0, float y0, float x1, float y1)
    {
        var image = Rect(name, parent, x0, y0, x1, y1).gameObject.AddComponent<Image>();
        image.color = color;
        image.sprite = round;
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text Text(string name, Transform parent, string value, float x0, float y0, float x1, float y1, float size, bool bold)
    {
        var text = Rect(name, parent, x0, y0, x1, y1).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.color = Ink;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 16;
        text.fontSizeMax = size;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static Button Button(string name, Transform parent, string value, float x0, float y0, float x1, float y1)
    {
        var panel = Panel(name, parent, new Color(.98f, .74f, .28f), x0, y0, x1, y1);
        panel.raycastTarget = true;
        var button = panel.gameObject.AddComponent<Button>();
        button.targetGraphic = panel;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        Text("Label", panel.transform, value, .035f, .08f, .965f, .92f, 44, true);
        return button;
    }
}
