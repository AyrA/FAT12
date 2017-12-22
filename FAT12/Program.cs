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

                ShowMapUnscaled(FAT.ClusterMap);

                /*
                using (var BMP = DrawMap(FAT.ClusterMap, 20))
                {
                    BMP.Save(@"C:\temp\diskmap.png");
                }
                //*/

                Console.WriteLine("TYPE={0}; LABEL={1}", FAT.ExtendedBiosParameters.SystemID, FAT.ExtendedBiosParameters.VolumeLabel);
            }
#if DEBUG
            Console.Error.WriteLine("#END");
            while (Console.KeyAvailable) { Console.ReadKey(true); }
            Console.ReadKey(true);
#endif
        }

        private static void ShowMapUnscaled(ClusterStatus[] Map)
        {
            var Colors = Map.Select(m => ToConsoleColor(m)).ToArray();
            var Chars = Map.Select(m => ToConsoleChar(m)).ToArray();
            for (var i = 0; i < Map.Length; i++)
            {
                Console.ForegroundColor = Colors[i];
                Console.Write(Chars[i]);
            }
            Console.WriteLine();
        }

        private static void ShowMap(ClusterStatus[] Map, int Width = -1, int Height = -1)
        {
            if (Width <= 0)
            {
                Width = Console.WindowWidth - 1;
            }
            if (Height <= 0)
            {
                Height = Console.WindowHeight - 1;
            }
            var Area = Width * Height;

            var Colors = Map.Select(m => ToConsoleColor(m)).ToArray();
            var Chars = Map.Select(m => ToConsoleChar(m)).ToArray();
            for (var i = 0; i < Area; i++)
            {
                int Index = (int)(i * 1.0 / Area * Map.Length);
                Console.ForegroundColor = Colors[Index];
                Console.Write(Chars[Index]);
                if ((i + 1) % Width == 0)
                {
                    Console.WriteLine();
                }
            }
            Console.ResetColor();
        }

        private static Image DrawMap(ClusterStatus[] Map, int PixelSize = 1)
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
                foreach (var Pixel in Map.Select(ToBitmapColor))
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

        private static char ToConsoleChar(ClusterStatus C)
        {
            switch (C)
            {
                case ClusterStatus.Damaged:
                    return 'B';
                case ClusterStatus.EOF:
                    return '░';
                case ClusterStatus.Occupied:
                    return '█';
                case ClusterStatus.Reserved:
                    return 'R';
            }
            return '.';
        }

        private static ConsoleColor ToConsoleColor(ClusterStatus C)
        {
            switch (C)
            {
                case ClusterStatus.Damaged:
                    return ConsoleColor.Red;
                case ClusterStatus.EOF:
                    return ConsoleColor.Green;
                case ClusterStatus.Occupied:
                    return ConsoleColor.Blue;
                case ClusterStatus.Reserved:
                    return ConsoleColor.DarkCyan;
            }
            return ConsoleColor.White;
        }

        private static Color ToBitmapColor(ClusterStatus C)
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
