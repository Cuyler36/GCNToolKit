using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCNToolKit.Formats
{
    public class RARC
    {
        private const int HeaderSize = 0x40;
        private const int NodeSize = 0x10;
        private const int EntrySize = 0x14;

        public string FileName;
        public RARCHeader Header { get; internal set; }
        public Node[] Nodes { get; internal set; }

        public RARC(byte[] Data, string Name)
        {
            FileName = Name;
            Header = new RARCHeader(Data);
            ReadNodes(Data);
        }

        public sealed class RARCHeader
        {
            public char[] Identifier;
            public uint FileSize;
            public uint Unknown1;
            public uint DataOffset;
            public uint Unknown2;
            public uint Unknown3;
            public uint Unknown4;
            public uint Unknown5;
            public uint NodeCount;
            public uint Unknown6;
            public uint Unknown7;
            public uint EntryOffset;
            public uint Unknown8;
            public uint StringTableOffset;
            public uint Unknown9;
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
                        Unknown1 = BitConverter.ToUInt32(Data, 8).Reverse();
                        DataOffset = BitConverter.ToUInt32(Data, 0xC).Reverse() + 0x20;
                        Unknown2 = BitConverter.ToUInt32(Data, 0x10).Reverse();
                        Unknown3 = BitConverter.ToUInt32(Data, 0x14).Reverse();
                        Unknown4 = BitConverter.ToUInt32(Data, 0x18).Reverse();
                        Unknown5 = BitConverter.ToUInt32(Data, 0x1C).Reverse();
                        NodeCount = BitConverter.ToUInt32(Data, 0x20).Reverse();
                        Unknown6 = BitConverter.ToUInt32(Data, 0x24).Reverse();
                        Unknown7 = BitConverter.ToUInt32(Data, 0x28).Reverse();
                        EntryOffset = BitConverter.ToUInt32(Data, 0x2C).Reverse() + 0x20;
                        Unknown8 = BitConverter.ToUInt32(Data, 0x30).Reverse();
                        StringTableOffset = BitConverter.ToUInt32(Data, 0x34).Reverse() + 0x20;
                        Unknown9 = BitConverter.ToUInt32(Data, 0x38).Reverse();
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
                        node.Entries[i].Data = Data.Skip((int)(Header.DataOffset + EntryDataOffset)).Take((int)DataSize).ToArray();
                    }
                }
            }
        }

        public void Dump(string RootPath)
        {
            if (!Directory.Exists(RootPath))
            {
                throw new ArgumentException("The RootPath must be an existing folder!");
            }

            string Path = RootPath + "\\" + FileName;
            Directory.CreateDirectory(Path);
            foreach (Node n in Nodes)
            {
                DumpNode(Path, n);
                Path += "\\" + n.Name; // Not sure about this
            }
        }

        internal void DumpNode(string RootPath, Node CurrentNode)
        {
            if (!Directory.Exists(RootPath))
            {
                throw new ArgumentException("The RootPath must be an existing folder!");
            }

            string Path = RootPath + "\\" + CurrentNode.Name;

            Directory.CreateDirectory(RootPath + "\\" + CurrentNode.Name);
            foreach (Entry FileEntry in CurrentNode.Entries)
            {
                if (FileEntry.IsDirectory())
                {
                    if (FileEntry.Name != "." && FileEntry.Name != "..")
                    {
                        Path = Path + "\\" + FileEntry.Name;
                        Directory.CreateDirectory(Path);
                    }
                }
                else
                {
                    using (var Stream = File.Create(Path + "\\" + FileEntry.Name))
                    {
                        Stream.Write(FileEntry.Data, 0, FileEntry.Data.Length);
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

            return Encoding.GetEncoding("shift_jis").GetString(StringBytes.ToArray());
        }
    }
}
