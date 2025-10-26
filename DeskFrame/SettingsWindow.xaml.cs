using System.Diagnostics;
using Wpf.Ui.Controls;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
namespace DeskFrame
{
    public partial class SettingsWindow : FluentWindow
    {
        private readonly InstanceController _controller;
        private DeskFrameWindow? _dWindows;
        private Instance? _instance;
        private readonly MainWindow _window;
        private bool _xamlLoaded;
        private ToggleSwitch? GetAutoUpdateToggleSwitch() => FindName("AutoUpdateToggleSwitch") as ToggleSwitch;
        private ToggleSwitch? GetDoubleClickToHideSwitch() => FindName("DoubleClickToHideSwitch") as ToggleSwitch;
        private ToggleSwitch? GetModifyDesktopEnvironmentSwitch() => FindName("ModifyDesktopEnvironmentSwitch") as ToggleSwitch;
        public SettingsWindow(InstanceController controller, MainWindow window)
        {
            ManualInitializeComponent();
            LocationChanged += Window_LocationChanged;
            this.MinHeight = 0;
            this.MinWidth = 200;
            _window = window;
            _controller = controller;
            // if (_controller.reg.KeyExistsRoot("blurBackground")) blurToggle.IsChecked = (bool)_controller.reg.ReadKeyValueRoot("blurBackground");
            var autoToggle = GetAutoUpdateToggleSwitch();
            if (_controller.reg.KeyExistsRoot("AutoUpdate") && autoToggle != null) autoToggle.IsChecked = (bool)_controller.reg.ReadKeyValueRoot("AutoUpdate");
            var dblToggle = GetDoubleClickToHideSwitch();
            if (_controller.reg.KeyExistsRoot("DoubleClickToHide") && dblToggle != null) dblToggle.IsChecked = (bool)_controller.reg.ReadKeyValueRoot("DoubleClickToHide");
            var modifyToggle = GetModifyDesktopEnvironmentSwitch();
            if (!_controller.reg.KeyExistsRoot("ModifyDesktopEnvironment"))
                _controller.reg.WriteToRegistryRoot("ModifyDesktopEnvironment", false);
            if (modifyToggle != null) modifyToggle.IsChecked = (bool)_controller.reg.ReadKeyValueRoot("ModifyDesktopEnvironment");
        }


        private void blurToggle_CheckChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            //   _controller.reg.WriteToRegistryRoot("blurBackground", blurToggle.IsChecked!);
            //   _controller.ChangeBlur((bool)blurToggle.IsChecked!);
        }

        private void ExportSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ExportRegistryKey(_controller.reg.regKeyName);
        }

        void ExportRegistryKey(string regKeyName)
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Registry Files (*.reg)|*.reg",
                Title = "Export Registry Key",
                FileName = $"DeskFrame_settings_{DateTime.Now.ToString("yyyy-MM-dd_hhmm")}"
            };
            if (saveDialog.ShowDialog() == true)
            {
                string fullKeyPath = $@"HKCU\SOFTWARE\{regKeyName}";
                string arguments = $"export \"{fullKeyPath}\" \"{saveDialog.FileName}\" /y";

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "reg.exe",
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                }.Start();
            }
        }

        private void AutoUpdateToggleSwitch_Click(object sender, RoutedEventArgs e)
        {
            var autoToggle = GetAutoUpdateToggleSwitch();
            if (autoToggle != null && (bool)autoToggle.IsChecked!)
            {

                _controller.reg.AddToAutoRun("DeskFrame", Process.GetCurrentProcess().MainModule!.FileName);
            }
            else
            {
                _controller.reg.RemoveFromAutoRun("DeskFrame");
            }
            if (autoToggle?.IsChecked != null)
            {
                _controller.reg.WriteToRegistryRoot("AutoUpdate", autoToggle.IsChecked!);
            }
        }

        private void DefaultFrameStyleButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_dWindows != null) _dWindows.Close();

            _instance = new Instance("Default Style", true);
            _instance.SettingDefault = true;
            _instance.Folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            _dWindows = new DeskFrameWindow(_instance);
            _dWindows.Left = this.Width + this.Left + 10;
            _dWindows.Top = this.Top;
            _dWindows.Show();

        }
        private void Window_LocationChanged(object? sender, EventArgs e)
        {
            if (_dWindows != null)
            {
                _dWindows.Left = this.Width + this.Left + 10;
                _dWindows.Top = this.Top;
            }
        }
        private void ResetDefaultFrameStyleButton_Click(object? sender, RoutedEventArgs e)
        {
            string[] keep = { "AutoUpdate", "blurBackground", "startOnLogin", "ModifyDesktopEnvironment" };
            RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\DeskFrame", writable: true)!;
            foreach (var name in key.GetValueNames())
            {
                if (Array.IndexOf(keep, name) == -1)
                {
                    try
                    {
                        key.DeleteValue(name);
                    }
                    catch
                    {
                    }
                }
            }
            key.Close();
        }

        private void FluentWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_dWindows != null)
            {
                _dWindows.Close();
            }
        }
        private void DoubleClickToHideSwitch_Click(object? sender, RoutedEventArgs e)
        {
            var dblToggle = GetDoubleClickToHideSwitch();
            if (dblToggle != null)
            {
                _controller.reg.WriteToRegistryRoot("DoubleClickToHide", dblToggle.IsChecked!);
                _window.DoubleClickToHide = (bool)dblToggle.IsChecked!;
            }
        }

        private void ModifyDesktopEnvironmentSwitch_Click(object? sender, RoutedEventArgs e)
        {
            var modifyToggle = GetModifyDesktopEnvironmentSwitch();
            if (modifyToggle != null && modifyToggle.IsChecked != null)
            {
                _controller.reg.WriteToRegistryRoot("ModifyDesktopEnvironment", modifyToggle.IsChecked!);
            }
        }

        private void ManageCategoriesButton_Click(object? sender, RoutedEventArgs e)
        {
            var win = new CategoryManagerWindow(_controller);
            win.Owner = this;
            win.Show();
        }
        private void ManualInitializeComponent()
        {
            if (_xamlLoaded) return;
            _xamlLoaded = true;
            var uri = new System.Uri("/DeskFrame;component/SettingsWindow.xaml", System.UriKind.Relative);
            System.Windows.Application.LoadComponent(this, uri);
        }

    }
}
