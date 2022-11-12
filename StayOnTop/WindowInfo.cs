namespace StayOnTop
{
    public class WindowInfo
    {
        public IntPtr Handle = IntPtr.Zero;
        public FileInfo? File = new(Application.ExecutablePath);
        public string Title = Application.ProductName;
        public SpecialWindowHandles WindowHandle;
        public override string ToString()
        {
            return File?.Name + "\t>\t" + Title;
        }
    }
}
