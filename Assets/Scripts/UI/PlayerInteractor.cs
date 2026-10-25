using UnityEngine;
using UnityEngine.InputSystem;

namespace GANDT
{
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private TextViewer textViewer;
        [SerializeField] private GameObject interactionPrompt;

        [Header("Interactable Objects")]
        [SerializeField] private NoteInteractable[] noteInteractables;

        private NoteInteractable currentTarget;

        public NoteInteractable CurrentTarget => currentTarget;

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
            if (textViewer == null || textViewer.IsOpen)
            {
                currentTarget = null;
                SetPrompt(false);
                return;
            }

            FindNearestInteractable();

            SetPrompt(currentTarget != null);

            if (currentTarget == null || Keyboard.current == null)
                return;

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                currentTarget.Interact(textViewer);
                SetPrompt(false);
            }
        }

        private void FindNearestInteractable()
        {
            currentTarget = null;

            float nearestDistance = float.MaxValue;

            if (noteInteractables == null)
                return;

            foreach (NoteInteractable note in noteInteractables)
            {
                if (note == null || !note.IsAvailable)
                    continue;

                Vector3 targetPosition = note.InteractionPosition;

                float distance =
                    (player.position - targetPosition).sqrMagnitude;

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    currentTarget = note;
                }
            }
        }

        private void SetPrompt(bool visible)
        {
            if (interactionPrompt == null)
                return;

            if (interactionPrompt.activeSelf != visible)
            {
                interactionPrompt.SetActive(visible);
            }
        }

        private void OnDisable()
        {
            currentTarget = null;
            SetPrompt(false);
        }
    }
}

