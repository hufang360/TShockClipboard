using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;

namespace Clipboard;

public partial class Plugin
{
    public static void Load(string filename, int posX, int posY)
    {
        using var stream = new FileStream(filename, FileMode.Open);
        using var b = new BinaryReader(stream);

        string name = b.ReadString();
        int version = b.ReadInt32();
        uint tVersion = (uint)version;

        // check all the old versions
        if (version > 10000)
        {
            tVersion = (uint)(version - 10000);
            LoadV4(b, name, (int)tVersion, posX, posY);
        }
    }


    static void LoadV4(BinaryReader b, string name, int version, int posX, int posY)
    {
        var tileFrameImportant = ReadBitArray(b);
        int sizeX = b.ReadInt32();
        int sizeY = b.ReadInt32();
        //Utils.Log($"size: {sizeX},{sizeY}");
        //var buffer = new ClipboardBuffer(new Vector2Int32(sizeX, sizeY));
        //buffer.Name = name;

        var tiles = LoadTileData(b, sizeX, sizeY, version, tileFrameImportant);
        var rect = new Rectangle(0, 0, sizeX, sizeY);
        for (int rx = rect.Left; rx < rect.Right; rx++)
        {
            for (int ry = rect.Top; ry < rect.Bottom; ry++)
            {

                var tileData = tiles[rx, ry];
                Utils.LogDict(new Dictionary<object, object>() {
                    {"type", tileData.Type },
                    {"wall", tileData.Wall },
                    {"liquid", tileData.LiquidType },
                    {"u", tileData.U },
                    {"v", tileData.V },

                });


                var tile = Main.tile[posX + rx, posY + ry];
                tile.ClearEverything();
                tile.type = tileData.Type;
                tile.wall = tileData.Wall;
                tile.liquidType((int)tileData.LiquidType);
                tile.frameX = tileData.U;
                tile.frameY = tileData.V;
                tile.active(tileData.IsActive);
            }
        }
        Netplay.ResetSections();
        ////buffer.Chests.AddRange(World.LoadChestData(b));
        ////buffer.Signs.AddRange(World.LoadSignData(b));
        ////buffer.TileEntities.AddRange(World.LoadTileEntityData(b, (uint)version));

        //string verifyName = b.ReadString();
        //int verifyVersion = b.ReadInt32();
        //int verifyX = b.ReadInt32();
        //int verifyY = b.ReadInt32();

        //b.Close();

        //if (buffer.Name == verifyName &&
        //    version <= verifyVersion &&
        //    buffer.Size.X == verifyX &&
        //    buffer.Size.Y == verifyY)
        //{
        //    // valid;
        //    return buffer;
        //}

        //return null;
    }

    //  World.ReadBitArray
    /// <summary>
    /// Read an array of booleans from a bit-packed array.
    /// </summary>
    /// <param name="reader">BinaryReader at start of bit array.</param>
    /// <returns>Array of booleans</returns>
    public static bool[] ReadBitArray(BinaryReader reader)
    {
        // get the number of bits
        int length = reader.ReadInt16();

        // read the bit data
        var booleans = new bool[length];
        byte data = 0;
        byte bitMask = 128;
        for (int i = 0; i < length; i++)
        {
            // If we read the last bit mask (B1000000 = 0x80 = 128), read the next byte from the stream and start the mask over.
            // Otherwise, keep incrementing the mask to get the next bit.
            if (bitMask != 128)
            {
                bitMask = (byte)(bitMask << 1);
            }
            else
            {
                data = reader.ReadByte();
                bitMask = 1;
            }

            // Check the mask, if it is set then set the current boolean to true
            if ((data & bitMask) == bitMask)
            {
                booleans[i] = true;
            }
        }

        return booleans;
    }

    public static Tile[,] LoadTileData(BinaryReader r, int maxX, int maxY, int version, bool[] tileFrameImportant, TextWriter debugger = null)
    {
        var tiles = new Tile[maxX, maxY];
        debugger?.WriteLine("\"Tiles\": [");
        int rle;
        for (int x = 0; x < maxX; x++)
        {
            for (int y = 0; y < maxY; y++)
            {
                try
                {
                    debugger?.Write("{{ \"x\": {0},\"y\": {1},", x, y);

                    Tile tile = DeserializeTileData(r, tileFrameImportant, version, out rle, debugger);


                    tiles[x, y] = tile;

                    debugger?.WriteLine("\"RLE\": {0} }},", rle);
                    while (rle > 0)
                    {
                        y++;

                        if (y >= maxY)
                        {
                            break;
                            //throw new FileFormatException(
                            //    $"Invalid Tile Data: RLE Compression outside of bounds [{x},{y}]");
                        }
                        tiles[x, y] = (Tile)tile.Clone();
                        rle--;
                    }
                }
                catch (Exception)
                {
                    // forcing some recovery here

                    for (int x2 = 0; x2 < maxX; x2++)
                    {
                        for (int y2 = 0; y2 < maxY; y2++)
                        {
                            if (tiles[x2, y2] == null) tiles[x2, y2] = new Tile();
                        }
                    }
                    return tiles;
                }
            }
        }
        debugger?.WriteLine("]");

        return tiles;
    }

