using System;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;

namespace Sherman.WpfReporting.Gui.DialogManagement
{
    public interface IDialogService
    {
        IObservableCollection<IDialog> OpenDialogs { get; }

        bool AnyOpenDialogs { get; }

        Task OpenAsync(IDialog dialog, CancellationToken cancellationToken);

        Task CloseAsync(IDialog dialog, CancellationToken cancellationToken);

        Task<T> AwaitModalAsync<T>(IModalDialog<T> dialog, CancellationToken cancellationToken);

        event EventHandler<IDialog> DialogOpened;

        event EventHandler<IDialog> DialogClosed;
    }
}