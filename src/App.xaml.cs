using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Windows;

namespace NetSpeed.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Constants and Fields

        /// <summary>The event mutex name.</summary>
        private const string UniqueEventName = "{EA63DE40-641A-495C-8A37-5D7885646077}";

        /// <summary>The unique mutex name.</summary>
        private const string UniqueMutexName = "{4FDF8CF5-322E-4AA0-91BC-D059AD4768FE}";

        /// <summary>The event wait handle.</summary>
        private EventWaitHandle eventWaitHandle;

        /// <summary>The mutex.</summary>
        private Mutex mutex;
        private AboutWindow aboutWindow;
        private System.Windows.Forms.NotifyIcon notifyIcon;

        public bool IsShuttingDown { get; private set; }

        #endregion

        #region Methods

        /// <summary>The app on startup.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void AppOnStartup(object sender, StartupEventArgs e)
        {
            Log.Logger = new LoggerConfiguration()
#if TRACE
                .WriteTo.Console()
                .WriteTo.File(
                    Path.Combine(AppConfig.Default.AppDataFolder, "nsv.log"),
                    shared: true,
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information
                    )
#else
                .WriteTo.File(
                    Path.Combine(AppConfig.Default.AppDataFolder, "nsv.log"),
                    fileSizeLimitBytes: 1048576,
                    shared: true,
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error
                    )
#endif
                .CreateLogger();

            bool isOwned;
            mutex = new Mutex(true, UniqueMutexName, out isOwned);
            eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, UniqueEventName);

            // So, R# would not give a warning that this variable is not used.
            GC.KeepAlive(mutex);

            if (isOwned)
            {
                Log.Information("NetSpeed app is already running.");
                // Spawn a thread which will be waiting for our event
                var thread = new Thread(
                    () =>
                    {
                        while (eventWaitHandle.WaitOne())
                        {
                            Current.Dispatcher.BeginInvoke(
                                (Action)(() => ((MainWindow)Current.MainWindow).BringToForeground()));
                        }
                    });

                // It is important mark it as background otherwise it will prevent app from exiting.
                thread.IsBackground = true;

                thread.Start();
                return;
            }

            // Notify other instance so it could bring itself to foreground.
            eventWaitHandle.Set();

            // Terminate this instance.
            Shutdown();
        }

        public void About()
        {
            aboutWindow = aboutWindow ?? new AboutWindow();
            aboutWindow.ShowDialog();
        }
        public void ShowNotifyIcon()
        {
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Icon = NetSpeed.Wpf.Properties.Resources.icon;
            notifyIcon.Visible = true;

            // Setup context menu
            notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            notifyIcon.ContextMenuStrip.Items.Add(NetSpeed.Wpf.Properties.Resources.NetInterfaces, null, (s, e) =>
            {
                ((MainWindow)Application.Current.MainWindow).ToggleVisibility();
            });

            var startupItem =
                (System.Windows.Forms.ToolStripMenuItem)notifyIcon.ContextMenuStrip.Items.Add(NetSpeed.Wpf.Properties.Resources.Startup, null, (s, e) =>
                {
                    // Toggle toolstrip item checked state
                    var item = s as System.Windows.Forms.ToolStripMenuItem;
                    item.Checked = !item.Checked;
                    AppConfig.Default.SetStartup(item.Checked);
                });
            startupItem.VisibleChanged += (s, e) =>
            {
                if (startupItem.Visible)
                {
                    startupItem.Checked = AppConfig.Default.GetStartup();
                }
            };

            notifyIcon.ContextMenuStrip.Items.Add(NetSpeed.Wpf.Properties.Resources.AboutNetSpeedCat, null, (s, e) =>
            {
                About();
            });

            notifyIcon.ContextMenuStrip.Items.Add(NetSpeed.Wpf.Properties.Resources.Exit, null, (s, e) =>
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
                Shutdown();
            });

            notifyIcon.DoubleClick += (s, e) => ((MainWindow)Application.Current.MainWindow).ToggleVisibility();
        }

        public new void Shutdown()
        {
            IsShuttingDown = true;
            base.Shutdown();
        }

        #endregion
    }
}
