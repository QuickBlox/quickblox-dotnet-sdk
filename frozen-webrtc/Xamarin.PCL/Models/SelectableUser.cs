using System;
using Quickblox.Sdk.Modules.UsersModule.Models;

namespace Xamarin.PCL
{
	public class SelectableUser : Observable
	{
		User user;
		private bool isSelected;

		public SelectableUser()
		{
		}

		public bool IsSelected
		{
			get
			{
				return isSelected;
			}

			set
			{
				isSelected = value;
				RaisePropertyChanged();
			}
		}

		public User User
		{
			get
			{
				return user;}
			set
			{
				user = value;
				RaisePropertyChanged(); 
			}
		}
	}
}

