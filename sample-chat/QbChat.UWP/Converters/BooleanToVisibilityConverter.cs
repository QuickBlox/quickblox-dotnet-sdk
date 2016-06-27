using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace QbChat.UWP.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var isVisible = (bool)value;
            Visibility state = Visibility.Visible;
            if (parameter != null && parameter.Equals("invert"))
            {
                state = isVisible ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                state = isVisible ? Visibility.Visible : Visibility.Collapsed;
            }

            return state;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
