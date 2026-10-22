using System.Collections;
using UnityEngine;

namespace GANDT
{
    [RequireComponent(typeof(CanvasGroup))]
    public class GANDT_ScreenFader : MonoBehaviour
    {
        [Header("Fade Settings")]
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.35f;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.35f;

        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        public IEnumerator FadeOutCor()
        {
            yield return Fade(0f, 1f, fadeOutDuration);
        }

        public IEnumerator FadeInCor()
        {
            yield return Fade(1f, 0f, fadeInDuration);
        }

        private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
        {
            canvasGroup.blocksRaycasts = true;

            if (duration <= 0f)
            {
                canvasGroup.alpha = 0f;
            }
            else
            {
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;

                    float t = Mathf.Clamp01(elapsed / duration);

                    // 부드러운 페이드 곡선
                    t = t * t * (3f - 2f * t);

                    canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                    
                    yield return null;
                }

                canvasGroup.alpha = endAlpha;
            }
        }
    }
}
