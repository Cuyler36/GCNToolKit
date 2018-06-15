namespace GCNToolKit
{
    public enum ImageFormat : byte
    {
        I4 = 0,
        I8 = 1,
        IA4 = 2,
        IA8 = 3,
        RGB565 = 4,
        RGB5A3 = 5,
        RGBA8 = 6,
        // 7 is unknown
        C4 = 8,
        C8 = 9,
        C14X2 = 10,
        // 11 - 13 are unknown
        CMPR = 14
    }

    public enum PixelFormat : byte
    {
        IA8 = 0,
        RGB565 = 1,
        RGB5A3 = 2
    }

    public enum Wrap_Mode : byte
    {
        Clamp_Edge = 0,
        Repeat = 1,
        Mirrored_Repeat = 2
    }
    public enum Filter_Type : byte
    {
        // Min & Mag Filter
        Nearest = 1,
        Linear = 2,

        // Min Filter only

        Nearest_Mipmap_Nearest = 3,
        Nearest_Mipmap_Linear = 4,
        Linear_Mipmap_Nearest = 5,
        Linear_Mipmap_Linear = 6
    }
}
