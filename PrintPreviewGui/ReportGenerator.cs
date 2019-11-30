using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

namespace PrintPreviewGui
{
    public class ReportGenerator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="printerCapabilities"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="printerCapabilities" /> is <see langword="null" />.</exception>
        public Thickness GetMinimumPageMargins(PrintCapabilities printerCapabilities)
        {
            if (printerCapabilities is null)
            {
                throw new ArgumentNullException(nameof(PrintCapabilities), $"{nameof(PrintCapabilities)} cannot be null.");
            }

            if (!printerCapabilities.OrientedPageMediaWidth.HasValue)
            {
                throw new ArgumentNullException(nameof(printerCapabilities.OrientedPageMediaWidth), $"{nameof(printerCapabilities.OrientedPageMediaWidth)} cannot be null.");
            }

            if (!printerCapabilities.OrientedPageMediaHeight.HasValue)
            {
                throw new ArgumentNullException(nameof(printerCapabilities.OrientedPageMediaHeight), $"{nameof(printerCapabilities.OrientedPageMediaHeight)} cannot be null.");
            }

            if (printerCapabilities.PageImageableArea == null)
            {
                throw new ArgumentNullException(nameof(printerCapabilities.PageImageableArea), $"{nameof(printerCapabilities.PageImageableArea)} cannot be null.");
            }

            var minLeftMargin = printerCapabilities.PageImageableArea.OriginWidth;
            var minTopMargin = printerCapabilities.PageImageableArea.OriginHeight;
            var minRightMargin = printerCapabilities.OrientedPageMediaWidth.Value - printerCapabilities.PageImageableArea.ExtentWidth - minLeftMargin;
            var minBottomMargin = printerCapabilities.OrientedPageMediaHeight.Value - printerCapabilities.PageImageableArea.ExtentHeight - minTopMargin;

            return new Thickness(minLeftMargin, minTopMargin, minRightMargin, minBottomMargin);
        }

        public async Task<List<FrameworkElement>> GenerateReport(Func<FrameworkElement> reportFactory, Size pageSize, Thickness pageMargins)
        {
            Dictionary<string, ItemsControlData> paginationTracker = null;
            var processedPages = new List<FrameworkElement>();
            var processing = true;
            var pageNumber = 0;

            while (processing)
            {
                var newPage = reportFactory();
                var pageLogicalChildren = FindLogicalChildren(newPage).ToList();

                pageNumber++;

                if (pageNumber == 1)
                {
                    //We only need to do this once to fetch the data and initialize the pagination tracker
                    Initialize(pageLogicalChildren, out paginationTracker);
                }

                var currentPageContainer = new ContentControl
                {
                    Width = pageSize.Width - (pageMargins.Left + pageMargins.Right),
                    Height = pageSize.Height - (pageMargins.Top + pageMargins.Bottom),
                    Margin = pageMargins
                };

                // Do a layout pass to get the actual height and width of the control. Add the content afterwards to
                // avoid calculating the content layout which can cause significant performance hits.
                currentPageContainer.Measure(pageSize);
                currentPageContainer.Arrange(new Rect(new Point(), pageSize));
                currentPageContainer.UpdateLayout();

                // Add the actual content
                currentPageContainer.Content = newPage;

                // TODO: Document
                PreProcessing(pageLogicalChildren, pageNumber);

                // Begin processing page (pagination, visibility, etc...)
                var createNextPage = Paginate(pageLogicalChildren, paginationTracker, pageNumber);
                processedPages.Add(currentPageContainer);

                if (!createNextPage)
                {
                    PostProcessing(processedPages);
                    processing = false;
                }

                // Yield control back to the current dispatcher to keep UI responsive
                await Dispatcher.Yield();
            }

            return processedPages;
        }

