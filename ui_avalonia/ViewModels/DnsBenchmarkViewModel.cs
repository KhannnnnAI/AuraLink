using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ui_avalonia.Services;
using FluentIcons.Common;

namespace ui_avalonia.ViewModels
{
    /// <summary>Item để hiển thị và lựa chọn trang web test</summary>
    public class DomainItem : ObservableObject
    {
        public string Label { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public Symbol Icon { get; set; } = Symbol.Globe;
        public string LogoIconKey { get; set; } = string.Empty;
        public string LogoColor { get; set; } = "#FFFFFF";
        public string IconSource { get; set; } = "SimpleIcons"; // "SimpleIcons" hoặc "FontAwesome"

        public bool IsSimpleIcon => IconSource == "SimpleIcons";
        public bool IsFontAwesome => IconSource == "FontAwesome";

        public IconPacks.Avalonia.SimpleIcons.PackIconSimpleIconsKind SimpleIconKind
        {
            get
            {
                if (IsSimpleIcon && Enum.TryParse<IconPacks.Avalonia.SimpleIcons.PackIconSimpleIconsKind>(LogoIconKey, out var result))
                {
                    return result;
                }
                return IconPacks.Avalonia.SimpleIcons.PackIconSimpleIconsKind.None;
            }
        }

        public string Display => $"{Label}";

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    OnPropertyChanged(nameof(LogoColorToShow));
                }
            }
        }

        public string LogoColorToShow => IsSelected ? "#FFFFFF" : LogoColor;
    }

    public partial class DnsBenchmarkViewModel : ViewModelBase
    {
        private readonly DnsBenchmarkService _service = new();
        private CancellationTokenSource? _cts;

        // ─── Danh sách domain cho user chọn ──────────────────────────────
        public ObservableCollection<DomainItem> AvailableDomains { get; } = new();

        [ObservableProperty]
        private DomainItem? _selectedDomain;

        partial void OnSelectedDomainChanged(DomainItem? value)
        {
            if (value == null) return;
            foreach (var item in AvailableDomains)
            {
                item.IsSelected = (item == value);
            }
        }

        // ─── Kết quả đo ──────────────────────────────────────────────────
        public ObservableCollection<DnsBenchmarkResult> Results { get; } = new();

        // ─── Trạng thái UI ───────────────────────────────────────────────
        [ObservableProperty]
        private bool _isBenchmarking;

        [ObservableProperty]
        private string _statusMessage = "Chọn một trang web và nhấn \"Bắt đầu đo\".";

        [ObservableProperty]
        private double _progressValue;

        [ObservableProperty]
        private DnsBenchmarkResult? _bestDns;

        [ObservableProperty]
        private bool _hasBestDns;

        [ObservableProperty]
        private string _applyMessage = string.Empty;

        [ObservableProperty]
        private bool _applySuccess;

        [ObservableProperty]
        private string _selectedInterface = string.Empty;

        [ObservableProperty]
        private string _testingDomainLabel = string.Empty;

        public ObservableCollection<string> Interfaces { get; } = new();

        public LocalizationService Local => LocalizationService.Instance;

        [ObservableProperty]
        private int _selectedGroupIndex = 1;

        public bool IsGroup1Selected => SelectedGroupIndex == 1;
        public bool IsGroup2Selected => SelectedGroupIndex == 2;
        public bool IsGroup3Selected => SelectedGroupIndex == 3;

        partial void OnSelectedGroupIndexChanged(int value)
        {
            OnPropertyChanged(nameof(IsGroup1Selected));
            OnPropertyChanged(nameof(IsGroup2Selected));
            OnPropertyChanged(nameof(IsGroup3Selected));
        }

        public DnsBenchmarkViewModel()
        {
            // Load danh sách domain nhóm 1 làm mặc định
            ChangeGroup(1);

            RefreshInterfaces();
        }

        [RelayCommand]
        public void ChangeGroup(object? param)
        {
            if (param == null) return;
            
            int newGroup = 1;
            if (param is int i) newGroup = i;
            else if (param is string s && int.TryParse(s, out int parsed)) newGroup = parsed;

            if (newGroup < 1 || newGroup > 3) return;

            SelectedGroupIndex = newGroup;

            // Load danh sách tương ứng
            AvailableDomains.Clear();
            var list = newGroup switch
            {
                1 => DnsBenchmarkService.AvailableDomainsGroup1,
                2 => DnsBenchmarkService.AvailableDomainsGroup2,
                3 => DnsBenchmarkService.AvailableDomainsGroup3,
                _ => DnsBenchmarkService.AvailableDomainsGroup1
            };

            foreach (var (label, domain, icon, logoIconKey, logoColor, iconSource) in list)
            {
                AvailableDomains.Add(new DomainItem 
                { 
                    Label = label, 
                    Domain = domain, 
                    Icon = icon,
                    LogoIconKey = logoIconKey,
                    LogoColor = logoColor,
                    IconSource = iconSource
                });
            }

            // Chọn mặc định là trang đầu tiên
            if (AvailableDomains.Count > 0)
                SelectedDomain = AvailableDomains[0];
        }

        [RelayCommand]
        public void SelectDomain(DomainItem? item)
        {
            if (item != null)
                SelectedDomain = item;
        }

        public void RefreshInterfaces()
        {
            Interfaces.Clear();
            var networkService = new NetworkService();
            var ifaces = networkService.GetInterfaces();
            foreach (var iface in ifaces)
                Interfaces.Add(iface.Name);

            if (Interfaces.Count > 0)
                SelectedInterface = Interfaces[0];
        }

        [RelayCommand]
        public async Task StartBenchmarkAsync()
        {
            if (IsBenchmarking)
            {
                _cts?.Cancel();
                return;
            }

            if (SelectedDomain == null)
            {
                StatusMessage = "⚠️ Vui lòng chọn một trang web để test.";
                return;
            }

            _cts = new CancellationTokenSource();
            IsBenchmarking = true;
            ApplySuccess = false;
            ApplyMessage = string.Empty;
            BestDns = null;
            HasBestDns = false;
            Results.Clear();
            ProgressValue = 0;
            TestingDomainLabel = $"{SelectedDomain.Label} ({SelectedDomain.Domain})";
            StatusMessage = $"Đang chuẩn bị đo với {SelectedDomain.Label}...";

            var progress = new Progress<(int current, int total, string message)>(p =>
            {
                ProgressValue = (double)p.current / p.total * 100;
                StatusMessage = p.message;
            });

            try
            {
                var results = await _service.RunBenchmarkAsync(
                    SelectedDomain.Domain,
                    DnsBenchmarkService.WellKnownDns,
                    progress,
                    _cts.Token);

                Results.Clear();
                foreach (var r in results)
                    Results.Add(r);

                if (Results.Count > 0 && Results[0].IsSuccess)
                {
                    BestDns = Results[0];
                    HasBestDns = true;
                    StatusMessage = $"✅ Xong! DNS nhanh nhất cho {SelectedDomain.Label}: {BestDns.Name} — {BestDns.AverageMs} ms";
                }
                else
                {
                    StatusMessage = "⚠️ Không đo được DNS nào. Kiểm tra kết nối mạng.";
                }

                ProgressValue = 100;
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "❌ Đã dừng.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Lỗi: {ex.Message}";
            }
            finally
            {
                IsBenchmarking = false;
            }
        }

        [RelayCommand]
        public async Task ApplyBestDnsAsync()
        {
            if (BestDns == null || string.IsNullOrEmpty(SelectedInterface)) return;

            ApplySuccess = false;
            ApplyMessage = $"Đang áp dụng DNS {BestDns.Name} ({BestDns.PrimaryIp})...";

            try
            {
                var dnsService = new DnsService();
                var (success, error) = await Task.Run(() =>
                    dnsService.ChangeDns(SelectedInterface, BestDns.PrimaryIp, BestDns.SecondaryIp));

                ApplySuccess = success;
                ApplyMessage = success
                    ? $"✅ Đã áp dụng {BestDns.Name} ({BestDns.PrimaryIp} / {BestDns.SecondaryIp}) vào \"{SelectedInterface}\"!"
                    : $"❌ Lỗi: {error}";

                if (success)
                {
                    new LoggerService().LogGeneric(
                        $"[DNS Benchmark] Áp dụng {BestDns.Name} ({BestDns.PrimaryIp}) vào {SelectedInterface} — {BestDns.AverageMs}ms");
                }
            }
            catch (Exception ex)
            {
                ApplyMessage = $"❌ {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task ApplySpecificDnsAsync(DnsBenchmarkResult? dns)
        {
            if (dns == null || !dns.IsSuccess || string.IsNullOrEmpty(SelectedInterface)) return;

            ApplySuccess = false;
            ApplyMessage = $"Đang áp dụng {dns.Name}...";

            try
            {
                var dnsService = new DnsService();
                var (success, error) = await Task.Run(() =>
                    dnsService.ChangeDns(SelectedInterface, dns.PrimaryIp, dns.SecondaryIp));

                ApplySuccess = success;
                ApplyMessage = success
                    ? $"✅ Đã áp dụng DNS {dns.Name} ({dns.PrimaryIp}) thành công!"
                    : $"❌ Lỗi: {error}";

                if (success)
                    new LoggerService().LogGeneric(
                        $"[DNS Benchmark] Áp dụng {dns.Name} ({dns.PrimaryIp}) vào {SelectedInterface}");
            }
            catch (Exception ex)
            {
                ApplyMessage = $"❌ {ex.Message}";
            }
        }
    }
}
