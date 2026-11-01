using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Hanoi scene controller. Keeps the existing serialized scene references.
/// The difficulty selector, reward button, bonus holder and victory stars are
/// added when Play starts. InstallHanoiUpgrade assigns disks 1..6 and star sprites.
/// </summary>
public class HanojasTornis : MonoBehaviour
{
    [Header("Existing scene references")]
    public RectTransform safeArea;
    public RectTransform board;
    public Button[] rods;
    public Image[] disks;
    public TMP_Text movesText;
    public TMP_Text hintText;
    public GameObject victoryOverlay;
    public TMP_Text victoryMovesText;

    [Header("Star sprites - assigned by Install Hanoi Upgrade")]
    public Sprite filledStarSprite;
    public Sprite emptyStarSprite;

    private readonly HanoiPuzzle puzzle = new HanoiPuzzle();
    private Vector2 lastBoardSize;
    private int selectedRod = -1; // Maintained for legacy editor checks only.
    private float elapsed;
    private bool paused;
    private bool gameFinished;
    private int difficulty = 4;
    private RectTransform bonusPanel;
    private Button bonusAdButton;
    private Button difficultyButton;
    private TMP_Text difficultyLabel;
    private GameObject startDifficultyOverlay;
    private RectTransform difficultyCard;
    private TMP_Text difficultyHeading;
    private TMP_Text difficultySubtitle;
    private Button[] difficultyOptions;
    private Button difficultyBackButton;
    private Button difficultyResumeButton;
    private bool hasStartedGame;
    private bool waitingForDifficulty;
    private Vector2 lastSafeAreaSize;
    private Image[] stars;
    private RewardedAds rewarded;

    public HanoiPuzzle Puzzle => puzzle;
    public int SelectedRod => selectedRod;
    public int Difficulty => difficulty;
    public float ElapsedSeconds => elapsed;

    private void Awake()
    {
        if (Application.isPlaying) BuildExtraUI();
    }

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Application.targetFrameRate = 60;
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
#endif
        // The original click-on-rod UI is deliberately replaced with dragging.
        if (rods != null)
            foreach (Button button in rods)
                if (button != null)
                {
                    Image target = button.GetComponent<Image>();
                    if (target != null) target.raycastTarget = false;
                }

        if (disks != null)
            for (int i = 0; i < disks.Length; i++)
            {
                if (disks[i] == null) continue;
                disks[i].raycastTarget = true;
                HanoiDiskDrag drag = disks[i].GetComponent<HanoiDiskDrag>();
                if (drag == null && Application.isPlaying)
                    drag = disks[i].gameObject.AddComponent<HanoiDiskDrag>();
                if (drag != null) drag.diskNumber = i + 1;
            }

        rewarded = AdManager.Instance != null ? AdManager.Instance.rewardedAds : null;
        if (rewarded != null)
        {
            rewarded.RewardGranted += GrantBonusAfterAd;
            if (bonusAdButton != null) rewarded.SetButton(bonusAdButton);
        }

