using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using EverythingModern.Models;

namespace EverythingModern.Services
{
    public enum PreviewType
    {
        None,
        Image,
        Text,
        Audio,
        Hex,
        Folder,
        Generic
    }

    public class PreviewData
    {
        public PreviewType Type { get; set; } = PreviewType.None;
        public BitmapImage? ImageSource { get; set; }
        public string? TextContent { get; set; }
        public string? HeaderInfo { get; set; }
        public string? ExtraDetail { get; set; }
        public int LineCount { get; set; }
        public string? AudioFilePath { get; set; }
        public string? HexContent { get; set; }
    }

    public static class FilePreviewService
    {
        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".ico", ".webp", ".tif", ".tiff", ".svg"
        };

        private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".m4a", ".flac", ".ogg", ".wma", ".aac", ".opus", ".mid", ".midi"
        };

        private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".txt", ".cs", ".json", ".xml", ".xaml", ".py", ".js", ".ts", ".tsx", ".jsx", ".html", ".css",
            ".md", ".log", ".ini", ".conf", ".yaml", ".yml", ".c", ".cpp", ".h", ".hpp", ".cc", ".cxx",
            ".sql", ".rs", ".go", ".java", ".kt", ".kts", ".sh", ".bash", ".zsh", ".bat", ".ps1", ".cmd", ".csv", ".tsv",
            ".config", ".gitignore", ".gitattributes", ".env", ".toml", ".props", ".targets", ".editorconfig",
            ".vue", ".svelte", ".dart", ".zig", ".swift", ".lua", ".rb", ".php", ".proto", ".graphql", ".cmake",
            ".dockerfile", ".r", ".scala", ".pl", ".tex", ".asm", ".s", ".lock"
        };

        private static readonly HashSet<string> KnownTextFilenames = new(StringComparer.OrdinalIgnoreCase)
        {
            "license", "licence", "readme", "notice", "copying", "dockerfile", "makefile", "procfile",
            "gemfile", "rakefile", "vagrantfile", "cname", "authors", "contributors", "changelog", "todo"
        };

        public static async Task<PreviewData> LoadPreviewAsync(string fullPath)
        {
            var data = new PreviewData();
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                data.Type = PreviewType.None;
                return data;
            }

            // Folder inspection
            if (Directory.Exists(fullPath))
            {
                data.Type = PreviewType.Folder;
                return await Task.Run(() =>
                {
                    try
                    {
                        var di = new DirectoryInfo(fullPath);
                        int fileCount = 0;
                        int dirCount = 0;
                        long totalSize = 0;
                        var topEntries = new List<string>();

                        foreach (var entry in di.EnumerateFileSystemInfos())
                        {
                            if (entry is DirectoryInfo)
                            {
                                dirCount++;
                                if (topEntries.Count < 8) topEntries.Add($"📁 {entry.Name}");
                            }
                            else if (entry is FileInfo fi)
                            {
                                fileCount++;
                                totalSize += fi.Length;
                                if (topEntries.Count < 8) topEntries.Add($"📄 {entry.Name} ({SearchResultItem.FormatSize(fi.Length)})");
                            }

                            if (fileCount + dirCount >= 600) break;
                        }

                        data.HeaderInfo = $"{fileCount:N0} files, {dirCount:N0} folders";
                        var sb = new StringBuilder();
                        sb.AppendLine($"Folder: {di.FullName}");
                        sb.AppendLine($"Contains: {fileCount:N0} files, {dirCount:N0} subdirectories");
                        sb.AppendLine($"Sample size: {SearchResultItem.FormatSize(totalSize)}");
                        sb.AppendLine($"Created: {di.CreationTime:yyyy-MM-dd HH:mm:ss}");
                        sb.AppendLine($"Last Modified: {di.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                        if (topEntries.Count > 0)
                        {
                            sb.AppendLine("\nContents preview:");
                            foreach (var item in topEntries)
                            {
                                sb.AppendLine($"  {item}");
                            }
                            if (fileCount + dirCount > topEntries.Count)
                            {
                                sb.AppendLine($"  ... and {fileCount + dirCount - topEntries.Count:N0} more items");
                            }
                        }
                        data.TextContent = sb.ToString();
                        data.ExtraDetail = $"Created: {di.CreationTime:yyyy-MM-dd HH:mm:ss}";
                    }
                    catch (Exception ex)
                    {
                        data.HeaderInfo = "Folder (restricted)";
                        data.TextContent = $"Access restricted: {ex.Message}";
                    }
                    return data;
                });
            }

            if (!File.Exists(fullPath))
            {
                data.Type = PreviewType.None;
                return data;
            }

            try
            {
                var fi = new FileInfo(fullPath);
                string ext = fi.Extension;
                string fileName = fi.Name;

                // 1. Image Preview
                if (ImageExtensions.Contains(ext))
                {
                    data.Type = PreviewType.Image;
                    data.ImageSource = await Task.Run(() =>
                    {
                        try
                        {
                            using var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            using var ms = new MemoryStream();
                            fs.CopyTo(ms);
                            ms.Position = 0;

                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.StreamSource = ms;
                            bitmap.DecodePixelWidth = 1000;
                            bitmap.EndInit();
                            bitmap.Freeze();
                            return bitmap;
                        }
                        catch
                        {
                            return null;
                        }
                    });

                    if (data.ImageSource != null)
                    {
                        data.HeaderInfo = $"{data.ImageSource.PixelWidth} × {data.ImageSource.PixelHeight} px • {SearchResultItem.FormatSize(fi.Length)}";
                    }
                    return data;
                }

                // 2. Audio Preview
                if (AudioExtensions.Contains(ext))
                {
                    data.Type = PreviewType.Audio;
                    data.AudioFilePath = fullPath;
                    data.HeaderInfo = $"Audio File • {SearchResultItem.FormatSize(fi.Length)}";
                    data.ExtraDetail = $"{ext.TrimStart('.').ToUpperInvariant()} Audio • Ready to play";
                    return data;
                }

                // 3. Text & Code Preview
                bool isText = TextExtensions.Contains(ext) ||
                              KnownTextFilenames.Contains(fileName) ||
                              IsSniffedAsText(fullPath, fi.Length);

                if (isText)
                {
                    data.Type = PreviewType.Text;
                    return await Task.Run(() =>
                    {
                        try
                        {
                            if (fi.Length > 1024 * 1024 * 6) // Skip > 6MB files
                            {
                                data.TextContent = $"File size ({SearchResultItem.FormatSize(fi.Length)}) exceeds 6MB preview threshold.";
                                return data;
                            }

                            using var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            using var reader = new StreamReader(fs, Encoding.UTF8, true);
                            var sb = new StringBuilder();
                            int lineCount = 0;
                            while (!reader.EndOfStream && lineCount < 400)
                            {
                                string? line = reader.ReadLine();
                                lineCount++;
                                sb.AppendLine($"{lineCount,4} | {line}");
                            }

                            if (!reader.EndOfStream)
                            {
                                sb.AppendLine($"\n... [preview truncated to first 400 lines • Total size: {SearchResultItem.FormatSize(fi.Length)}] ...");
                            }

                            data.TextContent = sb.ToString();
                            data.LineCount = lineCount;
                            data.HeaderInfo = $"{lineCount} lines displayed • {SearchResultItem.FormatSize(fi.Length)}";
                            return data;
                        }
                        catch (Exception ex)
                        {
                            data.TextContent = $"Preview error: {ex.Message}";
                            return data;
                        }
                    });
                }

                // 4. Hex Dump for Binary Files
                data.Type = PreviewType.Hex;
                return await Task.Run(() =>
                {
                    try
                    {
                        using var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        byte[] buffer = new byte[256];
                        int bytesRead = fs.Read(buffer, 0, buffer.Length);

                        var sb = new StringBuilder();
                        sb.AppendLine($"HEX DUMP (First {bytesRead} bytes of {SearchResultItem.FormatSize(fi.Length)}):");
                        sb.AppendLine("OFFSET    00 01 02 03 04 05 06 07  08 09 0A 0B 0C 0D 0E 0F  ASCII");
                        sb.AppendLine("─────────────────────────────────────────────────────────────────");

                        for (int i = 0; i < bytesRead; i += 16)
                        {
                            sb.Append($"{i:X8}  ");

                            // Hex bytes
                            for (int j = 0; j < 16; j++)
                            {
                                if (i + j < bytesRead)
                                    sb.Append($"{buffer[i + j]:X2} ");
                                else
                                    sb.Append("   ");

                                if (j == 7) sb.Append(" ");
                            }

                            sb.Append(" ");

                            // ASCII representation
                            for (int j = 0; j < 16; j++)
                            {
                                if (i + j < bytesRead)
                                {
                                    byte b = buffer[i + j];
                                    sb.Append(b >= 32 && b <= 126 ? (char)b : '.');
                                }
                            }
                            sb.AppendLine();
                        }

                        data.HexContent = sb.ToString();
                        data.HeaderInfo = $"Binary File • {SearchResultItem.FormatSize(fi.Length)}";
                    }
                    catch (Exception ex)
                    {
                        data.HexContent = $"Hex inspect failed: {ex.Message}";
                    }
                    return data;
                });
            }
            catch
            {
                data.Type = PreviewType.Generic;
            }

            return data;
        }

        private static bool IsSniffedAsText(string fullPath, long fileLength)
        {
            if (fileLength == 0) return true;
            if (fileLength > 1024 * 1024 * 8) return false;

            try
            {
                using var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                byte[] buffer = new byte[Math.Min(512, (int)fileLength)];
                int read = fs.Read(buffer, 0, buffer.Length);
                for (int i = 0; i < read; i++)
                {
                    if (buffer[i] == 0) return false; // null byte indicates binary
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
