using System;
using MugenMvvmToolkit.ViewModels;
using MugenMvvmToolkit.Interfaces.Models;
using MugenMvvmToolkit.Models;
using MugenMvvmToolkit.Collections;
using MugenMvvmToolkit.Interfaces.Collections;
using System.Threading.Tasks;
using MugenMvvmToolkit.Infrastructure;
using System.Windows.Input;
using MugenMvvmToolkit;
using System.Collections.Generic;

namespace Qmunicate.Xamarin
{
	public class ChatsViewModel : WorkspaceViewModel
	{
		#region Fields

		private static readonly DataConstant<TrackingCollection> StateConstant;
	
		private string _filterText;
		private Task _initializedTask;
		private GridViewModel<DialogTable> _gridViewModel;
		private readonly ITrackingCollection _trackingCollection;
		#endregion

		#region Constructors

		static ChatsViewModel()
		{
			StateConstant = DataConstant.Create(() => StateConstant, true);
		}

		public ChatsViewModel ()
		{
			OpenDialogCommand = RelayCommandBase.FromAsyncHandler<DialogTable>(OpenDialogExecute, CanOpenDialog, this);
			RemoveDialogCommand = RelayCommandBase.FromAsyncHandler<DialogTable>(RemoveDialogTable, CanRemoveDialogTable, this);

			_trackingCollection = new TrackingCollection(new CompositeEqualityComparer().AddComparer(DialogTable.KeyComparer));
			// TODO: add culture localization
			//DisplayName = "Chats";
			FilterText = "H";
		}

		#endregion

		#region Commands


		public ICommand OpenDialogCommand { get; private set; }

		public ICommand RemoveDialogCommand { get; private set; }

		#endregion

		#region Properties

		public string FilterText
		{
			get { return _filterText; }
			set
			{
				if (value == _filterText) return;
				_filterText = value;
				if (GridViewModel != null)
					GridViewModel.UpdateFilter();
				OnPropertyChanged();
			}
		}
		public GridViewModel<DialogTable> GridViewModel
		{
			get { return _gridViewModel; }
			private set
			{
				if (_gridViewModel == value)
					return;
				_gridViewModel = value;
				OnPropertyChanged();
			}
		}

		public bool HasChanges
		{
			get { return _trackingCollection.HasChanges; }
		}

		#endregion

		#region Command's methods

		private Task OpenDialogExecute(DialogTable dialogTable)
		{
			return Task.Delay (1000);
//			using (var editorVm = GetViewModel<OrderEditorViewModel>())
//			using (var editorDialogVm = editorVm.Wrap<IEditorWrapperViewModel>())
//			{
//				OrderModel orderModel = dialogTable ?? GridViewModel.SelectedItem;
//				IList<OrderProductModel> links = await GetOrLoadProductLinks(orderModel);
//
//				editorVm.InitializeEntity(orderModel, links);
//				if (!await editorDialogVm.ShowAsync())
//					return;
//
//				//NOTE: wait for load data, in case the view model has been restored.
//				await _initializedTask;
//				_trackingCollection.UpdateStates(editorVm.ApplyChanges());
//				orderModel = editorVm.Entity;
//
//				//NOTE: update item, in case the view model has been restored.
//				if (!GridViewModel.OriginalItemsSource.Contains(orderModel))
//				{
//					int index = 0;
//					OrderModel currentItem = GridViewModel
//						.OriginalItemsSource
//						.FirstOrDefault(model => model.Id == orderModel.Id);
//					if (currentItem != null)
//					{
//						index = GridViewModel.OriginalItemsSource.IndexOf(currentItem);
//						GridViewModel.OriginalItemsSource.RemoveAt(index);
//					}
//					GridViewModel.OriginalItemsSource.Insert(index, orderModel);
//				}
//
//				GridViewModel.SelectedItem = orderModel;
//				this.OnPropertyChanged(() => v => v.HasChanges);
//			}
		}

		private bool CanOpenDialog(DialogTable obj)
		{
			return obj != null || (GridViewModel != null && GridViewModel.SelectedItem != null);
		}

		private async Task RemoveDialogTable(DialogTable obj)
		{
			DialogTable item = obj ?? GridViewModel.SelectedItem;
//			string message = string.Format(UiResources.DeleteOrderQuestionFormat, item.Name);
//			if (await _messagePresenter.ShowAsync(message, DisplayName, MessageButton.YesNo, MessageImage.Question) !=
//				MessageResult.Yes)
//				return;

			GridViewModel.OriginalItemsSource.Remove(item);
			this.OnPropertyChanged("GridViewModel");
		}

		private bool CanRemoveDialogTable(DialogTable obj)
		{
			return obj != null || (GridViewModel != null && GridViewModel.SelectedItem != null);
		}

		#endregion

		#region Methods

		private bool Filter(DialogTable item)
		{
			System.Diagnostics.Debug.WriteLine ("Filtered item: " + item.Name);
			if (item == null)
				return false;
			if (string.IsNullOrWhiteSpace(FilterText))
				return true;
			return item.Name.SafeContains (FilterText);
		}

		#endregion

		#region Overrides of WorkspaceViewModel

		protected override async void OnInitialized()
		{
			GridViewModel = GetViewModel<GridViewModel<DialogTable>>();
			var login = "marina@dmail.com";
			var password = "marina@dmail.com";
			var userId = await App.QbProvider.LoginWithEmailAsync (login, password);

			var dialogs = await App.QbProvider.GetDialogs();
//			var dialogs = new List<DialogTable>();
//			for (int i = 0; i < 10; i++) {
//				dialogs.Add (new DialogTable () { Name = "Name: " + i, LastMessage = "Hello " + i }); 
//			}
			GridViewModel.UpdateItemsSource(dialogs);

			GridViewModel.Filter = Filter;

		}

		#endregion
	}
}

