using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

namespace PGIA.Test
{
    public class StoreItemTester : MonoBehaviour
    {
        public GridItemModelBehaviour Item;
        public GridModelBehaviour Inventory;
        public int x = 0;
        public int y = 0;

        public GridItemModelBehaviour[] AutoItems;

        private IEnumerator Start()
        {
            yield return null;
            foreach(var item in AutoItems)
            {
                var loc = Inventory.FindOpenSpace(item.Size.x, item.Size.y);
                if (loc != null)
                    Inventory.StoreItem(item, loc.Value.position);
            }
        }

        [Button("Test Store")]
        public void Store()
        {
            if (!Application.isPlaying) return;

            if(Inventory.StoreItem(Item, new Vector2Int(x, y)))
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
