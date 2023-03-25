using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace PGIA
{
    /// <summary>
    /// That one class that exists in every single module of C# code ever. You know the one. 
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queryPath"></param>
        static public VisualElement ParseQueryPath(VisualElement root, string queryPath)
        {
            if (string.IsNullOrEmpty(queryPath)) return null;
            var elements = queryPath.Split('.').Where(x => !string.IsNullOrEmpty(x));
            if (elements.Count() < 2)
                return root.Q<VisualElement>(queryPath);

            VisualElement newRoot = root;
            foreach (var element in elements)
            {
                var t = newRoot.Q<VisualElement>(element);
                if (t == null) break;
                newRoot = t;
            }

            return newRoot;
        }

        /// <summary>
        /// Works up the hierarchy until it cannot find anymore VisualElements and then returns the last one found.
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
        static public VisualElement FindRootElement(VisualElement start)
        {
            VisualElement root = start;
            while (true)
            {
                if (root == null)
                    break;
                if (root.parent == null) 
                    break;
                var parentType = root.parent.GetType();
                if (parentType != typeof(VisualElement) && !parentType.IsSubclassOf(typeof(VisualElement)))
                    break;
                root = root.parent;
            }

            return root;
        }
    }
}
