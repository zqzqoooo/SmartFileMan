using SmartFileMan.Contracts.Models;

namespace SmartFileMan.Contracts.Services
{
    public interface IFileScanner
    {
        /// <summary>
        /// 扫描指定文件夹
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        /// <param name="recursive">是否递归子文件夹</param>
        /// <returns>异步流 (IAsyncEnumerable)，实现"边扫描边显示"</returns>
        IAsyncEnumerable<IFileEntry> ScanFolderAsync(string folderPath, bool recursive = false);
    }
}