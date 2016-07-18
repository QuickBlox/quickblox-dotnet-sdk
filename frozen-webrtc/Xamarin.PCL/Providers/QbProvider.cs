using System;
using Quickblox.Sdk;
using System.Threading.Tasks;
using Quickblox.Sdk.GeneralDataModel.Response;
using System.Net;
using Quickblox.Sdk.GeneralDataModel.Models;
using System.Collections.Generic;
using Quickblox.Sdk.GeneralDataModel.Filter;
using Quickblox.Sdk.Modules.UsersModule.Requests;
using Quickblox.Sdk.GeneralDataModel.Filters;
using System.Linq;
using Quickblox.Sdk.Modules.UsersModule.Responses;
using Quickblox.Sdk.Modules.ChatModule.Requests;
using Quickblox.Sdk.Modules.UsersModule.Models;
using Quickblox.Sdk.Modules.ChatModule.Responses;
using Quickblox.Sdk.Modules.ChatModule.Models;
using Quickblox.Sdk.Modules.Models;
using Quickblox.Sdk.Modules.ChatXmppModule;

namespace Xamarin.PCL
{

    public class QbProvider
	{
        static QbProvider()
        {
        }

        private Action showInternetNotification;

        private readonly QuickbloxClient client = new QuickbloxClient(ApplicationKeys.ApplicationId,
			ApplicationKeys.AuthorizationKey,
			ApplicationKeys.AuthorizationSecret,
			ApplicationKeys.ApiBaseEndpoint,
			ApplicationKeys.ChatEndpoint,
			logger:new QbLogger());

        public int UserId { get; set; }

        public QbProvider(Action showInternetNotification)
        {
            this.showInternetNotification = showInternetNotification;
        }

        public ChatXmppClient GetXmppClient()
        {
            return client.ChatXmppClient;
        }

		public async Task<bool> GetBaseSession ()
		{
			var sessionResponse = await this.client.AuthenticationClient.CreateSessionBaseAsync (); 
			if (await HandleResponse(sessionResponse, HttpStatusCode.Created)){
				return true;
			}

			return false;
		}

		public async Task<User> SignUpUserWithLoginAsync(string login, string password, string userName, string roomName)
		{
			UserSignUpRequest request = new UserSignUpRequest();
			request.User = new UserRequest();
			request.User.Login = login;
			request.User.Password = password;
			request.User.FullName = userName;
			request.User.TagList = roomName;

			var signUpResponse = await this.client.UsersClient.SignUpUserAsync(request);
			if (await HandleResponse(signUpResponse, HttpStatusCode.Created))
			{
				return signUpResponse.Result.User;
			}

			return null;
		}

		public async Task<bool> DeleteUserById(int userId)
		{
			var response = await this.client.UsersClient.DeleteUserByIdAsync(userId);
			return await HandleResponse(response, HttpStatusCode.OK);
		}

		public async Task<int> LoginWithEmailAsync(string email, string password){
			var sessionResponse = await this.client.AuthenticationClient.CreateSessionWithEmailAsync (email, password); 
			if (await HandleResponse(sessionResponse, HttpStatusCode.Created)){
				UserId = sessionResponse.Result.Session.UserId;
				return sessionResponse.Result.Session.UserId;
			}
			else if ((int)sessionResponse.StatusCode == 422) {
				// Add logout
				return -1;
			}

			return 0;
		}

		public async Task<int> LoginWithLoginValueAsync(string login, string password, Platform platform, string uid)
        {
			var sessionResponse = await this.client.AuthenticationClient.CreateSessionWithLoginAsync (login, password); 
			if (await HandleResponse(sessionResponse, HttpStatusCode.Created)){
				UserId = sessionResponse.Result.Session.UserId;
				return sessionResponse.Result.Session.UserId;
			}
            else if (sessionResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return -1;
            }
            else if ((int)sessionResponse.StatusCode == 422) {
				// Add logout
				return -1;
			}

			return 0;
		}

