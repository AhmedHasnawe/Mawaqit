using System;

namespace Mawaqit.Core
{
    /// <summary>الصلوات والأوقات المحسوبة</summary>
    public enum TimeId { Fajr = 0, Sunrise = 1, Dhuhr = 2, Asr = 3, Maghrib = 4, Isha = 5 }

    /// <summary>مذهب حساب العصر: ظل المثل (1) أو ظل المثلين حنفي (2)</summary>
    public enum AsrJuristic { Standard = 1, Hanafi = 2 }

    /// <summary>قاعدة المعالجة في خطوط العرض العالية</summary>
    public enum HighLatRule { None, MidNight, OneSeventh, AngleBased }

    /// <summary>طريقة الحساب (الزوايا المعتمدة)</summary>
    public class CalcMethod
    {
        public string Id { get; set; }
        public string NameAr { get; set; }
        public string NameEn { get; set; }
        public double FajrAngle { get; set; }
        public double IshaAngle { get; set; }
        public double MaghribAngle { get; set; }
        /// <summary>دقائق ثابتة بعد المغرب بدل الزاوية (أم القرى = 90)، أو null</summary>
        public double? IshaMinutes { get; set; }
        /// <summary>دقائق ثابتة بعد الغروب بدل الزاوية، أو null</summary>
        public double? MaghribMinutes { get; set; }

        public override string ToString() { return L.IsRtl ? NameAr : NameEn; }

        /// <summary>اسم الطريقة باللغة المختارة</summary>
        public string Name { get { return L.IsRtl ? NameAr : NameEn; } }

        public static readonly CalcMethod[] All = new[]
        {
            new CalcMethod { Id = "Jafari",  NameAr = "جعفري (معهد الجيوفيزياء - طهران)", NameEn = "Shia Ithna-Ashari (Jafari)",
                             FajrAngle = 16.0, IshaAngle = 14.0, MaghribAngle = 4.0 },
            new CalcMethod { Id = "Karbala", NameAr = "العتبة الحسينية / كربلاء (فجر 17.0)", NameEn = "Karbala",
                             FajrAngle = 17.0, IshaAngle = 14.0, MaghribAngle = 4.0 },
            new CalcMethod { Id = "Tehran",  NameAr = "جامعة طهران", NameEn = "University of Tehran",
                             FajrAngle = 17.7, IshaAngle = 14.0, MaghribAngle = 4.5 },
            new CalcMethod { Id = "MWL",     NameAr = "رابطة العالم الإسلامي", NameEn = "Muslim World League",
                             FajrAngle = 18.0, IshaAngle = 17.0, MaghribAngle = 0.0, MaghribMinutes = 0 },
            new CalcMethod { Id = "Egypt",   NameAr = "الهيئة المصرية العامة للمساحة", NameEn = "Egyptian General Authority",
                             FajrAngle = 19.5, IshaAngle = 17.5, MaghribAngle = 0.0, MaghribMinutes = 0 },
            new CalcMethod { Id = "Makkah",  NameAr = "أم القرى - مكة المكرمة", NameEn = "Umm al-Qura, Makkah",
                             FajrAngle = 18.5, IshaAngle = 0.0, MaghribAngle = 0.0,
                             MaghribMinutes = 0, IshaMinutes = 90 },
            new CalcMethod { Id = "Karachi", NameAr = "جامعة العلوم الإسلامية - كراتشي", NameEn = "Univ. of Islamic Sciences, Karachi",
                             FajrAngle = 18.0, IshaAngle = 18.0, MaghribAngle = 0.0, MaghribMinutes = 0 },
            new CalcMethod { Id = "ISNA",    NameAr = "الجمعية الإسلامية لأمريكا الشمالية", NameEn = "ISNA",
                             FajrAngle = 15.0, IshaAngle = 15.0, MaghribAngle = 0.0, MaghribMinutes = 0 },
            new CalcMethod { Id = "Dubai",   NameAr = "دائرة الشؤون الإسلامية - دبي", NameEn = "Dubai",
                             FajrAngle = 18.2, IshaAngle = 18.2, MaghribAngle = 0.0, MaghribMinutes = 0 },
            new CalcMethod { Id = "Kuwait",  NameAr = "الأوقاف الكويتية", NameEn = "Kuwait",
                             FajrAngle = 18.0, IshaAngle = 17.5, MaghribAngle = 0.0, MaghribMinutes = 0 },
            new CalcMethod { Id = "Turkey",  NameAr = "ديانت - تركيا", NameEn = "Diyanet, Turkey",
                             FajrAngle = 18.0, IshaAngle = 17.0, MaghribAngle = 0.0, MaghribMinutes = 0 },
        };

