namespace GCNToolKit.Formats.Images
{
    public static class I8
    {
        private static int[] DecodeI8Routine(byte[] I8Data, int Width, int Height, bool Unswizzle = true)
        {
            int[] GrayscaleData = new int[I8Data.Length];
            for (int i = 0; i < GrayscaleData.Length; i++)
                GrayscaleData[i] = (I8Data[i] << 24) | (I8Data[i] << 16) | (I8Data[i] << 8) | I8Data[i];

            return Unswizzle ? SwizzleUtil.Unswizzle(GrayscaleData, Width, Height, 8, 4) : GrayscaleData;
        }

        private static byte[] EncodeI8Routine(int[] ImageData, int Width, int Height, bool Swizzle = true)
        {
            if (Swizzle)
                ImageData = SwizzleUtil.Swizzle(ImageData, Width, Height, 8, 4);

            byte[] I8Data = new byte[ImageData.Length];

            for (int i = 0; i < I8Data.Length; i++)
                I8Data[i] = (byte)((((ImageData[i] >> 16) & 0xFF) * 0.2126) + (((ImageData[i] >> 8) & 0xFF) * 0.7152) + ((ImageData[i] & 0xFF) * 0.0722));

            return I8Data;
        }

        public static int[] DecodeI8(byte[] i8Data, int width, int height, bool unswizzle = true)
        {
            return DecodeI8Routine(i8Data, width, height, unswizzle);
        }

        public static byte[] EncodeI8(int[] imageData, int width, int height, bool swizzle)
        {
            return EncodeI8Routine(imageData, width, height, swizzle);
        }
    }
}
