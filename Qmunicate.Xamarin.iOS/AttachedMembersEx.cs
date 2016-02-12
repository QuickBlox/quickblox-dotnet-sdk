using System;
using MugenMvvmToolkit.iOS.Binding;
using MugenMvvmToolkit.Binding.Models;

namespace Qmunicate.Xamarin.iOS
{
	public static class AttachedMembersEx
	{
		public class UIView : AttachedMembers.UIView
		{
			#region Fields

			public static readonly BindingMemberDescriptor<UIKit.UIView, bool> IsBusy;
			public static readonly BindingMemberDescriptor<UIKit.UIView, object> BusyMessage;

			#endregion

			#region Constructors

			static UIView()
			{
				IsBusy = new BindingMemberDescriptor<UIKit.UIView, bool>("IsBusy");
				BusyMessage = new BindingMemberDescriptor<UIKit.UIView, object>("BusyMessage");
			}

			#endregion
		}
	}
}

