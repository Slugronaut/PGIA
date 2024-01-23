using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace PGIA
{
    /// <summary>
    /// Backend that defines an inventory item that can be stored in a
    /// grid-based inventory represented by the IGridModel interface.
    /// </summary>
    [System.Serializable]
    public class GridItemModel : IGridItemModel
    {
        [PropertyOrder(-1)]
        public int MaxStackCount { get => _Shared.MaxStackSize; }
        [PropertyOrder(-1)]
        public int StackCount { get; set; } = 1;
        [PropertyOrder(-1)]
        [ShowInInspector]
        [ReadOnly]
        public System.Guid Guid { get; private set; } = System.Guid.NewGuid();

        [SerializeReference]
        InventoryItemAsset _Shared;
        [Tooltip("Shared state information for all instances of this item.")]
        public InventoryItemAsset Shared { get => _Shared; private set => _Shared = value; }
        public Vector2Int Size => _Shared.Size;
        public IGridModel Container { get; private set; } //set via GridModel

        //These default values can be overriden in derived classes if you want per-item custom background cell colors.
        public virtual bool OverrideBackgroundColor { get => false; }
        public virtual Color CustomBackgroundColor => Shared.Background;


        /// <summary>
        /// Primarily here as a means to allow the mono-behaviour version to initialize.
        /// Generally you should use <see cref="GridItemModel(InventoryItemAsset shared)"/> 
        /// instead if you are manually instantiating this object.
        /// </summary>
        public GridItemModel()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="shared"></param>
        public GridItemModel(InventoryItemAsset shared)
        {
            _Shared = shared;
        }

        #region Events
        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> _OnWillStoreItem = new();
        [PropertySpace(12)][ShowInInspector][FoldoutGroup("Store Events")] public UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> OnWillStoreItem { get => _OnWillStoreItem; set => _OnWillStoreItem = value; }

        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel> _OnStoredItem = new();
        [ShowInInspector][FoldoutGroup("Store Events")] public UnityEvent<IGridModel, IGridItemModel> OnStoredItem { get => _OnStoredItem; set => _OnStoredItem = value; }

        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel> _OnStoreRejected = new();
        [ShowInInspector][FoldoutGroup("Store Events")] public UnityEvent<IGridModel, IGridItemModel> OnStoreRejected { get => _OnStoreRejected; set => _OnStoreRejected = value; }

        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> _OnWillRemoveItem = new();
        [PropertySpace(12)][ShowInInspector][FoldoutGroup("Remove Events")] public UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> OnWillRemoveItem { get => _OnWillRemoveItem; set => _OnWillRemoveItem = value; }

        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel> _OnRemovedItem = new();
        [ShowInInspector][FoldoutGroup("Remove Events")] public UnityEvent<IGridModel, IGridItemModel> OnRemovedItem { get => _OnRemovedItem; set => _OnRemovedItem = value; }

        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel> _OnRemoveRejected = new();
        [ShowInInspector][FoldoutGroup("Remove Events")] public UnityEvent<IGridModel, IGridItemModel> OnRemoveRejected { get => _OnRemoveRejected; set => _OnRemoveRejected = value; }

        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel> _OnDroppedItem = new();
        [ShowInInspector][FoldoutGroup("Remove Events")] public UnityEvent<IGridModel, IGridItemModel> OnDroppedItem { get => _OnDroppedItem; set => _OnDroppedItem = value; }

        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel, IGridItemModel> _OnStackedItem = new();
        [PropertySpace(12)][ShowInInspector][FoldoutGroup("Stack Events")] public UnityEvent<IGridModel, IGridItemModel, IGridItemModel> OnStackedItem { get => _OnStackedItem; set => _OnStackedItem = value; }

        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel, IGridItemModel> _OnStackSplitItem = new();
        [ShowInInspector][FoldoutGroup("Stack Events")] public UnityEvent<IGridModel, IGridItemModel, IGridItemModel> OnStackSplitItem { get => _OnStackSplitItem; set => _OnStackSplitItem = value; }

        #endregion
    }
}
