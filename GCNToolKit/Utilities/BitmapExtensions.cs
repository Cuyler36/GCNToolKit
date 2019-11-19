using System.Drawing;

namespace GCNToolKit.Utilities
{
    public static class BitmapExtension
    {
        public static byte[] ToByteArray(this Bitmap bitmap)
        {
            byte[] Data = new byte[bitmap.Width * bitmap.Height * 4];
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color pixelColor = bitmap.GetPixel(x, y);
                    int idx = 4 * (y * bitmap.Width + x);
                    Data[idx] = pixelColor.A;
                    Data[idx + 1] = pixelColor.R;
                    Data[idx + 2] = pixelColor.G;
                    Data[idx + 3] = pixelColor.B;
                }
            }

            return Data;
        }
    }
}
