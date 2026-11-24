using System;
using System.Collections.Generic;
using System.Globalization;

namespace DeskCountdown
{
    public enum CountMode
    {
        Countdown = 0,
        CountUp = 1
    }

    public enum DisplayFormat
    {
        DaysAndHms = 0,
        DaysOnly = 1,
        YearsMonthsDays = 2,
        WorkdaysOnly = 3
    }

    public enum WindowLayerMode
    {
        Topmost = 0,
        Normal = 1,
        DesktopBottom = 2
    }

    public class CountdownItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string Note { get; set; }
        public string TargetDateIso { get; set; }
        public string StartDateIso { get; set; }
        public int Mode { get; set; }
        public int DisplayFormat { get; set; }
        public bool ShowProgressBar { get; set; }

        // Reminder settings
        public bool EnableReminder { get; set; }
        public int RemindBeforeMinutes { get; set; }
        public bool HasRemindedBefore { get; set; }
        public bool HasRemindedExact { get; set; }

        // Floating widget settings
        public bool ShowWidget { get; set; }
        public bool IsLocked { get; set; }
        public bool ClickThrough { get; set; }
        public int LayerMode { get; set; }
        public double WidgetLeft { get; set; }
        public double WidgetTop { get; set; }
        public double WidgetWidth { get; set; }
        public double WidgetHeight { get; set; }

        // Appearance
        public string ThemeKey { get; set; }
        public string BackgroundHex { get; set; }
        public string BackgroundEndHex { get; set; }
        public string TextHex { get; set; }
        public string AccentHex { get; set; }
        public double Opacity { get; set; }
        public double FontSize { get; set; }
        public double CornerRadius { get; set; }
        public string BackgroundImagePath { get; set; }

        public CountdownItem()
        {
            Id = Guid.NewGuid().ToString("N");
            Title = "新倒计时";
            Category = "工作项目";
            Note = "";
            TargetDateIso = DateTime.Now.AddDays(30).Date.AddHours(9).ToString("o");
            StartDateIso = DateTime.Now.ToString("o");
            Mode = (int)CountMode.Countdown;
            DisplayFormat = (int)DeskCountdown.DisplayFormat.DaysAndHms;
            ShowProgressBar = true;
            EnableReminder = true;
            RemindBeforeMinutes = 60;
            ShowWidget = true;
            IsLocked = false;
            ClickThrough = false;
            LayerMode = (int)WindowLayerMode.Topmost;
            WidgetLeft = 100;
            WidgetTop = 100;
            WidgetWidth = 300;
            WidgetHeight = 148;
            ThemeKey = "Midnight";
            BackgroundHex = "#1E2235";
            BackgroundEndHex = "#141826";
            TextHex = "#FFFFFF";
            AccentHex = "#4F8CFF";
            Opacity = 0.92;
            FontSize = 28;
            CornerRadius = 14;
            BackgroundImagePath = "";
        }

        public DateTime GetTargetDate()
        {
            DateTime dt;
            if (DateTime.TryParse(TargetDateIso, null, DateTimeStyles.RoundtripKind, out dt))
            {
                if (dt.Kind == DateTimeKind.Utc)
                    return dt.ToLocalTime();
                return dt;
            }
            return DateTime.Now.AddDays(1);
        }

        public void SetTargetDate(DateTime dt)
        {
            TargetDateIso = DateTime.SpecifyKind(dt, DateTimeKind.Local).ToString("o");
        }

        public DateTime GetStartDate()
        {
            DateTime dt;
            if (DateTime.TryParse(StartDateIso, null, DateTimeStyles.RoundtripKind, out dt))
            {
                if (dt.Kind == DateTimeKind.Utc)
                    return dt.ToLocalTime();
                return dt;
            }
            return DateTime.Now;
        }

