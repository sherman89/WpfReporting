using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;

namespace Sherman.WpfReporting.Gui.DialogManagement
{
    public class DialogService : Conductor<IDialog>.Collection.AllActive, IDialogService
    {
        public IObservableCollection<IDialog> OpenDialogs => Items;
        public bool AnyOpenDialogs => OpenDialogs.Any();

        public event EventHandler<IDialog> DialogOpened;
        public event EventHandler<IDialog> DialogClosed;

        private readonly Dictionary<IDialog, int> instanceCounter = new Dictionary<IDialog, int>();
        private readonly object locker = new object();

        public void Open(IDialog dialog)
        {
            AddDialog(dialog);
            NotifyOfPropertyChange(() => AnyOpenDialogs);
        }

        public void Close(IDialog dialog)
        {
            RemoveDialog(dialog);
            NotifyOfPropertyChange(() => AnyOpenDialogs);
        }

        public async Task<T> OpenModalAsync<T>(IModalDialog<T> dialog)
        {
            var task = dialog.ConfirmAsync();

            AddDialog(dialog);

            NotifyOfPropertyChange(() => AnyOpenDialogs);

            try
            {
                await task;
            }
            finally
            {
                RemoveDialog(dialog);
                NotifyOfPropertyChange(() => AnyOpenDialogs);
            }

            return await task;
        }

        private void AddDialog(IDialog dialog)
        {
            lock (locker)
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

                DialogOpened?.Invoke(this, dialog);
            }
        }

        private void RemoveDialog(IDialog dialog)
        {
            lock (locker)
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

                var topMostDialog = OpenDialogs.LastOrDefault();
                if (topMostDialog != null)
                {
                    topMostDialog.IsDialogEnabled = true;
                }

                DialogClosed?.Invoke(this, dialog);
            }
        }
    }
}
