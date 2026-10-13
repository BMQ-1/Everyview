using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EverythingModern.Models;
using EverythingModern.Services;

namespace EverythingModern.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly EverythingSearchService _searchService = new();
        private CancellationTokenSource? _searchCts;
        private CancellationTokenSource? _previewCts;
        private System.Windows.Threading.DispatcherTimer? _daemonWatchdog;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private SearchFilterType _selectedFilter = SearchFilterType.All;

        [ObservableProperty]
        private SortOrder _currentSort = SortOrder.NameAscending;

        [ObservableProperty]
        private string _sortColumn = "Name";

        [ObservableProperty]
        private bool _isSortAscending = true;

        [ObservableProperty]
        private bool _isGridView = false; // false = Detailed Table, true = Visual Grid

        [ObservableProperty]
        private bool _isQuickLookOpen = false;

        // Search Modifiers (Native Everything Flags)
        [ObservableProperty]
        private bool _matchCase;

        [ObservableProperty]
        private bool _matchWholeWord;

        [ObservableProperty]
        private bool _matchPath;

        [ObservableProperty]
        private bool _regex;

        // Drive Scope & Scale Limit
        [ObservableProperty]
        private ObservableCollection<string> _availableDrives = new();

        [ObservableProperty]
        private ObservableCollection<DriveItemInfo> _driveItems = new();

        [ObservableProperty]
        private string _selectedDriveMetricsDisplay = string.Empty;

        [ObservableProperty]
        private string _selectedDrive = "All Drives";

        [ObservableProperty]
        private int _maxResults = 25000;

        [ObservableProperty]
        private string _maxResultsLabel = "25k";

        // Results & Selection
        [ObservableProperty]
        private ObservableRangeCollection<SearchResultItem> _results = new();

        [ObservableProperty]
        private SearchResultItem? _selectedItem;

        [ObservableProperty]
        private string _selectionSummary = string.Empty;

        [ObservableProperty]
        private bool _hasMultipleSelected;

        [ObservableProperty]
        private int _selectedCount;

        [ObservableProperty]
        private bool _isSearching;

        [ObservableProperty]
        private string _statusText = "Ready";

        [ObservableProperty]
        private string _statusTextTooltip = "Ready";

        [ObservableProperty]
        private bool _isDaemonConnected;

        [ObservableProperty]
        private bool _isAdminRequired;

        [ObservableProperty]
        private bool _isDaemonStarting;

        [ObservableProperty]
        private string _daemonStatusText = "Connecting to Everything...";

        [ObservableProperty]
        private string _daemonTooltipText = "Click to reconnect Everything Engine";

        [ObservableProperty]
        private bool _isPreviewPaneVisible = true;

        [ObservableProperty]
        private PreviewData _currentPreview = new();

        [ObservableProperty]
        private bool _hasResults;

        [ObservableProperty]
        private ObservableRangeCollection<SearchResultItem> _gridResults = new();

        // Analytics & Disk Insights
        [ObservableProperty]
        private bool _isAnalyticsVisible = true;

        [ObservableProperty]
        private string _totalResultsSizeDisplay = "0 B";

        [ObservableProperty]
        private string _fileFolderBreakdownDisplay = "0 items";

        [ObservableProperty]
        private string _largestFileDisplay = string.Empty;

        [ObservableProperty]
        private string _topExtensionsDisplay = string.Empty;

        // Syntax Helper Popup
        [ObservableProperty]
        private bool _isSyntaxHelperOpen;

        // Direct two-way Category Filter Booleans for instant UI binding
        public bool IsFilterAll
        {
            get => SelectedFilter == SearchFilterType.All;
            set { if (value) SelectedFilter = SearchFilterType.All; }
        }
        public bool IsFilterFiles
        {
            get => SelectedFilter == SearchFilterType.Files;
            set { if (value) SelectedFilter = SearchFilterType.Files; }
        }
        public bool IsFilterFolders
        {
            get => SelectedFilter == SearchFilterType.Folders;
            set { if (value) SelectedFilter = SearchFilterType.Folders; }
        }
        public bool IsFilterDocuments
        {
            get => SelectedFilter == SearchFilterType.Documents;
            set { if (value) SelectedFilter = SearchFilterType.Documents; }
        }
        public bool IsFilterPictures
        {
            get => SelectedFilter == SearchFilterType.Pictures;
            set { if (value) SelectedFilter = SearchFilterType.Pictures; }
        }
        public bool IsFilterVideos
        {
            get => SelectedFilter == SearchFilterType.Videos;
            set { if (value) SelectedFilter = SearchFilterType.Videos; }
        }
        public bool IsFilterAudio
        {
            get => SelectedFilter == SearchFilterType.Audio;
            set { if (value) SelectedFilter = SearchFilterType.Audio; }
        }
        public bool IsFilterCode
        {
            get => SelectedFilter == SearchFilterType.Code;
            set { if (value) SelectedFilter = SearchFilterType.Code; }
        }
        public bool IsFilterArchives
        {
            get => SelectedFilter == SearchFilterType.Compressed;
            set { if (value) SelectedFilter = SearchFilterType.Compressed; }
        }
        public bool IsFilterApps
        {
            get => SelectedFilter == SearchFilterType.Executables;
            set { if (value) SelectedFilter = SearchFilterType.Executables; }
        }

        public void NotifyFilterProperties()
        {
            OnPropertyChanged(nameof(IsFilterAll));
            OnPropertyChanged(nameof(IsFilterFiles));
            OnPropertyChanged(nameof(IsFilterFolders));
            OnPropertyChanged(nameof(IsFilterDocuments));
            OnPropertyChanged(nameof(IsFilterPictures));
            OnPropertyChanged(nameof(IsFilterVideos));
            OnPropertyChanged(nameof(IsFilterAudio));
            OnPropertyChanged(nameof(IsFilterCode));
            OnPropertyChanged(nameof(IsFilterArchives));
            OnPropertyChanged(nameof(IsFilterApps));
        }

        // Visual Sorting Indicators & Properties
        public string SortNameIndicator => SortColumn == "Name" ? (IsSortAscending ? "▲" : "▼") : string.Empty;
        public string SortPathIndicator => SortColumn == "Path" ? (IsSortAscending ? "▲" : "▼") : string.Empty;
        public string SortSizeIndicator => SortColumn == "Size" ? (IsSortAscending ? "▲" : "▼") : string.Empty;
        public string SortDateModifiedIndicator => SortColumn == "DateModified" ? (IsSortAscending ? "▲" : "▼") : string.Empty;

        public bool IsSortNameActive => SortColumn == "Name";
        public bool IsSortPathActive => SortColumn == "Path";
        public bool IsSortSizeActive => SortColumn == "Size";
        public bool IsSortDateModifiedActive => SortColumn == "DateModified";

        public string SortSummaryDisplay => $"{SortColumn} ({(IsSortAscending ? "Ascending ▲" : "Descending ▼")})";

        public void NotifySortProperties()
        {
            OnPropertyChanged(nameof(SortNameIndicator));
            OnPropertyChanged(nameof(SortPathIndicator));
            OnPropertyChanged(nameof(SortSizeIndicator));
            OnPropertyChanged(nameof(SortDateModifiedIndicator));
            OnPropertyChanged(nameof(IsSortNameActive));
            OnPropertyChanged(nameof(IsSortPathActive));
            OnPropertyChanged(nameof(IsSortSizeActive));
            OnPropertyChanged(nameof(IsSortDateModifiedActive));
            OnPropertyChanged(nameof(SortSummaryDisplay));
        }

        public MainViewModel()
        {
            InitializeDrives();
            InitializeDaemonAsync();
        }

        private void InitializeDrives()
        {
            DriveItems.Clear();
            AvailableDrives.Clear();
            AvailableDrives.Add("All Drives");
            SelectedDriveMetricsDisplay = "Scanning drives...";

            _ = Task.Run(() =>
            {
                var driveLetters = new List<string>();
                var items = new List<DriveItemInfo>();
                long allTotal = 0;
                long allFree = 0;

                try
                {
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        if (drive.IsReady)
                        {
                            string letter = drive.Name.TrimEnd('\\');
                            driveLetters.Add(letter);

                            long total = drive.TotalSize;
                            long free = drive.TotalFreeSpace;
                            long used = Math.Max(0, total - free);
                            int percent = total > 0 ? (int)Math.Round((double)used / total * 100) : 0;
                            string label = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "Local Disk" : drive.VolumeLabel;
                            string fs = drive.DriveFormat;

                            allTotal += total;
                            allFree += free;

                            items.Add(new DriveItemInfo
                            {
                                DriveLetter = letter,
                                DisplayName = $"{letter} ({SearchResultItem.FormatSize(free)} free)",
                                VolumeLabel = label,
                                FreeSpaceDisplay = $"{SearchResultItem.FormatSize(free)} free",
                                TotalSpaceDisplay = SearchResultItem.FormatSize(total),
                                UsedPercent = percent,
                                FileSystem = fs,
                                IsSelected = letter.Equals(SelectedDrive, StringComparison.OrdinalIgnoreCase),
                                IsAllDrives = false,
                                IconSymbol = "HardDrive20",
                                Summary = $"{label} ({letter}) • {fs} • {SearchResultItem.FormatSize(free)} Free of {SearchResultItem.FormatSize(total)} ({percent}% used)"
                            });
                        }
                    }
                }
                catch { }

                long allUsed = Math.Max(0, allTotal - allFree);
                int allPercent = allTotal > 0 ? (int)Math.Round((double)allUsed / allTotal * 100) : 0;
                string drivesList = string.Join(", ", driveLetters);
                var allItem = new DriveItemInfo
                {
                    DriveLetter = "All Drives",
                    DisplayName = "All Drives",
                    VolumeLabel = "Entire PC",
                    FreeSpaceDisplay = $"{SearchResultItem.FormatSize(allFree)} free",
                    TotalSpaceDisplay = SearchResultItem.FormatSize(allTotal),
                    UsedPercent = allPercent,
                    FileSystem = "NTFS/ReFS",
                    IsSelected = SelectedDrive.Equals("All Drives", StringComparison.OrdinalIgnoreCase),
                    IsAllDrives = true,
                    IconSymbol = "Globe20",
                    Summary = $"Entire PC ({drivesList}) • {SearchResultItem.FormatSize(allFree)} Free of {SearchResultItem.FormatSize(allTotal)} total"
                };
                items.Insert(0, allItem);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    DriveItems.Clear();
                    foreach (var item in items)
                    {
                        DriveItems.Add(item);
                    }

                    AvailableDrives.Clear();
                    AvailableDrives.Add("All Drives");
                    foreach (var l in driveLetters)
                    {
                        AvailableDrives.Add(l);
                    }

                    UpdateDriveMetrics(SelectedDrive);
                });
            });
        }

        private void UpdateDriveSelection(string driveLetter)
        {
            foreach (var item in DriveItems)
            {
                item.IsSelected = item.DriveLetter.Equals(driveLetter, StringComparison.OrdinalIgnoreCase);
            }
            UpdateDriveMetrics(driveLetter);
        }

        private void UpdateDriveMetrics(string driveLetter)
        {
            var match = DriveItems.FirstOrDefault(d => d.DriveLetter.Equals(driveLetter, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                SelectedDriveMetricsDisplay = match.Summary;
            }
            else
            {
                SelectedDriveMetricsDisplay = driveLetter;
            }
        }

        private async void InitializeDaemonAsync()
        {
            IsDaemonStarting = true;
            DaemonStatusText = "Connecting to Everything Engine...";
            DaemonTooltipText = "Connecting to Voidtools Everything IPC interface...";
            
            bool ready = await EverythingDaemonManager.EnsureDaemonRunningAsync(
                timeoutMs: 12000,
                progressCallback: msg =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DaemonStatusText = msg;
                        StatusText = msg;
                    });
                });

            IsDaemonStarting = false;
            IsDaemonConnected = ready;

            if (ready)
            {
                IsAdminRequired = false;
                DaemonStatusText = "Engine: Ready";
                DaemonTooltipText = "Everything Engine is online and responding via IPC.";
                TriggerSearch(immediate: true);
            }
            else
            {
                bool inSession = EverythingDaemonManager.IsEverythingProcessRunningInSession();
                bool isCurrentAdmin = EverythingDaemonManager.IsCurrentProcessElevated();
                bool isTargetAdmin = EverythingDaemonManager.IsAnySessionEverythingElevated();

                if (inSession && !isCurrentAdmin && isTargetAdmin)
                {
                    IsAdminRequired = true;
                    DaemonStatusText = "Engine: Elevation Required";
                    StatusText = "Elevation required • Click [🛡️ Run as Admin]";
                    StatusTextTooltip = "Voidtools Everything is running with Administrator rights.\nWindows blocks IPC communication from standard applications.\n\nClick [🛡️ Run as Admin] to elevate Everyview, or configure Everything options.";
                    DaemonTooltipText = StatusTextTooltip;
                }
                else
                {
                    IsAdminRequired = false;
                    DaemonStatusText = "Engine: Offline";
                    StatusText = "Everything Engine offline • Click Reconnect";
                    StatusTextTooltip = "Everything background engine is not responding. Click Reconnect to restart it.";
                    DaemonTooltipText = StatusTextTooltip;
                }
            }

            StartDaemonWatchdog();
        }

        private void StartDaemonWatchdog()
        {
            _daemonWatchdog = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2.0)
            };
            _daemonWatchdog.Tick += (s, e) =>
            {
                if (!IsDaemonConnected)
                {
                    if (EverythingDaemonManager.IsIpcResponding())
                    {
                        IsDaemonStarting = false;
                        IsDaemonConnected = true;
                        IsAdminRequired = false;
                        DaemonStatusText = "Engine: Ready";
                        DaemonTooltipText = "Everything Engine is online and responding via IPC.";
                        TriggerSearch(immediate: true);
                    }
                    else if (!EverythingDaemonManager.IsCurrentProcessElevated() && EverythingDaemonManager.IsAnySessionEverythingElevated())
                    {
                        if (!IsAdminRequired)
                        {
                            IsAdminRequired = true;
                            DaemonStatusText = "Engine: Elevation Required";
                            StatusText = "Elevation required • Click [🛡️ Run as Admin]";
                            DaemonTooltipText = "Voidtools Everything is running with Administrator rights. Elevation required.";
                        }
                    }
                }
            };
            _daemonWatchdog.Start();
        }

        private bool _isSyncingDrive;

        partial void OnSearchTextChanged(string value)
        {
            if (!_isSyncingDrive)
            {
                SyncDriveFromSearchText(value);
            }
            TriggerSearch(immediate: false);
        }

        private void SyncDriveFromSearchText(string text)
        {
            string trimmed = (text ?? string.Empty).TrimStart();
            if (trimmed.Length >= 2 && char.IsLetter(trimmed[0]) && trimmed[1] == ':')
            {
                string letter = trimmed.Substring(0, 2).ToUpperInvariant();
                var matched = DriveItems.FirstOrDefault(d => d.DriveLetter.StartsWith(letter, StringComparison.OrdinalIgnoreCase));
                if (matched != null && !SelectedDrive.Equals(matched.DriveLetter, StringComparison.OrdinalIgnoreCase))
                {
                    _isSyncingDrive = true;
                    try
                    {
                        SelectedDrive = matched.DriveLetter;
                    }
                    finally
                    {
                        _isSyncingDrive = false;
                    }
                }
            }
            else if (string.IsNullOrWhiteSpace(trimmed))
            {
                if (!SelectedDrive.Equals("All Drives", StringComparison.OrdinalIgnoreCase))
                {
                    _isSyncingDrive = true;
                    try
                    {
                        SelectedDrive = "All Drives";
                    }
                    finally
                    {
                        _isSyncingDrive = false;
                    }
                }
            }
        }

        private static bool IsDriveOnlySearch(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string t = text.Trim();
            return t.Length >= 2 && char.IsLetter(t[0]) && t[1] == ':' && (t.Length == 2 || (t.Length == 3 && (t[2] == '\\' || t[2] == '/')));
        }

        private static string StripLeadingDrive(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            string t = text.Trim();
            if (t.Length >= 2 && char.IsLetter(t[0]) && t[1] == ':')
            {
                int start = 2;
                if (t.Length > 2 && (t[2] == '\\' || t[2] == '/')) start = 3;
                return t.Substring(start).TrimStart();
            }
            return t;
        }

        partial void OnSelectedFilterChanged(SearchFilterType value)
        {
            NotifyFilterProperties();
            TriggerSearch(immediate: true);
        }
        partial void OnCurrentSortChanged(SortOrder value) => TriggerSearch(immediate: true);
        partial void OnMatchCaseChanged(bool value) => TriggerSearch(immediate: true);
        partial void OnMatchWholeWordChanged(bool value) => TriggerSearch(immediate: true);
        partial void OnMatchPathChanged(bool value) => TriggerSearch(immediate: true);
        partial void OnRegexChanged(bool value) => TriggerSearch(immediate: true);
        partial void OnSelectedDriveChanged(string value)
        {
            UpdateDriveSelection(value);
            if (!_isSyncingDrive)
            {
                TriggerSearch(immediate: true);
            }
        }
        partial void OnMaxResultsChanged(int value) => TriggerSearch(immediate: true);

        partial void OnSelectedItemChanged(SearchResultItem? value)
        {
            TriggerPreview(value);
        }

        public void UpdateSelection(IList selectedItems)
        {
            SelectedCount = selectedItems.Count;
            HasMultipleSelected = selectedItems.Count > 1;
            if (selectedItems.Count > 1)
            {
                long totalBytes = 0;
                foreach (var obj in selectedItems)
                {
                    if (obj is SearchResultItem item && !item.IsFolder && item.Size > 0)
                    {
                        totalBytes += item.Size;
                    }
                }
                SelectionSummary = $"{selectedItems.Count:N0} items selected  •  {SearchResultItem.FormatSize(totalBytes)}";
            }
            else
            {
                SelectionSummary = string.Empty;
            }
        }

        public void TriggerSearch(bool immediate = false)
        {
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    if (!immediate)
                    {
                        // Ultra-responsive 18ms debounce for typing
                        await Task.Delay(18, token);
                        if (token.IsCancellationRequested) return;
                    }

                    Application.Current.Dispatcher.Invoke(() => IsSearching = true);

                    var batch = await _searchService.SearchAsync(
                        SearchText,
                        SelectedFilter,
                        CurrentSort,
                        matchCase: MatchCase,
                        matchWholeWord: MatchWholeWord,
                        matchPath: MatchPath,
                        regex: Regex,
                        driveScope: SelectedDrive,
                        maxResults: MaxResults,
                        cancellationToken: token);

                    if (token.IsCancellationRequested) return;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (batch.IsSuccess)
                        {
                            IsDaemonConnected = true;
                            IsAdminRequired = false;
                            DaemonStatusText = "Engine: Ready";
                            DaemonTooltipText = "Voidtools Everything Engine is connected and responding via high-speed IPC.";

                            Results.ReplaceRange(batch.Items);
                            GridResults.ReplaceRange(batch.Items.Take(400));

                            HasResults = Results.Count > 0;

                            TotalResultsSizeDisplay = SearchResultItem.FormatSize(batch.TotalSizeBytes);
                            FileFolderBreakdownDisplay = $"{batch.FileCount:N0} files, {batch.FolderCount:N0} folders";
                            LargestFileDisplay = string.IsNullOrEmpty(batch.LargestFileName)
                                ? string.Empty
                                : $"Largest: {batch.LargestFileName} ({SearchResultItem.FormatSize(batch.LargestFileSizeBytes)})";

                            if (batch.TopExtensions.Count > 0)
                            {
                                TopExtensionsDisplay = string.Join("  •  ", batch.TopExtensions.Select(kv => $"{kv.Key} ({kv.Value:N0})"));
                            }
                            else
                            {
                                TopExtensionsDisplay = string.Empty;
                            }

                            string scopeName = SelectedDrive.Equals("All Drives", StringComparison.OrdinalIgnoreCase)
                                ? "all drives"
                                : SelectedDrive;

                            string displayTotal = batch.TotalCount > MaxResults
                                ? $"{batch.TotalCount:N0} total on {scopeName} (showing {batch.Items.Count:N0})"
                                : $"{batch.TotalCount:N0} items on {scopeName}";

                            StatusText = $"{displayTotal}  •  {TotalResultsSizeDisplay}  •  {batch.ElapsedMilliseconds} ms";
                            StatusTextTooltip = $"Results: {batch.TotalCount:N0} total on {scopeName}\nResponse time: {batch.ElapsedMilliseconds} ms\nFiles: {batch.FileCount:N0}, Folders: {batch.FolderCount:N0}\nTotal Size: {TotalResultsSizeDisplay}";
                        }
                        else
                        {
                            IsDaemonConnected = false;
                            IsAdminRequired = batch.IsAdminMismatch;
                            DaemonStatusText = batch.IsAdminMismatch ? "Engine: Elevation Required" : "Engine: Offline";
                            StatusText = batch.ErrorMessage ?? "Search error";
                            StatusTextTooltip = batch.DiagnosticDetails ?? batch.ErrorMessage ?? "Engine connection error";
                            DaemonTooltipText = StatusTextTooltip;
                        }
                        IsSearching = false;
                    });
                }
                catch (OperationCanceledException)
                {
                    // Normal typing debounce
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusText = $"Error: {ex.Message}";
                        StatusTextTooltip = ex.ToString();
                        IsSearching = false;
                    });
                }
            }, token);
        }

        private void TriggerPreview(SearchResultItem? item)
        {
            _previewCts?.Cancel();
            _previewCts?.Dispose();

            if (item == null || string.IsNullOrWhiteSpace(item.FullPath) || !IsPreviewPaneVisible)
            {
                CurrentPreview = new PreviewData();
                return;
            }

            _previewCts = new CancellationTokenSource();
            var token = _previewCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    if (!item.DateCreated.HasValue && !string.IsNullOrWhiteSpace(item.FullPath))
                    {
                        try
                        {
                            if (File.Exists(item.FullPath))
                            {
                                var fi = new FileInfo(item.FullPath);
                                item.DateCreated = fi.CreationTime;
                                item.DateAccessed = fi.LastAccessTime;
                            }
                            else if (Directory.Exists(item.FullPath))
                            {
                                var di = new DirectoryInfo(item.FullPath);
                                item.DateCreated = di.CreationTime;
                                item.DateAccessed = di.LastAccessTime;
                            }
                        }
                        catch { }
                    }

                    var preview = await FilePreviewService.LoadPreviewAsync(item.FullPath);
                    if (!token.IsCancellationRequested)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CurrentPreview = preview;
                            OnPropertyChanged(nameof(SelectedItem));
                        });
                    }
                }
                catch
                {
                    if (!token.IsCancellationRequested)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CurrentPreview = new PreviewData { HeaderInfo = item.FullPath };
                        });
                    }
                }
            }, token);
        }

        [RelayCommand]
        public void ToggleViewMode()
        {
            IsGridView = !IsGridView;
        }

        [RelayCommand]
        public void ToggleQuickLook()
        {
            if (SelectedItem != null)
            {
                IsQuickLookOpen = !IsQuickLookOpen;
            }
        }

        [RelayCommand]
        public void CloseQuickLook()
        {
            IsQuickLookOpen = false;
        }

        [RelayCommand]
        public void SortBy(string column)
        {
            if (SortColumn == column)
            {
                IsSortAscending = !IsSortAscending;
            }
            else
            {
                SortColumn = column;
                IsSortAscending = true;
            }

            CurrentSort = column switch
            {
                "Name" => IsSortAscending ? SortOrder.NameAscending : SortOrder.NameDescending,
                "Path" => IsSortAscending ? SortOrder.PathAscending : SortOrder.PathDescending,
                "Size" => IsSortAscending ? SortOrder.SizeAscending : SortOrder.SizeDescending,
                "DateModified" => IsSortAscending ? SortOrder.DateModifiedAscending : SortOrder.DateModifiedDescending,
                _ => SortOrder.NameAscending
            };

            NotifySortProperties();
        }

        [RelayCommand]
        public void ApplyPreset(string presetQuery)
        {
            SearchText = presetQuery;
        }

        [RelayCommand]
        public void OpenFile(SearchResultItem? item)
        {
            var target = item ?? SelectedItem;
            if (target != null)
            {
                FileActionService.OpenFile(target.FullPath);
            }
        }

        [RelayCommand]
        public void OpenContainingFolder(SearchResultItem? item)
        {
            var target = item ?? SelectedItem;
            if (target != null)
            {
                FileActionService.OpenContainingFolder(target.FullPath);
            }
        }

        [RelayCommand]
        public void CopyFileName(SearchResultItem? item)
        {
            var target = item ?? SelectedItem;
            if (target != null && !string.IsNullOrEmpty(target.Name))
            {
                Clipboard.SetText(target.Name);
                StatusText = $"Copied '{target.Name}' to clipboard";
            }
        }

        [RelayCommand]
        public void CopyPath(SearchResultItem? item)
        {
            var target = item ?? SelectedItem;
            if (target != null)
            {
                FileActionService.CopyFilesAndPaths(new[] { target.FullPath }, quote: false);
            }
        }

        [RelayCommand]
        public void CopyQuotedPath(SearchResultItem? item)
        {
            var target = item ?? SelectedItem;
            if (target != null)
            {
                FileActionService.CopyFilesAndPaths(new[] { target.FullPath }, quote: true);
            }
        }

        [RelayCommand]
        public void CopySelectedFiles(IList? items)
        {
            var list = new List<string>();
            if (items != null && items.Count > 0)
            {
                foreach (var obj in items)
                {
                    if (obj is SearchResultItem item) list.Add(item.FullPath);
                }
            }
            else if (SelectedItem != null)
            {
                list.Add(SelectedItem.FullPath);
            }

            if (list.Count > 0)
            {
                FileActionService.CopyFilesAndPaths(list, quote: false);
                StatusText = $"Copied {list.Count} item(s) to clipboard (ready to paste in Explorer or text)";
            }
        }

        [RelayCommand]
        public void CopySelectedQuotedPaths(IList? items)
        {
            var list = new List<string>();
            if (items != null && items.Count > 0)
            {
                foreach (var obj in items)
                {
                    if (obj is SearchResultItem item) list.Add(item.FullPath);
                }
            }
            else if (SelectedItem != null)
            {
                list.Add(SelectedItem.FullPath);
            }

            if (list.Count > 0)
            {
                FileActionService.CopyFilesAndPaths(list, quote: true);
                StatusText = $"Copied {list.Count} quoted path(s) to clipboard";
            }
        }

        [RelayCommand]
        public void DeleteSelectedFiles(IList? items)
        {
            var list = new List<string>();
            if (items != null && items.Count > 0)
            {
                foreach (var obj in items)
                {
                    if (obj is SearchResultItem item) list.Add(item.FullPath);
                }
            }
            else if (SelectedItem != null)
            {
                list.Add(SelectedItem.FullPath);
            }

            int count = FileActionService.BatchDeleteToRecycleBin(list);
            if (count > 0)
            {
                TriggerSearch();
                StatusText = $"Moved {count} item(s) to Recycle Bin";
            }
        }

        [RelayCommand]
        public void OpenInTerminal(SearchResultItem? item)
        {
            var target = item ?? SelectedItem;
            if (target != null)
            {
                FileActionService.OpenInTerminal(target.FullPath);
            }
        }

        [RelayCommand]
        public void OpenInVSCode(SearchResultItem? item)
        {
            var target = item ?? SelectedItem;
            if (target != null)
            {
                FileActionService.OpenInVSCode(target.FullPath);
            }
        }

        [RelayCommand]
        public void OpenWith(SearchResultItem? item)
        {
            var target = item ?? SelectedItem;
            if (target != null)
            {
                FileActionService.OpenWith(target.FullPath);
            }
        }

        [RelayCommand]
        public void DeleteToRecycleBin(SearchResultItem? item)
        {
            var target = item ?? SelectedItem;
            if (target != null)
            {
                if (FileActionService.DeleteToRecycleBin(target.FullPath))
                {
                    Results.Remove(target);
                }
            }
        }

        [RelayCommand]
        public void ShowProperties(SearchResultItem? item)
        {
            var target = item ?? SelectedItem;
            if (target != null)
            {
                FileActionService.ShowProperties(target.FullPath);
            }
        }

        [RelayCommand]
        public async Task CalculateChecksumAsync(SearchResultItem? item)
        {
            var target = item ?? SelectedItem;
            if (target == null) return;

            target.IsCalculatingHash = true;
            target.FileHash = "Calculating SHA-256...";
            OnPropertyChanged(nameof(SelectedItem));

            string hash = await FileActionService.ComputeSha256Async(target.FullPath);
            target.FileHash = hash;
            target.IsCalculatingHash = false;
            OnPropertyChanged(nameof(SelectedItem));
        }

        [RelayCommand]
        public void TogglePreviewPane()
        {
            IsPreviewPaneVisible = !IsPreviewPaneVisible;
            if (IsPreviewPaneVisible && SelectedItem != null)
            {
                TriggerPreview(SelectedItem);
            }
        }

        [RelayCommand]
        public void SetFilter(string filterName)
        {
            if (Enum.TryParse<SearchFilterType>(filterName, true, out var f))
            {
                SelectedFilter = f;
            }
        }

        [RelayCommand]
        public void ClearSearch()
        {
            SearchText = string.Empty;
        }

        [RelayCommand]
        public void ToggleMatchCase() => MatchCase = !MatchCase;

        [RelayCommand]
        public void ToggleMatchWholeWord() => MatchWholeWord = !MatchWholeWord;

        [RelayCommand]
        public void ToggleMatchPath() => MatchPath = !MatchPath;

        [RelayCommand]
        public void ToggleRegex() => Regex = !Regex;

        [RelayCommand]
        public void ToggleAnalytics() => IsAnalyticsVisible = !IsAnalyticsVisible;

        [RelayCommand]
        public void ToggleSyntaxHelper() => IsSyntaxHelperOpen = !IsSyntaxHelperOpen;

        [RelayCommand]
        public void InsertSyntaxSnippet(string snippet)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                SearchText = snippet + " ";
            }
            else
            {
                SearchText = SearchText.TrimEnd() + " " + snippet + " ";
            }
            IsSyntaxHelperOpen = false;
        }

        [RelayCommand]
        public void SetMaxResultsLimit(string limit)
        {
            if (int.TryParse(limit, out int val))
            {
                MaxResults = val;
                MaxResultsLabel = val >= 1000 ? $"{val / 1000}k" : val.ToString();
            }
        }

        [RelayCommand]
        public async Task ExportResultsAsync(string format)
        {
            if (Results.Count == 0)
            {
                MessageBox.Show("No results to export.", "Everyview", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Search Results",
                FileName = $"everyview_results_{DateTime.Now:yyyyMMdd_HHmmss}.{format.ToLower()}",
                Filter = format.ToLower() switch
                {
                    "csv" => "CSV Spreadsheet (*.csv)|*.csv",
                    "json" => "JSON File (*.json)|*.json",
                    _ => "Plain Text List (*.txt)|*.txt"
                }
            };

            if (sfd.ShowDialog() == true)
            {
                await FileActionService.ExportResultsToFileAsync(Results, sfd.FileName, format);
                StatusText = $"Exported {Results.Count:N0} results to {Path.GetFileName(sfd.FileName)}";
            }
        }

        [RelayCommand]
        public void SelectDrive(string driveLetter)
        {
            if (string.IsNullOrWhiteSpace(driveLetter)) return;

            _isSyncingDrive = true;
            try
            {
                if (driveLetter.Equals("All Drives", StringComparison.OrdinalIgnoreCase))
                {
                    SelectedDrive = "All Drives";
                    if (IsDriveOnlySearch(SearchText))
                    {
                        SearchText = string.Empty;
                    }
                    StatusText = "Scanning all drives...";
                }
                else
                {
                    SelectedDrive = driveLetter;
                    string cleanDrive = driveLetter.TrimEnd('\\').ToLowerInvariant();
                    if (!cleanDrive.EndsWith(":"))
                    {
                        cleanDrive += ":";
                    }

                    if (string.IsNullOrWhiteSpace(SearchText) || IsDriveOnlySearch(SearchText))
                    {
                        SearchText = cleanDrive;
                    }
                    else
                    {
                        string remaining = StripLeadingDrive(SearchText);
                        SearchText = string.IsNullOrWhiteSpace(remaining) ? cleanDrive : $"{cleanDrive} {remaining}";
                    }
                    StatusText = $"Scanning {driveLetter}...";
                }
            }
            finally
            {
                _isSyncingDrive = false;
            }

            TriggerSearch(immediate: true);
        }

        [RelayCommand]
        public void OpenSelectedDriveInExplorer()
        {
            try
            {
                string target = SelectedDrive.Equals("All Drives", StringComparison.OrdinalIgnoreCase)
                    ? "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}"
                    : $"{SelectedDrive}\\";
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = target,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        [RelayCommand]
        public void OpenNativeEverything()
        {
            string? exe = EverythingDaemonManager.FindEverythingExecutable();
            if (!string.IsNullOrEmpty(exe))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exe,
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        }

        [RelayCommand]
        public async Task ReconnectDaemonAsync()
        {
            IsDaemonStarting = true;
            DaemonStatusText = "Connecting to Everything Engine...";
            DaemonTooltipText = "Connecting to Voidtools Everything IPC interface...";
            StatusText = "Connecting to Everything Engine...";
            
            bool ready = await EverythingDaemonManager.EnsureDaemonRunningAsync(
                timeoutMs: 12000,
                progressCallback: msg =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DaemonStatusText = msg;
                        StatusText = msg;
                    });
                });

            IsDaemonStarting = false;
            IsDaemonConnected = ready;

            if (ready)
            {
                IsAdminRequired = false;
                DaemonStatusText = "Engine: Ready";
                DaemonTooltipText = "Everything Engine is online and responding via IPC.";
                TriggerSearch(immediate: true);
            }
            else
            {
                bool inSession = EverythingDaemonManager.IsEverythingProcessRunningInSession();
                bool isCurrentAdmin = EverythingDaemonManager.IsCurrentProcessElevated();
                bool isTargetAdmin = EverythingDaemonManager.IsAnySessionEverythingElevated();

                if (inSession && !isCurrentAdmin && isTargetAdmin)
                {
                    IsAdminRequired = true;
                    DaemonStatusText = "Engine: Elevation Required";
                    StatusText = "Elevation required • Click [🛡️ Run as Admin]";
                    StatusTextTooltip = "Voidtools Everything is running with Administrator rights.\nWindows blocks IPC communication from standard applications.\n\nClick [🛡️ Run as Admin] to elevate Everyview, or configure Everything options.";
                    DaemonTooltipText = StatusTextTooltip;
                }
                else
                {
                    IsAdminRequired = false;
                    DaemonStatusText = "Engine: Offline";
                    StatusText = "Everything Engine offline • Click Reconnect";
                    StatusTextTooltip = "Everything background engine is not responding. Click Reconnect to restart it.";
                    DaemonTooltipText = StatusTextTooltip;
                }
            }
        }

        [RelayCommand]
        public void RelaunchAsAdmin()
        {
            EverythingDaemonManager.RelaunchAsAdministrator();
        }

        [RelayCommand]
        public async Task RestartEverythingEngineAsync()
        {
            IsDaemonStarting = true;
            DaemonStatusText = "Restarting Everything...";
            StatusText = "Stopping and restarting Everything Engine...";
            await EverythingDaemonManager.RestartEverythingClientAsync();
            await Task.Delay(1000);
            await ReconnectDaemonAsync();
        }

        [RelayCommand]
        public void OpenEverythingOptions()
        {
            EverythingDaemonManager.OpenEverythingOptions("general");
        }
    }
}
