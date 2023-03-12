
namespace PGIA
{
    /// <summary>
    /// A single cell used to form the grid elements of a grid-based inventory in PGIA.
    /// </summary>
    public class GridCellModel
    {
        public IGridItemModel Item;
        readonly public IGridModel Model;
        readonly public int X;
        readonly public int Y;

        public GridCellModel(IGridModel model, int x, int y)
        {
            Model = model;
            X = x;
            Y = y;
        }
    }
}
