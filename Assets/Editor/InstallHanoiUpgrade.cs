using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Run ONCE in the open HanojasTornis scene. Non-destructive: keeps the teacher's
/// background, existing menu and ads, and adds/assigns disks 5 and 6.
/// NEVER run the older Create or Rebuild Hanojas Tornis generator afterward.
/// </summary>
public static class InstallHanoiUpgrade
{
    [MenuItem("Tools/Where Is My Car/Upgrade Hanoi Disks (4-6)")]
    public static void Install()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("Stop Play Mode before upgrading Hanoi.");
            return;
        }

        var current = SceneManager.GetActiveScene();
        if (current.name != "HanojasTornis")
        {
            EditorUtility.DisplayDialog("Hanoi upgrade",
                "Open Assets/Scenes/HanojasTornis.unity first.", "OK");
            return;
        }

        HanojasTornis game = Object.FindFirstObjectByType<HanojasTornis>();
        if (game == null || game.board == null || game.disks == null || game.disks.Length < 4)
        {
            Debug.LogError("Could not find the configured HanojasTornis component and its first four disks.");
            return;
        }

        Image[] all = new Image[6];
        for (int i = 0; i < 6; i++)
        {
            Transform disk = game.board.Find("Disk" + (i + 1));
            if (disk == null && i >= 4)
            {
                GameObject copy = Object.Instantiate(game.disks[3].gameObject, game.board, false);
                copy.name = "Disk" + (i + 1);
                Undo.RegisterCreatedObjectUndo(copy, "Add Hanoi Disk" + (i + 1));
                disk = copy.transform;
            }
            if (disk == null)
                throw new InvalidOperationException("Missing Disk" + (i + 1));

            Image image = disk.GetComponent<Image>();
            if (image == null) throw new InvalidOperationException(disk.name + " is missing Image");
            Undo.RecordObject(image, "Assign Hanoi sprites");
            Sprite sprite = LoadSprite("Assets/Sprites/" + i + "block.png");
            if (sprite == null)
                throw new InvalidOperationException("Sprite " + i + "block.png not found");
            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = true;
            EditorUtility.SetDirty(image);

            HanoiDiskDrag drag = disk.GetComponent<HanoiDiskDrag>();
            if (drag == null) drag = Undo.AddComponent<HanoiDiskDrag>(disk.gameObject);
            Undo.RecordObject(drag, "Configure disk drag");
            drag.diskNumber = i + 1;
            EditorUtility.SetDirty(drag);
            Transform size = disk.Find("Size");
            if (size != null) size.gameObject.SetActive(false);
            all[i] = image;
        }

        Undo.RecordObject(game, "Configure 6 Hanoi disk references");
        game.disks = all;
        game.filledStarSprite = LoadSprite("Assets/Sprites/Star.png");
        game.emptyStarSprite = LoadSprite("Assets/Sprites/StarBlack.png");
        EditorUtility.SetDirty(game);

        Image boardImage = game.board.GetComponent<Image>();
        if (boardImage != null)
        {
            Undo.RecordObject(boardImage, "Set Hanoi teacher background");
            boardImage.sprite = LoadSprite("Assets/Sprites/towerOfHanoi.png");
            boardImage.color = Color.white;
            boardImage.type = Image.Type.Simple;
            boardImage.preserveAspect = true;
            boardImage.raycastTarget = false;
            EditorUtility.SetDirty(boardImage);
        }

        // Maintain the 1280:600 source aspect in both device orientations.
        var fitter = game.board.GetComponent<AspectRatioFitter>();
        if (fitter == null) fitter = Undo.AddComponent<AspectRatioFitter>(game.board.gameObject);
        Undo.RecordObject(fitter, "Correct Hanoi background aspect");
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 1280f / 600f;
        EditorUtility.SetDirty(fitter);

        // Repair an earlier incorrect x=5 anchor that hid the title.
        Transform title = game.safeArea != null ? game.safeArea.Find("Title") : null;
        if (title is RectTransform titleRect)
        {
            Undo.RecordObject(titleRect, "Center Hanoi title");
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(.5f, 1f);
            titleRect.pivot = new Vector2(.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -15);
            EditorUtility.SetDirty(titleRect);
        }

        EditorSceneManager.MarkSceneDirty(current);
        EditorSceneManager.SaveScene(current);
        Debug.Log("HANOI UPGRADE INSTALLED: 6 original sprites, drag handlers, stars, " +
                  "1280:600 background; scene saved. Run in Play Mode to test.");
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }
}
