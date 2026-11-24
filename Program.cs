using System;
using System.IO;
using System.Threading;
using System.Windows;

namespace DeskCountdown
{
    public static class Program
    {
        private static Mutex _singleInstanceMutex;

        [STAThread]
        public static int Main(string[] args)
        {
            bool runSelfTest = false;
            bool startMinimized = false;

            if (args != null)
            {
                foreach (string arg in args)
                {
                    if (string.Equals(arg, "--self-test", StringComparison.OrdinalIgnoreCase))
                        runSelfTest = true;
                    else if (string.Equals(arg, "--minimized", StringComparison.OrdinalIgnoreCase))
                        startMinimized = true;
                }
            }

            if (runSelfTest)
            {
                return RunSelfTests();
            }

            bool createdNew;
            _singleInstanceMutex = new Mutex(true, "DeskCountdown_Portable_SingleInstance_Mutex", out createdNew);
            if (!createdNew)
            {
                MessageBox.Show("DeskCountdown 已经在运行中，请检查右下角系统托盘图标。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return 0;
            }

            try
            {
                Application app = new Application();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                AppData data = StorageService.Load();
                StorageService.Save(data);

                MainWindow mainWin = new MainWindow(data, startMinimized);
                app.MainWindow = mainWin;
                if (!startMinimized)
                    mainWin.Show();

                return app.Run();
            }
            finally
            {
                if (_singleInstanceMutex != null)
                {
                    _singleInstanceMutex.ReleaseMutex();
                    _singleInstanceMutex.Dispose();
                }
            }
        }

        private static int RunSelfTests()
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "self_test_result.txt");
            try
            {
                // 1. Verify portable data path is NOT on C: drive
                string dataPath = StorageService.DataFilePath;
                if (dataPath.StartsWith("C:\\", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Data path unexpectedly points to C: drive: " + dataPath);

                // 2. Verify workday calculation (Mon 2026-11-23 to Mon 2026-11-30 = 5 workdays)
                int wd = TimeCalculator.CountWorkdays(new DateTime(2026, 11, 23), new DateTime(2026, 11, 30));
                if (wd != 5)
                    throw new Exception("Expected 5 workdays, got " + wd);

                // 3. Verify countdown & count-up formatting
                CountdownItem item = new CountdownItem { Title = "测试考试", DisplayFormat = (int)DisplayFormat.DaysAndHms };
                DateTime now = new DateTime(2026, 11, 24, 10, 0, 0);
                item.SetTargetDate(new DateTime(2026, 12, 4, 12, 30, 15));
                item.SetStartDate(new DateTime(2026, 11, 14, 10, 0, 0));
                TimeDisplayResult res = TimeCalculator.Compute(item, now);
                if (res.MainValue != "10天 02:30:15")
                    throw new Exception("Unexpected countdown value: " + res.MainValue);
                if (res.ProgressPercent < 40 || res.ProgressPercent > 60)
                    throw new Exception("Unexpected progress: " + res.ProgressPercent);

                // 4. Verify JSON save/load and CSV export inside workspace directory
                AppData data = StorageService.CreateDefaultData();
                data.TimerHistory.Add(new TimerRecord { Label = "单元测试番茄钟", Mode = "番茄钟", DurationSeconds = 1500 });
                string testJson = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_test_backup.json");
                string testCsv = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_test_export.csv");
                StorageService.ExportToJson(data, testJson);
                AppData loaded = StorageService.ImportFromJson(testJson);
                if (loaded.Items.Count != data.Items.Count || loaded.TimerHistory.Count != 1)
                    throw new Exception("JSON round-trip mismatch.");
                StorageService.ExportToCsv(data, testCsv);
                if (!File.Exists(testCsv) || new FileInfo(testCsv).Length == 0)
                    throw new Exception("CSV export failed.");
                File.Delete(testJson);
                File.Delete(testCsv);

                // 5. Verify WPF windows instantiate cleanly on STA thread
                Application app = new Application();
                FloatingWidgetWindow fw = new FloatingWidgetWindow(loaded.Items[0]);
                EditItemDialog dlg = new EditItemDialog(loaded.Items[0]);
                ReminderPopupWindow pop = new ReminderPopupWindow("自检提醒", "测试弹窗构造", false);
                MainWindow mw = new MainWindow(loaded, true);
                mw.ExitApplication();

                File.WriteAllText(logPath, "ALL_SELF_TESTS_PASSED\r\nDataPath=" + dataPath + "\r\nWorkdaysCheck=" + wd + "\r\nCountdownCheck=" + res.MainValue);
                return 0;
            }
            catch (Exception ex)
            {
                File.WriteAllText(logPath, "SELF_TEST_FAILED: " + ex.ToString());
                return 1;
            }
        }
    }
}
