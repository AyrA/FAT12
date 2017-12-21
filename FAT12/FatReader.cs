namespace FAT12
{
    public static class FatReader
    {
        //80*18*512*2
        public const int FLOPPY_TRACKS = 80;
        public const int FLOPPY_SECTORS_PER_TRACK = 18;
        public const int FLOPPY_BYTES_PER_SECTOR = 512;
        public const int FLOPPY_SIDES = 2;

        public const int FLOPPY_FORMAT_3_5 = FLOPPY_TRACKS * FLOPPY_SECTORS_PER_TRACK * FLOPPY_BYTES_PER_SECTOR * FLOPPY_SIDES;
    }
}
