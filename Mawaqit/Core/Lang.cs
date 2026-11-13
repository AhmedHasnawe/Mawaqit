using System;
using System.Globalization;

namespace Mawaqit.Core
{
    public enum AppLanguage { Arabic, English }

    /// <summary>
    /// كل نصوص الواجهة في مكان واحد، كل نصٍّ بلغتيه جنباً إلى جنب.
    /// لا ملفات موارد ولا تجميعات تابعة، فيبقى البرنامج ملفاً واحداً.
    /// </summary>
    public static class L
    {
        public static AppLanguage Current = AppLanguage.Arabic;

        /// <summary>اتجاه الواجهة تبعاً للغة</summary>
        public static bool IsRtl { get { return Current == AppLanguage.Arabic; } }

        private static string P(string ar, string en)
        {
            return Current == AppLanguage.Arabic ? ar : en;
        }

        /// <summary>اللغة المناسبة لإعدادات ويندوز، تُستعمل عند أول تشغيل</summary>
        public static AppLanguage FromSystem()
        {
            try
            {
                var name = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                return name == "ar" ? AppLanguage.Arabic : AppLanguage.English;
            }
            catch (Exception) { return AppLanguage.Arabic; }
        }

        /// <summary>رقم إصدار البرنامج كما في بيانات التجميعة</summary>
        public static string Version
        {
            get
            {
                try
                {
                    var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                    return v.Major + "." + v.Minor;
                }
                catch (Exception) { return ""; }
            }
        }

        public static string VersionLine
        {
            get
            {
                var v = Version;
                return v.Length == 0 ? "" : P("\n\nالإصدار ", "\n\nVersion ") + v;
            }
        }

        public static string NameOf(AppLanguage lang)
        {
            return lang == AppLanguage.Arabic ? "العربية" : "English";
        }

        // ================= عام =================
        public static string AppName { get { return P("مواقيت", "Mawaqit"); } }
        public static string Ok { get { return P("حسناً", "OK"); } }
        public static string Save { get { return P("حفظ", "Save"); } }
        public static string Cancel { get { return P("إلغاء", "Cancel"); } }
        public static string ResetDefaults { get { return P("استعادة الافتراضي", "Restore defaults"); } }
        public static string Confirm { get { return P("تأكيد", "Confirm"); } }

        // ================= قائمة الزر الأيمن =================
        public static string MenuNextPrayer { get { return P("الصلاة التالية", "Next prayer"); } }
        public static string MenuPrevPrayer { get { return P("الصلاة السابقة", "Previous prayer"); } }
        public static string MenuShowUpcoming { get { return P("عرض الصلاة القادمة", "Show upcoming prayer"); } }
        public static string MenuSettings { get { return P("الإعدادات...", "Settings…"); } }
        public static string MenuToday { get { return P("مواقيت اليوم...", "Today's times…"); } }
        public static string MenuAlwaysOnTop { get { return P("دائماً في المقدمة", "Always on top"); } }
        public static string MenuStartWithWindows { get { return P("التشغيل مع ويندوز", "Start with Windows"); } }
        public static string MenuLockPosition { get { return P("تثبيت المكان", "Lock position"); } }
        public static string MenuSize { get { return P("الحجم", "Size"); } }
        public static string MenuLanguage { get { return "اللغة / Language"; } }
        public static string MenuAbout { get { return P("عن البرنامج", "About"); } }
        public static string MenuExit { get { return P("إنهاء", "Exit"); } }

        public static string SizeTiny { get { return P("مصغّر (130)", "Tiny (130)"); } }
        public static string SizeSmall { get { return P("صغير (180)", "Small (180)"); } }
        public static string SizeMedium { get { return P("متوسط (260)", "Medium (260)"); } }
        public static string SizeLarge { get { return P("كبير (360)", "Large (360)"); } }
        public static string SizeHuge { get { return P("ضخم (520)", "Huge (520)"); } }

        // ================= أيقونة شريط المهام =================
        public static string TrayShowHide { get { return P("إظهار / إخفاء", "Show / Hide"); } }
        public static string TrayStopClickThrough { get { return P("إلغاء تمرير النقر", "Disable click-through"); } }

