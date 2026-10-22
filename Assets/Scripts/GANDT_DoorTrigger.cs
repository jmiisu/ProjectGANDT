using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GANDT
{
    public class GANDT_DoorTrigger : MonoBehaviour
    {
        [Header("Manager")]
        [SerializeField] private GANDT_RoomManager roomManager;

        [Header("Room Transition")]
        [SerializeField] private GameObject destinationRoom;
        [SerializeField] private Transform destinationSpawn;

        [Header("Interaction")]
        [SerializeField] private Key interactionKey = Key.E;
        [SerializeField] private GameObject interactionUI;

        [Header("Door Audio")]
        [SerializeField] private AudioClip doorOpenClip;
        [SerializeField] private AudioClip doorCloseClip;

        public AudioClip DoorOpenClip => doorOpenClip;
        public AudioClip DoorCloseClip => doorCloseClip;

        private Transform player;
        private bool isPlayerInside;

        public GameObject DestinationRoom => destinationRoom;
        public Transform DestinationSpawn => destinationSpawn;

        private void Awake()
        {
            SetInteractionUI(false);
        }

        private void OnDisable()
        {
            player = null;
            isPlayerInside = false;
            SetInteractionUI(false);
        }

        private void Update()
        {
            if (!isPlayerInside)
            {
                return;
            }

            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current[interactionKey].wasPressedThisFrame)
            {
                Interact();
            }
        }

        public void OnPlayerEnter(Transform playerTransform)
        {
            player = playerTransform;
            isPlayerInside = true;

            SetInteractionUI(true);
            Debug.Log("[DoorTrigger] 상호작용 가능");
        }

        public void OnPlayerExit(Transform playerTransform)
        {
            if (player != playerTransform)
            {
                return;
            }

            player = null;
            isPlayerInside = false;

            SetInteractionUI(false);
            Debug.Log($"[DoorTrigger] 상호작용 영역 이탈");
        }

        private void Interact()
        {
            if (roomManager == null)
            {
                Debug.LogWarning("[DoorTrigger] RoomManager가 지정되지 않았습니다.", this);
                return;
            }

            if (destinationRoom == null || destinationSpawn == null)
            {
                Debug.LogWarning("[DoorTrigger] Destination Room 또는 Spawn이 지정되지 않았습니다.", this);
                return;
            }

            bool success = roomManager.TryChangeRoom(destinationRoom, destinationSpawn, doorOpenClip, doorCloseClip);

            if (success)
            {
                Debug.Log($"[DoorTrigger] {destinationRoom.name}으로 이동 완료");

                // TODO:
                // GANDT_RoomManager 구현 후
                // 실제 방 전환 기능과 연결
            }
        }

        private void SetInteractionUI(bool onoff)
        {
            interactionUI.SetActive(onoff);
        }
    }
}
