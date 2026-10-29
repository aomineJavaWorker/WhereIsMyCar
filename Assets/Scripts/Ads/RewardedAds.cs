using System.Collections;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.UI;

// Rewarded ads are launched ONLY by the player.
// After finishing, destroy flying objects and allow another ad after 15 real seconds.
public class RewardedAds : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    // Preserve these serialized names, since CityScene already contains their values.
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
    private bool _bannerLocked;
    private float _cooldownEndsAt;
    private float _previousTimeScale = 1f;
    private bool _pausedByThisAd;

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

        if (_rewardedAdButton != null)
            SetButton(_rewardedAdButton);
        else
            UpdateButton();
    }

    private void Start()
    {
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
            Debug.LogError("RewardedAds: Ad Unit ID is empty.");
            return;
        }

        _isLoading = true;
        UpdateButton();
        Debug.Log("RewardedAds: Loading " + _adUnitId);
        Advertisement.Load(_adUnitId, this);
    }

    private IEnumerator WaitForInitialization()
    {
        while (isActiveAndEnabled && !Advertisement.isInitialized)
            yield return new WaitForSecondsRealtime(0.5f);

        _initializationRoutine = null;
        if (isActiveAndEnabled)
            LoadAd();
    }

    public void OnUnityAdsAdLoaded(string placementId)
    {
        if (placementId != _adUnitId) return;
        _isLoading = false;
        _isLoaded = true;
        Debug.Log("RewardedAds: Loaded " + placementId);
        UpdateButton();
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        if (placementId != _adUnitId) return;
        _isLoading = false;
        _isLoaded = false;
        Debug.LogError("RewardedAds: Failed to load " + placementId + ": " + error + " - " + message);
        UpdateButton();
        ScheduleRetry();
    }

    public void ShowAd()
    {
        if (_isShowing || !_isLoaded || !Advertisement.isInitialized ||
            Time.realtimeSinceStartup < _cooldownEndsAt)
        {
            Debug.Log("RewardedAds: Not ready or cooling down.");
            return;
        }

        if (_bannerAd == null)
            _bannerAd = FindFirstObjectByType<BannerAd>();
        if (_bannerAd != null && _bannerAd.IsFullscreenAdActive)
        {
            Debug.Log("RewardedAds: Another fullscreen ad is already active.");
            return;
        }

        _isShowing = true;
        _isLoaded = false;
        UpdateButton();

        // Lock banner visibility BEFORE the fullscreen SDK call. This also
        // works when the banner is loading asynchronously, not yet visible.
        if (_bannerAd != null)
        {
            _bannerAd.BeginRewarded();
            _bannerLocked = true;
        }

        Debug.Log("RewardedAds: Opening video after player click.");
        Advertisement.Show(_adUnitId, this);
    }

    public void OnUnityAdsShowStart(string placementId)
    {
        if (placementId != _adUnitId) return;
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
            Debug.Log("RewardedAds: Ad clicked.");
    }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        if (placementId != _adUnitId) return;

        RestoreTimeScale();
        _isShowing = false;
        RestoreBannerAfterReward();

        if (showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            GrantReward();
            BeginCooldown();
            Debug.Log("RewardedAds: Reward granted; 15-second cooldown started.");
        }
        else
        {
            Debug.Log("RewardedAds: Video not completed; no reward.");
        }

        LoadAd();
        UpdateButton();
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        if (placementId != _adUnitId) return;

        RestoreTimeScale();
        _isShowing = false;
        _isLoaded = false;
        RestoreBannerAfterReward();
        Debug.LogError("RewardedAds: Failed to show " + placementId + ": " + error + " - " + message);
        UpdateButton();
        ScheduleRetry();
    }

    private void RestoreBannerAfterReward()
    {
        if (!_bannerLocked)
            return;
        _bannerLocked = false;
        if (_bannerAd != null)
            _bannerAd.EndRewarded();
    }

    private void GrantReward()
    {
        if (FlyingObjectManager == null)
            FlyingObjectManager = FindFirstObjectByType<FlyingObjectManager>();

        if (FlyingObjectManager != null)
            FlyingObjectManager.DestroyAllFlyingObjects();
        else
            Debug.LogWarning("RewardedAds: FlyingObjectManager missing. No reward applied.");
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
            _rewardedAdButton.interactable = _isLoaded && !_isShowing &&
                Time.realtimeSinceStartup >= _cooldownEndsAt;
    }

    public void SetButton(Button button)
    {
        if (_rewardedAdButton != null)
            _rewardedAdButton.onClick.RemoveListener(ShowAd);

        _rewardedAdButton = button;
        if (button == null) return;

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
        if (!_pausedByThisAd) return;
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