    public static Tile DeserializeTileData(BinaryReader r, bool[] tileFrameImportant, int version, out int rle, TextWriter debugger = null)
    {
        Tile tile = new();

        int tileType = -1;
        byte header4 = 0;
        byte header3 = 0;
        byte header2 = 0;
        byte header1 = r.ReadByte();

        bool hasHeader2 = false;
        bool hasHeader3 = false;
        bool hasHeader4 = false;

        // check bit[0] to see if header2 has data
        if ((header1 & 0b_0000_0001) == 0b_0000_0001)
        {
            hasHeader2 = true;
            header2 = r.ReadByte();
        }

        // check bit[0] to see if header3 has data
        if (hasHeader2 && (header2 & 0b_0000_0001) == 0b_0000_0001)
        {
            hasHeader3 = true;
            header3 = r.ReadByte();
        }

        if (version >= 269) // 1.4.4+ 
        {
            // check bit[0] to see if header4 has data
            if (hasHeader3 && (header3 & 0b_0000_0001) == 0b_0000_0001)
            {
                hasHeader4 = true;
                header4 = r.ReadByte();
            }
        }

        // check bit[1] for active tile
        bool isActive = (header1 & 0b_0000_0010) == 0b_0000_0010;
        debugger?.Write("\"IsActive\": {0},", isActive);

        if (isActive)
        {
            tile.IsActive = isActive;
            // read tile type

            if ((header1 & 0b_0010_0000) != 0b_0010_0000) // check bit[5] to see if tile is byte or little endian int16
            {
                // tile is byte
                tileType = r.ReadByte();
            }
            else
            {
                // tile is little endian int16
                byte lowerByte = r.ReadByte();
                tileType = r.ReadByte();
                tileType = tileType << 8 | lowerByte;
            }
            tile.Type = (ushort)tileType; // convert type to ushort after bit operations
            debugger?.Write("\"Type\": {0},", tileType);

            // read frame UV coords
            if (!tileFrameImportant[tileType])
            {
                tile.U = 0;//-1;
                tile.V = 0;//-1;
            }
            else
            {
                // read UV coords
                tile.U = r.ReadInt16();
                tile.V = r.ReadInt16();

                // reset timers
                if (tile.Type == (int)TileType.Timer)
                {
                    tile.V = 0;
                }

                debugger?.Write("\"U\": {0},", tile.U);
                debugger?.Write("\"V\": {0},", tile.V);
            }

            // check header3 bit[3] for tile color
            if ((header3 & 0b_0000_1000) == 0b_0000_1000)
            {
                tile.TileColor = r.ReadByte();
                debugger?.Write("\"TileColor\": {0},", tile.TileColor);
            }
        }

        // Read Walls
        if ((header1 & 0b_0000_0100) == 0b_0000_0100) // check bit[3] bit for active wall
        {
            tile.Wall = r.ReadByte();
            debugger?.Write("\"Wall\": {0},", tile.Wall);


            // check bit[4] of header3 to see if there is a wall color
            if ((header3 & 0b_0001_0000) == 0b_0001_0000)
            {
                tile.WallColor = r.ReadByte();
                debugger?.Write("\"WallColor\": {0},", tile.WallColor);
            }
        }

        // check for liquids, grab the bit[3] and bit[4], shift them to the 0 and 1 bits
        byte liquidType = (byte)((header1 & 0b_0001_1000) >> 3);
        if (liquidType != 0)
        {
            tile.LiquidAmount = r.ReadByte();
            tile.LiquidType = (LiquidType)liquidType; // water, lava, honey

            // shimmer (v 1.4.4 +)
            if (version >= 269 && (header3 & 0b_1000_0000) == 0b_1000_0000)
            {
                tile.LiquidType = LiquidType.Shimmer;
            }

            debugger?.Write("\"LiquidType\": \"{0}\",", tile.LiquidType.ToString());
            debugger?.Write("\"LiquidAmount\": {0},", tile.LiquidAmount);
        }

        // check if we have data in header2 other than just telling us we have header3
        if (header2 > 1)
        {
            // check bit[1] for red wire
            if ((header2 & 0b_0000_0010) == 0b_0000_0010)
            {
                tile.WireRed = true;
                debugger?.Write("\"WireRed\": {0},", tile.WireRed);
            }
            // check bit[2] for blue wire
            if ((header2 & 0b_0000_0100) == 0b_0000_0100)
            {
                tile.WireBlue = true;
                debugger?.Write("\"WireBlue\": {0},", tile.WireBlue);
            }
            // check bit[3] for green wire
            if ((header2 & 0b_0000_1000) == 0b_0000_1000)
            {
                tile.WireGreen = true;
                debugger?.Write("\"WireGreen\": {0},", tile.WireGreen);
            }

            // grab bits[4, 5, 6] and shift 4 places to 0,1,2. This byte is our brick style
            byte brickStyle = (byte)((header2 & 0b_0111_0000) >> 4);
            //if (brickStyle != 0 && TileProperties.Count > tile.Type && TileProperties[tile.Type].HasSlopes)
            //{
            //    tile.BrickStyle = (BrickStyle)brickStyle;
            //    debugger?.Write("\"BrickStyle\": {0},", tile.BrickStyle);
            //}
        }

        // check if we have data in header3 to process
        if (header3 > 1)
        {
            // check bit[1] for actuator
            if ((header3 & 0b_0000_0010) == 0b_0000_0010)
            {
                tile.Actuator = true;
                debugger?.Write("\"Actuator\": {0},", tile.Actuator);
            }

            // check bit[2] for inactive due to actuator
            if ((header3 & 0b_0000_0100) == 0b_0000_0100)
            {
                tile.InActive = true;
                debugger?.Write("\"InActive\": {0},", tile.InActive);
            }

            if ((header3 & 0b_0010_0000) == 0b_0010_0000)
            {
                tile.WireYellow = true;
                debugger?.Write("\"WireYellow\": {0},", tile.WireYellow);
            }

            if (version >= 222)
            {
                if ((header3 & 0b_0100_0000) == 0b_0100_0000)
                {
                    tile.Wall = (ushort)(r.ReadByte() << 8 | tile.Wall);
                    debugger?.Write("\"WallExtra\": {0},", tile.Wall);

                }
            }
        }

        if (version >= 269 && header4 > 1)
        {
            if ((header4 & 0b_0000_0010) == 0b_0000_0010)
            {
                tile.InvisibleBlock = true;
            }
            if ((header4 & 0b_0000_0100) == 0b_0000_0100)
            {
                tile.InvisibleWall = true;
            }
            if ((header4 & 0b_0000_1000) == 0b_0000_1000)
            {
                tile.FullBrightBlock = true;
            }
            if ((header4 & 0b_0001_0000) == 0b_0001_0000)
            {
                tile.FullBrightWall = true;
            }
        }

        // get bit[6,7] shift to 0,1 for RLE encoding type
        // 0 = no RLE compression
        // 1 = byte RLE counter
        // 2 = int16 RLE counter
        // 3 = not implemented, assume int16
        byte rleStorageType = (byte)((header1 & 192) >> 6);

        rle = rleStorageType switch
        {
            0 => 0,
            1 => r.ReadByte(),
            _ => r.ReadInt16()
        };

        return tile;
    }

