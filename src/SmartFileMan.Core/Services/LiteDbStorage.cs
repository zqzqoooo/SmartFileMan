using LiteDB;
using SmartFileMan.Contracts.Storage;
using System.IO;

namespace SmartFileMan.Core.Services
{
    /// <summary>
    /// 基于 LiteDB 的插件存储实现
    /// Plugin storage implementation based on LiteDB
    /// </summary>
    public class LiteDbStorage : IPluginStorage
    {
        private readonly LiteDatabase _db;
        private readonly string _pluginId;

        // 构造函数：初始化数据库实例和插件标识
        // Constructor: Initialize database instance and plugin identifier
        public LiteDbStorage(LiteDatabase db, string pluginId)
        {
            _db = db;
            _pluginId = pluginId;
        }

        /// <summary>
        /// 保存数据到存储
        /// Save data to storage
        /// </summary>
        /// <typeparam name="T">数据类型 / Type of data</typeparam>
        /// <param name="key">键名 / Storage key</param>
        /// <param name="value">键值 / Storage value</param>
        public void Save<T>(string key, T value)
        {
            // 使用插件 ID 作为集合名称，但需要移除无效字符 (如 .)
            // Use Plugin ID as the collection name, but remove invalid chars (like .)
            // LiteDB only allows [a-Z$_]
            string safeCollectionName = _pluginId.Replace(".", "_").Replace("-", "_");
            
            var col = _db.GetCollection<EntryWrapper<T>>(safeCollectionName);

            // 更新或插入：如果键存在则更新，不存在则插入
            // Upsert: Update if the key exists, otherwise insert
            col.Upsert(new EntryWrapper<T> { Id = key, Value = value });
        }

        /// <summary>
        /// 从存储加载数据
        /// Load data from storage
        /// </summary>
        /// <typeparam name="T">数据类型 / Type of data</typeparam>
        /// <param name="key">键名 / Storage key</param>
        /// <param name="defaultValue">默认值（可选）/ Default value (optional)</param>
        /// <returns>加载的数据或默认值 / Loaded data or default value</returns>
        public T? Load<T>(string key, T? defaultValue = default)
        {
            string safeCollectionName = _pluginId.Replace(".", "_").Replace("-", "_");
            var col = _db.GetCollection<EntryWrapper<T>>(safeCollectionName);
            var doc = col.FindById(key);
            
            if (doc != null)
            {
                return doc.Value;
            }
            return defaultValue;
        }

        /// <summary>
        /// 内部包装类：用于存储各种类型的数据，包括基本类型
        /// Internal wrapper class: Used to store various types of data, including primitive types
        /// </summary>
        /// <typeparam name="T">被包装的数据类型 / Wrapped data type</typeparam>
        private class EntryWrapper<T>
        {
            [BsonId] // 指定 LiteDB 的主键 / Specify the primary key for LiteDB
            public string Id { get; set; } = string.Empty;

            public T? Value { get; set; }
        }
    }
}