using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Runs the existing isolated-save and ad-lifecycle suites after an engine update.
[InitializeOnLoad]
public static class UnitySecurityReview
{
    const string Key="Cashflow.SecurityReview.";
    static int stage { get=>SessionState.GetInt(Key+"stage",0); set=>SessionState.SetInt(Key+"stage",value); }
    static string result { get=>SessionState.GetString(Key+"result",""); set=>SessionState.SetString(Key+"result",value); }
    static DateTime previousResult { get=>new DateTime(long.Parse(SessionState.GetString(Key+"previous","0")),DateTimeKind.Utc); set=>SessionState.SetString(Key+"previous",value.Ticks.ToString()); }
    static UnitySecurityReview(){EditorApplication.update+=CheckTrigger;if(SessionState.GetBool(Key+"active",false))EditorApplication.update+=Tick;}
    static void CheckTrigger(){if(!File.Exists("work/run-security-review")||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("work/run-security-review");Run();}
    public static void Run()
    {
        if(Application.unityVersion!=UnitySecurityBuildGuard.ValidatedEditor)
            throw new Exception("Security review requires the patched editor.");
        EditorSceneManager.OpenScene("Assets/Scenes/ProfessionSelection.unity");
        stage=0;SessionState.SetString(Key+"deadline",DateTime.UtcNow.AddMinutes(5).Ticks.ToString());
        SessionState.SetBool(Key+"active",true);
        Begin("work/save-review/results.txt","work/run-save-review");
        EditorApplication.update+=Tick;
    }
    static void Begin(string output,string trigger)
    {
        result=output;previousResult=File.Exists(output)?File.GetLastWriteTimeUtc(output):DateTime.MinValue;
        File.WriteAllText(trigger,"");
    }
    static void Tick()
    {
        if(DateTime.UtcNow.Ticks>long.Parse(SessionState.GetString(Key+"deadline","0"))){Finish(false,"Timed out waiting for regression suites.");return;}
        if(stage==1)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            stage=2;Begin("work/admob-review/results.txt","work/run-admob-review");return;
        }
        if(!File.Exists(result)||File.GetLastWriteTimeUtc(result)<=previousResult)return;
        string outcome=File.ReadAllText(result);
        if(!outcome.StartsWith("PASS:")){Finish(false,outcome);return;}
        if(stage==0){stage=1;EditorApplication.isPlaying=false;return;}
        Finish(true,"Save/resume and ad lifecycle/layout regression suites passed on "+Application.unityVersion+".");
    }
    static void Finish(bool passed,string message)
    {
        EditorApplication.update-=Tick;
        SessionState.SetBool(Key+"active",false);
        Directory.CreateDirectory("work/unity-security");
        File.WriteAllText("work/unity-security/regression-result.txt",DateTime.UtcNow.ToString("O")+" "+(passed?"PASS: ":"FAIL: ")+message);
        if(Application.isBatchMode)EditorApplication.Exit(passed?0:1);
        else EditorApplication.isPlaying=false;
    }
}
