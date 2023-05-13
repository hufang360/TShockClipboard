using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Terraria;
using Terraria.GameContent;

namespace Clipboard;

public class TileSaver
{
    public int width = 0;
    public int height = 0;
    public int time = 0;
    public string timeDesc = "";
    public string version = "";
    public string author = "";


    [DefaultValue(default(List<TileData>))] public List<TileData> tiles = new();
    [DefaultValue(default(List<ChestData>))] public List<ChestData> chests = new();

    [DefaultValue(default(List<ItemFrameData>))] public List<ItemFrameData> itemFrames = new(); // 物品框
    [DefaultValue(default(List<ItemFrameData>))] public List<ItemFrameData> weaponsRacks = new(); // 武器架
    [DefaultValue(default(List<ItemFrameData>))] public List<ItemFrameData> foodPlatters = new(); // 盘子

    [DefaultValue(default(List<ContainerData>))] public List<ContainerData> displayDolls = new(); // 人体模型
    [DefaultValue(default(List<ContainerData>))] public List<ContainerData> hatRacks = new(); // 帽架

    [DefaultValue(default(List<PylonData>))] public List<PylonData> pylons = new(); // 晶塔
    [DefaultValue(default(List<PointData>))] public List<PointData> logicSensors = new(); // 感应器
    [DefaultValue(default(List<PointData>))] public List<PointData> trainingDummys = new(); // 训练假人

    public static TileSaver Load(string fileName)
    {
        var dir = Utils.SaveDir;
        string ConFile = Path.Combine(dir, $"{fileName}.json");
        if (!File.Exists(ConFile))
        {
            return new TileSaver();
        }
        return JsonConvert.DeserializeObject<TileSaver>(File.ReadAllText(ConFile));
        //return JsonConvert.DeserializeObject<TileSaver>(File.ReadAllText(path), new JsonSerializerSettings()
        //{
        //    Error = (sender, error) => error.ErrorContext.Handled = true
        //});
    }

    public static void Save(TileSaver obj, string fileName)
    {
        var dir = Utils.SaveDir;
        // 创建文件夹
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        string ConFile = Path.Combine(dir, $"{fileName}.json");
        File.WriteAllText(ConFile, JsonConvert.SerializeObject(obj, Formatting.None, new JsonSerializerSettings()
        {
            DefaultValueHandling = DefaultValueHandling.Ignore
        }));
    }


    /// <summary>
    /// 文件是否存在
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public static bool Exists(string fileName)
    {
        var dir = Utils.SaveDir;
        string ConFile = Path.Combine(dir, $"{fileName}.json");
        return File.Exists(ConFile);
    }
}






public class ContainerData
{
    public int x = 0;
    public int y = 0;
    [DefaultValue(default(List<ItemData>))] public List<ItemData> items = new();

    public ContainerData() { }

    public void Add(Item _item)
    {
        items.Add(new ItemData(_item));
    }
}


public class ChestData : ContainerData
{
    [DefaultValue("")] public string name = "";

    public ChestData() { }

    public ChestData(int relativeX, int relativeY, Chest ch)
    {
        name = ch.name;
        x = relativeX;
        y = relativeY;
        for (int i = 0; i < ch.item.Length; i++)
        {
            items.Add(new ItemData(ch.item[i]));
        }
    }
}


public class ItemFrameData
{
    public int x = 0;
    public int y = 0;
    public int i = 0;
    public int p = 0;

    public ItemFrameData(int relativeX, int relativeY, int itemID, int prefix)
    {
        x = relativeX;
        y = relativeY;
        i = itemID;
        this.p = prefix;
    }
}


public class ItemData
{
    public int i = 0;
    public int s = 0;
    public byte p = 0;

    public ItemData() { }

    public ItemData(Item item)
    {
        if (item != null)
        {
            i = item.netID;
            s = item.stack;
            p = item.prefix;
        }
    }

    public static Item CreateItem(ItemData _data)
    {
        var item = new Item();
        item.SetDefaults(_data.i);
        item.stack = _data.s;
        item.prefix = _data.p;

        return item;
    }
}


public class PylonData
{
    public int x = 0;
    public int y = 0;
    public TeleportPylonType type = 0;

    public PylonData() { }
}

public class PointData
{
    public int x = 0;
    public int y = 0;
    public PointData() { }
    public PointData(int _x, int _y)
    {
        x = _x;
        y = _y;
    }
}


public class TileData
{
    public ushort i;
    public ushort w;
    public byte l;
    public ushort s;
    public byte b1;
    public byte b2;
    public byte b3;
    public short x;
    public short y;


    public TileData() { }

    public TileData(ITile tile)
    {
        i = tile.type;
        w = tile.wall;
        l = tile.liquid;
        s = tile.sTileHeader;
        b1 = tile.bTileHeader;
        b2 = tile.bTileHeader2;
        b3 = tile.bTileHeader3;
        x = tile.frameX;
        y = tile.frameY;
    }
}