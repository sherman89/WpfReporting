using System;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Sherman.WpfReporting.Gui.DialogManagement;

namespace Sherman.WpfReporting.Gui.ViewModels
{
    public class ShellViewModel : Conductor<IScreen>
    {
        private readonly MainViewModel mainViewModel;

        public ShellViewModel(IDialogService dialogService, MainViewModel mainViewModel)
        {
            DialogService = dialogService;
            this.mainViewModel = mainViewModel;
        }

        public ShellViewModel()
        {
            if (!Execute.InDesignMode)
            {
                throw new InvalidOperationException("Parameterless constructor meant to be called only in design mode!");
            }

            ActiveItem = new MainViewModel();
        }

        public IDialogService DialogService { get; }

        protected override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            ActiveItem = mainViewModel;
            return base.OnActivateAsync(cancellationToken);
            // base.OnActivateAsync takes care of activating ActiveItem
        }
    }
}
