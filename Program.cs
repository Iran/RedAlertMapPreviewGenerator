﻿using System;
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
            if (args.Length < 2)
            {
                Console.WriteLine("ERROR: Argument count needs to be at least 2.");
                Console.WriteLine("Usage: ProgramName InputFile OutputFile");
                return;
            }
            // Make sure the Parse() functions parse commas and periods correctly
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

            MapPreviewGenerator.Load();


            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var MapPreview = new MapPreviewGenerator(args[0]);

            Bitmap mapPreview = MapPreview.Get_Bitmap();
            Graphics g = Graphics.FromImage(mapPreview);

            Image img = resizeImage(mapPreview, new Size(256, 256));
            img.Save(args[1]);


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
