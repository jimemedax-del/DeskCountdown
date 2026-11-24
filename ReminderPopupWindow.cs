using System;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace DeskCountdown
{
    public class ReminderPopupWindow : Window
    {
        private DispatcherTimer _autoCloseTimer;

        public ReminderPopupWindow(string title, string message, bool playSound)
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            Width = 340;
            Height = 136;

            Rect workArea = SystemParameters.WorkArea;
            Left = workArea.Right - Width - 18;
            Top = workArea.Bottom - Height - 18;

            Border card = new Border
            {
                Margin = new Thickness(8),
                Padding = new Thickness(16, 14, 16, 14),
                CornerRadius = new CornerRadius(12),
                Background = new LinearGradientBrush(
                    Color.FromRgb(28, 35, 58),
                    Color.FromRgb(18, 23, 38),
                    new Point(0, 0), new Point(1, 1)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(79, 140, 255)),
                BorderThickness = new Thickness(1.2),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 16,
                    ShadowDepth = 4,
                    Opacity = 0.45
                }
            };

            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            DockPanel top = new DockPanel();
            TextBlock badge = new TextBlock
            {
                Text = "⏰ 桌面倒计时提醒",
                Foreground = new SolidColorBrush(Color.FromRgb(96, 165, 250)),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(badge, Dock.Left);
            top.Children.Add(badge);

            Button closeBtn = new Button
            {
                Content = "✕",
                Width = 24,
                Height = 24,
                Background = Brushes.Transparent,
                Foreground = Brushes.LightGray,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            closeBtn.Click += delegate { Close(); };
            DockPanel.SetDock(closeBtn, Dock.Right);
            top.Children.Add(closeBtn);
            Grid.SetRow(top, 0);
            grid.Children.Add(top);

            StackPanel body = new StackPanel { Margin = new Thickness(0, 6, 0, 4) };
            body.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = Brushes.White,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            body.Children.Add(new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                FontSize = 12.5,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 3, 0, 0)
            });
            Grid.SetRow(body, 1);
            grid.Children.Add(body);

            TextBlock timeLbl = new TextBlock
            {
                Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(timeLbl, 2);
            grid.Children.Add(timeLbl);

            card.Child = grid;
            Content = card;

            if (playSound)
            {
                try { SystemSounds.Exclamation.Play(); } catch { }
            }

            _autoCloseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(12) };
            _autoCloseTimer.Tick += delegate
            {
                _autoCloseTimer.Stop();
                Close();
            };
            _autoCloseTimer.Start();
        }
    }
}
