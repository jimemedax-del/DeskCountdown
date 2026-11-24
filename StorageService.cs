using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace DeskCountdown
{
    public static class StorageService
    {
        private const string RegistryRunPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRegName = "DeskCountdownApp";

        public static string DataFilePath
        {
            get
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(baseDir, "countdown_data.json");
            }
        }

        public static AppData Load()
        {
            try
            {
                if (File.Exists(DataFilePath))
                {
                    string json = File.ReadAllText(DataFilePath, Encoding.UTF8);
                    JavaScriptSerializer ser = new JavaScriptSerializer();
                    ser.MaxJsonLength = int.MaxValue;
                    AppData data = ser.Deserialize<AppData>(json);
                    if (data != null)
                    {
                        if (data.Items == null) data.Items = new List<CountdownItem>();
                        if (data.TimerHistory == null) data.TimerHistory = new List<TimerRecord>();
                        if (data.Settings == null) data.Settings = new AppSettings();
                        return data;
                    }
                }
            }
            catch
            {
                // Fall back to default sample data if corrupted
            }

            return CreateDefaultData();
        }

        public static void Save(AppData data)
        {
            try
            {
                JavaScriptSerializer ser = new JavaScriptSerializer();
                ser.MaxJsonLength = int.MaxValue;
                string json = ser.Serialize(data);
                string tempPath = DataFilePath + ".tmp";
                File.WriteAllText(tempPath, json, Encoding.UTF8);
                if (File.Exists(DataFilePath))
                    File.Delete(DataFilePath);
                File.Move(tempPath, DataFilePath);
            }
            catch
            {
                // Ignore transient IO failures
            }
        }

        public static AppData CreateDefaultData()
        {
            AppData data = new AppData();
            DateTime now = DateTime.Now;

            DateTime nextNewYear = new DateTime(now.Year + 1, 1, 1, 0, 0, 0);
            CountdownItem item1 = new CountdownItem
            {
                Title = string.Format("{0} 年元旦", nextNewYear.Year),
                Category = "节日纪念",
                Note = "新年新气象，提前规划目标",
                Mode = (int)CountMode.Countdown,
                DisplayFormat = (int)DisplayFormat.DaysAndHms,
                ShowWidget = true,
                WidgetLeft = 60,
                WidgetTop = 60,
                ThemeKey = "Midnight",
                BackgroundHex = "#1E2235",
                BackgroundEndHex = "#141826",
                TextHex = "#FFFFFF",
                AccentHex = "#4F8CFF",
                Opacity = 0.92
            };
            item1.SetTargetDate(nextNewYear);
            item1.SetStartDate(new DateTime(now.Year, 1, 1, 0, 0, 0));

            DateTime projectDeadline = now.Date.AddDays(21).AddHours(18);
            CountdownItem item2 = new CountdownItem
            {
                Title = "项目里程碑交付",
                Category = "工作项目",
                Note = "按工作日排期推进",
                Mode = (int)CountMode.Countdown,
                DisplayFormat = (int)DisplayFormat.WorkdaysOnly,
                ShowWidget = false,
                WidgetLeft = 60,
                WidgetTop = 230,
                ThemeKey = "Emerald",
                BackgroundHex = "#0F3D3E",
                BackgroundEndHex = "#102A2B",
                TextHex = "#FFFFFF",
                AccentHex = "#2DD4BF",
                Opacity = 0.92
            };
            item2.SetTargetDate(projectDeadline);
            item2.SetStartDate(now);

            data.Items.Add(item1);
            data.Items.Add(item2);
            return data;
        }

        public static void ExportToCsv(AppData data, string filePath)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("类型,标题,分类,模式,目标时间,显示格式,备注");
            foreach (CountdownItem item in data.Items)
            {
                string modeStr = item.Mode == (int)CountMode.Countdown ? "倒计时" : "正计时";
                string fmtStr = FormatName((DisplayFormat)item.DisplayFormat);
                sb.AppendLine(string.Format("倒数日,{0},{1},{2},{3:yyyy-MM-dd HH:mm:ss},{4},{5}",
                    EscapeCsv(item.Title),
                    EscapeCsv(item.Category),
                    modeStr,
                    item.GetTargetDate(),
                    fmtStr,
                    EscapeCsv(item.Note)));
            }
            if (data.TimerHistory != null && data.TimerHistory.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("类型,标签,模式,时长(秒),完成时间");
                foreach (TimerRecord rec in data.TimerHistory)
                {
                    sb.AppendLine(string.Format("计时记录,{0},{1},{2},{3}",
                        EscapeCsv(rec.Label),
                        EscapeCsv(rec.Mode),
                        rec.DurationSeconds,
                        EscapeCsv(rec.CompletedAtIso)));
                }
            }
            // UTF-8 with BOM so Excel opens Chinese characters cleanly
            File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true));
        }

        public static void ExportToJson(AppData data, string filePath)
        {
            JavaScriptSerializer ser = new JavaScriptSerializer();
            ser.MaxJsonLength = int.MaxValue;
            File.WriteAllText(filePath, ser.Serialize(data), Encoding.UTF8);
        }

        public static AppData ImportFromJson(string filePath)
        {
            string json = File.ReadAllText(filePath, Encoding.UTF8);
            JavaScriptSerializer ser = new JavaScriptSerializer();
            ser.MaxJsonLength = int.MaxValue;
            AppData imported = ser.Deserialize<AppData>(json);
            if (imported == null || imported.Items == null)
                throw new InvalidDataException("JSON 文件格式不正确。");
            if (imported.TimerHistory == null) imported.TimerHistory = new List<TimerRecord>();
            if (imported.Settings == null) imported.Settings = new AppSettings();
            return imported;
        }

        public static bool IsAutoStartEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryRunPath, false))
                {
                    if (key == null) return false;
                    object val = key.GetValue(AppRegName);
                    return val != null && !string.IsNullOrEmpty(val.ToString());
                }
            }
            catch
            {
                return false;
            }
        }

        public static void SetAutoStart(bool enable)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryRunPath, true))
            {
                if (key == null) return;
                if (enable)
                {
                    string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    key.SetValue(AppRegName, string.Format("\"{0}\" --minimized", exePath));
                }
                else
                {
                    if (key.GetValue(AppRegName) != null)
                        key.DeleteValue(AppRegName, false);
                }
            }
        }

        private static string EscapeCsv(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            string s = input.Replace("\"", "\"\"");
            if (s.Contains(",") || s.Contains("\n") || s.Contains("\r") || s.Contains("\""))
                return "\"" + s + "\"";
            return s;
        }

        public static string FormatName(DisplayFormat fmt)
        {
            switch (fmt)
            {
                case DisplayFormat.DaysOnly: return "仅天数";
                case DisplayFormat.YearsMonthsDays: return "年月日";
                case DisplayFormat.WorkdaysOnly: return "工作日(排除周末)";
                default: return "天 + 时分秒";
            }
        }
    }
}
