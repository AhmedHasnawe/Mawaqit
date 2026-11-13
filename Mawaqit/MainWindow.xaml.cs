using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Win32;
using Mawaqit.Core;
using Forms = System.Windows.Forms;

namespace Mawaqit
{
    public partial class MainWindow : Window
    {
        private AppSettings _s;
        private DispatcherTimer _timer;
        private Forms.NotifyIcon _tray;
        private double[] _today = new double[6];
        private double[] _tomorrow = new double[6];
        private double[] _yesterday = new double[6];
        private DateTime _calcDay = DateTime.MinValue;
        private int _lastNotified = -1;
        private DateTime _lastNotifyDay = DateTime.MinValue;

        /// <summary>الصلاة التي اختارها المستخدم للعرض، أو -1 للصلاة القادمة تلقائياً</summary>
        private int _browse = -1;
        private DateTime _browseAt = DateTime.MinValue;
        /// <summary>آخر فهرس اختارته الآلية التلقائية، نقطةَ انطلاقٍ للتصفّح</summary>
        private int _autoIndex = -1;
        private static readonly TimeSpan BrowseTimeout = TimeSpan.FromSeconds(20);

        /// <summary>أصغر قطر مدعوم؛ دونه لا يبقى النص مقروءاً</summary>
        public const double MinWindowSize = 120;

        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValue = "Mawaqit";

        public MainWindow()
        {
            InitializeComponent();
            _s = AppSettings.Load();

            // المؤقّت والأيقونة قبل ApplySettings، لأنها تستعملهما
            _timer = new DispatcherTimer(DispatcherPriority.Render);
            _timer.Tick += Timer_Tick;
            BuildTray();

            ApplySettings();

            Loaded += (sender, e) => { ApplyClickThrough(); Tick(); };
            Closing += MainWindow_Closing;
        }

        // ================= الإعدادات والعرض =================

        private void RestartTimer()
        {
            if (_timer == null) return;
            _timer.Stop();
            _timer.Interval = _s.SmoothSeconds
                ? TimeSpan.FromMilliseconds(40)
                : TimeSpan.FromMilliseconds(250);
            _timer.Start();
        }

        /// <summary>يضع نصوص القائمة واتجاه النافذة بحسب اللغة المختارة</summary>
        private void ApplyLanguage()
        {
            L.Current = _s.Language;
            FlowDirection = L.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            if (RootMenu != null) RootMenu.FlowDirection = FlowDirection;

            MenuNext.Header = L.MenuNextPrayer;
            MenuPrev.Header = L.MenuPrevPrayer;
            MenuAuto.Header = L.MenuShowUpcoming;
            MenuSettings.Header = L.MenuSettings;
            MenuToday.Header = L.MenuToday;
            MenuTop.Header = L.MenuAlwaysOnTop;
            MenuStartup.Header = L.MenuStartWithWindows;
            MenuLock.Header = L.MenuLockPosition;
            MenuSizeRoot.Header = L.MenuSize;
            MenuSize1.Header = L.SizeTiny;
            MenuSize2.Header = L.SizeSmall;
            MenuSize3.Header = L.SizeMedium;
            MenuSize4.Header = L.SizeLarge;
            MenuSize5.Header = L.SizeHuge;
            MenuLangRoot.Header = L.MenuLanguage;
            MenuLangAr.IsChecked = _s.Language == AppLanguage.Arabic;
            MenuLangEn.IsChecked = _s.Language == AppLanguage.English;
            MenuAbout.Header = L.MenuAbout;
            MenuExit.Header = L.MenuExit;

            RebuildTrayMenu();
        }

        private void SetLanguage(AppLanguage lang)
        {
            if (_s.Language == lang) { ApplyLanguage(); return; }
            _s.Language = lang;
            _s.Save();
            ApplyLanguage();
            Tick();
        }

        private void MenuLangAr_Click(object sender, RoutedEventArgs e) { SetLanguage(AppLanguage.Arabic); }
        private void MenuLangEn_Click(object sender, RoutedEventArgs e) { SetLanguage(AppLanguage.English); }

