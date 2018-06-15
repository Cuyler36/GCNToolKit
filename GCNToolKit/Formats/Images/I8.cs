namespace GCNToolKit.Formats.Images
{
    public static class I8
    {
        private static int[] DecodeI8Routine(byte[] I8Data, int Width, int Height)
        {
            int[] GrayscaleData = new int[I8Data.Length];
            for (int i = 0; i < GrayscaleData.Length; i++)
            {
                GrayscaleData[i] = (I8Data[i] << 24) | (I8Data[i] << 16) | (I8Data[i] << 8) | I8Data[i];
            }

            return BlockFormat.Decode(GrayscaleData, Width, Height, 8, 4);
        }

        private static byte[] EncodeI8Routine(int[] ImageData, int Width, int Height)
        {
            ImageData = BlockFormat.Encode(ImageData, Width, Height, 8, 4);
            byte[] I8Data = new byte[ImageData.Length];

            for (int i = 0; i < I8Data.Length; i++)
            {
                I8Data[i] = (byte)((((ImageData[i] >> 16) & 0xFF) * 0.2126) + (((ImageData[i] >> 8) & 0xFF) * 0.7152) + ((ImageData[i] & 0xFF) * 0.0722));
            }

            return I8Data;
        }

        public static int[] DecodeI8(byte[] I4Data, int Width, int Height)
        {
            return DecodeI8Routine(I4Data, Width, Height);
        }

        public static byte[] EncodeI8(int[] ImageData, int Width, int Height)
        {
            return EncodeI8Routine(ImageData, Width, Height);
        }
    }
}
