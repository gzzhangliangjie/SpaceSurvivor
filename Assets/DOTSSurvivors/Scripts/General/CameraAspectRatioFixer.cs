using UnityEngine;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 用于在不支持的宽高比屏幕上添加黑边（letterboxing）的 MonoBehaviour 脚本。
    /// </summary>
    public class CameraAspectRatioFixer : MonoBehaviour
    {
        /// <summary>
        /// 最高的屏幕宽高比为 16:10
        /// </summary>
        private const float MIN_SUPPORTED_ASPECT_RATIO = 16f / 10f;
        /// <summary>
        /// 最宽的屏幕宽高比为 16:9
        /// </summary>
        private const float MAX_SUPPORTED_ASPECT_RATIO = 16f / 9f;

        /// <summary>
        /// 用于设置黑边的主摄像机。
        /// </summary>
        private Camera _camera;
        /// <summary>
        /// 用于检测宽高比何时发生变化，例如在显示器之间切换或全屏/窗口模式切换时。
        /// </summary>
        private float _previousAspectRatio;

        /// <summary>
        /// 初始化新摄像机以渲染黑边效果的黑色背景，然后设置初始黑边。
        /// </summary>
        private void Awake()
        {
            var letterboxCamera = new GameObject().AddComponent<Camera>();
            letterboxCamera.backgroundColor = Color.black;
            letterboxCamera.cullingMask = 0;
            letterboxCamera.depth = -100;
            letterboxCamera.farClipPlane = 1;
            letterboxCamera.useOcclusionCulling = false;
            letterboxCamera.allowHDR = false;
            letterboxCamera.allowMSAA = false;
            letterboxCamera.clearFlags = CameraClearFlags.Color;
            letterboxCamera.name = "Letterbox Camera";

            _camera = GetComponent<Camera>();
            _previousAspectRatio = (float)Screen.width / Screen.height;
            SetLetterboxing();
        }

        /// <summary>
        /// 检查宽高比的变化，并在需要时设置黑边。
        /// </summary>
        private void Update()
        {
            var currentAspectRatio = (float)Screen.width / Screen.height;
            if (Mathf.Abs(currentAspectRatio - _previousAspectRatio) > float.Epsilon)
            {
                _previousAspectRatio = currentAspectRatio;
                SetLetterboxing();
            }
        }

        /// <summary>
        /// 如果宽高比超出正常相机范围，则设置黑边。
        /// </summary>
        /// <remarks>
        /// 通过设置主相机的Rect来设置信箱模式。
        /// 基于这篇博客文章：https://gamedesigntheory.blogspot.com/2010/09/controlling-aspect-ratio-in-unity.html
        /// </remarks>
        private void SetLetterboxing()
        {
            if (_previousAspectRatio < MIN_SUPPORTED_ASPECT_RATIO)
            {
                var scaleHeight = _previousAspectRatio / MIN_SUPPORTED_ASPECT_RATIO;
                _camera.rect = new Rect
                {
                    x = 0f,
                    y = (1f - scaleHeight) / 2f,
                    width = 1f,
                    height = scaleHeight
                };
            }
            else if (_previousAspectRatio > MAX_SUPPORTED_ASPECT_RATIO)
            {
                var scaleWidth = 1f / (_previousAspectRatio / MAX_SUPPORTED_ASPECT_RATIO);
                _camera.rect = new Rect
                {
                    x = (1f - scaleWidth) / 2f,
                    y = 0f,
                    width = scaleWidth,
                    height = 1f
                };
            }
            else
            {
                _camera.rect = new Rect
                {
                    x = 0f,
                    y = 0f,
                    width = 1f,
                    height = 1f
                };
            }
        }
    }
}