using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Tile_Entities;
using TShockAPI;

namespace Clipboard;

public class TileScaner
{
    public static string DefaultClipName = "0";

    public static void Copy(TSPlayer op, Rectangle rect, string saveName = "")
    {
        TileSaver saver = new()
        {
            width = rect.Width,
            height = rect.Height,
            time = Utils.GetUnixTimestamp,
            timeDesc = string.Format("{0:yyyy/MM/dd-HH:mm}", DateTime.Now),
            version = "1.0",
            author = op.Name,
        };


        for (int rx = rect.Left; rx < rect.Right; rx++)
        {
            int index;
            int relativeX = rx - rect.Left;
            int relativeY;
            ContainerData containerData;
            for (int ry = rect.Top; ry < rect.Bottom; ry++)
            {
                relativeY = ry - rect.Top;

                ITile tile = Main.tile[rx, ry];

                // 图格数据
                saver.tiles.Add(new TileData(tile));

                // 箱子数据
                // 21=宝箱，467=宝箱2，88=梳妆台
                if (tile.type == 21 || tile.type == 467 || tile.type == 88)
                {
                    index = Chest.FindChest(rx, ry);
                    if (index != -1)
                    {
                        saver.chests.Add(new ChestData(relativeX, relativeY, Main.chest[index]));
                    }
                }

                // 物品框
                else if (tile.type == 395)
                {
                    index = TEItemFrame.Find(rx, ry);
                    if (index != -1)
                    {
                        TEItemFrame teFrame = (TEItemFrame)TileEntity.ByID[index];
                        saver.itemFrames.Add(new ItemFrameData(relativeX, relativeY, teFrame.item.netID, teFrame.item.prefix));
                    }
                }

                // 武器架
                else if (tile.type == 471)
                {
                    index = TEWeaponsRack.Find(rx, ry);
                    if (index != -1)
                    {
                        TEWeaponsRack teWeapon = (TEWeaponsRack)TileEntity.ByID[index];
                        saver.weaponsRacks.Add(new ItemFrameData(relativeX, relativeY, teWeapon.item.netID, teWeapon.item.prefix));
                    }
                }

                // 盘子
                else if (tile.type == 520)
                {
                    index = TEFoodPlatter.Find(rx, ry);
                    if (index != -1)
                    {
                        TEFoodPlatter tePlatter = (TEFoodPlatter)TileEntity.ByID[index];
                        saver.foodPlatters.Add(new ItemFrameData(relativeX, relativeY, tePlatter.item.netID, tePlatter.item.prefix));
                    }
                }

                // 帽架
                else if (tile.type == 475)
                {
                    index = TEHatRack.Find(rx, ry);
                    if (index != -1)
                    {
                        TEHatRack teHat = (TEHatRack)TileEntity.ByID[index];
                        containerData = new() { x = relativeX, y = relativeY };
                        containerData.Add(teHat._items[0]);
                        containerData.Add(teHat._items[1]);
                        containerData.Add(teHat._dyes[0]);
                        containerData.Add(teHat._dyes[1]);
                        saver.hatRacks.Add(containerData);
                    }
                }

                // 人体模型/女模特
                else if (tile.type == 470)
                {
                    index = TEDisplayDoll.Find(rx, ry);
                    if (index != -1)
                    {
                        TEDisplayDoll teDoll = (TEDisplayDoll)TileEntity.ByID[index];
                        containerData = new() { x = relativeX, y = relativeY };
                        foreach (var item in teDoll._items)
                        {
                            containerData.Add(item);
                        }
                        foreach (var item in teDoll._dyes)
                        {
                            containerData.Add(item);
                        }
                        saver.displayDolls.Add(containerData);
                    }
                }

                // 晶塔
                else if (tile.type == 597)
                {
                    index = TETeleportationPylon.Find(rx, ry);
                    if (index != -1)
                    {
                        var pylonData = new PylonData
                        {
                            x = relativeX,
                            y = relativeY,
                            type = TETeleportationPylon.GetPylonTypeFromPylonTileStyle(tile.frameX / 54),
                        };
                        saver.pylons.Add(pylonData);
                    }
                }

                // 感应器
                else if (tile.type == 423)
                {
                    index = TELogicSensor.Find(rx, ry);
                    if (index != -1)
                    {
                        saver.logicSensors.Add(new PointData(relativeX, relativeY));
                    }
                }

                // 训练假人
                else if (tile.type == 378)
                {
                    index = TETrainingDummy.Find(rx, ry);
                    if (index != -1)
                    {
                        saver.trainingDummys.Add(new PointData(relativeX, relativeY));
                    }
                }
            }
        }

        //string tileName = string.Format("{0:MMddHHmm}", DateTime.Now);
        TileSaver.Save(saver, saveName);
        op.SendSuccessMessage($"已复制！名称: {saveName}, 位置: {rect.X},{rect.Y}, 大小: {rect.Width}x{rect.Height}");
    }


