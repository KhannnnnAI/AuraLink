using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ui_avalonia.Services
{
    public class SchedulerService
    {
        public static SchedulerService Instance { get; } = new SchedulerService();

        private Timer? _timer;
        private bool _isRunning;
        private readonly List<SchedulerJob> _jobs = new();
        private readonly object _lock = new();

        public bool IsRunning => _isRunning;

        public void Start()
        {
            lock (_lock)
            {
                if (_isRunning) return;
                _isRunning = true;
                // Chạy kiểm tra mỗi 10 giây để đảm bảo độ chính xác
                _timer = new Timer(OnTimerTick, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _isRunning = false;
                _timer?.Dispose();
                _timer = null;
                _jobs.Clear();
            }
        }

        public List<SchedulerJobInfo> ListJobs()
        {
            lock (_lock)
            {
                var list = new List<SchedulerJobInfo>();
                foreach (var j in _jobs)
                {
                    list.Add(new SchedulerJobInfo
                    {
                        Type = j.Type,
                        Label = j.Label,
                        TimeStr = j.TimeStr,
                        IntervalMinutes = j.IntervalMinutes
                    });
                }
                return list;
            }
        }

        public bool ScheduleAtTime(string timeStr, Action action, string label = "")
        {
            if (!TimeSpan.TryParse(timeStr, out var time)) return false;

            lock (_lock)
            {
                _jobs.Add(new SchedulerJob
                {
                    Type = "daily",
                    Label = label,
                    TimeStr = timeStr,
                    TimeOfDay = time,
                    Action = action,
                    LastRun = DateTime.MinValue
                });
            }
            return true;
        }

        public bool ScheduleInterval(int intervalMinutes, Action action, string label = "")
        {
            if (intervalMinutes <= 0) return false;

            lock (_lock)
            {
                _jobs.Add(new SchedulerJob
                {
                    Type = "interval",
                    Label = label,
                    IntervalMinutes = intervalMinutes,
                    Action = action,
                    LastRun = DateTime.Now
                });
            }
            return true;
        }

        public bool ScheduleProfileRotation(List<Models.IPProfile> profiles, int intervalMinutes, Func<string, string, string, string, (bool success, string error)> changeIpFn)
        {
            if (profiles == null || profiles.Count == 0 || intervalMinutes <= 0) return false;

            int index = 0;
            Action action = () =>
            {
                var profile = profiles[index % profiles.Count];
                index++;
                changeIpFn(profile.Interface, profile.Ip, profile.Subnet, profile.Gateway);
            };

            return ScheduleInterval(intervalMinutes, action, "Xoay vòng Profile");
        }

        private void OnTimerTick(object? state)
        {
            lock (_lock)
            {
                if (!_isRunning) return;

                var now = DateTime.Now;
                foreach (var job in _jobs)
                {
                    if (job.Type == "daily")
                    {
                        var targetTime = now.Date.Add(job.TimeOfDay);
                        // Nếu đã đến hoặc vượt quá giờ hẹn và hôm nay chưa chạy
                        if (now >= targetTime && job.LastRun < now.Date)
                        {
                            job.LastRun = now;
                            Task.Run(() => job.Action());
                        }
                    }
                    else if (job.Type == "interval")
                    {
                        if (now >= job.LastRun.AddMinutes(job.IntervalMinutes))
                        {
                            job.LastRun = now;
                            Task.Run(() => job.Action());
                        }
                    }
                }
            }
        }
    }

    public class SchedulerJob
    {
        public string Type { get; set; } = string.Empty; // daily / interval
        public string Label { get; set; } = string.Empty;
        public string TimeStr { get; set; } = string.Empty;
        public TimeSpan TimeOfDay { get; set; }
        public int IntervalMinutes { get; set; }
        public Action Action { get; set; } = () => { };
        public DateTime LastRun { get; set; }
    }

    public class SchedulerJobInfo
    {
        public string Type { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string TimeStr { get; set; } = string.Empty;
        public int IntervalMinutes { get; set; }
    }
}