        public void ApplySettings()
        {
            ApplyLanguage();
            Width = _s.WindowSize;
            Height = _s.WindowSize;
            Topmost = _s.AlwaysOnTop;
            ShowInTaskbar = _s.ShowInTaskbar;
            Opacity = _s.Opacity;

            if (_s.HasPosition && IsOnAnyScreen(_s.WindowLeft, _s.WindowTop, _s.WindowSize))
            {
                Left = _s.WindowLeft;
                Top = _s.WindowTop;
            }
            else if (!_s.HasPosition)
            {
                var wa = SystemParameters.WorkArea;
                Left = wa.Right - Width - 24;
                Top = wa.Top + 24;
            }

            Face.Settings = _s;
            if (MenuTop != null) MenuTop.IsChecked = _s.AlwaysOnTop;
            if (MenuLock != null) MenuLock.IsChecked = _s.LockPosition;
            if (MenuStartup != null) MenuStartup.IsChecked = IsStartupEnabled();

            _browse = -1;                    // ابدأ دائماً من الصلاة القادمة
            _calcDay = DateTime.MinValue;    // أعد الحساب
            RestartTimer();
            ApplyClickThrough();
            Tick();
        }

        private static bool IsOnAnyScreen(double left, double top, double size)
        {
            // تُستدعى أثناء بدء التشغيل، فأي خطأ هنا يجب ألا يمنع ظهور الساعة
            var r = new Forms.Screen[0];
            try { r = Forms.Screen.AllScreens; }
            catch (Exception) { return false; }
            foreach (var sc in r)
            {
                var b = sc.WorkingArea;
                // يكفي أن يكون جزء معقول من النافذة ظاهراً
                if (left + size * 0.5 > b.Left && left + size * 0.5 < b.Right &&
                    top + size * 0.4 > b.Top && top + size * 0.4 < b.Bottom)
                    return true;
            }
            return false;
        }

        public AppSettings CurrentSettings { get { return _s; } }

        public void ReplaceSettings(AppSettings s)
        {
            _s = s;
            _s.Save();
            ApplySettings();
        }

        // ================= الحساب الدوري =================

        private void Timer_Tick(object sender, EventArgs e) { Tick(); }

        private void Tick()
        {
            var now = DateTime.Now;
            EnsureCalculated(now);

            int hi;
            DateTime target;
            TimeId targetId;
            FindHighlight(now, out hi, out target, out targetId);
            _autoIndex = hi;

            // عودة تلقائية إلى الصلاة القادمة بعد توقّف التصفّح
            if (_browse >= 0 && _s.AutoReturnToNext && now - _browseAt > BrowseTimeout)
                _browse = -1;

            int shown = hi;
            TimeId shownId = targetId;
            DateTime shownAt = target;
            bool haveTime = hi >= 0;

            if (_browse >= 0)
            {
                shown = _browse;
                shownId = _s.ActivePrayers[shown];
                double t = _today[(int)shownId];
                haveTime = !double.IsNaN(t);
                shownAt = haveTime ? PrayerTimesCalculator.ToDateTime(now.Date, t) : now;
            }

            // الوقت المعروض يُؤخذ من اللحظة المستهدفة نفسها، لا من جدول اليوم:
            // بعد العشاء يكون الهدف فجرَ الغد، وهو يختلف عن فجر اليوم بدقيقة.
            double shownHour = haveTime
                ? shownAt.Hour + shownAt.Minute / 60.0
                : double.NaN;

            string prayerName = "";
            string timeText = "";
            string countdown = "";
            bool past = false;
            if (shown >= 0)
            {
                prayerName = PrayerTimesCalculator.Names[(int)shownId];
                timeText = PrayerTimesCalculator.Format(shownHour, _s.Use24Hour);
                if (haveTime)
                {
                    var span = shownAt - now;
                    past = span.TotalSeconds < 0;
                    if (past) span = span.Negate();
                    countdown = (past ? "-" : "") +
                        string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}",
                                      (int)span.TotalHours, span.Minutes, span.Seconds);
                }
            }

