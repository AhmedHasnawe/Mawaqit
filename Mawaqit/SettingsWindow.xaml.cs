using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Mawaqit.Core;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace Mawaqit
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _s;
        private bool _loading = true;
        private readonly TextBox[] _tuneBoxes = new TextBox[6];
        private readonly Dictionary<Button, Action<string>> _setters = new Dictionary<Button, Action<string>>();
        private readonly Dictionary<Button, Func<string>> _getters = new Dictionary<Button, Func<string>>();
        private System.Windows.Threading.DispatcherTimer _previewTimer;

        public AppSettings Result { get { return _s; } }

        public SettingsWindow(AppSettings settings)
        {
            InitializeComponent();
            _s = settings;
            L.Current = _s.Language;
            ApplyLanguage();
            BuildTuneBoxes();
            BuildColorButtons();
            LoadFonts();
            LoadCities();
            LoadValues();
            _loading = false;

            _previewTimer = new System.Windows.Threading.DispatcherTimer
            { Interval = TimeSpan.FromMilliseconds(500) };
            _previewTimer.Tick += (a, b) => RefreshPreview();
            _previewTimer.Start();
            Closed += (a, b) => _previewTimer.Stop();

            RefreshPreview();
        }

        /// <summary>يضع كل نصوص النافذة واتجاهها بحسب اللغة</summary>
        private void ApplyLanguage()
        {
            L.Current = _s.Language;
            FlowDirection = L.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            Title = L.SettingsTitle;

            TabLocation.Header = L.TabLocation;
            TabCalc.Header = L.TabCalculation;
            TabColours.Header = L.TabColours;
            TabAppearance.Header = L.TabAppearance;

            // الموقع
            LblCity.Content = L.City;
            LblSearch.Text = L.Search;
            TxtSearch.ToolTip = L.SearchHint;
            ChkManual.Content = L.ManualCoords;
            LblPlaceName.Content = L.PlaceName;
            LblElevation.Content = L.Elevation;
            LblLat.Content = L.Latitude;
            LblLng.Content = L.Longitude;
            ChkSysTz.Content = L.UseSystemTz;
            LblFixedOffset.Content = L.FixedOffset;

            // الحساب
            LblMethod.Content = L.MethodLabel;
            LblPrayerSet.Content = L.PrayerSetLabel;
            ItmSetFive.Content = L.PrayerSetFive;
            ItmSetThree.Content = L.PrayerSetThree;
            LblSetNote.Text = L.PrayerSetNote;
            LblAsr.Content = L.AsrLabel;
            ItmAsrStd.Content = L.AsrStandard;
            ItmAsrHanafi.Content = L.AsrHanafi;
            LblHighLat.Content = L.HighLatLabel;
            ItmHl0.Content = L.HighLatNone;
            ItmHl1.Content = L.HighLatMidnight;
            ItmHl2.Content = L.HighLatSeventh;
            ItmHl3.Content = L.HighLatAngle;
            LblTune.Content = L.TuneLabel;
            LblTodayTimes.Content = L.TodayWithSettings;

            // الألوان
            LblPreset.Content = L.ThemePreset;
            LblPrayerColours.Content = L.PrayerColours;
            LblClockColours.Content = L.ClockColours;
            LblColoursNote.Text = L.ColoursNote;

            // المظهر والسلوك
            LblLanguage.Content = L.LanguageLabel;
            LblNumerals.Content = L.NumeralsLabel;
            ItmNum0.Content = L.NumeralsArabic;
            ItmNum1.Content = L.NumeralsWestern;
            ItmNum2.Content = L.NumeralsRoman;
            ItmNum3.Content = L.NumeralsNone;
            LblFont.Content = L.FontLabel;
            LblHighlight.Content = L.HighlightLabel;
            ItmHi0.Content = L.HighlightNext;
            ItmHi1.Content = L.HighlightNearest;
            LblSize.Content = L.ClockSize;
            LblOpacity.Content = L.Opacity;

            ChkPrayerHands.Content = L.ShowPrayerHands;
            ChkAutoReturn.Content = L.AutoReturn;
            ChkArc.Content = L.ShowArc;
            ChkPanel.Content = L.ShowPanel;
            ChkSeconds.Content = L.ShowSeconds;
            ChkSmooth.Content = L.SmoothSeconds;
            Chk24.Content = L.Use24Hour;
            ChkTop.Content = L.MenuAlwaysOnTop;
            ChkTaskbar.Content = L.ShowInTaskbar;
            ChkLock.Content = L.LockPositionFull;
            ChkClickThru.Content = L.ClickThrough;
            ChkNotify.Content = L.NotifyOnPrayer;
            ChkSound.Content = L.SoundWithNotify;
            ChkStartup.Content = L.StartWithWindowsFull;
            LblAppearanceNote.Text = L.AppearanceNote;

            LblPreview.Text = L.LivePreview;
            BtnOk.Content = L.Save;
            BtnCancel.Content = L.Cancel;
            BtnReset.Content = L.ResetDefaults;
        }

        private void CmbLanguage_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            var want = CmbLanguage.SelectedIndex == 1 ? AppLanguage.English : AppLanguage.Arabic;
            if (want == _s.Language) return;
            _s.Language = want;

            // أعد بناء كل ما يحمل نصاً: التسميات، والمدن، والطرق، والمجموعات
            ApplyLanguage();
            bool was = _loading;
            _loading = true;
            RefreshCityList();
            int method = CmbMethod.SelectedIndex;
            CmbMethod.ItemsSource = null;
            CmbMethod.ItemsSource = CalcMethod.All;
            CmbMethod.SelectedIndex = method;
            int theme = CmbTheme.SelectedIndex;
            FillThemes();
            CmbTheme.SelectedIndex = theme;
            _loading = was;
            RefreshPreview();
        }

        private void FillThemes()
        {
            CmbTheme.Items.Clear();
            CmbTheme.Items.Add(L.ThemeCustom);
            foreach (var t in ThemePresets.All) CmbTheme.Items.Add(t.Name);
        }

        private void RefreshCityList()
        {
            var keep = LstCities.SelectedItem as City;
            LstCities.ItemsSource = null;
            TxtSearch.Text = "";
            LstCities.ItemsSource = CityDatabase.All;
            if (keep != null) LstCities.SelectedItem = keep;
        }

        // ================= بناء العناصر =================

        private void BuildTuneBoxes()
        {
            for (int i = 0; i < 6; i++)
            {
                var sp = new StackPanel { Margin = new Thickness(3, 0, 3, 0) };
                sp.Children.Add(new TextBlock
                {
                    Text = PrayerTimesCalculator.NamesAr[i],
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 2)
                });
                var tb = new TextBox { Text = "0", TextAlignment = TextAlignment.Center, Height = 26 };
                tb.TextChanged += Recalc_Changed;
                _tuneBoxes[i] = tb;
                sp.Children.Add(tb);
                Grid.SetColumn(sp, i);
                Grid.SetRow(sp, 0);
                GridTune.Children.Add(sp);
            }
        }

        private void AddSwatch(Panel host, string label, Func<string> get, Action<string> set)
        {
            var sp = new StackPanel { Margin = new Thickness(2, 3, 2, 3) };
            sp.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 2),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            var btn = new Button
            {
                Height = 26,
                Style = (Style)FindResource("Swatch"),
                Content = ""
            };
            btn.Click += Swatch_Click;
            _getters[btn] = get;
            _setters[btn] = set;
            sp.Children.Add(btn);
            host.Children.Add(sp);
            RefreshSwatch(btn);
        }

        private void RefreshSwatch(Button b)
        {
            try
            {
                var c = (Color)ColorConverter.ConvertFromString(_getters[b]());
                b.Background = new SolidColorBrush(c);
            }
            catch (FormatException) { b.Background = Brushes.Transparent; }
            catch (NotSupportedException) { b.Background = Brushes.Transparent; }
        }

        private void RefreshAllSwatches()
        {
            foreach (var b in _getters.Keys.ToList()) RefreshSwatch(b);
        }

        private void BuildColorButtons()
        {
            for (int i = 0; i < 5; i++)
            {
                int k = i;
                AddSwatch(GridPrayerColors,
                          PrayerTimesCalculator.Names[(int)PrayerTimesCalculator.FivePrayers[i]],
                          () => _s.ColorPrayers[k],
                          v => { _s.ColorPrayers[k] = v; });
            }

            AddSwatch(GridFaceColors, L.ColDial,        () => _s.ColorFace,       v => _s.ColorFace = v);
            AddSwatch(GridFaceColors, L.ColRim,       () => _s.ColorRim,        v => _s.ColorRim = v);
            AddSwatch(GridFaceColors, L.ColTicks,     () => _s.ColorTicks,      v => _s.ColorTicks = v);
            AddSwatch(GridFaceColors, L.ColNumerals,      () => _s.ColorNumerals,   v => _s.ColorNumerals = v);
            AddSwatch(GridFaceColors, L.ColText,       () => _s.ColorText,       v => _s.ColorText = v);
            AddSwatch(GridFaceColors, L.ColHourHand,  () => _s.ColorHourHand,   v => _s.ColorHourHand = v);
            AddSwatch(GridFaceColors, L.ColMinuteHand, () => _s.ColorMinuteHand, v => _s.ColorMinuteHand = v);
            AddSwatch(GridFaceColors, L.ColSecondHand, () => _s.ColorSecondHand, v => _s.ColorSecondHand = v);
            AddSwatch(GridFaceColors, L.ColHighlight,  () => _s.ColorHighlight,  v => _s.ColorHighlight = v);
        }

        private void LoadFonts()
        {
            string[] preferred = { "Segoe UI", "Tahoma", "Arial", "Times New Roman",
                                   "Calibri", "Traditional Arabic", "Simplified Arabic",
                                   "Sakkal Majalla", "Dubai", "Amiri" };
            var installed = new HashSet<string>(
                Fonts.SystemFontFamilies.Select(f => f.Source), StringComparer.OrdinalIgnoreCase);
            foreach (var f in preferred)
                if (installed.Contains(f)) CmbFont.Items.Add(f);
            if (CmbFont.Items.Count == 0) CmbFont.Items.Add("Segoe UI");
        }

        private void LoadCities()
        {
            LstCities.ItemsSource = CityDatabase.All;
        }

        private void LoadValues()
        {
            // --- الموقع ---
            var city = CityDatabase.Find(_s.CityName);
            ChkManual.IsChecked = (city == null);
            if (city != null) LstCities.SelectedItem = city;
            TxtCityName.Text = _s.CityName;
            TxtLat.Text = _s.Latitude.ToString("0.####", CultureInfo.InvariantCulture);
            TxtLng.Text = _s.Longitude.ToString("0.####", CultureInfo.InvariantCulture);
            TxtElevation.Text = _s.Elevation.ToString("0.#", CultureInfo.InvariantCulture);
            ChkSysTz.IsChecked = _s.UseSystemTimeZone;
            TxtOffset.Text = _s.FallbackOffset.ToString("0.##", CultureInfo.InvariantCulture);
            GridManual.IsEnabled = ChkManual.IsChecked == true;
            TxtOffset.IsEnabled = ChkSysTz.IsChecked != true;
            UpdateTzInfo();

            // --- الحساب ---
            CmbMethod.ItemsSource = CalcMethod.All;
            CmbMethod.SelectedItem = CalcMethod.ById(_s.MethodId);
            CmbLanguage.SelectedIndex = _s.Language == AppLanguage.English ? 1 : 0;
            CmbPrayerSet.SelectedIndex = _s.Prayers == PrayerSet.Three ? 1 : 0;
            CmbAsr.SelectedIndex = _s.Asr == AsrJuristic.Hanafi ? 1 : 0;
            CmbHighLat.SelectedIndex = (int)_s.HighLat;
            for (int i = 0; i < 6; i++)
                _tuneBoxes[i].Text = _s.Tune[i].ToString(CultureInfo.InvariantCulture);

            // --- الألوان ---
            FillThemes();
            CmbTheme.SelectedIndex = 0;

            // --- المظهر والسلوك ---
            CmbNumerals.SelectedIndex = (int)_s.Numerals;
            CmbFont.SelectedItem = _s.FontFamily;
            if (CmbFont.SelectedItem == null && CmbFont.Items.Count > 0) CmbFont.SelectedIndex = 0;
            CmbHighlight.SelectedIndex = (int)_s.Highlight;
            TxtSize.Text = ((int)_s.WindowSize).ToString(CultureInfo.InvariantCulture);
            SldOpacity.Value = _s.Opacity;

            ChkPrayerHands.IsChecked = _s.ShowPrayerHands;
            ChkAutoReturn.IsChecked = _s.AutoReturnToNext;
            ChkArc.IsChecked = _s.ShowRemainingArc;
            ChkPanel.IsChecked = _s.ShowDigitalPanel;
            ChkSeconds.IsChecked = _s.ShowSeconds;
            ChkSmooth.IsChecked = _s.SmoothSeconds;
            Chk24.IsChecked = _s.Use24Hour;
            ChkTop.IsChecked = _s.AlwaysOnTop;
            ChkTaskbar.IsChecked = _s.ShowInTaskbar;
            ChkLock.IsChecked = _s.LockPosition;
            ChkClickThru.IsChecked = _s.ClickThrough;
            ChkNotify.IsChecked = _s.NotifyOnPrayer;
            ChkSound.IsChecked = _s.PlaySoundOnPrayer;
            ChkStartup.IsChecked = MainWindow.IsStartupEnabled();
        }

        // ================= جمع القيم =================

        private static double ParseD(string s, double fallback)
        {
            double v;
            if (double.TryParse((s ?? "").Trim(), NumberStyles.Float,
                                CultureInfo.InvariantCulture, out v)) return v;
            if (double.TryParse((s ?? "").Trim(), NumberStyles.Float,
                                CultureInfo.CurrentCulture, out v)) return v;
            return fallback;
        }

        private static int ParseI(string s, int fallback)
        {
            int v;
            if (int.TryParse((s ?? "").Trim(), NumberStyles.Integer,
                             CultureInfo.InvariantCulture, out v)) return v;
            return fallback;
        }

        private void Collect()
        {
            if (ChkManual.IsChecked == true)
            {
                _s.CityName = string.IsNullOrWhiteSpace(TxtCityName.Text) ? (L.IsRtl ? "موقع مخصّص" : "Custom location") : TxtCityName.Text.Trim();
                _s.Latitude = Math.Max(-90, Math.Min(90, ParseD(TxtLat.Text, _s.Latitude)));
                _s.Longitude = Math.Max(-180, Math.Min(180, ParseD(TxtLng.Text, _s.Longitude)));
                _s.Elevation = Math.Max(0, Math.Min(9000, ParseD(TxtElevation.Text, 0)));
            }
            else
            {
                var c = LstCities.SelectedItem as City;
                if (c != null)
                {
                    _s.CityName = c.NameAr;
                    _s.Latitude = c.Latitude;
                    _s.Longitude = c.Longitude;
                    _s.TimeZoneId = c.TimeZoneId;
                    _s.FallbackOffset = c.FallbackOffset;
                }
            }
            _s.UseSystemTimeZone = ChkSysTz.IsChecked == true;
            if (!_s.UseSystemTimeZone)
                _s.FallbackOffset = Math.Max(-14, Math.Min(14, ParseD(TxtOffset.Text, _s.FallbackOffset)));

            var m = CmbMethod.SelectedItem as CalcMethod;
            if (m != null) _s.MethodId = m.Id;
            _s.Prayers = CmbPrayerSet.SelectedIndex == 1 ? PrayerSet.Three : PrayerSet.Five;
            _s.Asr = CmbAsr.SelectedIndex == 1 ? AsrJuristic.Hanafi : AsrJuristic.Standard;
            _s.HighLat = (HighLatRule)Math.Max(0, CmbHighLat.SelectedIndex);
            for (int i = 0; i < 6; i++)
                _s.Tune[i] = Math.Max(-120, Math.Min(120, ParseI(_tuneBoxes[i].Text, 0)));

            _s.Numerals = (NumeralStyle)Math.Max(0, CmbNumerals.SelectedIndex);
            if (CmbFont.SelectedItem != null) _s.FontFamily = CmbFont.SelectedItem.ToString();
            _s.Highlight = CmbHighlight.SelectedIndex == 1 ? HighlightMode.Nearest : HighlightMode.Next;
            _s.WindowSize = Math.Max(MainWindow.MinWindowSize, Math.Min(1200, ParseD(TxtSize.Text, _s.WindowSize)));
            _s.Opacity = SldOpacity.Value;

            _s.ShowPrayerHands = ChkPrayerHands.IsChecked == true;
            _s.AutoReturnToNext = ChkAutoReturn.IsChecked == true;
            _s.ShowRemainingArc = ChkArc.IsChecked == true;
            _s.ShowDigitalPanel = ChkPanel.IsChecked == true;
            _s.ShowSeconds = ChkSeconds.IsChecked == true;
            _s.SmoothSeconds = ChkSmooth.IsChecked == true;
            _s.Use24Hour = Chk24.IsChecked == true;
            _s.AlwaysOnTop = ChkTop.IsChecked == true;
            _s.ShowInTaskbar = ChkTaskbar.IsChecked == true;
            _s.LockPosition = ChkLock.IsChecked == true;
            _s.ClickThrough = ChkClickThru.IsChecked == true;
            _s.NotifyOnPrayer = ChkNotify.IsChecked == true;
            _s.PlaySoundOnPrayer = ChkSound.IsChecked == true;
            _s.StartWithWindows = ChkStartup.IsChecked == true;
            _s.Validate();
        }

        // ================= المعاينة =================

        private void RefreshPreview()
        {
            if (_loading) return;
            Collect();

            var now = DateTime.Now;
            var times = _s.BuildCalculator(now.Date).Calculate(now.Date);

            // الصلاة القادمة
            int hi = -1; DateTime target = now; TimeId id = TimeId.Fajr;
            var ids = _s.ActivePrayers;
            for (int i = 0; i < ids.Length; i++)
            {
                double t = times[(int)ids[i]];
                if (double.IsNaN(t)) continue;
                var dt = PrayerTimesCalculator.ToDateTime(now.Date, t);
                if (dt > now) { hi = i; target = dt; id = ids[i]; break; }
            }
            if (hi < 0)
            {
                var tmr = now.Date.AddDays(1);
                var tt = _s.BuildCalculator(tmr).Calculate(tmr);
                if (!double.IsNaN(tt[(int)TimeId.Fajr]))
                {
                    hi = 0; id = TimeId.Fajr;
                    target = PrayerTimesCalculator.ToDateTime(tmr, tt[(int)TimeId.Fajr]);
                }
            }
            string label = "", cd = "";
            if (hi >= 0)
            {
                var sp = target - now;
                label = PrayerTimesCalculator.Names[(int)id];
                cd = string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}",
                                   (int)sp.TotalHours, sp.Minutes, sp.Seconds);
            }

            Preview.Settings = _s;
            Preview.UpdateData(now, hi >= 0 ? (int)id : -1,
                               hi >= 0 ? times[(int)id] : double.NaN, label,
                               hi >= 0 ? PrayerTimesCalculator.Format(times[(int)id], _s.Use24Hour) : "",
                               cd, false);

            // جدول الأوقات
            var sb = new StringBuilder();
            for (int i = 0; i < 6; i++)
            {
                sb.Append(PrayerTimesCalculator.Names[i]);
                sb.Append(" : ");
                sb.Append(PrayerTimesCalculator.Format(times[i], _s.Use24Hour));
                sb.Append(i % 2 == 1 ? "\n" : "        ");
            }
            if (TxtPreviewTimes != null) TxtPreviewTimes.Text = sb.ToString();

            var m = _s.Method;
            if (TxtMethodInfo != null)
            {
                TxtMethodInfo.Text = L.FajrAngleIs +
                    m.FajrAngle.ToString("0.##", CultureInfo.InvariantCulture) + "°" +
                    (m.IshaMinutes.HasValue
                        ? L.IshaAfterMaghrib + m.IshaMinutes.Value.ToString("0", CultureInfo.InvariantCulture) + L.Minutes
                        : L.IshaAngleIs + m.IshaAngle.ToString("0.##", CultureInfo.InvariantCulture) + "°") +
                    (m.MaghribMinutes.HasValue
                        ? L.MaghribAtSunset
                        : L.MaghribAngleIs + m.MaghribAngle.ToString("0.##", CultureInfo.InvariantCulture) + L.AfterSunset);
            }
            UpdateTzInfo();
        }

        private void UpdateTzInfo()
        {
            if (TxtTzInfo == null) return;
            try
            {
                double off = _s.OffsetAt(DateTime.Now);
                string sign = off >= 0 ? "+" : "-";
                var abs = TimeSpan.FromHours(Math.Abs(off));
                    TxtTzInfo.Text = L.TzNow + sign +
                                 ((int)abs.TotalHours).ToString("00", CultureInfo.InvariantCulture) + ":" +
                                 abs.Minutes.ToString("00", CultureInfo.InvariantCulture) +
                                 (_s.UseSystemTimeZone ? L.TzFromWindows : L.TzFixed);
            }
            catch (ArgumentException) { }
        }

        // ================= الأحداث =================

        private void Recalc_Changed(object sender, EventArgs e) { RefreshPreview(); }
        private void Preview_Changed(object sender, EventArgs e) { RefreshPreview(); }

        private void LstCities_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            var c = LstCities.SelectedItem as City;
            if (c == null) return;
            ChkManual.IsChecked = false;
            TxtCityName.Text = c.NameAr;
            TxtLat.Text = c.Latitude.ToString("0.####", CultureInfo.InvariantCulture);
            TxtLng.Text = c.Longitude.ToString("0.####", CultureInfo.InvariantCulture);
            _s.TimeZoneId = c.TimeZoneId;
            _s.FallbackOffset = c.FallbackOffset;
            TxtOffset.Text = c.FallbackOffset.ToString("0.##", CultureInfo.InvariantCulture);

            // اقتراح طريقة الحساب المناسبة للبلد
            var m = CalcMethod.ById(c.SuggestedMethod);
            if (m != null) CmbMethod.SelectedItem = m;

            RefreshPreview();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string q = (TxtSearch.Text ?? "").Trim();
            if (q.Length == 0) { LstCities.ItemsSource = CityDatabase.All; return; }
            LstCities.ItemsSource = CityDatabase.All.Where(c =>
                (c.NameAr ?? "").Contains(q) ||
                (c.CountryAr ?? "").Contains(q) ||
                (c.NameEn ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }

        private void ChkManual_Changed(object sender, RoutedEventArgs e)
        {
            if (GridManual != null) GridManual.IsEnabled = ChkManual.IsChecked == true;
            RefreshPreview();
        }

        private void ChkSysTz_Changed(object sender, RoutedEventArgs e)
        {
            if (TxtOffset != null) TxtOffset.IsEnabled = ChkSysTz.IsChecked != true;
            RefreshPreview();
        }

        private void CmbTheme_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            int i = CmbTheme.SelectedIndex - 1;
            if (i < 0 || i >= ThemePresets.All.Length) return;
            _s.ApplyTheme(ThemePresets.All[i]);
            RefreshAllSwatches();
            RefreshPreview();
        }

        private void Swatch_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null || !_getters.ContainsKey(btn)) return;

            Color current;
            try { current = (Color)ColorConverter.ConvertFromString(_getters[btn]()); }
            catch (FormatException) { current = Colors.White; }
            catch (NotSupportedException) { current = Colors.White; }

            using (var dlg = new Forms.ColorDialog())
            {
                dlg.FullOpen = true;
                dlg.AnyColor = true;
                dlg.Color = Drawing.Color.FromArgb(current.R, current.G, current.B);
                if (dlg.ShowDialog() != Forms.DialogResult.OK) return;
                var c = Color.FromArgb(current.A == 0 ? (byte)255 : current.A,
                                       dlg.Color.R, dlg.Color.G, dlg.Color.B);
                _setters[btn]("#" + c.A.ToString("X2") + c.R.ToString("X2") +
                              c.G.ToString("X2") + c.B.ToString("X2"));
            }
            CmbTheme.SelectedIndex = 0;      // صار مخصّصاً
            RefreshSwatch(btn);
            RefreshPreview();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(this, L.ConfirmReset, L.Confirm,
                                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            _s = new AppSettings();
            _loading = true;
            LoadValues();
            RefreshAllSwatches();
            _loading = false;
            RefreshPreview();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Collect();
            MainWindow.SetStartup(_s.StartWithWindows);
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
