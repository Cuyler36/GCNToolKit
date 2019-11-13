using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using GCNToolKit.Formats.Colors;
using GCNToolKit.Formats.Images;

namespace GCNToolKit.Formats
{
    public sealed class BTI_Header
    {
        public ImageFormat Image_Format;
        public byte Enable_Alpha;
        public ushort Width;
        public ushort Height;
        public Wrap_Mode WrapS;
        public Wrap_Mode WrapT;
        public byte Unknown1;
        public PixelFormat Palette_Format;
        public ushort Palette_Entry_Count;
        public uint Palette_Offset;
        public uint BorderColor;
        public Filter_Type Minification_Filter_Type;
        public Filter_Type Magnification_Filter_Type;
        public byte MinLod;
        public byte MaxLod;
        public byte Mipmap_Count; // Mipmaps + 1
        public byte Unknown2;
        public ushort LodBias;
        public uint Image_Data_Offset;

        public BTI_Header()
        {
            Image_Format = ImageFormat.C8;
            Enable_Alpha = 0x02;
            WrapS = Wrap_Mode.Clamp_Edge;
            WrapT = Wrap_Mode.Clamp_Edge;

            Unknown1 = 0x01; // Appears to be one in most of the Animal Crossing files
            Palette_Format = PixelFormat.RGB5A3;
            BorderColor = 0;
            Minification_Filter_Type = Filter_Type.Nearest;
            Magnification_Filter_Type = Filter_Type.Nearest;
            MinLod = 0;
            MaxLod = 0;
            Mipmap_Count = 1;
            Unknown2 = 0;
            LodBias = 0;
        }

        public BTI_Header(byte[] Data)
        {
            Image_Format = (ImageFormat)Data[0];
            Enable_Alpha = Data[1];
            Width = (ushort)((Data[2] << 8) | Data[3]);
            Height = (ushort)((Data[4] << 8) | Data[5]);
            WrapS = (Wrap_Mode)Data[6];
            WrapT = (Wrap_Mode)Data[7];
            Unknown1 = Data[8];
            Palette_Format = (PixelFormat)Data[9];
            Palette_Entry_Count = (ushort)((Data[10] << 8) | Data[11]);
            Palette_Offset = (uint)((Data[12] << 24) | (Data[13] << 16) | (Data[14] << 8) | Data[15]);
            BorderColor = (uint)((Data[16] << 24) | (Data[17] << 16) | (Data[18] << 8) | Data[19]);
            Minification_Filter_Type = (Filter_Type)Data[20];
            Magnification_Filter_Type = (Filter_Type)Data[21];
            MinLod = Data[22];
            MaxLod = Data[23];
            Mipmap_Count = Data[24];
            Unknown2 = Data[25];
            LodBias = (ushort)((Data[26] << 8) | Data[27]);
            Image_Data_Offset = (uint)((Data[28] << 24) | (Data[29] << 16) | (Data[30] << 8) | Data[31]);
        }
    }

    public sealed class BTI
    {
        public BTI_Header Header;
        public dynamic Palette;
        public byte[] Data;
        public byte[] Image_Data;
        public byte[] Bitmap_Data;
        public Bitmap Bitmap_Image;

        public BTI(int[] Imported_Data, int Width, int Height)
        {
            // Generate "Default" BTI Header (minus Palette & Image/Palette offsets)
            Header = new BTI_Header
            {
                Width = (ushort)Width,
                Height = (ushort)Height
            };

            // Generate Palette and Convert Image to Palette Index
            List<ushort> Palette_List = new List<ushort>();
            byte[] Converted_Data = new byte[Imported_Data.Length];

            for (int i = 0; i < Imported_Data.Length; i++)
            {
                int Current_Int = Imported_Data[i];
                ushort Pixel = RGB5A3.ToRGB5A3((byte)(Current_Int >> 24), (byte)(Current_Int >> 16), (byte)(Current_Int >> 8), (byte)(Current_Int >> 0));
                if (!Palette_List.Contains(Pixel))
                    Palette_List.Add(Pixel);
                int Palette_Idx = Palette_List.IndexOf(Pixel);

                if (Palette_Idx < 0 || Palette_Idx > 255)
                {
                    Console.WriteLine("Palette index was out of bounds!");
                    throw new IndexOutOfRangeException("Palette Index was outside of it's alloted bounds! Value: " + Palette_Idx);
                }

                Converted_Data[i] = (byte)Palette_Idx;
            }

            Palette = Palette_List.ToArray();

            // Turn Converted Data into C8 Encoded Data
            byte[] Encoded_Data = null;
            switch (Header.Image_Format)
            {
                case ImageFormat.C4:
                    Encoded_Data = Images.C4.EncodeC4(Imported_Data, Palette, Width, Height, ColorFormat.RGB5A3);
                    break;
                case ImageFormat.C8:
                    Encoded_Data = Images.C8.EncodeC8(Imported_Data, Palette, Width, Height);
                    break;
            }

            // Write Image Offset & Data
            Header.Image_Data_Offset = 0x20;
            Image_Data = Encoded_Data;

            // Write Palette Offset & Count
            Header.Palette_Offset = (uint)(0x20 + Encoded_Data.Length);
            Header.Palette_Entry_Count = (ushort)Palette.Length;
        }

