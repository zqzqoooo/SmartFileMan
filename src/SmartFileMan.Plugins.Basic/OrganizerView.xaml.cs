using Microsoft.Maui.Controls;
using SmartFileMan.Contracts;

namespace SmartFileMan.Plugins.Basic
{
    public partial class OrganizerView : ContentView
    {
        private readonly IPluginStorage _storage;

        // 构造函数接收 Storage
        public OrganizerView(IPluginStorage storage)
        {
            InitializeComponent();
            _storage = storage;

            // 加载数据
            LoadData();
        }

        private void LoadData()
        {
            if (_storage != null)
            {
                // 从数据库读取 "TotalOrganizedCount"
                int count = _storage.Load<int>("TotalOrganizedCount", 0);
                CountLabel.Text = count.ToString();
            }
        }

        private void OnRefreshClicked(object sender, EventArgs e)
        {
            LoadData();
        }
    }
}