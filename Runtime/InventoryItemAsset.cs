using Peg;
using UnityEngine;

namespace PGIA
{
    /// <summary>
    /// Describes the shared properties of an inventory item.
    /// </summary>
    [CreateAssetMenu(fileName = "Inventory Item", menuName = "PGIA/Inventory Item")]
    public class InventoryItemAsset : ScriptableObject
    {
        [Tooltip("The display name of this item.")]
        public HashedString Id;
        [Tooltip("Primaryily used to filter items. Can be used for other game-related systems that need to categorically search or sort items.")]
        public HashedString Category;
        [Tooltip("Used to determine how many instances of this kind of item can be combine into a single location. Any value less than 2 implies that the item is not stackable.")]
        public int MaxStackSize = 0;
        [Tooltip("A description of this item. Not needed in all situations by available for those that do.")]
        public string Description;
        [Tooltip("A general-purpose weight field. Not used specifically by PGIA but common enough that it was made available by default.")]
        public float Weight;
        [Tooltip("The icon to display in the grid view.")]
        public Sprite Icon;
        [Tooltip("The size of the item in grid cells. Should never be smaller than 1x1.")]
        public Vector2Int Size = new(1,1);
        [Tooltip("The background color for the item's icon when resting in a gridview.")]
        public Color Background;

        public bool IsStackable => MaxStackSize > 1;

        public bool CanStackWith(InventoryItemAsset item)
        {
            return Id.Hash == item.Id.Hash && Category.Hash == item.Category.Hash;
        }
    }
}