        public BTI(byte[] data)
        {
            Data = data;
            Header = new BTI_Header(data);
            Bitmap_Data = new byte[Header.Width * Header.Height * 4];

            if (Header.Image_Format == ImageFormat.RGB565 || Header.Image_Format == ImageFormat.RGB5A3)
            {
                ushort[] PixelData = new ushort[(data.Length - 0x20) / 2];
                for (int i = 0, idx = 0x20; i < PixelData.Length; i++, idx += 2)
                {
                    PixelData[i] = (ushort)((data[idx] << 8) | data[idx + 1]);
                }

                // Decode Block Format
                PixelData = SwizzleUtil.Unswizzle(PixelData, Header.Width, Header.Height, 4, 4);

                Bitmap_Data = new byte[PixelData.Length * 4];
                for (int i = 0, idx = 0; i < PixelData.Length; i++, idx += 4)
                {
                    byte A = 0, R = 0, G = 0, B = 0;
                    if (Header.Image_Format == ImageFormat.RGB5A3)
                    {
                        RGB5A3.ToARGB8(PixelData[i], out A, out R, out G, out B);
                    }
                    else
                    {
                        A = 0xFF;
                        RGB565.ToARGB8(PixelData[i], out R, out G, out B);
                    }

                    Bitmap_Data[idx] = B;
                    Bitmap_Data[idx + 1] = G;
                    Bitmap_Data[idx + 2] = R;
                    Bitmap_Data[idx + 3] = A;
                }
            }
            else if (Header.Image_Format == ImageFormat.RGBA8)
            {
                
            }
            else
            {
                switch (Header.Palette_Format)
                {
                    case PixelFormat.RGB565:
                    case PixelFormat.RGB5A3:
                        Palette = new ushort[Header.Palette_Entry_Count];
                        for (int i = 0; i < Header.Palette_Entry_Count; i++)
                        {
                            int offset = (int)(Header.Palette_Offset + i * 2);
                            Palette[i] = (ushort)((Data[offset] << 8) | Data[offset + 1]);
                        }
                        break;
                }
                switch (Header.Image_Format)
                {
                    case ImageFormat.I4:
                        Image_Data = Data.Skip((int)Header.Image_Data_Offset).Take(((Header.Width * Header.Height) + 1) / 2).ToArray();
                        Buffer.BlockCopy(I4.DecodeI4(Image_Data, Header.Width, Header.Height), 0, Bitmap_Data, 0, Bitmap_Data.Length);
                        break;

                    case ImageFormat.I8:
                        Image_Data = Data.Skip((int)Header.Image_Data_Offset).Take(Header.Width * Header.Height).ToArray();
                        Buffer.BlockCopy(I8.DecodeI8(Image_Data, Header.Width, Header.Height), 0, Bitmap_Data, 0, Bitmap_Data.Length);
                        break;

                    case ImageFormat.IA4:
                        Image_Data = Data.Skip((int)Header.Image_Data_Offset).Take(Header.Width * Header.Height).ToArray();
                        Buffer.BlockCopy(IA4.DecodeIA4(Image_Data, Header.Width, Header.Height, Color.White), 0, Bitmap_Data, 0, Bitmap_Data.Length);
                        break;

                    case ImageFormat.IA8:
                        Image_Data = Data.Skip((int)Header.Image_Data_Offset).Take((Header.Width * Header.Height) * 2).ToArray();
                        Buffer.BlockCopy(IA8.DecodeIA8(Image_Data, Header.Width, Header.Height), 0, Bitmap_Data, 0, Bitmap_Data.Length);
                        break;

                    case ImageFormat.C4:
                        Image_Data = Data.Skip((int)Header.Image_Data_Offset).Take((Header.Width * Header.Height) / 2).ToArray();
                        Buffer.BlockCopy(C4.DecodeC4(Image_Data, Palette, Header.Width, Header.Height, (ColorFormat)Header.Palette_Format), 0, Bitmap_Data, 0, Bitmap_Data.Length);
                        break;

                    case ImageFormat.C8:
                        Image_Data = Data.Skip((int)Header.Image_Data_Offset).Take(Header.Width * Header.Height).ToArray();
                        int Index = 0;

                        for (int blockY = 0; blockY < Header.Height; blockY += 4)
                        {
                            for (int blockX = 0; blockX < Header.Width; blockX += 8)
                            {
                                for (int Y = 0; Y < 4; Y++)
                                {
                                    for (int X = 0; X < 8; X++)
                                    {
                                        ushort Pixel = Palette[Image_Data[Index]];

                                        int New_Index = Header.Width * (blockY + Y) + blockX + X;
                                        byte A = 0, R = 0, G = 0, B = 0;
                                        switch (Header.Palette_Format)
                                        {
                                            case PixelFormat.RGB565:
                                                RGB565.ToARGB8(Pixel, out R, out G, out B);
                                                A = 0xFF;
                                                break;
                                            case PixelFormat.RGB5A3:
                                                RGB5A3.ToARGB8(Pixel, out A, out R, out G, out B);
                                                break;
                                            default:
                                                throw new NotImplementedException("Pixel format is not implemented!");
                                        }

                                        Bitmap_Data[New_Index * 4] = B;
                                        Bitmap_Data[New_Index * 4 + 1] = G;
                                        Bitmap_Data[New_Index * 4 + 2] = R;
                                        Bitmap_Data[New_Index * 4 + 3] = A;

                                        Index++;
                                    }
                                }
                            }
                        }
                        break;
                    default:
                        throw new NotImplementedException("Image Format is not implemented!");
                }
            }
            Bitmap_Image = Utilities.BitmapUtilities.CreateBitmap(Bitmap_Data, Header.Width, Header.Height);
        }

