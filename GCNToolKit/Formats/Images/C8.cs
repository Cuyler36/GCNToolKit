using GCNToolKit.Formats.Colors;

namespace GCNToolKit.Formats.Images
{
    public static class C8
    {
        private static int[] C8ImageSubroutineDecode(byte[] C8ImageData, ushort[] Palette, int Width, int Height)
        {
            int[] RGB8Palette = new int[Palette.Length];
            for (int i = 0; i < RGB8Palette.Length; i++)
            {
                RGB8Palette[i] = (int)RGB5A3.ToARGB8(Palette[i]);
            }

            byte[] UnscrambledData = BlockFormat.Decode(C8ImageData, Width, Height, 8, 4);
            int[] ImageData = new int[UnscrambledData.Length];

            for (int i = 0; i < ImageData.Length; i++)
            {
                ImageData[i] = RGB8Palette[UnscrambledData[i]];
            }

            return ImageData;
        }

        private static byte[] C8ImageSubroutineEncode(int[] ImageData, ushort[] Palette, int Width, int Height)
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

            return BlockFormat.Encode(C8Data, Width, Height, 8, 4);
        }

        public static int[] DecodeC8(byte[] C8ImageData, ushort[] Palette, int Width, int Height)
        {
            return C8ImageSubroutineDecode(C8ImageData, Palette, Width, Height);
        }

        public static byte[] EncodeC8(int[] ImageData, ushort[] Palette, int Width, int Height)
        {
            return C8ImageSubroutineEncode(ImageData, Palette, Width, Height);
        }
    }
}
