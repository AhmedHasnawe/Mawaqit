using System;
using System.IO;
using System.Xml.Serialization;

namespace Mawaqit.Core
{
    /// <summary>طريقة إبراز الصلاة</summary>
    public enum HighlightMode
    {
        /// <summary>الصلاة القادمة</summary>
        Next,
        /// <summary>الأقرب زمنياً (ماضية كانت أم قادمة)</summary>
        Nearest
    }

    public enum NumeralStyle { Arabic, Western, Roman, None }

    /// <summary>كم موقتاً يتنقّل بينها العرض</summary>
    public enum PrayerSet
    {
        /// <summary>الفجر والظهر والعصر والمغرب والعشاء</summary>
        Five,
        /// <summary>الفجر والظهر والمغرب، على الجمع بين الظهرين وبين العشاءين</summary>
        Three
    }

    /// <summary>
    /// كل إعدادات البرنامج. تُحفظ بصيغة XML في
    /// %AppData%\Mawaqit\settings.xml
    /// </summary>
    [Serializable]
    public class AppSettings
    {
        // ----- الموقع -----
        public string CityName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string TimeZoneId { get; set; }
        public double FallbackOffset { get; set; }
        public bool UseSystemTimeZone { get; set; }
        public double Elevation { get; set; }

        // ----- الحساب -----
        public string MethodId { get; set; }
        public AsrJuristic Asr { get; set; }
        public HighLatRule HighLat { get; set; }
        public int[] Tune { get; set; }

        // ----- المظهر -----
        public string ColorBackground { get; set; }
        public string ColorFace { get; set; }
        public string ColorRim { get; set; }
        public string ColorTicks { get; set; }
        public string ColorNumerals { get; set; }
        public string ColorHourHand { get; set; }
        public string ColorMinuteHand { get; set; }
        public string ColorSecondHand { get; set; }
        public string ColorText { get; set; }
        public string ColorHighlight { get; set; }
        /// <summary>لون كل صلاة: الفجر، الظهر، العصر، المغرب، العشاء</summary>
        public string[] ColorPrayers { get; set; }

        public AppLanguage Language { get; set; }
        public NumeralStyle Numerals { get; set; }
        public string FontFamily { get; set; }
        public double Opacity { get; set; }
        public double WindowSize { get; set; }
        public double WindowLeft { get; set; }
        public double WindowTop { get; set; }
        public bool HasPosition { get; set; }

        // ----- السلوك -----
        public bool AlwaysOnTop { get; set; }
        public bool ShowSeconds { get; set; }
        public bool SmoothSeconds { get; set; }
        public bool ShowPrayerHands { get; set; }
        public bool ShowRemainingArc { get; set; }
        public bool ShowDigitalPanel { get; set; }
        public bool Use24Hour { get; set; }
        public bool ShowInTaskbar { get; set; }
        public bool NotifyOnPrayer { get; set; }
        public bool PlaySoundOnPrayer { get; set; }
        public HighlightMode Highlight { get; set; }
        /// <summary>المواقيت التي تُعرض ويتنقّل بينها المستخدم</summary>
        public PrayerSet Prayers { get; set; }
        public bool StartWithWindows { get; set; }
        public bool ClickThrough { get; set; }
        public bool LockPosition { get; set; }
        /// <summary>بعد تصفّح الصلوات يدوياً، يعود العرض إلى الصلاة القادمة</summary>
        public bool AutoReturnToNext { get; set; }

        public AppSettings()
        {
            var samawah = CityDatabase.BuiltIn[0];
            CityName = samawah.NameAr;
            Latitude = samawah.Latitude;
            Longitude = samawah.Longitude;
            TimeZoneId = samawah.TimeZoneId;
            FallbackOffset = samawah.FallbackOffset;
            UseSystemTimeZone = true;
            Elevation = 0;

            MethodId = "Jafari";
            Asr = AsrJuristic.Standard;
            HighLat = HighLatRule.AngleBased;
            Tune = new int[6];

            ApplyTheme(ThemePresets.All[0]);

            Language = L.FromSystem();
            Numerals = Language == AppLanguage.Arabic ? NumeralStyle.Arabic : NumeralStyle.Western;
            FontFamily = "Segoe UI";
            Opacity = 1.0;
            WindowSize = 360;
            HasPosition = false;

            AlwaysOnTop = true;
            ShowSeconds = true;
            SmoothSeconds = false;
            ShowPrayerHands = true;
            ShowRemainingArc = true;
            ShowDigitalPanel = true;
            Use24Hour = false;
            ShowInTaskbar = false;
            NotifyOnPrayer = true;
            PlaySoundOnPrayer = false;
            Highlight = HighlightMode.Next;
            Prayers = PrayerSet.Five;
            StartWithWindows = false;
            ClickThrough = false;
            LockPosition = false;
            AutoReturnToNext = true;
        }

