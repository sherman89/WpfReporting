using System.Printing;

namespace PrintPreviewGui.ViewModels
{
    public class PageSizeViewModel
    {
        public PageSizeViewModel(PageMediaSize pageMediaSizeName)
        {
            PageMediaSize = pageMediaSizeName;
        }

        public PageMediaSize PageMediaSize { get; }

        public string PageSizeName => PageMediaSize.PageMediaSizeName.ToString();
    }
}