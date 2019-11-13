namespace GCNToolKit.Formats.Images
{
    public static class IA8
    {
        private static int[] DecodeIA8Routine(byte[] IA8Data, int Width, int Height, bool Unswizzle = true)
        {
            int[] GrayscaleData = new int[IA8Data.Length / 2];
            for (int i = 0; i < GrayscaleData.Length; i++)
            {
                int idx = i * 2;
                GrayscaleData[i] = (IA8Data[idx] << 24) | (IA8Data[idx + 1] << 16) | (IA8Data[idx + 1] << 8) | IA8Data[idx + 1];
            }

            return Unswizzle ? SwizzleUtil.Unswizzle(GrayscaleData, Width, Height, 4, 4) : GrayscaleData;
        }

        private static byte[] EncodeIA8Routine(int[] ImageData, int Width, int Height, bool Swizzle = true)
        {
            ImageData = Swizzle ? SwizzleUtil.Swizzle(ImageData, Width, Height, 4, 4) : ImageData;
            byte[] IA8Data = new byte[ImageData.Length * 2];

            for (int i = 0; i < ImageData.Length; i++)
            {
                int idx = i * 2;
                IA8Data[idx] = (byte)(ImageData[i] >> 24);
                IA8Data[idx + 1] = (byte)((((ImageData[i] >> 16) & 0xFF) * 0.2126) + (((ImageData[i] >> 8) & 0xFF) * 0.7152)
                    + ((ImageData[i] & 0xFF) * 0.0722));
            }

            return IA8Data;
        }

        public static int[] DecodeIA8(byte[] I4Data, int Width, int Height, bool Unswizzle = true)
        {
            return DecodeIA8Routine(I4Data, Width, Height, Unswizzle);
        }

        public static byte[] EncodeIA8(int[] ImageData, int Width, int Height, bool Swizzle = true)
        {
            return EncodeIA8Routine(ImageData, Width, Height, Swizzle);
        }
    }
}
