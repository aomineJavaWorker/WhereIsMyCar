using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.Rendering;
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

    void Awake()
    {
        _adUnitId = _androidAdUnityId;
    }

    // Update is called once per frame
    private void SetReady(bool ready)
    {
        isReady = ready;
        if (_interstitialAdButton != null) _interstitialAdButton.interactable = ready;
    }

    // 

    public void OnInterstitialAdButtonClicked()
    {
        Debug.Log("Interstitial Ad Button Clicked");
        ShowInterstitial();
    }
    public void ShowInterstitial()
    {
        if(isReady)
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
        if (isReady)
        {
            timeScaleBeforeAd = Time.timeScale;
            sceneBeforeAd = UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
            SetReady(false);
            Advertisement.Show(_adUnitId, this);
        }else
        {
            Debug.LogWarning("Interstitial ad is not ready yet!");
        }
    }

    public void LoadAd()
    {
        if (loading || isReady) return;
        if (!Advertisement.isInitialized)
        {
            Debug.LogWarning("Tried to load interstitial ad before Unity Ads was intialized!");
            return;
        }
        Debug.Log("Loading interstitial ad...");
        loading = true;
        Advertisement.Load(_adUnitId, this);
    }
    public void OnUnityAdsAdLoaded(string placementId)
    {
        Debug.Log("Interstitial ad loaded succesfully!");
        loading = false;
        SetReady(true);
        OnInterstitialAdReady?.Invoke();
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.LogError($"Failed to load interstitial ad: {error.ToString()} - {message}");
        loading = false;
        SetReady(false);
    }

    public void OnUnityAdsShowClick(string placementId)
    {
        Debug.Log("Interstitial ad clicked!");
    }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        if (showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            Debug.Log("Interstitial ad completed succesfully!");
            StartCoroutine(SlowDownTImeTemporarily(30f));
            LoadAd();
        }
        else
        {
            Debug.LogWarning($"interstitial ad was not completed: {showCompletionState.ToString()}");
            RestoreTime();
            LoadAd();
        }

    }

    private IEnumerator SlowDownTImeTemporarily(float duration)
    {
        GameManager manager = FindFirstObjectByType<GameManager>();
        int scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
        if (manager == null || manager.GameEnded || scene != sceneBeforeAd) { RestoreTime(); yield break; }
        Time.timeScale = 0.3f;
        Debug.Log("Time slowed down for " + duration + " seconds.");
        yield return new WaitForSecondsRealtime(duration);
        if (scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle) RestoreTime();
        Debug.Log("Time restored to normal.");
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        RestoreTime();
        Debug.LogError($"Failed to show interstitial ad: {error.ToString()} - {message}");
        LoadAd();
    }

    public void OnUnityAdsShowStart(string placementId)
    {
        Debug.Log("Interstitial ad started showing!");
        Time.timeScale = 0f;
    }

    public void SetButton(Button button)
    {
        if (_interstitialAdButton != null)
            _interstitialAdButton.onClick.RemoveListener(OnInterstitialAdButtonClicked);
        _interstitialAdButton = button;
        if (button == null)
            return;
        button.onClick.RemoveListener(OnInterstitialAdButtonClicked);
        // Keep Inspector callbacks (sounds and other existing effects).
        bool linkedInInspector = false;
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            linkedInInspector |= button.onClick.GetPersistentTarget(i) == this;
        if (!linkedInInspector)
        button.onClick.AddListener(OnInterstitialAdButtonClicked);
        _interstitialAdButton.interactable = isReady;
    }

    private void RestoreTime()
    {
        GameManager manager = FindFirstObjectByType<GameManager>();
        if (manager != null && manager.GameEnded) Time.timeScale = 0f;
        else if (sceneBeforeAd == UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle)
            Time.timeScale = timeScaleBeforeAd;
    }



}
