using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._0._Agent
{
    public class PlayerProgressControll : MonoBehaviour
    {
        [SerializeField] private Image frontImage;
        [SerializeField] private Image backImage;
        [SerializeField] private TextMeshProUGUI progressText;

        public void ProgressStart(string text)
        {
            frontImage.fillAmount = 0;
            progressText.text = text;
            StopAllCoroutines();
            StartCoroutine(FadeIn(frontImage, 1));
            StartCoroutine(FadeIn(backImage, 1));
            StartCoroutine(FadeIn(progressText, 1));
        }

        public void ProgressEnd()
        {
            progressText.text = "";
            StopAllCoroutines();
            StartCoroutine(FadeOut(frontImage, 1));
            StartCoroutine(FadeOut(backImage, 1));
            StartCoroutine(FadeOut(progressText, 1));
        }

        public void ProgressUpdate(float progress)
        {
            frontImage.fillAmount = Mathf.Clamp01(progress);
        }
        
        private IEnumerator FadeIn(Image image, float duration)
        {
            float time = duration / 30;
            float startAlpha = image.color.a;
            float p = 0;
            float pp = 0.035f;
            for (int i = 0; i < 30; i++)
            {
                p += pp;
                yield return new WaitForSeconds(time);
                float alpha = Mathf.Lerp(startAlpha, 1, p);
                image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
            }
        }
        private IEnumerator FadeOut(Image image, float duration)
        {
            float time = duration / 30;
            float startAlpha = image.color.a;
            float p = 0;
            float pp = 0.035f;
            for (int i = 0; i < 30; i++)
            {
                p += pp;
                yield return new WaitForSeconds(time);
                float alpha = Mathf.Lerp(startAlpha, 0, p);
                image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
            }
        }
        private IEnumerator FadeIn(TextMeshProUGUI text, float duration)
        {
            float time = duration / 30;
            float startAlpha = text.color.a;
            float p = 0;
            float pp = 0.035f;
            for (int i = 0; i < 30; i++)
            {
                p += pp;
                yield return new WaitForSeconds(time);
                float alpha = Mathf.Lerp(startAlpha, 1, p);
                text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);
            }
        }
        private IEnumerator FadeOut(TextMeshProUGUI text, float duration)
        {
            float time = duration / 30;
            float startAlpha = text.color.a;
            float p = 0;
            float pp = 0.035f;
            for (int i = 0; i < 30; i++)
            {
                p += pp;
                yield return new WaitForSeconds(time);
                float alpha = Mathf.Lerp(startAlpha, 0, p);
                text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);
            }
        }
    }
}
