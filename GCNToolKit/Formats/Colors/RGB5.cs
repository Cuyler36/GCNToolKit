namespace GCNToolKit.Formats.Colors
{
    public static class RGB5
    {
        public static ushort ToRGB5(byte a, byte r, byte g, byte b) =>
            (ushort)(((a >> 7) << 15) | ((r >> 3) << 10) | ((g >> 3) << 5) | (b >> 3));

        public static ushort ToRGB5(uint argb8) => ToRGB5((byte)((argb8 >> 24) & 0xFF), (byte)((argb8 >> 16) & 0xFF),
            (byte)((argb8 >> 8) & 0xFF), (byte)(argb8 & 0xFF));

        public static (byte, byte, byte, byte) To8Bits(ushort rgb5)
        {
            var r = (rgb5 >> 10) & 0x1F;
            var g = (rgb5 >> 5) & 0x1F;
            var b = (rgb5 & 0x1F);
            var transparent = (rgb5 >> 15) & 1;

            return ((byte) (transparent == 1 ? 0 : 255),
                (byte) ((r << 3) | (r >> 2)),
                (byte) ((g << 3) | (g >> 2)),
                (byte) ((b << 3) | (b >> 2)));
        }

        public static uint ToARGB8(ushort rgb5)
        {
            var (a, r, g, b) = To8Bits(rgb5);
            return (uint) ((a << 24) | (r << 16) | (g << 8) | b);
        }
    }
}
