using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GANDT
{
    public class NoteInteractable : MonoBehaviour
    {
        [Header("Note")]
        [SerializeField] private NoteData noteData;

        //[Header("Interaction Area")]
        //[SerializeField] 
        SphereCollider interactionCollider;

        private readonly HashSet<Collider> playerColliders = new HashSet<Collider>();

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

        public bool Interact(TextViewer viewer)
        {
            if (!IsAvailable || viewer == null)
            {
                return false;
            }

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
