using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace EverythingModern.Services
{
    public class GlobalHotkeyManager : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_NOREPEAT = 0x4000;

        private const uint VK_F = 0x46;
        private const uint VK_SPACE = 0x20;

        private const int HOTKEY_ID_CTRL_ALT_F = 9001;
        private const int HOTKEY_ID_ALT_SPACE = 9002;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private IntPtr _hWnd;
        private HwndSource? _source;
        private readonly Action _onHotkeyPressed;
        private bool _isRegistered;

        public GlobalHotkeyManager(Window window, Action onHotkeyPressed)
        {
            _onHotkeyPressed = onHotkeyPressed;
            var helper = new WindowInteropHelper(window);
            _hWnd = helper.Handle;

            if (_hWnd == IntPtr.Zero)
            {
                window.SourceInitialized += (s, e) =>
                {
                    _hWnd = new WindowInteropHelper(window).Handle;
                    InitializeHook();
                };
            }
            else
            {
                InitializeHook();
            }
        }

        private void InitializeHook()
        {
            if (_isRegistered || _hWnd == IntPtr.Zero) return;

            _source = HwndSource.FromHwnd(_hWnd);
            _source?.AddHook(HwndHook);

            // Register primary hotkey: Ctrl + Alt + F
            bool registered = RegisterHotKey(_hWnd, HOTKEY_ID_CTRL_ALT_F, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, VK_F);
            if (!registered)
            {
                // Fallback: Ctrl + Shift + F
                RegisterHotKey(_hWnd, HOTKEY_ID_CTRL_ALT_F, MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT, VK_F);
            }

            // Also try Alt + Space if available
            RegisterHotKey(_hWnd, HOTKEY_ID_ALT_SPACE, MOD_ALT | MOD_NOREPEAT, VK_SPACE);

            _isRegistered = true;
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == HOTKEY_ID_CTRL_ALT_F || id == HOTKEY_ID_ALT_SPACE)
                {
                    _onHotkeyPressed?.Invoke();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (_isRegistered && _hWnd != IntPtr.Zero)
            {
                try
                {
                    UnregisterHotKey(_hWnd, HOTKEY_ID_CTRL_ALT_F);
                    UnregisterHotKey(_hWnd, HOTKEY_ID_ALT_SPACE);
                    _source?.RemoveHook(HwndHook);
                }
                catch { }
                _isRegistered = false;
            }
        }
    }
}
