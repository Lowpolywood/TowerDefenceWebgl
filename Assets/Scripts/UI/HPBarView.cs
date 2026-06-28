using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class HPBarView : MonoBehaviour // or some base UIEntity in case of complete UI system
    {
        [SerializeField] Animation anim;
        [SerializeField] RectTransform fill;
        [SerializeField] RectTransform shadow;
        [SerializeField] Gradient hpColor;

        [SerializeField][Range(0, 1)] float value = 1f;
        public float Value
        {
            get => value;
            set
            {
                value = Mathf.Clamp01(value);

                if (value != this.value)
                {
                    this.value = value;
                    OnValueChanged?.Invoke();
                }
            }
        }

        public Action OnValueChanged { get; set; }

        [Header("Speeds")]
        [SerializeField] float fillSpeed = 15f;
        [SerializeField] float shadowSpeed = 5f;

        float maxWidth;
        float fillWidth;
        float shadowWidth;

        private Image fillImage;

        void Start()
        {
            maxWidth = fill.rect.width;

            fillWidth = maxWidth;
            shadowWidth = maxWidth;

            if (fill != null)
                fillImage = fill.GetComponent<Image>();

            OnValueChanged += () =>
            {
                if (anim)
                    anim.Play();
            };

            UpdateColor();
        }

        void LateUpdate()
        {
            fillWidth = Mathf.Lerp(fillWidth, maxWidth * Mathf.Clamp01(Value), Time.deltaTime * fillSpeed);
            shadowWidth = Mathf.Lerp(shadowWidth, fillWidth, Time.deltaTime * shadowSpeed);

            SetWidth(fill, fillWidth);
            SetWidth(shadow, shadowWidth);

            UpdateColor();
        }

        private void SetWidth(RectTransform rt, float width)
        {
            var size = rt.sizeDelta;
            size.x = width;
            rt.sizeDelta = size;
        }

        private void UpdateColor()
        {
            if (fillImage != null && hpColor != null && maxWidth > 0)
            {
                float currentProgress = fillWidth / maxWidth;
                fillImage.color = hpColor.Evaluate(currentProgress);
            }
        }
    }
}