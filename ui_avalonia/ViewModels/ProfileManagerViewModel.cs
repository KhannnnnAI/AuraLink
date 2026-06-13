using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ui_avalonia.Models;
using ui_avalonia.Services;

namespace ui_avalonia.ViewModels
{
    public partial class ProfileManagerViewModel : ViewModelBase
    {
        private readonly NetworkService _networkService = new();
        private readonly ProfileService _profileService = new();
        private readonly DnsService _dnsService = new();
        private readonly LoggerService _loggerService = new();

        [ObservableProperty]
        private ObservableCollection<IPProfile> _profiles = new();

        [ObservableProperty]
        private IPProfile? _selectedProfile;

        [ObservableProperty]
        private ObservableCollection<NetworkCard> _networkCards = new();

        [ObservableProperty]
        private NetworkCard? _selectedCard;

        [ObservableProperty]
        private string _newProfileName = string.Empty;

        [ObservableProperty]
        private string _newIp = string.Empty;

        [ObservableProperty]
        private string _newSubnet = "255.255.255.0";

        [ObservableProperty]
        private string _newGateway = string.Empty;

        [ObservableProperty]
        private string _newDns1 = "8.8.8.8";

        [ObservableProperty]
        private string _newDns2 = "8.8.4.4";

        [ObservableProperty]
        private string _newNote = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _statusBackground = "Transparent";

        [ObservableProperty]
        private string _statusBorder = "Transparent";

        [ObservableProperty]
        private string _statusIcon = "ℹ️";

        [ObservableProperty]
        private string _statusIconColor = "Gray";

        [ObservableProperty]
        private bool _isBusy;

        public LocalizationService Local => LocalizationService.Instance;

        public ProfileManagerViewModel()
        {
            RefreshProfiles();
            RefreshCards();
        }

        [RelayCommand]
        public void RefreshProfiles()
        {
            Profiles.Clear();
            var list = _profileService.LoadProfiles();
            foreach (var p in list)
            {
                Profiles.Add(p);
            }
        }

        [RelayCommand]
        public void RefreshCards()
        {
            NetworkCards.Clear();
            var cards = _networkService.GetInterfaces();
            foreach (var card in cards)
            {
                NetworkCards.Add(card);
            }
            if (NetworkCards.Count > 0)
            {
                SelectedCard = NetworkCards[0];
            }
        }

        partial void OnSelectedCardChanged(NetworkCard? value)
        {
            if (value != null)
            {
                NewIp = value.IpAddress != "N/A" ? value.IpAddress : "192.168.1.100";
                NewSubnet = value.SubnetMask != "N/A" ? value.SubnetMask : "255.255.255.0";
                NewGateway = value.Gateway != "N/A" ? value.Gateway : "192.168.1.1";
            }
        }

        private void SetStatus(string msg, bool isError)
        {
            StatusMessage = msg;
            if (isError)
            {
                StatusBackground = "#2d1a1a";
                StatusBorder = "#5a2727";
                StatusIcon = "❌";
                StatusIconColor = "Red";
            }
            else
            {
                StatusBackground = "#1a2e1a";
                StatusBorder = "#2d5a27";
                StatusIcon = "✅";
                StatusIconColor = "Green";
            }
        }

        [RelayCommand]
        public async Task ApplyProfileAsync(IPProfile? profile)
        {
            var p = profile ?? SelectedProfile;
            if (p == null)
            {
                SetStatus("Vui lòng chọn profile cần áp dụng!", true);
                return;
            }

            IsBusy = true;
            SetStatus($"Đang áp dụng profile '{p.ProfileName}'...", false);
            StatusIcon = "⏳";
            StatusIconColor = "Yellow";

            await Task.Run(() =>
            {
                var ipResult = _networkService.ChangeIp(p.Interface, p.Ip, p.Subnet, p.Gateway);
                _loggerService.LogChange(p.Interface, "N/A", p.Ip, ipResult.success, ipResult.error, p.ProfileName);

                if (!string.IsNullOrEmpty(p.Dns1))
                {
                    _dnsService.ChangeDns(p.Interface, p.Dns1, p.Dns2, isIpv6: false);
                }
            });

            SetStatus($"Đã áp dụng xong profile '{p.ProfileName}'!", false);
            IsBusy = false;
        }

        [RelayCommand]
        public void SaveProfile()
        {
            if (string.IsNullOrWhiteSpace(NewProfileName))
            {
                SetStatus("Vui lòng nhập tên profile!", true);
                return;
            }

            if (SelectedCard == null)
            {
                SetStatus("Vui lòng chọn card mạng cho profile!", true);
                return;
            }

            var profile = new IPProfile
            {
                ProfileName = NewProfileName.Trim(),
                Interface = SelectedCard.Name,
                Ip = NewIp.Trim(),
                Subnet = NewSubnet.Trim(),
                Gateway = NewGateway.Trim(),
                Dns1 = NewDns1.Trim(),
                Dns2 = NewDns2.Trim(),
                Note = NewNote.Trim()
            };

            if (_profileService.AddProfile(profile))
            {
                SetStatus($"Đã lưu profile '{profile.ProfileName}' thành công!", false);
                NewProfileName = string.Empty;
                NewNote = string.Empty;
                RefreshProfiles();
            }
            else
            {
                SetStatus("Lưu profile thất bại!", true);
            }
        }

        [RelayCommand]
        public void DeleteProfile(IPProfile? profile)
        {
            var p = profile ?? SelectedProfile;
            if (p == null) return;

            if (_profileService.DeleteProfile(p.ProfileName))
            {
                SetStatus($"Đã xóa profile '{p.ProfileName}'!", false);
                RefreshProfiles();
            }
            else
            {
                SetStatus("Xóa profile thất bại!", true);
            }
        }

        [RelayCommand]
        public async Task ExportProfilesAsync()
        {
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles_export.json");
                var list = _profileService.LoadProfiles();
                if (_profileService.ExportProfiles(path, list))
                {
                    SetStatus($"Đã xuất danh sách profile ra file: {path}", false);
                }
                else
                {
                    SetStatus("Xuất file thất bại!", true);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Lỗi: {ex.Message}", true);
            }
            await Task.CompletedTask;
        }

        [RelayCommand]
        public async Task ImportProfilesAsync()
        {
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles_export.json");
                if (!System.IO.File.Exists(path))
                {
                    SetStatus($"Không tìm thấy file profiles_export.json ở {path}!", true);
                    return;
                }

                var (added, skipped) = _profileService.ImportProfiles(path);
                SetStatus($"Đã nhập {added} profiles ({skipped} bỏ qua do trùng tên).", false);
                RefreshProfiles();
            }
            catch (Exception ex)
            {
                SetStatus($"Lỗi: {ex.Message}", true);
            }
            await Task.CompletedTask;
        }
    }
}
