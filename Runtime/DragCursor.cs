using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace PGIA
{
    /// <summary>
    /// To be shared between ModelViews, this represents the ircon of an item as it is being dragged around.
    /// 
    /// TODO:
    ///     -sticky drags
    ///     -hilight on non-drag hovering
    ///     -item swapping
    ///     -'bumping' - re-adjusting multi-cell drop locations to account for bounds of grid
    ///     -background hilighting for multi-cell items is flawed, it should not include cells below the 'root' or it will stack color effects when transparent
    ///     
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
            if (!IsDragging)
            {
                LastHoveredCells = CoveredCells(null, cellView, 0, 0);
                TintLastHoveredCells(cellView.GridView.Shared.HilightColorBackground, cellView.GridView.Shared.HilightColorIcon);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="cellView"></param>
        public void PointerHoverExit(PointerLeaveEvent evt, GridCellView cellView)
        {
            if(!IsDragging)
                TintLastHoveredCells(cellView.GridView.Shared.DefaultColorBackground, cellView.GridView.Shared.DefaultColorIcon);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="cellView"></param>
        public void CellPointerMoved(PointerMoveEvent evt, GridCellView cellView)
        {
            if (IsDragging)
                HilightForDragging(evt.localPosition, cellView);
            
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="cellView"></param>
        void HilightForDragging(Vector2 localPosition, GridCellView cellView)
        {
            //clear out previous cells if any
            TintLastHoveredCells(cellView.GridView.Shared.DefaultColorBackground, cellView.GridView.Shared.DefaultColorIcon);

            var region = new RectInt(cellView.X - CellOffsetX, cellView.Y - CellOffsetY, Item.Size.x, Item.Size.y);
            Color bgColor;
            Color iconTint;
            if (cellView.GridView.Model.CanMoveItemToLocation(Item, region))
            {
                bgColor = cellView.GridView.Shared.ValidColorBackground;
                iconTint = cellView.GridView.Shared.ValidColorIcon;
            }
            else
            {
                bgColor = cellView.GridView.Shared.InvalidColorBackground;
                iconTint = cellView.GridView.Shared.InvalidColorIcon;
            }


            //we have two potential problems here
            //1) the cellView is stretched over others so we don't have knowledge of the 'true' overlapped cells.
            //2) the covered cells that we calculate below might include one such overlapped cell in which case we'd also
            //   want the overlapper itself.
            //In order to fix this we need to find the true cell being hovered regardless of overlap
            var trueHoveredCell = FindHoveredGridCell(cellView.CellUI.LocalToWorld(localPosition), cellView.GridView);

            LastHoveredCells = CoveredCells(Item, trueHoveredCell, CellOffsetX, CellOffsetY);
            TintLastHoveredCells(bgColor, iconTint);
        }

        /// <summary>
        /// Due to the fact that multi-cell items display their item by stetching the first cell
        /// of the region over the others it becomes difficult to determine the actual within
        /// the grid itself that would be hovered in such situations.
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public static GridCellView FindHoveredGridCell(Vector2 pointerWorld, GridView gridView)
        {
            pointerWorld = gridView.GridRootUI.WorldToLocal(pointerWorld);
            pointerWorld -= gridView.GridLocalOffset;
            pointerWorld.x = Mathf.Max(1, pointerWorld.x);
            pointerWorld.y = Mathf.Max(1, pointerWorld.y);
            pointerWorld.x = Mathf.Min(pointerWorld.x, gridView.GridMaxPoint.x);
            pointerWorld.y = Mathf.Min(pointerWorld.y, gridView.GridMaxPoint.y);


            var cellHoveredX = (int)pointerWorld.x / (int)gridView.CellWidth;
            var cellHoveredY = (int)pointerWorld.y / (int)gridView.CellHeight;

            //this result can give us results beyond the bounds of the grid, clamp that here
            if (cellHoveredX >= gridView.Model.GridWidth)
                cellHoveredX = gridView.Model.GridWidth - 1;
            if(cellHoveredY >= gridView.Model.GridHeight)
                cellHoveredY = gridView.Model.GridHeight - 1;

            return gridView.GetCellView(gridView.Model.GridWidth, cellHoveredX, cellHoveredY);
        }

        static readonly List<GridCellView> TempList1 = new();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="destCell"></param>
        /// <param name="xCellOffset"></param>
        /// <param name="yCellOffset"></param>
        /// <returns></returns>
        static List<GridCellView> CoveredCells(IGridItemModel item, GridCellView destCellView, int xCellOffset, int yCellOffset)
        {
            var region = new RectInt(destCellView.X - xCellOffset, destCellView.Y - yCellOffset, item?.Size.x ?? 1, item?.Size.y ?? 1);
            region = destCellView.GridView.Model.ClipRegion(region);
            var rawCells = destCellView.GridView.GetCellViews(destCellView.GridView.Model.GridWidth, region);

            //we also need to account for any cells in the list that may be strecthing over
            //other cells or are being stretched over themselves due to multi-celled items.
            TempList1.Clear();
            foreach(var cell in rawCells)
            {
                if (cell.RootCellView != null)
                    TempList1.Add(cell.RootCellView);
                if(cell.OverlappedCellViews != null)
                {
                    /*
                    foreach (var subCell in cell.OverlappedCellViews)
                        if (subCell != null)
                            TempList1.Add(subCell);
                    */
                }    
            }
            rawCells.AddRange(TempList1);
            return rawCells;
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

            TintLastHoveredCells(SourceCellView.GridView.Shared.DefaultColorBackground, SourceCellView.GridView.Shared.DefaultColorIcon);
            LastHoveredCells = null;
            CursorRoot.style.visibility = Visibility.Hidden;
            SourceCellView = null;
            Item = null;

            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="backgroundColor"></param>
        void TintLastHoveredCells(Color backgroundColor, Color iconTint)
        {
            if (LastHoveredCells != null)
            {
                foreach (var cell in LastHoveredCells)
                {
                    cell.CellUI.style.backgroundColor = backgroundColor;
                    if(cell.CellUI.style.backgroundImage != null)
                    {
                        cell.CellUI.style.unityBackgroundImageTintColor = iconTint;
                    }
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
