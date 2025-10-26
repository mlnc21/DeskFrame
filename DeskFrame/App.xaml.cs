using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Application = System.Windows.Application;
using DeskFrame.Util; // DesktopIconManager für Icon-Hide/Show

namespace DeskFrame
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
    private DispatcherTimer updateTimer = new();
        public RegistryHelper reg = new RegistryHelper("DeskFrame");
        // Merkt, ob wir den Desktop-Icon-Zustand verändert haben
        private bool _desktopIconsOriginallyHidden = false;
        private bool _desktopIconsChangedByApp = false;
        protected override void OnStartup(StartupEventArgs e)
        {
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Critical;
            base.OnStartup(e);
            ToastNotificationManagerCompat.OnActivated += ToastActivatedHandler;
            StartUpdateCheckTimer();

            // Desktop-Symbole automatisch ausblenden, falls aktuell sichtbar
            try
            {
                _desktopIconsOriginallyHidden = DesktopIconManager.AreDesktopIconsHidden();
                if (!_desktopIconsOriginallyHidden)
                {
                    // Sichtbar -> wir blenden aus (ohne Explorer-Neustart, schneller Start)
                    if (DesktopIconManager.HideDesktopIcons(false))
                    {
                        _desktopIconsChangedByApp = true;
                        Debug.WriteLine("Desktop icons hidden by App startup.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"HideDesktopIcons OnStartup failed: {ex.Message}");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // Desktop-Items wiederherstellen falls versteckt
                DeskFrame.MainWindow._controller?.RestoreDesktopItems();

                // Desktop-Symbole wiederherstellen, falls wir sie verändert haben und sie ursprünglich sichtbar waren
                if (_desktopIconsChangedByApp && !_desktopIconsOriginallyHidden)
                {
                    if (DesktopIconManager.ShowDesktopIcons(false))
                    {
                        Debug.WriteLine("Desktop icons restored by App exit.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RestoreDesktopItems OnExit failed: {ex.Message}");
            }
            base.OnExit(e);
        }

        private void ToastActivatedHandler(ToastNotificationActivatedEventArgsCompat toastArgs)
        {
            var args = ToastArguments.Parse(toastArgs.Argument);
            Current.Dispatcher.Invoke(async () =>
            {
                if (args.Contains("action") && args["action"] == "install_update")
                {
                    await Updater.InstallUpdate();
                }

            });
        }

        private void StartUpdateCheckTimer()
        {
            updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromHours(6)
            };
            updateTimer.Tick += async (_, _) =>
            {
                var autoVal = reg.ReadKeyValueRoot("AutoUpdate");
                if (reg.KeyExistsRoot("AutoUpdate") && autoVal is bool auto && auto)
                {
                    await Updater.CheckUpdateAsync("https://api.github.com/repos/PinchToDebug/DeskFrame/releases/latest", true);
                }
            };
            updateTimer.Start();
        }
    }

}
