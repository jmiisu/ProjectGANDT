using UnityEngine;
namespace GANDT
{
    public class GANDT_DoorTriggerArea : MonoBehaviour
    {
        private GANDT_DoorTrigger doorTrigger;

        private void Awake()
        {
            doorTrigger = GetComponentInParent<GANDT_DoorTrigger>();

            if (doorTrigger == null)
            {
                Debug.LogError("[DoorTriggerArea] 부모에서 GANDT_DoorTrigger를 찾지 못했습니다.", this);
            }
        }
        private void OnTriggerEnter(Collider other)
        {
            if (doorTrigger == null)
            {
                return;
            }

            Transform player = GetPlayerTransform(other);

            if (player == null)
            {
                return;
            }

            doorTrigger.OnPlayerEnter(player);
        }

        private void OnTriggerExit(Collider other)
        {
            if (doorTrigger == null)
            {
                return;
            }

            Transform player = GetPlayerTransform(other);

            if (player == null)
            {
                return;
            }

            if (player == null)
            {
                return;
            }

            doorTrigger.OnPlayerExit(player);
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