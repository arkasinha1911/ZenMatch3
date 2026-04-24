using UnityEngine;
using System;
using System.Collections;
using Unity.Services.LevelPlay;

/// <summary>
/// AdManager acts as the central hub for IronSource LevelPlay Ad SDK integrations.
/// </summary>
public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    [Header("IronSource Settings")]
    public string androidAppKey = "260ae7de5";
    public string rewardedAdUnitId = "j8ftlz7n20bvqxud";

    private LevelPlayRewardedAd rewardedVideoAd;
    private Action<bool> currentAdCallback;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(androidAppKey))
        {
            Debug.LogWarning("AdManager: App Key is empty. Ads will not initialize.");
            return;
        }

        Debug.Log("AdManager: Initializing IronSource SDK...");
        LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
        LevelPlay.OnInitFailed += SdkInitializationFailedEvent;
        LevelPlay.Init(androidAppKey);
    }

    void SdkInitializationCompletedEvent(LevelPlayConfiguration config)
    {
        Debug.Log($"AdManager: LevelPlay Init Success with Config: {config}");
        
        // Initialize Rewarded Ad object
        rewardedVideoAd = new LevelPlayRewardedAd(rewardedAdUnitId);
        
        // Subscribe to Rewarded Video Events
        rewardedVideoAd.OnAdLoaded += RewardedVideoOnAdLoadedEvent;
        rewardedVideoAd.OnAdLoadFailed += RewardedVideoOnAdLoadFailedEvent;
        rewardedVideoAd.OnAdDisplayFailed += RewardedVideoOnAdDisplayFailedEvent;
        rewardedVideoAd.OnAdRewarded += RewardedVideoOnAdRewardedEvent;
        rewardedVideoAd.OnAdClosed += RewardedVideoOnAdClosedEvent;

        // Auto-load the first ad
        rewardedVideoAd.LoadAd();
    }

    void SdkInitializationFailedEvent(LevelPlayInitError error)
    {
        Debug.LogError($"AdManager: LevelPlay Init Failed: {error}");
    }

    private void OnDestroy()
    {
        if (rewardedVideoAd != null)
        {
            rewardedVideoAd.OnAdLoaded -= RewardedVideoOnAdLoadedEvent;
            rewardedVideoAd.OnAdLoadFailed -= RewardedVideoOnAdLoadFailedEvent;
            rewardedVideoAd.OnAdDisplayFailed -= RewardedVideoOnAdDisplayFailedEvent;
            rewardedVideoAd.OnAdRewarded -= RewardedVideoOnAdRewardedEvent;
            rewardedVideoAd.OnAdClosed -= RewardedVideoOnAdClosedEvent;
        }
    }


    /// <summary>
    /// Shows a rewarded ad using IronSource LevelPlay SDK.
    /// </summary>
    public void ShowRewardedAd(Action<bool> onComplete)
    {
#if UNITY_EDITOR
        Debug.LogWarning("AdManager: Showing simulate ad in editor since IronSource works best on devices.");
        StartCoroutine(SimulateAd(onComplete));
        return;
#endif

        if (rewardedVideoAd != null && rewardedVideoAd.IsAdReady())
        {
            Debug.Log("AdManager: Showing Rewarded Ad...");
            currentAdCallback = onComplete;
            rewardedVideoAd.ShowAd();
        }
        else
        {
            Debug.LogWarning("AdManager: Rewarded Ad not ready yet or no network available!");
            onComplete?.Invoke(false);
        }
    }

    // Fallback simulation for Editor testing so the developer isn't blocked!
    private IEnumerator SimulateAd(Action<bool> onComplete)
    {
        yield return new WaitForSecondsRealtime(2.5f);
        Debug.Log("AdManager: Rewarded Ad simulated successfully in Editor!");
        onComplete?.Invoke(true);
    }

    // --- IronSource Callbacks ---

    private void RewardedVideoOnAdLoadedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"AdManager: Rewarded Ad Loaded: {adInfo}");
    }

    private void RewardedVideoOnAdLoadFailedEvent(LevelPlayAdError error)
    {
        Debug.LogError($"AdManager: Rewarded Ad Load Failed: {error}");
    }

    private void RewardedVideoOnAdRewardedEvent(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        Debug.Log($"AdManager: Rewarded Ad finished successfully! Reward: {reward}");
        if (currentAdCallback != null)
        {
            Action<bool> cb = currentAdCallback;
            currentAdCallback = null;
            cb.Invoke(true);
        }
    }

    private void RewardedVideoOnAdDisplayFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError($"AdManager: Rewarded Ad Show Failed: {error}");
        
        if (currentAdCallback != null)
        {
            Action<bool> cb = currentAdCallback;
            currentAdCallback = null;
            cb.Invoke(false);
        }
        
        // Reload ad for next time
        if (rewardedVideoAd != null) rewardedVideoAd.LoadAd();
    }

    private void RewardedVideoOnAdClosedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log($"AdManager: Rewarded Ad Closed: {adInfo}");
        
        if (currentAdCallback != null)
        {
            Action<bool> cb = currentAdCallback;
            currentAdCallback = null;
            cb.Invoke(false);
        }

        // Reload ad for the next time the user wants one
        if (rewardedVideoAd != null) rewardedVideoAd.LoadAd();
    }
}
