namespace GCNToolKit.Formats.Colors
{
    /// <summary>
    /// RGB5A3 Pixel Format Functions
    /// </summary>
    public static class RGB5A3
    {
        /// <summary>
        /// Converts a RGB5A3 Pixel to an ARGB8 Pixel in its byte components
        /// </summary>
        /// <param name="pixel">The RGB5A3 Pixel</param>
        /// <param name="A">The ARGB8 Alpha Component</param>
        /// <param name="R">The ARGB8 Red Component</param>
        /// <param name="G">The ARGB8 Green Component</param>
        /// <param name="B">The ARGB8 Blue Component</param>
        public static void ToARGB8(ushort pixel, out byte A, out byte R, out byte G, out byte B)
        {
            if ((pixel & 0x8000) == 0x8000)
            {
                // No Alpha Channel
                A = 0xFF;

                // Separate RGB from bits
                R = (byte)((pixel & 0x7C00) >> 10);
                G = (byte)((pixel & 0x03E0) >> 5);
                B = (byte)(pixel & 0x001F);

                // Convert to RGB8 values
                R = (byte)((R << (8 - 5)) | (R >> (10 - 8)));
                G = (byte)((G << (8 - 5)) | (G >> (10 - 8)));
                B = (byte)((B << (8 - 5)) | (B >> (10 - 8)));
            }
            else
            {
                // An Alpha Channel Exists, 3 bits for Alpha Channel and 4 bits each for RGB
                A = (byte)((pixel & 0x7000) >> 12);
                R = (byte)((pixel & 0x0F00) >> 8);
                G = (byte)((pixel & 0x00F0) >> 4);
                B = (byte)(pixel & 0x000F);

                A = (byte)((A << (8 - 3)) | (A << (8 - 6)) | (A >> (9 - 8)));
                R = (byte)((R << (8 - 4)) | R);
                G = (byte)((G << (8 - 4)) | G);
                B = (byte)((B << (8 - 4)) | B);
            }
        }

        /// <summary>
        /// Converts a RGB5A3 Pixel to an ARGB8 Pixel
        /// </summary>
        /// <param name="Pixel">The RGB5A3 Pixel</param>
        /// <returns>ARGB8 Pixel</returns>
        public static uint ToARGB8(ushort Pixel)
        {
            ToARGB8(Pixel, out byte A, out byte R, out byte G, out byte B);
            return (uint)((A << 24) | (R << 16) | (G << 8) | B);
        }

        /// <summary>
        /// Creates an RGB5A3 Pixel from an ARGB8 Pixel's component values
        /// </summary>
        /// <param name="A">The ARGB8 Alpha Component</param>
        /// <param name="R">The ARGB8 Red Component</param>
        /// <param name="G">The ARGB8 Green Component</param>
        /// <param name="B">The ARGB8 Blue Component</param>
        /// <returns>RGB5A3 Pixel</returns>
        public static ushort ToRGB5A3(byte A, byte R, byte G, byte B)
        {
            if (A >= 0xE0)
            {
                return (ushort)(0x8000 | (((R & 0xF8) << 7) | ((G & 0xF8) << 2) | (B >> 3)));
            }
            else
            {
                return (ushort)(((A & 0xE0) << 7) | ((R & 0xF0) << 4) | (G & 0xF0) | ((B & 0xF0) >> 4));
            }
        }

        /// <summary>
        /// Creates an RGB5A3 Pixel from an ARGB8 Pixel
        /// </summary>
        /// <param name="ARGB8">The ARGB8 Pixel</param>
        /// <returns>RGB5A3 Pixel</returns>
        public static ushort ToRGB5A3(uint ARGB8)
        {
            return ToRGB5A3((byte)((ARGB8 & 0xFF000000) >> 24), (byte)((ARGB8 & 0xFF0000) >> 16), (byte)((ARGB8 & 0xFF00) >> 8), (byte)(ARGB8 & 0xFF));
        }

        /// <summary>
        /// Creates an RGB5A3 Pixel from an ARGB8 Pixel
        /// </summary>
        /// <param name="ARGB8">The ARGB8 Pixel</param>
        /// <returns>RGB5A3 Pixel</returns>
        public static ushort ToRGB5A3(int ARGB8)
        {
            return ToRGB5A3((uint)ARGB8);
        }
    }
}
