using Vintagestory.API.Common;

namespace Viconomy.src.Util
{
    public class VinUtils
    {
        /*
         * https://stackoverflow.com/questions/42519/how-do-you-rotate-a-two-dimensional-array
        def rotate(matrix):
            size = len(matrix)
            layer_count = size / 2

            for layer in range(0, layer_count):
                first = layer
                last = size - first - 1

                for element in range(first, last):
                    offset = element - first

                    top = matrix[first][element]
                    right_side = matrix[element][last]
                    bottom = matrix[last][last - offset]
                    left_side = matrix[last - offset][first]

                    matrix[first][element] = left_side
                    matrix[element][last] = top
                    matrix[last][last - offset] = right_side
                    matrix[last - offset][first] = bottom
        */
        public static void Rotate(ItemStack[][] slots)
        {
            int size = slots.Length;

            int n = slots.Length;
            for (int i = 0; i < n / 2; i++)
            {
                for (int j = i; j < n - i - 1; j++)
                {
                    /*
                    ItemStack tmp = slots[i][j];
                    slots[i][j] = slots[j][n - i - 1];
                    slots[j][n - i - 1] = slots[n - i - 1][n - j - 1];
                    slots[n - i - 1][n - j - 1] = slots[n - j - 1][i];
                    slots[n - j - 1][i] = tmp;
                    */

                    ItemStack tmp = slots[i][j];
                    slots[i][j] = slots[n - 1 - j][i];
                    slots[n - 1 - j][i] = slots[n - 1 - i][n - 1 - j];
                    slots[n - 1 - i][n - 1 - j] = slots[j][n - 1 - i];
                    slots[j][n - 1 - i] = tmp;
                }
            }
        }

    }
}
