using Microsoft.Xna.Framework;

namespace Clipboard;

/// <summary>
/// 拉线数据（使用精密线控仪连续拖动放置电线，行为数据）
/// </summary>
public class OpData
{
    /// <summary>
    /// 玩家索引，-1代表服务器，但是服务器不支持拉线操作
    /// </summary>
    public int index = -1;

    /// <summary>
    /// 操作类型
    /// </summary>
    public OpCode opType = OpCode.None;

    /// <summary>
    /// 复制后保存的文件名
    /// </summary>
    public string copyName = "";

    /// <summary>
    /// 选区
    /// </summary>
    public Rectangle rect = new();


    /// <summary>
    /// 是否需要捕获拉线行为
    /// </summary>
    public bool needMassWore = false;
}