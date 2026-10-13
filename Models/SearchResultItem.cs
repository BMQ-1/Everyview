using System;
using System.IO;
using System.Windows.Media;
using EverythingModern.Services;

namespace EverythingModern.Models
{
    public class SearchResultItem
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime? DateModified { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateAccessed { get; set; }
        public bool IsFolder { get; set; }
        public uint Attributes { get; set; }

        public string? FileHash { get; set; }
        public bool IsCalculatingHash { get; set; }

        public string SizeDisplay => IsFolder ? string.Empty : FormatSize(Size);

        public string SizeDetailedDisplay => IsFolder ? "Folder" : $"{FormatSize(Size)} ({Size:N0} bytes)";

        public string DateModifiedDisplay => DateModified.HasValue
            ? DateModified.Value.ToString("yyyy-MM-dd HH:mm:ss")
            : string.Empty;

        public string DateCreatedDisplay => DateCreated.HasValue
            ? DateCreated.Value.ToString("yyyy-MM-dd HH:mm:ss")
            : "Unknown";

        public string DateAccessedDisplay => DateAccessed.HasValue
            ? DateAccessed.Value.ToString("yyyy-MM-dd HH:mm:ss")
            : "Unknown";

        public string TypeDisplay
        {
            get
            {
                if (IsFolder) return "File folder";
                if (string.IsNullOrEmpty(Extension)) return "File";
                return $"{Extension.TrimStart('.').ToUpperInvariant()} File";
            }
        }

        public string ExtensionBadge
        {
            get
            {
                if (IsFolder) return "DIR";
                if (string.IsNullOrEmpty(Extension)) return "FILE";
                return Extension.TrimStart('.').ToUpperInvariant();
            }
        }

        private static readonly SolidColorBrush FolderBg = FreezeBrush(new SolidColorBrush(Color.FromArgb(0x35, 0xEA, 0xB3, 0x08)));
        private static readonly SolidColorBrush FolderFg = FreezeBrush(new SolidColorBrush(Color.FromRgb(0xFA, 0xCC, 0x15)));
        private static readonly SolidColorBrush FolderBorder = FreezeBrush(new SolidColorBrush(Color.FromArgb(0x55, 0xFA, 0xCC, 0x15)));

        private static readonly SolidColorBrush ExeBg = FreezeBrush(new SolidColorBrush(Color.FromArgb(0x35, 0xA8, 0x55, 0xF7)));
        private static readonly SolidColorBrush ExeFg = FreezeBrush(new SolidColorBrush(Color.FromRgb(0xC0, 0x84, 0xFC)));
        private static readonly SolidColorBrush ExeBorder = FreezeBrush(new SolidColorBrush(Color.FromArgb(0x55, 0xC0, 0x84, 0xFC)));

        private static readonly SolidColorBrush ImageBg = FreezeBrush(new SolidColorBrush(Color.FromArgb(0x35, 0x10, 0xB9, 0x81)));
        private static readonly SolidColorBrush ImageFg = FreezeBrush(new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99)));
        private static readonly SolidColorBrush ImageBorder = FreezeBrush(new SolidColorBrush(Color.FromArgb(0x55, 0x34, 0xD3, 0x99)));

        private static readonly SolidColorBrush VideoBg = FreezeBrush(new SolidColorBrush(Color.FromArgb(0x35, 0xF4, 0x3F, 0x5E)));
        private static readonly SolidColorBrush VideoFg = FreezeBrush(new SolidColorBrush(Color.FromRgb(0xFB, 0x71, 0x85)));
        private static readonly SolidColorBrush VideoBorder = FreezeBrush(new SolidColorBrush(Color.FromArgb(0x55, 0xFB, 0x71, 0x85)));

        private static readonly SolidColorBrush AudioBg = FreezeBrush(new SolidColorBrush(Color.FromArgb(0x35, 0xEC, 0x48, 0x99)));
        private static readonly SolidColorBrush AudioFg = FreezeBrush(new SolidColorBrush(Color.FromRgb(0xF4, 0x72, 0xB6)));
        private static readonly SolidColorBrush AudioBorder = FreezeBrush(new SolidColorBrush(Color.FromArgb(0x55, 0xF4, 0x72, 0xB6)));

        private static readonly SolidColorBrush ArchiveBg = FreezeBrush(new SolidColorBrush(Color.FromArgb(0x35, 0xF5, 0x9E, 0x0B)));
        private static readonly SolidColorBrush ArchiveFg = FreezeBrush(new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24)));
        private static readonly SolidColorBrush ArchiveBorder = FreezeBrush(new SolidColorBrush(Color.FromArgb(0x55, 0xFB, 0xBF, 0x24)));

        private static readonly SolidColorBrush CodeBg = FreezeBrush(new SolidColorBrush(Color.FromArgb(0x35, 0x06, 0xB6, 0xD4)));
        private static readonly SolidColorBrush CodeFg = FreezeBrush(new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8)));
        private static readonly SolidColorBrush CodeBorder = FreezeBrush(new SolidColorBrush(Color.FromArgb(0x55, 0x38, 0xBD, 0xF8)));

        private static readonly SolidColorBrush DocBg = FreezeBrush(new SolidColorBrush(Color.FromArgb(0x35, 0x3B, 0x82, 0xF6)));
        private static readonly SolidColorBrush DocFg = FreezeBrush(new SolidColorBrush(Color.FromRgb(0x60, 0xA5, 0xFA)));
        private static readonly SolidColorBrush DocBorder = FreezeBrush(new SolidColorBrush(Color.FromArgb(0x55, 0x60, 0xA5, 0xFA)));

        private static readonly SolidColorBrush DefaultBg = FreezeBrush(new SolidColorBrush(Color.FromArgb(0x35, 0x64, 0x74, 0x8B)));
        private static readonly SolidColorBrush DefaultFg = FreezeBrush(new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)));
        private static readonly SolidColorBrush DefaultBorder = FreezeBrush(new SolidColorBrush(Color.FromArgb(0x55, 0x94, 0xA3, 0xB8)));

        private static SolidColorBrush FreezeBrush(SolidColorBrush brush)
        {
            if (brush.CanFreeze) brush.Freeze();
            return brush;
        }

        public SolidColorBrush BadgeBackground
        {
            get
            {
                if (IsFolder) return FolderBg;
                string e = Extension.TrimStart('.').ToLowerInvariant();
                return e switch
                {
                    "exe" or "bat" or "cmd" or "msi" or "msp" or "scr" or "ps1" or "vbs" => ExeBg,
                    "png" or "jpg" or "jpeg" or "gif" or "webp" or "svg" or "ico" or "bmp" or "tif" or "tiff" or "avif" or "heic" or "raw" or "psd" => ImageBg,
                    "mp4" or "mkv" or "mov" or "avi" or "webm" or "wmv" or "flv" or "m4v" or "ts" or "3gp" => VideoBg,
                    "mp3" or "wav" or "flac" or "m4a" or "aac" or "ogg" or "wma" or "opus" or "mid" or "midi" => AudioBg,
                    "zip" or "rar" or "7z" or "tar" or "gz" or "iso" or "bz2" or "cab" or "jar" => ArchiveBg,
                    "cs" or "js" or "ts" or "jsx" or "tsx" or "py" or "json" or "html" or "css" or "cpp" or "c" or "h" or "hpp" or "sql" or "xml" or "yaml" or "yml" or "sh" or "lua" or "go" or "rs" or "php" or "rb" or "kt" or "swift" or "md" => CodeBg,
                    "pdf" or "docx" or "doc" or "xlsx" or "xls" or "csv" or "pptx" or "ppt" or "txt" or "rtf" or "epub" => DocBg,
                    _ => DefaultBg
                };
            }
        }

        public SolidColorBrush BadgeForeground
        {
            get
            {
                if (IsFolder) return FolderFg;
                string e = Extension.TrimStart('.').ToLowerInvariant();
                return e switch
                {
                    "exe" or "bat" or "cmd" or "msi" or "msp" or "scr" or "ps1" or "vbs" => ExeFg,
                    "png" or "jpg" or "jpeg" or "gif" or "webp" or "svg" or "ico" or "bmp" or "tif" or "tiff" or "avif" or "heic" or "raw" or "psd" => ImageFg,
                    "mp4" or "mkv" or "mov" or "avi" or "webm" or "wmv" or "flv" or "m4v" or "ts" or "3gp" => VideoFg,
                    "mp3" or "wav" or "flac" or "m4a" or "aac" or "ogg" or "wma" or "opus" or "mid" or "midi" => AudioFg,
                    "zip" or "rar" or "7z" or "tar" or "gz" or "iso" or "bz2" or "cab" or "jar" => ArchiveFg,
                    "cs" or "js" or "ts" or "jsx" or "tsx" or "py" or "json" or "html" or "css" or "cpp" or "c" or "h" or "hpp" or "sql" or "xml" or "yaml" or "yml" or "sh" or "lua" or "go" or "rs" or "php" or "rb" or "kt" or "swift" or "md" => CodeFg,
                    "pdf" or "docx" or "doc" or "xlsx" or "xls" or "csv" or "pptx" or "ppt" or "txt" or "rtf" or "epub" => DocFg,
                    _ => DefaultFg
                };
            }
        }

        public SolidColorBrush BadgeBorder
        {
            get
            {
                if (IsFolder) return FolderBorder;
                string e = Extension.TrimStart('.').ToLowerInvariant();
                return e switch
                {
                    "exe" or "bat" or "cmd" or "msi" or "msp" or "scr" or "ps1" or "vbs" => ExeBorder,
                    "png" or "jpg" or "jpeg" or "gif" or "webp" or "svg" or "ico" or "bmp" or "tif" or "tiff" or "avif" or "heic" or "raw" or "psd" => ImageBorder,
                    "mp4" or "mkv" or "mov" or "avi" or "webm" or "wmv" or "flv" or "m4v" or "ts" or "3gp" => VideoBorder,
                    "mp3" or "wav" or "flac" or "m4a" or "aac" or "ogg" or "wma" or "opus" or "mid" or "midi" => AudioBorder,
                    "zip" or "rar" or "7z" or "tar" or "gz" or "iso" or "bz2" or "cab" or "jar" => ArchiveBorder,
                    "cs" or "js" or "ts" or "jsx" or "tsx" or "py" or "json" or "html" or "css" or "cpp" or "c" or "h" or "hpp" or "sql" or "xml" or "yaml" or "yml" or "sh" or "lua" or "go" or "rs" or "php" or "rb" or "kt" or "swift" or "md" => CodeBorder,
                    "pdf" or "docx" or "doc" or "xlsx" or "xls" or "csv" or "pptx" or "ppt" or "txt" or "rtf" or "epub" => DocBorder,
                    _ => DefaultBorder
                };
            }
        }

        private ImageSource? _icon;
        public ImageSource? Icon
        {
            get
            {
                if (_icon == null)
                {
                    _icon = ShellIconHelper.GetIcon(FullPath, IsFolder, Extension);
                }
                return _icon;
            }
        }

        public static string FormatSize(long bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }
    }
}
