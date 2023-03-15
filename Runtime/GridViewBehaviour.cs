using AdvLifeSim;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TPUModelerEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using static UnityEditor.Progress;
using Tooltip = UnityEngine.TooltipAttribute;

namespace PGIA
{
    /// <summary>
    /// The controller interfaces between the GridModel and the UIElements used to visualize it.
    /// This would be the 'C' in MVC or the 'VM' in MMVM design patterns.
    /// 
    /// 
    /// 
    /// TODO:
    ///     -problem when overlapping odd-numbered mutli-cell items, tends to drift too far
    ///     -stacking process:
    ///         -1) target has enough room, source is destroyed
    ///         -2) target does not have enough room, source returns to it's destintion in full
    ///             -if destination not available, drop total
    ///         -3) target has partial room, source returned to destination in part
    ///             -if destination jot available, drop difference
    ///             
    ///     -stack splitting process
    /// </summary>
    public class GridViewBehaviour : MonoBehaviour
    {
        #region Public Fields and Properties
        [SerializeReference]
        [SerializeField]
        [HideInInspector]
        GridModelBehaviour _Model; //note that we have used to concrete type here for the sake of serialization. there are workarounds to avoid this but I can't be fucked to bother right now
        [ShowInInspector]
        [Tooltip("The inventory that this grid will represent.")]
        public IGridModel Model
        {
            get => _Model;
            set
            {
#pragma warning disable CS0253 // Possible unintended reference comparison; right hand side needs cast
                if (_Model != value)
                {
                    _Model = (GridModelBehaviour)value; //again, casting to concrete type. can't be fucked
                    if (Application.isPlaying)
                        PushModelToView();
                }
#pragma warning restore CS0253 // Possible unintended reference comparison; right hand side needs cast
            }
        }
        [Tooltip("The document that holds the UI data.")]
        public UIDocument View;
        public VisualTreeAsset CellUIPrefab;
        [Tooltip("An asset that stores common properties that are often shared by many grids.")]
        public GridViewAsset Shared;
        [Tooltip("The name of the visual element of the supplied UI document that will contain this grid.")]
        public string GridContainerId = "GridContainer";
        [Tooltip("Asset that describes the cursor used by this view model when dragging items.")]
        public DragCursor SharedCursor;



        /// <summary>
        /// How many pixels wide a cell is for this view.
        /// </summary>
        public float CellWidth { get; private set; }

        /// <summary>
        /// How many pixels tall a cell is for this view.
        /// </summary>
        public float CellHeight { get; private set; }

        /// <summary>
        /// Returns the local offset from the containing UI element that the first grid cell starts.
        /// </summary>
        public Vector2 GridLocalOffset => CellViews[0].CellUI.layout.min;

        /// <summary>
        /// Returns the local position from the containing UI element of the bottom-right corner of the grid itself.
        /// </summary>
        public Vector2 GridMaxPoint => CellViews[^1].CellUI.layout.max;

        public VisualElement GridRootUI { get; private set; }

        #endregion


        #region Private Fields
        List<GridCellView> CellViews;
        bool Started;
        bool IsDragging => DragSource != null;
        bool CandidateForStickyDrag;

        //For now I only forsee a single hilight needed for all menus since it is driven by the mouse.
        //I've referenced this list by the full class name so if this ends up changing to an instance variable
        //the compiler will spit errors so that it's easier to spot what needs updated.
        readonly static List<GridCellView> HilightedCells = new();
        static DragPayload DragSource;
        static bool AppIsQuitting = false;
        readonly static List<GridCellView> TempCells1 = new();
        readonly static List<GridCellView> TempCells2 = new();
        #endregion


        #region Unity Events
        /// <summary>
        /// 
        /// </summary>
        private IEnumerator Start()
        {
            yield return null; //need to wait a frame for UIToolkit stuff
            SetupGrid(_Model);
            Started = true;
            SharedCursor.Initialize(View);
        }

        private void Awake()
        {
            AppIsQuitting = false;
            Application.quitting += HandleAppQuitting;
        }

        private void OnDestroy()
        {
            Application.quitting -= HandleAppQuitting;
        }

