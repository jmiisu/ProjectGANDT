using System.Collections;
using TMPro;
using UnityEngine;

namespace GANDT
{
    public class GANDT_DoorTrigger : MonoBehaviour, IInteractable
    {
        [Header("Manager")]
        [SerializeField]
        private GANDT_RoomManager roomManager;

        [Header("Room Transition")]
        [SerializeField]
        private GameObject destinationRoom;

        [SerializeField]
        private Transform destinationSpawn;

        [Header("Interaction")]
        [SerializeField]
        private string interactionText = "문 열기";

        [Header("Lock Settings")]
        [SerializeField]
        private bool isLocked = false;

        [SerializeField]
        private ItemData requiredKey;

        [SerializeField]
        private bool consumeKey = true;

        [Header("Messages")]
        [SerializeField]
        private GameObject messagePanel;

        [SerializeField]
        private TMP_Text messageText;

        [SerializeField]
        private string lockedMessage = "문이 잠겨 있다.";

        [SerializeField]
        private string unlockedMessage = "문이 열렸다.";

        [SerializeField, Min(0f)]
        private float messageDuration = 2f;

        [Header("Door Audio")]
        [SerializeField]
        private AudioClip doorOpenClip;

        [SerializeField]
        private AudioClip doorCloseClip;

        private Transform player;
        private GANDT_Inventory playerInventory;

        private bool isPlayerInside;

        private Coroutine messageCoroutine;

        public AudioClip DoorOpenClip => doorOpenClip;
        public AudioClip DoorCloseClip => doorCloseClip;

        public GameObject DestinationRoom => destinationRoom;
        public Transform DestinationSpawn => destinationSpawn;

        public bool IsLocked => isLocked;

        public string InteractionText =>
            isLocked ? "문 조사하기" : interactionText;

        public bool CanInteract =>
            isActiveAndEnabled &&
            isPlayerInside &&
            player != null &&
            roomManager != null &&
            roomManager.isActiveAndEnabled &&
            !roomManager.IsTransitioning &&
            destinationRoom != null &&
            destinationSpawn != null &&
            destinationRoom != roomManager.CurrentRoom;

        private void Awake()
        {
            HideMessage();
        }

        private void OnDisable()
        {
            player = null;
            playerInventory = null;
            isPlayerInside = false;

            if (messageCoroutine != null)
            {
                StopCoroutine(messageCoroutine);
                messageCoroutine = null;
            }

            HideMessage();
        }

        public void OnPlayerEnter(Transform playerTransform)
        {
            if (playerTransform == null)
            {
                return;
            }

            player = playerTransform;
            isPlayerInside = true;

            // 플레이어의 Inventory 컴포넌트 탐색
            playerInventory =
                playerTransform.GetComponentInParent<GANDT_Inventory>();

            if (playerInventory == null)
            {
                playerInventory =
                    playerTransform.GetComponentInChildren<GANDT_Inventory>();
            }

            Debug.Log("[DoorTrigger] 상호작용 가능", this);
        }

        public void OnPlayerExit(Transform playerTransform)
        {
            if (player != playerTransform)
            {
                return;
            }

            player = null;
            playerInventory = null;
            isPlayerInside = false;

            Debug.Log("[DoorTrigger] 상호작용 영역 이탈", this);
        }

        public void Interact()
        {
            if (!CanInteract)
            {
                return;
            }

            // 1. 잠긴 문이라면 열쇠 확인
            if (isLocked)
            {
                if (requiredKey == null)
                {
                    Debug.LogWarning(
                        "[DoorTrigger] 잠긴 문에 Required Key가 " +
                        "설정되지 않았습니다.",
                        this
                    );

                    ShowMessage(lockedMessage);
                    return;
                }

                if (playerInventory == null ||
                    !playerInventory.HasItem(requiredKey))
                {
                    ShowMessage(lockedMessage);

                    Debug.Log(
                        "[DoorTrigger] 필요한 열쇠가 없습니다.",
                        this
                    );

                    return;
                }

                // 열쇠가 있으면 방 전환 시도
                bool success = TryEnterRoom();

                if (!success)
                {
                    return;
                }

                // 방 전환 요청 성공 후 열쇠 소모
                if (consumeKey)
                {
                    bool removed =
                        playerInventory.TryRemoveItem(requiredKey, 1);

                    if (!removed)
                    {
                        Debug.LogWarning(
                            "[DoorTrigger] 열쇠 제거에 실패했습니다.",
                            this
                        );
                    }
                }

                // 문 잠금 해제 상태 유지
                isLocked = false;

                ShowMessage(unlockedMessage);

                Debug.Log(
                    $"[DoorTrigger] {gameObject.name} 잠금 해제",
                    this
                );

                return;
            }

            // 2. 잠기지 않은 일반 문
            TryEnterRoom();
        }

        private bool TryEnterRoom()
        {
            if (roomManager == null)
            {
                return false;
            }

            bool success = roomManager.TryChangeRoom(
                destinationRoom,
                destinationSpawn,
                doorOpenClip,
                doorCloseClip
            );

            if (success)
            {
                Debug.Log(
                    $"[DoorTrigger] {destinationRoom.name}으로 " +
                    "방 전환 시작",
                    this
                );
            }

            return success;
        }

        private void ShowMessage(string message)
        {
            if (messageCoroutine != null)
            {
                StopCoroutine(messageCoroutine);
                messageCoroutine = null;
            }

            // UI가 연결되지 않았을 경우 Console로 출력
            if (messagePanel == null || messageText == null)
            {
                Debug.Log($"[DoorTrigger] {message}", this);
                return;
            }

            messageText.text = message;
            messagePanel.SetActive(true);

            messageCoroutine =
                StartCoroutine(HideMessageRoutine());
        }

        private IEnumerator HideMessageRoutine()
        {
            if (messageDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    messageDuration
                );
            }
            else
            {
                yield return null;
            }

            HideMessage();
            messageCoroutine = null;
        }

        private void HideMessage()
        {
            if (messagePanel != null)
            {
                messagePanel.SetActive(false);
            }
        }

        // 추후 이벤트, 퍼즐 등의 조건으로
        // 문을 해제할 때 사용할 수 있는 함수
        public void UnlockDoor()
        {
            if (!isLocked)
            {
                return;
            }

            isLocked = false;

            Debug.Log(
                $"[DoorTrigger] {gameObject.name} 잠금 해제",
                this
            );
        }

        // 디버그 또는 게임 이벤트 전용
        public void LockDoor()
        {
            isLocked = true;

            Debug.Log(
                $"[DoorTrigger] {gameObject.name} 잠금 설정",
                this
            );
        }
    }
}
