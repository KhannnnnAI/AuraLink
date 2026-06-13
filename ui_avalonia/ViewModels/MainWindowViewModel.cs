using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ui_avalonia.Services;

namespace ui_avalonia.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ViewModelBase _currentPage;

        public DashboardViewModel Dashboard { get; } = new();
        public IpChangerViewModel IpChanger { get; } = new();
        public DnsManagerViewModel DnsManager { get; } = new();
        public ProfileManagerViewModel ProfileManager { get; } = new();
        public SchedulerViewModel Scheduler { get; } = new();
        public ToolsViewModel Tools { get; } = new();
        public HistorySettingsViewModel HistorySettings { get; } = new();
        public DnsBenchmarkViewModel DnsBenchmark { get; } = new();

        public Services.LocalizationService Local => Services.LocalizationService.Instance;

        public bool IsDashboardActive => CurrentPage == Dashboard;
        public bool IsIpChangerActive => CurrentPage == IpChanger;
        public bool IsDnsManagerActive => CurrentPage == DnsManager;
        public bool IsDnsBenchmarkActive => CurrentPage == DnsBenchmark;
        public bool IsProfileManagerActive => CurrentPage == ProfileManager;
        public bool IsSchedulerActive => CurrentPage == Scheduler;
        public bool IsToolsActive => CurrentPage == Tools;
        public bool IsHistorySettingsActive => CurrentPage == HistorySettings;

        partial void OnCurrentPageChanged(ViewModelBase value)
        {
            OnPropertyChanged(nameof(IsDashboardActive));
            OnPropertyChanged(nameof(IsIpChangerActive));
            OnPropertyChanged(nameof(IsDnsManagerActive));
            OnPropertyChanged(nameof(IsDnsBenchmarkActive));
            OnPropertyChanged(nameof(IsProfileManagerActive));
            OnPropertyChanged(nameof(IsSchedulerActive));
            OnPropertyChanged(nameof(IsToolsActive));
            OnPropertyChanged(nameof(IsHistorySettingsActive));
        }

        public MainWindowViewModel()
        {
            _currentPage = Dashboard;
        }

        [RelayCommand]
        private void Navigate(string viewName)
        {
            if (viewName == "Exit")
            {
                SchedulerService.Instance.Stop();
                Environment.Exit(0);
                return;
            }

            CurrentPage = viewName switch
            {
                "Dashboard" => Dashboard,
                "IpChanger" => IpChanger,
                "DnsManager" => DnsManager,
                "ProfileManager" => ProfileManager,
                "Scheduler" => Scheduler,
                "Tools" => Tools,
                "HistorySettings" => HistorySettings,
                "DnsBenchmark" => DnsBenchmark,
                _ => Dashboard
            };

            // Tự động làm mới dữ liệu khi di chuyển tab
            if (CurrentPage == Dashboard) Dashboard.RefreshCards();
            else if (CurrentPage == IpChanger) IpChanger.RefreshCards();
            else if (CurrentPage == DnsManager) DnsManager.RefreshCards();
            else if (CurrentPage == ProfileManager)
            {
                ProfileManager.RefreshCards();
                ProfileManager.RefreshProfiles();
            }
            else if (CurrentPage == HistorySettings) HistorySettings.RefreshLogs();
            else if (CurrentPage == Tools)
            {
                if (Tools.AvailableServers.Count == 0)
                {
                    _ = Tools.LoadServersAsync();
                }
                if (Tools.PublicIp == "N/A" || string.IsNullOrEmpty(Tools.PublicIp))
                {
                    _ = Tools.QueryGeoIpAsync();
                }
            }
        }
    }
}
