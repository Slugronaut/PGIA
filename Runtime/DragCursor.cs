using System.Collections.Generic;
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
        int PointerOffsetX;
        int PointerOffsetY;
        List<GridCellView> LastHoveredCells;

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
            CursorRoot.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="view"></param>
        /// <param name="srouceModel"></param>
        /// <param name="item"></param>
        public void BeginDrag(PointerDownEvent evt, GridCellView clickedCellView)
        {
            if (clickedCellView.Cell.Item == null) return;

            Item = clickedCellView.Cell.Item;
            SourceCellView = clickedCellView;

            CursorScreen.RegisterCallback<PointerMoveEvent>(HandleMove);
            CursorRoot.style.visibility = Visibility.Visible;
            CursorRoot.style.backgroundImage = new StyleBackground(Item.Shared.Icon);
            CursorRoot.style.width = clickedCellView.CellUI.style.width;
            CursorRoot.style.height = clickedCellView.CellUI.style.height;
            CursorRoot.pickingMode = PickingMode.Ignore; //this seems to be bugged. setting it via the stylesheets in the ui builder works though
            

            //NOTE: It is vital to remember that when an item is multi-celled, the root cell (i.e. the top-left cell)
            //is stretched over all others and thus it is the only one that can be interacted with so it will always
            //be the cell passed to this function as 'clickedCellView'.

            //get the actual cell grabbed by and store that offset from the top-left cell of the item,
            //we'll need it later for placement calculations
            var offset = CalculateCellOffsetFromLocalCoords(clickedCellView, evt.localPosition);
            CellOffsetX = offset.x;
            CellOffsetY = offset.y;
            PointerOffsetX = (int)evt.localPosition.x;
            PointerOffsetY = (int)evt.localPosition.y;

            //we are removing the item from the view but not the model. that way if something goes catastrophically wrong
            //we can recover the view by simply pushing the full model back in to update it. maybe. i think.
            clickedCellView.GridView.HandleRemovedItem(Item.Container, Item);
            SetCursorPosition(evt.position.x, evt.position.y);

        }

        /// <summary>
        /// Helper method to figure out which cell was actually clicked in a multi-celled item.
        /// </summary>
        /// <param name="local"></param>
        /// <returns></returns>
        Vector2Int CalculateCellOffsetFromLocalCoords(GridCellView rootCell, Vector2 local)
        {
            return new Vector2Int(
                (int)local.x / (int)rootCell.GridView.CellWidth,
                (int)local.y / (int)rootCell.GridView.CellHeight
                );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="cellView"></param>
        public void PointerHoverEnter(PointerEnterEvent evt, GridCellView cellView)
        {
            if (!IsDragging) return;

            var region = new RectInt(cellView.X - CellOffsetX, cellView.Y - CellOffsetY, Item.Size.x, Item.Size.y);
            Color color = cellView.GridView.Model.CanMoveItemToLocation(Item, region) ? cellView.GridView.Shared.ValidColor
                                                                             : cellView.GridView.Shared.InvalidColor;
            LastHoveredCells = CoveredCells(Item, cellView, CellOffsetX, CellOffsetY);
            TintLastHoveredCells(color);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="cellView"></param>
        public void PointerHoverExit(PointerLeaveEvent evt, GridCellView cellView)
        {
            if (!IsDragging) return;

            LastHoveredCells = CoveredCells(Item, cellView, CellOffsetX, CellOffsetY);
            TintLastHoveredCells(cellView.GridView.Shared.DefaultColor);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="destCell"></param>
        /// <param name="xCellOffset"></param>
        /// <param name="yCellOffset"></param>
        /// <returns></returns>
        static List<GridCellView> CoveredCells(IGridItemModel item, GridCellView destCellView, int xCellOffset, int yCellOffset)
        {
            var region = new RectInt(destCellView.X - xCellOffset, destCellView.Y - yCellOffset, item.Size.x, item.Size.y);
            region = destCellView.GridView.Model.ClipRegion(region);
            return destCellView.GridView.GetCellViews(destCellView.GridView.Model.GridWidth, region);
        }
        

        /// <summary>
        /// 
        /// </summary>
        public void Drop(PointerUpEvent evt, GridCellView clickedCellView)
        {
            if (!IsDragging) return; //just in case we click elsewhere without starting a drag and then release over a slot
            //Assert.IsNull(destSlot.Slot.Item); //we don't use this cause we haven't actually moved the fucker yet
            Assert.IsNotNull(Item);

            //first we need to actually put the item back in the visuals otherwise bad things will happen
            SourceCellView.GridView.HandleStoredItem(Item.Container, Item);

            //now we can request an actual model-backed move which should
            //propgate the visual updates for both src and dest models.
            GridView.RequestMoveItem(Item, clickedCellView.GridView, clickedCellView.X - CellOffsetX, clickedCellView.Y - CellOffsetY);
            Cancel();
        }

        /// <summary>
        /// Simply cleans up any visual effects 
        /// </summary>
        public void Cancel()
        {
            CursorScreen.UnregisterCallback<PointerMoveEvent>(HandleMove);

            TintLastHoveredCells(SourceCellView.GridView.Shared.DefaultColor);
            LastHoveredCells = null;
            CursorRoot.style.visibility = Visibility.Hidden;
            SourceCellView = null;
            Item = null;

            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="color"></param>
        void TintLastHoveredCells(Color color)
        {
            if (LastHoveredCells != null)
            {
                foreach (var cell in LastHoveredCells)
                {
                    cell.CellUI.style.backgroundColor = color;
                }
            }
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
            CursorRoot.style.left = new StyleLength(x - PointerOffsetX);// - (CursorRoot.style.width.value.value * 0.5f));
            CursorRoot.style.top = new StyleLength(y - PointerOffsetY);// - (CursorRoot.style.height.value.value * 0.5f));
        }


    }
}
