using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

public sealed class AdMobProvider : ICashflowAdProvider
{
    CashflowAdSettings settings;
    RewardedAd reward;
    InterstitialAd interstitial;
    bool interstitialLoading,gameplayBanner;
    float interstitialRetryAt,interstitialLoadedAt;
    int interstitialFailures;
    public bool InterstitialReady=>!disposed&&!privacyOpen&&adsAllowed&&interstitial!=null&&
        Time.realtimeSinceStartup-interstitialLoadedAt<3300&&interstitial.CanShowAd();
    BannerView banner;
    bool initialized,initializing,disposed,loading,bannerWanted,bannerLoaded,adsAllowed,privacyOpen;
    float retryAt,loadedAt,bannerRetryAt;
    int failures,bannerFailures,generation;
    Rect bannerSafeArea;
    Vector2 screen;
    public bool RewardReady=>!disposed&&!privacyOpen&&adsAllowed&&reward!=null&&
        Time.realtimeSinceStartup-loadedAt<3300&&reward.CanShowAd();
    public bool PrivacyRequired=>settings!=null&&!settings.useTestAds&&ConsentInformation.PrivacyOptionsRequirementStatus==PrivacyOptionsRequirementStatus.Required;
    public float BannerHeight=>bannerWanted&&bannerLoaded&&banner!=null?banner.GetHeightInPixels()+16:0;
    void Main(Action action){MobileAdsEventExecutor.ExecuteInUpdate(()=>{if(!disposed)action();});}
    public void Start(CashflowAdSettings config)
    {
        settings=config;if(!config.CanInitialize)return;
        MobileAds.SetRequestConfiguration(new RequestConfiguration {
            TestDeviceIds=new List<string>(config.testDeviceIds),
            TagForChildDirectedTreatment=config.childDirected?TagForChildDirectedTreatment.True:TagForChildDirectedTreatment.Unspecified,
            TagForUnderAgeOfConsent=config.underAgeOfConsent?TagForUnderAgeOfConsent.True:TagForUnderAgeOfConsent.Unspecified,
            MaxAdContentRating=MaxAdContentRating.G
        });
        // Google's sample App IDs have no publisher UMP message configuration.
        // Sample ads may initialize directly; production always goes through UMP.
        if(config.useTestAds){adsAllowed=true;InitializeIfAllowed();return;}
        ConsentInformation.Update(new ConsentRequestParameters{TagForUnderAgeOfConsent=config.underAgeOfConsent},error=>Main(()=>{
            if(error!=null){Debug.LogWarning("Ad privacy update unavailable; normal play continues.");UpdateConsent();return;}
            ConsentForm.LoadAndShowConsentFormIfRequired(formError=>Main(UpdateConsent));
        }));
    }
    void UpdateConsent(){adsAllowed=ConsentInformation.CanRequestAds();InitializeIfAllowed();}
    void InitializeIfAllowed()
    {
        if(initialized||initializing||!adsAllowed)return;
        initializing=true;
        MobileAds.Initialize(status=>Main(()=>{initialized=true;initializing=false;LoadReward();SetBanner(bannerWanted,gameplayBanner);}));
    }
    public void Tick()
    {
        if(!initialized||disposed||privacyOpen||!adsAllowed)return;
        if(reward!=null&&Time.realtimeSinceStartup-loadedAt>=3300){reward.Destroy();reward=null;}
        if(!loading&&reward==null&&Time.realtimeSinceStartup>=retryAt)LoadReward();
        if(interstitial!=null&&Time.realtimeSinceStartup-interstitialLoadedAt>=3300){interstitial.Destroy();interstitial=null;}
        if(settings.enableInterstitials&&!interstitialLoading&&interstitial==null&&Time.realtimeSinceStartup>=interstitialRetryAt)LoadInterstitial();
        if(bannerWanted&&(screen!=new Vector2(Screen.width,Screen.height)||bannerSafeArea!=Screen.safeArea))DestroyBanner();
        if(bannerWanted&&banner==null&&Time.realtimeSinceStartup>=bannerRetryAt)CreateBanner();
    }
    void LoadReward()
    {
        if(loading||privacyOpen||!adsAllowed||string.IsNullOrWhiteSpace(settings.RewardedId))return;
        loading=true;
        int requestGeneration=generation;
        RewardedAd.Load(settings.RewardedId,new AdRequest(),(ad,error)=>{
            MobileAdsEventExecutor.ExecuteInUpdate(()=>{
                if(disposed||requestGeneration!=generation){ad?.Destroy();return;}
                loading=false;
                if(error!=null||ad==null){ad?.Destroy();retryAt=Time.realtimeSinceStartup+Backoff(++failures);return;}
                reward=ad;loadedAt=Time.realtimeSinceStartup;failures=0;
            });
        });
    }
    static float Backoff(int failures)=>Mathf.Min(120,15*Mathf.Pow(2,Mathf.Min(failures-1,3)));
    public void ShowReward(Action earned,Action closed)
    {
        if(!RewardReady){closed();return;}
        var ad=reward;reward=null;bool finished=false;
        Action finish=()=>{
            if(finished)return;finished=true;ad.Destroy();retryAt=Time.realtimeSinceStartup+2;closed();
        };
        ad.OnAdFullScreenContentClosed+=()=>Main(finish);
        ad.OnAdFullScreenContentFailed+=error=>Main(finish);
        try{ad.Show(value=>Main(()=>{if(!finished)earned();}));}
        catch{finish();throw;}
    }
    void LoadInterstitial()
    {
        if(string.IsNullOrWhiteSpace(settings.InterstitialId))return;
        interstitialLoading=true;int requestGeneration=generation;
        InterstitialAd.Load(settings.InterstitialId,new AdRequest(),(ad,error)=>MobileAdsEventExecutor.ExecuteInUpdate(()=>{
            if(disposed||requestGeneration!=generation){ad?.Destroy();return;}
            interstitialLoading=false;
            if(error!=null||ad==null){ad?.Destroy();interstitialRetryAt=Time.realtimeSinceStartup+Backoff(++interstitialFailures);return;}
            interstitial=ad;interstitialLoadedAt=Time.realtimeSinceStartup;interstitialFailures=0;
        }));
    }
    public void ShowInterstitial(Action closed)
    {
        if(!InterstitialReady){closed();return;}
        var ad=interstitial;interstitial=null;bool finished=false;
        Action finish=()=>{if(finished)return;finished=true;ad.Destroy();interstitialRetryAt=Time.realtimeSinceStartup+2;closed();};
        ad.OnAdFullScreenContentClosed+=()=>Main(finish);
        ad.OnAdFullScreenContentFailed+=error=>Main(finish);
        try{ad.Show();}catch{finish();throw;}
    }
    public void SetBanner(bool visible,bool gameplay=false)
    {
        if(gameplayBanner!=gameplay){DestroyBanner();gameplayBanner=gameplay;bannerRetryAt=0;}
        bannerWanted=visible;
        if(!visible){banner?.Hide();return;}
        if(!initialized||privacyOpen||!adsAllowed)return;
        if(banner==null&&Time.realtimeSinceStartup>=bannerRetryAt)CreateBanner();
        else if(bannerLoaded)banner?.Show();
    }
    void CreateBanner()
    {
        string id=gameplayBanner?settings.GameplayBannerId:settings.BannerId;
        if(string.IsNullOrWhiteSpace(id))return;
        screen=new Vector2(Screen.width,Screen.height);bannerSafeArea=Screen.safeArea;
        var size=AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(MobileAds.Utils.GetDeviceSafeWidth());
        var view=new BannerView(id,size,gameplayBanner?AdPosition.Top:AdPosition.Bottom);banner=view;view.Hide();
        view.OnBannerAdLoaded+=()=>Main(()=>{
            if(banner!=view)return;bannerLoaded=true;bannerFailures=0;
            if(bannerWanted)view.Show();else view.Hide();
        });
        view.OnBannerAdLoadFailed+=error=>Main(()=>{
            if(banner!=view)return;DestroyBanner();bannerRetryAt=Time.realtimeSinceStartup+Backoff(++bannerFailures);
        });
        view.LoadAd(new AdRequest());
    }
    void DestroyBanner(){banner?.Destroy();banner=null;bannerLoaded=false;}
    public void ShowPrivacy(Action closed)
    {
        // Retire cached ads when privacy choices change; reload under the new state.
        privacyOpen=true;generation++;loading=false;DestroyBanner();reward?.Destroy();reward=null;
        interstitialLoading=false;interstitial?.Destroy();interstitial=null;
        ConsentForm.ShowPrivacyOptionsForm(error=>Main(()=>{
            if(error!=null)Debug.LogWarning("Ad privacy options could not be displayed. Please try again.");
            privacyOpen=false;UpdateConsent();closed();
        }));
    }
    public void Dispose(){disposed=true;reward?.Destroy();reward=null;interstitial?.Destroy();interstitial=null;DestroyBanner();}
}
