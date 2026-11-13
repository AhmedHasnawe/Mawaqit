using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Mawaqit.Core;

namespace Mawaqit.Controls
{
    /// <summary>ما الذي وقعت عليه نقرة الفأرة على القرص</summary>
    public enum ClockHit { None, NextPrayer, PrevPrayer }

    /// <summary>
    /// وجه الساعة الكلاسيكي: عقارب الساعة والدقيقة والثانية،
    /// ومعها صلاة واحدة فقط تُرسم بعقربَي ساعة ودقيقة بلونها، على رأس كلٍّ
    /// منهما حرفها. المستخدم ينتقل إلى الصلاة التالية بزرّ على القرص.
    /// كل الرسم يدوي عبر OnRender لضمان الدقة والنعومة عند أي حجم.
    /// </summary>
    public class ClockFace : FrameworkElement
    {
        // ---------- مقياس التصغير ----------
        // كل المقاسات نسبية لنصف القطر، فلو تُركت على حالها لاختفى كل نص عند
        // التصغير. لذلك تكبر النسب كلما صغرت الساعة، وتُحذف العناصر الأقل
        // أهمية عند حدود محسوبة بالبكسل لا بالنسبة.

        /// <summary>نصف القطر الذي تبدأ عنده النسب الكاملة</summary>
        private const double FullRadius = 110.0;
        /// <summary>نصف القطر الأصغر المدعوم (ساعة بقطر 120 بكسل)</summary>
        private const double TinyRadius = 56.0;

        // حدود إظهار العناصر، بالبكسل
        private const double MinRadiusForMinuteTicks = 68.0;
        private const double MinRadiusForNumerals = 92.0;
        private const double MinRadiusForCity = 100.0;
        private const double MinRadiusForCountdown = 84.0;

        // أصغر حجم خط مقروء، بالبكسل
        private const double MinFontCity = 10.0;
        private const double MinFontLabel = 10.0;
        private const double MinFontCountdown = 11.0;

        /// <summary>0 عند أصغر حجم، و1 عند الحجم الكامل</summary>
        private static double Scale(double R)
        {
            double t = (R - TinyRadius) / (FullRadius - TinyRadius);
            return t < 0 ? 0 : (t > 1 ? 1 : t);
        }

        private static double Lerp(double tiny, double full, double t)
        {
            return tiny + (full - tiny) * t;
        }

        // ---------- هندسة عقربي الصلاة ----------
        // تُعرض هذه الدوال علناً كي تقيسها الاختبارات من المصدر نفسه بدل
        // تكرار المعادلة. عند التصغير يكبر الرأس نسبياً ويقترب عقرب الساعات
        // من المركز، فيبقى ما بين الحلقتين أوسع من قطر الرأس دائماً.

        /// <summary>نصف قطر رأس عقرب الساعات</summary>
        public static double TipRadiusFor(double R) { return R * Lerp(0.112, 0.062, Scale(R)); }
        /// <summary>نصف قطر عقرب ساعات الصلاة</summary>
        public static double HourRadiusFor(double R) { return R * Lerp(0.345, 0.430, Scale(R)); }
        /// <summary>نصف قطر عقرب دقائق الصلاة</summary>
        public static double MinuteRadiusFor(double R) { return R * 0.600; }
        /// <summary>رأس عقرب الدقائق أصغر قليلاً، ويتساوى معه عند أصغر الأحجام</summary>
        public static double MinuteTipFactorFor(double R) { return Lerp(1.0, 0.86, Scale(R)); }
        /// <summary>حجم حرف الصلاة داخل رأس قطره tr</summary>
        public static double LetterFontFor(double tr) { return tr * 1.50; }

        // أنصاف أقطار عقربي الصلاة ورأسيهما، بدلالة نصف قطر الساعة.
        // عند التصغير يكبر الرأس ويقترب عقرب الساعات من المركز، فيبقى الفارق
        // بين الحلقتين أكبر من قطر الرأس دائماً فلا يتلامسان.

        private AppSettings _settings = new AppSettings();
        private DateTime _now = DateTime.Now;
        /// <summary>وقت الصلاة المعروضة بالساعات (0-24)، أو NaN إن لم توجد</summary>
        private double _shownHour = double.NaN;
        private int _shownTime = -1;               // (int)TimeId أو -1 لا شيء
        private bool _isPast;                      // هل مضى وقت الصلاة المعروضة
        private string _countdown = "";
        private string _prayerName = "";
        private string _timeText = "";

