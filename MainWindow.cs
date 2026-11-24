using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;
using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;
using MediaPoint = System.Windows.Point;

namespace DeskCountdown
{
    public class MainWindow : Window
    {
        private AppData _data;
        private Dictionary<string, FloatingWidgetWindow> _widgets = new Dictionary<string, FloatingWidgetWindow>();
        private DispatcherTimer _mainTimer;
        private WinForms.NotifyIcon _trayIcon;
        private bool _forceExit;

        // Navigation
        private Button _navCountdownBtn;
        private Button _navTimerBtn;
        private Button _navSettingsBtn;
        private Grid _countdownPage;
        private Grid _timerPage;
        private ScrollViewer _settingsPage;

        // Countdown page controls
        private TextBox _searchBox;
        private StackPanel _categoryBar;
        private string _selectedCategory = "全部";
        private WrapPanel _cardsPanel;
        private Dictionary<string, CardUiRef> _cardRefs = new Dictionary<string, CardUiRef>();
        private TextBlock _statsSummaryText;

        // Timer / Stopwatch controls
        private bool _isStopwatchMode;
        private bool _timerRunning;
        private int _timerRemainingSeconds = 25 * 60;
        private int _timerInitialSeconds = 25 * 60;
        private int _stopwatchElapsedSeconds;
        private TextBox _timerLabelBox;
        private TextBox _customMinBox;
        private TextBlock _timerDisplayText;
        private TextBlock _timerModeSubtitle;
        private Button _timerStartPauseBtn;
        private Button _timerLapBtn;
        private ListBox _lapListBox;
        private ListBox _historyListBox;

        // Settings controls
        private CheckBox _autoStartCheck;
        private CheckBox _minToTrayCheck;
        private CheckBox _soundCheck;
        private ComboBox _batchThemeBox;
        private TextBlock _hideToggleStatusText;

        private class CardUiRef
        {
            public TextBlock PrefixText;
            public TextBlock ValueText;
            public TextBlock SubText;
            public Border ProgressFill;
            public Border ProgressTrack;
        }

        public MainWindow(AppData data, bool startMinimized)
        {
            _data = data;

            Title = "DeskCountdown - 桌面倒计时与专注工具";
            Width = 1040;
            Height = 680;
            MinWidth = 860;
            MinHeight = 560;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FontFamily = new System.Windows.Media.FontFamily("Microsoft YaHei UI, Segoe UI");
            Background = new SolidColorBrush(MediaColor.FromRgb(241, 245, 249));

            BuildLayout();
            SetupTrayIcon();
            SyncAllWidgets();
            RefreshCountdownCards();
            RefreshHistoryList();
            SwitchTab(0);

            _mainTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _mainTimer.Tick += MainTimer_Tick;
            _mainTimer.Start();

            Closing += MainWindow_Closing;

            if (startMinimized)
            {
                WindowState = WindowState.Minimized;
                ShowInTaskbar = false;
                Hide();
            }
        }

        private void BuildLayout()
        {
            Grid root = new Grid();
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Left sidebar
            Border sidebar = new Border
            {
                Background = new SolidColorBrush(MediaColor.FromRgb(15, 23, 42)),
                Padding = new Thickness(16, 22, 16, 16)
            };
            DockPanel sideDock = new DockPanel();

            StackPanel brand = new StackPanel { Margin = new Thickness(4, 0, 0, 26) };
            brand.Children.Add(new TextBlock
            {
                Text = "DeskCountdown",
                Foreground = MediaBrushes.White,
                FontSize = 18,
                FontWeight = FontWeights.Bold
            });
            brand.Children.Add(new TextBlock
            {
                Text = "桌面倒计时 · 便携纯净版",
                Foreground = new SolidColorBrush(MediaColor.FromRgb(148, 163, 184)),
                FontSize = 11.5,
                Margin = new Thickness(0, 3, 0, 0)
            });
            DockPanel.SetDock(brand, Dock.Top);
            sideDock.Children.Add(brand);

            // Bottom quick action in sidebar
            StackPanel sideFooter = new StackPanel();
            Button toggleAllWidgetsBtn = CreateSidebarActionButton("👁 一键隐藏/显示悬浮窗");
            toggleAllWidgetsBtn.Click += delegate { ToggleGlobalWidgetsHidden(); };
            Button unlockAllBtn = CreateSidebarActionButton("🔓 解锁全部悬浮窗");
            unlockAllBtn.Click += delegate { UnlockAllWidgets(); };
            sideFooter.Children.Add(toggleAllWidgetsBtn);
            sideFooter.Children.Add(unlockAllBtn);
            DockPanel.SetDock(sideFooter, Dock.Bottom);
            sideDock.Children.Add(sideFooter);

            // Nav buttons
            StackPanel navStack = new StackPanel();
            _navCountdownBtn = CreateNavButton("📅  倒计时管理");
            _navCountdownBtn.Click += delegate { SwitchTab(0); };
            _navTimerBtn = CreateNavButton("⏱  专注计时与秒表");
            _navTimerBtn.Click += delegate { SwitchTab(1); };
            _navSettingsBtn = CreateNavButton("⚙  悬浮窗与设置");
            _navSettingsBtn.Click += delegate { SwitchTab(2); };

            navStack.Children.Add(_navCountdownBtn);
            navStack.Children.Add(_navTimerBtn);
            navStack.Children.Add(_navSettingsBtn);
            sideDock.Children.Add(navStack);

            sidebar.Child = sideDock;
            Grid.SetColumn(sidebar, 0);
            root.Children.Add(sidebar);

            // Right content area
            Grid contentHost = new Grid { Margin = new Thickness(22, 18, 22, 18) };
            BuildCountdownPage();
            BuildTimerPage();
            BuildSettingsPage();

            contentHost.Children.Add(_countdownPage);
            contentHost.Children.Add(_timerPage);
            contentHost.Children.Add(_settingsPage);

            Grid.SetColumn(contentHost, 1);
            root.Children.Add(contentHost);

            Content = root;
        }

