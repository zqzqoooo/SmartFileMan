# SmartFileMan 开发者指南

欢迎使用 SmartFileMan 开发框架。这是一个模块化、可扩展的智能文件管理系统。本文档将指导你如何开发插件、使用核心 API、存储数据以及发布安全的插件。

## 目录

1. [架构概览](#1-架构概览)
2. [开发环境设置](#2-开发环境设置)
3. [开发第一个插件](#3-开发第一个插件)
4. [核心 API 参考](#4-核心-api-参考)
5. [插件 UI 开发](#5-插件-ui-开发)
6. [数据存储 (LiteDB)](#6-数据存储-litedb)
7. [插件安全与签名](#7-插件安全与签名)

---

## 1. 架构概览

SmartFileMan 采用分层架构设计：

*   **Contracts (契约层)**: 定义了系统核心接口 (`IPlugin`, `IFileEntry`, `RouteProposal`)。
*   **SDK (工具包)**: 提供了开发者基类 (`PluginBase`) 和安全上下文 (`SafeContext`)。
*   **Core (核心层)**: 实现了插件加载、竞价仲裁 (`PluginManager`) 和文件调度 (`FileManager`)。
*   **App (应用层)**: .NET MAUI 用户界面。

### 核心机制：竞价流水线 (Bidding Pipeline)

系统采用“**观察-竞价**”模式来决定如何处理文件：

1.  **观察 (Observe)**: 所有插件都会收到 `OnFileDetectedAsync` 通知，可以读取文件特征。
2.  **竞价 (Bid)**: 插件通过 `ProposeDestinationAsync` 返回一个“提案” (`RouteProposal`)，包含目标路径和信心分数 (0-100)。
3.  **仲裁 (Arbitrate)**: 系统选择分数最高的提案执行移动操作。

---

## 2. 开发环境设置

### 启用开发者模式

默认情况下，SmartFileMan 仅加载经过数字签名的插件。为了方便开发调试，你可以开启“开发者模式”来加载未签名的插件。

1.  启动 SmartFileMan。
2.  进入 **设置 (Settings)** 页面。
3.  找到 **开发者选项**，开启 **开发者模式 (Developer Mode)**。
4.  **重启应用**以生效。

> **注意**: 开发者模式仅用于测试，生产环境请务必对插件进行签名。

---

## 3. 开发第一个插件

### 步骤 1: 创建项目

创建一个新的 .NET 类库项目 (Class Library)，目标框架建议为 `.NET 10`。
引用 `SmartFileMan.Contracts` 和 `SmartFileMan.Sdk` 项目或 DLL。

### 步骤 2: 继承 `PluginBase`

```csharp
using SmartFileMan.Sdk;
using SmartFileMan.Contracts;
using SmartFileMan.Contracts.Models;
using System.Threading.Tasks;
using System.IO;

public class MyInvoicePlugin : PluginBase
{
    public override string Id => "com.example.invoice";
    public override string DisplayName => "发票归档助手";
    public override string Description => "自动识别并归档发票 PDF";

    // 设置插件类型：Specific (专用) 通常比 General (通用) 权重更高
    public override PluginType Type => PluginType.Specific;

    // 阶段一：观察 (可选)
    public override async Task OnFileDetectedAsync(IFileEntry file)
    {
        // 你可以在这里预加载数据或更新统计
        await Task.CompletedTask;
    }

    // 阶段二：竞价 (核心)
    public override async Task<RouteProposal?> ProposeDestinationAsync(IFileEntry file)
    {
        // 1. 检查是否是 PDF
        if (file.Extension != ".pdf") return null; // 不感兴趣

        // 2. 检查文件名是否包含关键词
        if (file.Name.Contains("发票") || file.Name.Contains("Invoice"))
        {
            // 3. 生成目标路径
            string targetPath = Path.Combine("D:\\Documents\\Invoices", $"{DateTime.Now.Year}", file.Name);
            
            // 4. 返回提案，给出高分 (90分)
            return new RouteProposal(targetPath, 90, "检测到发票关键词");
        }

        return null; // 不处理
    }

    // 旧版接口实现 (如果不需要批量处理，留空即可)
    public override Task ExecuteAsync(IList<IFileEntry> files) => Task.CompletedTask;
}
```

### 步骤 3: 部署测试

1.  编译你的插件项目。
2.  将生成的 `.dll` 文件复制到 SmartFileMan 的 `Plugins` 目录下。
3.  确保已开启 **开发者模式**。
4.  重启 SmartFileMan，在 **插件管理** 页面应能看到你的插件。

---

## 4. 核心 API 参考

在继承 `PluginBase` 的类中，你可以直接调用以下方法。这些操作都是**安全且可撤销**的。

### 文件操作

*   **`Rename(IFileEntry file, string newName)`**
    *   重命名文件。
    *   示例: `await Rename(file, "NewName.txt");`

*   **`Move(IFileEntry file, string destinationFolder)`**
    *   移动文件到指定文件夹。
    *   示例: `await Move(file, "D:\\Archive");`

*   **`Delete(IFileEntry file)`**
    *   **安全删除**：将文件移动到系统的临时回收站 (`SmartFileMan_RecycleBin`)。
    *   支持撤销。
    *   示例: `await Delete(file);`

### `IFileEntry` 对象

代表一个文件，提供比 `FileInfo` 更丰富的抽象：

*   `Id`: 文件唯一标识。
*   `Name`: 文件名 (e.g., "report.pdf").
*   `Extension`: 小写扩展名 (e.g., ".pdf").
*   `FullPath`: 完整路径。
*   `GetHashAsync()`: 获取文件哈希 (SHA256)。
*   `OpenReadAsync()`: 打开读取流。

---

## 5. 插件 UI 开发

如果你的插件需要配置界面或状态展示，可以实现 `IPluginUI` 接口。

```csharp
using SmartFileMan.Contracts;
using Microsoft.Maui.Controls;

public class MyInvoicePlugin : PluginBase, IPluginUI
{
    // ... 其他代码 ...

    public View GetView()
    {
        // 返回一个 MAUI View，例如 ContentView 或 Grid
        // 你可以创建一个 XAML ContentView 并返回其实例
        return new Label { Text = "这是发票插件的设置界面" };
    }
}
```

---

## 6. 数据存储 (LiteDB)

每个插件都自动获得了一个隔离的 NoSQL 存储空间。你不需要关心数据库连接，直接使用 `Storage` 属性即可。

### 保存数据

```csharp
// 保存简单的配置
Storage.Save("LastRunTime", DateTime.Now);

// 保存复杂对象
var config = new MyConfig { AutoSort = true, TargetFolder = "D:\\Docs" };
Storage.Save("UserConfig", config);
```

### 读取数据

```csharp
// 读取配置，如果不存在则返回默认值
var lastRun = Storage.Load<DateTime>("LastRunTime", DateTime.MinValue);

var config = Storage.Load<MyConfig>("UserConfig");
if (config == null) 
{
    // 初始化默认配置
}
```

---

## 7. 插件安全与签名

为了保证系统安全，SmartFileMan 在非开发者模式下会校验插件的数字签名。

### 签名工具 (SmartFileMan.Signer)

我们提供了一个命令行工具 `SmartFileMan.Signer` 用于生成密钥和签名。

#### 生成密钥对

```bash
dotnet run --project src/SmartFileMan.Signer -- keygen Keys
```
这将生成 `private.key` (私钥，请妥善保管) 和 `public.key` (公钥，分发给 App)。

#### 给插件签名

```bash
dotnet run --project src/SmartFileMan.Signer -- sign "Path/To/MyPlugin.dll" "Path/To/private.key"
```
这将生成 `MyPlugin.dll.sig` 文件。

### 自动签名 (推荐)

在你的插件 `.csproj` 文件中添加 `PostBuild` 事件，实现编译后自动签名：

```xml
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
    <Exec Command="dotnet run --project &quot;$(SolutionDir)src\SmartFileMan.Signer\SmartFileMan.Signer.csproj&quot; -- sign &quot;$(TargetPath)&quot; &quot;$(SolutionDir)Keys\private.key&quot;" />
</Target>
```

### 发布

发布插件时，请同时提供：
1.  `MyPlugin.dll`
2.  `MyPlugin.dll.sig`

用户将这两个文件放入 `Plugins` 目录后，SmartFileMan 会自动验证签名并加载插件。

