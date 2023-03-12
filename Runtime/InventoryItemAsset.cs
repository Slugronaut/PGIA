using Toolbox;
using UnityEngine;

namespace PGIA
{
    /// <summary>
    /// Describes the shared properties of an inventory item.
    /// </summary>
    [CreateAssetMenu(fileName = "Inventory Item", menuName = "PGIA/Inventory Item")]
    public class InventoryItemAsset : ScriptableObject
    {
        public HashedString Id;
        public string Description;
        public float Weight;
        public Sprite Icon;
        public HashedString Category;
        public Vector2Int Size;
    }
}
