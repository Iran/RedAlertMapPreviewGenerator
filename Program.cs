using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace RedAlertMapPreviewGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var MapPreview = new MapPreviewGenerator("testmap.mpr");
//            MapPreview.Get_Bitmap().Save("derp.png");

            Image img = resizeImage(MapPreview.Get_Bitmap(), new Size(512, 512));
            img.Save("derp.png");

            Console.Read();
        }

        public static Image resizeImage(Image imgToResize, Size size)
        {
            return (Image)(new Bitmap(imgToResize, size));
        }
    }
}
