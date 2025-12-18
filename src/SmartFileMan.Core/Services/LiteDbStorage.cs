using LiteDB;
using SmartFileMan.Contracts;
using System.IO;

namespace SmartFileMan.Core.Services
{
    /// <summary>
    /// 基于 LiteDB 的插件存储实现
    /// </summary>
    public class LiteDbStorage : IPluginStorage
    {
        private readonly LiteDatabase _db;
        private readonly string _pluginId;

        public LiteDbStorage(LiteDatabase db, string pluginId)
        {
            _db = db;
            _pluginId = pluginId;
        }

        public void Save<T>(string key, T value)
        {
            // 使用插件 ID 作为 Collection 名称，实现数据隔离
            var col = _db.GetCollection<EntryWrapper<T>>(_pluginId);
            
            // Upsert: 如果存在则更新，不存在则插入
            col.Upsert(new EntryWrapper<T> { Id = key, Value = value });
        }

        public T? Load<T>(string key, T? defaultValue = default)
        {
            var col = _db.GetCollection<EntryWrapper<T>>(_pluginId);
            var entry = col.FindById(key);
            
            if (entry != null)
            {
                return entry.Value;
            }
            
            return defaultValue;
        }

        // 简单的包装类，用于存储各种类型的数据 (包括基本类型 int, string 等)
        private class EntryWrapper<T>
        {
            [BsonId]
            public string Id { get; set; } = string.Empty;
            public T? Value { get; set; }
        }
    }
}