using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Autofac;
using Caliburn.Micro;
using Sherman.WpfReporting.Gui.DialogManagement;
using Sherman.WpfReporting.Gui.ViewModels;
using Sherman.WpfReporting.Lib;

namespace Sherman.WpfReporting.Gui
{
    public class Bootstrapper : BootstrapperBase
    {
        private static IContainer _container;

        public Bootstrapper()
        {
            Initialize();
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            SetupExceptionHandlers();
            DisplayRootViewFor<ShellViewModel>();
        }

        protected override void Configure()
        {
            var builder = new ContainerBuilder();

            builder.RegisterAssemblyTypes(typeof(Bootstrapper).Assembly)
                .AssignableTo<Screen>()
                .AsSelf();

            builder.RegisterType<WindowManager>()
                .AsImplementedInterfaces()
                .SingleInstance();

            builder.RegisterType<EventAggregator>()
                .AsImplementedInterfaces()
                .SingleInstance();

            builder.RegisterType<Printing>()
                .As<IPrinting>()
                .SingleInstance();

            builder.RegisterType<Paginator>()
                .As<IPaginator>()
                .SingleInstance();

            builder.RegisterType<DialogService>()
                .As<IDialogService>()
                .InstancePerLifetimeScope();

            builder.RegisterType<Dispatcher>()
                .As<IDispatcher>()
                .SingleInstance();

            _container = builder.Build();
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            var type = typeof(IEnumerable<>).MakeGenericType(service);
            return _container.Resolve(type) as IEnumerable<object>;
        }

        protected override object GetInstance(Type service, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                if (_container.IsRegistered(service))
                {
                    return _container.Resolve(service);
                }
            }
            else
            {
                if (_container.IsRegisteredWithKey(key, service))
                {
                    return _container.ResolveKeyed(key, service);
                }
            }

            var msg = $"Could not locate any instances of contract {key ?? service.Name}.";
            throw new Exception(msg);
        }

        protected override void BuildUp(object instance)
        {
            _container.InjectProperties(instance);
        }

        private static void SetupExceptionHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, a) =>
            {
                HandleException((Exception)a.ExceptionObject);
            };

            Application.Current.DispatcherUnhandledException += (s, a) =>
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
