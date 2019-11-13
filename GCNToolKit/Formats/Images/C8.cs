using GCNToolKit.Formats.Colors;

namespace GCNToolKit.Formats.Images
{
    public static class C8
    {
        private static int[] C8ImageSubroutineDecode(byte[] C8ImageData, ushort[] Palette, int Width, int Height, bool Unswizzle)
        {
            int[] RGB8Palette = new int[Palette.Length];
            for (int i = 0; i < RGB8Palette.Length; i++)
            {
                RGB8Palette[i] = (int)RGB5A3.ToARGB8(Palette[i]);
            }

            byte[] UnscrambledData = Unswizzle ? SwizzleUtil.Unswizzle(C8ImageData, Width, Height, 8, 4) : C8ImageData;
            int[] ImageData = new int[UnscrambledData.Length];

            for (int i = 0; i < ImageData.Length; i++)
            {
                ImageData[i] = RGB8Palette[UnscrambledData[i]];
            }

            return ImageData;
        }

        private static byte[] C8ImageSubroutineEncode(int[] ImageData, ushort[] Palette, int Width, int Height, bool Swizzle)
        {
            int[] RGB8Palette = new int[Palette.Length];
            for (int i = 0; i < RGB8Palette.Length; i++)
            {
                RGB8Palette[i] = (int)RGB5A3.ToARGB8(Palette[i]);
            }

            byte[] C8Data = new byte[ImageData.Length];
            for (int i = 0; i < C8Data.Length; i++)
            {
                C8Data[i] = Utilities.ColorUtilities.ClosestColorRGB(ImageData[i], RGB8Palette);
            }

            return Swizzle ? SwizzleUtil.Swizzle(C8Data, Width, Height, 8, 4) : C8Data;
        }

        public static int[] DecodeC8(byte[] c8ImageData, ushort[] palette, int width, int height, bool unswizzle = true)
        {
            return C8ImageSubroutineDecode(c8ImageData, palette, width, height, unswizzle);
        }

        public static byte[] EncodeC8(int[] imageData, ushort[] palette, int width, int height, bool swizzle = true)
        {
            return C8ImageSubroutineEncode(imageData, palette, width, height, swizzle);
        }
    }
}
