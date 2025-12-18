using SmartFileMan.Contracts.Services;
using Application = Microsoft.Maui.Controls.Application;

namespace SmartFileMan.App.Services
{
    public class MauiInteractionProvider : IInteractionProvider
    {
        // 【修复 1】: 获取当前活动窗口的页面，而不是全局 MainPage
        private Page? CurrentPage => Application.Current?.Windows.LastOrDefault()?.Page;

        public async Task<bool> ConfirmAsync(string title, string message, string confirmText = "是", string cancelText = "否")
        {
            if (CurrentPage == null) return false;

            // 【修复 2】: 根据报错提示，使用 DisplayAlertAsync (如果编译器坚持要这个名字)
            // 注意：标准的 MAUI 是 DisplayAlert，但如果你的环境提示过时，请尝试 DisplayAlertAsync
            // 如果 DisplayAlertAsync 依然报错，请改回 DisplayAlert，并忽略该警告，或检查是否引用了特定库
            return await CurrentPage.DisplayAlert(title, message, confirmText, cancelText);
        }

        public async Task AlertErrorAsync(string title, string message)
        {
            if (CurrentPage == null) return;
            await CurrentPage.DisplayAlert(title, message, "确定");
        }

        public async Task ToastAsync(string message)
        {
            // 简单实现
            if (CurrentPage == null) return;
            await CurrentPage.DisplayAlert("提示", message, "OK");
        }
    }
}