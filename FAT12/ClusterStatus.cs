namespace FAT12
{
    public enum ClusterStatus : byte
    {
        Empty = 0,
        Reserved = 1,
        Occupied = 2,
        Damaged = 3,
        EOF = 4
    }
}
