using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace EverythingModern.Services
{
    public static class EverythingDaemonManager
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint TOKEN_QUERY = 0x0008;

        public static bool IsCurrentProcessElevated()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsRunningProcessElevated(int pid)
        {
            if (IsCurrentProcessElevated())
            {
                return false;
            }

            IntPtr hProc = OpenProcess(PROCESS_QUERY_INFORMATION, false, pid);
            if (hProc == IntPtr.Zero)
            {
                int err = Marshal.GetLastWin32Error();
                return err == 5; // ERROR_ACCESS_DENIED implies higher integrity level
            }

            try
            {
                if (OpenProcessToken(hProc, TOKEN_QUERY, out IntPtr hToken))
                {
                    CloseHandle(hToken);
                    return false; // Token readable at current integrity
                }
                else
                {
                    int err = Marshal.GetLastWin32Error();
                    return err == 5; // ERROR_ACCESS_DENIED implies elevated process
                }
            }
            finally
            {
                CloseHandle(hProc);
            }
        }

        public static bool IsAnySessionEverythingElevated()
        {
            try
            {
                int currentSessionId = Process.GetCurrentProcess().SessionId;
                var procs = Process.GetProcessesByName("Everything");
                foreach (var p in procs)
                {
                    if (p.SessionId == currentSessionId)
                    {
                        if (IsRunningProcessElevated(p.Id))
                        {
                            return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        public static bool IsEverythingProcessRunning()
        {
            var procs = Process.GetProcessesByName("Everything");
            return procs.Length > 0;
        }

        public static bool IsEverythingProcessRunningInSession()
        {
            try
            {
                int currentSessionId = Process.GetCurrentProcess().SessionId;
                var procs = Process.GetProcessesByName("Everything");
                foreach (var p in procs)
                {
                    if (p.SessionId == currentSessionId)
                    {
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        public static bool IsIpcWindowPresent()
        {
            try
            {
                return FindWindow("EVERYTHING_TASKBAR_NOTIFICATION", null) != IntPtr.Zero;
            }
            catch
            {
                return false;
            }
        }

        public static int GetUserSessionProcessCount()
        {
            try
            {
                int currentSessionId = Process.GetCurrentProcess().SessionId;
                return Process.GetProcessesByName("Everything")
                    .Count(p => p.SessionId == currentSessionId);
            }
            catch
            {
                return 0;
            }
        }

        public static void CleanupDuplicateInstances()
        {
            try
            {
                int currentSessionId = Process.GetCurrentProcess().SessionId;
                var procs = Process.GetProcessesByName("Everything")
                    .Where(p => p.SessionId == currentSessionId)
                    .OrderByDescending(p => p.WorkingSet64)
                    .ToList();

                // If more than 1 user-mode client process exists, terminate redundant/hung duplicates
                if (procs.Count > 1)
                {
                    for (int i = 1; i < procs.Count; i++)
                    {
                        try
                        {
                            procs[i].Kill();
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        public static string? FindEverythingExecutable()
        {
            // 1. Check standard paths
            string[] knownPaths =
            {
                @"C:\Program Files\Everything\Everything.exe",
                @"C:\Program Files (x86)\Everything\Everything.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Everything\Everything.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Everything\Everything.exe")
            };

            foreach (var path in knownPaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            // 2. Check Windows Registry App Paths (HKLM)
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\Everything.exe");
                if (key != null)
                {
                    string? regPath = key.GetValue("") as string;
                    if (!string.IsNullOrEmpty(regPath) && File.Exists(regPath))
                    {
                        return regPath;
                    }
                }
            }
            catch { }

            // 3. Check Windows Registry App Paths (HKCU)
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\Everything.exe");
                if (key != null)
                {
                    string? regPath = key.GetValue("") as string;
                    if (!string.IsNullOrEmpty(regPath) && File.Exists(regPath))
                    {
                        return regPath;
                    }
                }
            }
            catch { }

            return null;
        }

        public static bool IsIpcResponding()
        {
            try
            {
                if (EverythingInterop.Everything_IsDBLoaded())
                {
                    return true;
                }

                EverythingInterop.Everything_Reset();
                EverythingInterop.Everything_SetMax(1);
                EverythingInterop.Everything_SetSearchW(string.Empty);
                bool queryRes = EverythingInterop.Everything_QueryW(true);
                uint err = EverythingInterop.Everything_GetLastError();
                return queryRes || err == 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool LaunchEverythingClient()
        {
            // If already running in current user session, clean up duplicates and avoid spawning another
            if (IsEverythingProcessRunningInSession())
            {
                CleanupDuplicateInstances();
                return true;
            }

            string? exePath = FindEverythingExecutable();
            if (string.IsNullOrEmpty(exePath))
            {
                return false;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = "-startup",
                    UseShellExecute = true
                };
                Process.Start(psi);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool RelaunchAsAdministrator()
        {
            try
            {
                string? exe = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exe)) return false;

                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(psi);
                Application.Current?.Shutdown();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> RestartEverythingClientAsync()
        {
            try
            {
                int currentSessionId = Process.GetCurrentProcess().SessionId;
                var procs = Process.GetProcessesByName("Everything");
                foreach (var p in procs)
                {
                    try
                    {
                        // Only kill user-session desktop instances, never the Session 0 service
                        if (p.SessionId == currentSessionId)
                        {
                            p.Kill();
                        }
                    }
                    catch { }
                }
                await Task.Delay(600);
                return LaunchEverythingClient();
            }
            catch
            {
                return false;
            }
        }

        public static bool OpenEverythingOptions(string page = "general")
        {
            string? exe = FindEverythingExecutable();
            if (string.IsNullOrEmpty(exe)) return false;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = $"-options {page}",
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> EnsureDaemonRunningAsync(int timeoutMs = 12000, Action<string>? progressCallback = null)
        {
            // Automatically clean up any duplicate or orphaned user instances first
            CleanupDuplicateInstances();

            progressCallback?.Invoke("Checking Everything Engine status...");
            if (IsIpcResponding())
            {
                progressCallback?.Invoke("Everything Engine connected!");
                return true;
            }

            // Early check: If Everything is already running elevated while we are not, fail fast
            if (!IsCurrentProcessElevated() && IsAnySessionEverythingElevated())
            {
                progressCallback?.Invoke("Everything is running with Administrator rights • Elevation required");
                return false;
            }

            if (!IsEverythingProcessRunningInSession())
            {
                progressCallback?.Invoke("Starting Everything background daemon...");
                LaunchEverythingClient();
                await Task.Delay(400).ConfigureAwait(false);
            }
            else if (IsIpcWindowPresent())
            {
                progressCallback?.Invoke("Everything file database is loading into memory...");
            }
            else
            {
                progressCallback?.Invoke("Waiting for Everything daemon to initialize...");
            }

            var sw = Stopwatch.StartNew();
            int lastSec = -1;
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                await Task.Delay(200).ConfigureAwait(false);
                if (IsIpcResponding())
                {
                    progressCallback?.Invoke("Connected to Everything Engine!");
                    return true;
                }

                // Check again if process started as elevated
                if (!IsCurrentProcessElevated() && IsAnySessionEverythingElevated())
                {
                    progressCallback?.Invoke("Everything is running with Administrator rights • Elevation required");
                    return false;
                }

                int currentSec = (int)(sw.ElapsedMilliseconds / 1000);
                if (currentSec != lastSec)
                {
                    lastSec = currentSec;
                    if (IsIpcWindowPresent())
                    {
                        progressCallback?.Invoke($"Loading Everything file database... ({currentSec}s)");
                    }
                    else
                    {
                        progressCallback?.Invoke($"Initializing Everything daemon... ({currentSec}s)");
                    }
                }
            }

            progressCallback?.Invoke("Connection timed out.");
            return false;
        }
    }
}
