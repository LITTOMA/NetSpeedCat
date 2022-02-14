using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace NetSpeed.Wpf
{
    public partial class TaskBarWindow : Window, INotifyPropertyChanged
    {
        private readonly ObservableCollection<NetSpeedItem> netSpeedItems;
        private string uploadSpeed;
        private string downloadSpeed;
        private System.Windows.Forms.NotifyIcon notifyIcon;

        public event PropertyChangedEventHandler PropertyChanged;

        public string UploadSpeed
        {
            get => uploadSpeed;
            set => SetProperty(ref uploadSpeed, value);
        }

        public string DownloadSpeed
        {
            get => downloadSpeed;
            set => SetProperty(ref downloadSpeed, value);
        }

        private WindowInteropHelper windowInteropHelper;

        public IntPtr Handle { get; private set; }

        public TaskBarWindow()
        {
            InitializeComponent();
            DataContext = this;

            UploadSpeed = "0.00 B/s";
            DownloadSpeed = "0.00 B/s";

            MinHeight = TaskBarHelper.GetTaskBarSize().Height;
        }

        public TaskBarWindow(ObservableCollection<NetSpeedItem> netSpeedItems) : this()
        {
            this.netSpeedItems = netSpeedItems;
            BindEvent(this.netSpeedItems);
            this.netSpeedItems.CollectionChanged += NetSpeedItems_CollectionChanged;
            Loaded += OnLoaded;
            Closing += TaskBarWindow_Closing;
        }

        private void TaskBarWindow_Closing(object sender, CancelEventArgs e)
        {
            Serilog.Log.Information("Some one is closing this window");
            // Do not close the window
            e.Cancel = true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            windowInteropHelper = new WindowInteropHelper(this);
            Handle = windowInteropHelper.Handle;
            InitializeWindowsPlatform();

            // Hide window from "Tasks" view
            windowInteropHelper.Owner = PInvoke.User32.GetDesktopWindow();

            HwndSource hwndSource = HwndSource.FromHwnd(Handle);
            hwndSource.AddHook(new HwndSourceHook(WndProc));
        }

        private const uint WM_SYSTEMMENU = 0xa4;
        private const uint WP_SYSTEMMENU = 0x02;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if ((msg == WM_SYSTEMMENU) && (wParam.ToInt32() == WP_SYSTEMMENU))
            {
                AppContexMenu.IsOpen = true;
                handled = true;
            }

            return IntPtr.Zero;
        }

        private void InitializeWindowsPlatform()
        {
            // Only shows notify icon on Windows 11
            if (Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22000)
            {
                notifyIcon = new System.Windows.Forms.NotifyIcon();
                notifyIcon.Icon = Properties.Resources.icon;
                notifyIcon.Visible = true;

                // Setup context menu
                notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
                notifyIcon.ContextMenuStrip.Items.Add(Properties.Resources.NetInterfaces, null, (s, e) =>
                {
                    ((MainWindow)Application.Current.MainWindow).ToggleVisibility();
                });

                var startupItem = notifyIcon.ContextMenuStrip.Items.Add(Properties.Resources.Startup, null, (s, e) =>
                {
                    // Toggle toolstrip item checked state
                    var item = s as System.Windows.Forms.ToolStripMenuItem;
                    item.Checked = !item.Checked;
                    AppConfig.Default.SetStartup(item.Checked);
                });
                ((System.Windows.Forms.ToolStripMenuItem)startupItem).Checked = AppConfig.Default.GetStartup();

                notifyIcon.ContextMenuStrip.Items.Add(Properties.Resources.AboutNetSpeedCat, null, (s, e) =>
                {
                    notifyIcon.Visible = false;
                    notifyIcon.Dispose();
                    About();
                });

                notifyIcon.ContextMenuStrip.Items.Add(Properties.Resources.Exit, null, (s, e) =>
                {
                    notifyIcon.Visible = false;
                    notifyIcon.Dispose();
                    ExitApp();
                });

                notifyIcon.DoubleClick += (s, e) => ((MainWindow)Application.Current.MainWindow).ToggleVisibility();
            }

            StartupItem.IsChecked = AppConfig.Default.GetStartup();

            // Windows 10 initializations
            if (Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build < 22000)
            {
                // TODO
            }

            // Listen to system theme change
            Microsoft.Win32.SystemEvents.UserPreferenceChanged += (s, e) =>
            {
                if (e.Category == Microsoft.Win32.UserPreferenceCategory.General)
                    SetTheme();
            };
            SetTheme();
        }

        private void SetTheme()
        {
            string keyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            using (Microsoft.Win32.RegistryKey rKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyName))
            {
                object value = null;
                if (rKey == null || (value = rKey.GetValue("SystemUsesLightTheme")) == null)
                {
                    SetLightTheme();
                }
                int theme = (int)value;
                if (theme == 1)
                    SetLightTheme();
                else if (theme == 0)
                    SetDarkTheme();
            }
        }

        private void SetDarkTheme()
        {
            // Set Foreground of all TextBlocks to black using resource dictionary
            Resources["NormalTextColor"] = new SolidColorBrush(Colors.White);
        }

        private void SetLightTheme()
        {
            // Set Foreground of all TextBlocks to black using resource dictionary
            Resources["NormalTextColor"] = new SolidColorBrush(Colors.Black);
        }

        private void ExitApp()
        {
            Application.Current.Shutdown();
        }

        private void NetSpeedItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                BindEvent(e.NewItems.Cast<NetSpeedItem>().ToList());
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                UnBindEvent(e.OldItems.Cast<NetSpeedItem>().ToList());
            }

            // If the collection is empty
            if (netSpeedItems.Count == 0)
            {
                UploadSpeed = "0.00 B/s";
                DownloadSpeed = "0.00 B/s";
            }
        }

        private void BindEvent(IList<NetSpeedItem> items)
        {
            foreach (var item in items)
            {
                item.SpeedChanged += NetSpeedItem_SpeedChanged;
            }
        }

        private void UnBindEvent(IList<NetSpeedItem> items)
        {
            foreach (var item in items)
            {
                item.SpeedChanged -= NetSpeedItem_SpeedChanged;
            }
        }

        private void NetSpeedItem_SpeedChanged(object sender, NetSpeedEventArgs e)
        {
            var totalUploadSpeed = netSpeedItems.Sum(i => i.SpeedSent);
            var totalDownloadSpeed = netSpeedItems.Sum(i => i.SpeedReceived);

            UploadSpeed = NetSpeedItem.HumanReadableSpeed(totalUploadSpeed);
            DownloadSpeed = NetSpeedItem.HumanReadableSpeed(totalDownloadSpeed);

            // Invoke on main thread
            Dispatcher.Invoke(() =>
            {
                // Serilog.Log.Information("Updating taskbar window...");
                // var taskbarSize = TaskBarHelper.GetTaskBarSize();
                // var taskbarLocation = TaskBarHelper.GetTaskBarLocation();
                // if (taskbarLocation == TaskBarHelper.TaskBarLocation.Top || taskbarLocation == TaskBarHelper.TaskBarLocation.Bottom)
                // {
                //     Width = 100;
                //     Height = taskbarSize.Height;
                // }
                // else if (taskbarLocation == TaskBarHelper.TaskBarLocation.Left || taskbarLocation == TaskBarHelper.TaskBarLocation.Right)
                // {
                //     Width = taskbarSize.Width;
                //     Height = ContentArea.ActualHeight + 10;
                // }
                // Serilog.Log.Information($"Height: {Height}, Width: {Width}");

                // Unlock the window
                WindowPos.SetIsLocked(this, false);
                Serilog.Log.Information("Unlocked window");

                TaskBarHelper.PutSubWindow(this);
                Serilog.Log.Information("Put window to taskbar");

                // Lock the window
                WindowPos.SetIsLocked(this, true);
                Serilog.Log.Information("Locked window");

                Show();
            });
        }

        private void SetProperty<T>(ref T target, T value, [CallerMemberName] string propertyName = null)
        {
            target = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                ((MainWindow)Application.Current.MainWindow).ToggleVisibility();
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            ExitApp();
        }

        private void Startup_Click(object sender, RoutedEventArgs e)
        {
            // Toggle menu item checked state
            var menuItem = (MenuItem)sender;
            menuItem.IsChecked = !menuItem.IsChecked;
            var startup = menuItem.IsChecked;

            // Set startup
            AppConfig.Default.SetStartup(startup);
        }

        private void MainWindowVisible_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Application.Current.MainWindow).ToggleVisibility();
        }

        private void AboutItem_Click(object sender, RoutedEventArgs e)
        {
            About();
        }

        private void About()
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.Show();
        }
    }
}
