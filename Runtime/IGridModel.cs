using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PGIA
{
    /// <summary>
    /// Interface for describing a grid-based inventory model for PGIA.
    /// </summary>
    public interface IGridModel
    {
        Vector2Int GridSize { get; set; }
        bool ShrinkItemWidths { get; set; }
        bool ShrinkItemHeights { get; set; }
        int GridWidth { get; }
        int GridHeight { get; }
        IEnumerable<IGridItemModel> Contents { get; }

        void OnEnable();
        bool StoreItem(IGridItemModel item, RectInt region);
        bool RemoveItem(IGridItemModel item);
        void ForceRemoveItem(IGridItemModel item);
        void DropItem(IGridItemModel item);
        bool SwapItems(IGridItemModel swapItem, IGridItemModel draggedItem, RectInt dropRegion);
        int StackItems(IGridItemModel incoming, IGridItemModel receiver, int qty);
        IGridItemModel SplitStackItem(IGridItemModel item, int qty, Func<IGridItemModel> instantiateAction);
        bool IsLocationEmpty(RectInt region);
        bool IsLocationEmpty(RectInt region, IGridItemModel excludedItem);
        bool ValidateRegion(RectInt region);
        GridCellModel GetCell(int x, int y);
        GridCellModel GetCell(int index);
        GridCellModel FindCell(IGridItemModel item);
        RectInt? GetLocation(IGridItemModel item);
        void SortInventory();
        RectInt ClipRegion(RectInt region);
        RectInt? FindOpenSpace(int width, int height);
        bool CanMoveItemToLocation(IGridItemModel item, RectInt region);
        IGridItemModel CheckForSwappableItem(IGridItemModel item, int xPos, int yPos);
        IGridItemModel CheckForStackableItem(IGridItemModel item, int xPos, int yPos);

        UnityEvent<IGridModel, Vector2Int> OnGridSizeChanged { get; set; }
        UnityEvent<IGridModel, IEnumerable<GridCellModel>> OnCellsUpdated { get; set; }
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
