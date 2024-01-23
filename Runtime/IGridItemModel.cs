using UnityEngine;
using UnityEngine.Events;

namespace PGIA
{
    /// <summary>
    /// Interface for describing an inventory item for a grid-based inventory in PGIA
    /// </summary>
    public interface IGridItemModel
    {
        int MaxStackCount { get; }
        int StackCount { get; set; }
        System.Guid Guid { get; }
        InventoryItemAsset Shared { get; }
        Vector2Int Size { get; }
        IGridModel Container { get; } //do we actually need this?
        public bool OverrideBackgroundColor { get; }
        public Color CustomBackgroundColor { get; }

        UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> OnWillStoreItem { get; set; }
        UnityEvent<IGridModel, IGridItemModel> OnStoredItem { get; set; }
        UnityEvent<IGridModel, IGridItemModel> OnStoreRejected { get; set; }
        UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> OnWillRemoveItem { get; set; }
        UnityEvent<IGridModel, IGridItemModel> OnRemovedItem { get; set; }
        UnityEvent<IGridModel, IGridItemModel> OnRemoveRejected { get; set; }
        UnityEvent<IGridModel, IGridItemModel> OnDroppedItem { get; set; }
        UnityEvent<IGridModel, IGridItemModel, IGridItemModel> OnStackedItem { get; set; }
        UnityEvent<IGridModel, IGridItemModel, IGridItemModel> OnStackSplitItem { get; set; }
    }
}
