using System;
using System.Threading;
using System.Windows;
using Mawaqit.Core;

namespace Mawaqit
{
    public partial class App : Application
    {
        private Mutex _single;

        protected override void OnStartup(StartupEventArgs e)
        {
            // نسخة واحدة فقط من البرنامج
            bool isNew;
            _single = new Mutex(true, "Mawaqit.SingleInstance.v1", out isNew);
            if (!isNew)
            {
                L.Current = AppSettings.Load().Language;
                MessageBox.Show(L.AlreadyRunning, L.AppName,
                                MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show(L.UnexpectedError + "\n\n" + args.Exception.Message,
                                L.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_single != null)
            {
                try { _single.ReleaseMutex(); } catch (ApplicationException) { }
                _single.Dispose();
                _single = null;
            }
            base.OnExit(e);
        }
    }
}
