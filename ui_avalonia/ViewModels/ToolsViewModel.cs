using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ui_avalonia.Services;

namespace ui_avalonia.ViewModels
{
    public partial class ToolsViewModel : ViewModelBase
    {
        private readonly SpeedtestService _speedtestService = new();
        private readonly GeoIpService _geoIpService = new();
        private CancellationTokenSource? _speedtestCts;

        public ToolsViewModel()
        {
        }

        // ═══════════════════════ Server List ════════════════════════════

        public ObservableCollection<SpeedtestServer> AvailableServers { get; } = new();

        [ObservableProperty]
        private SpeedtestServer? _selectedServer;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanRunTest))]
        private bool _isLoadingServers;

        [ObservableProperty]
        private string _serverLoadStatus = string.Empty;

        // ═══════════════════════ Speedtest Results ═════════════════════

        [ObservableProperty]
        private double _downloadSpeed;

        [ObservableProperty]
        private double _uploadSpeed;

        [ObservableProperty]
        private double _pingLatency;

        [ObservableProperty]
        private double _jitter;

        [ObservableProperty]
        private double _packetLoss = -1;

        [ObservableProperty]
        private string _speedtestServer = "N/A";

        [ObservableProperty]
        private string _ispName = string.Empty;

        [ObservableProperty]
        private string _resultUrl = string.Empty;

        [ObservableProperty]
        private string _speedtestProgress = string.Empty;

        [ObservableProperty]
        private double _speedtestProgressValue;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanRunTest))]
        private bool _isTestingSpeed;

        [ObservableProperty]
        private bool _isUploadingPhase;

        public bool CanRunTest => !IsTestingSpeed && !IsLoadingServers;

        // ═══════════════════════ Gauge Animation ═══════════════════════

        [ObservableProperty]
        private double _gaugeDisplayValue;

        private DispatcherTimer? _gaugeTimer;
        private double _gaugeStart, _gaugeTarget;
        private int _gaugeFrame;
        private int _gaugeTotalFrames = 55; // ~900ms at 60fps

        private void AnimateGaugeTo(double target, int totalFrames = 55)
        {
            _gaugeTimer?.Stop();
            _gaugeStart = GaugeDisplayValue;
            _gaugeTarget = target;
            _gaugeFrame = 0;
            _gaugeTotalFrames = totalFrames;

            _gaugeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _gaugeTimer.Tick += OnGaugeTick;
            _gaugeTimer.Start();
        }

        private void OnGaugeTick(object? sender, EventArgs e)
        {
            _gaugeFrame++;
            double t = Math.Clamp((double)_gaugeFrame / _gaugeTotalFrames, 0, 1);
            // Ease-out cubic: nhanh đầu, chậm cuối
            double eased = 1 - Math.Pow(1 - t, 3);
            GaugeDisplayValue = _gaugeStart + (_gaugeTarget - _gaugeStart) * eased;

            if (_gaugeFrame >= _gaugeTotalFrames)
            {
                GaugeDisplayValue = _gaugeTarget;
                _gaugeTimer?.Stop();
            }
        }

        private void SetGaugeValueDirect(double value)
        {
            _gaugeTimer?.Stop();
            GaugeDisplayValue = value;
        }

        // ═══════════════════════ GeoIP ═════════════════════════════════

        [ObservableProperty]
        private string _publicIp = "N/A";

        [ObservableProperty]
        private string _country = "N/A";

        [ObservableProperty]
        private string _region = "N/A";

        [ObservableProperty]
        private string _city = "N/A";

        [ObservableProperty]
        private string _isp = "N/A";

        [ObservableProperty]
        private string _coordinates = "N/A";

        [ObservableProperty]
        private string _timezone = "N/A";

        [ObservableProperty]
        private string _mapsUrl = string.Empty;

        [ObservableProperty]
        private string _geoIpStatus = string.Empty;

        [ObservableProperty]
        private bool _isGeoIpQuerying;

        public LocalizationService Local => LocalizationService.Instance;

        // ═══════════════════════ Commands ══════════════════════════════

        [RelayCommand]
        public async Task LoadServersAsync()
        {
            if (IsLoadingServers) return;

            IsLoadingServers = true;
            ServerLoadStatus = "Đang tải danh sách server...";
            AvailableServers.Clear();
            SelectedServer = null;

            try
            {
                var servers = await _speedtestService.GetServersAsync();
                foreach (var s in servers)
                    AvailableServers.Add(s);

                if (AvailableServers.Count > 0)
                {
                    SelectedServer = AvailableServers[0];
                    ServerLoadStatus = $"Tìm thấy {AvailableServers.Count} server gần bạn.";
                }
                else
                {
                    ServerLoadStatus = "Không tìm thấy server nào.";
                }
            }
            catch (Exception ex)
            {
                ServerLoadStatus = $"Lỗi: {ex.Message}";
            }
            finally
            {
                IsLoadingServers = false;
            }
        }

        [RelayCommand]
        public async Task RunSpeedtestAsync()
        {
            if (IsTestingSpeed || IsLoadingServers) return;

            IsTestingSpeed = true;
            IsUploadingPhase = false; // Reset
            DownloadSpeed = 0;
            UploadSpeed = 0;
            PingLatency = 0;
            Jitter = 0;
            PacketLoss = -1;
            SpeedtestServer = "...";
            IspName = string.Empty;
            ResultUrl = string.Empty;
            SpeedtestProgressValue = 0;
            GaugeDisplayValue = 0;

            _speedtestCts = new CancellationTokenSource();

            try
            {
                var result = await _speedtestService.RunSpeedtestAsync(
                    serverId: SelectedServer?.Id,
                    progressCallback: state =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            SpeedtestProgressValue = state.Progress;
                            SpeedtestProgress = state.Message;

                            if (state.Type == "ping")
                            {
                                PingLatency = state.PingMs;
                                Jitter = state.JitterMs;
                            }
                            else if (state.Type == "download")
                            {
                                IsUploadingPhase = false;
                                DownloadSpeed = state.DownloadMbps;
                                if (state.Progress >= 48)
                                {
                                    // Đạt 48% (hết download), trả kim về 0
                                    AnimateGaugeTo(0, 30);
                                }
                                else
                                {
                                    AnimateGaugeTo(state.CurrentSpeedMbps, 10);
                                }
                            }
                            else if (state.Type == "upload")
                            {
                                IsUploadingPhase = true;
                                UploadSpeed = state.UploadMbps;
                                AnimateGaugeTo(state.CurrentSpeedMbps, 10);
                            }
                            else if (state.Type == "result")
                            {
                                IsUploadingPhase = false;
                                DownloadSpeed = state.DownloadMbps;
                                UploadSpeed = state.UploadMbps;
                                PingLatency = state.PingMs;
                                Jitter = state.JitterMs;
                                PacketLoss = state.PacketLoss;
                                SpeedtestServer = state.ServerName;
                                IspName = state.Isp;
                                ResultUrl = state.ResultUrl;
                            }
                        });
                    },
                    ct: _speedtestCts.Token
                );

                if (result.Success)
                {
                    IsUploadingPhase = false;
                    DownloadSpeed = result.DownloadMbps;
                    UploadSpeed = result.UploadMbps;
                    PingLatency = result.PingMs;
                    Jitter = result.Jitter;
                    PacketLoss = result.PacketLoss;
                    SpeedtestServer = result.ServerName;
                    IspName = result.Isp;
                    ResultUrl = result.ResultUrl;
                    SpeedtestProgress = "Đo tốc độ hoàn thành!";

                    // Animate gauge back to 0
                    AnimateGaugeTo(0, 30);
                }
                else
                {
                    SpeedtestProgress = $"Thất bại: {result.Error}";
                }
            }
            catch (Exception ex)
            {
                SpeedtestProgress = $"Lỗi: {ex.Message}";
            }
            finally
            {
                IsTestingSpeed = false;
                IsUploadingPhase = false; // Reset
                _speedtestCts?.Dispose();
                _speedtestCts = null;
            }
        }

        [RelayCommand]
        public async Task QueryGeoIpAsync()
        {
            IsGeoIpQuerying = true;
            GeoIpStatus = "Đang tra cứu vị trí địa lý...";

            var info = await _geoIpService.GetLocationInfoAsync();

            if (info.Success)
            {
                PublicIp = info.Ip;
                Country = info.Country;
                Region = info.Region;
                City = info.City;
                Isp = info.Isp;
                Coordinates = $"{info.Latitude}, {info.Longitude}";
                Timezone = info.Timezone;
                MapsUrl = $"https://www.google.com/maps?q={info.Latitude},{info.Longitude}";
                GeoIpStatus = "Tra cứu thành công!";
            }
            else
            {
                GeoIpStatus = $"Lỗi tra cứu: {info.Error}";
            }

            IsGeoIpQuerying = false;
        }

        [RelayCommand]
        public void OpenMaps()
        {
            if (string.IsNullOrEmpty(MapsUrl)) return;
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = MapsUrl,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        [RelayCommand]
        public void OpenResultUrl()
        {
            if (string.IsNullOrEmpty(ResultUrl)) return;
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ResultUrl,
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }
}
