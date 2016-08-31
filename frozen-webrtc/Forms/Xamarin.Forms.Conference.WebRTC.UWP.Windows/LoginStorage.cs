using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows8.Conference.WebRTC;
using Xamarin.PCL;

[assembly: Xamarin.Forms.Dependency(typeof(LoginStorage))]
namespace Windows8.Conference.WebRTC
{
    public class LoginStorage : ILoginStorage
    {
        public void Clear()
        {
            Windows.Storage.ApplicationDataContainer localSettings =
                Windows.Storage.ApplicationData.Current.LocalSettings;

            localSettings.Values["Login"] = null;
            localSettings.Values["Password"] = null;
        }

        public void Init(object context)
        {
        }

        public KeyValuePair<string, string>? Load()
        {
            Windows.Storage.ApplicationDataContainer localSettings =
                Windows.Storage.ApplicationData.Current.LocalSettings;

            var login = localSettings.Values["Login"];
            var password = localSettings.Values["Password"];

            if (login != null && password != null)
            {
                return new KeyValuePair<string, string>(login.ToString(), password.ToString());
            }

            return null;
        }

        public void Save(string login, string password)
        {
            Windows.Storage.ApplicationDataContainer localSettings =
                Windows.Storage.ApplicationData.Current.LocalSettings;

            localSettings.Values["Login"] = login;
            localSettings.Values["Password"] = password;
        }
    }
}
