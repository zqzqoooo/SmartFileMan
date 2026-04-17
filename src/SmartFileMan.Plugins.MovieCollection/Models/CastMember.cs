namespace SmartFileMan.Plugins.MovieCollection.Models;

/// <summary>
/// 演员信息
/// Cast member information
/// </summary>
public class CastMember
{
    /// <summary>演员ID</summary>
    public int Id { get; set; }

    /// <summary>演员姓名</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>饰演角色</summary>
    public string Character { get; set; } = string.Empty;

    /// <summary>头像路径（TMDB）</summary>
    public string? ProfilePath { get; set; }
}
