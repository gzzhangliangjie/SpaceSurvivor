using Unity.Burst;
using UnityEngine;

namespace Lemo.SpaceSurvivors.EditorScripts
{
    /// <summary>
    /// Static class to check to see if Burst compilation is enabled in the editor.
    /// </summary>
    /// <remarks>
    /// 如果Burst被禁用，将在编辑器中打印一条警告，为用户提供有关如何启用的更多信息。
    /// </remarks>
    public static class BurstCompilationChecker
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (!BurstCompiler.Options.EnableBurstCompilation)
            {
                Debug.LogWarning("Burst compilation is not enabled and performance is expected to be degraded. Enable this at Jobs > Burst > Enable Compilation.\n\nFor more information view the FAQ page: https://survivors.tmg.dev/FAQ.html#why-does-the-game-run-at-a-low-frame-rate-in-the-editor\n");
            }
        }
    }
}