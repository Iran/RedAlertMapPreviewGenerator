using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using Nyerguds.Ini;

namespace RedAlertMapPreviewGenerator
{
    class MapPreviewGenerator
    {
        IniFile MapINI;
        CellStruct[,] Cells = new CellStruct[128,128];
        int MapWidth = -1, MapHeight = -1, MapY = -1, MapX = -1;

        public MapPreviewGenerator(string FileName)
        {
            MapINI = new IniFile(FileName);

            MapHeight = MapINI.getIntValue("Map", "Height", -1);
            MapWidth = MapINI.getIntValue("Map", "Height", -1);
            MapX = MapINI.getIntValue("Map", "X", -1);
            MapY = MapINI.getIntValue("Map", "Y", -1);

            CellStruct[] Raw = new CellStruct[16384];
            MemoryStream ms = Get_Packed_Section("MapPack");

           /* using (FileStream file = new FileStream("file.bin", FileMode.Create, System.IO.FileAccess.Write))
            {
                byte[] bytes = new byte[ms.Length];
                ms.Read(bytes, 0, (int)ms.Length);
                file.Write(bytes, 0, bytes.Length);
                ms.Close();
            } */

            var ByteReader = new FastByteReader(ms.GetBuffer());

            int i = 0;
            while (!ByteReader.Done())
            {
                
                Raw[i].Template = ByteReader.ReadWord();

//                Console.WriteLine("{0} = {1}", i, Cells[i].Template);
                ++i;

                if (i == 128*128)
                    break;
            }

            i = 0;
            while (!ByteReader.Done())
            {

                Raw[i].Tile = ByteReader.ReadByte();

//                Console.WriteLine("{0} = {1}", i, Raw[i].Tile);
                ++i;


                if (i == 128 * 128)
                    break;
            }

            MemoryStream ms2 = Get_Packed_Section("OverlayPack");
            var OverLayReader = new FastByteReader(ms2.GetBuffer());

            i = 0;
            while (!OverLayReader.Done())
            {

                Raw[i].Overlay = OverLayReader.ReadByte();

//                Console.WriteLine("{0} = {1}", i, Cells[i].Overlay);
                ++i;

                if (i == 128 * 128)
                    break;
            }

            var SectionKeyValues = MapINI.getSectionContent("Waypoints");

            foreach (KeyValuePair<string, string> entry in SectionKeyValues)
            {
                int WayPoint = int.Parse(entry.Key);
                int Cell = int.Parse(entry.Value);

                Raw[Cell].Waypoint = WayPoint;

//                Console.WriteLine("{0} = {1}", WayPoint, Cell);
            }

            var SectionTerrrain = MapINI.getSectionContent("Terrain");

            foreach (KeyValuePair<string, string> entry in SectionTerrrain)
            {
                int Cell = int.Parse(entry.Key);
                string Terrain = entry.Value;

                Raw[Cell].Terrain = Terrain;

//                Console.WriteLine("{0} = {1}", Cell, Terrain);
            }

            for (int x = 0; x < 128; x++)
            {
                for (int y = 0; y < 128; y++)
                {
                    Cells[y, x] = Raw[(x * 128) + y];
                }
            }
        }

        bool Is_Out_Of_Bounds(int X, int Y)
        {
            if (MapX > X || X >= MapX + MapWidth)
                return true;

            if (MapY > Y || Y >= MapY + MapHeight)
                return true;

            return false;
        }

        public Bitmap Get_Bitmap()
        {
            Color Transparent = new Color();
            Bitmap bitMap = new Bitmap(128, 128);
            bitMap.MakeTransparent(Transparent);


            for (int y = 0; y < 128; y++) 
            {
                for (int x = 0; x < 128; x++) 
                {
                    Color color = new Color();
                    color = Color.Green;

                    CellStruct data = Cells[x,y];
                    int terrain = 0;

//            $template = $tileset->getTemplate($data['template']);
//            if (!$template) {
 //               $template = $tileset->getTemplate(255);
//            }
//            $terrain = $tileset->getTerrain($template['Tiles'][$data['tile']]);

 //           if (data.Terrain != null) {
 //               $terrain = $tileset->getTerrain('Tree');
 //           } 
            if (data.Waypoint != 0 && data.Waypoint < 10) 
            {
                Console.WriteLine("Map[{0},{1}] waypoint = {2} = {3}", x, y, data.Waypoint, (y * 128) + x);
                color = Color.Red;
            } 
            else if (data.Overlay != 255) 
            {
//                Console.WriteLine("Overlay = {0}", data.Overlay);

                switch (data.Overlay) 
                {
                    case 0x05:
                    case 0x06:
                    case 0x07:
                    case 0x08:
//                        Console.WriteLine("Ore");
                        color = Color.Yellow;
//                        $terrain = $tileset->getTerrain('Ore');
                        break;
                    case 0x09:
                    case 0x0A:
                    case 0x0B:
                    case 0x0C:
//                        Console.WriteLine("Gem");
                        color = Color.Pink;
//                        $terrain = $tileset->getTerrain('Gems');
                        break;
                    default:
                        break;
                }
            }

        /*    if (isset($colors[$terrain['Color']])) {
                $color = $colors[$terrain['Color']];
            } else {
                list($r,$g,$b) = explode(',', $terrain['Color']);
                $color = imagecolorallocate($im, (int)$r, (int)$g, (int)$b);
                $colors[$terrain['Color']] = $color;
            } */

            // add out-of-boundaries check to set transparent background
            if (Is_Out_Of_Bounds(x, y) && color == Color.Red) {
                color = Transparent;
            }

            bitMap.SetPixel(x, y, color);
        }
    }


            return bitMap;
        }