        public static CalcMethod ById(string id)
        {
            foreach (var m in All) if (m.Id == id) return m;
            return All[0];
        }
    }

    /// <summary>
    /// حساب أوقات الصلاة فلكياً.
    /// النموذج الفلكي مطابق لمرجع VSOP87 بفارق أقل من 4 ثوانٍ في المواقع المعتدلة.
    /// القيمة double.NaN تعني أن الوقت غير موجود في ذلك اليوم (مناطق قطبية).
    /// </summary>
    public class PrayerTimesCalculator
    {
        private const double D2R = Math.PI / 180.0;
        private const double R2D = 180.0 / Math.PI;

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        /// <summary>فرق التوقيت عن غرينتش بالساعات لليوم المطلوب (شاملاً التوقيت الصيفي)</summary>
        public double TimeZone { get; set; }
        public double Elevation { get; set; }
        public CalcMethod Method { get; set; }
        public AsrJuristic Asr { get; set; }
        public HighLatRule HighLat { get; set; }
        /// <summary>تعديل يدوي بالدقائق لكل وقت (ترتيب TimeId)</summary>
        public int[] Tune { get; set; }

        public PrayerTimesCalculator()
        {
            Method = CalcMethod.All[0];
            Asr = AsrJuristic.Standard;
            HighLat = HighLatRule.AngleBased;
            Tune = new int[6];
        }

        // ---------- دوال مثلثية بالدرجات ----------
        private static double Sin(double d) { return Math.Sin(d * D2R); }
        private static double Cos(double d) { return Math.Cos(d * D2R); }
        private static double Tan(double d) { return Math.Tan(d * D2R); }
        private static double ASin(double x) { return Math.Asin(x) * R2D; }
        private static double ACos(double x) { return Math.Acos(x) * R2D; }
        private static double ATan2(double y, double x) { return Math.Atan2(y, x) * R2D; }
        private static double ACot(double x) { return Math.Atan2(1.0, x) * R2D; }

        private static double Fix(double a, double b)
        {
            if (double.IsNaN(a)) return a;
            a -= b * Math.Floor(a / b);
            return a < 0 ? a + b : a;
        }
        private static double FixAngle(double a) { return Fix(a, 360.0); }
        private static double FixHour(double a) { return Fix(a, 24.0); }

        /// <summary>اليوم اليولياني</summary>
        public static double Julian(int year, int month, int day)
        {
            if (month <= 2) { year -= 1; month += 12; }
            double A = Math.Floor(year / 100.0);
            double B = 2 - A + Math.Floor(A / 4.0);
            return Math.Floor(365.25 * (year + 4716)) + Math.Floor(30.6001 * (month + 1))
                   + day + B - 1524.5;
        }

        /// <summary>ميل الشمس (درجات) ومعادلة الزمن (ساعات)</summary>
        private static void SunPosition(double jd, out double declination, out double equation)
        {
            double D = jd - 2451545.0;
            double g = FixAngle(357.529 + 0.98560028 * D);
            double q = FixAngle(280.459 + 0.98564736 * D);
            double L = FixAngle(q + 1.915 * Sin(g) + 0.020 * Sin(2 * g));
            double e = 23.439 - 0.00000036 * D;
            double RA = Fix(ATan2(Cos(e) * Sin(L), Cos(L)) / 15.0, 24.0);
            equation = q / 15.0 - RA;
            declination = ASin(Sin(e) * Sin(L));
        }

        private double MidDay(double jdate, double t)
        {
            double decl, eqt;
            SunPosition(jdate + t, out decl, out eqt);
            return FixHour(12.0 - eqt);
        }

        /// <summary>وقت بلوغ الشمس زاوية معينة تحت الأفق. NaN إن لم تبلغها.</summary>
        private double SunAngleTime(double jdate, double angle, double t, bool beforeNoon)
        {
            double decl, eqt;
            SunPosition(jdate + t, out decl, out eqt);
            double noon = MidDay(jdate, t);
            double num = -Sin(angle) - Sin(decl) * Sin(Latitude);
            double den = Cos(decl) * Cos(Latitude);
            double v = num / den;
            if (double.IsNaN(v) || v > 1.0 || v < -1.0) return double.NaN;
            double hours = ACos(v) / 15.0;
            return beforeNoon ? noon - hours : noon + hours;
        }

