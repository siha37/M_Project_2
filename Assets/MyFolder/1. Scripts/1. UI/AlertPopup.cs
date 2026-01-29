using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MyFolder._1._Scripts._3._SingleTone;

namespace MyFolder._1._Scripts._1._UI
{
    /// <summary>
    /// 경고창 팝업 UI 컴포넌트
    /// AlertManager에 의해 생성되고 관리됨
    /// </summary>
    public class AlertPopup : MonoBehaviour
    {
        [Header("UI 요소")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundPanel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("아이콘 색상")]
        [SerializeField] private Color infoColor = new Color(0.2f, 0.6f, 1f);      // 파란색
        [SerializeField] private Color warningColor = new Color(1f, 0.8f, 0.2f);   // 노란색
        [SerializeField] private Color errorColor = new Color(1f, 0.3f, 0.3f);     // 빨간색
        [SerializeField] private Color successColor = new Color(0.3f, 0.9f, 0.4f); // 초록색

        [Header("애니메이션 설정")]
        [SerializeField] private float fadeInDuration = 0.2f;
        [SerializeField] private float fadeOutDuration = 0.15f;
        [SerializeField] private bool useScaleAnimation = true;
        [SerializeField] private float scaleAnimationDuration = 0.3f;

        private Action onConfirmCallback;
        private Action onCloseCallback;
        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            
            
            // 확인 버튼 리스너 등록
            if (confirmButton)
            {
                confirmButton.onClick.AddListener(OnConfirmClicked);
            }
        }

        /// <summary>
        /// 경고창 표시
        /// </summary>
        public void Show(AlertData data, Action onClose)
        {
            // 텍스트 설정
            if (titleText)
                titleText.text = data.title;
            
            if (messageText)
                messageText.text = data.message;

            onConfirmCallback = data.onConfirm;
            onCloseCallback = onClose;

            // 타입에 따른 색상 변경
            Color typeColor = GetColorForType(data.type);
            
            if (iconImage != null)
                iconImage.color = typeColor;

            // 배경 패널 약간의 색조 적용 (선택적)
            if (backgroundPanel != null)
            {
                Color bgColor = backgroundPanel.color;
                bgColor = Color.Lerp(bgColor, typeColor, 0.1f);
                backgroundPanel.color = bgColor;
            }

            // 애니메이션 시작
            if (useScaleAnimation)
            {
                StartCoroutine(FadeInWithScale());
            }
            else
            {
                StartCoroutine(FadeIn());
            }
        }

        private Color GetColorForType(AlertManager.AlertType type)
        {
            return type switch
            {
                AlertManager.AlertType.Info => infoColor,
                AlertManager.AlertType.Warning => warningColor,
                AlertManager.AlertType.Error => errorColor,
                AlertManager.AlertType.Success => successColor,
                _ => infoColor
            };
        }

        private void OnConfirmClicked()
        {
            onConfirmCallback?.Invoke();
            StartCoroutine(FadeOutAndDestroy());
        }

        /// <summary>
        /// 외부에서 알람 즉시 닫기 (콜백 호출 없음)
        /// </summary>
        public void Close()
        {
            StopAllCoroutines();
            Destroy(gameObject);
            onCloseCallback?.Invoke();
        }

        private IEnumerator FadeIn()
        {
            canvasGroup.alpha = 0f;
            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        private IEnumerator FadeInWithScale()
        {
            canvasGroup.alpha = 0f;
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.zero;
            }

            float elapsed = 0f;
            float duration = Mathf.Max(fadeInDuration, scaleAnimationDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                
                // Ease Out Back 효과
                float scale = EaseOutBack(t);
                
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                
                if (rectTransform != null)
                {
                    rectTransform.localScale = Vector3.one * scale;
                }
                
                yield return null;
            }

            canvasGroup.alpha = 1f;
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
            }
        }

        private IEnumerator FadeOutAndDestroy()
        {
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / fadeOutDuration;
                
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                
                if (useScaleAnimation && rectTransform)
                {
                    rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.8f, t);
                }
                
                yield return null;
            }

            onCloseCallback?.Invoke();
            Destroy(gameObject);
        }

        // Ease Out Back 함수 (통통 튀는 효과)
        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        #region 인스펙터 테스트

        [ContextMenu("테스트: 경고창 표시")]
        private void TestShow()
        {
            var testData = new AlertData
            {
                title = "테스트 제목",
                message = "이것은 테스트 메시지입니다.\n여러 줄도 표시할 수 있습니다.",
                type = AlertManager.AlertType.Info,
                onConfirm = () => Debug.Log("확인 버튼 클릭됨")
            };
            
            Show(testData, () => Debug.Log("경고창 닫힘"));
        }

        #endregion
    }
}

