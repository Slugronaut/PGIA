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
        public int MaxStackCount { get; set; } = 1;
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

        #region Events
        [Space(12)]
        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> _OnWillStoreItem;
        [ShowInInspector][FoldoutGroup("Store Events")] public UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> OnWillStoreItem { get => _OnWillStoreItem; set => _OnWillStoreItem = value; }

        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel> _OnStoredItem;
        [ShowInInspector][FoldoutGroup("Store Events")] public UnityEvent<IGridModel, IGridItemModel> OnStoredItem { get => _OnStoredItem; set => _OnStoredItem = value; }

        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel> _OnStoreRejected;
        [ShowInInspector][FoldoutGroup("Store Events")] public UnityEvent<IGridModel, IGridItemModel> OnStoreRejected { get => _OnStoreRejected; set => _OnStoreRejected = value; }

        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> _OnWillRemoveItem;
        [ShowInInspector][FoldoutGroup("Remove Events")] public UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> OnWillRemoveItem { get => _OnWillRemoveItem; set => _OnWillRemoveItem = value; }

        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel> _OnRemovedItem;
        [ShowInInspector][FoldoutGroup("Remove Events")] public UnityEvent<IGridModel, IGridItemModel> OnRemovedItem { get => _OnRemovedItem; set => _OnRemovedItem = value; }

        [SerializeField][HideInInspector] UnityEvent<IGridModel, IGridItemModel> _OnRemoveRejected;
        [ShowInInspector][FoldoutGroup("Remove Events")] public UnityEvent<IGridModel, IGridItemModel> OnRemoveRejected { get => _OnRemoveRejected; set => _OnRemoveRejected = value; }
        #endregion
    }
}
