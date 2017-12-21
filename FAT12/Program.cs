using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace FAT12
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var FS = File.OpenRead(@"S:\Backup\Floppy_Images\Eliza.ima"))
            {
                var FAT = new FatPartition(FS);

                using (var BMP = DrawMap(FAT.ClusterMap, 20))
                {
                    BMP.Save(@"C:\temp\diskmap.png");
                }

                Console.WriteLine("TYPE={0}; LABEL={1}", FAT.ExtendedBiosParameters.SystemID, FAT.ExtendedBiosParameters.VolumeLabel);
            }
#if DEBUG
            Console.Error.WriteLine("#END");
            while (Console.KeyAvailable) { Console.ReadKey(true); }
            Console.ReadKey(true);
#endif
        }

        static Image DrawMap(ClusterStatus[] Map, int PixelSize = 1)
        {
            //Get square that definitely can hold the entire map
            int Format = (int)Math.Ceiling(Math.Sqrt(Map.Length));
            //Eventually cut off bottom row if not needed
            using (var BMP = new Bitmap(Format, Format * Format - Format > Map.Length ? Format - 1 : Format))
            {
                //Draw Black rectangle
                using (Graphics G = Graphics.FromImage(BMP))
                {
                    G.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    G.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                    G.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                    G.FillRectangle(Brushes.Black, new Rectangle(new Point(0, 0), BMP.Size));
                }
                int x = 0;
                int y = 0;
                //Set all Pixels
                foreach (var Pixel in Map.Select(ToColor))
                {
                    BMP.SetPixel(x, y, Pixel);

                    if (++x >= BMP.Width)
                    {
                        x = 0;
                        ++y;
                    }
                }
                //Return as is
                if (PixelSize <= 1)
                {
                    return (Image)BMP.Clone();
                }
                //Scale image
                using (var Copy = new Bitmap(Format * PixelSize, Format * PixelSize))
                {
                    using (Graphics G = Graphics.FromImage(Copy))
                    {
                        G.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        G.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                        G.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                        G.DrawImage(BMP, new Rectangle(new Point(0, 0), Copy.Size));
                    }
                    return (Image)Copy.Clone();
                }
            }
        }

        static Color ToColor(ClusterStatus C)
        {
            switch (C)
            {
                case ClusterStatus.Damaged:
                    return Color.Red;
                case ClusterStatus.EOF:
                    return Color.Green;
                case ClusterStatus.Occupied:
                    return Color.Blue;
                case ClusterStatus.Reserved:
                    return Color.Purple;
            }
            return Color.White;
        }
    }
}
