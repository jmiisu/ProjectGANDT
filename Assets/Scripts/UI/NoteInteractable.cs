using System.Collections.Generic;
using UnityEngine;

namespace GANDT
{
    public class NoteInteractable : MonoBehaviour, IInteractable
    {
        [Header("Note")]
        [SerializeField] private NoteData noteData;
        [SerializeField] private TextViewer textViewer;
        [SerializeField] private string interactionText = "Read Note";

        //[Header("Interaction Area")]
        //[SerializeField] 
        private SphereCollider interactionCollider;

        private readonly HashSet<Collider> playerColliders = new HashSet<Collider>();

        public string InteractionText => interactionText;

        public bool CanInteract =>
            isActiveAndEnabled &&
            noteData != null &&
            noteData.pages != null &&
            noteData.pages.Length > 0 &&
            playerColliders.Count > 0 &&
            textViewer != null &&
            !textViewer.IsOpen;


        public bool IsAvailable =>
            isActiveAndEnabled &&
            noteData != null &&
            playerColliders.Count > 0;

        public Vector3 InteractionPosition =>
            interactionCollider != null
                ? transform.TransformPoint(interactionCollider.center)
                : transform.position;

        private void Reset()
        {
            interactionCollider = GetComponent<SphereCollider>();

            interactionCollider.isTrigger = true;
            interactionCollider.radius = 1f;
        }

        private void OnDisable()
        {
            playerColliders.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other))
            {
                return;
            }
            playerColliders.Add(other);
            Debug.Log("[NoteInteractable] 조사 가능");
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other))
            {
                return;
            }
            playerColliders.Remove(other);
        }

        // IInteractable 인터페이스 구현
        public void Interact()
        {
            if (!CanInteract)
            {
                return;
            }

            TryOpenNote(textViewer);
        }

        public bool Interact(TextViewer viewer)
        {
            if (!IsAvailable || viewer == null)
            {
                return false;
            }

            return TryOpenNote(viewer);
        }
        
        private bool TryOpenNote(TextViewer viewer)
        {
            bool success = viewer.TryOpen(noteData);

            if (success)
            {
                Debug.Log($"[NoteInteractable] {gameObject.name} 조사");
            }

            return success;
        }
        private static bool IsPlayer(Collider other)
        {
            return other.CompareTag("Player");
        }
    }
}