        // مناطق الضغط على زرّي التنقّل، تُحسب أثناء الرسم
        private Rect _nextZone, _prevZone;
        private bool _zonesValid;

        public AppSettings Settings
        {
            get { return _settings; }
            set { _settings = value; InvalidateVisual(); }
        }

        /// <summary>أي زرّ تقع عليه هذه النقطة، إن وُجد</summary>
        public ClockHit HitTest(Point p)
        {
            if (!_zonesValid) return ClockHit.None;
            if (_nextZone.Contains(p)) return ClockHit.NextPrayer;
            if (_prevZone.Contains(p)) return ClockHit.PrevPrayer;
            return ClockHit.None;
        }

        /// <summary>تحديث البيانات المعروضة دفعة واحدة</summary>
        /// <param name="shownTime">(int)TimeId للصلاة المعروضة، أو -1 إن لم تكن هناك واحدة</param>
        /// <param name="prayerName">اسم الصلاة، يُسقَط وحده إذا ضاق القرص</param>
        /// <param name="timeText">وقتها مكتوباً</param>
        public void UpdateData(DateTime now, int shownTime, double shownHour,
                               string prayerName, string timeText, string countdown, bool isPast)
        {
            _now = now;
            _shownHour = shownHour;
            _shownTime = shownTime;
            _isPast = isPast;
            _prayerName = prayerName;
            _timeText = timeText;
            _countdown = countdown;
            InvalidateVisual();
        }

        // ---------- أدوات ----------

        private static Brush B(string hex, double opacity)
        {
            var c = C(hex);
            c.A = (byte)Math.Round(c.A * Math.Max(0.0, Math.Min(1.0, opacity)));
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        private static Brush B(string hex)
        {
            try
            {
                var c = (Color)ColorConverter.ConvertFromString(hex);
                var b = new SolidColorBrush(c);
                b.Freeze();
                return b;
            }
            catch (FormatException) { return Brushes.Gray; }
            catch (NotSupportedException) { return Brushes.Gray; }
        }

        private static Color C(string hex)
        {
            try { return (Color)ColorConverter.ConvertFromString(hex); }
            catch (FormatException) { return Colors.Gray; }
            catch (NotSupportedException) { return Colors.Gray; }
        }

        private static Pen P(string hex, double thickness, double opacity = 1.0)
        {
            var c = C(hex);
            if (opacity < 1.0) c.A = (byte)Math.Round(c.A * opacity);
            var br = new SolidColorBrush(c); br.Freeze();
            var p = new Pen(br, thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            p.Freeze();
            return p;
        }

        /// <summary>تحويل ساعة (0-24) إلى زاوية على قرص 12 ساعة، بالدرجات من الأعلى</summary>
        private static double HourToAngle(double hours)
        {
            double h = hours % 12.0;
            if (h < 0) h += 12.0;
            return h * 30.0;
        }

        private static Point Pt(Point c, double angleDeg, double r)
        {
            double a = (angleDeg - 90.0) * Math.PI / 180.0;
            return new Point(c.X + r * Math.Cos(a), c.Y + r * Math.Sin(a));
        }

        private double Dpi
        {
            get
            {
                try { return VisualTreeHelper.GetDpi(this).PixelsPerDip; }
                catch (InvalidOperationException) { return 1.0; }
            }
        }

        /// <summary>
        /// نُبقي اتجاه الفقرة من اليسار إلى اليمين دائماً، ونتكفّل بالتوسيط يدوياً
        /// اعتماداً على عرض النص. خوارزمية Unicode ثنائية الاتجاه تتكفّل بعرض
        /// الحروف العربية من اليمين إلى اليسار داخل النص نفسه.
        /// </summary>
        private FormattedText Text(string s, double size, Brush brush, bool bold = false,
                                   string family = null)
        {
            var tf = new Typeface(new FontFamily(family ?? _settings.FontFamily ?? "Segoe UI"),
                                  FontStyles.Normal,
                                  bold ? FontWeights.Bold : FontWeights.Normal,
                                  FontStretches.Normal);
            return new FormattedText(s ?? "", L.IsRtl ? CultureInfo.GetCultureInfo("ar-IQ") : CultureInfo.InvariantCulture,
                                     FlowDirection.LeftToRight,
                                     tf, Math.Max(1.0, size), brush, Dpi);
        }

        /// <summary>يرسم النص بحيث يقع مركزه تماماً على النقطة المعطاة</summary>
        private void DrawCentered(DrawingContext dc, FormattedText ft, Point center)
        {
            dc.DrawText(ft, new Point(center.X - ft.Width / 2.0, center.Y - ft.Height / 2.0));
        }

        // ---------- الرسم ----------

        protected override void OnRender(DrawingContext dc)
        {
            double w = ActualWidth, h = ActualHeight;
            if (w < 20 || h < 20) return;

            var s = _settings;
            var center = new Point(w / 2.0, h / 2.0);
            double R = Math.Min(w, h) / 2.0 - 4;
            if (R <= 10) return;

            // خلفية شفافة تلتقط نقرات الفأرة لكامل المربع
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, w, h));

            DrawDial(dc, center, R, s);
            if (s.ShowRemainingArc) DrawRemainingArc(dc, center, R, s);
            DrawPrayerHand(dc, center, R, s);
            DrawClockHands(dc, center, R, s);
            DrawPanels(dc, center, R, s);
        }