    #region Int32
    [Serializable]
    public struct Vector2Int32
    {
        public int X;
        public int Y;

        public int PosX { get => X; set => X = value; }
        public int PosY { get => Y; set => Y = value; }

        public Vector2Int32(int x, int y)
            : this()
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return $"({X:0},{Y:0})";
        }

        public static bool Parse(string text, out Vector2Int32 vector)
        {
            vector = new Vector2Int32();
            if (string.IsNullOrWhiteSpace(text)) return false;

            var split = text.Split(',', 'x');
            if (split.Length != 2) return false;
            int x, y;
            if (int.TryParse(split[0], out x) ||
                int.TryParse(split[1], out y))
                return false;

            vector = new Vector2Int32(x, y);
            return true;
        }

        public static Vector2Int32 operator +(Vector2Int32 a, Vector2Int32 b) => new((short)(a.X + b.X), (short)(a.Y + b.Y));
        public static Vector2Int32 operator -(Vector2Int32 a, Vector2Int32 b) => new((short)(a.X - b.X), (short)(a.Y - b.Y));
        public static Vector2Int32 operator *(Vector2Int32 a, Vector2Int32 b) => new((short)(a.X * b.X), (short)(a.Y * b.Y));
        public static Vector2Int32 operator /(Vector2Int32 a, Vector2Int32 b) => new((short)(a.X / b.X), (short)(a.Y / b.Y));
        public static Vector2Int32 operator *(Vector2Int32 a, short b) => new((short)(a.X * b), (short)(a.Y * b));
        public static Vector2Int32 operator /(Vector2Int32 a, short b) => new((short)(a.X / b), (short)(a.Y / b));

        #region Equality
        public bool Equals(Vector2Int32 other)
        {
            return other.Y == Y && other.X == X;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != typeof(Vector2Int32)) return false;
            return Equals((Vector2Int32)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Y * 397) ^ X;
            }
        }

        public static bool operator ==(Vector2Int32 left, Vector2Int32 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector2Int32 left, Vector2Int32 right)
        {
            return !left.Equals(right);
        }
        #endregion
    }

    #endregion



}
