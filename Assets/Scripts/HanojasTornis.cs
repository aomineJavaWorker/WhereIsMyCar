using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Scene references are serialized by SetupHanojasTornis; no runtime UI generation.</summary>
public class HanojasTornis : MonoBehaviour
{
    public RectTransform safeArea;
    public RectTransform board;
    public Button[] rods;
    public Image[] disks;
    public TMP_Text movesText;
    public TMP_Text hintText;
    public GameObject victoryOverlay;
    public TMP_Text victoryMovesText;

    private readonly HanoiPuzzle puzzle = new HanoiPuzzle();
    private int selectedRod = -1;
    private Vector2 lastBoardSize;
    public HanoiPuzzle Puzzle => puzzle;
    public int SelectedRod => selectedRod;

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Application.targetFrameRate = 60;
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
#endif
        RestartGame();
    }

    private void LateUpdate()
    {
        // MobileUISafeArea and CanvasScaler resolve orientation/cutout changes first.
        if (board.rect.size != lastBoardSize) RefreshDisks();
    }

    public void SelectRod(int rod)
    {
        if (rod < 0 || rod >= rods.Length || puzzle.IsWon ||
            (AdManager.Instance != null && AdManager.Instance.bannerAd != null &&
             AdManager.Instance.bannerAd.IsFullscreenAdActive)) return;
        if (selectedRod < 0)
        {
            if (puzzle.Top(rod) == 0)
                hintText.text = "Šis stienis ir tukšs. Izvēlies disku.";
            else
            {
                selectedRod = rod;
                hintText.text = "Izvēlies mērķa stieni. Vēlreiz nospied, lai atceltu.";
            }
        }
        else if (selectedRod == rod)
        {
            selectedRod = -1;
            SetDefaultHint();
        }
        else if (puzzle.TryMove(selectedRod, rod))
        {
            selectedRod = -1;
            movesText.text = "Gājieni: " + puzzle.Moves;
            SetDefaultHint();
            if (puzzle.IsWon)
            {
                victoryMovesText.text = "Gājieni: " + puzzle.Moves + "\nMinimums: 15";
                victoryOverlay.SetActive(true);
                victoryOverlay.transform.SetAsLastSibling();
                foreach (Button button in rods) button.interactable = false;
            }
        }
        else hintText.text = "Lielāku disku nevar likt uz mazāka.";
        RefreshDisks();
    }

    public void RestartGame()
    {
        puzzle.Reset();
        selectedRod = -1;
        victoryOverlay.SetActive(false);
        foreach (Button button in rods) button.interactable = true;
        movesText.text = "Gājieni: 0";
        SetDefaultHint();
        Canvas.ForceUpdateCanvases();
        RefreshDisks();
    }

    public void OpenMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void SetDefaultHint()
    {
        hintText.text =
            "Velc augšējo disku uz citu stieni.";
    }



    public bool CanDragDisk(int diskNumber, out int sourceRod)
    {
        sourceRod = -1;

        if (puzzle.IsWon || victoryOverlay.activeSelf)
            return false;

        if (AdManager.Instance != null &&
            AdManager.Instance.bannerAd != null &&
            AdManager.Instance.bannerAd.IsFullscreenAdActive)
            return false;

        for (int i = 0; i < 3; i++)
        {
            if (puzzle.Top(i) == diskNumber)
            {
                sourceRod = i;
                return true;
            }
        }

        return false;
    }

    public void DropDraggedDisk(
        int diskNumber,
        int sourceRod,
        Vector2 screenPosition,
        Camera eventCamera)
    {
        bool moved = false;

        if (CanDragDisk(diskNumber, out int currentRod) &&
            currentRod == sourceRod &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                board,
                screenPosition,
                eventCamera,
                out Vector2 localPoint))
        {
            Rect bounds = board.rect;

            if (bounds.Contains(localPoint))
            {
                float x = (localPoint.x - bounds.xMin) /
                          bounds.width;

                float y = (localPoint.y - bounds.yMin) /
                          bounds.height;

                // Accept drops within the playable rod area.
                if (y >= 0.05f && y <= 0.90f)
                {
                    int targetRod =
                        x < 0.36f ? 0 :
                        x < 0.69f ? 1 : 2;

                    if (targetRod != sourceRod)
                    {
                        moved = puzzle.TryMove(
                            sourceRod, targetRod
                        );

                        if (!moved)
                            hintText.text =
                                "Lielāku disku nevar likt uz mazāka.";
                    }
                }
            }
        }

        selectedRod = -1;

        if (moved)
        {
            movesText.text = "Gājieni: " + puzzle.Moves;
            SetDefaultHint();

            if (puzzle.IsWon)
            {
                victoryMovesText.text =
                    "Gājieni: " + puzzle.Moves +
                    "\nMinimums: 15";

                victoryOverlay.SetActive(true);
                victoryOverlay.transform.SetAsLastSibling();

                foreach (Button button in rods)
                    button.interactable = false;
            }
        }

        RefreshDisks();
    }


    public void RefreshDisks()
    {
        // Original background dimensions: 1280x600
        float scale = Mathf.Min(
            board.rect.width / 1280f,
            board.rect.height / 600f
        );

        // Rod positions in the teacher's background image.
        float[] rodX = { 0.195f, 0.528f, 0.855f };

        float diskHeight = 69f * scale;
        float bottomY = board.rect.height * 0.06f;

        for (int rod = 0; rod < 3; rod++)
        {
            for (int level = 0; level < puzzle.Count(rod); level++)
            {
                int disk = puzzle.DiskAt(rod, level);
                Image image = disks[disk - 1];
                RectTransform rect = image.rectTransform;

                bool selected =
                    rod == selectedRod &&
                    disk == puzzle.Top(rod);

                rect.anchorMin = new Vector2(rodX[rod], 0f);
                rect.anchorMax = rect.anchorMin;
                rect.pivot = new Vector2(0.5f, 0.5f);

                // Preserve the original sprite proportions.
                if (image.sprite != null)
                {
                    rect.sizeDelta = new Vector2(
                        image.sprite.rect.width * scale,
                        image.sprite.rect.height * scale
                    );
                }

                float y = bottomY +
                          (level + 0.5f) * diskHeight;

                if (selected)
                    y += diskHeight * 0.25f;

                rect.anchoredPosition = new Vector2(0f, y);

                Outline outline = image.GetComponent<Outline>();
                if (outline != null)
                    outline.enabled = selected;
            }
        }

        lastBoardSize = board.rect.size;
    }

}
