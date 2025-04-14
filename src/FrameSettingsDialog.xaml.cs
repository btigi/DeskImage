using System.Collections.ObjectModel;
using System.Drawing.Text;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Controls;
using Color = System.Windows.Media.Color;

namespace DeskImage
{
    public partial class FrameSettingsDialog : FluentWindow
    {
        private DeskImageWindow _frame;
        private Instance _instance;
        private Instance _originalInstance;
        private bool _isValidTitleBarColor = false;
        private bool _isValidTitleTextColor = false;
        private bool _isValidTitleTextAlignment = true;
        private bool _isValidBorderColor = false;
        private bool _isValidListViewBackgroundColor = true;
        private bool _isReverting = false;
        string _lastInstanceName;
        public ObservableCollection<string> FontList;

        public FrameSettingsDialog(DeskImageWindow frame)
        {
            InitializeComponent();
            DataContext = this;
            _originalInstance = new Instance(frame.Instance);
            _lastInstanceName = _originalInstance.Name;
            _instance = frame.Instance;
            _frame = frame;
            TitleBarColorTextBox.Text = _instance.TitleBarColor;
            TitleTextColorTextBox.Text = _instance.TitleTextColor;
            ListViewBackgroundColorTextBox.Text = _instance.ListViewBackgroundColor;
            BorderColorTextBox.Text = _instance.BorderColor;
            BorderEnabledCheckBox.IsChecked = _instance.BorderEnabled;
            TitleTextBox.Text = _instance.TitleText ?? _instance.Name;
            TitleFontSizeNumberBox.Value = _instance.TitleFontSize;
            _originalInstance.TitleText = TitleTextBox.Text;
            TitleTextAlignmentComboBox.SelectedIndex = (int)_instance.TitleTextAlignment;

            _frame.title.FontSize = _instance.TitleFontSize;
            _frame.title.TextWrapping = TextWrapping.Wrap;

            double titleBarHeight = Math.Max(30, _instance.TitleFontSize * 1.5);
            _frame.titleBar.Height = titleBarHeight;

            TitleFontSizeNumberBox.ValueChanged += (sender, args) =>
            {
                if (args.NewValue.HasValue)
                {
                    _instance.TitleFontSize = args.NewValue.Value;
                    _frame.title.FontSize = args.NewValue.Value;
                    _frame.title.TextWrapping = TextWrapping.Wrap;

                    double titleBarHeight = Math.Max(30, args.NewValue.Value * 1.5);
                    _frame.titleBar.Height = titleBarHeight;
                }
            };

            UpdateBorderColorEnabled();
            ValidateSettings();

            FontList = new ObservableCollection<string>();
            InstalledFontCollection fonts = new InstalledFontCollection();
            foreach (System.Drawing.FontFamily font in fonts.Families)
            {
                FontList.Add(font.Name);
            }

            TitleTextAutoSuggestionBox.OriginalItemsSource = FontList;
            TitleTextAutoSuggestionBox.TextChanged += (sender, args) =>
            {
                _frame.title.FontFamily = new System.Windows.Media.FontFamily(TitleTextAutoSuggestionBox.Text);
                _instance.TitleFontFamily = TitleTextAutoSuggestionBox.Text;
            };
        }

        private void TextChangedHandler(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ValidateSettings();
        }
        private void TitleTextAlignmentComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ValidateSettings();
        }

