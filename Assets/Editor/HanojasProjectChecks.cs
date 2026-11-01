using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Batch entry points, also available from Tools. Never saves CityScene.</summary>
public static class HanojasProjectChecks
{
    public static void GenerateAndValidate()
    {
        SetupHanojasTornis.Create();
        Validate();
    }

    [MenuItem("Tools/Where Is My Car/Validate Hanojas Integration")]
    public static void Validate()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        CheckRules();
        string[] names = { "MainMenu", "CityScene", "HanojasTornis" };
        Require(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => Path.GetFileNameWithoutExtension(s.path))
            .SequenceEqual(names), "Build scenes/order");
        foreach (string name in names)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
            foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0,
                        name + ": missing script on " + transform.name);
                    foreach (var component in transform.GetComponents<Component>())
                    {
                        var properties = new SerializedObject(component).GetIterator();
                        while (properties.NextVisible(true))
                            if (properties.propertyType == SerializedPropertyType.ObjectReference)
                                Require(properties.objectReferenceValue != null || properties.objectReferenceInstanceIDValue == 0,
                                    name + ": broken reference " + transform.name + "." + properties.propertyPath);
                    }
                }
            Require(Object.FindFirstObjectByType<EventSystem>() != null, name + ": EventSystem");
            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    var target = button.onClick.GetPersistentTarget(i);
                    string method = button.onClick.GetPersistentMethodName(i);
                    Require(target != null && target.GetType().GetMethods().Any(m => m.Name == method),
                        name + ": broken button " + button.name + "." + method);
                }
            if (name == "MainMenu")
            {
                var menu = Object.FindFirstObjectByType<MainMenu>();
                CheckButton(menu.transform.Find("StartButton"), "StartGame");
                CheckButton(menu.transform.Find("HanojasButton"), "StartHanojasTornis");
                CheckButton(menu.transform.Find("ExitButton"), "ExitGame");
            }
            if (name == "CityScene")
            {
                var city = Object.FindFirstObjectByType<GameManager>();
                Require(city != null && city.timerText != null && city.winPanel != null && city.resultTitle != null &&
                    city.winTimeText != null && city.star1 != null && city.star2 != null && city.star3 != null &&
                    city.hearts.Length == 4 && city.hearts.All(h => h != null), "City HUD/result/lives references");
                Require(Object.FindObjectsByType<DragAndDropScript>(FindObjectsSortMode.None).Length == city.totalVehicles,
                    "City draggable vehicle count");
                Require(Object.FindObjectsByType<MobileTransformButtonScript>(FindObjectsSortMode.None).Length == 7,
                    "City seven mobile transformation controls");
                Require(Object.FindFirstObjectByType<AdaptiveOrientationUI>() != null, "City adaptive UI");
            }
            if (name == "HanojasTornis") CheckScene();
        }
        Debug.Log("HANOI CHECKS PASS: rules, all 81 legal configurations, optimal 15 moves, scene references, menu routes, City controls, UI and restart.");
    }

    private static void CheckScene()
    {
        var game = Object.FindFirstObjectByType<HanojasTornis>();
        Require(game != null && game.rods.Length == 3 && game.disks.Length == 4, "Hanoi scene objects");
        Require(game.safeArea.GetComponent<MobileUISafeArea>() != null, "Hanoi safe area");
        Require(game.GetComponent<CanvasScaler>().uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize, "Hanoi scaler");
        Require(Object.FindFirstObjectByType<AdManager>() != null, "Hanoi ads integration");
        game.RestartGame();
        game.SelectRod(1);
        Require(game.SelectedRod == -1 && game.Puzzle.Moves == 0, "Empty rod selection");
        game.SelectRod(0);
        Require(game.disks[0].GetComponent<Outline>().enabled, "Selection outline");
        game.SelectRod(0);
        Require(game.SelectedRod == -1, "Cancel selection");
        game.SelectRod(0); game.SelectRod(2);
        game.SelectRod(0); game.SelectRod(2);
        Require(game.Puzzle.Moves == 1 && game.Puzzle.Top(0) == 2 && game.Puzzle.Top(2) == 1, "Invalid move preserves scene");
        game.RestartGame();
        Solve(4, 0, 2, 1, (a, b) => { game.SelectRod(a); game.SelectRod(b); });
        Require(game.Puzzle.IsWon && game.Puzzle.Moves == 15 && game.victoryOverlay.activeSelf &&
            game.victoryMovesText.text.Contains("15") && game.movesText.text == "Gājieni: 15", "Victory UI");
        game.SelectRod(2); game.SelectRod(0);
        Require(game.Puzzle.Moves == 15, "Victory locks board");
        game.RestartGame();
        Require(!game.victoryOverlay.activeSelf && game.Puzzle.Moves == 0 && game.rods.All(r => r.interactable), "Restart UI");
        // Simulate resolved safe-area sizes in canvas units; proportions are independent of pixel density.
        var root = (RectTransform)game.transform;
        game.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        game.GetComponent<Canvas>().enabled = false;
        root.localScale = Vector3.one;
        game.SelectRod(0); game.SelectRod(2); game.SelectRod(0);
        foreach (var size in new[] { new Vector2(1080, 1920), new Vector2(1920, 1080),
            new Vector2(1080, 2400), new Vector2(2400, 1080), new Vector2(1080, 1080) })
        {
            root.sizeDelta = size;
            game.safeArea.anchorMin = new Vector2(.04f, .04f);
            game.safeArea.anchorMax = new Vector2(.96f, .96f);
            game.RefreshDisks();
            var buttons = game.safeArea.GetComponentsInChildren<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                var bounds = Bounds((RectTransform)buttons[i].transform);
                Require(bounds.width >= 144 && bounds.height >= 144, "Touch target " + buttons[i].name);
                var safe = Bounds(game.safeArea);
                Require(safe.Contains(bounds.min) && safe.Contains(bounds.max), "Button safe bounds");
                for (int j = 0; j < i; j++) Require(!bounds.Overlaps(Bounds((RectTransform)buttons[j].transform)), "Buttons overlap");
            }
            Require(game.Puzzle.Count(0) == 3 && game.Puzzle.Top(2) == 1 && game.Puzzle.Moves == 1 &&
                game.SelectedRod == 0 && game.disks[1].GetComponent<Outline>().enabled,
                "Orientation preserves in-progress puzzle and selection");
            game.victoryOverlay.SetActive(true);
            var victoryButtons = game.victoryOverlay.GetComponentsInChildren<Button>();
            foreach (var button in victoryButtons)
            {
                Rect bounds = Bounds((RectTransform)button.transform);
                Require(bounds.width >= 144 && bounds.height >= 144, "Victory touch target " + button.name);
                var safe = Bounds((RectTransform)game.victoryOverlay.transform.Find("VictorySafeArea"));
                Require(safe.Contains(bounds.min) && safe.Contains(bounds.max), "Victory safe bounds");
            }
            Require(!Bounds((RectTransform)victoryButtons[0].transform).Overlaps(Bounds((RectTransform)victoryButtons[1].transform)),
                "Victory buttons overlap");
            game.victoryOverlay.SetActive(false);
        }
    }

    private static Rect Bounds(RectTransform transform)
    {
        var corners = new Vector3[4]; transform.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }

    private static void CheckButton(Transform transform, string method)
    {
        Require(transform != null, "Menu button " + method);
        var button = transform.GetComponent<Button>();
        Require(Enumerable.Range(0, button.onClick.GetPersistentEventCount()).Any(i =>
            button.onClick.GetPersistentMethodName(i) == method), "Menu callback " + method);
    }

    private static void CheckRules()
    {
        var puzzle = new HanoiPuzzle();
        Require(puzzle.Count(0) == 4 && puzzle.Top(0) == 1, "Initial tower");
        Require(!puzzle.TryMove(1, 2) && !puzzle.TryMove(0, 0) && !puzzle.TryMove(-1, 3), "Invalid sources");
        var queue = new Queue<List<int[]>>();
        var visited = new HashSet<string>();
        queue.Enqueue(new List<int[]>());
        int shortest = -1;
        while (queue.Count > 0)
        {
            var path = queue.Dequeue();
            puzzle.Reset();
            foreach (var move in path) Require(puzzle.TryMove(move[0], move[1]), "Replay");
            string key = Key(puzzle);
            if (!visited.Add(key)) continue;
            for (int r = 0; r < 3; r++)
                for (int l = 1; l < puzzle.Count(r); l++)
                    Require(puzzle.DiskAt(r, l - 1) > puzzle.DiskAt(r, l), "Stack invariant");
            if (puzzle.IsWon) { if (shortest < 0) shortest = path.Count; continue; }
            for (int from = 0; from < 3; from++)
                for (int to = 0; to < 3; to++)
                {
                    puzzle.Reset();
                    foreach (var move in path) puzzle.TryMove(move[0], move[1]);
                    bool legal = from != to && puzzle.Top(from) != 0 &&
                        (puzzle.Top(to) == 0 || puzzle.Top(from) < puzzle.Top(to));
                    int before = puzzle.Moves;
                    bool moved = puzzle.TryMove(from, to);
                    Require(moved == legal && puzzle.Moves == before + (legal ? 1 : 0), "Move legality/counter");
                    if (moved) queue.Enqueue(new List<int[]>(path) { new[] { from, to } });
                    else Require(Key(puzzle) == key, "Illegal move changed state");
                }
        }
        Require(visited.Count == 81 && shortest == 15, "Reachable states / shortest solution");
    }

    private static string Key(HanoiPuzzle puzzle) => string.Join("|", Enumerable.Range(0, 3)
        .Select(r => string.Join(",", Enumerable.Range(0, puzzle.Count(r)).Select(l => puzzle.DiskAt(r, l)))));

    private static void Solve(int n, int from, int to, int other, Action<int, int> move)
    {
        if (n == 0) return;
        Solve(n - 1, from, other, to, move); move(from, to); Solve(n - 1, other, to, from, move);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("CHECK FAILED: " + message);
    }

    [MenuItem("Tools/Where Is My Car/Build Android APK")]
    public static void BuildAndroid()
    {
        Validate();
        Directory.CreateDirectory("Builds/Android");
        EditorUserBuildSettings.buildAppBundle = false;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = "Builds/Android/WhereIsMyCar-HanojasTornis.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        });
        Require(report.summary.result == BuildResult.Succeeded, "APK build: " + report.summary.result);
        Debug.Log("HANOI APK BUILD PASS: " + report.summary.outputPath + " (" + report.summary.totalSize + " bytes)");
    }

    public static void CapturePreviews()
    {
        Directory.CreateDirectory("Builds/Previews");
        foreach (var size in new[] { new Vector2Int(540, 960), new Vector2Int(960, 540) })
        {
            EditorSceneManager.OpenScene("Assets/Scenes/HanojasTornis.unity");
            var game = Object.FindFirstObjectByType<HanojasTornis>();
            var canvas = game.GetComponent<Canvas>();
            game.GetComponent<CanvasScaler>().enabled = false;
            canvas.renderMode = RenderMode.WorldSpace;
            var root = (RectTransform)canvas.transform;
            root.localScale = Vector3.one;
            root.sizeDelta = (Vector2)size * 2;
            root.position = Vector3.zero;
            var camera = Camera.main;
            camera.orthographic = true;
            camera.orthographicSize = size.y;
            camera.transform.position = new Vector3(0, 0, -10);
            canvas.worldCamera = camera;
            var target = new RenderTexture(size.x, size.y, 24);
            camera.targetTexture = target;
            game.RestartGame();
            for (int victory = 0; victory < 2; victory++)
            {
                if (victory == 1) Solve(4, 0, 2, 1, (a, b) => { game.SelectRod(a); game.SelectRod(b); });
                Canvas.ForceUpdateCanvases();
                foreach (var text in game.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate();
                camera.Render();
                RenderTexture.active = target;
                var image = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                image.Apply();
                File.WriteAllBytes("Builds/Previews/Hanoi-" + size.x + "x" + size.y + (victory == 1 ? "-win" : "") + ".png", image.EncodeToPNG());
                Object.DestroyImmediate(image);
            }
            RenderTexture.active = null;
            camera.targetTexture = null;
            Object.DestroyImmediate(target);
        }
    }
}
