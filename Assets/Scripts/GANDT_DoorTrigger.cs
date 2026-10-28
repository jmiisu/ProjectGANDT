using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GANDT
{
    public class GANDT_DoorTrigger : MonoBehaviour, IInteractable
    {
        [Header("Manager")]
        [SerializeField] private GANDT_RoomManager roomManager;

        [Header("Room Transition")]
        [SerializeField] private GameObject destinationRoom;
        [SerializeField] private Transform destinationSpawn;

        [Header("Interaction")]
        //[SerializeField] private Key interactionKey = Key.E;
        [SerializeField] private string interactionText;

        [Header("Door Audio")]
        [SerializeField] private AudioClip doorOpenClip;
        [SerializeField] private AudioClip doorCloseClip;

        public AudioClip DoorOpenClip => doorOpenClip;
        public AudioClip DoorCloseClip => doorCloseClip;

        private Transform player;
        private bool isPlayerInside;

        public GameObject DestinationRoom => destinationRoom;
        public Transform DestinationSpawn => destinationSpawn;

        // IInteractable 구현
        public string InteractionText => interactionText;

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

        private void OnDisable()
        {
            player = null;
            isPlayerInside = false;
        }

        public void OnPlayerEnter(Transform playerTransform)
        {
            if (playerTransform == null)
            {
                return;
            }

            player = playerTransform;
            isPlayerInside = true;

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

            Debug.Log($"[DoorTrigger] 상호작용 영역 이탈");
        }

        public void Interact()
        {
            if (!CanInteract)
            {
                return;
            }

            bool success = roomManager.TryChangeRoom(destinationRoom, destinationSpawn, doorOpenClip, doorCloseClip);

            if (success)
            {
                Debug.Log($"[DoorTrigger] {destinationRoom.name}으로 이동 완료");
            }
        }
    }
}
