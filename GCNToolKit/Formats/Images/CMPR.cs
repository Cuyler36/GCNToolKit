using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCNToolKit.Formats.Images
{
    public static class CMPR
    {
        public static int[] Decode(in byte[] data, int width, int height, bool unswizzle = true)
        {
            int[] output = new int[width * height];
            return output;
        }
    }
}
