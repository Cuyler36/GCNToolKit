using System;
using System.IO;
using System.Linq;
using System.Text;

namespace GCNToolKit.Formats
{
    public static class Yaz0
    {
        public static byte[] Decompress(byte[] EncodedFileData)
        {
            if (Encoding.ASCII.GetString(EncodedFileData, 0, 4) == "Yaz0")
            {
                int DecompressedFileSize = BitConverter.ToInt32(EncodedFileData, 4).Reverse();
                EncodedFileData = EncodedFileData.Skip(0x10).ToArray();
                byte[] DecompressedFileData = new byte[DecompressedFileSize];

                int Read_Position = 0;
                int Write_Position = 0;
                uint ValidBitCount = 0;
                byte CurrentCodeByte = 0;

                while (Write_Position < DecompressedFileSize)
                {
                    if (ValidBitCount == 0)
                    {
                        CurrentCodeByte = EncodedFileData[Read_Position++];
                        ValidBitCount = 8;
                    }

                    if ((CurrentCodeByte & 0x80) != 0)
                    {
                        DecompressedFileData[Write_Position++] = EncodedFileData[Read_Position++];
                    }
                    else
                    {
                        byte Byte1 = EncodedFileData[Read_Position];
                        byte Byte2 = EncodedFileData[Read_Position + 1];
                        Read_Position += 2;

                        uint Dist = (uint)(((Byte1 & 0xF) << 8) | Byte2);
                        uint CopySource = (uint)(Write_Position - (Dist + 1));

                        uint Byte_Count = (uint)(Byte1 >> 4);
                        if (Byte_Count == 0)
                        {
                            Byte_Count = (uint)(EncodedFileData[Read_Position++] + 0x12);
                        }
                        else
                        {
                            Byte_Count += 2;
                        }

                        for (int i = 0; i < Byte_Count; ++i)
                        {
                            DecompressedFileData[Write_Position++] = DecompressedFileData[CopySource++];
                        }
                    }

                    CurrentCodeByte <<= 1;
                    ValidBitCount -= 1;
                }

                return DecompressedFileData;
            }
            else
            {
                System.Windows.MessageBox.Show("The selected file does not to be a Yaz0 compressed file!", "Yaz0 Decompress Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return null;
            }
        }

        private static uint Encode(byte[] Source, int Size, int Position, ref uint MatchPosition)
        {
            MatchPosition = 0;

            int startPosition = Position - 0x1000;
            uint byteCount = 1;
            int i = 0;
            int j = 0;

            if (startPosition < 0)
                startPosition = 0;

            for (i = startPosition; i < Position; i++)
            {
                for (j = 0; j < Size - Position; j++)
                {
                    if (Source[i + j] != Source[j + Position])
                        break;
                }

                if (j > byteCount)
                {
                    byteCount = (uint)j;
                    MatchPosition = (uint)i;
                }
            }

            if (byteCount == 2)
                byteCount = 1;

            return byteCount;
        }

        private static bool previousFlag = false;
        private static uint previousByteCount = 0;
        private static uint previousMatchPosition = 0;

        private static uint NintendoEncode(byte[] Source, int Size, int Position, ref uint MatchPosition)
        {
            uint byteCount = 1;
            
            if (previousFlag == true)
            {
                MatchPosition = previousMatchPosition;
                previousFlag = false;
                return previousByteCount;
            }

            byteCount = Encode(Source, Size, Position, ref previousMatchPosition);
            MatchPosition = previousMatchPosition;

            if (byteCount >= 3)
            {
                previousByteCount = Encode(Source, Size, Position + 1, ref previousMatchPosition);

                if (previousByteCount >= byteCount + 2)
                {
                    byteCount = 1;
                    previousFlag = true;
                }
            }

            return byteCount;
        }

        /*
         * Compresses a file using Yaz0 compression
         * 
         * Params:
         *  @Data = data array to compress
         * 
         * Returns:
         *  @byte[] CompressedData = a byte array containing a file header ["Yaz0", Decompress Size] and the compressed data
         */
        public static byte[] Compress(byte[] Data)
        {
            int OutputBufferSize = 0;
            int SourcePosition = 0;
            int WritePosition = 0;
            byte[] OutputBuffer = new byte[24];
            MemoryStream OutputStream = new MemoryStream();

            uint ValidBitCount = 0;
            byte CurrentCodeByte = 0;
            uint ByteCount = 0;
            uint MatchPosition = 0;
            byte A = 0, B = 0, C = 0;

            while (SourcePosition < Data.Length)
            {
                ByteCount = 0;
                MatchPosition = 0;

                ByteCount = NintendoEncode(Data, Data.Length, SourcePosition, ref MatchPosition);

                if (ByteCount < 3)
                {
                    OutputBuffer[WritePosition] = Data[SourcePosition];
                    WritePosition++;
                    SourcePosition++;
                    CurrentCodeByte |= (byte)(0x80 >> (int)ValidBitCount);
                }
                else
                {
                    uint Distance = (uint)(SourcePosition - MatchPosition - 1);
                    A = 0;
                    B = 0;
                    C = 0;

                    if (ByteCount >= 0x12)
                    {
                        A = (byte)(0 | (Distance >> 8));
                        B = (byte)(Distance & 0xFF);
                        OutputBuffer[WritePosition++] = A;
                        OutputBuffer[WritePosition++] = B;

                        if (ByteCount > 0xFF + 0x12)
                        {
                            ByteCount = 0xFF + 0x12;
                        }

                        C = (byte)(ByteCount - 0x12);
                        OutputBuffer[WritePosition++] = C;
                    }
                    else
                    {
                        A = (byte)(((ByteCount - 2) << 4) | (Distance >> 8));
                        B = (byte)(Distance & 0xFF);
                        OutputBuffer[WritePosition++] = A;
                        OutputBuffer[WritePosition++] = B;
                    }
                    SourcePosition += (int)ByteCount;
                }

                ValidBitCount++;

                if (ValidBitCount == 8)
                {
                    OutputStream.WriteByte(CurrentCodeByte);
                    OutputStream.Write(OutputBuffer, 0, WritePosition);
                    OutputBufferSize += WritePosition + 1;
                    OutputStream.Position = OutputBufferSize;

                    CurrentCodeByte = 0;
                    ValidBitCount = 0;
                    WritePosition = 0;
                }
            }

            if (ValidBitCount > 0)
            {
                OutputStream.WriteByte(CurrentCodeByte);
                OutputStream.Write(OutputBuffer, 0, WritePosition);
                OutputBufferSize += WritePosition + 1;
                OutputStream.Position = OutputBufferSize;

                CurrentCodeByte = 0;
                ValidBitCount = 0;
                WritePosition = 0;
            }

            byte[] FileData = new byte[OutputStream.Length + 0x10];
            Encoding.ASCII.GetBytes("Yaz0").CopyTo(FileData, 0);
            BitConverter.GetBytes(Data.Length).Reverse().ToArray().CopyTo(FileData, 4);
            OutputStream.ToArray().CopyTo(FileData, 0x10);

            return FileData;
        }
    }
}
