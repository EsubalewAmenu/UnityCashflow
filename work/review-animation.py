from pathlib import Path
p=Path('Assets/Editor/DealReview.cs');s=p.read_text();s=s.replace('    static int step;','    static int step,frames;\n    static double captureNext;').replace('step=0;running=true;','step=0;frames=0;running=true;').replace('        if(!running||EditorApplication.isCompiling', '''        if(running&&step==3&&EditorApplication.timeSinceStartup>=captureNext&&frames<12)
        {
            ScreenCapture.CaptureScreenshot(Folder+"/reveal-"+(frames++)+".png");captureNext=EditorApplication.timeSinceStartup+.07;
        }
        if(!running||EditorApplication.isCompiling''').replace('!UnityEngine.Object.FindObjectOfType<BoardWorld>().InputBlocked==false','UnityEngine.Object.FindObjectOfType<BoardWorld>().InputBlocked');p.write_text(s)
