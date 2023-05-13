namespace Clipboard;

public enum OpCode
{
    None = 0,

    /// <summary>
    /// 复制拉线区域图格
    /// </summary>
    Copy = 1,

    /// <summary>
    /// 粘贴图格到拉线起始点
    /// </summary>
    Paste = 2,
}