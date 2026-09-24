using UnityEngine;

[CreateAssetMenu(menuName="Cashflow/Ad settings")]
public sealed class CashflowAdSettings : ScriptableObject
{
    public bool useTestAds=true;
    public bool enableBanners=true;
    public bool enableGameplayBanner=true,enableInterstitials=true;
    public float interstitialCooldownSeconds=90;
    public string androidGameplayBannerId="",iosGameplayBannerId="",androidInterstitialId="",iosInterstitialId="";
    public bool productionPrivacyReviewed;
    // Owner-authorized artifact preparation, separate from completed privacy review.
    public bool productionBuildAuthorized;
    public string androidRewardedId="",androidBannerId="",iosRewardedId="",iosBannerId="";
    // Do not enable production until audience and privacy declarations are reviewed.
    public bool childDirected,underAgeOfConsent;
    public string[] testDeviceIds=new string[0];
    public bool CanInitialize=>useTestAds||productionPrivacyReviewed||productionBuildAuthorized;
    public string RewardedId=>useTestAds?(Application.platform==RuntimePlatform.IPhonePlayer?
        "ca-app-pub-3940256099942544/1712485313":"ca-app-pub-3940256099942544/5224354917"):
        (Application.platform==RuntimePlatform.IPhonePlayer?iosRewardedId:androidRewardedId);
    public string BannerId=>useTestAds?(Application.platform==RuntimePlatform.IPhonePlayer?
        "ca-app-pub-3940256099942544/2934735716":"ca-app-pub-3940256099942544/6300978111"):
        (Application.platform==RuntimePlatform.IPhonePlayer?iosBannerId:androidBannerId);
    public string GameplayBannerId=>useTestAds?BannerId:(Application.platform==RuntimePlatform.IPhonePlayer?iosGameplayBannerId:androidGameplayBannerId);
    public string InterstitialId=>useTestAds?(Application.platform==RuntimePlatform.IPhonePlayer?
        "ca-app-pub-3940256099942544/4411468910":"ca-app-pub-3940256099942544/1033173712"):
        (Application.platform==RuntimePlatform.IPhonePlayer?iosInterstitialId:androidInterstitialId);
}
