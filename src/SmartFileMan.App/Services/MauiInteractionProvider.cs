using SmartFileMan.Contracts.Services;
using Application = Microsoft.Maui.Controls.Application;
using Microsoft.Maui.ApplicationModel; // For MainThread
using System.Linq; // For LastOrDefault

namespace SmartFileMan.App.Services
{
    public class MauiInteractionProvider : IInteractionProvider
    {
        private Page? CurrentPage => Application.Current?.Windows.LastOrDefault()?.Page ?? Application.Current?.MainPage;

        public async Task<bool> ConfirmAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel")
        {
            if (CurrentPage == null) return false;

            return await MainThread.InvokeOnMainThreadAsync(async () => 
            {
                return await CurrentPage.DisplayAlert(title, message, confirmText, cancelText);
            });
        }

        public async Task AlertErrorAsync(string title, string message)
        {
            if (CurrentPage == null) return;
            
            await MainThread.InvokeOnMainThreadAsync(async () => 
            {
                await CurrentPage.DisplayAlert(title, message, "OK");
            });
        }

        public async Task ToastAsync(string message)
        {
            if (CurrentPage == null) return;

            // Use MainThread to ensure UI access safety
            // Ideally use CommunityToolkit Toast if available, falling back to Alert for now
            await MainThread.InvokeOnMainThreadAsync(async () => 
            {
                // Simple ALERT for now to ensure visibility in this Phase
                await CurrentPage.DisplayAlert("Notification", message, "OK");
            });
        }
    }
}