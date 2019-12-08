using Caliburn.Micro;
using Sherman.WpfReporting.Gui.DialogManagement;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sherman.WpfReporting.Gui.ViewModels.Dialogs
{
    public class ProgressDialogViewModel : Screen, IDialog
    {
        private CancellationTokenSource cts;

        public ProgressDialogViewModel(bool isCancellingAllowed)
        {
            IsCancellingAllowed = isCancellingAllowed;
        }

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

        public CancellationToken DialogCancellationToken => cts.Token;

        public bool IsCancellingAllowed { get; }

        public void Cancel()
        {
            cts?.Cancel();
        }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await base.OnActivateAsync(cancellationToken);
            cts = new CancellationTokenSource();
        }

        protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            try
            {
                await base.OnDeactivateAsync(close, cancellationToken);

                try
                {
                    cts?.Dispose();
                }
                catch (ObjectDisposedException)
                {
                }
            }
            catch(OperationCanceledException)
            {
            }
        }
    }
}
