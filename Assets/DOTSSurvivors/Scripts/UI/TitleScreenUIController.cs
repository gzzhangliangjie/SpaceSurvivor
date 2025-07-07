using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 与标题屏幕中的主菜单关联的UI控制器。
    /// </summary>
    /// <remarks>
    /// 处理显示和隐藏适当的UI面板。
    /// </remarks>
    public class TitleScreenUIController : MonoBehaviour
    {
        [Header("Top Level")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _aboutButton;
        [SerializeField] private Button _optionsButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private TextMeshProUGUI _coinCountText;
        [SerializeField] private TextMeshProUGUI _aliensDefeatedText;
        [SerializeField] private TextMeshProUGUI _timeSurvivedText;

        [Header("关于面板")]
        [SerializeField] private GameObject _aboutPanel;
        [SerializeField] private Button _aboutCloseButton;
        [SerializeField] private Button _turboMakesGamesYouTubeButton;

        [Header("选项面板")]
        [SerializeField] private GameObject _optionsPanel;
        [SerializeField] private Button _optionsCloseButton;
        [SerializeField] private Button _toggleDisplayButton;
        [SerializeField] private Button _toggleFullScreenButton;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private Button _deleteDataButton;
        [SerializeField] private GameObject _confirmDeleteDataPanel;
        [SerializeField] private Button _confirmDeleteDataButton;
        [SerializeField] private Button _cancelDeleteDataButton;
        [SerializeField] private AudioClip _onSfxVolumeChangeAudioClip;

        [Header("确认退出面板")]
        [SerializeField] private GameObject _confirmQuitPanel;
        [SerializeField] private Button _confirmQuitButton;
        [SerializeField] private Button _cancelQuitButton;

        [Header("角色选择面板")]
        [SerializeField] private GameObject _characterSelectPanel;
        [SerializeField] private Button _characterSelectNextButton;
        [SerializeField] private Button _characterSelectBackButton;
        [SerializeField] private CharacterSelectPanelUIController _characterSelectPanelUIController;

        [Header("关卡选择面板")]
        [SerializeField] private GameObject _stageSelectPanel;
        [SerializeField] private Button _stageSelectGoButton;
        [SerializeField] private Button _stageSelectBackButton;
        [SerializeField] private Button _stageSelectArtTestSceneButton;
        [SerializeField] private StageSelectPanelUIController _stageSelectPanelUIController;

        private const int ART_TEST_SCENE_INDEX = 4;
        private DOTSSurvivorsInputActions _inputActions;
        private Action _onCancelAction;

        private void Awake()
        {
            _inputActions = new DOTSSurvivorsInputActions();
        }

        private void Start()
        {
            _aboutPanel.SetActive(false);
            _optionsPanel.SetActive(false);
            _confirmQuitPanel.SetActive(false);
            _characterSelectPanel.SetActive(false);
            _stageSelectPanel.SetActive(false);
            _playButton.Select();

            UpdateCoinCountText(PersistentDataManager.Instance.CurrentCoinCount);
            UpdateAliensDefeatedText(PersistentDataManager.Instance.EnemiesDefeatedCount);
            UpdateTimeSurvivedText(PersistentDataManager.Instance.TotalTimeSurvived);
        }

        private void OnEnable()
        {
            _inputActions.Enable();
            _inputActions.UI.Cancel.performed += OnCancelButton;

            _playButton.onClick.AddListener(OnButtonPlay);
            _aboutButton.onClick.AddListener(OnButtonAbout);
            _optionsButton.onClick.AddListener(OnButtonOptions);
            _quitButton.onClick.AddListener(OnButtonQuit);

            _aboutCloseButton.onClick.AddListener(OnButtonAboutClose);
            _turboMakesGamesYouTubeButton.onClick.AddListener(OnButtonTurboMakesGamesYouTube);

            _optionsCloseButton.onClick.AddListener(OnButtonOptionsClose);
            _toggleDisplayButton.onClick.AddListener(ToggleActiveDisplay);
            _toggleFullScreenButton.onClick.AddListener(ToggleFullScreen);
            _musicVolumeSlider.onValueChanged.AddListener(OnSliderMusicVolume);
            _sfxVolumeSlider.onValueChanged.AddListener(OnSliderSfxVolume);
            _deleteDataButton.onClick.AddListener(OnButtonDeleteData);
            _confirmDeleteDataButton.onClick.AddListener(OnButtonConfirmDeleteData);
            _cancelDeleteDataButton.onClick.AddListener(OnButtonCancelDeleteData);

            _confirmQuitButton.onClick.AddListener(OnButtonConfirmQuit);
            _cancelQuitButton.onClick.AddListener(OnButtonCancelQuit);

            _characterSelectNextButton.onClick.AddListener(OnButtonCharacterSelectNext);
            _characterSelectBackButton.onClick.AddListener(OnButtonCharacterSelectBack);

            _stageSelectGoButton.onClick.AddListener(OnButtonStageSelectGo);
            _stageSelectBackButton.onClick.AddListener(OnButtonStageSelectBack);
            _stageSelectArtTestSceneButton.onClick.AddListener(OnButtonArtTestScene);

            PersistentDataManager.Instance.OnUpdateCoinCount += UpdateCoinCountText;
            PersistentDataManager.Instance.OnUpdateEnemiesDefeated += UpdateAliensDefeatedText;
            PersistentDataManager.Instance.OnUpdateTimeSurvived += UpdateTimeSurvivedText;
        }

        private void OnDisable()
        {
            _inputActions.UI.Cancel.performed -= OnCancelButton;
            _inputActions.Disable();

            _playButton.onClick.RemoveAllListeners();
            _aboutButton.onClick.RemoveAllListeners();
            _optionsButton.onClick.RemoveAllListeners();
            _quitButton.onClick.RemoveAllListeners();

            _aboutCloseButton.onClick.RemoveAllListeners();

            _optionsCloseButton.onClick.RemoveAllListeners();
            _toggleDisplayButton.onClick.RemoveAllListeners();
            _toggleFullScreenButton.onClick.RemoveAllListeners();
            _musicVolumeSlider.onValueChanged.RemoveAllListeners();
            _sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            _deleteDataButton.onClick.RemoveAllListeners();
            _confirmDeleteDataButton.onClick.RemoveAllListeners();
            _cancelDeleteDataButton.onClick.RemoveAllListeners();

            _confirmQuitButton.onClick.RemoveAllListeners();
            _cancelQuitButton.onClick.RemoveAllListeners();

            _characterSelectNextButton.onClick.RemoveAllListeners();
            _characterSelectBackButton.onClick.RemoveAllListeners();

            _stageSelectGoButton.onClick.RemoveAllListeners();
            _stageSelectBackButton.onClick.RemoveAllListeners();
            _stageSelectArtTestSceneButton.onClick.RemoveAllListeners();

            PersistentDataManager.Instance.OnUpdateCoinCount -= UpdateCoinCountText;
            PersistentDataManager.Instance.OnUpdateEnemiesDefeated -= UpdateAliensDefeatedText;
            PersistentDataManager.Instance.OnUpdateTimeSurvived -= UpdateTimeSurvivedText;
        }

        private void OnCancelButton(InputAction.CallbackContext obj)
        {
            _onCancelAction?.Invoke();
        }

        private void ToggleActiveDisplay()
        {
            var displays = new List<DisplayInfo>();
            Screen.GetDisplayLayout(displays);

            var mainWindowInfo = Screen.mainWindowDisplayInfo;

            var displayIndex = 0;
            for (var i = 0; i < displays.Count; i++)
            {
                var display = displays[i];
                if (mainWindowInfo.Equals(display))
                {
                    Debug.Log(i);
                    displayIndex = i;
                    break;
                }
            }

            displayIndex += 1;
            displayIndex %= displays.Count;

            var newDisplayInfo = displays[displayIndex];
            Screen.MoveMainWindowTo(in newDisplayInfo, Vector2Int.zero);
        }

        private void ToggleFullScreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            var displayInfo = Screen.mainWindowDisplayInfo;
            Screen.MoveMainWindowTo(in displayInfo, Vector2Int.zero);
        }

        private void OnButtonPlay()
        {
            _characterSelectPanel.SetActive(true);
            _quitButton.gameObject.SetActive(false);

            _characterSelectPanelUIController.ShowCharacterSelectPanel();

            _onCancelAction = OnButtonCharacterSelectBack;
        }

        private void OnButtonAbout()
        {
            _aboutPanel.SetActive(true);
            _quitButton.gameObject.SetActive(false);
            _aboutCloseButton.Select();

            _onCancelAction = OnButtonAboutClose;
        }

        private void OnButtonOptions()
        {
            _optionsPanel.SetActive(true);
            _musicVolumeSlider.value = MainMenuAudioController.Instance.GetNormalizedMusicLevel();
            _sfxVolumeSlider.value = MainMenuAudioController.Instance.GetNormalizedSfxLevel();
            _confirmDeleteDataPanel.SetActive(false);
            _quitButton.gameObject.SetActive(false);
            _toggleDisplayButton.Select();

            _onCancelAction = OnButtonOptionsClose;
        }

        private void OnButtonQuit()
        {
            _quitButton.gameObject.SetActive(false);
            _playButton.gameObject.SetActive(false);
            _aboutButton.gameObject.SetActive(false);
            _optionsButton.gameObject.SetActive(false);
            _confirmQuitPanel.SetActive(true);
            _cancelQuitButton.Select();

            _onCancelAction = OnButtonCancelQuit;
        }

        private void OnButtonConfirmQuit()
        {
#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }

        private void OnButtonCancelQuit()
        {
            _quitButton.gameObject.SetActive(true);
            _playButton.gameObject.SetActive(true);
            _aboutButton.gameObject.SetActive(true);
            _optionsButton.gameObject.SetActive(true);
            _confirmQuitPanel.SetActive(false);
            _playButton.Select();

            _onCancelAction = null;
        }

        private void OnButtonAboutClose()
        {
            _aboutPanel.SetActive(false);
            _quitButton.gameObject.SetActive(true);
            _playButton.Select();

            _onCancelAction = null;
        }

        private void OnButtonTurboMakesGamesYouTube()
        {
            Application.OpenURL("https://www.youtube.com/@turbomakesgames");
        }

        private void OnButtonCharacterSelectNext()
        {
            _characterSelectPanel.SetActive(false);
            _stageSelectPanel.SetActive(true);
            _characterSelectPanelUIController.HideCharacterSelectPanel();
            _stageSelectPanelUIController.ShowStageSelectPanel();

            _onCancelAction = OnButtonStageSelectBack;
        }

        private void OnButtonCharacterSelectBack()
        {
            _characterSelectPanel.SetActive(false);
            _quitButton.gameObject.SetActive(true);
            _characterSelectPanelUIController.HideCharacterSelectPanel();
            _playButton.Select();

            _onCancelAction = null;
        }

        private void OnButtonStageSelectGo()
        {
            _onCancelAction = null;

            LoadingScreenUIController.Instance.ShowLoadingScreen();
            var stageSceneIndex = _stageSelectPanelUIController.SelectedStageSceneIndex;
            SceneManager.LoadSceneAsync(stageSceneIndex);
        }

        private void OnButtonArtTestScene()
        {
            _onCancelAction = null;

            LoadingScreenUIController.Instance.ShowLoadingScreen();
            SceneManager.LoadSceneAsync(ART_TEST_SCENE_INDEX);
        }

        private void OnButtonStageSelectBack()
        {
            _stageSelectPanel.SetActive(false);
            _characterSelectPanel.SetActive(true);
            _characterSelectPanelUIController.ShowCharacterSelectPanel();
            _stageSelectPanelUIController.HideStageSelectPanel();

            _onCancelAction = OnButtonCharacterSelectBack;
        }

        private void OnButtonOptionsClose()
        {
            _optionsPanel.SetActive(false);
            _quitButton.gameObject.SetActive(true);
            _playButton.Select();

            _onCancelAction = null;
        }

        private void OnSliderMusicVolume(float level)
        {
            MainMenuAudioController.Instance.SetMusicVolume(level);
        }

        private void OnSliderSfxVolume(float level)
        {
            MainMenuAudioController.Instance.SetSfxVolume(level);
            MainMenuAudioController.Instance.PlaySfxAudioClip(_onSfxVolumeChangeAudioClip, (int)AudioPriority.High);
        }

        private void OnButtonDeleteData()
        {
            _confirmDeleteDataPanel.SetActive(true);
            _cancelDeleteDataButton.Select();

            _onCancelAction = OnButtonCancelDeleteData;
        }

        private void OnButtonConfirmDeleteData()
        {
            PersistentDataManager.Instance.DeleteData();
            MainMenuAudioController.Instance.SetMusicVolume(1f);
            MainMenuAudioController.Instance.SetSfxVolume(1f);
            _optionsPanel.SetActive(false);
            _quitButton.gameObject.SetActive(true);
            _playButton.Select();

            _onCancelAction = null;
        }

        private void OnButtonCancelDeleteData()
        {
            _confirmDeleteDataPanel.SetActive(false);
            _toggleDisplayButton.Select();

            _onCancelAction = OnButtonOptionsClose;
        }

        private void UpdateCoinCountText(int coinCount)
        {
            _coinCountText.text = $"${coinCount:N0}";
        }

        private void UpdateAliensDefeatedText(int aliensDefeated)
        {
            _aliensDefeatedText.text = $"{aliensDefeated:N0}";
        }

        private void UpdateTimeSurvivedText(float secondsSurvived)
        {
            var time = TimeSpan.FromSeconds(secondsSurvived);
            var totalHours = (int)time.TotalHours;
            _timeSurvivedText.text = $"{totalHours}:{time.Minutes:D2}:{time.Seconds:D2}";
        }
    }
}