using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EverythingModern.Models;

namespace EverythingModern.Services
{
    public enum SearchFilterType
    {
        All,
        Files,
        Folders,
        Documents,
        Pictures,
        Videos,
        Audio,
        Code,
        Compressed,
        Executables,
        Duplicates,
        EmptyFolders
    }

    public enum SortOrder
    {
        NameAscending,
        NameDescending,
        PathAscending,
        PathDescending,
        SizeAscending,
        SizeDescending,
        DateModifiedAscending,
        DateModifiedDescending
    }

    public class SearchResultBatch
    {
        public List<SearchResultItem> Items { get; set; } = new();
        public uint TotalCount { get; set; }
        public long TotalSizeBytes { get; set; }
        public int FileCount { get; set; }
        public int FolderCount { get; set; }
        public string? LargestFileName { get; set; }
        public long LargestFileSizeBytes { get; set; }
        public List<KeyValuePair<string, int>> TopExtensions { get; set; } = new();
        public long ElapsedMilliseconds { get; set; }
        public bool IsSuccess { get; set; }
        public bool IsAdminMismatch { get; set; }
        public string? ErrorMessage { get; set; }
        public string? DiagnosticDetails { get; set; }
    }

    public class EverythingSearchService
    {
        private readonly object _lock = new();

        public Task<SearchResultBatch> SearchAsync(
            string searchText,
            SearchFilterType filter,
            SortOrder sortOrder,
            bool matchCase = false,
            bool matchWholeWord = false,
            bool matchPath = false,
            bool regex = false,
            string? driveScope = null,
            int maxResults = 25000,
            CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                var sw = Stopwatch.StartNew();
                var result = new SearchResultBatch();

                lock (_lock)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        result.IsSuccess = false;
                        return result;
                    }

                    string effectiveQuery = BuildQuery(searchText, filter, driveScope);

                    EverythingInterop.Everything_Reset();
                    EverythingInterop.Everything_SetSearchW(effectiveQuery);

                    // Search flags
                    EverythingInterop.Everything_SetMatchCase(matchCase);
                    EverythingInterop.Everything_SetMatchWholeWord(matchWholeWord);
                    EverythingInterop.Everything_SetMatchPath(matchPath);
                    EverythingInterop.Everything_SetRegex(regex);

                    // Configure sort
                    uint nativeSort = sortOrder switch
                    {
                        SortOrder.NameAscending => EverythingInterop.EVERYTHING_SORT_NAME_ASCENDING,
                        SortOrder.NameDescending => EverythingInterop.EVERYTHING_SORT_NAME_DESCENDING,
                        SortOrder.PathAscending => EverythingInterop.EVERYTHING_SORT_PATH_ASCENDING,
                        SortOrder.PathDescending => EverythingInterop.EVERYTHING_SORT_PATH_DESCENDING,
                        SortOrder.SizeAscending => EverythingInterop.EVERYTHING_SORT_SIZE_ASCENDING,
                        SortOrder.SizeDescending => EverythingInterop.EVERYTHING_SORT_SIZE_DESCENDING,
                        SortOrder.DateModifiedAscending => EverythingInterop.EVERYTHING_SORT_DATE_MODIFIED_ASCENDING,
                        SortOrder.DateModifiedDescending => EverythingInterop.EVERYTHING_SORT_DATE_MODIFIED_DESCENDING,
                        _ => EverythingInterop.EVERYTHING_SORT_NAME_ASCENDING
                    };
                    EverythingInterop.Everything_SetSort(nativeSort);

                    // Configure request flags - ONLY request in-memory indexed attributes for microsecond response times
                    uint requestFlags =
                        EverythingInterop.EVERYTHING_REQUEST_FILE_NAME |
                        EverythingInterop.EVERYTHING_REQUEST_PATH |
                        EverythingInterop.EVERYTHING_REQUEST_EXTENSION |
                        EverythingInterop.EVERYTHING_REQUEST_SIZE |
                        EverythingInterop.EVERYTHING_REQUEST_DATE_MODIFIED |
                        EverythingInterop.EVERYTHING_REQUEST_DATE_CREATED |
                        EverythingInterop.EVERYTHING_REQUEST_ATTRIBUTES;

                    EverythingInterop.Everything_SetRequestFlags(requestFlags);
                    EverythingInterop.Everything_SetMax((uint)Math.Max(100, maxResults));
                    EverythingInterop.Everything_SetOffset(0);

