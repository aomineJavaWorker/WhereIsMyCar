using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class AdManager : MonoBehaviour
{
    [Header("Ads components")]
    public AdsInitializer adsInitializer;
    public InterstitialAd interstitialAd;
    public RewardedAds rewardedAds;
    public BannerAd bannerAd;

    [Header("Disable specific ad types if needed")]
    [SerializeField] private bool turnOffInterstitialAds = false;
    [SerializeField] private bool turnOffRewardedAds = false;
    [SerializeField] private bool turnOffBannerAds = false;

    public static AdManager Instance { get; private set; }

    // A scene has loaded and needs an interstitial. If the ad is still
    // loading, the request is fulfilled when OnInterstitialAdReady fires.
    private bool interstitialPendingForScene;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Stop duplicate SDK components before their Awake/Start can run.
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (adsInitializer == null)
            adsInitializer = FindFirstObjectByType<AdsInitializer>();
        if (interstitialAd == null)
            interstitialAd = FindFirstObjectByType<InterstitialAd>();
        if (rewardedAds == null)
            rewardedAds = FindFirstObjectByType<RewardedAds>();
        if (bannerAd == null)
            bannerAd = FindFirstObjectByType<BannerAd>();

        if (adsInitializer != null)
            adsInitializer.OnAdsInitialized += HandleAdsInitialized;
        if (interstitialAd != null)
            interstitialAd.OnInterstitialAdReady += HandleInterstitialReady;
        if (bannerAd != null)
            bannerAd.FullscreenAdsEnded += TryShowSceneInterstitial;
    }

    private void OnEnable()
    {
        if (Instance == this)
            SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // Covers the case where the SDK initialized before we subscribed.
        if (Advertisement.isInitialized)
            HandleAdsInitialized();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (adsInitializer != null)
            adsInitializer.OnAdsInitialized -= HandleAdsInitialized;
        if (interstitialAd != null)
            interstitialAd.OnInterstitialAdReady -= HandleInterstitialReady;
        if (bannerAd != null)
            bannerAd.FullscreenAdsEnded -= TryShowSceneInterstitial;

        if (Instance == this)
            Instance = null;
    }

    private void HandleAdsInitialized()
    {
        if (!turnOffInterstitialAds && interstitialAd != null)
            interstitialAd.LoadAd();

        if (!turnOffRewardedAds && rewardedAds != null)
            rewardedAds.LoadAd();

        if (!turnOffBannerAds && bannerAd != null)
            bannerAd.LoadBanner();

        TryShowSceneInterstitial();
    }

    private void HandleInterstitialReady()
    {
        TryShowSceneInterstitial();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // The UI objects belong to individual scenes, while this manager
        // survives scene changes. Rebind only the buttons that actually exist.
        BindSceneButtons();

        // Main menu displays only the banner, never an automatic fullscreen ad.
        if (scene.name == "MainMenu")
        {
            interstitialPendingForScene = false;
            return;
        }

        if (turnOffInterstitialAds || interstitialAd == null)
            return;

        // This also covers entering CityScene from MainMenu when the manager
        // was first created in CityScene (as in the existing project).
        interstitialPendingForScene = true;
        Debug.Log("AdManager: Scene loaded: " + scene.name + ". Waiting for interstitial.");

        TryShowSceneInterstitial();

        // If it was not loaded yet, request one. The ready callback will
        // attempt to show it once, without needing a separate UI button.
        if (interstitialPendingForScene)
            interstitialAd.LoadAd();
    }

    private void TryShowSceneInterstitial()
    {
        if (turnOffInterstitialAds || !interstitialPendingForScene ||
            interstitialAd == null || !Advertisement.isInitialized ||
            !interstitialAd.isReady ||
            (bannerAd != null && bannerAd.IsFullscreenAdActive))
            return;

        // Clear BEFORE ShowAd so a callback cannot show it twice.
        interstitialPendingForScene = false;
        Debug.Log("AdManager: Showing interstitial after scene change.");
        interstitialAd.ShowAd();
    }

    private void BindSceneButtons()
    {
        Button interstitialButton = FindSceneButton("InterstitialAdButton");
        Button rewardedButton = FindSceneButton("RewardedAdButton");
        Button bannerButton = FindSceneButton("BannerAdButton");

        if (interstitialAd != null)
            interstitialAd.SetButton(interstitialButton);

        if (rewardedAds != null)
            rewardedAds.SetButton(rewardedButton);

        if (bannerAd != null)
            bannerAd.SetButton(bannerButton);
    }

    private static Button FindSceneButton(string tagName)
    {
        GameObject obj = GameObject.FindGameObjectWithTag(tagName);
        return obj != null ? obj.GetComponent<Button>() : null;
    }
}
