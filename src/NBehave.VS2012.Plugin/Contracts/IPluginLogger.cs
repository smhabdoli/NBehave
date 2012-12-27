using System;
using System.Runtime.InteropServices;

namespace NBehave.VS2012.Plugin.Contracts
{
    [Guid("00685310-eb75-4c6a-953d-1cb901e1445d")]
    public interface IPluginLogger
    {
        void ErrorException(string message, Exception exception);
        void FatalException(string message, Exception exception);
    }
}
