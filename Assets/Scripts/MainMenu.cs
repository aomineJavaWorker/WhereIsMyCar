using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    private Canvas menuCanvas;
    private RectTransform safeArea;
    private Rect lastSafe;
    private Vector2Int lastSize;

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Application.targetFrameRate = 60;
#endif
        if (gameObject.scene.name != "MainMenu") return;
        menuCanvas = GetComponent<Canvas>();
        if (menuCanvas == null) return;
        CanvasScaler scaler = menuCanvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = menuCanvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        GameObject safe = new GameObject("MenuSafeArea", typeof(RectTransform), typeof(MobileUISafeArea));
        safe.transform.SetParent(menuCanvas.transform, false);
        safeArea = safe.GetComponent<RectTransform>();
        string[] names = { "TitleText", "SubtitleText", "StartButton", "HanojasButton", "ExitButton" };
        foreach (string name in names)
        {
            Transform item = menuCanvas.transform.Find(name);
            if (item != null) item.SetParent(safeArea, false);
        }
        Transform background = menuCanvas.transform.Find("Background");
        if (background != null && background.TryGetComponent(out Image image))
        {
            image.raycastTarget = false;
            AspectRatioFitter fitter = background.GetComponent<AspectRatioFitter>();
            if (fitter == null) fitter = background.gameObject.AddComponent<AspectRatioFitter>();
            if (image.sprite != null) fitter.aspectRatio = image.sprite.rect.width / image.sprite.rect.height;
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        }
        ApplyMenuLayout();
    }

    private void LateUpdate()
    {
        if (safeArea != null && (lastSize.x != Screen.width || lastSize.y != Screen.height || lastSafe != Screen.safeArea))
            ApplyMenuLayout();
    }

    private void ApplyMenuLayout()
    {
        if (Screen.width < 1 || Screen.height < 1) return;
        Rect safe = Screen.safeArea;
        if (safe.width <= 0 || safe.height <= 0) safe = new Rect(0, 0, Screen.width, Screen.height);
        safeArea.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        safeArea.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        Canvas.ForceUpdateCanvases();
        bool portrait = Screen.height > Screen.width;
        PlaceMenuItem("TitleText", .83f, .90f, 140f, 76f);
        PlaceMenuItem("SubtitleText", .71f, .88f, 100f, 34f);
        PlaceMenuItem("StartButton", portrait ? .54f : .53f, .70f, 156f, 50f);
        PlaceMenuItem("HanojasButton", portrait ? .38f : .36f, .70f, 156f, 50f);
        PlaceMenuItem("ExitButton", portrait ? .22f : .19f, .70f, 156f, 50f);
        lastSize = new Vector2Int(Screen.width, Screen.height);
        lastSafe = Screen.safeArea;
    }

    private void PlaceMenuItem(string name, float y, float widthFraction, float height, float fontSize)
    {
        RectTransform item = safeArea.Find(name) as RectTransform;
        if (item == null) return;
        item.anchorMin = item.anchorMax = new Vector2(.5f, y);
        item.pivot = new Vector2(.5f, .5f);
        item.anchoredPosition3D = Vector3.zero;
        float heightFraction = item.GetComponent<Button>() != null ? .145f : .12f;
        item.sizeDelta = new Vector2(Mathf.Min(1000f, safeArea.rect.width * widthFraction),
            Mathf.Min(height, safeArea.rect.height * heightFraction));
        item.localScale = Vector3.one;
        TMP_Text text = item.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = 24;
            text.fontSizeMax = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }
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