    public static void Paste(TSPlayer op, string name, Rectangle selection)
    {
        int posX = selection.X;
        int posY = selection.Y;

        TileSaver saver = TileSaver.Load(name);
        if (saver.tiles.Count == 0)
        {
            op.SendErrorMessage($"找不到 {name}");
            return;
        }

        int i = 0;
        int index;

        #region 图格
        Rectangle rect = new(posX, posY, saver.width, saver.height);

        for (int rx = rect.Left; rx < rect.Right; rx++)
        {
            for (int ry = rect.Top; ry < rect.Bottom; ry++)
            {
                ITile tile = Main.tile[rx, ry];
                if (i >= saver.tiles.Count)
                {
                    break;
                }
                tile.ClearEverything();

                TileData rawTile = saver.tiles[i];
                i++;

                tile.type = rawTile.i;
                tile.wall = rawTile.w;
                tile.liquid = rawTile.l;
                tile.sTileHeader = rawTile.s;
                tile.bTileHeader = rawTile.b1;
                tile.bTileHeader2 = rawTile.b2;
                tile.bTileHeader3 = rawTile.b3;
                tile.frameX = rawTile.x;
                tile.frameY = rawTile.y;
            }
        }
        Netplay.ResetSections();
        #endregion


        #region 箱子
        foreach (var ele in saver.chests)
        {
            var cx = ele.x + rect.Left;
            var cy = ele.y + rect.Top;
            index = Chest.CreateChest(cx, cy);
            if (index != -1)
            {
                var ch = Main.chest[index];
                ch.name = ele.name;
                NetMessage.SendData((int)PacketTypes.ChestName, -1, -1, null, index, cx, cy);// 更新箱子名
                for (int k = 0; k < 40; k++)
                {
                    if (k >= ele.items.Count)
                    {
                        break;
                    }
                    ch.item[k] = ItemData.CreateItem(ele.items[k]);
                    NetMessage.SendData((int)PacketTypes.ChestItem, -1, -1, null, index, k); // 更新箱子里的物品
                }
            }
            else
            {
                Utils.Log($"粘贴箱子失败！箱子名: {ele.name}, 坐标: {cx},{cy}, 相对坐标: {ele.x},{ele.y}");
            }
        }
        #endregion


        #region 物品框
        foreach (var ele in saver.itemFrames)
        {
            var cx = ele.x + rect.Left;
            var cy = ele.y + rect.Top;
            TileEntity.PlaceEntityNet(cx, cy, (int)TEType.TEItemFrame);  // 这一步，可以防止物品掉下来
            TEItemFrame.TryPlacing(cx, cy, ele.i, ele.p, 1);

            //int itemIndex = Item.NewItem(new EntitySource_TileBreak(cx, cy), cx * 16, cy * 16, 32, 32, 1);
            //Main.item[itemIndex].netDefaults(ele.id);
            //Main.item[itemIndex].Prefix(ele.prefix);
            //Main.item[itemIndex].stack = 1;
            //NetMessage.SendData(21, -1, -1, null, itemIndex);

            // 尝试未果
            //WorldGen.PlaceObject(cx, cy, 395, mute: false, 0, 0, -1, op.TPlayer.direction);
            //NetMessage.SendData((int)PacketTypes.PlaceObject, -1, -1, null, cx, cy, 1);
        }
        #endregion


        #region 武器架
        foreach (var ele in saver.weaponsRacks)
        {
            var cx = ele.x + rect.Left;
            var cy = ele.y + rect.Top;
            TileEntity.PlaceEntityNet(cx, cy, (int)TEType.TEWeaponsRack);
            TEWeaponsRack.TryPlacing(cx, cy, ele.i, ele.p, 1);
        }
        #endregion


        #region 盘子
        foreach (var ele in saver.foodPlatters)
        {
            var cx = ele.x + rect.Left;
            var cy = ele.y + rect.Top;
            TileEntity.PlaceEntityNet(cx, cy, (int)TEType.TEFoodPlatter);
            TEFoodPlatter.TryPlacing(cx, cy, ele.i, ele.p, 1);
        }
        #endregion


        #region 帽架
        foreach (var ele in saver.hatRacks)
        {
            var cx = ele.x + rect.Left;
            var cy = ele.y + rect.Top;
            TileEntity.PlaceEntityNet(cx, cy, (int)TEType.TEHatRack);
            index = TEHatRack.Place(cx, cy);
            // TileEntity.ByPosition.TryGetValue(new Point16(cx, cy), out var value)
            // TileEntity.ByID.TryGetValue(hatRackIndex, out var value)
            if (TileEntity.ByID.TryGetValue(index, out var value) && value is TEHatRack teHat)
            {
                var dye = 0; //0=false,1=true
                var total = 4;
                var half = total / 2;
                for (int k = 0; k < ele.items.Count; k++)
                {
                    if (k < half)
                    {
                        teHat._items[k] = ItemData.CreateItem(ele.items[k]);
                        dye = 0;
                    }
                    else if (k < total)
                    {
                        teHat._dyes[k - half] = ItemData.CreateItem(ele.items[k]);
                        dye = 1;
                    }
                    NetMessage.TrySendData((int)PacketTypes.TileEntityHatRackItemSync, -1, -1, null, -1, index, k, dye);
                }
            }
            if (index == -1)
            {
                Utils.Log($"粘贴帽架失败！坐标: {cx},{cy}, 相对坐标: {ele.x},{ele.y}");
            }
        }
        #endregion


        #region 人体模型
        foreach (var ele in saver.displayDolls)
        {
            var cx = ele.x + rect.Left;
            var cy = ele.y + rect.Top;
            TileEntity.PlaceEntityNet(cx, cy, (int)TEType.TEDisplayDoll);
            index = TEDisplayDoll.Place(cx, cy);
            if (TileEntity.ByID.TryGetValue(index, out var value) && value is TEDisplayDoll teDoll)
            {
                var dye = 0; //0=false,1=true
                var total = 16;
                var half = total / 2;
                for (int k = 0; k < ele.items.Count; k++)
                {
                    if (k < half)
                    {
                        teDoll._items[k] = ItemData.CreateItem(ele.items[k]);
                        dye = 0;
                    }
                    else if (k < total)
                    {
                        teDoll._dyes[k - half] = ItemData.CreateItem(ele.items[k]);
                        dye = 1;
                    }
                    NetMessage.TrySendData((int)PacketTypes.TileEntityDisplayDollItemSync, -1, -1, null, -1, index, k, dye);
                }
            }
            if (index == -1)
            {
                Utils.Log($"粘贴人体模型失败！坐标: {cx},{cy}, 相对坐标: {ele.x},{ele.y}");
            }
        }
        #endregion


        #region 晶塔
        foreach (var ele in saver.pylons)
        {
            var cx = ele.x + rect.Left;
            var cy = ele.y + rect.Top;
            index = TETeleportationPylon.Place(cx, cy);
            NetMessage.SendData(86, -1, -1, null, index, cx, cy);
        }
        #endregion


        #region 感应器
        foreach (var ele in saver.logicSensors)
        {
            var cx = ele.x + rect.Left;
            var cy = ele.y + rect.Top;
            TileEntity.PlaceEntityNet(cx, cy, (int)TEType.TELogicSensor);
        }
        #endregion


        #region 训练假人
        foreach (var ele in saver.trainingDummys)
        {
            var cx = ele.x + rect.Left;
            var cy = ele.y + rect.Top;
            TileEntity.PlaceEntityNet(cx, cy, (int)TEType.TETrainingDummy);
        }
        #endregion


        op.SendSuccessMessage($"已粘贴！位置: {posX},{posY}, 大小: {rect.Width}x{rect.Height}");
    }


    public static void Dispose()
    {
    }

}
