namespace NBehave.VS2012.Plugin.Editor.Domain
{
    public interface IGherkinText
    {
        string Title { get; }
        string AsString { get; }
        int SourceLine { get; }
    }
}