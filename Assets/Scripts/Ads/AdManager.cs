using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AdManager : MonoBehaviour
{
    public AdsInitializer adsInitializer;
    public InterstitialAd interstitialAd;
    [SerializeField] bool turnOffInterstitialAds = false;
    private bool firstAdShown = false;

    public RewardedAds rewardedAds;
    [SerializeField] bool turnOffRewardedAds = false;

    public BannerAd bannerAd;
    [SerializeField] bool turnOffBannerAds = false;

    private bool firstSceneLoaded = false;



    public static AdManager Instance { get; private set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        if (adsInitializer == null)
        {
            adsInitializer = FindFirstObjectByType<AdsInitializer>();
        }

        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (interstitialAd == null) interstitialAd = FindFirstObjectByType<InterstitialAd>();
        if (adsInitializer != null) adsInitializer.OnAdsInitialized += HandleAdsInitialized;
        if (interstitialAd != null) interstitialAd.OnInterstitialAdReady += HandleInterstitialReady;
    }

    private void Start()
    {
        if (UnityEngine.Advertisements.Advertisement.isInitialized) HandleAdsInitialized();
    }

    private void OnDestroy()
    {
        if (adsInitializer != null) adsInitializer.OnAdsInitialized -= HandleAdsInitialized;
        if (interstitialAd != null) interstitialAd.OnInterstitialAdReady -= HandleInterstitialReady;
        if (Instance == this) Instance = null;
    }

    private void HandleAdsInitialized()
    {
        if (!turnOffInterstitialAds && interstitialAd != null)
        {
            interstitialAd.LoadAd();
        }
        if (!turnOffRewardedAds)
        {
            rewardedAds.LoadAd();
        }

        if (!turnOffBannerAds)
        {
            bannerAd.LoadBanner();
        }
        //Velak
    }

    private void HandleInterstitialReady()
    {
        if (!firstAdShown)
        {
            Debug.Log("Showing interstitial ad for the first time!");
            interstitialAd.ShowAd();
            firstAdShown = true;
        }
        else
        {
            Debug.Log("Interstitial ad is ready to be shown again!");
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (interstitialAd == null)
            interstitialAd = FindAnyObjectByType<InterstitialAd>();

        GameObject buttonObject = GameObject.FindGameObjectWithTag("InterstitialAdButton");
        Button interstitialButton = buttonObject != null ? buttonObject.GetComponent<Button>() : null;
        if (interstitialAd != null)
        {
            interstitialAd.SetButton(interstitialButton);
        }

        if (rewardedAds == null)
            rewardedAds = FindFirstObjectByType<RewardedAds>();

        Button rewardedAdBitton = GameObject.FindGameObjectWithTag("RewardedAdButton").GetComponent<Button>();

        if (rewardedAds != null && rewardedAdBitton != null)
        {
            rewardedAds.SetButton(rewardedAdBitton);
        }
        //velak

        Button bannerAdButton = GameObject.FindGameObjectWithTag("BannerAdButton").GetComponent<Button>();

        if (bannerAd != null && bannerAdButton != null)
        {
            bannerAd.SetButton(bannerAdButton);
        }
        if (!firstSceneLoaded)
        {
            firstSceneLoaded = true;
            Debug.Log("First scene loaded");
            return;
        }

        Debug.Log("Scene reloaded!");
        HandleAdsInitialized();
    }
}
