using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PGIA
{
    /// <summary>
    /// Interface for a grid-based inventory backend in PGIA.
    /// This implementation is MonoBehaviour based and uses a <see cref="GridModel"/>
    /// as its backing data.
    /// </summary>
    public class GridModelBehaviour : MonoBehaviour, IGridModel
    {
        #region Public Fields and Properties
        [PropertyOrder(-1)]
        [ShowInInspector]
        public Vector2Int GridSize { get => BackingModel.GridSize; set => BackingModel.GridSize = value; }
        public int GridWidth => GridSize.x;
        public int GridHeight => GridSize.y;

        [DetailedInfoBox(@"What does 'item shrinking' do?",
              "There are times when you may want a grid to effectively ignore items sizes in one or both dimensions. This "+
               "setting allows you to configure that. This allows for things like vertical list views that show one item per "+
               "line or equipment slots that don't care what the size of an item is.")]
        [Tooltip("Should this model treat all items with a width greater than 1 as though it was in fact just 1?")]
        [ShowInInspector]
        public bool ShrinkItemWidths { get => BackingModel.ShrinkItemWidths; set => BackingModel.ShrinkItemWidths = value; }

        [Tooltip("Should this model treat all items with a height greater than 1 as though it was in fact just 1?")]
        [ShowInInspector]
        public bool ShrinkItemHeights { get => BackingModel.ShrinkItemHeights; set => BackingModel.ShrinkItemHeights = value; }

        /// <summary>
        /// A copy of the internal list of items contained within this inventory.
        /// </summary>
        public IEnumerable<IGridItemModel> Contents => BackingModel.Contents;
        #endregion


        #region Events
        [PropertySpace(12)][ShowInInspector][FoldoutGroup("Events")] public UnityEvent<IGridModel, Vector2Int> OnGridSizeChanged { get => BackingModel.OnGridSizeChanged; set => BackingModel.OnGridSizeChanged = value; }
        [PropertySpace(12)][ShowInInspector][FoldoutGroup("Events")] public UnityEvent<IGridModel, IEnumerable<GridCellModel>> OnCellsUpdated { get => BackingModel.OnCellsUpdated; set => BackingModel.OnCellsUpdated = value; }
        [PropertySpace(12)][ShowInInspector][FoldoutGroup("Store Events")] public UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> OnWillStoreItem { get => BackingModel.OnWillStoreItem; set => BackingModel.OnWillStoreItem = value; }
        [ShowInInspector][FoldoutGroup("Store Events")] public UnityEvent<IGridModel, IGridItemModel> OnStoredItem { get => BackingModel.OnStoredItem; set => BackingModel.OnStoredItem = value; }
        [ShowInInspector][FoldoutGroup("Store Events")] public UnityEvent<IGridModel, IGridItemModel> OnStoreRejected { get => BackingModel.OnStoreRejected; set => BackingModel.OnStoreRejected = value; }
        [PropertySpace(12)][ShowInInspector][FoldoutGroup("Remove Events")] public UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> OnWillRemoveItem { get => BackingModel.OnWillRemoveItem; set => BackingModel.OnWillRemoveItem = value; }
        [ShowInInspector][FoldoutGroup("Remove Events")] public UnityEvent<IGridModel, IGridItemModel> OnRemovedItem { get => BackingModel.OnRemovedItem; set => BackingModel.OnRemovedItem = value; }
        [ShowInInspector][FoldoutGroup("Remove Events")] public UnityEvent<IGridModel, IGridItemModel> OnRemoveRejected { get => BackingModel.OnRemoveRejected; set => BackingModel.OnRemoveRejected = value; }
        [ShowInInspector][FoldoutGroup("Remove Events")] public UnityEvent<IGridModel, IGridItemModel> OnDroppedItem { get => BackingModel.OnDroppedItem; set => BackingModel.OnDroppedItem = value; }
        [PropertySpace(12)][ShowInInspector][FoldoutGroup("Stack Events")] public UnityEvent<IGridModel, IGridItemModel, IGridItemModel> OnStackedItem { get => BackingModel.OnStackedItem; set => BackingModel.OnStackedItem = value; }
        [ShowInInspector][FoldoutGroup("Stack Events")] public UnityEvent<IGridModel, IGridItemModel, IGridItemModel> OnStackSplitItem { get => BackingModel.OnStackSplitItem; set => BackingModel.OnStackSplitItem = value; }
        
        #endregion


        #region Private Fields and Properties
        /// <summary>
        /// This is the actual model data here. This component is just forwarding everything to it. We need to initialize it here
        /// so that when this component is added in the editor this backing model is automatically created, otherwise we'd
        /// get null refs immediately from the inspector trying to get and set values from it. When this component is instantiated at
        /// runtime this value will be overwritten when the desierializer kicks in (which basically makes it a garbage generator :p).
        /// </summary>
        [SerializeField]
        [HideInInspector]
        GridModel BackingModel = new();
        #endregion

        #region UnityEvents
        /// <summary>
        /// 
        /// </summary>
        public void OnEnable()
        {
            BackingModel.OnEnable();
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// Stores the item in the inventory model. If the operation is rejected by a listener
        /// or the item was already in this container then false is returned. 
        /// If the item was inside of another container it will be removed from that container first.
        /// If that removal fails, so will this operation, no events will trigger on this model.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool StoreItem(IGridItemModel item, Vector2Int topLeft) => BackingModel.StoreItem(item, topLeft);

        /// <summary>
        /// Removes the item from inventory model. If the operation is rejected by a listener
        /// or it does not exist in this inventory then false is returned.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool RemoveItem(IGridItemModel item) => BackingModel.RemoveItem(item);

        /// <summary>
        /// Forces an item to be removed from the inventory without checking for validation from outside listeners
        /// </summary>
        public void ForceRemoveItem(IGridItemModel item) => BackingModel.ForceRemoveItem(item);

        /// <summary>
        /// Similar to ForceRemoveItem, this method is used to signal that an item has been ejected
        /// from the grid system entirely and is now at the mercy of the gamesystem as to what to do with it.
        /// Typically RemoveItem and ForceRemove item will be used for things like swapping, trading, and moving
        /// items around into different inventory containers. This is the point where the item is completely
        /// removed from that system.
        /// <param name="item"></param>
        public void DropItem(IGridItemModel item) => BackingModel.DropItem(item);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="incoming"></param>
        /// <param name="receiver"></param>
        /// <param name="qty"></param>
        /// <returns></returns>
        public int StackItems(IGridItemModel incoming, IGridItemModel receiver, int qty) => BackingModel.StackItems(incoming, receiver, qty);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="qty"></param>
        /// <returns></returns>
        public IGridItemModel SplitStackItem(IGridItemModel item, int qty, Func<IGridItemModel> creationAction) => BackingModel.SplitStackItem(item, qty, creationAction);

        /// <summary>
        /// Ensures that all slots within the given region are indeed empty.
        /// </summary>
        /// <param name="xPos"></param>
        /// <param name="yPos"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public bool IsLocationEmpty(RectInt region) => BackingModel.IsLocationEmpty(region);

        /// <summary>
        /// Ensures that all slots within the given region are indeed empty with the exception
        /// that it ignored the given item.
        /// </summary>
        /// <param name="region"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool IsLocationEmpty(RectInt region, IGridItemModel item) => BackingModel.IsLocationEmpty(region, item);

        /// <summary>
        /// Performs quick check to ensure the region is valid within the bounds of the grid.
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public bool ValidateRegion(RectInt region) => BackingModel.ValidateRegion(region);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public GridCellModel GetCell(int x, int y) => BackingModel.GetCell(x, y);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public GridCellModel GetCell(int index) => BackingModel.GetCell(index);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public GridCellModel FindCell(IGridItemModel item) => BackingModel.FindCell(item);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public RectInt? GetLocation(IGridItemModel item) => BackingModel.GetLocation(item);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Vector2Int AdjustedSize(IGridItemModel item) => BackingModel.AdjustedSize(item);

        /// <summary>
        /// Sorts inventory into best-fit scenario via a simple but exponetial knapsack solver.
        /// This method could quickly get out of hand with larger data sets. Use with caution.
        /// </summary>
        public void SortInventory() => BackingModel.SortInventory();

        /// <summary>
        /// Locates the first spot in the inventory with the given width and hieght and returns a region for it.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public RectInt? FindOpenSpace(int width, int height) => BackingModel.FindOpenSpace(width, height);

        /// <summary>
        /// Returns true if the item can be moved to the given location within the given model.
        /// The item ignores itself when checking to see if a cell is available.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="region"></param>
        public bool CanMoveItemToLocation(IGridItemModel item, Vector2Int topLeft) => BackingModel.CanMoveItemToLocation(item, topLeft);

        /// <summary>
        /// Performs the task of dropping a dragged into into an inventory model with swap as needed. If successfull, the swapped item will be returned.
        /// If any cancellation action occur null is returned and the draggedItem and dropModel remain untouched.
        /// </summary>
        /// <param name="swapItem">The item in the dest model that is going to be swapped out for the draggedItem. It is assumed this item has been obtained via <see cref="IGridModel.CheckForSwappableItem(IGridItemModel, int, int)"/>.</param>
        /// <param name="draggedItem">The item currently being drag n dropped.</param>
        /// <param name="dropRegion">The location on the dropModel to drop the draggedItem.</param>
        /// <returns></returns>
        public bool SwapItems(IGridItemModel swapItem, IGridItemModel draggedItem, Vector2Int dropTopLeft) => BackingModel.SwapItems(swapItem, draggedItem, dropTopLeft);

        /// <summary>
        /// Checks a region of space to see if there is exactly 1 IGridItemModel within
        /// and if there is enough room for the passed item to fit in that location if it
        /// were removed. If so, a reference to that single item is returned.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="xPos"></param>
        /// <param name="yPos"></param>
        /// <returns></returns>
        public IGridItemModel CheckForSwappableItem(IGridItemModel item, int xPos, int yPos) => BackingModel.CheckForSwappableItem(item, xPos, yPos);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="xPos"></param>
        /// <param name="yPos"></param>
        /// <returns></returns>
        public IGridItemModel CheckForStackableItem(IGridItemModel item, int xPos, int yPos) => BackingModel.CheckForStackableItem(item, xPos, yPos);

        #endregion

    }

}
