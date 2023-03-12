using Sirenix.OdinInspector;
using UnityEngine;

namespace PGIA.Test
{
    public class StoreItemTester : MonoBehaviour
    {
        public GridItemModelBehaviour Item;
        public GridModelBehaviour Inventory;
        public int x = 0;
        public int y = 0;

        [Button("Test Store")]
        public void Store()
        {
            if (!Application.isPlaying) return;

            if(Inventory.StoreItem(Item, new RectInt(x, y, Item.Shared.Size.x, Item.Shared.Size.y)))
            {
                Debug.Log("According to PGIA, the STORE operation was a success.");
            }
            else
            {
                Debug.Log("He's dead, Jim");
            }
        }

        [Button("Test Remove")]
        public void Remove()
        {
            if (!Application.isPlaying) return;

            if (Inventory.RemoveItem(Item))
            {
                Debug.Log("According to PGIA, the REMOVE operation was a success.");
            }
            else
            {
                Debug.Log("Well that's just great, man.");
            }
        }
    }
}
