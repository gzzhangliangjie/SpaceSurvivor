using System;
using System.IO;
using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 将保存到磁盘的持久性游戏数据。
    /// </summary>
    [Serializable]
    public struct PersistentGameData
    {
        /// <summary>
        /// 版本号最新保存文件。
        /// </summary>
        /// <remarks>
        /// <see cref="PersistentDataManager"/> 可以使用此版本号将存档文件升级到当前版本。
        /// </remarks>
        public int PersistentGameDataVersion;
        /// <summary>
        /// 玩家可用的硬币总数。可以通过游戏获得硬币，并用硬币购买角色和关卡。
        /// </summary>
        /// <remarks>
        /// 该值会显示在标题屏幕上给玩家看。
        /// </remarks>
        public int CurrentCoinCount;
        /// <summary>
        /// 总击败敌人数量。
        /// </summary>
        /// <remarks>
        /// 该值会显示在标题屏幕上给玩家看。
        /// </remarks>
        public int TotalEnemiesDefeated;
        /// <summary>
        /// 玩家在所有游戏会话中生存的总时间（秒）。
        /// </summary>
        /// <remarks>
        /// 该值会显示在标题屏幕上给玩家看。
        /// </remarks>
        public float TotalTimeSurvived;
        /// <summary>
        /// 字节标志以跟踪哪些角色已解锁。
        /// </summary>
        /// <remarks>
        /// 每个角色都被分配一个在 <see cref="CharacterProperties.CharacterID"/> 中指定的位。
        /// </remarks>
        public byte UnlockedCharacterFlags;
        /// <summary>
        /// 字节标志以跟踪哪些关卡已解锁。
        /// </summary>
        /// <remarks>
        /// 每个关卡都被分配一个在 <see cref="StageProperties.StageID"/> 中指定的位。
        /// </remarks>
        public byte UnlockedStageFlags;
        /// <summary>
        /// 当前音乐音量级别，以dB为单位存储。
        /// </summary>
        public float MusicVolumeLevel;
        /// <summary>
        /// 当前SFX音量级别，以dB为单位存储。
        /// </summary>
        public float SfxVolumeLevel;
    }

    /// <summary>
    /// 用于处理保存、加载和处理与游戏玩法相关的持久性数据的单例MonoBehaviour。
    /// </summary>
    public class PersistentDataManager : MonoBehaviour
    {
        /// <summary>
        /// 对此MonoBehaviour的公共单例访问。
        /// </summary>
        public static PersistentDataManager Instance;

        /// <summary>
        /// 最新存档文件的当前版本。
        /// </summary>
        /// <remarks>
        /// 将此值与给定存档文件的 <see cref="PersistentGameData.PersistentGameDataVersion"/> 进行比较，以确定其是否为当前版本。如果不是，则可以实施一些步骤将存档文件升级到当前版本。
        /// </remarks>
        private const int PERSISTENT_GAME_DATA_VERSION = 1;
        /// <summary>
        /// 将要使用的存档文件的文件路径。
        /// </summary>
        /// <remarks>
        /// 此字符串将附加到 Application.PersistentDataPath 以确定完整的文件路径。
        /// </remarks>
        private const string PERSISTENT_FILE_PATH = "/PlayerSave.bin";

        /// <summary>
        /// 当玩家金币数更新时调用的事件。这将触发UI元素以适当的金币数进行更新。
        /// </summary>
        public Action<int> OnUpdateCoinCount;
        /// <summary>
        /// 当玩家击败的外星人数量更新时调用的事件。这将触发UI元素以适当的金币数进行更新。
        /// </summary>
        public Action<int> OnUpdateEnemiesDefeated;
        /// <summary>
        /// 当玩家存活时间更新时调用的事件。这将触发UI元素以适当的金币数进行更新。
        /// </summary>
        public Action<float> OnUpdateTimeSurvived;

        /// <summary>
        /// 首次开始游戏或删除所有存档数据时，这是将解锁并可供玩家使用的初始角色。
        /// </summary>
        [SerializeField] private CharacterProperties _initialUnlockedCharacter;
        /// <summary>
        /// 首次开始游戏或删除所有存档数据时，这是将解锁并可供玩家使用的初始关卡。
        /// </summary>
        [SerializeField] private StageProperties _initialUnlockedStage;

        /// <summary>
        /// 将保存到磁盘的持久性游戏数据。
        /// </summary>
        private PersistentGameData _persistentGameData;

        /// <summary>
        /// 玩家可用的硬币总数。可以通过游戏获得硬币，并用硬币购买角色和关卡。
        /// </summary>
        public int CurrentCoinCount => _persistentGameData.CurrentCoinCount;
        /// <summary>
        /// 用于访问玩家已击败的敌人总数的属性。
        /// </summary>
        public int EnemiesDefeatedCount => _persistentGameData.TotalEnemiesDefeated;
        /// <summary>
        /// 用于访问玩家在游戏中存活的总时间（秒）的属性。
        /// </summary>
        public float TotalTimeSurvived => _persistentGameData.TotalTimeSurvived;
        /// <summary>
        /// 用于访问当前音乐音量级别（dB）的属性。
        /// </summary>
        public float MusicVolumeLevel => _persistentGameData.MusicVolumeLevel;
        /// <summary>
        /// 用于访问当前sfx音量级别（dB）的属性。
        /// </summary>
        public float SfxVolumeLevel => _persistentGameData.SfxVolumeLevel;

        /// <summary>
        /// 设置单例实例并标记为在加载时不销毁。
        /// </summary>
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadPersistentGameData();
            _persistentGameData.UnlockedCharacterFlags |= (byte)_initialUnlockedCharacter.CharacterID;
            _persistentGameData.UnlockedStageFlags |= (byte)_initialUnlockedStage.StageID;
            SavePersistentGameData();
        }

        private void Start()
        {
            OnUpdateCoinCount?.Invoke(CurrentCoinCount);
            OnUpdateEnemiesDefeated?.Invoke(EnemiesDefeatedCount);
            OnUpdateTimeSurvived?.Invoke(TotalTimeSurvived);
        }

        /// <summary>
        /// 从存档文件中加载已保存的 <see cref="PersistentGameData"/> 到内存中。
        /// </summary>
        /// <remarks>
        /// 存储在二进制文件中，一次读取一个字段。
        /// </remarks>
        private void LoadPersistentGameData()
        {
            var fullPath = Application.persistentDataPath + PERSISTENT_FILE_PATH;
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("Save file not found: " + fullPath);
                return;
            }

            using var fileStream = new FileStream(fullPath, FileMode.Open);
            using var reader = new BinaryReader(fileStream);

            _persistentGameData = new PersistentGameData
            {
                PersistentGameDataVersion = reader.ReadInt32(),
                CurrentCoinCount = reader.ReadInt32(),
                TotalEnemiesDefeated = reader.ReadInt32(),
                TotalTimeSurvived = reader.ReadSingle(),
                UnlockedCharacterFlags = reader.ReadByte(),
                UnlockedStageFlags = reader.ReadByte(),
                MusicVolumeLevel = reader.ReadSingle(),
                SfxVolumeLevel = reader.ReadSingle()
            };

            if (_persistentGameData.PersistentGameDataVersion < PERSISTENT_GAME_DATA_VERSION)
            {
                Debug.LogWarning($"Loaded persistent save data from previous version: {_persistentGameData.PersistentGameDataVersion} while current version is: {PERSISTENT_GAME_DATA_VERSION}");

                // Resolve any save data version conflicts here.

                _persistentGameData.PersistentGameDataVersion = PERSISTENT_GAME_DATA_VERSION;
            }
        }

        /// <summary>
        /// 将当前的 <see cref="PersistentGameData"/> 结构体保存到磁盘。
        /// </summary>
        /// <remarks>
        /// 存储在二进制文件中，一次写入一个字段。
        /// </remarks>
        private void SavePersistentGameData()
        {
            var fullPath = Application.persistentDataPath + PERSISTENT_FILE_PATH;

            using var fileStream = new FileStream(fullPath, FileMode.Create);
            using var writer = new BinaryWriter(fileStream);

            writer.Write(PERSISTENT_GAME_DATA_VERSION);
            writer.Write(_persistentGameData.CurrentCoinCount);
            writer.Write(_persistentGameData.TotalEnemiesDefeated);
            writer.Write(_persistentGameData.TotalTimeSurvived);
            writer.Write(_persistentGameData.UnlockedCharacterFlags);
            writer.Write(_persistentGameData.UnlockedStageFlags);
            writer.Write(_persistentGameData.MusicVolumeLevel);
            writer.Write(_persistentGameData.SfxVolumeLevel);
        }

        /// <summary>
        /// 尝试购买角色访问权限的方法。
        /// </summary>
        /// <param name="cost">角色的花费。</param>
        /// <param name="characterID">角色的ID，如果角色被解锁，用于在 <see cref="PersistentGameData.UnlockedCharacterFlags"/> 上设置角色的位标志。</param>
        /// <returns>如果角色成功购买则为true，如果玩家没有足够的钱购买角色则为false。</returns>
        /// <remarks>
        /// 如果角色成功购买，<see cref="PersistentGameData.CurrentCoinCount"/> 和 <see cref="PersistentGameData.UnlockedCharacterFlags"/> 的持久性字段将相应更新。
        /// </remarks>
        public bool TryBuyCharacter(int cost, CharacterID characterID)
        {
            if (cost > CurrentCoinCount) return false;
            _persistentGameData.CurrentCoinCount -= cost;
            _persistentGameData.UnlockedCharacterFlags |= (byte)characterID;
            SavePersistentGameData();
            OnUpdateCoinCount?.Invoke(CurrentCoinCount);
            return true;
        }

        /// <summary>
        /// 用于确定角色是否已解锁的方法。
        /// </summary>
        /// <param name="characterID">用于查看 <see cref="PersistentGameData.UnlockedCharacterFlags"/> 中的位标志以确定角色是否已解锁的角色ID。</param>
        /// <returns>如果角色已解锁则为true，否则为false。</returns>
        public bool IsCharacterUnlocked(CharacterID characterID)
        {
            return (_persistentGameData.UnlockedCharacterFlags & (byte)characterID) != 0;
        }

        /// <summary>
        /// 尝试购买关卡访问权限的方法。
        /// </summary>
        /// <param name="cost">关卡的花费。</param>
        /// <param name="stageID">关卡的ID，如果关卡被解锁，用于在 <see cref="PersistentGameData.UnlockedStageFlags"/> 上设置关卡的位标志。</param>
        /// <returns>如果关卡成功购买则为true，如果玩家没有足够的钱购买关卡则为false。</returns>
        /// <remarks>
        /// 如果关卡成功购买，<see cref="PersistentGameData.CurrentCoinCount"/> 和 <see cref="PersistentGameData.UnlockedStageFlags"/> 的持久性字段将相应更新。
        /// </remarks>
        public bool TryBuyStage(int cost, StageID stageID)
        {
            if (cost > CurrentCoinCount) return false;
            _persistentGameData.CurrentCoinCount -= cost;
            _persistentGameData.UnlockedStageFlags |= (byte)stageID;
            SavePersistentGameData();
            OnUpdateCoinCount?.Invoke(CurrentCoinCount);
            return true;
        }

        /// <summary>
        /// 解锁美术测试场景，以防此构建是测试构建。
        /// </summary>
        /// <remarks>
        /// 以前有一个专用的美术测试场景，但现在已合并到常规游戏中。
        /// </remarks>
        public void UnlockArtTestScene()
        {
            _persistentGameData.UnlockedStageFlags |= (byte)StageID.ArtTestScene;
            SavePersistentGameData();
        }

        /// <summary>
        /// 用于确定关卡是否已解锁的方法。
        /// </summary>
        /// <param name="stageID">用于查看 <see cref="PersistentGameData.UnlockedStageFlags"/> 中的位标志以确定关卡是否已解锁的关卡ID。</param>
        /// <returns>如果关卡已解锁则为true，否则为false。</returns>
        public bool IsStageUnlocked(StageID stageID)
        {
            return (_persistentGameData.UnlockedStageFlags & (byte)stageID) != 0;
        }

        /// <summary>
        /// 将钱添加到玩家的金币计数中。
        /// </summary>
        /// <param name="moneyToAdd">要添加的金币数量。</param>
        public void AddMoney(int moneyToAdd)
        {
            _persistentGameData.CurrentCoinCount += moneyToAdd;
            SavePersistentGameData();
            OnUpdateCoinCount?.Invoke(CurrentCoinCount);
        }

        /// <summary>
        /// 将击败的敌人数量添加到玩家的统计数据中。
        /// </summary>
        /// <param name="additionalEnemiesDefeated">要添加的击败的敌人数量。</param>
        public void AddEnemiesDefeated(int additionalEnemiesDefeated)
        {
            _persistentGameData.TotalEnemiesDefeated += additionalEnemiesDefeated;
            SavePersistentGameData();
            OnUpdateEnemiesDefeated?.Invoke(EnemiesDefeatedCount);
        }

        /// <summary>
        /// 将存活时间（秒）添加到玩家的统计数据中。
        /// </summary>
        /// <param name="additionalTimeSurvived">要添加的存活时间。</param>
        public void AddTimeSurvived(float additionalTimeSurvived)
        {
            _persistentGameData.TotalTimeSurvived += additionalTimeSurvived;
            SavePersistentGameData();
            OnUpdateTimeSurvived?.Invoke(TotalTimeSurvived);
        }

        /// <summary>
        /// 保存音乐音量。
        /// </summary>
        /// <param name="volumeLevel">要保存的音量级别（dB）。</param>
        public void SaveMusicVolume(float volumeLevel)
        {
            _persistentGameData.MusicVolumeLevel = volumeLevel;
            SavePersistentGameData();
        }

        /// <summary>
        /// 保存音效音量。
        /// </summary>
        /// <param name="volumeLevel">要保存的音量级别（dB）。</param>
        public void SaveSfxVolume(float volumeLevel)
        {
            _persistentGameData.SfxVolumeLevel = volumeLevel;
            SavePersistentGameData();
        }

        /// <summary>
        /// 用于在开发期间测试的辅助函数。
        /// </summary>
        [ContextMenu("Delete Save Data")]
        public void DeleteData()
        {
            var fullPath = Application.persistentDataPath + PERSISTENT_FILE_PATH;
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                Debug.Log("存档数据已删除");
                _persistentGameData = new PersistentGameData
                {
                    PersistentGameDataVersion = PERSISTENT_GAME_DATA_VERSION,
                    CurrentCoinCount = 0,
                    TotalEnemiesDefeated = 0,
                    TotalTimeSurvived = 0,
                    UnlockedCharacterFlags = (byte)_initialUnlockedCharacter.CharacterID,
                    UnlockedStageFlags = (byte)_initialUnlockedStage.StageID,
                    MusicVolumeLevel = 0f,
                    SfxVolumeLevel = 0f
                };
                OnUpdateCoinCount?.Invoke(CurrentCoinCount);
                OnUpdateEnemiesDefeated?.Invoke(EnemiesDefeatedCount);
                OnUpdateTimeSurvived?.Invoke(TotalTimeSurvived);
            }
            else
            {
                Debug.LogWarning("未找到要删除的存档数据");
            }
        }
    }
}