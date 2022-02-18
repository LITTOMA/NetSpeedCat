using PInvoke;
using System;
using System.Drawing;

namespace NetSpeed.Wpf
{
    public static class TaskBarHelper
    {
        public enum TaskBarLocation { Top, Bottom, Left, Right }

        public static TaskBarLocation GetTaskBarLocation()
        {
            TaskBarLocation taskBarLocation = TaskBarLocation.Bottom;
            bool taskBarOnTopOrBottom = System.Windows.SystemParameters.WorkArea.Width == System.Windows.SystemParameters.PrimaryScreenWidth;
            if (taskBarOnTopOrBottom)
            {
                if (System.Windows.SystemParameters.WorkArea.Top > 0) taskBarLocation = TaskBarLocation.Top;
            }
            else
            {
                if (System.Windows.SystemParameters.WorkArea.Left > 0)
                {
                    taskBarLocation = TaskBarLocation.Left;
                }
                else
                {
                    taskBarLocation = TaskBarLocation.Right;
                }
            }
            return taskBarLocation;
        }

        public static Size GetTaskBarSize()
        {
            var tblocation = GetTaskBarLocation();
            var taskBarSize = new Size();
            switch (tblocation)
            {
                case TaskBarLocation.Top:
                case TaskBarLocation.Bottom:
                    taskBarSize.Height = (int)(System.Windows.SystemParameters.PrimaryScreenHeight - System.Windows.SystemParameters.WorkArea.Height);
                    taskBarSize.Width = (int)System.Windows.SystemParameters.PrimaryScreenWidth;
                    break;
                case TaskBarLocation.Left:
                case TaskBarLocation.Right:
                    taskBarSize.Height = (int)System.Windows.SystemParameters.PrimaryScreenHeight;
                    taskBarSize.Width = (int)(System.Windows.SystemParameters.PrimaryScreenWidth - System.Windows.SystemParameters.WorkArea.Width);
                    break;
            }
            return taskBarSize;
        }

        public static Point GetTrayIconLocation()
        {
            var taskBarHandle = User32.FindWindow("Shell_TrayWnd", string.Empty);
            var trayIconHandle = User32.FindWindowEx(taskBarHandle, IntPtr.Zero, "TrayNotifyWnd", string.Empty);
            var rect = new RECT();
            User32.GetWindowRect(trayIconHandle, out rect);
            return new Point(rect.left, rect.top);
        }

        public static void PutSubWindow(TaskBarWindow window)
        {
            // Get the window handle
            IntPtr nscHandle = window.Handle;
            // Get the taskbar handle
            IntPtr taskbarHandle = User32.FindWindow("Shell_TrayWnd", string.Empty);

            // Set parent
            User32.SetParent(nscHandle, taskbarHandle);
            var style = User32.GetWindowLong(nscHandle, User32.WindowLongIndexFlags.GWL_STYLE);
            style |= (int)User32.WindowStyles.WS_CHILD;
            User32.SetWindowLong(nscHandle, User32.WindowLongIndexFlags.GWL_STYLE, (User32.SetWindowLongFlags)style);

            // Move window next to tray icon
            var trayLocation = GetTrayIconLocation();
            var taskbarLocation = GetTaskBarLocation();
            RECT rectTb;
            User32.GetClientRect(taskbarHandle, out rectTb);
            if (taskbarLocation == TaskBarLocation.Top || taskbarLocation == TaskBarLocation.Bottom)
            {
                User32.MoveWindow(nscHandle, trayLocation.X - (int)window.Width, rectTb.top, (int)window.Width, (int)window.Height, true);
                User32.UpdateWindow(taskbarHandle);
            }
            else
            {
                User32.MoveWindow(nscHandle, 0, trayLocation.Y - (int)window.Height, (int)window.Width, (int)window.Height, true);
                User32.UpdateWindow(taskbarHandle);
            }
        }
    }
}