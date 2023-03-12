using System;
using UnityEngine.UIElements;
namespace PGIA
{
    /// <summary>
    /// 
    /// </summary>
    public class GridCellView : System.IDisposable
    {
        public IGridItemModel Item; //as much as I hate it, it makes it easier to do visual-only stuff with this
        readonly public GridCellModel Cell;
        readonly public GridView GridView;
        readonly public VisualElement CellUI;
        readonly public int X;
        readonly public int Y;

        public int GridCellsX { get => Cell.Item == null ? 0 : Cell.Item.Size.x; }
        public int GridCellsY { get => Cell.Item == null ? 0 : Cell.Item.Size.y; }
        public string QtyStr
        {
            get => (Cell.Item == null) || (Cell.Item.MaxStackCount) < 2 ? string.Empty : Cell.Item.StackCount.ToString();
        }

        bool Disposed;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gridView"></param>
        /// <param name="cell"></param>
        /// <param name="cellUI"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public GridCellView(GridView gridView, GridCellModel cell, VisualElement cellUI, int x, int y)
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
            CellUI.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        void HandlePointerDown(PointerDownEvent evt)
        {
            GridView.SharedCursor.BeginDrag(evt, this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        void HandlePointerUp(PointerUpEvent evt)
        {
            GridView.SharedCursor.Drop(evt, this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        void HandlePointerEnter(PointerEnterEvent evt)
        {
            GridView.SharedCursor.PointerHoverEnter(evt, this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        void HandlePointerLeave(PointerLeaveEvent evt)
        {
            GridView.SharedCursor.PointerHoverExit(evt, this);
        }

        /// <summary>
        /// 
        /// </summary>
        void Dispose(bool disposing)
        {
            if(Disposed) return;
            //currently not using any un-managed resources so the 'disposing' flag isn't even used.
            CellUI.UnregisterCallback<PointerDownEvent>(HandlePointerDown);
            CellUI.UnregisterCallback<PointerUpEvent>(HandlePointerUp);
            CellUI.UnregisterCallback<PointerEnterEvent>(HandlePointerEnter);
            CellUI.UnregisterCallback<PointerLeaveEvent>(HandlePointerLeave);
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
