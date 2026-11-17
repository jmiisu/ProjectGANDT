
using UnityEngine;

namespace GANDT
{
    public class GANDT_WallOccluder : MonoBehaviour
    {
        [Header("Occlusion Renderers")]
        [Tooltip("페이드 효과를 적용할 Renderer들입니다.")]
        [SerializeField]
        private Renderer[] targetRenderers;

        [Header("Dither Fade Settings")]
        [SerializeField, Min(0f)]
        private float fadeOutDuration = 0.25f;

        [SerializeField, Min(0f)]
        private float fadeInDuration = 0.4f;

        [Header("Debug")]
        [SerializeField]
        private bool enableDebugLog = false;

        private static readonly int OcclusionFadeID =
            Shader.PropertyToID("_OcclusionFade");

        private MaterialPropertyBlock propertyBlock;

        private bool isOccluded;
        private float currentFade;
        private float targetFade;

        public bool IsOccluded => isOccluded;
        public float CurrentFade => currentFade;

        private void Reset()
        {
            FindRenderersIfNeeded();
        }

        private void Awake()
        {
            FindRenderersIfNeeded();

            propertyBlock = new MaterialPropertyBlock();

            currentFade = 0f;
            targetFade = 0f;

            ApplyFade();
            ValidateMaterials();
        }

        private void OnValidate()
        {
            fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
            fadeInDuration = Mathf.Max(0f, fadeInDuration);
        }

        private void Update()
        {
            if (Mathf.Approximately(currentFade, targetFade))
            {
                return;
            }

            float duration = isOccluded
                ? fadeOutDuration
                : fadeInDuration;

            if (duration <= 0f)
            {
                currentFade = targetFade;
            }
            else
            {
                currentFade = Mathf.MoveTowards(
                    currentFade,
                    targetFade,
                    Time.deltaTime / duration
                );
            }

            ApplyFade();
        }

        private void FindRenderersIfNeeded()
        {
            if (targetRenderers != null &&
                targetRenderers.Length > 0)
            {
                return;
            }

            targetRenderers =
                GetComponentsInChildren<Renderer>(true);
        }

        public void SetOccluded(bool occluded)
        {
            if (isOccluded == occluded)
            {
                return;
            }

            isOccluded = occluded;
            targetFade = occluded ? 1f : 0f;

            if (enableDebugLog)
            {
                Debug.Log(
                    $"[WallOccluder] {gameObject.name} " +
                    $"{(occluded ? "Fade Out" : "Fade In")}",
                    this
                );
            }
        }

        private void ApplyFade()
        {
            if (targetRenderers == null)
            {
                return;
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            foreach (Renderer targetRenderer in targetRenderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                // 기존 PropertyBlock 값은 유지한다.
                targetRenderer.GetPropertyBlock(propertyBlock);

                propertyBlock.SetFloat(
                    OcclusionFadeID,
                    Mathf.Clamp01(currentFade)
                );

                targetRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void ValidateMaterials()
        {
            if (targetRenderers == null)
            {
                return;
            }

            foreach (Renderer targetRenderer in targetRenderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                foreach (Material material
                         in targetRenderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        continue;
                    }

                    if (!material.HasProperty(OcclusionFadeID))
                    {
                        Debug.LogWarning(
                            "[WallOccluder] " +
                            $"{targetRenderer.name}의 재질 " +
                            $"{material.name}에는 " +
                            "_OcclusionFade 속성이 없습니다. " +
                            "디더 페이드 지원 셰이더가 필요합니다.",
                            targetRenderer
                        );
                    }
                }
            }
        }

        private void OnDisable()
        {
            // 페이드 상태 복원
            isOccluded = false;
            currentFade = 0f;
            targetFade = 0f;

            ApplyFade();
        }
    }
}
