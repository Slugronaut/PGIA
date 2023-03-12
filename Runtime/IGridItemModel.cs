using UnityEngine;
using UnityEngine.Events;

namespace PGIA
{
    /// <summary>
    /// Interface for describing an inventory item for a grid-based inventory in PGIA
    /// </summary>
    public interface IGridItemModel
    {
        int MaxStackCount { get; set; }
        int StackCount { get; set; }
        System.Guid Guid { get; }
        InventoryItemAsset Shared { get; }
        Vector2Int Size { get; }
        IGridModel Container { get; } //do we actually need this?

        public UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> OnWillStoreItem { get; set; }
        public UnityEvent<IGridModel, IGridItemModel> OnStoredItem { get; set; }
        public UnityEvent<IGridModel, IGridItemModel> OnStoreRejected { get; set; }
        public UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> OnWillRemoveItem { get; set; }
        public UnityEvent<IGridModel, IGridItemModel> OnRemovedItem { get; set; }
        public UnityEvent<IGridModel, IGridItemModel> OnRemoveRejected { get; set; }
    }
}
