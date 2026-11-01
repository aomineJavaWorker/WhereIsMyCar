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

    private void SetDefaultHint() => hintText.text = "Pārvieto visus diskus uz 3. stieni. Nospied disku, tad stieni.";

    public void RefreshDisks()
    {
        float column = board.rect.width / 3f;
        float height = Mathf.Min(72f, board.rect.height * .115f);
        for (int rod = 0; rod < 3; rod++)
        {
            for (int level = 0; level < puzzle.Count(rod); level++)
            {
                int disk = puzzle.DiskAt(rod, level);
                Image image = disks[disk - 1];
                RectTransform rect = image.rectTransform;
                bool selected = rod == selectedRod && disk == puzzle.Top(rod);
                rect.anchorMin = rect.anchorMax = new Vector2((rod + .5f) / 3f, 0f);
                rect.pivot = new Vector2(.5f, .5f);
                rect.sizeDelta = new Vector2(column * (.30f + disk * .145f), height);
                rect.anchoredPosition = new Vector2(0f, board.rect.height * .17f +
                    (level + .5f) * (height + 6f) + (selected ? height * .28f : 0f));
                Outline outline = image.GetComponent<Outline>();
                if (outline != null) outline.enabled = selected;
            }
        }
        lastBoardSize = board.rect.size;
    }
}
