using System;
using System.Collections.Generic;
using UnityEngine;

namespace GANDT
{
    [Serializable]
    public class InventorySlot
    {
        [SerializeField] private ItemData item;
        [SerializeField, Min(1)] private int quantity;

        public ItemData Item => item;
        public int Quantity => quantity;

        public InventorySlot(ItemData item, int quantity)
        {
            this.item = item;
            this.quantity = quantity;
        }

        public void AddQuantity(int amount)
        {
            quantity += amount;
        }

        public void RemoveQuantity(int amount)
        {
            quantity -= amount;
        }
    }

    public class GANDT_Inventory : MonoBehaviour
    {
        [Header("Inventory")]
        [SerializeField]
        private List<InventorySlot> slots =
                new List<InventorySlot>();

        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = true;

        public event Action OnInventoryChanged;

        public IReadOnlyList<InventorySlot> Slots => slots;

        public int GetItemCount(ItemData item)
        {
            if (item == null)
            {
                return 0;
            }

            int total = 0;

            foreach (InventorySlot slot in slots)
            {
                if (slot.Item == item)
                {
                    total += slot.Quantity;
                }
            }

            return total;
        }

        public bool HasItem(ItemData item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
            {
                return false;
            }

            return GetItemCount(item) >= quantity;
        }

        public bool TryAddItem(ItemData item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
            {
                return false;
            }

            // 수량 합산 시 정수 오버플로 방지
            if (GetItemCount(item) > int.MaxValue - quantity)
            {
                return false;
            }

            int remaining = quantity;

            // 기존 슬롯의 빈 공간부터 채움
            foreach (InventorySlot slot in slots)
            {
                if (slot.Item != item)
                {
                    continue;
                }

                int space = item.MaxStack - slot.Quantity;

                if (space <= 0)
                {
                    continue;
                }

                int addAmount = Mathf.Min(space, remaining);

                slot.AddQuantity(addAmount);
                remaining -= addAmount;

                if (remaining <= 0)
                {
                    break;
                }
            }

            // 초과 수량은 새 슬롯에 저장
            while (remaining > 0)
            {
                int addAmount = Mathf.Min(
                    item.MaxStack,
                    remaining
                );

                slots.Add(
                    new InventorySlot(item, addAmount)
                );

                remaining -= addAmount;
            }

            OnInventoryChanged?.Invoke();

            if (enableDebugLog)
            {
                Debug.Log(
                    $"[Inventory] {item.ItemName} " +
                    $"{quantity}개 획득. " +
                    $"현재 수량: {GetItemCount(item)}",
                    this
                );
            }

            return true;
        }

        public bool TryRemoveItem(ItemData item, int quantity = 1)
        {
            if (!HasItem(item, quantity))
            {
                return false;
            }

            int remaining = quantity;

            // 뒤에서부터 순회하여 빈 슬롯을 제거
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                InventorySlot slot = slots[i];

                if (slot.Item != item)
                {
                    continue;
                }

                int removeAmount = Mathf.Min(
                    slot.Quantity,
                    remaining
                );

                slot.RemoveQuantity(removeAmount);
                remaining -= removeAmount;

                if (slot.Quantity <= 0)
                {
                    slots.RemoveAt(i);
                }

                if (remaining <= 0)
                {
                    break;
                }
            }

            OnInventoryChanged?.Invoke();

            if (enableDebugLog)
            {
                Debug.Log(
                    $"[Inventory] {item.ItemName} " +
                    $"{quantity}개 제거. " +
                    $"현재 수량: {GetItemCount(item)}",
                    this
                );
            }

            return true;
        }

        [ContextMenu("Print Inventory")]
        public void PrintInventory()
        {
            Debug.Log(
                $"[Inventory] 슬롯 수: {slots.Count}",
                this
            );

            foreach (InventorySlot slot in slots)
            {
                if (slot.Item == null)
                {
                    continue;
                }

                Debug.Log(
                    $"[Inventory] {slot.Item.ItemName} " +
                    $"x{slot.Quantity}",
                    this
                );
            }
        }
    }
}