        public BTI(string Directory) : this(File.ReadAllBytes(Directory)) { }

        public void Write(string Location)
        {
            try
            {
                using (var Stream = File.Create(Location))
                {
                    using (BinaryWriter Writer = new BinaryWriter(Stream))
                    {
                        void Write(dynamic Data)
                        {
                            Writer.Write(((byte[])typeof(BitConverter)
                              .GetMethod("GetBytes", new Type[] { Data.GetType() }).Invoke(null, new object[] { Data })).Reverse().ToArray());
                        }

                        // Write Header
                        Writer.Write((byte)Header.Image_Format);
                        Writer.Write(Header.Enable_Alpha);
                        Write(Header.Width);
                        Write(Header.Height);
                        Writer.Write((byte)Header.WrapS);
                        Writer.Write((byte)Header.WrapT);
                        Writer.Write(Header.Unknown1);
                        Writer.Write((byte)Header.Palette_Format);
                        Write(Header.Palette_Entry_Count);
                        Write(Header.Palette_Offset);
                        Write(Header.BorderColor);
                        Writer.Write((byte)Header.Minification_Filter_Type);
                        Writer.Write((byte)Header.Magnification_Filter_Type);
                        Writer.Write(Header.MinLod);
                        Writer.Write(Header.MaxLod);
                        Writer.Write(Header.Mipmap_Count);
                        Writer.Write(Header.Unknown2);
                        Write(Header.LodBias);
                        Write(Header.Image_Data_Offset);

                        // Write Image Data
                        Writer.Write(Image_Data);

                        if (Palette != null)
                        {
                            // Write Palette Data
                            for (int i = 0; i < Palette.Length; i++)
                                Write(Palette[i]);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception Occurred: " + e.StackTrace);
                Console.ReadKey();
            }
        }
    }
}
