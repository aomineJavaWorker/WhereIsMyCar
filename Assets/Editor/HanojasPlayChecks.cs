using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Run in an isolated batch editor without -quit. Uses real scene/button callbacks.
/// SDK display is suppressed during this test; network delivery needs an Android device.</summary>
[InitializeOnLoad]
public static class HanojasPlayChecks
{
    private const string Running = "Hanoi.PlayChecks";
    private static int step;
    private static double next;
    private static double deadline;
    private static Vector3 beforeScale;
    private static Quaternion beforeRotation;
    private static DragAndDropScript car;
    private static float rewardTime;

    static HanojasPlayChecks()
    {
        if (SessionState.GetBool(Running, false))
        {
            deadline = EditorApplication.timeSinceStartup + 150;
            next = EditorApplication.timeSinceStartup + 1;
            EditorApplication.update += Tick;
            SceneManager.sceneLoaded += SuppressSdkDisplay;
        }
    }

    public static void RunBatch()
    {
        HanojasProjectChecks.CapturePreviews();
        SessionState.SetBool(Running, true);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        EditorApplication.EnterPlaymode();
    }

    private static void SuppressSdkDisplay(Scene scene, LoadSceneMode mode)
    {
        if (AdManager.Instance == null) return;
        Set(AdManager.Instance, "turnOffInterstitialAds", true);
        Set(AdManager.Instance, "turnOffBannerAds", true);
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (EditorApplication.timeSinceStartup < next) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Play checks timed out");
            next = EditorApplication.timeSinceStartup + 1;
            switch (step++)
            {
                case 0:
                    Require(SceneManager.GetActiveScene().name == "MainMenu", "initial menu");
                    ValidateAndroidLayout.Run();
                    Click("HanojasButton"); break;
                case 1:
                    Require(SceneManager.GetActiveScene().name == "HanojasTornis", "menu -> Hanoi");
                    var game = Object.FindFirstObjectByType<HanojasTornis>();
                    Require(game.Puzzle.Moves == 0, "fresh Hanoi");
                    // Raycast on the smallest disk must reach the rod Button behind its decoration.
                    Canvas.ForceUpdateCanvases();
                    var position = RectTransformUtility.WorldToScreenPoint(null, game.disks[0].transform.position);
                    var results = new System.Collections.Generic.List<RaycastResult>();
                    EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, results);
                    Require(results.Count > 0 && results[0].gameObject.GetComponentInParent<Button>() == game.rods[0], "disk touch raycast");
                    Solve(4, 0, 2, 1, (a, b) => { game.rods[a].onClick.Invoke(); game.rods[b].onClick.Invoke(); });
                    Require(game.Puzzle.Moves == 15 && game.victoryOverlay.activeSelf, "Hanoi victory through buttons");
                    Click("PlayAgainButton");
                    Require(game.Puzzle.Moves == 0 && !game.victoryOverlay.activeSelf, "play again button");
                    Click("MainMenuButton"); break;
                case 2:
                    Require(SceneManager.GetActiveScene().name == "MainMenu", "Hanoi -> menu");
                    Click("StartButton"); break;
                case 3:
                    Require(SceneManager.GetActiveScene().name == "CityScene", "menu -> City");
                    ValidateAndroidLayout.Run();
                    Require(Object.FindObjectsByType<AdManager>(FindObjectsSortMode.None).Length == 1, "one persistent ads manager");
                    car = Object.FindFirstObjectByType<DragAndDropScript>();
                    car.OnPointerDown(new PointerEventData(EventSystem.current));
                    Require(GameObjectsScript.lastDragged == car.gameObject, "tap selects car");
                    car.transform.localScale = new Vector3(.5f, .5f, 1f);
                    beforeScale = car.transform.localScale;
                    beforeRotation = car.transform.localRotation;
                    Object.FindFirstObjectByType<ObjectTransformationScript>().StartRotateCW();
                    Object.FindFirstObjectByType<ObjectTransformationScript>().StartScaleUpX();
                    Object.FindFirstObjectByType<ObjectTransformationScript>().StartScaleUpY(); break;
                case 4:
                    var transform = Object.FindFirstObjectByType<ObjectTransformationScript>();
                    transform.StopRotateCW(); transform.StopScaleUpY(); transform.StopScaleUpX();
                    Require(car.transform.localRotation != beforeRotation, "rotation");
                    Require(Mathf.Abs(car.transform.localScale.y) > Mathf.Abs(beforeScale.y) &&
                        Mathf.Abs(car.transform.localScale.x) > Mathf.Abs(beforeScale.x), "resize both axes");
                    float x = car.transform.localScale.x;
                    transform.FlipSelected();
                    Require(Mathf.Approximately(car.transform.localScale.x, -x), "flip");
                    var pointer = new PointerEventData(EventSystem.current) { pointerDrag = car.gameObject,
                        position = RectTransformUtility.WorldToScreenPoint(Camera.main, car.transform.position) };
                    car.OnBeginDrag(pointer);
                    Require(GameObjectsScript.isDragging, "drag begins");
                    var place = Object.FindObjectsByType<DropPlaceScript>(FindObjectsSortMode.None).First(p => p.tag == car.tag);
                    car.transform.localScale = place.transform.localScale;
                    car.transform.localRotation = place.transform.localRotation;
                    place.OnDrop(pointer); car.OnEndDrag(pointer);
                    Require(car.isPlaced && !GameObjectsScript.isDragging, "drop confirms placement");
                    var city = Object.FindFirstObjectByType<GameManager>();
                    Require((float)Get(city, "elapsedTime") > 0 && city.timerText.text.Length > 0, "City timer advances");
                    for (int i = 1; i < city.totalVehicles; i++) city.VehiclePlaced();
                    Require(city.GameEnded && city.winPanel.activeSelf && city.resultTitle.text == "UZVARA!" && Time.timeScale == 0, "City victory");
                    Require(city.star1.sprite == city.yellowStar && city.star2.sprite == city.yellowStar &&
                        city.star3.sprite == city.yellowStar, "City three stars");
                    Click("RestartButton"); break;
                case 5:
                    var restarted = Object.FindFirstObjectByType<GameManager>();
                    Require(!restarted.GameEnded && Time.timeScale == 1 && restarted.hearts.All(h => h.gameObject.activeSelf), "City restart");
                    for (int i = 0; i < 4; i++) restarted.VehicleDestroyed();
                    Require(restarted.GameEnded && restarted.winPanel.activeSelf &&
                        restarted.hearts.All(h => !h.gameObject.activeSelf) && Time.timeScale == 0, "City defeat/lives");
                    Require(restarted.star1.sprite == restarted.grayStar && restarted.star2.sprite == restarted.grayStar &&
                        restarted.star3.sprite == restarted.grayStar, "City defeat stars");
                    Click("MainMenuButton"); break;
                case 6:
                    Require(SceneManager.GetActiveScene().name == "MainMenu", "City result -> menu");
                    Click("HanojasButton"); break;
                case 7:
                    var hanoi = Object.FindFirstObjectByType<HanojasTornis>();
                    Require(hanoi != null && Time.timeScale == 1, "second Hanoi entry");
                    var banner = AdManager.Instance.bannerAd;
                    Require(!(bool)Get(banner, "_isGameplayScene"), "no banner in Hanoi");
                    banner.BeginInterstitial(); Require(banner.IsFullscreenAdActive && !banner.isBannerVisible, "interstitial hides banner");
                    banner.EndInterstitial(); Require(!banner.IsFullscreenAdActive, "interstitial releases banner");
                    banner.BeginRewarded(); Require(banner.IsFullscreenAdActive && !banner.isBannerVisible, "reward hides banner");
                    banner.EndRewarded(); Require(!banner.IsFullscreenAdActive, "reward releases banner");
                    var reward = AdManager.Instance.rewardedAds;
                    var testButton = new GameObject("RewardTestButton", typeof(RectTransform), typeof(Button)).GetComponent<Button>();
                    reward.SetButton(testButton);
                    reward.OnUnityAdsShowComplete("BP_Rewarded_Android", UnityAdsShowCompletionState.COMPLETED);
                    reward.OnUnityAdsAdLoaded("BP_Rewarded_Android");
                    Require(!testButton.interactable, "reward cooldown begins");
                    rewardTime = Time.realtimeSinceStartup;
                    next = EditorApplication.timeSinceStartup + 15.5;
                    break;
                case 8:
                    AdManager.Instance.rewardedAds.OnUnityAdsAdLoaded("BP_Rewarded_Android");
                    Require(Time.realtimeSinceStartup - rewardTime >= 15 && GameObject.Find("RewardTestButton").GetComponent<Button>().interactable,
                        "reward available again after 15 real seconds");
                    Debug.Log("HANOI PLAY CHECKS PASS: real menu round trips, raycast, solve/replay, City drag/drop/rotation/resize/flip, win/loss/restart/lives, persistent ads, banner locks, rewarded cooldown.");
                    Finish(0); break;
            }
        }
        catch (Exception exception) { Debug.LogException(exception); Finish(1); }
    }

    private static void Click(string name)
    {
        var button = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == name);
        button.onClick.Invoke();
    }
    private static void Solve(int n, int a, int b, int c, Action<int, int> move)
    {
        if (n == 0) return;
        Solve(n - 1, a, c, b, move); move(a, b); Solve(n - 1, c, b, a, move);
    }
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static object Get(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Require(bool value, string message) { if (!value) throw new Exception("PLAY CHECK FAILED: " + message); }
    private static void Finish(int code)
    {
        SessionState.SetBool(Running, false);
        EditorApplication.update -= Tick;
        EditorApplication.Exit(code);
    }
}
