using System.Collections.ObjectModel;
using System.Windows.Input;
using GetDevice.Models;
using GetDevice.Services;

namespace GetDevice.ViewModels;

public class DeviceFieldItem : BaseViewModel
{
    private bool _isChecked;

    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public bool IsChecked
    {
        get => _isChecked;
        set => SetProperty(ref _isChecked, value);
    }
}

public class MainViewModel : BaseViewModel
{
    private readonly IDeviceInfoService _deviceInfoService;
    private readonly IExportService _exportService;
    private readonly IConfigService _configService;
    private readonly IPasswordService _passwordService;
    private readonly IHttpServerService _httpServerService;

    private DeviceInfo? _deviceInfo;
    private bool _isHttpRunning;
    private string _httpStatusText = string.Empty;

    public ObservableCollection<DeviceFieldItem> Fields { get; } = new();
    public ICommand ExportCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand SelectNoneCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ToggleApiCommand { get; }

    public bool IsHttpRunning
    {
        get => _isHttpRunning;
        set => SetProperty(ref _isHttpRunning, value);
    }

    public string HttpStatusText
    {
        get => _httpStatusText;
        set => SetProperty(ref _httpStatusText, value);
    }

    public string HttpToggleText => _isHttpRunning ? "Stop" : "Start";

    public event Action? OpenSettingsRequested;

    public MainViewModel(
        IDeviceInfoService deviceInfoService,
        IExportService exportService,
        IConfigService configService,
        IPasswordService passwordService,
        IHttpServerService httpServerService)
    {
        _deviceInfoService = deviceInfoService;
        _exportService = exportService;
        _configService = configService;
        _passwordService = passwordService;
        _httpServerService = httpServerService;

        ExportCommand = new RelayCommand(ExecuteExport);
        SelectAllCommand = new RelayCommand(_ => SetAllChecked(true));
        SelectNoneCommand = new RelayCommand(_ => SetAllChecked(false));
        OpenSettingsCommand = new RelayCommand(_ => OpenSettingsRequested?.Invoke());
        RefreshCommand = new RelayCommand(_ => LoadDeviceInfo());
        ToggleApiCommand = new RelayCommand(_ =>
        {
            if (_httpServerService.IsRunning)
                _httpServerService.Stop();
            else
                _httpServerService.Start(_configService.Load().HttpPort);
        });

        IsHttpRunning = _httpServerService.IsRunning;
        HttpStatusText = _httpServerService.IsRunning
            ? $"localhost:{_configService.Load().HttpPort}"
            : "Stopped";

        _httpServerService.RunningChanged += (_, running) =>
        {
            IsHttpRunning = running;
            HttpStatusText = running
                ? $"localhost:{_configService.Load().HttpPort}"
                : "Stopped";
            OnPropertyChanged(nameof(HttpToggleText));
            if (System.Windows.Application.Current is not null)
                CommandManager.InvalidateRequerySuggested();
        };

        if (_configService.Load().HttpEnabled && !_httpServerService.IsRunning)
            _httpServerService.Start(_configService.Load().HttpPort);

        LoadDeviceInfo();
    }

    public void LoadDeviceInfo()
    {
        _deviceInfo = _deviceInfoService.Gather();
        var config = _configService.Load();

        var fieldDefs = new Dictionary<string, (string Label, Func<DeviceInfo, string>)>
        {
            ["device_id"] = ("Device ID", i => i.DeviceId),
            ["device_name"] = ("Device Name", i => i.DeviceName),
            ["client_key"] = ("Client Key", i => i.ClientKey),
            ["hostname"] = ("Hostname", i => i.Hostname),
            ["os"] = ("OS", i => i.Os),
            ["arch"] = ("Architecture", i => i.Arch),
            ["mac_address"] = ("MAC Address", i => i.MacAddress),
            ["ip_address"] = ("IP Address", i => i.IpAddress),
            ["timestamp"] = ("Timestamp", i => i.Timestamp),
        };

        Fields.Clear();
        foreach (var (key, (label, valueSelector)) in fieldDefs)
        {
            Fields.Add(new DeviceFieldItem
            {
                Key = key,
                Label = label,
                Value = valueSelector(_deviceInfo),
                IsChecked = config.CheckedFields.Contains(key)
            });
        }
    }

    private void SetAllChecked(bool isChecked)
    {
        foreach (var field in Fields)
            field.IsChecked = isChecked;
    }

    private async void ExecuteExport(object? parameter)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Device Info",
            Filter = "JSON files (*.json)|*.json",
            DefaultExt = "json",
            FileName = "GetDevice-export.json"
        };

        if (dialog.ShowDialog() != true)
            return;

        var checkedFields = Fields.Where(f => f.IsChecked).Select(f => f.Key).ToList();

        var config = _configService.Load();
        config.CheckedFields = checkedFields;
        _configService.Save(config);

        if (_deviceInfo == null)
            _deviceInfo = _deviceInfoService.Gather();

        await _exportService.ExportToFileAsync(_deviceInfo, dialog.FileName, checkedFields);
    }
}
