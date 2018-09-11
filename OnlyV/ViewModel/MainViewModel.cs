namespace OnlyV.ViewModel
{
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.CommandWpf;
    using Helpers;
    using MaterialDesignThemes.Wpf;
    using Services.Images;
    using Services.Options;
    using Services.Snackbar;
    using Services.UI;

    // ReSharper disable once UnusedMember.Global
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class MainViewModel : ViewModelBase
    {
        private readonly ScripturesViewModel _scripturesViewModel;
        private readonly PreviewViewModel _previewViewModel;
        private readonly SettingsViewModel _settingsViewModel;
        private readonly StartupViewModel _startupViewModel;

        private readonly IImagesService _imagesService;
        private readonly IOptionsService _optionsService;
        private readonly ISnackbarService _snackbarService;
        private readonly IUserInterfaceService _userInterfaceService;

        private ViewModelBase _currentPage;
        private ViewModelBase _preSettingsPage;
        private string _nextPageTooltip;
        private string _previousPageTooltip;

        public MainViewModel(
            ScripturesViewModel scripturesViewModel,
            PreviewViewModel previewViewModel,
            SettingsViewModel settingsViewModel,
            StartupViewModel startupViewModel,
            IImagesService imagesService,
            IOptionsService optionsService,
            ISnackbarService snackbarService,
            IUserInterfaceService userInterfaceService)
        {
            _scripturesViewModel = scripturesViewModel;
            _previewViewModel = previewViewModel;
            _settingsViewModel = settingsViewModel;
            _startupViewModel = startupViewModel;

            _settingsViewModel.EpubChangedEvent += HandleEpubChangedEvent;

            _imagesService = imagesService;
            _optionsService = optionsService;
            _snackbarService = snackbarService;
            _userInterfaceService = userInterfaceService;

            _optionsService.AlwaysOnTopChangedEvent += HandleAlwaysOnTopChangedEvent;
            _optionsService.EpubPathChangedEvent += HandleEpubPathChangedEvent;

            InitCommands();

            _currentPage = scripturesViewModel;
            _preSettingsPage = scripturesViewModel;

            if (IsNewInstallation())
            {
                _currentPage = startupViewModel;
            }
            else if (IsBadEpubPath())
            {
                _currentPage = settingsViewModel;
            }

            _nextPageTooltip = Properties.Resources.NEXT_PAGE_PREVIEW;

            GetVersionData();
        }

        public ViewModelBase CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    RaisePropertyChanged();

                    PreviousPageToolTip = 
                        _currentPage == _previewViewModel || _currentPage == _settingsViewModel
                            ? Properties.Resources.PREV_PAGE_SCRIPS
                            : null;

                    NextPageToolTip =
                        _currentPage == _scripturesViewModel || _currentPage == _settingsViewModel
                            ? Properties.Resources.NEXT_PAGE_PREVIEW
                            : null;
                }
            }
        }

        public bool AlwaysOnTop => _optionsService.AlwaysOnTop;

        public string NextPageToolTip
        {
            get => _nextPageTooltip;
            set
            {
                if (_nextPageTooltip == null || _nextPageTooltip != value)
                {
                    _nextPageTooltip = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string PreviousPageToolTip
        {
            get => _previousPageTooltip;
            set
            {
                if (_previousPageTooltip == null || _previousPageTooltip != value)
                {
                    _previousPageTooltip = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool ShowNewVersionButton { get; private set; }

        public string SettingsButtonToolTip =>
            CurrentPage == _settingsViewModel
                ? Properties.Resources.BACK
                : Properties.Resources.SETTINGS_PAGE;

        public string SettingsIconKind => 
            CurrentPage == _settingsViewModel
                ? @"BackBurger"
                : @"Settings";

        public RelayCommand NextPageCommand { get; set; }

        public RelayCommand BackPageCommand { get; set; }

        public RelayCommand SettingsCommand { get; set; }

        public RelayCommand LaunchHelpPageCommand { get; set; }

        public ISnackbarMessageQueue TheSnackbarMessageQueue => _snackbarService.TheSnackbarMessageQueue;

        private void InitCommands()
        {
            NextPageCommand = new RelayCommand(OnNext, CanDoNext);
            BackPageCommand = new RelayCommand(OnBack, CanDoBack);
            SettingsCommand = new RelayCommand(OnToggleSettings, CanToggleSettings);
            LaunchHelpPageCommand = new RelayCommand(LaunchHelpPage);
        }

        private bool CanToggleSettings()
        {
            if (CurrentPage == _settingsViewModel)
            {
                return !IsBadEpubPath();
            }

            return CurrentPage != _startupViewModel;
        }

        private void OnToggleSettings()
        {
            if (CurrentPage == _settingsViewModel)
            {
                CurrentPage = _preSettingsPage;
            }
            else
            {
                _preSettingsPage = CurrentPage;
                CurrentPage = _settingsViewModel;
            }

            RaisePropertyChanged(nameof(SettingsIconKind));
            RaisePropertyChanged(nameof(SettingsButtonToolTip));
        }

        private bool CanDoBack()
        {
            return CurrentPage == _previewViewModel;
        }

        private void OnBack()
        {
            if (CurrentPage == _previewViewModel)
            {
                CurrentPage = _scripturesViewModel;
            }
        }

        private bool CanDoNext()
        {
            if (CurrentPage == _scripturesViewModel)
            {
                return _scripturesViewModel.ValidScripture();
            }

            return false;
        }

        private void OnNext()
        {
            if (CurrentPage == _scripturesViewModel)
            {
                PreparePreviewPage();
                CurrentPage = _previewViewModel;
            }
        }

        private void InitImagesService()
        {
            _imagesService.Init(
                _optionsService.EpubPath, 
                _scripturesViewModel.BookNumber, 
                _scripturesViewModel.ChapterAndVersesString);
        }

        private void HandleEpubChangedEvent(object sender, System.EventArgs e)
        {
            _scripturesViewModel.HandleEpubChanged();

            if (_previewViewModel.ImageIndex != null)
            {
                PreparePreviewPage();
            }
        }

        private void PreparePreviewPage()
        {
            using (_userInterfaceService.GetBusy())
            {
                _previewViewModel.ImageIndex = null;
                InitImagesService();
                _previewViewModel.ImageIndex = 0;
                _previewViewModel.BookChapterAndVersesString = _scripturesViewModel.ScriptureText;
            }
        }

        private void HandleAlwaysOnTopChangedEvent(object sender, System.EventArgs e)
        {
            RaisePropertyChanged(nameof(AlwaysOnTop));
        }

        private bool IsBadEpubPath()
        {
            return string.IsNullOrEmpty(_optionsService.EpubPath) ||
                   !File.Exists(_optionsService.EpubPath);
        }

        private bool IsNewInstallation()
        {
            return !Directory.GetFiles(FileUtils.GetEpubFolder(), "*.epub").Any();
        }

        private void HandleEpubPathChangedEvent(object sender, System.EventArgs e)
        {
            if (CurrentPage == _startupViewModel)
            {
                CurrentPage = _scripturesViewModel;
            }
        }

        private void GetVersionData()
        {
            Task.Delay(2000).ContinueWith(_ =>
            {
                var latestVersion = VersionDetection.GetLatestReleaseVersion();
                if (latestVersion != null)
                {
                    if (latestVersion != VersionDetection.GetCurrentVersion())
                    {
                        // there is a new version....
                        ShowNewVersionButton = true;
                        RaisePropertyChanged(nameof(ShowNewVersionButton));

                        _snackbarService.Enqueue(
                            Properties.Resources.NEW_UPDATE_AVAILABLE,
                            Properties.Resources.VIEW,
                            LaunchReleasePage);
                    }
                }
            });
        }

        private void LaunchReleasePage()
        {
            Process.Start(VersionDetection.LatestReleaseUrl);
        }

        private void LaunchHelpPage()
        {
            Process.Start(@"https://github.com/AntonyCorbett/OnlyV/wiki");
        }
    }
}