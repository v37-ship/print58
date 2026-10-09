using System.Windows;

namespace WanggaSpa.App;

public partial class AboutWindow : Window
{
    public AboutWindow(string detail)
    {
        InitializeComponent();
        VersionText.Text = $"Versi {AppInfo.Version} (build {AppInfo.BuildTime})";
        DetailText.Text = detail;
    }

    void OnClose(object sender, RoutedEventArgs e) => Close();
}
