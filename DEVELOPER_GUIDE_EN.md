# SmartFileMan Developer Guide

Welcome to the SmartFileMan development framework. This is a modular, extensible, and intelligent file management system. This document will guide you on how to develop plugins, use core APIs, manage data storage, and publish secure plugins.

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Development Environment Setup](#2-development-environment-setup)
3. [Developing Your First Plugin](#3-developing-your-first-plugin)
4. [Core API Reference](#4-core-api-reference)
5. [Plugin UI Development](#5-plugin-ui-development)
6. [Data Storage (LiteDB)](#6-data-storage-litedb)
7. [Plugin Security & Signing](#7-plugin-security--signing)

---

## 1. Architecture Overview

SmartFileMan adopts a layered architecture design:

*   **Contracts**: Defines the core interfaces of the system (`IPlugin`, `IFileEntry`, `RouteProposal`).
*   **SDK**: Provides developer base classes (`PluginBase`) and secure contexts (`SafeContext`).
*   **Core**: Implements plugin loading, bidding arbitration (`PluginManager`), and file scheduling (`FileManager`).
*   **App**: The .NET MAUI user interface.

### Core Mechanism: Batch Bidding Pipeline

The system uses a "**Flattening Input -> Contextual Analysis -> Bidding**" model:

1.  **Flattening**: Folders dragged in by the user are automatically disassembled to extract all files within them, forming a "flattened" Batch.
2.  **Phase 0: Analyze (Context Awareness)**: 
    *   All plugins receive an `AnalyzeBatchAsync(BatchContext)` call.
    *   **Context**: Contains a list of all files in the current batch.
    *   **Purpose**: Plugins can traverse all files at this stage to identify implicit relationships between them (e.g., game saves and preview images, multi-part archives) and build an index in memory.
3.  **Phase 1: Bid**: 
    *   The system asks for proposals file by file (`ProposeDestinationAsync`).
    *   Plugins provide smarter suggestions based on the context built in Phase 0 (e.g., making the preview image follow the save file).
4.  **Arbitrate**: The system selects the proposal with the highest score to execute the move operation.

---

## 2. Development Environment Setup

### Enabling Developer Mode

By default, SmartFileMan only loads plugins with valid digital signatures. To facilitate development and debugging, you can enable "Developer Mode" to load unsigned plugins.

1.  Launch SmartFileMan.
2.  Go to the **Settings** page.
3.  Find **Developer Options** and enable **Developer Mode**.
4.  **Restart the application** for the changes to take effect.

> **Note**: Developer Mode is for testing purposes only. Always sign your plugins before deploying them to production.

---

## 3. Developing Your First Plugin

### Step 1: Create a Project

Create a new .NET Class Library project. The recommended target framework is `.NET 10`.
Add references to the `SmartFileMan.Contracts` and `SmartFileMan.Sdk` projects or DLLs.

### Step 2: Inherit `PluginBase`

```csharp
using SmartFileMan.Sdk;
using SmartFileMan.Contracts;
using SmartFileMan.Contracts.Models;
using System.Threading.Tasks;
using System.IO;

public class MyInvoicePlugin : PluginBase
{
    public override string Id => "com.example.invoice";
    public override string DisplayName => "Invoice Archiver";
    public override string Description => "Automatically identifies and archives invoice PDFs.";

    // Set Plugin Type: Specific generally has a higher weight than General
    public override PluginType Type => PluginType.Specific;

    // Phase 0: Contextual Analysis
    public override async Task AnalyzeBatchAsync(BatchContext context)
    {
        // Traverse all files in this batch to establish relationships
        foreach (var file in context.AllFiles)
        {
            // E.g., record all filenames for subsequent judgments
            _fileNamesInBatch.Add(file.Name);
        }
        await Task.CompletedTask;
    }

    // Phase 1: Observation (Optional)
    public override async Task OnFileDetectedAsync(IFileEntry file)
    {
        // You can preload data or update statistics here
        await Task.CompletedTask;
    }
}
```