        public void ApplyTheme(Theme t)
        {
            ColorBackground = t.Background;
            ColorFace = t.Face;
            ColorRim = t.Rim;
            ColorTicks = t.Ticks;
            ColorNumerals = t.Numerals;
            ColorHourHand = t.HourHand;
            ColorMinuteHand = t.MinuteHand;
            ColorSecondHand = t.SecondHand;
            ColorText = t.Text;
            ColorHighlight = t.Highlight;
            ColorPrayers = (string[])t.Prayers.Clone();
        }

        public CalcMethod Method { get { return CalcMethod.ById(MethodId); } }

        /// <summary>
        /// اسم المدينة كما يُعرض. المحفوظ دائماً هو الاسم العربي لأنه المفتاح
        /// في قاعدة المدن، فيُترجم هنا عند العرض. والمواقع اليدوية تُعرض كما
        /// كتبها المستخدم.
        /// </summary>
        public string DisplayCityName
        {
            get
            {
                var c = CityDatabase.Find(CityName);
                return c != null ? c.Name : (CityName ?? "");
            }
        }

        /// <summary>المواقيت المعروضة حالياً، بترتيبها خلال اليوم</summary>
        public TimeId[] ActivePrayers
        {
            get
            {
                return Prayers == PrayerSet.Three
                    ? PrayerTimesCalculator.ThreePrayers
                    : PrayerTimesCalculator.FivePrayers;
            }
        }

        /// <summary>فرق التوقيت المستعمل فعلياً في التاريخ المعطى</summary>
        public double OffsetAt(DateTime date)
        {
            if (UseSystemTimeZone)
            {
                var probe = DateTime.SpecifyKind(date.Date.AddHours(12), DateTimeKind.Unspecified);
                return TimeZoneInfo.Local.GetUtcOffset(probe).TotalHours;
            }
            if (!string.IsNullOrEmpty(TimeZoneId))
            {
                try
                {
                    var tz = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
                    var probe = DateTime.SpecifyKind(date.Date.AddHours(12), DateTimeKind.Unspecified);
                    return tz.GetUtcOffset(probe).TotalHours;
                }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
                catch (ArgumentException) { }
            }
            return FallbackOffset;
        }

        public PrayerTimesCalculator BuildCalculator(DateTime date)
        {
            return new PrayerTimesCalculator
            {
                Latitude = Latitude,
                Longitude = Longitude,
                TimeZone = OffsetAt(date),
                Elevation = Elevation,
                Method = Method,
                Asr = Asr,
                HighLat = HighLat,
                Tune = Tune ?? new int[6]
            };
        }

        // ---------- الحفظ والتحميل ----------

