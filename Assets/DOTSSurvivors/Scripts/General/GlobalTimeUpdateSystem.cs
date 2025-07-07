using UnityEngine;
using Unity.Entities;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 更新全局时间着色器属性的系统。这是必需的，因为当游戏暂停时，任何依赖于此属性的动画着色器也将暂停，因为此系统是因升级或按 ESC 键（<see cref="PauseManager"/>）而暂停游戏时将被禁用的组的一部分。
    /// </summary>
    /// <remarks>
    /// 系统在 Unity 的 InitializationSystemGroup 中更新，并在其 UpdateWorldTimeSystem 之后更新，以确保 SystemAPI.Time.ElapsedTime 对于当前帧是准确的。
    /// </remarks>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(UpdateWorldTimeSystem))]
    public partial class GlobalTimeUpdateSystem : SystemBase
    {
        /// <summary>
        /// 着色器属性 ID 的缓存整数，用于更高效地设置着色器属性。
        /// </summary>
        private static int _globalTimeShaderPropertyID;

        protected override void OnCreate()
        {
            _globalTimeShaderPropertyID = Shader.PropertyToID("_GlobalTime");
        }

        protected override void OnUpdate()
        {
            Shader.SetGlobalFloat(_globalTimeShaderPropertyID, (float)SystemAPI.Time.ElapsedTime);
        }
    }
}