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
        static bool IsLoaded = false;
        IniFile MapINI;
        IniFile TemplatesINI;
        CellStruct[,] Cells = new CellStruct[128,128];
        Color Transparent = new Color();
        Color[] TerrainColors = new Color[20];
        WaypointStruct[] Waypoints = new WaypointStruct[8];
        static Bitmap[] SpawnLocationBitmaps = new Bitmap[8];
        int MapWidth = -1, MapHeight = -1, MapY = -1, MapX = -1;
        Dictionary<int, Template> TemplatesDict = new Dictionary<int,Template>();

        static public void Load()
        {
//            SpwanLocationBitmaps[0] = RedAlertMapPreviewGenerator.Properties.Resources._1;
            SpawnLocationBitmaps[1] = RedAlertMapPreviewGenerator.Properties.Resources._2;
        }

        public MapPreviewGenerator(string FileName)
        {
            MapINI = new IniFile(FileName);

            MapHeight = MapINI.getIntValue("Map", "Height", -1);
            MapWidth = MapINI.getIntValue("Map", "Height", -1);
            MapX = MapINI.getIntValue("Map", "X", -1);
            MapY = MapINI.getIntValue("Map", "Y", -1);

            string TheaterName = MapINI.getStringValue("Map", "Theater", "temperate");
            TemplatesINI = new IniFile(TheaterName + ".ini");

            Read_Terrain_Colors();

            CellStruct[] Raw = new CellStruct[16384];
            MemoryStream ms = Get_Packed_Section("MapPack");

            Init_Templates();

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

                Raw[Cell].Waypoint = WayPoint + 1;

//                Console.WriteLine("{0} = {1}", WayPoint, Cell);
            }

            var SectionTerrrain = MapINI.getSectionContent("Terrain");

            if (SectionTerrrain != null)
            {
                foreach (KeyValuePair<string, string> entry in SectionTerrrain)
                {
                    int Cell = int.Parse(entry.Key);
                    string Terrain = entry.Value;

                    Raw[Cell].Terrain = Terrain;

                    //                Console.WriteLine("{0} = {1}", Cell, Terrain);
                }
            }
            for (int x = 0; x < 128; x++)
            {
                for (int y = 0; y < 128; y++)
                {
                    int Index = (x * 128) + y;
                    Cells[y, x] = Raw[Index];

                    int WayPoint = Raw[Index].Waypoint-1;
                    if (WayPoint >= 0 && WayPoint <= 8)
                    {
                        Console.WriteLine("Waypoint found! ID = {0}, Raw = {1}", WayPoint, Index);
                        Waypoints[WayPoint].WasFound = true;
                        Waypoints[WayPoint].X = x;
                        Waypoints[WayPoint].Y = y;
                    }
                }
            }
        }

        void Read_Terrain_Colors()
        {
            var SectionKeyValues = TemplatesINI.getSectionContent("Terrain");

            foreach (KeyValuePair<string, string> entry in SectionKeyValues)
            {
                string ColorString = TemplatesINI.getStringValue(entry.Value, "Color", "0, 0, 0");

                string[] RGB = ColorString.Split(',');
                int Red = int.Parse(RGB[0]);
                int Green = int.Parse(RGB[1]);
                int Blue = int.Parse(RGB[2]);

 //               Console.WriteLine("{0}, {1}, {2}", Red, Green, Blue);
                TerrainColors[(int)Get_TerrainType_From_Name(entry.Value)] = Color.FromArgb(Red, Green, Blue);
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

        Color Color_From_TerrainType(TerrainType type)
        {
            if ((int)type < TerrainColors.Length)
            {
                return TerrainColors[(int)type];
            }
            return Transparent;
        }

        public Bitmap Get_Bitmap()
        {
            Bitmap bitMap = new Bitmap(128, 128);
            bitMap.MakeTransparent(Transparent);


            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    Color color = new Color();
                    color = Color.Transparent;

                    CellStruct data = Cells[x, y];
//                    int terrain = 0;

                    TerrainType terrainType = TerrainType_From_Template(data.Template, data.Tile);

                    color = Color_From_TerrainType(terrainType);

                    if (data.Terrain != null)
                    {
//                        Console.WriteLine("Terrain string != null");
                        color = Color_From_TerrainType(TerrainType.Tree);
                    }          
                        else if (data.Waypoint != 0 && data.Waypoint < 10)
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
                                    color = Color_From_TerrainType(TerrainType.Ore);
                                    //                        $terrain = $tileset->getTerrain('Ore');
                                    break;
                                case 0x09:
                                case 0x0A:
                                case 0x0B:
                                case 0x0C:
                                    //                        Console.WriteLine("Gem");
                                    color = Color_From_TerrainType(TerrainType.Gems);
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
                        if (Is_Out_Of_Bounds(x, y))
                        {
                            color = Transparent;
                        }

                        bitMap.SetPixel(x, y, color);
                    }
                }

            Graphics g = Graphics.FromImage(bitMap);

            Draw_Spawn_Locations(ref g);
            g.Flush();

            return bitMap;
        }

        void Draw_Spawn_Locations(ref Graphics g)
        {
            for (int i = 1; i < 8; i++)
            {
                Draw_Spawn_Location(ref g, i);
            }
        }

        void Draw_Spawn_Location(ref Graphics g, int SpawnNumber)
        {
            WaypointStruct Waypoint = Waypoints[SpawnNumber - 1];
            if (Waypoint.WasFound == false) return;
            if (SpawnLocationBitmaps[SpawnNumber - 1] == null) return;

//            Console.WriteLine("draw spawn: X = {0}, Y = {1}", Waypoint.X, Waypoint.Y);

            var Spawn = SpawnLocationBitmaps[SpawnNumber - 1];
            int SpawnX = Spawn.Height / 2;
            int SpawnY = Spawn.Width / 2;
            g.DrawImage(Spawn, Waypoint.Y - SpawnX , Waypoint.X  - SpawnY, Spawn.Width, Spawn.Height);

            g.Flush();
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

        TerrainType TerrainType_From_Template(int Template, int Tile)
        {
            if (Template == 65535) return TerrainType.Clear;
            string Section = Template.ToString();
            string Key = Tile.ToString();

            string Terrain = TemplatesINI.getStringValue(Section, Key, "Clear");

            return Get_TerrainType_From_Name(Terrain);
        }

        TerrainType Get_TerrainType_From_Name(string Name)
        {
            switch (Name)
            {
                case "Rough": return TerrainType.Rough;
                case "Clear": return TerrainType.Clear;
                case "Water": return TerrainType.Water;
                case "Road": return TerrainType.Road;
                case "Rock": return TerrainType.Rock;
                case "Tree": return TerrainType.Tree;
                case "River": return TerrainType.River;
                case "Wall": return TerrainType.Wall;
                case "Beach": return TerrainType.Beach;
                case "Ore": return TerrainType.Ore;
                case "Gems": return TerrainType.Gems;
                default: return TerrainType.Clear;
            }
        }

        void Init_Templates()
        {
            Template temp255 = new Template(); temp255.Set_Clear(); TemplatesDict.Add(255, temp255);
            Template temp65535 = new Template(); temp65535.Set_Clear(); TemplatesDict.Add(65535, temp65535);
            Template temp3 = new Template(); 
            temp3.T[3] = TerrainType.Rock;
            temp3.T[5] = TerrainType.Clear;
            temp3.T[6] = TerrainType.Clear;
            temp3.T[7] = TerrainType.Beach;
            temp3.T[8] = TerrainType.Beach;
            temp3.T[9] = TerrainType.Water;
            temp3.T[10] = TerrainType.Water;
            temp3.T[11] = TerrainType.River;
            temp3.T[12] = TerrainType.Water;
            temp3.T[13] = TerrainType.Water;
            temp3.T[16] = TerrainType.Water;
            TemplatesDict.Add(3, temp3);
            Template temp4 = new Template();
            temp4.Set_Type(TerrainType.Beach);
            temp4.T[4] = TerrainType.Clear;
            temp4.T[11] = TerrainType.Clear;
            temp4.Set(16, TerrainType.Rough);
            temp4.Set(17, TerrainType.Rough);
            temp4.Set(18, TerrainType.River);
            temp4.Set(20, TerrainType.Water);
            temp4.Set(21, TerrainType.Water);
            temp4.Set(22, TerrainType.Rock);
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

    struct WaypointStruct
    {
        public bool WasFound;
        public int X;
        public int Y;
    }

    enum TerrainType
    {
        Clear = 0,
        Water,
        Road,
        Rock,
        Tree,
        River,
        Rough,
        Wall,
        Beach,
        Ore,
        Gems,
    }
    class Template
    {
        public TerrainType[] T = new TerrainType[40]; // T = Tiles

        public void Set_Clear()
        {
            for (int i = 0; i < T.Length; i++)
            {
                T[i] = TerrainType.Clear;
            }
        }
        public void Set_Type(TerrainType type)
        {
            for (int i = 0; i < T.Length; i++)
            {
                T[i] = type;
            }
        }
        public void Set(int Index, TerrainType type)
        {
            T[Index] = type;
        }
    }
}
