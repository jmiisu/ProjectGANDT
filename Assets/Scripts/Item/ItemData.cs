using UnityEngine;

namespace GANDT
{
    public enum GANDT_ITEM_TYPE
    {
        KEY,
        MENTAL_RECOVERY,
        SANITY_RECOVERY,
    }

    [CreateAssetMenu(fileName = "Item_New", menuName = "GANDT/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Basic Information")]
        [SerializeField] private string itemId;
        [SerializeField] private string itemName;
        [SerializeField, TextArea(2, 5)] private string description;
        [SerializeField] private Sprite icon;

        [Header("Item Settings")]
        [SerializeField] private  GANDT_ITEM_TYPE itemType = GANDT_ITEM_TYPE.KEY;
        [SerializeField, Min(1)] private int maxStack = 1;

        [Header("Recovery Effect")]
        [SerializeField, Range(0f, 1f)] private float effectAmount = 0f;

        public string ItemID => itemId;
        public string ItemName => itemName;
        public string Description => description;
        public Sprite Icon => icon;

        public GANDT_ITEM_TYPE Type => itemType;
        public int MaxStack => Mathf.Max(1, maxStack);
        public float EffectAmount => effectAmount;

        private void OnValidate()
        {
            maxStack = Mathf.Max(1, maxStack);
            effectAmount = Mathf.Clamp01(effectAmount);
        }
    }
}