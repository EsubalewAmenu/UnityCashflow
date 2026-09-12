using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Pin builds to the engine security patch validated for this project.
public sealed class UnitySecurityBuildGuard : IPreprocessBuildWithReport
{
    public const string ValidatedEditor = "2022.3.62f3";
    public int callbackOrder => -2000;
    public void OnPreprocessBuild(BuildReport report)
    {
        if(Application.unityVersion != ValidatedEditor)
            throw new BuildFailedException("Use Unity " + ValidatedEditor +
                " for this project. Older builds were rejected for CVE-2025-59489. Validate any future editor upgrade before changing this guard.");
    }
}