		public async Task<int> LoginWithFbUserAsync(String accessToken, Platform platform, string uid)
		{
			var sessionResponse = await this.client.AuthenticationClient.CreateSessionWithSocialNetworkKey("facebook",
				"public_profile",
				accessToken,
				null,
				new DeviceRequest() 
				{ 
					//Platform = Device.OS == TargetPlatform.iOS ? Platform.ios : Platform.android,
                    //Udid = DependencyService.Get<IDeviceIdentifier>().GetIdentifier()
                     Platform = platform,
                     Udid = uid
                });
			if (sessionResponse.StatusCode == HttpStatusCode.Created) {
				this.client.Token = sessionResponse.Result.Session.Token;
				this.UserId = sessionResponse.Result.Session.UserId;
				return sessionResponse.Result.Session.UserId;
			} 
			else if ((int)sessionResponse.StatusCode == 422) {
				// Add logout
				return -1;
			}
			return 0;
		}

		public async Task<List<Quickblox.Sdk.Modules.UsersModule.Models.User>> GetUserByTag(String tag)
		{
			var usersResponse = await this.client.UsersClient.GetUserByTagsAsync (new string[] { tag }, 1, 100);
			if (await HandleResponse(usersResponse, HttpStatusCode.OK)) {
				return usersResponse.Result.Items.Select(userResponse => userResponse.User).ToList();
			} 

			return new List<Quickblox.Sdk.Modules.UsersModule.Models.User> ();
		}

		private async Task<bool> HandleResponse(HttpResponse response, HttpStatusCode resultStatusCode)
		{
			switch (response.StatusCode) {
			case HttpStatusCode.NotFound:
				{
                    this.showInternetNotification();
                }
				break;
			case HttpStatusCode.Unauthorized:
				{
					
				}
				break;
			default:
				break;
			}

			return response.StatusCode == resultStatusCode;
		}

		public async Task<byte[]> GetImageAsync(int blobId){
			var downloadResponse = await client.ContentClient.DownloadFileById(blobId);
			if (downloadResponse.StatusCode == HttpStatusCode.OK)
			{
				return downloadResponse.Result;
			}

			return null;
		}

		public async Task<Quickblox.Sdk.Modules.UsersModule.Models.User> GetUserAsync(int qbUserId)
		{
			var response = await this.client.UsersClient.GetUserByIdAsync(qbUserId);
			if (await HandleResponse(response, HttpStatusCode.OK))
			{
				return response.Result.User;
			}

			return null;
		}

		public async Task<List<Quickblox.Sdk.Modules.UsersModule.Models.User>> GetUsersByIdsAsync (string ids)
		{
			try{
				var retriveUserRequest = new RetrieveUsersRequest();
				retriveUserRequest.PerPage = 100;
				var aggregator = new FilterAggregator();
				aggregator.Filters.Add(new RetrieveUserFilter<int>(UserOperator.In, () => new Quickblox.Sdk.Modules.UsersModule.Models.User().Id, ids));
				retriveUserRequest.Filter = aggregator;
				var response = await this.client.UsersClient.RetrieveUsersAsync(retriveUserRequest);
				if (await HandleResponse(response, HttpStatusCode.OK))
				{
					return response.Result.Items.Select(userResponse => userResponse.User).ToList();
				}
			}
			catch (Exception ex)
			{
			}

			return new List<Quickblox.Sdk.Modules.UsersModule.Models.User> ();
		}

		public async Task<UserResponse> UpdateUserDataAsync(int qbUserId, UserRequest updateUserRequest)
		{
			var updateData = new UpdateUserRequest();
			updateData.User = updateUserRequest;
			var response = await this.client.UsersClient.UpdateUserAsync(qbUserId, updateData);
			if (await HandleResponse(response, HttpStatusCode.OK))
			{
				return response.Result;
			}

			return null;
		}

		public async Task<List<Message>> GetMessagesAsync(string dialogId)
		{
			var retrieveMessagesRequest = new RetrieveMessagesRequest ();
			var aggregator = new FilterAggregator ();
			aggregator.Filters.Add (new FieldFilter<string> (() => new Message ().ChatDialogId, dialogId));
			aggregator.Filters.Add (new SortFilter<long> (SortOperator.Desc, () => new Message ().DateSent));
			retrieveMessagesRequest.Filter = aggregator;

			var responseResult = await client.ChatClient.GetMessagesAsync (retrieveMessagesRequest);
			if (await HandleResponse (responseResult, HttpStatusCode.OK)) {
				return responseResult.Result.Items.ToList();
			}

			return new List<Message>();
		}

