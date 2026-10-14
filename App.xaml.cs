using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using EverythingModern.Services;

namespace EverythingModern
{
    public partial class App : Application
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private static readonly IntPtr HWND_BROADCAST = (IntPtr)0xffff;
        private static readonly uint WM_EVERYVIEW_SUMMON = RegisterWindowMessage("EVERYVIEW_SUMMON_INSTANCE_MSG");

        private static Mutex? _singleInstanceMutex;
        private SplashScreenWindow? _splashWindow;

        public App()
        {
            DispatcherUnhandledException += (s, args) =>
            {
                try
                {
                    File.AppendAllText("app_startup.log", $"{DateTime.Now}: DispatcherUnhandledException: {args.Exception}\n");
                }
                catch { }

                MessageBox.Show(
                    $"Everyview encountered an unexpected error:\n\n{args.Exception.Message}\n\nStack:\n{args.Exception}",
                    "Everyview Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = false;
            };
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

                // Check for single instance: if another instance is already running, summon it and exit
                _singleInstanceMutex = new Mutex(true, "Everyview_SingleInstance_Mutex_399f", out bool isNewInstance);
                if (!isNewInstance)
                {
                    ActivateExistingInstance();
                    Shutdown();
                    return;
                }

                // Prevent app from exiting when splash closes
                ShutdownMode = ShutdownMode.OnExplicitShutdown;

                // 1. Show Splash Screen IMMEDIATELY (< 50ms)
                _splashWindow = new SplashScreenWindow();
                _splashWindow.Show();

                // Force splash window to paint its initial frame immediately
                _splashWindow.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);

                bool skipRequested = false;
                _splashWindow.SkipRequested += () => { skipRequested = true; };

                _splashWindow.UpdateStatus("Initializing Everyview v1.1...", "Connecting to Everything Engine and loading index...");

                // Warm up and verify Everything Engine in background during splash
                var daemonWarmupTask = EverythingDaemonManager.EnsureDaemonRunningAsync(
                    timeoutMs: 3500,
                    progressCallback: msg =>
                    {
                        _splashWindow?.Dispatcher.Invoke(() =>
                        {
                            _splashWindow?.UpdateStatus("Connecting Everything Engine...", msg);
                        });
                    });

                // Smooth visual entry animation
                if (!skipRequested)
                {
                    await Task.Delay(120);
                }

                if (!skipRequested)
                {
                    _splashWindow.UpdateStatus("Loading Search Interface...", "Initializing Fluent Mica theme and controls...");
                }

                // 2. Initialize MainWindow (XAML parsing occurs while splash is visible)
                var mainWindow = new MainWindow();
                MainWindow = mainWindow;
                ShutdownMode = ShutdownMode.OnMainWindowClose;

                // Wait briefly for daemon warmup task (up to 400ms or until done)
                await Task.WhenAny(daemonWarmupTask, Task.Delay(skipRequested ? 0 : 400));

                if (!skipRequested)
                {
                    _splashWindow.UpdateStatus("Ready!", "Welcome to Everyview v1.1", isComplete: true);
                    await Task.Delay(140);
                }

                // 3. Reveal MainWindow and smoothly fade out Splash
                mainWindow.Show();
                mainWindow.Activate();
                mainWindow.Focus();

                _splashWindow.FadeOutAndClose();
            }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText("app_startup.log", $"{DateTime.Now}: EXCEPTION: {ex}\n");
                }
                catch { }

                MessageBox.Show($"Startup Exception:\n\n{ex.Message}\n\n{ex}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private static void ActivateExistingInstance()
        {
            try
            {
                PostMessage(HWND_BROADCAST, WM_EVERYVIEW_SUMMON, IntPtr.Zero, IntPtr.Zero);
            }
            catch { }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_singleInstanceMutex != null)
            {
                try
                {
                    _singleInstanceMutex.ReleaseMutex();
                    _singleInstanceMutex.Dispose();
                }
                catch { }
            }
            base.OnExit(e);
        }
    }
}