                    // Execute query
                    bool success = EverythingInterop.Everything_QueryW(true);
                    if (!success)
                    {
                        uint err = EverythingInterop.Everything_GetLastError();
                        if (err == 2)
                        {
                            if (!EverythingDaemonManager.IsEverythingProcessRunningInSession())
                            {
                                EverythingDaemonManager.LaunchEverythingClient();
                            }
                            else if (EverythingDaemonManager.GetUserSessionProcessCount() > 1)
                            {
                                EverythingDaemonManager.CleanupDuplicateInstances();
                            }

                            for (int retry = 0; retry < 6 && !success; retry++)
                            {
                                Thread.Sleep(150);
                                if (EverythingInterop.Everything_IsDBLoaded())
                                {
                                    success = EverythingInterop.Everything_QueryW(true);
                                }
                                else
                                {
                                    success = EverythingInterop.Everything_QueryW(true);
                                }
                            }
                        }

                        if (!success)
                        {
                            err = EverythingInterop.Everything_GetLastError();
                            result.IsSuccess = false;
                            if (err == 2)
                            {
                                bool inSession = EverythingDaemonManager.IsEverythingProcessRunningInSession();
                                bool isCurrentAdmin = EverythingDaemonManager.IsCurrentProcessElevated();
                                bool isTargetAdmin = EverythingDaemonManager.IsAnySessionEverythingElevated();
                                bool ipcWindowPresent = EverythingDaemonManager.IsIpcWindowPresent();

                                if (inSession && !isCurrentAdmin && isTargetAdmin)
                                {
                                    result.IsAdminMismatch = true;
                                    result.ErrorMessage = "Elevation required • Click [🛡️ Run as Admin] to connect";
                                    result.DiagnosticDetails = "Voidtools Everything is running with Administrator rights.\nWindows blocks IPC communication from standard-rights applications.\n\nSolutions:\n• Click [🛡️ Run as Administrator] to elevate Everyview.\n• Or in Everything: Tools > Options > General, check 'Everything Service' and uncheck 'Run as administrator'.";
                                }
                                else if (inSession && ipcWindowPresent && !EverythingInterop.Everything_IsDBLoaded())
                                {
                                    result.ErrorMessage = "Everything is loading database into memory • Please wait";
                                    result.DiagnosticDetails = "The Everything process is active and indexing NTFS volumes. Results will appear automatically in moments.";
                                }
                                else if (inSession)
                                {
                                    result.ErrorMessage = "Everything daemon busy • Initializing database...";
                                    result.DiagnosticDetails = "The Everything process is active in your session. If Everything was just launched, the database will be loaded momentarily.";
                                }
                                else
                                {
                                    result.ErrorMessage = "Everything Engine offline • Click Reconnect to launch";
                                    result.DiagnosticDetails = "Voidtools Everything client is not currently running in your user session. Click Reconnect to launch it in background.";
                                }
                            }
                            else if (err == 1)
                            {
                                result.ErrorMessage = "Out of memory";
                                result.DiagnosticDetails = "Everything engine ran out of memory processing this query.";
                            }
                            else if (err == 7 || err == 8)
                            {
                                result.ErrorMessage = "Invalid regex pattern • Check syntax";
                                result.DiagnosticDetails = "The regular expression entered contains syntax errors or unclosed brackets.";
                            }
                            else
                            {
                                result.ErrorMessage = $"Query failed (error {err})";
                                result.DiagnosticDetails = $"Everything returned error code {err}.";
                            }
                            return result;
                        }
                    }

                    uint count = EverythingInterop.Everything_GetNumResults();
                    uint total = EverythingInterop.Everything_GetTotResults();
                    result.TotalCount = total;

                    var items = new List<SearchResultItem>((int)count);
                    var extCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    long totalBytes = 0;
                    int files = 0;
                    int folders = 0;
                    long maxFileSize = 0;
                    string? maxFileName = null;

                    for (uint i = 0; i < count; i++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            result.IsSuccess = false;
                            return result;
                        }

                        IntPtr pName = EverythingInterop.Everything_GetResultFileNameW(i);
                        string fileName = Marshal.PtrToStringUni(pName) ?? string.Empty;

                        IntPtr pPath = EverythingInterop.Everything_GetResultPathW(i);
                        string filePath = Marshal.PtrToStringUni(pPath) ?? string.Empty;

                        IntPtr pExt = EverythingInterop.Everything_GetResultExtensionW(i);
                        string ext = Marshal.PtrToStringUni(pExt) ?? string.Empty;
                        if (!string.IsNullOrEmpty(ext))
                        {
                            ext = "." + ext;
                        }

                        bool isFolder = EverythingInterop.Everything_IsFolderResult(i);

                        EverythingInterop.Everything_GetResultSize(i, out long size);
                        EverythingInterop.Everything_GetResultDateModified(i, out long ftMod);
                        EverythingInterop.Everything_GetResultDateCreated(i, out long ftCreated);
                        uint attrs = EverythingInterop.Everything_GetResultAttributes(i);

                        DateTime? dtModified = null;
                        if (ftMod > 0)
                        {
                            try { dtModified = DateTime.FromFileTime(ftMod); } catch { }
                        }