        private Button CreateNavButton(string text)
        {
            return new Button
            {
                Content = text,
                Height = 42,
                Margin = new Thickness(0, 0, 0, 6),
                Padding = new Thickness(14, 0, 12, 0),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = MediaBrushes.Transparent,
                Foreground = new SolidColorBrush(MediaColor.FromRgb(203, 213, 225)),
                BorderThickness = new Thickness(0),
                FontSize = 13.5,
                Cursor = System.Windows.Input.Cursors.Hand
            };
        }

        private Button CreateSidebarActionButton(string text)
        {
            return new Button
            {
                Content = text,
                Height = 34,
                Margin = new Thickness(0, 6, 0, 0),
                Background = new SolidColorBrush(MediaColor.FromRgb(30, 41, 59)),
                Foreground = new SolidColorBrush(MediaColor.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(0),
                FontSize = 12,
                Cursor = System.Windows.Input.Cursors.Hand
            };
        }

        private void SwitchTab(int index)
        {
            _countdownPage.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
            _timerPage.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
            _settingsPage.Visibility = index == 2 ? Visibility.Visible : Visibility.Collapsed;

            MediaBrush activeBg = new SolidColorBrush(MediaColor.FromRgb(37, 99, 235));
            MediaBrush inactiveBg = MediaBrushes.Transparent;
            MediaBrush activeFg = MediaBrushes.White;
            MediaBrush inactiveFg = new SolidColorBrush(MediaColor.FromRgb(203, 213, 225));

            _navCountdownBtn.Background = index == 0 ? activeBg : inactiveBg;
            _navCountdownBtn.Foreground = index == 0 ? activeFg : inactiveFg;
            _navTimerBtn.Background = index == 1 ? activeBg : inactiveBg;
            _navTimerBtn.Foreground = index == 1 ? activeFg : inactiveFg;
            _navSettingsBtn.Background = index == 2 ? activeBg : inactiveBg;
            _navSettingsBtn.Foreground = index == 2 ? activeFg : inactiveFg;

            if (index == 2)
                RefreshSettingsState();
        }

        private void BuildCountdownPage()
        {
            _countdownPage = new Grid();
            _countdownPage.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _countdownPage.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _countdownPage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Top bar
            DockPanel header = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
            StackPanel titleBlock = new StackPanel();
            titleBlock.Children.Add(new TextBlock
            {
                Text = "倒计时与纪念日",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(MediaColor.FromRgb(15, 23, 42))
            });
            _statsSummaryText = new TextBlock
            {
                FontSize = 12,
                Foreground = new SolidColorBrush(MediaColor.FromRgb(100, 116, 139)),
                Margin = new Thickness(0, 2, 0, 0)
            };
            titleBlock.Children.Add(_statsSummaryText);
            DockPanel.SetDock(titleBlock, Dock.Left);
            header.Children.Add(titleBlock);

            StackPanel rightActions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            _searchBox = new TextBox
            {
                Width = 190,
                Height = 34,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(10, 0, 10, 0),
                Margin = new Thickness(0, 0, 10, 0),
                ToolTip = "搜索标题或备注..."
            };
            _searchBox.TextChanged += delegate { RefreshCountdownCards(); };

            Button addBtn = new Button
            {
                Content = "+ 新建倒计时",
                Height = 34,
                Padding = new Thickness(16, 0, 16, 0),
                Background = new SolidColorBrush(MediaColor.FromRgb(37, 99, 235)),
                Foreground = MediaBrushes.White,
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            addBtn.Click += delegate { OpenAddDialog(); };

            rightActions.Children.Add(_searchBox);
            rightActions.Children.Add(addBtn);
            DockPanel.SetDock(rightActions, Dock.Right);
            header.Children.Add(rightActions);

            Grid.SetRow(header, 0);
            _countdownPage.Children.Add(header);

            // Category filter bar
            ScrollViewer catScroll = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(0, 0, 0, 12)
            };
            _categoryBar = new StackPanel { Orientation = Orientation.Horizontal };
            catScroll.Content = _categoryBar;
            Grid.SetRow(catScroll, 1);
            _countdownPage.Children.Add(catScroll);

            // Cards scroll
            ScrollViewer cardsScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            _cardsPanel = new WrapPanel();
            cardsScroll.Content = _cardsPanel;
            Grid.SetRow(cardsScroll, 2);
            _countdownPage.Children.Add(cardsScroll);
        }

        private void BuildTimerPage()
        {
            _timerPage = new Grid();
            _timerPage.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _timerPage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            TextBlock title = new TextBlock
            {
                Text = "专注计时器 / 番茄钟 & 秒表",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(MediaColor.FromRgb(15, 23, 42)),
                Margin = new Thickness(0, 0, 0, 16)
            };
            Grid.SetRow(title, 0);
            _timerPage.Children.Add(title);

            Grid body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Left timer card
            Border timerCard = new Border
            {
                Background = MediaBrushes.White,
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(24),
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1)
            };
            StackPanel tStack = new StackPanel();

            // Mode switch
            StackPanel modeRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 14) };
            Button cdModeBtn = new Button
            {
                Content = "🍅 番茄钟 / 倒计时",
                Height = 32,
                Padding = new Thickness(14, 0, 14, 0),
                Margin = new Thickness(0, 0, 8, 0),
                Background = new SolidColorBrush(MediaColor.FromRgb(37, 99, 235)),
                Foreground = MediaBrushes.White,
                BorderThickness = new Thickness(0)
            };
            Button swModeBtn = new Button
            {
                Content = "⏱ 正向秒表",
                Height = 32,
                Padding = new Thickness(14, 0, 14, 0),
                Background = new SolidColorBrush(MediaColor.FromRgb(226, 232, 240)),
                Foreground = new SolidColorBrush(MediaColor.FromRgb(30, 41, 59)),
                BorderThickness = new Thickness(0)
            };
            cdModeBtn.Click += delegate
            {
                _isStopwatchMode = false;
                _timerRunning = false;
                _timerStartPauseBtn.Content = "开始";
                _timerLapBtn.Visibility = Visibility.Collapsed;
                cdModeBtn.Background = new SolidColorBrush(MediaColor.FromRgb(37, 99, 235));
                cdModeBtn.Foreground = MediaBrushes.White;
                swModeBtn.Background = new SolidColorBrush(MediaColor.FromRgb(226, 232, 240));
                swModeBtn.Foreground = new SolidColorBrush(MediaColor.FromRgb(30, 41, 59));
                UpdateTimerDisplay();
            };
            swModeBtn.Click += delegate
            {
                _isStopwatchMode = true;
                _timerRunning = false;
                _timerStartPauseBtn.Content = "开始";
                _timerLapBtn.Visibility = Visibility.Visible;
                swModeBtn.Background = new SolidColorBrush(MediaColor.FromRgb(37, 99, 235));
                swModeBtn.Foreground = MediaBrushes.White;
                cdModeBtn.Background = new SolidColorBrush(MediaColor.FromRgb(226, 232, 240));
                cdModeBtn.Foreground = new SolidColorBrush(MediaColor.FromRgb(30, 41, 59));
                UpdateTimerDisplay();
            };
            modeRow.Children.Add(cdModeBtn);
            modeRow.Children.Add(swModeBtn);
            tStack.Children.Add(modeRow);

