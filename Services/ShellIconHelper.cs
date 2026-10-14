using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EverythingModern.Services
{
    /// <summary>
    /// High-performance shell icon provider with zero-allocation static cache.
    /// Never performs synchronous disk I/O on the UI rendering thread.
    /// </summary>
    public static class ShellIconHelper
    {
        private static readonly ConcurrentDictionary<string, ImageSource> _iconCache = new(StringComparer.OrdinalIgnoreCase);

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_SMALLICON = 0x000000001;
        private const uint SHGFI_LARGEICON = 0x000000000;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            out SHFILEINFO psfi,
            uint cbFileInfo,
            uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private static readonly ImageSource? _folderIcon;
        private static readonly ImageSource? _genericFileIcon;
        private static readonly ImageSource? _genericExeIcon;

        static ShellIconHelper()
        {
            try
            {
                _folderIcon = ExtractGenericIcon("folder", FILE_ATTRIBUTE_DIRECTORY);
                _genericFileIcon = ExtractGenericIcon("dummy.file", FILE_ATTRIBUTE_NORMAL);
                _genericExeIcon = ExtractGenericIcon("dummy.exe", FILE_ATTRIBUTE_NORMAL);

                if (_folderIcon != null) _iconCache["::folder::"] = _folderIcon;
                if (_genericFileIcon != null) _iconCache["::file::"] = _genericFileIcon;
                if (_genericExeIcon != null) _iconCache[".exe"] = _genericExeIcon;
            }
            catch
            {
                // Fallback gracefully
            }
        }

        /// <summary>
        /// Instant, non-blocking icon lookup for virtualized lists. 
        /// Zero disk access: all icons are resolved via shell file-type attributes and memory-cached.
        /// </summary>
        public static ImageSource? GetIcon(string fullPath, bool isFolder, string extension)
        {
            if (isFolder)
            {
                return _folderIcon;
            }

            string ext = string.IsNullOrEmpty(extension)
                ? (string.IsNullOrEmpty(fullPath) ? "" : Path.GetExtension(fullPath))
                : extension;

            if (string.IsNullOrEmpty(ext))
            {
                return _genericFileIcon;
            }

            ext = ext.ToLowerInvariant();
            if (!ext.StartsWith('.'))
            {
                ext = "." + ext;
            }

            if (_iconCache.TryGetValue(ext, out var cached))
            {
                return cached;
            }

            try
            {
                var iconSource = ExtractGenericIcon("dummy" + ext, FILE_ATTRIBUTE_NORMAL);
                if (iconSource != null)
                {
                    _iconCache[ext] = iconSource;
                    return iconSource;
                }
            }
            catch
            {
                // Fallback gracefully
            }

            return _genericFileIcon;
        }

        /// <summary>
        /// Asynchronously extracts the exact native high-res file icon for the Preview/Inspector panel.
        /// </summary>
        public static async Task<ImageSource?> GetDetailedFileIconAsync(string filePath, bool isFolder)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return null;

            return await Task.Run(() =>
            {
                try
                {
                    if (isFolder)
                    {
                        return _folderIcon;
                    }

                    if (File.Exists(filePath))
                    {
                        var flags = SHGFI_ICON | SHGFI_LARGEICON;
                        var res = SHGetFileInfo(filePath, 0, out var shinfo, (uint)Marshal.SizeOf<SHFILEINFO>(), flags);
                        if (res != IntPtr.Zero && shinfo.hIcon != IntPtr.Zero)
                        {
                            try
                            {
                                var img = Imaging.CreateBitmapSourceFromHIcon(
                                    shinfo.hIcon,
                                    Int32Rect.Empty,
                                    BitmapSizeOptions.FromEmptyOptions());
                                img.Freeze();
                                return (ImageSource)img;
                            }
                            finally
                            {
                                DestroyIcon(shinfo.hIcon);
                            }
                        }
                    }
                }
                catch { }

                return GetIcon(filePath, isFolder, Path.GetExtension(filePath));
            }).ConfigureAwait(false);
        }

        private static ImageSource? ExtractGenericIcon(string path, uint fileAttributes)
        {
            var flags = SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES;
            var res = SHGetFileInfo(path, fileAttributes, out var shinfo, (uint)Marshal.SizeOf<SHFILEINFO>(), flags);
            if (res != IntPtr.Zero && shinfo.hIcon != IntPtr.Zero)
            {
                try
                {
                    var img = Imaging.CreateBitmapSourceFromHIcon(
                        shinfo.hIcon,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    img.Freeze();
                    return img;
                }
                finally
                {
                    DestroyIcon(shinfo.hIcon);
                }
            }
            return null;
        }
    }
}
