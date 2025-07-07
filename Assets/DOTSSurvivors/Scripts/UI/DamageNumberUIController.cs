using UnityEngine;
using System.Collections;
using TMPro;

namespace Lemo.SpaceSurvivors
{
    /// <summary>
    /// 与在敌人角色受到伤害时在其上方显示的伤害图标关联的UI控制器。
    /// </summary>
    /// <remarks>
    /// Damage text elements are pooled in <see cref="WorldUICanvasController"/>.
    /// </remarks>
    public class DamageNumberUIController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _damageText;

        private const float EFFECT_TIME = 0.75f;

        private readonly Vector3 _movementAmount = new(0f, 0f, 0.25f);

        public void DisplayDamageNumber(int damageValue, bool isHealing)
        {
            var isHealingColor = isHealing ? "<color=green>+" : "";
            var colorClosingTag = isHealing ? "</color>" : "";
            _damageText.text = $"{isHealingColor}{Mathf.Abs(damageValue)}{colorClosingTag}";

            StartCoroutine(DamageNumberLifecycle());
        }

        private IEnumerator DamageNumberLifecycle()
        {
            var timer = 0f;
            var startPosition = transform.position;
            var endPosition = startPosition + _movementAmount;

            while (timer < EFFECT_TIME)
            {
                var t = timer / EFFECT_TIME;

                transform.position = Vector3.Lerp(startPosition, endPosition, t);
                var textColor = Color.Lerp(Color.white, Color.clear, t);
                _damageText.color = textColor;

                yield return null;
                timer += Time.deltaTime;
            }
            _damageText.text = "";
            WorldUICanvasController.Instance.ReturnDamageNumberToPool(this);
        }
    }
}