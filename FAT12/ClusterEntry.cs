namespace FAT12
{
    public struct ClusterEntry
    {
        public ushort RawValue;
        public ClusterStatus Status;

        public ClusterEntry(ushort Raw)
        {
            RawValue = Raw;
            Status = GetFat12Status(Raw);
        }

        public static ClusterStatus GetFat12Status(ushort u)
        {
            if (u == 0)
            {
                return ClusterStatus.Empty;
            }
            if (u == 1 || u == 0xFF6)
            {
                return ClusterStatus.Reserved;
            }
            if (u <= 0xFEF || u == 0xFF0)
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