        public void SetStartDate(DateTime dt)
        {
            StartDateIso = DateTime.SpecifyKind(dt, DateTimeKind.Local).ToString("o");
        }
    }

    public class TimerRecord
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string Mode { get; set; }
        public int DurationSeconds { get; set; }
        public string CompletedAtIso { get; set; }

        public TimerRecord()
        {
            Id = Guid.NewGuid().ToString("N");
            Label = "专注计时";
            Mode = "倒计时";
            CompletedAtIso = DateTime.Now.ToString("o");
        }
    }

    public class AppSettings
    {
        public bool MinimizeToTrayOnClose { get; set; }
        public bool WidgetsGloballyHidden { get; set; }
        public string DefaultThemeKey { get; set; }
        public bool PlaySoundOnReminder { get; set; }

        public AppSettings()
        {
            MinimizeToTrayOnClose = true;
            WidgetsGloballyHidden = false;
            DefaultThemeKey = "Midnight";
            PlaySoundOnReminder = true;
        }
    }

    public class AppData
    {
        public List<CountdownItem> Items { get; set; }
        public List<TimerRecord> TimerHistory { get; set; }
        public AppSettings Settings { get; set; }

        public AppData()
        {
            Items = new List<CountdownItem>();
            TimerHistory = new List<TimerRecord>();
            Settings = new AppSettings();
        }
    }

    public class TimeDisplayResult
    {
        public string MainValue { get; set; }
        public string SubText { get; set; }
        public string PrefixLabel { get; set; }
        public bool IsExpired { get; set; }
        public double ProgressPercent { get; set; }
        public TimeSpan RawSpan { get; set; }
    }

    public static class TimeCalculator
    {
        public static TimeDisplayResult Compute(CountdownItem item, DateTime now)
        {
            DateTime target = item.GetTargetDate();
            CountMode mode = (CountMode)item.Mode;
            DisplayFormat fmt = (DisplayFormat)item.DisplayFormat;

            TimeDisplayResult res = new TimeDisplayResult();
            TimeSpan span;
            if (mode == CountMode.Countdown)
            {
                span = target - now;
                res.IsExpired = span.TotalSeconds <= 0;
                if (res.IsExpired)
                {
                    res.PrefixLabel = "已过去";
                    span = now - target;
                }
                else
                {
                    res.PrefixLabel = "距离目标还有";
                }
            }
            else
            {
                span = now - target;
                if (span.TotalSeconds < 0)
                {
                    res.PrefixLabel = "距离开始还有";
                    span = target - now;
                }
                else
                {
                    res.PrefixLabel = "已经过去";
                }
                res.IsExpired = false;
            }

            res.RawSpan = span;

            if (fmt == DisplayFormat.DaysOnly)
            {
                int days = (int)Math.Ceiling(span.TotalDays);
                if (mode == CountMode.CountUp || res.IsExpired)
                    days = (int)Math.Floor(span.TotalDays);
                res.MainValue = string.Format("{0} 天", days);
                res.SubText = string.Format("目标：{0:yyyy-MM-dd HH:mm}", target);
            }
            else if (fmt == DisplayFormat.WorkdaysOnly)
            {
                int workdays = CountWorkdays(
                    now < target ? now.Date : target.Date,
                    now < target ? target.Date : now.Date);
                res.MainValue = string.Format("{0} 工作日", workdays);
                res.SubText = string.Format("排除周末 · 共 {0} 自然日", (int)Math.Floor(span.TotalDays));
            }
            else if (fmt == DisplayFormat.YearsMonthsDays)
            {
                DateTime from = now < target ? now : target;
                DateTime to = now < target ? target : now;
                int years, months, days;
                ComputeCalendarDiff(from, to, out years, out months, out days);
                if (years > 0)
                    res.MainValue = string.Format("{0}年 {1}月 {2}天", years, months, days);
                else if (months > 0)
                    res.MainValue = string.Format("{0}个月 {1}天", months, days);
                else
                    res.MainValue = string.Format("{0}天 {1:D2}:{2:D2}:{3:D2}", span.Days, span.Hours, span.Minutes, span.Seconds);
                res.SubText = string.Format("{0:D2}时 {1:D2}分 {2:D2}秒", span.Hours, span.Minutes, span.Seconds);
            }
            else
            {
                // DaysAndHms
                if (span.Days > 0)
                {
                    res.MainValue = string.Format("{0}天 {1:D2}:{2:D2}:{3:D2}", span.Days, span.Hours, span.Minutes, span.Seconds);
                }
                else
                {
                    res.MainValue = string.Format("{0:D2}:{1:D2}:{2:D2}", span.Hours, span.Minutes, span.Seconds);
                }
                res.SubText = string.Format("目标：{0:yyyy-MM-dd HH:mm}", target);
            }

            // Progress calculation
            DateTime start = item.GetStartDate();
            if (mode == CountMode.Countdown && target > start)
            {
                double total = (target - start).TotalSeconds;
                double elapsed = (now - start).TotalSeconds;
                double pct = (elapsed / total) * 100.0;
                if (pct < 0) pct = 0;
                if (pct > 100) pct = 100;
                res.ProgressPercent = pct;
            }
            else
            {
                res.ProgressPercent = res.IsExpired ? 100.0 : 0.0;
            }

            return res;
        }

        public static int CountWorkdays(DateTime startDate, DateTime endDate)
        {
            if (endDate <= startDate)
                return 0;

            int count = 0;
            DateTime cur = startDate;
            while (cur < endDate)
            {
                if (cur.DayOfWeek != DayOfWeek.Saturday && cur.DayOfWeek != DayOfWeek.Sunday)
                    count++;
                cur = cur.AddDays(1);
            }
            return count;
        }

        public static void ComputeCalendarDiff(DateTime from, DateTime to, out int years, out int months, out int days)
        {
            DateTime f = from.Date;
            DateTime t = to.Date;
            years = t.Year - f.Year;
            months = t.Month - f.Month;
            days = t.Day - f.Day;

            if (days < 0)
            {
                months--;
                DateTime prevMonth = t.AddMonths(-1);
                days += DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
            }
            if (months < 0)
            {
                years--;
                months += 12;
            }
            if (years < 0)
            {
                years = 0;
                months = 0;
                days = 0;
            }
        }
    }
}
