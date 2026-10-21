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

    void Awake()
    {
        _adUnitId = _androidAdUnityId;
    }

    // Update is called once per frame
    private void Update()
    {
        if(AdManager.Instance != null && AdManager.Instance.interstitialAd != null)
        {
            _interstitialAdButton.interactable = AdManager.Instance.interstitialAd.isReady;
        }
    }

    // 

    public void OnInterstitialAdButtonClicked()
    {
        Debug.Log("Interstitial Ad Button Clicked");
        ShowInterstitial();
    }
    public void ShowInterstitial()
    {
        if(AdManager.Instance.interstitialAd != null && AdManager.Instance.interstitialAd.isReady)
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

            Advertisement.Show(_adUnitId, this);
            isReady = false;
        }else
        {
            Debug.LogWarning("Interstitial ad is not ready yet!");
        }
    }

    public void LoadAd()
    {
        if (!Advertisement.isInitialized)
        {
            Debug.LogWarning("Tried to load interstitial ad before Unity Ads was intialized!");
            return;
        }
        Debug.Log("Loading interstitial ad...");
        Advertisement.Load(_adUnitId, this);
    }
    public void OnUnityAdsAdLoaded(string placementId)
    {
        Debug.Log("Interstitial ad loaded succesfully!");
        _interstitialAdButton.interactable = true;
        isReady = true;
        OnInterstitialAdReady?.Invoke();
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.LogError($"Failed to load interstitial ad: {error.ToString()} - {message}");
        LoadAd();
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
            Time.timeScale = 1f;
            LoadAd();
        }

    }

    private IEnumerator SlowDownTImeTemporarily(float duration)
    {
        Time.timeScale = 0.3f;
        Debug.Log("Time slowed down for " + duration + " seconds.");
        yield return new WaitForSeconds(duration);
        Time.timeScale = 1f;
        Debug.Log("Time restored to normal.");
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
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
        if (button == null)
            return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnInterstitialAdButtonClicked);
        _interstitialAdButton = button;
        _interstitialAdButton.interactable = false;
    }



}
