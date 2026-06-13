using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ui_avalonia.Services;
using Avalonia;
using Avalonia.Styling;

namespace ui_avalonia.ViewModels
{
    public partial class HistorySettingsViewModel : ViewModelBase
    {
        private readonly LoggerService _loggerService = new();

        [ObservableProperty]
        private ObservableCollection<string> _logEntries = new();

        [ObservableProperty]
        private string _selectedLanguage = "vi";

        [ObservableProperty]
        private string _selectedTheme = "Dark";

        [ObservableProperty]
        private string _logPath = string.Empty;

        public LocalizationService Local => LocalizationService.Instance;

        // Properties for RadioButton binding (avoiding converter errors)
        public bool IsLanguageVi
        {
            get => SelectedLanguage == "vi";
            set { if (value) SelectedLanguage = "vi"; }
        }

        public bool IsLanguageEn
        {
            get => SelectedLanguage == "en";
            set { if (value) SelectedLanguage = "en"; }
        }

        public bool IsThemeDark
        {
            get => SelectedTheme == "Dark";
            set { if (value) SelectedTheme = "Dark"; }
        }

        public bool IsThemeLight
        {
            get => SelectedTheme == "Light";
            set { if (value) SelectedTheme = "Light"; }
        }

        public bool IsThemeSystem
        {
            get => SelectedTheme == "System";
            set { if (value) SelectedTheme = "System"; }
        }

        public HistorySettingsViewModel()
        {
            RefreshLogs();
            SelectedLanguage = LocalizationService.Instance.CurrentLang;
            
            if (Application.Current != null)
            {
                var currentTheme = Application.Current.RequestedThemeVariant;
                if (currentTheme == ThemeVariant.Dark) SelectedTheme = "Dark";
                else if (currentTheme == ThemeVariant.Light) SelectedTheme = "Light";
                else SelectedTheme = "System";
            }
        }

        [RelayCommand]
        public void RefreshLogs()
        {
            LogEntries.Clear();
            var logs = _loggerService.ReadHistory(100);
            foreach (var log in logs)
            {
                LogEntries.Add(log);
            }
            LogPath = _loggerService.GetLogPath();
        }

        [RelayCommand]
        public void ClearHistoryLogs()
        {
            if (_loggerService.ClearHistory())
            {
                RefreshLogs();
            }
        }

        partial void OnSelectedLanguageChanged(string value)
        {
            if (value == "vi" || value == "en")
            {
                LocalizationService.Instance.CurrentLang = value;
                OnPropertyChanged(nameof(IsLanguageVi));
                OnPropertyChanged(nameof(IsLanguageEn));
            }
        }

        partial void OnSelectedThemeChanged(string value)
        {
            if (Application.Current == null) return;

            if (value == "Dark")
            {
                Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
            }
            else if (value == "Light")
            {
                Application.Current.RequestedThemeVariant = ThemeVariant.Light;
            }
            else
            {
                Application.Current.RequestedThemeVariant = ThemeVariant.Default;
            }

            OnPropertyChanged(nameof(IsThemeDark));
            OnPropertyChanged(nameof(IsThemeLight));
            OnPropertyChanged(nameof(IsThemeSystem));
        }
    }
}
