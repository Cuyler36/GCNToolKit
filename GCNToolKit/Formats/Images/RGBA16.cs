using GCNToolKit.Formats.Colors;

namespace GCNToolKit.Formats.Images
{
    public static class RGBA16
    {
        public static int[] Decode(ushort[] rgba16Data, int width, int height)
        {
            var dataOut = new int[width * height];
            var position = 0;
            rgba16Data = BlockFormat.Decode(rgba16Data, width, height, 4, 4);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    dataOut[position] = (int) RGB5A3.ToARGB8(rgba16Data[position]);
                    position++;
                }
            }

            return dataOut;
        }

        public static ushort[] Encode(in int[] rgbaData, int width, int height)
        {
            var dataOut = new ushort[width * height];
            var position = 0;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    dataOut[position] = RGB5A3.ToRGB5A3(rgbaData[position]);
                    position++;
                }
            }

            return BlockFormat.Encode(dataOut, width, height, 4, 4);
        }
    }
}
