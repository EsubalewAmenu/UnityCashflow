using System;
using UnityEngine;

public interface ICashflowAdProvider : IDisposable
{
    bool RewardReady { get; }
    bool InterstitialReady { get; }
    bool PrivacyRequired { get; }
    float BannerHeight { get; }
    void Start(CashflowAdSettings settings);
    void Tick();
    void SetBanner(bool visible,bool gameplay=false);
    void ShowInterstitial(Action closed);
    void ShowReward(Action earned,Action closed);
    void ShowPrivacy(Action closed);
}

// A single, scene-independent owner prevents duplicate initialization and stacked ads.
public sealed class CashflowAds : MonoBehaviour
{
    public static CashflowAds Instance { get; private set; }
    public static CashflowAds Ensure()
    {
        if(Instance==null)new GameObject("Cashflow Ads").AddComponent<CashflowAds>();
        return Instance;
    }
    ICashflowAdProvider provider;
    bool bannerWanted,gameplayBanner,pausedAudio;
    float lastFullScreenEnded=-9999;
    float previousTimeScale;
    public bool FullScreen { get; private set; }
    public bool RewardReady=>!FullScreen&&provider!=null&&provider.RewardReady;
    public bool PrivacyRequired=>provider!=null&&provider.PrivacyRequired;
    public float ReservedBottomPixels=>!gameplayBanner&&bannerWanted&&provider!=null?provider.BannerHeight:0;
    public float ReservedTopPixels=>gameplayBanner&&bannerWanted&&provider!=null?provider.BannerHeight:0;
    public CashflowAdSettings Settings { get; private set; }
    void Awake()
    {
        if(Instance!=null){Destroy(gameObject);return;}
        Instance=this;DontDestroyOnLoad(gameObject);
        Settings=Resources.Load<CashflowAdSettings>("CashflowAdSettings");
        if(Settings==null)Settings=ScriptableObject.CreateInstance<CashflowAdSettings>();
        provider=new AdMobProvider();provider.Start(Settings);
    }
    void Update(){provider?.Tick();}
    public void SetBannerPlacement(bool visible,bool gameplay=false)
    {
        gameplayBanner=gameplay;
        bannerWanted=visible&&Settings.enableBanners&&(!gameplay||Settings.enableGameplayBanner);
        provider?.SetBanner(bannerWanted&&!FullScreen,gameplayBanner);
    }
    // Never wait for loading, and never show a late ad after gameplay has resumed.
    public bool TryInterstitial(Action closed)
    {
        if(FullScreen||provider==null||!Settings.enableInterstitials||!provider.InterstitialReady||
            Time.realtimeSinceStartup-lastFullScreenEnded<Mathf.Max(0,Settings.interstitialCooldownSeconds))return false;
        BeginFullScreen();bool finished=false;
        Action finish=()=>{if(finished)return;finished=true;EndFullScreen();closed?.Invoke();};
        try{provider.ShowInterstitial(finish);}catch(Exception e){Debug.LogWarning("Ad break unavailable: "+e.Message);finish();}
        return true;
    }
    public bool TryReward(Action earned,Action closed)
    {
        if(!RewardReady)return false;
        BeginFullScreen();bool rewarded=false,finished=false;
        Action finish=()=>{if(finished)return;finished=true;EndFullScreen();closed?.Invoke();};
        try{provider.ShowReward(()=>{if(rewarded||finished)return;rewarded=true;earned?.Invoke();},finish);}
        catch(Exception e){Debug.LogWarning("Rewarded ad could not open: "+e.Message);finish();}
        return true;
    }
    public void ShowPrivacy(Action closed)
    {
        if(FullScreen||!PrivacyRequired)return;
        BeginFullScreen();
        provider.ShowPrivacy(()=>{EndFullScreen();closed?.Invoke();});
    }
    void BeginFullScreen()
    {
        FullScreen=true;provider.SetBanner(false,gameplayBanner);
        pausedAudio=AudioListener.pause;previousTimeScale=Time.timeScale;
        AudioListener.pause=true;Time.timeScale=0;
    }
    void EndFullScreen()
    {
        if(!FullScreen)return;
        FullScreen=false;lastFullScreenEnded=Time.realtimeSinceStartup;AudioListener.pause=pausedAudio;Time.timeScale=previousTimeScale;
        provider?.SetBanner(bannerWanted,gameplayBanner);
    }
    void OnDestroy()
    {
        if(Instance!=this)return;
        EndFullScreen();provider?.Dispose();Instance=null;
    }
#if UNITY_EDITOR
    // Deterministic lifecycle tests never request or click live ads.
    public void UseReviewProvider(ICashflowAdProvider replacement)
    {
        EndFullScreen();provider.Dispose();provider=replacement;provider.Start(Settings);
        provider.SetBanner(bannerWanted,gameplayBanner);
    }
#endif
}