        public FixedDocument GetFixedDocumentFromProcessedPages(List<FrameworkElement> processedPages, Size pageSize)
        {
            var document = new FixedDocument();
            document.DocumentPaginator.PageSize = pageSize;

            foreach (var page in processedPages)
            {
                var fixedPage = new FixedPage
                {
                    Width = document.DocumentPaginator.PageSize.Width,
                    Height = document.DocumentPaginator.PageSize.Height
                };

                FixedPage.SetLeft(page, 0);
                FixedPage.SetTop(page, 0);

                fixedPage.Children.Add(page);

                var pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(fixedPage);

                fixedPage.Measure(pageSize);
                fixedPage.Arrange(new Rect(new Point(), pageSize));
                fixedPage.UpdateLayout();

                document.Pages.Add(pageContent);
            }

            return document;
        }

        private static void Initialize(IReadOnlyCollection<DependencyObject> pageLogicalChildren, out Dictionary<string, ItemsControlData> paginationTracker)
        {
            paginationTracker = new Dictionary<string, ItemsControlData>();

            var itemsControls = pageLogicalChildren.OfType<ItemsControl>()
                .Where(i => i.GetValue(ReportHelper.PaginateProperty) is bool paginate && paginate).ToList();

            foreach (var itemsControl in itemsControls)
            {
                itemsControl.UpdateLayout();

                if (string.IsNullOrWhiteSpace(itemsControl.Name))
                {
                    throw new InvalidOperationException("ItemsControl must have a unique name. Set the x:Name property.");
                }

                if (itemsControl.Items.Count <= 0)
                {
                    throw new InvalidOperationException($"ItemsControl '{itemsControl.Name}' has no items.");
                }

                var items = Array.CreateInstance(itemsControl.Items[0].GetType(), itemsControl.Items.Count);
                itemsControl.Items.CopyTo(items, 0);

                if (!paginationTracker.ContainsKey(itemsControl.Name))
                {
                    paginationTracker.Add(itemsControl.Name, new ItemsControlData(items, 0));
                }
            }
        }

        private static void PreProcessing(IReadOnlyList<DependencyObject> pageLogicalChildren, int pageNumber)
        {
            foreach (var dp in pageLogicalChildren)
            {
                if (dp.GetValue(ReportHelper.VisibleOnFirstPageOnlyProperty) is bool visibleOnFirstPage)
                {
                    if (visibleOnFirstPage && pageNumber == 1)
                    {
                        dp.SetValue(UIElement.VisibilityProperty, Visibility.Visible);
                    }
                    else
                    {
                        dp.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
                    }
                }

                if (dp.GetValue(ReportHelper.SetCurrentPageNumberAttachedPropertyProperty) is bool setPageNumber && setPageNumber)
                {
                    dp.SetValue(ReportHelper.CurrentPageNumberProperty, pageNumber);
                }
            }
        }

        private static bool Paginate(IReadOnlyCollection<DependencyObject> pageLogicalChildren, IDictionary<string, ItemsControlData> paginationTracker, int pageNumber)
        {
            var itemsControlsToPaginate = pageLogicalChildren.OfType<ItemsControl>()
                .Where(i => i.GetValue(ReportHelper.PaginateProperty) is bool paginate && paginate).ToList();

            // Paginate lists
            foreach (var itemsControl in itemsControlsToPaginate)
            {
                itemsControl.ItemsSource = null;
                itemsControl.Items.Clear();

                if (paginationTracker.ContainsKey(itemsControl.Name))
                {
                    var items = paginationTracker[itemsControl.Name].Items;
                    var startIndex = paginationTracker[itemsControl.Name].CurrentIndex;

                    var needsPagination = PopulateItemsControl(items, itemsControl, startIndex, out var currentItemIndex);
                    if (needsPagination)
                    {
                        // Update CurrentIndex for this ItemsControl, so we continue where we left off when processing the next page.
                        paginationTracker[itemsControl.Name].CurrentIndex = currentItemIndex;
                    }
                    else
                    {
                        // Nothing else to add, remove ItemsControl from pagination tracker
                        paginationTracker.Remove(itemsControl.Name);
                    }
                }
            }

            // Check if all of the visible lists are empty, if so, pagination failed.
            if (itemsControlsToPaginate.Any() && itemsControlsToPaginate.All(ic => !ic.HasItems))
            {
                throw new InvalidOperationException("Tried to populate ItemsControls, but not a single item had enough space. " +
                                                    "This can be caused by bad XAML or a given page size that is too small to fit the content.");
            }

            // If paginationTracker is empty, this is the last page, so return false.
            // Otherwise return true which says that pagination should continue.
            return paginationTracker.Any();
        }

