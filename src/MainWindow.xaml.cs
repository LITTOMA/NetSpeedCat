using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace NetSpeed.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<NetSpeedItem> NetSpeedItems { get; set; }
        private readonly ObservableCollection<NetSpeedItem> ItemsToShowOnTaskBar = new ObservableCollection<NetSpeedItem>();
        private TaskBarWindow taskBarWindow;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            CancellationTokenSource cts = new CancellationTokenSource();
            Closing += (s, e) => cts.Cancel();

            // Get all network interfaces
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            NetSpeedItems = interfaces.Select(i => new NetSpeedItem(i, 1000, cts.Token)).ToList();

            var selectedAddresses = AppConfig.Default.GetSelectedInterfaces();
            foreach (var item in NetSpeedItems)
            {
                if (selectedAddresses.Contains(item.ToString()))
                {
                    ItemsToShowOnTaskBar.Add(item);
                }
            }

            CreateTaskbarWindow();

            Loaded += Onloaded;
        }

        private void CreateTaskbarWindow()
        {
            taskBarWindow = new TaskBarWindow(ItemsToShowOnTaskBar);
            ((App)App.Current).ShuttingDown += (s, e) =>
            {
                taskBarWindow.Close();
            };
        }

        private void Onloaded(object sender, RoutedEventArgs e)
        {
            if (ItemsToShowOnTaskBar != null && ItemsToShowOnTaskBar.Count > 0)
            {
                Hide();
            }

            try
            {
                ((App)Application.Current).ShowNotifyIcon();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to show notify icon");
            }
        }

        /// <summary>Brings main window to foreground.</summary>
        public void BringToForeground()
        {
            if (WindowState == WindowState.Minimized || Visibility == Visibility.Hidden)
            {
                Show();
                WindowState = WindowState.Normal;
            }

            // According to some sources these steps gurantee that an app will be brought to foreground.
            Activate();
            Topmost = true;
            Topmost = false;
            Focus();
        }

        public void ToggleVisibility()
        {
            if (Visibility == Visibility.Visible)
            {
                Hide();
            }
            else
            {
                BringToForeground();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!((App)App.Current).IsShuttingDown)
            {
                // Hide the window
                e.Cancel = true;
                Hide();
            }
        }

        private void NetInterfaceCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            if (cb == null)
                return;

            var item = cb.DataContext as NetSpeedItem;
            if (item == null)
                return;

            if (!ItemsToShowOnTaskBar.Contains(item))
            {
                ItemsToShowOnTaskBar.Add(item);
            }

            AppConfig.Default.SetSelectedInterfaces(ItemsToShowOnTaskBar.Select(i => i.ToString()).ToArray());
        }

        private void NetInterfaceCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            if (cb == null)
                return;

            var item = cb.DataContext as NetSpeedItem;
            if (item == null)
                return;

            if (ItemsToShowOnTaskBar.Contains(item))
            {
                ItemsToShowOnTaskBar.Remove(item);
            }
            AppConfig.Default.SetSelectedInterfaces(ItemsToShowOnTaskBar.Select(i => i.ToString()).ToArray());
        }

        private void CheckBox_Loaded(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            if (cb != null && cb.DataContext != null)
            {
                var nsItem = (NetSpeedItem)cb.DataContext;
                if (ItemsToShowOnTaskBar.Contains(nsItem))
                {
                    cb.IsChecked = true;
                }
                else
                {
                    cb.IsChecked = false;
                }
            }
        }
    }
}
