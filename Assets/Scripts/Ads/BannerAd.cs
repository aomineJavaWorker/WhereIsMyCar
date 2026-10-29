using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Place this on the existing AdsScriptHolder in CityScene.
// Automatically displays a banner on game levels, but never over fullscreen ads.
public class BannerAd : MonoBehaviour
{
    // Keep these serialized field names: they are already configured in CityScene.
    [SerializeField] private string _AndroidAdUnitId = "Banner_Android";
    [SerializeField] private Button _bannerButton;
    [SerializeField] private BannerPosition bannerPosition = BannerPosition.BOTTOM_CENTER;
    [SerializeField, Min(1f)] private float retryDelaySeconds = 10f;

    public bool isBannerVisible = false;
    public bool IsFullscreenAdActive => _interstitialActive || _rewardedActive;
    public event Action FullscreenAdsEnded;

    private string _adUnitId;
    private bool _isLoading;
    private bool _isLoaded;
    private bool _showRequested;
    private bool _manualHidden;
    private bool _interstitialActive;
    private bool _rewardedActive;
    private bool _isGameplayScene;
    private Coroutine _initializationRoutine;
    private Coroutine _retryRoutine;
    private Coroutine _resumeRoutine;
    private static BannerAd _instance;
    private bool _isDuplicate;

    private bool CanShow => _isGameplayScene && !_manualHidden && !IsFullscreenAdActive;

    private void Awake()
    {
        // AdsScriptHolder is recreated when the level is loaded again.
        // The existing one survives with DontDestroyOnLoad, so ignore clones.
        if (_instance != null && _instance != this)
        {
            _isDuplicate = true;
            enabled = false;
            return;
        }
        _instance = this;
        _adUnitId = _AndroidAdUnitId;
        _isGameplayScene = SceneManager.GetActiveScene().name != "MainMenu";
        Advertisement.Banner.SetPosition(bannerPosition);
    }

    private void OnEnable()
    {
        if (!_isDuplicate)
            SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (_bannerButton != null)
            SetButton(_bannerButton);

        LoadBanner();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _isGameplayScene = scene.name != "MainMenu";
        if (!_isGameplayScene)
        {
            HideNativeBanner();
            Debug.Log("BannerAd: Hidden in MainMenu.");
            return;
        }

        Debug.Log("BannerAd: Entered " + scene.name + ", preparing banner.");
        LoadBanner();
        TryShowBanner();
    }

    public void LoadBanner()
    {
        if (!isActiveAndEnabled || _isLoading)
            return;

        if (_isLoaded)
        {
            TryShowBanner();
            return;
        }

        if (!Advertisement.isInitialized)
        {
            if (_initializationRoutine == null)
                _initializationRoutine = StartCoroutine(WaitForInitialization());
            return;
        }

        if (string.IsNullOrWhiteSpace(_adUnitId))
        {
            Debug.LogError("BannerAd: Ad Unit ID is empty. Check AdsScriptHolder in Inspector.");
            return;
        }

        _isLoading = true;
        Debug.Log("BannerAd: Loading " + _adUnitId);
        Advertisement.Banner.Load(_adUnitId, new BannerLoadOptions
        {
            loadCallback = OnBannerLoaded,
            errorCallback = OnBannerError
        });
    }

    private IEnumerator WaitForInitialization()
    {
        while (isActiveAndEnabled && !Advertisement.isInitialized)
            yield return new WaitForSecondsRealtime(0.5f);

        _initializationRoutine = null;
        if (isActiveAndEnabled)
            LoadBanner();
    }

    private void OnBannerLoaded()
    {
        _isLoading = false;
        _isLoaded = true;
        Debug.Log("BannerAd: Banner loaded successfully.");
        if (_bannerButton != null)
            _bannerButton.interactable = true;

        // This may run while a rewarded/interstitial is already in progress.
        TryShowBanner();
    }

