using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SmartFileMan.Contracts.Models;
using SmartFileMan.Core.Models;

namespace SmartFileMan.Core.Services
{
    public class FileScanner
    {
        /// <summary>
        /// 异步扫描指定文件夹下的所有文件
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        /// <param name="recursive">是否递归子文件夹 (默认否，为了演示简单)</param>
        public async Task<IList<IFileEntry>> ScanAsync(string folderPath, bool recursive = false)
        {
            // 简单校验
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                return new List<IFileEntry>();
            }

            return await Task.Run(() =>
            {
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var dirInfo = new DirectoryInfo(folderPath);

                try
                {
                    // 获取所有文件
                    var fileInfos = dirInfo.GetFiles("*.*", searchOption);

                    // 转换为我们的 IFileEntry (LocalFileEntry)
                    var entries = fileInfos.Select(f => new LocalFileEntry(f.FullName))
                                           .Cast<IFileEntry>() // 强转为接口类型
                                           .ToList();

                    return entries;
                }
                catch (UnauthorizedAccessException)
                {
                    // 遇到没权限的文件夹跳过
                    return new List<IFileEntry>();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"扫描出错: {ex.Message}");
                    return new List<IFileEntry>();
                }
            });
        }
    }
}