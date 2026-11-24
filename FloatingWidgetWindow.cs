using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace DeskCountdown
{
    public class FloatingWidgetWindow : Window
    {
        public CountdownItem Item { get; private set; }
        public Action<CountdownItem> OnEditRequested;
        public Action<CountdownItem> OnChanged;
        public Action OnOpenMainRequested;

        private Border _rootBorder;
        private TextBlock _categoryText;
        private TextBlock _lockBadge;
        private TextBlock _titleText;
        private TextBlock _prefixText;
        private TextBlock _valueText;
        private TextBlock _subText;
        private Border _progressTrack;
        private Border _progressFill;
        private Thumb _resizeThumb;
        private HwndSource _hwndSource;
        private bool _bottomHookAttached;

        public FloatingWidgetWindow(CountdownItem item)
        {
            Item = item;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            MinWidth = 220;
            MinHeight = 112;

            Width = item.WidgetWidth >= MinWidth ? item.WidgetWidth : 300;
            Height = item.WidgetHeight >= MinHeight ? item.WidgetHeight : 148;
            Left = item.WidgetLeft;
            Top = item.WidgetTop;

            BuildVisualTree();
            BuildContextMenu();
            ApplyAppearance();
            UpdateTimeDisplay(DateTime.Now);

            SourceInitialized += FloatingWidgetWindow_SourceInitialized;
            MouseLeftButtonDown += FloatingWidgetWindow_MouseLeftButtonDown;
            LocationChanged += FloatingWidgetWindow_LocationChanged;
            Closed += FloatingWidgetWindow_Closed;
        }

        private void BuildVisualTree()
        {
            Grid outer = new Grid();
            outer.Margin = new Thickness(8);

            _rootBorder = new Border();
            _rootBorder.Padding = new Thickness(16, 12, 16, 12);
            _rootBorder.Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 14,
                ShadowDepth = 3,
                Opacity = 0.35
            };

            Grid layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Top bar: category + lock indicator
            DockPanel header = new DockPanel();
            header.LastChildFill = false;

            _categoryText = new TextBlock
            {
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Opacity = 0.85,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(_categoryText, Dock.Left);
            header.Children.Add(_categoryText);

            _lockBadge = new TextBlock
            {
                Text = "🔒",
                FontSize = 11,
                Opacity = 0.8,
                Visibility = Visibility.Collapsed,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(_lockBadge, Dock.Right);
            header.Children.Add(_lockBadge);

            Grid.SetRow(header, 0);
            layout.Children.Add(header);

            // Title + Prefix
            StackPanel titleRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 4, 0, 2)
            };
            _titleText = new TextBlock
            {
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            _prefixText = new TextBlock
            {
                FontSize = 11,
                Opacity = 0.72,
                Margin = new Thickness(8, 0, 0, 1),
                VerticalAlignment = VerticalAlignment.Bottom
            };
            titleRow.Children.Add(_titleText);
            titleRow.Children.Add(_prefixText);
            Grid.SetRow(titleRow, 1);
            layout.Children.Add(titleRow);

            // Big countdown value
            Viewbox vb = new Viewbox
            {
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.DownOnly,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 2)
            };
            _valueText = new TextBlock
            {
                FontFamily = new FontFamily("Segoe UI, Microsoft YaHei UI"),
                FontWeight = FontWeights.Bold
            };
            vb.Child = _valueText;
            Grid.SetRow(vb, 2);
            layout.Children.Add(vb);

            // Subtext
            _subText = new TextBlock
            {
                FontSize = 11,
                Opacity = 0.75,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetRow(_subText, 3);
            layout.Children.Add(_subText);

            // Progress bar
            _progressTrack = new Border
            {
                Height = 5,
                CornerRadius = new CornerRadius(2.5),
                Margin = new Thickness(0, 6, 0, 0),
                Background = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _progressFill = new Border
            {
                Height = 5,
                CornerRadius = new CornerRadius(2.5),
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 0
            };
            _progressTrack.Child = _progressFill;
            Grid.SetRow(_progressTrack, 4);
            layout.Children.Add(_progressTrack);

            // Resize thumb at bottom-right
            _resizeThumb = new Thumb
            {
                Width = 14,
                Height = 14,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, -10, -8),
                Cursor = Cursors.SizeNWSE,
                Opacity = 0.01
            };
            _resizeThumb.DragDelta += ResizeThumb_DragDelta;
            _resizeThumb.DragCompleted += ResizeThumb_DragCompleted;
            Grid.SetRowSpan(_resizeThumb, 5);
            layout.Children.Add(_resizeThumb);

            _rootBorder.Child = layout;
            outer.Children.Add(_rootBorder);
            Content = outer;
        }

        private void BuildContextMenu()
        {
            ContextMenu menu = new ContextMenu();

            MenuItem miEdit = new MenuItem { Header = "编辑倒计时与外观..." };
            miEdit.Click += delegate { if (OnEditRequested != null) OnEditRequested(Item); };

            MenuItem miTopmost = new MenuItem { Header = "置顶显示" };
            miTopmost.Click += delegate
            {
                Item.LayerMode = (int)WindowLayerMode.Topmost;
                ApplyWindowBehavior();
                NotifyChanged();
            };

            MenuItem miNormal = new MenuItem { Header = "普通窗口层级" };
            miNormal.Click += delegate
            {
                Item.LayerMode = (int)WindowLayerMode.Normal;
                ApplyWindowBehavior();
                NotifyChanged();
            };

            MenuItem miBottom = new MenuItem { Header = "嵌入桌面底层" };
            miBottom.Click += delegate
            {
                Item.LayerMode = (int)WindowLayerMode.DesktopBottom;
                ApplyWindowBehavior();
                NotifyChanged();
            };

            MenuItem miLock = new MenuItem { Header = "锁定/解锁位置" };
            miLock.Click += delegate
            {
                Item.IsLocked = !Item.IsLocked;
                ApplyAppearance();
                NotifyChanged();
            };

            MenuItem miClickThrough = new MenuItem { Header = "开启鼠标穿透 (托盘可解锁)" };
            miClickThrough.Click += delegate
            {
                Item.ClickThrough = true;
                Item.IsLocked = true;
                ApplyAppearance();
                ApplyWindowBehavior();
                NotifyChanged();
            };

            MenuItem miCloseWidget = new MenuItem { Header = "关闭此悬浮窗" };
            miCloseWidget.Click += delegate
            {
                Item.ShowWidget = false;
                NotifyChanged();
                Close();
            };

            MenuItem miOpenMain = new MenuItem { Header = "打开主管理界面" };
            miOpenMain.Click += delegate { if (OnOpenMainRequested != null) OnOpenMainRequested(); };

            menu.Items.Add(miEdit);
            menu.Items.Add(new Separator());
            menu.Items.Add(miTopmost);
            menu.Items.Add(miNormal);
            menu.Items.Add(miBottom);
            menu.Items.Add(new Separator());
            menu.Items.Add(miLock);
            menu.Items.Add(miClickThrough);
            menu.Items.Add(new Separator());
            menu.Items.Add(miCloseWidget);
            menu.Items.Add(miOpenMain);

            ContextMenu = menu;
        }

        private void FloatingWidgetWindow_SourceInitialized(object sender, EventArgs e)
        {
            _hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            Win32Helper.SetToolWindow(this);
            ApplyWindowBehavior();
        }

        public void ApplyWindowBehavior()
        {
            WindowLayerMode layer = (WindowLayerMode)Item.LayerMode;
            if (_hwndSource != null)
            {
                if (layer == WindowLayerMode.DesktopBottom && !_bottomHookAttached)
                {
                    _hwndSource.AddHook(Win32Helper.KeepBottomWndProc);
                    _bottomHookAttached = true;
                }
                else if (layer != WindowLayerMode.DesktopBottom && _bottomHookAttached)
                {
                    _hwndSource.RemoveHook(Win32Helper.KeepBottomWndProc);
                    _bottomHookAttached = false;
                }
            }
            Win32Helper.ApplyLayerMode(this, layer);
            Win32Helper.SetClickThrough(this, Item.ClickThrough);
        }

        public void ApplyAppearance()
        {
            _rootBorder.CornerRadius = new CornerRadius(Item.CornerRadius);
            _rootBorder.Opacity = Math.Max(0.1, Math.Min(1.0, Item.Opacity));

            Color bgStart = ThemePresets.ParseColor(Item.BackgroundHex, Color.FromRgb(30, 34, 53));
            Color bgEnd = ThemePresets.ParseColor(Item.BackgroundEndHex, bgStart);
            Color fg = ThemePresets.ParseColor(Item.TextHex, Colors.White);
            Color accent = ThemePresets.ParseColor(Item.AccentHex, Color.FromRgb(79, 140, 255));

            bool isTransparentTheme = bgStart.A == 0 && bgEnd.A == 0;

            if (!string.IsNullOrEmpty(Item.BackgroundImagePath) && File.Exists(Item.BackgroundImagePath))
            {
                try
                {
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(Item.BackgroundImagePath, UriKind.Absolute);
                    bmp.EndInit();
                    _rootBorder.Background = new ImageBrush(bmp) { Stretch = Stretch.UniformToFill };
                    _rootBorder.BorderThickness = new Thickness(1);
                    _rootBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
                }
                catch
                {
                    SetGradientBackground(bgStart, bgEnd, isTransparentTheme);
                }
            }
            else
            {
                SetGradientBackground(bgStart, bgEnd, isTransparentTheme);
            }

            SolidColorBrush fgBrush = new SolidColorBrush(fg);
            SolidColorBrush accentBrush = new SolidColorBrush(accent);

            _categoryText.Foreground = accentBrush;
            _titleText.Foreground = fgBrush;
            _prefixText.Foreground = fgBrush;
            _valueText.Foreground = isTransparentTheme ? fgBrush : accentBrush;
            _valueText.FontSize = Item.FontSize >= 14 ? Item.FontSize : 28;
            _subText.Foreground = fgBrush;
            _progressFill.Background = accentBrush;
            _progressTrack.Visibility = Item.ShowProgressBar ? Visibility.Visible : Visibility.Collapsed;
            _lockBadge.Visibility = Item.IsLocked ? Visibility.Visible : Visibility.Collapsed;
            _lockBadge.Foreground = fgBrush;
            _resizeThumb.IsEnabled = !Item.IsLocked;
        }

        private void SetGradientBackground(Color bgStart, Color bgEnd, bool isTransparentTheme)
        {
            if (isTransparentTheme)
            {
                _rootBorder.Background = Brushes.Transparent;
                _rootBorder.BorderThickness = new Thickness(0);
                _rootBorder.Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 6,
                    ShadowDepth = 1,
                    Opacity = 0.85
                };
            }
            else
            {
                LinearGradientBrush lgb = new LinearGradientBrush(bgStart, bgEnd, new Point(0, 0), new Point(1, 1));
                _rootBorder.Background = lgb;
                _rootBorder.BorderThickness = new Thickness(1);
                _rootBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                _rootBorder.Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 14,
                    ShadowDepth = 3,
                    Opacity = 0.35
                };
            }
        }

        public void UpdateTimeDisplay(DateTime now)
        {
            TimeDisplayResult res = TimeCalculator.Compute(Item, now);
            _categoryText.Text = string.IsNullOrEmpty(Item.Category) ? "倒计时" : Item.Category;
            _titleText.Text = Item.Title;
            _prefixText.Text = res.PrefixLabel;
            _valueText.Text = res.MainValue;
            _subText.Text = string.IsNullOrEmpty(Item.Note) ? res.SubText : string.Format("{0} · {1}", res.SubText, Item.Note);

            if (Item.ShowProgressBar && _progressTrack.ActualWidth > 0)
            {
                _progressFill.Width = _progressTrack.ActualWidth * (res.ProgressPercent / 100.0);
            }
        }

        private void FloatingWidgetWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (OnEditRequested != null) OnEditRequested(Item);
                return;
            }
            if (!Item.IsLocked && e.ButtonState == MouseButtonState.Pressed)
            {
                try
                {
                    DragMove();
                    Item.WidgetLeft = Left;
                    Item.WidgetTop = Top;
                    NotifyChanged();
                }
                catch
                {
                }
            }
        }

        private void FloatingWidgetWindow_LocationChanged(object sender, EventArgs e)
        {
            if (IsLoaded)
            {
                Item.WidgetLeft = Left;
                Item.WidgetTop = Top;
            }
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (Item.IsLocked) return;
            double newW = Width + e.HorizontalChange;
            double newH = Height + e.VerticalChange;
            if (newW >= MinWidth && newW <= 800) Width = newW;
            if (newH >= MinHeight && newH <= 500) Height = newH;
        }

        private void ResizeThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            Item.WidgetWidth = Width;
            Item.WidgetHeight = Height;
            NotifyChanged();
        }

        private void FloatingWidgetWindow_Closed(object sender, EventArgs e)
        {
            if (_hwndSource != null && _bottomHookAttached)
            {
                _hwndSource.RemoveHook(Win32Helper.KeepBottomWndProc);
                _bottomHookAttached = false;
            }
        }

        private void NotifyChanged()
        {
            if (OnChanged != null) OnChanged(Item);
        }
    }
}
