using System;
using System.Collections.Generic;
using System.IO;
using BinaryX;

namespace GCNToolKit.Formats
{
    public sealed class RelocatableModule
    {
        public enum RelocationType : byte
        {
            R_PPC_NONE = 0,
            R_PPC_ADDR32 = 1,
            R_PPC_ADDR24 = 2,
            R_PPC_ADDR16 = 3,
            R_PPC_ADDR16_LO = 4,
            R_PPC_ADDR16_HI = 5,
            R_PPC_ADDR16_HA = 6,
            R_PPC_ADDR14 = 7,
            R_PPC_ADDR14_BRTAKEN = 8,
            R_PPC_ADDR14_BRNTAKEN = 9,
            R_PPC_REL24 = 10,
            R_PPC_REL14 = 11,
            R_PPC_REL14_BRTAKEN = 12,
            R_PPC_REL14_BRNTAKEN = 13,

            R_DOLPHIN_NOP = 201,
            R_DOLPHIN_SECTION = 202,
            R_DOLPHIN_END = 203,
            R_DOLPHIN_MRKREF = 204
        }

        public sealed class Header
        {
            public uint ModuleId;
            public uint PrevModule;
            public uint NextModule;
            public uint NumSections;

            public uint SectionInfoOffset;
            public uint NameOffset;
            public uint NameSize;
            public uint ModuleVersion;

            public uint BssSize;
            public uint RelocationTableOffset;
            public uint ImportTableOffset;
            public uint ImportTableSize;

            public byte PrologSectionId;
            public byte EpilogSectionId;
            public byte UnresolvedSectionId;
            public byte BssSectionId; // This is set at runtime
            public uint PrologFuncOffset;
            public uint EpilogFuncOffset;
            public uint UnresolvedFuncOffset;

            // Version >= 2
            public uint ModuleAlignment;
            public uint BssAlignment;

            // Version >= 3
            public uint FixSize;

            public Header() { }

            public Header(BinaryReaderX reader)
            {
                ModuleId = reader.ReadUInt32();
                PrevModule = reader.ReadUInt32(); // Should be 0 since it's a OSModule*
                NextModule = reader.ReadUInt32(); // Same as above
                NumSections = reader.ReadUInt32();

                SectionInfoOffset = reader.ReadUInt32();
                NameOffset = reader.ReadUInt32();
                NameSize = reader.ReadUInt32();
                ModuleVersion = reader.ReadUInt32();

                BssSize = reader.ReadUInt32();
                RelocationTableOffset = reader.ReadUInt32();
                ImportTableOffset = reader.ReadUInt32();
                ImportTableSize = reader.ReadUInt32();

                PrologSectionId = reader.ReadByte();
                EpilogSectionId = reader.ReadByte();
                UnresolvedSectionId = reader.ReadByte();
                BssSectionId = reader.ReadByte(); // Again, this is set at runtime
                PrologFuncOffset = reader.ReadUInt32();
                EpilogFuncOffset = reader.ReadUInt32();
                UnresolvedFuncOffset = reader.ReadUInt32();

                if (ModuleVersion >= 2)
                {
                    ModuleAlignment = reader.ReadUInt32();
                    BssAlignment = reader.ReadUInt32();
                }

                if (ModuleVersion >= 3)
                {
                    FixSize = reader.ReadUInt32();
                }
            }
        }

        public sealed class Section
        {
            public const uint SECT_EXEC = 1;
            public const uint SECT_OFFS = ~SECT_EXEC;

            public uint Offset;
            public uint Size;

            public int Id;
            public byte[] Data;

            public bool IsExecutable() => (Offset & SECT_EXEC) != 0;
            public uint GetOffset() => (Offset & SECT_OFFS);

            public Section(BinaryReaderX reader)
            {
                Offset = reader.ReadUInt32();
                Size = reader.ReadUInt32();

                // Read data
                var currentOffset = reader.Position;
                reader.Seek(GetOffset());
                Data = reader.ReadBytes((int)Size);
                reader.Seek(currentOffset);
            }
        }

        public sealed class Relocation
        {
            public ushort Offset;
            public RelocationType Type;
            public byte Section;
            public uint Addend;

            public Relocation(BinaryReaderX reader)
            {
                Offset = reader.ReadUInt16();
                Type = (RelocationType)reader.ReadByte();
                Section = reader.ReadByte();
                Addend = reader.ReadUInt32();
            }
        }

        public sealed class Import
        {
            public const int IMPORT_SIZE = 8;

            public uint ModuleId;
            public uint Offset;

            public Relocation[] Relocations;

            public Import(BinaryReaderX reader)
            {
                ModuleId = reader.ReadUInt32();
                Offset = reader.ReadUInt32();
            }
        }

        public Header ModuleHeader { get; private set; }
        public Section[] Sections;
        public Import[] Imports;

        public RelocatableModule(BinaryReaderX reader) => Load(reader);

        public RelocatableModule(string path)
        {
            if (!File.Exists(path))
                throw new ArgumentException("The given file doesn't exist!", nameof(path));

            using var reader = new BinaryReaderX(File.OpenRead(path), ByteOrder.BigEndian);
            Load(reader);
        }

        private void Load(BinaryReaderX reader)
        {
            ModuleHeader = new Header(reader);

            // Initialize Section Entries
            Sections = new Section[ModuleHeader.NumSections];
            reader.Seek(ModuleHeader.SectionInfoOffset);
            for (var i = 0; i < Sections.Length; i++)
            {
                Sections[i] = new Section(reader);
                Sections[i].Id = i;
            }

            // Initialize Imports & Relocations
            Imports = new Import[ModuleHeader.ImportTableSize / Import.IMPORT_SIZE];
            for (var i = 0; i < Imports.Length; i++)
            {
                reader.Seek(ModuleHeader.ImportTableOffset + i * Import.IMPORT_SIZE);
                Imports[i] = new Import(reader);

                // Load relocations
                var relocations = new List<Relocation>();
                reader.Seek(Imports[i].Offset);
                while (true)
                {
                    var relocation = new Relocation(reader);
                    relocations.Add(relocation);
                    if (relocation.Type == RelocationType.R_DOLPHIN_END) break;
                }
                Imports[i].Relocations = relocations.ToArray();
            }
        }
    }
}