        SetDifficulty(4);
        ShowDifficultyPanel();
    }

    private void OnDestroy()
    {
        if (rewarded != null) rewarded.RewardGranted -= GrantBonusAfterAd;
    }

    private void OnApplicationPause(bool value) => paused = value;

    private void Update()
    {
        if (gameFinished || waitingForDifficulty || paused || AdsAreFullscreen()) return;
        elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.15f);
        UpdateMovesText();
    }

    private void LateUpdate()
    {
        if (board != null && board.rect.size != lastBoardSize) RefreshDisks();
        if (safeArea != null && safeArea.rect.size != lastSafeAreaSize)
            LayoutDifficultyPanel();
    }

    private bool AdsAreFullscreen() =>
        AdManager.Instance != null && AdManager.Instance.bannerAd != null &&
        AdManager.Instance.bannerAd.IsFullscreenAdActive;

    public void SetDifficulty(int count)
    {
        if (count < 4 || count > 6) return;
        if (disks == null || disks.Length < count)
        {
            Debug.LogError("Hanoi: Missing disks for " + count +
                "-disk difficulty. Run Tools/Where Is My Car/Upgrade Hanoi Disks (4-6).");
            return;
        }

        difficulty = count;
        puzzle.Reset(count);
        elapsed = 0f;
        paused = false;
        gameFinished = false;
        selectedRod = -1;
        if (victoryOverlay != null) victoryOverlay.SetActive(false);
        waitingForDifficulty = false;
        if (startDifficultyOverlay != null) startDifficultyOverlay.SetActive(false);
        if (bonusPanel != null) bonusPanel.gameObject.SetActive(false);
        if (bonusAdButton != null) bonusAdButton.gameObject.SetActive(true);
        if (difficultyLabel != null) difficultyLabel.text = "GRŪTĪBA";
        if (rods != null)
            foreach (Button button in rods)
                if (button != null) button.interactable = true;

        UpdateMovesText();
        SetDefaultHint();
        Canvas.ForceUpdateCanvases();
        RefreshDisks();
    }

    public void RestartGame() => SetDifficulty(difficulty);

    public void OpenMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void UpdateMovesText()
    {
        if (movesText == null) return;
        int secs = Mathf.FloorToInt(elapsed);
        movesText.text = "Gājieni: " + puzzle.Moves + "   Laiks: " +
                         (secs / 60).ToString("00") + ":" + (secs % 60).ToString("00");
    }

    private void SetDefaultHint()
    {
        if (hintText != null) hintText.text = "Velc augšējo disku uz citu stieni.";
    }

    public bool CanDragDisk(int diskNumber, out int sourceRod)
    {
        sourceRod = -1;
        if (waitingForDifficulty || gameFinished || puzzle.IsWon || AdsAreFullscreen()) return false;
        for (int rod = 0; rod < 4; rod++)
            if (puzzle.Top(rod) == diskNumber)
            {
                sourceRod = rod;
                return true;
            }
        return false;
    }

    public void DropDraggedDisk(int diskNumber, int sourceRod,
        Vector2 screenPosition, Camera eventCamera)
    {
        bool moved = false;
        if (CanDragDisk(diskNumber, out int currentRod) && currentRod == sourceRod)
        {
            // Check the reward dock before the main board. The dock can be outside
            // the board's rect in portrait orientation.
            if (puzzle.BonusUnlocked && bonusPanel != null &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    bonusPanel, screenPosition, eventCamera))
            {
                if (sourceRod != HanoiPuzzle.BonusRod)
                    moved = puzzle.TryMove(sourceRod, HanoiPuzzle.BonusRod);
            }
            else if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                board, screenPosition, eventCamera, out Vector2 point))
            {
                Rect bounds = board.rect;
                if (bounds.Contains(point))
                {
                    float x = (point.x - bounds.xMin) / bounds.width;
                    float y = (point.y - bounds.yMin) / bounds.height;
                    if (y >= 0.03f && y <= 0.93f)
                    {
                        int target = x < .36f ? 0 : x < .69f ? 1 : 2;
                        if (target != sourceRod) moved = puzzle.TryMove(sourceRod, target);
                    }
                }
            }
        }

        selectedRod = -1;
        if (moved)
        {
            UpdateMovesText();
            SetDefaultHint();
            if (puzzle.IsWon) FinishGame();
        }
        else if (hintText != null)
            hintText.text = "Nederīgs gājiens. Pārvieto tikai augšējo disku.";

        RefreshDisks();
    }

    // Kept for compatibility with the old editor checks. The player uses drag/drop.
    public void SelectRod(int rod)
    {
        if (rod < 0 || rod > 2 || waitingForDifficulty || gameFinished || AdsAreFullscreen()) return;
        if (selectedRod < 0)
        {
            if (puzzle.Top(rod) != 0) selectedRod = rod;
            RefreshDisks();
            return;
        }
        int from = selectedRod;
        selectedRod = -1;
        if (puzzle.TryMove(from, rod))
        {
            UpdateMovesText();
            if (puzzle.IsWon) FinishGame();
        }
        RefreshDisks();
    }

    private void GrantBonusAfterAd()
    {
        if (puzzle.IsWon || !puzzle.UnlockBonus()) return;
        if (bonusPanel != null) bonusPanel.gameObject.SetActive(true);
        if (bonusAdButton != null) bonusAdButton.gameObject.SetActive(false);
        if (hintText != null) hintText.text = "Bonuss atbloķēts! Uz paliktņa drīkst novietot vienu disku.";
        RefreshDisks();
    }

