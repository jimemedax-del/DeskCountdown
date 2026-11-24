using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DeskCountdown
{
    public class EditItemDialog : Window
    {
        public CountdownItem ResultItem { get; private set; }

        private TextBox _titleBox;
        private ComboBox _categoryBox;
        private TextBox _noteBox;
        private ComboBox _modeBox;
        private DatePicker _targetDatePicker;
        private TextBox _targetTimeBox;
        private DatePicker _startDatePicker;
        private ComboBox _formatBox;
        private CheckBox _showProgressCheck;

        private CheckBox _enableReminderCheck;
        private ComboBox _remindBeforeBox;

        private CheckBox _showWidgetCheck;
        private ComboBox _layerModeBox;
        private CheckBox _lockCheck;
        private CheckBox _clickThroughCheck;

        private ComboBox _themeBox;
        private TextBox _bgHexBox;
        private TextBox _bgEndHexBox;
        private TextBox _textHexBox;
        private TextBox _accentHexBox;
        private Slider _opacitySlider;
        private TextBlock _opacityValueText;
        private Slider _fontSizeSlider;
        private TextBlock _fontSizeValueText;
        private Slider _radiusSlider;
        private TextBox _bgImageBox;

        private bool _isInitializing;

        public EditItemDialog(CountdownItem existing)
        {
            _isInitializing = true;
            bool isNew = (existing == null);
            ResultItem = existing != null ? CloneItem(existing) : new CountdownItem();

            Title = isNew ? "新建倒计时 / 正计时" : "编辑倒计时与悬浮窗外观";
            Width = 600;
            Height = 680;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(245, 247, 251));
            FontFamily = new FontFamily("Microsoft YaHei UI, Segoe UI");

            BuildUi();
            PopulateFields();
            _isInitializing = false;
        }

        private CountdownItem CloneItem(CountdownItem src)
        {
            return new CountdownItem
            {
                Id = src.Id,
                Title = src.Title,
                Category = src.Category,
                Note = src.Note,
                TargetDateIso = src.TargetDateIso,
                StartDateIso = src.StartDateIso,
                Mode = src.Mode,
                DisplayFormat = src.DisplayFormat,
                ShowProgressBar = src.ShowProgressBar,
                EnableReminder = src.EnableReminder,
                RemindBeforeMinutes = src.RemindBeforeMinutes,
                HasRemindedBefore = src.HasRemindedBefore,
                HasRemindedExact = src.HasRemindedExact,
                ShowWidget = src.ShowWidget,
                IsLocked = src.IsLocked,
                ClickThrough = src.ClickThrough,
                LayerMode = src.LayerMode,
                WidgetLeft = src.WidgetLeft,
                WidgetTop = src.WidgetTop,
                WidgetWidth = src.WidgetWidth,
                WidgetHeight = src.WidgetHeight,
                ThemeKey = src.ThemeKey,
                BackgroundHex = src.BackgroundHex,
                BackgroundEndHex = src.BackgroundEndHex,
                TextHex = src.TextHex,
                AccentHex = src.AccentHex,
                Opacity = src.Opacity,
                FontSize = src.FontSize,
                CornerRadius = src.CornerRadius,
                BackgroundImagePath = src.BackgroundImagePath
            };
        }

        private void BuildUi()
        {
            DockPanel root = new DockPanel { Margin = new Thickness(16) };

            // Bottom buttons
            StackPanel btnBar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            };
            Button cancelBtn = new Button
            {
                Content = "取消",
                Width = 90,
                Height = 34,
                Margin = new Thickness(0, 0, 10, 0),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225))
            };
            cancelBtn.Click += delegate { DialogResult = false; Close(); };

            Button saveBtn = new Button
            {
                Content = "保存并应用",
                Width = 110,
                Height = 34,
                Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.SemiBold
            };
            saveBtn.Click += SaveBtn_Click;

            btnBar.Children.Add(cancelBtn);
            btnBar.Children.Add(saveBtn);
            DockPanel.SetDock(btnBar, Dock.Bottom);
            root.Children.Add(btnBar);

            ScrollViewer scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            StackPanel content = new StackPanel();

            // Quick Templates
            content.Children.Add(CreateSectionHeader("快捷模板（一键填充）"));
            WrapPanel tplPanel = new WrapPanel { Margin = new Thickness(0, 4, 0, 10) };
            AddTemplateBtn(tplPanel, "高考", "考试备考", NextMonthDay(6, 7, 9, 0));
            AddTemplateBtn(tplPanel, "考研", "考试备考", NextMonthDay(12, 20, 8, 30));
            AddTemplateBtn(tplPanel, "元旦", "节日纪念", NextMonthDay(1, 1, 0, 0));
            AddTemplateBtn(tplPanel, "国庆节", "节日纪念", NextMonthDay(10, 1, 0, 0));
            AddTemplateBtn(tplPanel, "今日下班", "工作项目", DateTime.Now.Date.AddHours(18));
            AddTemplateBtn(tplPanel, "项目交付(30天)", "工作项目", DateTime.Now.Date.AddDays(30).AddHours(18));
            content.Children.Add(tplPanel);

            // Basic Info
            content.Children.Add(CreateSectionHeader("基本信息与计时规则"));
            Grid basicGrid = new Grid { Margin = new Thickness(0, 6, 0, 12) };
            basicGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            basicGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 7; i++)
                basicGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _titleBox = new TextBox { Height = 30, VerticalContentAlignment = VerticalAlignment.Center, Padding = new Thickness(6, 0, 6, 0) };
            AddRow(basicGrid, 0, "事项标题：", _titleBox);

            _categoryBox = new ComboBox { Height = 30, IsEditable = true };
            _categoryBox.Items.Add("工作项目");
            _categoryBox.Items.Add("考试备考");
            _categoryBox.Items.Add("节日纪念");
            _categoryBox.Items.Add("生活日常");
            _categoryBox.Items.Add("演讲汇报");
            AddRow(basicGrid, 1, "分类标签：", _categoryBox);

            _modeBox = new ComboBox { Height = 30 };
            _modeBox.Items.Add("倒计时（距离目标还有）");
            _modeBox.Items.Add("正计时（从起始日已过）");
            AddRow(basicGrid, 2, "计时模式：", _modeBox);

            StackPanel targetPanel = new StackPanel { Orientation = Orientation.Horizontal };
            _targetDatePicker = new DatePicker { Width = 170, Height = 30 };
            _targetTimeBox = new TextBox { Width = 110, Height = 30, Margin = new Thickness(8, 0, 0, 0), VerticalContentAlignment = VerticalAlignment.Center, Padding = new Thickness(6, 0, 6, 0), ToolTip = "格式 HH:mm:ss" };
            targetPanel.Children.Add(_targetDatePicker);
            targetPanel.Children.Add(_targetTimeBox);
            targetPanel.Children.Add(new TextBlock { Text = " (时:分:秒)", VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.Gray, FontSize = 11 });
            AddRow(basicGrid, 3, "目标时间：", targetPanel);

            _formatBox = new ComboBox { Height = 30 };
            _formatBox.Items.Add("天 + 时分秒（精准）");
            _formatBox.Items.Add("仅天数");
            _formatBox.Items.Add("年 / 月 / 日");
            _formatBox.Items.Add("仅工作日（自动排除周末）");
            AddRow(basicGrid, 4, "显示格式：", _formatBox);

            StackPanel progPanel = new StackPanel { Orientation = Orientation.Horizontal };
            _startDatePicker = new DatePicker { Width = 150, Height = 30 };
            _showProgressCheck = new CheckBox { Content = "显示进度条", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) };
            progPanel.Children.Add(_startDatePicker);
            progPanel.Children.Add(_showProgressCheck);
            AddRow(basicGrid, 5, "起始日期：", progPanel);

            _noteBox = new TextBox { Height = 30, VerticalContentAlignment = VerticalAlignment.Center, Padding = new Thickness(6, 0, 6, 0) };
            AddRow(basicGrid, 6, "备注说明：", _noteBox);

            content.Children.Add(basicGrid);

            // Reminder
            content.Children.Add(CreateSectionHeader("到期提醒设置"));
            StackPanel remPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 12) };
            _enableReminderCheck = new CheckBox { Content = "启用到期弹窗提醒", VerticalAlignment = VerticalAlignment.Center };
            _remindBeforeBox = new ComboBox { Width = 160, Height = 28, Margin = new Thickness(16, 0, 0, 0) };
            _remindBeforeBox.Items.Add("仅到期时提醒");
            _remindBeforeBox.Items.Add("提前 10 分钟提醒");
            _remindBeforeBox.Items.Add("提前 1 小时提醒");
            _remindBeforeBox.Items.Add("提前 1 天提醒");
            remPanel.Children.Add(_enableReminderCheck);
            remPanel.Children.Add(_remindBeforeBox);
            content.Children.Add(remPanel);

            // Floating Widget & Appearance
            content.Children.Add(CreateSectionHeader("桌面悬浮窗与外观定制"));
            Grid styleGrid = new Grid { Margin = new Thickness(0, 6, 0, 12) };
            styleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            styleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 8; i++)
                styleGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            StackPanel widgetFlags = new StackPanel { Orientation = Orientation.Horizontal };
            _showWidgetCheck = new CheckBox { Content = "开启桌面悬浮窗", Margin = new Thickness(0, 0, 14, 0) };
            _lockCheck = new CheckBox { Content = "锁定位置", Margin = new Thickness(0, 0, 14, 0) };
            _clickThroughCheck = new CheckBox { Content = "鼠标穿透" };
            widgetFlags.Children.Add(_showWidgetCheck);
            widgetFlags.Children.Add(_lockCheck);
            widgetFlags.Children.Add(_clickThroughCheck);
            AddRow(styleGrid, 0, "悬浮窗开关：", widgetFlags);

            _layerModeBox = new ComboBox { Height = 30 };
            _layerModeBox.Items.Add("置顶显示（始终在最前）");
            _layerModeBox.Items.Add("普通窗口层级");
            _layerModeBox.Items.Add("嵌入桌面底层（不遮挡办公窗口）");
            AddRow(styleGrid, 1, "窗口层级：", _layerModeBox);

            _themeBox = new ComboBox { Height = 30 };
            foreach (ThemePreset p in ThemePresets.All)
                _themeBox.Items.Add(p.DisplayName);
            _themeBox.SelectionChanged += ThemeBox_SelectionChanged;
            AddRow(styleGrid, 2, "主题皮肤：", _themeBox);

            // Colors
            StackPanel colorRow1 = new StackPanel { Orientation = Orientation.Horizontal };
            _bgHexBox = CreateColorPickerBox(colorRow1, "背景起点");
            _bgEndHexBox = CreateColorPickerBox(colorRow1, "背景终点");
            AddRow(styleGrid, 3, "背景渐变色：", colorRow1);

            StackPanel colorRow2 = new StackPanel { Orientation = Orientation.Horizontal };
            _textHexBox = CreateColorPickerBox(colorRow2, "文字颜色");
            _accentHexBox = CreateColorPickerBox(colorRow2, "数字强调色");
            AddRow(styleGrid, 4, "文字与强调：", colorRow2);

            // Opacity
            DockPanel opPanel = new DockPanel();
            _opacityValueText = new TextBlock { Width = 44, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right };
            DockPanel.SetDock(_opacityValueText, Dock.Right);
            _opacitySlider = new Slider { Minimum = 0.15, Maximum = 1.0, TickFrequency = 0.05, VerticalAlignment = VerticalAlignment.Center };
            _opacitySlider.ValueChanged += delegate { _opacityValueText.Text = string.Format("{0:0}%", _opacitySlider.Value * 100); };
            opPanel.Children.Add(_opacityValueText);
            opPanel.Children.Add(_opacitySlider);
            AddRow(styleGrid, 5, "窗口透明度：", opPanel);

            // Font size + Corner radius
            StackPanel sizePanel = new StackPanel { Orientation = Orientation.Horizontal };
            _fontSizeSlider = new Slider { Minimum = 16, Maximum = 52, Width = 140, VerticalAlignment = VerticalAlignment.Center };
            _fontSizeValueText = new TextBlock { Width = 36, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 12, 0) };
            _fontSizeSlider.ValueChanged += delegate { _fontSizeValueText.Text = string.Format("{0:0}pt", _fontSizeSlider.Value); };
            sizePanel.Children.Add(_fontSizeSlider);
            sizePanel.Children.Add(_fontSizeValueText);
            sizePanel.Children.Add(new TextBlock { Text = "圆角：", VerticalAlignment = VerticalAlignment.Center });
            _radiusSlider = new Slider { Minimum = 0, Maximum = 28, Width = 110, VerticalAlignment = VerticalAlignment.Center };
            sizePanel.Children.Add(_radiusSlider);
            AddRow(styleGrid, 6, "字号与圆角：", sizePanel);

            // Background image
            DockPanel imgPanel = new DockPanel();
            Button clearImgBtn = new Button { Content = "清除", Width = 48, Height = 28, Margin = new Thickness(6, 0, 0, 0) };
            clearImgBtn.Click += delegate { _bgImageBox.Text = ""; };
            DockPanel.SetDock(clearImgBtn, Dock.Right);
            Button browseImgBtn = new Button { Content = "选择图片", Width = 70, Height = 28, Margin = new Thickness(6, 0, 0, 0) };
            browseImgBtn.Click += BrowseImgBtn_Click;
            DockPanel.SetDock(browseImgBtn, Dock.Right);
            _bgImageBox = new TextBox { Height = 28, VerticalContentAlignment = VerticalAlignment.Center, Padding = new Thickness(4, 0, 4, 0) };
            imgPanel.Children.Add(clearImgBtn);
            imgPanel.Children.Add(browseImgBtn);
            imgPanel.Children.Add(_bgImageBox);
            AddRow(styleGrid, 7, "背景图片：", imgPanel);

            content.Children.Add(styleGrid);
            scroll.Content = content;
            root.Children.Add(scroll);
            Content = root;
        }

        private TextBox CreateColorPickerBox(StackPanel parent, string tooltip)
        {
            TextBox tb = new TextBox
            {
                Width = 92,
                Height = 28,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(4, 0, 4, 0),
                ToolTip = tooltip
            };
            Button pick = new Button
            {
                Content = "选色",
                Width = 44,
                Height = 28,
                Margin = new Thickness(4, 0, 14, 0)
            };
            pick.Click += delegate
            {
                System.Windows.Forms.ColorDialog dlg = new System.Windows.Forms.ColorDialog();
                Color cur = ThemePresets.ParseColor(tb.Text, Colors.SteelBlue);
                dlg.Color = System.Drawing.Color.FromArgb(cur.A, cur.R, cur.G, cur.B);
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    tb.Text = string.Format("#{0:X2}{1:X2}{2:X2}", dlg.Color.R, dlg.Color.G, dlg.Color.B);
                }
            };
            parent.Children.Add(tb);
            parent.Children.Add(pick);
            return tb;
        }

        private void AddTemplateBtn(WrapPanel panel, string title, string category, DateTime dt)
        {
            Button b = new Button
            {
                Content = title,
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 6, 6),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            b.Click += delegate
            {
                _titleBox.Text = title;
                _categoryBox.Text = category;
                _targetDatePicker.SelectedDate = dt.Date;
                _targetTimeBox.Text = dt.ToString("HH:mm:ss");
                _modeBox.SelectedIndex = 0;
            };
            panel.Children.Add(b);
        }

        private DateTime NextMonthDay(int month, int day, int hour, int minute)
        {
            DateTime now = DateTime.Now;
            DateTime candidate = new DateTime(now.Year, month, day, hour, minute, 0);
            if (candidate <= now)
                candidate = candidate.AddYears(1);
            return candidate;
        }

        private TextBlock CreateSectionHeader(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                Margin = new Thickness(0, 6, 0, 2)
            };
        }

        private void AddRow(Grid grid, int row, string label, UIElement ctrl)
        {
            TextBlock lbl = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                Margin = new Thickness(0, 4, 6, 4)
            };
            Grid.SetRow(lbl, row);
            Grid.SetColumn(lbl, 0);
            grid.Children.Add(lbl);

            FrameworkElement fe = ctrl as FrameworkElement;
            if (fe != null) fe.Margin = new Thickness(0, 4, 0, 4);
            Grid.SetRow(ctrl, row);
            Grid.SetColumn(ctrl, 1);
            grid.Children.Add(ctrl);
        }

        private void PopulateFields()
        {
            _titleBox.Text = ResultItem.Title;
            _categoryBox.Text = ResultItem.Category;
            _noteBox.Text = ResultItem.Note;
            _modeBox.SelectedIndex = ResultItem.Mode == (int)CountMode.CountUp ? 1 : 0;

            DateTime target = ResultItem.GetTargetDate();
            _targetDatePicker.SelectedDate = target.Date;
            _targetTimeBox.Text = target.ToString("HH:mm:ss");

            _startDatePicker.SelectedDate = ResultItem.GetStartDate().Date;
            _formatBox.SelectedIndex = Math.Max(0, Math.Min(3, ResultItem.DisplayFormat));
            _showProgressCheck.IsChecked = ResultItem.ShowProgressBar;

            _enableReminderCheck.IsChecked = ResultItem.EnableReminder;
            if (ResultItem.RemindBeforeMinutes >= 1440) _remindBeforeBox.SelectedIndex = 3;
            else if (ResultItem.RemindBeforeMinutes >= 60) _remindBeforeBox.SelectedIndex = 2;
            else if (ResultItem.RemindBeforeMinutes >= 10) _remindBeforeBox.SelectedIndex = 1;
            else _remindBeforeBox.SelectedIndex = 0;

            _showWidgetCheck.IsChecked = ResultItem.ShowWidget;
            _lockCheck.IsChecked = ResultItem.IsLocked;
            _clickThroughCheck.IsChecked = ResultItem.ClickThrough;
            _layerModeBox.SelectedIndex = Math.Max(0, Math.Min(2, ResultItem.LayerMode));

            int themeIdx = 0;
            for (int i = 0; i < ThemePresets.All.Count; i++)
            {
                if (string.Equals(ThemePresets.All[i].Key, ResultItem.ThemeKey, StringComparison.OrdinalIgnoreCase))
                {
                    themeIdx = i;
                    break;
                }
            }
            _themeBox.SelectedIndex = themeIdx;

            _bgHexBox.Text = ResultItem.BackgroundHex;
            _bgEndHexBox.Text = ResultItem.BackgroundEndHex;
            _textHexBox.Text = ResultItem.TextHex;
            _accentHexBox.Text = ResultItem.AccentHex;
            _opacitySlider.Value = ResultItem.Opacity;
            _opacityValueText.Text = string.Format("{0:0}%", ResultItem.Opacity * 100);
            _fontSizeSlider.Value = ResultItem.FontSize;
            _fontSizeValueText.Text = string.Format("{0:0}pt", ResultItem.FontSize);
            _radiusSlider.Value = ResultItem.CornerRadius;
            _bgImageBox.Text = ResultItem.BackgroundImagePath ?? "";
        }

        private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _themeBox.SelectedIndex < 0) return;
            ThemePreset preset = ThemePresets.All[_themeBox.SelectedIndex];
            _bgHexBox.Text = preset.BackgroundHex;
            _bgEndHexBox.Text = preset.BackgroundEndHex;
            _textHexBox.Text = preset.TextHex;
            _accentHexBox.Text = preset.AccentHex;
            _opacitySlider.Value = preset.DefaultOpacity;
        }

        private void BrowseImgBtn_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp"
            };
            if (ofd.ShowDialog() == true)
            {
                _bgImageBox.Text = ofd.FileName;
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            string title = (_titleBox.Text ?? "").Trim();
            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("请输入事项标题。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DateTime datePart = _targetDatePicker.SelectedDate.HasValue ? _targetDatePicker.SelectedDate.Value.Date : DateTime.Now.Date;
            TimeSpan timePart = TimeSpan.Zero;
            TimeSpan.TryParse(_targetTimeBox.Text, out timePart);
            DateTime targetDt = datePart.Add(timePart);

            DateTime startDt = _startDatePicker.SelectedDate.HasValue ? _startDatePicker.SelectedDate.Value.Date : DateTime.Now.Date;

            ResultItem.Title = title;
            ResultItem.Category = string.IsNullOrWhiteSpace(_categoryBox.Text) ? "工作项目" : _categoryBox.Text.Trim();
            ResultItem.Note = (_noteBox.Text ?? "").Trim();
            ResultItem.Mode = _modeBox.SelectedIndex == 1 ? (int)CountMode.CountUp : (int)CountMode.Countdown;
            ResultItem.SetTargetDate(targetDt);
            ResultItem.SetStartDate(startDt);
            ResultItem.DisplayFormat = Math.Max(0, _formatBox.SelectedIndex);
            ResultItem.ShowProgressBar = _showProgressCheck.IsChecked == true;

            ResultItem.EnableReminder = _enableReminderCheck.IsChecked == true;
            if (_remindBeforeBox.SelectedIndex == 3) ResultItem.RemindBeforeMinutes = 1440;
            else if (_remindBeforeBox.SelectedIndex == 2) ResultItem.RemindBeforeMinutes = 60;
            else if (_remindBeforeBox.SelectedIndex == 1) ResultItem.RemindBeforeMinutes = 10;
            else ResultItem.RemindBeforeMinutes = 0;

            if (targetDt > DateTime.Now)
            {
                ResultItem.HasRemindedBefore = false;
                ResultItem.HasRemindedExact = false;
            }

            ResultItem.ShowWidget = _showWidgetCheck.IsChecked == true;
            ResultItem.IsLocked = _lockCheck.IsChecked == true;
            ResultItem.ClickThrough = _clickThroughCheck.IsChecked == true;
            ResultItem.LayerMode = Math.Max(0, _layerModeBox.SelectedIndex);

            if (_themeBox.SelectedIndex >= 0 && _themeBox.SelectedIndex < ThemePresets.All.Count)
                ResultItem.ThemeKey = ThemePresets.All[_themeBox.SelectedIndex].Key;

            ResultItem.BackgroundHex = _bgHexBox.Text.Trim();
            ResultItem.BackgroundEndHex = _bgEndHexBox.Text.Trim();
            ResultItem.TextHex = _textHexBox.Text.Trim();
            ResultItem.AccentHex = _accentHexBox.Text.Trim();
            ResultItem.Opacity = _opacitySlider.Value;
            ResultItem.FontSize = _fontSizeSlider.Value;
            ResultItem.CornerRadius = _radiusSlider.Value;
            ResultItem.BackgroundImagePath = _bgImageBox.Text.Trim();

            DialogResult = true;
            Close();
        }
    }
}
