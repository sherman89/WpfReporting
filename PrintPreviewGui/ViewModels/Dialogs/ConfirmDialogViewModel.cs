using Caliburn.Micro;
using System;
using System.Threading;
using System.Threading.Tasks;
using Sherman.WpfReporting.Gui.DialogManagement;

namespace Sherman.WpfReporting.Gui.ViewModels.Dialogs
{
    public class ConfirmDialogViewModel : Screen, IModalDialog<bool>
    {
        private readonly TaskCompletionSource<bool> tcs;

        public ConfirmDialogViewModel(string message, string yes, string no)
        {
            Message = message;
            NoText = no;
            YesText = yes;
            tcs = new TaskCompletionSource<bool>();
        }

        public ConfirmDialogViewModel()
        {
            if (!Execute.InDesignMode)
            {
                throw new InvalidOperationException("Parameterless constructor meant to be called only in design mode!");
            }

            Message = "Are you sure you want to use the designer?";
            NoText = "No";
            YesText = "Yes";
        }

        public string Message { get; }
        public string NoText { get; }
        public string YesText { get; }

        private bool isDialogEnabled;
        public bool IsDialogEnabled
        {
            get => isDialogEnabled;
            set
            {
                isDialogEnabled = value;
                NotifyOfPropertyChange(() => IsDialogEnabled);
            }
        }

        public async Task<bool> ConfirmAsync(CancellationToken cancellationToken)
        {
            using (cancellationToken.Register(() => { tcs.TrySetCanceled(); }))
            {
                return await tcs.Task;
            }
        }

        public void No()
        {
            tcs.TrySetResult(false);
        }

        public void Yes()
        {
            tcs.TrySetResult(true);
        }
    }
}
