# SmartFileMan 工作流程与架构

## 概述
SmartFileMan 是一个智能文件整理工具，采用基于插件的架构来扫描、分类和移动文件。典型的工作流程包括监控文件夹、检测文件、插件竞价（Bidding）以及最终执行。

## 文件处理流程

1.  **监控 (FileWatcherService)**
    *   系统监控用户配置的文件夹。
    *   当文件被创建或重命名时，它会被添加到一个防抖队列中。
    *   经过短暂的稳定期（例如 1 秒）后，一批文件会被发送进行处理。

2.  **验证与去重 (FileManager)**
    *   **忽略列表**: 检查扩展名是否在忽略列表中（系统设置）。
    *   **历史检查**: 查询 `FileTracker` (LiteDB) 以查看该特定文件 (路径 + 修改时间 + 大小) 是否已被处理过。
        *   如果匹配，则跳过（增量更新）。
    *   **稳定性检查**: 确保文件未被锁定或当前正在写入（通过延迟后的大小检查）。

3.  **竞价流水线 (PluginManager)**
    *   `PluginManager` 将文件广播给所有已启用的插件。
    *   **阶段 1: 检测**: 插件观察文件（例如加载元数据）。
    *   **阶段 2: 竞价**: 每个插件返回一个 `RouteProposal` (整理提案):
        *   `Score` (0-100): 插件的自信程度。
        *   `DestinationPath`: 插件想要移动文件的目标路径。
        *   `Explanation`: 理由说明。
    *   **仲裁**: 系统选择得分最高的提案。

4.  **执行 (FileManager)**
    *   **安全移动**: 系统将文件移动到获胜的目标路径。
    *   **哈希计算**: 成功后，计算最终文件的 SHA256 哈希值。
    *   **回调执行**: 调用获胜插件提供的 `OnProcessingSuccess` 委托（如果定义）。
    *   **追踪**: 事务被记录在 `FileTracker` 中:
        *   `OriginalPath`: 来源路径。
        *   `FileHash`: 内容签名。
        *   `ResponsiblePluginId`: 执行移动的插件 ID。
        *   `NewPath`: 新路径。
        *   `Status`: 成功状态。

## 数据库模式 (LiteDB)
*   **集合**: `file_tracker`
*   **字段**:
    *   `_id` (String): 原始文件路径 (主键)
    *   `OriginalPath` (String)
    *   `FileHash` (String)
    *   `SizeBytes` (Int64)
    *   `LastWriteTime` (DateTime)
    *   `ResponsiblePluginId` (String)
    *   `NewPath` (String)
    *   `Status` (String)
    *   `ProcessedAt` (DateTime)

## 插件架构
*   **隔离性**: 插件在主进程中运行，但通过严格的接口 (`IFilePlugin`) 操作。
*   **存储**: 插件通过 `IPluginStorage` 拥有隔离的存储（支持前缀的 LiteDB 集合）。
*   **UI**: 插件可以提供自定义 UI（例如 `GetView()`），并在主应用中渲染。
*   **无框架依赖**: 插件不应直接依赖主应用的 UI 库（如 `CommunityToolkit.MediaElement`），以保持低耦合，或者必须小心管理依赖项。

## 日志
日志存储在 `%AppData%\SmartFileMan\logs\` 中，提供以下详细跟踪信息：
*   文件检测事件。
*   竞价结果（获胜者和得分）。
*   哈希计算和数据库更新。
*   错误和异常。