                        DateTime? dtCreated = null;
                        if (ftCreated > 0)
                        {
                            try { dtCreated = DateTime.FromFileTime(ftCreated); } catch { }
                        }

                        string fullPath = string.IsNullOrEmpty(filePath)
                            ? fileName
                            : Path.Combine(filePath, fileName);

                        if (isFolder)
                        {
                            folders++;
                        }
                        else
                        {
                            files++;
                            if (size > 0) totalBytes += size;
                            if (size > maxFileSize)
                            {
                                maxFileSize = size;
                                maxFileName = fileName;
                            }
                            if (!string.IsNullOrEmpty(ext))
                            {
                                extCounts.TryGetValue(ext, out int c);
                                extCounts[ext] = c + 1;
                            }
                        }

                        items.Add(new SearchResultItem
                        {
                            Name = fileName,
                            Path = filePath,
                            FullPath = fullPath,
                            Extension = ext,
                            IsFolder = isFolder,
                            Size = size,
                            DateModified = dtModified,
                            DateCreated = dtCreated,
                            DateAccessed = null,
                            Attributes = attrs != 0 ? attrs : (isFolder ? 0x10u : 0u)
                        });
                    }

                    sw.Stop();
                    result.Items = items;
                    result.TotalSizeBytes = totalBytes;
                    result.FileCount = files;
                    result.FolderCount = folders;
                    result.LargestFileName = maxFileName;
                    result.LargestFileSizeBytes = maxFileSize;
                    result.TopExtensions = extCounts.OrderByDescending(kv => kv.Value).Take(4).ToList();
                    result.ElapsedMilliseconds = sw.ElapsedMilliseconds;
                    result.IsSuccess = true;
                    return result;
                }
            }, cancellationToken);
        }

        private static string BuildQuery(string userQuery, SearchFilterType filter, string? driveScope)
        {
            string trimmed = (userQuery ?? string.Empty).Trim();

            // Drive scope handling: Voidtools syntax is "c:" for entire drive, "c: query" for scoped query
            string drivePart = string.Empty;
            if (!string.IsNullOrWhiteSpace(driveScope) && !driveScope.Equals("All Drives", StringComparison.OrdinalIgnoreCase))
            {
                drivePart = driveScope.TrimEnd('\\');
                if (!drivePart.EndsWith(":"))
                {
                    drivePart += ":";
                }
                drivePart = drivePart.ToLowerInvariant();
            }

            string baseQuery;
            if (!string.IsNullOrEmpty(drivePart))
            {
                if (string.IsNullOrEmpty(trimmed))
                {
                    // User chose a drive: "c:" scans and returns the entire drive!
                    baseQuery = drivePart;
                }
                else if (trimmed.StartsWith(drivePart, StringComparison.OrdinalIgnoreCase))
                {
                    baseQuery = trimmed;
                }
                else
                {
                    baseQuery = $"{drivePart} {trimmed}";
                }
            }
            else
            {
                baseQuery = trimmed;
            }

            string filterSuffix = filter switch
            {
                SearchFilterType.Files => " file:",
                SearchFilterType.Folders => " folder:",
                SearchFilterType.Documents => " ext:doc;docx;docm;dot;dotx;epub;mobi;pdf;pps;ppt;pptx;rtf;txt;wps;xls;xlsx;xlsm;csv",
                SearchFilterType.Pictures => " ext:bmp;gif;ico;jpeg;jpg;png;psd;svg;tga;tif;tiff;webp;avif;heic;raw",
                SearchFilterType.Videos => " ext:3g2;3gp;avi;flv;m4v;mkv;mov;mp4;mpeg;mpg;rm;rmvb;vob;webm;wmv;ts",
                SearchFilterType.Audio => " ext:aac;ac3;aif;aifc;aiff;au;cda;dts;fla;flac;m4a;mid;midi;mka;mp2;mp3;mpa;ogg;opus;ra;rmi;wav;wma",
                SearchFilterType.Code => " ext:c;cpp;cs;css;go;h;hpp;html;java;js;json;jsx;kt;lua;md;php;py;rb;rs;sql;swift;ts;tsx;xml;yaml;yml;sh;ps1",
                SearchFilterType.Compressed => " ext:7z;ace;arj;bz2;cab;gz;gzip;jar;r00;rar;tar;tgz;z;zip;iso",
                SearchFilterType.Executables => " ext:bat;cmd;exe;msi;msp;scr;ps1;vbs;com",
                SearchFilterType.Duplicates => " dupe:name;size",
                SearchFilterType.EmptyFolders => " empty: folder:",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(baseQuery))
            {
                return string.IsNullOrEmpty(filterSuffix) ? "" : filterSuffix.Trim();
            }

            return string.IsNullOrEmpty(filterSuffix) ? baseQuery : $"{baseQuery}{filterSuffix}";
        }
    }
}
