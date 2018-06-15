namespace GCNToolKit.Formats.Colors
{
    /// <summary>
    /// RGBA4 Pixel Format Functions
    /// </summary>
    public static class RGBA4
    {
        /// <summary>
        /// Converts an ARGB8 Pixel's component values to a RGBA4 value
        /// </summary>
        /// <param name="R">The ARGB8 Red Component</param>
        /// <param name="G">The ARGB8 Green Component</param>
        /// <param name="B">The ARGB8 Blue Component</param>
        /// /// <param name="A">The ARGB8 Alpha Component</param>
        /// <returns>RGBA4 Pixel</returns>
        public static ushort ToRGBA4(byte R, byte G, byte B, byte A)
        {
            return (ushort)(((R & 0xF0) << 8) | ((G & 0xF0) << 4) | (B & 0xF0) | (A & 0xF0) >> 4);
        }

        /// <summary>
        /// Converts an ARGB8 Pixel into a RGBA4 Pixel
        /// </summary>
        /// <param name="ARGB8">The ARGB8 Pixel</param>
        /// <returns>RGBA4 Pixel</returns>
        public static ushort ToRGBA4(int ARGB8)
        {
            return ToRGBA4((byte)((ARGB8 & 0xFF0000) >> 16), (byte)((ARGB8 & 0xFF00) >> 8), (byte)(ARGB8 & 0xFF), (byte)((ARGB8 & 0xFF000000) >> 24));
        }

        /// <summary>
        /// Converts an ARGB8 Pixel into a RGBA4 Pixel
        /// </summary>
        /// <param name="ARGB8">The ARGB8 Pixel</param>
        /// <returns>RGBA4 Pixel</returns>
        public static ushort ToRGBA4(uint ARGB8)
        {
            return ToRGBA4((int)ARGB8);
        }

        /// <summary>
        /// Converts a RGBA4 Pixel to an ARGB8 Pixel
        /// </summary>
        /// <param name="RGBA4">The RGBA4 Pixel</param>
        /// <returns>ARGB8 Pixel</returns>
        public static uint ToARGB8(ushort RGBA4)
        {
            int R = (RGBA4 & 0xF000) >> 12;
            int G = (RGBA4 & 0xF00) >> 8;
            int B = (RGBA4 & 0xF0) >> 4;
            int A = (RGBA4 & 0xF);

            R |= (R << 4);
            G |= (G << 4);
            B |= (B << 4);
            A |= (A << 4);

            return (uint)((A << 24) | (R << 16) | (G << 8) | B);
        }
    }
}
