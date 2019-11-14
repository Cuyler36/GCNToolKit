using System;
using System.IO;
using System.Linq;
using System.Text;

namespace GCNToolKit.Formats.Compression
{
    public static class Yay0
    {
        public static bool IsYay0(in byte[] data) => data?.Length > 0x10 && Encoding.ASCII.GetString(data, 0, 4) == "Yay0";

        /// <summary>
        /// Compresses a given byte array using SZP compression.
        /// </summary>
        /// <param name="data">The byte array to compress</param>
        /// <returns>compressedData</returns>
        public static byte[] Compress(in byte[] data)
        {
            const int OFSBITS = 12;
            int decPtr;

            // masks buffer
            uint maskMaxSize = (uint)(data.Length + 32) >> 3; // 1 bit per byte
            uint maskBitCount = 0, mask = 0;
            uint[] maskBuffer = new uint[maskMaxSize / 4];
            int maskPtr;

            // links buffer
            uint linkMaxSize = (uint)data.Length / 2;
            ushort linkOffset = 0;
            ushort[] linkBuffer = new ushort[linkMaxSize];
            int linkPtr;
            ushort minCount = 3, maxCount = 273;

            // chunks buffer
            uint chunkMaxSize = (uint)data.Length;
            byte[] chunkBuffer = new byte[chunkMaxSize];
            int chunkPtr;

            int windowPtr;
            int windowLen = 0, length, maxlen;

            //set pointers
            decPtr = 0;
            maskPtr = 0;
            linkPtr = 0;

            chunkPtr = 0;
            windowPtr = decPtr;

            // start enconding
            while (decPtr < data.Length)
            {

                if (windowLen >= (1 << OFSBITS))
                {
                    windowLen = windowLen - (1 << OFSBITS);
                    windowPtr = decPtr - windowLen;
                }

                if ((data.Length - decPtr) < maxCount)
                    maxCount = (ushort)(data.Length - decPtr);

                // Scan through the window.
                maxlen = 0;
                for (int i = 0; i < windowLen; i++)
                {
                    for (length = 0; length < (windowLen - i) && length < maxCount; length++)
                    {
                        if (data[decPtr + length] != data[windowPtr + length + i]) break;
                    }
                    if (length > maxlen)
                    {
                        maxlen = length;
                        linkOffset = (ushort)(windowLen - i);
                    }
                }
                length = maxlen;

                mask <<= 1;
                if (length >= minCount)      // Add Link
                {
                    ushort link = (ushort)((linkOffset - 1) & 0x0FFF);

                    if (length < 18)
                    {
                        link |= (ushort)((length - 2) << 12);
                    }
                    else
                    {
                        // store current count as a chunk.
                        chunkBuffer[chunkPtr++] = (byte)(length - 18);
                    }

                    linkBuffer[linkPtr++] = link.Reverse();
                    decPtr += length;
                    windowLen += length;
                }
                else                        // Add single byte, increase Window.
                {
                    chunkBuffer[chunkPtr++] = data[decPtr++];
                    windowLen++;
                    mask |= 1;
                }

                maskBitCount++;
                if (maskBitCount == 32)
                {
                    // store current mask
                    maskBuffer[maskPtr] = mask.Reverse();
                    maskPtr++;
                    maskBitCount = 0;
                }
            }

            //flush mask 
            if (maskBitCount > 0)
            {
                mask <<= (int)(32 - maskBitCount);
                // store current mask
                maskBuffer[maskPtr] = mask.Reverse();
                maskPtr++;
            }

            // now join all pieces
            uint maskSize = (uint)maskPtr * sizeof(uint);
            uint linkSize = (uint)linkPtr * sizeof(ushort);
            uint chunkSize = (uint)chunkPtr * sizeof(byte);

            uint encodedSize = 0x10 + maskSize + linkSize + chunkSize;
            byte[] buffer = new byte[encodedSize];
            using (var writer = new BinaryWriter(new MemoryStream(buffer)))
            {
                uint hdrLinkOffset = 0x10 + maskSize;
                uint hdrChunkOffset = hdrLinkOffset + linkSize;
                // Write header
                writer.Write(Encoding.ASCII.GetBytes("Yay0"));
                writer.Write(((uint)data.Length).Reverse());
                writer.Write(hdrLinkOffset.Reverse());
                writer.Write(hdrChunkOffset.Reverse());

                // Write data
                Buffer.BlockCopy(maskBuffer, 0, buffer, 0x10, (int)maskSize);
                Buffer.BlockCopy(linkBuffer, 0, buffer, (int)hdrLinkOffset, (int)linkSize);
                Buffer.BlockCopy(chunkBuffer, 0, buffer, (int)hdrChunkOffset, (int)chunkSize);
            }

            return buffer;
        }

        /// <summary>
        /// Decompresses a given byte buffer if it is compressed with SZP compression
        /// </summary>
        /// <param name="compressedData">The SZP compressed byte buffer</param>
        /// <returns>decompressedData</returns>
        public static byte[] Decompress(in byte[] compressedData)
        {
            if (IsYay0(compressedData))
            {
                uint DecompressedSize = BitConverter.ToUInt32(compressedData, 4).Reverse();
                uint CountOffset = BitConverter.ToUInt32(compressedData, 8).Reverse();
                uint DataOffset = BitConverter.ToUInt32(compressedData, 12).Reverse();
                byte[] DecompressedFileData = new byte[DecompressedSize];

                int CodePosition = 0x10;
                int Write_Position = 0;
                uint ValidBitCount = 0;
                byte CurrentCodeByte = 0;

                while (Write_Position < DecompressedSize)
                {
                    if (ValidBitCount == 0)
                    {
                        CurrentCodeByte = compressedData[CodePosition];
                        ++CodePosition;
                        ValidBitCount = 8;
                    }

                    if ((CurrentCodeByte & 0x80) != 0)
                    {
                        DecompressedFileData[Write_Position] = compressedData[DataOffset];
                        Write_Position++;
                        DataOffset++;
                    }
                    else
                    {
                        try
                        {
                            byte Byte1 = compressedData[CountOffset];
                            byte Byte2 = compressedData[CountOffset + 1];
                            CountOffset += 2;

                            uint Dist = (uint)(((Byte1 & 0xF) << 8) | Byte2);
                            uint CopySource = (uint)(Write_Position - (Dist + 1));

                            uint Byte_Count = (uint)(Byte1 >> 4);
                            if (Byte_Count == 0)
                            {
                                Byte_Count = (uint)(compressedData[DataOffset] + 0x12);
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
                            Console.WriteLine(e.Message + "\n" + e.StackTrace);
                            return null;
                        }
                    }

                    CurrentCodeByte <<= 1;
                    ValidBitCount -= 1;
                }

                return DecompressedFileData;
            }
            else
            {
                return null;
            }
        }
    }
}
