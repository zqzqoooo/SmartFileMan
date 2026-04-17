using System.Threading.Tasks;

namespace SmartFileMan.Contracts.Services
{
    public interface IInteractionProvider
    {
        // 弹出确认框 (例如: "确定要删除这 5 个文件吗？")
        Task<bool> ConfirmAsync(string title, string message, string confirmText = "Yes", string cancelText = "No");

        // 通知消息 (例如: "操作成功")
        Task ToastAsync(string message);

        // 错误提示
        Task AlertErrorAsync(string title, string message);
    }
}