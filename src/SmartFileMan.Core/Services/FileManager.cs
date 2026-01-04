using System;
using System.IO;
using System.Threading.Tasks;
using SmartFileMan.Contracts.Models;
using SmartFileMan.Contracts.Common;
using SmartFileMan.Sdk.Services;
using SmartFileMan.Contracts.Services;

namespace SmartFileMan.Core.Services
{
    /// <summary>
    /// 文件管理器：负责文件的调度、防抖检查和安全移动
    /// File Manager: Responsible for file scheduling, debounce checks, and safe moving
    /// </summary>
    public class FileManager
    {
        private readonly PluginManager _pluginManager;
        private readonly SafeContext _safeContext;
        private readonly ISettingsService _settings; // 新增依赖

        public FileManager(PluginManager pluginManager, SafeContext safeContext, ISettingsService settings)
        {
            _pluginManager = pluginManager;
            _safeContext = safeContext;
            _settings = settings;
        }

        /// <summary>
        /// 处理单个文件：防抖 -> 竞价 -> 移动
        /// Process a single file: Debounce -> Bid -> Move
        /// </summary>
        public async Task<OperationResult> ProcessFileAsync(IFileEntry file)
        {
            // 0. 检查忽略列表
            var ignoredExtensions = await _settings.GetIgnoredExtensionsAsync();
            if (ignoredExtensions.Contains(file.Extension))
            {
                return OperationResult.Fail("文件格式在忽略列表中");
            }

            // 1. 防抖检查 (Debounce)
            if (!await IsFileStableAsync(file))
            {
                return OperationResult.Fail("文件正在被占用或写入中");
            }

            // 2. 竞价 (Bidding)
            // 增加超时控制 (Circuit Breaker 简化版)
            var bidTask = _pluginManager.GetBestRouteAsync(file);
            var timeoutTask = Task.Delay(3000); // 3秒超时

            if (await Task.WhenAny(bidTask, timeoutTask) == timeoutTask)
            {
                return OperationResult.Fail("插件响应超时");
            }

            var bestProposal = await bidTask;

            if (bestProposal == null)
            {
                return OperationResult.Fail("没有插件想要处理此文件");
            }

            // 3. 安全移动 (Safe Move)
            // 使用 SafeContext 以确保支持撤销
            return await _safeContext.MoveAsync(file, bestProposal.DestinationPath);
        }

        /// <summary>
        /// 检查文件是否稳定 (大小不变且未被锁定)
        /// Check if the file is stable (size unchanged and not locked)
        /// </summary>
        private async Task<bool> IsFileStableAsync(IFileEntry file)
        {
            try
            {
                long size1 = file.SizeBytes;
                await Task.Delay(500); // 等待 500ms
                
                // 重新获取文件信息 (需要刷新)
                var fileInfo = new FileInfo(file.FullPath);
                long size2 = fileInfo.Length;

                if (size1 != size2) return false;

                // 尝试打开文件流以检查锁
                using (var stream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
