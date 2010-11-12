// Utility to automatically download radio programmes, using a plugin framework for provider specific implementation.
// Copyright © 2007-2010 Matt Robinson
//
// This program is free software; you can redistribute it and/or modify it under the terms of the GNU General
// Public License as published by the Free Software Foundation; either version 2 of the License, or (at your
// option) any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public
// License for more details.
//
// You should have received a copy of the GNU General Public License along with this program; if not, write
// to the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.

namespace RadioDld
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Text;
    using System.Windows.Forms;
    using Microsoft.VisualBasic.ApplicationServices;
    using Microsoft.Win32;

    internal class OsUtils
    {
        public static bool WinSevenOrLater()
        {
            OperatingSystem curOs = System.Environment.OSVersion;

            if (curOs.Platform == PlatformID.Win32NT && (((curOs.Version.Major == 6) && (curOs.Version.Minor >= 1)) || (curOs.Version.Major > 6)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool WinVistaOrLater()
        {
            OperatingSystem curOs = System.Environment.OSVersion;

            if (curOs.Platform == PlatformID.Win32NT && (((curOs.Version.Major == 6) && (curOs.Version.Minor >= 0)) || (curOs.Version.Major > 6)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool WinXpOrLater()
        {
            OperatingSystem curOs = System.Environment.OSVersion;

            if (curOs.Platform == PlatformID.Win32NT && (((curOs.Version.Major == 5) && (curOs.Version.Minor >= 1)) || (curOs.Version.Major > 5)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void TrayAnimate(Form form, bool down)
        {
            StringBuilder className = new StringBuilder(255);
            IntPtr taskbarHwnd = default(IntPtr);
            IntPtr trayHwnd = default(IntPtr);

            // Get taskbar handle
            taskbarHwnd = NativeMethods.FindWindow("Shell_traywnd", null);

            if (taskbarHwnd == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            // Get system tray handle
            trayHwnd = NativeMethods.GetWindow(taskbarHwnd, NativeMethods.GW_CHILD);

            do
            {
                if (trayHwnd == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }

                if (NativeMethods.GetClassName(trayHwnd, className, className.Capacity) == 0)
                {
                    throw new Win32Exception();
                }

                if (className.ToString() == "TrayNotifyWnd")
                {
                    break;
                }

                trayHwnd = NativeMethods.GetWindow(trayHwnd, NativeMethods.GW_HWNDNEXT);
            }
            while (true);

            NativeMethods.RECT systray = default(NativeMethods.RECT);
            NativeMethods.RECT window = default(NativeMethods.RECT);

            // Fetch the location of the systray from its window handle
            if (NativeMethods.GetWindowRect(trayHwnd, ref systray) == false)
            {
                throw new Win32Exception();
            }

            // Fetch the location of the window from its window handle
            if (NativeMethods.GetWindowRect(form.Handle, ref window) == false)
            {
                throw new Win32Exception();
            }

            // Perform the animation
            if (down == true)
            {
                if (NativeMethods.DrawAnimatedRects(form.Handle, NativeMethods.IDANI_CAPTION, ref window, ref systray) == false)
                {
                    throw new Win32Exception();
                }
            }
            else
            {
                if (NativeMethods.DrawAnimatedRects(form.Handle, NativeMethods.IDANI_CAPTION, ref systray, ref window) == false)
                {
                    throw new Win32Exception();
                }
            }
        }

        public static bool CompositionEnabled()
        {
            if (!WinVistaOrLater())
            {
                return false;
            }

            bool enabled = false;

            if (NativeMethods.DwmIsCompositionEnabled(ref enabled) != 0)
            {
                throw new Win32Exception();
            }

            return enabled;
        }

        public static void ApplyRunOnStartup()
        {
            RegistryKey runKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (Properties.Settings.Default.RunOnStartup)
            {
                runKey.SetValue(new ApplicationBase().Info.Title, "\"" + Application.ExecutablePath + "\" /hidemainwindow");
            }
            else
            {
                if (runKey.GetValue(new ApplicationBase().Info.Title) != null)
                {
                    runKey.DeleteValue(new ApplicationBase().Info.Title);
                }
            }
        }

        public static bool VisibleOnScreen(Rectangle location)
        {
            foreach (Screen thisScreen in Screen.AllScreens)
            {
                if (thisScreen.WorkingArea.IntersectsWith(location))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
