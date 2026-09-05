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
    static readonly int[,] sizes = { {1280,720}, {1920,1080}, {2532,1170}, {2340,1080}, {1024,768} };
    static string Folder => Path.GetFullPath("work/3d-review");
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
            else if (step <= sizes.GetLength(0)*2)
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
                    var camera=UnityEngine.Object.FindObjectOfType<BoardWorld>().View;
                    if(camera.rect.x!=0||camera.rect.width!=1)throw new Exception("Camera does not fill screen width");
                    for(int sample=0;sample<96;sample++)
                    {
                        var edge=BoardTrackGenerator.Point(sample/96f,267);
                        var projected=camera.WorldToViewportPoint(new Vector3(edge.x*.01f,.16f,edge.y*.01f));
                        if(projected.z<=0||projected.x<0||projected.x>1||projected.y<0||projected.y>1)throw new Exception("Board clipped at "+Screen.width+"x"+Screen.height);
                    }

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
                        Vector2 screen=RectTransformUtility.WorldToScreenPoint(Camera.main,segment.transform.TransformPoint(midpoint));
                        if(!segment.IsRaycastLocationValid(screen,Camera.main))throw new Exception("Tile center is not clickable");
                    }
                    var root=UnityEngine.Object.FindObjectOfType<SafeBoardLayout>().GetComponent<RectTransform>();
                    var corners=new Vector3[4]; root.GetWorldCorners(corners);
                    foreach(var corner in corners) if(!new Rect(-1,-1,Screen.width+2,Screen.height+2).Contains(corner))throw new Exception("HUD exceeds screen: "+corner+" screen "+Screen.width+"x"+Screen.height+" safe "+Screen.safeArea);
                    typeof(CashflowGame).GetMethod("HideHover",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(UnityEngine.Object.FindObjectOfType<CashflowGame>(),null);
                    ScreenCapture.CaptureScreenshot(Path.Combine(Folder,sizes[i,0]+"x"+sizes[i,1]+".png"));
                    File.AppendAllText(Path.Combine(Folder,"results.txt"),"PASS: "+Screen.width+"x"+Screen.height+"; 48 segments, shared seams, tile hit targets, HUD bounds.\n");
                }
            }
            else if(step==sizes.GetLength(0)*2+1)
            {
                SetSize(1920,1080);UnityEngine.Object.FindObjectOfType<BoardWorld>().SetOverview(false);
            }
            else if(step==sizes.GetLength(0)*2+2)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(Folder,"follow-camera.png"));
                var game=UnityEngine.Object.FindObjectOfType<CashflowGame>();
                typeof(CashflowGame).GetMethod("Roll",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);
            }
            else
            {
                var game=UnityEngine.Object.FindObjectOfType<CashflowGame>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                if((bool)typeof(CashflowGame).GetField("rolling",flags).GetValue(game)){if(++retries>12)throw new Exception("Roll did not finish");next=EditorApplication.timeSinceStartup+2;return;}
                int position=(int)typeof(CashflowGame).GetField("position",flags).GetValue(game);
                var world=UnityEngine.Object.FindObjectOfType<BoardWorld>();
                if(world.DiceTotal<(step==sizes.GetLength(0)*2+3?1:2)||world.DiceTotal>(step==sizes.GetLength(0)*2+3?6:12))throw new Exception("Dice result invalid");
                if(GameObject.Find("Live minimap")!=null)throw new Exception("Minimap was not removed");
                int sum=0;foreach(var die in UnityEngine.Object.FindObjectsOfType<BoardDie>())sum+=die.Value;
                if(sum!=world.DiceTotal)throw new Exception("Visible dice do not match gameplay total");
                if(Vector3.Distance(world.Pawn.position,BoardWorld.TrackPoint(position,false))>.01f)throw new Exception("3D pawn does not match landed space");
                if(step==sizes.GetLength(0)*2+3)
                {
                    File.AppendAllText(Path.Combine(Folder,"results.txt"),"PASS: single physical die, animated pawn landing, follow camera; minimap removed.\n");
                    GameObject.Find("Board Settings Button").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                    var settings=GameObject.Find("Modal Blocker");
                    foreach(string mode in new[]{"OVERVIEW","FOLLOW"})
                    {
                        var choice=settings.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.GetComponentInChildren<UnityEngine.UI.Text>().text.StartsWith(mode));
                        choice.onClick.Invoke();
                        if(world.Follow!=(mode=="FOLLOW"))throw new Exception("Settings camera selection failed");
                    }
                    File.AppendAllText(Path.Combine(Folder,"results.txt"),"PASS: bottom-bar settings opens; Overview and Follow selections work.\n");
                    typeof(CashflowGame).GetMethod("Roll",flags).Invoke(game,null);
                    if((bool)typeof(CashflowGame).GetField("rolling",flags).GetValue(game))throw new Exception("Modal did not block rolling");
                    typeof(CashflowGame).GetMethod("CloseModal",flags).Invoke(game,null);
                    typeof(CashflowGame).GetField("charity",flags).SetValue(game,true);
                    typeof(CashflowGame).GetField("skipTurns",flags).SetValue(game,0);
                    SessionState.SetInt("ReviewOldPosition",position);
                    typeof(CashflowGame).GetMethod("Roll",flags).Invoke(game,null);
                    retries=0;
                }
                else
                {
                    if(position!=(SessionState.GetInt("ReviewOldPosition",0)+world.DiceTotal)%24)throw new Exception("Two-dice movement incorrect");
                    File.AppendAllText(Path.Combine(Folder,"results.txt"),"PASS: two physical dice match movement; settings modal blocks extra rolls.\n");
                    ScreenCapture.CaptureScreenshot(Path.Combine(Folder,"landed-event.png"));
                    SessionState.SetBool("BoardReview",false);Debug.Log("3D board review complete: "+Folder);return;
                }
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
        view.Focus();view.Repaint();
    }
}
