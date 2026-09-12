using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Local device build. Uses Unity 2022.3's supported Java/NDK toolchain.
[InitializeOnLoad]
public static class AndroidPhoneBuild
{
    static AndroidPhoneBuild(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!File.Exists("work/run-android-build")||EditorApplication.isCompiling||BuildPipeline.isBuildingPlayer)return;
        if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;return;}
        File.Delete("work/run-android-build");Build();
    }
    [MenuItem("Cashflow/Build Android Phone APK")]
    public static void Build()
    {
        Directory.CreateDirectory("work/android-build");
        try
        {
            string sdk=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"UnityAndroid2022");
            string ndk=Path.Combine(sdk,"ndk","23.1.7779620");
            if(!Directory.Exists(ndk)||!Directory.Exists(Path.Combine(sdk,"cmdline-tools","6.0")))throw new Exception("Install SDK command-line tools 6.0 and NDK 23.1.7779620 first.");
            AndroidExternalToolsSettings.sdkRootPath=sdk;
            AndroidExternalToolsSettings.ndkRootPath=ndk;
            AndroidExternalToolsSettings.jdkRootPath=Path.Combine(sdk,"OpenJDK");
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion=(AndroidSdkVersions)24;
            PlayerSettings.Android.targetSdkVersion=(AndroidSdkVersions)36;
            EditorUserBuildSettings.buildAppBundle=false;
            AssetDatabase.SaveAssets();
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
                locationPathName="work/android-build/UnityCashflow.apk",target=BuildTarget.Android,options=BuildOptions.Development
            });
            File.WriteAllText("work/android-build/result.txt",report.summary.result+"; errors="+report.summary.totalErrors+"; duration="+report.summary.totalTime);
        }
        catch(Exception error){File.WriteAllText("work/android-build/result.txt",error.ToString());Debug.LogException(error);}
    }
}
