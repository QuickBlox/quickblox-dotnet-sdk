using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace QbChat.UWP.Helpers
{
    public static class CommandBarExtensions
    {
        public static readonly DependencyProperty HideMoreButtonProperty =
            DependencyProperty.RegisterAttached("HideMoreButton", typeof(bool), typeof(CommandBarExtensions),
                new PropertyMetadata(false, OnHideMoreButtonChanged));

        public static bool GetHideMoreButton(UIElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return (bool)element.GetValue(HideMoreButtonProperty);
        }

        public static void SetHideMoreButton(UIElement element, bool value)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            element.SetValue(HideMoreButtonProperty, value);
        }

        private static void OnHideMoreButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var commandBar = d as CommandBar;
            if (e == null || commandBar == null || e.NewValue == null) return;
            var morebutton = commandBar.FindDescendantByName("MoreButton");
            if (morebutton != null)
            {
                var value = GetHideMoreButton(commandBar);
                morebutton.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                commandBar.Loaded += CommandBarLoaded;
            }
        }

        private static void CommandBarLoaded(object o, object args)
        {
            var commandBar = o as CommandBar;
            var morebutton = commandBar?.FindDescendantByName("MoreButton");
            if (morebutton == null) return;
            var value = GetHideMoreButton(commandBar);
            morebutton.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
            commandBar.Loaded -= CommandBarLoaded;
        }

        public static FrameworkElement FindDescendantByName(this FrameworkElement element, string name)
        {
            if (element == null || string.IsNullOrWhiteSpace(name)) { return null; }

            if (name.Equals(element.Name, StringComparison.OrdinalIgnoreCase))
            {
                return element;
            }
            var childCount = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < childCount; i++)
            {
                var result = (VisualTreeHelper.GetChild(element, i) as FrameworkElement).FindDescendantByName(name);
                if (result != null) { return result; }
            }
            return null;
        }
    }
}
