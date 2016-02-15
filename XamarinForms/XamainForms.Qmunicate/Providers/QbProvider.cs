using System;
using Quickblox.Sdk;
using System.Threading.Tasks;
using Quickblox.Sdk.GeneralDataModel.Response;
using System.Net;
using Quickblox.Sdk.GeneralDataModel.Models;
using Xamarin.Forms;
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
using Quickblox.Sdk.Modules.ContentModule.Requests;
using Quickblox.Sdk.Modules.ContentModule.Models;
using Quickblox.Sdk.Http;
using Quickblox.Sdk.Modules.NotificationModule.Requests;
using Quickblox.Sdk.Modules.NotificationModule.Models;
using Quickblox.Sdk.Logger;
using Xamarin.Forms;
using XamarinForms.Qmunicate.Repository;
using Quickblox.Sdk.Modules.ChatXmppModule;
using XamarinForms.Qmunicate.Interfaces;

namespace XamarinForms.Qmunicate
{
	public class QbProvider
	{
		private readonly QuickbloxClient client = new QuickbloxClient(ApplicationKeys.ApplicationId,
			ApplicationKeys.AuthorizationKey,
			ApplicationKeys.AuthorizationSecret,
			ApplicationKeys.ApiBaseEndpoint,
			ApplicationKeys.ChatEndpoint,
			new QbLogger());

		public int UserId { get; set; }

		public QbProvider ()
		{

		}

