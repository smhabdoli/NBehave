using System.ComponentModel;

namespace NBehave.VS2012.Plugin.Domain
{
    public interface IPluginConfiguration : INotifyPropertyChanged
    {
        bool CreateHtmlReport { get; set; }
    }
}