		public async Task<bool> DeleteDialogAsync (string dialogId)
		{			
			var dialogResponse = await client.ChatClient.DeleteDialogAsync(dialogId);
			return await HandleResponse(dialogResponse, HttpStatusCode.OK);
		}

		public async Task<Dialog> GetDialogAsync(int[] userIds)
		{
			var retrieveDialogsRequest = new RetrieveDialogsRequest();
            retrieveDialogsRequest.Limit = 100;

            var filterAgreaggator = new FilterAggregator ();
            filterAgreaggator.Filters.Add (new FieldFilterWithOperator<int> (SearchOperators.In, () => new DialogResponse ().Type, (int)DialogType.Private));
			retrieveDialogsRequest.Filter = filterAgreaggator;
			var response = await client.ChatClient.GetDialogsAsync(retrieveDialogsRequest);
			if (await HandleResponse(response, HttpStatusCode.OK) && response.Result.Items.Any()) {
				var dialog = response.Result.Items.FirstOrDefault (d => d.OccupantsIds.Contains (userIds [0]) && d.OccupantsIds.Contains (userIds [1]));
				return dialog;
			}

			return null;
		}

		public async Task<Dialog> GetDialogAsync(string dialogId)
		{
			var retrieveDialogsRequest = new RetrieveDialogsRequest();
			var filterAgreaggator = new FilterAggregator ();
			filterAgreaggator.Filters.Add(new FieldFilterWithOperator<string>(SearchOperators.In, () => new DialogResponse().Id, dialogId));
			retrieveDialogsRequest.Filter = filterAgreaggator;
			var response = await client.ChatClient.GetDialogsAsync(retrieveDialogsRequest);
			if (await HandleResponse(response, HttpStatusCode.OK) && response.Result.Items.Any()) {
				return response.Result.Items[0];
			}

			return null;
		}

		public async Task<List<Dialog>> GetDialogsAsync(List<DialogType> dialogTypeParams)
		{
			var retrieveDialogsRequest = new RetrieveDialogsRequest();

			//TODO: change limit
			retrieveDialogsRequest.Limit = 100;
			if (dialogTypeParams != null) {
				var filterAgreaggator = new FilterAggregator ();
				var dialogTypes = string.Join (",", dialogTypeParams.Select(type => (int)type));
				filterAgreaggator.Filters.Add (new FieldFilterWithOperator<int> (SearchOperators.In, () => new DialogResponse ().Type, dialogTypes));
				retrieveDialogsRequest.Filter = filterAgreaggator;
			}
			var response = await client.ChatClient.GetDialogsAsync(retrieveDialogsRequest);
			if (await HandleResponse(response, HttpStatusCode.OK)) {
				return response.Result.Items.ToList();
			}

			return new List<Dialog>();
		}


		public async Task<Dialog> CreateDialogAsync(string dialogName, string userIds, DialogType dialogType = DialogType.Private)
		{
			var dialogResponse = await this.client.ChatClient.CreateDialogAsync (dialogName, dialogType, userIds);
			if (await HandleResponse(dialogResponse, HttpStatusCode.Created)) {
				return dialogResponse.Result;
			}

			return null;
		}

		public async Task<Dialog> UpdateDialogAsync(string dialogId, List<int> addedUsers = null, List<int> deletedUsers = null, string name = null, string photo = null)
		{
			var updateDialog = new UpdateDialogRequest ();
			updateDialog.DialogId = dialogId;

			if (addedUsers != null) {
				updateDialog.PushAll = new EditedOccupants () {
					OccupantsIds = addedUsers
				};
			}

			if (deletedUsers != null) {
				updateDialog.PullAll = new EditedOccupants () {
					OccupantsIds = deletedUsers
				};
			}

			updateDialog.Name = name;
			updateDialog.PhotoLink = photo;

			var dialogResponse = await this.client.ChatClient.UpdateDialogAsync(updateDialog);
			if (dialogResponse.StatusCode == HttpStatusCode.OK) {
				return dialogResponse.Result;
			}

			return null;
		}
	}
}

