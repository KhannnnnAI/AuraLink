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
    public partial class SchedulerViewModel : ViewModelBase
    {
        private readonly NetworkService _networkService = new();
        private readonly ProfileService _profileService = new();
        private readonly SchedulerService _schedulerService = SchedulerService.Instance;

        [ObservableProperty]
        private bool _isSchedulerRunning;

        [ObservableProperty]
        private ObservableCollection<IPProfile> _profiles = new();

        [ObservableProperty]
        private IPProfile? _selectedProfile;

        [ObservableProperty]
        private string _timeOfDayStr = "08:00";

        [ObservableProperty]
        private int _rotationMinutes = 60;

        [ObservableProperty]
        private ObservableCollection<SchedulerJobInfo> _jobs = new();

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
        private bool _isError;

        public LocalizationService Local => LocalizationService.Instance;

        public SchedulerViewModel()
        {
            RefreshStatus();
            LoadProfiles();
        }

        private void LoadProfiles()
        {
            Profiles.Clear();
            var list = _profileService.LoadProfiles();
            foreach (var p in list)
            {
                Profiles.Add(p);
            }
        }

        private void RefreshStatus()
        {
            IsSchedulerRunning = _schedulerService.IsRunning;
            Jobs.Clear();
            var activeJobs = _schedulerService.ListJobs();
            foreach (var job in activeJobs)
            {
                Jobs.Add(job);
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
        public void ToggleScheduler()
        {
            if (IsSchedulerRunning)
            {
                _schedulerService.Stop();
                SetStatus("Đã dừng Scheduler.", false);
            }
            else
            {
                _schedulerService.Start();
                SetStatus("Đã khởi chạy Scheduler.", false);
            }
            RefreshStatus();
        }

        [RelayCommand]
        public void ScheduleDaily()
        {
            if (SelectedProfile == null)
            {
                SetStatus("Vui lòng chọn profile cần lên lịch!", true);
                return;
            }

            var p = SelectedProfile;
            Action action = () =>
            {
                _networkService.ChangeIp(p.Interface, p.Ip, p.Subnet, p.Gateway);
            };

            if (_schedulerService.ScheduleAtTime(TimeOfDayStr, action, p.ProfileName))
            {
                _schedulerService.Start();
                SetStatus($"Đã đặt lịch đổi IP sang '{p.ProfileName}' lúc {TimeOfDayStr} hằng ngày!", false);
                RefreshStatus();
            }
            else
            {
                SetStatus("Định dạng thời gian không hợp lệ (ví dụ: 08:00)!", true);
            }
        }

        [RelayCommand]
        public void ScheduleRotation()
        {
            var list = _profileService.LoadProfiles();
            if (list.Count < 2)
            {
                SetStatus("Cần ít nhất 2 profile lưu sẵn để thiết lập xoay vòng!", true);
                return;
            }

            if (RotationMinutes <= 0)
            {
                SetStatus("Số phút xoay vòng phải lớn hơn 0!", true);
                return;
            }

            if (_schedulerService.ScheduleProfileRotation(list, RotationMinutes, _networkService.ChangeIp))
            {
                _schedulerService.Start();
                SetStatus($"Đã thiết lập xoay vòng {list.Count} profiles mỗi {RotationMinutes} phút!", false);
                RefreshStatus();
            }
            else
            {
                SetStatus("Thiết lập xoay vòng thất bại!", true);
            }
        }

        [RelayCommand]
        public void ClearJobs()
        {
            _schedulerService.Stop();
            SetStatus("Đã dừng Scheduler và xoá sạch toàn bộ tiến trình hẹn giờ.", false);
            RefreshStatus();
        }
    }
}
