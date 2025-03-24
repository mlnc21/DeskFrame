using Wpf.Ui.Controls;

namespace DeskFrame
{
    public partial class SettingsWindow : FluentWindow
    {
        InstanceController _controller;

        public SettingsWindow(InstanceController controller)
        {
            InitializeComponent();

            this.MinHeight = 0;
            this.MinWidth = 200;

            _controller = controller;
            blurToggle.IsChecked = true;
            if (_controller.reg.KeyExistsRoot("isBlack")) bgToggle.IsChecked = (bool)_controller.reg.ReadKeyValueRoot("isBlack");
            if (_controller.reg.KeyExistsRoot("blurBackground")) blurToggle.IsChecked = (bool)_controller.reg.ReadKeyValueRoot("blurBackground");
            if (_controller.reg.KeyExistsRoot("Opacity")) opacitySlider.Value = Double.Parse(_controller.reg.ReadKeyValueRoot("Opacity").ToString()!);
            else
            {
                opacitySlider.Value = 26;
            }
        }
        private void bgToggle_CheckChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            _controller.reg.WriteToRegistryRoot("isBlack", bgToggle.IsChecked!);

            _controller.ChangeIsBlack((bool)bgToggle.IsChecked!);
            _controller.ChangeBackgroundOpacity((int)opacitySlider.Value);

            bgToggle.Content = (bool)bgToggle.IsChecked ? "Black" : "White";
        }

        private void blurToggle_CheckChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            _controller.reg.WriteToRegistryRoot("blurBackground", blurToggle.IsChecked!);
            _controller.ChangeBlur((bool)blurToggle.IsChecked!);
        }

        private void opacitySlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (_controller != null)
            {
                _controller.reg.WriteToRegistryRoot("Opacity", (uint)opacitySlider.Value);
                _controller.ChangeBackgroundOpacity((int)opacitySlider.Value);
            }

        }
    }
}
