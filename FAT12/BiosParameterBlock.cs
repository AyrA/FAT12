using System.IO;
using System.Text;

namespace FAT12
{
    public class BiosParameterBlock
    {
        public ushort BytesPerSector;
        public byte SectorsPerCluster;
        public ushort ReservedSectors;
        public byte NumberOfFatTables;
        public ushort NumberOfRootEntries;
        public ushort SmallSectors;
        public byte MediaType;
        public ushort SectorsPerFat;
        public ushort SectorsPerTrack;
        public ushort NumberOfHeads;
        public uint HiddenSectors;
        public uint LargeSectors;

        public BiosParameterBlock(Stream S)
        {
            using (var BR = new BinaryReader(S, Encoding.Default, true))
            {
                BytesPerSector = BR.ReadUInt16();
                SectorsPerCluster = BR.ReadByte();
                ReservedSectors = BR.ReadUInt16();
                NumberOfFatTables = BR.ReadByte();
                NumberOfRootEntries = BR.ReadUInt16();
                SmallSectors = BR.ReadUInt16();
                MediaType = BR.ReadByte();
                SectorsPerFat = BR.ReadUInt16();
                SectorsPerTrack = BR.ReadUInt16();
                NumberOfHeads = BR.ReadUInt16();
                HiddenSectors = BR.ReadUInt32();
                LargeSectors = BR.ReadUInt32();
            }
        }
    }
}