        public ChatXmppClient GetXmppClient()
        {
            return client.ChatXmppClient;
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

		public async Task<int> LoginWithFbUserAsync(String accessToken)
		{
			var sessionResponse = await this.client.AuthenticationClient.CreateSessionWithSocialNetworkKey("facebook",
				"public_profile",
				accessToken,
				null,
				new DeviceRequest() 
				{ 
					Platform = Device.OS == TargetPlatform.iOS ? Platform.ios : Platform.android,
                    Udid = DependencyService.Get<IDeviceIdentifier>().GetIdentifier()
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


		private async Task<bool> HandleResponse(HttpResponse response, HttpStatusCode resultStatusCode)
		{
			switch (response.StatusCode) {
			case HttpStatusCode.NotFound:
				{ 
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

		public async Task<Quickblox.Sdk.Modules.UsersModule.Models.User> GetUserProfile(int qbUserId)
		{
			try{
				var response = await this.client.UsersClient.GetUserByIdAsync(qbUserId);
				if (await HandleResponse(response, HttpStatusCode.OK))
				{
					return response.Result.User;
				}
			}
			catch (Exception ex)
			{
			}

			return null;
		}

		public async Task<List<Quickblox.Sdk.Modules.UsersModule.Models.User>> GetProfilesByIds (IEnumerable<long> ids)
		{
			try{
				var retriveUserRequest = new RetrieveUsersRequest();
				var idsString = String.Join(",", ids);
				var aggregator = new FilterAggregator();
				aggregator.Filters.Add(new RetrieveUserFilter<int>(UserOperator.In, () => new Quickblox.Sdk.Modules.UsersModule.Models.User().Id, idsString));
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

		public async Task<UserResponse> UpdateUserData(int qbUserId, UserRequest updateUserRequest)
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

		public async Task<List<MessageTable>> GetMessages(string dialogId)
		{
			List<MessageTable> messages = new List<MessageTable> ();
			var retrieveMessagesRequest = new RetrieveMessagesRequest ();
			var aggregator = new FilterAggregator ();
			aggregator.Filters.Add (new FieldFilter<string> (() => new Message ().ChatDialogId, dialogId));
			aggregator.Filters.Add (new SortFilter<long> (SortOperator.Desc, () => new Message ().DateSent));
			retrieveMessagesRequest.Filter = aggregator;

			var responseResult = await client.ChatClient.GetMessagesAsync (retrieveMessagesRequest);
			if (await HandleResponse (responseResult, HttpStatusCode.OK)) {
				foreach (var message in responseResult.Result.Items) {
					if (!string.IsNullOrWhiteSpace (message.MessageText)) {
						var chatMessage = new MessageTable ();
						chatMessage.Text = message.MessageText;
						chatMessage.DateSent = message.DateSent.ToDateTime ();
						chatMessage.SenderId = message.SenderId;
						chatMessage.MessageId = message.Id;
						if (message.RecipientId.HasValue)
							chatMessage.RecepientId = message.RecipientId.Value;
						chatMessage.DialogId = message.ChatDialogId;
						chatMessage.IsRead = message.Read == 1;
						messages.Add (chatMessage);
					}
				} 
			}

			return messages;
		}

		public async Task<bool> DeleteDialog (string dialogId)
		{			
			var dialogResponse = await client.ChatClient.DeleteDialogAsync(dialogId);
			return await HandleResponse(dialogResponse, HttpStatusCode.OK);
		}

		public async Task<DialogTable> GetDialog(int[] userIds)
		{
			var retrieveDialogsRequest = new RetrieveDialogsRequest();
			var filterAgreaggator = new FilterAggregator ();
			filterAgreaggator.Filters.Add(new FieldFilterWithOperator<int[]>(SearchOperators.All, () => new DialogResponse().OccupantsIds, userIds));
			retrieveDialogsRequest.Filter = filterAgreaggator;
			var response = await client.ChatClient.GetDialogsAsync(retrieveDialogsRequest);
			if (await HandleResponse(response, HttpStatusCode.OK) && response.Result.Items.Any()) {
				return new DialogTable(response.Result.Items[0]);
			}

			return null;
		}

		public async Task<DialogTable> GetDialog(string dialogId)
		{
			var retrieveDialogsRequest = new RetrieveDialogsRequest();
			var filterAgreaggator = new FilterAggregator ();
			filterAgreaggator.Filters.Add(new FieldFilterWithOperator<string>(SearchOperators.In, () => new DialogResponse().Id, dialogId));
			retrieveDialogsRequest.Filter = filterAgreaggator;
			var response = await client.ChatClient.GetDialogsAsync(retrieveDialogsRequest);
			if (await HandleResponse(response, HttpStatusCode.OK) && response.Result.Items.Any()) {
				return new DialogTable(response.Result.Items[0]);
			}

			return null;
		}

		public async Task<List<DialogTable>> GetDialogs()
		{
			var dialogs = new List<DialogTable> ();
			var retrieveDialogsRequest = new RetrieveDialogsRequest();
			retrieveDialogsRequest.Limit = 10;
			var response = await client.ChatClient.GetDialogsAsync(retrieveDialogsRequest);
			if (await HandleResponse(response, HttpStatusCode.OK)) {
				dialogs = response.Result.Items.Select(d => new DialogTable(d)).ToList();
			}

			return dialogs;
		}


		public async Task<Dialog> CreateDialogAsync(string userId)
		{
			var dialogResponse = await this.client.ChatClient.CreateDialogAsync (userId, DialogType.Private, userId);
			if (await HandleResponse(dialogResponse, HttpStatusCode.Created)) {
				return dialogResponse.Result;
			}

			return null;
		}

		public async Task<int?> UploadPrivateImage(byte[] imageBytes)
		{
			var createFileRequest = new CreateFileRequest()
			{
				Blob = new BlobRequest()
				{
					Name = String.Format("image_{0}.jpeg", Guid.NewGuid()),
					IsPublic = false
				}
			};

			var createFileInfoResponse = await client.ContentClient.CreateFileInfoAsync(createFileRequest);

			if (await HandleResponse (createFileInfoResponse, HttpStatusCode.Created)) {
				var uploadFileRequest = new UploadFileRequest {
					BlobObjectAccess = createFileInfoResponse.Result.Blob.BlobObjectAccess,
					FileContent = new BytesContent () {
						Bytes = imageBytes,
						ContentType = "image/jpg",
					}
				};

				var uploadFileResponse = await client.ContentClient.FileUploadAsync (uploadFileRequest);

				if (!await HandleResponse (createFileInfoResponse, HttpStatusCode.Created))
					return null;

				var blobUploadCompleteRequest = new BlobUploadCompleteRequest {
					BlobUploadSize = new BlobUploadSize () { Size = (uint)imageBytes.Length }
				};
				var response = await client.ContentClient.FileUploadCompleteAsync (createFileInfoResponse.Result.Blob.Id, blobUploadCompleteRequest);
				if (!await HandleResponse (response, HttpStatusCode.OK))
					return null;
				return createFileInfoResponse.Result.Blob.Id;
			} else {
				return null;
			}
		}

		public async Task<bool> UnsubscribeForPushNotification(string deviceUid){
			var result = false;
			var subscriptions = await client.NotificationClient.GetSubscriptionsAsync ();
			if (await HandleResponse (subscriptions, HttpStatusCode.OK)) {
				var deletedSubscription = subscriptions.Result.FirstOrDefault (s => s.Subscription.DeviceRequest.Udid == deviceUid);
				if (deletedSubscription != null) {
					var deletedResponse = await client.NotificationClient.DeleteSubscriptionsAsync (deletedSubscription.Subscription.Id);
					if (await HandleResponse (deletedResponse, HttpStatusCode.OK)) {
						result = true;
					}
				}
			}

			return result;
		}

		public async Task<bool> SubscribeForPushNotification(string pushtoken, string deviceUid)
		{
			var result = false;
			var settings = new CreateSubscriptionsRequest () {
				DeviceRequest = new DeviceRequest () {
					Platform = Platform.ios, 
					Udid = deviceUid

				},
				PushToken = new PushToken () {
					ClientIdentificationSequence = pushtoken,
					#if DEBUG
					Environment = Quickblox.Sdk.Modules.NotificationModule.Models.Environment.development
					#else
					Environment = Quickblox.Sdk.Modules.NotificationModule.Models.Environment.production
					#endif
				},
				Name = NotificationChannelType.apns
			};

			var createSubscribtionResponse = await client.NotificationClient.CreateSubscriptionsAsync (settings);
			if (await HandleResponse (createSubscribtionResponse, HttpStatusCode.Created)) {
				result = true;
			}

			return result;
		}
	}
}