#if UNITY_EDITOR
    [ContextMenu("TEST: Unlock bonus holder (Play Mode)")]
    public void DebugUnlockBonus() => GrantBonusAfterAd();
#endif

    private void FinishGame()
    {
        gameFinished = true;
        int awardedStars = CalculateStars();
        int secs = Mathf.RoundToInt(elapsed);
        if (victoryMovesText != null)
            victoryMovesText.text = "Gājieni: " + puzzle.Moves +
                "  (minimums: " + puzzle.MinimumMoves + ")\n" +
                "Laiks: " + (secs / 60).ToString("00") + ":" +
                (secs % 60).ToString("00") +
                (puzzle.BonusUsed ? "\nIzmantots bonuss" : "");

        if (stars != null)
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null) continue;
                stars[i].sprite = i < awardedStars ? filledStarSprite : emptyStarSprite;
                stars[i].color = i < awardedStars ? new Color(1f, .83f, .16f) :
                    new Color(.55f, .53f, .47f);
            }

        if (victoryOverlay != null)
        {
            victoryOverlay.SetActive(true);
            victoryOverlay.transform.SetAsLastSibling();
        }
        if (rods != null)
            foreach (Button button in rods)
                if (button != null) button.interactable = false;
    }

    private int CalculateStars()
    {
        int minimum = puzzle.MinimumMoves;
        float timeTarget = difficulty == 4 ? 120f : difficulty == 5 ? 210f : 360f;
        int starsCount = 1;
        if (puzzle.Moves <= Mathf.CeilToInt(minimum * 1.75f) &&
            elapsed <= timeTarget * 1.8f) starsCount = 2;
        if (puzzle.Moves <= Mathf.CeilToInt(minimum * 1.25f) &&
            elapsed <= timeTarget) starsCount = 3;
        if (puzzle.BonusUsed) starsCount = Mathf.Min(starsCount, 2);
        return starsCount;
    }


    public void RefreshDisks()
    {
        if (board == null || disks == null) return;

        float scale = Mathf.Min(
            board.rect.width / 1280f,
            board.rect.height / 600f
        );

        float[] rodX = { .195f, .528f, .855f };

        float height = 69f * scale;

        // Нижние диски ближе к основанию стержней
        float bottom = board.rect.height * .07f;

        for (int i = 0; i < disks.Length; i++)
        {
            if (disks[i] != null)
                disks[i].gameObject.SetActive(
                    i < puzzle.ActiveDiskCount
                );
        }

        for (int rod = 0; rod < 4; rod++)
        {
            for (int level = 0; level < puzzle.Count(rod); level++)
            {
                int disk = puzzle.DiskAt(rod, level);

                if (disk < 1 || disk > disks.Length)
                    continue;

                Image image = disks[disk - 1];

                if (image == null)
                    continue;

                RectTransform rect = image.rectTransform;

                float nativeW = image.sprite != null
                    ? image.sprite.rect.width : 150f;

                float nativeH = image.sprite != null
                    ? image.sprite.rect.height : 69f;

                // Бонусная палочка
                if (rod == HanoiPuzzle.BonusRod &&
                    bonusPanel != null)
                {
                    if (rect.parent != bonusPanel)
                        rect.SetParent(bonusPanel, false);

                    rect.anchorMin = rect.anchorMax =
                        new Vector2(.5f, .5f);

                    rect.pivot = new Vector2(.5f, .5f);

                    float maxWidth = bonusPanel.rect.width * .75f;

                    float factor = Mathf.Min(
                        scale * .55f,
                        maxWidth / nativeW
                    );

                    rect.sizeDelta = new Vector2(
                        nativeW * factor,
                        nativeH * factor
                    );

                    rect.anchoredPosition = new Vector2(0f, 13f);
                }
                // Основные три стержня
                else if (rod < 3)
                {
                    if (rect.parent != board)
                        rect.SetParent(board, false);

                    rect.anchorMin = rect.anchorMax =
                        new Vector2(rodX[rod], 0f);

                    rect.pivot = new Vector2(.5f, .5f);

                    rect.sizeDelta = new Vector2(
                        nativeW * scale,
                        nativeH * scale
                    );

                    float y = bottom + (level + .5f) * height;

                    bool selected =
                        rod == selectedRod &&
                        disk == puzzle.Top(rod);

                    rect.anchoredPosition = new Vector2(
                        0f,
                        y + (selected ? height * .18f : 0f)
                    );

                    Outline outline = image.GetComponent<Outline>();

                    if (outline != null)
                        outline.enabled = selected;
                }

                image.raycastTarget = true;
            }
        }

        lastBoardSize = board.rect.size;
    }


    private void BuildExtraUI()
    {
        if (safeArea == null || victoryOverlay == null) return;

        // Fix top texts so they do not overlap.
        Transform titleTransform = safeArea.Find("Title");
        if (titleTransform != null)
        {
            RectTransform rect = titleTransform as RectTransform;
            TMP_Text title = titleTransform.GetComponent<TMP_Text>();

            if (rect != null)
            {
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
                rect.pivot = new Vector2(.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -6f);
                rect.sizeDelta = new Vector2(420f, 42f);
            }

            if (title != null)
            {
                title.alignment = TextAlignmentOptions.Center;
                title.textWrappingMode = TextWrappingModes.NoWrap;
                title.enableAutoSizing = true;
                title.fontSizeMin = 18f;
                title.fontSizeMax = 30f;
            }
        }

        if (movesText != null)
        {
            RectTransform rect = movesText.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -34f);
            rect.sizeDelta = new Vector2(360f, 52f);

            movesText.alignment = TextAlignmentOptions.Center;
            movesText.textWrappingMode = TextWrappingModes.NoWrap;
            movesText.enableAutoSizing = true;
            movesText.fontSizeMin = 16f;
            movesText.fontSizeMax = 24f;
        }

        // A compact button reopens the full selection window during gameplay.
        // There is no in-game dropdown: difficulty is always chosen on a panel.
        difficultyButton = MakeButton("DifficultyButton", safeArea,
            "GRŪTĪBA", new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(20, -78), new Vector2(150, 46), new Color(.88f, .64f, .23f));
        difficultyLabel = difficultyButton.GetComponentInChildren<TMP_Text>();
        difficultyButton.onClick.AddListener(ShowDifficultyPanel);

        // The tag is already defined in the existing project. AdManager binds it
        // to RewardedAds.ShowAd on scene load; the existing Unity Ads SDK is reused.
        bonusAdButton = MakeButton("HanoiRewardButton", safeArea,
            "BONUSS", new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -78), new Vector2(148, 46), new Color(.32f, .70f, .42f));
        bonusAdButton.gameObject.tag = "RewardedAdButton";

        // Target label above the winning rod (3rd rod on the right).
        // More visible target marker above the winning rod
        RectTransform targetBadge = MakeRect(
            "TargetBadge",
            board,
            new Vector2(.855f, .97f),
            new Vector2(.5f, 1f),
            Vector2.zero,
            new Vector2(175f, 34f)
        );

        Image badgeImage = targetBadge.gameObject.AddComponent<Image>();
        badgeImage.color = new Color(.96f, .88f, .62f, .92f);
        badgeImage.raycastTarget = false;

        Outline badgeOutline = targetBadge.gameObject.AddComponent<Outline>();
        badgeOutline.effectColor = new Color(.48f, .27f, .08f, .95f);
        badgeOutline.effectDistance = new Vector2(2f, -2f);

        TMP_Text targetLabel = MakeText(
            "TargetLabel",
            targetBadge,
            "MĒRĶIS ↓",
            new Vector2(.5f, .5f),
            new Vector2(.5f, .5f),
            Vector2.zero,
            new Vector2(165f, 28f),
            24f
        );

        targetLabel.color = new Color(.45f, .20f, .02f);
        targetLabel.textWrappingMode = TextWrappingModes.NoWrap;
        targetLabel.fontStyle = FontStyles.Bold;

        // Perpendicular side peg at the left edge, kept out of the main towers.

        // Bonus holder - wooden horizontal peg on the left edge.
        bonusPanel = MakeRect(
            "BonusHolder",
            safeArea,
            new Vector2(0f, .70f),
            new Vector2(0f, .5f),
            new Vector2(18f, 0f),
            new Vector2(170f, 100f)
        );

        // No background and no BONUSS text.
        // BonusHolder remains an invisible drop zone.

        // Vertical mounting bracket
        RectTransform mount = MakeRect(
            "WoodenMount",
            bonusPanel,
            new Vector2(.10f, .42f),
            new Vector2(.5f, .5f),
            Vector2.zero,
            new Vector2(18f, 42f)
        );

        Image mountImage = mount.gameObject.AddComponent<Image>();
        mountImage.color = new Color(.48f, .27f, .10f);
        mountImage.raycastTarget = false;

        // Main horizontal wooden peg
        RectTransform pole = MakeRect(
            "HorizontalPeg",
            bonusPanel,
            new Vector2(.5f, .42f),
            new Vector2(.5f, .5f),
            Vector2.zero,
            new Vector2(140f, 16f)
        );

        Image pegImage = pole.gameObject.AddComponent<Image>();
        pegImage.color = new Color(.88f, .57f, .23f);
        pegImage.raycastTarget = false;

        Outline pegOutline = pole.gameObject.AddComponent<Outline>();
        pegOutline.effectColor = new Color(.35f, .17f, .06f);
        pegOutline.effectDistance = new Vector2(2f, -2f);

        // Lighter highlight on top
        RectTransform highlight = MakeRect(
            "WoodHighlight",
            bonusPanel,
            new Vector2(.5f, .49f),
            new Vector2(.5f, .5f),
            Vector2.zero,
            new Vector2(132f, 4f)
        );

        Image highlightImage = highlight.gameObject.AddComponent<Image>();
        highlightImage.color = new Color(.99f, .79f, .45f);
        highlightImage.raycastTarget = false;

        // Rounded end of the peg
        RectTransform endCap = MakeRect(
            "PegEnd",
            bonusPanel,
            new Vector2(.91f, .42f),
            new Vector2(.5f, .5f),
            Vector2.zero,
            new Vector2(18f, 22f)
        );

        Image endImage = endCap.gameObject.AddComponent<Image>();
        endImage.color = new Color(.69f, .39f, .13f);
        endImage.raycastTarget = false;

        bonusPanel.gameObject.SetActive(false);


        // Star graphics inside the existing victory dialog.
        Transform window = victoryOverlay.transform.Find("VictorySafeArea/VictoryWindow");
        if (window != null)
        {
            if (victoryMovesText != null)
            {
                RectTransform info = victoryMovesText.rectTransform;
                info.anchorMin = new Vector2(.06f, .50f);
                info.anchorMax = new Vector2(.94f, .65f);
                info.offsetMin = info.offsetMax = Vector2.zero;
            }
            RectTransform starBar = MakeRect("VictoryStars", window,
                new Vector2(.5f, .715f), new Vector2(.5f, .5f),
                Vector2.zero, new Vector2(340f, 65f));
            stars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform star = MakeRect("Star" + (i + 1), starBar,
                    new Vector2(i * .35f + .15f, .5f), new Vector2(.5f, .5f),
                    Vector2.zero, new Vector2(63f, 63f));
                stars[i] = star.gameObject.AddComponent<Image>();
                stars[i].sprite = emptyStarSprite;
                stars[i].preserveAspect = true;
                stars[i].raycastTarget = false;
            }
        }

        CreateDifficultyPanel();
    }

    /// <summary>Opens the modal. Timer and disk interaction stop until a difficulty is picked.</summary>
    public void ShowDifficultyPanel()
    {
        if (startDifficultyOverlay == null) return;
        waitingForDifficulty = true;
        startDifficultyOverlay.SetActive(true);
        startDifficultyOverlay.transform.SetAsLastSibling();
        if (difficultyResumeButton != null)
            difficultyResumeButton.gameObject.SetActive(hasStartedGame);
        LayoutDifficultyPanel();
    }

    private void ResumeGame()
    {
        if (!hasStartedGame) return;
        waitingForDifficulty = false;
        if (startDifficultyOverlay != null) startDifficultyOverlay.SetActive(false);
    }

    private void ChooseDifficulty(int count)
    {
        // Reject difficulties that have not been configured in Unity yet.
        if (disks == null || disks.Length < count)
        {
            Debug.LogError("Hanoi: Add Disk5 and Disk6 with Upgrade Hanoi Disks (4-6) first.");
            return;
        }
        SetDifficulty(count);  // Resets timer, bonus holder and puzzle.
        hasStartedGame = true;
    }

    private void CreateDifficultyPanel()
    {
        if (safeArea == null) return;

        var overlay = new GameObject("StartDifficultyOverlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(safeArea, false);
        var overlayRect = (RectTransform)overlay.transform;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Image scrim = overlay.GetComponent<Image>();
        scrim.color = new Color(.075f, .053f, .036f, .85f);
        scrim.raycastTarget = true; // Prevents interacting with the game behind the popup.
        startDifficultyOverlay = overlay;

        difficultyCard = MakeRect("DifficultyCard", overlay.transform,
            new Vector2(.5f, .5f), new Vector2(.5f, .5f),
            Vector2.zero, new Vector2(600f, 490f));
        Image cardImage = difficultyCard.gameObject.AddComponent<Image>();
        cardImage.color = new Color(.98f, .93f, .84f);
        cardImage.raycastTarget = true;

        difficultyHeading = MakeText("Heading", difficultyCard, "HANOJAS TORNIS",
            new Vector2(.5f, .87f), new Vector2(.5f, .5f),
            Vector2.zero, new Vector2(545, 55), 46f);
        difficultySubtitle = MakeText("Instruction", difficultyCard,
            "IZVĒLIES GRŪTĪBAS PAKĀPI",
            new Vector2(.5f, .73f), new Vector2(.5f, .5f),
            Vector2.zero, new Vector2(545, 40), 25f);

        int[] counts = { 4, 5, 6 };
        string[] titles = {
            "VIEGLS  ·  4 DISKI",
            "VIDĒJS  ·  5 DISKI",
            "GRŪTS  ·  6 DISKI"
        };
        Color[] colors = {
            new Color(.48f, .75f, .46f),
            new Color(.94f, .71f, .31f),
            new Color(.91f, .46f, .34f)
        };
        float[] y = { .575f, .415f, .255f };
        difficultyOptions = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            int chosenCount = counts[i];
            Button option = MakeButton("Choose" + chosenCount, difficultyCard,
                titles[i], new Vector2(.5f, y[i]), new Vector2(.5f, .5f),
                Vector2.zero, new Vector2(480, 62), colors[i]);
            option.onClick.AddListener(() => ChooseDifficulty(chosenCount));
            difficultyOptions[i] = option;
        }

        difficultyResumeButton = MakeButton("ResumeGame", difficultyCard,
            "ATPAKAĻ", new Vector2(.25f, .085f),
            new Vector2(.5f, .5f), Vector2.zero,
            new Vector2(210, 39), new Color(.75f, .78f, .67f));
        difficultyResumeButton.onClick.AddListener(ResumeGame);
        difficultyBackButton = MakeButton("BackToMainMenu", difficultyCard,
            "GALVENĀ IZVĒLNE", new Vector2(.75f, .085f),
            new Vector2(.5f, .5f), Vector2.zero,
            new Vector2(220, 39), new Color(.79f, .75f, .67f));
        difficultyBackButton.onClick.AddListener(OpenMainMenu);
        difficultyResumeButton.gameObject.SetActive(false);
        startDifficultyOverlay.SetActive(false);
        LayoutDifficultyPanel();
    }

    private void LayoutDifficultyPanel()
    {
        if (safeArea == null || difficultyCard == null) return;
        float w = Mathf.Max(100f, safeArea.rect.width - 28f);
        float h = Mathf.Max(100f, safeArea.rect.height - 28f);
        float cw = Mathf.Min(620f, w);
        float ch = Mathf.Min(515f, h);
        difficultyCard.sizeDelta = new Vector2(cw, ch);
        if (difficultyHeading != null)
            difficultyHeading.rectTransform.sizeDelta = new Vector2(cw - 20f, Mathf.Min(64f, ch * .12f));
        if (difficultySubtitle != null)
            difficultySubtitle.rectTransform.sizeDelta = new Vector2(cw - 20f, Mathf.Min(42f, ch * .09f));
        if (difficultyOptions != null)
        {
            float buttonWidth = Mathf.Max(65f, cw - 48f);
            float buttonHeight = Mathf.Min(76f, ch * .13f);
            foreach (Button option in difficultyOptions)
                if (option != null)
                {
                    RectTransform rect = (RectTransform)option.transform;
                    rect.sizeDelta = new Vector2(buttonWidth, buttonHeight);
                    TMP_Text caption = option.GetComponentInChildren<TMP_Text>();
                    if (caption != null)
                        caption.rectTransform.sizeDelta = new Vector2(buttonWidth - 12f, buttonHeight - 8f);
                }
        }
        float footerHeight = Mathf.Min(44f, ch * .085f);
        float footerWidth = hasStartedGame ? (cw - 56f) * .5f : cw - 48f;
        if (difficultyBackButton != null)
        {
            RectTransform rect = (RectTransform)difficultyBackButton.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(hasStartedGame ? .75f : .5f, .085f);
            rect.sizeDelta = new Vector2(footerWidth, footerHeight);
            TMP_Text text = difficultyBackButton.GetComponentInChildren<TMP_Text>();
            if (text != null)
                text.rectTransform.sizeDelta = new Vector2(footerWidth - 8f, footerHeight - 4f);
        }
        if (difficultyResumeButton != null)
        {
            RectTransform rect = (RectTransform)difficultyResumeButton.transform;
            rect.sizeDelta = new Vector2(footerWidth, footerHeight);
            TMP_Text text = difficultyResumeButton.GetComponentInChildren<TMP_Text>();
            if (text != null)
                text.rectTransform.sizeDelta = new Vector2(footerWidth - 8f, footerHeight - 4f);
        }
        lastSafeAreaSize = safeArea.rect.size;
    }

    private RectTransform MakeRect(string name, Transform parent, Vector2 anchor,
        Vector2 pivot, Vector2 pos, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        return rect;
    }

    private TMP_Text MakeText(string name, Transform parent, string value,
        Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, float fontSize)
    {
        RectTransform rect = MakeRect(name, parent, anchor, pivot, pos, size);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (movesText != null) text.font = movesText.font;
        text.text = value;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(.15f, .10f, .06f);
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = 13f;
        text.fontSizeMax = fontSize;
        text.raycastTarget = false;
        return text;
    }

    private Button MakeButton(string name, Transform parent, string caption,
        Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
    {
        RectTransform rect = MakeRect(name, parent, anchor, pivot, pos, size);
        Image bg = rect.gameObject.AddComponent<Image>();
        bg.color = color;
        bg.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        MakeText("Label", rect, caption,
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero,
            new Vector2(size.x - 6, size.y - 6), 23f);
        return button;
    }
}
