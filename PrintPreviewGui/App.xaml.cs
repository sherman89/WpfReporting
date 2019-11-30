using System;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;

namespace PrintPreviewGui
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            SetupExceptionHandlers();
            base.OnStartup(e);
        }

        private static void SetupExceptionHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, a) =>
            {
                HandleException((Exception)a.ExceptionObject);
            };

            Current.DispatcherUnhandledException += (s, a) =>
            {
                HandleException(a.Exception);
                a.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, a) =>
            {
                HandleException(a.Exception);
                a.SetObserved();
            };

            Coroutine.Completed += (s, a) =>
            {
                if (a.Error != null)
                {
                    HandleException(a.Error);
                }
            };
        }

        private static void HandleException(Exception exception)
        {
            MessageBox.Show(exception.GetBaseException().Message, "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
