// From: https://stackoverflow.com/a/43023257/12148752
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using static NativeMethods;

public class WindowPos : DependencyObject
{
    public static bool GetIsLocked(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsLockedProperty);
    }

    public static void SetIsLocked(DependencyObject obj, bool value)
    {
        obj.SetValue(IsLockedProperty, value);
    }

    public static readonly DependencyProperty IsLockedProperty =
        DependencyProperty.RegisterAttached("IsLocked", typeof(bool), typeof(WindowPos),
            new PropertyMetadata(false, IsLocked_Changed));

    private static void IsLocked_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var window = (Window)d;
        var isHooked = d.GetValue(IsHookedProperty) != null;

        if (!isHooked)
        {
            var hook = new WindowLockHook(window);
            d.SetValue(IsHookedProperty, hook);
        }
    }

    private static readonly DependencyProperty IsHookedProperty =
        DependencyProperty.RegisterAttached("IsHooked", typeof(WindowLockHook), typeof(WindowPos),
            new PropertyMetadata(null));

    private class WindowLockHook
    {
        private readonly Window Window;

        public WindowLockHook(Window window)
        {
            Window = window;

            var source = PresentationSource.FromVisual(window) as HwndSource;
            if (source == null)
            {
                // If there is no hWnd, we need to wait until there is
                window.SourceInitialized += Window_SourceInitialized;
            }
            else
            {
                source.AddHook(WndProc);
            }
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var source = (HwndSource)PresentationSource.FromVisual(Window);
            source.AddHook(WndProc);
        }

        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_WINDOWPOSCHANGING && GetIsLocked(Window))
            {
                var wp = Marshal.PtrToStructure<WINDOWPOS>(lParam);
                wp.flags |= SWP_NOMOVE | SWP_NOSIZE;
                Marshal.StructureToPtr(wp, lParam, false);
            }

            return IntPtr.Zero;
        }
    }
}

internal static class NativeMethods
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPOS
    {
        public IntPtr hwnd;
        public IntPtr hwndInsertAfter;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public int flags;
    }

    public const int
        SWP_NOMOVE = 0x0002,
        SWP_NOSIZE = 0x0001;

    public const int
        WM_WINDOWPOSCHANGING = 0x0046;
}