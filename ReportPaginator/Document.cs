using System.Windows;
using System.Windows.Controls;

namespace Sherman.WpfReporting.Lib
{
    public static class Document
    {
        /// <summary>
        /// If true, <see cref="Paginator"/> shows this element only on the first page.
        /// </summary>
        public static readonly DependencyProperty VisibleOnFirstPageOnlyProperty =
            DependencyProperty.RegisterAttached("VisibleOnFirstPageOnly", typeof(bool?), typeof(Document),
                new PropertyMetadata(default(bool?)));

        public static bool? GetVisibleOnFirstPageOnly(DependencyObject element)
        {
            return (bool?)element.GetValue(VisibleOnFirstPageOnlyProperty);
        }

        public static void SetVisibleOnFirstPageOnly(DependencyObject element, bool? value)
        {
            element.SetValue(VisibleOnFirstPageOnlyProperty, value);
        }

        /// <summary>
        /// If true, <see cref="Paginator"/> knows to set the <see cref="CurrentPageNumberProperty"/> on this element.
        /// </summary>
        public static readonly DependencyProperty SetCurrentPageNumberAttachedPropertyProperty =
            DependencyProperty.RegisterAttached("SetCurrentPageNumberAttachedProperty",
                typeof(bool), typeof(Document), new PropertyMetadata(false));

        public static bool GetSetCurrentPageNumberAttachedProperty(DependencyObject element)
        {
            return (bool)element.GetValue(SetCurrentPageNumberAttachedPropertyProperty);
        }

        public static void SetSetCurrentPageNumberAttachedProperty(DependencyObject element, bool value)
        {
            element.SetValue(SetCurrentPageNumberAttachedPropertyProperty, value);
        }

        /// <summary>
        /// Holds the current page number processed by <see cref="Paginator"/>.
        /// </summary>
        public static readonly DependencyProperty CurrentPageNumberProperty = DependencyProperty.RegisterAttached("CurrentPageNumber",
            typeof(int), typeof(Document), new PropertyMetadata(default(int)));

        public static int GetCurrentPageNumber(DependencyObject element)
        {
            return (int)element.GetValue(CurrentPageNumberProperty);
        }

        public static void SetCurrentPageNumber(DependencyObject element, int value)
        {
            element.SetValue(CurrentPageNumberProperty, value);
        }

        /// <summary>
        /// If set to true, <see cref="Paginator"/> knows to set the <see cref="LastPageNumberProperty"/> on this element.
        /// </summary>
        public static readonly DependencyProperty SetLastPageNumberAttachedPropertyProperty =
            DependencyProperty.RegisterAttached("SetLastPageNumberAttachedProperty",
                typeof(bool), typeof(Document), new PropertyMetadata(false));

        public static bool GetSetLastPageNumberAttachedProperty(DependencyObject element)
        {
            return (bool)element.GetValue(SetLastPageNumberAttachedPropertyProperty);
        }

        public static void SetSetLastPageNumberAttachedProperty(DependencyObject element, bool value)
        {
            element.SetValue(SetLastPageNumberAttachedPropertyProperty, value);
        }

        /// <summary>
        /// Holds the last page number processed by <see cref="Paginator"/>.
        /// </summary>
        public static readonly DependencyProperty LastPageNumberProperty = DependencyProperty.RegisterAttached("LastPageNumber",
            typeof(int), typeof(Document), new PropertyMetadata(default(int)));

        public static int GetLastPageNumber(DependencyObject element)
        {
            return (int)element.GetValue(LastPageNumberProperty);
        }

        public static void SetLastPageNumber(DependencyObject element, int value)
        {
            element.SetValue(LastPageNumberProperty, value);
        }

        /// <summary>
        /// If true, enables <see cref="Paginator"/> to paginate any control that is or derives from <see cref="ItemsControl"/>.
        /// </summary>
        public static readonly DependencyProperty PaginateProperty = DependencyProperty.RegisterAttached("Paginate",
            typeof(bool), typeof(Document), new PropertyMetadata(default(bool)));

        public static bool GetPaginate(ItemsControl element)
        {
            return (bool)element.GetValue(PaginateProperty);
        }

        public static void SetPaginate(ItemsControl element, bool value)
        {
            element.SetValue(PaginateProperty, value);
        }
    }
}
