using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EverythingModern.Services
{
    public static class ShellIconHelper
    {
        private static readonly ConcurrentDictionary<string, ImageSource> _iconCache = new(StringComparer.OrdinalIgnoreCase);

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_SMALLICON = 0x000000001;
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

        public static ImageSource? GetIcon(string fullPath, bool isFolder, string extension)
        {
            string cacheKey;
            if (isFolder)
            {
                cacheKey = "::folder::";
            }
            else
            {
                string ext = string.IsNullOrEmpty(extension) ? Path.GetExtension(fullPath) : extension;
                ext = ext.ToLowerInvariant();

                // Specific files with unique icons
                if (ext == ".exe" || ext == ".ico" || ext == ".lnk")
                {
                    cacheKey = fullPath.ToLowerInvariant();
                }
                else
                {
                    cacheKey = string.IsNullOrEmpty(ext) ? "::file::" : ext;
                }
            }

            if (_iconCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            if (_iconCache.Count > 1500)
            {
                _iconCache.Clear();
            }

            try
            {
                ImageSource? iconSource = null;
                if (isFolder)
                {
                    iconSource = ExtractGenericIcon("folder", FILE_ATTRIBUTE_DIRECTORY);
                }
                else if (cacheKey.StartsWith("::") || (!cacheKey.EndsWith(".exe") && !cacheKey.EndsWith(".lnk") && !cacheKey.EndsWith(".ico")))
                {
                    string dummyPath = "dummy" + (cacheKey.StartsWith("::") ? "" : cacheKey);
                    iconSource = ExtractGenericIcon(dummyPath, FILE_ATTRIBUTE_NORMAL);
                }
                else
                {
                    // For exe/lnk/ico, if file exists, grab direct icon, else fallback
                    if (File.Exists(fullPath))
                    {
                        iconSource = ExtractFileIcon(fullPath);
                    }
                    else
                    {
                        iconSource = ExtractGenericIcon("dummy.exe", FILE_ATTRIBUTE_NORMAL);
                    }
                }

                if (iconSource != null)
                {
                    _iconCache[cacheKey] = iconSource;
                    return iconSource;
                }
            }
            catch
            {
                // Fallback gracefully
            }

            return null;
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

        private static ImageSource? ExtractFileIcon(string filePath)
        {
            var flags = SHGFI_ICON | SHGFI_SMALLICON;
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
