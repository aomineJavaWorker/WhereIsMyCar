using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.UI;

public class InterstitialAd : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] string _androidAdUnityId = "Interstitial_Android";
    string _adUnitId;
    public event Action OnInterstitialAdReady;
    public bool isReady = false;
    [SerializeField] Button _interstitialAdButton;
    private bool loading;
    private float timeScaleBeforeAd = 1f;
    private int sceneBeforeAd;
    private bool isShowing;
    private BannerAd activeBanner;

    void Awake()
    {
        _adUnitId = _androidAdUnityId;
    }

    private void SetReady(bool ready)
    {
        isReady = ready;
        if (_interstitialAdButton != null) _interstitialAdButton.interactable = ready;
    }

    public void OnInterstitialAdButtonClicked()
    {
        Debug.Log("Interstitial Ad Button Clicked");
        ShowInterstitial();
    }

    public void ShowInterstitial()
    {
        if (isReady)
        {
            Debug.Log("Showing interstitial ad manually!");
            ShowAd();
        }
        else
        {
            Debug.LogWarning("Interstitial ad is not ready yet, loading again!");
            LoadAd();
        }
    }

    public void ShowAd()
    {
        if (!isReady || isShowing || !Advertisement.isInitialized)
        {
            Debug.LogWarning("Interstitial ad is not ready yet or already showing.");
            return;
        }

        activeBanner = AdManager.Instance != null
            ? AdManager.Instance.bannerAd : FindFirstObjectByType<BannerAd>();
        if (activeBanner != null && activeBanner.IsFullscreenAdActive)
        {
            Debug.Log("Interstitial ad: another fullscreen ad is active; delaying show.");
            return;
        }

        // Block the banner BEFORE asking the SDK to open the fullscreen ad.
        if (activeBanner != null)
            activeBanner.BeginInterstitial();

        isShowing = true;
        timeScaleBeforeAd = Time.timeScale;
        sceneBeforeAd = UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
        SetReady(false);
        Debug.Log("Interstitial ad: opening fullscreen advertisement.");
        Advertisement.Show(_adUnitId, this);
    }

    public void LoadAd()
    {
        if (loading || isReady) return;
        if (!Advertisement.isInitialized)
        {
            Debug.LogWarning("Tried to load interstitial ad before Unity Ads was initialized!");
            return;
        }
        Debug.Log("Loading interstitial ad...");
        loading = true;
        Advertisement.Load(_adUnitId, this);
    }

    public void OnUnityAdsAdLoaded(string placementId)
    {
        if (placementId != _adUnitId) return;
        Debug.Log("Interstitial ad loaded successfully!");
        loading = false;
        SetReady(true);
        OnInterstitialAdReady?.Invoke();
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        if (placementId != _adUnitId) return;
        Debug.LogError($"Failed to load interstitial ad: {error} - {message}");
        loading = false;
        SetReady(false);
    }

    public void OnUnityAdsShowClick(string placementId)
    {
        Debug.Log("Interstitial ad clicked!");
    }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        if (placementId != _adUnitId) return;

        if (showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            Debug.Log("Interstitial ad completed successfully!");
            StartCoroutine(SlowDownTImeTemporarily(30f));
        }
        else
        {
            Debug.LogWarning($"Interstitial ad was not completed: {showCompletionState}");
            RestoreTime();
        }

        isShowing = false;
        RestoreBannerAfterInterstitial();
        LoadAd();
    }

    private IEnumerator SlowDownTImeTemporarily(float duration)
    {
        GameManager manager = FindFirstObjectByType<GameManager>();
        int scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
        if (manager == null || manager.GameEnded || scene != sceneBeforeAd)
        {
            RestoreTime();
            yield break;
        }
        Time.timeScale = 0.3f;
        Debug.Log("Time slowed down for " + duration + " seconds.");
        yield return new WaitForSecondsRealtime(duration);
        if (scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle)
            RestoreTime();
        Debug.Log("Time restored to normal.");
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        if (placementId != _adUnitId) return;
        RestoreTime();
        isShowing = false;
        RestoreBannerAfterInterstitial();
        Debug.LogError($"Failed to show interstitial ad: {error} - {message}");
        LoadAd();
    }

    public void OnUnityAdsShowStart(string placementId)
    {
        if (placementId != _adUnitId) return;
        Debug.Log("Interstitial ad started showing!");
        Time.timeScale = 0f;
    }

    public void SetButton(Button button)
    {
        if (_interstitialAdButton != null)
            _interstitialAdButton.onClick.RemoveListener(OnInterstitialAdButtonClicked);

        _interstitialAdButton = button;
        if (button == null) return;

        bool linkedInInspector = false;
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            linkedInInspector |= button.onClick.GetPersistentTarget(i) == this;

        if (!linkedInInspector)
            button.onClick.AddListener(OnInterstitialAdButtonClicked);

        _interstitialAdButton.interactable = isReady;
    }

    private void RestoreBannerAfterInterstitial()
    {
        if (activeBanner != null)
        {
            activeBanner.EndInterstitial();
            activeBanner = null;
        }
    }

    private void RestoreTime()
    {
        GameManager manager = FindFirstObjectByType<GameManager>();
        if (manager != null && manager.GameEnded) Time.timeScale = 0f;
        else if (sceneBeforeAd == UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle)
            Time.timeScale = timeScaleBeforeAd;
    }
}
