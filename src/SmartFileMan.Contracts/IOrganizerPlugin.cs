using System.Collections.Generic;
using System.Threading.Tasks;
using SmartFileMan.Contracts.Models;

namespace SmartFileMan.Contracts
{
    // 必须是 public 才能被其他项目引用
    public interface IOrganizerPlugin : IPlugin
    {
        // 核心执行方法
        Task ExecuteAsync(IList<IFileEntry> files);
    }
}