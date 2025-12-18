namespace SmartFileMan.Contracts
{
    /// <summary>
    /// 插件的专属存储接口
    /// 类似于一个简单的 Key-Value 数据库
    /// </summary>
    public interface IPluginStorage
    {
        /// <summary>
        /// 保存数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">键名</param>
        /// <param name="value">值</param>
        void Save<T>(string key, T value);

        /// <summary>
        /// 读取数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>读取到的值</returns>
        T? Load<T>(string key, T? defaultValue = default);
        
        // 以后可以扩展：Delete, Clear 等
    }
}
