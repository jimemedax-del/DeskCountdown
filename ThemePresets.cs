using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace DeskCountdown
{
    public class ThemePreset
    {
        public string Key { get; set; }
        public string DisplayName { get; set; }
        public string BackgroundHex { get; set; }
        public string BackgroundEndHex { get; set; }
        public string TextHex { get; set; }
        public string AccentHex { get; set; }
        public double DefaultOpacity { get; set; }
    }

    public static class ThemePresets
    {
        public static readonly List<ThemePreset> All = new List<ThemePreset>
        {
            new ThemePreset
            {
                Key = "Midnight",
                DisplayName = "极简暗夜",
                BackgroundHex = "#1E2235",
                BackgroundEndHex = "#141826",
                TextHex = "#FFFFFF",
                AccentHex = "#4F8CFF",
                DefaultOpacity = 0.92
            },
            new ThemePreset
            {
                Key = "SnowFrost",
                DisplayName = "磨砂雪白",
                BackgroundHex = "#F8FAFC",
                BackgroundEndHex = "#E2E8F0",
                TextHex = "#0F172A",
                AccentHex = "#2563EB",
                DefaultOpacity = 0.94
            },
            new ThemePreset
            {
                Key = "CoralSunset",
                DisplayName = "活力珊瑚",
                BackgroundHex = "#E11D48",
                BackgroundEndHex = "#F97316",
                TextHex = "#FFFFFF",
                AccentHex = "#FDE047",
                DefaultOpacity = 0.92
            },
            new ThemePreset
            {
                Key = "Emerald",
                DisplayName = "翡翠森林",
                BackgroundHex = "#0F3D3E",
                BackgroundEndHex = "#102A2B",
                TextHex = "#FFFFFF",
                AccentHex = "#2DD4BF",
                DefaultOpacity = 0.92
            },
            new ThemePreset
            {
                Key = "AuroraPurple",
                DisplayName = "极光紫韵",
                BackgroundHex = "#312E81",
                BackgroundEndHex = "#581C87",
                TextHex = "#FFFFFF",
                AccentHex = "#C084FC",
                DefaultOpacity = 0.92
            },
            new ThemePreset
            {
                Key = "TransparentMinimal",
                DisplayName = "透明极简(融入壁纸)",
                BackgroundHex = "#00000000",
                BackgroundEndHex = "#00000000",
                TextHex = "#FFFFFF",
                AccentHex = "#38BDF8",
                DefaultOpacity = 1.0
            }
        };

        public static ThemePreset Find(string key)
        {
            foreach (ThemePreset p in All)
            {
                if (string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase))
                    return p;
            }
            return All[0];
        }

        public static Color ParseColor(string hex, Color fallback)
        {
            try
            {
                if (!string.IsNullOrEmpty(hex))
                {
                    object obj = ColorConverter.ConvertFromString(hex);
                    if (obj is Color)
                        return (Color)obj;
                }
            }
            catch
            {
            }
            return fallback;
        }

        public static SolidColorBrush CreateBrush(string hex, Color fallback)
        {
            return new SolidColorBrush(ParseColor(hex, fallback));
        }
    }
}
