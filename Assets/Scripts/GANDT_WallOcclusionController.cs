using System.Collections.Generic;
using UnityEngine;

namespace GANDT
{
    public class GANDT_WallOcclusionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Transform player;

        [Header("Detection")]
        [Tooltip("벽 가림 검사용 Layer입니다. Occluder Layer만 지정하는 것을 권장합니다.")]
        [SerializeField]
        private LayerMask occluderMask;

        [Tooltip("플레이어 위치에서 어느 높이를 바라볼지 설정합니다.")]
        [SerializeField]
        private float playerTargetHeight = 1.2f;

        [Tooltip("Raycast 대신 SphereCast를 사용할 때의 반경입니다.")]
        [SerializeField, Min(0.01f)]
        private float castRadius = 0.25f;

        [Header("Debug")]
        [SerializeField]
        private bool drawDebugRay = true;

        private readonly HashSet<GANDT_WallOccluder>
            currentOccluders =
                new HashSet<GANDT_WallOccluder>();

        private readonly HashSet<GANDT_WallOccluder>
            detectedOccluders =
                new HashSet<GANDT_WallOccluder>();

        private readonly List<GANDT_WallOccluder>
            restoreBuffer =
                new List<GANDT_WallOccluder>();

        private void Awake()
        {
            FindPlayerIfNeeded();
        }

        private void LateUpdate()
        {
            if (player == null)
            {
                FindPlayerIfNeeded();

                if (player == null)
                {
                    return;
                }
            }

            UpdateOcclusion();
        }

        private void FindPlayerIfNeeded()
        {
            if (player != null)
            {
                return;
            }

            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        private void UpdateOcclusion()
        {
            Vector3 cameraPosition = transform.position;

            Vector3 playerTargetPosition =
                player.position +
                Vector3.up * playerTargetHeight;

            Vector3 direction =
                playerTargetPosition -
                cameraPosition;

            float distance = direction.magnitude;

            if (distance <= Mathf.Epsilon)
            {
                RestoreAllOccluders();
                return;
            }

            direction /= distance;

            detectedOccluders.Clear();

            RaycastHit[] hits =
                Physics.SphereCastAll(
                    cameraPosition,
                    castRadius,
                    direction,
                    distance,
                    occluderMask,
                    QueryTriggerInteraction.Ignore
                );

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null)
                {
                    continue;
                }

                GANDT_WallOccluder occluder =
                    hit.collider.GetComponentInParent<
                        GANDT_WallOccluder>();

                if (occluder == null)
                {
                    continue;
                }

                detectedOccluders.Add(occluder);
            }

            HideDetectedOccluders();
            RestoreUndetectedOccluders();

            if (drawDebugRay)
            {
                Debug.DrawLine(
                    cameraPosition,
                    playerTargetPosition,
                    hits.Length > 0
                        ? Color.red
                        : Color.green
                );
            }
        }

        private void HideDetectedOccluders()
        {
            foreach (GANDT_WallOccluder occluder
                     in detectedOccluders)
            {
                if (occluder == null)
                {
                    continue;
                }

                occluder.SetOccluded(true);
                currentOccluders.Add(occluder);
            }
        }

        private void RestoreUndetectedOccluders()
        {
            restoreBuffer.Clear();

            foreach (GANDT_WallOccluder occluder
                     in currentOccluders)
            {
                if (occluder == null)
                {
                    restoreBuffer.Add(occluder);
                    continue;
                }

                if (!detectedOccluders.Contains(occluder))
                {
                    occluder.SetOccluded(false);
                    restoreBuffer.Add(occluder);
                }
            }

            foreach (GANDT_WallOccluder occluder
                     in restoreBuffer)
            {
                currentOccluders.Remove(occluder);
            }
        }

        private void RestoreAllOccluders()
        {
            foreach (GANDT_WallOccluder occluder
                     in currentOccluders)
            {
                if (occluder != null)
                {
                    occluder.SetOccluded(false);
                }
            }

            currentOccluders.Clear();
            detectedOccluders.Clear();
        }

        private void OnDisable()
        {
            RestoreAllOccluders();
        }

        private void OnDrawGizmosSelected()
        {
            if (player == null)
            {
                return;
            }

            Vector3 targetPosition =
                player.position +
                Vector3.up * playerTargetHeight;

            Gizmos.DrawWireSphere(
                targetPosition,
                castRadius
            );
        }
    }
}