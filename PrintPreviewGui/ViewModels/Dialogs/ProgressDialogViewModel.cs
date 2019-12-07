using Caliburn.Micro;
using Sherman.WpfReporting.Gui.DialogManagement;
using System.Threading;
using System.Threading.Tasks;

namespace Sherman.WpfReporting.Gui.ViewModels.Dialogs
{
    public class ProgressDialogViewModel : Screen, IDialog
    {
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

        protected override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            return base.OnActivateAsync(cancellationToken);
        }
    }
}