        // ================= الرسائل =================
        public static string AlreadyRunning
        {
            get
            {
                return P("«مواقيت» يعمل بالفعل. ابحث عن أيقونته قرب الساعة في شريط المهام.",
                         "Mawaqit is already running. Look for its icon near the clock in the taskbar.");
            }
        }

        public static string UnexpectedError { get { return P("حدث خطأ غير متوقع:", "An unexpected error occurred:"); } }

        public static string AboutText
        {
            get
            {
                return P(
                    "مواقيت — ساعة كلاسيكية لسطح المكتب تُظهر أوقات الصلاة بعقارب ملوّنة.\n\n" +
                    "الحساب فلكي محلي بالكامل ولا يحتاج اتصالاً بالإنترنت.\n" +
                    "اسحب الساعة لتحريكها، ودوّر عجلة الفأرة لتغيير حجمها،\n" +
                    "وانقر عليها نقرتين لفتح الإعدادات.",
                    "Mawaqit — a classic desktop clock that shows prayer times with coloured hands.\n\n" +
                    "Times are computed astronomically on your own machine; no internet needed.\n" +
                    "Drag the clock to move it, scroll the mouse wheel to resize it,\n" +
                    "and double-click it to open the settings.");
            }
        }

        public static string ConfirmReset
        {
            get { return P("استعادة كل الإعدادات الافتراضية؟", "Restore all settings to their defaults?"); }
        }

        public static string TodayTitle { get { return P("مواقيت اليوم", "Today's times"); } }
        public static string TodayMethod { get { return P("طريقة الحساب: ", "Calculation method: "); } }
        public static string TodayAsr { get { return P("العصر: ", "Asr: "); } }
        public static string AsrStandardShort { get { return P("ظل المثل", "shadow ×1"); } }
        public static string AsrHanafiShort { get { return P("ظل المثلين (حنفي)", "shadow ×2 (Hanafi)"); } }

        /// <summary>عنوان التنبيه عند دخول الوقت، {0} اسم الصلاة</summary>
        public static string PrayerBegunTitle { get { return P("دخل وقت {0}", "{0} has begun"); } }
        /// <summary>تلميح أيقونة شريط المهام: {0} الصلاة و{1} المدة</summary>
        public static string TrayCountdown { get { return P("{0} بعد {1}", "{0} in {1}"); } }

        // ================= نافذة الإعدادات =================
        public static string SettingsTitle { get { return P("إعدادات مواقيت", "Mawaqit settings"); } }
        public static string TabLocation { get { return P("  الموقع  ", "  Location  "); } }
        public static string TabCalculation { get { return P("  الحساب  ", "  Calculation  "); } }
        public static string TabColours { get { return P("  الألوان  ", "  Colours  "); } }
        public static string TabAppearance { get { return P("  المظهر والسلوك  ", "  Appearance  "); } }
        public static string LivePreview { get { return P("معاينة حيّة", "Live preview"); } }

        // --- الموقع ---
        public static string City { get { return P("المدينة", "City"); } }
        public static string Search { get { return P("بحث", "Search"); } }
        public static string SearchHint
        {
            get { return P("اكتب جزءاً من اسم المدينة للبحث", "Type part of a city name to search"); }
        }
        public static string ManualCoords
        {
            get
            {
                return P("تحديد الإحداثيات يدوياً (لمدينة غير مدرجة)",
                         "Enter coordinates manually (for a city not listed)");
            }
        }
        public static string PlaceName { get { return P("اسم الموقع", "Place name"); } }
        public static string Elevation { get { return P("الارتفاع عن سطح البحر (م)", "Elevation above sea level (m)"); } }
        public static string Latitude { get { return P("خط العرض (موجب شمالاً)", "Latitude (positive north)"); } }
        public static string Longitude { get { return P("خط الطول (موجب شرقاً)", "Longitude (positive east)"); } }
        public static string UseSystemTz
        {
            get
            {
                return P("استعمال المنطقة الزمنية لجهازي (يُنصح بها — تتكفّل بالتوقيت الصيفي)",
                         "Use my computer's time zone (recommended — handles daylight saving)");
            }
        }
        public static string FixedOffset
        {
            get { return P("أو فرق ثابت عن غرينتش بالساعات", "Or a fixed offset from GMT, in hours"); }
        }
        /// <summary>{0} الإشارة و{1} الساعات و{2} الدقائق</summary>
        public static string TzNow { get { return P("الفرق المستعمل الآن: غرينتش ", "Offset in use now: GMT "); } }
        public static string TzFromWindows { get { return P("  (من إعدادات ويندوز)", "  (from Windows)"); } }
        public static string TzFixed { get { return P("  (ثابت)", "  (fixed)"); } }

