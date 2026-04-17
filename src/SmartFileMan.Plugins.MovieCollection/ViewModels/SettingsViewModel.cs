using System;
using SmartFileMan.Plugins.MovieCollection.Models;
using SmartFileMan.Plugins.MovieCollection.Services;

namespace SmartFileMan.Plugins.MovieCollection.ViewModels;

/// <summary>
/// 设置页视图模型
/// Settings page view model
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly ICacheService _cacheService;

    private string _apiKey = string.Empty;
    public string ApiKey
    {
        get => _apiKey;
        set => SetProperty(ref _apiKey, value);
    }

    private string _renameTemplate = "{title} S{season:00}E{episode:00}";
    public string RenameTemplate
    {
        get => _renameTemplate;
        set => SetProperty(ref _renameTemplate, value);
    }

    private string? _targetFolder;
    public string? TargetFolder
    {
        get => _targetFolder;
        set => SetProperty(ref _targetFolder, value);
    }

    private string _language = "zh-CN";
    public string Language
    {
        get => _language;
        set => SetProperty(ref _language, value);
    }

    private bool _autoProcess;
    public bool AutoProcess
    {
        get => _autoProcess;
        set => SetProperty(ref _autoProcess, value);
    }

    private bool _isSaving;
    public bool IsSaving
    {
        get => _isSaving;
        set => SetProperty(ref _isSaving, value);
    }

    private string? _statusMessage;
    public string? StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private bool _hasApiKeyError;
    public bool HasApiKeyError
    {
        get => _hasApiKeyError;
        set => SetProperty(ref _hasApiKeyError, value);
    }

    public System.Windows.Input.ICommand SaveCommand { get; }
    public System.Windows.Input.ICommand ClearCacheCommand { get; }

    public event EventHandler? SettingsSaved;

    public SettingsViewModel(ICacheService cacheService)
    {
        _cacheService = cacheService;

        SaveCommand = new Command(SaveSettings);
        ClearCacheCommand = new Command(ClearCache);

        LoadSettings();
    }

    public void LoadSettings()
    {
        var config = _cacheService.GetConfig();
        ApiKey = config.TmdbApiKey;
        RenameTemplate = config.RenameTemplate;
        TargetFolder = config.TargetFolder;
        Language = config.Language;
        AutoProcess = config.AutoProcess;
    }

    public bool ValidateApiKey()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            HasApiKeyError = true;
            StatusMessage = "API Key不能为空";
            return false;
        }

        if (ApiKey.Length < 30)
        {
            HasApiKeyError = true;
            StatusMessage = "API Key格式不正确";
            return false;
        }

        HasApiKeyError = false;
        return true;
    }

    public void SaveSettings()
    {
        if (!ValidateApiKey())
            return;

        IsSaving = true;
        StatusMessage = null;

        try
        {
            var config = new PluginConfig
            {
                TmdbApiKey = ApiKey.Trim(),
                RenameTemplate = RenameTemplate,
                TargetFolder = string.IsNullOrWhiteSpace(TargetFolder) ? null : TargetFolder,
                Language = Language,
                AutoProcess = AutoProcess
            };

            _cacheService.SaveConfig(config);
            _cacheService.ClearExpiredCache();

            StatusMessage = "设置已保存";
            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存失败: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    public void ClearCache()
    {
        try
        {
            _cacheService.ClearExpiredCache();
            StatusMessage = "缓存已清除";
        }
        catch (Exception ex)
        {
            StatusMessage = $"清除缓存失败: {ex.Message}";
        }
    }

    public string GetPreviewFileName()
    {
        return RenameTemplate
            .Replace("{title}", "Breaking Bad")
            .Replace("{season:00}", "01")
            .Replace("{episode:00}", "01")
            .Replace("{season}", "1")
            .Replace("{episode}", "1")
            .Replace("{episode_name}", "Pilot");
    }
}
