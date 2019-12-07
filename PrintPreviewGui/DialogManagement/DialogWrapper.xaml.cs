using System.Windows;

namespace Sherman.WpfReporting.Gui.DialogManagement
{
    public partial class DialogWrapper
    {
        public static readonly DependencyProperty DialogParentProperty = DependencyProperty.Register("DialogParent", typeof(UIElement), typeof(DialogWrapper), new PropertyMetadata(default(UIElement), OnParentChanged));
        public static readonly DependencyProperty IsShowingProperty = DependencyProperty.Register("IsShowing", typeof(bool), typeof(DialogWrapper), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsShowingChanged));

        //Store old value of IsEnabled of parent
        private static bool parentWasEnabled;

        private static void OnParentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DialogWrapper dialog)
            {
                if (e.NewValue is UIElement parent)
                {
                    parentWasEnabled = parent.IsEnabled;
                    dialog.DialogParent.IsEnabled = !dialog.IsShowing && parentWasEnabled;
                }
            }
        }

        private static void OnIsShowingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DialogWrapper dialog)
            {
                if (e.NewValue is bool isShowing)
                {
                    dialog.Visibility = isShowing ? Visibility.Visible : Visibility.Hidden;
                    if (dialog.DialogParent != null)
                    {
                        dialog.DialogParent.IsEnabled = !isShowing && parentWasEnabled;
                    }
                }
            }
        }

        public DialogWrapper()
        {
            InitializeComponent();
            Visibility = Visibility.Hidden;
        }

        public UIElement DialogParent
        {
            get => (UIElement) GetValue(DialogParentProperty);
            set => SetValue(DialogParentProperty, value);
        }

        public bool IsShowing
        {
            get => (bool) GetValue(IsShowingProperty);
            set => SetValue(IsShowingProperty, value);
        }
    }
}