    private void OnBannerError(string message)
    {
        _isLoading = false;
        _isLoaded = false;
        _showRequested = false;
        isBannerVisible = false;
        Debug.LogWarning("BannerAd: Failed to load '" + _adUnitId + "': " + message);
        if (_bannerButton != null)
            _bannerButton.interactable = false;

        if (_retryRoutine == null && isActiveAndEnabled)
            _retryRoutine = StartCoroutine(RetryLoad());
    }

    private IEnumerator RetryLoad()
    {
        yield return new WaitForSecondsRealtime(retryDelaySeconds);
        _retryRoutine = null;
        LoadBanner();
    }

    private void TryShowBanner()
    {
        if (!CanShow || !Advertisement.isInitialized || !_isLoaded ||
            _showRequested || isBannerVisible)
            return;

        _showRequested = true;
        Advertisement.Banner.SetPosition(bannerPosition);
        Debug.Log("BannerAd: Showing banner in " + SceneManager.GetActiveScene().name);
        Advertisement.Banner.Show(_adUnitId, new BannerOptions
        {
            clickCallback = OnBannerClicked,
            hideCallback = OnBannerHidden,
            showCallback = OnBannerShown
        });
    }

    // Public API retained for existing Inspector button and other scripts.
    public void ShowBannerAd()
    {
        _manualHidden = false;
        if (_isLoaded)
            TryShowBanner();
        else
            LoadBanner();
    }

    public void HideBannerAd()
    {
        // Manual hide, unlike the temporary fullscreen suppression below.
        _manualHidden = true;
        HideNativeBanner();
    }

    private void HideNativeBanner()
    {
        _showRequested = false;
        isBannerVisible = false;
        if (Advertisement.isInitialized)
            Advertisement.Banner.Hide();
    }

    public void BeginInterstitial()
    {
        _interstitialActive = true;
        HideNativeBanner();
    }

    public void EndInterstitial()
    {
        _interstitialActive = false;
        ResumeAfterFullscreen();
    }

    public void BeginRewarded()
    {
        _rewardedActive = true;
        HideNativeBanner();
    }

    public void EndRewarded()
    {
        _rewardedActive = false;
        ResumeAfterFullscreen();
    }

    private void ResumeAfterFullscreen()
    {
        // If another fullscreen ad remains active, keep the banner hidden.
        if (IsFullscreenAdActive)
            return;

        FullscreenAdsEnded?.Invoke();
        TryShowBanner();
    }

    private void OnBannerShown()
    {
        _showRequested = false;
        isBannerVisible = true;
        if (!CanShow)
        {
            // A fullscreen ad could have started while Banner.Show was pending.
            HideNativeBanner();
            return;
        }
        Debug.Log("BannerAd: Banner is visible.");
    }

    private void OnBannerHidden()
    {
        _showRequested = false;
        isBannerVisible = false;
        Debug.Log("BannerAd: Banner hidden.");

        // Hide callbacks can arrive AFTER an interstitial/rewarded has ended.
        // Re-show in that case, but not while another fullscreen ad is active.
        if (CanShow && _isLoaded && _resumeRoutine == null && isActiveAndEnabled)
            _resumeRoutine = StartCoroutine(RetryShowAfterHide());
    }

    private IEnumerator RetryShowAfterHide()
    {
        yield return new WaitForSecondsRealtime(0.25f);
        _resumeRoutine = null;
        TryShowBanner();
    }

    private void OnBannerClicked()
    {
        Debug.Log("BannerAd: Banner clicked.");
    }

    public void SetButton(Button button)
    {
        if (_bannerButton != null)
            _bannerButton.onClick.RemoveListener(ShowBannerAd);

        _bannerButton = button;
        if (_bannerButton == null)
            return;

        bool linkedInInspector = false;
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentTarget(i) == this &&
                button.onClick.GetPersistentMethodName(i) == nameof(ShowBannerAd))
            {
                linkedInInspector = true;
                break;
            }
        }
        if (!linkedInInspector)
            button.onClick.AddListener(ShowBannerAd);
        button.interactable = _isLoaded;
    }

    private void OnDestroy()
    {
        if (_bannerButton != null)
            _bannerButton.onClick.RemoveListener(ShowBannerAd);
        if (_instance == this)
            _instance = null;
    }
}
