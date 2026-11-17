using System.Collections;
using System.ComponentModel;
using UnityEngine;

namespace GANDT
{
    public class CutsceneLetterbox : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform topBar;
        [SerializeField] private RectTransform bottomBar;

        [Header("Settings")]
        [SerializeField, Min(0f)] private float targetHeight = 120f;
        [SerializeField, Min(0f)] private float duration = 0.25f;

        private Coroutine currentCoroutine;

        private void Awake()
        {
            SetBarHeightImmediate(0f);
            gameObject.SetActive(false);
        }
        
        public void Show()
        {
            gameObject.SetActive(true);
            StartTransition(targetHeight);
        }

        public void Hide()
        {
            StartTransition(0f);
        }

        private void StartTransition(float target)
        {
            if (currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
            }

            currentCoroutine = StartCoroutine(AnimateBars(target));
        }

        private IEnumerator AnimateBars(float target)
        {
            float start = GetCurrentHeight();

            if (Mathf.Approximately(duration, 0f))
            {
                SetBarHeightImmediate(target);

                if (Mathf.Approximately(target, 0f))
                {
                    gameObject.SetActive(false);
                }

                currentCoroutine = null;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);

                float height = Mathf.Lerp(start, target, t);
                SetBarHeightImmediate(height);

                yield return null;
            }

            SetBarHeightImmediate(target);

            if (Mathf.Approximately(target, 0f))
            {
                gameObject.SetActive(false);
            }

            currentCoroutine = null;
        }

        private float GetCurrentHeight()
        {
            return topBar.sizeDelta.y;
        }

        private void SetBarHeightImmediate(float height)
        {
            if (topBar != null)
            {
                Vector2 size = topBar.sizeDelta;
                size.y = height;
                topBar.sizeDelta = size;
            }

            if (bottomBar != null)
            {
                Vector2 size = bottomBar.sizeDelta;
                size.y = height;
                bottomBar.sizeDelta = size;
            }
        }
    }
}