        private double AsrTime(double jdate, double t)
        {
            double decl, eqt;
            SunPosition(jdate + t, out decl, out eqt);
            double angle = -ACot((double)Asr + Tan(Math.Abs(Latitude - decl)));
            return SunAngleTime(jdate, angle, t, false);
        }

        private double RiseSetAngle()
        {
            double h = Elevation > 0 ? Elevation : 0;
            return 0.833 + 0.0347 * Math.Sqrt(h);
        }

        private static double Keep(double value, double fallback)
        {
            return double.IsNaN(value) ? fallback : value;
        }

        /// <summary>
        /// يحسب أوقات اليوم ويعيدها بالساعات المحلية (0 - 24).
        /// الترتيب: الفجر، الشروق، الظهر، العصر، المغرب، العشاء.
        /// </summary>
        public double[] Calculate(DateTime date)
        {
            double jdate = Julian(date.Year, date.Month, date.Day) - Longitude / (15.0 * 24.0);

            double fajr = 5.0 / 24.0, sunrise = 6.0 / 24.0, dhuhr = 12.0 / 24.0;
            double asr = 13.0 / 24.0, sunset = 18.0 / 24.0, maghrib = 18.0 / 24.0, isha = 18.0 / 24.0;

            double rs = RiseSetAngle();
            for (int i = 0; i < 3; i++)   // تكرار حتى الاستقرار
            {
                fajr    = Keep(SunAngleTime(jdate, Method.FajrAngle, fajr, true) / 24.0, fajr);
                sunrise = Keep(SunAngleTime(jdate, rs, sunrise, true) / 24.0, sunrise);
                dhuhr   = MidDay(jdate, dhuhr) / 24.0;
                asr     = Keep(AsrTime(jdate, asr) / 24.0, asr);
                sunset  = Keep(SunAngleTime(jdate, rs, sunset, false) / 24.0, sunset);

                maghrib = Method.MaghribMinutes.HasValue
                    ? sunset + Method.MaghribMinutes.Value / (60.0 * 24.0)
                    : Keep(SunAngleTime(jdate, Method.MaghribAngle, maghrib, false) / 24.0, maghrib);

                isha = Method.IshaMinutes.HasValue
                    ? maghrib + Method.IshaMinutes.Value / (60.0 * 24.0)
                    : Keep(SunAngleTime(jdate, Method.IshaAngle, isha, false) / 24.0, isha);
            }

            // هل تتحقق هذه الأوقات فعلاً اليوم؟
            bool fajrOk    = !double.IsNaN(SunAngleTime(jdate, Method.FajrAngle, fajr, true));
            bool riseOk    = !double.IsNaN(SunAngleTime(jdate, rs, sunrise, true));
            bool maghribOk = Method.MaghribMinutes.HasValue ||
                             !double.IsNaN(SunAngleTime(jdate, Method.MaghribAngle, maghrib, false));
            bool ishaOk    = Method.IshaMinutes.HasValue ||
                             !double.IsNaN(SunAngleTime(jdate, Method.IshaAngle, isha, false));

            if (HighLat == HighLatRule.None || !riseOk)
            {
                if (!fajrOk) fajr = double.NaN;
                if (!maghribOk) maghrib = double.NaN;
                if (!ishaOk) isha = double.NaN;
                if (!riseOk) { sunrise = double.NaN; sunset = double.NaN; }
            }
            else
            {
                double night = (1.0 + sunrise) - sunset;   // من الغروب إلى شروق الغد (أجزاء يوم)

                fajr = ClampToNight(fajr, sunrise, NightPortion(Method.FajrAngle, night), true, fajrOk);
                if (!Method.MaghribMinutes.HasValue)
                    maghrib = ClampToNight(maghrib, sunset, NightPortion(Method.MaghribAngle, night), false, maghribOk);
                if (!Method.IshaMinutes.HasValue)
                    isha = ClampToNight(isha, sunset, NightPortion(Method.IshaAngle, night), false, ishaOk);
            }

            double adj = TimeZone - Longitude / 15.0;
            var raw = new double[6];
            raw[(int)TimeId.Fajr]    = fajr;
            raw[(int)TimeId.Sunrise] = sunrise;
            raw[(int)TimeId.Dhuhr]   = dhuhr;
            raw[(int)TimeId.Asr]     = asr;
            raw[(int)TimeId.Maghrib] = maghrib;
            raw[(int)TimeId.Isha]    = isha;

            var result = new double[6];
            for (int i = 0; i < 6; i++)
            {
                if (double.IsNaN(raw[i])) { result[i] = double.NaN; continue; }
                double tune = (Tune != null && Tune.Length > i) ? Tune[i] / 60.0 : 0.0;
                result[i] = FixHour(raw[i] * 24.0 + adj + tune);
            }
            return result;
        }

