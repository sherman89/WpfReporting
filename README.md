### **WARNING: THIS IS NOT PRODUCTION READY CODE, AND I'M NOT A WPF EXPERT. For best results with the paginator, do _NOT_ use StackPanel. Grid is your friend. Paginator does not accept any wacky XAML, it's meant to work with simple reports and that's all, so just use a simple Grid, preferably with star sizing.**

&nbsp;<br>
# WPF pagination, printing & other stuff

The main purpose of this repository is to demonstrate how to create paginated printable documents from plain old XAML controls
with the help of attached properties and a paginator class.

I made this repository for myself so that I won't forget how I did it, which is why I also added other features that I think 
will be useful for my future WPF development work.

Features include:
* Paginating XAML documents if they contain `ItemsControl` derived controls.
* Setting page numbers and other properties (e.g. header on first page only) on paginated documents.
* Getting correct page margins based on printer capabilities and applying them on the generated pages.
* How printing in general works in WPF (Fetching USB printers, etc...)
* Creating an MVVM dialog service that can be used in ViewModels to show modal / regular dialogs.
* Creating an animated progress indicator.
* Using Caliburn.Micro framework (Alpha version with async support)
* Simple styling, dependecy injection, and other simple stuff

**This code is meant as a reference for myself, and some parts are ugly or done on purpose to workaround certain issues, so
don't just copy paste but try to understand what is happening and read the comments.**

## How the pagination works
It's very simple: the reports (or documents, or whatever you want to call them) are simple XAML user controls with headers and
lists and all the regular WPF stuff. The only exception is the addition of **attached properties** (found in *Document.cs* file)
that are read by / written to by the **Paginator** class. These attached properties help the paginator know when to hide certain
elements, which lists to paginate, and where to set page numbers. The paginator itself is where the magic happens.

The paginator looks for any controls that derive from `ItemsControl` and checks to see if it has the **Paginate** attached 
property set to true, and if it is, the paginator will save the contents in memory and clears the list, then starts adding the 
items again one by one. After each addition, the paginator checks if the last added item is fully visible, and if not, will 
create a new page and continues from there until all the items have been added.

Example reports can be loaded by clicking the buttons on the left. They exist in the **Reports** folder.

## Other notes
Don't pay too much attention to these MVVM practices, some of them I don't like anymore but I don't have time to refactor them.