        public static string FolderPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Mawaqit");
            }
        }

        public static string FilePath { get { return Path.Combine(FolderPath, "settings.xml"); } }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var ser = new XmlSerializer(typeof(AppSettings));
                    using (var fs = File.OpenRead(FilePath))
                    {
                        var s = (AppSettings)ser.Deserialize(fs);
                        s.Validate();
                        return s;
                    }
                }
            }
            catch (InvalidOperationException) { }   // ملف تالف
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return new AppSettings();
        }

        public bool Save()
        {
            try
            {
                Directory.CreateDirectory(FolderPath);
                var ser = new XmlSerializer(typeof(AppSettings));
                using (var fs = File.Create(FilePath))
                    ser.Serialize(fs, this);
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        /// <summary>يصلح أي قيمة ناقصة أو غير منطقية بعد القراءة من ملف قديم</summary>
        public void Validate()
        {
            var d = new AppSettings();
            if (Tune == null || Tune.Length != 6) Tune = new int[6];
            if (ColorPrayers == null || ColorPrayers.Length != 5)
                ColorPrayers = (string[])d.ColorPrayers.Clone();
            if (string.IsNullOrEmpty(MethodId)) MethodId = d.MethodId;
            if (string.IsNullOrEmpty(FontFamily)) FontFamily = d.FontFamily;
            if (Opacity < 0.2 || Opacity > 1.0) Opacity = 1.0;
            if (WindowSize < 120 || WindowSize > 1400) WindowSize = d.WindowSize;
            if (Latitude < -90 || Latitude > 90) Latitude = d.Latitude;
            if (Longitude < -180 || Longitude > 180) Longitude = d.Longitude;
            if (Elevation < -500 || Elevation > 9000) Elevation = 0;

            if (string.IsNullOrEmpty(ColorBackground)) ColorBackground = d.ColorBackground;
            if (string.IsNullOrEmpty(ColorFace)) ColorFace = d.ColorFace;
            if (string.IsNullOrEmpty(ColorRim)) ColorRim = d.ColorRim;
            if (string.IsNullOrEmpty(ColorTicks)) ColorTicks = d.ColorTicks;
            if (string.IsNullOrEmpty(ColorNumerals)) ColorNumerals = d.ColorNumerals;
            if (string.IsNullOrEmpty(ColorHourHand)) ColorHourHand = d.ColorHourHand;
            if (string.IsNullOrEmpty(ColorMinuteHand)) ColorMinuteHand = d.ColorMinuteHand;
            if (string.IsNullOrEmpty(ColorSecondHand)) ColorSecondHand = d.ColorSecondHand;
            if (string.IsNullOrEmpty(ColorText)) ColorText = d.ColorText;
            if (string.IsNullOrEmpty(ColorHighlight)) ColorHighlight = d.ColorHighlight;
            for (int i = 0; i < 5; i++)
                if (string.IsNullOrEmpty(ColorPrayers[i])) ColorPrayers[i] = d.ColorPrayers[i];
        }

        public AppSettings Clone()
        {
            var c = (AppSettings)MemberwiseClone();
            c.Tune = (int[])Tune.Clone();
            c.ColorPrayers = (string[])ColorPrayers.Clone();
            return c;
        }
    }

    /// <summary>مجموعة ألوان جاهزة</summary>
    public class Theme
    {
        public string NameAr;
        public string NameEn;
        /// <summary>اسم المجموعة باللغة المختارة</summary>
        public string Name { get { return L.IsRtl ? NameAr : NameEn; } }
        public string Background, Face, Rim, Ticks, Numerals;
        public string HourHand, MinuteHand, SecondHand, Text, Highlight;
        public string[] Prayers;
        public override string ToString() { return Name; }
    }

    public static class ThemePresets
    {
        public static readonly Theme[] All =
        {
            new Theme {
                NameAr = "ليلي كلاسيكي", NameEn = "Classic night",
                Background = "#00000000", Face = "#FF14181F", Rim = "#FFC9A227",
                Ticks = "#FFB9C2D0", Numerals = "#FFF2E9D8",
                HourHand = "#FFF2E9D8", MinuteHand = "#FFF2E9D8", SecondHand = "#FFE05A47",
                Text = "#FFF2E9D8", Highlight = "#FF42D29D",
                Prayers = new[] { "#FF6FB3F2", "#FFF5C542", "#FFF08A3C", "#FFE0556E", "#FF9B7BE0" }
            },
            new Theme {
                NameAr = "نهاري فاتح", NameEn = "Daylight",
                Background = "#00000000", Face = "#FFFBF7EF", Rim = "#FF8D7B52",
                Ticks = "#FF5A5A5A", Numerals = "#FF262626",
                HourHand = "#FF1E1E1E", MinuteHand = "#FF1E1E1E", SecondHand = "#FFC0392B",
                Text = "#FF262626", Highlight = "#FF1E8E5A",
                Prayers = new[] { "#FF2F6FB0", "#FFC9901B", "#FFC2621F", "#FFB03050", "#FF6A4BAF" }
            },
            new Theme {
                NameAr = "ذهبي ملكي", NameEn = "Royal gold",
                Background = "#00000000", Face = "#FF1A1208", Rim = "#FFD4AF37",
                Ticks = "#FFD4AF37", Numerals = "#FFF6E7B4",
                HourHand = "#FFF6E7B4", MinuteHand = "#FFF6E7B4", SecondHand = "#FFD4AF37",
                Text = "#FFF6E7B4", Highlight = "#FFFFD700",
                Prayers = new[] { "#FF8FD3FF", "#FFFFD166", "#FFFF9F45", "#FFFF6B8A", "#FFC792EA" }
            },
            new Theme {
                NameAr = "أخضر زمردي", NameEn = "Emerald",
                Background = "#00000000", Face = "#FF07211A", Rim = "#FF2E9E7A",
                Ticks = "#FF8FD9C3", Numerals = "#FFE4FFF5",
                HourHand = "#FFE4FFF5", MinuteHand = "#FFE4FFF5", SecondHand = "#FFFFC857",
                Text = "#FFE4FFF5", Highlight = "#FFFFC857",
                Prayers = new[] { "#FF7FD1FF", "#FFFFE066", "#FFFFA552", "#FFFF7A93", "#FFB69CFF" }
            },
            new Theme {
                NameAr = "أزرق نيلي", NameEn = "Indigo",
                Background = "#00000000", Face = "#FF0B1A33", Rim = "#FF4A7FD6",
                Ticks = "#FF9FC0F0", Numerals = "#FFEAF2FF",
                HourHand = "#FFEAF2FF", MinuteHand = "#FFEAF2FF", SecondHand = "#FFFF8C5A",
                Text = "#FFEAF2FF", Highlight = "#FF5BE0C0",
                Prayers = new[] { "#FF6FD3F2", "#FFFFD95C", "#FFFFA24D", "#FFFF6F91", "#FFAE8CFF" }
            },
            new Theme {
                NameAr = "رمادي معدني", NameEn = "Steel",
                Background = "#00000000", Face = "#FF2B2F33", Rim = "#FF9AA3AC",
                Ticks = "#FFCBD3DA", Numerals = "#FFF0F3F6",
                HourHand = "#FFF0F3F6", MinuteHand = "#FFF0F3F6", SecondHand = "#FFFF6B5B",
                Text = "#FFF0F3F6", Highlight = "#FF57D1A0",
                Prayers = new[] { "#FF7FBEEA", "#FFF0C65A", "#FFEE9B4E", "#FFE4697F", "#FFA68CE0" }
            },
        };
    }
}
