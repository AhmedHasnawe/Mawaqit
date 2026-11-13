using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Mawaqit.Core
{
    public class City
    {
        public string NameAr { get; set; }
        public string NameEn { get; set; }
        public string CountryAr { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        /// <summary>معرّف المنطقة الزمنية في ويندوز (يتكفّل بالتوقيت الصيفي)</summary>
        public string TimeZoneId { get; set; }
        /// <summary>فرق ثابت عن غرينتش يُستعمل إن تعذّر إيجاد المنطقة الزمنية</summary>
        public double FallbackOffset { get; set; }
        /// <summary>طريقة الحساب المقترحة لهذه المدينة</summary>
        public string SuggestedMethod { get; set; }

        public City() { }

        public City(string ar, string en, string country, double lat, double lng,
                    string tzId, double fallback, string method)
        {
            NameAr = ar; NameEn = en; CountryAr = country;
            Latitude = lat; Longitude = lng;
            TimeZoneId = tzId; FallbackOffset = fallback; SuggestedMethod = method;
        }

        /// <summary>اسم المدينة باللغة المختارة</summary>
        public string Name { get { return L.IsRtl ? NameAr : NameEn; } }

        public string Display
        {
            get { return L.IsRtl ? NameAr + " - " + CountryAr : NameEn + " - " + CountryEn; }
        }

        /// <summary>اسم البلد بالإنكليزية، يُشتقّ من جدول ثابت</summary>
        public string CountryEn { get { return Countries.English(CountryAr); } }

        public override string ToString() { return Display; }

        /// <summary>فرق التوقيت الفعلي بالساعات في التاريخ المعطى (مع التوقيت الصيفي)</summary>
        public double OffsetAt(DateTime localDate)
        {
            if (!string.IsNullOrEmpty(TimeZoneId))
            {
                try
                {
                    var tz = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
                    // نستعمل الظهر لتفادي الالتباس عند لحظة تغيير التوقيت الصيفي
                    var probe = DateTime.SpecifyKind(localDate.Date.AddHours(12), DateTimeKind.Unspecified);
                    return tz.GetUtcOffset(probe).TotalHours;
                }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
                catch (ArgumentException) { }
            }
            return FallbackOffset;
        }
    }

    public static class CityDatabase
    {
        private static List<City> _all;

        public static List<City> All
        {
            get
            {
                if (_all == null)
                {
                    _all = new List<City>(BuiltIn);
                    _all.AddRange(LoadExtra());
                }
                return _all;
            }
        }

        public static City Find(string nameAr)
        {
            if (string.IsNullOrEmpty(nameAr)) return null;
            foreach (var c in All)
                if (c.NameAr == nameAr) return c;
            return null;
        }

        /// <summary>
        /// ملف اختياري بجانب البرنامج لإضافة مدن:
        /// الاسم العربي,الاسم الإنكليزي,البلد,خط العرض,خط الطول,المنطقة الزمنية,الفرق,الطريقة
        /// </summary>
        private static IEnumerable<City> LoadExtra()
        {
            var list = new List<City>();
            try
            {
                string path = Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
                    "cities.csv");
                if (!File.Exists(path)) return list;
                foreach (var raw in File.ReadAllLines(path))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    var p = line.Split(',');
                    if (p.Length < 6) continue;
                    double lat, lng, off = 3;
                    if (!double.TryParse(p[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out lat)) continue;
                    if (!double.TryParse(p[4].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out lng)) continue;
                    if (p.Length > 6) double.TryParse(p[6].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out off);
                    list.Add(new City(p[0].Trim(), p[1].Trim(), p[2].Trim(), lat, lng,
                                      p[5].Trim(), off, p.Length > 7 ? p[7].Trim() : "Jafari"));
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return list;
        }

        private const string IQ = "Arabic Standard Time";      // بغداد +3
        private const string SA = "Arab Standard Time";        // الرياض/الكويت +3
        private const string AE = "Arabian Standard Time";     // الإمارات/عُمان +4
        private const string EG = "Egypt Standard Time";
        private const string TR = "Turkey Standard Time";
        private const string IR = "Iran Standard Time";

        public static readonly City[] BuiltIn =
        {
            // ---------- العراق ----------
            new City("السماوة",   "Samawah",      "العراق", 31.3094,  45.2819, IQ, 3, "Jafari"),
            new City("بغداد",     "Baghdad",      "العراق", 33.3152,  44.3661, IQ, 3, "Jafari"),
            new City("البصرة",    "Basra",        "العراق", 30.5085,  47.7804, IQ, 3, "Jafari"),
            new City("النجف",     "Najaf",        "العراق", 31.9959,  44.3148, IQ, 3, "Jafari"),
            new City("كربلاء",    "Karbala",      "العراق", 32.6160,  44.0249, IQ, 3, "Karbala"),
            new City("الحلة",     "Hilla",        "العراق", 32.4637,  44.4198, IQ, 3, "Jafari"),
            new City("الديوانية", "Diwaniyah",    "العراق", 31.9893,  44.9247, IQ, 3, "Jafari"),
            new City("الناصرية",  "Nasiriyah",    "العراق", 31.0436,  46.2572, IQ, 3, "Jafari"),
            new City("العمارة",   "Amarah",       "العراق", 31.8356,  47.1450, IQ, 3, "Jafari"),
            new City("الكوت",     "Kut",          "العراق", 32.5128,  45.8181, IQ, 3, "Jafari"),
            new City("بعقوبة",    "Baqubah",      "العراق", 33.7500,  44.6433, IQ, 3, "Jafari"),
            new City("الرمادي",   "Ramadi",       "العراق", 33.4258,  43.3008, IQ, 3, "MWL"),
            new City("الفلوجة",   "Fallujah",     "العراق", 33.3500,  43.7833, IQ, 3, "MWL"),
            new City("تكريت",     "Tikrit",       "العراق", 34.6100,  43.6786, IQ, 3, "MWL"),
            new City("سامراء",    "Samarra",      "العراق", 34.1958,  43.8742, IQ, 3, "Jafari"),
            new City("الموصل",    "Mosul",        "العراق", 36.3350,  43.1189, IQ, 3, "MWL"),
            new City("كركوك",     "Kirkuk",       "العراق", 35.4681,  44.3922, IQ, 3, "MWL"),
            new City("أربيل",     "Erbil",        "العراق", 36.1911,  44.0094, IQ, 3, "MWL"),
            new City("السليمانية","Sulaymaniyah", "العراق", 35.5556,  45.4351, IQ, 3, "MWL"),
            new City("دهوك",      "Duhok",        "العراق", 36.8617,  42.9880, IQ, 3, "MWL"),
            new City("الرطبة",    "Rutba",        "العراق", 33.0386,  40.2847, IQ, 3, "MWL"),
            new City("خانقين",    "Khanaqin",     "العراق", 34.3519,  45.3842, IQ, 3, "Jafari"),

            // ---------- الخليج والجزيرة ----------
            new City("مكة المكرمة",   "Makkah",     "السعودية", 21.4225, 39.8262, SA, 3, "Makkah"),
            new City("المدينة المنورة","Madinah",   "السعودية", 24.4686, 39.6142, SA, 3, "Makkah"),
            new City("الرياض",        "Riyadh",     "السعودية", 24.7136, 46.6753, SA, 3, "Makkah"),
            new City("جدة",           "Jeddah",     "السعودية", 21.4858, 39.1925, SA, 3, "Makkah"),
            new City("الدمام",        "Dammam",     "السعودية", 26.4207, 50.0888, SA, 3, "Makkah"),
            new City("الكويت",        "Kuwait City","الكويت",   29.3759, 47.9774, SA, 3, "Kuwait"),
            new City("الدوحة",        "Doha",       "قطر",      25.2854, 51.5310, SA, 3, "Makkah"),
            new City("المنامة",       "Manama",     "البحرين",  26.2285, 50.5860, SA, 3, "Makkah"),
            new City("أبوظبي",        "Abu Dhabi",  "الإمارات", 24.4539, 54.3773, AE, 4, "Dubai"),
            new City("دبي",           "Dubai",      "الإمارات", 25.2048, 55.2708, AE, 4, "Dubai"),
            new City("مسقط",          "Muscat",     "عُمان",    23.5880, 58.3829, AE, 4, "Makkah"),
            new City("صنعاء",         "Sanaa",      "اليمن",    15.3694, 44.1910, SA, 3, "MWL"),
            new City("عدن",           "Aden",       "اليمن",    12.7855, 45.0187, SA, 3, "MWL"),

            // ---------- الشام ومصر ----------
            new City("دمشق",       "Damascus",   "سوريا",    33.5138, 36.2765, "Syria Standard Time", 3, "MWL"),
            new City("حلب",        "Aleppo",     "سوريا",    36.2021, 37.1343, "Syria Standard Time", 3, "MWL"),
            new City("بيروت",      "Beirut",     "لبنان",    33.8938, 35.5018, "Middle East Standard Time", 2, "MWL"),
            new City("عمّان",      "Amman",      "الأردن",   31.9454, 35.9284, "Jordan Standard Time", 3, "MWL"),
            new City("القدس",      "Jerusalem",  "فلسطين",   31.7683, 35.2137, "Israel Standard Time", 2, "MWL"),
            new City("غزة",        "Gaza",       "فلسطين",   31.5017, 34.4668, "West Bank Standard Time", 2, "MWL"),
            new City("القاهرة",    "Cairo",      "مصر",      30.0444, 31.2357, EG, 2, "Egypt"),
            new City("الإسكندرية", "Alexandria", "مصر",      31.2001, 29.9187, EG, 2, "Egypt"),
            new City("أسوان",      "Aswan",      "مصر",      24.0889, 32.8998, EG, 2, "Egypt"),

            // ---------- إيران وتركيا وآسيا الوسطى ----------
            new City("طهران",   "Tehran",     "إيران",      35.6892, 51.3890, IR, 3.5, "Tehran"),
            new City("مشهد",    "Mashhad",    "إيران",      36.2605, 59.6168, IR, 3.5, "Tehran"),
            new City("قم",      "Qom",        "إيران",      34.6416, 50.8746, IR, 3.5, "Tehran"),
            new City("الأهواز", "Ahvaz",      "إيران",      31.3183, 48.6706, IR, 3.5, "Tehran"),
            new City("إسطنبول", "Istanbul",   "تركيا",      41.0082, 28.9784, TR, 3, "Turkey"),
            new City("أنقرة",   "Ankara",     "تركيا",      39.9334, 32.8597, TR, 3, "Turkey"),
            new City("باكو",    "Baku",       "أذربيجان",   40.4093, 49.8671, "Azerbaijan Standard Time", 4, "Jafari"),
            new City("طشقند",   "Tashkent",   "أوزبكستان",  41.2995, 69.2401, "West Asia Standard Time", 5, "MWL"),
            new City("ألماتي",  "Almaty",     "كازاخستان",  43.2220, 76.8512, "Central Asia Standard Time", 6, "MWL"),

            // ---------- شمال أفريقيا ----------
            new City("الخرطوم",      "Khartoum",   "السودان",  15.5007, 32.5599, "Sudan Standard Time", 2, "MWL"),
            new City("طرابلس",       "Tripoli",    "ليبيا",    32.8872, 13.1913, "Libya Standard Time", 2, "MWL"),
            new City("تونس",         "Tunis",      "تونس",     36.8065, 10.1815, "W. Central Africa Standard Time", 1, "MWL"),
            new City("الجزائر",      "Algiers",    "الجزائر",  36.7538,  3.0588, "W. Central Africa Standard Time", 1, "MWL"),
            new City("الرباط",       "Rabat",      "المغرب",   34.0209, -6.8416, "Morocco Standard Time", 1, "MWL"),
            new City("الدار البيضاء","Casablanca", "المغرب",   33.5731, -7.5898, "Morocco Standard Time", 1, "MWL"),
            new City("نواكشوط",      "Nouakchott", "موريتانيا",18.0735,-15.9582, "Greenwich Standard Time", 0, "MWL"),

            // ---------- جنوب وشرق آسيا ----------
            new City("إسلام آباد",  "Islamabad",    "باكستان",  33.6844,  73.0479, "Pakistan Standard Time", 5, "Karachi"),
            new City("كراتشي",      "Karachi",      "باكستان",  24.8607,  67.0011, "Pakistan Standard Time", 5, "Karachi"),
            new City("لاهور",       "Lahore",       "باكستان",  31.5204,  74.3587, "Pakistan Standard Time", 5, "Karachi"),
            new City("كابل",        "Kabul",        "أفغانستان",34.5553,  69.2075, "Afghanistan Standard Time", 4.5, "Karachi"),
            new City("نيودلهي",     "New Delhi",    "الهند",    28.6139,  77.2090, "India Standard Time", 5.5, "Karachi"),
            new City("دكا",         "Dhaka",        "بنغلاديش", 23.8103,  90.4125, "Bangladesh Standard Time", 6, "Karachi"),
            new City("جاكرتا",      "Jakarta",      "إندونيسيا",-6.2088, 106.8456, "SE Asia Standard Time", 7, "MWL"),
            new City("كوالالمبور",  "Kuala Lumpur", "ماليزيا",   3.1390, 101.6869, "Singapore Standard Time", 8, "MWL"),

            // ---------- أوروبا وأمريكا ----------
            new City("لندن",       "London",      "بريطانيا",  51.5074,  -0.1278, "GMT Standard Time", 0, "MWL"),
            new City("باريس",      "Paris",       "فرنسا",     48.8566,   2.3522, "Romance Standard Time", 1, "MWL"),
            new City("برلين",      "Berlin",      "ألمانيا",   52.5200,  13.4050, "W. Europe Standard Time", 1, "MWL"),
            new City("ستوكهولم",   "Stockholm",   "السويد",    59.3293,  18.0686, "W. Europe Standard Time", 1, "MWL"),
            new City("موسكو",      "Moscow",      "روسيا",     55.7558,  37.6173, "Russian Standard Time", 3, "MWL"),
            new City("نيويورك",    "New York",    "أمريكا",    40.7128, -74.0060, "Eastern Standard Time", -5, "ISNA"),
            new City("ديترويت",    "Detroit",     "أمريكا",    42.3314, -83.0458, "Eastern Standard Time", -5, "ISNA"),
            new City("شيكاغو",     "Chicago",     "أمريكا",    41.8781, -87.6298, "Central Standard Time", -6, "ISNA"),
            new City("لوس أنجلوس", "Los Angeles", "أمريكا",    34.0522,-118.2437, "Pacific Standard Time", -8, "ISNA"),
            new City("تورنتو",     "Toronto",     "كندا",      43.6532, -79.3832, "Eastern Standard Time", -5, "ISNA"),
            new City("سيدني",      "Sydney",      "أستراليا", -33.8688, 151.2093, "AUS Eastern Standard Time", 10, "MWL"),
        };
    }
}
