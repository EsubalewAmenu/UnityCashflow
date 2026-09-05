using UnityEngine;
using UnityEngine.EventSystems;

public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StartCashflow()
    {
        // The UI uses Screen Space Overlay, but Unity's Game view still expects
        // an enabled camera in an otherwise empty scene.
        if (Camera.main == null)
        {
            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.043f, 0.071f, 0.125f);
            camera.orthographic = true;
            cameraObject.tag = "MainCamera";
        }

        // Canvas buttons require one EventSystem and input module to receive
        // mouse/touch clicks in the Game view.
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Object.DontDestroyOnLoad(eventSystem);
        }

        if (Object.FindObjectOfType<CashflowGame>() == null)
            new GameObject("Cashflow Game").AddComponent<CashflowGame>();
    }
}
