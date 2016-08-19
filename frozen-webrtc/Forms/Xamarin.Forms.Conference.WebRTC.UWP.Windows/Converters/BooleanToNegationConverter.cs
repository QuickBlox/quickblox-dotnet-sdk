using System;
using System.Globalization;
using Windows.UI.Xaml.Data;

namespace Xamarin.Forms.Conference.WebRTC
{
	public class BooleanToNegationConverter : IValueConverter
	{
#if __ANDROID__ || __IOS__
      
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool)
            {
                return !(bool)value;
            }

            return value;
        }
#elif WINDOWS_APP
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                return !(bool)value;
            }

            return value;
        }
#endif
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
	}
}

