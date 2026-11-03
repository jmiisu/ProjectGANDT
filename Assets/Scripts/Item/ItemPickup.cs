using System.Collections.Generic;
using UnityEngine;

namespace GANDT
{
    public class ItemPickup : MonoBehaviour, IInteractable
    {
        [Header("Item")]
        [SerializeField] private ItemData itemData;
        [SerializeField, Min(1)] private int quantity = 1;

        [Header("Interaction")]
        [SerializeField] private string interactionText = "아이템 획득";
        [SerializeField] private bool destroyOnPickup = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = true;

        private readonly HashSet<Collider> playerColliders = new HashSet<Collider>();

        private GANDT_Inventory playerInventory;
        private bool isCollected;

        public string InteractionText
        {
            get
            {
                if (itemData == null)
                {
                    return interactionText;
                }

                return $"{itemData.ItemName} {interactionText}";
            }
        }

        public bool CanInteract =>
            isActiveAndEnabled &&
            !isCollected &&
            itemData != null &&
            quantity > 0 &&
            playerInventory != null &&
            playerColliders.Count > 0;

        private void Reset()
        {
            Collider col = GetComponent<Collider>();

            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnValidate()
        {
            quantity = Mathf.Max(1, quantity);
        }

        private void OnDisable()
        {
            playerColliders.Clear();
            playerInventory = null;
        }

        private void OnTriggerEnter(Collider other)
        {
            GANDT_Inventory inventory =
                GetPlayerInventory(other);

            if (inventory == null)
            {
                return;
            }

            // 현재 획득 대상으로 인식한 플레이어만 등록
            if (playerInventory != null &&
                playerInventory != inventory)
            {
                return;
            }

            playerInventory = inventory;
            playerColliders.Add(other);

            if (enableDebugLog)
            {
                Debug.Log(
                    $"[ItemPickup] {itemData?.ItemName} " +
                    "획득 가능",
                    this
                );
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!playerColliders.Remove(other))
            {
                return;
            }

            if (playerColliders.Count == 0)
            {
                playerInventory = null;
            }
        }

        public void Interact()
        {
            if (!CanInteract)
            {
                return;
            }

            bool success = playerInventory.TryAddItem(
                itemData,
                quantity
            );

            if (!success)
            {
                Debug.LogWarning(
                    "[ItemPickup] 아이템 획득에 실패했습니다.",
                    this
                );

                return;
            }

            isCollected = true;

            if (enableDebugLog)
            {
                Debug.Log(
                    $"[ItemPickup] {itemData.ItemName} " +
                    $"{quantity}개 획득 완료",
                    this
                );
            }

            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private static GANDT_Inventory GetPlayerInventory(
            Collider other)
        {
            Transform root = other.transform.root;

            bool isPlayer =
                other.CompareTag("Player") ||
                root.CompareTag("Player");

            if (!isPlayer)
            {
                return null;
            }

            // 플레이어 루트 또는 하위 오브젝트에
            // Inventory가 부착된 경우 모두 탐색
            return other.GetComponentInParent<GANDT_Inventory>();
        }
    }

}
