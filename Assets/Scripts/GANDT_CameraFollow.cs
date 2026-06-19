using UnityEngine;

/// <summary>
/// GANDT 시연용 카메라 팔로우 스크립트.
///
/// 역할:
/// - 플레이어를 일정한 오프셋으로 따라가며 3인칭 시점 카메라를 구성한다.
/// - LateUpdate에서 이동/회전을 처리하여, 플레이어 이동이 끝난 뒤 카메라가 따라오도록 한다.
/// - 영상처리 후처리 셰이더가 안정적인 화면을 입력으로 받을 수 있도록 카메라 움직임을 부드럽게 만든다.
/// </summary>
public class GANDT_CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("카메라가 따라갈 대상. 일반적으로 Player Transform을 연결한다.")]
    [SerializeField] private Transform target;

    [Header("Follow Offset")]
    [Tooltip("플레이어 위치 기준 카메라가 떨어져 있을 상대 위치.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 12f, -10f);

    [Header("Follow Settings")]
    [Tooltip("true이면 SmoothDamp를 사용해 부드럽게 따라가고, false이면 즉시 목표 위치로 이동한다.")]
    [SerializeField] private bool useSmoothFollow = true;

    [Tooltip("카메라가 목표 위치에 도달하는 데 걸리는 완충 시간. 값이 작을수록 즉각적으로 따라간다.")]
    [SerializeField] private float smoothTime = 0.15f;

    // SmoothDamp 내부에서 사용하는 현재 속도값.
    // 프레임 간 이동을 자연스럽게 이어주기 위해 ref로 전달된다.
    private Vector3 velocity;

    private void Reset()
    {
        // 컴포넌트를 처음 붙였을 때 Player 태그 오브젝트를 자동으로 찾아 편의성을 높인다.
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            target = playerObject.transform;
        }
    }

    private void OnValidate()
    {
        // smoothTime이 음수가 되면 SmoothDamp가 의도와 다르게 동작할 수 있으므로 최소 0으로 제한한다.
        smoothTime = Mathf.Max(0f, smoothTime);
    }

    private void LateUpdate()
    {
        // 플레이어가 연결되지 않은 경우 카메라 제어를 중단한다.
        if (target == null)
        {
            return;
        }

        FollowTarget();
    }

    private void FollowTarget()
    {
        // 플레이어의 월드 좌표에 offset을 더해 카메라의 목표 위치를 만든다.
        Vector3 targetPosition = target.position + offset;

        if (useSmoothFollow)
        {
            // 갑작스러운 카메라 이동은 후처리 화면 왜곡과 겹쳐 보기 불편할 수 있으므로 부드럽게 보간한다.
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref velocity,
                smoothTime
            );
        }
        else
        {
            // 디버그 상황에서 즉각적인 위치 반영이 필요할 때 사용할 수 있다.
            transform.position = targetPosition;
        }
    }
}
