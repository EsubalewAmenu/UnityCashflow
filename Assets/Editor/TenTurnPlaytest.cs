using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Manual review bridge: invokes actual UI buttons and captures their results.
[InitializeOnLoad]
public static class TenTurnPlaytest
{
    const string Root = "work/ten-turn-playtest/";
    static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static string pending;
    static double captureAt;
    static CashflowGame Game => UnityEngine.Object.FindObjectOfType<CashflowGame>();
    static object Read(string name) => typeof(CashflowGame).GetField(name, Flags).GetValue(Game);
    static object Call(string name) => typeof(CashflowGame).GetMethod(name, Flags).Invoke(Game, null);
    static TenTurnPlaytest() { EditorApplication.update += Tick; }
    static void Tick()
    {
        if(EditorApplication.isCompiling) return;
        try {
            if(File.Exists(Root + "command.txt")) {
                if(!EditorApplication.isPlaying) { EditorApplication.isPlaying = true; return; }
                if(Game == null) return;
                string[] input = File.ReadAllLines(Root + "command.txt");
                File.Delete(Root + "command.txt");
                pending = input[0];
                if(input[1] == "start") {
                    typeof(BoardReview).GetMethod("SetSize", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[]{1920,1080});
                    SceneManager.LoadScene("GameBoard");
                }
                else if(input[1] == "roll") Call("Roll");
                else if(input[1].StartsWith("click:")) {
                    string name = input[1].Substring(6);
                    var candidates = UnityEngine.Object.FindObjectsOfType<Button>().Where(b => b.isActiveAndEnabled && b.interactable && b.name == name).ToArray();
                    if(candidates.Length != 1) throw new Exception("Expected one active button: " + name + "; found " + candidates.Length);
                    candidates[0].onClick.Invoke();
                }
                captureAt = EditorApplication.timeSinceStartup + 2;
            }
            if(pending == null || Game == null || EditorApplication.timeSinceStartup < captureAt) return;
            if((bool)Read("rolling")) return;
            Canvas.ForceUpdateCanvases();
            var modal = Read("modal") as GameObject;
            string texts = string.Join("\n", (modal != null ? modal.GetComponentsInChildren<Text>() : UnityEngine.Object.FindObjectsOfType<Text>()).Select(t => t.text));
            string buttons = string.Join("\n", UnityEngine.Object.FindObjectsOfType<Button>().Where(b=>b.isActiveAndEnabled && b.interactable).Select(b=> b.name + " | " + string.Join(" ", b.GetComponentsInChildren<Text>().Select(t=>t.text))));
            string status = "Profession=" + ((CashflowGame.Profession)Read("profession")).name + "; cash="+Read("cash")+"; monthly="+((int)Call("Income")-(int)Call("Expenses"))+"; passive="+Read("passive")+"; position="+Read("position")+"; lastRoll="+Read("lastRoll")+"; skipTurns="+Read("skipTurns")+"; bankLoan="+Read("bankLoan");
            File.WriteAllText(Root + pending + ".txt", status + "\nTEXT\n" + texts + "\nBUTTONS\n" + buttons);
            ScreenCapture.CaptureScreenshot(Root + pending + ".png");
            pending = null;
        } catch(Exception e) { Directory.CreateDirectory(Root); File.WriteAllText(Root+"error.txt", e.ToString()); pending=null; }
    }
}
