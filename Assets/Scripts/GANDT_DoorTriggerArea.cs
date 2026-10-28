using System.Collections.Generic;
using UnityEngine;
namespace GANDT
{
    public class GANDT_DoorTriggerArea : MonoBehaviour
    {
        private GANDT_DoorTrigger doorTrigger;
        private readonly HashSet<Collider> playerColliders = new HashSet<Collider>();

        private Transform currentPlayer;

        private void Awake()
        {
            doorTrigger = GetComponentInParent<GANDT_DoorTrigger>();
            Debug.Log(doorTrigger);
        }

        private void OnDisable()
        {
            if (doorTrigger != null && currentPlayer != null)
            {
                doorTrigger.OnPlayerExit(currentPlayer);
            }

            playerColliders.Clear();
            currentPlayer = null;
        }

        private void OnTriggerEnter(Collider other)
        {
            Transform player = GetPlayerTransform(other);

            if (player == null)
            {
                Debug.Log("Here1");
                return;
            }

            if (currentPlayer != null && currentPlayer != player)
            {
                Debug.Log($"current: {currentPlayer}, player: {player}");
                return;
            }

            bool wasEmpty = playerColliders.Count == 0;
            playerColliders.Add(other);

            Debug.Log($"wasEmpty: {wasEmpty}, playerColliders.count: {playerColliders.Count}");

            if (wasEmpty && playerColliders.Count > 0)
            {
                currentPlayer = player;
                doorTrigger.OnPlayerEnter(player);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!playerColliders.Remove(other))
            {
                return;
            }

            if (playerColliders.Count > 0)
            {
                return;
            }

            if (currentPlayer != null)
            {
                doorTrigger.OnPlayerExit(currentPlayer);
            }
            currentPlayer = null;
        }

        private Transform GetPlayerTransform(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                return other.transform;
            }

            Transform root = other.transform.root;

            if (root.CompareTag("Player"))
            {
                return root;
            }
            return null;
        }

    }
}