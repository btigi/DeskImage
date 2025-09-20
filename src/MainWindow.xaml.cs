﻿using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using Application = System.Windows.Application;
using static DeskImage.Util.Interop;

namespace DeskImage
{
    public partial class MainWindow : Window
    {
        private string url = "https://api.github.com/repos/btigi/DeskImage/releases/latest";
        bool startOnLogin;
        public static InstanceController _controller;
        public MainWindow()
        {
            InitializeComponent();

            versionHeader.Header += " " + Process.GetCurrentProcess().MainModule!.FileVersionInfo.FileVersion!.ToString();
            _controller = new InstanceController();
            _controller.InitInstances();
            if (_controller.reg.KeyExistsRoot("startOnLogin")) startOnLogin = (bool)_controller.reg.ReadKeyValueRoot("startOnLogin");
            AutorunToggle.IsChecked = startOnLogin;
            if (_controller.reg.KeyExistsRoot("blurBackground")) BlurToggle.IsChecked = (bool)_controller.reg.ReadKeyValueRoot("blurBackground");
            if (_controller.reg.KeyExistsRoot("AutoUpdate") && (bool)_controller.reg.ReadKeyValueRoot("AutoUpdate"))
            {
                Update();
                Debug.WriteLine("Auto update checking for update");
            }
            else
            {
                _controller.reg.WriteToRegistryRoot("AutoUpdate", "True");
            }
        }

        private async void Update()
        {
            await Updater.CheckUpdateAsync(url,false);
        }

        private void addDesktopFrame_Click(object sender, RoutedEventArgs e)
        {
            _controller.AddInstance();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            this.ShowInTaskbar = false;
            this.Width = 0;
            this.Height = 0;
            this.ResizeMode = ResizeMode.NoResize;
            this.WindowStyle = WindowStyle.None;
            this.Visibility = Visibility.Collapsed;
            this.Left = -500;
            this.Top = -500;

            CloseHide();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = (int)GetWindowLong(hwnd, GWL_EXSTYLE);
            exStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            SetWindowLong(hwnd, GWL_EXSTYLE, (IntPtr)exStyle);
        }

        private void CloseHide()
        {
            Task.Run(() =>
            {
                Thread.Sleep(100);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.Close();
                });
            });
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            e.Cancel = true;
            this.Hide();
        }

        private async void Update_Button_Click(object sender, RoutedEventArgs e)
        {
            await Updater.CheckUpdateAsync(url,true);

        }

        private void BlurToggle_CheckChanged(object sender, RoutedEventArgs e)
        {
            _controller.reg.WriteToRegistryRoot("blurBackground", BlurToggle.IsChecked!);
            _controller.ChangeBlur((bool)BlurToggle.IsChecked!);
        }

        private void AutorunToggle_CheckChanged(object sender, RoutedEventArgs e)
        {
            if ((bool)AutorunToggle.IsChecked!)
            {

                _controller.reg.AddToAutoRun("DeskImage", Process.GetCurrentProcess().MainModule!.FileName);
            }
            else
            {
                _controller.reg.RemoveFromAutoRun("DeskImage");
            }
            _controller.reg.WriteToRegistryRoot("startOnLogin", AutorunToggle.IsChecked);
        }

        private void visitGithub_Buton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProcessStartInfo sInfo = new ProcessStartInfo($"https://github.com/btigi/DeskImage") { UseShellExecute = true };
                _ = Process.Start(sInfo);
            }
            catch
            {
            }
        }

        private void ExitApp(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Settings_Button_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow(_controller).Show();
        }
    }
}