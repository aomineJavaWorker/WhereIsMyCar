using System.Collections;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.UI;

// Rewarded video: repeatable after a 15-second REAL-TIME cooldown.
// The button becomes clickable only when the cooldown has ended AND a new ad is loaded.
public class RewardedAds : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [Header("Unity Ads")]
    [SerializeField] private string _androidAdUnitId = "BP_Rewarded_Android";
    [SerializeField] private Button _rewardedAdButton;

    [Header("Reward")]
    public FlyingObjectManager FlyingObjectManager;

    [Header("Banner integration")]
    [SerializeField] private BannerAd _bannerAd;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float cooldownSeconds = 15f;
    [SerializeField, Min(1f)] private float retryDelaySeconds = 10f;

    private string _adUnitId;
    private bool _isLoaded;
    private bool _isLoading;
    private bool _isShowing;
    private float _cooldownEndsAt;
    private float _previousTimeScale = 1f;
    private bool _pausedByThisAd;
    private bool _bannerWasVisible;
    private bool _bannerHiddenForReward;

    private Coroutine _initializationRoutine;
    private Coroutine _retryRoutine;
    private Coroutine _cooldownRoutine;

    private void Awake()
    {
        _adUnitId = _androidAdUnitId;
        if (FlyingObjectManager == null)
            FlyingObjectManager = FindFirstObjectByType<FlyingObjectManager>();
        if (_bannerAd == null)
            _bannerAd = FindFirstObjectByType<BannerAd>();

        // Attach the button automatically if it is assigned in the Inspector.
        // AdManager in this project only connects Interstitial, not Rewarded.
        if (_rewardedAdButton != null)
            SetButton(_rewardedAdButton);
        else
            UpdateButton();
    }

    private void Start()
    {
        // AdsInitializer initializes Unity Ads separately. Wait for it rather than
        // trying to load an ad before the SDK is ready.
        LoadAd();
    }

    public void LoadAd()
    {
        if (!isActiveAndEnabled || _isLoading || _isLoaded || _isShowing)
            return;

        if (!Advertisement.isInitialized)
        {
            if (_initializationRoutine == null)
                _initializationRoutine = StartCoroutine(WaitForInitialization());
            return;
        }

        if (string.IsNullOrWhiteSpace(_adUnitId))
        {
            Debug.LogError("Rewarded Ads: Android Ad Unit ID is empty.");
            return;
        }

        _isLoading = true;
        UpdateButton();
        Debug.Log("Loading rewarded ad: " + _adUnitId);
        Advertisement.Load(_adUnitId, this);
    }

    private IEnumerator WaitForInitialization()
    {
        while (!Advertisement.isInitialized)
            yield return new WaitForSecondsRealtime(0.5f);

        _initializationRoutine = null;
        LoadAd();
    }

    public void OnUnityAdsAdLoaded(string placementId)
    {
        if (placementId != _adUnitId)
            return;

        _isLoading = false;
        _isLoaded = true;
        Debug.Log("Rewarded ad loaded: " + placementId);
        UpdateButton();
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        if (placementId != _adUnitId)
            return;

        _isLoading = false;
        _isLoaded = false;
        Debug.LogError($"Failed to load rewarded ad {placementId}: {error} - {message}");
        UpdateButton();
        ScheduleRetry();
    }

    public void ShowAd()
    {
        if (_isShowing || !_isLoaded || !Advertisement.isInitialized ||
            Time.realtimeSinceStartup < _cooldownEndsAt)
        {
            Debug.Log("Rewarded ad is not ready yet (still loading or cooling down).");
            return;
        }

        _isShowing = true;
        _isLoaded = false; // The currently loaded ad can only be shown once.
        UpdateButton();
        HideBannerForReward();
        Advertisement.Show(_adUnitId, this);
    }

    public void OnUnityAdsShowStart(string placementId)
    {
        if (placementId != _adUnitId)
            return;

        if (!_pausedByThisAd)
        {
            _previousTimeScale = Time.timeScale;
            _pausedByThisAd = true;
            Time.timeScale = 0f;
        }
    }

    public void OnUnityAdsShowClick(string placementId)
    {
        if (placementId == _adUnitId)
            Debug.Log("Rewarded ad clicked: " + placementId);
    }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        if (placementId != _adUnitId)
            return;

        RestoreTimeScale();
        RestoreBannerAfterReward();
        _isShowing = false;

        if (showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            Debug.Log("Rewarded ad completed. Granting reward and starting 15s cooldown.");
            GrantReward();
            BeginCooldown();
        }
        else
        {
            // No reward (and no cooldown) for an interrupted/skipped video.
            Debug.LogWarning("Rewarded ad was not completed: " + showCompletionState);
        }

        // Preload the next ad during the cooldown. Do not enable the button until
        // BOTH the cooldown has expired and the next ad has loaded.
        LoadAd();
        UpdateButton();
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        if (placementId != _adUnitId)
            return;

        RestoreTimeScale();
        RestoreBannerAfterReward();
        _isShowing = false;
        _isLoaded = false;
        Debug.LogError($"Failed to show rewarded ad {placementId}: {error} - {message}");
        UpdateButton();
        ScheduleRetry();
    }

    // Rewarded video is opened only after the player presses the rewarded button.
    private void HideBannerForReward()
    {
        if (_bannerAd == null)
            _bannerAd = FindFirstObjectByType<BannerAd>();

        _bannerWasVisible = _bannerAd != null && _bannerAd.isBannerVisible;
        _bannerHiddenForReward = _bannerWasVisible;

        if (_bannerHiddenForReward)
            _bannerAd.HideBannerAd();
    }

    // Restore only a banner that was visible before watching the video.
    private void RestoreBannerAfterReward()
    {
        if (!_bannerHiddenForReward)
            return;

        _bannerHiddenForReward = false;
        if (_bannerAd != null && _bannerWasVisible && isActiveAndEnabled)
            StartCoroutine(RestoreBannerWhenHidden());

        _bannerWasVisible = false;
    }

    private IEnumerator RestoreBannerWhenHidden()
    {
        // BannerAd.HideBannerAd() receives its hide callback asynchronously.
        // Do not call its toggle method before isBannerVisible is set to false.
        float stopWaitingAt = Time.realtimeSinceStartup + 2f;
        while (_bannerAd != null && _bannerAd.isBannerVisible &&
               Time.realtimeSinceStartup < stopWaitingAt)
            yield return null;

        if (_bannerAd != null && !_bannerAd.isBannerVisible && !_isShowing)
            _bannerAd.ShowBannerAd();
    }

    private void GrantReward()
    {
        if (FlyingObjectManager == null)
            FlyingObjectManager = FindFirstObjectByType<FlyingObjectManager>();

        if (FlyingObjectManager != null)
            FlyingObjectManager.DestroyAllFlyingObjects();
        else
            Debug.LogWarning("Rewarded ad completed, but FlyingObjectManager was not found. Reward not applied.");
    }

    private void BeginCooldown()
    {
        _cooldownEndsAt = Time.realtimeSinceStartup + cooldownSeconds;
        UpdateButton();

        if (_cooldownRoutine != null)
            StopCoroutine(_cooldownRoutine);
        _cooldownRoutine = StartCoroutine(WaitForCooldown());
    }

    private IEnumerator WaitForCooldown()
    {
        while (Time.realtimeSinceStartup < _cooldownEndsAt)
            yield return new WaitForSecondsRealtime(0.2f);

        _cooldownRoutine = null;
        UpdateButton();
    }

    private void ScheduleRetry()
    {
        if (_retryRoutine == null && isActiveAndEnabled)
            _retryRoutine = StartCoroutine(RetryLoad());
    }

    private IEnumerator RetryLoad()
    {
        yield return new WaitForSecondsRealtime(retryDelaySeconds);
        _retryRoutine = null;
        LoadAd();
    }

    private void UpdateButton()
    {
        if (_rewardedAdButton != null)
            _rewardedAdButton.interactable = !_isShowing && _isLoaded &&
                Time.realtimeSinceStartup >= _cooldownEndsAt;
    }

    public void SetButton(Button button)
    {
        if (_rewardedAdButton != null)
            _rewardedAdButton.onClick.RemoveListener(ShowAd);

        _rewardedAdButton = button;
        if (button == null)
            return;

        // Keep other callbacks, e.g. existing UI sounds.
        bool linkedInInspector = false;
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentTarget(i) == this &&
                button.onClick.GetPersistentMethodName(i) == nameof(ShowAd))
            {
                linkedInInspector = true;
                break;
            }
        }

        if (!linkedInInspector)
            button.onClick.AddListener(ShowAd);

        UpdateButton();
    }

    private void RestoreTimeScale()
    {
        if (!_pausedByThisAd)
            return;

        Time.timeScale = _previousTimeScale;
        _pausedByThisAd = false;
    }

    private void OnDestroy()
    {
        if (_rewardedAdButton != null)
            _rewardedAdButton.onClick.RemoveListener(ShowAd);

        RestoreTimeScale();
        RestoreBannerAfterReward();
    }
}
