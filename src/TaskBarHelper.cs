using PInvoke;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

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

        delegate bool EnumChildProc(IntPtr hwnd, IntPtr lParam);
        public static void PutSubWindow(IntPtr handle, System.Windows.Size size)
        {
            // Get the window handle
            IntPtr nscHandle = handle;

            if (nscHandle == IntPtr.Zero)
                return;

            // Get the taskbar handle
            IntPtr taskbarHandle = User32.FindWindow("Shell_TrayWnd", string.Empty);
            if (taskbarHandle == IntPtr.Zero)
                return;

            // Get ReBarWindow32 handle
            IntPtr rebarHandle = User32.FindWindowEx(taskbarHandle, IntPtr.Zero, "ReBarWindow32", string.Empty);
            if (rebarHandle == IntPtr.Zero)
                return;

            // Get MSTaskSwWClass handle
            IntPtr mstaskHandle = User32.FindWindowEx(rebarHandle, IntPtr.Zero, "MSTaskSwWClass", null);
            if (mstaskHandle == IntPtr.Zero)
                return;


            var isChild = User32.IsChild(rebarHandle, nscHandle);
            if (!isChild)
            {
                // Set parent
                User32.SetParent(nscHandle, rebarHandle);
                var style = User32.GetWindowLong(nscHandle, User32.WindowLongIndexFlags.GWL_STYLE);
                style |= (int)User32.WindowStyles.WS_CHILD;
                User32.SetWindowLong(nscHandle, User32.WindowLongIndexFlags.GWL_STYLE, (User32.SetWindowLongFlags)style);
            }


            // Get the client rect of nscHandle
            User32.GetClientRect(nscHandle, out RECT nscClientRect);
            // Get the client rect of rebarHandle
            User32.GetClientRect(rebarHandle, out RECT rebarClientRect);
            // Get the client rect of mstaskHandle
            User32.GetClientRect(mstaskHandle, out RECT mstaskClientRect);
            // Get the window rect of rebarHandle
            User32.GetWindowRect(rebarHandle, out RECT rebarWindowRect);

            Rectangle nscRectangle = new Rectangle();
            Rectangle msTaskRectangle = new Rectangle();

            nscRectangle.Width = (int)size.Width;
            nscRectangle.Height = (int)size.Height;

            msTaskRectangle.X = mstaskClientRect.left;
            msTaskRectangle.Y = mstaskClientRect.top;

            var taskbarLocation = GetTaskBarLocation();
            if (taskbarLocation == TaskBarLocation.Top || taskbarLocation == TaskBarLocation.Bottom)
            {
                // Find the leftmost child window of ReBarWindow32, except the MSTaskSwWClass window
                IntPtr leftmostChildHandle = IntPtr.Zero;
                int leftSide = rebarClientRect.right;
                EnumChildProc enumChildProc = delegate (IntPtr hwnd, IntPtr lParam)
                {
                    if (hwnd == mstaskHandle || User32.IsChild(mstaskHandle, hwnd) || hwnd == nscHandle)
                        return true;

                    RECT rect = new RECT();
                    User32.GetWindowRect(hwnd, out rect);
                    if (rect.left < leftSide)
                    {
                        leftSide = rect.left;
                        leftmostChildHandle = hwnd;
                    }
                    return true;
                };
                User32.EnumChildWindows(rebarHandle, Marshal.GetFunctionPointerForDelegate(enumChildProc), IntPtr.Zero);


                nscRectangle.X = leftSide - rebarWindowRect.left - (int)size.Width;
                nscRectangle.Y = nscClientRect.top;
                msTaskRectangle.Width = nscRectangle.X;
                msTaskRectangle.Height = rebarClientRect.bottom - rebarClientRect.top;
            }
            else
            {
                // Find the topmost child window of ReBarWindow32, except the MSTaskSwWClass window
                IntPtr topmostChildHandle = IntPtr.Zero;
                int topSide = rebarClientRect.bottom;
                EnumChildProc enumChildProc = delegate (IntPtr hwnd, IntPtr lParam)
                {
                    if (hwnd == mstaskHandle || User32.IsChild(mstaskHandle, hwnd) || hwnd == nscHandle)
                        return true;

                    RECT rect = new RECT();
                    User32.GetWindowRect(hwnd, out rect);
                    if (rect.top < topSide)
                    {
                        topSide = rect.top;
                        topmostChildHandle = hwnd;
                    }
                    return true;
                };
                User32.EnumChildWindows(rebarHandle, Marshal.GetFunctionPointerForDelegate(enumChildProc), IntPtr.Zero);


                nscRectangle.X = nscClientRect.left;
                nscRectangle.Y = topSide - rebarWindowRect.top - (int)size.Height;
                msTaskRectangle.Width = rebarClientRect.right - rebarClientRect.left;
                msTaskRectangle.Height = nscRectangle.Y;
            }

            // Put nsc tb window to the taskbar
            User32.MoveWindow(
                nscHandle,
                nscRectangle.X,
                nscRectangle.Y,
                nscRectangle.Width,
                nscRectangle.Height,
                true);

            // Change height of mstask window
            User32.MoveWindow(
                mstaskHandle,
                msTaskRectangle.X,
                msTaskRectangle.Y,
                msTaskRectangle.Width,
                msTaskRectangle.Height,
                true);

            User32.UpdateWindow(taskbarHandle);
        }
    }
}