using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace PGIA
{
    /// <summary>
    /// Interface for a grid-based inventory item backend in PGIA.
    /// This implementation is MonoBehaviour based and uses a <see cref="GridItemModel"/>
    /// as its backing data.
    /// </summary>
    public class GridItemModelBehaviour : MonoBehaviour, IGridItemModel
    {
        [PropertyOrder(-1)]
        public int MaxStackCount { get => BackingModel.MaxStackCount; }
        [PropertyOrder(-1)]
        public int StackCount { get => BackingModel.StackCount; set => BackingModel.StackCount = value; }
        [PropertyOrder(-1)]
        [ShowInInspector]
        [ReadOnly]
        public System.Guid Guid { get => BackingModel.Guid; }

        
        [Tooltip("Shared state information for all instances of this item.")]
        [ShowInInspector]
        public InventoryItemAsset Shared
        {
            get => BackingModel.Shared;
            private set
            {
                var type = BackingModel.GetType();
                type.GetProperty(nameof(BackingModel.Shared)).SetValue(BackingModel, value);
            }
        }
        public Vector2Int Size => BackingModel.Shared.Size;
        public IGridModel Container
        { 
            get => BackingModel.Container; 
            private set
            {
                var type = BackingModel.GetType();
                type.GetProperty(nameof(BackingModel.Container)).SetValue(BackingModel, value);
            }
        }

        [SerializeField]
        GridItemModel BackingModel = new();


        #region Events
        [PropertySpace(12)][ShowInInspector][FoldoutGroup("Store Events")] public UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> OnWillStoreItem { get => BackingModel.OnWillStoreItem; set => BackingModel.OnWillStoreItem = value; }
        [ShowInInspector][FoldoutGroup("Store Events")] public UnityEvent<IGridModel, IGridItemModel> OnStoredItem { get => BackingModel.OnStoredItem; set => BackingModel.OnStoredItem = value; }
        [ShowInInspector][FoldoutGroup("Store Events")] public UnityEvent<IGridModel,IGridItemModel> OnStoreRejected { get => BackingModel.OnStoreRejected; set => BackingModel.OnStoreRejected = value; }
        [PropertySpace(12)][ShowInInspector][FoldoutGroup("Remove Events")] public UnityEvent<IGridModel, IGridItemModel, OperationCancelAction> OnWillRemoveItem { get => BackingModel.OnWillRemoveItem; set => BackingModel.OnWillRemoveItem = value; }
        [ShowInInspector][FoldoutGroup("Remove Events")] public UnityEvent<IGridModel, IGridItemModel> OnRemovedItem { get => BackingModel.OnRemovedItem; set => BackingModel.OnRemovedItem = value; }
        [ShowInInspector][FoldoutGroup("Remove Events")] public UnityEvent<IGridModel, IGridItemModel> OnRemoveRejected { get => BackingModel.OnRemoveRejected; set => BackingModel.OnRemoveRejected = value; }
        [ShowInInspector][FoldoutGroup("Remove Events")] public UnityEvent<IGridModel, IGridItemModel> OnDroppedItem { get => BackingModel.OnDroppedItem; set => BackingModel.OnDroppedItem = value; }
        [PropertySpace(12)][ShowInInspector][FoldoutGroup("Stack Events")] public UnityEvent<IGridModel, IGridItemModel, IGridItemModel> OnStackedItem { get => BackingModel.OnStackedItem; set => BackingModel.OnStackedItem = value; }
        [ShowInInspector][FoldoutGroup("Stack Events")] public UnityEvent<IGridModel, IGridItemModel, IGridItemModel> OnStackSplitItem { get => BackingModel.OnStackSplitItem; set => BackingModel.OnStackSplitItem = value; }

        #endregion
    }
}