            Face.UpdateData(now, shown >= 0 ? (int)shownId : -1, shownHour,
                            prayerName, timeText, countdown, past);
            UpdateTrayText(now, hi, targetId, target);
            CheckNotification(now);
        }

        private void EnsureCalculated(DateTime now)
        {
            if (_calcDay.Date == now.Date) return;
            _calcDay = now.Date;
            _today = _s.BuildCalculator(now.Date).Calculate(now.Date);
            var tm = now.Date.AddDays(1);
            _tomorrow = _s.BuildCalculator(tm).Calculate(tm);
            var ys = now.Date.AddDays(-1);
            _yesterday = _s.BuildCalculator(ys).Calculate(ys);
        }

        /// <summary>يحدد الصلاة المُبرزة ووقتها المطلق</summary>
        private void FindHighlight(DateTime now, out int index, out DateTime target, out TimeId id)
        {
            index = -1;
            target = now;
            id = TimeId.Fajr;

            var ids = _s.ActivePrayers;

            if (_s.Highlight == HighlightMode.Next)
            {
                for (int i = 0; i < ids.Length; i++)
                {
                    double t = _today[(int)ids[i]];
                    if (double.IsNaN(t)) continue;
                    var dt = PrayerTimesCalculator.ToDateTime(now.Date, t);
                    if (dt > now) { index = i; target = dt; id = ids[i]; return; }
                }
                // انقضت صلوات اليوم: فجر الغد
                double f = _tomorrow[(int)TimeId.Fajr];
                if (!double.IsNaN(f))
                {
                    index = 0;
                    target = PrayerTimesCalculator.ToDateTime(now.Date.AddDays(1), f);
                    id = TimeId.Fajr;
                }
                return;
            }

            // الأقرب زمنياً في الاتجاهين
            double best = double.MaxValue;
            for (int i = 0; i < ids.Length; i++)
            {
                double t = _today[(int)ids[i]];
                if (double.IsNaN(t)) continue;
                var dt = PrayerTimesCalculator.ToDateTime(now.Date, t);
                double diff = Math.Abs((dt - now).TotalSeconds);
                if (diff < best) { best = diff; index = i; target = dt; id = ids[i]; }
            }
            double fj = _tomorrow[(int)TimeId.Fajr];
            if (!double.IsNaN(fj))
            {
                var dt = PrayerTimesCalculator.ToDateTime(now.Date.AddDays(1), fj);
                double diff = Math.Abs((dt - now).TotalSeconds);
                if (diff < best) { best = diff; index = 0; target = dt; id = TimeId.Fajr; }
            }
            var lastId = ids[ids.Length - 1];
            double lastYesterday = _yesterday[(int)lastId];
            if (!double.IsNaN(lastYesterday))
            {
                var dt = PrayerTimesCalculator.ToDateTime(now.Date.AddDays(-1), lastYesterday);
                double diff = Math.Abs((dt - now).TotalSeconds);
                if (diff < best) { index = ids.Length - 1; target = dt; id = lastId; }
            }
        }

        private void CheckNotification(DateTime now)
        {
            if (!_s.NotifyOnPrayer || _tray == null) return;
            if (_lastNotifyDay.Date != now.Date) { _lastNotifyDay = now.Date; _lastNotified = -1; }

            var ids = _s.ActivePrayers;
            for (int i = 0; i < ids.Length; i++)
            {
                double t = _today[(int)ids[i]];
                if (double.IsNaN(t)) continue;
                var dt = PrayerTimesCalculator.ToDateTime(now.Date, t);
                double sec = (now - dt).TotalSeconds;
                if (sec >= 0 && sec < 50 && _lastNotified < i)
                {
                    _lastNotified = i;
                    string nm = PrayerTimesCalculator.Names[(int)ids[i]];
                    try
                    {
                        _tray.ShowBalloonTip(8000,
                            string.Format(CultureInfo.CurrentCulture, L.PrayerBegunTitle, nm),
                            nm + " — " + PrayerTimesCalculator.Format(t, _s.Use24Hour) +
                            "  (" + _s.DisplayCityName + ")", Forms.ToolTipIcon.Info);
                        if (_s.PlaySoundOnPrayer) System.Media.SystemSounds.Exclamation.Play();
                    }
                    catch (InvalidOperationException) { }
                    break;
                }
            }
        }

        // ================= أيقونة شريط المهام =================

        private void BuildTray()
        {
            try
            {
                _tray = new Forms.NotifyIcon();
                var uri = new Uri("pack://application:,,,/Resources/clock.ico");
                var stream = Application.GetResourceStream(uri);
                if (stream != null) _tray.Icon = new System.Drawing.Icon(stream.Stream);
                _tray.Visible = true;
                _tray.Text = L.AppName;

                _tray.DoubleClick += (a, b) => ToggleVisible();
                RebuildTrayMenu();
            }
            catch (Exception)
            {
                // الساعة تعمل بلا أيقونة شريط المهام إن تعذّر إنشاؤها
                if (_tray != null) { try { _tray.Dispose(); } catch (Exception) { } }
                _tray = null;
            }
        }

        /// <summary>قائمة أيقونة شريط المهام تُبنى من جديد عند تغيير اللغة</summary>
        private void RebuildTrayMenu()
        {
            if (_tray == null) return;
            try
            {
                var old = _tray.ContextMenuStrip;
                var menu = new Forms.ContextMenuStrip
                {
                    RightToLeft = L.IsRtl ? Forms.RightToLeft.Yes : Forms.RightToLeft.No
                };
                menu.Items.Add(L.TrayShowHide, null, (a, b) => ToggleVisible());
                menu.Items.Add(L.MenuSettings, null, (a, b) => OpenSettings());
                menu.Items.Add(L.MenuToday, null, (a, b) => ShowToday());
                menu.Items.Add(new Forms.ToolStripSeparator());
                menu.Items.Add(L.TrayStopClickThrough, null, (a, b) =>
                {
                    _s.ClickThrough = false; _s.Save(); ApplyClickThrough();
                });
                menu.Items.Add(new Forms.ToolStripSeparator());
                menu.Items.Add(L.MenuExit, null, (a, b) => ExitApp());
                _tray.ContextMenuStrip = menu;
                if (old != null) old.Dispose();
            }
            catch (Exception) { }
        }

        private void UpdateTrayText(DateTime now, int hi, TimeId id, DateTime target)
        {
            if (_tray == null) return;
            try
            {
                string txt = L.AppName;
                if (hi >= 0)
                {
                    var span = target - now;
                    if (span.TotalSeconds > 0)
                        txt = string.Format(CultureInfo.CurrentCulture, L.TrayCountdown,
                            PrayerTimesCalculator.Names[(int)id],
                            string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}",
                                          (int)span.TotalHours, span.Minutes));
                }
                if (txt.Length > 62) txt = txt.Substring(0, 62);
                if (_tray.Text != txt) _tray.Text = txt;
            }
            catch (InvalidOperationException) { }
        }

        private void ToggleVisible()
        {
            if (IsVisible) Hide();
            else { Show(); Activate(); }
        }

        // ================= تمرير النقر =================

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_TOOLWINDOW = 0x80;

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private static int GetWindowLongX(IntPtr h, int i)
        {
            return IntPtr.Size == 8 ? (int)GetWindowLongPtr64(h, i).ToInt64() : GetWindowLong32(h, i);
        }
        private static void SetWindowLongX(IntPtr h, int i, int v)
        {
            if (IntPtr.Size == 8) SetWindowLongPtr64(h, i, new IntPtr(v));
            else SetWindowLong32(h, i, v);
        }

        private void ApplyClickThrough()
        {
            try
            {
                var h = new WindowInteropHelper(this).Handle;
                if (h == IntPtr.Zero) return;
                int ex = GetWindowLongX(h, GWL_EXSTYLE);
                ex = _s.ClickThrough ? (ex | WS_EX_TRANSPARENT) : (ex & ~WS_EX_TRANSPARENT);
                if (!_s.ShowInTaskbar) ex |= WS_EX_TOOLWINDOW;
                SetWindowLongX(h, GWL_EXSTYLE, ex);
            }
            catch (EntryPointNotFoundException) { }
            catch (DllNotFoundException) { }
        }

        // ================= تفاعل المستخدم =================

        private void Face_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) { OpenSettings(); return; }

            var hit = Face.HitTest(e.GetPosition(Face));
            if (hit == Controls.ClockHit.NextPrayer) { StepPrayer(1); return; }
            if (hit == Controls.ClockHit.PrevPrayer) { StepPrayer(-1); return; }

            if (_s.LockPosition) return;
            try { DragMove(); }
            catch (InvalidOperationException) { }
        }

        private void Face_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double step = (Keyboard.Modifiers & ModifierKeys.Control) != 0 ? 40 : 8;
            double size = _s.WindowSize + (e.Delta > 0 ? step : -step);
            size = Math.Max(MinWindowSize, Math.Min(1200, size));
            SetSize(size);
        }

        private void SetSize(double size)
        {
            double cx = Left + Width / 2.0, cy = Top + Height / 2.0;
            _s.WindowSize = size;
            Width = size; Height = size;
            Left = cx - size / 2.0; Top = cy - size / 2.0;
            _s.Save();
            Face.InvalidateVisual();
        }

        /// <summary>ينتقل إلى الصلاة التالية (+1) أو السابقة (-1)</summary>
        private void StepPrayer(int delta)
        {
            int n = _s.ActivePrayers.Length;
            int from = _browse >= 0 ? _browse : (_autoIndex >= 0 ? _autoIndex : 0);
            _browse = ((from + delta) % n + n) % n;
            _browseAt = DateTime.Now;
            Tick();
        }

        /// <summary>يعود إلى عرض الصلاة القادمة تلقائياً</summary>
        private void ResetBrowse()
        {
            _browse = -1;
            Tick();
        }

        private void MenuNext_Click(object sender, RoutedEventArgs e) { StepPrayer(1); }
        private void MenuPrev_Click(object sender, RoutedEventArgs e) { StepPrayer(-1); }
        private void MenuAuto_Click(object sender, RoutedEventArgs e) { ResetBrowse(); }

        private void MenuSettings_Click(object sender, RoutedEventArgs e) { OpenSettings(); }
        private void MenuToday_Click(object sender, RoutedEventArgs e) { ShowToday(); }
        private void MenuExit_Click(object sender, RoutedEventArgs e) { ExitApp(); }

        private void MenuTop_Click(object sender, RoutedEventArgs e)
        {
            _s.AlwaysOnTop = MenuTop.IsChecked;
            Topmost = _s.AlwaysOnTop;
            _s.Save();
        }

        private void MenuLock_Click(object sender, RoutedEventArgs e)
        {
            _s.LockPosition = MenuLock.IsChecked;
            _s.Save();
        }

        private void MenuSize_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as System.Windows.Controls.MenuItem;
            double v;
            if (mi != null && double.TryParse(Convert.ToString(mi.Tag, CultureInfo.InvariantCulture),
                                              NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                SetSize(v);
        }

        private void MenuStartup_Click(object sender, RoutedEventArgs e)
        {
            SetStartup(MenuStartup.IsChecked);
            MenuStartup.IsChecked = IsStartupEnabled();
            _s.StartWithWindows = MenuStartup.IsChecked;
            _s.Save();
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this, L.AboutText + L.VersionLine, L.MenuAbout,
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenSettings()
        {
            if (!IsVisible) Show();
            var w = new SettingsWindow(_s.Clone()) { Owner = this };
            if (w.ShowDialog() == true)
                ReplaceSettings(w.Result);
        }

        private void ShowToday()
        {
            var now = DateTime.Now;
            EnsureCalculated(now);
            var sb = new StringBuilder();
            sb.AppendLine(_s.DisplayCityName + "  —  " + now.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture));
            sb.AppendLine(L.TodayMethod + _s.Method.Name);
            sb.AppendLine(L.TodayAsr + (_s.Asr == AsrJuristic.Hanafi ? L.AsrHanafiShort : L.AsrStandardShort));
            sb.AppendLine(new string('-', 34));
            for (int i = 0; i < 6; i++)
                sb.AppendLine(PrayerTimesCalculator.Names[i].PadRight(9) + " : " +
                              PrayerTimesCalculator.Format(_today[i], _s.Use24Hour));
            MessageBox.Show(this, sb.ToString(), L.TodayTitle,
                            MessageBoxButton.OK, MessageBoxImage.None);
        }

        // ================= التشغيل مع ويندوز =================

        public static bool IsStartupEnabled()
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(RunKey, false))
                    return k != null && k.GetValue(RunValue) != null;
            }
            catch (System.Security.SecurityException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        public static void SetStartup(bool on)
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (k == null) return;
                    if (on)
                    {
                        string exe = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        k.SetValue(RunValue, "\"" + exe + "\"");
                    }
                    else if (k.GetValue(RunValue) != null) k.DeleteValue(RunValue, false);
                }
            }
            catch (System.Security.SecurityException) { }
            catch (UnauthorizedAccessException) { }
        }

        // ================= الإغلاق =================

        private void ExitApp()
        {
            SavePosition();
            if (_tray != null) { _tray.Visible = false; _tray.Dispose(); _tray = null; }
            Application.Current.Shutdown();
        }

        private void SavePosition()
        {
            if (!double.IsNaN(Left) && !double.IsNaN(Top))
            {
                _s.WindowLeft = Left;
                _s.WindowTop = Top;
                _s.HasPosition = true;
            }
            _s.Save();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SavePosition();
            if (_tray != null) { _tray.Visible = false; _tray.Dispose(); _tray = null; }
            if (_timer != null) _timer.Stop();
        }
    }
}