        // --- الحساب ---
        public static string MethodLabel
        {
            get { return P("طريقة الحساب (زوايا الفجر والعشاء)", "Calculation method (Fajr and Isha angles)"); }
        }
        public static string PrayerSetLabel
        {
            get { return P("عدد المواقيت المعروضة على الساعة", "How many times the clock shows"); }
        }
        public static string PrayerSetFive
        {
            get
            {
                return P("خمسة — الفجر والظهر والعصر والمغرب والعشاء",
                         "Five — Fajr, Dhuhr, Asr, Maghrib and Isha");
            }
        }
        public static string PrayerSetThree
        {
            get
            {
                return P("ثلاثة — الفجر والظهر والمغرب (جمع الظهرين والعشاءين)",
                         "Three — Fajr, Dhuhr and Maghrib (Dhuhr with Asr, Maghrib with Isha)");
            }
        }
        public static string PrayerSetNote
        {
            get
            {
                return P("الأوقات المحسوبة لا تتغيّر، وإنما يتغيّر ما يُعرض منه على القرص وما يُنبَّه عليه.",
                         "The computed times do not change; only which of them the clock shows and announces.");
            }
        }
        public static string AsrLabel { get { return P("مذهب حساب العصر", "Asr calculation"); } }
        public static string AsrStandard
        {
            get { return P("ظل المثل — الجمهور والجعفرية", "Shadow ×1 — majority and Jafari"); }
        }
        public static string AsrHanafi { get { return P("ظل المثلين — الحنفية", "Shadow ×2 — Hanafi"); } }
        public static string HighLatLabel
        {
            get
            {
                return P("معالجة خطوط العرض العالية (خارج المنطقة المعتدلة)",
                         "Higher-latitude rule (outside the temperate zone)");
            }
        }
        public static string HighLatNone { get { return P("بلا معالجة", "None"); } }
        public static string HighLatMidnight { get { return P("منتصف الليل", "Middle of the night"); } }
        public static string HighLatSeventh { get { return P("سُبع الليل", "One-seventh of the night"); } }
        public static string HighLatAngle { get { return P("نسبة الزاوية (الأنسب)", "Angle-based (recommended)"); } }
        public static string TuneLabel
        {
            get
            {
                return P("تعديل يدوي بالدقائق (يُضاف أو يُطرح من الوقت المحسوب)",
                         "Manual adjustment in minutes (added to or taken off the computed time)");
            }
        }
        public static string TodayWithSettings
        {
            get { return P("مواقيت اليوم بالإعدادات الحالية", "Today's times with the current settings"); }
        }
        /// <summary>{0} زاوية الفجر</summary>
        public static string FajrAngleIs { get { return P("زاوية الفجر ", "Fajr angle "); } }
        public static string IshaAfterMaghrib { get { return P("، والعشاء بعد المغرب بـ ", ", Isha after Maghrib by "); } }
        public static string Minutes { get { return P(" دقيقة", " minutes"); } }
        public static string IshaAngleIs { get { return P("، وزاوية العشاء ", ", Isha angle "); } }
        public static string MaghribAtSunset { get { return P("، والمغرب عند الغروب", ", Maghrib at sunset"); } }
        public static string MaghribAngleIs { get { return P("، والمغرب بزاوية ", ", Maghrib at angle "); } }
        public static string AfterSunset { get { return P("° بعد الغروب", "° below the horizon"); } }

