namespace GCNToolKit.Formats.Colors
{
    public static class BGR5A1
    {
        public static ushort ToBGR5A1(byte a, byte r, byte g, byte b) =>
            (ushort)(((a >> 7) << 15) | ((b >> 3) << 10) | ((g >> 3) << 5) | (r >> 3));

        public static ushort ToBGR5A1(uint argb8) => ToBGR5A1((byte)((argb8 >> 24) & 0xFF), (byte)(argb8 & 0xFF),
            (byte)((argb8 >> 8) & 0xFF), (byte)((argb8 >> 16) & 0xFF));

        public static (byte, byte, byte, byte) To8Bits(ushort bgr5a1)
        {
            var b = (bgr5a1 >> 10) & 0x1F;
            var g = (bgr5a1 >> 5) & 0x1F;
            var r = (bgr5a1 & 0x1F);
            var transparent = (bgr5a1 >> 15) & 1;

            return ((byte)(transparent == 1 ? 0 : 255),
                (byte)((r << 3) | (r >> 2)),
                (byte)((g << 3) | (g >> 2)),
                (byte)((b << 3) | (b >> 2)));
        }

        public static uint ToARGB8(ushort bgr5a1)
        {
            var (a, r, g, b) = To8Bits(bgr5a1);
            return (uint)((a << 24) | (r << 16) | (g << 8) | b);
        }
    }
}
