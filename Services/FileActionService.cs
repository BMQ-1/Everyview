using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace EverythingModern.Services
{
    public static class FileActionService
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpVerb;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpParameters;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pFrom;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string? pTo;
            public ushort fFlags;
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string? lpszProgressTitle;
        }

        private const uint FO_DELETE = 0x0003;
        private const ushort FOF_ALLOWUNDO = 0x0040;
        private const ushort FOF_NOCONFIRMATION = 0x0010;
        private const uint SEE_MASK_INVOKEIDLIST = 0x0000000C;
        private const int SW_SHOW = 5;

        public static void OpenFile(string fullPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fullPath)) return;
                var psi = new ProcessStartInfo
                {
                    FileName = fullPath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open file: {ex.Message}", "Everyview", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public static void OpenContainingFolder(string fullPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fullPath)) return;

                if (File.Exists(fullPath))
                {
                    Process.Start("explorer.exe", $"/select,\"{fullPath}\"");
                }
                else if (Directory.Exists(fullPath))
                {
                    Process.Start("explorer.exe", $"\"{fullPath}\"");
                }
                else
                {
                    string dir = Path.GetDirectoryName(fullPath) ?? string.Empty;
                    if (Directory.Exists(dir))
                    {
                        Process.Start("explorer.exe", $"\"{dir}\"");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open directory: {ex.Message}", "Everyview", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public static void CopyPath(string fullPath, bool quote = false)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(fullPath))
                {
                    string textToCopy = quote ? $"\"{fullPath}\"" : fullPath;
                    Clipboard.SetText(textToCopy);
                }
            }
            catch { }
        }

        public static void OpenInTerminal(string fullPath)
        {
            try
            {
                string targetDir = Directory.Exists(fullPath)
                    ? fullPath
                    : (Path.GetDirectoryName(fullPath) ?? string.Empty);

                if (!string.IsNullOrEmpty(targetDir) && Directory.Exists(targetDir))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "wt.exe",
                        Arguments = $"-d \"{targetDir}\"",
                        UseShellExecute = true
                    });
                }
            }
            catch
            {
                try
                {
                    string targetDir = Directory.Exists(fullPath)
                        ? fullPath
                        : (Path.GetDirectoryName(fullPath) ?? string.Empty);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        WorkingDirectory = targetDir,
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        }

        public static void OpenInVSCode(string fullPath)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c code \"{fullPath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not launch VS Code: {ex.Message}", "Everyview", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public static void OpenWith(string fullPath)
        {
            try
            {
                Process.Start("rundll32.exe", $"shell32.dll,OpenAs_RunDLL \"{fullPath}\"");
            }
            catch { }
        }

        public static bool DeleteToRecycleBin(string fullPath)
        {
            try
            {
                if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                {
                    return false;
                }

                var result = MessageBox.Show(
                    $"Are you sure you want to move this item to the Recycle Bin?\n\n{Path.GetFileName(fullPath)}",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return false;

                var shf = new SHFILEOPSTRUCT
                {
                    wFunc = FO_DELETE,
                    pFrom = fullPath + '\0' + '\0', // Must be double null-terminated
                    fFlags = FOF_ALLOWUNDO
                };

                return SHFileOperation(ref shf) == 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete item: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static async Task<string> ComputeSha256Async(string fullPath)
        {
            if (!File.Exists(fullPath)) return "File not found";

            return await Task.Run(() =>
            {
                try
                {
                    var fi = new FileInfo(fullPath);
                    if (fi.Length > 1024 * 1024 * 500) // 500MB safety limit
                    {
                        return "File exceeds 500MB (hash skipped)";
                    }

                    using var sha256 = SHA256.Create();
                    using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    byte[] hashBytes = sha256.ComputeHash(stream);
                    var sb = new StringBuilder();
                    foreach (byte b in hashBytes)
                    {
                        sb.Append(b.ToString("x2"));
                    }
                    return sb.ToString();
                }
                catch (Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
            });
        }

        public static async Task<string> ComputeMd5Async(string fullPath)
        {
            if (!File.Exists(fullPath)) return "File not found";

            return await Task.Run(() =>
            {
                try
                {
                    var fi = new FileInfo(fullPath);
                    if (fi.Length > 1024 * 1024 * 500)
                    {
                        return "File exceeds 500MB (hash skipped)";
                    }

                    using var md5 = MD5.Create();
                    using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    byte[] hashBytes = md5.ComputeHash(stream);
                    var sb = new StringBuilder();
                    foreach (byte b in hashBytes)
                    {
                        sb.Append(b.ToString("x2"));
                    }
                    return sb.ToString();
                }
                catch (Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
            });
        }

        public static (bool success, string newFullPath, string? error) RenameFileOrFolder(string currentFullPath, string newName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(currentFullPath)) return (false, currentFullPath, "Invalid path");
                if (string.IsNullOrWhiteSpace(newName)) return (false, currentFullPath, "Name cannot be empty");

                char[] invalidChars = Path.GetInvalidFileNameChars();
                if (newName.IndexOfAny(invalidChars) >= 0)
                {
                    return (false, currentFullPath, "Filename contains invalid characters: \\ / : * ? \" < > |");
                }

                bool isDir = Directory.Exists(currentFullPath);
                bool isFile = File.Exists(currentFullPath);

                if (!isDir && !isFile)
                {
                    return (false, currentFullPath, "File or folder not found on disk");
                }

                string? parentDir = Path.GetDirectoryName(currentFullPath);
                if (string.IsNullOrEmpty(parentDir))
                {
                    return (false, currentFullPath, "Cannot rename root directory");
                }

                string targetPath = Path.Combine(parentDir, newName.Trim());
                if (string.Equals(currentFullPath, targetPath, StringComparison.OrdinalIgnoreCase))
                {
                    return (true, currentFullPath, null);
                }

                if (File.Exists(targetPath) || Directory.Exists(targetPath))
                {
                    return (false, currentFullPath, $"An item named '{newName}' already exists in this directory");
                }

                if (isDir)
                {
                    Directory.Move(currentFullPath, targetPath);
                }
                else
                {
                    File.Move(currentFullPath, targetPath);
                }

                return (true, targetPath, null);
            }
            catch (Exception ex)
            {
                return (false, currentFullPath, ex.Message);
            }
        }

        public static void CopyDirectoryPath(string fullPath)
        {
            try
            {
                string? dir = Directory.Exists(fullPath)
                    ? fullPath
                    : Path.GetDirectoryName(fullPath);

                if (!string.IsNullOrEmpty(dir))
                {
                    Clipboard.SetText(dir);
                }
            }
            catch { }
        }

        public static void CopyFileAsUrl(string fullPath)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(fullPath))
                {
                    var uri = new Uri(fullPath);
                    Clipboard.SetText(uri.AbsoluteUri);
                }
            }
            catch { }
        }

        public static void CopyText(string text)
        {
            try
            {
                if (!string.IsNullOrEmpty(text))
                {
                    Clipboard.SetText(text);
                }
            }
            catch { }
        }

        public static int BatchPermanentDelete(System.Collections.Generic.IEnumerable<string> paths)
        {
            try
            {
                var existingPaths = new System.Collections.Generic.List<string>();
                foreach (var p in paths)
                {
                    if (File.Exists(p) || Directory.Exists(p))
                    {
                        existingPaths.Add(p);
                    }
                }

                if (existingPaths.Count == 0) return 0;

                string prompt = existingPaths.Count == 1
                    ? $"Are you sure you want to PERMANENTLY delete this item?\n\n{Path.GetFileName(existingPaths[0])}\n\nWARNING: This cannot be undone and will NOT go to the Recycle Bin!"
                    : $"Are you sure you want to PERMANENTLY delete these {existingPaths.Count} items?\n\nWARNING: This cannot be undone and will NOT go to the Recycle Bin!";

                var result = MessageBox.Show(prompt, "Confirm Permanent Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return 0;

                int deletedCount = 0;
                foreach (var p in existingPaths)
                {
                    try
                    {
                        if (File.Exists(p))
                        {
                            File.Delete(p);
                            deletedCount++;
                        }
                        else if (Directory.Exists(p))
                        {
                            Directory.Delete(p, true);
                            deletedCount++;
                        }
                    }
                    catch { }
                }
                return deletedCount;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to permanently delete items: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return 0;
            }
        }

        public static void ShowProperties(string fullPath)
        {
            try
            {
                var sei = new SHELLEXECUTEINFO();
                sei.cbSize = Marshal.SizeOf(sei);
                sei.lpVerb = "properties";
                sei.lpFile = fullPath;
                sei.nShow = SW_SHOW;
                sei.fMask = SEE_MASK_INVOKEIDLIST;
                ShellExecuteEx(ref sei);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to display properties: {ex.Message}", "Everyview", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public static void CopyFilesAndPaths(System.Collections.Generic.IEnumerable<string> paths, bool quote = false)
        {
            try
            {
                var list = new System.Collections.Generic.List<string>();
                foreach (var p in paths)
                {
                    if (!string.IsNullOrWhiteSpace(p)) list.Add(p);
                }
                if (list.Count == 0) return;

                var dataObject = new DataObject();
                string text = quote
                    ? string.Join(Environment.NewLine, list.ConvertAll(p => $"\"{p}\""))
                    : string.Join(Environment.NewLine, list);

                dataObject.SetData(DataFormats.Text, text);
                dataObject.SetData(DataFormats.UnicodeText, text);

                var stringCollection = new System.Collections.Specialized.StringCollection();
                foreach (var p in list)
                {
                    if (File.Exists(p) || Directory.Exists(p))
                    {
                        stringCollection.Add(p);
                    }
                }
                if (stringCollection.Count > 0)
                {
                    dataObject.SetFileDropList(stringCollection);
                }

                Clipboard.SetDataObject(dataObject, true);
            }
            catch { }
        }

        public static int BatchDeleteToRecycleBin(System.Collections.Generic.IEnumerable<string> paths)
        {
            try
            {
                var existingPaths = new System.Collections.Generic.List<string>();
                foreach (var p in paths)
                {
                    if (File.Exists(p) || Directory.Exists(p))
                    {
                        existingPaths.Add(p);
                    }
                }

                if (existingPaths.Count == 0) return 0;

                string prompt = existingPaths.Count == 1
                    ? $"Are you sure you want to move this item to the Recycle Bin?\n\n{Path.GetFileName(existingPaths[0])}"
                    : $"Are you sure you want to move these {existingPaths.Count} items to the Recycle Bin?";

                var result = MessageBox.Show(prompt, "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return 0;

                int deletedCount = 0;
                foreach (var p in existingPaths)
                {
                    var shf = new SHFILEOPSTRUCT
                    {
                        wFunc = FO_DELETE,
                        pFrom = p + '\0' + '\0',
                        fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION
                    };
                    if (SHFileOperation(ref shf) == 0)
                    {
                        deletedCount++;
                    }
                }
                return deletedCount;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete selected items: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return 0;
            }
        }

        public static async Task ExportResultsToFileAsync(System.Collections.Generic.IEnumerable<EverythingModern.Models.SearchResultItem> items, string filePath, string format)
        {
            await Task.Run(() =>
            {
                var sb = new StringBuilder();
                if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine("Name,Path,Size,DateModified,DateCreated,Type");
                    foreach (var item in items)
                    {
                        string safeName = $"\"{item.Name.Replace("\"", "\"\"")}\"";
                        string safePath = $"\"{item.Path.Replace("\"", "\"\"")}\"";
                        sb.AppendLine($"{safeName},{safePath},{item.Size},{item.DateModifiedDisplay},{item.DateCreatedDisplay},\"{item.TypeDisplay}\"");
                    }
                }
                else if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine("[");
                    bool first = true;
                    foreach (var item in items)
                    {
                        if (!first) sb.AppendLine(",");
                        first = false;
                        sb.Append($"  {{\"name\": \"{EscapeJson(item.Name)}\", \"path\": \"{EscapeJson(item.Path)}\", \"size\": {item.Size}, \"dateModified\": \"{item.DateModifiedDisplay}\", \"isFolder\": {item.IsFolder.ToString().ToLower()}}}");
                    }
                    sb.AppendLine();
                    sb.AppendLine("]");
                }
                else
                {
                    // Plain Text (Full paths list)
                    foreach (var item in items)
                    {
                        sb.AppendLine(item.FullPath);
                    }
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            });
        }

        private static string EscapeJson(string str)
        {
            return (str ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }
    }
}
