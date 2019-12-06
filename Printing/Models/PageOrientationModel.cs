using System.Printing;

namespace Sherman.WpfReporting.Lib.Models
{
    public class PageOrientationModel
    {
        public PageOrientationModel(PageOrientation pageOrientation)
        {
            PageOrientation = pageOrientation;
        }

        public PageOrientation PageOrientation { get; }

        public string PageOrientationName => PageOrientation.ToString();
    }
}