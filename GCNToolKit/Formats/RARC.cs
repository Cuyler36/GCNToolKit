using GCNToolKit.Formats.Compression;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCNToolKit.Formats
{
    // RARC Info:
    // Nintendo Jsystem Memory Archive Implementation
    // Works by generate a fake archive implementation in memory.
    // Code can request "files" from the achive and they will be "loaded"
    // Several types:
    //    DVD-Archive: Loaded from DVD when file is request
    //    Memory-Archive: Stored in RAM and copied to requested buffer
    //    ARAM-Archive: Stored in ARAM and copied to requested buffer
    //    Compressed-Archive: Stored in either RAM, ARAM, or both.
    //      NOTE: Supposed to be able to decompress everything after the 0x20 byte long header, but there is a bug that causes a crash instead.
    public sealed class RARC
    {
        private const int HeaderSize = 0x40;
        private const int NodeSize = 0x10;
        private const int EntrySize = 0x14;

        public string FileName;
        public RARCHeader Header { get; internal set; }
        public Node[] Nodes { get; internal set; }

        /// <summary>
        /// Creates an RARC virtual folder from data and an archive name.
        /// </summary>
        /// <param name="data">The RARC file data</param>
        /// <param name="name">The name of the RARC archive</param>
        public RARC(byte[] data, string name)
        {
            FileName = name;
            Header = new RARCHeader(data);
            ReadNodes(data);
        }

        public sealed class RARCHeader
        {
            public char[] Identifier;
            public uint FileSize;
            public uint HeaderSize;
            public uint ArchiveInfoSize;
            public uint DataSize; // Total size of data in file
            public uint Unknown1;
            public uint CompressedDataSize; // Size of compressed data in file
            public uint Unknown5;

            // Info Struct
            public uint NodeCount;
            public uint InfoStructSize;
            public uint NumFiles;
            public uint EntryOffset;
            public uint StringTableSize;
            public uint StringTableOffset;
            public ushort NumFiles2;
            public ushort Unknown9;
            public uint Unknown10;

            public RARCHeader(byte[] Data)
            {
                if (Data.Length >= 0x40)
                {
                    string FileIdentifier = Encoding.ASCII.GetString(Data, 0, 4);
                    if (FileIdentifier == "RARC")
                    {
                        Identifier = FileIdentifier.ToCharArray();
                        FileSize = BitConverter.ToUInt32(Data, 4).Reverse();
                        HeaderSize = BitConverter.ToUInt32(Data, 8).Reverse();
                        ArchiveInfoSize = BitConverter.ToUInt32(Data, 0xC).Reverse() + 0x20;
                        DataSize = BitConverter.ToUInt32(Data, 0x10).Reverse();
                        Unknown1 = BitConverter.ToUInt32(Data, 0x14).Reverse();
                        CompressedDataSize = BitConverter.ToUInt32(Data, 0x18).Reverse();
                        Unknown5 = BitConverter.ToUInt32(Data, 0x1C).Reverse();
                        NodeCount = BitConverter.ToUInt32(Data, 0x20).Reverse();
                        InfoStructSize = BitConverter.ToUInt32(Data, 0x24).Reverse();
                        NumFiles = BitConverter.ToUInt32(Data, 0x28).Reverse();
                        EntryOffset = BitConverter.ToUInt32(Data, 0x2C).Reverse() + 0x20;
                        StringTableSize = BitConverter.ToUInt32(Data, 0x30).Reverse();
                        StringTableOffset = BitConverter.ToUInt32(Data, 0x34).Reverse() + 0x20;
                        NumFiles2 = BitConverter.ToUInt16(Data, 0x38).Reverse();
                        Unknown9 = BitConverter.ToUInt16(Data, 0x3A).Reverse();
                        Unknown10 = BitConverter.ToUInt32(Data, 0x3C).Reverse();
                    }
                }
            }
        }

        public sealed class Node
        {
            public string Type { get; internal set; }
            public uint NameOffset { get; internal set; }
            public string Name { get; internal set; }
            public ushort NameHash { get; internal set; }
            public ushort EntryCount { get; internal set; }
            public uint FirstFileOffset { get; internal set; }
            public Entry[] Entries { get; internal set; }

            public override string ToString()
            {
                return Name;
            }
        }

        public sealed class Entry
        {
            /// <summary>File ID. If 0xFFFF, then this entry is a subdirectory link.</summary>
            public ushort ID { get; internal set; }
            /// <summary>String hash of the <see cref="Name"/> field.</summary>
            public ushort NameHash { get; internal set; }
            /// <summary>Type of entry. 0x2 = Directory, 0x11 = File.</summary>
            public byte Type { get; internal set; }
            /// <summary>Padding byte. Included here for the sake of documentation. </summary>
            public byte Padding { get; internal set; }
            public ushort NameOffset { get; internal set; }
            /// <summary>File/subdirectory name.</summary>
            public string Name { get; internal set; }
            /// <summary>Data bytes. If this entry is a directory, it will be the node index.</summary>
            public byte[] Data { get; internal set; }
            /// <summary>Always zero.</summary>
            public uint ZeroPadding { get; internal set; }

            // Non actual struct items

            /// <summary>Node index representing the subdirectory. Will only be non-zero if IsDirectory is true.</summary>
            public uint SubDirIndex { get; internal set; }

            // True if the data resides in area that is marked compressed
            public bool IsMarkedCompressed { get; internal set; }

            /// <summary>Whether or not this entry is a directory.</summary>
            public bool IsDirectory()
                => ID == 0xFFFF || Type == 0x02;

            public bool IsFile()
                => Type == 0x11;

            public override string ToString()
            {
                return Name;
            }
        }

        internal void ReadNodes(byte[] Data)
        {
            Nodes = new Node[Header.NodeCount];
            for (int i = 0; i < Header.NodeCount; i++)
            {
                int Offset = HeaderSize + NodeSize * i;
                Nodes[i] = new Node
                {
                    Type = Encoding.ASCII.GetString(Data, Offset, 4),
                    NameOffset = BitConverter.ToUInt32(Data, Offset + 4).Reverse(),
                    NameHash = BitConverter.ToUInt16(Data, Offset + 8).Reverse(),
                    EntryCount = BitConverter.ToUInt16(Data, Offset + 0xA).Reverse(),
                    FirstFileOffset = BitConverter.ToUInt32(Data, Offset + 0xC).Reverse()
                };

                Nodes[i].Name = ReadStringTableString(Data, Nodes[i].NameOffset);
                Nodes[i].Entries = new Entry[Nodes[i].EntryCount];
            }

            foreach (Node node in Nodes)
            {
                for (int i = 0; i < node.EntryCount; i++)
                {
                    int EntryOffset = (int)(Header.EntryOffset + ((node.FirstFileOffset + i) * EntrySize));
                    node.Entries[i] = new Entry
                    {
                        ID = BitConverter.ToUInt16(Data, EntryOffset).Reverse(),
                        NameHash = BitConverter.ToUInt16(Data, EntryOffset + 2).Reverse(),
                        Type = Data[EntryOffset + 4],
                        Padding = Data[EntryOffset + 5],
                        NameOffset = BitConverter.ToUInt16(Data, EntryOffset + 6).Reverse(),
                        ZeroPadding = BitConverter.ToUInt32(Data, EntryOffset + 0x10).Reverse()
                    };

                    node.Entries[i].Name = ReadStringTableString(Data, node.Entries[i].NameOffset);
                    uint EntryDataOffset = BitConverter.ToUInt32(Data, EntryOffset + 8).Reverse();
                    uint DataSize = BitConverter.ToUInt32(Data, EntryOffset + 0xC).Reverse();

                    if (node.Entries[i].IsDirectory())
                    {
                        node.Entries[i].SubDirIndex = EntryDataOffset;
                    }
                    else
                    {
                        node.Entries[i].Data = Data.Skip((int)(Header.ArchiveInfoSize + EntryDataOffset)).Take((int)DataSize).ToArray();
                        node.Entries[i].IsMarkedCompressed = EntryDataOffset < Header.CompressedDataSize;
                    }
                }
            }
        }

        public void Dump(string rootPath, bool decompressFiles = true)
        {
            if (!Directory.Exists(rootPath))
            {
                throw new ArgumentException("The RootPath must be an existing folder!");
            }

            string path = rootPath + "\\" + FileName;
            Directory.CreateDirectory(path);
            foreach (Node n in Nodes)
            {
                DumpNode(path, n, decompressFiles);
                path += "\\" + n.Name;
            }
        }

        internal void DumpNode(string rootPath, Node currentNode, bool decompressFiles)
        {
            if (!Directory.Exists(rootPath))
            {
                throw new ArgumentException("The RootPath must be an existing folder!");
            }

            string path = rootPath + "\\" + currentNode.Name;

            Directory.CreateDirectory(rootPath + "\\" + currentNode.Name);
            foreach (Entry FileEntry in currentNode.Entries)
            {
                if (FileEntry.IsDirectory())
                {
                    if (FileEntry.Name != "." && FileEntry.Name != "..")
                    {
                        path = path + "\\" + FileEntry.Name;
                    }
                }
                else
                {
                    var data = FileEntry.Data;
                    if (decompressFiles && FileEntry.IsMarkedCompressed)
                    {
                        if (Yaz0.IsYaz0(data))
                        {
                            data = Yaz0.Decompress(data);
                        }
                        else if (Yay0.IsYay0(data))
                        {
                            data = Yay0.Decompress(data);
                        }
                    }
                    using (var Stream = File.Create(path + "\\" + FileEntry.Name))
                    {
                        Stream.Write(data, 0, data.Length);
                        Stream.Flush();
                        Stream.Close();
                    }
                }
            }
        }

        internal string ReadStringTableString(byte[] Data, uint StringOffset)
        {
            List<byte> StringBytes = new List<byte>();
            for (uint i = Header.StringTableOffset + StringOffset; i < Data.Length; i++)
            {
                if (Data[i] == 0)
                {
                    break;
                }
                StringBytes.Add(Data[i]);
            }

            return Encoding.ASCII.GetString(StringBytes.ToArray());
        }
    }
}
