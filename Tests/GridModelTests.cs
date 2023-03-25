using UnityEngine;
using NUnit.Framework;

namespace PGIA.Tests
{
    /// <summary>
    /// Tests for the PGIA GridModel.
    /// </summary>
    public class GridModelTests : ScriptableObject
    {
        [SerializeField] InventoryItemAsset ItemAsset1x1_001;
        [SerializeField] InventoryItemAsset ItemAsset2x2_001;
        [SerializeField] InventoryItemAsset StackableItemAsset1x1_001;
        [SerializeField] InventoryItemAsset StackableItemAsset1x1_002;

        #region Helpers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static RectInt MakeRegion(IGridItemModel item, int x, int y)
        {
            return new RectInt(x, y, item.Size.x, item.Size.y);
        }

        /// <summary>
        /// Helper for initializing a model grid quickly
        /// and in a consistent manner.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        static IGridModel CreateModel(int x, int y)
        {
            IGridModel model = new GridModel();
            model.GridSize = new Vector2Int(x, y);
            model.OnEnable();
            return model;
        }

        /// <summary>
        /// Helper for quickly instantiating an item.
        /// </summary>
        /// <returns></returns>
        IGridItemModel CreateItem(InventoryItemAsset sharedItemData)
        {
            IGridItemModel item = new GridItemModel(sharedItemData);
            return item;
        }
        #endregion


        #region TEST INIT
        [Test]
        public void GridModelInitializeWithNoGridSize()
        {
            GridModel model = new();
            Assert.DoesNotThrow(() =>
            {
                model.OnEnable();
            });

        }

        [Test]
        public void GridModelInitializeWithGridSize()
        {
            GridModel model = new();

            model.GridSize = new Vector2Int(5, 8);
            model.OnEnable();

            Assert.AreEqual(5, model.GridWidth);
            Assert.AreEqual(8, model.GridHeight);
        }
        #endregion


        #region TEST STORAGE
        [Test]
        public void CanStoreItemInGrid()
        {
            var model = CreateModel(5, 8);
            var item = CreateItem(ItemAsset1x1_001);

            var inRegion = new Vector2Int(0, 0);
            var result = model.StoreItem(item, inRegion);

            Assert.IsTrue(result);
        }

        [Test]
        public void CanFindStoredItemInGrid()
        {
            var model = CreateModel(5, 8);
            var item = CreateItem(ItemAsset2x2_001);

            //TODO: make this loop over every valid location possible and try them all
            const int xPos = 0;
            const int yPos = 0;
            var inRegion = new Vector2Int(xPos, yPos);
            model.StoreItem(item, inRegion);

            var cell = model.FindCell(item);
            Assert.IsNotNull(cell);
            Assert.AreEqual(xPos, cell.X);
            Assert.AreEqual(yPos, cell.Y);

            var loc = model.GetLocation(item);
            Assert.IsNotNull(loc);
            Assert.IsTrue(loc.HasValue);

            var outRegion = loc.Value;
            Assert.AreEqual(2, outRegion.width);
            Assert.AreEqual(2, outRegion.height);
            Assert.AreEqual(new RectInt(inRegion, item.Size), outRegion);
        }
        #endregion

        #region TEST STACKING
        /// <summary>
        /// Helper method for setting up a basic and repeatable stacking scenario for testing.
        /// </summary>
        /// <returns></returns>
        (IGridModel model, IGridItemModel storedItem, IGridItemModel dropItem) SetupStackScenario(int xPos, int yPos)
        {
            var model = CreateModel(10, 10);
            var storedItem = CreateItem(StackableItemAsset1x1_001);
            var dropItem = CreateItem(StackableItemAsset1x1_001);
            Assert.Greater(storedItem.MaxStackCount, 1);
            Assert.Greater(dropItem.MaxStackCount, 1);
            model.StoreItem(storedItem, new Vector2Int(xPos, yPos));

            return (model, storedItem, dropItem);
        }

        [Test]
        public void CanFindStackableItem()
        {
            const int xPos = 0;
            const int yPos = 0;
            var (model, storedItem, dropItem) = SetupStackScenario(xPos, yPos);

            var foundItem = model.CheckForStackableItem(dropItem, xPos, yPos);
            Assert.IsNotNull(foundItem);
            Assert.AreSame(storedItem, foundItem);
        }

