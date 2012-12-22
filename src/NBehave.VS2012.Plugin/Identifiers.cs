using System;

namespace NBehave.VS2012.Plugin
{
    internal static class Identifiers
    {
        // These guids must match those in VSPackage2.vsct
        public const string NBehavePackageGuidString = "9f5c3a26-d26a-4733-ac9b-f32002ed1c9f";

        public static readonly Guid CommandGroupSet = new Guid("1060fdb6-d772-4b43-9f3a-d11690bf6524");
        public static readonly Guid TopLevelMenuCmdSet = new Guid("863f4db0-6047-436c-999f-27f75034680f");

        public const uint RunCommandId = 0x100;
        public const uint DebugCommandId = 0x101;

        public const int MenuCommandHtmlReportToggleId = 0x201;


    }
}