using System;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.UI;

public class BannerAd : MonoBehaviour
{
    [SerializeField] string _AndroidAdUnitId = "Banner_Android";
    string _adUnitId;

    [SerializeField] Button _bannerButton;
    public bool isBannerVisible = false;

    [SerializeField] BannerPosition bannerPosition = BannerPosition.BOTTOM_CENTER;

    private void Awake()
    {
        _adUnitId = _AndroidAdUnitId;
        Advertisement.Banner.SetPosition(bannerPosition);
    }

    public void LoadBanner()
    {
        if (!Advertisement.isInitialized)
        {
            Debug.Log("Tried to load banner ad before Unity ads was initialized");
            return;
        }

        BannerLoadOptions bannerOptions = new BannerLoadOptions
        {
            loadCallback = OnBannerLoaded,
            errorCallback = OnBannerError
        };

        Advertisement.Banner.Load(_adUnitId, bannerOptions);
    }

    void OnBannerLoaded()
    {
        Debug.Log("Banner ad loaded!");
        _bannerButton.interactable = true;
    }

    void OnBannerError(string message)
    {
        Debug.Log($"Banner error: {message}");
        LoadBanner();
    }

    public void ShowBannerAd()
    {
        if (isBannerVisible)
        {
            HideBannerAd();
        }else
        {
            BannerOptions bannerOptions = new BannerOptions
            {
                clickCallback = OnBannerClicked,
                hideCallback = OnBannerHidden,
                showCallback = OnBannerShown
            };
            Advertisement.Banner.Show(_adUnitId, bannerOptions);
        }
    }

     

     void OnBannerHidden()
    {
        Debug.Log("Banner ad is hiden! " + _adUnitId);
        isBannerVisible = false;
    }

    void OnBannerShown()
    {
        Debug.Log("Banner ad is visible " + _adUnitId);
        isBannerVisible = true;
    }

    void OnBannerClicked()
    {
        Debug.Log("User clicked on banner ad: " + _adUnitId);
    }

    public void HideBannerAd()
    {
        Advertisement.Banner.Hide();
    }

    public void SetButton(Button button)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(ShowBannerAd);
        button.onClick.AddListener(ShowBannerAd);
        _bannerButton = button;
        _bannerButton.interactable = false;
    }
}
