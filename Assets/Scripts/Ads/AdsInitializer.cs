using System;
using UnityEngine;
using UnityEngine.Advertisements;
public class AdsInitializer : MonoBehaviour, IUnityAdsInitializationListener
{
    [SerializeField] string _androidGameId;
    [SerializeField] bool _testMode = true;
    private string _gameId;
    public event Action OnAdsInitialized;

    void Awake()
    {
        InitializeAds();
    }
    public void InitializeAds()
    {
#if UNITY_ANDROID || UNITY_EDITOR
        _gameId = _androidGameId;
#endif
        if(!Advertisement.isInitialized && Advertisement.isSupported)
        {
            Advertisement.Initialize(_gameId, _testMode, this);
        }
    }

    public void OnInitializationComplete()
    {
        Debug.Log("Unity Ads intialization complete");
        OnAdsInitialized?.Invoke();

    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
    }

}
