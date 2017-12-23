using System;
using System.Collections.Generic;
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
        private ClusterEntry[] _clusterMap;
        private FatDirectoryEntry[] _rootDirectory;

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
        public ClusterEntry[] ClusterMap
        {
            get
            {
                return (ClusterEntry[])_clusterMap.Clone();
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
                _clusterMap = (ClusterEntry[])value.Clone();
            }
        }
        public FatDirectoryEntry[] RootDirectory
        {
            get
            {
                return (FatDirectoryEntry[])_rootDirectory.Clone();
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("RootDirectory");
                }
                if (value.Length != _rootDirectory.Length)
                {
                    throw new FormatException($"Root Directory must have {_clusterMap.Length} entries");
                }
                _rootDirectory = (FatDirectoryEntry[])value.Clone();
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
                        _clusterMap = new ClusterEntry[_biosParameters.BytesPerSector * _biosParameters.SectorsPerFat / 3];
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

                            _clusterMap[i] = new ClusterEntry(Cluster1);
                            _clusterMap[i + 1] = new ClusterEntry(Cluster2);
                        }
                        //Hardcode first two clusters to "reserved"
                        _clusterMap[0].Status = _clusterMap[1].Status = ClusterStatus.Reserved;
                    }
                }
                //Skip all other FAT tables
                for (var i = 0; i < _biosParameters.NumberOfFatTables - 1; i++)
                {
                    BR.ReadBytes(_biosParameters.BytesPerSector * _biosParameters.SectorsPerFat);
                }
                //Root Directory
                _rootDirectory = ReadDirectory(BR.ReadBytes(FatReader.FAT_BYTES_PER_DIRECTORY_ENTRY * _biosParameters.NumberOfRootEntries));
                //Enumerable.Range(0, _biosParameters.NumberOfRootEntries).Select(m => new FatDirectoryEntry(BR.ReadBytes(FatReader.FAT_BYTES_PER_DIRECTORY_ENTRY))).ToArray();
            }
        }

        public int CalculateOffset(ushort Cluster)
        {
            return _biosParameters.SectorsPerCluster * _biosParameters.BytesPerSector * (Cluster - 1) +
                _biosParameters.NumberOfFatTables * _biosParameters.SectorsPerFat * _biosParameters.BytesPerSector +
                _biosParameters.NumberOfRootEntries * FatReader.FAT_BYTES_PER_DIRECTORY_ENTRY;
        }

        public byte[] ReadClusters(ushort[] ClusterChain, Stream FATStream)
        {
            int BlockSize = _biosParameters.SectorsPerCluster * _biosParameters.BytesPerSector;
            byte[] Data = new byte[BlockSize];
            using (var MS = new MemoryStream())
            {
                foreach (var Cluster in ClusterChain)
                {
                    FATStream.Seek(CalculateOffset(Cluster), SeekOrigin.Begin);
                    FATStream.Read(Data, 0, Data.Length);
                    MS.Write(Data, 0, Data.Length);
                }
                return MS.ToArray();
            }
        }

        public byte[] ReadFile(ushort[] ClusterChain, int FileSize, Stream FATStream)
        {
            return ReadClusters(ClusterChain, FATStream).Take(FileSize).ToArray();
        }

        public static FatDirectoryEntry[] ReadDirectory(byte[] RawDirectory)
        {
            using (var MS = new MemoryStream(RawDirectory, false))
            {
                using (var BR = new BinaryReader(MS))
                {
                    return Enumerable.Range(0, RawDirectory.Length / FatReader.FAT_BYTES_PER_DIRECTORY_ENTRY).Select(m => new FatDirectoryEntry(BR.ReadBytes(FatReader.FAT_BYTES_PER_DIRECTORY_ENTRY))).ToArray();
                }
            }
        }

        public ushort[] GetClusterChain(ushort Start)
        {
            if (Start >= _clusterMap.Length)
            {
                throw new ArgumentOutOfRangeException("Start", "Start must be smaller than ClusterMap.Length");
            }
            if (_clusterMap[Start].Status == ClusterStatus.Reserved)
            {
                throw new ArgumentException("Start refers to reserved cluster");
            }
            if (_clusterMap[Start].Status == ClusterStatus.Damaged)
            {
                throw new ArgumentException("Start refers to damaged cluster");
            }
            if (_clusterMap[Start].Status == ClusterStatus.Empty)
            {
                throw new ArgumentException("Start refers to empty cluster");
            }
            List<ushort> L = new List<ushort>();

            L.Add(Start);
            var Current = Start;
            while (_clusterMap[Current].Status == ClusterStatus.Occupied)
            {
                Current = _clusterMap[Current].RawValue;
                L.Add(Current);
            }

            return L.ToArray();
        }
    }
}
