using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace GCNToolKit.Utilities
{
    public static class BitmapExtension
    {
        // From: https://stackoverflow.com/questions/26260654/wpf-converting-bitmap-to-imagesource
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        public static BitmapSource ToBitmapSource(this Bitmap bitmap)
        {
            var handle = bitmap.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }

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
