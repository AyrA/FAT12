using System;
using System.IO;
using System.Linq;
using System.Text;

namespace FAT12
{
    public class FatDirectoryEntry
    {
        private string _fileName;
        private string _fileExt;
        private ushort _createTime;
        private ushort _createDate;
        private ushort _accessDate;
        private ushort _modifyDate;
        private ushort _modifyTime;

        public string FileName
        {
            get
            {
                return _fileName.TrimEnd();
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("FileName");
                }
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("File name can't be exclusively made up of whitespace");
                }
                if (Encoding.Default.GetByteCount(value) > FatReader.FAT_DIRECTORY_FILENAME_LENGTH)
                {
                    throw new FormatException("File name can't be longer than 8 chars");
                }
                _fileName = value.ToUpper().PadRight(FatReader.FAT_DIRECTORY_FILENAME_LENGTH);
            }
        }
        public string Extension
        {
            get
            {
                return _fileExt.TrimEnd();
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("FileName");
                }
                if (Encoding.Default.GetByteCount(value) > FatReader.FAT_DIRECTORY_FILEEXT_LENGTH)
                {
                    throw new FormatException("File extension can't be longer than 3 chars");
                }
                _fileExt = value.ToUpper().PadRight(FatReader.FAT_DIRECTORY_FILEEXT_LENGTH);
            }
        }
        public string FullName
        {
            get
            {
                return FileName + "." + Extension;
            }
        }
        public DirectoryEntryStatus EntryStatus
        {
            get
            {
                switch (_fileName[0])
                {
                    case '\0':
                        return DirectoryEntryStatus.Empty;
                    case '\x05':
                        return DirectoryEntryStatus.PendingDelete;
                    case '\xE5':
                        return DirectoryEntryStatus.Deleted;
                    case '.':
                        return DirectoryEntryStatus.DotEntry;
                }
                return DirectoryEntryStatus.InUse;
            }
            set
            {
                if (!Enum.IsDefined(value.GetType(), value))
                {
                    throw new ArgumentException($"Undefined Enum:DirectoryEntryStatus Value: {value}");
                }
                if (value == DirectoryEntryStatus.InUse)
                {
                    throw new ArgumentException("'InUse' can't be set directly. Instead properly set the first char of the File Name");
                }
                //Replace first char with appropriate Value
                var Name = _fileName.ToCharArray();
                Name[0] = (char)value;
                _fileName = new string(Name);
            }
        }
        public DirectoryEntryAttribute Attributes;
        public byte AdditionalAttributes;
        public byte UndeleteCharOrCreateFineResolution;
        public ushort FirstCluster;
        public uint FileSize;

        public TimeSpan CreateTime
        {
            get
            {
                return ParseTimestamp(_createTime);
            }
            set
            {
            }
        }
        public DateTime CreateDate
        {
            get
            {
                return ParseDate(_createDate);
            }
        }
        public TimeSpan ModifyTime
        {
            get
            {
                return ParseTimestamp(_modifyTime);
            }
            set
            {
            }
        }
        public DateTime ModifyDate
        {
            get
            {
                return ParseDate(_modifyDate);
            }
        }

        public ushort ExtendedAttributes;

        public FatDirectoryEntry(byte[] Entry)
        {
            if (Entry == null)
            {
                throw new ArgumentNullException("FileName");
            }
            if (Entry.Length != FatReader.FAT_BYTES_PER_DIRECTORY_ENTRY)
            {
                throw new ArgumentOutOfRangeException($"Entry must be {FatReader.FAT_BYTES_PER_DIRECTORY_ENTRY} bytes in Length");
            }
            using (var BR = new BinaryReader(new MemoryStream(Entry, false)))
            {
                _fileName = Encoding.Default.GetString(BR.ReadBytes(FatReader.FAT_DIRECTORY_FILENAME_LENGTH));
                _fileExt = Encoding.Default.GetString(BR.ReadBytes(FatReader.FAT_DIRECTORY_FILEEXT_LENGTH));
                Attributes = (DirectoryEntryAttribute)BR.ReadByte();
                AdditionalAttributes = BR.ReadByte();
                UndeleteCharOrCreateFineResolution = BR.ReadByte();

                _createTime = BR.ReadUInt16();
                _createDate = BR.ReadUInt16();

                _accessDate = BR.ReadUInt16();

                ExtendedAttributes = BR.ReadUInt16();

                _modifyTime = BR.ReadUInt16();
                _modifyDate = BR.ReadUInt16();

                FirstCluster = BR.ReadUInt16();
                FileSize = BR.ReadUInt32();
            }
        }

        /// <summary>
        /// Checks if the given file name Segment (name OR extension)
        /// is only made up of supported characters
        /// </summary>
        /// <param name="FileNameSegment">File Name OR Extension</param>
        /// <returns>true, if valid chars used only</returns>
        public static bool IsValidFileName(string FileNameSegment)
        {
            return FileNameSegment != null && FileNameSegment.ToCharArray().All(m => FatReader.VALID_FAT_NAME_CHARS.Contains(m));
        }

        public static DateTime ParseDate(ushort Date)
        {
            if (Date == 0)
            {
                return DateTime.MinValue;
            }
            //Bitmap: yyyyyyymmmmddddd
            var Days = Date & 0x1F;
            var Months = (Date >> 5) & 0xF;
            var Years = Date >> 9;
            return new DateTime(1980 + Years, Months, Days, 0, 0, 0, DateTimeKind.Local);
        }

        public static TimeSpan ParseTimestamp(ushort Timestamp)
        {
            //Bitmap: hhhhhmmmmmmsssss
            var seconds = (Timestamp & 0x1F) / 2;
            var minutes = (Timestamp >> 5) & 0x7e0;
            var hours = Timestamp >> 11;
            return TimeSpan.FromSeconds(seconds + minutes * 60 + hours * 3600);
        }
    }

    [Flags]
    public enum DirectoryEntryAttribute : byte
    {
        /// <summary>
        /// No Attributes designate a regular File
        /// </summary>
        Normal = 0,
        /// <summary>
        /// File is Readonly
        /// </summary>
        Readonly = 1,
        /// <summary>
        /// File/Directory is Hidden
        /// </summary>
        Hidden = Readonly << 1,
        /// <summary>
        /// File/Directory is important to the System.
        /// Defragmenters will ignore that file
        /// </summary>
        System = Hidden << 1,
        /// <summary>
        /// Entry is a Volume Label
        /// </summary>
        VolumeLabel = System << 1,
        /// <summary>
        /// Entry is a Directory and not a File
        /// </summary>
        Directory = VolumeLabel << 1,
        /// <summary>
        /// Entry has been modified since the last Backup
        /// </summary>
        Archive = Directory << 1,
        /// <summary>
        /// Entry represents a Device
        /// </summary>
        Device = Archive << 1,
        /// <summary>
        /// Reserved, must never be set
        /// </summary>
        Reserved = Archive << 1
    }

    public enum DirectoryEntryStatus : byte
    {
        /// <summary>
        /// Entry is not used
        /// </summary>
        Empty = 0,
        /// <summary>
        /// File is pending for removal
        /// </summary>
        PendingDelete = 5,
        /// <summary>
        /// File is "." or ".." entry
        /// </summary>
        DotEntry = 0x2E,
        /// <summary>
        /// File is deleted. The reason, why 0xE5 was chosen for this purpose in 86-DOS is down to the fact, that 8-inch CP/M floppies came pre-formatted with this value filled and so could be used to store files out-of-the box.
        /// </summary>
        Deleted = 0xE5,
        /// <summary>
        /// Regular Entry that is in use
        /// </summary>
        InUse = 0xFF
    }
}