        [Test]
        public void WillNotFindUnstackableItem()
        {
            const int xPos = 0;
            const int yPos = 0;
            var model = CreateModel(10, 10);
            var storedItem = CreateItem(ItemAsset1x1_001);
            var dropItem = CreateItem(StackableItemAsset1x1_001);
            Assert.IsFalse(storedItem.Shared.IsStackable);
            Assert.IsTrue(dropItem.Shared.IsStackable);
            model.StoreItem(storedItem, new Vector2Int(xPos, yPos));

            var foundItem = model.CheckForStackableItem(dropItem, xPos, yPos);
            Assert.IsNull(foundItem);
        }

        [Test]
        public void WillNotFindWhenDropIsUnstackableItem()
        {
            const int xPos = 0;
            const int yPos = 0;
            var model = CreateModel(10, 10);
            var storedItem = CreateItem(StackableItemAsset1x1_001);
            var dropItem = CreateItem(ItemAsset1x1_001);
            Assert.IsTrue(storedItem.Shared.IsStackable);
            Assert.IsFalse(dropItem.Shared.IsStackable);
            model.StoreItem(storedItem, new Vector2Int(xPos, yPos));

            var foundItem = model.CheckForStackableItem(dropItem, xPos, yPos);
            Assert.IsNull(foundItem);
        }

        [Test]
        public void WillNotFindIncompatibleStackItem()
        {
            const int xPos = 0;
            const int yPos = 0;
            var model = CreateModel(10, 10);
            var storedItem = CreateItem(StackableItemAsset1x1_001);
            var dropItem = CreateItem(StackableItemAsset1x1_002);
            Assert.IsTrue(storedItem.Shared.IsStackable);
            Assert.IsTrue(dropItem.Shared.IsStackable);
            model.StoreItem(storedItem, new Vector2Int(xPos, yPos));

            var foundItem = model.CheckForStackableItem(dropItem, xPos, yPos);
            Assert.IsNull(foundItem);
        }

        [Test]
        public void StackExchangesPartialQty()
        {
            var (model, storedItem, dropItem) = SetupStackScenario(0, 0);
            storedItem.StackCount = 5;
            dropItem.StackCount = 4;
            Assert.AreEqual(11, storedItem.Shared.MaxStackSize);

            var foundStack = model.CheckForStackableItem(dropItem, 0, 0);
            Assert.IsNotNull(foundStack);
            Assert.AreEqual(2, model.StackItems(dropItem, foundStack, 2));
            Assert.AreEqual(2, dropItem.StackCount);
            Assert.AreEqual(7, foundStack.StackCount);
        }

        [Test]
        public void StackExchangesFullQty()
        {
            var (model, storedItem, dropItem) = SetupStackScenario(0, 0);
            storedItem.StackCount = 5;
            dropItem.StackCount = 4;
            Assert.AreEqual(11, storedItem.Shared.MaxStackSize);

            var foundStack = model.CheckForStackableItem(dropItem, 0, 0);
            Assert.IsNotNull(foundStack);
            Assert.AreEqual(4, model.StackItems(dropItem, foundStack, 4));
            Assert.AreEqual(0, dropItem.StackCount);
            Assert.AreEqual(9, foundStack.StackCount);
        }

        [Test]
        public void StackDoesNotOverExchangeQty()
        {
            var (model, storedItem, dropItem) = SetupStackScenario(0, 0);
            storedItem.StackCount = 5;
            dropItem.StackCount = 7;
            Assert.AreEqual(11, storedItem.Shared.MaxStackSize);

            var foundStack = model.CheckForStackableItem(dropItem, 0, 0);
            Assert.IsNotNull(foundStack);
            Assert.AreEqual(6, model.StackItems(dropItem, foundStack, 7));
            Assert.AreEqual(1, dropItem.StackCount);
            Assert.AreEqual(11, foundStack.StackCount);
        }

        [Test]
        public void StackSplitPartial()
        {
            var (model, storedItem, dropItem) = SetupStackScenario(0, 0);
            storedItem.StackCount = 5;

            var newItem = model.SplitStackItem(storedItem, 3, () => CreateItem(StackableItemAsset1x1_001));
            Assert.IsNotNull(newItem);
            Assert.AreEqual(2, storedItem.StackCount);
            Assert.AreEqual(3, newItem.StackCount);
        }

        [Test]
        public void StackSplitTotal()
        {
            var (model, storedItem, dropItem) = SetupStackScenario(0, 0);
            storedItem.StackCount = 5;

            var newItem = model.SplitStackItem(storedItem, 5, () => CreateItem(StackableItemAsset1x1_001));
            Assert.IsNotNull(newItem);
            Assert.AreSame(storedItem, newItem);
            Assert.AreEqual(5, storedItem.StackCount);
            Assert.AreEqual(5, newItem.StackCount);

        }
        #endregion

    }
}
