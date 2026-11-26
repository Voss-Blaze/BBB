using UnityEngine;
using UnityEngine.EventSystems;

namespace Lingjuan.UI
{
    /// <summary>按钮悬停/按下的缩放反馈，挂在需要交互反馈的按钮物体上。</summary>
    public class ButtonScaleEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public float hoverScale = 1.06f;
        public float pressScale = 1.1f;
        public float speed = 14f;

        private Vector3 _baseScale = Vector3.one;
        private bool _hovered;
        private bool _pressed;
        private float _current = 1f;
        private float _target = 1f;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        /// <summary>给按钮挂上缩放反馈，已存在时只更新参数。</summary>
        public static ButtonScaleEffect Attach(GameObject target, float hover = 1.04f, float press = 1.07f)
        {
            if (target == null)
            {
                return null;
            }

            ButtonScaleEffect effect = target.GetComponent<ButtonScaleEffect>();
            if (effect == null)
            {
                effect = target.AddComponent<ButtonScaleEffect>();
            }

            effect.hoverScale = hover;
            effect.pressScale = press;
            return effect;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            UpdateTarget();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
            UpdateTarget();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
            UpdateTarget();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
            UpdateTarget();
        }

        private void UpdateTarget()
        {
            _target = _pressed ? pressScale : (_hovered ? hoverScale : 1f);
        }

        private void Update()
        {
            if (Mathf.Approximately(_current, _target))
            {
                return;
            }

            _current = Mathf.Lerp(_current, _target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
            transform.localScale = _baseScale * _current;
        }

        private void OnDisable()
        {
            _hovered = false;
            _pressed = false;
            _current = 1f;
            _target = 1f;
            transform.localScale = _baseScale;
        }
    }
}