            // Label & custom minutes
            StackPanel cfgRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
            cfgRow.Children.Add(new TextBlock { Text = "任务名称：", VerticalAlignment = VerticalAlignment.Center });
            _timerLabelBox = new TextBox { Text = "深度专注", Width = 150, Height = 30, VerticalContentAlignment = VerticalAlignment.Center, Padding = new Thickness(6, 0, 6, 0), Margin = new Thickness(4, 0, 14, 0) };
            cfgRow.Children.Add(_timerLabelBox);
            cfgRow.Children.Add(new TextBlock { Text = "分钟：", VerticalAlignment = VerticalAlignment.Center });
            _customMinBox = new TextBox { Text = "25", Width = 54, Height = 30, VerticalContentAlignment = VerticalAlignment.Center, Padding = new Thickness(6, 0, 6, 0), Margin = new Thickness(4, 0, 6, 0) };
            Button applyMinBtn = new Button { Content = "设定", Height = 30, Padding = new Thickness(10, 0, 10, 0) };
            applyMinBtn.Click += delegate
            {
                double mins;
                if (double.TryParse(_customMinBox.Text, out mins) && mins > 0)
                {
                    _isStopwatchMode = false;
                    _timerRunning = false;
                    _timerInitialSeconds = (int)Math.Round(mins * 60);
                    _timerRemainingSeconds = _timerInitialSeconds;
                    _timerStartPauseBtn.Content = "开始";
                    UpdateTimerDisplay();
                }
            };
            cfgRow.Children.Add(_customMinBox);
            cfgRow.Children.Add(applyMinBtn);
            tStack.Children.Add(cfgRow);

            // Presets
            WrapPanel presetRow = new WrapPanel { Margin = new Thickness(0, 0, 0, 18) };
            AddTimerPreset(presetRow, "5 分钟", 5);
            AddTimerPreset(presetRow, "15 分钟", 15);
            AddTimerPreset(presetRow, "25 分钟 (番茄钟)", 25);
            AddTimerPreset(presetRow, "45 分钟", 45);
            AddTimerPreset(presetRow, "60 分钟", 60);
            tStack.Children.Add(presetRow);

            // Big clock
            Border clockBox = new Border
            {
                Background = new SolidColorBrush(MediaColor.FromRgb(15, 23, 42)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20, 28, 20, 28),
                Margin = new Thickness(0, 0, 0, 18)
            };
            StackPanel clockInner = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            _timerModeSubtitle = new TextBlock
            {
                Text = "番茄钟倒计时",
                Foreground = new SolidColorBrush(MediaColor.FromRgb(148, 163, 184)),
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _timerDisplayText = new TextBlock
            {
                Text = "25:00",
                Foreground = new SolidColorBrush(MediaColor.FromRgb(96, 165, 250)),
                FontSize = 54,
                FontWeight = FontWeights.Bold,
                FontFamily = new System.Windows.Media.FontFamily("Consolas, Segoe UI"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 6, 0, 0)
            };
            clockInner.Children.Add(_timerModeSubtitle);
            clockInner.Children.Add(_timerDisplayText);
            clockBox.Child = clockInner;
            tStack.Children.Add(clockBox);

            // Control buttons
            StackPanel ctrlRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            _timerStartPauseBtn = new Button
            {
                Content = "开始",
                Width = 110,
                Height = 38,
                Background = new SolidColorBrush(MediaColor.FromRgb(37, 99, 235)),
                Foreground = MediaBrushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            _timerStartPauseBtn.Click += TimerStartPauseBtn_Click;

            Button resetBtn = new Button
            {
                Content = "重置",
                Width = 90,
                Height = 38,
                Background = new SolidColorBrush(MediaColor.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(0),
                FontSize = 14,
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            resetBtn.Click += delegate
            {
                _timerRunning = false;
                _timerStartPauseBtn.Content = "开始";
                if (_isStopwatchMode)
                {
                    if (_stopwatchElapsedSeconds > 0)
                        RecordTimerSession(_timerLabelBox.Text, "秒表", _stopwatchElapsedSeconds);
                    _stopwatchElapsedSeconds = 0;
                    _lapListBox.Items.Clear();
                }
                else
                {
                    _timerRemainingSeconds = _timerInitialSeconds;
                }
                UpdateTimerDisplay();
            };

            _timerLapBtn = new Button
            {
                Content = "计次 (Lap)",
                Width = 96,
                Height = 38,
                Visibility = Visibility.Collapsed,
                Background = new SolidColorBrush(MediaColor.FromRgb(16, 185, 129)),
                Foreground = MediaBrushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 13.5,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            _timerLapBtn.Click += delegate
            {
                int idx = _lapListBox.Items.Count + 1;
                _lapListBox.Items.Insert(0, string.Format("计次 #{0}  —  {1}", idx, FormatSeconds(_stopwatchElapsedSeconds)));
            };

            ctrlRow.Children.Add(_timerStartPauseBtn);
            ctrlRow.Children.Add(resetBtn);
            ctrlRow.Children.Add(_timerLapBtn);
            tStack.Children.Add(ctrlRow);

            // Laps
            _lapListBox = new ListBox { Height = 90, Margin = new Thickness(0, 14, 0, 0), BorderBrush = new SolidColorBrush(MediaColor.FromRgb(226, 232, 240)) };
            tStack.Children.Add(_lapListBox);

            timerCard.Child = tStack;
            Grid.SetColumn(timerCard, 0);
            body.Children.Add(timerCard);

            // Right history card
            Border histCard = new Border
            {
                Background = MediaBrushes.White,
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(18),
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1)
            };
            DockPanel histDock = new DockPanel();
            DockPanel histHeader = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
            histHeader.Children.Add(new TextBlock
            {
                Text = "历史计时记录（可导出 CSV）",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            });
            Button clearHistBtn = new Button { Content = "清空", Padding = new Thickness(8, 2, 8, 2), HorizontalAlignment = HorizontalAlignment.Right };
            clearHistBtn.Click += delegate
            {
                _data.TimerHistory.Clear();
                StorageService.Save(_data);
                RefreshHistoryList();
            };
            DockPanel.SetDock(clearHistBtn, Dock.Right);
            histHeader.Children.Add(clearHistBtn);
            DockPanel.SetDock(histHeader, Dock.Top);
            histDock.Children.Add(histHeader);

            _historyListBox = new ListBox { BorderThickness = new Thickness(0) };
            histDock.Children.Add(_historyListBox);
            histCard.Child = histDock;
            Grid.SetColumn(histCard, 2);
            body.Children.Add(histCard);

            Grid.SetRow(body, 1);
            _timerPage.Children.Add(body);
        }

        private void AddTimerPreset(WrapPanel panel, string text, int minutes)
        {
            Button b = new Button
            {
                Content = text,
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 8, 6),
                Background = new SolidColorBrush(MediaColor.FromRgb(248, 250, 252)),
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(203, 213, 225)),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            b.Click += delegate
            {
                _isStopwatchMode = false;
                _timerRunning = false;
                _timerInitialSeconds = minutes * 60;
                _timerRemainingSeconds = _timerInitialSeconds;
                _customMinBox.Text = minutes.ToString();
                _timerStartPauseBtn.Content = "开始";
                UpdateTimerDisplay();
            };
            panel.Children.Add(b);
        }

        private void TimerStartPauseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!_isStopwatchMode && _timerRemainingSeconds <= 0)
                _timerRemainingSeconds = _timerInitialSeconds;

            _timerRunning = !_timerRunning;
            _timerStartPauseBtn.Content = _timerRunning ? "暂停" : "继续";
        }

