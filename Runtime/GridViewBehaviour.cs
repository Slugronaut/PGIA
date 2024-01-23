using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace PGIA
{
    /// <summary>
    /// The controller interfaces between the GridModel and the UIElements used to visualize it.
    /// 
    /// TODO:
    ///     -I *REALLY* need to separate out the logic of the View-Model from the view itself.
    ///         The view-model should not rely on monobehaviours at all and should be pure code and SO data.
    ///         The view then should never directly access the model but should instead make requests to perform
    ///         actions via the view-model. This will allow more flexible designs where views can be implemented
    ///         for different control schemes and setups plus allow for networking more easily.
    ///     -add events for hovering and clicking on items
    ///     -add the ability to 'select' items in a selection mode rather than begin dragging
    ///     -add support for per-item override of background colors. This way we can hilight item states for each individual item
    ///     -stack splitting process
    /// </summary>
    public class GridViewBehaviour : MonoBehaviour
    {
        #region Public Fields and Properties
        [SerializeReference]
        [SerializeField]
        [HideInInspector]
        GridModelBehaviour _Model; //note that we have used to concrete type here for the sake of serialization. there are workarounds to avoid this but I can't be fucked to bother right now
        [PropertySpace(12)]
        [PropertyOrder(-1)]
        [Title("Instance Data")]
        [ShowInInspector]
        [PropertyTooltip("The inventory that this grid will represent.")]
        public IGridModel Model
        {
            get => _Model;
            set
            {
#pragma warning disable CS0253 // Possible unintended reference comparison; right hand side needs cast
                if (_Model != value)
                {
                    if (Application.isPlaying)
                    {
                        TeardownGrid();
                        _Model = (GridModelBehaviour)value; //again, casting to concrete type. can't be fucked
                        SetupGrid();
                        UpdateGridContents();
                    }
                    else _Model = (GridModelBehaviour)value;
                }
#pragma warning restore CS0253 // Possible unintended reference comparison; right hand side needs cast
            }
        }
        [PropertyOrder(0)]
        [PropertyTooltip("The UIDocument in the scene that holds the UI data.")]
        public UIDocument View;
        [PropertyOrder(0)]
        [PropertyTooltip("The name of the visual element of the supplied UI document that will contain this grid.")]
        public string GridContainerId = "GridContainer";

        [Title("Shared Assets")]
        [PropertyTooltip("Asset that describes the cursor used by this view model when dragging items.")]
        public DragCursor CursorAsset;
        [PropertyTooltip("The UI document asset that describes an instance of a grid cell.")]
        public VisualTreeAsset CellUIAsset;
        [PropertyTooltip("The UI document asset that describes an instance of a grid cell icon.")]
        public VisualTreeAsset CellIconAsset;
        [UnityEngine.Tooltip("An asset that stores common properties that are often shared by many grids.")]
        public GridViewAsset SharedGridAsset;
       
        

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
        bool Initialized;
        bool IsDragging => DragSource != null;
        bool CandidateForStickyDrag;
        double StickyDragStartTime = -1;

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
            Started = true;
            SetupGrid();
            UpdateGridContents();
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
            PushModelToView();
        }

        /// <summary>
        /// Creates a UI grid for the model currently assgined to this view.
        /// Subsiquent calls to this method do nothing until <see cref="TeardownGrid"/>
        /// has been called first.
        /// </summary>
        /// <param name="model"></param>
        void SetupGrid()
        {
            Assert.IsNotNull(CellUIAsset);
            if (_Model == null || !Started || Initialized) return;

            Initialized = true;
            GridRootUI = Utilities.ParseQueryPath(View.rootVisualElement, GridContainerId);
            int total = _Model.GridWidth * _Model.GridHeight;
            CellViews = new(total);


            for (int i = 0; i < total; i++)
            {
                int x = i % _Model.GridWidth;
                int y = i / _Model.GridWidth;
                var cellUI = CellUIAsset.Instantiate();
                CellViews.Add(new GridCellView(this, _Model.GetCell(x, y), cellUI, x, y));
                cellUI.userData = CellViews[i];
                cellUI.name = $"Cell ({x},{y})";
                cellUI.style.backgroundColor = SharedGridAsset.DefaultColorBackground;
                var stackLabel = cellUI.Q<Label>(SharedGridAsset.StackQtyId);
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
            var stackLabel = cellUI.Q<Label>(SharedGridAsset.StackQtyId);
            stackLabel.text = cellView.QtyStr;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        void TeardownGrid()
        {
            if (_Model == null || !Started || !Initialized) return;

            Initialized = false;
            GridRootUI.UnregisterCallback<GeometryChangedEvent>(HandleGeometryChangedEvent);
            _Model.OnGridSizeChanged.RemoveListener(HandleModelGridSizeChanged);
            _Model.OnStoredItem.RemoveListener(HandleStoredItem);
            _Model.OnRemovedItem.RemoveListener(HandleRemovedItem);
            _Model.OnCellsUpdated.RemoveListener(HandleCellsUpdated);

            CellViews.Clear();
            if (View != null && View.rootVisualElement != null)
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
            SetupGrid();
            foreach (var item in _Model.Contents)
                HandleStoredItem(_Model, item);
        }

        /// <summary>
        /// 
        /// </summary>
        void UpdateGridContents()
        {
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
            firstCellView.OverlappedCellViews = cellViews.Count > 1 ? cellViews : null;
            firstCellView.RootCellView = null;

            //update background colors (this will internally set the colors to the item color if the cells are occupied by that item
            TintCells(cellViews, SharedGridAsset.DefaultColorBackground, SharedGridAsset.DefaultColorIcon);
            //TintCells(cellViews, item.Shared.Background, SharedGridAsset.DefaultColorIcon, false);

            //now we want to set the icon of that cell and stretch it to fill the entire item region on the grid
            //TODO: hack to see if this works - needs to be cached so that we don't thrash our heap
            var coverCell = CellIconAsset.Instantiate();
            var adjustedItemSize = model.AdjustedSize(item);
            PositionCellUI(GridRootUI, coverCell, region.x, region.y, adjustedItemSize.x, adjustedItemSize.y);
            firstCellView.SetIcon(coverCell);
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

            //force the cell colors to be the default background color
            TintCells(cellViews, SharedGridAsset.DefaultColorBackground, SharedGridAsset.DefaultColorIcon, false);

            //TODO: instead of destroying, we should cache this
            firstCellView.SetIcon(null).RemoveFromHierarchy();
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
            StickyDragStartTime = Time.unscaledTimeAsDouble;

            //setup the cursor
            CursorAsset.SyncCursorToDragState(DragSource);

            //reset the color of all cells being dragged
            TintCells(DragSource.RootCellView.OverlappedCellViews, SharedGridAsset.DefaultColorBackground, SharedGridAsset.DefaultColorIcon, false);

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
            if (CandidateForStickyDrag && Time.realtimeSinceStartupAsDouble - StickyDragStartTime < SharedGridAsset.StickyDragMoveTime)
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
                var cachedCells = rootCell.OverlappedCellViews.ToList();
                if (!targetModel.SwapItems(swapItem, DragSource.Item, region.position))
                {
                    Cancel();
                    return;
                }

                //update internal state to reflect the new drag item
                #region Swap version of 'BeginDrag()'
                //clear out space where swapped item was first, then fill in where we are going
                TintCells(cachedCells, SharedGridAsset.DefaultColorBackground, SharedGridAsset.DefaultColorIcon);
                TintCells(GridViewBehaviour.HilightedCells, DragSource.CellView.GridView.SharedGridAsset.DefaultColorBackground, DragSource.CellView.GridView.SharedGridAsset.DefaultColorIcon);
                ResetDragState();
                DragSource = swapDragSource;
                CandidateForStickyDrag = true;
                CursorAsset.SyncCursorToDragState(DragSource);
                #endregion

                return;
            }
            #endregion


            #region Drop
            if (!clickedCellView.GridView.Model.StoreItem(DragSource.Item, region.position))
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
            TintCells(GridViewBehaviour.HilightedCells, DragSource.CellView.GridView.SharedGridAsset.DefaultColorBackground, DragSource.CellView.GridView.SharedGridAsset.DefaultColorIcon);
            CursorAsset.SyncCursorToDragState(null);
            DragSource = null;
        }

        /// <summary>
        /// Given a target cell and the local pointer position on it, this calculates the best region to hilight given
        /// the state of any drag operations. If no drag is ocurring only the cell provided is hilighted.
        /// This method will reset the background colors of the cells list passed in before spitting out the new cell
        /// 'best fit' cells for hilighting. So be sure to only pass in a list that was previously processed by this method
        /// or is fresh.
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="cellView"></param>
        void HilightHoveredCells(GridCellView cellView, Vector2 localPosition, List<GridCellView> cells)
        {
            //clear out previous cells if any
            TintCells(cells, cellView.GridView.SharedGridAsset.DefaultColorBackground, cellView.GridView.SharedGridAsset.DefaultColorIcon);
            cells.Clear();

            if (IsDragging)
            {
                var region = CalculateBestFitCells(localPosition, cellView, DragSource.Item);
                Color bgColor;
                Color iconTint;
                if (cellView.GridView.Model.CanMoveItemToLocation(DragSource.Item, region.position) ||
                    cellView.GridView.Model.CheckForSwappableItem(DragSource.Item, region.x, region.y) != null ||
                    cellView.GridView.Model.CheckForStackableItem(DragSource.Item, region.x, region.y) != null)
                {
                    bgColor = cellView.GridView.SharedGridAsset.ValidColorBackground;
                    iconTint = cellView.GridView.SharedGridAsset.ValidColorIcon;
                }
                else
                {
                    bgColor = cellView.GridView.SharedGridAsset.InvalidColorBackground;
                    iconTint = cellView.GridView.SharedGridAsset.InvalidColorIcon;
                }

                cellView.GridView.GetCellViews(cellView.GridView.Model.GridWidth, cellView.GridView.Model.GridHeight, region, cells);
                AdjustListForOverlappedCells(cells);
                TintCells(cells, bgColor, iconTint);
            }
            else
            {
                cells.Add(cellView);
                TintCells(cells, cellView.GridView.SharedGridAsset.HilightColorBackground, cellView.GridView.SharedGridAsset.HilightColorIcon);
            }
        }
        #endregion


        #region Static Helper Methods
        /// <summary>
        /// Helper for applying a tint and background color to a list of cell UI elements.
        /// The emptyBackgroundColor is applied only if the cells are actually devoid of any items, otherwise they
        /// will be the color of those item's chosing. This can be overriden by also setting 'allowItemBackgroundColors'
        /// to false at which point only the emptyBackgroundColor will be used.
        /// </summary>
        /// <param name="emptyBackgroundColor"></param>
        public static void TintCells(List<GridCellView> cells, Color emptyBackgroundColor, Color iconTint, bool allowItemBackgroundColors = true)
        {
            if (cells != null)
            {
                foreach (var cell in cells)
                {
                    //set default tint just to be sure
                    cell.CellUI.style.backgroundColor = emptyBackgroundColor;

                    //if this cell has an item we want to tint it
                    if (allowItemBackgroundColors && cell.Item != null)
                    {
                        //this is kinda nasty since we're now asking the model what color to use but... I don't feel like thinking up a better way
                        cell.CellUI.style.backgroundColor = cell.Item.OverrideBackgroundColor ? cell.Item.CustomBackgroundColor : cell.Item.Shared.Background;
                    }


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
            if (draggedItem == null || draggedItem is null)
                return new RectInt(cellView.X, cellView.Y, 1, 1);

            int offsetX = cellView.X;
            int offsetY = cellView.Y;
            int cellsX = draggedItem.Size.x;
            int cellsY = draggedItem.Size.y;

            int gridWidth = cellView.GridView.Model.GridWidth;
            int gridHeight = cellView.GridView.Model.GridHeight;
            Vector2 normalized = new(localPos.x / cellView.GridView.CellWidth, localPos.y / cellView.GridView.CellHeight);
            float halfWay = 0.5f;

            if (cellsX > 1)
            {
                if ((cellsX & 0x1) == 1) offsetX -= (int)(cellsX / 2); //odd width
                else if (normalized.x < halfWay) offsetX -= (int)(cellsX / 2); //even width
                else offsetX -= (int)(cellsX / 2) - 1; //even width
            }
            if (cellsY > 1)
            {
                if ((cellsY & 0x1) == 1) offsetY -= (int)(cellsY / 2); //odd height
                else if (normalized.y < halfWay) offsetY -= (int)(cellsY / 2); //even height
                else offsetY -= (int)(cellsY / 2) - 1; //even height
            }

            //limit the final location to the bounds of the grid
            offsetX = Mathf.Max(0, offsetX);
            if (offsetX + cellsX >= gridWidth)
                offsetX = gridWidth - cellsX;
            offsetX = Mathf.Max(0, offsetX); //re-apply again in case we went negative

            offsetY = Mathf.Max(0, offsetY);
            if (offsetY + cellsY >= gridHeight)
                offsetY = gridHeight - cellsY;
            offsetY = Mathf.Max(0, offsetY); //re-apply again in case we went negative

            var finalRect = new RectInt(offsetX, offsetY, cellsX, cellsY);


            return finalRect;
        }
        static List<GridCellView> TempCells3 = new List<GridCellView>();

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