        private void DrawDial(DrawingContext dc, Point center, double R, AppSettings s)
        {
            // القرص
            var face = C(s.ColorFace);
            var inner = Color.FromArgb(face.A,
                (byte)Math.Min(255, face.R + 18), (byte)Math.Min(255, face.G + 18), (byte)Math.Min(255, face.B + 18));
            var grad = new RadialGradientBrush(inner, face) { GradientOrigin = new Point(0.5, 0.35), Center = new Point(0.5, 0.42) };
            grad.Freeze();
            dc.DrawEllipse(grad, null, center, R, R);

            // الإطار
            dc.DrawEllipse(null, P(s.ColorRim, Math.Max(2.0, R * 0.022)), center, R - R * 0.011, R - R * 0.011);
            dc.DrawEllipse(null, P(s.ColorRim, Math.Max(1.0, R * 0.006), 0.55), center, R * 0.93, R * 0.93);

            // الشعيرات — شعيرات الدقائق تختفي أولاً عند التصغير، ثم تتميّز
            // شعيرات 12 و3 و6 و9 لتقوم مقام الأرقام
            bool minuteTicks = R >= MinRadiusForMinuteTicks;
            bool numerals = s.Numerals != NumeralStyle.None && R >= MinRadiusForNumerals;

            var minorPen = P(s.ColorTicks, Math.Max(0.8, R * 0.006), 0.55);
            var majorPen = P(s.ColorTicks, Math.Max(1.8, R * 0.016));
            var cardinalPen = P(s.ColorTicks, Math.Max(2.4, R * 0.026));

            for (int i = 0; i < 60; i++)
            {
                bool major = (i % 5 == 0);
                if (!major && !minuteTicks) continue;
                bool cardinal = (i % 15 == 0);
                double ang = i * 6.0;
                double r1 = major ? R * 0.845 : R * 0.875;
                if (cardinal && !numerals) r1 = R * 0.780;
                var pen = cardinal && !numerals ? cardinalPen : (major ? majorPen : minorPen);
                dc.DrawLine(pen, Pt(center, ang, r1), Pt(center, ang, R * 0.915));
            }

            // الأرقام
            if (numerals)
            {
                string[] labels = NumeralLabels(s.Numerals);
                double size = R * 0.125;
                var brush = B(s.ColorNumerals);
                for (int i = 0; i < 12; i++)
                {
                    int hour = (i == 0) ? 12 : i;
                    var p = Pt(center, i * 30.0, R * 0.745);
                    DrawCentered(dc, Text(labels[hour - 1], size, brush, true), p);
                }
            }
        }

