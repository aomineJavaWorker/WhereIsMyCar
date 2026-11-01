using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Responsive main menu for Where Is My Car.
/// Reuses the original scene objects and existing Button.onClick actions.
/// Does not replace the background, animated cars, or menu transitions.
/// </summary>
public class MainMenu : MonoBehaviour
{
    private Canvas menuCanvas;
    private RectTransform safeArea;
    private Rect lastSafe;
    private Vector2Int lastSize;

    private static readonly Color Gold = new Color(1f, .76f, .34f, 1f);
    private static readonly Color Cream = new Color(1f, .96f, .85f, 1f);
    private static readonly Color Ink = new Color(.18f, .10f, .055f, 1f);

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Application.targetFrameRate = 60;
#endif
        if (gameObject.scene.name != "MainMenu") return;

        menuCanvas = GetComponent<Canvas>();
        if (menuCanvas == null) return;

        // Keep the existing scaling model: the background cars depend on it.
        CanvasScaler scaler = menuCanvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = menuCanvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        Transform existingSafeArea = menuCanvas.transform.Find("MenuSafeArea");
        if (existingSafeArea != null)
            safeArea = existingSafeArea as RectTransform;
        else
        {
            GameObject area = new GameObject("MenuSafeArea", typeof(RectTransform), typeof(MobileUISafeArea));
            area.transform.SetParent(menuCanvas.transform, false);
            safeArea = area.GetComponent<RectTransform>();
        }

        string[] names = { "TitleText", "SubtitleText", "StartButton", "HanojasButton", "ExitButton" };
        foreach (string name in names)
        {
            Transform item = menuCanvas.transform.Find(name);
            if (item != null)
                item.SetParent(safeArea, false);
        }

        Transform background = menuCanvas.transform.Find("Background");
        if (background != null && background.TryGetComponent(out Image backgroundImage))
        {
            backgroundImage.raycastTarget = false;
            AspectRatioFitter fitter = background.GetComponent<AspectRatioFitter>();
            if (fitter == null)
                fitter = background.gameObject.AddComponent<AspectRatioFitter>();
            if (backgroundImage.sprite != null)
                fitter.aspectRatio = backgroundImage.sprite.rect.width / backgroundImage.sprite.rect.height;
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        }

