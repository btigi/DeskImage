using DeskImage.Util;
using Microsoft.Win32;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using Wpf.Ui.Controls;
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using DataFormats = System.Windows.DataFormats;
using DragEventArgs = System.Windows.DragEventArgs;
using MenuItem = Wpf.Ui.Controls.MenuItem;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace DeskImage
{
    public partial class DeskImageWindow : Window
    {
        public Instance Instance { get; set; }
        public string _path;
        private FileSystemWatcher _fileWatcher = new FileSystemWatcher();
        private bool _isMinimized = false;
        private int _snapDistance = 8;
        private bool _canAutoClose = true;
        private bool _isLocked = false;
        private bool _isOnEdge = false;
        public int neighborFrameCount = 0;
        public bool isMouseDown = false;
        DeskImageWindow _wOnLeft = null;
        DeskImageWindow _wOnRight = null;

        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (!(HwndSource.FromHwnd(hWnd).RootVisual is System.Windows.Window rootVisual))
                return IntPtr.Zero;

            if (msg == 0x0214) // WM_SIZING
            {
                Interop.RECT rect = (Interop.RECT)Marshal.PtrToStructure(lParam, typeof(Interop.RECT));
                double width = rect.Right - rect.Left;
                Instance.Width = width;
                
                double height = rect.Bottom - rect.Top;
                if (height <= 102)
                {
                    this.Height = 102;
                    rect.Bottom = rect.Top + 102;
                    Marshal.StructureToPtr(rect, lParam, true);
                    handled = true;
                    return (IntPtr)4;
                }
                ResizeBottomAnimation(height, rect, lParam);
            }

            if (msg == 70)
            {
                Interop.WINDOWPOS structure = Marshal.PtrToStructure<Interop.WINDOWPOS>(lParam);
                structure.flags |= 4U;
                Marshal.StructureToPtr<Interop.WINDOWPOS>(structure, lParam, false);
            }

            if (msg == 0x0003) // WM_MOVE
            {
                HandleWindowMove();

                if (_wOnLeft != null)
                {
                    _wOnLeft.HandleWindowMove();
                }
                if (_wOnRight != null)
                {
                    _wOnRight.HandleWindowMove();
                }
            }

            return IntPtr.Zero;
        }

        private void ResizeBottomAnimation(double targetBottom, Interop.RECT rect, IntPtr lParam)
        {
            if (!_canAnimate) 
                return;

            var animation = new DoubleAnimation
            {
                To = targetBottom,
                Duration = TimeSpan.FromMilliseconds(10),
                FillBehavior = FillBehavior.Stop
            };

            animation.Completed += (s, e) =>
            {
                _canAnimate = true;
                Marshal.StructureToPtr(rect, lParam, true);
            };
            _canAnimate = false;
            this.BeginAnimation(HeightProperty, animation);
        }

        public void HandleWindowMove()
        {
            Interop.RECT windowRect;
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            Interop.GetWindowRect(hwnd, out windowRect);

            int windowLeft = windowRect.Left;
            int windowTop = windowRect.Top;
            int windowRight = windowRect.Right;
            int windowBottom = windowRect.Bottom;

            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null) return;

            int newWindowLeft = windowLeft;
            int newWindowTop = windowTop;

            var workingArea = SystemParameters.WorkArea;

            if (Math.Abs(windowTop - workingArea.Top) <= _snapDistance)
            {
                newWindowTop = (int)workingArea.Top;
                WindowBackground.CornerRadius = new CornerRadius(0, 0, 5, 5);
                _isOnEdge = true;
            }
            else if (Math.Abs(windowBottom - workingArea.Bottom) <= _snapDistance)
            {
                newWindowTop = (int)(workingArea.Bottom - (windowBottom - windowTop));
            }
            else
            {
                _isOnEdge = false;
                WindowBackground.CornerRadius = new CornerRadius(5);
                titleBar.CornerRadius = new CornerRadius(5, 5, 0, 0);
            }
            neighborFrameCount = 0;

            bool onLeft = false;
            bool onRight = false;
            foreach (var otherWindow in MainWindow._controller._subWindows)
            {
                if (otherWindow == this) continue;

                IntPtr otherHwnd = new WindowInteropHelper(otherWindow).Handle;
                Interop.RECT otherWindowRect;
                Interop.GetWindowRect(otherHwnd, out otherWindowRect);

                int otherLeft = otherWindowRect.Left;
                int otherTop = otherWindowRect.Top;
                int otherRight = otherWindowRect.Right;
                int otherBottom = otherWindowRect.Bottom;

                if (Math.Abs(windowLeft - otherRight) <= _snapDistance && Math.Abs(windowTop - otherTop) <= _snapDistance)
                {
                    newWindowLeft = otherRight;
                    newWindowTop = otherTop;
                    _wOnLeft = otherWindow;
                    onLeft = true;
                    neighborFrameCount++;
                }
                else if (Math.Abs(windowRight - otherLeft) <= _snapDistance && Math.Abs(windowTop - otherTop) <= _snapDistance)
                {
                    newWindowLeft = otherLeft - (windowRight - windowLeft);
                    newWindowTop = otherTop;
                    _wOnRight = otherWindow;
                    onRight = true;
                    neighborFrameCount++;
                }


                if (Math.Abs(windowTop - otherBottom) <= _snapDistance && Math.Abs(windowLeft - otherLeft) <= _snapDistance)
                {
                    newWindowTop = otherBottom;
                }
                else if (Math.Abs(windowBottom - otherTop) <= _snapDistance && Math.Abs(windowLeft - otherLeft) <= _snapDistance)
                {
                    newWindowTop = otherTop - (windowBottom - windowTop);
                }
                if (neighborFrameCount == 2) break;
            }

            if (neighborFrameCount == 2)
            {
                WindowBackground.CornerRadius = new CornerRadius(0);
                titleBar.CornerRadius = new CornerRadius(0);
            }

            if (neighborFrameCount == 0)
            {
                if (_wOnLeft != null && !onLeft)
                {
                    if (!_wOnLeft._isMinimized)
                    {
                        _wOnLeft.WindowBorder.CornerRadius = new CornerRadius(
                            topLeft: _wOnLeft._isOnEdge ? 0 : _wOnLeft._wOnLeft == null ? 5 : 0,
                            topRight: _wOnLeft._isOnEdge ? 0 : 5,
                            bottomRight: 5,
                            bottomLeft: 5
                        );
                        _wOnLeft.titleBar.CornerRadius = new CornerRadius(
                            topLeft: _wOnLeft.WindowBorder.CornerRadius.TopLeft,
                            topRight: _wOnLeft.WindowBorder.CornerRadius.TopRight,
                            bottomRight: 0,
                            bottomLeft: 0
                        );
                    }
                    else
                    {
                        _wOnLeft.WindowBorder.CornerRadius = new CornerRadius(
                            topLeft: _wOnLeft._isOnEdge ? 0 : _wOnLeft._wOnLeft == null ? 5 : 0,
                            topRight: _wOnLeft._isOnEdge ? 0 : 5,
                            bottomRight: 5,
                            bottomLeft: _wOnLeft._wOnLeft == null ? 5 : 0
                        );
                        _wOnLeft.titleBar.CornerRadius = _wOnLeft.WindowBorder.CornerRadius;

                    }
                    _wOnLeft.WindowBackground.CornerRadius = _wOnLeft.WindowBorder.CornerRadius;
                    _wOnLeft._wOnRight = null;
                    _wOnLeft = null;
                }
                if (_wOnRight != null && !onRight)
                {
                    if (!_wOnRight._isMinimized)
                    {
                        _wOnRight.WindowBorder.CornerRadius = new CornerRadius(
                            topLeft: _wOnRight._isOnEdge ? 0 : 5,
                            topRight: _wOnRight._isOnEdge ? 0 : _wOnRight._wOnRight == null ? 5 : 0,
                            bottomRight: 5,
                            bottomLeft: 5
                        );
                        _wOnRight.titleBar.CornerRadius = new CornerRadius(
                            topLeft: _wOnRight.WindowBorder.CornerRadius.TopLeft,
                            topRight: _wOnRight.WindowBorder.CornerRadius.TopRight,
                            bottomRight: 0,
                            bottomLeft: 0
                        );
                    }
                    else
                    {
                        _wOnRight.WindowBorder.CornerRadius = new CornerRadius(
                        topLeft: _wOnRight._isOnEdge ? 0 : 5,
                        topRight: _wOnRight._isOnEdge ? 0 : _wOnRight._wOnRight == null ? 5 : 0,
                        bottomRight: _wOnRight._wOnRight == null ? 5 : 0,
                        bottomLeft: 5
                        );
                        _wOnRight.titleBar.CornerRadius = _wOnRight.WindowBorder.CornerRadius;

                    }
                    _wOnRight.WindowBackground.CornerRadius = _wOnRight.WindowBorder.CornerRadius;
                    _wOnRight._wOnLeft = null;
                    _wOnRight = null;
                }

            }

            if (!_isMinimized)
            {
                WindowBorder.CornerRadius = new CornerRadius(
                    topLeft: _isOnEdge ? 0 : _wOnLeft == null ? 5 : 0,
                    topRight: _isOnEdge ? 0 : _wOnRight == null ? 5 : 0,
                    bottomRight: 5,
                    bottomLeft: 5
                );
                WindowBackground.CornerRadius = WindowBorder.CornerRadius;
                titleBar.CornerRadius = new CornerRadius(
                    topLeft: WindowBorder.CornerRadius.TopLeft,
                    topRight: WindowBorder.CornerRadius.TopRight,
                    bottomRight: 0,
                    bottomLeft: 0
                );
            }
            else
            {
                WindowBorder.CornerRadius = new CornerRadius(
                    topLeft: _isOnEdge ? 0 : _wOnLeft == null ? 5 : 0,
                    topRight: _isOnEdge ? 0 : _wOnRight == null ? 5 : 0,
                    bottomRight: _wOnRight == null ? 5 : 0,
                    bottomLeft: _wOnLeft == null ? 5 : 0
                );
                WindowBackground.CornerRadius = WindowBorder.CornerRadius;
                titleBar.CornerRadius = WindowBorder.CornerRadius;
            }

            if (newWindowLeft != windowLeft || newWindowTop != windowTop)
            {
                Interop.SetWindowPos(hwnd, IntPtr.Zero, newWindowLeft, newWindowTop, 0, 0, Interop.SWP_NOREDRAW | Interop.SWP_NOACTIVATE | Interop.SWP_NOZORDER | Interop.SWP_NOSIZE);
            }
        }

        private void SetAsDesktopChild()
        {
            ArrayList windowHandles = new ArrayList();
            Interop.EnumedWindow callback = Interop.EnumWindowCallback;
            Interop.EnumWindows(callback, windowHandles);

            foreach (IntPtr windowHandle in windowHandles)
            {
                IntPtr progmanHandle = Interop.FindWindowEx(windowHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (progmanHandle != IntPtr.Zero)
                {
                    var interopHelper = new WindowInteropHelper(this);
                    interopHelper.EnsureHandle();
                    interopHelper.Owner = progmanHandle;
                    break;
                }
            }
        }

        public void SetAsToolWindow()
        {
            WindowInteropHelper wih = new WindowInteropHelper(this);
            IntPtr dwNew = new IntPtr(((long)Interop.GetWindowLong(wih.Handle, Interop.GWL_EXSTYLE).ToInt32() | 128L | 0x00200000L) & 4294705151L);
            Interop.SetWindowLong((nint)new HandleRef(this, wih.Handle), Interop.GWL_EXSTYLE, dwNew);
        }

        public void SetNoActivate()
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            IntPtr style = Interop.GetWindowLong(hwnd, Interop.GWL_EXSTYLE);
            IntPtr newStyle = new IntPtr(style.ToInt64() | Interop.WS_EX_NOACTIVATE);
            Interop.SetWindowLong(hwnd, Interop.GWL_EXSTYLE, newStyle);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = (int)Interop.GetWindowLong(hwnd, Interop.GWL_EXSTYLE);
            Interop.SetWindowLong(hwnd, Interop.GWL_EXSTYLE, exStyle | Interop.WS_EX_NOACTIVATE);
            KeepWindowBehind();
            SetAsDesktopChild();
            SetNoActivate();
            SetAsToolWindow();
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source.AddHook(WndProc);
        }
        public DeskImageWindow(Instance instance)
        {
            InitializeComponent();
            this.MinWidth = 98;
            this.Loaded += MainWindow_Loaded;
            this.SourceInitialized += MainWindow_SourceInitialized!;

            this.StateChanged += (sender, args) =>
            {
                this.WindowState = WindowState.Normal;
            };

            Instance = instance;
            this.Width = instance.Width;
            _path = instance.Folder;
            _isLocked = instance.IsLocked;
            this.Top = instance.PosY;
            this.Left = instance.PosX;

            title.FontSize = Instance.TitleFontSize;
            title.TextWrapping = TextWrapping.Wrap;

            double titleBarHeight = Math.Max(30, Instance.TitleFontSize * 1.5);
            titleBar.Height = titleBarHeight;

            titleBar.Cursor = _isLocked ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.SizeAll;
            if ((int)instance.Height <= 30) _isMinimized = true;
            if (instance.Minimized)
            {
                _isMinimized = instance.Minimized;
                this.Height = titleBarHeight;
            }
            else
            {
                this.Height = instance.Height;
            }

            if (instance.Folder == "empty")
            {
                //showFolder.Visibility = Visibility.Hidden;
                //addFolder.Visibility = Visibility.Visible;
            }
            else
            {
                if (File.Exists(instance.Folder))
                {
                    LoadImage(instance.Folder);
                }
                title.Text = Instance.TitleText ?? Instance.Name;

                DataContext = this;
                //InitializeFileWatcher();
            }
            titleBar.Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(Instance.TitleBarColor));
            title.Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(Instance.TitleTextColor));
            if (Instance.TitleFontFamily != null)
            {
                try
                {
                    title.FontFamily = new System.Windows.Media.FontFamily(Instance.TitleFontFamily);
                }
                catch
                {
                }
            }
            ChangeBackgroundOpacity(Instance.Opacity);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                KeepWindowBehind();
                if (!_isLocked)
                {
                    this.DragMove();
                }
                Debug.WriteLine("win left hide");
                return;
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged && !_isMinimized)
            {
                if (this.ActualHeight != 30)
                {
                    Instance.Height = this.ActualHeight;
                }
            }
        }

        private void AnimateChevron(bool flip, bool onLoad)
        {
            var rotateTransform = ChevronRotate;

            int angleToAnimateTo;
            int duration;
            if (onLoad)
            {
                angleToAnimateTo = flip ? 0 : 180;
                duration = 10;
            }
            else
            {
                angleToAnimateTo = (rotateTransform.Angle == 180) ? 0 : 180;
                duration = 200;
            }
            if (_isLocked) duration = 100;

            var rotateAnimation = new DoubleAnimation
            {
                From = rotateTransform.Angle,
                To = angleToAnimateTo,
                Duration = new Duration(TimeSpan.FromMilliseconds(duration)),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            _canAnimate = false;
            rotateAnimation.Completed += (s, e) => _canAnimate = true;

            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
        }

        bool _canAnimate = true;
        private void Minimize_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AnimateChevron(_isMinimized, false);

            if (!_isMinimized)
            {
                _isMinimized = true;
                Instance.Minimized = true;
                Debug.WriteLine("minimize: " + Instance.Height);
                AnimateWindowHeight(titleBar.Height);
            }
            else
            {
                WindowBackground.CornerRadius = new CornerRadius(
                         topLeft: WindowBackground.CornerRadius.TopLeft,
                         topRight: WindowBackground.CornerRadius.TopRight,
                         bottomRight: 5.0,
                         bottomLeft: 5.0
                      );
                _isMinimized = false;
                Instance.Minimized = false;

                Debug.WriteLine("unminimize: " + Instance.Height);
                AnimateWindowHeight(Instance.Height);
            }
            HandleWindowMove();
        }

        private void ToggleIsLocked() => Instance.IsLocked = !Instance.IsLocked;

        private void OpenFolder()
        {
            try
            {
                Process.Start(new ProcessStartInfo(_path) { UseShellExecute = true });
            }
            catch
            { }
        }
        private void AnimateWindowHeight(double targetHeight)
        {
            var animation = new DoubleAnimation
            {
                To = targetHeight,
                Duration = (_isLocked) ? TimeSpan.FromSeconds(0.1) : TimeSpan.FromSeconds(0.2),
                EasingFunction = new QuadraticEase()
            };
            animation.Completed += (s, e) =>
            {
                _canAnimate = true;
                WindowChrome.SetWindowChrome(this, Instance.IsLocked ?
                    new WindowChrome
                    {
                        ResizeBorderThickness = new Thickness(0),
                        CaptionHeight = 0
                    } :
                    new WindowChrome
                    {
                        GlassFrameThickness = new Thickness(5),
                        CaptionHeight = 0,
                        ResizeBorderThickness = new Thickness(5, 0, 5, Instance.Minimized ? 0 : 5),
                        CornerRadius = new CornerRadius(5)
                    }
                );
            };
            _canAnimate = false;
            this.BeginAnimation(HeightProperty, animation);
        }

        public void InitializeFileWatcher()
        {
            _fileWatcher = null;
            _fileWatcher = new FileSystemWatcher(_path)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };
            _fileWatcher.Created += OnFileChanged;
            _fileWatcher.Deleted += OnFileChanged;
            _fileWatcher.Renamed += OnFileRenamed;
            _fileWatcher.Changed += OnFileChanged;
        }
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Debug.WriteLine($"File changed: {e.ChangeType} - {e.FullPath}");
                LoadImage(_path);
            });
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Debug.WriteLine($"File renamed: {e.OldFullPath} to {e.FullPath}");
                /*
                var renamedItem = FileItems.First(item => item.Name == Path.GetFileName(e.OldFullPath));

                if (renamedItem != null)
                {
                    renamedItem.Name = Path.GetFileName(e.FullPath);
                }*/
            });
        }

        private void KeepWindowBehind()
        {
            IntPtr HWND_BOTTOM = new IntPtr(1);
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            Interop.SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, Interop.SWP_NOREDRAW | Interop.SWP_NOACTIVATE | Interop.SWP_NOMOVE | Interop.SWP_NOSIZE);
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    string file = files[0];
                    if (File.Exists(file))
                    {
                        string extension = Path.GetExtension(file).ToLower();
                        if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || 
                            extension == ".bmp" || extension == ".gif" || extension == ".tiff" || 
                            extension == ".ico" || extension == ".svg" || extension == ".webp")
                        {
                            _path = file;
                            title.Text = Path.GetFileName(_path);
                            Instance.Folder = file;
                            Instance.Name = Path.GetFileName(_path);
                            MainWindow._controller.WriteInstanceToKey(Instance);
                            LoadImage(_path);
                            DataContext = this;
                        }
                    }
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AnimateChevron(_isMinimized, true);
            KeepWindowBehind();
            RegistryHelper rgh = new RegistryHelper("DeskImage");
            bool toBlur = true;
            if (rgh.KeyExistsRoot("blurBackground"))
            {
                toBlur = (bool)rgh.ReadKeyValueRoot("blurBackground");
            }

            BackgroundType(toBlur);
        }

        public void ChangeBackgroundOpacity(int num)
        {
            try
            {
                var c = (Color)System.Windows.Media.ColorConverter.ConvertFromString(Instance.ListViewBackgroundColor);
                WindowBackground.Background = new SolidColorBrush(Color.FromArgb((byte)Instance.Opacity, c.R, c.G, c.B));
            }
            catch
            {

            }
        }

        public void BackgroundType(bool toBlur)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var accent = new Interop.AccentPolicy
            {
                AccentState = toBlur ? Interop.AccentState.ACCENT_ENABLE_BLURBEHIND :
                                       Interop.AccentState.ACCENT_DISABLED
            };

            var data = new Interop.WindowCompositionAttributeData
            {
                Attribute = Interop.WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = Marshal.SizeOf(accent),
                Data = Marshal.AllocHGlobal(Marshal.SizeOf(accent))
            };

            Marshal.StructureToPtr(accent, data.Data, false);
            Interop.SetWindowCompositionAttribute(hwnd, ref data);
            Marshal.FreeHGlobal(data.Data);
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            Instance.PosX = this.Left;
            Instance.PosY = this.Top;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            KeepWindowBehind();
            WindowChrome.SetWindowChrome(this, Instance.IsLocked ?
            new WindowChrome
            {
                ResizeBorderThickness = new Thickness(0),
                CaptionHeight = 0
            }
            : new WindowChrome
            {
                GlassFrameThickness = new Thickness(5),
                CaptionHeight = 0,
                ResizeBorderThickness = new Thickness(5, 0, 5, Instance.Minimized ? 0 : 5),
                CornerRadius = new CornerRadius(5)
            }
         );

        }
        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            KeepWindowBehind();
        }

        private void titleBar_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ContextMenu contextMenu = new ContextMenu();

            MenuItem frameSettings = new MenuItem
            {
                Header = "Frame Settings",
                Height = 34,
                Icon = new SymbolIcon(SymbolRegular.Settings20)
            };
            frameSettings.Click += (s, args) =>
            {
                var dialog = new FrameSettingsDialog(this);
                dialog.ShowDialog();
                if (dialog.DialogResult == true)
                {
                    LoadImage(_path);
                }
            };

            MenuItem reloadItems = new MenuItem
            {
                Header = "Reload",
                Height = 34,
                Icon = new SymbolIcon(SymbolRegular.ArrowSync20)
            };
            reloadItems.Click += (s, args) => { LoadImage(_path); };

            MenuItem lockFrame = new MenuItem
            {
                Header = Instance.IsLocked ? "Unlock frame" : "Lock frame",
                Height = 34,
                Icon = Instance.IsLocked ? new SymbolIcon(SymbolRegular.LockClosed20) : new SymbolIcon(SymbolRegular.LockOpen20)
            };
            lockFrame.Click += (s, args) =>
            {
                _isLocked = !_isLocked;
                ToggleIsLocked();
                HandleWindowMove();
                WindowChrome.SetWindowChrome(this, Instance.IsLocked ?
                       new WindowChrome
                       {
                           ResizeBorderThickness = new Thickness(0),
                           CaptionHeight = 0

                       }
                       : new WindowChrome
                       {
                           GlassFrameThickness = new Thickness(5),
                           CaptionHeight = 0,
                           ResizeBorderThickness = new Thickness(5, 0, 5, Instance.Minimized ? 0 : 5),
                           CornerRadius = new CornerRadius(5)
                       }
                 );

                titleBar.Cursor = _isLocked ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.SizeAll;
            };

            MenuItem exitItem = new MenuItem
            {
                Header = "Remove",
                Height = 34,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFC6060")),
                Icon = new SymbolIcon(SymbolRegular.Delete20)

            };

            exitItem.Click += async (s, args) =>
            {
                var dialog = new MessageBox
                {
                    Title = "Confirm",
                    Content = "Are you sure you want to remove this frame?",
                    PrimaryButtonText = "Yes",
                    CloseButtonText = "No"
                };

                var result = await dialog.ShowDialogAsync();

                if (result == MessageBoxResult.Primary)
                {
                    RegistryKey key = Registry.CurrentUser.OpenSubKey(Instance.GetKeyLocation(), true)!;
                    if (key != null)
                    {
                        Registry.CurrentUser.DeleteSubKeyTree(Instance.GetKeyLocation());
                    }
                    this.Close();

                }
            };

            MenuItem openInExplorerMenuItem = new MenuItem
            {
                Header = "Open image",
                Icon = new SymbolIcon { Symbol = SymbolRegular.FolderOpen20 }
            };

            openInExplorerMenuItem.Click += (_, _) => { OpenFolder(); };

            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(lockFrame);
            contextMenu.Items.Add(reloadItems);
            contextMenu.Items.Add(openInExplorerMenuItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(frameSettings);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(exitItem);

            contextMenu.IsOpen = true;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            KeepWindowBehind();
            Debug.WriteLine("Window_StateChanged hide");
        }

        public Task<string> BytesToStringAsync(long byteCount)
        {
            return Task.Run(() =>
            {
                double kilobytes = byteCount / 1024.0;
                string formattedKilobytes = kilobytes.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", " ");
                return formattedKilobytes + " KB";
            });
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Activate();
            _canAutoClose = true;
            if (_isOnEdge && _isMinimized)
            {
                if (!_canAnimate) return;
                Minimize_MouseLeftButtonDown(null, null);
            }
        }

        public void LoadImage(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                {
                    showImage.Visibility = Visibility.Hidden;
                    addImage.Visibility = Visibility.Visible;
                    return;
                }

                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(imagePath);
                image.EndInit();

                imageDisplay.Source = image;
                showImage.Visibility = Visibility.Visible;
                addImage.Visibility = Visibility.Hidden;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading image: {ex.Message}");
                showImage.Visibility = Visibility.Hidden;
                addImage.Visibility = Visibility.Visible;
            }
        }
    }
}