using System;
using Android.App;
using Android.Content;
using Xamarin.Forms;
using Android.Runtime;
using Android.Net;
using XamarinForms.QbChat.Providers;

namespace XamarinForms.QbChat.Android
{
	[Application(Theme="@android:style/Theme.Material.Light")]
	public class QmunicateApplication : global::Android.App.Application
	{ 
		public QmunicateApplication (IntPtr handle, JniHandleOwnership transfer)
			: base (handle, transfer)
		{
			App.Version = AppVersionNumber (this);

            // Create the broadcast receiver and bind the event handler
            // so that the app gets updates of the network connectivity status
            var _broadcastReceiver = new NetworkStatusBroadcastReceiver();
            _broadcastReceiver.ConnectionStatusChanged += OnNetworkStatusChanged;

            // Register the broadcast receiver
            global::Android.App.Application.Context.RegisterReceiver(_broadcastReceiver,
                new IntentFilter(ConnectivityManager.ConnectivityAction));
        }

        private void OnNetworkStatusChanged(object sender, EventArgs e)
        {
            // Retrieve the connectivity manager service
            var connectivityManager = (ConnectivityManager)
                global::Android.App.Application.Context.GetSystemService(
                    Context.ConnectivityService);

            // Check if the network is connected or connecting.
            // This means that it will be available, 
            // or become available in a few seconds.
            var activeNetworkInfo = connectivityManager.ActiveNetworkInfo;

            App.IsInternetAvaliable = activeNetworkInfo != null && activeNetworkInfo.Type == ConnectivityType.Wifi;
            //if (App.IsInternetAvaliable)
            //{
            //    if (App.UserId > 0)
            //        MessageProvider.Instance.ConnetToXmpp(App.UserId, App.UserPassword);
            //}
            //else
            //{
            //    MessageProvider.Instance.DisconnectToXmpp();
            //}
        }

        public string AppVersionNumber(Context context){
			var version = "";
			try {
				var pkgInfo = context.PackageManager.GetPackageInfo(context.PackageName, (global::Android.Content.PM.PackageInfoFlags)0);
				version = pkgInfo.VersionName;
			} catch (Exception ex) {
				
			}

			return version;
		}
	}

    [BroadcastReceiver()]
    public class NetworkStatusBroadcastReceiver : BroadcastReceiver
    {
        public event EventHandler ConnectionStatusChanged;

        public override void OnReceive(Context context, Intent intent)
        {
            if (ConnectionStatusChanged != null)
                ConnectionStatusChanged(this, EventArgs.Empty);
        }
    }
}

