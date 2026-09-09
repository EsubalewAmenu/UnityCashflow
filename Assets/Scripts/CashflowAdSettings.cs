using UnityEngine;

[CreateAssetMenu(menuName="Cashflow/Ad settings")]
public sealed class CashflowAdSettings : ScriptableObject
{
    public bool useTestAds=true;
    public bool enableBanners=true;
    public bool productionPrivacyReviewed;
    public string androidRewardedId="",androidBannerId="",iosRewardedId="",iosBannerId="";
    // Do not enable production until audience and privacy declarations are reviewed.
    public bool childDirected,underAgeOfConsent;
    public string[] testDeviceIds=new string[0];
    public bool CanInitialize=>useTestAds||productionPrivacyReviewed;
    public string RewardedId=>useTestAds?(Application.platform==RuntimePlatform.IPhonePlayer?
        "ca-app-pub-3940256099942544/1712485313":"ca-app-pub-3940256099942544/5224354917"):
        (Application.platform==RuntimePlatform.IPhonePlayer?iosRewardedId:androidRewardedId);
    public string BannerId=>useTestAds?(Application.platform==RuntimePlatform.IPhonePlayer?
        "ca-app-pub-3940256099942544/2934735716":"ca-app-pub-3940256099942544/6300978111"):
        (Application.platform==RuntimePlatform.IPhonePlayer?iosBannerId:androidBannerId);
}
