using System.Collections;
using UnityEngine;

namespace GANDT
{
    public class GANDT_RoomManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private GANDT_ScreenFader screenFader;

        [Header("Movement Lock")]
        [SerializeField] private MonoBehaviour[] movementScripts;

        [Header("Room Settings")]
        [SerializeField] private GameObject initialRoom;
        [SerializeField] private GameObject[] rooms;

        [Header("Transition Settings")]
        [SerializeField, Min(0f)] private float transtionCooldown = 0.5f;
        [SerializeField, Min(0f)] private float blackScreenHold = 0.1f;

        [Header("Door Audio")]
        [SerializeField] private AudioSource doorAudioSource;

        private GameObject currentRoom;
        private float nextTransitionTime;
        private bool isTransitioning;

        public GameObject CurrentRoom => currentRoom;
        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            if (player == null)
            {
                Debug.LogError("[RoomManager] Player가 지정되지 않았습니다.", this);
                enabled = false;
                return;
            }

            if (initialRoom == null)
            {
                Debug.LogError("[RoomManager] Initial Room이 지정되지 않았습니다.", this);
                enabled = false;
                return;
            }

            if (characterController == null)
            {
                characterController = player.GetComponent<CharacterController>();
            }

            currentRoom = initialRoom;

            // 시작 방만 활성화
            foreach (GameObject room in rooms)
            {
                if (room == null)
                {
                    continue;
                }
                room.SetActive(room == currentRoom);
            }

            currentRoom.SetActive(true);

            Debug.Log($"[RoomManager] 시작 방: {currentRoom.name}");
        }

        public bool TryChangeRoom(GameObject destinationRoom, Transform destinationSpawn, AudioClip doorOpenClip, AudioClip doorCloseClip)
        {
            if (!isActiveAndEnabled || isTransitioning)
            {
                return false;
            }

            if (Time.unscaledTime < nextTransitionTime)
            {
                return false;
            }

            if (destinationRoom == null || destinationSpawn == null)
            {
                Debug.LogWarning("[RoomManager] 목적지 방 또는 지정된 스폰 위치가 없습니다.", this);
                return false;
            }

            if (destinationRoom == currentRoom)
            {
                Debug.LogWarning("[RoomManager] 현재 방과 목적지 방이 같습니다.", this);
                return false;
            }

            if (screenFader == null)
            {
                Debug.LogError("[RoomManager] ScreenFader가 없습니다.", this);
                return false;
            }

            isTransitioning = true;

            StartCoroutine(ChangeRoomRoutine(destinationRoom, destinationSpawn, doorOpenClip, doorCloseClip));

            return true;
        }

        private IEnumerator ChangeRoomRoutine(GameObject destinationRoom, Transform destinationSpawn, AudioClip doorOpenClip, AudioClip doorCloseClip)
        {
            // 기존 이동 스크립트 상태 저장
            bool[] previousStates = new bool[movementScripts.Length];

            for (int i = 0; i < movementScripts.Length; i++)
            {
                if (movementScripts[i] == null)
                {
                    continue;
                }

                previousStates[i] = movementScripts[i].enabled;
                movementScripts[i].enabled = false;
            }

            // 1. 문 열림 소리
            PlayDoorSound(doorOpenClip);

            // 화면 암전
            yield return screenFader.FadeOutCor();

            // 기존 방과 새로운 방 교체
            GameObject previousRoom = currentRoom;

            // 다음 방 활성화
            destinationRoom.SetActive(true);

            // 플레이어 이동
            MovePlayer(destinationSpawn);

            // 이전 방 비활성화
            if (previousRoom != null)
            {
                previousRoom.SetActive(false);
            }

            currentRoom = destinationRoom;

            if (blackScreenHold > 0f)
            {
                yield return new WaitForSecondsRealtime(blackScreenHold);
            }

            // 5. 문 닫힘 소리
            PlayDoorSound(doorCloseClip);

            yield return screenFader.FadeInCor();

            for (int i = 0; i < movementScripts.Length; i++)
            {
                if (movementScripts[i] != null)
                {
                    movementScripts[i].enabled = previousStates[i];
                }
            }

            // 연속 상호작용 방지
            nextTransitionTime = Time.unscaledTime + transtionCooldown;
            isTransitioning = false;

            Debug.Log($"[RoomManager] 방 전환 완료: {currentRoom.name}");
        }

        private void MovePlayer(Transform spawn)
        {
            bool wasControllerEnabled = characterController.enabled;

            if (wasControllerEnabled)
            {
                characterController.enabled = false;
            }

            player.transform.GetChild(1).SetPositionAndRotation(spawn.position, spawn.rotation);
            //Debug.Log($"현재 플레이어 위치: {spawn.position}, {spawn.rotation}");
            if (wasControllerEnabled)
            {
                characterController.enabled = true;
            }
        }

        private void PlayDoorSound(AudioClip clip)
        {
            if (clip == null || doorAudioSource == null)
                return;

            doorAudioSource.PlayOneShot(clip);
        }
    }
}
