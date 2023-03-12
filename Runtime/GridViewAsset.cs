using UnityEngine;

namespace AdvLifeSim
{
    /// <summary>
    /// Data that is commonly shared by many grid views.
    /// </summary>
    [CreateAssetMenu(fileName = "Grid View Asset", menuName = "PGIA/Grid View Asset")]
    public class GridViewAsset : ScriptableObject
    {
        public string StackQtyId = "StackQty";
        [Tooltip("The color to display on cells when an item can be transfered to a valid location.")]
        public Color ValidColor = Color.blue;
        [Tooltip("The color to display on cells when an item cannot be transferred to a location.")]
        public Color InvalidColor = Color.red;
        [Tooltip("The color to set cells by default.")]
        public Color DefaultColor = Color.clear;
    }
}
