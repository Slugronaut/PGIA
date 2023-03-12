using UnityEngine;

namespace AdvLifeSim
{
    [CreateAssetMenu(fileName = "Grid View Asset", menuName = "PGIA/Grid View Asset")]
    public class GridViewAsset : ScriptableObject
    {
        public string StackQtyId = "StackQty";
        public Color ValidColor = Color.blue;
        public Color InvalidColor = Color.red;
    }
}
