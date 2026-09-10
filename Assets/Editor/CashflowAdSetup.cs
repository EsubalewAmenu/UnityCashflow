using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

[InitializeOnLoad]
public sealed class CashflowAdSetup : IPreprocessBuildWithReport
{
    static CashflowAdSetup(){EditorApplication.delayCall+=EnsureSettings;}
    static void EnsureSettings()
    {
        const string path="Assets/Resources/CashflowAdSettings.asset";
        if(AssetDatabase.LoadAssetAtPath<CashflowAdSettings>(path)!=null)return;
        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<CashflowAdSettings>(),path);
        AssetDatabase.SaveAssets();
    }
    public int callbackOrder=>-1000;
    public void OnPreprocessBuild(BuildReport report)
    {
        EnsureSettings();
        if(report.summary.platform!=BuildTarget.Android&&report.summary.platform!=BuildTarget.iOS)return;
        // The Unity 11.5.0 Android bridge declares API 24 in its AAR manifest.
        if(report.summary.platform==BuildTarget.Android&&PlayerSettings.Android.minSdkVersion<(AndroidSdkVersions)24)
            PlayerSettings.Android.minSdkVersion=(AndroidSdkVersions)24;
        var settings=Resources.Load<CashflowAdSettings>("CashflowAdSettings");
        if((report.summary.options&BuildOptions.Development)!=0)return;
        // Internal Play tests need a non-debuggable bundle while still using sample ads.
        if(AndroidInternalBuild.InProgress && report.summary.platform==BuildTarget.Android && settings.useTestAds)return;
        if(settings.useTestAds||!settings.productionPrivacyReviewed)
            throw new BuildFailedException("AdMob is in test mode. Use a Development Build for testing. Before a store build, configure your real IDs and complete the audience/privacy review in CashflowAdSettings.");
        string unit=report.summary.platform==BuildTarget.Android?settings.androidRewardedId:settings.iosRewardedId;
        string banner=report.summary.platform==BuildTarget.Android?settings.androidBannerId:settings.iosBannerId;
        string native=File.ReadAllText("Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset");
        string appField=report.summary.platform==BuildTarget.Android?"adMobAndroidAppId":"adMobIOSAppId";
        string appId=Regex.Match(native,@"(?m)^\s*"+appField+@":\s*(ca-app-pub-[0-9]+~[0-9]+)").Groups[1].Value;
        if(!ValidProductionId(unit,"/")||(settings.enableBanners&&!ValidProductionId(banner,"/"))||!ValidProductionId(appId,"~"))
            throw new BuildFailedException("Production AdMob app/rewarded/banner IDs are incomplete or still use Google sample IDs.");
    }
    static bool ValidProductionId(string id,string separator)=>!string.IsNullOrEmpty(id)&&
        Regex.IsMatch(id,@"^ca-app-pub-[0-9]{16}"+separator+@"[0-9]{10}$")&&!id.Contains("3940256099942544");
}
