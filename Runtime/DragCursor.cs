using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace PGIA
{
    /// <summary>
    /// To be shared between ModelViews, this represents the ircon of an item as it is being dragged around.
    /// </summary>
    [CreateAssetMenu(fileName = "Drag Cursor", menuName = "PGIA/Drag Cursor")]
    public class DragCursor : ScriptableObject
    {
        [Tooltip("A UI document describing the cursor.")]
        public VisualTreeAsset CursorAsset;

        GridCellView SourceCellView;
        IGridItemModel Item;

        VisualElement CursorScreen;
        VisualElement CursorRoot;
        public bool IsDragging => Item != null;
        bool Initialized = false;
        int CellOffsetX;
        int CellOffsetY;

        #region UNITY_EDITOR
        /// <summary>
        /// We need this bit of editor-only logic so that we can reset the Initialized state in the editor
        /// since SO assets referenced in a scene will come into existance at asignment and persist until
        /// domain reload.
        /// </summary>
        private void OnEnable()
        {
            Initialized = false;
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
        }

        void HandlePlayModeChanged(PlayModeStateChange state)
        {
            Initialized = false;
        }
        #endregion


        /// <summary>
        /// Currently, this is called by GridViewModel upon it's own initialization.
        /// If it has already been called once by any source, further calls will do nothing.
        /// </summary>
        public void Initialize(VisualElement cursorScreenRoot)
        {
            Assert.IsNotNull(cursorScreenRoot, "You must specify a valid UI Document root for the cursor container.");
            if (Initialized) return;
            Initialized = true;
            CursorScreen = cursorScreenRoot;
            CursorRoot = CursorAsset.Instantiate();
            CursorScreen.Add(CursorRoot);

            CursorRoot.SetEnabled(true);
            CursorRoot.pickingMode = PickingMode.Ignore;
            CursorRoot.style.position = Position.Absolute;
            CursorRoot.style.flexShrink = 0;
            CursorRoot.style.flexGrow = 0;
            CursorRoot.style.visibility = Visibility.Hidden;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="view"></param>
        /// <param name="srouceModel"></param>
        /// <param name="item"></param>
        public void BeginDrag(PointerDownEvent evt, GridCellView srcCellView)
        {
            if (srcCellView.Cell.Item == null) return;

            Item = srcCellView.Cell.Item;
            SourceCellView = srcCellView;

            CursorScreen.RegisterCallback<PointerMoveEvent>(HandleMove);
            CursorRoot.style.visibility = Visibility.Visible;
            CursorRoot.style.backgroundImage = new StyleBackground(Item.Shared.Icon);
            CursorRoot.style.width = srcCellView.CellUI.style.width;
            CursorRoot.style.height = srcCellView.CellUI.style.height;
            CursorRoot.pickingMode = PickingMode.Ignore; //this seems to be bugged. setting it via the stylesheets in the ui builder works though
            SetCursorPosition(evt.position.x, evt.position.y);

            //we are removing the item from the view but not the model. that way if something goes catastrophically wrong
            //we can recover the view by simply pushing the full model back in to update it. maybe. i think.
            srcCellView.GridView.HandleRemovedItem(Item.Container, Item);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="cellView"></param>
        public void DragHoverEnter(GridCellView cellView)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="cellView"></param>
        public void DragHoverExit(GridCellView cellView)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public void Drop(PointerUpEvent evt, GridCellView destCellView)
        {
            if (!IsDragging) return; //just in case we click elsewhere without starting a drag and then release over a slot
            //Assert.IsNull(destSlot.Slot.Item); //we don't use this cause we haven't actually moved the fucker yet
            Assert.IsNotNull(Item);

            //first we need to actually put the item back in the visuals otherwise bad things will happen
            SourceCellView.GridView.HandleStoredItem(Item.Container, Item);

            //now we can request an actual model-backed move which should
            //propgate the visual updates for both src and dest models.
            GridView.RequestMoveItem(Item, destCellView.GridView, destCellView.X, destCellView.Y);
            Cancel();
        }

        /// <summary>
        /// Simply cleans up any visual effects 
        /// </summary>
        public void Cancel()
        {
            CursorScreen.UnregisterCallback<PointerMoveEvent>(HandleMove);
            CursorRoot.style.visibility = Visibility.Hidden;
            SourceCellView = null;
            Item = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        void HandleMove(PointerMoveEvent evt)
        {
            SetCursorPosition(evt.position.x, evt.position.y);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        void SetCursorPosition(float x, float y)
        {
            CursorRoot.style.left = new StyleLength(x - (CursorRoot.style.width.value.value * 0.5f));
            CursorRoot.style.top = new StyleLength(y - (CursorRoot.style.height.value.value * 0.5f));
        }
    }
}
