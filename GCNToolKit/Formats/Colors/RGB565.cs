namespace GCNToolKit.Formats.Colors
{
    /// <summary>
    /// RGB565 Pixel Format Functions
    /// </summary>
    public static class RGB565
    {
        /// <summary>
        /// Converts a RGB8 Pixel's component values into a RGB565 Pixel
        /// </summary>
        /// <param name="R">The RGB8 Red Component</param>
        /// <param name="G">The RGB8 Green Component</param>
        /// <param name="B">The RGB8 Blue Component</param>
        /// <returns>RGB565 Pixel</returns>
        public static ushort ToRGB565(byte R, byte G, byte B)
        {
            return (ushort)(((R >> 3) << 11) | ((G >> 2) << 5) | (B >> 3));
        }

        /// <summary>
        /// Converts a RGB8 Pixel into a RGB565 Pixel
        /// </summary>
        /// <param name="RGB8">The RGB8 Pixel</param>
        /// <returns>RGB565 Pixel</returns>
        public static ushort ToRGB565(int RGB8)
        {
            return ToRGB565((byte)((RGB8 & 0xFF0000) >> 16), (byte)((RGB8 & 0xFF00) >> 8), (byte)(RGB8 & 0xFF));
        }

        /// <summary>
        /// Converts a RGB8 Pixel into a RGB565 Pixel
        /// </summary>
        /// <param name="RGB8">The RGB8 Pixel</param>
        /// <returns>RGB565 Pixel</returns>
        public static ushort ToRGB565(uint RGB8)
        {
            return ToRGB565((int)RGB8);
        }

        public static void ToARGB8(ushort RGB565, out byte R, out byte G, out byte B)
        {
            int r = (RGB565 >> 11) & 0x1F;
            int g = (RGB565 >> 5) & 0x3F;
            int b = (RGB565 & 0x1F);

            R = (byte)((r << 3) | (r >> 2));
            G = (byte)((g << 2) | (g >> 4));
            B = (byte)((b << 3) | (b >> 2));
        }

        /// <summary>
        /// Converts a RGB565 Pixel into an ARGB8 Pixel
        /// </summary>
        /// <param name="RGB565">The RGB565 Pixel</param>
        /// <returns>ARGB8 Pixel</returns>
        public static uint ToARGB8(ushort RGB565)
        {
            ToARGB8(RGB565, out byte R, out byte G, out byte B);
            return (uint)((0xFF << 24) | (R << 16) | (G << 8) | B);
        }
    }
}
