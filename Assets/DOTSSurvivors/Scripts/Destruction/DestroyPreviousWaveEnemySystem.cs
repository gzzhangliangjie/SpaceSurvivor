using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace Lemo.SpaceSurvivors
{

    /// <summary>
    /// 用于存储敌人生成波次的索引的共享组件。
    /// </summary>
    /// <remarks>
    /// 使用共享组件以便可以查询特定波数中的敌人。这在 <see cref="DestroyPreviousWaveEnemySystem"/> 系统中尤其有用，该系统用于清理先前波数中屏幕外的敌人。
    /// </remarks>
    public struct WaveIndexOfSpawn : ISharedComponentData
    {
        public int Value;
    }

    /// <summary>
    /// 用于销毁前一波生成的屏幕外敌人的系统。
    /// </summary>
    /// <remarks>
    /// 这用于确保定期生成一批新的敌人。
    /// </remarks>
    [UpdateInGroup(typeof(DS_DestructionSystemGroup))]
    public partial struct DestroyPreviousWaveEnemySystem : ISystem
    {
        /// <summary>
        /// 所有敌人的实体查询。共享组件过滤器将在此查询上设置，以测试前一波中是否存在敌人，如果它们在相机边界之外，则将其销毁。
        /// </summary>
        private EntityQuery _enemiesQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemySpawnerState>();
            state.RequireForUpdate<CameraTarget>();
            _enemiesQuery = state.EntityManager.CreateEntityQuery(typeof(EnemyTag), typeof(WaveIndexOfSpawn));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var curWaveIndex = SystemAPI.GetSingleton<EnemySpawnerState>().CurWaveIndex;
            var cameraReferenceEntity = SystemAPI.GetSingletonEntity<CameraTarget>();
            var cameraPosition = SystemAPI.GetComponent<LocalToWorld>(cameraReferenceEntity).Position;

            for (var i = 0; i < curWaveIndex; i++)
            {
                var sharedWaveIndex = new WaveIndexOfSpawn { Value = i };
                _enemiesQuery.SetSharedComponentFilter(sharedWaveIndex);
                if (_enemiesQuery.IsEmpty) continue;

                var enemiesInPreviousWave = _enemiesQuery.ToEntityArray(state.WorldUpdateAllocator);
                foreach (var enemy in enemiesInPreviousWave)
                {
                    var transform = SystemAPI.GetComponent<LocalTransform>(enemy);
                    var distanceFromCameraSq = math.distancesq(transform.Position.xz, cameraPosition.xz);
                    if (distanceFromCameraSq > 500)
                    {
                        state.EntityManager.AddComponent<InstantDestroyTag>(enemy);
                    }
                }
            }
        }
    }
}