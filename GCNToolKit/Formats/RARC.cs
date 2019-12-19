using BinaryX;
using GCNToolKit.Formats.Compression;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
    
    public sealed class RARC
    {
        private const int HeaderSize = 0x40;
        private const int NodeSize = 0x10;
        private const int EntrySize = 0x14;
        private const int RARC_MAGIC_BIN = 0x52415243;
        private const string RARC_MAGIC = "RARC";

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

            if (Yay0.IsYay0(data))
            {
                data = Yay0.Decompress(data);
            }
            else if (Yaz0.IsYaz0(data))
            {
                data = Yaz0.Decompress(data);
            }

            Header = new RARCHeader(data);
            ReadNodes(data);
        }

        /// <summary>
        /// Creates an RARC archive from the given directory and settings.
        /// </summary>
        /// <param name="directory">The directory to be converted to an RARC archive.</param>
        /// <param name="archiveType">The type of archive this will be treated as.</param>
        /// <param name="compressionType">The compression type of the final archive, if one is desired.</param>
        public RARC(string directory, ArchiveType archiveType = ArchiveType.MemoryArchive, CompressionType compressionType = CompressionType.None)
        {
            if (!Directory.Exists(directory))
                throw new ArgumentException("Constructor expects a valid directory!", nameof(directory));

            var archiveName = $"{Path.GetFileName(directory)}.arc";
            if (compressionType != CompressionType.None)
                archiveName += compressionType == CompressionType.SZP ? ".szp" : ".szs";
            var archiveFileName = Path.Combine(Path.GetDirectoryName(directory), archiveName);
            using var writer = new BinaryWriterX(File.Create(archiveFileName), ByteOrder.BigEndian);
            Header = new RARCHeader();
            CreateArchive(writer, directory, archiveType, compressionType);
            writer.Close();

            // Compress the archive if requested
            if (compressionType != CompressionType.None)
            {
                var fileData = File.ReadAllBytes(archiveFileName);
                switch (compressionType)
                {
                    case CompressionType.SZP:
                        var szpData = Yay0.Compress(fileData);
                        if (szpData.Length < fileData.Length)
                            File.WriteAllBytes(archiveFileName, szpData);
                        break;
                    case CompressionType.SZS:
                        var szsData = Yaz0.Compress(fileData);
                        if (szsData.Length < fileData.Length)
                            File.WriteAllBytes(archiveFileName, szsData);
                        break;
                }
            }
        }

        public sealed class RARCHeader
        {
            public string Identifier;
            public uint FileSize;
            public uint HeaderSize;
            public uint ArchiveInfoSize;
            public uint DataSize; // Total size of data in file
            public uint Unknown1;
            public uint AramDataSize; // Size of ARAM-destined data in file
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

            public RARCHeader()
            {
                Identifier = RARC_MAGIC;
            }

            public RARCHeader(in byte[] data)
            {
                if (data.Length >= 0x40)
                {
                    string FileIdentifier = Encoding.ASCII.GetString(data, 0, 4);
                    if (FileIdentifier == RARC_MAGIC)
                    {
                        Identifier = FileIdentifier;
                        FileSize = BitConverter.ToUInt32(data, 4).Reverse();
                        HeaderSize = BitConverter.ToUInt32(data, 8).Reverse();
                        ArchiveInfoSize = BitConverter.ToUInt32(data, 0xC).Reverse() + 0x20;
                        DataSize = BitConverter.ToUInt32(data, 0x10).Reverse();
                        Unknown1 = BitConverter.ToUInt32(data, 0x14).Reverse();
                        AramDataSize = BitConverter.ToUInt32(data, 0x18).Reverse();
                        Unknown5 = BitConverter.ToUInt32(data, 0x1C).Reverse();
                        NodeCount = BitConverter.ToUInt32(data, 0x20).Reverse();
                        InfoStructSize = BitConverter.ToUInt32(data, 0x24).Reverse();
                        NumFiles = BitConverter.ToUInt32(data, 0x28).Reverse();
                        EntryOffset = BitConverter.ToUInt32(data, 0x2C).Reverse() + 0x20;
                        StringTableSize = BitConverter.ToUInt32(data, 0x30).Reverse();
                        StringTableOffset = BitConverter.ToUInt32(data, 0x34).Reverse() + 0x20;
                        NumFiles2 = BitConverter.ToUInt16(data, 0x38).Reverse();
                        Unknown9 = BitConverter.ToUInt16(data, 0x3A).Reverse();
                        Unknown10 = BitConverter.ToUInt32(data, 0x3C).Reverse();
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

            public Node ParentNode { get; internal set; }

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
            public uint NameOffset { get; internal set; }
            /// <summary>File/subdirectory name.</summary>
            public string Name { get; internal set; }
            public uint DataOffset { get; internal set; }
            public uint Size { get; internal set; }
            /// <summary>Data bytes. If this entry is a directory, it will be the node index.</summary>
            public byte[] Data { get; internal set; }
            /// <summary>Always zero.</summary>
            public uint MemoryPointer { get; internal set; }

            // Non actual struct items

            /// <summary>Node index representing the subdirectory. Will only be non-zero if IsDirectory is true.</summary>
            public uint SubDirIndex { get; internal set; }

            // True if the data resides in area that is marked compressed
            public bool IsMarkedCompressed { get; internal set; }

            public bool IsFile()
                => (Type & 1) == 1;

            public bool IsCompressedFile()
                => (Type & 4) != 0;

            /// <summary>Whether or not this entry is a directory.</summary>
            public bool IsDirectory()
                => ID == 0xFFFF || (Type & 2) != 0;

            public bool FetchFromRAM() => (Type & 0x10) != 0;

            public bool FetchFromARAM() => (Type & 0x20) != 0;

            public bool FetchFromDVD() => (Type & 0x40) != 0;

            public bool IsSZSCompressed() => IsCompressedFile() && (Type & 0x80) != 0;

            public bool IsSZPCompressed() => IsCompressedFile() && (Type & 0x80) == 0;

            public override string ToString()
            {
                return Name;
            }
        }

        internal void ReadNodes(in byte[] data)
        {
            Nodes = new Node[Header.NodeCount];
            for (int i = 0; i < Header.NodeCount; i++)
            {
                int Offset = HeaderSize + NodeSize * i;
                Nodes[i] = new Node
                {
                    Type = Encoding.ASCII.GetString(data, Offset, 4),
                    NameOffset = BitConverter.ToUInt32(data, Offset + 4).Reverse(),
                    NameHash = BitConverter.ToUInt16(data, Offset + 8).Reverse(),
                    EntryCount = BitConverter.ToUInt16(data, Offset + 0xA).Reverse(),
                    FirstFileOffset = BitConverter.ToUInt32(data, Offset + 0xC).Reverse()
                };

                Nodes[i].Name = ReadStringTableString(data, Nodes[i].NameOffset);
                Nodes[i].Entries = new Entry[Nodes[i].EntryCount];
            }

            foreach (Node node in Nodes)
            {
                for (int i = 0; i < node.EntryCount; i++)
                {
                    int EntryOffset = (int)(Header.EntryOffset + ((node.FirstFileOffset + i) * EntrySize));
                    node.Entries[i] = new Entry
                    {
                        ID = BitConverter.ToUInt16(data, EntryOffset).Reverse(),
                        NameHash = BitConverter.ToUInt16(data, EntryOffset + 2).Reverse(),
                        Type = data[EntryOffset + 4],
                        NameOffset = BitConverter.ToUInt32(data, EntryOffset + 4).Reverse() & 0x00FFFFFF,
                        DataOffset = BitConverter.ToUInt32(data, EntryOffset + 8).Reverse(),
                        Size = BitConverter.ToUInt32(data, EntryOffset + 0xC).Reverse(),
                        MemoryPointer = BitConverter.ToUInt32(data, EntryOffset + 0x10).Reverse()
                    };

                    node.Entries[i].Name = ReadStringTableString(data, node.Entries[i].NameOffset);
                    uint EntryDataOffset = node.Entries[i].DataOffset;
                    uint DataSize = node.Entries[i].Size;

                    if (node.Entries[i].IsDirectory())
                    {
                        node.Entries[i].SubDirIndex = EntryDataOffset;
                    }
                    else
                    {
                        node.Entries[i].Data = new byte[DataSize];
                        Buffer.BlockCopy(data, (int)(Header.ArchiveInfoSize + EntryDataOffset), node.Entries[i].Data, 0, (int)DataSize);
                        node.Entries[i].IsMarkedCompressed = EntryDataOffset < Header.AramDataSize;
                    }
                }
            }
        }

        public void Dump(string rootPath, bool decompressFiles = true)
        {
            if (!Directory.Exists(rootPath))
            {
                throw new ArgumentException("The root path must be an existing folder!", nameof(rootPath));
            }

            var path = Path.Combine(rootPath, FileName + "_dir");
            Directory.CreateDirectory(path);
            DumpNode(path, Nodes[0], decompressFiles);
        }

        internal void DumpNode(string path, Node currentNode, bool decompressFiles)
        {
            path = Path.Combine(path, currentNode.Name);
            Directory.CreateDirectory(path);
            foreach (Entry FileEntry in currentNode.Entries)
            {
                if (FileEntry.IsDirectory())
                {
                    if (FileEntry.Name != "." && FileEntry.Name != "..")
                    {
                        DumpNode(path, Nodes[FileEntry.DataOffset], decompressFiles);
                    }
                }
                else
                {
                    var data = FileEntry.Data;
                    if (decompressFiles && FileEntry.IsMarkedCompressed)
                    {
                        if (FileEntry.IsCompressedFile())
                        {
                            if (FileEntry.IsSZPCompressed() && Yay0.IsYay0(data))
                            {
                                data = Yay0.Decompress(data);
                            }
                            else if (FileEntry.IsSZSCompressed() && Yaz0.IsYaz0(data))
                            {
                                data = Yaz0.Decompress(data);
                            }
                        }
                    }

                    using var stream = File.Create(Path.Combine(path, FileEntry.Name));
                    stream.Write(data, 0, data.Length);
                    stream.Flush();
                    stream.Close();
                }
            }
        }

        internal string ReadStringTableString(in byte[] data, uint stringOffset)
        {
            var size = 0;
            for (var i = Header.StringTableOffset + stringOffset; i < data.Length; i++, size++)
            {
                if (data[i] == 0) break;
            }

            return Encoding.ASCII.GetString(data, (int)(Header.StringTableOffset + stringOffset), size);
        }

        private void CreateArchive(BinaryWriterX writer, string rootDir, ArchiveType arcType, CompressionType compressionType)
        {
            var stringTable = new List<byte[]>();
            var nodes = new List<Node>();
            var entries = new List<Entry>();
            var rootName = Path.GetFileName(rootDir);
            var rootNode = new Node
            {
                Type = "ROOT",
                Name = rootName,
                NameHash = GetNameHash(rootName)
            };
            nodes.Add(rootNode);
            Header.NodeCount = 1;

            using var strTableStream = new MemoryStream();
            using var strTable = new BinaryWriter(strTableStream);
            ConvertDirectory(rootNode, nodes, entries, strTable, arcType, rootDir, 0);

            var entrySize = (uint)entries.Count * 0x14u;
            Align(ref entrySize, 32);
            Header.StringTableOffset = 0x20 + Header.NodeCount * NodeSize + entrySize;
            Align(ref Header.StringTableOffset, 32);
            Header.StringTableSize = (uint)strTableStream.Length;
            Align(ref Header.StringTableSize, 32);

            using var dataStream = new MemoryStream();
            using var dataWriter = new BinaryWriter(dataStream);

            // Get a copy of the list without sorting
            List<Entry> unsortedEntries = new List<Entry>(entries.Count);
            foreach (var entry in entries)
            {
                unsortedEntries.Add(entry);
            }

            entries.Sort((a, b) => a.IsCompressedFile() && !b.IsCompressedFile() ? -1 : (!a.IsCompressedFile() && b.IsCompressedFile()) ? 1 : 0);

            var compSize = 0u;
            var totSize = 0u;
            foreach (var entry in entries)
            {
                if (entry.IsDirectory()) continue;
                var size = (uint)entry.Data.Length;
                entry.DataOffset = (uint)dataStream.Length;
                dataWriter.Write(entry.Data);
                if ((entry.Data.Length & 0x1F) != 0)
                {
                    size = (size + 0x1F) & ~0x1Fu;
                    dataWriter.Write(new byte[32 - (entry.Data.Length & 0x1F)]);
                }
                if (entry.IsCompressedFile())
                {
                    compSize += size;
                }
                totSize += size;
            }
            Header.NumFiles = Header.NumFiles2 = (ushort)entries.Count;
            Header.DataSize = totSize;
            if (arcType == ArchiveType.Compressed)
            {
                Header.AramDataSize = compSize;
            }
            else if (arcType == ArchiveType.AramArchive)
            {
                Header.AramDataSize = totSize;
            }

            Header.EntryOffset = 0x20 + (Header.NodeCount & 1) != 0 ? (Header.NodeCount + 1) * NodeSize : Header.NodeCount * NodeSize;
            Header.ArchiveInfoSize = Header.StringTableOffset + Header.StringTableSize;
            Header.FileSize = 0x20 + Header.ArchiveInfoSize + Header.DataSize;

            // Write header
            writer.Write(RARC_MAGIC_BIN);
            writer.Write(Header.FileSize);
            writer.Write(0x20u); // Header Size
            writer.Write(Header.ArchiveInfoSize);
            writer.Write(Header.DataSize);
            writer.Write(0u); // Unknown 1
            writer.Write(Header.AramDataSize);
            writer.Write(0u); // Unknown 5

            writer.Write(Header.NodeCount);
            writer.Write(0x20);
            writer.Write(Header.NumFiles);
            writer.Write(Header.EntryOffset);
            writer.Write(Header.StringTableSize);
            writer.Write(Header.StringTableOffset);
            writer.Write(Header.NumFiles2);
            writer.Write(Header.Unknown9);
            writer.Write(0u); // Unknown 10

            foreach (var node in nodes)
            {
                writer.Write(Encoding.ASCII.GetBytes(node.Type));
                writer.Write(node.NameOffset);
                writer.Write(node.NameHash);
                writer.Write(node.EntryCount);
                writer.Write(node.FirstFileOffset / EntrySize);
            }
            if ((Header.NodeCount & 1) != 0)
            {
                writer.Write(new byte[16]);
            }

            foreach (var entry in unsortedEntries)
            {
                WriteEntryInfo(writer, entry);
            }
            
            if (writer.BaseStream.Length % 32 != 0)
            {
                writer.Write(new byte[32 - writer.BaseStream.Length % 32]);
            }
            writer.Write(strTableStream.ToArray());
            if (writer.BaseStream.Length % 32 != 0)
            {
                writer.Write(new byte[32 - writer.BaseStream.Length % 32]);
            }
            writer.Write(dataStream.ToArray());
        }

        private void ConvertDirectory(Node parentNode, List<Node> nodes, List<Entry> entries, BinaryWriter strTable, ArchiveType arcType, string dirPath, ushort entryId)
        {
            var files = Directory.GetFiles(dirPath);
            var dirs = Directory.GetDirectories(dirPath);
            parentNode.EntryCount = (ushort)(files.Length + dirs.Length + 2);
            parentNode.Entries = new Entry[parentNode.EntryCount];

            var nodeProcessQueue = new List<Node>();
            var nodeFileEntries = new List<Entry>();

            if (nodes.Count == 1)
            {
                AddNameToStringTableList(".", strTable);
                AddNameToStringTableList("..", strTable);
            }

            if (parentNode.Type == "ROOT")
            {
                // Write the root node's name to the string table
                parentNode.NameOffset = (uint)strTable.BaseStream.Length;
                AddNameToStringTableList(Path.GetFileName(dirPath), strTable);
            }

            var entryIdx = 0;
            // Load files first
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var nameOffset = (uint)strTable.BaseStream.Length;
                AddNameToStringTableList(fileName, strTable);
                var entry = new Entry
                {
                    ID = entryId,
                    Data = File.ReadAllBytes(file),
                    Name = fileName,
                    NameHash = GetNameHash(fileName),
                    NameOffset = nameOffset
                };
                if (fileName.EndsWith(".szp") && !Yay0.IsYay0(entry.Data))
                {
                    var data = Yay0.Compress(entry.Data);
                    if (data.Length < entry.Data.Length)
                    {
                        entry.Data = data;
                    }
                    Header.AramDataSize += (uint)entry.Data.Length;
                }
                else if (fileName.EndsWith(".szs") && !Yaz0.IsYaz0(entry.Data))
                {
                    var data = Yaz0.Compress(entry.Data);
                    if (data.Length < entry.Data.Length)
                    {
                        entry.Data = data;
                    }
                    Header.AramDataSize += (uint)entry.Data.Length;
                }
                entry.Size = (uint)entry.Data.Length;

                entryId++;
                Header.NumFiles++;
                Header.NumFiles2++;
                Header.DataSize += (uint)entry.Data.Length;
                parentNode.Entries[entryIdx++] = entry;
                SetType(entry, arcType, true);
                entries.Add(entry);
            }

            foreach (var dir in dirs)
            {
                var name = Path.GetFileName(dir);
                var nodeType = name.ToUpper();
                if (nodeType.Length > 4)
                {
                    nodeType = nodeType.Substring(0, 4);
                }
                else if (nodeType.Length < 4)
                {
                    nodeType = nodeType.PadRight(4);
                }

                var subNode = new Node
                {
                    Name = name,
                    NameHash = GetNameHash(name),
                    Type = nodeType,
                    ParentNode = parentNode
                };

                var entry = new Entry
                {
                    ID = 0xFFFF,
                    Name = subNode.Name,
                    NameHash = subNode.NameHash,
                    Size = 16
                };
                Header.NodeCount++;
                entries.Add(entry);
                SetType(entry, arcType, false);
                parentNode.Entries[entryIdx++] = entry;
                nodeProcessQueue.Add(subNode);
                nodeFileEntries.Add(entry);
            }

            // Create . & .. folder entries at the end of the entry list for this node
            for (var i = 0u; i < 2; i++)
            {
                var name = i == 0 ? "." : "..";
                var nameOffset = i * 2;
                var entry = new Entry
                {
                    ID = 0xFFFF,
                    Name = name,
                    NameHash = GetNameHash(name),
                    DataOffset = (uint)(nodes.Count - (i + 1)),
                    NameOffset = nameOffset,
                    Size = 16
                };

                SetType(entry, arcType, false);
                parentNode.Entries[parentNode.EntryCount - (2 - i)] = entry;
                entries.Add(entry);
            }

            var subNodeIdx = 0;
            foreach (var dir in dirs)
            {
                // Process subnode
                var entry = nodeFileEntries[subNodeIdx];
                var node = nodeProcessQueue[subNodeIdx++];
                node.FirstFileOffset = (ushort)(entries.Count * 0x14);
                nodes.Add(node);
                entry.DataOffset = (uint)(nodes.Count - 1);
                entry.NameOffset = node.NameOffset = (uint)strTable.BaseStream.Length;
                AddNameToStringTableList(Path.GetFileName(dir), strTable);
                ConvertDirectory(node, nodes, entries, strTable, arcType, dir, entryId);
            }
        }

        private static ushort GetNameHash(in string name)
        {
            ushort hash = 0;
            foreach (var character in Encoding.ASCII.GetBytes(name))
            {
                if (character == 0) break;
                hash = (ushort)(character + hash * 3);
            }
            return hash;
        }

        private void WriteEntryInfo(BinaryWriterX writer, Entry entry)
        {
            writer.Write(entry.ID);
            writer.Write(entry.NameHash);
            writer.Write((uint)(entry.Type << 24) | (entry.NameOffset & 0xFFFFFF));
            writer.Write(entry.DataOffset);
            writer.Write(entry.Size);
            writer.Write(entry.MemoryPointer);
        }

        private void AddNameToStringTableList(string name, BinaryWriter stringTable) => stringTable.Write(Encoding.ASCII.GetBytes(name + "\0"));

        private static bool Align(ref uint offset, uint alignment)
        {
            if (offset % alignment != 0)
            {
                offset = (offset + (alignment - 1)) & ~(alignment - 1);
                return true;
            }
            return false;
        }

        private void SetType(Entry entry, ArchiveType arcType, bool isFile)
        {
            byte type = 0;
            if (isFile)
            {
                var isSZP = Yay0.IsYay0(entry.Data);
                var isSZS = !isSZP && Yaz0.IsYaz0(entry.Data);
                if (isSZP || isSZS)
                {
                    type |= 4;
                    if (isSZS)
                    {
                        type |= 0x80;
                    }
                }

                type |= 1;
                if (arcType == ArchiveType.Compressed)
                {
                    if ((type & 4) != 0)
                    {
                        type |= 0x10 << (int)ArchiveType.AramArchive;
                    }
                    else
                    {
                        type |= 0x10 << (int)ArchiveType.DvdArchive;
                    }
                }
                else
                {
                    type |= (byte)(0x10 << (int)arcType);
                }
            }
            else
            {
                type |= 2;
            }

            entry.Type = type;
        }
    }
}
