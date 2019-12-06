using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

namespace Sherman.WpfReporting.Lib
{
    public class Paginator : IPaginator
    {
        /// <summary>
        /// Take a factory that produces an instance of a UIElement derived class (i.e. <see cref="UserControl"/> with a header and list) which is used to create pages until all items in the list fit into the document.
        /// Returns a list of paginated elements that are wrapped in a <see cref="ContentControl"/>. In order for this method to work, the given type in the factory must contain elements with
        /// attached properties from <see cref="Document"/> set, for instance the <see cref="Document.PaginateProperty"/> must be set for lists to be paginated.
        /// </summary>
        /// <param name="pageFactory">The factory that will be used to create pages.</param>
        /// <param name="pageSize">Desired page size in WPF device independent pixel units (1/96 of an inch).</param>
        /// <param name="pageMargins">Desired page margins. This will be subtracted from page size.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of type <see cref="ContentControl"/> that contains the pages resulting from pagination.</returns>
        public async Task<List<UIElement>> PaginateAsync(Func<UIElement> pageFactory, Size pageSize, Thickness pageMargins, CancellationToken cancellationToken)
        {
            Dictionary<string, ItemsControlData> paginationTracker = null;
            var processedPages = new List<UIElement>();
            var processing = true;
            var pageNumber = 0;

            while (processing)
            {
                var newPage = pageFactory();
                var pageLogicalChildren = FindLogicalChildren(newPage).ToList();

                pageNumber++;

                if (pageNumber == 1)
                {
                    // We only need to do this once to fetch the data and initialize the pagination tracker
                    Initialize(pageLogicalChildren, out paginationTracker);
                }

                var currentPageContainer = new ContentControl
                {
                    Width = pageSize.Width - (pageMargins.Left + pageMargins.Right),
                    Height = pageSize.Height - (pageMargins.Top + pageMargins.Bottom),
                    Margin = pageMargins
                };

                // Do a layout pass to initialize the actual height and width of the control.
                // Set the content only afterwards to avoid significant performance hits.
                currentPageContainer.Measure(pageSize);
                currentPageContainer.Arrange(new Rect(new Point(), pageSize));
                currentPageContainer.UpdateLayout();

                // Add the actual content of the page
                currentPageContainer.Content = newPage;

                // First stage of processing where we set things like current page number, hiding elements, etc...
                PreProcessing(pageLogicalChildren, pageNumber);

                // Second stage is pagination where we go through the items controls until no more items are left in paginationTracker
                var createNextPage = Paginate(pageLogicalChildren, paginationTracker);
                processedPages.Add(currentPageContainer);

                if (!createNextPage)
                {
                    // Third stage after all pages have been created, here we do things like adding the total pages which can only be known after pagination
                    PostProcessing(processedPages);
                    processing = false;
                }

                // Yield control back to the current dispatcher to keep UI responsive
                await Dispatcher.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }

            return processedPages;
        }

        /// <summary>
        /// Take a list of <see cref="UIElement"/> and return a <see cref="FixedDocument"/> where each page contains the given <see cref="UIElement"/>.
        /// </summary>
        /// <param name="uiElements"><see cref="UIElement"/> derived classes to place each in a <see cref="FixedPage"/>.</param>
        /// <param name="pageSize">Desired page size of each <see cref="FixedPage"/>. If <see cref="Paginator"/> was used to paginate, this should be the same value given to the Paginate method.</param>
        /// <returns></returns>
        public FixedDocument GetFixedDocumentFromPages(List<UIElement> uiElements, Size pageSize)
        {
            var document = new FixedDocument();
            document.DocumentPaginator.PageSize = pageSize;

            foreach (var page in uiElements)
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
                ((IAddChild) pageContent).AddChild(fixedPage);

                fixedPage.Measure(pageSize);
                fixedPage.Arrange(new Rect(new Point(), pageSize));
                fixedPage.UpdateLayout();

                document.Pages.Add(pageContent);
            }

            return document;
        }

        /// <summary>
        /// Called once to initialize the pagination tracker.
        /// </summary>
        /// <param name="pageLogicalChildren">Logical children of the first page of the element to be paginated. Used to find any ItemsControls that have the <see cref="Document.PaginateProperty"/> set to true.</param>
        /// <param name="paginationTracker">The pagination tracker that is used throughout the pagination process.</param>
        private static void Initialize(IReadOnlyCollection<DependencyObject> pageLogicalChildren, out Dictionary<string, ItemsControlData> paginationTracker)
        {
            paginationTracker = new Dictionary<string, ItemsControlData>();

            var itemsControls = pageLogicalChildren.OfType<ItemsControl>()
                .Where(i => i.GetValue(Document.PaginateProperty) is bool paginate && paginate).ToList();

            foreach (var itemsControl in itemsControls)
            {
                itemsControl.UpdateLayout();

                if (string.IsNullOrWhiteSpace(itemsControl.Name))
                {
                    throw new InvalidOperationException("ItemsControl that will be paginated must have a unique name. Set the x:Name property.");
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
                if (dp.GetValue(Document.VisibleOnFirstPageOnlyProperty) is bool visibleOnFirstPage)
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

                if (dp.GetValue(Document.SetCurrentPageNumberAttachedPropertyProperty) is bool setPageNumber && setPageNumber)
                {
                    dp.SetValue(Document.CurrentPageNumberProperty, pageNumber);
                }
            }
        }

        private static bool Paginate(IReadOnlyCollection<DependencyObject> pageLogicalChildren, IDictionary<string, ItemsControlData> paginationTracker)
        {
            var itemsControlsToPaginate = pageLogicalChildren.OfType<ItemsControl>()
                .Where(i => i.GetValue(Document.PaginateProperty) is bool paginate && paginate).ToList();

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

                var itemContainer = (FrameworkElement)itemsControl.ItemContainerGenerator.ContainerFromItem(item);
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

        private static void PostProcessing(IReadOnlyCollection<UIElement> processedPages)
        {
            var lastPageNumber = processedPages.Count;
            foreach (var page in processedPages)
            {
                var logicalChildren = FindLogicalChildren(page).ToList();

                foreach (var dp in logicalChildren)
                {
                    if (dp.GetValue(Document.SetLastPageNumberAttachedPropertyProperty) is bool setLastPageNumber && setLastPageNumber)
                    {
                        dp.SetValue(Document.LastPageNumberProperty, lastPageNumber);
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
