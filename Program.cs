using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace RedAlertMapPreviewGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("ERROR: Argument count needs to be at least 3.");
                Console.WriteLine("Usage: ProgramName InputFile OutputFile ScaleFactor");
                return;
            }
            // Make sure the Parse() functions parse commas and periods correctly
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

            MapPreviewGenerator.Load();


            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var MapPreview = new MapPreviewGenerator(args[0]);

            int ScaleFactor = int.Parse(args[2]);
            MapPreview.Get_Bitmap(ScaleFactor).Save(args[1]);


            stopwatch.Stop();

            Console.WriteLine("");
            Console.WriteLine("Created image '{0}' using map '{1}'.", args[1], args[0]);
            Console.WriteLine("Time elapsed: {0} milliseconds.",
            stopwatch.ElapsedMilliseconds);

        }

        public static Image resizeImage(Image imgToResize, Size size)
        {
            return (Image)(new Bitmap(imgToResize, size));
        }
    }
}
