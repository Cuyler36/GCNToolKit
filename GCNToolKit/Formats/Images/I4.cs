namespace GCNToolKit.Formats.Images
{
    public class I4
    {
        private static int[] DecodeI4Routine(byte[] I4Data, int Width, int Height, bool Unswizzle = true)
        {
            int[] GrayscaleData = new int[I4Data.Length * 2];
            for (int i = 0; i < I4Data.Length; i++)
            {
                byte LeftPixelValue = (byte)((I4Data[i] & 0xF0) | (I4Data[i] >> 4));
                byte RightPixelValue = (byte)((I4Data[i] << 4) | (I4Data[i] & 0x0F));

                int idx = i * 2;
                GrayscaleData[idx] = (0xFF << 24) | (LeftPixelValue << 16) | (LeftPixelValue << 8) | LeftPixelValue;
                GrayscaleData[idx + 1] = (0xFF << 24) | (RightPixelValue << 16) | (RightPixelValue << 8) | RightPixelValue;
            }

            return Unswizzle ? BlockFormat.Decode(GrayscaleData, Width, Height, 8, 8) : GrayscaleData;
        }

        private static byte[] EncodeI4Routine(int[] ImageData, int Width, int Height)
        {
            ImageData = BlockFormat.Encode(ImageData, Width, Height, 8, 8);
            byte[] PackedI4Data = new byte[ImageData.Length / 2];

            // We're only taking the red channel here for re-encoding.
            for (int i = 0; i < PackedI4Data.Length; i++)
            {
                int idx = i * 2;
                byte LeftValue = (byte)(ImageData[idx] >> 16);
                byte RightValue = (byte)(ImageData[idx + 1] >> 16);
                PackedI4Data[i] = (byte)(((LeftValue / 0x10) << 4) | (RightValue / 0x10));
            }

            return PackedI4Data;
        }

        public static int[] DecodeI4(byte[] I4Data, int Width, int Height, bool Unswizzle = true)
        {
            return DecodeI4Routine(I4Data, Width, Height, Unswizzle);
        }

        public static byte[] EncodeI4(int[] ImageData, int Width, int Height)
        {
            return EncodeI4Routine(ImageData, Width, Height);
        }
    }
}
