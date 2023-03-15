using UnityEngine;


namespace PGIA
{
    /// <summary>
    /// That one class that exists in every single model of C# code ever. You know the one. 
    /// It has all of those random functions that just don't seem to belong anywhere in particular.
    /// </summary>
    public static class Utilities
    {
        /// <summary>
        /// Clips a rect to fit within the confines of this model's grid bounds.
        /// Returns null if the source region is not within the grid at all.
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        static public RectInt ClipRegion(int maxWidth, int maxHeight, RectInt region)
        {
            int xMin = Mathf.Max(region.xMin, 0);
            int yMin = Mathf.Max(region.yMin, 0);
            int xMax = Mathf.Min(region.xMax, maxWidth);
            int yMax = Mathf.Min(region.yMax, maxHeight);

            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }
    }
}