        private void BorderEnabledCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateBorderColorEnabled();
            ValidateSettings();
        }

        private void UpdateBorderColorEnabled() => BorderColorTextBox.IsEnabled = BorderEnabledCheckBox.IsChecked == true;

        private void ValidateSettings()
        {
            if (_isReverting) return;

            _isValidTitleBarColor = TryParseColor(string.IsNullOrEmpty(TitleBarColorTextBox.Text) ? "#0C000000" : TitleBarColorTextBox.Text);
            _isValidTitleTextColor = TryParseColor(string.IsNullOrEmpty(TitleTextColorTextBox.Text) ? "#FFFFFF" : TitleTextColorTextBox.Text);
            _isValidBorderColor = BorderEnabledCheckBox.IsChecked == true ? TryParseColor(BorderColorTextBox.Text) : true;

            _isValidTitleTextAlignment = TitleTextAlignmentComboBox.SelectedIndex >= 0;
            _isValidListViewBackgroundColor = TryParseColor(string.IsNullOrEmpty(ListViewBackgroundColorTextBox.Text) ? "#0C000000" : ListViewBackgroundColorTextBox.Text);

            if (_isValidTitleBarColor && _isValidTitleTextColor && _isValidTitleTextAlignment && _isValidBorderColor && _isValidListViewBackgroundColor)
            {
                _instance.TitleBarColor = string.IsNullOrEmpty(TitleBarColorTextBox.Text) ? "#0C000000" : TitleBarColorTextBox.Text;
                _instance.TitleTextColor = string.IsNullOrEmpty(TitleTextColorTextBox.Text) ? "#FFFFFF" : TitleTextColorTextBox.Text;

                _instance.BorderColor = BorderColorTextBox.Text;
                _instance.BorderEnabled = BorderEnabledCheckBox.IsChecked == true;
                _instance.TitleTextAlignment = (System.Windows.HorizontalAlignment)TitleTextAlignmentComboBox.SelectedIndex;
                _instance.TitleText = TitleTextBox.Text;

                _instance.ListViewBackgroundColor = string.IsNullOrEmpty(ListViewBackgroundColorTextBox.Text) ? "#0C000000" : ListViewBackgroundColorTextBox.Text;
                _instance.Opacity = ((Color)System.Windows.Media.ColorConverter.ConvertFromString(_instance.ListViewBackgroundColor)).A;
                _instance.TitleFontSize = TitleFontSizeNumberBox.Value ?? 12;

                _frame.titleBar.Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(_instance.TitleBarColor));
                _frame.title.Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(_instance.TitleTextColor));
                _frame.title.Text = TitleTextBox.Text ?? _frame.Instance.Name;
                _frame.WindowBackground.Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(_instance.ListViewBackgroundColor)); ;
            }
        }

        private bool TryParseColor(string colorText)
        {
            try
            {
                new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(colorText));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async void RevertButton_Click(object sender, RoutedEventArgs e)
        {

            var dialog = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Confirm",
                Content = "Are you sure you want to revert it?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No"
            };

            var result = await dialog.ShowDialogAsync();

            if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
            {
                _isReverting = true;
                _instance.TitleBarColor = _originalInstance.TitleBarColor;
                _instance.TitleTextColor = _originalInstance.TitleTextColor;
                _instance.BorderColor = _originalInstance.BorderColor;
                _instance.BorderEnabled = _originalInstance.BorderEnabled;
                _instance.TitleText = _originalInstance.TitleText ?? _originalInstance.Name;
                _instance.TitleTextAlignment = _originalInstance.TitleTextAlignment;
                _instance.ListViewBackgroundColor = _originalInstance.ListViewBackgroundColor;
                _instance.Opacity = _originalInstance.Opacity;
                _instance.TitleFontSize = _originalInstance.TitleFontSize;
                if (_originalInstance.Folder != _instance.Folder)
                {
                    _instance.Folder = _originalInstance.Folder;
                    _frame._path = _originalInstance.Folder;
                    string name = _instance.Name;

                    _frame.title.Text = Path.GetFileName(_frame._path);
                    _instance.Name = Path.GetFileName(_originalInstance.Name);

                    MainWindow._controller.WriteOverInstanceToKey(_instance, name);
                    _frame.LoadImage(_frame._path);
                    DataContext = this;
                    _frame.InitializeFileWatcher();

                }
                _instance.Folder = _originalInstance.Folder;
                _instance.Name = _originalInstance.Name;
                _instance.TitleText = _originalInstance.TitleText;

                TitleBarColorTextBox.Text = _instance.TitleBarColor;
                TitleTextColorTextBox.Text = _instance.TitleTextColor;
                BorderColorTextBox.Text = _instance.BorderColor;
                BorderEnabledCheckBox.IsChecked = _instance.BorderEnabled;
                TitleTextBox.Text = _instance.TitleText ?? _instance.Name;

                TitleTextAlignmentComboBox.SelectedIndex = (int)_instance.TitleTextAlignment;
                ListViewBackgroundColorTextBox.Text = _instance.ListViewBackgroundColor;
                TitleFontSizeNumberBox.Value = _instance.TitleFontSize;

                UpdateBorderColorEnabled();
                _isReverting = false;
                ValidateSettings();
            }
        }
        private void OpenColorPicker(System.Windows.Controls.TextBox textbox)
        {
            ColorCard.Children.Clear();
            var colorPicker = new ColorPicker.ColorPicker(textbox);
            ColorCard.Children.Add(colorPicker);
            uiFlyout.IsOpen = true;
        }

        private void BorderColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (BorderEnabledCheckBox.IsChecked == false) return;
            OpenColorPicker(BorderColorTextBox);
        }

        private void FilesBackgroundColorButton_Click(object sender, RoutedEventArgs e)
        {
            ColorCard.Children.Clear();
            OpenColorPicker(ListViewBackgroundColorTextBox);
        }

        private void TitleTextColorButton_Click(object sender, RoutedEventArgs e)
        {
            OpenColorPicker(TitleTextColorTextBox);
        }

        private void TitleBarColorButton_Click(object sender, RoutedEventArgs e)
        {
            OpenColorPicker(TitleBarColorTextBox);
        }

        private void Titlebar_CloseClicked(TitleBar sender, RoutedEventArgs args)
        {
            this.DialogResult = true;
        }

        private void ChangeImageButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff;*.ico;*.svg;*.webp",
                Title = "Select an image file"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _instance.Folder = openFileDialog.FileName;
                _frame._path = _instance.Folder;
                _frame.title.Text = Path.GetFileName(_frame._path);
                _instance.Name = Path.GetFileName(openFileDialog.FileName);
                MainWindow._controller.WriteOverInstanceToKey(_instance, _lastInstanceName);
                _lastInstanceName = _instance.Name;
                _frame.LoadImage(_frame._path);
                DataContext = this;
            }
        }
    }
}