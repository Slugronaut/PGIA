using System.Collections.Generic;
using UnityEngine;


namespace PGIA
{
    /// <summary>
    /// Helper class for solving knapsack problem for grids and items.
    /// </summary>
    public static class KnapsackSolver
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="containerSize"></param>
        /// <param name="itemSizes"></param>
        /// <returns></returns>
        public static List<Vector2Int> Solve(Vector2Int containerSize, List<Vector2Int> itemSizes)
        {
            int n = itemSizes.Count;
            int[,] table = new int[n + 1, containerSize.x + 1];
            bool[,] keep = new bool[n + 1, containerSize.x + 1];

            // Initialize the first row and column of the table to 0
            for (int j = 0; j <= containerSize.x; j++)
            {
                table[0, j] = 0;
            }
            for (int i = 0; i <= n; i++)
            {
                table[i, 0] = 0;
            }

            // Fill in the table
            for (int i = 1; i <= n; i++)
            {
                Vector2Int itemSize = itemSizes[i - 1];

                for (int j = 1; j <= containerSize.x; j++)
                {
                    if (itemSize.x <= j && table[i - 1, j - itemSize.x] + itemSize.y > table[i - 1, j])
                    {
                        table[i, j] = table[i - 1, j - itemSize.x] + itemSize.y;
                        keep[i, j] = true;
                    }
                    else
                    {
                        table[i, j] = table[i - 1, j];
                        keep[i, j] = false;
                    }
                }
            }

            // Find the items that were selected
            List<Vector2Int> selectedItems = new();
            int remainingWidth = containerSize.x;

            for (int i = n; i >= 1; i--)
            {
                if (keep[i, remainingWidth])
                {
                    Vector2Int itemSize = itemSizes[i - 1];
                    selectedItems.Add(itemSize);
                    remainingWidth -= itemSize.x;
                }
            }

            // Reverse the list to get the items in the correct order
            selectedItems.Reverse();

            return selectedItems;
        }
    }
}

