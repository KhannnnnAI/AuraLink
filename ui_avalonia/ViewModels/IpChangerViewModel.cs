using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ui_avalonia.Models;
using ui_avalonia.Services;

namespace ui_avalonia.ViewModels
{
    public partial class IpChangerViewModel : ViewModelBase
    {
        private readonly NetworkService _networkService = new();
        private readonly LoggerService _loggerService = new();

        [ObservableProperty]
        private ObservableCollection<NetworkCard> _networkCards = new();

        [ObservableProperty]
        private NetworkCard? _selectedCard;

        [ObservableProperty]
        private string _ipAddress = "192.168.1.100";

        [ObservableProperty]
        private string _subnetMask = "255.255.255.0";

        [ObservableProperty]
        private string _gateway = "192.168.1.1";

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

        public IpChangerViewModel()
        {
            RefreshCards();
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
                IpAddress = value.IpAddress != "N/A" ? value.IpAddress : "192.168.1.100";
                SubnetMask = value.SubnetMask != "N/A" ? value.SubnetMask : "255.255.255.0";
                Gateway = value.Gateway != "N/A" ? value.Gateway : "192.168.1.1";
            }
        }

        private bool ValidateIp(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return false;
            var match = Regex.Match(ip, @"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$");
            if (!match.Success) return false;
            var parts = ip.Split('.');
            foreach (var part in parts)
            {
                if (!int.TryParse(part, out int val) || val < 0 || val > 255)
                    return false;
            }
            return true;
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
        public async Task ApplyIpAsync()
        {
            if (SelectedCard == null)
            {
                SetStatus("Vui lòng chọn card mạng trước!", true);
                return;
            }

            if (!ValidateIp(IpAddress))
            {
                SetStatus("Địa chỉ IP không hợp lệ!", true);
                return;
            }

            if (!ValidateIp(SubnetMask))
            {
                SetStatus("Subnet Mask không hợp lệ!", true);
                return;
            }

            if (!ValidateIp(Gateway))
            {
                SetStatus("Default Gateway không hợp lệ!", true);
                return;
            }

            IsBusy = true;
            SetStatus("Đang cấu hình IP...", false);
            StatusIcon = "⏳";
            StatusIconColor = "Yellow";

            string cardName = SelectedCard.Name;
            string oldIp = SelectedCard.IpAddress;
            string ip = IpAddress;
            string subnet = SubnetMask;
            string gw = Gateway;

            var result = await Task.Run(() =>
            {
                var changeResult = _networkService.ChangeIp(cardName, ip, subnet, gw);
                _loggerService.LogChange(cardName, oldIp, ip, changeResult.success, changeResult.error);
                return changeResult;
            });

            if (result.success)
            {
                SetStatus($"Đã thay đổi IP thành công: {ip}", false);
            }
            else
            {
                SetStatus($"Lỗi khi thay đổi IP: {result.error}", true);
            }

            RefreshCards();
            IsBusy = false;
        }
    }
}
