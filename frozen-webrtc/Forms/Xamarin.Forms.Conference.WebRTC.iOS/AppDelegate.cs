using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using QbChat.Pcl;
using Security;
using UIKit;

namespace Xamarin.Forms.Conference.WebRTC.iOS
{
	[Register("AppDelegate")]
	public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
	{
		public override bool FinishedLaunching(UIApplication app, NSDictionary options)
		{
			global::Xamarin.Forms.Forms.Init();
			Quickblox.Sdk.Platform.QuickbloxPlatform.Init();


			//foreach (var recordKind in new[]{
			//	SecKind.GenericPassword})
			//{
			//	SecRecord query = new SecRecord(recordKind);
			//	query.Generic = "uidNumber";
			//	var err = SecKeyChain.Remove(query);

			//	SecRecord query2 = new SecRecord(recordKind);
			//query2.Account = "login";
			//query2.Label = "login";
			//query2.Service = "login";
			//	var err2 = SecKeyChain.Remove(query2);

			//	SecRecord query3 = new SecRecord(recordKind);
			//query3.Account = "login";
			//query3.Label = "login";
			//query3.Service = "login";
			//	var err3 = SecKeyChain.Remove(query2);
			//}

			LoadApplication(new App());

			return base.FinishedLaunching(app, options);
		}
	}
}

