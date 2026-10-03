using Avalonia.Controls;

namespace pallyFilePaste.Views;

public partial class AboutPage : UserControl
{
    public AboutPage()
    {
        InitializeComponent();
        theVersion.Text = $"{AppVersion.GetReleaseDate()} - v{AppVersion.GetVersion()}";
    }
}