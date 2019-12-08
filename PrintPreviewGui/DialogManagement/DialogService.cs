using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;

namespace Sherman.WpfReporting.Gui.DialogManagement
{
    public class DialogService : Conductor<IDialog>.Collection.AllActive, IDialogService
    {
        //TODO: Make this readonly
        public IObservableCollection<IDialog> OpenDialogs => Items;
        public bool AnyOpenDialogs => OpenDialogs.Any();

        public event EventHandler<IDialog> DialogOpened;
        public event EventHandler<IDialog> DialogClosed;

        private readonly Dictionary<IDialog, int> instanceCounter = new Dictionary<IDialog, int>();

        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public async Task OpenAsync(IDialog dialog, CancellationToken cancellationToken)
        {
            await AddDialogAsync(dialog, cancellationToken);
            NotifyOfPropertyChange(() => AnyOpenDialogs);
        }

        public async Task CloseAsync(IDialog dialog, CancellationToken cancellationToken)
        {
            await RemoveDialogAsync(dialog, cancellationToken);
            NotifyOfPropertyChange(() => AnyOpenDialogs);
        }

        public async Task<T> OpenModalAsync<T>(IModalDialog<T> dialog, CancellationToken cancellationToken)
        {
            var task = dialog.ConfirmAsync(cancellationToken);

            await AddDialogAsync(dialog, cancellationToken);

            NotifyOfPropertyChange(() => AnyOpenDialogs);

            try
            {
                await task;
            }
            finally
            {
                await RemoveDialogAsync(dialog, cancellationToken);
                NotifyOfPropertyChange(() => AnyOpenDialogs);
            }

            return await task;
        }

        private async Task AddDialogAsync(IDialog dialog, CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync();

            try
            {
                if (Items.Contains(dialog))
                {
                    if (!instanceCounter.ContainsKey(dialog))
                    {
                        instanceCounter.Add(dialog, 0);
                    }

                    instanceCounter[dialog]++;
                    return;
                }

                var topMostDialog = OpenDialogs.LastOrDefault();
                if (topMostDialog != null)
                {
                    topMostDialog.IsDialogEnabled = false;
                }

                dialog.IsDialogEnabled = true;
                Items.Add(dialog);
                await ScreenExtensions.TryActivateAsync(dialog, cancellationToken);

                DialogOpened?.Invoke(this, dialog);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task RemoveDialogAsync(IDialog dialog, CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync();

            try
            {
                if (instanceCounter.ContainsKey(dialog))
                {
                    var remaining = instanceCounter[dialog]--;
                    if (remaining > 0)
                    {
                        return;
                    }

                    instanceCounter.Remove(dialog);
                }

                Items.Remove(dialog);
                await ScreenExtensions.TryDeactivateAsync(dialog, true, cancellationToken);

                var topMostDialog = OpenDialogs.LastOrDefault();
                if (topMostDialog != null)
                {
                    topMostDialog.IsDialogEnabled = true;
                }

                DialogClosed?.Invoke(this, dialog);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
