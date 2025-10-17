using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace DeskFrame.Util
{
    /// <summary>
    /// Hilfsmethoden zum Ein- und Ausblenden aller Desktop-Symbole.
    /// Nutzt den Registry-Wert HideIcons (0=anzeigen, 1=ausblenden).
    /// Hinweis: Offiziell wird der Zustand erst nach Ab-/Anmeldung oder Explorer-Neustart garantiert angezeigt.
    /// Wir versuchen zusätzlich ein sofortiges Refresh über Nachrichten und optionales Fenster-Hiden.
    /// </summary>
    public static class DesktopIconManager
    {
        private const string ExplorerAdvancedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
        private const string HideIconsValueName = "HideIcons";

        // Win32 Interop
        private const int WM_SETTINGCHANGE = 0x1A;
        private const int SMTO_ABORTIFHUNG = 0x0002;
        private const int WM_COMMAND = 0x0111;
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        // Interner Explorer-Befehl zum Toggle der Desktop Icons (0x7402) – nur als Fallback nutzbar (toggelnd, nicht zustands-bezogen)
        private const int TOGGLE_DESKTOP_ICONS_COMMAND = 0x7402;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam, int fuFlags, int uTimeout, out IntPtr lpdwResult);
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        /// <summary>
        /// Blendet Desktop-Symbole aus. Optional mit Explorer-Neustart für garantiertes Update.
        /// </summary>
        public static bool HideDesktopIcons(bool restartExplorer = false)
        {
            try
            {
                SetRegistryHideIcons(true);
                BroadcastSettingChange();

                // Versuche sofortiges Ausblenden der ListView-Shell-Views ohne Neustart.
                TryHideShellViews();

                if (restartExplorer)
                {
                    RestartExplorer();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Zeigt Desktop-Symbole an. Optional mit Explorer-Neustart für garantiertes Update.
        /// </summary>
        public static bool ShowDesktopIcons(bool restartExplorer = false)
        {
            try
            {
                SetRegistryHideIcons(false);
                BroadcastSettingChange();

                // Versuche sofortiges Anzeigen der Shell-Views.
                TryShowShellViews();

                if (restartExplorer)
                {
                    RestartExplorer();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Liest aktuellen Wert: true = ausgeblendet, false = sichtbar.
        /// </summary>
        public static bool AreDesktopIconsHidden()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(ExplorerAdvancedPath, false);
                var val = key?.GetValue(HideIconsValueName) as int?;
                return val == 1;
            }
            catch
            {
                return false;
            }
        }

        private static void SetRegistryHideIcons(bool hide)
        {
            using var key = Registry.CurrentUser.CreateSubKey(ExplorerAdvancedPath);
            key.SetValue(HideIconsValueName, hide ? 1 : 0, RegistryValueKind.DWord);
        }

        private static void BroadcastSettingChange()
        {
            IntPtr result;
            SendMessageTimeout(new IntPtr(0xffff), WM_SETTINGCHANGE, IntPtr.Zero,
                Marshal.StringToHGlobalUni("Environment"), SMTO_ABORTIFHUNG, 2000, out result);
        }

        private static void RestartExplorer()
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("explorer"))
                {
                    try { p.Kill(); p.WaitForExit(5000); } catch { }
                }
                Process.Start("explorer.exe");
            }
            catch { }
        }

        private static void TryHideShellViews()
        {
            EnumWindows((hWnd, _) =>
            {
                var cls = GetClassNameString(hWnd);
                if (cls == "Progman" || cls == "WorkerW")
                {
                    EnumChildWindows(hWnd, (child, _) =>
                    {
                        var ccls = GetClassNameString(child);
                        if (ccls == "SHELLDLL_DefView")
                        {
                            EnumChildWindows(child, (lv, _) =>
                            {
                                var lcls = GetClassNameString(lv);
                                if (lcls == "SysListView32")
                                {
                                    ShowWindow(lv, SW_HIDE);
                                }
                                return true;
                            }, IntPtr.Zero);
                        }
                        return true;
                    }, IntPtr.Zero);
                }
                return true;
            }, IntPtr.Zero);
        }

        private static void TryShowShellViews()
        {
            EnumWindows((hWnd, _) =>
            {
                var cls = GetClassNameString(hWnd);
                if (cls == "Progman" || cls == "WorkerW")
                {
                    EnumChildWindows(hWnd, (child, _) =>
                    {
                        var ccls = GetClassNameString(child);
                        if (ccls == "SHELLDLL_DefView")
                        {
                            EnumChildWindows(child, (lv, _) =>
                            {
                                var lcls = GetClassNameString(lv);
                                if (lcls == "SysListView32")
                                {
                                    ShowWindow(lv, SW_SHOW);
                                }
                                return true;
                            }, IntPtr.Zero);
                        }
                        return true;
                    }, IntPtr.Zero);
                }
                return true;
            }, IntPtr.Zero);
        }

        /// <summary>
        /// Not-Toggle (falls kein Neustart gewünscht und direkte ShellView-Manipulation nicht greift): sendet den internen Toggle-Befehl.
        /// Kann den Zustand umkehren, daher nur benutzen wenn aktueller Zustand bekannt und wir danach Registry-Wert korrigieren.
        /// </summary>
        public static void SendToggleCommand()
        {
            // Broadcast an alle Top-Level Fenster (HWND_BROADCAST = 0xffff)
            IntPtr result;
            SendMessageTimeout(new IntPtr(0xffff), WM_COMMAND, new IntPtr(TOGGLE_DESKTOP_ICONS_COMMAND), IntPtr.Zero,
                SMTO_ABORTIFHUNG, 2000, out result);
        }

        private static string GetClassNameString(IntPtr hWnd)
        {
            var sb = new System.Text.StringBuilder(256);
            GetClassName(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }
    }
}
