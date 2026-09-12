using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build.Reporting;
using UnityEngine;

[InitializeOnLoad]
// Signed internal-track bundles keep sample ads and target the current Play requirement.
public static class AndroidInternalBuild
{
    public static bool InProgress { get; private set; }
    static AndroidInternalBuild(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!File.Exists("work/run-internal-build")||EditorApplication.isCompiling||BuildPipeline.isBuildingPlayer)return;
        if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;return;}
        File.Delete("work/run-internal-build");Build();
    }
    [MenuItem("Cashflow/Build Internal Test Bundle (Test Ads)")]
    public static void Build()=>BuildBundle(false);
    [MenuItem("Cashflow/Build Production Bundle (Live Ads)")]
    public static void BuildProduction()=>BuildBundle(true);
    static void BuildBundle(bool production)
    {
        string root=production?"work/production-v4/":"work/internal-testing/";
        Directory.CreateDirectory(root);
        bool oldCustom=PlayerSettings.Android.useCustomKeystore,oldBundle=EditorUserBuildSettings.buildAppBundle;
        string oldKey=PlayerSettings.Android.keystoreName,oldAlias=PlayerSettings.Android.keyaliasName;
        string oldPass=PlayerSettings.Android.keystorePass,oldAliasPass=PlayerSettings.Android.keyaliasPass;
        try
        {
            var ads=Resources.Load<CashflowAdSettings>("CashflowAdSettings");
            if(ads==null||ads.useTestAds==production)throw new Exception(production?"Production bundle requires live ads.":"Internal test bundle requires sample ads.");
            string password=File.ReadAllText("work/signing/upload-password.txt").Trim();
            PlayerSettings.Android.useCustomKeystore=true;
            PlayerSettings.Android.keystoreName=Path.GetFullPath("work/signing/cashflow-upload.jks");
            PlayerSettings.Android.keyaliasName="cashflow-upload";
            PlayerSettings.Android.keystorePass=password;PlayerSettings.Android.keyaliasPass=password;
            string sdk=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"UnityAndroid2022");
            AndroidExternalToolsSettings.sdkRootPath=sdk;
            AndroidExternalToolsSettings.ndkRootPath=Path.Combine(sdk,"ndk","23.1.7779620");
            AndroidExternalToolsSettings.jdkRootPath=Path.Combine(sdk,"OpenJDK");
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion=(AndroidSdkVersions)24;
            PlayerSettings.Android.targetSdkVersion=(AndroidSdkVersions)36;
            PlayerSettings.Android.bundleVersionCode=Math.Max(production?4:3,PlayerSettings.Android.bundleVersionCode);
            EditorUserBuildSettings.buildAppBundle=true;
            EditorUserBuildSettings.androidCreateSymbols=AndroidCreateSymbols.Public;
            InProgress=!production;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
                locationPathName=root+"CashflowKingdom.aab",target=BuildTarget.Android,options=BuildOptions.None
            });
            File.WriteAllText(root+"result.txt",DateTime.Now+" "+report.summary.result+"; errors="+report.summary.totalErrors+"; duration="+report.summary.totalTime);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Android bundle build failed: "+report.summary.result);
        }
        catch(Exception error){File.WriteAllText(root+"result.txt",error.ToString());Debug.LogException(error);throw;}
        finally
        {
            InProgress=false;EditorUserBuildSettings.buildAppBundle=oldBundle;
            PlayerSettings.Android.useCustomKeystore=oldCustom;PlayerSettings.Android.keystoreName=oldKey;
            PlayerSettings.Android.keyaliasName=oldAlias;PlayerSettings.Android.keystorePass=oldPass;PlayerSettings.Android.keyaliasPass=oldAliasPass;
            AssetDatabase.SaveAssets();
        }
    }
}
