using System.Runtime.InteropServices;

namespace NBehave.VS2012.Plugin.Contracts
{
    [Guid("14600e9c-c610-45ab-8202-e33434747327")]
    [ComVisible(true)]
    public interface IOutputWindow
    {
        void WriteLine(string message);
        void BringToFront();
        void Clear();
    }
}