        private void UpdateTimerDisplay()
        {
            if (_isStopwatchMode)
            {
                _timerModeSubtitle.Text = "正向秒表计时";
                _timerDisplayText.Text = FormatSeconds(_stopwatchElapsedSeconds);
            }
            else
            {
                _timerModeSubtitle.Text = string.Format("专注倒计时 · {0}", _timerLabelBox.Text);
                _timerDisplayText.Text = FormatSeconds(_timerRemainingSeconds);
            }
        }

        private string FormatSeconds(int totalSeconds)
        {
            if (totalSeconds < 0) totalSeconds = 0;
            TimeSpan ts = TimeSpan.FromSeconds(totalSeconds);
            if (ts.TotalHours >= 1)
                return string.Format("{0:D2}:{1:D2}:{2:D2}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
            return string.Format("{0:D2}:{1:D2}", ts.Minutes, ts.Seconds);
        }

        private void RecordTimerSession(string label, string mode, int seconds)
        {
            _data.TimerHistory.Insert(0, new TimerRecord
            {
                Label = string.IsNullOrWhiteSpace(label) ? "专注计时" : label.Trim(),
                Mode = mode,
                DurationSeconds = seconds,
                CompletedAtIso = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
            StorageService.Save(_data);
            RefreshHistoryList();
        }

        private void RefreshHistoryList()
        {
            _historyListBox.Items.Clear();
            foreach (TimerRecord rec in _data.TimerHistory)
            {
                _historyListBox.Items.Add(string.Format("[{0}] {1} — {2} ({3})",
                    rec.Mode, rec.Label, FormatSeconds(rec.DurationSeconds), rec.CompletedAtIso));
            }
        }

        private void BuildSettingsPage()
        {
            _settingsPage = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            StackPanel stack = new StackPanel();

            stack.Children.Add(new TextBlock
            {
                Text = "悬浮窗管理、外观与系统设置",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(MediaColor.FromRgb(15, 23, 42)),
                Margin = new Thickness(0, 0, 0, 16)
            });

            // Card 1: Floating widgets batch operations
            StackPanel card1 = CreateSettingsCard(stack, "桌面悬浮窗批量控制");
            _hideToggleStatusText = new TextBlock
            {
                Foreground = new SolidColorBrush(MediaColor.FromRgb(100, 116, 139)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            card1.Children.Add(_hideToggleStatusText);

            WrapPanel batchBtns = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
            Button btnHideShow = CreateActionBtn("一键隐藏 / 显示全部悬浮窗", "#2563EB");
            btnHideShow.Click += delegate { ToggleGlobalWidgetsHidden(); RefreshSettingsState(); };
            Button btnUnlockAll = CreateActionBtn("全部解锁（解除锁定与鼠标穿透）", "#0D9488");
            btnUnlockAll.Click += delegate { UnlockAllWidgets(); };
            Button btnCascade = CreateActionBtn("整理排列所有悬浮窗位置", "#475569");
            btnCascade.Click += delegate { CascadeWidgets(); };
            batchBtns.Children.Add(btnHideShow);
            batchBtns.Children.Add(btnUnlockAll);
            batchBtns.Children.Add(btnCascade);
            card1.Children.Add(batchBtns);

            StackPanel themeRow = new StackPanel { Orientation = Orientation.Horizontal };
            themeRow.Children.Add(new TextBlock { Text = "一键统一皮肤：", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
            _batchThemeBox = new ComboBox { Width = 200, Height = 30 };
            foreach (ThemePreset p in ThemePresets.All)
                _batchThemeBox.Items.Add(p.DisplayName);
            _batchThemeBox.SelectedIndex = 0;
            Button applyThemeAll = CreateActionBtn("应用到全部悬浮窗", "#4F46E5");
            applyThemeAll.Margin = new Thickness(10, 0, 0, 0);
            applyThemeAll.Click += delegate
            {
                if (_batchThemeBox.SelectedIndex < 0) return;
                ThemePreset p = ThemePresets.All[_batchThemeBox.SelectedIndex];
                foreach (CountdownItem it in _data.Items)
                {
                    it.ThemeKey = p.Key;
                    it.BackgroundHex = p.BackgroundHex;
                    it.BackgroundEndHex = p.BackgroundEndHex;
                    it.TextHex = p.TextHex;
                    it.AccentHex = p.AccentHex;
                    it.Opacity = p.DefaultOpacity;
                }
                StorageService.Save(_data);
                SyncAllWidgets();
                RefreshCountdownCards();
                MessageBox.Show("已将所选主题皮肤应用到所有事项。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            themeRow.Children.Add(_batchThemeBox);
            themeRow.Children.Add(applyThemeAll);
            card1.Children.Add(themeRow);

            // Card 2: System behavior
            StackPanel card2 = CreateSettingsCard(stack, "常规与开机启动");
            _autoStartCheck = new CheckBox { Content = "开机自动启动（静默启动到系统托盘并显示桌面悬浮窗）", Margin = new Thickness(0, 4, 0, 8) };
            _autoStartCheck.Click += delegate
            {
                try
                {
                    StorageService.SetAutoStart(_autoStartCheck.IsChecked == true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("设置开机启动失败：" + ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _autoStartCheck.IsChecked = StorageService.IsAutoStartEnabled();
                }
            };
            _minToTrayCheck = new CheckBox { Content = "点击关闭按钮时最小化到系统托盘（不退出悬浮窗）", Margin = new Thickness(0, 4, 0, 8) };
            _minToTrayCheck.Click += delegate
            {
                _data.Settings.MinimizeToTrayOnClose = _minToTrayCheck.IsChecked == true;
                StorageService.Save(_data);
            };
            _soundCheck = new CheckBox { Content = "到期提醒与番茄钟结束时播放提示音", Margin = new Thickness(0, 4, 0, 4) };
            _soundCheck.Click += delegate
            {
                _data.Settings.PlaySoundOnReminder = _soundCheck.IsChecked == true;
                StorageService.Save(_data);
            };
            card2.Children.Add(_autoStartCheck);
            card2.Children.Add(_minToTrayCheck);
            card2.Children.Add(_soundCheck);

            // Card 3: Data export & portable storage
            StackPanel card3 = CreateSettingsCard(stack, "数据导出、备份与便携存储");
            card3.Children.Add(new TextBlock
            {
                Text = "当前数据文件（纯绿色便携模式，不占用 C 盘）：\n" + StorageService.DataFilePath,
                Foreground = new SolidColorBrush(MediaColor.FromRgb(71, 85, 105)),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            });

            WrapPanel ioBtns = new WrapPanel();
            Button exportCsvBtn = CreateActionBtn("导出数据为 CSV 表格", "#059669");
            exportCsvBtn.Click += ExportCsvBtn_Click;
            Button exportJsonBtn = CreateActionBtn("备份 JSON 配置文件", "#2563EB");
            exportJsonBtn.Click += ExportJsonBtn_Click;
            Button importJsonBtn = CreateActionBtn("从 JSON 恢复数据", "#D97706");
            importJsonBtn.Click += ImportJsonBtn_Click;
            Button openFolderBtn = CreateActionBtn("打开程序所在目录", "#475569");
            openFolderBtn.Click += delegate
            {
                try { Process.Start("explorer.exe", AppDomain.CurrentDomain.BaseDirectory); } catch { }
            };
            ioBtns.Children.Add(exportCsvBtn);
            ioBtns.Children.Add(exportJsonBtn);
            ioBtns.Children.Add(importJsonBtn);
            ioBtns.Children.Add(openFolderBtn);
            card3.Children.Add(ioBtns);

            _settingsPage.Content = stack;
        }

        private StackPanel CreateSettingsCard(StackPanel parent, string title)
        {
            Border card = new Border
            {
                Background = MediaBrushes.White,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 14),
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1)
            };
            StackPanel inner = new StackPanel();
            inner.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(MediaColor.FromRgb(15, 23, 42)),
                Margin = new Thickness(0, 0, 0, 10)
            });
            card.Child = inner;
            parent.Children.Add(card);
            return inner;
        }

        private Button CreateActionBtn(string text, string hex)
        {
            return new Button
            {
                Content = text,
                Height = 34,
                Padding = new Thickness(14, 0, 14, 0),
                Margin = new Thickness(0, 0, 10, 8),
                Background = ThemePresets.CreateBrush(hex, Colors.SteelBlue),
                Foreground = MediaBrushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
        }

        private void RefreshSettingsState()
        {
            _autoStartCheck.IsChecked = StorageService.IsAutoStartEnabled();
            _minToTrayCheck.IsChecked = _data.Settings.MinimizeToTrayOnClose;
            _soundCheck.IsChecked = _data.Settings.PlaySoundOnReminder;
            _hideToggleStatusText.Text = _data.Settings.WidgetsGloballyHidden
                ? "当前状态：所有桌面悬浮窗已全局隐藏。"
                : string.Format("当前状态：桌面正在显示 {0} 个悬浮窗。", _widgets.Count);
        }

        private void RefreshCountdownCards()
        {
            // Rebuild category filter pills
            List<string> cats = new List<string> { "全部" };
            foreach (CountdownItem it in _data.Items)
            {
                string c = string.IsNullOrEmpty(it.Category) ? "未分类" : it.Category;
                if (!cats.Contains(c)) cats.Add(c);
            }
            if (!cats.Contains(_selectedCategory)) _selectedCategory = "全部";

            _categoryBar.Children.Clear();
            foreach (string c in cats)
            {
                string catCopy = c;
                bool active = (catCopy == _selectedCategory);
                Button pill = new Button
                {
                    Content = catCopy,
                    Padding = new Thickness(12, 4, 12, 4),
                    Margin = new Thickness(0, 0, 8, 0),
                    Background = active ? new SolidColorBrush(MediaColor.FromRgb(37, 99, 235)) : MediaBrushes.White,
                    Foreground = active ? MediaBrushes.White : new SolidColorBrush(MediaColor.FromRgb(51, 65, 85)),
                    BorderBrush = new SolidColorBrush(MediaColor.FromRgb(203, 213, 225)),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                pill.Click += delegate
                {
                    _selectedCategory = catCopy;
                    RefreshCountdownCards();
                };
                _categoryBar.Children.Add(pill);
            }

            string keyword = (_searchBox.Text ?? "").Trim().ToLowerInvariant();
            _cardsPanel.Children.Clear();
            _cardRefs.Clear();

            DateTime now = DateTime.Now;
            int activeWidgetCount = 0;

            foreach (CountdownItem item in _data.Items)
            {
                if (item.ShowWidget) activeWidgetCount++;
                string cat = string.IsNullOrEmpty(item.Category) ? "未分类" : item.Category;
                if (_selectedCategory != "全部" && cat != _selectedCategory) continue;
                if (!string.IsNullOrEmpty(keyword))
                {
                    string hay = ((item.Title ?? "") + " " + (item.Note ?? "") + " " + cat).ToLowerInvariant();
                    if (!hay.Contains(keyword)) continue;
                }

                _cardsPanel.Children.Add(BuildItemCard(item, now));
            }

            _statsSummaryText.Text = string.Format("共 {0} 个事项 · {1} 个开启桌面悬浮窗", _data.Items.Count, activeWidgetCount);
        }

        private UIElement BuildItemCard(CountdownItem item, DateTime now)
        {
            TimeDisplayResult res = TimeCalculator.Compute(item, now);
            MediaColor accent = ThemePresets.ParseColor(item.AccentHex, MediaColor.FromRgb(37, 99, 235));

            Border card = new Border
            {
                Width = 350,
                Margin = new Thickness(0, 0, 14, 14),
                Padding = new Thickness(16, 14, 16, 14),
                Background = MediaBrushes.White,
                CornerRadius = new CornerRadius(12),
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1)
            };

            StackPanel sp = new StackPanel();

            // Header row: Category badge + Mode badge + Widget toggle
            DockPanel topRow = new DockPanel();
            StackPanel badges = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            Border catBadge = new Border
            {
                Background = new SolidColorBrush(MediaColor.FromArgb(30, accent.R, accent.G, accent.B)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 0, 6, 0)
            };
            catBadge.Child = new TextBlock
            {
                Text = string.IsNullOrEmpty(item.Category) ? "倒计时" : item.Category,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(accent)
            };
            badges.Children.Add(catBadge);

            badges.Children.Add(new TextBlock
            {
                Text = item.Mode == (int)CountMode.CountUp ? "正计时" : "倒计时",
                FontSize = 11,
                Foreground = new SolidColorBrush(MediaColor.FromRgb(100, 116, 139)),
                VerticalAlignment = VerticalAlignment.Center
            });

            DockPanel.SetDock(badges, Dock.Left);
            topRow.Children.Add(badges);

            CheckBox widgetToggle = new CheckBox
            {
                Content = "桌面悬浮",
                IsChecked = item.ShowWidget,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            widgetToggle.Click += delegate
            {
                item.ShowWidget = widgetToggle.IsChecked == true;
                if (item.ShowWidget) _data.Settings.WidgetsGloballyHidden = false;
                StorageService.Save(_data);
                SyncWidgetForItem(item);
                _statsSummaryText.Text = string.Format("共 {0} 个事项 · {1} 个开启桌面悬浮窗",
                    _data.Items.Count, _data.Items.Count(x => x.ShowWidget));
            };
            DockPanel.SetDock(widgetToggle, Dock.Right);
            topRow.Children.Add(widgetToggle);
            sp.Children.Add(topRow);

            // Title
            sp.Children.Add(new TextBlock
            {
                Text = item.Title,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(MediaColor.FromRgb(15, 23, 42)),
                Margin = new Thickness(0, 8, 0, 2),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            // Prefix & Value
            TextBlock prefixTb = new TextBlock
            {
                Text = res.PrefixLabel,
                FontSize = 11.5,
                Foreground = new SolidColorBrush(MediaColor.FromRgb(100, 116, 139))
            };
            TextBlock valueTb = new TextBlock
            {
                Text = res.MainValue,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(accent),
                Margin = new Thickness(0, 2, 0, 4)
            };
            TextBlock subTb = new TextBlock
            {
                Text = string.IsNullOrEmpty(item.Note) ? res.SubText : string.Format("{0} · {1}", res.SubText, item.Note),
                FontSize = 11.5,
                Foreground = new SolidColorBrush(MediaColor.FromRgb(100, 116, 139)),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            sp.Children.Add(prefixTb);
            sp.Children.Add(valueTb);
            sp.Children.Add(subTb);

            // Progress bar
            Border pTrack = new Border
            {
                Height = 5,
                CornerRadius = new CornerRadius(2.5),
                Background = new SolidColorBrush(MediaColor.FromRgb(226, 232, 240)),
                Margin = new Thickness(0, 8, 0, 10),
                Visibility = item.ShowProgressBar ? Visibility.Visible : Visibility.Collapsed
            };
            Border pFill = new Border
            {
                Height = 5,
                CornerRadius = new CornerRadius(2.5),
                Background = new SolidColorBrush(accent),
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 316 * (res.ProgressPercent / 100.0)
            };
            pTrack.Child = pFill;
            sp.Children.Add(pTrack);

            // Action row
            StackPanel actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 4, 0, 0)
            };
            Button editBtn = CreateSmallCardBtn("编辑与外观");
            editBtn.Click += delegate { OpenEditDialog(item); };

            Button dupBtn = CreateSmallCardBtn("复制");
            dupBtn.Click += delegate
            {
                EditItemDialog tmp = new EditItemDialog(item);
                CountdownItem copy = tmp.ResultItem;
                copy.Id = Guid.NewGuid().ToString("N");
                copy.Title = item.Title + " (副本)";
                copy.WidgetLeft = item.WidgetLeft + 30;
                copy.WidgetTop = item.WidgetTop + 30;
                _data.Items.Add(copy);
                StorageService.Save(_data);
                SyncWidgetForItem(copy);
                RefreshCountdownCards();
            };

            Button delBtn = CreateSmallCardBtn("删除");
            delBtn.Foreground = new SolidColorBrush(MediaColor.FromRgb(220, 38, 38));
            delBtn.Click += delegate
            {
                if (MessageBox.Show(string.Format("确定要删除「{0}」吗？", item.Title), "确认删除",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    CloseWidget(item.Id);
                    _data.Items.Remove(item);
                    StorageService.Save(_data);
                    RefreshCountdownCards();
                }
            };

            actions.Children.Add(editBtn);
            actions.Children.Add(dupBtn);
            actions.Children.Add(delBtn);
            sp.Children.Add(actions);

            card.Child = sp;

            _cardRefs[item.Id] = new CardUiRef
            {
                PrefixText = prefixTb,
                ValueText = valueTb,
                SubText = subTb,
                ProgressTrack = pTrack,
                ProgressFill = pFill
            };

            return card;
        }

        private Button CreateSmallCardBtn(string text)
        {
            return new Button
            {
                Content = text,
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(6, 0, 0, 0),
                Background = new SolidColorBrush(MediaColor.FromRgb(248, 250, 252)),
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(203, 213, 225)),
                FontSize = 12,
                Cursor = System.Windows.Input.Cursors.Hand
            };
        }

        private void OpenAddDialog()
        {
            EditItemDialog dlg = new EditItemDialog(null);
            dlg.Owner = IsVisible ? this : null;
            if (dlg.ShowDialog() == true)
            {
                CountdownItem newItem = dlg.ResultItem;
                // Offset position nicely
                newItem.WidgetLeft = 80 + (_data.Items.Count % 6) * 36;
                newItem.WidgetTop = 80 + (_data.Items.Count % 6) * 36;
                _data.Items.Add(newItem);
                StorageService.Save(_data);
                SyncWidgetForItem(newItem);
                RefreshCountdownCards();
            }
        }

        private void OpenEditDialog(CountdownItem item)
        {
            EditItemDialog dlg = new EditItemDialog(item);
            dlg.Owner = IsVisible ? this : null;
            if (dlg.ShowDialog() == true)
            {
                CountdownItem updated = dlg.ResultItem;
                int idx = _data.Items.IndexOf(item);
                if (idx >= 0)
                    _data.Items[idx] = updated;
                StorageService.Save(_data);
                CloseWidget(item.Id);
                SyncWidgetForItem(updated);
                RefreshCountdownCards();
            }
        }

        private void SyncAllWidgets()
        {
            List<string> existingIds = _widgets.Keys.ToList();
            foreach (string id in existingIds)
                CloseWidget(id);

            if (_data.Settings.WidgetsGloballyHidden)
                return;

            foreach (CountdownItem item in _data.Items)
            {
                if (item.ShowWidget)
                    SyncWidgetForItem(item);
            }
        }

        private void SyncWidgetForItem(CountdownItem item)
        {
            if (_data.Settings.WidgetsGloballyHidden || !item.ShowWidget)
            {
                CloseWidget(item.Id);
                return;
            }

            FloatingWidgetWindow win;
            if (_widgets.TryGetValue(item.Id, out win))
            {
                win.ApplyAppearance();
                win.ApplyWindowBehavior();
                win.UpdateTimeDisplay(DateTime.Now);
                return;
            }

            win = new FloatingWidgetWindow(item);
            win.OnEditRequested = delegate(CountdownItem target) { OpenEditDialog(target); };
            win.OnOpenMainRequested = delegate { RestoreMainWindow(); };
            win.OnChanged = delegate(CountdownItem target)
            {
                StorageService.Save(_data);
                RefreshCountdownCards();
            };
            win.Closed += delegate
            {
                if (_widgets.ContainsKey(item.Id))
                    _widgets.Remove(item.Id);
            };
            _widgets[item.Id] = win;
            win.Show();
        }

        private void CloseWidget(string itemId)
        {
            FloatingWidgetWindow win;
            if (_widgets.TryGetValue(itemId, out win))
            {
                _widgets.Remove(itemId);
                try { win.Close(); } catch { }
            }
        }

        private void ToggleGlobalWidgetsHidden()
        {
            _data.Settings.WidgetsGloballyHidden = !_data.Settings.WidgetsGloballyHidden;
            StorageService.Save(_data);
            SyncAllWidgets();
        }

        private void UnlockAllWidgets()
        {
            foreach (CountdownItem it in _data.Items)
            {
                it.IsLocked = false;
                it.ClickThrough = false;
            }
            StorageService.Save(_data);
            foreach (FloatingWidgetWindow win in _widgets.Values)
            {
                win.ApplyAppearance();
                win.ApplyWindowBehavior();
            }
            MessageBox.Show("已解除所有桌面悬浮窗的锁定与鼠标穿透状态。", "已解锁", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CascadeWidgets()
        {
            double x = 60, y = 60;
            foreach (CountdownItem it in _data.Items)
            {
                if (!it.ShowWidget) continue;
                it.WidgetLeft = x;
                it.WidgetTop = y;
                FloatingWidgetWindow win;
                if (_widgets.TryGetValue(it.Id, out win))
                {
                    win.Left = x;
                    win.Top = y;
                }
                y += 165;
                if (y > SystemParameters.WorkArea.Height - 180)
                {
                    y = 60;
                    x += 320;
                }
            }
            StorageService.Save(_data);
        }

        private void MainTimer_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            bool needSave = false;

            // 1. Update cards & floating widgets & check reminders
            foreach (CountdownItem item in _data.Items)
            {
                TimeDisplayResult res = TimeCalculator.Compute(item, now);

                CardUiRef cardRef;
                if (_cardRefs.TryGetValue(item.Id, out cardRef))
                {
                    cardRef.PrefixText.Text = res.PrefixLabel;
                    cardRef.ValueText.Text = res.MainValue;
                    cardRef.SubText.Text = string.IsNullOrEmpty(item.Note) ? res.SubText : string.Format("{0} · {1}", res.SubText, item.Note);
                    if (item.ShowProgressBar)
                        cardRef.ProgressFill.Width = 316 * (res.ProgressPercent / 100.0);
                }

                FloatingWidgetWindow win;
                if (_widgets.TryGetValue(item.Id, out win))
                {
                    win.UpdateTimeDisplay(now);
                }

                // Reminder checks
                if (item.EnableReminder && item.Mode == (int)CountMode.Countdown)
                {
                    DateTime target = item.GetTargetDate();
                    TimeSpan remaining = target - now;

                    if (item.RemindBeforeMinutes > 0 && !item.HasRemindedBefore
                        && remaining.TotalMinutes <= item.RemindBeforeMinutes && remaining.TotalSeconds > 0)
                    {
                        item.HasRemindedBefore = true;
                        needSave = true;
                        ShowReminder(item.Title, string.Format("距离目标时间还有约 {0} 分钟！", (int)Math.Ceiling(remaining.TotalMinutes)));
                    }

                    if (!item.HasRemindedExact && remaining.TotalSeconds <= 0 && remaining.TotalSeconds > -60)
                    {
                        item.HasRemindedExact = true;
                        needSave = true;
                        ShowReminder(item.Title, "倒计时已到期！");
                    }
                }
            }

            if (needSave)
                StorageService.Save(_data);

            // 2. Update Pomodoro / Stopwatch
            if (_timerRunning)
            {
                if (_isStopwatchMode)
                {
                    _stopwatchElapsedSeconds++;
                    UpdateTimerDisplay();
                }
                else
                {
                    _timerRemainingSeconds--;
                    UpdateTimerDisplay();
                    if (_timerRemainingSeconds <= 0)
                    {
                        _timerRunning = false;
                        _timerStartPauseBtn.Content = "开始";
                        RecordTimerSession(_timerLabelBox.Text, "番茄钟/倒计时", _timerInitialSeconds);
                        ShowReminder(_timerLabelBox.Text, string.Format("专注计时已完成（{0}）！", FormatSeconds(_timerInitialSeconds)));
                    }
                }
            }
        }

        private void ShowReminder(string title, string message)
        {
            ReminderPopupWindow popup = new ReminderPopupWindow(title, message, _data.Settings.PlaySoundOnReminder);
            popup.Show();
        }

        private void ExportCsvBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV 表格文件|*.csv",
                FileName = string.Format("DeskCountdown_导出_{0:yyyyMMdd_HHmm}.csv", DateTime.Now),
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory
            };
            if (sfd.ShowDialog() == true)
            {
                StorageService.ExportToCsv(_data, sfd.FileName);
                MessageBox.Show("CSV 数据导出成功！", "导出完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportJsonBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON 备份文件|*.json",
                FileName = string.Format("DeskCountdown_备份_{0:yyyyMMdd_HHmm}.json", DateTime.Now),
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory
            };
            if (sfd.ShowDialog() == true)
            {
                StorageService.ExportToJson(_data, sfd.FileName);
                MessageBox.Show("JSON 备份成功！", "备份完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ImportJsonBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON 备份文件|*.json",
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory
            };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    _data = StorageService.ImportFromJson(ofd.FileName);
                    StorageService.Save(_data);
                    SyncAllWidgets();
                    RefreshCountdownCards();
                    RefreshHistoryList();
                    RefreshSettingsState();
                    MessageBox.Show("数据恢复成功！", "恢复完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("导入失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SetupTrayIcon()
        {
            _trayIcon = new WinForms.NotifyIcon
            {
                Text = "DeskCountdown 桌面倒计时",
                Icon = CreateTrayIconBitmap(),
                Visible = true
            };

            WinForms.ContextMenuStrip menu = new WinForms.ContextMenuStrip();
            menu.Items.Add("打开主管理界面", null, delegate { RestoreMainWindow(); });
            menu.Items.Add("新建倒计时...", null, delegate { RestoreMainWindow(); OpenAddDialog(); });
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add("显示 / 隐藏全部悬浮窗", null, delegate { Dispatcher.Invoke(new Action(ToggleGlobalWidgetsHidden)); });
            menu.Items.Add("解锁全部悬浮窗", null, delegate { Dispatcher.Invoke(new Action(UnlockAllWidgets)); });
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add("退出程序", null, delegate { ExitApplication(); });

            _trayIcon.ContextMenuStrip = menu;
            _trayIcon.DoubleClick += delegate { RestoreMainWindow(); };
        }

        private System.Drawing.Icon CreateTrayIconBitmap()
        {
            using (Bitmap bmp = new Bitmap(32, 32))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);
                using (System.Drawing.Brush bg = new SolidBrush(System.Drawing.Color.FromArgb(37, 99, 235)))
                {
                    g.FillEllipse(bg, 2, 2, 28, 28);
                }
                using (System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.White, 2.5f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawLine(pen, 16, 16, 16, 8);
                    g.DrawLine(pen, 16, 16, 22, 19);
                }
                IntPtr hIcon = bmp.GetHicon();
                return System.Drawing.Icon.FromHandle(hIcon);
            }
        }

        public void RestoreMainWindow()
        {
            Dispatcher.Invoke(new Action(delegate
            {
                ShowInTaskbar = true;
                Show();
                WindowState = WindowState.Normal;
                Activate();
            }));
        }

        public void ExitApplication()
        {
            Dispatcher.Invoke(new Action(delegate
            {
                _forceExit = true;
                StorageService.Save(_data);
                foreach (FloatingWidgetWindow win in _widgets.Values.ToList())
                {
                    try { win.Close(); } catch { }
                }
                _widgets.Clear();
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                    _trayIcon = null;
                }
                Close();
                Application.Current.Shutdown();
            }));
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_forceExit && _data.Settings.MinimizeToTrayOnClose)
            {
                e.Cancel = true;
                Hide();
                ShowInTaskbar = false;
                return;
            }

            if (_mainTimer != null) _mainTimer.Stop();
            foreach (FloatingWidgetWindow win in _widgets.Values.ToList())
            {
                try { win.Close(); } catch { }
            }
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        }
    }
}
