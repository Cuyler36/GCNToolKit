using GCNToolKit.Formats.Colors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCNToolKit.Utilities
{
    public static class PaletteManager
    {
        public static int[] GetRGBA8Palette(ushort[] rawPalette, ColorFormat colorFormat)
        {
            if (rawPalette == null) throw new ArgumentNullException($"{nameof(rawPalette)} cannot be null!");

            var palette = new int[rawPalette.Length];
            switch (colorFormat)
            {
                case ColorFormat.RGB565:
                    for (var i = 0; i < palette.Length; i++)
                    {
                        palette[i] = (int)RGB565.ToARGB8(rawPalette[i]);
                    }

                    break;

                case ColorFormat.RGB5A1:
                    for (var i = 0; i < palette.Length; i++)
                    {
                        palette[i] = (int)RGB5.ToARGB8(rawPalette[i]);
                    }

                    break;

                case ColorFormat.RGB5A3:
                    for (var i = 0; i < palette.Length; i++)
                    {
                        palette[i] = (int)RGB5A3.ToARGB8(rawPalette[i]);
                    }

                    break;

                case ColorFormat.RGBA4:
                    for (var i = 0; i < palette.Length; i++)
                    {
                        palette[i] = (int)RGBA4.ToARGB8(rawPalette[i]);
                    }

                    break;
            }

            return palette;
        }
    }
}
