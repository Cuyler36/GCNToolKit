using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCNToolKit.Formats
{
    public static class Yay0
    {
        public static byte[] Decompress(byte[] CompressedData)
        {
            if (Encoding.ASCII.GetString(CompressedData, 0, 4).Equals("Yay0"))
            {
                uint DecompressedSize = BitConverter.ToUInt32(CompressedData, 4).Reverse();
                uint CountOffset = BitConverter.ToUInt32(CompressedData, 8).Reverse();
                uint DataOffset = BitConverter.ToUInt32(CompressedData, 12).Reverse();

                CompressedData = CompressedData.Skip(0x10).ToArray();
                byte[] DecompressedFileData = new byte[DecompressedSize];

                int CodePosition = 0;
                int Write_Position = 0;
                uint ValidBitCount = 0;
                byte CurrentCodeByte = 0;

                while (Write_Position < DecompressedSize)
                {
                    if (ValidBitCount == 0)
                    {
                        CurrentCodeByte = CompressedData[CodePosition];
                        ++CodePosition;
                        ValidBitCount = 8;
                    }

                    if ((CurrentCodeByte & 0x80) != 0)
                    {
                        DecompressedFileData[Write_Position] = CompressedData[DataOffset];
                        Write_Position++;
                        DataOffset++;
                    }
                    else
                    {
                        try
                        {
                            byte Byte1 = CompressedData[CountOffset];
                            byte Byte2 = CompressedData[CountOffset + 1];
                            CountOffset += 2;

                            uint Dist = (uint)(((Byte1 & 0xF) << 8) | Byte2);
                            uint CopySource = (uint)(Write_Position - (Dist + 1));

                            uint Byte_Count = (uint)(Byte1 >> 4);
                            if (Byte_Count == 0)
                            {
                                Byte_Count = (uint)(CompressedData[DataOffset] + 0x12);
                                DataOffset++;
                            }
                            else
                            {
                                Byte_Count += 2;
                            }

                            for (int i = 0; i < Byte_Count; ++i)
                            {
                                DecompressedFileData[Write_Position] = DecompressedFileData[CopySource];
                                CopySource++;
                                Write_Position++;
                            }
                        }
                        catch (Exception e)
                        {
                            System.Windows.MessageBox.Show(e.Message + "\n" + e.StackTrace);
                            return null;
                        }
                        CurrentCodeByte <<= 1;
                        ValidBitCount -= 1;
                    }

                    return DecompressedFileData;
                }
            }
            else
            {
                System.Windows.MessageBox.Show("The selected file does not to be a Yay0 compressed file!", "Yay0 Decompress Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return null;
            }

            return null;
        }
    }
}
