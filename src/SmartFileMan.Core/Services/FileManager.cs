using System;
using System.IO;
using System.Threading.Tasks;
using SmartFileMan.Contracts.Models;
using SmartFileMan.Contracts.Common;
using SmartFileMan.Contracts.Services;
using SmartFileMan.Contracts.Core;
using SmartFileMan.Sdk.Services;
using Microsoft.Extensions.Logging;    // ILogger

namespace SmartFileMan.Core.Services
{
    /// <summary>
    /// 文件管理器：负责文件调度、防抖检查和安全移动
    /// File Manager: Responsible for file scheduling, debounce checks, and safe moving
    /// </summary>
    public class FileManager : IFileManager
    {
        private readonly PluginManager _pluginManager;
        private readonly SafeContext _safeContext;
        private readonly ISettingsService _settings;
        private readonly ILogger<FileManager> _logger;
        private readonly IFileTracker _fileTracker; // Invoke Tracker

        public FileManager(PluginManager pluginManager, SafeContext safeContext, ISettingsService settings, ILogger<FileManager> logger, IFileTracker fileTracker)
        {
            _pluginManager = pluginManager;
            _safeContext = safeContext;
            _settings = settings;
            _logger = logger;
            _fileTracker = fileTracker;
        }

        // ============================================
        // 辅助方法：计算哈希
        // Helper method: Compute hash
        // ============================================
        private async Task<string> ComputeHashAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var fs = File.OpenRead(filePath);
                    using var sha = System.Security.Cryptography.SHA256.Create();
                    byte[] hashBytes = sha.ComputeHash(fs);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error computing hash for file: {Path}", filePath);
                    return $"ERROR-{Guid.NewGuid().ToString().Substring(0, 8)}";
                }
            });
        }

        /// <summary>
        /// 处理单个文件：防抖 -> 竞价 -> 移动
        /// Process a single file: Debounce -> Bid -> Move
        /// </summary>
        public async Task<OperationResult> ProcessFileAsync(IFileEntry file)
        {
            _logger.LogInformation("Processing file: {FullPath}", file.FullPath);

            // -1. 检查历史记录 (增量扫描)
            // -1. Check history (Incremental Scan)
            if (_fileTracker.IsProcessed(file.FullPath, file.LastWriteTime, file.SizeBytes))
            {
                _logger.LogDebug("Skipping already processed file: {Path}", file.FullPath);
                return OperationResult.Success("File already processed (Incremental Skip) / 文件已处理 (增量跳过)");
            }

            // 0. 检查忽略列表
            // 0. Check ignore list
            var ignoredExtensions = await _settings.GetIgnoredExtensionsAsync();
            if (ignoredExtensions.Contains(file.Extension))
            {
                _logger.LogDebug("File ignored by extension filter: {Extension}", file.Extension);
                // 即使忽略，也标记为已处理以避免重复检查？
                // Even if ignored, mark as processed to avoid re-check?
                // 是的，静态文件不需要重新检查如果未改变。
                // Yes, static files don't need re-check if unchanged.
                // 但动态文件可能被忽略，是否每次都检查扩展名列表（虽然列表可能变化）
                // However, dynamic files might be ignored, should we check extension list each time?
                _fileTracker.TrackProcessed(
                    file.FullPath,
                    "UnknownHash",
                    file.SizeBytes,
                    file.LastWriteTime,
                    "System.Ignored",
                    "",
                    "IgnoredExtension"
                );
                return OperationResult.Fail("File ignored by extension filter / 文件被扩展名过滤掉了");
            }

            // 1. 稳定性检查 (防抖)
            // 1. Stability Check (Debounce)
            if (!await IsFileStableAsync(file))
            {
                _logger.LogWarning("File unstable (locked/changing): {FullPath}", file.FullPath);
                return OperationResult.Fail("File is unstable or locked / 文件不稳定或被锁定"); // Can throw SmartException(ErrorCode.FileLocked) if desired, but sticking to OperationResult for now
            }

            // 2. 竞价 (Bidding)
            try
            {
                var bidTask = _pluginManager.GetBestRouteAsync(file);
                var timeoutTask = Task.Delay(3000); // 3 seconds timeout / 3秒超时

                if (await Task.WhenAny(bidTask, timeoutTask) == timeoutTask)
                {
                    _logger.LogError("Validator timeout for file: {FullPath}", file.FullPath);
                    throw new SmartException(ErrorCode.PluginTimeout, "Plugins timed out bidding");
                }

                var bestProposal = await bidTask;
                if (bestProposal == null)
                {
                    _logger.LogInformation("No plugin wanted this file: {FullPath}", file.FullPath);
                    return OperationResult.Fail("No plugin wanted to handle this file / 没有插件想要处理这个文件");
                }

                // 记录竞价成功日志
                // Log Bidding Success
                string successLog = $"文件{file.Name}竞价成功-赢家{bestProposal.PluginName}获取此文件-竞分数{bestProposal.Score}";
                _logger.LogInformation(successLog);
                _safeContext.BroadcastLog("BID", "WIN", successLog);

                _logger.LogInformation("Winner: {PluginName} (Score: {Score}) -> {Dest}", bestProposal.PluginName, bestProposal.Score, bestProposal.DestinationPath);

                // 3. 安全移动 (Safe Move)
                var result = await _safeContext.MoveAsync(file, bestProposal.DestinationPath);

                // 计算 Hash (延迟到成功处理后，或者之前？如果为了去重应该之前，但昂贵。这里用于记录)
                // Calculate Hash (Delayed to after success, or before? If for dedup should be before, but expensive. Here for recording)
                string hash = "Unknown";

                if (result.IsSuccess)
                {
                    string finalPath = Path.Combine(bestProposal.DestinationPath, file.Name);

                    if (File.Exists(finalPath))
                    {
                        // 立即标记为已处理（使用临时哈希）
                        // Immediately mark as processed (use temporary hash)
                        string tempHash = $"PENDING-{Guid.NewGuid().ToString().Substring(0, 8)}";

                        _fileTracker.TrackProcessed(
                            file.FullPath,
                            tempHash,
                            file.SizeBytes,
                            file.LastWriteTime,
                            bestProposal.PluginName,
                            finalPath,
                            "Processing" // 状态改为 Processing
                        );

                        // 立即执行插件回调（使用临时哈希）
                        // Immediately execute plugin callback (use temporary hash)
                        if (bestProposal.OnProcessingSuccess != null)
                        {
                            await bestProposal.OnProcessingSuccess(file, finalPath, tempHash);
                        }

                        // 后台异步计算真实哈希
                        // Background async compute real hash
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                string realHash = await ComputeHashAsync(finalPath);

                                _logger.LogInformation("File {FileName} hash computed: {Hash}", file.Name, realHash);

                                // 更新哈希到数据库
                                // Update hash in database
                                _fileTracker.UpdateHash(file.FullPath, realHash);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to compute hash for file: {Path}", finalPath);
                            }
                        });
                    }
                }

                return result;
            }
            catch (SmartException sex)
            {
                // 记录失败一般不标记为"已处理"，除非致命不想重试
                // Failures usually not marked "processed" unless fatal and don't want retry
                // 给下次重试的机会
                // Give a chance to retry next time
                _logger.LogError(sex, "SmartException in ProcessFileAsync: {Code}", sex.Code);
                return OperationResult.Fail(sex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ProcessFileAsync");
                throw new SmartException(ErrorCode.UnknownError, "Internal Error", ex);
            }
        }

        /// <summary>
        /// 处理一批文件：建立上下文 -> 竞价 -> 执行
        /// Process a batch of files: Build Context -> Bid -> Execute
        /// </summary>
        public async Task<OperationResult> ProcessBatchAsync(IEnumerable<IFileEntry> files)
        {
            var fileList = files.ToList();
            if (fileList.Count == 0) return OperationResult.Success();

            _logger.LogInformation("Starting batch processing for {Count} files", fileList.Count);

            // 阶段 0: 验证过滤
            // Phase 0: Validate & Filter
            var validFiles = new List<IFileEntry>();
            var ignoredExtensions = await _settings.GetIgnoredExtensionsAsync(); // Cache this call

            foreach (var file in fileList)
            {
                // 1. 检查忽略列表
                // 1. Check Ignore List
                if (ignoredExtensions.Contains(file.Extension)) continue;

                // 2. 检查历史记录 (优化：跳过已处理的文件)
                // 2. Check History (Optimization: Skip analysis for already processed files)
                if (_fileTracker.IsProcessed(file.FullPath, file.LastWriteTime, file.SizeBytes))
                {
                     // Typically silent skip in batch to avoid noise, unless verbose logging needed
                     continue;
                }

                // 3. 稳定性检查
                // 3. Stability Check
                if (!await IsFileStableAsync(file))
                {
                     _logger.LogWarning("Skipping unstable file in batch: {Path}", file.FullPath);
                     continue;
                }
                validFiles.Add(file);
            }

            if (validFiles.Count == 0) return OperationResult.Success("No valid files to process in batch");

            // 阶段 1: 建立上下文
            // Phase 1: Context Analysis
            var batchId = Guid.NewGuid().ToString();
            var context = new BatchContext(batchId, validFiles);
            await _pluginManager.AnalyzeBatchAsync(context);

            // Phase 2: Process Individually (but now plugins have context)
            // We can optimize this loop to run in parallel if needed, but safe context implies sequential execution usually unless SafeContext supports concurrent.
            // Bidding can be concurrent. Moving should be sequential to avoid disk thrashing?
            // Let's do Bidding Concurrent, Moving Sequential.

            // Actually, we reuse ProcessFileAsync logic but skip Analysis? No, ProcessFileAsync calls GetBestRoute, which plugins now handle smartly.
            // BUT ProcessFileAsync does "IsFileStable" check again. It's fine (double check is safe).
            // However, ProcessFileAsync doesn't know about batch context created in PluginManager implicitly?
            // Plugins store context in THEIR memory keyed by batch ID?
            // OR Plugins just updated their internal state "recently".
            // Since PluginManager is Singleton and Plugins are Singletons (in this architecture),
            // If we run multiple batches in parallel, we have a RACE CONDITION on member variables in plugins.
            // CAUTION: Plugins must handle AnalyzeBatchAsync in a thread-safe way or likely just use it for "Current Batch".
            // If we process one batch at a time, it's fine.
            // For now, assume sequential batch processing.

            _safeContext.BeginBatch($"Batch-{batchId.Substring(0,8)}");

            foreach (var file in validFiles)
            {
                // We call GetBestRouteAsync directly here to avoid re-doing checks?
                // Or just call ProcessFileAsync and let it flow.
                // ProcessFileAsync does checks we already did (Ignored, Stable), but mostly harmless.
                // Let's call ProcessFileAsync to reuse logic.
                await ProcessFileAsync(file);
            }

            _safeContext.CommitBatch();

            return OperationResult.Success($"Processed batch of {validFiles.Count} files");
        }

        /// <summary>
        /// 智能检查文件是否稳定
        /// Intelligently check if file is stable
        /// </summary>
        private async Task<bool> IsFileStableAsync(IFileEntry file)
        {
            try
            {
                var fileInfo = new FileInfo(file.FullPath);

                // 策略1：检查文件修改时间
                // Strategy 1: Check file last write time
                // 如果文件在 1 秒前就停止修改，直接认为是稳定的
                // If file stopped writing 1 second ago, directly consider it stable
                if ((DateTime.Now - fileInfo.LastWriteTime).TotalSeconds > 1)
                {
                    // 仍然尝试打开文件以确保可访问
                    // Still try to open file to ensure accessibility
                    try
                    {
                        using (var stream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            return true;
                        }
                    }
                    catch (IOException)
                    {
                        _logger.LogDebug("File {Path} is old but locked", file.FullPath);
                        return false;
                    }
                }

                // 策略2：对于最近修改的文件，快速检查大小变化
                // Strategy 2: For recently modified files, quick size check
                long size1 = fileInfo.Length;
                await Task.Delay(100); // 只等待 100ms / Only wait 100ms
                long size2 = fileInfo.Length;

                if (size1 == size2)
                {
                    // 大小稳定，尝试打开文件
                    // Size stable, try to open file
                    try
                    {
                        using (var stream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            return true;
                        }
                    }
                    catch (IOException ex)
                    {
                        _logger.LogWarning(ex, "File {Path} size stable but locked", file.FullPath);
                        return false;
                    }
                }

                // 大小仍在变化，不稳定
                // Size still changing, not stable
                _logger.LogDebug("File {Path} size changed: {Size1} -> {Size2}", file.FullPath, size1, size2);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file stability: {Path}", file.FullPath);
                return false;
            }
        }
    }
}
