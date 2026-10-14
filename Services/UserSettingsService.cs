using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EverythingModern.Models;

namespace EverythingModern.Services
{
    public class UserSettingsData
    {
        public List<string> SearchHistory { get; set; } = new();
        public List<SearchBookmark> Bookmarks { get; set; } = new();
        public int MaxResults { get; set; } = 1000;
        public bool IsAnalyticsVisible { get; set; } = true;
        public bool IsPreviewPaneVisible { get; set; } = true;
        public bool IsGridView { get; set; } = false;
        public bool IsCompactView { get; set; } = false;
        public bool MinimizeToTray { get; set; } = true;
        public string SelectedDrive { get; set; } = "All Drives";
    }

    public static class UserSettingsService
    {
        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Everyview");

        private static readonly string SettingsFile = Path.Combine(SettingsDir, "user_settings.json");
        private static readonly object _fileLock = new();
        private static UserSettingsData _current = new();
        private static CancellationTokenSource? _saveCts;

        public static UserSettingsData Current => _current;

        static UserSettingsService()
        {
            Load();
        }

        public static void Load()
        {
            lock (_fileLock)
            {
                try
                {
                    if (File.Exists(SettingsFile))
                    {
                        string json = File.ReadAllText(SettingsFile);
                        var data = JsonSerializer.Deserialize<UserSettingsData>(json);
                        if (data != null)
                        {
                            _current = data;
                        }
                    }
                }
                catch { }

                // Seed useful defaults if empty
                if (_current.Bookmarks == null || _current.Bookmarks.Count == 0)
                {
                    _current.Bookmarks = GetDefaultBookmarks();
                }

                _current.SearchHistory ??= new List<string>();
            }
        }

        public static void SaveDebounced()
        {
            _saveCts?.Cancel();
            _saveCts?.Dispose();
            _saveCts = new CancellationTokenSource();
            var token = _saveCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(500, token);
                    if (token.IsCancellationRequested) return;

                    SaveNow();
                }
                catch { }
            }, token);
        }

        public static void SaveNow()
        {
            lock (_fileLock)
            {
                try
                {
                    if (!Directory.Exists(SettingsDir))
                    {
                        Directory.CreateDirectory(SettingsDir);
                    }

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string json = JsonSerializer.Serialize(_current, options);
                    File.WriteAllText(SettingsFile, json);
                }
                catch { }
            }
        }

        public static void AddSearchHistory(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;
            string trimmed = query.Trim();

            lock (_fileLock)
            {
                _current.SearchHistory.RemoveAll(q => string.Equals(q, trimmed, StringComparison.OrdinalIgnoreCase));
                _current.SearchHistory.Insert(0, trimmed);

                if (_current.SearchHistory.Count > 30)
                {
                    _current.SearchHistory = _current.SearchHistory.Take(30).ToList();
                }
            }

            SaveDebounced();
        }

        public static void ClearSearchHistory()
        {
            lock (_fileLock)
            {
                _current.SearchHistory.Clear();
            }
            SaveNow();
        }

        public static void AddBookmark(SearchBookmark bookmark)
        {
            if (bookmark == null || string.IsNullOrWhiteSpace(bookmark.Query)) return;

            lock (_fileLock)
            {
                _current.Bookmarks.RemoveAll(b => string.Equals(b.Title, bookmark.Title, StringComparison.OrdinalIgnoreCase));
                _current.Bookmarks.Insert(0, bookmark);
            }
            SaveNow();
        }

        public static void RemoveBookmark(string title)
        {
            lock (_fileLock)
            {
                _current.Bookmarks.RemoveAll(b => string.Equals(b.Title, title, StringComparison.OrdinalIgnoreCase));
            }
            SaveNow();
        }

        public static List<SearchBookmark> GetDefaultBookmarks()
        {
            return new List<SearchBookmark>
            {
                new("🔍 Find Duplicates", "dupe:name;size", "Find files with duplicate names and sizes", "Cleanup", "DocumentCopy20"),
                new("🗑️ Empty Folders", "empty: folder:", "Locate empty directories taking up space", "Cleanup", "Folder20"),
                new("🔥 Gigantic (>1GB)", "size:>1gb", "Highlight files over 1 GB", "Storage", "Fire20"),
                new("🕒 Modified Today", "dm:today", "Files changed today", "Recent", "Clock20"),
                new("📅 Modified This Week", "dm:thisweek", "Files changed during this week", "Recent", "Calendar20"),
                new("💻 Git Repositories", ".git folder:", "Find all git projects across disks", "Dev", "BranchFork20"),
                new("📥 Downloads Folder", "downloads:", "Jump directly to downloaded files", "General", "ArrowDownload20"),
                new("📦 Archives & Zips", "ext:zip;7z;rar;tar;gz;iso", "Compressed archives and disk images", "Files", "Archive20"),
                new("🎬 4K & Big Movies", "ext:mp4;mkv;mov size:>2gb", "High-definition movies and recordings", "Media", "Video20"),
                new("📄 Documents & PDFs", "ext:pdf;docx;xlsx;pptx", "Work documents, spreadsheets, presentations", "Work", "Document20"),
                new("⚡ Scripts & Batch", "ext:ps1;bat;cmd;sh;py", "Automation scripts and batch files", "Dev", "WindowConsole20"),
                new("📷 RAW & Photos", "ext:cr2;nef;arw;raw;psd;png;jpg", "High-res images and photo captures", "Media", "Image20")
            };
        }
    }
}
