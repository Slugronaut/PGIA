using System.Collections.Generic;
using Toolbox;
using UnityEngine;

namespace PGIA
{
    /// <summary>
    /// Simple list of hashed strings that can be used for filtering logic.
    /// </summary>
    [RequireComponent(typeof(GridModelBehaviour))]
    public class AllowedItemCategories : MonoBehaviour
    {
        [Tooltip("A list of allowed item categories for this model.")]
        public List<HashedString> AllowedCategories;


        private void Awake()
        {
            var models = GetComponents<GridModelBehaviour>();
            foreach(var model in models)
                model.OnWillStoreItem.AddListener(HandleFilterItems);
        }

        private void OnDestroy()
        {
            var models = GetComponents<GridModelBehaviour>();
            foreach(var model in models)
                model.OnWillStoreItem.RemoveListener(HandleFilterItems);
        }

        void HandleFilterItems(IGridModel model, IGridItemModel item, OperationCancelAction cancelAction)
        {
            var cats = item.Shared.Category;
            foreach(var c in AllowedCategories)
            {
                if (c.Hash == cats.Hash)
                    return;
            }

            cancelAction.Cancel();
        }
    }
}
