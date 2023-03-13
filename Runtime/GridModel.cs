using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace PGIA
{
    /// <summary>
    /// Backend for a grid-based inventory system in PGIA.
    /// 
    /// TODO:
    ///     -ignore item dimensions (treats them all as a size of 1 in that dimension
    ///         -needed for list views and equipment slots
    ///     -reference mode, for things like hotbars. doesn't remove item from source when it accepts it
    ///     -filter asset, a list of item categories that are not allowed in a given slot
    ///     -instance limiting - only allow certain amount of each type of item
    /// </summary>
    [System.Serializable]
    public class GridModel : IGridModel
    {
        #region Public Fields and Properties
        [SerializeField]
        [HideInInspector]
        Vector2Int _GridSize = new(1, 1);
        [PropertyOrder(-1)]
        [ShowInInspector]
        public Vector2Int GridSize
        {
            get => _GridSize;
            set
            {
                if (value.x < 1 || value.y < 1) return;

                if (!Application.isPlaying)
                {
                    _GridSize = value;
                    return;
                }

                if (value != _GridSize)
                {
                    var oldSize = _GridSize;
                    var leftoverItems = ResolveItemsWithNewSize(value);
                    foreach (var item in leftoverItems)
                        ForceRemoveItem(item);

                    _GridSize = value;
                    OnGridSizeChanged.Invoke(this, oldSize);
                }
            }
        }
        public int GridWidth => GridSize.x;
        public int GridHeight => GridSize.y;

        [SerializeField]
        [HideInInspector]
        bool _ShrinkItemWidths = false;
        [DetailedInfoBox(@"What does 'item shrinking' do?",
              "There are times when you may want a grid to effectively ignore items sizes in one or both dimensions. This " +
               "setting allows you to configure that. This allows for things like vertical list views that show one item per " +
               "line or equipment slots that don't care what the size of an item is.")]
        [Tooltip("Should this model treat all items with a width greater than 1 as though it was in fact just 1?")]
        [ShowInInspector]
        public bool ShrinkItemWidths { get => _ShrinkItemWidths; set => _ShrinkItemWidths = value; }

        [SerializeField]
        [HideInInspector]
        bool _ShrinkItemHeights = false;
        [Tooltip("Should this model treat all items with a height greater than 1 as though it was in fact just 1?")]
        [ShowInInspector]
        public bool ShrinkItemHeights { get => _ShrinkItemHeights; set => _ShrinkItemHeights = value; }

        /// <summary>
        /// A copy of the internal list of items contained within this inventory.
        /// </summary>
        public IEnumerable<IGridItemModel> Contents
        {
            get => CellModels.Select(slot => slot.Item)
                        .Where(item => item != null)
                        .Distinct();
        }
        #endregion


        #region Events
        [SerializeField][HideInInspector] UnityEvent<IGridModel, Vector2Int> _OnGridSizeChanged = new();
        [PropertySpace(12)][ShowInInspector][FoldoutGroup("Events")] public UnityEvent<IGridModel, Vector2Int> OnGridSizeChanged { get => _OnGridSizeChanged; set => _OnGridSizeChanged = value; }

        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> _OnWillStoreItem = new UnityEvent<IGridModel, IGridItemModel, OperationCancelAction>();
        [PropertySpace(12)][ShowInInspector][FoldoutGroup("Store Events")] public UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> OnWillStoreItem { get => _OnWillStoreItem; set => _OnWillStoreItem = value; }

        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel> _OnStoredItem = new();
        [ShowInInspector][FoldoutGroup("Store Events")] public UnityEvent<IGridModel, IGridItemModel> OnStoredItem { get => _OnStoredItem; set => _OnStoredItem = value; }

        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel> _OnStoreRejected = new();
        [ShowInInspector][FoldoutGroup("Store Events")] public UnityEvent<IGridModel, IGridItemModel> OnStoreRejected { get => _OnStoreRejected; set => _OnStoreRejected = value; }

        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> _OnWillRemoveItem = new();
        [PropertySpace(12)][ShowInInspector][FoldoutGroup("Remove Events")] public UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> OnWillRemoveItem { get => _OnWillRemoveItem; set => _OnWillRemoveItem = value; }

        [SerializeField][HideInInspector] public UnityEvent<IGridModel, IGridItemModel> _OnRemovedItem = new();
        [ShowInInspector][FoldoutGroup("Remove Events")] public UnityEvent<IGridModel, IGridItemModel> OnRemovedItem { get => _OnRemovedItem; set => _OnRemovedItem = value; }

        [SerializeField][HideInInspector] public UnityEvent<IGridModel, IGridItemModel> _OnRemoveRejected = new();
        [ShowInInspector][FoldoutGroup("Remove Events")] public UnityEvent<IGridModel, IGridItemModel> OnRemoveRejected { get => _OnRemoveRejected; set => _OnRemoveRejected = value; }
        
        [SerializeField][HideInInspector] public UnityEvent<IGridModel, IGridItemModel> _OnDroppedItem = new();
        [ShowInInspector][FoldoutGroup("Remove Events")] public UnityEvent<IGridModel, IGridItemModel> OnDroppedItem { get => _OnDroppedItem; set => _OnDroppedItem = value; }

        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel, IGridItemModel> _OnStackItem = new();
        [PropertySpace(12)][ShowInInspector][FoldoutGroup("Stack Events")] public UnityEvent<IGridModel, IGridItemModel, IGridItemModel> OnStackedItem { get => _OnStackItem; set => _OnStackItem = value; }

        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel, IGridItemModel> _OnStackSplitItem = new();
        [ShowInInspector][FoldoutGroup("Stack Events")] public UnityEvent<IGridModel, IGridItemModel, IGridItemModel> OnStackSplitItem { get => _OnStackSplitItem; set => _OnStackSplitItem = value; }
        #endregion


        #region Private Fields
        List<GridCellModel> CellModels;
        static bool PendingActionCancelled;
        #endregion


        #region Private Methods
        /// <summary>
        /// Initialize the model grid using the grid width and height already provided.
        /// Mostly exists as an easy way to construct this grid while matching the interface of a monobehaviour-based version.
        /// </summary>
        public void OnEnable()
        {
            ConstructGrid();
        }

        /// <summary>
        /// Helper for enumerating all of the cells in a region on the grid.
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        IEnumerable<GridCellModel> CellsInRegion(RectInt region)
        {
            Assert.IsTrue(ValidateRegion(region));

            for (int y = region.y; y < region.y + region.height; y++)
            {
                for (int x = region.x; x < region.x + region.width; x++)
                {
                    yield return CellModels[(y * GridWidth) + x];
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void ConstructGrid()
        {
            int total = GridHeight * GridWidth;
            CellModels = new(total);
            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                    CellModels.Add(new GridCellModel(this, x, y));
            }
        }

        /// <summary>
        /// Attempts to fit inventory contents into the new space. If this cannot be done,
        /// the final items that will not fit (usually the smallest) will be returned in a list.
        /// 
        /// NOTE: This is not a perfect algorithm here. I'm not trying to solve the napsack problem, okay?
        /// 
        /// </summary>
        /// <param name="newSize"></param>
        /// <returns></returns>
        List<IGridItemModel> ResolveItemsWithNewSize(Vector2Int newSize)
        {
            if (newSize.x * newSize.y < 1) return null;
            var items = Contents.ToList();
            if (items == null || items.Count < 1) return null;

            var positions = KnapsackSolver.Solve(GridSize, items.Select(item => item.Shared.Size).ToList());
            Assert.IsNotNull(positions);
            Assert.IsTrue(positions.Count <= items.Count, "Something is wrong with our Knapsack algorithm if you are seeing this.");
            List<IGridItemModel> tempList = new(items.Count - positions.Count);

            //clear out all slots and then start putting things back in until we run out of space.
            //the positions in our list will ensure we are limited to the new space available.
            foreach (var slot in CellModels)
                slot.Item = null;
            int i = 0;
            for (; i < positions.Count; i++)
                FlagSlots(items[i], new RectInt(positions[i], items[i].Size));

            //anything that we didn't loop over above is what couldn't fit withint
            for (; i < items.Count; i++)
                tempList.Add(items[i]);
            return tempList;
        }

        /// <summary>
        /// Helper for quickly setting the item reference for a region of grid slots.
        /// </summary>
        void FlagSlots(IGridItemModel item, RectInt region)
        {
            Assert.IsTrue(ValidateRegion(region));
            Assert.IsTrue(item == null ? !IsLocationEmpty(region) : IsLocationEmpty(region));

            for (int y = region.y; y < region.y + region.height; y++)
            {
                for (int x = region.x; x < region.x + region.width; x++)
                {
                    int index = (y * GridWidth) + x;
                    CellModels[index].Item = item;
                }
            }
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
        public bool StoreItem(IGridItemModel item, RectInt region)
        {
            Assert.IsNotNull(item);

            if (item.Container == this)
                return false;

            if (item.Container != null)
            {
                if (!item.Container.RemoveItem(item))
                    return false;
            }

            PendingActionCancelled = false;
            OperationCancelAction opCan = new(() => PendingActionCancelled = true);
            OnWillStoreItem.Invoke(this, item, opCan);
            item.OnWillStoreItem.Invoke(this, item, opCan);

            if (PendingActionCancelled || !ValidateRegion(region) || !IsLocationEmpty(region))
            {
                OnStoreRejected.Invoke(this, item);
                item.OnStoreRejected.Invoke(this, item);
                return false;
            }

            FlagSlots(item, region);
            ReflectiveSetContainer(item, this);
            OnStoredItem.Invoke(this, item);
            item.OnStoredItem.Invoke(this, item);
            return true;
        }

        /// <summary>
        /// Removes the item from inventory model. If the operation is rejected by a listener
        /// or it does not exist in this inventory then false is returned.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool RemoveItem(IGridItemModel item)
        {
            Assert.IsNotNull(item);

            if (item.Container != this)
                return false;

            PendingActionCancelled = false;
            OperationCancelAction opCan = new(() => PendingActionCancelled = true);
            OnWillRemoveItem.Invoke(this, item, opCan);
            item.OnWillRemoveItem.Invoke(this, item, opCan);
            var loc = GetLocation(item);

            if (PendingActionCancelled || loc == null)
            {
                OnRemoveRejected.Invoke(this, item);
                item.OnRemoveRejected.Invoke(this, item);
                return false;
            }

            //note that we call these BEFORE actually updating the model. very important for views if they want to know what to actually update
            OnRemovedItem.Invoke(this, item);
            item.OnRemovedItem.Invoke(this, item);
            FlagSlots(null, loc.Value);
            ReflectiveSetContainer(item, null);
            return true;
        }

        /// <summary>
        /// Forces an item to be removed from the inventory without checking for validation from outside listeners
        /// </summary>
        public void ForceRemoveItem(IGridItemModel item)
        {
            var loc = GetLocation(item);
            if (loc != null)
                FlagSlots(null, loc.Value);


            ReflectiveSetContainer(item, null);
            OnRemovedItem.Invoke(this, item);
            item.OnRemovedItem.Invoke(this, item);
        }

        /// <summary>
        /// Similar to ForceRemoveItem, this method is used to signal that an item has been ejected
        /// from the grid system entirely and is now at the mercy of the gamesystem as to what to do with it.
        /// Typically RemoveItem and ForceRemove item will be used for things like swapping, trading, and moving
        /// items around into different inventory containers. This is the point where the item is completely
        /// removed from that system.
        /// <param name="item"></param>
        public void DropItem(IGridItemModel item)
        {
            //if the item has a location in this model, also fire the remove events
            var loc = GetLocation(item);
            if (loc != null)
            {
                FlagSlots(null, loc.Value);
                OnRemovedItem.Invoke(this, item);
                item.OnRemovedItem.Invoke(this, item);
            }

            ReflectiveSetContainer(item, null);
            OnDroppedItem.Invoke(this, item);
            item.OnDroppedItem.Invoke(this, item);
        }

        /// <summary>
        /// Performs the task of dropping a dragged into into an inventory model with swap as needed. If successfull, the swapped item will be returned.
        /// If any cancellation action occur null is returned and the draggedItem and dropModel remain untouched.
        /// </summary>
        /// <param name="swapItem">The item in the dest model that is going to be swapped out for the draggedItem. It is assumed this item has been obtained via <see cref="IGridModel.CheckForSwappableItem(IGridItemModel, int, int)"/>.</param>
        /// <param name="draggedItem">The item currently being drag n dropped.</param>
        /// <param name="dropRegion">The location on the dropModel to drop the draggedItem.</param>
        /// <returns></returns>
        public bool SwapItems(IGridItemModel swapItem, IGridItemModel draggedItem, RectInt dropRegion)
        {
            if (swapItem != null)
            {
                var swappedItemOriginalRegion = GetLocation(swapItem);
                Assert.IsTrue(swappedItemOriginalRegion.HasValue);

                if (!RemoveItem(swapItem))
                    return false;

                if (!StoreItem(draggedItem, dropRegion))
                {
                    if (!StoreItem(swapItem, swappedItemOriginalRegion.Value))
                    {
                        //boy oh boy if this fails we are fucked, just drop the item and let the game figure out what to do with it, I guess?
                        var model = swapItem.Container ?? this;
                        model.DropItem(swapItem);
                    }
                    return false;
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="incoming"></param>
        /// <param name="receiver"></param>
        /// <param name="qty"></param>
        /// <returns></returns>
        public int StackItems(IGridItemModel incoming, IGridItemModel receiver, int qty)
        {
            Assert.IsNotNull(incoming);
            Assert.IsNotNull(receiver);
            Assert.IsTrue(qty > 0);
            if (!incoming.Shared.IsStackable || !receiver.Shared.IsStackable) return -1;
            if (incoming.Shared.StackId != receiver.Shared.StackId) return -1;
            if (incoming.StackCount < qty) return -1;

            var maxToMove = Mathf.Min(qty, receiver.MaxStackCount - receiver.StackCount);
            incoming.StackCount -= maxToMove;
            receiver.StackCount += maxToMove;

            OnStackedItem.Invoke(this, incoming, receiver);
            incoming.OnStackedItem.Invoke(this, incoming, receiver);
            receiver.OnStackedItem.Invoke(this, incoming, receiver);
            return maxToMove;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="qty"></param>
        /// <param name="instantiateAction">A function to execute that will generate a new item.</param>
        /// <returns></returns>
        public IGridItemModel SplitStackItem(IGridItemModel item, int qty, Func<IGridItemModel> instantiateAction)
        {
            Assert.IsNotNull(item);
            Assert.IsNotNull(instantiateAction);
            Assert.IsTrue(item.Shared.IsStackable);
            Assert.IsTrue(item.StackCount >= qty);

            if(item.StackCount == qty)
            {
                //let's play a dirty trick here, remove this item from the inventory and then return it as our 'new' stack
                if (!RemoveItem(item)) return null;
                OnStackSplitItem.Invoke(this, item, item);
                item.OnStackSplitItem.Invoke(this, item, item);
                return item;
            }

            var newItem = instantiateAction();
            Assert.IsNotNull(newItem);
            Assert.IsTrue(newItem.Shared.IsStackable);
            item.StackCount -= qty;
            newItem.StackCount = qty;

            OnStackSplitItem.Invoke(this, item, newItem);
            item.OnStackSplitItem.Invoke(this, item, newItem);
            newItem.OnStackSplitItem.Invoke(this, item, newItem);
            return newItem;
        }

        /// <summary>
        /// Ensures that all slots within the given region are indeed empty.
        /// </summary>
        /// <param name="xPos"></param>
        /// <param name="yPos"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public bool IsLocationEmpty(RectInt region)
        {
            foreach (var cell in CellsInRegion(region))
            {
                if (cell.Item != null) return false;
            }
            return true;
        }

        /// <summary>
        /// Ensures that all slots within the given region are indeed empty with the exception
        /// that it ignored the given item.
        /// </summary>
        /// <param name="region"></param>
        /// <param name="item">The item being checked against. If a cell has this item in it it will be ignored and the cell considered empty.</param>
        /// <param name="allowSwap">Can a single other item be in the location desitination that is valid for swapping?</param>
        /// <returns></returns>
        public bool IsLocationEmpty(RectInt region, IGridItemModel item)
        {
            foreach (var cell in CellsInRegion(region))
            {
                if (cell.Item != null && cell.Item != item)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Performs quick check to ensure the region is valid within the bounds of the grid.
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public bool ValidateRegion(RectInt region)
        {
            if (region.x < 0 || region.x + region.width > GridWidth)
                return false;
            if (region.y < 0 || region.y + region.height > GridHeight)
                return false;

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public GridCellModel GetCell(int x, int y)
        {
            Assert.IsTrue(ValidateRegion(new RectInt(x, y, 1, 1)));
            return CellModels[(y * GridWidth) + x];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public GridCellModel GetCell(int index)
        {
            Assert.IsTrue(index < CellModels.Count);
            return CellModels[index];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public GridCellModel FindCell(IGridItemModel item)
        {
            Assert.IsNotNull(item);
            foreach (var slot in CellModels)
                if (slot.Item == item)
                    return slot;
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public RectInt? GetLocation(IGridItemModel item)
        {
            Assert.IsNotNull(item);

            int total = GridWidth * GridHeight;
            for (int i = 0; i < total; i++)
            {
                if (CellModels[i].Item == item)
                {
                    int x = i % GridWidth;
                    int y = i / GridWidth;
                    return new RectInt(x, y, item.Size.x, item.Size.y);
                }
            }

            return null;
        }

        /// <summary>
        /// Sorts inventory into best-fit scenario via a simple but exponetial knapsack solver.
        /// This method could quickly get out of hand with larger data sets. Use with caution.
        /// </summary>
        public void SortInventory()
        {
            var result = ResolveItemsWithNewSize(_GridSize);
            Assert.IsNull(result, "The knapsack alogrithm looks like it failed.");
        }

        /// <summary>
        /// Clips a rect to fit within the confines of this model's grid bounds.
        /// Returns null if the source region is not within the grid at all.
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public RectInt ClipRegion(RectInt region)
        {
            int xMin = Mathf.Max(region.xMin, 0);
            int yMin = Mathf.Max(region.yMin, 0);
            int xMax = Mathf.Min(region.xMax, GridWidth);
            int yMax = Mathf.Min(region.yMax, GridHeight);

            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        /// <summary>
        /// Locates the first spot in the inventory with the given width and hieght and returns a region for it.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public RectInt? FindOpenSpace(int width, int height)
        {
            RectInt region = new(0, 0, width, height);

            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    region.x = x;
                    region.y = y;
                    if (ValidateRegion(region) && IsLocationEmpty(region))
                        return region;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns true if the item can be moved to the given location within the given model.
        /// The item ignores itself when checking to see if a cell is available.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="region"></param>
        public bool CanMoveItemToLocation(IGridItemModel item, RectInt region)
        {
            if (!ValidateRegion(region)) return false;
            return IsLocationEmpty(region, item);
        }

        /// <summary>
        /// Checks a region of space to see if there is exactly 1 IGridItemModel within
        /// and if there is enough room for the passed item to fit in that location if it
        /// were removed. If so, a reference to that single item is returned.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="xPos"></param>
        /// <param name="yPos"></param>
        /// <returns></returns>
        public IGridItemModel CheckForSwappableItem(IGridItemModel item, int xPos, int yPos)
        {
            var destRegion = new RectInt(xPos, yPos, item.Size.x, item.Size.y);

            IGridItemModel swapItem = null;
            foreach (var cell in CellsInRegion(destRegion))
            {
                if (swapItem == null && cell.Item != null)
                    swapItem = cell.Item;
                else if (cell.Item != null && swapItem != cell.Item)
                    return null;
            }

            return swapItem;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="xPos"></param>
        /// <param name="yPos"></param>
        /// <returns></returns>
        public IGridItemModel CheckForStackableItem(IGridItemModel item, int xPos, int yPos)
        {
            if (item.Shared.MaxStackSize < 2) return null;

            //first, we have to ensure that there is exactly only one item here
            //we can find out using the swap check method
            var stackItem = CheckForSwappableItem(item, xPos, yPos);
            if (stackItem == null) return null;

            //now let's compare stackability
            if (stackItem.MaxStackCount < 2) return null;
            if(stackItem.Shared.StackId.Hash != item.Shared.StackId.Hash) return null;
            return stackItem;
        }

        #endregion


        #region Static Methods
        /// <summary>
        /// This breaks every rule of OOP or so they say but really it just makes it easier for me since
        /// I don't have to remember or leave a comment about whether or not I should call 'remove()' from
        /// the item or the container itself. It'll work with both using this technique. So nyeeeeah! *sticks thumb on nose*
        /// </summary>
        static void ReflectiveSetContainer(IGridItemModel item, IGridModel container)
        {
            var type = item.GetType();
            type.GetProperty(nameof(item.Container)).SetValue(item, container);
        }
        #endregion
    }
}
