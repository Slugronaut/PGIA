using UnityEngine;

namespace PGIA
{
    /// <summary>
    /// Data that is commonly shared by many grid views.
    /// </summary>
    [CreateAssetMenu(fileName = "Grid View Asset", menuName = "PGIA/Grid View Asset")]
    public class GridViewAsset : ScriptableObject
    {
        [Space(12)]
        [Tooltip("The name used to identify the visual element in cells when displaying stack counts.")]
        public string StackQtyId = "StackQty";
        [Tooltip("If the pointer is pressed down and doesn't move beyond this threshold the the drag will be 'sticky' upon release. I.E. it will not immeditately be dropped but will instead require another press.")]
        public float StickyDragMoveThreshold = 12;

        [Space(12)]
        [Tooltip("The color to display when hovering the pointer over an item while not dragging anything.")]
        public Color HilightColorBackground = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        public Color HilightColorIcon = Color.white;

        [Space(12)]
        [Tooltip("The color to display on cells when an item can be transfered to a valid location.")]
        public Color ValidColorBackground = new Color(0, 0, 1, 0.5f);
        public Color ValidColorIcon = new Color(0.5f, 0.5f, 1, 1);

        [Space(12)]
        [Tooltip("The color to display on cells when an item cannot be transferred to a location.")]
        public Color InvalidColorBackground = new Color(1, 0, 0, 0.5f);
        public Color InvalidColorIcon = new Color(1, 0.5f, 0.5f, 1);

        [Space(12)]
        [Tooltip("The color to set cells by default.")]
        public Color DefaultColorBackground = Color.clear;
        public Color DefaultColorIcon = Color.white;
    }
}
