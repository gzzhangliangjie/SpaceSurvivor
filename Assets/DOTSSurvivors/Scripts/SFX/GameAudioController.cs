using UnityEngine;
using UnityEngine.Audio;
using Unity.Entities;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于设置正在播放的音频剪辑优先级的枚举。
    /// </summary>
    /// <remarks>
    /// When lots of audio sources are being played at once, audio sources with a lower priority value are more likely to play than audio sources with a higher priority value.
    /// </remarks>
    /// <seealso cref="GameAudioController"/>
    /// <seealso cref="MainMenuAudioController"/>
    public enum AudioPriority : byte
    {
        Highest = 0,
        NextHighest = 1,
        VeryHigh = 16,
        High = 32,
        AboveAverage = 64,
        Average = 128,
        Low = 192,
        Lowest = 255
    }

    /// <summary>
    /// 用于控制游戏中音频的MonoBehaviour。
    /// </summary>
    /// <seealso cref="MainMenuAudioController"/>
    public class GameAudioController : MonoBehaviour
    {
        /// <summary>
        /// 在游戏中播放音频的公共单例访问。
        /// </summary>
        public static GameAudioController Instance;

        /// <summary>
        /// 当玩家被摧毁且游戏结束时播放的音频剪辑。
        /// </summary>
        [SerializeField] private AudioClip _gameOverAudioClip;
        /// <summary>
        /// 播放背景音乐的音频源。用于在特定场景中播放/暂停背景音乐。
        /// </summary>
        [SerializeField] private AudioSource _backgroundMusicAudioSource;
        /// <summary>
        /// 音效池数量。在关卡开始时，会生成这个数量的音频源来播放一次性音效。
        /// </summary>
        [SerializeField] private int _sfxPoolCount = 100;
        /// <summary>
        /// 循环音效池数量。在关卡开始时，会生成这个数量的音频源来播放循环音效。
        /// </summary>
        [SerializeField] private int _loopingPoolCount = 50;
        /// <summary>
        /// 抗暂停音效池数量。在关卡开始时，会生成这个数量的音频源，它们播放的音效在会暂停其他音效类型的暂停事件中不会暂停。
        /// </summary>
        [SerializeField] private int _pauseResistantPoolCount = 10;
        /// <summary>
        /// 用于独立设置音乐和音效音量的AudioMixer。
        /// </summary>
        [SerializeField] private AudioMixer _audioMixer;
        /// <summary>
        /// 用于音效的AudioMixerGroup，在实例化音效的AudioSources池时使用。
        /// </summary>
        [SerializeField] private AudioMixerGroup _soundEffectsMixerGroup;

        /// <summary>
        /// 此控制器订阅来自ECS系统的事件。在游戏开始时，此控制器将尝试订阅事件，如果失败次数超过此数量，则很可能ECS初始化系统存在问题，并将抛出错误。这是为了确保即使在此控制器加载时ECS系统尚未创建，事件也能被订阅。
        /// </summary>
        private const int EVENT_SCHEDULE_FAIL_COUNT = 600;

        /// <summary>
        /// 用于播放常规音效的音频源池。
        /// </summary>
        private AudioSource[] _sfxAudioSources;
        /// <summary>
        /// 用于跟踪各自池中当前播放音效的音频源索引。
        /// </summary>
        private int _sfxAudioSourceIndex;

        /// <summary>
        /// 用于播放循环音效的音频源池。
        /// </summary>
        private AudioSource[] _loopingAudioSources;
        /// <summary>
        /// 用于跟踪各自池中当前播放音效的音频源索引。
        /// </summary>
        private int _loopingAudioSourceIndex;

        /// <summary>
        /// 用于播放抗暂停音效的音频源池。
        /// </summary>
        private AudioSource[] _pauseResistantAudioSources;
        /// <summary>
        /// 用于跟踪各自池中当前播放音效的音频源索引。
        /// </summary>
        private int _pauseResistantAudioSourceIndex;

        /// <summary>
        /// 用于跟踪在暂停事件期间暂停并应在游戏恢复时恢复的音效剪辑的列表。
        /// </summary>
        private List<int> _pausedSfxClipIndices;
        /// <summary>
        /// 用于跟踪在暂停事件期间暂停并应在游戏恢复时恢复的循环音效剪辑的列表。
        /// </summary>
        private List<int> _pausedLoopingClipIndices;

        /// <summary>
        /// 表示游戏结束且不应恢复音效的布尔值。
        /// </summary>
        private bool _isGameOver;

        /// <summary>
        /// 播放音乐的最大分贝。
        /// </summary>
        private const float MAX_MUSIC_DB = 0f;
        /// <summary>
        /// 播放音效的最大分贝。
        /// </summary>
        private const float MAX_SFX_DB = 0f;
        /// <summary>
        /// 在完全静音音乐通道前播放音乐的最小分贝。
        /// </summary>
        private const float MIN_MUSIC_DB = -40f;
        /// <summary>
        /// 在完全静音音效通道前播放音效的最小分贝。
        /// </summary>
        private const float MIN_SFX_DB = -40f;
        /// <summary>
        /// 音量UI滑块的截止电平。如果值低于此值，关联通道的音频将被完全静音。
        /// </summary>
        private const float MIN_CUTOFF_LEVEL = 0.05f;
        /// <summary>
        /// 音乐音量参数的字符串。
        /// </summary>
        private const string MUSIC_VOLUME_PARAMETER = "MusicVolume";
        /// <summary>
        /// 音效音量参数的字符串。
        /// </summary>
        private const string SFX_VOLUME_PARAMETER = "SFXVolume";

        /// <summary>
        /// 初始化单例实例。
        /// </summary>
        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("Warning, multiple instances of AudioController detected. Destroying new instance");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// 实例化并初始化音效池。从 <see cref="PersistentDataManager"/> 加载音乐和音效通道的音量级别。
        /// </summary>
        private void Start()
        {
            _sfxAudioSources = new AudioSource[_sfxPoolCount];
            for (var i = 0; i < _sfxPoolCount; i++)
            {
                var newSfxAudioSourceGameObject = new GameObject($"SFXAudioSource_{i}");
                newSfxAudioSourceGameObject.transform.parent = transform;
                var newSfxAudioSource = newSfxAudioSourceGameObject.AddComponent<AudioSource>();
                newSfxAudioSource.outputAudioMixerGroup = _soundEffectsMixerGroup;
                newSfxAudioSource.priority = (int)AudioPriority.Average;
                _sfxAudioSources[i] = newSfxAudioSource;
            }

            _loopingAudioSources = new AudioSource[_loopingPoolCount];
            for (var i = 0; i < _loopingPoolCount; i++)
            {
                var newLoopingAudioSourceGameObject = new GameObject($"LoopingAudioSource_{i}");
                newLoopingAudioSourceGameObject.transform.parent = transform;
                var newLoopingAudioSource = newLoopingAudioSourceGameObject.AddComponent<AudioSource>();
                newLoopingAudioSource.loop = true;
                newLoopingAudioSource.outputAudioMixerGroup = _soundEffectsMixerGroup;
                newLoopingAudioSource.priority = (int)AudioPriority.AboveAverage;
                _loopingAudioSources[i] = newLoopingAudioSource;
            }

            _pauseResistantAudioSources = new AudioSource[_pauseResistantPoolCount];
            for (var i = 0; i < _pauseResistantPoolCount; i++)
            {
                var newPauseResistantAudioSourceGameObject = new GameObject($"PauseResistantAudioSource_{i}");
                newPauseResistantAudioSourceGameObject.transform.parent = transform;
                var newPauseResistantAudioSource = newPauseResistantAudioSourceGameObject.AddComponent<AudioSource>();
                newPauseResistantAudioSource.outputAudioMixerGroup = _soundEffectsMixerGroup;
                newPauseResistantAudioSource.priority = (int)AudioPriority.High;
                _pauseResistantAudioSources[i] = newPauseResistantAudioSource;
            }

            var savedMusicVolumeLevel = PersistentDataManager.Instance.MusicVolumeLevel;
            _audioMixer.SetFloat(MUSIC_VOLUME_PARAMETER, savedMusicVolumeLevel);

            var savedSfxVolumeLevel = PersistentDataManager.Instance.SfxVolumeLevel;
            _audioMixer.SetFloat(SFX_VOLUME_PARAMETER, savedSfxVolumeLevel);
        }

        private void OnEnable()
        {
            StartCoroutine(DelayEventSubscription());
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        /// <summary>
        /// 此控制器订阅来自ECS系统的事件。在游戏开始时，此控制器将尝试订阅事件，如果失败次数超过 <see cref="EVENT_SCHEDULE_FAIL_COUNT"/> 次，则很可能ECS初始化系统存在问题，并将抛出错误。这是为了确保即使在此控制器加载时ECS系统尚未创建，事件也能被订阅。
        /// </summary>
        private IEnumerator DelayEventSubscription()
        {
            yield return null;
            var failCount = 0;
            var defaultWorld = World.DefaultGameObjectInjectionWorld;
            while (defaultWorld == null)
            {
                failCount++;
                if (failCount > EVENT_SCHEDULE_FAIL_COUNT)
                {
                    Debug.LogError($"Default World was null for {EVENT_SCHEDULE_FAIL_COUNT} frames. Check to ensure ECS world is being properly initialized.");
                    yield break;
                }
                yield return null;
                defaultWorld = World.DefaultGameObjectInjectionWorld;
            }

            SubscribeToEvents(defaultWorld);
        }

        private void SubscribeToEvents(World world)
        {
            PauseManager.Instance.OnPauseGame += PauseAudioClips;
            PauseManager.Instance.OnResumeGame += ResumeAudioClips;

            var beginGameOverSystem = world.GetExistingSystemManaged<BeginGameOverSystem>();
            beginGameOverSystem.OnGameOver += PlayGameOverAudio;
        }

        private void UnsubscribeFromEvents()
        {
            PauseManager.Instance.OnPauseGame -= PauseAudioClips;
            PauseManager.Instance.OnResumeGame -= ResumeAudioClips;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var beginGameOverSystem = world.GetExistingSystemManaged<BeginGameOverSystem>();
            beginGameOverSystem.OnGameOver -= PlayGameOverAudio;
        }

        /// <summary>
        /// 当游戏结束时，播放 <see cref="_gameOverAudioClip"/>。
        /// </summary>
        private void PlayGameOverAudio()
        {
            _isGameOver = true;
            _backgroundMusicAudioSource.Stop();
            PlayPauseResistantAudioClip(_gameOverAudioClip, (int)AudioPriority.Highest);
        }

        /// <summary>
        /// 播放普通音效剪辑的方法。
        /// </summary>
        /// <param name="audioClip">Audio clip to play.</param>
        /// <param name="priority">Priority of the sound effect. <see cref="AudioPriority"/></param>
        public void PlaySfxAudioClip(AudioClip audioClip, int priority = (int)AudioPriority.Average)
        {
            var currentAudioSource = _sfxAudioSources[_sfxAudioSourceIndex];
            currentAudioSource.clip = audioClip;
            currentAudioSource.priority = priority;
            currentAudioSource.Play();
            _sfxAudioSourceIndex += 1;
            _sfxAudioSourceIndex %= _sfxAudioSources.Length;
        }

        /// <summary>
        /// 播放循环音效剪辑的方法。
        /// </summary>
        /// <param name="audioClip">Audio clip to play.</param>
        /// <param name="priority">Priority of the sound effect. <see cref="AudioPriority"/></param>
        /// <returns>Index of the audio source playing the clip, so it can be stopped at a later time.</returns>
        public int PlayLoopingAudioClip(AudioClip audioClip, int priority = (int)AudioPriority.AboveAverage)
        {
            var currentAudioSource = _loopingAudioSources[_loopingAudioSourceIndex];
            currentAudioSource.clip = audioClip;
            currentAudioSource.priority = priority;
            currentAudioSource.Play();
            var audioSourceIndex = _loopingAudioSourceIndex;
            _loopingAudioSourceIndex += 1;
            _loopingAudioSourceIndex %= _loopingAudioSources.Length;
            return audioSourceIndex;
        }

        /// <summary>
        /// 停止循环音频源的方法。
        /// </summary>
        /// <param name="index">Index of the looping audio source to stop.</param>
        public void StopLoopingAudioClip(int index)
        {
            if (index < 0 || index >= _loopingAudioSources.Length) return;
            _loopingAudioSources[index].Stop();
        }

        /// <summary>
        /// 播放抗暂停音效剪辑的方法。
        /// </summary>
        /// <param name="audioClip">Audio clip to play.</param>
        /// <param name="priority">Priority of the sound effect. <see cref="AudioPriority"/></param>
        public void PlayPauseResistantAudioClip(AudioClip audioClip, int priority = (int)AudioPriority.High)
        {
            var currentAudioSource = _pauseResistantAudioSources[_pauseResistantAudioSourceIndex];
            currentAudioSource.clip = audioClip;
            currentAudioSource.priority = priority;
            currentAudioSource.Play();
            _pauseResistantAudioSourceIndex += 1;
            _pauseResistantAudioSourceIndex %= _pauseResistantAudioSources.Length;
        }

        /// <summary>
        /// 在播放和暂停状态之间切换背景音乐的方法。
        /// </summary>
        public void PlayPauseBackgroundMusic()
        {
            if (_backgroundMusicAudioSource.isPlaying)
            {
                _backgroundMusicAudioSource.Pause();
            }
            else
            {
                _backgroundMusicAudioSource.Play();
            }
        }

        /// <summary>
        /// 暂停音频剪辑的方法。
        /// </summary>
        private void PauseAudioClips()
        {
            _pausedSfxClipIndices = new List<int>();
            for (var i = 0; i < _sfxAudioSources.Length; i++)
            {
                var audioSource = _sfxAudioSources[i];
                if (audioSource.isPlaying)
                {
                    _pausedSfxClipIndices.Add(i);
                    audioSource.Pause();
                }
            }

            _pausedLoopingClipIndices = new List<int>();
            for (var i = 0; i < _loopingAudioSources.Length; i++)
            {
                var audioSource = _loopingAudioSources[i];
                if (audioSource.isPlaying)
                {
                    _pausedLoopingClipIndices.Add(i);
                    audioSource.Pause();
                }
            }
        }

        /// <summary>
        /// 当游戏从暂停状态返回播放状态时恢复音频剪辑的方法。
        /// </summary>
        private void ResumeAudioClips()
        {
            if (_isGameOver) return;

            foreach (var sfxClipIndex in _pausedSfxClipIndices)
            {
                _sfxAudioSources[sfxClipIndex].Play();
            }

            _pausedSfxClipIndices = new List<int>();

            foreach (var loopingClipIndex in _pausedLoopingClipIndices)
            {
                _loopingAudioSources[loopingClipIndex].Play();
            }

            _pausedLoopingClipIndices = new List<int>();
        }

        /// <summary>
        /// 返回标准化的音乐音量级别的方法。
        /// </summary>
        /// <remarks>
        /// If volume level is lower than the defined minimum value, 0 will be returned as the music volume can be considered muted.
        /// This is used to set the music volume slider in UI configuration based on its current values.
        /// </remarks>
        /// <returns>Normalized music volume level from 0 and 1</returns>
        public float GetNormalizedMusicLevel()
        {
            _audioMixer.GetFloat(MUSIC_VOLUME_PARAMETER, out var musicVolume);
            if (musicVolume <= MIN_MUSIC_DB)
            {
                return 0f;
            }

            return math.unlerp(MIN_MUSIC_DB, MAX_MUSIC_DB, musicVolume);
        }

        /// <summary>
        /// 根据UI滑块的标准化值设置音乐音量。
        /// </summary>
        /// <remarks>
        /// If normalized value is below the <see cref="MIN_CUTOFF_LEVEL"/>, the music volume will be set to -80dB, effectively muting the channel.
        /// </remarks>
        /// <param name="normalizedLevel">Value from 0 to 1 for the music volume.</param>
        public void SetMusicVolume(float normalizedLevel)
        {
            var newVolume = Mathf.Lerp(MIN_MUSIC_DB, MAX_MUSIC_DB, normalizedLevel);
            if (normalizedLevel < MIN_CUTOFF_LEVEL)
            {
                newVolume = -80;
            }
            _audioMixer.SetFloat(MUSIC_VOLUME_PARAMETER, newVolume);
            PersistentDataManager.Instance.SaveMusicVolume(newVolume);
        }

        /// <summary>
        /// 返回标准化的音效音量级别的方法。
        /// </summary>
        /// <remarks>
        /// If volume level is lower than the defined minimum value, 0 will be returned as the sound effects volume can be considered muted.
        /// This is used to set the sound effects volume slider in UI configuration based on its current values.
        /// </remarks>
        /// <returns>Normalized sound effects volume level from 0 and 1</returns>
        public float GetNormalizedSfxLevel()
        {
            _audioMixer.GetFloat(SFX_VOLUME_PARAMETER, out var sfxVolume);
            if (sfxVolume <= MIN_SFX_DB)
            {
                return 0f;
            }

            return math.unlerp(MIN_SFX_DB, MAX_SFX_DB, sfxVolume);
        }

        /// <summary>
        /// 根据UI滑块的标准化值设置音效音量。
        /// </summary>
        /// <remarks>
        /// If normalized value is below the <see cref="MIN_CUTOFF_LEVEL"/>, the sound effects volume will be set to -80dB, effectively muting the channel.
        /// </remarks>
        /// <param name="normalizedLevel">Value from 0 to 1 for the sound effects volume.</param>
        public void SetSfxVolume(float normalizedLevel)
        {
            var newVolume = Mathf.Lerp(MIN_SFX_DB, MAX_SFX_DB, normalizedLevel);
            if (normalizedLevel < MIN_CUTOFF_LEVEL)
            {
                newVolume = -80;
            }
            _audioMixer.SetFloat(SFX_VOLUME_PARAMETER, newVolume);
            PersistentDataManager.Instance.SaveSfxVolume(newVolume);
        }
    }
}