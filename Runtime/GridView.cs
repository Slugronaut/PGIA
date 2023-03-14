using AdvLifeSim;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace PGIA
{
    /// <summary>
    /// The controller interfaces between the GridModel and the UIElements used to visualize it.
    /// This would be the 'C' in MVC or the 'VM' in MMVM design patterns.
    /// </summary>
    public class GridView : MonoBehaviour
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
        public Vector2 GridMaxPoint => CellViews[CellViews.Count - 1].CellUI.layout.max;

        public VisualElement GridRootUI { get; private set; }
        #endregion


        #region Private Fields
        List<GridCellView> CellViews;
        bool Started;
        static bool AppIsQuitting = false;
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
            SharedCursor.Initialize(View.rootVisualElement);
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
        void UpdateCelll(GridCellView cellView)
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
            cellUI.style.left = new StyleLength(new Length((xDiff * 0.5f) +(xPos * cellWidth), LengthUnit.Pixel));
            cellUI.style.top = new StyleLength(new Length((yDiff * 0.5f) + (yPos * cellheight),LengthUnit.Pixel));
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
        #endregion


        #region Public Methods
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
            return CellViews[(y*modelGridWidth)+x];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelGridWidth"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        public List<GridCellView> GetCellViews(int modelGridWidth, RectInt region)
        {
            List<GridCellView> cellViews = new();

            for(int y = region.y; y < region.y + region.height; y++)
            {
                for (int x = region.x; x < region.x + region.width; x++)
                {
                    cellViews.Add(CellViews[(y * modelGridWidth) + x]);
                }
            }

            return cellViews.Count > 0 ? cellViews : null;
        }

        /// <summary>
        /// Returns the top-left cell containing the item.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public GridCellView FindCellView(IGridItemModel item)
        {
            //due to the way we store things we'll always
            //find the upper-left cell first
            Assert.IsNotNull(item);
            foreach (var cellView in CellViews)
                if (cellView.Item == item) return cellView;

            return null;
        }

        // <summary>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="item"></param>
        public void HandleStoredItem(IGridModel model, IGridItemModel item)
        {
            //first thing is to find the 'root' cell which is the cell
            //representing to top-left corner of the item's region
            var loc = model.GetLocation(item);
            Assert.IsTrue(loc != null);
            var region = loc.Value;

            var cellViews = GetCellViews(Model.GridWidth, region);
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
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="item"></param>
        public void HandleRemovedItem(IGridModel model, IGridItemModel item)
        {
            var loc = model.GetLocation(item);
            Assert.IsTrue(loc != null);
            var region = loc.Value;

            var cellViews = GetCellViews(Model.GridWidth, region);
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
                UpdateCelll(FindCellView(cell));
        }

        public RectInt? GetLocation(IGridItemModel item) => Model.GetLocation(item);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool RemoveItem(IGridItemModel item) => Model.RemoveItem(item);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool StoreItem(IGridItemModel item, RectInt region) => Model.StoreItem(item, region);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="xPos"></param>
        /// <param name="yPos"></param>
        /// <returns></returns>
        public IGridItemModel CheckForSwappableItem(IGridItemModel item, int xPos, int yPos) => Model.CheckForSwappableItem(item, xPos, yPos);
        #endregion


        #region Static Methods
        /// <summary>
        /// Makes a request to the backing model to move the item.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="srcModel"></param>
        /// <param name="destModel"></param>
        public static bool RequestMoveItem(IGridItemModel item, GridView destModel, int xPos, int yPos)
        {
            Assert.IsNotNull(destModel);

            var srcModel = item.Container;
            var originLoc = srcModel.GetLocation(item);
            Assert.IsTrue(originLoc != null);
            var originRegion = originLoc.Value;
            var destRegion = new RectInt(xPos, yPos, item.Size.x, item.Size.y);

            if (!destModel.Model.CanMoveItemToLocation(item, destRegion))
                return false;

            if(srcModel.RemoveItem(item))
            {
                if (destModel.Model.StoreItem(item, destRegion))
                    return true;
                if (!srcModel.StoreItem(item, originRegion))
                    throw new UnityException("A catastrophic error has ocurred with PGIA!"); //should never happen, but who knows?
            }

            return false;
        }

        #endregion

    }
}
