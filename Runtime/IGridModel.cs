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
        public Vector2Int GridSize { get; set; }
        public bool ShrinkItemWidths { get; set; }
        public bool ShrinkItemHeights { get; set; }
        public int GridWidth { get; }
        public int GridHeight { get; }
        IEnumerable<IGridItemModel> Contents { get; }

        void OnEnable();
        bool StoreItem(IGridItemModel item, RectInt region);
        bool RemoveItem(IGridItemModel item);
        void ForceRemoveItem(IGridItemModel item);
        void DropItem(IGridItemModel item);
        bool IsLocationEmpty(RectInt region);
        bool IsLocationEmpty(RectInt region, IGridItemModel excludedItem);
        bool ValidateRegion(RectInt region);
        GridCellModel GetCell(int x, int y);
        GridCellModel GetCell(int index);
        GridCellModel FindCell(IGridItemModel item);
        RectInt? GetLocation(IGridItemModel item);
        void SortInventory();
        bool CanMoveItemToLocation(IGridItemModel item, RectInt region);
        RectInt ClipRegion(RectInt region);
        IGridItemModel CheckForSwappableItem(IGridItemModel item, int xPos, int yPos);
        RectInt? FindOpenSpace(int width, int height);
        public bool Swap(IGridItemModel swapItem, IGridItemModel draggedItem, RectInt dropRegion);

        public UnityEvent<IGridModel, Vector2Int> OnGridSizeChanged { get; set; }
        public UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> OnWillStoreItem { get; set; }
        public UnityEvent<IGridModel, IGridItemModel> OnStoredItem { get; set; }
        public UnityEvent<IGridModel, IGridItemModel> OnStoreRejected { get; set; }
        public UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> OnWillRemoveItem { get; set; }
        public UnityEvent<IGridModel, IGridItemModel> OnRemovedItem { get; set; }
        public UnityEvent<IGridModel, IGridItemModel> OnRemoveRejected { get; set; }
        public UnityEvent<IGridModel, IGridItemModel> OnDroppedItem { get; set; }

    }
}
