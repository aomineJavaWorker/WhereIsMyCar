using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Read-only Play Mode check of actual resolved UI geometry at the current
// Game View / Device Simulator size. No scene or Player Settings are modified.
public static class ValidateAndroidLayout
{
    [MenuItem("Tools/Where Is My Car/Validate Current Android Layout")]
    public static void Run()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode and select the desired Game View resolution first.");
            return;
        }
        Canvas.ForceUpdateCanvases();
        GameManager game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var rects = new List<Rect>();
        var names = new List<string>();
        var corners = new Vector3[4];
        Rect safe = Screen.safeArea;
        if (safe.width <= 0 || safe.height <= 0) safe = new Rect(0, 0, Screen.width, Screen.height);
        int errors = 0;
        AdaptiveOrientationUI ui = UnityEngine.Object.FindFirstObjectByType<AdaptiveOrientationUI>();
        if (ui != null && (game == null || !game.GameEnded))
        {
            // Include non-interactive HUD panels so a timer/heart collision
            // with a control cannot slip through the button-only checks.
            foreach (RectTransform panel in new[] { ui.timerSign, ui.livesPanel })
            {
                if (panel == null) continue;
                Rect bounds = ScreenRect(panel, corners);
                if (!safe.Contains(bounds.min) || !safe.Contains(bounds.max))
                {
                    Debug.LogError($"HUD outside Safe Area: {panel.name}", panel);
                    errors++;
                }
                rects.Add(bounds);
                names.Add(panel.name);
            }
            if (ui.timerSign != null && ui.timerSign.TryGetComponent(out Image timerBackground) &&
                timerBackground.sprite != null &&
                Mathf.Abs(ui.timerSign.rect.width / ui.timerSign.rect.height -
                    timerBackground.sprite.rect.width / timerBackground.sprite.rect.height) > .001f)
            {
                Debug.LogError("Timer panel aspect differs from its sprite.", ui.timerSign);
                errors++;
            }
        }
        foreach (Button button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            Canvas canvas = button.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            if (game != null && game.GameEnded && game.winPanel != null && !button.transform.IsChildOf(game.winPanel.transform)) continue;
            Rect rect = ScreenRect(button.GetComponent<RectTransform>(), corners);
            if (rect.xMin < safe.xMin - 1 || rect.yMin < safe.yMin - 1 ||
                rect.xMax > safe.xMax + 1 || rect.yMax > safe.yMax + 1)
            {
                Debug.LogError($"Outside Safe Area: {button.name}, {rect}", button);
                errors++;
            }
            for (int i = 0; i < rects.Count; i++)
            {
                if (!rect.Overlaps(rects[i])) continue;
                Debug.LogError($"Buttons overlap: {names[i]} and {button.name}", button);
                errors++;
            }
            rects.Add(rect);
            names.Add(button.name);
        }

        CameraMapViewportFill fill = UnityEngine.Object.FindFirstObjectByType<CameraMapViewportFill>();
        if (fill != null && fill.mapImage != null)
        {
            Camera camera = fill.GetComponent<Camera>();
            fill.mapImage.GetWorldCorners(corners);
            float halfHeight = camera.orthographicSize;
            float halfWidth = halfHeight * camera.aspect;
            Vector3 position = camera.transform.position;
            if (position.x - halfWidth < corners[0].x - .1f || position.x + halfWidth > corners[2].x + .1f ||
                position.y - halfHeight < corners[0].y - .1f || position.y + halfHeight > corners[2].y + .1f)
            {
                Debug.LogError("Camera viewport extends outside the map.", camera);
                errors++;
            }
            Image image = fill.mapImage.GetComponent<Image>();
            if (image != null && image.sprite != null &&
                Mathf.Abs(fill.mapImage.rect.width / fill.mapImage.rect.height - image.sprite.rect.width / image.sprite.rect.height) > .001f)
            {
                Debug.LogError("Map aspect differs from its sprite.", image);
                errors++;
            }
        }
        if (errors > 0) throw new InvalidOperationException($"Android layout: {errors} problems at {Screen.width}x{Screen.height}.");
        Debug.Log($"Android layout PASS: {Screen.width}x{Screen.height}, Safe Area {safe}, {rects.Count} checked UI elements. Gesture and visual checks still required.");
    }

    private static Rect ScreenRect(RectTransform transform, Vector3[] corners)
    {
        transform.GetWorldCorners(corners);
        Vector2 min = corners[0], max = corners[0];
        foreach (Vector3 corner in corners)
        {
            min = Vector2.Min(min, corner);
            max = Vector2.Max(max, corner);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
}
