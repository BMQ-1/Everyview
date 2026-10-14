using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace EverythingModern.Services
{
    public class TrayIconManager : IDisposable
    {
        private const int WM_USER = 0x0400;
        private const int WM_TRAYICON = WM_USER + 2048;

        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_RBUTTONUP = 0x0205;

        private const uint NIM_ADD = 0x00000000;
        private const uint NIM_MODIFY = 0x00000001;
        private const uint NIM_DELETE = 0x00000002;

        private const uint NIF_MESSAGE = 0x00000001;
        private const uint NIF_ICON = 0x00000002;
        private const uint NIF_TIP = 0x00000004;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uID;
            public uint uFlags;
            public int uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public int dwState;
            public int dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public int uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public int dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private readonly Window _window;
        private readonly Action _onRestoreRequested;
        private IntPtr _hWnd;
        private IntPtr _hIcon;
        private HwndSource? _source;
        private bool _isCreated;
        private ContextMenu? _trayMenu;

        public TrayIconManager(Window window, Action onRestoreRequested)
        {
            _window = window;
            _onRestoreRequested = onRestoreRequested;

            var helper = new WindowInteropHelper(window);
            _hWnd = helper.Handle;

            if (_hWnd == IntPtr.Zero)
            {
                window.SourceInitialized += (s, e) =>
                {
                    _hWnd = new WindowInteropHelper(window).Handle;
                    InitializeTray();
                };
            }
            else
            {
                InitializeTray();
            }
        }

        private void InitializeTray()
        {
            if (_isCreated || _hWnd == IntPtr.Zero) return;

            _source = HwndSource.FromHwnd(_hWnd);
            _source?.AddHook(HwndHook);

            // Load icon from Assets\app.ico or fallback to app icon
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "app.ico");
            if (!File.Exists(iconPath))
            {
                iconPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "app.ico");
            }

            if (File.Exists(iconPath))
            {
                _hIcon = LoadImage(IntPtr.Zero, iconPath, 1 /* IMAGE_ICON */, 16, 16, 0x00000010 /* LR_LOADFROMFILE */);
            }

            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hWnd,
                uID = 1001,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = WM_TRAYICON,
                hIcon = _hIcon,
                szTip = "Everyview - Ultra-fast File Search (Ctrl+Alt+F)"
            };

            _isCreated = Shell_NotifyIcon(NIM_ADD, ref nid);
            CreateContextMenu();
        }

        private void CreateContextMenu()
        {
            _trayMenu = new ContextMenu();

            var openItem = new MenuItem { Header = "Show Everyview (Ctrl+Alt+F)" };
            openItem.Click += (s, e) => _onRestoreRequested?.Invoke();
            _trayMenu.Items.Add(openItem);

            _trayMenu.Items.Add(new Separator());

            var exitItem = new MenuItem { Header = "Exit Everyview" };
            exitItem.Click += (s, e) =>
            {
                Dispose();
                Application.Current.Shutdown();
            };
            _trayMenu.Items.Add(exitItem);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_TRAYICON)
            {
                int mouseMsg = lParam.ToInt32();
                if (mouseMsg == WM_LBUTTONUP || mouseMsg == WM_LBUTTONDBLCLK)
                {
                    _onRestoreRequested?.Invoke();
                    handled = true;
                }
                else if (mouseMsg == WM_RBUTTONUP)
                {
                    ShowTrayMenu();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private void ShowTrayMenu()
        {
            if (_trayMenu == null) return;
            SetForegroundWindow(_hWnd);
            _trayMenu.IsOpen = true;
        }

        public void Dispose()
        {
            if (_isCreated)
            {
                var nid = new NOTIFYICONDATA
                {
                    cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                    hWnd = _hWnd,
                    uID = 1001
                };
                Shell_NotifyIcon(NIM_DELETE, ref nid);
                _isCreated = false;
            }

            if (_hIcon != IntPtr.Zero)
            {
                DestroyIcon(_hIcon);
                _hIcon = IntPtr.Zero;
            }

            _source?.RemoveHook(HwndHook);
        }
    }
}
