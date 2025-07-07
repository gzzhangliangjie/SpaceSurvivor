using System;
using UnityEngine;
using Unity.Entities;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 控制游戏暂停状态的单例MonoBehaviour。
    /// </summary>
    public class PauseManager : MonoBehaviour
    {
        /// <summary>
        /// 对此MonoBehaviour的公共单例访问。
        /// </summary>
        public static PauseManager Instance;

        /// <summary>
        /// 当游戏进入暂停状态时调用的事件。
        /// </summary>
        /// <remarks>
        /// 这也会触发某些效果（如音效）进入暂停状态。
        /// </remarks>
        public Action OnPauseGame;
        /// <summary>
        /// 当游戏退出暂停状态并恢复游戏时调用的事件。
        /// </summary>
        /// <remarks>
        /// 这会触发某些效果（如音效）退出暂停状态并恢复其效果。
        /// </remarks>
        public Action OnResumeGame;

        /// <summary>
        /// 用于确定游戏当前是否处于暂停状态的布尔值。
        /// </summary>
        public bool IsPaused { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("Warning multiple instances of PauseManager detected. Destroying new one.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// Pauses the game.
        /// </summary>
        /// <remarks>
        /// 禁用了Unity的顶级系统组InitializationSystemGroup和SimulationSystemGroup，所有游戏性系统都在其下，从而有效地暂停了游戏。
        /// </remarks>
        public void PauseGame()
        {
            var initializationSystemGroup = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<InitializationSystemGroup>();
            initializationSystemGroup.Enabled = false;
            var simulationSystemGroup = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<SimulationSystemGroup>();
            simulationSystemGroup.Enabled = false;
            OnPauseGame?.Invoke();
            IsPaused = true;
        }

        /// <summary>
        /// Resumes the game.
        /// </summary>
        /// <remarks>
        /// 启用了Unity的顶级系统组InitializationSystemGroup和SimulationSystemGroup，所有游戏性系统都在其下，从而有效地恢复了游戏。
        /// </remarks>
        public void ResumeGame()
        {
            var initializationSystemGroup = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<InitializationSystemGroup>();
            initializationSystemGroup.Enabled = true;
            var simulationSystemGroup = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<SimulationSystemGroup>();
            simulationSystemGroup.Enabled = true;
            OnResumeGame?.Invoke();
            IsPaused = false;
        }
    }
}