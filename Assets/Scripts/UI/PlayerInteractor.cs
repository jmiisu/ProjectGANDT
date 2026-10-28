using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GANDT
{
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private TextViewer textViewer;
        [SerializeField] private GANDT_RoomManager roomManager;

        [Header("Interaction UI")]
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private TMP_Text interactionPromptText;

        [Header("Interactable Objects")]
        [Tooltip("IInteractable을 구현한 컴포넌트를 등록하세요.")]
        [SerializeField] private MonoBehaviour[] interactableObjects;

        [Header("Input")]
        [SerializeField] private Key interactionKey = Key.E;
        [SerializeField] private Key switchKey = Key.F;

        private readonly List<MonoBehaviour> availableObjects = new List<MonoBehaviour>();

        private IInteractable currentTarget;

        public IInteractable CurrentTarget => currentTarget;

        private void Awake()
        {
            if (player == null)
            {
                player = transform;
            }

            SetPrompt(false);
        }

        private void Update()
        {
            // 문서 열람 또는 방 전환 중에는 상호작용 차단
            if (textViewer == null || textViewer.IsOpen)
            {
                ClearTarget();
                availableObjects.Clear();
                return;
            }

            // 현재 상호작용 가능한 대상 수집
            RefreshAvailableObjects();

            // 선택 대상 유지 또는 자동 선택
            UpdateCurrentTarget();

            // 대상 전환 및 상호작용 입력
            HandleInput();

            // 현재 대상에 맞춰 UI 갱신
            RefreshPrompt();
        }

        private void RefreshAvailableObjects()
        {
            availableObjects.Clear();

            if (interactableObjects == null)
            {
                return;
            }

            foreach (MonoBehaviour component in interactableObjects)
            {
                if (component == null || !component.isActiveAndEnabled)
                {
                    continue;
                }

                if (component is not IInteractable interactable)
                {
                    continue;
                }

                if (!interactable.CanInteract)
                {
                    continue;
                }

                // Inspector에 중복 등록된 대상 방지
                if (availableObjects.Contains(component))
                {
                    continue;
                }

                availableObjects.Add(component);
            }

            // 거리순 정렬
            availableObjects.Sort(CompareDistance);
        }

        private int CompareDistance(MonoBehaviour a, MonoBehaviour b)
        {
            float distanceA = GetSqrDistance(a);
            float distanceB = GetSqrDistance(b);

            return distanceA.CompareTo(distanceB);
        }

        private float GetSqrDistance(MonoBehaviour component)
        {
            if (player == null)
            {
                return float.MaxValue;
            }

            Vector3 targetPosition = component.transform.position;

            if (component is NoteInteractable note)
            {
                targetPosition = note.InteractionPosition;
            }

            float distance = (player.position - targetPosition).sqrMagnitude;

            return distance;
        }

        private void UpdateCurrentTarget()
        {
            if (availableObjects.Count == 0)
            {
                currentTarget = null;
                return;
            }

            // 이미 선택한 대상이 유효하다면 유지
            if (currentTarget is MonoBehaviour currentComponent &&
                availableObjects.Contains(currentComponent))
            {
                return;
            }

            // 선택 대상이 사라졌으면 최근접 대상 선택
            currentTarget = availableObjects[0] as IInteractable;
        }

        private void HandleInput()
        {
            if (currentTarget == null || Keyboard.current == null)
            {
                return;
            }

            // 여러 대상이 있으면 F키로 전환
            if (availableObjects.Count > 1 &&
                Keyboard.current[switchKey].wasPressedThisFrame)
            {
                SwitchTarget();
                return;
            }

            // E키로 현재 선택된 대상과 상호작용
            if (Keyboard.current[interactionKey].wasPressedThisFrame)
            {
                if (currentTarget.CanInteract)
                {
                    currentTarget.Interact();
                }
            }
        }

        private void SwitchTarget()
        {
            if (availableObjects.Count <= 1)
            {
                return;
            }

            MonoBehaviour currentComponent = currentTarget as MonoBehaviour;
            int currentIndex = availableObjects.IndexOf(currentComponent);
            int nextIndex = (currentIndex + 1) % availableObjects.Count;

            currentTarget = availableObjects[nextIndex] as IInteractable;

            Debug.Log($"[PlayerInteractable] 대상 전환: {currentTarget.InteractionText}");
        }

        private void RefreshPrompt()
        {
            if (currentTarget == null ||
                !currentTarget.CanInteract ||
                IsInteractionBlocked())
            {
                SetPrompt(false);
                return;
            }

            SetPrompt(true);

            string prompt = $"[{interactionKey}] {currentTarget.InteractionText}";

            if (availableObjects.Count > 1)
            {
                prompt += $"\n[{switchKey}] 다른 액션 전환";
            }

            if (interactionPromptText.text != prompt)
            {
                interactionPromptText.text = prompt;
            }
        }

        private bool IsInteractionBlocked()
        {
            if (textViewer.IsOpen)
            {
                return true;
            }

            if (roomManager.IsTransitioning)
            {
                return true;
            }

            return false;
        }

        private void SetPrompt(bool visible)
        {
            if (interactionPrompt == null)
            {
                return;
            }

            if (interactionPrompt.activeSelf != visible)
            {
                interactionPrompt.SetActive(visible);
            }
        }

        private void OnDisable()
        {
            ClearTarget();
            availableObjects.Clear();
        }

        private void ClearTarget()
        {
            currentTarget = null;
            SetPrompt(false);
        }
    }
}

