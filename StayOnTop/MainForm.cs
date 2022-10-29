namespace StayOnTop
{
    public partial class MainForm : Form
    {
        private readonly bool _allowVisible;
        private bool _allowClose;

        private readonly ContextMenuStrip _contextMenuStrip = new();

        private Dictionary<IntPtr, WindowInfo> _windows = new();
        private readonly Dictionary<IntPtr, WindowInfo> _pinned = new();
        //private Timer _refresher = new() { Interval = 500 };
        //private List<WindowInfo> _windowInfos;


        public MainForm()
        {
            InitializeComponent();
            // _refresher.Tick += Refresher_Tick;
            _contextMenuStrip.Opening += (sender, e) => RefreshMenuItems();
            Text = Resource.AppName;
            notifyIcon.Text = Resource.AppName;
            notifyIcon.ContextMenuStrip = _contextMenuStrip;
            RefreshMenuItems();
            //_refresher.Start();
        }
        private void RefreshMenuItems()
        {
            _windows = WindowService.GetOpenedWindows();

            _contextMenuStrip.Items.Clear();
            var menuItems = _windows.Select(pair => new ToolStripMenuItem(pair.Value.Title, default, WindowMenuItemOnClick)).ToArray();
            _contextMenuStrip.Items.AddRange(menuItems);
            _contextMenuStrip.Items.Add(new ToolStripSeparator());
            _contextMenuStrip.Items.Add(new ToolStripMenuItem("Exit", default, (sender, e) =>
            {
                _allowClose = true;
                Application.Exit();
            })
            { Font = new Font(Font, FontStyle.Bold) });

            foreach (var item in menuItems)
            {
                string? str = _pinned.FirstOrDefault(wnd => wnd.Value?.Title == item.Text).Value?.Title;
                if (item.Text == str)
                    item.Checked = true;
            }
            _contextMenuStrip.Refresh();
        }

        private void WindowMenuItemOnClick(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem item and not null)
            {
                KeyValuePair<IntPtr, WindowInfo> handleInfoPair = _windows.FirstOrDefault(x => x.Value.Title == item.Text);
                if (handleInfoPair.Equals(default(KeyValuePair<IntPtr, WindowInfo>))) return;

                IntPtr hWnd = handleInfoPair.Key;
                if (_pinned.ContainsKey(hWnd))
                {
                    _ = WindowService.Reset(hWnd);
                    _ = _pinned.Remove(hWnd);
                    handleInfoPair.Value.WindowHandle = SpecialWindowHandles.HWND_NOTOPMOST;
                    return;
                }
                _ = WindowService.SetTopmost(hWnd);
                _ = WindowService.SetForeground(hWnd);
                _pinned.Add(hWnd, handleInfoPair.Value);
                _windows[hWnd].WindowHandle = SpecialWindowHandles.HWND_TOPMOST;
            }
            else throw new NullReferenceException(nameof(item));
        }
        #region overrides
        protected override void SetVisibleCore(bool value)
        {
            if (!_allowVisible)
            {
                value = false;
                if (!IsHandleCreated) CreateHandle();
            }
            base.SetVisibleCore(value);
            notifyIcon.ShowBalloonTip(500, Resource.AppName, "Now active in tray", ToolTipIcon.Info);
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_allowClose)
            {
                Hide();
                e.Cancel = true;
            }
            base.OnFormClosing(e);
        }
        #endregion
    }
}
