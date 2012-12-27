using System;

namespace NBehave.VS2012.Plugin
{
    internal static class Identifiers
    {
        // These guids must match those in VSPackage2.vsct
        public const string NBehavePackageGuidString = "a7fb21c3-d0a4-4443-9461-95af7699a242";

        public static readonly Guid CommandGroupSet = new Guid("79e08fee-c965-4923-b7dc-b30d269bad9c");
        public static readonly Guid TopLevelMenuCmdSet = new Guid("1f7dffc2-d8b1-4022-8333-fb4c976c857f");

        public const uint RunCommandId = 0x100;
        public const uint DebugCommandId = 0x101;

        public const int MenuCommandHtmlReportToggleId = 0x201;


    }
}