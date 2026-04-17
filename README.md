# SmartFileMan 📂✨

English | [简体中文](README_ZH.md)

**SmartFileMan** is a modular and intelligent file management system built with .NET 10 and MAUI. It goes beyond simple rule-based sorting by utilizing a "Bidding Pipeline" architecture, allowing different plugins to automatically and intelligently organize your disk space based on file metadata and contextual associations.

## 🌟 Core Features

*   **Intelligent Bidding Pipeline**: Plugins actively "bid" based on file characteristics instead of passive filtering. The system automatically mediates the best archive path through a scoring algorithm.
*   **Context Awareness**: Supports batch processing. Plugins scan the entire folder first (Phase 0) to identify implicit relationships between files (e.g., game saves and their preview screenshots, multi-volume archives).
*   **Incremental Processing & Duplicate Detection**: Built-in `FileTracker` engine. Automatically skips unchanged files via hash and metadata comparison, tracking files accurately even if renamed.
*   **High Modularity**: Fully isolated plugins supporting **hot-swapping**. Easily add plugins for image recognition, invoice archiving, or music organization on demand.
*   **Developer Friendly**: Provides an RSA plugin signature system, built-in LiteDB isolated storage, real-time log console, and database SQL inspector.
*   **Cross-platform Foundation**: Built on .NET MAUI, natively supporting multi-language (English/Chinese) interfaces and documentation.

## 🏗️ Project Architecture

*   `SmartFileMan.Contracts`: Core interface definitions, standardizing communication between plugins and the system.
*   `SmartFileMan.Sdk`: Developer toolkit containing `PluginBase`, `SafeContext`, and common file operation commands.
*   `SmartFileMan.Core`: The central brain of the system, responsible for plugin loading, file monitoring, bid mediation, and scheduling.
*   `SmartFileMan.App`: .NET MAUI-based desktop application providing seamless UI interaction and visual data management.
*   `SmartFileMan.Signer`: Security signing tool to ensure only safe and reliable plugins are loaded.

## 🚀 Quick Start

### For Users
1. Download and run `SmartFileMan.App`.
2. Configure the folders you want to monitor in "Settings".
3. Drop files in and watch the system automatically categorize them.

### For Developers
Read our [Developer Guide (English)](DEVELOPER_GUIDE_EN.md) to start writing your first smart organization plugin!

## 🛡️ Security

By default, the system only loads plugins with a valid digital signature. To develop and test, please enable "Developer Mode" in the settings.

---

**SmartFileMan** - Let file management step into a new era of automation and intelligence.
