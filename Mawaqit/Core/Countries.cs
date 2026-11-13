using System.Collections.Generic;

namespace Mawaqit.Core
{
    /// <summary>أسماء البلدان بالإنكليزية، لعرض المدن حين تكون اللغة إنكليزية</summary>
    public static class Countries
    {
        private static readonly Dictionary<string, string> Map =
            new Dictionary<string, string>
        {
            { "العراق", "Iraq" },
            { "السعودية", "Saudi Arabia" },
            { "الكويت", "Kuwait" },
            { "قطر", "Qatar" },
            { "البحرين", "Bahrain" },
            { "الإمارات", "UAE" },
            { "عُمان", "Oman" },
            { "اليمن", "Yemen" },
            { "سوريا", "Syria" },
            { "لبنان", "Lebanon" },
            { "الأردن", "Jordan" },
            { "فلسطين", "Palestine" },
            { "مصر", "Egypt" },
            { "إيران", "Iran" },
            { "تركيا", "Turkey" },
            { "أذربيجان", "Azerbaijan" },
            { "أوزبكستان", "Uzbekistan" },
            { "كازاخستان", "Kazakhstan" },
            { "السودان", "Sudan" },
            { "ليبيا", "Libya" },
            { "تونس", "Tunisia" },
            { "الجزائر", "Algeria" },
            { "المغرب", "Morocco" },
            { "موريتانيا", "Mauritania" },
            { "باكستان", "Pakistan" },
            { "أفغانستان", "Afghanistan" },
            { "الهند", "India" },
            { "بنغلاديش", "Bangladesh" },
            { "إندونيسيا", "Indonesia" },
            { "ماليزيا", "Malaysia" },
            { "بريطانيا", "United Kingdom" },
            { "فرنسا", "France" },
            { "ألمانيا", "Germany" },
            { "السويد", "Sweden" },
            { "روسيا", "Russia" },
            { "أمريكا", "United States" },
            { "كندا", "Canada" },
            { "أستراليا", "Australia" },
        };

        public static string English(string arabicName)
        {
            string en;
            if (arabicName != null && Map.TryGetValue(arabicName, out en)) return en;
            return arabicName ?? "";
        }
    }
}
