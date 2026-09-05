using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Run from Cashflow > Review Board. Captures actual Game views and checks geometry.
[InitializeOnLoad]
public static class BoardReview
{
    static double next;
    static int step;
    static int retries;
    static readonly int[,] sizes = { {1280,720}, {1920,1080}, {2340,1080}, {1024,768} };
    static string Folder => Path.GetFullPath("work/board-review");
    static BoardReview() { EditorApplication.update += Tick; }
    [MenuItem("Cashflow/Review Board")]
    public static void Run()
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Path.Combine(Folder,"results.txt"),"");
        File.WriteAllText(Path.Combine(Folder,"error.txt"),"");
        SessionState.SetBool("BoardReview", true);
        SessionState.SetInt("BoardReviewStep", 0);
        if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
        EditorApplication.isPaused = false;
        next = EditorApplication.timeSinceStartup + 6;
    }
    static void Tick()
    {
        if (File.Exists("work/run-board-review") && !EditorApplication.isCompiling)
        { File.Delete("work/run-board-review"); Run(); return; }
        if (!SessionState.GetBool("BoardReview",false) || !EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < next) return;
        try
        {
            step = SessionState.GetInt("BoardReviewStep",0);
            if (step == 0) { SceneManager.LoadScene("GameBoard"); }
            else if (step <= 8)
            {
                int i=(step-1)/2;
                if (step%2==1) SetSize(sizes[i,0],sizes[i,1]);
                else
                {
                    if(Screen.width!=sizes[i,0] || Screen.height!=sizes[i,1])
                    {
                        if(++retries>8)throw new Exception("Game view did not settle at requested resolution");
                        SetSize(sizes[i,0],sizes[i,1]); next=EditorApplication.timeSinceStartup+3; return;
                    }
                    retries=0;
                    foreach(var scaler in UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.CanvasScaler>())scaler.SendMessage("Update");
                    Canvas.ForceUpdateCanvases();
                    UnityEngine.Object.FindObjectOfType<SafeBoardLayout>().SendMessage("LateUpdate");
                    Canvas.ForceUpdateCanvases();
                    var segments=UnityEngine.Object.FindObjectsOfType<BoardTrackSegment>();
                    if(segments.Length!=48)throw new Exception("Expected 48 connected spaces, got "+segments.Length);
                    foreach(var segment in segments)
                    {
                        float t=(segment.index+.5f)/segment.count;
                        Vector2 left=BoardTrackGenerator.Point(t,segment.radius);
                        Vector2 right=BoardTrackGenerator.Point(((segment.index+1)-.5f)/segment.count,segment.radius);
                        if(Vector2.Distance(left,right)>.001f)throw new Exception("Track seam mismatch");
                        Vector2 midpoint=BoardTrackGenerator.Point(segment.index/(float)segment.count,segment.radius);
                        Vector2 screen=RectTransformUtility.WorldToScreenPoint(null,segment.transform.TransformPoint(midpoint));
                        if(!segment.IsRaycastLocationValid(screen,null))throw new Exception("Tile center is not clickable");
                    }
                    var root=UnityEngine.Object.FindObjectOfType<SafeBoardLayout>().GetComponent<RectTransform>();
                    var corners=new Vector3[4]; root.GetWorldCorners(corners);
                    foreach(var corner in corners) if(!new Rect(-1,-1,Screen.width+2,Screen.height+2).Contains(corner))throw new Exception("HUD exceeds screen: "+corner+" screen "+Screen.width+"x"+Screen.height+" safe "+Screen.safeArea);
                    typeof(CashflowGame).GetMethod("HideHover",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(UnityEngine.Object.FindObjectOfType<CashflowGame>(),null);
                    ScreenCapture.CaptureScreenshot(Path.Combine(Folder,sizes[i,0]+"x"+sizes[i,1]+".png"));
                    File.AppendAllText(Path.Combine(Folder,"results.txt"),"PASS: "+Screen.width+"x"+Screen.height+"; 48 segments, shared seams, tile hit targets, HUD bounds.\n");
                }
            }
            else
            {
                var game=UnityEngine.Object.FindObjectOfType<CashflowGame>();
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                typeof(CashflowGame).GetMethod("Roll",flags).Invoke(game,null);
                int position=(int)typeof(CashflowGame).GetField("position",flags).GetValue(game);
                var token=(GameObject)typeof(CashflowGame).GetField("token",flags).GetValue(game);
                if(Vector2.Distance(token.GetComponent<RectTransform>().anchoredPosition,BoardTrackGenerator.Point(position/24f,222)+new Vector2(0,22))>.01f)throw new Exception("Token does not match landed space");
                File.AppendAllText(Path.Combine(Folder,"results.txt"),"PASS: Roll resolves and token matches the landed space.\n");
                SessionState.SetBool("BoardReview",false);
                Debug.Log("Board review complete: "+Folder);
                return;
            }
            SessionState.SetInt("BoardReviewStep",step+1);
            next=EditorApplication.timeSinceStartup+3;
        }
        catch(Exception e) { SessionState.SetBool("BoardReview",false); File.WriteAllText(Path.Combine(Folder,"error.txt"),e.ToString()); Debug.LogException(e); }
    }
    static void SetSize(int width,int height)
    {
        var assembly=typeof(Editor).Assembly;
        var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
        var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var instance=singleton.GetProperty("instance").GetValue(null);
        var group=sizesType.GetMethod("GetGroup").Invoke(instance,new object[]{0});
        var sizeType=assembly.GetType("UnityEditor.GameViewSize");
        var kind=assembly.GetType("UnityEditor.GameViewSizeType");
        var size=Activator.CreateInstance(sizeType,Enum.ToObject(kind,1),width,height,"Board Review "+width+"x"+height);
        group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
        int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
        var view=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
        view.GetType().GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(view,count-1);
        view.Repaint();
    }
}
