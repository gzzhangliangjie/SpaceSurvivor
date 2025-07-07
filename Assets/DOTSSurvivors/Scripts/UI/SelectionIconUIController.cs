using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.InputSystem;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 与显示在当前高亮元素两侧的选择图标关联的UI控制器。
    /// </summary>
    /// <remarks>
    /// Primarily implemented for the ability to navigate UI menus using a controller or the keyboard with no mouse.
    /// </remarks>
    public class SelectionIconUIController : MonoBehaviour
    {
        public static SelectionIconUIController Instance;

        [SerializeField] private GameObject _baseSelectionElement;
        [SerializeField] private GameObject _leftSelectionElement;
        [SerializeField] private GameObject _rightSelectionElement;
        [SerializeField] private float _padding;
        [SerializeField] private Selectable _offscreenSelectionElement;
        [SerializeField] private float _mouseReturnDelta;

        private EventSystem _eventSystem;
        private GameObject _previouslySelectedElement;
        private RectTransform _baseRectTransform;
        private RectTransform _leftRectTransform;
        private RectTransform _rightRectTransform;
        private Image _leftImage;
        private Image _rightImage;
        private DOTSSurvivorsInputActions _inputActions;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _baseRectTransform = _baseSelectionElement.GetComponent<RectTransform>();
            _leftRectTransform = _leftSelectionElement.GetComponent<RectTransform>();
            _rightRectTransform = _rightSelectionElement.GetComponent<RectTransform>();
            _leftImage = _leftSelectionElement.GetComponent<Image>();
            _rightImage = _rightSelectionElement.GetComponent<Image>();
            SetPositionOffscreen();
            _inputActions = new DOTSSurvivorsInputActions();
        }

        private void Start()
        {
            _eventSystem = EventSystem.current;
        }

        private void OnEnable()
        {
            _inputActions.UI.Navigate.performed += HideCursor;
            _inputActions.Enable();
        }

        private void OnDisable()
        {
            _inputActions.UI.Navigate.performed -= HideCursor;
            _inputActions.Disable();
        }

        /// <summary>
        /// 当使用控制器或键盘导航输入时自动隐藏光标。Update函数将检查鼠标移动，如果玩家移动鼠标则重新启用。
        /// </summary>
        private void HideCursor(InputAction.CallbackContext obj)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            var currentSelectedElement = _eventSystem.currentSelectedGameObject;
            if (currentSelectedElement != _previouslySelectedElement)
            {
                _previouslySelectedElement = currentSelectedElement;
                StartCoroutine(UpdateSelectionIconPosition());
            }

            var mouseDelta = _inputActions.UI.MouseMovement.ReadValue<Vector2>();
            var deltaMagnitude = mouseDelta.magnitude;
            if (deltaMagnitude > _mouseReturnDelta)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        /// <summary>
        /// 需要1帧延迟，因为某些元素如果是布局组的一部分，不会立即处于正确位置
        /// </summary>
        private IEnumerator UpdateSelectionIconPosition()
        {
            _leftImage.enabled = false;
            _rightImage.enabled = false;
            yield return null;
            var selectedElementTransform = _previouslySelectedElement.GetComponent<RectTransform>();
            var pivot = selectedElementTransform.pivot;
            var rect = selectedElementTransform.rect;
            var localCenter = new Vector3((0.5f - pivot.x) * rect.width, (0.5f - pivot.y) * rect.height, 0f);
            var worldCenter = selectedElementTransform.TransformPoint(localCenter);
            SetPosition(worldCenter, rect.width);
            _baseSelectionElement.SetActive(true);
            _leftImage.enabled = true;
            _rightImage.enabled = true;
        }

        private void SetPosition(Vector3 position, float width)
        {
            _baseRectTransform.position = position;
            width += _padding;
            _leftRectTransform.localPosition = new Vector3(-0.5f * width, 0f, 0f);
            _rightRectTransform.localPosition = new Vector3(0.5f * width, 0f, 0f);
        }

        public void SetPositionOffscreen()
        {
            _leftImage.enabled = false;
            _rightImage.enabled = false;
            _offscreenSelectionElement.Select();
        }
    }
}