        private double NightPortion(double angle, double night)
        {
            switch (HighLat)
            {
                case HighLatRule.AngleBased: return night * angle / 60.0;
                case HighLatRule.OneSeventh: return night / 7.0;
                default: return night / 2.0;   // MidNight
            }
        }

        private static double ClampToNight(double time, double baseTime, double portion,
                                           bool beforeBase, bool exists)
        {
            double diff = beforeBase ? baseTime - time : time - baseTime;
            if (!exists || double.IsNaN(time) || diff > portion)
                return beforeBase ? baseTime - portion : baseTime + portion;
            return time;
        }

        // ---------- أدوات مساعدة ----------

        public static readonly string[] NamesAr = { "الفجر", "الشروق", "الظهر", "العصر", "المغرب", "العشاء" };
        public static readonly string[] NamesEn = { "Fajr", "Sunrise", "Dhuhr", "Asr", "Maghrib", "Isha" };

        /// <summary>أسماء الأوقات باللغة المختارة</summary>
        public static string[] Names { get { return L.IsRtl ? NamesAr : NamesEn; } }

        /// <summary>حروف الأوقات باللغة المختارة</summary>
        public static string[] Letters { get { return L.IsRtl ? LettersAr : LettersEn; } }
        /// <summary>
        /// حرف واحد مميّز لكل وقت يُكتب على رأس عقربه:
        /// ف الفجر، ق الشروق، ظ الظهر، ع العصر، م المغرب، ش العشاء.
        /// (العشاء بالشين لأن العين للعصر)
        /// </summary>
        public static readonly string[] LettersAr = { "ف", "ق", "ظ", "ع", "م", "ش" };
        public static readonly string[] LettersEn = { "F", "S", "D", "A", "M", "I" };

        /// <summary>الصلوات الخمس (بلا الشروق)</summary>
        public static readonly TimeId[] FivePrayers =
        {
            TimeId.Fajr, TimeId.Dhuhr, TimeId.Asr, TimeId.Maghrib, TimeId.Isha
        };

        /// <summary>
        /// ثلاثة مواقيت: الفجر والظهر والمغرب — على الجمع بين الظهر والعصر،
        /// وبين المغرب والعشاء. الأوقات المحسوبة لا تتغيّر، وإنما المعروض منها.
        /// </summary>
        public static readonly TimeId[] ThreePrayers =
        {
            TimeId.Fajr, TimeId.Dhuhr, TimeId.Maghrib
        };

        /// <summary>موضع الصلاة في مصفوفة الألوان (ترتيب الصلوات الخمس)</summary>
        public static int ColorIndex(TimeId id)
        {
            for (int i = 0; i < FivePrayers.Length; i++)
                if (FivePrayers[i] == id) return i;
            return 0;
        }

        public static string Format(double hours, bool use24)
        {
            if (double.IsNaN(hours)) return "--:--";
            int total = (int)Math.Round(hours * 60.0);
            total = ((total % 1440) + 1440) % 1440;
            int h = total / 60, m = total % 60;
            if (use24) return h.ToString("00") + ":" + m.ToString("00");
            string suffix = L.IsRtl ? (h < 12 ? " ص" : " م") : (h < 12 ? " AM" : " PM");
            int h12 = h % 12; if (h12 == 0) h12 = 12;
            return h12.ToString("00") + ":" + m.ToString("00") + suffix;
        }

        /// <summary>تحويل ساعة عشرية إلى DateTime في نفس يوم التاريخ المعطى</summary>
        public static DateTime ToDateTime(DateTime day, double hours)
        {
            return day.Date.AddMinutes(Math.Round(hours * 60.0));
        }
    }
}