        // --- الألوان ---
        public static string ThemePreset { get { return P("مجموعة جاهزة", "Preset"); } }
        public static string ThemeCustom { get { return P("(مخصّص)", "(custom)"); } }
        public static string PrayerColours { get { return P("ألوان الصلوات الخمس", "Prayer colours"); } }
        public static string ClockColours { get { return P("ألوان الساعة", "Clock colours"); } }
        public static string ColoursNote
        {
            get
            {
                return P("انقر أي مربّع لونٍ لاختيار لون آخر. خلفية الساعة شفافة افتراضياً لتبدو كجزء من سطح المكتب.",
                         "Click any swatch to pick another colour. The clock's background is transparent by default so it sits on the desktop.");
            }
        }
        public static string ColDial { get { return P("القرص", "Dial"); } }
        public static string ColRim { get { return P("الإطار", "Rim"); } }
        public static string ColTicks { get { return P("الشعيرات", "Ticks"); } }
        public static string ColNumerals { get { return P("الأرقام", "Numerals"); } }
        public static string ColText { get { return P("النصوص", "Text"); } }
        public static string ColHourHand { get { return P("عقرب الساعة", "Hour hand"); } }
        public static string ColMinuteHand { get { return P("عقرب الدقيقة", "Minute hand"); } }
        public static string ColSecondHand { get { return P("عقرب الثانية", "Second hand"); } }
        public static string ColHighlight { get { return P("لون الإبراز", "Highlight"); } }

        // --- المظهر والسلوك ---
        public static string LanguageLabel { get { return P("اللغة", "Language"); } }
        public static string NumeralsLabel { get { return P("شكل الأرقام", "Numerals"); } }
        public static string NumeralsArabic { get { return P("عربية هندية ١٢٣", "Arabic-Indic ١٢٣"); } }
        public static string NumeralsWestern { get { return P("عربية غربية 123", "Western 123"); } }
        public static string NumeralsRoman { get { return P("رومانية XII", "Roman XII"); } }
        public static string NumeralsNone { get { return P("بلا أرقام", "None"); } }
        public static string FontLabel { get { return P("الخط", "Font"); } }
        public static string HighlightLabel { get { return P("الصلاة المُبرزة", "Which prayer is shown"); } }
        public static string HighlightNext { get { return P("الصلاة القادمة", "The upcoming prayer"); } }
        public static string HighlightNearest
        {
            get { return P("الأقرب زمنياً (ماضية أو قادمة)", "The nearest in time (past or upcoming)"); }
        }
        public static string ClockSize { get { return P("حجم الساعة (بكسل)", "Clock size (pixels)"); } }
        public static string Opacity { get { return P("الشفافية", "Opacity"); } }
        public static string ShowPrayerHands
        {
            get
            {
                return P("إظهار عقربي الصلاة (وإلا فالرأسان وحدهما)",
                         "Show the prayer's two hands (otherwise just their tips)");
            }
        }
        public static string AutoReturn
        {
            get { return P("العودة تلقائياً إلى الصلاة القادمة", "Return automatically to the upcoming prayer"); }
        }
        public static string ShowArc { get { return P("قوس المدة المتبقية", "Remaining-time arc"); } }
        public static string ShowPanel { get { return P("لوحة الاسم والعدّ التنازلي", "Name and countdown panel"); } }
        public static string ShowSeconds { get { return P("عقرب الثواني", "Second hand"); } }
        public static string SmoothSeconds { get { return P("حركة ناعمة للثواني", "Sweeping second hand"); } }
        public static string Use24Hour { get { return P("عرض الأوقات بنظام 24 ساعة", "Show times on a 24-hour clock"); } }
        public static string ShowInTaskbar { get { return P("إظهار في شريط المهام", "Show in the taskbar"); } }
        public static string LockPositionFull { get { return P("تثبيت المكان (منع السحب)", "Lock position (no dragging)"); } }
        public static string ClickThrough
        {
            get { return P("تمرير النقر إلى ما خلف الساعة", "Let clicks pass through to what is behind"); }
        }
        public static string NotifyOnPrayer { get { return P("تنبيه عند دخول الوقت", "Notify when a prayer time begins"); } }
        public static string SoundWithNotify { get { return P("صوت مع التنبيه", "Play a sound with the notification"); } }
        public static string StartWithWindowsFull { get { return P("التشغيل مع بدء ويندوز", "Start with Windows"); } }
        public static string AppearanceNote
        {
            get
            {
                return P("تُعرض صلاة واحدة على القرص. انقر السهم على طرف سطر الصلاة للانتقال إلى غيرها.\n" +
                         "وعند تفعيل «تمرير النقر» لن تستجيب الساعة للفأرة؛ يمكن إلغاؤه من قائمة الأيقونة في شريط المهام.",
                         "One prayer is shown at a time. Click an arrow beside the prayer line to step to another.\n" +
                         "With click-through on, the clock ignores the mouse; turn it off from the taskbar icon's menu.");
            }
        }
    }
}
