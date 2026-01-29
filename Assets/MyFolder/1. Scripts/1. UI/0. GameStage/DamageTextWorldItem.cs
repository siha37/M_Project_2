using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace MyFolder._1._Scripts._1._UI._0._GameStage
{
    public sealed class DamageTextWorldItem : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Color HitColor = new Color(1f, 0.92f, 0.16f);
        [SerializeField] private Color CriticalColor = new Color(1f, 0.12f, 0.16f);
        [SerializeField] private Color ShieldColor = new Color(1f, 0.12f, 0.16f);
        [SerializeField] private Color HealColor = new Color(1f, 0.12f, 0.16f);

        private DamageTextWorldManager ownerManager;
        private Coroutine playRoutine;

        public void Initialize(DamageTextWorldManager manager)
        {
            ownerManager = manager;
            if (!rectTransform) rectTransform = GetComponent<RectTransform>();
            if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Play(float amount, DamageTextWorldManager.DamageType type, float duration, float riseDistance, float criticalScale)
        {
            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
            }

            Color textcolor = Color.white;
            switch (type)
            {
                case DamageTextWorldManager.DamageType.hit:
                    textcolor = HitColor;
                    break;
                case DamageTextWorldManager.DamageType.critical:
                    textcolor = CriticalColor;
                    break;
                case DamageTextWorldManager.DamageType.shield:
                    textcolor = ShieldColor;
                    break;
                case DamageTextWorldManager.DamageType.heal:
                    textcolor = HealColor;
                    break;
            }
            text.color = textcolor;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.richText = false;
            text.SetText("{0}", amount);

            canvasGroup.alpha = 1f;
            rectTransform.localScale = type == DamageTextWorldManager.DamageType.critical ? Vector3.one * criticalScale : Vector3.one;

            playRoutine = StartCoroutine(PlayAndDespawn(duration, riseDistance));
        }

        private IEnumerator PlayAndDespawn(float duration, float riseDistance)
        {
            float elapsed = 0f;
            Vector3 startPos = rectTransform.position;
            Vector3 endPos = startPos + new Vector3(0, 1, 0) * riseDistance;
            
            Vector3 startScale = rectTransform.localScale;
            Vector3 endScale = Vector3.zero;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rectTransform.position = Vector3.Lerp(startPos, endPos, t);
                rectTransform.localScale = Vector3.Lerp(startScale, endScale, t);
                canvasGroup.alpha = 1f - t;
                yield return null;
            }

            canvasGroup.alpha = 0f;
            playRoutine = null;
            ownerManager.Return(this);
        }
    }
}


