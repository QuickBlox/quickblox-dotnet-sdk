using QbChat.Pcl.Repository;
using System;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace QbChat.UWP.Converters
{
    public class DialogToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var dialog = value as DialogTable;
            ImageSource source = null;

            if (dialog != null)
            {
                if (dialog.DialogType == Quickblox.Sdk.Modules.ChatModule.Models.DialogType.Private)
                {
                    source = new BitmapImage(new Uri("ms-appx:///Assets/privateholder.png"));
                }
                else
                {
                    source = new BitmapImage(new Uri("ms-appx:///Assets/groupholder.png"));
                }
            }

            return source;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
