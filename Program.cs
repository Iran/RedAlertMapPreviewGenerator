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
            // Make sure the Parse() functions parse commas and periods correctly
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

	        Stopwatch stopwatch = new Stopwatch();
	        stopwatch.Start();

            MapPreviewGenerator.Load();

            var MapPreview = new MapPreviewGenerator("testmap.mpr");

            Bitmap mapPreview = MapPreview.Get_Bitmap();
            Graphics g = Graphics.FromImage(mapPreview);

            Image img = resizeImage(mapPreview, new Size(256, 256));
            img.Save("derp.png");


	        stopwatch.Stop();

	        Console.WriteLine("Time elapsed: {0}",
	        stopwatch.ElapsedMilliseconds);

            Console.Read();
        }

        public static Image resizeImage(Image imgToResize, Size size)
        {
            return (Image)(new Bitmap(imgToResize, size));
        }
    }
}
