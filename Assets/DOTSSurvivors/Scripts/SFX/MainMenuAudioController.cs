using UnityEngine;
using UnityEngine.Audio;
using Unity.Mathematics;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于控制主菜单音频的MonoBehaviour。
    /// </summary>
    /// <seealso cref="GameAudioController"/>
    public class MainMenuAudioController : MonoBehaviour
    {
        /// <summary>
        /// 在主菜单中播放音频的公共单例访问。
        /// </summary>
        public static MainMenuAudioController Instance;

        /// <summary>
        /// 音效池数量。在主菜单场景开始时，会生成这个数量的音频源来播放一次性音效。
        /// </summary>
        [SerializeField] private int _sfxPoolCount = 10;
        /// <summary>
        /// 用于独立设置音乐和音效音量的AudioMixer。
        /// </summary>
        [SerializeField] private AudioMixer _audioMixer;
        /// <summary>
        /// 用于音效的AudioMixerGroup，在实例化音效的AudioSources池时使用。
        /// </summary>
        [SerializeField] private AudioMixerGroup _soundEffectsMixerGroup;

        /// <summary>
        /// 用于播放常规音效的音频源池。
        /// </summary>
        private AudioSource[] _sfxAudioSources;
        /// <summary>
        /// 用于跟踪各自池中当前播放音效的音频源索引。
        /// </summary>
        private int _sfxAudioSourceIndex;

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
                Debug.LogWarning("Warning, multiple instances of MainMenuAudioController detected. Destroying new instance.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// 实例化并初始化音效池。从 <see cref="SerializationController"/> 加载音乐和音效通道的音量级别。
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

            var savedMusicVolumeLevel = PersistentDataManager.Instance.MusicVolumeLevel;
            _audioMixer.SetFloat(MUSIC_VOLUME_PARAMETER, savedMusicVolumeLevel);

            var savedSfxVolumeLevel = PersistentDataManager.Instance.SfxVolumeLevel;
            _audioMixer.SetFloat(SFX_VOLUME_PARAMETER, savedSfxVolumeLevel);
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