        private static bool PopulateItemsControl(Array items, ItemsControl itemsControl, int startIndex, out int currentItemIndex)
        {
            currentItemIndex = 0;

            if (items.Length <= 0)
            {
                return false;
            }

            for (var i = startIndex; i < items.Length; i++)
            {
                var item = items.GetValue(i);
                itemsControl.Items.Add(item);
                itemsControl.UpdateLayout();

                var itemContainer = (FrameworkElement) itemsControl.ItemContainerGenerator.ContainerFromItem(item);
                var itemsPresenter = FindVisualParent<ItemsPresenter>(itemContainer).Single();
                var isVisible = IsElementFullyVisibleInContainer(itemsPresenter, itemContainer);

                if (!isVisible)
                {
                    itemsControl.Items.Remove(item);
                    currentItemIndex = i;
                    return true;
                }
            }

            return false;
        }

        private static bool IsElementFullyVisibleInContainer(FrameworkElement container, UIElement element)
        {
            var panelRect = element.TransformToAncestor(container).TransformBounds(new Rect(0.0, 0.0, element.DesiredSize.Width, element.DesiredSize.Height));

            var roundedActualHeight = Math.Round(container.ActualHeight, 2);
            var roundedActualWidth = Math.Round(container.ActualWidth, 2);

            var containerRect = new Rect(0.0, 0.0, roundedActualWidth, roundedActualHeight);
            
            var topLeftPointRounded = new Point(Math.Round(panelRect.TopLeft.X, 2), Math.Round(panelRect.TopLeft.Y, 2));
            var topRightPointRounded = new Point(Math.Round(panelRect.TopRight.X, 2), Math.Round(panelRect.TopRight.Y, 2));
            var bottomLeftPointRounded = new Point(Math.Round(panelRect.BottomLeft.X, 2), Math.Round(panelRect.BottomLeft.Y, 2));
            var bottomRightPointRounded = new Point(Math.Round(panelRect.BottomRight.X, 2), Math.Round(panelRect.BottomRight.Y, 2));

            return containerRect.Contains(topLeftPointRounded) && containerRect.Contains(topRightPointRounded) &&
                   containerRect.Contains(bottomLeftPointRounded) && containerRect.Contains(bottomRightPointRounded);
        }

        private static void PostProcessing(IReadOnlyCollection<FrameworkElement> processedPages)
        {
            var lastPageNumber = processedPages.Count;
            foreach (var page in processedPages)
            {
                var logicalChildren = FindLogicalChildren(page).ToList();

                foreach (var dp in logicalChildren)
                {
                    if (dp.GetValue(ReportHelper.SetLastPageNumberAttachedPropertyProperty) is bool setLastPageNumber && setLastPageNumber)
                    {
                        dp.SetValue(ReportHelper.LastPageNumberProperty, lastPageNumber);
                    }
                }
            }
        }

        private static IEnumerable<T> FindVisualParent<T>(DependencyObject dp) where T : DependencyObject
        {
            if (dp != null)
            {
                var child = VisualTreeHelper.GetParent(dp);
                if (child is T t)
                {
                    yield return t;
                }

                foreach (var childOfChild in FindVisualParent<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }

        private static IEnumerable<DependencyObject> FindLogicalChildren(DependencyObject dp)
        {
            var children = LogicalTreeHelper.GetChildren(dp).OfType<DependencyObject>();

            foreach (var child in children)
            {
                yield return child;

                var subChildren = FindLogicalChildren(child);
                foreach (var subChild in subChildren)
                {
                    yield return subChild;
                }
            }
        }

        private class ItemsControlData
        {
            public ItemsControlData(Array items, int currentIndex)
            {
                Items = items;
                CurrentIndex = currentIndex;
            }

            public Array Items { get; }
            public int CurrentIndex { get; internal set; }

            public override string ToString()
            {
                return $"{nameof(Items)}: {Items.Length}, {nameof(CurrentIndex)}: {CurrentIndex}";
            }
        }
    }
}
