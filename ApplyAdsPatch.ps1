# Run from a backup/committed Unity project, with this file in the project root.
# Changes only BannerAd.cs and RewardedAds.cs and is safe to run twice.
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$bannerFile = Join-Path $root 'Assets/Scripts/Ads/BannerAd.cs'
$rewardFile = Join-Path $root 'Assets/Scripts/Ads/RewardedAds.cs'
if (!(Test-Path $bannerFile) -or !(Test-Path $rewardFile)) {
    throw 'Run this script from the Unity project root (same directory as Assets).'
}

$banner = [System.IO.File]::ReadAllText($bannerFile)
$oldAwake = '_isGameplayScene = SceneManager.GetActiveScene().name == "CityScene";'
$newAwake = '_isGameplayScene = SceneManager.GetActiveScene().name == "CityScene" || SceneManager.GetActiveScene().name == "HanojasTornis";'
$oldLoaded = '_isGameplayScene = scene.name == "CityScene";'
$newLoaded = '_isGameplayScene = scene.name == "CityScene" || scene.name == "HanojasTornis";'
if ($banner.Contains($oldAwake)) { $banner = $banner.Replace($oldAwake, $newAwake) }
elseif (!$banner.Contains($newAwake)) { throw 'BannerAd Awake pattern not found; inspect before changing.' }
if ($banner.Contains($oldLoaded)) { $banner = $banner.Replace($oldLoaded, $newLoaded) }
elseif (!$banner.Contains($newLoaded)) { throw 'BannerAd OnSceneLoaded pattern not found.' }

$reward = [System.IO.File]::ReadAllText($rewardFile)
if (!$reward.Contains('public event System.Action RewardGranted;')) {
    $needle = 'public class RewardedAds : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener'
    if (!$reward.Contains($needle)) { throw 'RewardedAds class declaration not found.' }
    # Insert just after the class opening brace.
    $pattern = '(public class RewardedAds : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener\s*\{)'
    $reward = [System.Text.RegularExpressions.Regex]::Replace($reward, $pattern,
        '$1' + "`r`n    public event System.Action RewardGranted;", 1)
}
if (!$reward.Contains('RewardGranted?.Invoke();')) {
    $needle = 'Debug.LogWarning("RewardedAds: FlyingObjectManager missing. No reward applied.");'
    if (!$reward.Contains($needle)) { throw 'RewardedAds reward method pattern not found.' }
    $reward = $reward.Replace($needle, $needle + "`r`n`r`n        RewardGranted?.Invoke();")
}

[System.IO.File]::WriteAllText($bannerFile, $banner, [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText($rewardFile, $reward, [System.Text.UTF8Encoding]::new($false))
Write-Host 'OK: Banner shows in CityScene and HanojasTornis; RewardedAds exposes RewardGranted event.' -ForegroundColor Green
