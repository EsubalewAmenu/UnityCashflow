using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StartCashflow()
    {
        if (Object.FindObjectOfType<CashflowSceneRouter>() != null) return;
        var host = new GameObject("Cashflow Scene Router");
        Object.DontDestroyOnLoad(host);
        host.AddComponent<CashflowSceneRouter>().Open(SceneManager.GetActiveScene());
    }
}

// Persists across scene loads and starts the matching page controller.
public class CashflowSceneRouter : MonoBehaviour
{
    void Awake() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) { Open(scene); }

    public void Open(Scene scene)
    {
        if (Camera.main == null)
        {
            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.043f, 0.071f, 0.125f);
            camera.orthographic = true;
            cameraObject.tag = "MainCamera";
        }

        if (Object.FindObjectOfType<EventSystem>() == null)
            Object.DontDestroyOnLoad(new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));

        if (Object.FindObjectOfType<CashflowGame>() == null)
        {
            var game = new GameObject("Cashflow Game").AddComponent<CashflowGame>();
            game.OpenScene(scene.name);
        }
    }
}
