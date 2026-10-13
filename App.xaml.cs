using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using EverythingModern.Services;

namespace EverythingModern
{
    public partial class App : Application
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        protected override async void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Length > 0 && e.Args[0] == "--test")
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown;
                AttachConsole(-1);
                var stdOut = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
                Console.SetOut(stdOut);
                Console.WriteLine("\n[Everyview CLI Test Mode]");
                try
                {
                    EverythingDaemonManager.EnsureDaemonRunningAsync().GetAwaiter().GetResult();
                    var service = new EverythingSearchService();
                    string testQuery = e.Args.Length > 1 ? e.Args[1] : "*.exe";
                    Console.WriteLine($"Running test query: '{testQuery}'...");
                    var batch = service.SearchAsync(testQuery, SearchFilterType.All, SortOrder.NameAscending, maxResults: 10).GetAwaiter().GetResult();
                    if (batch.IsSuccess)
                    {
                        Console.WriteLine($"SUCCESS! Total found in Everything: {batch.TotalCount:N0} in {batch.ElapsedMilliseconds} ms");
                        Console.WriteLine($"First {batch.Items.Count} items:");
                        foreach (var item in batch.Items)
                        {
                            Console.WriteLine($"  - {item.Name} ({item.SizeDisplay}) -> {item.Path}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Query failed: {batch.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during test: {ex.Message}");
                }
                finally
                {
                    Console.Out.Flush();
                    FreeConsole();
                    Shutdown(0);
                }
                return;
            }

            base.OnStartup(e);

            // Fast check: Is Everything daemon already running and responding to IPC?
            bool isWarm = EverythingDaemonManager.IsIpcResponding();

            // Check if elevation mismatch is present (Everything running elevated while Everyview is standard user)
            bool isElevationMismatch = !EverythingDaemonManager.IsCurrentProcessElevated() && EverythingDaemonManager.IsAnySessionEverythingElevated();

            if (isWarm || isElevationMismatch)
            {
                // Instant hot-start or immediate interactive resolution window
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
            else
            {
                // Cold start: Show modern splash window immediately (< 80ms) to provide immediate visual feedback
                var splash = new SplashScreenWindow();
                splash.Show();

                // Connect to Everything daemon with live progress reporting
                var daemonTask = EverythingDaemonManager.EnsureDaemonRunningAsync(
                    timeoutMs: 10000,
                    progressCallback: msg =>
                    {
                        splash.UpdateStatus(msg, "Cold-starting Voidtools Everything & loading disk indexes...");
                    });

                while (!daemonTask.IsCompleted && !splash.IsForceOpened)
                {
                    await Task.Delay(100);
                }

                if (daemonTask.IsCompleted && daemonTask.Result)
                {
                    splash.UpdateStatus("Connected! Launching Everyview...", "Opening main search interface...", isComplete: true);
                    await Task.Delay(200);
                }

                var mainWindow = new MainWindow();
                mainWindow.Show();
                splash.FadeOutAndClose();
            }
        }
    }
}
