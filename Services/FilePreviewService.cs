using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace EverythingModern.Services
{
    public enum PreviewType
    {
        None,
        Image,
        Text,
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
    }

    public static class FilePreviewService
    {
        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".ico", ".webp", ".tif", ".tiff"
        };

        private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".txt", ".cs", ".json", ".xml", ".xaml", ".py", ".js", ".ts", ".html", ".css",
            ".md", ".log", ".ini", ".conf", ".yaml", ".yml", ".c", ".cpp", ".h", ".hpp",
            ".sql", ".rs", ".go", ".java", ".kt", ".sh", ".bat", ".ps1", ".cmd", ".csv",
            ".config", ".gitignore", ".env", ".toml", ".props", ".targets"
        };

        public static async Task<PreviewData> LoadPreviewAsync(string fullPath)
        {
            var data = new PreviewData();
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                data.Type = PreviewType.None;
                return data;
            }

            // Check if folder
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
                        foreach (var entry in di.EnumerateFileSystemInfos())
                        {
                            if (entry is DirectoryInfo) dirCount++;
                            else fileCount++;
                            if (fileCount + dirCount >= 500) break; // performance cap
                        }
                        data.HeaderInfo = $"{fileCount} files, {dirCount} folders";
                        data.ExtraDetail = $"Created: {di.CreationTime:yyyy-MM-dd HH:mm:ss}";
                    }
                    catch (Exception ex)
                    {
                        data.HeaderInfo = "Folder (access restricted)";
                        data.ExtraDetail = ex.Message;
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
                string ext = Path.GetExtension(fullPath);

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
                            bitmap.DecodePixelWidth = 800; // fast thumbnail
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
                        data.HeaderInfo = $"{data.ImageSource.PixelWidth} × {data.ImageSource.PixelHeight} px";
                    }
                    return data;
                }

                if (TextExtensions.Contains(ext))
                {
                    data.Type = PreviewType.Text;
                    return await Task.Run(() =>
                    {
                        try
                        {
                            var fi = new FileInfo(fullPath);
                            if (fi.Length > 1024 * 1024 * 4) // Skip > 4MB files
                            {
                                data.TextContent = "File exceeds 4MB (preview truncated).";
                                return data;
                            }

                            using var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            using var reader = new StreamReader(fs, Encoding.UTF8, true);
                            var sb = new StringBuilder();
                            int lineCount = 0;
                            while (!reader.EndOfStream && lineCount < 300)
                            {
                                sb.AppendLine(reader.ReadLine());
                                lineCount++;
                            }
                            if (!reader.EndOfStream)
                            {
                                sb.AppendLine("\n... [preview truncated to first 300 lines] ...");
                            }
                            data.TextContent = sb.ToString();
                            data.LineCount = lineCount;
                            data.HeaderInfo = $"{lineCount} lines displayed";
                            return data;
                        }
                        catch (Exception ex)
                        {
                            data.TextContent = $"Preview unavailable: {ex.Message}";
                            return data;
                        }
                    });
                }

                data.Type = PreviewType.Generic;
            }
            catch
            {
                data.Type = PreviewType.Generic;
            }

            return data;
        }
    }
}
