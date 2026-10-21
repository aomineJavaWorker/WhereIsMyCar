using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AdManager : MonoBehaviour
{
    public AdsInitializer adsInitializer;
    public InterstitialAd interstitialAd;
    [SerializeField] bool turnOffInterstitialAds = false;
    private bool firstAdShown = false;

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
        adsInitializer.OnAdsInitialized += HandleAdsInitialized;
    }

    private void HandleAdsInitialized()
    {
        if (!turnOffInterstitialAds)
        {
            interstitialAd.OnInterstitialAdReady += HandleInterstitialReady;
            interstitialAd.LoadAd();
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

        Button interstitialButton =
            GameObject.FindGameObjectWithTag("HandleAdsInitialized").GetComponent<Button>();
        if (interstitialAd != null && interstitialButton != null)
        {
            interstitialAd.SetButton(interstitialButton);
        }
        //velak
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
