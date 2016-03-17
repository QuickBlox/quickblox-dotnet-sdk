using System;
using System.ComponentModel;
using Quickblox.Sdk.Modules.UsersModule.Models;

namespace XamarinForms.QbChat
{
	public class SelectedUser : INotifyPropertyChanged
	{
		bool isSelected;

		#region INotifyPropertyChanged implementation

		public event PropertyChangedEventHandler PropertyChanged;

		public void Raise(string propName){
			var handler = PropertyChanged;
			if (handler != null) {
				handler.Invoke(this, new PropertyChangedEventArgs(propName));
			}
		}

		#endregion

		public bool IsSelected {
			get{
				return isSelected;
			}
			set{
				isSelected = value;
				Raise ("IsSelected");
			}
		}

		public User User { get; set; }
	}
}