        ConfigureText();
        StyleMenuButton("StartButton", "LĪMENIS 1", new Color(.12f, .25f, .30f, .95f));
        StyleMenuButton("HanojasButton", "HANOJAS TORNIS", new Color(.12f, .25f, .30f, .95f));
        StyleMenuButton("ExitButton", "IZIET", new Color(.30f, .16f, .16f, .95f));
        ApplyMenuLayout();
    }

    private void LateUpdate()
    {
        if (safeArea != null &&
            (lastSize.x != Screen.width || lastSize.y != Screen.height || lastSafe != Screen.safeArea))
            ApplyMenuLayout();
    }

    private void ConfigureText()
    {
        TMP_Text title = safeArea.Find("TitleText")?.GetComponent<TMP_Text>();
        if (title != null)
        {
            title.text = "WHERE IS MY CAR?";
            title.color = Ink;
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.alignment = TextAlignmentOptions.Center;
            title.enableAutoSizing = true;
            title.fontSizeMin = 22f;
            title.fontSizeMax = 60f;
            title.raycastTarget = false;
        }

        TMP_Text subtitle = safeArea.Find("SubtitleText")?.GetComponent<TMP_Text>();
        if (subtitle != null)
        {
            // Keep the original caption from the scene.
            subtitle.text = "Match every vehicle with its correct place";
            subtitle.color = Cream;
            subtitle.fontStyle = FontStyles.Bold;
            subtitle.textWrappingMode = TextWrappingModes.NoWrap;
            subtitle.alignment = TextAlignmentOptions.Center;
            subtitle.enableAutoSizing = true;
            subtitle.fontSizeMin = 18f;
            subtitle.fontSizeMax = 32f;
            subtitle.raycastTarget = false;
            Outline edge = subtitle.GetComponent<Outline>();
            if (edge == null) edge = subtitle.gameObject.AddComponent<Outline>();
            edge.effectColor = new Color(.055f, .07f, .075f, .85f);
            edge.effectDistance = new Vector2(1.8f, -1.8f);
        }
    }

    private void StyleMenuButton(string name, string caption, Color backgroundColor)
    {
        Transform item = safeArea.Find(name);
        if (item == null) return;

        Button button = item.GetComponent<Button>();
        Image image = item.GetComponent<Image>();
        if (button == null || image == null) return;

        // Consistent solid button face even if the old scene used transparent sprites.
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = backgroundColor;
        image.raycastTarget = true;

        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, .93f, .82f, 1f);
        colors.pressedColor = new Color(.70f, .69f, .66f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(.65f, .65f, .65f, .55f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = .10f;
        button.colors = colors;

        Outline border = item.GetComponent<Outline>();
        if (border == null) border = item.gameObject.AddComponent<Outline>();
        border.effectColor = Gold;
        border.effectDistance = new Vector2(3f, -3f);

        // Outline is itself a subclass of Shadow, so GetComponent<Shadow>()
        // can return the gold outline. Find a genuine Shadow component instead.
        Shadow shadow = null;
        foreach (Shadow effect in item.GetComponents<Shadow>())
            if (effect.GetType() == typeof(Shadow))
            {
                shadow = effect;
                break;
            }
        if (shadow == null) shadow = item.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(.05f, .04f, .03f, .65f);
        shadow.effectDistance = new Vector2(0f, -6f);

        TMP_Text text = item.GetComponentInChildren<TMP_Text>(true);
        if (text == null) return;
        text.text = caption;
        text.color = Cream;
        text.fontStyle = FontStyles.Bold;
        text.fontSize = 44f;
        text.fontSizeMax = 44f;
        text.fontSizeMin = 20f;
        text.enableAutoSizing = true;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        // The original labels have a stretched rect with extra positive sizeDelta,
        // so the text used to extend beyond the button surface.
        RectTransform labelRect = text.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(18f, 8f);
        labelRect.offsetMax = new Vector2(-18f, -8f);
        labelRect.localRotation = Quaternion.identity;
        labelRect.localScale = Vector3.one;
    }

    private void ApplyMenuLayout()
    {
        if (safeArea == null || Screen.width < 1 || Screen.height < 1) return;

        Rect safe = Screen.safeArea;
        if (safe.width <= 0f || safe.height <= 0f)
            safe = new Rect(0f, 0f, Screen.width, Screen.height);

        safeArea.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        safeArea.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        safeArea.offsetMin = Vector2.zero;
        safeArea.offsetMax = Vector2.zero;
        Canvas.ForceUpdateCanvases();

        bool portrait = Screen.height > Screen.width;
        float w = safeArea.rect.width;
        float h = safeArea.rect.height;

        // Billboard lettering on the original gas-station background.
        Place("TitleText", portrait ? .87f : .865f,
            Mathf.Min(portrait ? 770f : 720f, w * (portrait ? .88f : .43f)),
            Mathf.Min(94f, h * .105f));

        // The orange billboard is slightly right of the screen center.
        // Correct its lettering position without moving the other UI.
        RectTransform billboardTitle = safeArea.Find("TitleText") as RectTransform;
        if (billboardTitle != null && !portrait)
            billboardTitle.anchoredPosition = new Vector2(w * .035f, h * .025f);
        Place("SubtitleText", portrait ? .75f : .70f,
            Mathf.Min(920f, w * (portrait ? .91f : .65f)),
            Mathf.Min(64f, h * .09f));

        float buttonWidth = Mathf.Min(portrait ? 760f : 600f,
            w * (portrait ? .83f : .58f));
        float buttonHeight = Mathf.Min(portrait ? 112f : 90f,
            h * (portrait ? .085f : .105f));

        Place("StartButton", portrait ? .56f : .51f, buttonWidth, buttonHeight);
        Place("HanojasButton", portrait ? .43f : .38f, buttonWidth, buttonHeight);
        Place("ExitButton", portrait ? .30f : .25f, buttonWidth, buttonHeight);

        lastSize = new Vector2Int(Screen.width, Screen.height);
        lastSafe = Screen.safeArea;
    }

    private void Place(string name, float y, float width, float height)
    {
        RectTransform item = safeArea.Find(name) as RectTransform;
        if (item == null) return;
        item.anchorMin = item.anchorMax = new Vector2(.5f, y);
        item.pivot = new Vector2(.5f, .5f);
        item.anchoredPosition3D = Vector3.zero;
        item.sizeDelta = new Vector2(width, height);
        item.localScale = Vector3.one;
        item.localRotation = Quaternion.identity;
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("CityScene");
    }

    public void OpenMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void StartHanojasTornis()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("HanojasTornis");
    }

    public void ExitGame()
    {
        Debug.Log("Exit game");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
