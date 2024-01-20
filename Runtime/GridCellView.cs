using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PGIA
{
    /// <summary>
    /// The viewmodel for a cell within a GridView.
    /// </summary>
    public class GridCellView : System.IDisposable
    {
        public IGridItemModel Item; //as much as I hate it, it makes it easier to do visual-only stuff with this
        readonly public GridCellModel Cell;
        readonly public GridViewBehaviour GridView;
        readonly public VisualElement CellUI;
        readonly public int X;
        readonly public int Y;

        public int GridCellsX { get => Cell.Item == null ? 0 : Cell.Item.Size.x; }
        public int GridCellsY { get => Cell.Item == null ? 0 : Cell.Item.Size.y; }
        public string QtyStr
        {
            get => (Cell.Item == null) || (!Cell.Item.Shared.IsStackable) ? string.Empty : Cell.Item.StackCount.ToString();
        }

        public StyleLength IconWidth => Icon?.style.width ?? 0;

        public StyleLength IconHeight => Icon?.style.height ?? 0;

        /// <summary>
        /// In the case that this cell has been stetched to overlap others, this will return the list of those overlapped cells.
        /// </summary>
        public List<GridCellView> OverlappedCellViews;

        /// <summary>
        /// In the case that this cell has been overlapped by another that was stretched over it, this will return that overlapping cell.
        /// </summary>
        public GridCellView RootCellView;

        bool Hovered = false;
        bool Disposed;
        VisualElement Icon;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gridView"></param>
        /// <param name="cell"></param>
        /// <param name="cellUI"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public GridCellView(GridViewBehaviour gridView, GridCellModel cell, VisualElement cellUI, int x, int y)
        {
            Cell = cell;
            GridView = gridView;
            CellUI = cellUI;
            X = x;
            Y = y;

            CellUI.RegisterCallback<PointerDownEvent>(HandlePointerDown);
            CellUI.RegisterCallback<PointerUpEvent>(HandlePointerUp);
            CellUI.RegisterCallback<PointerEnterEvent>(HandlePointerEnter);
            CellUI.RegisterCallback<PointerLeaveEvent>(HandlePointerLeave);
            CellUI.RegisterCallback<PointerMoveEvent>(HandlePointerMove);
            CellUI.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="icon"></param>
        /// <returns></returns>
        public VisualElement SetIcon(VisualElement icon)
        {
            VisualElement old = null;
            if (Icon != null)
            {
                CellUI.Remove(Icon);
                Icon.visible = false;
                old = Icon;
            }

            Icon = icon;
            if (icon != null)
            {
                CellUI.Add(Icon);
                Icon.pickingMode = PickingMode.Ignore;
                Icon.focusable = false;
                Icon.style.position = Position.Absolute;
                Icon.style.left = 0;
                Icon.style.top = 0;
                Icon.style.backgroundImage = new StyleBackground(Item.Shared.Icon);
                Icon.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                //Icon.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Length));// // new StyleLength(100, LengthUnit.Percent);
                //Icon.style.backbackgroundPosition = new StyleLength(50, LengthUnit.Percent);
                //Icon.style.backgroundRepeat = new StyleBackgroundRepeat(new BackgroundRepeat(0, 0));

                Icon.BringToFront();
                Icon.visible = true;
            }
            return old;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        void HandlePointerDown(PointerDownEvent evt)
        {
            if(this.Item != null)
                GridView.BeginDrag(new DragPayload(this, evt.localPosition, evt.position));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        void HandlePointerUp(PointerUpEvent evt)
        {
            GridView.Drop(this, evt.localPosition);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        void HandlePointerEnter(PointerEnterEvent evt)
        {
            Hovered = true;
            GridView.PointerHoverEnter(this, evt.localPosition);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        void HandlePointerLeave(PointerLeaveEvent evt)
        {
            Hovered = false;
            GridView.PointerHoverExit(this, evt.localPosition);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        void HandlePointerMove(PointerMoveEvent evt)
        {
            if (Hovered)
            {
                //as usual, stretching the cells for multi-cell items kinda fucks us.
                //we can't properly send update info to the drag cursor for hilighting
                //just with enter/exit hover events alone so we need to send that info
                //constantly while hovering
                GridView.CellPointerMoved(this, evt.localPosition);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void Dispose(bool disposing)
        {
            if(Disposed) return;
            //currently not even making use of the 'disposing' flag
            CellUI.UnregisterCallback<PointerDownEvent>(HandlePointerDown);
            CellUI.UnregisterCallback<PointerUpEvent>(HandlePointerUp);
            CellUI.UnregisterCallback<PointerEnterEvent>(HandlePointerEnter);
            CellUI.UnregisterCallback<PointerLeaveEvent>(HandlePointerLeave);
            CellUI.UnregisterCallback<PointerMoveEvent>(HandlePointerMove);
            Disposed = true;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        ~GridCellView()
        {
            Dispose(false);
        }
    }
}
