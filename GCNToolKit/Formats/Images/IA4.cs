using System.Drawing;

namespace GCNToolKit.Formats.Images
{
    public static class IA4
    {
        private static int[] DecodeIA4Routine(byte[] IA4Data, int Width, int Height, Color Color, bool Unswizzle = true)
        {
            int[] GrayscaleData = new int[IA4Data.Length];
            for (int i = 0; i < GrayscaleData.Length; i++)
            {
                byte LeftPixelValue = (byte)(((IA4Data[i] & 0xF0) | (IA4Data[i] >> 4)) & 0xFF); // Alpha
                byte RightPixelValue = (byte)(((IA4Data[i] << 4) | (IA4Data[i] & 0x0F)) & 0xFF);

                GrayscaleData[i] = (LeftPixelValue << 24) | (RightPixelValue << 16) | (RightPixelValue << 8) | RightPixelValue;
            }

            // Apply color to decoded pixel data.
            for (var i = 0; i < GrayscaleData.Length; i++)
            {

            }

            return Unswizzle ? SwizzleUtil.Unswizzle(GrayscaleData, Width, Height, 8, 4) : GrayscaleData;
        }

        private static byte[] EncodeIA4Routine(int[] ImageData, int Width, int Height)
        {
            ImageData = SwizzleUtil.Swizzle(ImageData, Width, Height, 8, 4);
            byte[] PackedIA4Data = new byte[ImageData.Length];

            for (int i = 0; i < PackedIA4Data.Length; i++)
            {
                byte LeftValue = (byte)((ImageData[i] >> 24) & 0xFF); // Alpha
                byte RightValue = (byte)(ImageData[i] >> 16); // Only use red

                PackedIA4Data[i] = (byte)(((LeftValue / 16) << 4) | (RightValue / 16));
            }

            return PackedIA4Data;
        }

        public static int[] DecodeIA4(byte[] IA4Data, int Width, int Height, Color Color, bool Unswizzle = true)
        {
            return DecodeIA4Routine(IA4Data, Width, Height, Color, Unswizzle);
        }

        public static byte[] EncodeIA4(int[] ImageData, int Width, int Height)
        {
            return EncodeIA4Routine(ImageData, Width, Height);
        }
    }
}
