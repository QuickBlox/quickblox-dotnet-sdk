using MugenMvvmToolkit.Modules;
using MugenMvvmToolkit.Models;
using MugenMvvmToolkit;
using MugenMvvmToolkit.Binding.Interfaces.Models;
using UIKit;
using MugenMvvmToolkit.Binding.Models;
using MugenMvvmToolkit.Binding.Interfaces;
using MugenMvvmToolkit.Binding;
using MugenMvvmToolkit.Binding.Models.EventArg;
using CoreGraphics;

namespace Qmunicate.Xamarin.iOS
{
	public class TouchModule : ModuleBase
	{
		/// <summary>
		///     Defines the attached property for busy indicator.
		/// </summary>
		private static readonly IAttachedBindingMemberInfo<UIView, LoadingOverlay> BusyViewMember =
			AttachedBindingMember.CreateAutoProperty<UIView, LoadingOverlay>("#busyView",
				getDefaultValue: CreateLoadingOverlay);
		
		public TouchModule () : base(false, LoadMode.Runtime)
		{
		}

		protected override bool LoadInternal ()
		{
			IBindingMemberProvider memberProvider = BindingServiceProvider.MemberProvider;
			memberProvider.Register(AttachedBindingMember.CreateAutoProperty(AttachedMembersEx.UIView.IsBusy, IsBusyChanged));
			memberProvider.Register(AttachedBindingMember.CreateAutoProperty(AttachedMembersEx.UIView.BusyMessage, BusyMessageChanged));

			IocContainer.BindToConstant<IDeviceUid> (new IOSDeviceUid ());
			return true;
		}

		protected override void UnloadInternal ()
		{
			
		}

		private static LoadingOverlay CreateLoadingOverlay(UIView uiView, IBindingMemberInfo bindingMemberInfo)
		{
			// Determine the correct size to start the overlay (depending on device orientation)
			var bounds = UIScreen.MainScreen.Bounds; // portrait bounds
			if (UIApplication.SharedApplication.StatusBarOrientation == UIInterfaceOrientation.LandscapeLeft ||
				UIApplication.SharedApplication.StatusBarOrientation == UIInterfaceOrientation.LandscapeRight)
			{
				bounds.Size = new CGSize(bounds.Size.Height, bounds.Size.Width);
			}
			return new LoadingOverlay(bounds);
		}

		private static void IsBusyChanged(UIView uiView, AttachedMemberChangedEventArgs<bool> args)
		{
			//Ignoring view and set overlay over main window
			uiView = UIApplication.SharedApplication.Windows[0];
			LoadingOverlay busyIndicator = BusyViewMember.GetValue(uiView, null);
			if (args.NewValue)
				busyIndicator.Show(uiView);
			else
				busyIndicator.Hide();
		}

		private static void BusyMessageChanged(UIView uiView, AttachedMemberChangedEventArgs<object> args)
		{
			//Ignoring view and set overlay over main window
			uiView = UIApplication.SharedApplication.Windows[0];
			LoadingOverlay busyIndicator = BusyViewMember.GetValue(uiView, null);
			busyIndicator.BusyMessage = args.NewValue == null ? null : args.NewValue.ToString();
		}
	}
}

