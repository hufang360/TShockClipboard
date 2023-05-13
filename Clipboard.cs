using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace Clipboard;

[ApiVersion(2, 1)]
public partial class Plugin : TerrariaPlugin
{
    public override string Name => "图格剪贴板";
    public override string Author => "hufang360";
    public override string Description => "复制粘贴图格";
    public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

    static bool hasEvent = false;
    static OpData[] datas = new OpData[Main.maxPlayers];
    static string PermAdmin = "clipboard";

    public Plugin(Main game) : base(game)
    {
        Utils.SaveDir = Path.Combine(TShock.SavePath, "Clipboard");
    }

    public override void Initialize()
    {
        Commands.ChatCommands.Add(new Command(PermAdmin, Manage, "clipboard", "clip") { HelpText = "复制粘贴图格", AllowServer = false });
    }

    /// <summary>
    /// 指令管理
    /// </summary>
    void Manage(CommandArgs args)
    {
        TSPlayer op = args.Player;
        if (args.Parameters.Count == 0)
        {
            op.SendInfoMessage("输入 /clip help 查看指令用法");
            return;
        }


        OpData data;
        switch (args.Parameters[0].ToLowerInvariant())
        {
            case "help":
                op.SendInfoMessage("/clip copy, 复制");
                op.SendInfoMessage("/clip paste, 粘贴");
                op.SendInfoMessage("/clip list, 列表");
                break;

            // 列出文件
            case "list":
                List<string> lines = new();
                foreach (var f in Utils.GetFiles(true))
                {
                    var fileName = f.Value.Name.Replace(".json", "");
                    var ts = TileSaver.Load(fileName);
                    var timeDesc = ts.time == 0 ? "" : $"{string.Format("{0:yyyy/MM/dd-HH:mm}", Utils.UnixTimeStampToDateTime(ts.time))}";
                    lines.Add(Utils.DictToString(new Dictionary<object, object>() {
                        {"名称", fileName },
                        {"大小", $"{ts.width}x{ts.height}"},
                        {"时间", timeDesc},
                    }));
                }
                var hStr = "保存的剪贴板({0}/{1}):";
                var fStr = "输入 /clip list {{0}} 查看更多".SFormat(Commands.Specifier);
                Utils.Pagination(args, lines, hStr, fStr);
                return;

            // 复制
            case "copy":
            case "c":
                data = GetData(op.Index);
                data.opType = OpCode.Copy;
                data.needMassWore = true;
                data.copyName = args.Parameters.Count > 1 ? args.Parameters[1] : TileScaner.DefaultClipName;
                RegisterEvent();
                op.SendSuccessMessage("等待你设置复制区域（使用[i:3611]精密线控仪）");
                break;

            // 粘贴
            case "paste":
            case "p":
            case "v":
                data = GetData(op.Index);
                if (args.Parameters.Count > 1)
                {
                    var fileName = args.Parameters[1];
                    if (!TileSaver.Exists(fileName))
                    {
                        op.SendErrorMessage($"未找到 {fileName}，输入 /clip list 查看可用的名称");
                        return;
                    }
                    var ts = TileSaver.Load(fileName);
                    data.rect.Width = ts.width;
                    data.rect.Height = ts.height;
                    data.copyName = fileName;
                    op.SendSuccessMessage($"等待你设置粘贴位置, 剪贴板名称:{fileName}, 大小: {ts.width}x{ts.height}。");
                }
                else
                {
                    if (data.copyName == "")
                    {
                        op.SendErrorMessage("剪贴板为空，请先复制或者指定要粘贴的名称！");
                        return;
                    }

                    op.SendSuccessMessage($"等待你设置粘贴位置, 剪贴板大小: {data.rect.Width}x{data.rect.Height}（使用[i:3611]精密线控仪）。");
                }
                data.opType = OpCode.Paste;
                data.needMassWore = true;
                RegisterEvent();
                break;

            case "import":
            case "i":
                Load(Path.Combine(Utils.SaveDir, "0.TEditSch"), op.TileX, op.TileY+3);
                break;
        }
    }

    static OpData GetData(int whoAmI)
    {
        if (datas[whoAmI] == null)
        {
            datas[whoAmI] = new OpData();
        }
        return datas[whoAmI];
    }

    static void RegisterEvent()
    {
        if (!hasEvent)
        {
            hasEvent = true;
            GetDataHandlers.MassWireOperation += OnMassWire;
        }
    }

    static void OnMassWire(object sender, GetDataHandlers.MassWireOperationEventArgs e)
    {
        var index = e.Player.Index;
        var d = datas[index];
        // ToolMode BitFlags: 1 = Red, 2 = Green, 4 = Blue, 8 = Yellow, 16 = Actuator, 32 = Cutter 33移除红电线 34移绿
        if (d.needMassWore && e.ToolMode == 1)
        {
            Rectangle rect = new(e.StartX, e.StartY, e.EndX - e.StartX, e.EndY - e.StartY);
            if (rect.Width < 0)
            {
                rect.X += rect.Width;
                rect.Width = Math.Abs(rect.Width);
            }
            if (rect.Height < 0)
            {
                rect.Y += rect.Height;
                rect.Height = Math.Abs(rect.Height);
            }

            // 边界
            rect.Width++;
            rect.Height++;

            d.rect = rect;
            d.needMassWore = false;
            e.Handled = true;

            switch (d.opType)
            {
                case OpCode.Copy:
                    TileScaner.Copy(TShock.Players[index], rect, d.copyName);
                    break;

                case OpCode.Paste:
                    TileScaner.Paste(TShock.Players[index], d.copyName, rect);
                    break;

                default:
                    break;
            }

        }
    }


    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (hasEvent)
            {
                hasEvent = false;
                GetDataHandlers.MassWireOperation -= OnMassWire;
            }
        }
        base.Dispose(disposing);
    }
}
