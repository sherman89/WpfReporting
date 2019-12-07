using System.Threading.Tasks;

namespace Sherman.WpfReporting.Gui
{
    public class Dispatcher : IDispatcher
    {
        public async Task Yield()
        {
            await System.Windows.Threading.Dispatcher.Yield();
        }
    }

    public interface IDispatcher
    {
        Task Yield();
    }
}
