using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Timer = System.Windows.Forms.Timer;

namespace StayOnTop
{
    public static class WindowService
    {
        #region Native
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        //WARN: Only for "Any CPU":
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out uint processId);
        #endregion

        private delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);
        internal static WindowInfo _windowActive = new();
        internal static event EventHandler? WindowActivatedChanged;
        internal static Timer _timerWatcher = new();


        internal static void DoStartWatcher()
        {
            _timerWatcher.Interval = 500;
            _timerWatcher.Tick += TimerTick;
        }

        private static void TimerTick(object? sender, EventArgs e)
        {
            var windowActive = new WindowInfo
            {
                Handle = GetForegroundWindow()
            };
            string path = GetProcessPath(windowActive.Handle);
            if (string.IsNullOrEmpty(path)) return;
            windowActive.File = new FileInfo(path);

            int length = GetWindowTextLength(windowActive.Handle);
            if (length == 0) return;

            var sb = new StringBuilder(length);
            _ = GetWindowText(windowActive.Handle, sb, length + 1);
            windowActive.Title = sb.ToString();
            if (windowActive.ToString() != _windowActive.ToString())
            {
                //fire:
                _windowActive = windowActive;
                WindowActivatedChanged?.Invoke(sender, e);
                Console.WriteLine("Window: " + _windowActive.ToString());
            };
            _timerWatcher.Start();
        }

        public static bool SetForeground(IntPtr hWnd) => SetForegroundWindow(hWnd);
        public static bool SetTopmost(IntPtr hWnd) => SetWindowPosition(hWnd, SpecialWindowHandles.HWND_TOPMOST);
        public static bool Reset(IntPtr hWnd) => SetWindowPosition(hWnd, SpecialWindowHandles.HWND_NOTOPMOST);
        private static bool SetWindowPosition(IntPtr hWnd, SpecialWindowHandles handleFlag)
        {
            try
            {
                bool gotRect = GetWindowRect(hWnd, out RECT rct);
                if (!gotRect) return false;
                bool isSet = SetWindowPos(hWnd,
                                        (IntPtr)handleFlag,
                                        rct.X,
                                        rct.Y,
                                        rct.Width,
                                        rct.Height,
                                        (uint)SetWindowPosFlags.SWP_SHOWWINDOW);
                return isSet;
            }
            catch (Exception e) { MessageBox.Show(e.Message); return false; }
        }
        public static Dictionary<IntPtr, WindowInfo> GetOpenedWindows()
        {
            IntPtr shellWindow = GetShellWindow();
            var windows = new Dictionary<IntPtr, WindowInfo>();

            _ = EnumWindows(new EnumWindowsProc(delegate (IntPtr hWnd, int lParam)
            {
                if (hWnd == shellWindow) return true;
                if (!IsWindowVisible(hWnd)) return true;

                int length = GetWindowTextLength(hWnd);
                if (length == 0) return true;

                var sb = new StringBuilder(length);
                _ = GetWindowText(hWnd, sb, length + 1);
                string filePath = GetProcessPath(hWnd);
                var info = new WindowInfo
                {
                    Handle = hWnd,
                    File = !string.IsNullOrWhiteSpace(filePath) ? new FileInfo(filePath) : null,
                    Title = sb.ToString()
                };
                windows[hWnd] = info;
                return true;
            }), 0);
            return windows;
        }
        public static string GetProcessPath(IntPtr hwnd)
        {
            _ = GetWindowThreadProcessId(hwnd, out uint pid);
            if (hwnd != IntPtr.Zero)
            {
                if (pid != 0)
                {
                    using var process = Process.GetProcessById((int)pid);
                    if (process != null)
                    {

                        try { return process.MainModule?.FileName?.ToString() ?? ""; }
                        catch (Exception) { return ""; }
                    }
                }
            }
            return "";
        }

    }
}
