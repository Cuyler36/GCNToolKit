using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace GCNToolKit.Utilities
{
    public static class BitmapUtilities
    {
        /*
         * Creates a Bitmap from an array of bytes.
         */
        public static Bitmap CreateBitmap(byte[] BitmapBuffer, uint Width = 32, uint Height = 32)
        {
            Bitmap NewBitmap = new Bitmap((int)Width, (int)Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            BitmapData bitmapData = NewBitmap.LockBits(new Rectangle(0, 0, (int)Width, (int)Height), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            System.Runtime.InteropServices.Marshal.Copy(BitmapBuffer, 0, bitmapData.Scan0, BitmapBuffer.Length);
            NewBitmap.UnlockBits(bitmapData);
            return NewBitmap;
        }
    }

    public static class ColorUtilities
    {
        public static byte ClosestColorRGB(int Color, int[] PaletteData)
        {
            double Distance = double.MaxValue;
            byte ClosestPaletteIndex = 0;
            double R = Color & 0xFF;
            double G = (Color >> 8) & 0xFF;
            double B = (Color >> 16) & 0xFF;
            double A = (Color >> 24) & 0xFF;

            for (int i = 0; i < PaletteData.Length; i++)
            {
                int PaletteColor = PaletteData[i];
                double pR = PaletteColor & 0xFF;
                double pG = (PaletteColor >> 8) & 0xFF;
                double pB = (PaletteColor >> 16) & 0xFF;
                double pA = (PaletteColor >> 24) & 0xFF;

                double ThisDistance = Math.Sqrt(Math.Pow(pR - R, 2) + Math.Pow(pG - G, 2) + Math.Pow(pB - B, 2) + Math.Pow(pA - A, 2));
                if (ThisDistance == 0)
                {
                    // Perfect match
                    return (byte)i;
                }
                else if (ThisDistance < Distance)
                {
                    Distance = ThisDistance;
                    ClosestPaletteIndex = (byte)i;
                }
            }

            return ClosestPaletteIndex;
        }
    }

    public static class Utilities
    {
        public static byte[] SeparateNibbles(byte[] Input)
        {
            byte[] Output = new byte[Input.Length * 2];
            for (int i = 0, idx = 0; i < Input.Length; i++, idx += 2)
            {
                Output[idx] = (byte)((Input[i] >> 4) & 0x0F);
                Output[idx + 1] = (byte)(Input[i] & 0x0F);
            }

            return Output;
        }

        public static byte[] CondenseNibbles(byte[] Input)
        {
            if (Input.Length % 2 == 1)
            {
                throw new Exception("Error: byte[] Input must be aligned to 2 (it must have an even amount of data in it)");
            }

            byte[] Output = new byte[Input.Length / 2];
            for (int i = 0, idx = 0; i < Output.Length; i++, idx += 2)
            {
                Output[i] = (byte)(((Input[idx] & 0x0F) << 4) | (Input[idx + 1] & 0x0F));
            }

            return Output;
        }
    }
}
