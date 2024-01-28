using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace PGIA
{
    /// <summary>
    /// This script can be attached to a UIDocument and act as a way to allow elements within that doc to accept
    /// item drop actions. Upon drag-dropping them the item will either be destroyed or simply removed from the inventory
    /// based on configured settings.
    /// </summary>
    public class TrashCan : MonoBehaviour
    {
        public enum Operations
        {
            Destroy,
            TriggerDropEvent,
        }

        [Tooltip("The UIDocument that contains the drop region UI.")]
        public UIDocument Document;

        [Tooltip("The name of the element within the document that actually defines the drop region.")]
        public string DropContainerId;

        [Tooltip("What happens when an item is dragged and dropped here and is successfully removed from the inventory?")]
        public Operations OperationOnTrash;

        [HideIf("IsDropOp")]
        public UnityEvent<IGridItemModel, IGridModel, GridCellView> OnDestroyed;
        [ShowIf("IsDropOp")]
        public UnityEvent<IGridItemModel, IGridModel, GridCellView> OnDropped;

        bool IsDropOp => OperationOnTrash == Operations.TriggerDropEvent;

        VisualElement RootUI;

        void OnEnable()
        {
            
            RootUI = Utilities.ParseQueryPath(Document.rootVisualElement, DropContainerId);
            RootUI.RegisterCallback<PointerUpEvent>(HandlePointerUp);

            //RootUI.RegisterCallback<PointerDownEvent>(HandlePointerDown);
            //RootUI.RegisterCallback<PointerEnterEvent>(HandlePointerEnter);
            //RootUI.RegisterCallback<PointerLeaveEvent>(HandlePointerLeave);
        }

        private void OnDisable()
        {
            RootUI.UnregisterCallback<PointerUpEvent>(HandlePointerUp);

            //RootUI.UnregisterCallback<PointerDownEvent>(HandlePointerDown);
            //RootUI.UnregisterCallback<PointerEnterEvent>(HandlePointerEnter);
            //RootUI.UnregisterCallback<PointerLeaveEvent>(HandlePointerLeave);
        }

        void TrashItem()
        {
            //dogshit code alert: yikes! we're accessing an internal shared state here... careful with that!
            var ds = GridViewBehaviour.DragSource;
            if (ds == null) return;

            var item = ds.Item;
            var model = ds.Model;
            var cellView = ds.CellView;
            //cancel the drag so that the item returns to the model (if possible)
            ds.GridView.Cancel();
            model.DropItem(item);

            switch (OperationOnTrash)
            {
                case Operations.Destroy:
                    {
                        //NOTE: we only destroy if it's actually behaviour-based, otherwise ther really is nothing
                        //      to destroy and we simply release the ref and let the gc do its thing later
                        var itemBeh = item as GridItemModelBehaviour;
                        if (itemBeh != null)
                            Destroy(itemBeh.gameObject);
                        OnDestroyed.Invoke(item, model, cellView);
                        break;
                    }
                case Operations.TriggerDropEvent:
                    {
                        //let the client decide how to handle 'dropping on the ground'
                        OnDropped.Invoke(item, model, cellView);
                        break;
                    }
            }
            //TODO: 
            // - get the global drag operation object. this will tell us what is actually being transported and from where
            // - cancel the drag on the view
            // - get the item from the associated model and remove it
            // - provide link to item in event handler so that we can let client decide where to go from there (destroy the item, drop it on ground, etc...)

            //GridView.Drop(this, evt.localPosition);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        void HandlePointerUp(PointerUpEvent evt)
        {
            TrashItem();
        }

        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        void HandlePointerEnter(PointerEnterEvent evt)
        {
            //TODO: change the cursor icon to indicate trash
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        void HandlePointerLeave(PointerLeaveEvent evt)
        {
            //TODO: change cursor to normal

            //Hovered = false;
            //GridView.PointerHoverExit(this, evt.localPosition);
        }
    }
}
