using System.IO;
using System.Text;

namespace FAT12
{
    public class ExtendedBiosParameterBlock
    {
        public byte PhysicalDiskNumber;
        public byte CurrentHead;
        public byte Signature;
        public uint VolumeSerialNumber;
        public string VolumeLabel;
        public string SystemID;

        public ExtendedBiosParameterBlock(Stream S)
        {
            using (var BR = new BinaryReader(S, Encoding.Default, true))
            {
                PhysicalDiskNumber = BR.ReadByte();
                CurrentHead = BR.ReadByte();
                Signature = BR.ReadByte();
                VolumeSerialNumber = BR.ReadUInt32();
                VolumeLabel = Encoding.Default.GetString(BR.ReadBytes(11)).TrimEnd();
                SystemID = Encoding.Default.GetString(BR.ReadBytes(8)).TrimEnd();
            }
        }
    }
}
