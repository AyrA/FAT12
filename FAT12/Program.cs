using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace FAT12
{
    class Program
    {
        /// <summary>
        /// Main Entry Point
        /// </summary>
        /// <param name="args">Don't care (Yet)</param>
        /// <returns>0 on success</returns>
        static int Main(string[] args)
        {
            const int RET_OK = 0;
            const int RET_FAIL = 1;
            const string IMAGE = @"S:\Backup\Floppy_Images\Eliza.ima";

            if (!File.Exists(IMAGE))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("The Image {0} was not found. Change Program.cs to fit your needs.", IMAGE);
                Console.ResetColor();
                return RET_FAIL;
            }
            using (var FS = File.OpenRead(IMAGE))
            {
                //Read File as FAT12 Image
                var FAT = new FatPartition(FS);

                //Show Map
                ShowMapUnscaled(FAT.ClusterMap.Select(m => m.Status).ToArray());

                //If you want to test this with your own image, change the name to a valid root directory entry.
                var Entry = FAT.RootDirectory.FirstOrDefault(m => m.FullName.ToLower() == "eliza.dat");
                if (Entry != null)
                {
                    var Data = Encoding.Default.GetString(FAT.ReadFile(Entry, FS));
                    Console.Error.WriteLine(Data);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine("Specified file not found in the image. Change Program.cs to fit your needs.");
                    Console.ResetColor();
                    return RET_FAIL;
                }
                //Show FAT Directory recursively
                ShowDirectory(FAT.RootDirectory, FAT, FS);
                return RET_OK;
            }
#if DEBUG
            Console.Error.WriteLine("#END");
            while (Console.KeyAvailable) { Console.ReadKey(true); }
            Console.ReadKey(true);
#endif
        }

        /// <summary>
        /// Shows a Directory Entry recursively
        /// </summary>
        /// <param name="Entries">Directory Entries</param>
        /// <param name="FAT">FAT Partition of given Entries</param>
        /// <param name="FATStream">Stream for given FAT Partition</param>
        /// <param name="Parent">Parent Directory Path</param>
        private static void ShowDirectory(FatDirectoryEntry[] Entries, FatPartition FAT, Stream FATStream, string Parent = "")
        {
            if (Entries == null)
            {
                Entries = FAT.RootDirectory;
            }
            Console.WriteLine("Directory: {0}", string.IsNullOrEmpty(Parent) ? "\\" : Parent);
            foreach (var Entry in Entries.Where(m => m.EntryStatus == DirectoryEntryStatus.InUse))
            {
                Console.WriteLine("NAME={0,-12} TYPE={1,-15} SIZE={2,-10} START={3}", Entry.FullName, Entry.Attributes, Entry.FileSize, Entry.FirstCluster);
                //Recursively enter directories if the name is not "parent" or "current"
                if (Entry.Attributes.HasFlag(DirectoryEntryAttribute.Directory) && Entry.FullName != ".." && Entry.FullName != ".")
                {
                    //- Reads cluster chain of given Directory
                    //- Reads directory from given Chain
                    //- Recursively enters function to show directory listing
                    ShowDirectory(FatPartition.ReadDirectory(FAT.ReadClusters(FAT.GetClusterChain(Entry.FirstCluster), FATStream)), FAT, FATStream, Parent + "\\" + Entry.FullName);
                }
            }
        }

        /// <summary>
        /// Draws the cluster map 1:1 to the console
        /// </summary>
        /// <param name="Map">Cluster Map</param>
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
            Console.ResetColor();
        }

        /// <summary>
        /// Draws the Cluster Map into a given rectangular Area
        /// </summary>
        /// <param name="Map">Cluster Map</param>
        /// <param name="Width">Area Width</param>
        /// <param name="Height">Area Height</param>
        /// <remarks>This will upscale as well as downscale</remarks>
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

        /// <summary>
        /// Draws the Cluster Map into an Image File
        /// </summary>
        /// <param name="Map">Cluster Map</param>
        /// <param name="PixelSize">Pixel size of each Map entry</param>
        /// <returns>Image containing Cluster Map</returns>
        /// <remarks>This will make the image into a Square. Unused pixels are black.</remarks>
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

        /// <summary>
        /// Translates a Cluster Status Value into a character
        /// </summary>
        /// <param name="C">Cluster Status</param>
        /// <returns>Character</returns>
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

        /// <summary>
        /// Translates a Cluster Status Value into a Console color
        /// </summary>
        /// <param name="C">Cluster Status</param>
        /// <returns>Console Color Value</returns>
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

        /// <summary>
        /// Translates a Cluster Status Value into an RGB bitmap Color
        /// </summary>
        /// <param name="C">Cluster Status</param>
        /// <returns>Bitmap Color Value</returns>
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
