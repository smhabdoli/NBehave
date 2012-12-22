namespace NBehave.VS2012.Plugin.Editor.Glyphs.Views
{
    public interface IRunOrDebugView
    {
        bool IsMouseOverPopup { get; }
        void Deselect();
    }
}