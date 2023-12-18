using Cairo;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
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
            int layerCount = (int)Math.Ceiling(size / 2.0f);

            for (int i = 0; i < layerCount; i++)
            {
                int first = i;
                int last = size - first - 1;

                for (int element = first; element < last; element++)
                {
                    int offset = element - first;

                    ItemStack top = slots[first][element];
                    ItemStack right = slots[element][last];
                    ItemStack bottom = slots[element][last - offset];
                    ItemStack left = slots[last - offset][first];

                    slots[first][element] = left;
                    slots[element][last] = top;
                    slots[last][last - offset] = right;
                    slots[last - offset][first] = bottom;
                }
            }
        }

    }
}
