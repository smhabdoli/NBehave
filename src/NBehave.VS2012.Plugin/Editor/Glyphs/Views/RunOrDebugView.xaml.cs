using System.Windows.Controls.Primitives;

namespace NBehave.VS2012.Plugin.Editor.Glyphs.Views
{
    /// <summary>
    /// Interaction logic for RunOrDebugView.xaml
    /// </summary>
    public partial class RunOrDebugView : Popup, IRunOrDebugView
    {
        public RunOrDebugView()
        {
            InitializeComponent();
        }

        public bool IsMouseOverPopup { get { return this.IsMouseOver; }}
        public void Deselect()
        {
//            this.commandListBox.SelectedItem = null;
//            this.commandListBox.SelectedIndex = -1;
            this.commandListBox.SelectedValue = null;
        }
    }
}
