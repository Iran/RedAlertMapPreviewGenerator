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
        static IniFile TemperateINI, SnowINI, DesertINI, InteriorINI, WinterINI;
        CellStruct[,] Cells = new CellStruct[128,128];
        Color Transparent = new Color();
        Color[] TerrainColors = new Color[20];
        WaypointStruct[] Waypoints = new WaypointStruct[8];
        static Bitmap[] SpawnLocationBitmaps = new Bitmap[8];
        int MapWidth = -1, MapHeight = -1, MapY = -1, MapX = -1;

        static public void Load()
        {
            if (IsLoaded == true) return;
            IsLoaded = true;

            SpawnLocationBitmaps[0] = RedAlertMapPreviewGenerator.Properties.Resources._1;
            SpawnLocationBitmaps[1] = RedAlertMapPreviewGenerator.Properties.Resources._2;
            SpawnLocationBitmaps[2] = RedAlertMapPreviewGenerator.Properties.Resources._3;
            SpawnLocationBitmaps[3] = RedAlertMapPreviewGenerator.Properties.Resources._4;
            SpawnLocationBitmaps[4] = RedAlertMapPreviewGenerator.Properties.Resources._5;
            SpawnLocationBitmaps[5] = RedAlertMapPreviewGenerator.Properties.Resources._6;
            SpawnLocationBitmaps[6] = RedAlertMapPreviewGenerator.Properties.Resources._7;
            SpawnLocationBitmaps[7] = RedAlertMapPreviewGenerator.Properties.Resources._8;

            TemperateINI = new IniFile("data/temperate.ini");
            SnowINI = new IniFile("data/snow.ini");
            DesertINI = new IniFile("data/desert.ini");
            WinterINI = new IniFile("data/winter.ini");
            InteriorINI = new IniFile("data/interior.ini");
        }

        public MapPreviewGenerator(string FileName)
        {
            MapINI = new IniFile(FileName);

            MapHeight = MapINI.getIntValue("Map", "Height", -1);
            MapWidth = MapINI.getIntValue("Map", "Width", -1);
            MapX = MapINI.getIntValue("Map", "X", -1);
            MapY = MapINI.getIntValue("Map", "Y", -1);

            Set_Templates_INI();

            Read_Terrain_Colors();

            CellStruct[] Raw = new CellStruct[16384];
            MemoryStream ms = Get_Packed_Section("MapPack");

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

            if (SectionKeyValues != null)
            {
                foreach (KeyValuePair<string, string> entry in SectionKeyValues)
                {
                    int WayPoint = int.Parse(entry.Key);
                    int Cell = int.Parse(entry.Value);

                    Raw[Cell].Waypoint = WayPoint + 1;

                    //                Console.WriteLine("{0} = {1}", WayPoint, Cell);
                }
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
                    if (WayPoint >= 0 && WayPoint < 8)
                    {
//                        Console.WriteLine("Waypoint found! ID = {0}, Raw = {1}", WayPoint, Index);

                        Waypoints[WayPoint].WasFound = true;
                        Waypoints[WayPoint].X = x;
                        Waypoints[WayPoint].Y = y;
                    }
                }
            }
        }

        void Set_Templates_INI()
        {
            string TheaterName = MapINI.getStringValue("Map", "Theater", "temperate");
            TheaterName.ToUpper();

            switch (TheaterName)
            {
                case "TEMPERATE": TemplatesINI = TemperateINI; break;
                case "SNOW": TemplatesINI = SnowINI; break;
                case "INTERIOR": TemplatesINI = InteriorINI; break;
                case "DESERT": TemplatesINI = DesertINI; break;
                case "WINTER": TemplatesINI = WinterINI; break;
                default: TemplatesINI = TemperateINI; break;
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

        public Bitmap Get_Bitmap(int _ScaleFactor)
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

                    TerrainType terrainType = TerrainType_From_Template(data.Template, data.Tile);
                    color = Color_From_TerrainType(terrainType);

                    if (data.Terrain != null)
                    {
                        color = Color_From_TerrainType(TerrainType.Tree);
                    }

                    else if (data.Overlay != 255)
                    {
                        switch (data.Overlay)
                        {
                            case 0x05:
                            case 0x06:
                            case 0x07:
                            case 0x08:
                                color = Color_From_TerrainType(TerrainType.Ore);
                                break;
                            case 0x09:
                            case 0x0A:
                            case 0x0B:
                            case 0x0C:
                                color = Color_From_TerrainType(TerrainType.Gems);
                                break;
                            default:
                                break;
                        }
                    }

                    if (Is_Out_Of_Bounds(x, y))
                    {
                        color = Transparent;
                    }

                    bitMap.SetPixel(x, y, color);
                }
            }

            var _Bitmap = new Bitmap(bitMap.Width * _ScaleFactor, bitMap.Height * _ScaleFactor);
            using (var _Graphics = Graphics.FromImage(_Bitmap))
            {
                _Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                _Graphics.DrawImage(bitMap, 0, 0, _Bitmap.Width, _Bitmap.Height);
            }

            Graphics g = Graphics.FromImage(_Bitmap);

            Draw_Spawn_Locations(ref g, _ScaleFactor);
            g.Flush();

            return _Bitmap;
        }


        void Draw_Spawn_Locations(ref Graphics g, int _ScaleFactor)
        {
            for (int i = 1; i < 9; i++)
            {
                Draw_Spawn_Location(ref g, i, _ScaleFactor);
            }
        }


        void Draw_Spawn_Location(ref Graphics g, int SpawnNumber, int _ScaleFactor)
        {
            WaypointStruct Waypoint = Waypoints[SpawnNumber - 1];
            if (Waypoint.WasFound == false) return;
            if (SpawnLocationBitmaps[SpawnNumber - 1] == null) return;

            // Console.WriteLine("draw spawn: X = {0}, Y = {1}", Waypoint.X, Waypoint.Y);

            var Spawn = SpawnLocationBitmaps[SpawnNumber - 1];
            int SpawnX = Spawn.Height / (2 * _ScaleFactor);
            int SpawnY = Spawn.Width / (2 * _ScaleFactor);
            g.DrawImage(Spawn, (Waypoint.Y - SpawnY) * _ScaleFactor, (Waypoint.X - SpawnX) * _ScaleFactor, Spawn.Width, Spawn.Height);

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

//            Console.WriteLine("Template = {0}", Template);

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
}