        private static string[] NumeralLabels(NumeralStyle style)
        {
            switch (style)
            {
                case NumeralStyle.Arabic:
                    return new[] { "١", "٢", "٣", "٤", "٥", "٦", "٧", "٨", "٩", "١٠", "١١", "١٢" };
                case NumeralStyle.Roman:
                    return new[] { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII" };
                default:
                    return new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" };
            }
        }

        /// <summary>قوس يمتد من الوقت الحالي إلى الصلاة القادمة</summary>
        private void DrawRemainingArc(DrawingContext dc, Point center, double R, AppSettings s)
        {
            if (_shownTime < 0 || _isPast) return;
            double target = _shownHour;
            if (double.IsNaN(target)) return;

            double nowH = _now.Hour + _now.Minute / 60.0 + _now.Second / 3600.0;
            double a0 = HourToAngle(nowH);
            double a1 = HourToAngle(target);
            double sweep = a1 - a0;
            while (sweep < 0) sweep += 360.0;
            if (sweep < 0.4 || sweep > 359.6) return;

            double r = R * 0.955;
            var geo = new StreamGeometry();
            using (var g = geo.Open())
            {
                g.BeginFigure(Pt(center, a0, r), false, false);
                g.ArcTo(Pt(center, a1, r), new Size(r, r), 0,
                        sweep > 180.0, SweepDirection.Clockwise, true, false);
            }
            geo.Freeze();
            dc.DrawGeometry(null, P(s.ColorHighlight, Math.Max(2.0, R * 0.028), 0.70), geo);
        }

        /// <summary>
        /// الصلاة المعروضة وحدها: عقرب ساعات وعقرب دقائق بلونها، وعلى رأس كلٍّ
        /// منهما حرفها، فيُقرأ وقتها من القرص تماماً كما يُقرأ الوقت الحالي.
        /// </summary>
        private void DrawPrayerHand(DrawingContext dc, Point center, double R, AppSettings s)
        {
            if (_shownTime < 0 || _shownTime >= PrayerTimesCalculator.NamesAr.Length) return;
            double t = _shownHour;
            if (double.IsNaN(t)) return;

            var id = (TimeId)_shownTime;
            string col = s.ColorPrayers[PrayerTimesCalculator.ColorIndex(id)];
            string letter = PrayerTimesCalculator.Letters[_shownTime];

            int hh = (int)Math.Floor(t);
            double mm = (t - hh) * 60.0;
            double aHour = ((hh % 12) + mm / 60.0) * 30.0;
            double aMin = mm * 6.0;

            double rHour = HourRadiusFor(R);
            double rMin = MinuteRadiusFor(R);
            double tipR = TipRadiusFor(R);
            double op = _isPast ? 0.55 : 1.0;

            if (s.ShowPrayerHands)
            {
                // هالة خفيفة تفصل عقربي الصلاة عن عقربي الوقت الحالي
                dc.DrawLine(P(s.ColorFace, Math.Max(5.0, R * 0.044), 0.85),
                            Pt(center, aHour, -R * 0.05), Pt(center, aHour, rHour));
                dc.DrawLine(P(s.ColorFace, Math.Max(4.0, R * 0.034), 0.85),
                            Pt(center, aMin, -R * 0.05), Pt(center, aMin, rMin));

                dc.DrawLine(P(col, Math.Max(2.6, R * 0.026), op),
                            Pt(center, aHour, -R * 0.05), Pt(center, aHour, rHour));
                dc.DrawLine(P(col, Math.Max(1.8, R * 0.018), op),
                            Pt(center, aMin, -R * 0.05), Pt(center, aMin, rMin));
            }

            DrawPrayerTip(dc, center, aHour, rHour, tipR, col, letter, op, s, R);
            DrawPrayerTip(dc, center, aMin, rMin, tipR * MinuteTipFactorFor(R), col, letter, op, s, R);
        }

        /// <summary>رأس عقرب الصلاة: قرص ملوّن عليه حرفها</summary>
        private void DrawPrayerTip(DrawingContext dc, Point center, double ang, double r,
                                   double tr, string col, string letter, double op,
                                   AppSettings s, double R)
        {
            var tip = Pt(center, ang, r);
            if (!_isPast)
                dc.DrawEllipse(null, P(s.ColorHighlight, Math.Max(1.4, R * 0.012), 0.9),
                               tip, tr + R * 0.016, tr + R * 0.016);
            dc.DrawEllipse(B(col, op), P(s.ColorFace, Math.Max(1.0, R * 0.007)), tip, tr, tr);
            DrawCentered(dc, Text(letter, LetterFontFor(tr), B(s.ColorFace), true), tip);
        }

        private void DrawClockHands(DrawingContext dc, Point center, double R, AppSettings s)
        {
            double sec = _now.Second + (s.SmoothSeconds ? _now.Millisecond / 1000.0 : 0);
            double min = _now.Minute + sec / 60.0;
            double hr = (_now.Hour % 12) + min / 60.0;

            double aH = hr * 30.0, aM = min * 6.0, aS = sec * 6.0;

            // ظل خفيف
            var shadow = P("#55000000", Math.Max(2.0, R * 0.030));
            dc.DrawLine(shadow, Pt(center, aH, -R * 0.06), Pt(center, aH, R * 0.47 + 1.5));

            DrawTaperedHand(dc, center, aH, R * 0.47, Math.Max(3.5, R * 0.042), s.ColorHourHand, R);
            DrawTaperedHand(dc, center, aM, R * 0.70, Math.Max(2.5, R * 0.028), s.ColorMinuteHand, R);

            if (s.ShowSeconds)
            {
                var pen = P(s.ColorSecondHand, Math.Max(1.2, R * 0.012));
                dc.DrawLine(pen, Pt(center, aS, -R * 0.14), Pt(center, aS, R * 0.80));
                dc.DrawEllipse(B(s.ColorSecondHand), null, Pt(center, aS, R * 0.80), R * 0.022, R * 0.022);
            }

            // المسمار المركزي
            dc.DrawEllipse(B(s.ColorHourHand), null, center, R * 0.040, R * 0.040);
            dc.DrawEllipse(B(s.ColorFace), null, center, R * 0.018, R * 0.018);
        }

        /// <summary>عقرب مدبّب على شكل معيّن ممدود</summary>
        private void DrawTaperedHand(DrawingContext dc, Point c, double ang, double len,
                                     double width, string color, double R)
        {
            var tip = Pt(c, ang, len);
            var tail = Pt(c, ang, -R * 0.085);
            var left = Pt(c, ang - 90, width / 2.0);
            var right = Pt(c, ang + 90, width / 2.0);
            var l2 = new Point(left.X + (tail.X - c.X) * 0.35, left.Y + (tail.Y - c.Y) * 0.35);
            var r2 = new Point(right.X + (tail.X - c.X) * 0.35, right.Y + (tail.Y - c.Y) * 0.35);

            var geo = new StreamGeometry();
            using (var g = geo.Open())
            {
                g.BeginFigure(tip, true, true);
                g.LineTo(right, true, true);
                g.LineTo(r2, true, true);
                g.LineTo(tail, true, true);
                g.LineTo(l2, true, true);
                g.LineTo(left, true, true);
            }
            geo.Freeze();
            dc.DrawGeometry(B(color), null, geo);
        }

        /// <summary>
        /// اسم المدينة أعلى المركز، والصلاة القادمة ووقتها والعدّ التنازلي أسفله.
        /// كل سطر يُرسم فوق لوحة شبه شفافة بلون القرص كي يبقى مقروءاً مهما مرّ
        /// خلفه من عقارب.
        /// </summary>
        private void DrawPanels(DrawingContext dc, Point center, double R, AppSettings s)
        {
            if (!s.ShowDigitalPanel) { _zonesValid = false; return; }

            double k = Scale(R);
            var textBrush = B(s.ColorText);
            var hotBrush = B(s.ColorHighlight);

            // المدينة أول ما يُستغنى عنه عند التصغير
            string city = s.DisplayCityName;
            if (!string.IsNullOrEmpty(city) && R >= MinRadiusForCity)
                DrawPlated(dc, Text(city, Math.Max(MinFontCity, R * 0.080), textBrush),
                           new Point(center.X, center.Y - R * 0.285), R, s);

            // سطر الصلاة يبقى في كل الأحجام، فهو جوهر البرنامج. وإن ضاق
            // القرص عن الاسم والوقت معاً اكتُفي بالوقت، والحرفُ على رأس
            // العقرب يدلّ على الصلاة.
            // وكلما صغر القرص نزلت اللوحة عن مركزه، كي لا تحجب مسمار
            // العقارب وأصولها فيضيع الوقت الحالي — وهو أهمّ ما على الساعة.
            double nameBottom = center.Y;
            if (string.IsNullOrEmpty(_timeText) && string.IsNullOrEmpty(_prayerName))
            {
                _zonesValid = false;
            }
            else
            {
                double size = Math.Max(MinFontLabel, R * 0.088);
                var full = Text(JoinLabel(_prayerName, _timeText), size, hotBrush, true);
                var ft = SelectorWidth(full, R) <= R * 1.55
                    ? full
                    : Text(string.IsNullOrEmpty(_timeText) ? _prayerName : _timeText,
                           size, hotBrush, true);

                double y = center.Y + R * Lerp(0.265, 0.190, k);
                double h = SelectorHeight(ft, R);
                DrawPrayerSelector(dc, ft, new Point(center.X, y), R, s);
                nameBottom = y + h / 2.0;
            }

            // العدّاد يُرصّ تحت سطر الصلاة بقياس ارتفاعه الفعلي، لا بنسبة ثابتة
            if (!string.IsNullOrEmpty(_countdown) && R >= MinRadiusForCountdown)
            {
                var cd = Text(_countdown, Math.Max(MinFontCountdown, R * 0.105),
                              _isPast ? B(s.ColorText, 0.6) : textBrush, true, "Consolas");
                double h = cd.Height + R * 0.016 * 2.0;
                DrawPlated(dc, cd, new Point(center.X, nameBottom + h / 2.0 + R * 0.020), R, s);
            }
        }

        private static string JoinLabel(string name, string time)
        {
            if (string.IsNullOrEmpty(name)) return time ?? "";
            if (string.IsNullOrEmpty(time)) return name;
            return name + "  " + time;
        }

        /// <summary>عرض لوحة الاختيار كاملةً بسهميها وهوامشها</summary>
        private static double SelectorWidth(FormattedText ft, double R)
        {
            return ft.Width + R * 0.050 * 2.0 + R * 0.090 * 2.0;
        }

        /// <summary>ارتفاع لوحة الاختيار</summary>
        private static double SelectorHeight(FormattedText ft, double R)
        {
            return Math.Max(ft.Height + R * 0.016 * 2.0, R * 0.130);
        }

        /// <summary>
        /// اسم الصلاة المعروضة ووقتها، وعلى طرفيه سهمان للانتقال إلى الصلاة
        /// التالية أو السابقة. تُسجَّل مساحتا السهمين ليتعرّف عليهما HitTest.
        /// الاتجاه عربي: السهم الأيسر ينتقل إلى التالية.
        /// </summary>
        private void DrawPrayerSelector(DrawingContext dc, FormattedText ft, Point center,
                                        double R, AppSettings s)
        {
            double padX = R * 0.050;
            double arrowBox = R * 0.090;
            double w = SelectorWidth(ft, R);
            double h = SelectorHeight(ft, R);

            var rect = new Rect(center.X - w / 2.0, center.Y - h / 2.0, w, h);
            dc.DrawRoundedRectangle(PlateBrush(s), null, rect, h / 2.0, h / 2.0);

            _nextZone = new Rect(rect.X, rect.Y, arrowBox + padX * 0.6, h);
            _prevZone = new Rect(rect.Right - arrowBox - padX * 0.6, rect.Y, arrowBox + padX * 0.6, h);
            _zonesValid = true;

            double a = R * 0.026;
            var arrowBrush = B(s.ColorText, 0.85);
            DrawTriangle(dc, new Point(_nextZone.X + _nextZone.Width / 2.0, center.Y), a, true, arrowBrush);
            DrawTriangle(dc, new Point(_prevZone.X + _prevZone.Width / 2.0, center.Y), a, false, arrowBrush);

            DrawCentered(dc, ft, center);
        }

        /// <summary>مثلث صغير يشير يساراً أو يميناً</summary>
        private static void DrawTriangle(DrawingContext dc, Point c, double a, bool left, Brush brush)
        {
            double dx = left ? -a : a;
            var geo = new StreamGeometry();
            using (var g = geo.Open())
            {
                g.BeginFigure(new Point(c.X + dx, c.Y), true, true);
                g.LineTo(new Point(c.X - dx * 0.75, c.Y - a), true, true);
                g.LineTo(new Point(c.X - dx * 0.75, c.Y + a), true, true);
            }
            geo.Freeze();
            dc.DrawGeometry(brush, null, geo);
        }

        private static Brush PlateBrush(AppSettings s)
        {
            var face = C(s.ColorFace);
            face.A = (byte)Math.Max(190, face.A * 0.88);
            var plate = new SolidColorBrush(face);
            plate.Freeze();
            return plate;
        }

        /// <summary>يرسم خلفية صغيرة ثم النص فوقها</summary>
        private void DrawPlated(DrawingContext dc, FormattedText ft, Point center,
                                double R, AppSettings s)
        {
            double padX = R * 0.050, padY = R * 0.016;
            var rect = new Rect(center.X - ft.Width / 2.0 - padX,
                                center.Y - ft.Height / 2.0 - padY,
                                ft.Width + padX * 2.0,
                                ft.Height + padY * 2.0);

            dc.DrawRoundedRectangle(PlateBrush(s), null, rect, R * 0.035, R * 0.035);

            DrawCentered(dc, ft, center);
        }
    }
}
