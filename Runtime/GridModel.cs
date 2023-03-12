using Sirenix.OdinInspector;
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
    ///     -stacking
    ///     -stack splitting
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
        [Space(12)]
        [SerializeField][HideInInspector] UnityEvent<IGridModel, Vector2Int> _OnGridSizeChanged = new UnityEvent<IGridModel, Vector2Int>();
        [ShowInInspector][FoldoutGroup("Events")] public UnityEvent<IGridModel, Vector2Int> OnGridSizeChanged { get => _OnGridSizeChanged; set => _OnGridSizeChanged = value; }

        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> _OnWillStoreItem = new UnityEvent<IGridModel, IGridItemModel, OperationCancelAction>();
        [ShowInInspector][FoldoutGroup("Store Events")] public UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> OnWillStoreItem { get => _OnWillStoreItem; set => _OnWillStoreItem = value; }

        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel> _OnStoredItem = new UnityEvent<IGridModel, IGridItemModel>();
        [ShowInInspector][FoldoutGroup("Store Events")] public UnityEvent<IGridModel, IGridItemModel> OnStoredItem { get => _OnStoredItem; set => _OnStoredItem = value; }

        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel> _OnStoreRejected = new UnityEvent<IGridModel, IGridItemModel>();
        [ShowInInspector][FoldoutGroup("Store Events")] public UnityEvent<IGridModel, IGridItemModel> OnStoreRejected { get => _OnStoreRejected; set => _OnStoreRejected = value; }

        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> _OnWillRemoveItem = new UnityEvent<IGridModel, IGridItemModel, OperationCancelAction>();
        [ShowInInspector][FoldoutGroup("Remove Events")] public UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> OnWillRemoveItem { get => _OnWillRemoveItem; set => _OnWillRemoveItem = value; }

        [SerializeField][HideInInspector] public UnityEvent<IGridModel, IGridItemModel> _OnRemovedItem = new UnityEvent<IGridModel, IGridItemModel>();
        [ShowInInspector][FoldoutGroup("Remove Events")] public UnityEvent<IGridModel, IGridItemModel> OnRemovedItem { get => _OnRemovedItem; set => _OnRemovedItem = value; }

        [SerializeField][HideInInspector] public UnityEvent<IGridModel, IGridItemModel> _OnRemoveRejected = new UnityEvent<IGridModel, IGridItemModel>();
        [ShowInInspector][FoldoutGroup("Remove Events")] public UnityEvent<IGridModel, IGridItemModel> OnRemoveRejected { get => _OnRemoveRejected; set => _OnRemoveRejected = value; }
        #endregion


        #region Private Fields
        List<GridCellModel> CellModels;
        static bool PendingActionCancelled;
        #endregion


        #region Private Methods
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

        /// <summary>
        /// 
        /// </summary>
        public void OnEnable()
        {
            ConstructGrid();
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
            if (item.Container != this)
                return;

            var loc = GetLocation(item);
            if (loc == null)
                return;

            FlagSlots(null, loc.Value);
            ReflectiveSetContainer(item, null);
            OnRemovedItem.Invoke(this, item);
            item.OnRemovedItem.Invoke(this, item);
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
            Assert.IsTrue(ValidateRegion(region));

            for (int y = region.y; y < region.y + region.height; y++)
            {
                for (int x = region.x; x < region.x + region.width; x++)
                {
                    int index = (y * GridWidth) + x;
                    if (CellModels[index].Item != null)
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Ensures that all slots within the given region are indeed empty with the exception
        /// that it ignored the given item.
        /// </summary>
        /// <param name="region"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool IsLocationEmpty(RectInt region, IGridItemModel item)
        {
            Assert.IsTrue(ValidateRegion(region));

            for (int y = region.y; y < region.y + region.height; y++)
            {
                for (int x = region.x; x < region.x + region.width; x++)
                {
                    var slotItem = CellModels[(y * GridWidth) + x].Item;
                    if (slotItem != null && slotItem != item)
                        return false;
                }
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
        #endregion

    }
}
