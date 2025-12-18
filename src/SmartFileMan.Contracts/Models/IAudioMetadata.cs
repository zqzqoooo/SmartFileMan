using System;

namespace SmartFileMan.Contracts.Models
{
    /// <summary>
    /// 音频文件的元数据接口
    /// </summary>
    public interface IAudioMetadata
    {
        string Title { get; }
        string Artist { get; }
        string Album { get; }
        TimeSpan Duration { get; }
        int Bitrate { get; }

        /// <summary>
        /// 封面图片数据 (字节数组)，如果没有封面则为 null
        /// </summary>
        byte[]? CoverArt { get; }
    }
}