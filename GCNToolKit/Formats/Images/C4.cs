using GCNToolKit.Formats.Colors;
using GCNToolKit.Utilities;

namespace GCNToolKit.Formats.Images
{
    public static class C4
    {
        private static int[] C4ImageSubroutineDecode(byte[] C4ImageData, ushort[] Palette, int Width, int Height, bool UsesBlocks = true)
        {
            C4ImageData = Utilities.Utilities.SeparateNibbles(C4ImageData);
            int[] RGB8Palette = new int[Palette.Length];
            for (int i = 0; i < RGB8Palette.Length; i++)
            {
                RGB8Palette[i] = (int)RGB5A3.ToARGB8(Palette[i]);
            }

            byte[] UnscrambledData = UsesBlocks ? BlockFormat.Decode(C4ImageData, Width, Height, 8, 8) : C4ImageData;
            int[] ImageData = new int[UnscrambledData.Length];

            for (int i = 0; i < ImageData.Length; i++)
            {
                ImageData[i] = RGB8Palette[UnscrambledData[i]];
            }

            return ImageData;
        }

        private static byte[] C4ImageSubroutineEncode(int[] ImageData, ushort[] Palette, int Width, int Height, bool UsesBlocks = true)
        {
            int[] RGB8Palette = new int[Palette.Length];
            for (int i = 0; i < RGB8Palette.Length; i++)
            {
                RGB8Palette[i] = (int)RGB5A3.ToARGB8(Palette[i]);
            }

            byte[] C4Data = new byte[ImageData.Length];
            for (int i = 0; i < C4Data.Length; i++)
            {
                C4Data[i] = ColorUtilities.ClosestColorRGB(ImageData[i], RGB8Palette);
            }

            return Utilities.Utilities.CondenseNibbles(UsesBlocks ? BlockFormat.Encode(C4Data, Width, Height, 8, 8) : C4Data);
        }

        public static int[] DecodeC4(byte[] C4ImageData, ushort[] Palette, int Width, int Height, bool UsesBlocks = true)
        {
            return C4ImageSubroutineDecode(C4ImageData, Palette, Width, Height, UsesBlocks);
        }

        public static byte[] EncodeC4(int[] ImageData, ushort[] Palette, int Width, int Height, bool UsesBlocks = true)
        {
            return C4ImageSubroutineEncode(ImageData, Palette, Width, Height, UsesBlocks);
        }
    }
}
