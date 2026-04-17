# SmartFileMan 📂✨

**SmartFileMan** is a modular and intelligent file management system built with .NET 10 and MAUI. It goes beyond simple rule-based sorting by utilizing a "Bidding Pipeline" architecture, allowing different plugins to automatically and intelligently organize your disk space based on file metadata and contextual associations.

**SmartFileMan** 是一个基于 .NET 10 和 MAUI 构建的模块化智能文件管理系统。它超越了简单的规则分类，通过“竞价流水线”架构，允许不同插件根据文件元数据和上下文关联，自动、智能地整理你的磁盘空间。

## 🌟 Core Features | 核心特性

*   **Intelligent Bidding Pipeline (智能竞价流水线)**: Plugins actively "bid" based on file characteristics instead of passive filtering. The system automatically mediates the best archive path through a scoring algorithm.
*   **Context Awareness (全景分析)**: Supports batch processing. Plugins scan the entire folder first (Phase 0) to identify implicit relationships between files (e.g., game saves and their preview screenshots, multi-volume archives).
*   **Incremental Processing & Duplicate Detection (增量处理与分身识别)**: Built-in `FileTracker` engine. Automatically skips unchanged files via hash and metadata comparison, tracking files accurately even if renamed.
*   **High Modularity (高度模块化)**: Fully isolated plugins supporting **hot-swapping**. Easily add plugins for image recognition, invoice archiving, or music organization on demand.
*   **Developer Friendly (开发者友好)**: Provides an RSA plugin signature system, built-in LiteDB isolated storage, real-time log console, and database SQL inspector.
*   **Cross-platform Foundation (跨平台基础)**: Built on .NET MAUI, natively supporting multi-language (English/Chinese) interfaces and documentation.

## 🏗️ Project Architecture | 项目架构

*   `SmartFileMan.Contracts`: Core interface definitions, standardizing communication between plugins and the system.
*   `SmartFileMan.Sdk`: Developer toolkit containing `PluginBase`, `SafeContext`, and common file operation commands.
*   `SmartFileMan.Core`: The central brain of the system, responsible for plugin loading, file monitoring, bid mediation, and scheduling.
*   `SmartFileMan.App`: .NET MAUI-based desktop application providing seamless UI interaction and visual data management.
*   `SmartFileMan.Signer`: Security signing tool to ensure only safe and reliable plugins are loaded.

## 🚀 Quick Start | 快速开始

### For Users | 用户
1. Download and run `SmartFileMan.App` (下载并运行 `SmartFileMan.App`).
2. Configure the folders you want to monitor in "Settings" (在“设置”中配置你要监控的文件夹).
3. Drop files in and watch the system automatically categorize them (放入文件，观察系统自动分类).

### For Developers | 开发者
Read our [Developer Guide / 开发者指南 (DEVELOPER_GUIDE.md)](DEVELOPER_GUIDE.md) to start writing your first smart organization plugin!

## 🛡️ Security | 安全性

By default, the system only loads plugins with a valid digital signature. To develop and test, please enable "Developer Mode" in the settings.
系统默认仅加载拥有合法数字签名的插件。如需开发和测试，请在“设置”中开启“开发者模式”。

---

**SmartFileMan** - Let file management step into a new era of automation and intelligence. (让文件管理进入自动化、智能化的新时代)
# SmartFileMan 📂✨

**SmartFileMan** is a modular and intelligent file management system built with .NET 10 and MAUI. It goes beyond simple rule-based sorting by utilizing a "Bidding Pipeline" architecture, allowing different plugins to automatically and intelligently organize your disk space based on file metadata and contextual associations.

## 🌟 Core Features | 核心特性

*   **Intelligent Bidding Pipeline (智能竞价流水线)**: Plugins actively "bid" based on file characteristics instead of passive filtering. The system automatically mediates the best archive path through a scoring algorithm.
*   **Context Awareness (全景分析)**: Supports batch processing. Plugins scan the entire folder first (Phase 0) to identify implicit relationships between files (e.g., game saves and their preview screenshots, multi-volume archives).
*   **Incremental Processing & Duplicate Detection (增量处理与分身识别)**: Built-in FileTracker engine. Automatically skips unchanged files via hash and metadata comparison, tracking files accurately even if renamed.
*   **High Modularity (高度模块化)**: Fully isolated plugins supporting **hot-swapping**. Easily add plugins for image recognition, invoice archiving, or music organization on demand.
*   **Developer Friendly (开发者友好)**: Provides an RSA plugin signature system, built-in LiteDB isolated storage, real-time log console, and database SQL inspector.
*   **Cross-platform Foundation (跨平台基础)**: Built on .NET MAUI, natively supporting multi-language (English/Chinese) interfaces and documentation.

## 🏗️ Project Architecture | 项目架构

*   SmartFileMan.Contracts: Core interface definitions, standardizing communication between plugins and the system.
*   SmartFileMan.Sdk: Developer toolkit containing PluginBase, SafeContext, and common file operation commands.
*   SmartFileMan.Core: The central brain of the system, responsible for plugin loading, file monitoring, bid mediation, and scheduling.
*   SmartFileMan.App: .NET MAUI-based desktop application providing seamless UI interaction and visual data management.
*   SmartFileMan.Signer: Security signing tool to ensure only safe and reliable plugins are loaded.

## 🚀 Quick Start | 快速开始

### For Users | 用户
1. Download and run SmartFileMan.App (下载并运行 SmartFileMan.App).
2. Configure the folders you want to monitor in "Settings" (在“设置”中配置你要监控的文件夹).
3. Drop files in and watch the system automatically categorize them (放入文件，观察系统自动分类).

### For Developers | 开发者
Read our [Developer Guide / 开发者指南 (DEVELOPER_GUIDE.md)](DEVELOPER_GUIDE.md) to start writing your first smart organization plugin!

## 🛡️ Security | 安全性

By default, the system only loads plugins with a valid digital signature. To develop and test, please enable "Developer Mode" in the settings.
系统默认仅加载拥有合法数字签名的插件。如需开发和测试，请在“设置”中开启“开发者模式”。

---
**SmartFileMan** - Let file management step into a new era of automation and intelligence. (让文件管理进入自动化、智能化的新时代)
