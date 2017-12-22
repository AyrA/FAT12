using System;
using System.IO;
using System.Linq;
using System.Text;

namespace FAT12
{
    public class FatPartition
    {
        public static byte[] DefaultBootJumpInstructions
        {
            get
            {
                return new byte[] { 0xEB, 0x3C, 0x90 };
            }
        }
        public static string DefaultOemName
        {
            get
            {
                return "MSDOS5.0";
            }
        }

        private byte[] _bootJumpInstructions;
        private string _oemName;
        private BiosParameterBlock _biosParameters;
        private ExtendedBiosParameterBlock _extendedBiosParameters;
        private byte[] _bootCode;
        private byte[] _bootSectorSignature;
        private ClusterStatus[] _clusterMap;

        public byte[] BootJumpInstructions
        {
            get
            {
                return (byte[])_bootJumpInstructions.Clone();
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("BootJumpInstructions");
                }
                if (value.Length != 3)
                {
                    throw new FormatException("BootJumpInstructions must be 3 bytes long");
                }
                _bootJumpInstructions = (byte[])value.Clone();
            }
        }
        public string OemName
        {
            get
            {
                return _oemName;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("OemName");
                }
                if (Encoding.Default.GetByteCount(value) > 8)
                {
                    throw new FormatException("OemName must not be longer than 8 ASCII characters");
                }
                _oemName = value.PadRight(8);
            }
        }
        public BiosParameterBlock BiosParameters
        {
            get
            {
                return _biosParameters;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("BiosParameters");
                }
                _biosParameters = value;
            }
        }
        public ExtendedBiosParameterBlock ExtendedBiosParameters
        {
            get
            {
                return _extendedBiosParameters;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("BiosParameters");
                }
                _extendedBiosParameters = value;
            }
        }
        public ClusterStatus[] ClusterMap
        {
            get
            {
                return (ClusterStatus[])_clusterMap.Clone();
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("ClusterMap");
                }
                if (value.Length != _clusterMap.Length)
                {
                    throw new FormatException($"Cluster map must have {_clusterMap.Length} entries");
                }
                _clusterMap = (ClusterStatus[])value.Clone();
            }
        }

        public FatPartition(Stream S)
        {
            using (var BR = new BinaryReader(S, Encoding.Default, true))
            {
                //Read the first Sector
                _bootJumpInstructions = BR.ReadBytes(3);
                _oemName = Encoding.Default.GetString(BR.ReadBytes(8)).TrimEnd();
                _biosParameters = new BiosParameterBlock(S);
                _extendedBiosParameters = new ExtendedBiosParameterBlock(S);
                _bootCode = BR.ReadBytes(448);
                _bootSectorSignature = BR.ReadBytes(2);

                //Read the FAT12 Entry
                using (var MS = new MemoryStream(BR.ReadBytes(_biosParameters.BytesPerSector * _biosParameters.SectorsPerFat)))
                {
                    using (var MR = new BinaryReader(MS))
                    {
                        _clusterMap = new ClusterStatus[_biosParameters.BytesPerSector * _biosParameters.SectorsPerFat / 3];
                        for (var i = 0; i < _clusterMap.Length; i += 2)
                        {
                            var Clusters = MR.ReadBytes(3);
                            //Swap nibbles of byte array
                            Clusters[1] = (byte)((Clusters[1] << 4) + (Clusters[1] >> 4));

                            if (i > 1500)
                            {
                                Console.Write("");
                            }

                            ushort Cluster1 = (ushort)(Clusters[0] + ((Clusters[1] >> 4) * 256));
                            ushort Cluster2 = (ushort)((Clusters[2] << 4) + (Clusters[1] & 0xF));

                            _clusterMap[i] = GetFat12Status(Cluster1);
                            _clusterMap[i + 1] = GetFat12Status(Cluster2);
                        }
                        _clusterMap[0] = _clusterMap[1] = ClusterStatus.Reserved;
                    }
                }
                //Skip all other FAT tables
                for (var i = 0; i < _biosParameters.NumberOfFatTables - 1; i++)
                {
                    BR.ReadBytes(_biosParameters.BytesPerSector * _biosParameters.SectorsPerFat);
                }
                //Root Directory
                Enumerable.Range(0, _biosParameters.NumberOfRootEntries).Select(m => new FatDirectoryEntry(BR.ReadBytes(FatReader.FAT_BYTES_PER_DIRECTORY_ENTRY))).ToArray();
            }
        }

        private ClusterStatus GetFat12Status(ushort u)
        {
            if (u == 0)
            {
                return ClusterStatus.Empty;
            }
            if (u == 1 || u==0xFF6)
            {
                return ClusterStatus.Reserved;
            }
            if (u <= 0xFEF || u==0xFF0)
            {
                return ClusterStatus.Occupied;
            }
            if (u == 0xFF7)
            {
                return ClusterStatus.Damaged;
            }
            //End of file marker otherwise
            return ClusterStatus.EOF;
        }
    }
}
