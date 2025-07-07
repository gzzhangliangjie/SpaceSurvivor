using UnityEditor;
using UnityEngine;

namespace Lemo.SpaceSurvivors.EditorScripts
{
    /// <summary>
    /// Custom editor script to display <see cref="EnemySpawnEventProperties"/> in the editor.
    /// </summary>
    /// <remarks>
    /// <see cref="EnemySpawnEventProperties"/> 是一个可编程对象(ScriptableObject)，用于保存与敌人生成事件相关的数据。这个编辑器脚本使得编辑这些生成事件更加容易，因为它只显示与所选生成阵型相关的字段。
    /// </remarks>
    [CustomEditor(typeof(EnemySpawnEventProperties))]
    public class SpawnEventPropertiesEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var spawnEventProperties = (EnemySpawnEventProperties)target;
            EditorGUI.BeginChangeCheck();

            spawnEventProperties.SpawnFormation = (SpawnFormation)EditorGUILayout.EnumPopup(new GUIContent("Spawn Formation", "Select the spawn formation type."), spawnEventProperties.SpawnFormation);

            if (spawnEventProperties.SpawnFormation == SpawnFormation.None) return;

            spawnEventProperties.EnemyType = (EnemyType)EditorGUILayout.EnumPopup("Enemy Type", spawnEventProperties.EnemyType);

            spawnEventProperties.EnemyCount = EditorGUILayout.IntField("Enemy Count", spawnEventProperties.EnemyCount);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(new GUIContent("Spawn Formation Specific Properties", "Certain spawn formations require additional data which can be set below."), EditorStyles.boldLabel);
            EditorGUILayout.Space();

            switch (spawnEventProperties.SpawnFormation)
            {
                case SpawnFormation.LinearMoveGroup:
                    spawnEventProperties.MoveSpeed = EditorGUILayout.FloatField(new GUIContent("Move Speed", "Linear speed the enemy will be moving"), spawnEventProperties.MoveSpeed);
                    spawnEventProperties.EnemySpacing = EditorGUILayout.FloatField("Enemy Spacing", spawnEventProperties.EnemySpacing);
                    break;
                case SpawnFormation.EllipseAroundView:
                    spawnEventProperties.MoveSpeed = EditorGUILayout.FloatField(new GUIContent("Move Speed", "Linear speed the enemy will be moving"), spawnEventProperties.MoveSpeed);
                    spawnEventProperties.TimeToLive = EditorGUILayout.FloatField("Time to Live", spawnEventProperties.TimeToLive);
                    break;
                case SpawnFormation.SineMoveVerticalLine or SpawnFormation.SineMoveHorizontalLine:
                    spawnEventProperties.MoveSpeed = EditorGUILayout.FloatField(new GUIContent("Constant Move Speed", "Linear speed the enemy will be moving, perpendicular to the sine oscillation."), spawnEventProperties.MoveSpeed);
                    spawnEventProperties.Period = EditorGUILayout.FloatField(new GUIContent("Period", "Frequency of sine oscillations"), spawnEventProperties.Period);
                    spawnEventProperties.Amplitude = EditorGUILayout.FloatField(new GUIContent("Amplitude", "1/2 height of the full sine oscillation"), spawnEventProperties.Amplitude);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(spawnEventProperties, "Updated SpawnEventProperties");
                EditorUtility.SetDirty(spawnEventProperties);
            }
        }
    }
}