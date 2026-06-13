using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ui_avalonia.Models;
using ui_avalonia.Services;

namespace ui_avalonia.ViewModels
{
    public partial class DnsManagerViewModel : ViewModelBase
    {
        private readonly NetworkService _networkService = new();
        private readonly DnsService _dnsService = new();
        private List<DnsRecord> _allDnsRecords = new();

        [ObservableProperty]
        private ObservableCollection<NetworkCard> _networkCards = new();

        [ObservableProperty]
        private NetworkCard? _selectedCard;

        [ObservableProperty]
        private ObservableCollection<DnsRecord> _dnsRecords = new();

        [ObservableProperty]
        private DnsRecord? _selectedDns;

        [ObservableProperty]
        private ObservableCollection<string> _categories = new();

        [ObservableProperty]
        private string _selectedCategory = "Tất cả";

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

        public DnsManagerViewModel()
        {
            RefreshCards();
            LoadDnsRecords();
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

        private void LoadDnsRecords()
        {
            _allDnsRecords = _dnsService.ParseDnsFile();
            
            Categories.Clear();
            Categories.Add("Tất cả");
            var cats = _allDnsRecords.Select(r => r.Section).Distinct();
            foreach (var cat in cats)
            {
                Categories.Add(cat);
            }

            FilterDnsRecords();
        }

        partial void OnSelectedCategoryChanged(string value)
        {
            FilterDnsRecords();
        }

        private void FilterDnsRecords()
        {
            DnsRecords.Clear();
            var filtered = SelectedCategory == "Tất cả" 
                ? _allDnsRecords 
                : _allDnsRecords.Where(r => r.Section == SelectedCategory);

            foreach (var record in filtered)
            {
                DnsRecords.Add(record);
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
        public async Task ApplyDnsAsync()
        {
            if (SelectedCard == null)
            {
                SetStatus("Vui lòng chọn card mạng!", true);
                return;
            }

            if (SelectedDns == null)
            {
                SetStatus("Vui lòng chọn một DNS để áp dụng!", true);
                return;
            }

            IsBusy = true;
            SetStatus($"Đang đặt DNS sang {SelectedDns.Name}...", false);
            StatusIcon = "⏳";
            StatusIconColor = "Yellow";

            string cardName = SelectedCard.Name;
            string primary = SelectedDns.Primary;
            string secondary = SelectedDns.Secondary;
            bool isIpv6 = SelectedDns.IsIpv6;

            var result = await Task.Run(() =>
            {
                return _dnsService.ChangeDns(cardName, primary, secondary, isIpv6);
            });

            if (result.success)
            {
                SetStatus($"Đã thay đổi DNS sang {SelectedDns.Name}!", false);
            }
            else
            {
                SetStatus($"Lỗi khi đổi DNS: {result.error}", true);
            }

            IsBusy = false;
        }

        [RelayCommand]
        public async Task RestoreDefaultDnsAsync()
        {
            if (SelectedCard == null)
            {
                SetStatus("Vui lòng chọn card mạng!", true);
                return;
            }

            IsBusy = true;
            SetStatus("Đang khôi phục DNS tự động (DHCP)...", false);
            StatusIcon = "⏳";
            StatusIconColor = "Yellow";

            string cardName = SelectedCard.Name;

            var result = await Task.Run(() =>
            {
                var r4 = _dnsService.ChangeDns(cardName, "", "", isIpv6: false);
                var r6 = _dnsService.ChangeDns(cardName, "", "", isIpv6: true);
                return (r4.success, r4.error);
            });

            if (result.success)
            {
                SetStatus("Đã khôi phục DNS tự động thành công!", false);
            }
            else
            {
                SetStatus($"Lỗi khi khôi phục DNS: {result.error}", true);
            }

            IsBusy = false;
        }
    }
}