        /// <summary>
        /// 
        /// </summary>
        void HandleAppQuitting()
        {
            AppIsQuitting = true;
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnEnable()
        {
            if (!Started) return;
            PushModelToView();
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnDisable()
        {
            //skip if we are quitting the game otherwise we can get nullref exceptions
            //due to race-conditions with UIToolkit.
            if (AppIsQuitting) return;

            TeardownGrid();
        }
        #endregion


        #region Private Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="geometryChangedEvent"></param>
        void HandleGeometryChangedEvent(GeometryChangedEvent geometryChangedEvent)
        {
            Debug.Log("Handling rebuild request");
            PushModelToView();
        }

        /// <summary>
        /// Assigns the model to this controller and creates a grid within the View element.
        /// If a model was already present the grid representing that one is destroyed first
        /// via Teardown().
        /// </summary>
        /// <param name="model"></param>
        void SetupGrid(IGridModel model)
        {
            Assert.IsNotNull(CellUIPrefab);
            if (model == null) return;
            if (_Model != null)
                TeardownGrid();
            _Model = (GridModelBehaviour)model;


            GridRootUI = View.rootVisualElement.Q<VisualElement>(GridContainerId);
            int total = Model.GridWidth * Model.GridHeight;
            CellViews = new(total);


            for (int i = 0; i < total; i++)
            {
                int x = i % Model.GridWidth;
                int y = i / Model.GridWidth;
                var cellUI = CellUIPrefab.Instantiate();
                CellViews.Add(new GridCellView(this, model.GetCell(x, y), cellUI, x, y));
                cellUI.userData = CellViews[i];
                cellUI.name = $"Cell ({x},{y})";
                cellUI.style.backgroundColor = Shared.DefaultColorBackground;
                var stackLabel = cellUI.Q<Label>(Shared.StackQtyId);
                stackLabel.text = CellViews[i].QtyStr;
                PositionCellUI(GridRootUI, cellUI, x, y);
                GridRootUI.Add(cellUI);

                //technically only need to set this once but I need to know the info after parenting so I have to do it in this loop
                CellWidth = cellUI.style.width.value.value;
                CellHeight = cellUI.style.height.value.value;
            }

            GridRootUI.RegisterCallback<GeometryChangedEvent>(HandleGeometryChangedEvent);
            _Model.OnGridSizeChanged.AddListener(HandleModelGridSizeChanged);
            _Model.OnStoredItem.AddListener(HandleStoredItem);
            _Model.OnRemovedItem.AddListener(HandleRemovedItem);
            _Model.OnCellsUpdated.AddListener(HandleCellsUpdated);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cellView"></param>
        /// <param name="cellModel"></param>
        void UpdateCell(GridCellView cellView)
        {
            var cellUI = cellView.CellUI;
            var stackLabel = cellUI.Q<Label>(Shared.StackQtyId);
            stackLabel.text = cellView.QtyStr;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        void TeardownGrid()
        {
            if (_Model == null || !Started) return;

            GridRootUI.UnregisterCallback<GeometryChangedEvent>(HandleGeometryChangedEvent);
            _Model.OnGridSizeChanged.RemoveListener(HandleModelGridSizeChanged);
            _Model.OnStoredItem.RemoveListener(HandleStoredItem);
            _Model.OnRemovedItem.RemoveListener(HandleRemovedItem);
            _Model.OnCellsUpdated.RemoveListener(HandleCellsUpdated);

            CellViews.Clear();
            GridRootUI = View.rootVisualElement.Q<VisualElement>(GridContainerId);
            GridRootUI.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cellUI"></param>
        void PositionCellUI(VisualElement container, VisualElement cellUI, int xPos, int yPos, int gridCellWidth = 1, int gridCellHeight = 1)
        {
            var r = container.layout;
            var cellWidth = r.width / Model.GridWidth;
            var cellheight = r.height / Model.GridHeight;
            if (cellWidth < cellheight) cellheight = cellWidth;
            else cellWidth = cellheight;

            float xDiff = r.width - (cellWidth * Model.GridWidth);
            float yDiff = r.height - (cellheight * Model.GridHeight);

            cellUI.style.width = new StyleLength(new Length(cellWidth * gridCellWidth, LengthUnit.Pixel));
            cellUI.style.height = new StyleLength(new Length(cellheight * gridCellHeight, LengthUnit.Pixel));
            cellUI.style.position = Position.Absolute;
            cellUI.style.left = new StyleLength(new Length((xDiff * 0.5f) + (xPos * cellWidth), LengthUnit.Pixel));
            cellUI.style.top = new StyleLength(new Length((yDiff * 0.5f) + (yPos * cellheight), LengthUnit.Pixel));
        }

        /// <summary>
        /// Forces the entire view to update to match the current state of the model.
        /// </summary>
        void PushModelToView()
        {
            //super lazy here. just gonna wipe everything out and rebuild from scratch
            //the new ui system seems pretty zippy so let's see how bad this is before worrying
            //about doing it a 'more efficient' way.
            TeardownGrid();
            SetupGrid(_Model);
            foreach (var item in _Model.Contents)
                HandleStoredItem(_Model, item);
        }

        /// <summary>
        /// Invoked by the GridModel whever its size changes.
        /// </summary>
        /// <param name="model"></param>
        void HandleModelGridSizeChanged(IGridModel model, Vector2Int oldSize)
        {
            PushModelToView();
        }

        /// <summary>
        /// Updates the view when the model signals a change.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="item"></param>
        void HandleStoredItem(IGridModel model, IGridItemModel item)
        {
            //first thing is to find the 'root' cell which is the cell
            //representing to top-left corner of the item's region
            var loc = model.GetLocation(item);
            Assert.IsTrue(loc != null);
            var region = loc.Value;

            GetCellViews(Model.GridWidth, Model.GridHeight, region, TempCells1);
            var cellViews = TempCells1;
            Assert.IsNotNull(cellViews);
            var firstCellView = cellViews[0];
            foreach (var cellView in cellViews)
            {
                cellView.Item = item;
                cellView.RootCellView = firstCellView;
            }
            firstCellView.OverlappedCellViews = cellViews;
            firstCellView.RootCellView = null;

            //now we want to set the icon of that cell and stretch it to fill the entire item region on the grid
            GridRootUI = View.rootVisualElement.Q<VisualElement>(GridContainerId);
            firstCellView.CellUI.style.backgroundImage = new StyleBackground(item.Shared.Icon);
            PositionCellUI(GridRootUI, firstCellView.CellUI, region.x, region.y, item.Size.x, item.Size.y);
            firstCellView.CellUI.BringToFront();
        }

        /// <summary>
        /// Updates the view when the model signals a change.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="item"></param>
        void HandleRemovedItem(IGridModel model, IGridItemModel item)
        {
            var loc = model.GetLocation(item);
            Assert.IsTrue(loc != null);
            var region = loc.Value;

            GetCellViews(Model.GridWidth, Model.GridHeight, region, TempCells1);
            var cellViews = TempCells1;
            Assert.IsNotNull(cellViews);
            var firstCellView = cellViews[0];
            foreach (var cellView in cellViews)
            {
                cellView.Item = null;
                cellView.RootCellView = null;
            }
            firstCellView.OverlappedCellViews = null;
            firstCellView.RootCellView = null;

            GridRootUI = View.rootVisualElement.Q<VisualElement>(GridContainerId);
            firstCellView.CellUI.style.backgroundImage = null;
            PositionCellUI(GridRootUI, firstCellView.CellUI, firstCellView.X, firstCellView.Y, 1, 1);
        }

        /// <summary>
        /// 
        /// </summary>
        void HandleCellsUpdated(IGridModel model, IEnumerable<GridCellModel> cells)
        {
            foreach (var cell in cells)
                UpdateCell(FindCellView(cell));
        }
        #endregion


        #region Public Grid View
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public GridCellView GetCellView(int modelGridWidth, int x, int y)
        {
            Assert.IsTrue(modelGridWidth > 0);
            Assert.IsTrue(x >= 0 && x < Model.GridWidth);
            Assert.IsTrue(y >= 0);
            return CellViews[(y * modelGridWidth) + x];
        }

        /// <summary>
        /// Fills a list of GridCellViews with the cells in a given region.
        /// </summary>
        /// <param name="modelGridWidth"></param>
        /// <param name="region"></param>
        public void GetCellViews(int modelGridWidth, int modelGridHeight, RectInt region, List<GridCellView> cellViews)
        {
            Assert.IsNotNull(cellViews);
            cellViews.Clear();

            region = Utilities.ClipRegion(modelGridWidth, modelGridHeight, region);
            for (int y = region.y; y < region.y + region.height; y++)
            {
                for (int x = region.x; x < region.x + region.width; x++)
                {
                    cellViews.Add(CellViews[(y * modelGridWidth) + x]);
                }
            }

        }

        /// <summary>
        /// Returns the top-left cell containing the item.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public GridCellView FindRootCellView(IGridItemModel item)
        {
            //due to the way we store things we'll always
            //find the upper-left cell first
            Assert.IsNotNull(item);
            foreach (var cellView in CellViews)
                if (cellView.Item == item) return cellView;

            return null;
        }

        /// Returns the associated cell view for the given model.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public GridCellView FindCellView(GridCellModel cellModel)
        {
            Assert.IsNotNull(cellModel);
            foreach (var cellView in CellViews)
                if (cellView.Cell == cellModel) return cellView;

            return null;
        }
        #endregion


        #region Public Drag n Drop
        /// <summary>
        /// 
        /// </summary>
        /// <param name="view"></param>
        /// <param name="srouceModel"></param>
        /// <param name="item"></param>
        public void BeginDrag(DragPayload dragSource)
        {
            if (IsDragging) return;

            //cache data
            DragSource = dragSource;
            CandidateForStickyDrag = true;

            //setup the cursor
            SharedCursor.SyncCursorToDragState(DragSource);

            //update the model and confirm success
            if (!Model.RemoveItem(DragSource.Item))
            {
                //... or not. something stopped us from even starting lol
                ResetDragState();
                return;
            }
        }


        /// <summary>
        /// Not gonna lie. This fuction is some real R Kelly Doo-doo Butter. Don't even read it. Seriously. Look away! I'm too ashamed!
        /// </summary>
        public void Drop(GridCellView clickedCellView, Vector2 localPos)
        {
            if (!IsDragging) return; //just in case we click elsewhere without starting a drag and then release over a slot


            #region Sticky Drag
            if (CandidateForStickyDrag)
            {
                HilightHoveredCells(clickedCellView, localPos, GridViewBehaviour.HilightedCells);
                CandidateForStickyDrag = false;
                return;
            }
            #endregion


            var targetModel = clickedCellView.GridView.Model;
            var region = CalculateBestFitCells(localPos, clickedCellView, DragSource.Item);


            #region Stack Check
            var stackItem = targetModel.CheckForStackableItem(DragSource.Item, region.x, region.y);
            if (stackItem != null)
            {
                //if this is less than zero, it means the stack was full. in that
                //case we'll simply fallthrough to the swapitem logic below.
                int qtyStacked = targetModel.StackItems(DragSource.Item, stackItem, DragSource.Item.StackCount);
                if (qtyStacked == DragSource.Item.StackCount)
                {
                    //in this case we've completely transferred the qty so both item refs are to the same thing
                    //we can just leave the drag and everything is golden
                    ResetDragState();
                    return;
                }
                else if (qtyStacked > 0)
                {
                    //we now have and item we are dragging with the difference after the dest stack
                    //was filled to capacity, at this point we can just leave and the drag should continue as normal.
                }
                else
                {
                    //we didn't trasnfer anything so it must have failed. just cancel
                    Cancel();
                    return;
                }
            }
            #endregion


            #region Swap
            var swapItem = targetModel.CheckForSwappableItem(DragSource.Item, region.x, region.y);
            if (swapItem != null)
            {
                var rootCell = this.FindRootCellView(swapItem);
                var swapDragSource = new DragPayload(rootCell, localPos, clickedCellView.CellUI.LocalToWorld(localPos)); //cache drag info no before we actually move the item
                if (!targetModel.SwapItems(swapItem, DragSource.Item, region))
                {
                    Cancel();
                    return;
                }

                //update internal state to reflect the new drag item
                #region Swap version of 'BeginDrag()'
                ResetDragState();
                DragSource = swapDragSource;
                CandidateForStickyDrag = true;
                SharedCursor.SyncCursorToDragState(DragSource);
                #endregion

                return;
            }
            #endregion


            #region Drop
            if (!clickedCellView.GridView.Model.StoreItem(DragSource.Item, region))
                Cancel();
            else ResetDragState();
            #endregion
        }

        /// <summary>
        /// Cancels an active drag operation and returns the item to its original location.
        /// </summary>
        public void Cancel()
        {
            if (!IsDragging) return;

            DragSource.RestoreSourceState();
            ResetDragState();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="cellView"></param>
        public void PointerHoverEnter(GridCellView cellView, Vector2 localPos)
        {
            HilightHoveredCells(cellView, localPos, GridViewBehaviour.HilightedCells);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="cellView"></param>
        public void PointerHoverExit(GridCellView cellView, Vector2 localPos)
        {
            HilightHoveredCells(cellView, localPos, GridViewBehaviour.HilightedCells);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="cellView"></param>
        public void CellPointerMoved(GridCellView cellView, Vector2 localPos)
        {
            if (IsDragging)
            {
                HilightHoveredCells(cellView, localPos, GridViewBehaviour.HilightedCells);
                if (CandidateForStickyDrag)
                {
                    if ((DragSource.PointerWorld - cellView.CellUI.LocalToWorld(localPos)).sqrMagnitude > Shared.StickyDragMoveThreshold)
                        CandidateForStickyDrag = false;
                }
            }

        }
        #endregion


        #region Private Drag n Drop
        /// <summary>
        /// Shared by several methods to reset global state data to a before-dragging condition.
        /// </summary>
        void ResetDragState()
        {
            if (!IsDragging) return;
            TintCells(GridViewBehaviour.HilightedCells, DragSource.CellView.GridView.Shared.DefaultColorBackground, DragSource.CellView.GridView.Shared.DefaultColorIcon);
            SharedCursor.SyncCursorToDragState(null);
            DragSource = null;
        }

        /// <summary>
        /// Given a target cell and the local pointer position on it, this calculates the best region to hilight given
        /// the state of any drag operations. In no drag is ocurring only the cell provided is hilighted.
        /// This method will reset the background colors of the cells list past in before spitting out the new cell
        /// 'best fit' cells for hilighting. So be sure to only pass in a list that was previously processed by this method
        /// or is fresh.
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="cellView"></param>
        void HilightHoveredCells(GridCellView cellView, Vector2 localPosition, List<GridCellView> cells)
        {
            //clear out previous cells if any
            TintCells(cells, cellView.GridView.Shared.DefaultColorBackground, cellView.GridView.Shared.DefaultColorIcon);
            cells.Clear();

            if (IsDragging)
            {
                var region = CalculateBestFitCells(localPosition, cellView, DragSource.Item);
                Color bgColor;
                Color iconTint;
                if (cellView.GridView.Model.CanMoveItemToLocation(DragSource.Item, region) ||
                    cellView.GridView.Model.CheckForSwappableItem(DragSource.Item, region.x, region.y) != null ||
                    cellView.GridView.Model.CheckForStackableItem(DragSource.Item, region.x, region.y) != null)
                {
                    bgColor = cellView.GridView.Shared.ValidColorBackground;
                    iconTint = cellView.GridView.Shared.ValidColorIcon;
                }
                else
                {
                    bgColor = cellView.GridView.Shared.InvalidColorBackground;
                    iconTint = cellView.GridView.Shared.InvalidColorIcon;
                }

                cellView.GridView.GetCellViews(cellView.GridView.Model.GridWidth, cellView.GridView.Model.GridHeight, region, cells);
                AdjustListForOverlappedCells(cells);
                TintCells(cells, bgColor, iconTint);
            }
            else
            {
                cells.Add(cellView);
                TintCells(cells, cellView.GridView.Shared.HilightColorBackground, cellView.GridView.Shared.HilightColorIcon);
            }
        }
        #endregion


        #region Static Helper Methods
        /// <summary>
        /// Helper for applying a tiny and background color to a list of cell UI elements.
        /// </summary>
        /// <param name="backgroundColor"></param>
        static void TintCells(List<GridCellView> cells, Color backgroundColor, Color iconTint)
        {
            if (cells != null)
            {
                foreach (var cell in cells)
                {
                    cell.CellUI.style.backgroundColor = backgroundColor;
                    if (cell.CellUI.style.backgroundImage != null)
                    {
                        cell.CellUI.style.unityBackgroundImageTintColor = iconTint;
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the best fit location of cells based on the pointer location. This is used to
        /// provide a more natural feeling when selecting where items will drop to on the grid.
        /// </summary>
        /// <param name="localPos"></param>
        /// <param name="cellView"></param>
        /// <returns></returns>
        static RectInt CalculateBestFitCells(Vector2 localPos, GridCellView cellView, IGridItemModel draggedItem)
        {
            if (draggedItem == null)
                return new RectInt(cellView.X, cellView.Y, 1, 1);

            int offsetX = cellView.X;
            int offsetY = cellView.Y;
            int w = draggedItem.Size.x;
            int h = draggedItem.Size.y;

            int gridWidth = cellView.GridView.Model.GridWidth;
            int gridHeight = cellView.GridView.Model.GridHeight;
            Vector2 normalized = new(localPos.x / cellView.GridView.CellWidth, localPos.y / cellView.GridView.CellHeight);
            float halfWay = 0.5f;

            if (w > 1)
            {
                if ((w & 0x1) == 1) offsetX -= (int)(w / 2); //odd width
                else if (normalized.x < halfWay) offsetX -= (int)(w / 2); //even width
                else offsetX -= (int)(w / 2) - 1; //even width
            }
            if (h > 1)
            {
                if ((h & 0x1) == 1) offsetY -= (int)(h / 2); //odd height
                else if (normalized.y < halfWay) offsetY -= (int)(h / 2); //even height
                else offsetY -= (int)(h / 2) - 1; //even height
            }

            //limit the final location to the bounds of the grid
            offsetX = Mathf.Max(0, offsetX);
            if (offsetX + w >= gridWidth)
                offsetX = gridWidth - w;

            offsetY = Mathf.Max(0, offsetY);
            if (offsetY + h > gridHeight)
                offsetY = gridHeight - h;

            return new RectInt(offsetX, offsetY, w, h);
        }

        /// <summary>
        /// Given a list of cells, this will be sure to include any root cells of multi-celled items who's children are
        /// in the initial list. It will also remove those children so as not to cause duplicated effects. This method
        /// is primarily for figuring out which cells in a RectInt region actually need hilighting applied.
        /// </summary>
        /// <param name="cells"></param>
        static void AdjustListForOverlappedCells(List<GridCellView> cells)
        {
            TempCells1.Clear();
            TempCells2.Clear();
            foreach (var cell in cells)
            {
                if (cell.RootCellView != null && !TempCells2.Contains(cell))
                {
                    TempCells1.Add(cell.RootCellView);
                    TempCells2.Add(cell);
                }
            }

            cells.AddRange(TempCells1);
            foreach (var cell in TempCells2)
                cells.Remove(cell);
            TempCells1.Clear();
            TempCells2.Clear();
        }

        /// <summary>
        /// Given a grid cell view, this will check to see if any multi-slot items are causing it to be stretched over and return that if there are.
        /// In this way we can always find the top-left cell of any region occupied by any item regardless of its size.
        /// </summary>
        /// <param name="cellView"></param>
        public static GridCellView FindRootCell(GridCellView cellView)
        {
            var item = cellView.Cell.Item;
            if (item == null) return cellView;
            var model = cellView.GridView.Model;
            cellView.GridView.GetCellViews(model.GridWidth, model.GridHeight, model.GetLocation(item).Value, TempCells1);
            return TempCells1[0];
        }

        /*
        /// <summary>
        /// Makes a request to the backing model to move the item.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="srcModel"></param>
        /// <param name="destModel"></param>
        public static bool RequestMoveItem(IGridItemModel item, GridViewBehaviour destModel, int xPos, int yPos)
        {
            Assert.IsNotNull(destModel);

            var srcModel = item.Container;
            var originLoc = srcModel.GetLocation(item);
            Assert.IsTrue(originLoc != null);
            var originRegion = originLoc.Value;
            var destRegion = new RectInt(xPos, yPos, item.Size.x, item.Size.y);

            if (!destModel.Model.CanMoveItemToLocation(item, destRegion))
                return false;

            if (srcModel.RemoveItem(item))
            {
                if (destModel.Model.StoreItem(item, destRegion))
                    return true;
                if (!srcModel.StoreItem(item, originRegion))
                    throw new UnityException("A catastrophic error has ocurred with PGIA!"); //should never happen, but who knows?
            }

            return false;
        }
        */
        #endregion

    }
}
