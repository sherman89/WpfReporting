using System.Threading;
using System.Threading.Tasks;

namespace Sherman.WpfReporting.Gui.DialogManagement
{
    public interface IDialog
    {
        bool IsDialogEnabled { get; set; }
    }

    public interface IModalDialog<T> : IDialog
    {
        Task<T> ConfirmAsync(CancellationToken cancellationToken);
    }
}