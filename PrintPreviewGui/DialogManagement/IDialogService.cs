using System;
using System.Threading.Tasks;
using Caliburn.Micro;

namespace Sherman.WpfReporting.Gui.DialogManagement
{
    public interface IDialogService
    {
        IObservableCollection<IDialog> OpenDialogs { get; }

        bool AnyOpenDialogs { get; }

        void Open(IDialog dialog);

        void Close(IDialog dialog);

        Task<T> OpenModalAsync<T>(IModalDialog<T> dialog);

        event EventHandler<IDialog> DialogOpened;

        event EventHandler<IDialog> DialogClosed;
    }
}