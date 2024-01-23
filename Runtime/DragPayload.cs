using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace PGIA
{
    /// <summary>
    /// All of the state info in regards to a drag operation in a GridView.
    /// </summary>
    public class DragPayload
    {
        public readonly IGridModel Model;
        public readonly IGridItemModel Item;
        public readonly GridCellView CellView;
        public readonly Vector2 PointerLocal;
        public readonly Vector2 PointerWorld;
        public readonly RectInt? Region;
        public readonly GridCellView RootCellView;
        public readonly StyleLength IconWidth;
        public readonly StyleLength IconHeight;

        public GridViewBehaviour GridView => CellView.GridView;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cellView"></param>
        /// <param name="pointerLocal"></param>
        /// <param name="pointerWorld"></param>
        public DragPayload(GridCellView cellView, Vector2 pointerLocal, Vector2 pointerWorld)
        {
            Assert.IsNotNull(cellView.Item, "Drag payloads cannot be formed from GridCellViews that have no items."); //eveything hinges on this
            Item = cellView.Item;
            //Assert.IsTrue(Item.Container == cellView.GridView.Model, "There was an inconsistency between the item's container model and the cellview's gridview model.");
            Model = Item.Container;
            CellView = cellView;
            PointerLocal = pointerLocal;
            PointerWorld = pointerWorld;
            RootCellView = GridViewBehaviour.FindRootCell(CellView);
            IconWidth = RootCellView.IconWidth;
            IconHeight = RootCellView.IconHeight;

            Region = Model.GetLocation(Item);
            if(Region == null)
            {
                //the region given was invalid, can we find a space to put it as a backup?
                var loc = Model.FindOpenSpace(Item.Size.x, Item.Size.y);
                if (loc == null)
                    Debug.LogWarning("During an item swap sequence ample space was lost to confirm a cancellation action. The item will be dropped " +
                        "from the inventory and issued with the 'OnDroppedItem' event if the drag action is cancelled in this state. " +
                        "\n\nThis is not a bug but rather a heads up about this safety feature.");
                else Region = loc.Value;
            }
        }

        static List<GridCellView> Temp = new();
        /// <summary>
        /// Attempts to place the currently dragged item back into its source model.
        /// </summary>
        /// <returns></returns>
        public void RestoreSourceState()
        {
            //put the item back where it came from, if this fails, not much we can do about it at this point
            if (Region == null || !Model.StoreItem(Item, Region.Value.position))
            {
                //since the region it came from is invalid, can we resolve a new spot that is big enough?
                var rect = GridView.Model.FindOpenSpace(Item.Size.x, Item.Size.y);
                if (!rect.HasValue || !Model.StoreItem(Item, rect.Value.position))
                {
                    Debug.LogWarning("Due to the amount of item swapping, no valid location could be found for this cancelled operation. As a result the item " +
                        "will be expelled from the inventory entirely and the 'OnDroppedItem' event will be triggered. " +
                        "\n\nThis is not a bug but rather a heads up about this safety feature.");
                    var model = Item.Container ?? GridView.Model;
                    model.DropItem(Item);
                    return;
                }

                GridView.GetCellViews(GridView.Model.GridWidth, GridView.Model.GridHeight, rect.Value, Temp);
            }
            else
            {
                GridView.GetCellViews(GridView.Model.GridWidth, GridView.Model.GridHeight, Region.Value, Temp);
            }

            //tint the final resting location with the item
            GridViewBehaviour.TintCells(Temp, this.CellView.GridView.SharedGridAsset.DefaultColorBackground, this.CellView.GridView.SharedGridAsset.DefaultColorIcon);

        }


    }
}