        MemoryStream Get_Packed_Section(string SectionName)
        {
            var SectionKeyValues = MapINI.getSectionContent(SectionName);

            StringBuilder sb = new StringBuilder();

            foreach (KeyValuePair<string, string> entry in SectionKeyValues)
            {
                sb.Append(entry.Value);
            }
            String Base64String = sb.ToString();

            //            Console.WriteLine(sb.ToString());

            byte[] data = Convert.FromBase64String(Base64String);
            byte[] RawBytes = new Byte[8192];

            var chunks = new List<byte[]>();
            var reader = new BinaryReader(new MemoryStream(data));

            try
            {
                while (true)
                {
                    var length = reader.ReadUInt32() & 0xdfffffff;
                    var dest = new byte[8192];
                    var src = reader.ReadBytes((int)length);

                    /*int actualLength =*/
                    Format80.DecodeInto(src, dest);

                    chunks.Add(dest);
                }
            }
            catch (EndOfStreamException) { }

            var ms = new MemoryStream();
            foreach (var chunk in chunks)
                ms.Write(chunk, 0, chunk.Length);

            ms.Position = 0;

            return ms;
        }
    }

    class FastByteReader
    {
        readonly byte[] src;
        int offset = 0;

        public FastByteReader(byte[] src)
        {
            this.src = src;
        }

        public bool Done() { return offset >= src.Length; }
        public byte ReadByte() { return src[offset++]; }
        public int ReadWord()
        {
            int x = ReadByte();
            return x | (ReadByte() << 8);
        }

        public void CopyTo(byte[] dest, int offset, int count)
        {
            Array.Copy(src, this.offset, dest, offset, count);
            this.offset += count;
        }

        public int Remaining() { return src.Length - offset; }
    }

    public static class Format80
    {
        static void ReplicatePrevious(byte[] dest, int destIndex, int srcIndex, int count)
        {
            if (srcIndex > destIndex)
                throw new NotImplementedException(String.Format("srcIndex > destIndex {0} {1}", srcIndex, destIndex));

            if (destIndex - srcIndex == 1)
            {
                for (int i = 0; i < count; i++)
                    dest[destIndex + i] = dest[destIndex - 1];
            }
            else
            {
                for (int i = 0; i < count; i++)
                    dest[destIndex + i] = dest[srcIndex + i];
            }
        }

        public static int DecodeInto(byte[] src, byte[] dest)
        {
            var ctx = new FastByteReader(src);
            int destIndex = 0;

            while (true)
            {
                byte i = ctx.ReadByte();
                if ((i & 0x80) == 0)
                {
                    // case 2
                    byte secondByte = ctx.ReadByte();
                    int count = ((i & 0x70) >> 4) + 3;
                    int rpos = ((i & 0xf) << 8) + secondByte;

                    ReplicatePrevious(dest, destIndex, destIndex - rpos, count);
                    destIndex += count;
                }
                else if ((i & 0x40) == 0)
                {
                    // case 1
                    int count = i & 0x3F;
                    if (count == 0)
                        return destIndex;

                    ctx.CopyTo(dest, destIndex, count);
                    destIndex += count;
                }
                else
                {
                    int count3 = i & 0x3F;
                    if (count3 == 0x3E)
                    {
                        // case 4
                        int count = ctx.ReadWord();
                        byte color = ctx.ReadByte();

                        for (int end = destIndex + count; destIndex < end; destIndex++)
                            dest[destIndex] = color;
                    }
                    else if (count3 == 0x3F)
                    {
                        // case 5
                        int count = ctx.ReadWord();
                        int srcIndex = ctx.ReadWord();
                        if (srcIndex >= destIndex)
                            throw new NotImplementedException(String.Format("srcIndex >= destIndex {0} {1}",srcIndex, destIndex));

                        for (int end = destIndex + count; destIndex < end; destIndex++)
                            dest[destIndex] = dest[srcIndex++];
                    }
                    else
                    {
                        // case 3
                        int count = count3 + 3;
                        int srcIndex = ctx.ReadWord();
                        if (srcIndex >= destIndex)
                            throw new NotImplementedException(String.Format("srcIndex >= destIndex {0} {1}", srcIndex, destIndex));

                        for (int end = destIndex + count; destIndex < end; destIndex++)
                            dest[destIndex] = dest[srcIndex++];
                    }
                }
            }
        }

        public static byte[] Encode(byte[] src)
        {
            /* quick & dirty format80 encoder -- only uses raw copy operator, terminated with a zero-run. */
            /* this does not produce good compression, but it's valid format80 */

            var ctx = new FastByteReader(src);
            var ms = new MemoryStream();

            do
            {
                var len = Math.Min(ctx.Remaining(), 0x3F);
                ms.WriteByte((byte)(0x80 | len));
                while (len-- > 0)
                    ms.WriteByte(ctx.ReadByte());
            }
            while (!ctx.Done());

            ms.WriteByte(0x80);	// terminator -- 0-length run.

            return ms.ToArray();
        }
    }

    struct CellStruct
    {
        public int Template;
        public int Tile;
        public int Overlay;
        public string Terrain;
        public int Waypoint;
    }
}
