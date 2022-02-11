using System;
using System.Windows;

namespace NetSpeed.Wpf
{
    /// <summary>
    /// AboutWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            if (DesktopBridge.Helpers.IsRunningAsUwp())
            {
                var pkg = Windows.ApplicationModel.Package.Current;
                versionText.Text = $"{pkg.Id.Version.Major}.{pkg.Id.Version.Minor}";
                updateUrl.NavigateUri = new Uri("ms-windows-store://pdp/?productid=9N27PCTHF9KF");
            }
            else
            {
                var version = Application.ResourceAssembly.GetName().Version;
                versionText.Text = $"{version.Major}.{version.Minor}";
                updateUrl.NavigateUri = new Uri("https://github.com/LITTOMA/NetSpeedCat/releases/latest");
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Windows.System.Launcher.LaunchUriAsync(e.Uri);
            e.Handled = true;
        }
    }
}
