using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TShockAPI;

namespace Clipboard;

class Utils
{
    /// <summary>
    /// 保存目录
    /// </summary>
    public static string SaveDir;

    /// <summary>
    /// 列出json文件
    /// </summary>
    /// <param name="isDescend">是否进行倒序排序</param>
    public static Dictionary<string, FileInfo> GetFiles(bool isDescend = false)
    {
        Dictionary<string, FileInfo> dict = new();
        foreach (var f in new DirectoryInfo(SaveDir).GetFiles("*.json"))
        {
            if (!dict.ContainsKey(f.Name))
            {
                dict.Add(f.Name, f);
            }
        }

        // 排序
        if (!isDescend)
            dict = dict.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        else
            dict = dict.OrderByDescending(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

        return dict;
    }

    /// <summary>
    /// 将字符串换行
    /// </summary>
    /// <param name="lines"></param>
    /// <param name="column">列数，1行显示多个</param>
    /// <returns></returns>
    public static List<string> WarpLines(List<string> lines, int column = 5)
    {
        List<string> li1 = new();
        List<string> li2 = new();
        foreach (var line in lines)
        {
            if (li2.Count % column == 0)
            {
                if (li2.Count > 0)
                {
                    li1.Add(string.Join(", ", li2));
                    li2.Clear();
                }
            }
            li2.Add(line);
        }
        if (li2.Any())
        {
            li1.Add(string.Join(", ", li2));
        }
        return li1;
    }


    /// <summary>
    /// 分页显示帮助
    /// </summary>
    public static void Pagination(CommandArgs args, List<string> lines, string header, string footer = "", int expectedParameterIndex = 1)
    {
        if (!PaginationTools.TryParsePageNumber(args.Parameters, expectedParameterIndex, args.Player, out int pageNumber))
        {
            return;
        }
        var settings = new PaginationTools.Settings() { HeaderFormat = header };
        if (!string.IsNullOrEmpty(footer))
            settings.FooterFormat = footer;
        PaginationTools.SendPage(args.Player, pageNumber, lines, settings);
    }

    public static string RectangleToString(Rectangle rect) { return $"{rect.X},{rect.Y} {rect.Width}x{rect.Height}"; }


    /// <summary>
    /// 获取当前时间的 unix时间戳
    /// </summary>
    public static int GetUnixTimestamp
    {
        get
        {
            return (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }
    }

    public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
    {
        // Unix timestamp is seconds past epoch
        DateTime dateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        return dateTime;
    }

    public static void LogDict(Dictionary<object, object> dict) { Log(DictToString(dict)); }
    public static string DictToString(Dictionary<object, object> dict, bool ignoreEmpty = true)
    {
        var li = new List<string>();
        foreach (var ele in dict)
        {
            var s = ele.Value.ToString();
            if (ignoreEmpty && string.IsNullOrEmpty(s))
            {
                continue;
            }
            li.Add($"{ele.Key}: {s}");
        }
        return string.Join(", ", li);
    }

    /// <summary>
    /// 输出日志
    /// </summary>
    public static void Log(object obj) { TShock.Log.ConsoleInfo($"[clip]{obj}"); }
}

