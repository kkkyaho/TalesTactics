using UnityEngine;

namespace TalesTactics
{
    public static class AndroidRuntimeSettings
    {
        // A conservative starting point for battery use. Validate on devices before raising it.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            if (Application.platform != RuntimePlatform.Android) return;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 30;
        }
    }
}
