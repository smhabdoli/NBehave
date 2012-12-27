using System.Runtime.InteropServices;

namespace NBehave.VS2012.Plugin.Contracts
{
    [Guid("212a1c76-12b4-486b-aad4-d57c8a73ada1")]
    [ComVisible(true)]
    public interface IVisualStudioService
    {
        string GetProjectAssemblyOutputPath();
        string GetActiveDocumentFullName();
        void AttachDebugger(int processId);
        void BuildSolution();
        string GetTargetFrameworkVersion();
        string ReferencedAssemblyFolder(string referencedAssembly);
        bool Is32Bit { get; }
        bool IsSolutionOpen { get; }
        string SolutionFolder { get; }
    }
}