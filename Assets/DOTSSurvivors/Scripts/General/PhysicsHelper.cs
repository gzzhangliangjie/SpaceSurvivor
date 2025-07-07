using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于实现Unity API中不存在的辅助功能的静态帮助类。
    /// </summary>
    public static class PhysicsHelper
    {
        /// <summary>
        /// 静态方法，返回32位碰撞层位掩码，其可碰撞层的索引位将设置为1。
        /// </summary>
        /// <remarks>
        /// 通常用于创建可在射线投射和空间查询中使用的 CollisionFilter 类型。
        /// </remarks>
        /// <param name="layer">要获取碰撞位掩码的层的索引。</param>
        /// <returns>返回此层与之碰撞的所有层的32位碰撞层位掩码。</returns>
        public static int GetCollisionMaskForLayer(int layer)
        {
            var mask = 0;
            for (var i = 0; i < 32; i++)
            {
                if (Physics.GetIgnoreLayerCollision(layer, i)) continue;
                mask |= 1 << i;
            }
            return mask;
        }
    }
}